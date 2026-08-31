using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using WicStock_.Models.Dtos;

namespace WicStock_.Services
{
    public interface IAnalyseSurstockService
    {
        Task<AnalyseProduitDto> AnalyserSurstockAsync(MetriquesStockDto metriques);
    }

    public class AnalyseSurstockService : IAnalyseSurstockService
    {
        private readonly HttpClient _httpIA;

        public AnalyseSurstockService(IHttpClientFactory httpClientFactory)
        {
            _httpIA = httpClientFactory.CreateClient("WicStockAI");
        }

        public async Task<AnalyseProduitDto> AnalyserSurstockAsync(MetriquesStockDto metriques)
        {
            var result = new AnalyseProduitDto
            {
                ProduitId = metriques.ProduitId,
                DateAnalyse = DateTime.Now,
                Metriques = metriques
            };

            try
            {
                var requestBody = new
                {
                    produit_id = metriques.ProduitId,
                    nom_produit = metriques.NomProduit,
                    stock_actuel = metriques.StockActuel,
                    seuil_surstock = metriques.SeuilSurstock,
                    pourcentage_au_dessus_du_seuil = metriques.PourcentageAuDessusDuSeuil,
                    jours_depuis_derniere_sortie = metriques.JoursDepuisDerniereSortie,
                    taux_ecoulement_90_jours = metriques.TauxEcoulement90Jours,
                    categorie = metriques.Categorie,
                    taux_ecoulement_moyen_categorie_90_jours = metriques.TauxEcoulementMoyenCategorie90Jours,
                    est_tendance_categorie = metriques.EstTendanceCategorie,
                    nb_references_similaires_en_surstock = metriques.NbReferencesSimilairesEnSurstock,
                    duree_ecoulement_moyenne_produits_similaires = metriques.DureeEcoulementMoyenneProduitsSimilaires,
                    valeur_stock_immobilisee = (double)metriques.ValeurStockImmobilisee,
                    cout_possession_estime_mensuel = (double)metriques.CoutPossessionEstimeMensuel
                };

                var response = await _httpIA.PostAsJsonAsync("analyser-surstock", requestBody);

                if (response.IsSuccessStatusCode)
                {
                    var jsonDoc = await response.Content.ReadFromJsonAsync<JsonObject>();
                    bool succes = jsonDoc?["succes"]?.GetValue<bool>() ?? false;

                    if (succes)
                    {
                        string? diagnostic = jsonDoc?["diagnostic"]?.ToString();
                        var actionsNode = jsonDoc?["actions"]?.AsArray();

                        if (!string.IsNullOrWhiteSpace(diagnostic) && actionsNode != null)
                        {
                            result.Diagnostic = diagnostic;
                            result.Actions = ParseActions(actionsNode, metriques);
                            return result;
                        }
                    }
                }
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("[ANALYSE IA] Timeout du service IA -> activation du mode degrade.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ANALYSE IA] Erreur service IA : {ex.Message} -> activation du mode degrade.");
            }

            // ===== MODE DEGRADE : diagnostic et actions calcules en C# depuis les metriques SQL =====
            result.EstModeDegrade = true;
            int surplus = Math.Max(0, metriques.StockActuel - metriques.SeuilSurstock);
            string detailSurplus = surplus > 0 ? $" (surplus de {surplus} u., soit +{metriques.PourcentageAuDessusDuSeuil}% au-dessus du seuil de {metriques.SeuilSurstock} u.)" : "";

            result.Diagnostic = metriques.EstTendanceCategorie
                ? $"Le stock de \"{metriques.NomProduit}\" compte {metriques.StockActuel} unites{detailSurplus}, sans vente depuis {metriques.JoursDepuisDerniereSortie} jours. Ce surstock s'inscrit dans une tendance globale de la categorie \"{metriques.Categorie}\" ({metriques.NbReferencesSimilairesEnSurstock} references similaires concernees)."
                : $"Le stock de \"{metriques.NomProduit}\" compte {metriques.StockActuel} unites{detailSurplus}, sans vente depuis {metriques.JoursDepuisDerniereSortie} jours. La categorie \"{metriques.Categorie}\" conserve un ecoulement dynamique -- ce ralentissement est propre a cette reference.";

            result.Actions = GenererActionsFallback(metriques);
            return result;
        }

