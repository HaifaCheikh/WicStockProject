using MudBlazor;
using System.Globalization;

namespace WicStock.Web.Models
{
    /// <summary>
    /// Modèle représentant une suggestion de question rapide ("Quick Prompt") pour le chat de l'assistant IA.
    /// </summary>
    public class QuickPrompt
    {
        public string Question { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Icon { get; set; } = Icons.Material.Filled.Search;
        public string? Category { get; set; }
    }

    /// <summary>
    /// Configuration centralisée du design system des graphiques et de l'interactivité de l'assistant WicStock.
    /// </summary>
    public static class WicStockChartConfig
    {
        /// <summary>
        /// Palette officielle WicStock : Denim (bleu profond), Rust (terracotta), Pine (vert forêt),
        /// Amber (ambre chaud), Slate (gris ardoise), Sand (beige chaud), Teal (sarcelle), Indigo, Cyan, Rose.
        /// </summary>
        public static readonly string[] Palette = new[]
        {
            "#2B4C7E", // Denim principal (Bleu denim WicStock)
            "#F59E0B", // Jaune orangé / Ambre éclatant (Accent WicStock)
            "#1E6B4C", // Pine (vert forêt)
            "#C85A32", // Rust / Terracotta
            "#475569", // Slate (ardoise)
            "#D4A373", // Sand chaud
            "#0D9488", // Teal
            "#406BA8", // Denim clair
            "#FBBF24", // Jaune ambre
            "#6366F1", // Indigo
            "#0284C7", // Cyan
            "#E11D48"  // Rose
        };

        /// <summary>
        /// Options par défaut pour les composants MudChart (barres, lignes) avec la palette WicStock.
        /// </summary>
        public static ChartOptions DefaultChartOptions => new ChartOptions
        {
            ChartPalette = Palette
        };

        /// <summary>
        /// Retourne la couleur de la palette pour un index donné (boucle si dépassement).
        /// </summary>
        public static string GetColor(int index)
        {
            if (Palette.Length == 0) return "#2B4C7E";
            var safeIndex = Math.Abs(index) % Palette.Length;
            return Palette[safeIndex];
        }

        /// <summary>
        /// Formate un nombre de façon lisible et compacte (ex: 1 250, 15.4k).
        /// </summary>
        public static string FormatNumber(double value, bool compact = false)
        {
            var culture = CultureInfo.GetCultureInfo("fr-FR");
            if (compact)
            {
                if (Math.Abs(value) >= 1_000_000)
                    return (value / 1_000_000.0).ToString("0.#", culture) + "M";
                if (Math.Abs(value) >= 1_000)
                    return (value / 1_000.0).ToString("0.#", culture) + "k";
            }

            if (Math.Abs(value % 1) < double.Epsilon)
            {
                return ((long)value).ToString("N0", culture);
            }

            return value.ToString("N1", culture);
        }

