using System.Text;
using System.Text.Json;

namespace WicStock_.Services
{
    public class WhatsAppService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public WhatsAppService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        public async Task EnvoyerCodeReinitialisationAsync(string numeroDestinataire, string prenom, string code)
        {
            var urlService = _config["WhatsAppService:Url"] 
                ?? _config["WhatsAppService__Url"] 
                ?? _config["WHATSAPPSERVICE_URL"] 
                ?? _config["WHATSAPP_SERVICE_URL"] 
                ?? "http://localhost:3001/send";

            urlService = urlService.Trim().Trim('"', '\'');
            if (!urlService.EndsWith("/send", StringComparison.OrdinalIgnoreCase))
            {
                urlService = urlService.TrimEnd('/') + "/send";
            }

            var message = $"Bonjour {prenom}, votre code de réinitialisation WicStock est : {code}. Il expire dans 15 minutes.";

            var payload = new
            {
                to = numeroDestinataire,
                message = message
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            Console.WriteLine($"[WHATSAPP SERVICE] Envoi vers {urlService} pour le numéro {numeroDestinataire}...");

            var response = await _httpClient.PostAsync(urlService, content);

            if (!response.IsSuccessStatusCode)
            {
                var erreur = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[WHATSAPP SERVICE ERROR] Status: {response.StatusCode}, Content: {erreur}");
                throw new Exception($"Erreur service WhatsApp ({response.StatusCode}) : {erreur}");
            }
        }
    }
}