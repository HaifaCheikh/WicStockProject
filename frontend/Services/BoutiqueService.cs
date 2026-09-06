using System.Net.Http.Json;
using System.Text.Json;
using WicStock.Web.Models.Dtos;

namespace WicStock.Web.Services
{
    /// <summary>
    /// Service pour les appels publics au catalogue — sans token JWT.
    /// Utilise le client HTTP "WicStockPublic" (sans AuthorizationMessageHandler).
    /// </summary>
    public class BoutiqueService
    {
        private readonly HttpClient _http;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public BoutiqueService(HttpClient http)
        {
            _http = http;
        }

        /// <summary>Récupère tous les produits actifs du catalogue public.</summary>
        public async Task<List<CatalogueProduitDto>> ObtenirCataloguePublic()
        {
            try
            {
                return await _http.GetFromJsonAsync<List<CatalogueProduitDto>>(
                    "api/Produit/catalogue", _jsonOptions)
                    ?? new List<CatalogueProduitDto>();
            }
            catch
            {
                return new List<CatalogueProduitDto>();
            }
        }

        /// <summary>Récupère la fiche détail d'un produit (public, sans token).</summary>
        public async Task<CatalogueProduitDto?> ObtenirProduitPublic(int id)
        {
            try
            {
                return await _http.GetFromJsonAsync<CatalogueProduitDto>(
                    $"api/Produit/catalogue/{id}", _jsonOptions);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Soumet une commande multi-articles (nécessite un token JWT — appelé côté panier une fois connecté).
        /// Retourne (succès, résultat, erreurStock).
        /// </summary>
        public async Task<(bool Succes, CommandeMultiResultClientDto? Resultat, CommandeStockErrorClientDto? ErreurStock, string MessageErreur)>
            PasserCommandeMulti(HttpClient httpWithAuth, CommandeMultiCreateClientDto dto)
        {
            try
            {
                var response = await httpWithAuth.PostAsJsonAsync("api/HistoriqueVente/multi", dto);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<CommandeMultiResultClientDto>(_jsonOptions);
                    return (true, result, null, string.Empty);
                }

                if ((int)response.StatusCode == 409)
                {
                    var erreur = await response.Content.ReadFromJsonAsync<CommandeStockErrorClientDto>(_jsonOptions);
                    return (false, null, erreur, erreur?.Message ?? "Stock insuffisant.");
                }

                var msg = await response.Content.ReadAsStringAsync();
                return (false, null, null, $"Erreur {(int)response.StatusCode} : {msg.Trim('"')}");
            }
            catch (Exception ex)
            {
                return (false, null, null, $"Erreur réseau : {ex.Message}");
            }
        }
    }
}