        /// <summary>
        /// Retourne la liste des Quick Prompts suggérés en fonction du rôle utilisateur,
        /// alignée avec les requêtes pré-entraînées du catalogue RAG (sql_examples.json).
        /// </summary>
        public static List<QuickPrompt> ObtenirSuggestionsParRole(string? role)
        {
            var roleNormalise = (role ?? string.Empty).ToUpperInvariant().Trim();

            if (roleNormalise == "CLIENT")
            {
                return new List<QuickPrompt>
                {
                    new() { Label = "Mes commandes", Question = "Quel est l'état de mes commandes ?", Icon = Icons.Material.Filled.LocalShipping, Category = "Commandes" },
                    new() { Label = "Sur commande", Question = "Quels sont les articles disponibles sur commande ?", Icon = Icons.Material.Filled.Inventory, Category = "Catalogue" },
                    new() { Label = "Mieux notés", Question = "Quels sont les articles les mieux notés ?", Icon = Icons.Material.Filled.Star, Category = "Avis" },
                    new() { Label = "Catalogue", Question = "Quels sont les articles du catalogue ?", Icon = Icons.Material.Filled.Storefront, Category = "Catalogue" }
                };
            }

            if (roleNormalise == "RESPONSABLE_STOCK_PRODUCTION")
            {
                return new List<QuickPrompt>
                {
                    new() { Label = "Ruptures", Question = "Quels produits sont en rupture de stock ?", Icon = Icons.Material.Filled.WarningAmber, Category = "Stock" },
                    new() { Label = "Surstock", Question = "Quels produits sont en surstock ?", Icon = Icons.Material.Filled.Layers, Category = "Stock" },
                    new() { Label = "États de stock", Question = "Quelle est la répartition des états de stock ?", Icon = Icons.Material.Filled.DonutLarge, Category = "Analyse" },
                    new() { Label = "Stock global", Question = "Quel est le stock actuel de tous les produits ?", Icon = Icons.Material.Filled.BarChart, Category = "Stock" },
                    new() { Label = "Par catégorie", Question = "Quelle est la répartition des produits par catégorie ?", Icon = Icons.Material.Filled.Category, Category = "Catalogue" },
                    new() { Label = "Sur commande", Question = "Quels sont les produits sur commande ?", Icon = Icons.Material.Filled.PendingActions, Category = "Stock" }
                };
            }

            // ADMIN par défaut
            return new List<QuickPrompt>
            {
                new() { Label = "Répartition stocks", Question = "Quelle est la répartition des états de stock ?", Icon = Icons.Material.Filled.DonutLarge, Category = "Stock" },
                new() { Label = "Ruptures", Question = "Quels produits sont en rupture de stock ?", Icon = Icons.Material.Filled.WarningAmber, Category = "Stock" },
                new() { Label = "Par catégorie", Question = "Quelle est la répartition des produits par catégorie ?", Icon = Icons.Material.Filled.Category, Category = "Catalogue" },
                new() { Label = "Commandes", Question = "Répartition des commandes par statut", Icon = Icons.Material.Filled.ShoppingCart, Category = "Commandes" },
                new() { Label = "Surstock", Question = "Quels produits sont en surstock ?", Icon = Icons.Material.Filled.Layers, Category = "Stock" },
                new() { Label = "Stock total", Question = "Quel est le stock actuel de tous les produits ?", Icon = Icons.Material.Filled.BarChart, Category = "Stock" }
            };
        }

        /// <summary>
        /// Table déclarative de mapping pour le Drill-Down au clic sur un segment ou un badge de graphique.
        /// Retourne l'URL de redirection correspondante ou null si l'élément n'est pas navigable.
        /// </summary>
        public static string? ResoudreRouteDrillDown(string label, string? chartTitle, string? role)
        {
            if (string.IsNullOrWhiteSpace(label)) return null;

            var cleanLabel = label.Trim().ToUpperInvariant();
            var cleanTitle = (chartTitle ?? string.Empty).ToUpperInvariant();
            var roleUpper = (role ?? string.Empty).ToUpperInvariant();
            var isClient = roleUpper == "CLIENT";

            // 1. MAPPING STATUTS DE COMMANDE
            var statutsCommande = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "EN_ATTENTE", "ACCEPTEE", "ACCEPTÉE", "REFUSEE", "REFUSÉE", "EXPEDIEE", "EXPÉDIÉE", "LIVREE", "LIVRÉE", "ANNULEE", "ANNULÉE",
                "EN ATTENTE", "ACCEPTÉ", "ACCEPTE", "REFUSÉ", "REFUSE", "LIVRÉ", "LIVRE", "EXPÉDIÉ", "EXPEDIE"
            };

