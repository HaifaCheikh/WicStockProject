using System.Net.Http.Json;
using WicStock.Web.Models.Dtos;

namespace WicStock.Web.Services
{
    public class LivraisonService
    {
        private readonly HttpClient _http;

        public LivraisonService(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<LivraisonCommandeDto>> ObtenirMesLivraisons()
        {
            try
            {
                var result = await _http.GetFromJsonAsync<List<LivraisonCommandeDto>>("api/livraison/mes-livraisons");
                return result ?? new List<LivraisonCommandeDto>();
            }
            catch
            {
                return new List<LivraisonCommandeDto>();
            }
        }

        public async Task<List<LivraisonCommandeDto>> ObtenirLivraisonsDisponibles()
        {
            try
            {
                var result = await _http.GetFromJsonAsync<List<LivraisonCommandeDto>>("api/livraison/disponibles");
                return result ?? new List<LivraisonCommandeDto>();
            }
            catch
            {
                return new List<LivraisonCommandeDto>();
            }
        }

        public async Task<bool> AutoAssigner(int commandeId)
        {
            try
            {
                var response = await _http.PostAsync($"api/livraison/auto-assigner/{commandeId}", null);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<(bool Succes, string Message)> PasserEnLivraison(int commandeId)
        {
            try
            {
                var response = await _http.PostAsync($"api/livraison/passer-en-livraison/{commandeId}", null);
                if (response.IsSuccessStatusCode) return (true, "Commande passée en livraison avec succès.");

                var error = await response.Content.ReadAsStringAsync();
                return (false, string.IsNullOrWhiteSpace(error) ? "Impossible de passer en livraison." : error);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public async Task<bool> MarquerLivree(int commandeId)
        {
            try
            {
                var response = await _http.PostAsync($"api/livraison/marquer-livree/{commandeId}", null);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<LivreurInfoDto>> ObtenirLivreurs()
        {
            try
            {
                var result = await _http.GetFromJsonAsync<List<LivreurInfoDto>>("api/livraison/livreurs");
                return result ?? new List<LivreurInfoDto>();
            }
            catch
            {
                return new List<LivreurInfoDto>();
            }
        }

        public async Task<bool> AssignerLivreur(int commandeId, int livreurId)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("api/livraison/assigner", new AssignerLivreurDto
                {
                    CommandeId = commandeId,
                    LivreurId = livreurId
                });
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
