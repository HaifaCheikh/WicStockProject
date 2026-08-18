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

        public StockDto? Stock { get; set; } = new StockDto();
        public List<PrevisionEtatProduitDto> Previsions { get; set; } = new();

        public PrevisionEtatProduitDto? DernierePrevision => Previsions?.OrderByDescending(p => p.DateCalcul).FirstOrDefault();

        public string StatutIAType
        {
            get
            {
                int qte = Stock?.QuantiteActuelle ?? 0;
                int seuilAlerte = (Stock?.SeuilAlerte ?? 0) > 0 ? Stock!.SeuilAlerte : 10;
                int seuilSurstock = (Stock?.SeuilAlerte > 50) ? Stock.SeuilAlerte : 100;
                string? prevRisque = DernierePrevision?.TypeRisquePredit?.ToUpperInvariant();

                if (DisponibleSurCommande && qte == 0)
                    return "SUR_COMMANDE";

                if (qte == 0 || qte < seuilAlerte || prevRisque == "RUPTURE")
                    return "RUPTURE";
                if (prevRisque == "SURSTOCK" || qte > seuilSurstock)
                    return "SURSTOCK";
                if (prevRisque == "OBSOLESCENCE")
                    return "OBSOLESCENCE";

                return "OPTIMAL";
            }
        }

        public int StatutIAPriorite => StatutIAType switch
        {
            "RUPTURE" => 1,
            "SURSTOCK" => 2,
            "OBSOLESCENCE" => 3,
            "SUR_COMMANDE" => 4,
            _ => 5
        };

        public string StatutIABadgeText => StatutIAType switch
        {
            "SUR_COMMANDE" => "Sur commande",
            "RUPTURE" => "Rupture",
            "SURSTOCK" => "Surstock",
            "OBSOLESCENCE" => "Obsolète",
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
        public string Emplacement { get; set; } = "Magasin principal";
    }
}