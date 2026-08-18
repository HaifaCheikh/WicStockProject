using System.Net.Http.Json;
using WicStock.Web.Models.Dtos;

namespace WicStock.Web.Services
{
    public class AuthService
    {
        private readonly HttpClient _http;
        private readonly LocalStorageService _localStorage;
        private readonly ApiAuthenticationStateProvider _authStateProvider;

        public AuthService(HttpClient http, LocalStorageService localStorage,
            ApiAuthenticationStateProvider authStateProvider)
        {
            _http = http;
            _localStorage = localStorage;
            _authStateProvider = authStateProvider;
        }

        public async Task<(bool Succes, string Message)> Login(LoginDto dto)
        {
            var reponse = await _http.PostAsJsonAsync("api/Auth/login", dto);

            if (!reponse.IsSuccessStatusCode)
                return (false, "Email ou mot de passe incorrect.");

            var resultat = await reponse.Content.ReadFromJsonAsync<AuthResponseDto>();
            if (resultat == null)
                return (false, "Erreur inattendue.");

            await _localStorage.SetItemAsync("authToken", resultat.Token);
            await _localStorage.SetItemAsync("userRole", resultat.Role);

            _authStateProvider.NotifierUtilisateurConnecte();

            return (true, "Connexion réussie.");
        }

        public async Task Logout()
        {
            await _localStorage.RemoveItemAsync("authToken");
            await _localStorage.RemoveItemAsync("userRole");
            _authStateProvider.NotifierDeconnexion();
        }

        public async Task<string?> ObtenirRole()
        {
            return await _localStorage.GetItemAsync("userRole");
        }

        public async Task<(bool Succes, string Message)> Register(RegisterDto dto)
        {
            var reponse = await _http.PostAsJsonAsync("api/Auth/register", dto);

            if (!reponse.IsSuccessStatusCode)
            {
                var erreur = await reponse.Content.ReadAsStringAsync();
                return (false, $"Erreur lors de l'inscription : {erreur}");
            }

            return (true, "Inscription réussie.");
        }

        public async Task<(bool Succes, string Message, string? WhatsAppLink)> ForgotPassword(ForgotPasswordDto dto)
        {
            var reponse = await _http.PostAsJsonAsync("api/Auth/forgot-password", dto);

            if (!reponse.IsSuccessStatusCode)
            {
                var erreur = await reponse.Content.ReadAsStringAsync();
                return (false, erreur, null);
            }

            var resultat = await reponse.Content.ReadFromJsonAsync<ForgotPasswordResponse>();
            return (true, resultat?.Message ?? "Code envoyé.", resultat?.WhatsAppLink);
        }

        public async Task<(bool Succes, string Message)> ResetPassword(ResetPasswordDto dto)
        {
            var reponse = await _http.PostAsJsonAsync("api/Auth/reset-password", dto);

            if (!reponse.IsSuccessStatusCode)
            {
                var erreur = await reponse.Content.ReadAsStringAsync();
                return (false, erreur);
            }

            return (true, "Mot de passe réinitialisé avec succès.");
        }
    }
}