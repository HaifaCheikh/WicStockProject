using System;
using System.Collections.Generic;
using System.Linq;

namespace WicStock.Web.Models.Dtos
{
    public class PrevisionEtatProduitDto
    {
        public int Id { get; set; }
        public int ProduitId { get; set; }
        public string TypeRisquePredit { get; set; } = string.Empty;
        public float ScoreRisque { get; set; }
        public int QuantitePredite { get; set; }
        public int HorizonJours { get; set; }
        public DateTime DateCalcul { get; set; }
        public ActionRecommandeeDto? ActionRecommandee { get; set; }
    }

    public class ActionRecommandeeDto
    {
        public int Id { get; set; }
        public string TypeAction { get; set; } = string.Empty;
        public string? TexteGenere { get; set; }
        public DateTime DateGeneration { get; set; }
    }

    public class ProduitDto
    {
        public int Id { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string Nom { get; set; } = string.Empty;
        public string TypeTissu { get; set; } = string.Empty;
        public string Categorie { get; set; } = string.Empty;
        public string CycleDeVie { get; set; } = string.Empty;
        public decimal PrixUnitaire { get; set; } = 0;
        public DateTime DateCreation { get; set; }
        public string? ImageUrl { get; set; }
        public string? ImageBase64 { get; set; }
        public bool DisponibleSurCommande { get; set; } = false;
        public bool EstArchive { get; set; } = false;
        public int? RemisePourcentage { get; set; }
        public DateTime? DateFinPromotion { get; set; }

        public bool EstEnPromotion =>
            RemisePourcentage.HasValue && RemisePourcentage.Value > 0 &&
            DateFinPromotion.HasValue && DateFinPromotion.Value.Date >= DateTime.Today;

        public decimal PrixPromo =>
            EstEnPromotion
                ? Math.Round(PrixUnitaire * (1 - RemisePourcentage!.Value / 100m), 2)
                : PrixUnitaire;

        private int _joursInactivite = -1;
        public int JoursInactivite
        {
            get
            {
                if (_joursInactivite >= 0) return _joursInactivite;
                if (DateCreation.Year > 2000)
                {
                    return (int)Math.Max(0, (DateTime.Today - DateCreation.Date).TotalDays);
                }
                return 0;
            }
            set => _joursInactivite = value;
        }

        public StockDto? Stock { get; set; } = new StockDto();
        public List<PrevisionEtatProduitDto> Previsions { get; set; } = new();

        public PrevisionEtatProduitDto? DernierePrevision => Previsions?.OrderByDescending(p => p.DateCalcul).FirstOrDefault();
        /// <summary>
        /// Détermine le statut IA et stock du produit selon un ordre de priorité STRICT :
        /// 1. RUPTURE / SUR_COMMANDE (stock physique == 0)
        /// 2. STOCK_FAIBLE (stock < seuil alerte effectif, fallback 10)
        /// 3. SURSTOCK (stock >= 500 u. ET inactivité >= 21j) — PRIORITAIRE sur OBSOLESCENCE
        /// 4. OBSOLESCENCE (prédiction IA)
        /// 5. OPTIMAL (par défaut)
        /// 
        /// REMARQUE IMPORTANTE : SURSTOCK est intentionnellement évalué AVANT OBSOLESCENCE.
        /// Si un produit remplit les deux conditions simultanément, il sera classé SURSTOCK.
        /// </summary>
        public string StatutIAType
        {
            get
            {
                int qte = Stock?.QuantiteActuelle ?? 0;
                int seuilAlerte = (Stock?.SeuilAlerte > 0) ? Stock.SeuilAlerte : 10;
                const int SEUIL_SURSTOCK_UNITES = 500;
                const int DELAI_INACTIVITE_JOURS = 21;
                string? prevRisque = DernierePrevision?.TypeRisquePredit?.ToUpperInvariant();

                // 1. Stock nul
                if (qte == 0)
                {
                    return DisponibleSurCommande ? "SUR_COMMANDE" : "RUPTURE";
                }
                // 2. Stock faible (sous le seuil d'alerte sécurisé)
                else if (qte < seuilAlerte)
                {
                    return "STOCK_FAIBLE";
                }
                // 3. Surstock (prioritaire sur Obsolescence quand les 2 conditions sont vraies)
                else if (qte >= SEUIL_SURSTOCK_UNITES && JoursInactivite >= DELAI_INACTIVITE_JOURS)
                {
                    return "SURSTOCK";
                }
                // 4. Obsolescence (signal IA)
                else if (prevRisque == "OBSOLESCENCE")
                {
                    return "OBSOLESCENCE";
                }
                // 5. Optimal / Normal par défaut
                else
                {
                    return "OPTIMAL";
                }
            }
        }

        public int StatutIAPriorite => StatutIAType switch
        {
            "RUPTURE" => 1,
            "STOCK_FAIBLE" => 2,
            "SURSTOCK" => 3,
            "OBSOLESCENCE" => 4,
            "SUR_COMMANDE" => 5,
            _ => 6
        };

        public string StatutIABadgeText => StatutIAType switch
        {
            "SUR_COMMANDE" => "Sur commande",
            "RUPTURE" => "Rupture",
            "STOCK_FAIBLE" => "Stock faible",
            "SURSTOCK" => "Surstock",
            "OBSOLESCENCE" => "Obsolete",
            _ => "Optimal"
        };

        public string? StatutIAActionTexte
        {
            get
            {
                if (StatutIAType == "SURSTOCK" || StatutIAType == "OBSOLESCENCE")
                {
                    if (!string.IsNullOrWhiteSpace(DernierePrevision?.ActionRecommandee?.TexteGenere))
                    {
                        return DernierePrevision.ActionRecommandee.TexteGenere;
                    }
                }
                return null;
            }
        }
    }

    public class StockDto
    {
        public int Id { get; set; }
        public int QuantiteActuelle { get; set; }
        public int SeuilAlerte { get; set; } = 10;
        public int? SeuilSurstock { get; set; }
        public string Emplacement { get; set; } = "Magasin principal";
    }
}