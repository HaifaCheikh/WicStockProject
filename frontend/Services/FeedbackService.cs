using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Forms;
using WicStock.Web.Models.Dtos;

namespace WicStock.Web.Services
{
    public class FeedbackService
    {
        private readonly HttpClient _http;

        public FeedbackService(HttpClient http)
        {
            _http = http;
        }

        // --- AVIS CLIENT ---

        public async Task<List<AvisDto>> ObtenirMesAvis()
        {
            try
            {
                return await _http.GetFromJsonAsync<List<AvisDto>>("api/Avis/mes-avis") ?? new List<AvisDto>();
            }
            catch
            {
                return new List<AvisDto>();
            }
        }

        public async Task<AvisDto?> ObtenirAvisCommande(int commandeId)
        {
            try
            {
                var response = await _http.GetAsync($"api/Avis/commande/{commandeId}");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<AvisDto>();
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<List<AvisDto>> ObtenirAvisPublicsProduit(int produitId)
        {
            try
            {
                return await _http.GetFromJsonAsync<List<AvisDto>>($"api/Avis/produit/{produitId}") ?? new List<AvisDto>();
            }
            catch
            {
                return new List<AvisDto>();
            }
        }

        public async Task<(bool Succes, string Message, AvisDto? Avis)> SoumettreAvis(CreerModifierAvisDto dto)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("api/Avis", dto);
                if (response.IsSuccessStatusCode)
                {
                    var avis = await response.Content.ReadFromJsonAsync<AvisDto>();
                    return (true, "Avis enregistré avec succès.", avis);
                }

                var errorMsg = await response.Content.ReadAsStringAsync();
                return (false, string.IsNullOrWhiteSpace(errorMsg) ? "Erreur lors de la soumission de l'avis." : errorMsg, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message, null);
            }
        }

        // --- RÉCLAMATIONS CLIENT ---

        public async Task<List<ReclamationDto>> ObtenirMesReclamations()
        {
            try
            {
                return await _http.GetFromJsonAsync<List<ReclamationDto>>("api/Reclamation/mes-reclamations") ?? new List<ReclamationDto>();
            }
            catch
            {
                return new List<ReclamationDto>();
            }
        }

        public async Task<List<ReclamationDto>> ObtenirReclamationsCommande(int commandeId)
        {
            try
            {
                return await _http.GetFromJsonAsync<List<ReclamationDto>>($"api/Reclamation/commande/{commandeId}") ?? new List<ReclamationDto>();
            }
            catch
            {
                return new List<ReclamationDto>();
            }
        }

        public async Task<(bool Succes, string Message, ReclamationDto? Reclamation)> CreerReclamation(CreerReclamationDto dto)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("api/Reclamation", dto);
                if (response.IsSuccessStatusCode)
                {
                    var rec = await response.Content.ReadFromJsonAsync<ReclamationDto>();
                    return (true, "Réclamation envoyée avec succès.", rec);
                }

                var errorMsg = await response.Content.ReadAsStringAsync();
                return (false, string.IsNullOrWhiteSpace(errorMsg) ? "Erreur lors de l'envoi de la réclamation." : errorMsg, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message, null);
            }
        }

        public async Task<string?> UploadPhoto(IBrowserFile file)
        {
            try
            {
                using var content = new MultipartFormDataContent();
                var fileContent = new StreamContent(file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024));
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
                content.Add(fileContent, "file", file.Name);

                var response = await _http.PostAsync("api/Reclamation/upload-photo", content);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<UploadResultDto>();
                    return result?.Url;
                }
            }
            catch
            {
                // Upload error handling fallback
            }
            return null;
        }

        // --- BACK-OFFICE ADMIN ---

        public async Task<List<ReclamationDto>> ObtenirReclamationsAdmin(string? statut = null)
        {
            try
            {
                var url = string.IsNullOrEmpty(statut) ? "api/Reclamation/admin" : $"api/Reclamation/admin?statut={statut}";
                return await _http.GetFromJsonAsync<List<ReclamationDto>>(url) ?? new List<ReclamationDto>();
            }
            catch
            {
                return new List<ReclamationDto>();
            }
        }

        public async Task<bool> TraiterReclamation(int id, TraiterReclamationDto dto)
        {
            try
            {
                var response = await _http.PutAsJsonAsync($"api/Reclamation/admin/{id}/traiter", dto);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> SupprimerReclamationAdmin(int id)
        {
            try
            {
                var response = await _http.DeleteAsync($"api/Reclamation/admin/{id}");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<AvisDto>> ObtenirToutesAvisAdmin()
        {
            try
            {
                return await _http.GetFromJsonAsync<List<AvisDto>>("api/Avis/admin") ?? new List<AvisDto>();
            }
            catch
            {
                return new List<AvisDto>();
            }
        }

        public async Task<bool> ModererAvis(int id, ModererAvisDto dto)
        {
            try
            {
                var response = await _http.PutAsJsonAsync($"api/Avis/admin/{id}/visibilite", dto);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> SupprimerAvis(int id)
        {
            try
            {
                var response = await _http.DeleteAsync($"api/Avis/admin/{id}");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }

    public class UploadResultDto
    {
        public string Url { get; set; } = string.Empty;
    }
}
