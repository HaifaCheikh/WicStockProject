using Microsoft.AspNetCore.Components.Forms;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using WicStock.Web.Models.Dtos;

namespace WicStock.Web.Services
{
    public class ProfilService
    {
        private readonly HttpClient _http;

        public ProfilService(HttpClient http)
        {
            _http = http;
        }

        public async Task<ProfilDto?> ObtenirMonProfil()
        {
            try
            {
                return await _http.GetFromJsonAsync<ProfilDto>("api/Profil/me");
            }
            catch
            {
                return null;
            }
        }

        public async Task<(bool Succes, string Message)> ModifierMonProfil(ModifierProfilDto dto)
        {
            var reponse = await _http.PutAsJsonAsync("api/Profil/me", dto);
            if (reponse.IsSuccessStatusCode)
            {
                return (true, "Profil mis à jour avec succès.");
            }
            var msg = await reponse.Content.ReadAsStringAsync();
            msg = msg.Trim('"', ' ', '\r', '\n');
            return (false, string.IsNullOrWhiteSpace(msg) ? "Erreur lors de la mise à jour." : msg);
        }

        public async Task<(bool Succes, string? PhotoUrl, string Message)> TeleverserPhoto(IBrowserFile file)
        {
            try
            {
                using var content = new MultipartFormDataContent();
                using var fileStream = file.OpenReadStream(maxAllowedSize: 5 * 1024 * 1024);
                var streamContent = new StreamContent(fileStream);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
                content.Add(streamContent, "file", file.Name);

                var reponse = await _http.PostAsync("api/Profil/me/photo", content);
                if (reponse.IsSuccessStatusCode)
                {
                    var result = await reponse.Content.ReadFromJsonAsync<PhotoResponseDto>();
                    return (true, result?.PhotoUrl, "Photo de profil mise à jour.");
                }

                var msg = await reponse.Content.ReadAsStringAsync();
                msg = msg.Trim('"', ' ', '\r', '\n');
                return (false, null, string.IsNullOrWhiteSpace(msg) ? "Erreur lors de l'envoi de la photo." : msg);
            }
            catch (Exception ex)
            {
                return (false, null, $"Erreur : {ex.Message}");
            }
        }

        public async Task<(bool Succes, string Message)> ChangerMotDePasse(ChangerMotDePasseDto dto)
        {
            var reponse = await _http.PutAsJsonAsync("api/Profil/changer-mot-de-passe", dto);
            if (reponse.IsSuccessStatusCode)
            {
                return (true, "Mot de passe modifié avec succès.");
            }
            var msg = await reponse.Content.ReadAsStringAsync();
            msg = msg.Trim('"', ' ', '\r', '\n');
            return (false, string.IsNullOrWhiteSpace(msg) ? "Erreur lors du changement de mot de passe." : msg);
        }
    }
}
