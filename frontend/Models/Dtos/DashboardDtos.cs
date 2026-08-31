using System;
using System.Collections.Generic;

namespace WicStock.Web.Models.Dtos
{
    public class AdminDashboardDto
    {
        public int TotalProduits { get; set; }
        public int TotalProduitsVendus { get; set; }
        public decimal RevenuTotal { get; set; }
        public int TotalUtilisateurs { get; set; }
        public int TotalCommandes { get; set; }
        public Dictionary<string, int> RepartitionStatuts { get; set; } = new();
        public int ProduitsEnRupture { get; set; }

        public List<MonthlyRevenueDto> EvolutionRevenus { get; set; } = new();
        public List<TopProduitDto> TopProduitsVendus { get; set; } = new();
        public StatutCommandesDto StatutCommandes { get; set; } = new();
        public List<ProduitSurstockDto> ProduitsEnSurstock { get; set; } = new();
    }

    public class ManagerDashboardDto
    {
        public int TotalStockArticles { get; set; }
        public int ProduitsStockFaible { get; set; }
        public int TotalProduitsVendus { get; set; }
        public decimal RevenuAujourdhui { get; set; }
        public decimal RevenuSemaine { get; set; }
        public Dictionary<string, int> RepartitionStatuts { get; set; } = new();
        public int CommandesEnAttenteCount { get; set; }

        public List<ProduitStockAlerteDto> ProduitsAlerte { get; set; } = new();
        public List<MouvementRecentDto> MouvementsRecents { get; set; } = new();
        public List<ProduitSurstockDto> ProduitsEnSurstock { get; set; } = new();
    }

    public class MonthlyRevenueDto
    {
        public string Mois { get; set; } = string.Empty;
        public decimal Montant { get; set; }
    }

    public class TopProduitDto
    {
        public string Nom { get; set; } = string.Empty;
        public int QuantiteVendue { get; set; }
        public decimal TotalVentes { get; set; }
    }

    public class StatutCommandesDto
    {
        public int Acceptees { get; set; }
        public int EnAttente { get; set; }
        public int Refusees { get; set; }
    }

    public class ProduitStockAlerteDto
    {
        public int Id { get; set; }
        public string Nom { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public int QuantiteActuelle { get; set; }
        public int SeuilAlerte { get; set; }
    }

    public class MouvementRecentDto
    {
        public int Id { get; set; }
        public string ProduitNom { get; set; } = string.Empty;
        public string TypeMouvement { get; set; } = string.Empty;
        public int Quantite { get; set; }
        public DateTime DateMouvement { get; set; }
    }

    public class ProduitSurstockDto
    {
        public int Id { get; set; }
        public string Nom { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public int QuantiteActuelle { get; set; }
        /// <summary>
        /// Surplus en unités = max(0, QuantiteActuelle - SeuilSurstock).
        /// Donnée brute et actionnable, à afficher en priorité (ex: "+3500 u. en surplus")
        /// plutôt que le pourcentage seul.
        /// </summary>
        public int SurplusUnites { get; set; }
        /// <summary>
        /// Part du surstock dans le stock total, TOUJOURS bornée entre 0 et 100%.
        /// Formule (backend) : ((QuantiteActuelle - SeuilSurstock) / QuantiteActuelle) * 100.
        /// Remplace l'ancien calcul non borné ((stock-seuil)/seuil)*100 qui donnait des
        /// valeurs du type +700% pour de gros stocks.
        /// </summary>
        public int PourcentageSurstock { get; set; }
    }
}