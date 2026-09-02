using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;
using WicStock_.Hubs;
using WicStock_.Services;

var builder = WebApplication.CreateBuilder(args);

// Log effective connection string at startup to help debugging
Console.WriteLine($"[CONFIG] DefaultConnection = {builder.Configuration.GetConnectionString("DefaultConnection")}");

// Base de données
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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
var jwtKey = builder.Configuration["Jwt:Key"]!;
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
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
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

// CORS MUST be before Authentication/Authorization
app.UseCors("PermettreBlazor");

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Ok(new { status = "WicStock API Online", timestamp = DateTime.UtcNow }));
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();