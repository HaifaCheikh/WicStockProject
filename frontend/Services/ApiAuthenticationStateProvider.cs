using Microsoft.AspNetCore.Components.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace WicStock.Web.Services
{
    public class ApiAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly LocalStorageService _localStorage;

        public ApiAuthenticationStateProvider(LocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var token = await _localStorage.GetItemAsync("authToken");

            var identity = new ClaimsIdentity();

            if (!string.IsNullOrWhiteSpace(token))
            {
                try
                {
                    var handler = new JwtSecurityTokenHandler();
                    var jwt = handler.ReadJwtToken(token);

                    if (jwt.ValidTo > DateTime.UtcNow)
                    {
                        identity = new ClaimsIdentity(jwt.Claims, "jwt", ClaimTypes.Name, ClaimTypes.Role);
                    }
                    else
                    {
                        await _localStorage.RemoveItemAsync("authToken");
                        await _localStorage.RemoveItemAsync("userRole");
                    }
                }
                catch
                {
                    await _localStorage.RemoveItemAsync("authToken");
                    await _localStorage.RemoveItemAsync("userRole");
                }
            }

            var user = new ClaimsPrincipal(identity);
            return new AuthenticationState(user);
        }

        public void NotifierUtilisateurConnecte()
        {
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        public void NotifierDeconnexion()
        {
            var anonyme = new ClaimsPrincipal(new ClaimsIdentity());
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(anonyme)));
        }
    }
}