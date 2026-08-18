using System.Net.Http.Json;
using WicStock.Web.Models.Dtos;

namespace WicStock.Web.Services
{
    public class ProduitService
    {
        private readonly HttpClient _http;

        public ProduitService(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<ProduitDto>> ObtenirTous()
        {
            return await _http.GetFromJsonAsync<List<ProduitDto>>("api/Produit")
                   ?? new List<ProduitDto>();
        }

        public async Task<ProduitDto?> ObtenirParId(int id)
        {
            return await _http.GetFromJsonAsync<ProduitDto>($"api/Produit/{id}");
        }

        public async Task<bool> Creer(ProduitDto produit)
        {
            var reponse = await _http.PostAsJsonAsync("api/Produit", produit);
            return reponse.IsSuccessStatusCode;
        }

        public async Task<bool> Modifier(int id, ProduitDto produit)
        {
            var reponse = await _http.PutAsJsonAsync($"api/Produit/{id}", produit);
            return reponse.IsSuccessStatusCode;
        }

        public async Task<bool> Supprimer(int id)
        {
            var reponse = await _http.DeleteAsync($"api/Produit/{id}");
            return reponse.IsSuccessStatusCode;
        }

        public async Task<bool> Archiver(int id)
        {
            try
            {
                var reponse = await _http.PatchAsync($"api/Produit/{id}/archiver", null);
                return reponse.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PRODUIT SERVICE ERROR] Erreur archivage produit {id} : {ex.Message}");
                return false;
            }
        }

        public async Task<bool> Desarchiver(int id)
        {
            try
            {
                var reponse = await _http.PatchAsync($"api/Produit/{id}/desarchiver", null);
                return reponse.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PRODUIT SERVICE ERROR] Erreur dÃ©sarchivage produit {id} : {ex.Message}");
                return false;
            }
        }

        public async Task<AnalyseProduitDto?> ObtenirAnalyseSurstock(int produitId)
        {
            try
            {
                var reponse = await _http.PostAsync($"api/Produit/{produitId}/analyse", null);
                if (reponse.IsSuccessStatusCode)
                {
                    return await reponse.Content.ReadFromJsonAsync<AnalyseProduitDto>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PRODUIT SERVICE ERROR] Erreur analyse produit {produitId} : {ex.Message}");
            }
            return null;
        }

        public async Task<bool> ExecuterActionSurstock(int produitId, string typeAction, string label, Dictionary<string, string>? paramsDict)
        {
            try
            {
                var req = new ExecutionActionRequestDto
                {
                    ProduitId = produitId,
                    TypeAction = typeAction,
                    ActionLabel = label,
                    Params = paramsDict
                };
                var reponse = await _http.PostAsJsonAsync($"api/Produit/{produitId}/executer-action", req);
                return reponse.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> AnnulerActionSurstock(int produitId)
        {
            try
            {
                var reponse = await _http.PostAsync($"api/Produit/{produitId}/annuler-action", null);
                return reponse.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PRODUIT SERVICE ERROR] Erreur annulation action produit {produitId} : {ex.Message}");
                return false;
            }
        }
    }
}