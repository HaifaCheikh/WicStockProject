using System.Net.Http.Json;
using WicStock.Web.Models.Dtos;

namespace WicStock.Web.Services
{
    public class AssistantIAService
    {
        private readonly HttpClient _http;

        public AssistantIAService(HttpClient http)
        {
            _http = http;
        }

        public async Task<ReponseIA?> PoserQuestion(QuestionIARequest requete)
        {
            try
            {
                var reponse = await _http.PostAsJsonAsync("ask", requete);
                if (!reponse.IsSuccessStatusCode)
                    return null;

                return await reponse.Content.ReadFromJsonAsync<ReponseIA>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AssistantIAService Error] {ex.Message}");
                return null;
            }
        }
    }
}
