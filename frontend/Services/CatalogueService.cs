using System.Net.Http.Json;
using System.Text.Json;
using WicStock.Web.Models.Dtos;

namespace WicStock.Web.Services
{
    public class CatalogueService
    {
        private readonly HttpClient _http;

        public CatalogueService(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<CatalogueProduitDto>> ObtenirCatalogue()
        {
            return await _http.GetFromJsonAsync<List<CatalogueProduitDto>>("api/Produit/catalogue")
                   ?? new List<CatalogueProduitDto>();
        }

        public async Task<List<MaCommandeDto>> ObtenirMesCommandes()
        {
            return await _http.GetFromJsonAsync<List<MaCommandeDto>>("api/HistoriqueVente/mes-commandes")
                   ?? new List<MaCommandeDto>();
        }

        public async Task<List<CommandeManagerDto>> ObtenirToutesLesCommandes()
        {
            return await _http.GetFromJsonAsync<List<CommandeManagerDto>>("api/HistoriqueVente")
                   ?? new List<CommandeManagerDto>();
        }

        public async Task<CommandeCreateResultDto?> PasserCommande(CommandeCreateDto dto)
        {
            var response = await _http.PostAsJsonAsync("api/HistoriqueVente", dto);
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<CommandeCreateResultDto>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

        public async Task<SuiviCommandeDto?> ObtenirSuiviCommande(int id)
        {
            return await _http.GetFromJsonAsync<SuiviCommandeDto>($"api/HistoriqueVente/{id}/suivi");
        }

        public async Task<bool> AnnulerCommande(int commandeId)
        {
            var response = await _http.DeleteAsync($"api/HistoriqueVente/annuler/{commandeId}");
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> ModifierCommande(int commandeId, CommandeCreateDto dto)
        {
            var response = await _http.PutAsJsonAsync($"api/HistoriqueVente/modifier/{commandeId}", dto);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> AccepterCommande(int commandeId)
        {
            var response = await _http.PutAsync($"api/HistoriqueVente/{commandeId}/accepter", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> RefuserCommande(int commandeId)
        {
            var response = await _http.PutAsync($"api/HistoriqueVente/{commandeId}/refuser", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> SupprimerCommande(int commandeId)
        {
            var response = await _http.DeleteAsync($"api/HistoriqueVente/{commandeId}");
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> ConfirmerCommandeSurCommande(int commandeId, ConfirmerCommandeRequestDto? dto = null)
        {
            var response = await _http.PutAsJsonAsync($"api/HistoriqueVente/{commandeId}/confirmer", dto ?? new ConfirmerCommandeRequestDto());
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DemarrerPreparation(int commandeId)
        {
            var response = await _http.PutAsync($"api/HistoriqueVente/{commandeId}/demarrer-preparation", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> MarquerPrete(int commandeId)
        {
            var response = await _http.PutAsync($"api/HistoriqueVente/{commandeId}/marquer-prete", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<List<ResponsableDto>> ObtenirResponsables()
        {
            return await _http.GetFromJsonAsync<List<ResponsableDto>>("api/HistoriqueVente/responsables")
                   ?? new List<ResponsableDto>();
        }

        public async Task<(bool Succes, string Message)> AssignerCommande(int commandeId, int responsableId)
        {
            var response = await _http.PostAsJsonAsync($"api/HistoriqueVente/{commandeId}/assigner", new AssignerCommandeDto { ResponsableId = responsableId });
            if (response.IsSuccessStatusCode)
            {
                return (true, "Commande assignée avec succès.");
            }
            var msg = await response.Content.ReadAsStringAsync();
            msg = msg.Trim('"', ' ', '\r', '\n');
            return (false, string.IsNullOrWhiteSpace(msg) ? "Erreur lors de l'assignation." : msg);
        }
    }
}
