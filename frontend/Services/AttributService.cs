using System.Net.Http.Json;
using WicStock.Web.Models.Dtos;

namespace WicStock.Web.Services
{
    public class AttributService
    {
        private readonly HttpClient _http;
        private readonly Dictionary<string, List<AttributValeurDto>> _cache = new();

        public AttributService(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<AttributValeurDto>> ObtenirAttributsAsync(string type, bool tous = false, bool forceReload = false)
        {
            string cacheKey = $"{type.ToLower()}_{tous}";
            if (!forceReload && _cache.TryGetValue(cacheKey, out var cached))
            {
                return cached;
            }

            try
            {
                var result = await _http.GetFromJsonAsync<List<AttributValeurDto>>($"api/attributs?type={Uri.EscapeDataString(type)}&tous={tous}")
                             ?? new List<AttributValeurDto>();
                _cache[cacheKey] = result;
                return result;
            }
            catch
            {
                return new List<AttributValeurDto>();
            }
        }

        public async Task<bool> AjouterAttributAsync(AttributValeurDto dto)
        {
            var response = await _http.PostAsJsonAsync("api/attributs", dto);
            if (response.IsSuccessStatusCode)
            {
                ClearCache();
                return true;
            }
            return false;
        }

        public async Task<bool> ModifierAttributAsync(int id, AttributValeurDto dto)
        {
            var response = await _http.PutAsJsonAsync($"api/attributs/{id}", dto);
            if (response.IsSuccessStatusCode)
            {
                ClearCache();
                return true;
            }
            return false;
        }

        public async Task<bool> SupprimerAttributAsync(int id)
        {
            var response = await _http.DeleteAsync($"api/attributs/{id}");
            if (response.IsSuccessStatusCode)
            {
                ClearCache();
                return true;
            }
            return false;
        }

        public void ClearCache() => _cache.Clear();
    }
}
