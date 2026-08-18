using Microsoft.JSInterop;

namespace WicStock.Web.Services
{
    public class LocalStorageService
    {
        private readonly IJSRuntime _js;

        public LocalStorageService(IJSRuntime js)
        {
            _js = js;
        }

        public async Task SetItemAsync(string key, string value)
        {
            await _js.InvokeVoidAsync("localStorageInterop.setItem", key, value);
        }

        public async Task<string?> GetItemAsync(string key)
        {
            return await _js.InvokeAsync<string?>("localStorageInterop.getItem", key);
        }

        public async Task RemoveItemAsync(string key)
        {
            await _js.InvokeVoidAsync("localStorageInterop.removeItem", key);
        }
    }
}