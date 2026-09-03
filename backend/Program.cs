using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;
using WicStock_.Hubs;
using WicStock_.Services;

var builder = WebApplication.CreateBuilder(args);

// Disable reloadOnChange for file configuration sources to prevent Linux inotify limit crashes on Render/Docker
foreach (var source in builder.Configuration.Sources.OfType<Microsoft.Extensions.Configuration.FileConfigurationSource>())
{
    source.ReloadOnChange = false;
}

string ConvertPostgresConnectionString(string? connString)
{
    if (string.IsNullOrWhiteSpace(connString)) return string.Empty;
    if (connString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
        connString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
    {
        try
        {
            var uri = new Uri(connString);
            var userInfo = uri.UserInfo.Split(':');
            var username = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : "";
            var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
            var db = uri.AbsolutePath.TrimStart('/');
            var port = uri.Port > 0 ? uri.Port : 5432;
            return $"Host={uri.Host};Port={port};Database={db};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CONFIG WARNING] Failed to parse URI connection string: {ex.Message}");
        }
    }
    return connString;
}

var rawConnStr = builder.Configuration.GetConnectionString("DefaultConnection");
var effectiveConnStr = ConvertPostgresConnectionString(rawConnStr);

Console.WriteLine($"[CONFIG] Effective ConnectionString Host configured: {!string.IsNullOrEmpty(effectiveConnStr)}");

// Base de données PostgreSQL (Aiven/Supabase/Render - gratuit)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(effectiveConnStr));

// Services métier
builder.Services.AddScoped<JwtService>();
builder.Services.AddSingleton<PasswordResetService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<IAExplicationService>();
builder.Services.AddScoped<IMetriquesStockService, MetriquesStockService>();
builder.Services.AddScoped<IAnalyseSurstockService, AnalyseSurstockService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<LemonSqueezyService>();
builder.Services.AddHttpClient<LemonSqueezyService>();

// SignalR
builder.Services.AddSignalR();

// HttpClient pour l'IA
builder.Services.AddHttpClient("WicStockIA", client =>
    client.BaseAddress = new Uri("http://localhost:8001/"));

// Authentification JWT
var jwtKey = builder.Configuration["Jwt:Key"] ?? "WicStockDefaultFallbackSecretKey2026Min32Chars!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "WicStock";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "WicStockUsers";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/notifications"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddEndpointsApiExplorer();

// Swagger avec support du bouton "Authorize"
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "WicStock API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "Entrez : Bearer {votre token}",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("PermettreBlazor", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

builder.Services.AddHttpClient<WhatsAppService>();


var app = builder.Build();

// 1. CORS MUST be the very first middleware so ALL responses (including 500 errors) carry CORS headers
app.UseCors("PermettreBlazor");

// 2. Exception Handler ensures 500 errors return clear JSON instead of unhandled crashes
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        var exceptionHandlerFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
        var ex = exceptionHandlerFeature?.Error;
        var errorMessage = ex?.Message ?? "Une erreur serveur interne s'est produite.";
        Console.WriteLine($"[API ERROR 500] {ex}");
        await context.Response.WriteAsJsonAsync(new { message = errorMessage, detail = ex?.InnerException?.Message });
    });
});

try
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await DatabaseSchemaBootstrap.ApplyAsync(db, scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseSchemaBootstrap"));
    }
}
catch (Exception ex)
{
    Console.WriteLine($"[DB BOOTSTRAP WARNING] {ex.Message}");
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "WicStock API v1");
    c.RoutePrefix = "swagger";
});

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Ok(new { status = "WicStock API Online", timestamp = DateTime.UtcNow }));
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();