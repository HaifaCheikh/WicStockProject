using System.Net.Http.Json;

namespace WicStock_.Services
{
    public class IAExplicationService
    {
        private readonly HttpClient _http;

        public IAExplicationService(IHttpClientFactory factory)
        {
            _http = factory.CreateClient("WicStockIA");
        }

        public async Task<string?> GenererExplication(string nomProduit, string typeRisque,
            float scoreRisque, int quantiteActuelle, string typeAction)
        {
            var requete = new
            {
                nom_produit = nomProduit,
                type_risque = typeRisque,
                score_risque = scoreRisque,
                quantite_actuelle = quantiteActuelle,
                type_action = typeAction
            };

            var reponse = await _http.PostAsJsonAsync("expliquer-action", requete);
            if (!reponse.IsSuccessStatusCode) return null;

            var resultat = await reponse.Content.ReadFromJsonAsync<Dictionary<string, string>>();
            return resultat?.GetValueOrDefault("texte_genere");
        }
    }
}
