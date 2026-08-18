using System.Text.Json;
using System.Text.Json.Serialization;

namespace WicStock_.Services
{
    public class LemonSqueezyService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public LemonSqueezyService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;

            var apiKey = _configuration["LemonSqueezy:ApiKey"];
            if (!string.IsNullOrEmpty(apiKey))
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.api+json");
            }
        }

        public string? GetStoreId() => _configuration["LemonSqueezy:StoreId"];
        public string? GetVariantId() => _configuration["LemonSqueezy:VariantId"];

        public async Task<CheckoutResponse?> CreateCheckoutAsync(
            decimal amount,
            int quantite,
            string productName,
            int commandeId,
            string successUrl,
            string cancelUrl,
            string? clientName = null,
            string? clientEmail = null,
            string? country = null,
            string? zip = null)
        {
            var storeId = GetStoreId();
            var variantId = GetVariantId();

            if (string.IsNullOrEmpty(storeId) || string.IsNullOrEmpty(variantId))
                return null;

            // custom_price = prix unitaire en centimes (PrixUnitaire × 100)
            // LemonSqueezy calcule automatiquement : Total = custom_price × quantity
            var prixUnitaireCentimes = (long)Math.Round((amount / (quantite > 0 ? quantite : 1)) * 100);

            var checkoutDataInfo = new CheckoutDataInfo
            {
                Name = clientName,
                Email = clientEmail,
                BillingAddress = (!string.IsNullOrWhiteSpace(country) || !string.IsNullOrWhiteSpace(zip))
                    ? new BillingAddressInfo { Country = country, Zip = zip }
                    : null,
                Custom = new Dictionary<string, string>
                {
                    { "commande_id", commandeId.ToString() }
                }
            };

            var request = new CheckoutRequest
            {
                Data = new CheckoutData
                {
                    Type = "checkouts",
                    Attributes = new CheckoutAttributes
                    {
                        // Prix unitaire en centimes — LemonSqueezy multiplie par quantity pour le total
                        CustomPrice = prixUnitaireCentimes,
                        CheckoutData = checkoutDataInfo,
                        ProductOptions = new ProductOptions
                        {
                            Name = productName,
                            Description = $"Paiement sécurisé sur WicStock — {productName}",
                            ReceiptButtonText = "Retour à WicStock",
                            RedirectUrl = successUrl
                        },
                        CheckoutOptions = new CheckoutOptions
                        {
                            ButtonColor = "#1E6B4C",
                            Embed = false,
                            Quantity = quantite > 0 ? quantite : 1
                        }
                    },
                    Relationships = new CheckoutRelationships
                    {
                        Store = new RelationshipData
                        {
                            Data = new RelationshipItem { Type = "stores", Id = storeId }
                        },
                        Variant = new RelationshipData
                        {
                            Data = new RelationshipItem { Type = "variants", Id = variantId }
                        }
                    }
                }
            };

            var jsonOptions = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            };

            var json = JsonSerializer.Serialize(request, jsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/vnd.api+json");

            var response = await _httpClient.PostAsync("https://api.lemonsqueezy.com/v1/checkouts", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"LemonSqueezy error {(int)response.StatusCode}: {errorBody}");
            }

            var result = await response.Content.ReadFromJsonAsync<LemonSqueezyResponse<CheckoutResponse>>();
            return result?.Data;
        }

        public async Task<OrderResponse?> GetOrderAsync(string orderId)
        {
            var response = await _httpClient.GetAsync($"https://api.lemonsqueezy.com/v1/orders/{orderId}");
            if (!response.IsSuccessStatusCode)
                return null;

            var result = await response.Content.ReadFromJsonAsync<LemonSqueezyResponse<OrderResponse>>();
            return result?.Data;
        }
    }

    // ─── Request DTOs ────────────────────────────────────────────────────────────

    public class CheckoutRequest
    {
        [JsonPropertyName("data")]
        public CheckoutData Data { get; set; } = null!;
    }

    public class CheckoutData
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "checkouts";

        [JsonPropertyName("attributes")]
        public CheckoutAttributes Attributes { get; set; } = null!;

        [JsonPropertyName("relationships")]
        public CheckoutRelationships Relationships { get; set; } = null!;
    }

    public class CheckoutAttributes
    {
        /// <summary>Prix unitaire en centimes. LemonSqueezy calcule Total = CustomPrice × Quantity.</summary>
        [JsonPropertyName("custom_price")]
        public long? CustomPrice { get; set; }

        [JsonPropertyName("checkout_data")]
        public CheckoutDataInfo CheckoutData { get; set; } = null!;

        [JsonPropertyName("product_options")]
        public ProductOptions ProductOptions { get; set; } = null!;

        [JsonPropertyName("checkout_options")]
        public CheckoutOptions CheckoutOptions { get; set; } = null!;
    }

    public class CheckoutDataInfo
    {
        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("billing_address")]
        public BillingAddressInfo? BillingAddress { get; set; }

        [JsonPropertyName("custom")]
        public Dictionary<string, string>? Custom { get; set; }
    }

    public class BillingAddressInfo
    {
        [JsonPropertyName("country")]
        public string? Country { get; set; }

        [JsonPropertyName("zip")]
        public string? Zip { get; set; }
    }

    public class ProductOptions
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = null!;

        [JsonPropertyName("description")]
        public string Description { get; set; } = null!;

        [JsonPropertyName("receipt_button_text")]
        public string ReceiptButtonText { get; set; } = null!;

        [JsonPropertyName("redirect_url")]
        public string RedirectUrl { get; set; } = null!;
    }

    public class CheckoutOptions
    {
        [JsonPropertyName("button_color")]
        public string ButtonColor { get; set; } = null!;

        [JsonPropertyName("embed")]
        public bool Embed { get; set; }

        [JsonPropertyName("quantity")]
        public int? Quantity { get; set; }
    }

    public class CheckoutRelationships
    {
        [JsonPropertyName("store")]
        public RelationshipData Store { get; set; } = null!;

        [JsonPropertyName("variant")]
        public RelationshipData Variant { get; set; } = null!;
    }

    public class RelationshipData
    {
        [JsonPropertyName("data")]
        public RelationshipItem Data { get; set; } = null!;
    }

    public class RelationshipItem
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = null!;

        [JsonPropertyName("id")]
        public string Id { get; set; } = null!;
    }

    // ─── Response DTOs ───────────────────────────────────────────────────────────

    public class LemonSqueezyResponse<T>
    {
        [JsonPropertyName("data")]
        public T? Data { get; set; }
    }

    public class CheckoutResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = null!;

        [JsonPropertyName("attributes")]
        public CheckoutResponseAttributes Attributes { get; set; } = null!;
    }

    public class CheckoutResponseAttributes
    {
        [JsonPropertyName("url")]
        public string Url { get; set; } = null!;
    }

    public class OrderResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = null!;

        [JsonPropertyName("attributes")]
        public OrderAttributes Attributes { get; set; } = null!;
    }

    public class OrderAttributes
    {
        [JsonPropertyName("first_order_item")]
        public OrderItem? FirstOrderItem { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = null!;

        [JsonPropertyName("user_name")]
        public string? UserName { get; set; }

        [JsonPropertyName("user_email")]
        public string? UserEmail { get; set; }

        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }

        [JsonPropertyName("country_formatted")]
        public string? CountryFormatted { get; set; }

        [JsonPropertyName("state")]
        public string? State { get; set; }

        [JsonPropertyName("state_formatted")]
        public string? StateFormatted { get; set; }

        [JsonPropertyName("zip")]
        public string? Zip { get; set; }

        [JsonPropertyName("tax_name")]
        public string? TaxName { get; set; }
    }

    public class OrderItem
    {
        [JsonPropertyName("variant_name")]
        public string VariantName { get; set; } = null!;
    }
}