        private List<ActionRecommandeeDto> ParseActions(JsonArray actionsNode, MetriquesStockDto m)
        {
            var list = new List<ActionRecommandeeDto>();
            foreach (var node in actionsNode)
            {
                if (node == null) continue;
                string type = node["typeAction"]?.ToString() ?? "PROMOTION_CIBLEE";
                list.Add(new ActionRecommandeeDto
                {
                    Id = Guid.NewGuid().ToString("N"),
                    TypeAction = type.ToUpperInvariant(),
                    Label = node["label"]?.ToString() ?? GetDefaultLabel(type),
                    Justification = node["justification"]?.ToString() ?? GetDefaultJustif(type, m),
                    Params = BuildParamsForType(type, m)
                });
            }
            return list.Count > 0 ? list : GenererActionsFallback(m);
        }

        private List<ActionRecommandeeDto> GenererActionsFallback(MetriquesStockDto m)
        {
            int surplus = Math.Max(0, m.StockActuel - m.SeuilSurstock);
            int baseVolume = surplus > 0 ? surplus : m.StockActuel;

            int qtePromo = surplus > 0 ? surplus : Math.Min(m.StockActuel, 50);
            int qteRecyclage = (int)Math.Max(10, Math.Round(baseVolume * 0.5));

            return new List<ActionRecommandeeDto>
            {
                new ActionRecommandeeDto
                {
                    Id = Guid.NewGuid().ToString("N"),
                    TypeAction = "PROMOTION_CIBLEE",
                    Label = "Creer une promotion ciblee (-20%)",
                    Justification = $"Permet de resorber les {qtePromo} unites en surstock (promotion valable 14 jours).",
                    Params = new Dictionary<string, object>
                    {
                        { "remisePourcentage", 20 },
                        { "dureeJours", 14 },
                        { "quantiteCible", qtePromo }
                    }
                },
                new ActionRecommandeeDto
                {
                    Id = Guid.NewGuid().ToString("N"),
                    TypeAction = "RECYCLAGE_ANTICIPE",
                    Label = $"Marquer {qteRecyclage} unites pour recyclage",
                    Justification = $"Libere une valeur immobilisee de {m.ValeurStockImmobilisee:N0} DT en marquant {qteRecyclage} unites (cout de possession estime : {m.CoutPossessionEstimeMensuel:N0} DT/mois).",
                    Params = new Dictionary<string, object>
                    {
                        { "quantite", qteRecyclage },
                        { "motif", "Destockage surstock persistant" }
                    }
                },
                new ActionRecommandeeDto
                {
                    Id = Guid.NewGuid().ToString("N"),
                    TypeAction = "NOTIFICATION_PRODUCTION",
                    Label = "Notifier l'atelier de production",
                    Justification = m.EstTendanceCategorie
                        ? $"{m.NbReferencesSimilairesEnSurstock} references similaires sont egalement en surstock -- ajustement de categorie requis."
                        : "Aucune tendance categorie detectee -- ajustement cible sur cette reference uniquement.",
                    Params = new Dictionary<string, object>
                    {
                        { "destinataire", "Chef de Production" },
                        { "priorite", m.EstTendanceCategorie ? "HAUTE" : "MOYENNE" }
                    }
                }
            };
        }

        private Dictionary<string, object> BuildParamsForType(string typeAction, MetriquesStockDto m)
        {
            return (typeAction.ToUpperInvariant()) switch
            {
                "PROMOTION_CIBLEE" => new Dictionary<string, object>
                {
                    { "remisePourcentage", 20 },
                    { "dureeJours", 14 },
                    { "quantiteCible", Math.Min(m.StockActuel, 50) }
                },
                "RECYCLAGE_ANTICIPE" => new Dictionary<string, object>
                {
                    { "quantite", (int)Math.Max(10, Math.Round(m.StockActuel * 0.25)) },
                    { "motif", "Recyclage surstock persistant" }
                },
                _ => new Dictionary<string, object>
                {
                    { "destinataire", "Chef de Production" },
                    { "priorite", "MOYENNE" }
                }
            };
        }

        private string GetDefaultLabel(string typeAction) => (typeAction.ToUpperInvariant()) switch
        {
            "PROMOTION_CIBLEE" => "Creer une promotion ciblee (-20%)",
            "RECYCLAGE_ANTICIPE" => "Marquer pour recyclage",
            _ => "Notifier la production"
        };

        private string GetDefaultJustif(string typeAction, MetriquesStockDto m) => (typeAction.ToUpperInvariant()) switch
        {
            "PROMOTION_CIBLEE" => $"Permet de resorber le surstock (promotion valable 14 jours).",
            "RECYCLAGE_ANTICIPE" => $"Valeur immobilisee de {m.ValeurStockImmobilisee:N0} DT.",
            _ => "Ajustement recommande sur les ordres de coupe."
        };
    }
}