            if (statutsCommande.Contains(cleanLabel) || cleanTitle.Contains("COMMANDE"))
            {
                if (statutsCommande.Contains(cleanLabel))
                {
                    if (isClient) return "/suivi-commande";

                    var codeStatut = cleanLabel switch
                    {
                        "EN ATTENTE" or "EN_ATTENTE" => "EN_ATTENTE",
                        "ACCEPTEE" or "ACCEPTÉE" or "ACCEPTE" or "ACCEPTÉ" => "ACCEPTEE",
                        "REFUSEE" or "REFUSÉE" or "REFUSE" or "REFUSÉ" => "REFUSEE",
                        "EXPEDIEE" or "EXPÉDIÉE" or "EXPEDIE" or "EXPÉDIÉ" => "EXPEDIEE",
                        "LIVREE" or "LIVRÉE" or "LIVRE" or "LIVRÉ" => "LIVREE",
                        "ANNULEE" or "ANNULÉE" => "ANNULEE",
                        _ => cleanLabel
                    };
                    return $"/commandes?statut={codeStatut}";
                }
            }

            // 2. MAPPING STATUTS DE STOCK
            var statutsStock = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "RUPTURE", "RUPTURE" },
                { "EN RUPTURE", "RUPTURE" },
                { "RUPTURE DE STOCK", "RUPTURE" },
                { "STOCK_FAIBLE", "STOCK_FAIBLE" },
                { "STOCK FAIBLE", "STOCK_FAIBLE" },
                { "FAIBLE", "STOCK_FAIBLE" },
                { "FAIBLE STOCK", "STOCK_FAIBLE" },
                { "EN STOCK FAIBLE", "STOCK_FAIBLE" },
                { "SURSTOCK", "SURSTOCK" },
                { "EN SURSTOCK", "SURSTOCK" },
                { "OPTIMAL", "OPTIMAL" },
                { "NORMAL", "OPTIMAL" },
                { "STOCK NORMAL", "OPTIMAL" },
                { "SUR_COMMANDE", "SUR_COMMANDE" },
                { "SUR COMMANDE", "SUR_COMMANDE" },
                { "SUR COMMANDES", "SUR_COMMANDE" }
            };

            if (statutsStock.TryGetValue(cleanLabel, out var statutFiltre) ||
                (cleanTitle.Contains("STOCK") && statutsStock.Keys.FirstOrDefault(k => cleanLabel.Contains(k)) is string matchedKey && statutsStock.TryGetValue(matchedKey, out statutFiltre)))
            {
                var code = statutFiltre ?? "RUPTURE";
                return isClient ? "/produits" : $"/produits?statut={code}";
            }

            // 3. MAPPING CATÉGORIES DE PRODUITS
            // CORRIGÉ : ne se déclenche QUE si le titre mentionne explicitement "CATÉGORIE".
            // "PRODUIT" et "REPARTITION" ont été retirés : ils étaient trop génériques et
            // capturaient à tort les graphiques de répartition PAR PRODUIT (ex: surstock par
            // article), envoyant le nom du produit comme si c'était une catégorie
            // (ex: /produits?categorie=Denim%20Shirt au lieu de ?highlight=Denim%20Shirt).
            if (cleanTitle.Contains("CATÉGORIE") || cleanTitle.Contains("CATEGORIE"))
            {
                // Vérifie que ce n'est pas une simple valeur numérique brute
                if (!double.TryParse(label, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                {
                    var encodedCat = Uri.EscapeDataString(label.Trim());
                    return isClient ? $"/produits" : $"/produits?categorie={encodedCat}";
                }
            }

            // 4. MAPPING NOMS DE PRODUITS
            // Tout label non numérique, non statut, non catégorie connu est traité comme un nom de produit
            if (!double.TryParse(label, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out _)
                && label.Length > 2 && label.Length < 100)
            {
                var encoded = Uri.EscapeDataString(label.Trim());
                return isClient ? null : $"/produits?highlight={encoded}";
            }

            // 5. AUCUN MAPPING (Donnée non navigable)
            return null;
        }
    }
}