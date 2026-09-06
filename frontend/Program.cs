using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using WicStock.Web;
using WicStock.Web.Services;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddMudServices(config =>
{
    config.PopoverOptions.ThrowOnDuplicateProvider = false;
});
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://wicstock-api.onrender.com/";
if (!apiBaseUrl.EndsWith("/")) apiBaseUrl += "/";

builder.Services.AddScoped<LocalStorageService>();
builder.Services.AddScoped<AuthorizationMessageHandler>();

builder.Services.AddHttpClient("WicStockAPI", client =>
    client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<AuthorizationMessageHandler>();

builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>()
    .CreateClient("WicStockAPI"));

var iaBaseUrl = builder.Configuration["WicStockIAUrl"] ?? builder.Configuration["AiBaseUrl"] ?? "https://wicstock-ai.onrender.com/";
if (!iaBaseUrl.EndsWith("/")) iaBaseUrl += "/";

builder.Services.AddHttpClient("WicStockIA", client =>
    client.BaseAddress = new Uri(iaBaseUrl));

// Client HTTP PUBLIC (sans token) — pour le catalogue boutique
builder.Services.AddHttpClient("WicStockPublic", client =>
    client.BaseAddress = new Uri(apiBaseUrl));

builder.Services.AddScoped<AssistantIAService>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    return new AssistantIAService(factory.CreateClient("WicStockIA"));
});

builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<ApiAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<ApiAuthenticationStateProvider>());

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ProduitService>();
builder.Services.AddScoped<UtilisateurService>();
builder.Services.AddScoped<CatalogueService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<ProfilService>();
builder.Services.AddScoped<NotificationClientService>();
builder.Services.AddScoped<CommandeBadgeService>(sp => 
{
    var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient("WicStockAPI");
    var authService = sp.GetRequiredService<AuthService>();
    return new CommandeBadgeService(http, authService);
});

builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<LivraisonService>();
builder.Services.AddScoped<FeedbackService>();
builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<BoutiqueService>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    return new BoutiqueService(factory.CreateClient("WicStockPublic"));
});

await builder.Build().RunAsync();
