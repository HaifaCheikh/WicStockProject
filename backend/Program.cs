using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Prometheus;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using WicStock_.Hubs;
using WicStock_.Services;

/*
 * =========================================================================================
 * 🎓 CONCEPT D'OBSERVABILITÉ (Pour entretien / défense de projet) :
 * 
 * 1. LOG (Événement ponctuel) :
 *    - Définition : Enregistrement discret horodaté d'un événement précis dans l'application.
 *    - Exemple : "L'utilisateur admin@wicstock.com a créé la commande #1042 à 14:32:05".
 *    - Usage : Débogage ciblé, audit de sécurité et traçabilité métier.
 * 
 * 2. MÉTRIQUE (Agrégation numérique) :
 *    - Définition : Valeur numérique mesurée sur un intervalle de temps (Compteur, Jauge, Histogramme).
 *    - Exemple : "Taux d'erreur HTTP 5xx = 0.5%", "Nombre moyen de requêtes = 45 req/sec", "Mémoire RAM = 120 Mo".
 *    - Usage : Alerting automatique, tableaux de bord temps réel (Grafana) et analyse de tendance.
 * 
 * 3. TRACE (Parcours d'une requête distribuée) :
 *    - Définition : Suivi du cheminement complet d'une requête à travers plusieurs microservices (CorrelationId).
 *    - Exemple : Requête client -> API .NET (span 1) -> Service IA FastAPI (span 2) -> PostgreSQL (span 3).
 *    - Usage : Identification des goulots d'étranglement de latence et diagnostic de pannes distribuées.
 * =========================================================================================
 */

// Configure Serilog logger with CompactJsonFormatter console output
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "WicStock.Api")
    .WriteTo.Console(new CompactJsonFormatter())
    .CreateLogger();

// Allow legacy DateTime behavior (DateTime.Now) with Npgsql PostgreSQL
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

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

// HealthChecks : Live (API running) & Ready (API + EF Core DB connected)
builder.Services.AddHealthChecks()
    .AddCheck("liveness", () => HealthCheckResult.Healthy("API is alive"), tags: new[] { "live" })
    .AddDbContextCheck<AppDbContext>("database", tags: new[] { "ready" });

// Services métier
builder.Services.AddScoped<JwtService>();
builder.Services.AddSingleton<PasswordResetService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<IAExplicationService>();
builder.Services.AddScoped<IMetriquesStockService, MetriquesStockService>();
builder.Services.AddScoped<IAnalyseSurstockService, AnalyseSurstockService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<WicStock_.Services.IAttributService, WicStock_.Services.AttributService>();
builder.Services.AddHttpClient<LemonSqueezyService>();

// SignalR
builder.Services.AddSignalR();

// HttpClient pour l'IA
var iaBaseUrl = builder.Configuration["WicStockIAUrl"] ?? builder.Configuration["AiBaseUrl"] ?? "http://ai:8000/";
if (!iaBaseUrl.EndsWith("/")) iaBaseUrl += "/";
builder.Services.AddHttpClient("WicStockIA", client =>
    client.BaseAddress = new Uri(iaBaseUrl));

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

builder.Services.AddControllers(options =>
{
    // Désactiver la suppression des sources de liaison implicites pour éviter des comportements inattendus
})
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        // Ignorer silencieusement les propriétés JSON inconnues (ex: champs DTO non mappés dans le modèle)
        options.JsonSerializerOptions.UnknownTypeHandling = System.Text.Json.Serialization.JsonUnknownTypeHandling.JsonNode;
    });

// Désactiver la validation automatique du modèle de [ApiController] — les contrôleurs gèrent
// eux-mêmes la validation pour permettre la normalisation des données avant tout rejet.
builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
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

// 2. Correlation ID Middleware (lit Header X-Correlation-Id ou génère un nouveau Guid)
app.Use(async (context, next) =>
{
    const string HeaderName = "X-Correlation-Id";
    string correlationId = context.Request.Headers[HeaderName].FirstOrDefault()
        ?? Guid.NewGuid().ToString();

    context.Response.Headers[HeaderName] = correlationId;

    using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
    {
        await next();
    }
});

// 3. Serilog HTTP Request Logging (Timestamp, Route, Duration, StatusCode)
app.UseSerilogRequestLogging();

// 4. Prometheus metrics middleware & server (/metrics)
app.UseMetricServer();
app.UseHttpMetrics();

// 5. Exception Handler ensures 500 errors return clear JSON instead of unhandled crashes
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
        var totalProds = await db.Produits.CountAsync();
        WicStockMetrics.ProductsTotal.Set(totalProds);
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

// Format de réponse JSON clair pour les HealthChecks
// Mapping explicite : Healthy=200, Degraded=200 (avertissement, pas panne), Unhealthy=503
static Task WriteHealthReportResponse(HttpContext context, HealthReport result)
{
    context.Response.ContentType = "application/json";
    var response = new
    {
        // Utilise ToString() qui retourne "Healthy", "Degraded" ou "Unhealthy" (jamais ambigu)
        status = result.Status.ToString(),
        timestamp = DateTime.UtcNow,
        totalDurationMs = Math.Round(result.TotalDuration.TotalMilliseconds, 2),
        entries = result.Entries.ToDictionary(
            pair => pair.Key,
            pair => new
            {
                // Mapping explicite : évite tout bug si un check retourne Degraded
                status = pair.Value.Status switch
                {
                    HealthStatus.Healthy   => "Healthy",
                    HealthStatus.Degraded  => "Degraded",
                    HealthStatus.Unhealthy => "Unhealthy",
                    _                      => pair.Value.Status.ToString()
                },
                description = pair.Value.Description ?? (pair.Value.Status == HealthStatus.Healthy ? "OK" : pair.Value.Exception?.Message ?? pair.Value.Status.ToString()),
                durationMs = Math.Round(pair.Value.Duration.TotalMilliseconds, 2),
                // Expose l'exception si présente (utile en développement)
                exception = pair.Value.Exception?.Message
            }
        )
    };
    return context.Response.WriteAsJsonAsync(response);
}

// HealthCheck Liveness (API est vivante - idéal pour Render.com Health Check Path)
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live"),
    ResponseWriter = WriteHealthReportResponse
});

// HealthCheck Readiness (API + Base de données opérationnelles)
// Degraded → HTTP 200 (avertissement acceptable), Unhealthy → HTTP 503
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = WriteHealthReportResponse,
    ResultStatusCodes =
    {
        [HealthStatus.Healthy]   = StatusCodes.Status200OK,
        [HealthStatus.Degraded]  = StatusCodes.Status200OK,   // Dégradé ≠ panne
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
    }
});

// Alias /health (compatible avec Render.com par défaut)
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = WriteHealthReportResponse,
    ResultStatusCodes =
    {
        [HealthStatus.Healthy]   = StatusCodes.Status200OK,
        [HealthStatus.Degraded]  = StatusCodes.Status200OK,
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
    }
});

// GitOps demo endpoint — version bumped automatically by CI/CD pipeline
// Each deployment updates the image tag in gitops/environments/dev/values.yaml
// Argo CD detects the Git change and rolls out the new pod automatically
app.MapGet("/", () => Results.Ok(new
{
    status      = "WicStock API Online",
    version     = "2.1.0-gitops",
    k8s_managed = true,
    deployed_at = "2026-09-15T23:47:00Z",   // bumped by CI job update-gitops-manifests
    timestamp   = DateTime.UtcNow
}));
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();