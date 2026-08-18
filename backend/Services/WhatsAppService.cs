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
            var urlService = _config["WhatsAppService:Url"] ?? "http://localhost:3001/send";

            var message = $"Bonjour {prenom}, votre code de r\u00e9initialisation WicStock est : {code}. Il expire dans 15 minutes.";

            var payload = new
            {
                to = numeroDestinataire,
                message = message
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Logs temporaires pour debug : afficher l'URL, le numéro et le payload
            Console.WriteLine($"[WHATSAPP SERVICE] Appel vers: {urlService}");
            Console.WriteLine($"[WHATSAPP SERVICE] Destinataire (tel transmis) : {numeroDestinataire}");
            Console.WriteLine($"[WHATSAPP SERVICE] Payload JSON : {json}");

            var response = await _httpClient.PostAsync(urlService, content);

            if (!response.IsSuccessStatusCode)
            {
                var erreur = await response.Content.ReadAsStringAsync();
                throw new Exception($"Erreur service WhatsApp : {erreur}");
            }
        }
    }
}