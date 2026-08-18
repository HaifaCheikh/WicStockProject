using System.Net.Http.Headers;

namespace WicStock.Web.Services
{
    public class AuthorizationMessageHandler : DelegatingHandler
    {
        private readonly LocalStorageService _localStorage;

        public AuthorizationMessageHandler(LocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = await _localStorage.GetItemAsync("authToken");

            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}