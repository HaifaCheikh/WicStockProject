using System.Net.Http.Json;
using WicStock.Web.Models.Dtos;

namespace WicStock.Web.Services
{
    public class PaymentService
    {
        private readonly HttpClient _http;

        public PaymentService(HttpClient http)
        {
            _http = http;
        }

        public async Task<string?> GetPaymentConfig()
        {
            try
            {
                var response = await _http.GetAsync("api/payment/config");
                response.EnsureSuccessStatusCode();
                var config = await response.Content.ReadFromJsonAsync<PaymentConfigDto>();
                return config?.StoreId;
            }
            catch
            {
                return null;
            }
        }

        public async Task<PaymentResponseDto?> CreateCheckout(PaymentRequestDto dto)
        {
            var response = await _http.PostAsJsonAsync("api/payment/create-checkout", dto);
            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(errorText) ? $"Erreur HTTP {(int)response.StatusCode}" : errorText);
            }
            return await response.Content.ReadFromJsonAsync<PaymentResponseDto>();
        }

        public async Task<bool> ConfirmPayment(PaymentSuccessDto dto)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("api/payment/confirm-payment", dto);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }

    public class PaymentConfigDto
    {
        public string? StoreId { get; set; }
        public string? Provider { get; set; }
    }
}