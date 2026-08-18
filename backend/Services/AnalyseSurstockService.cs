using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
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

        // Le microservice Python (FastAPI + Qwen3) tourne sur le port 8001 (même client que WicStockIA)
        public AnalyseSurstockService(IHttpClientFactory httpClientFactory)
        {
            _httpIA = httpClientFactory.CreateClient("WicStockIA");
            _httpIA.Timeout = TimeSpan.FromSeconds(90); // Qwen3 peut mettre du temps sur petits modèles
        }

        public async Task<AnalyseProduitDto> AnalyserSurstockAsync(MetriquesStockDto metriques)
        {
            var result = new AnalyseProduitDto
            {
                ProduitId = metriques.ProduitId,
                DateAnalyse = DateTime.Now,
                Metriques = metriques,
                EstModeDegrade = false
            };

            try
            {
                // Construire le corps de la requête vers le endpoint Python /analyser-surstock
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
                Console.WriteLine("[ANALYSE IA] Timeout du service IA — activation du mode dégradé.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ANALYSE IA] Erreur service IA : {ex.Message} — activation du mode dégradé.");
            }

            // ===== MODE DÉGRADÉ : diagnostic et actions calculés en C# depuis les métriques SQL =====
            result.EstModeDegrade = true;
            int surplus = Math.Max(0, metriques.StockActuel - metriques.SeuilSurstock);
            string detailSurplus = surplus > 0 ? $" (surplus de {surplus} u., soit +{metriques.PourcentageAuDessusDuSeuil}% au-dessus du seuil de {metriques.SeuilSurstock} u.)" : "";

            result.Diagnostic = metriques.EstTendanceCategorie
                ? $"Le stock de \"{metriques.NomProduit}\" compte {metriques.StockActuel} unités{detailSurplus}, sans vente depuis {metriques.JoursDepuisDerniereSortie} jours. Ce surstock s'inscrit dans une tendance globale de la catégorie \"{metriques.Categorie}\" ({metriques.NbReferencesSimilairesEnSurstock} références similaires concernées)."
                : $"Le stock de \"{metriques.NomProduit}\" compte {metriques.StockActuel} unités{detailSurplus}, sans vente depuis {metriques.JoursDepuisDerniereSortie} jours. La catégorie \"{metriques.Categorie}\" conserve un écoulement dynamique — ce ralentissement est propre à cette référence.";

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
            int qteRecyclage = (int)Math.Max(10, Math.Round(m.StockActuel * 0.25));
            return new List<ActionRecommandeeDto>
            {
                new ActionRecommandeeDto
                {
                    Id = Guid.NewGuid().ToString("N"),
                    TypeAction = "PROMOTION_CIBLEE",
                    Label = "Créer une promotion ciblée (-20%)",
                    Justification = $"Durée d'écoulement moyenne de {m.DureeEcoulementMoyenneProduitsSimilaires} jours sur les produits similaires en promotion.",
                    Params = new Dictionary<string, object>
                    {
                        { "remisePourcentage", 20 },
                        { "dureeJours", 14 },
                        { "quantiteCible", Math.Min(m.StockActuel, 50) }
                    }
                },
                new ActionRecommandeeDto
                {
                    Id = Guid.NewGuid().ToString("N"),
                    TypeAction = "RECYCLAGE_ANTICIPE",
                    Label = $"Marquer {qteRecyclage} unités pour recyclage",
                    Justification = $"Valeur de stock immobilisée : {m.ValeurStockImmobilisee:N0} DT ; coût de possession estimé : {m.CoutPossessionEstimeMensuel:N0} DT/mois.",
                    Params = new Dictionary<string, object>
                    {
                        { "quantite", qteRecyclage },
                        { "motif", "Déstockage surstock persistant" }
                    }
                },
                new ActionRecommandeeDto
                {
                    Id = Guid.NewGuid().ToString("N"),
                    TypeAction = "NOTIFICATION_PRODUCTION",
                    Label = "Notifier l'atelier de production",
                    Justification = m.EstTendanceCategorie
                        ? $"{m.NbReferencesSimilairesEnSurstock} références similaires sont également en surstock — ajustement de catégorie requis."
                        : "Aucune tendance catégorie détectée — ajustement ciblé sur cette référence uniquement.",
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
            "PROMOTION_CIBLEE" => "Créer une promotion ciblée (-20%)",
            "RECYCLAGE_ANTICIPE" => "Marquer pour recyclage",
            _ => "Notifier la production"
        };

        private string GetDefaultJustif(string typeAction, MetriquesStockDto m) => (typeAction.ToUpperInvariant()) switch
        {
            "PROMOTION_CIBLEE" => $"Écoulement moyen de {m.DureeEcoulementMoyenneProduitsSimilaires} jours sur les produits similaires.",
            "RECYCLAGE_ANTICIPE" => $"Valeur immobilisée de {m.ValeurStockImmobilisee:N0} DT.",
            _ => "Ajustement recommandé sur les ordres de coupe."
        };
    }
}
