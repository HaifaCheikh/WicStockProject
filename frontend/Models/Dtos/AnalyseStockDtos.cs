using System;
using System.Collections.Generic;

namespace WicStock.Web.Models.Dtos
{
    public class MetriquesStockDto
    {
        public int ProduitId { get; set; }
        public int StockActuel { get; set; }
        public bool EstEnSurstock { get; set; }
        public int SurplusUnites { get; set; }
        public int SeuilSurstock { get; set; }
        public double PourcentageAuDessusDuSeuil { get; set; }
        /// <summary>
        /// Part du surstock dans le stock total, TOUJOURS bornee entre 0 et 100%.
        /// Formule (backend) : ((StockActuel - SeuilSurstock) / StockActuel) * 100.
        /// Complementaire a PourcentageAuDessusDuSeuil (ecart brut, non borne, ex: +700%) :
        /// celle-ci repond a "quelle part de mon stock est du surstock ?" (ex: 87%).
        /// </summary>
        public double PourcentagePartSurstock { get; set; }
        public int JoursDepuisDerniereSortie { get; set; }
        public string DateAjoutFormatee { get; set; } = string.Empty;
        public double TauxEcoulement90Jours { get; set; }
        public string Categorie { get; set; } = string.Empty;
        public double TauxEcoulementMoyenCategorie90Jours { get; set; }
        public bool EstTendanceCategorie { get; set; }
        public int NbReferencesSimilairesEnSurstock { get; set; }
        public int DureeEcoulementMoyenneProduitsSimilaires { get; set; }
        public decimal ValeurStockImmobilisee { get; set; }
        public decimal CoutPossessionEstimeMensuel { get; set; }
    }

    public class ActionRecommandeeIaDto
    {
        public string Id { get; set; } = string.Empty;
        public string TypeAction { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Justification { get; set; } = string.Empty;
        public Dictionary<string, object>? Params { get; set; }
    }

    public class ActionActiveDto
    {
        public int Id { get; set; }
        public string TypeAction { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime DateApplication { get; set; } = DateTime.Now;
        public DateTime? DateFin { get; set; }
        public int? RemisePourcentage { get; set; }
        public int? DureeJours { get; set; }
        public int? QuantiteCible { get; set; }
        public decimal? PrixPromo { get; set; }
        public string? Motif { get; set; }
        public string? Destination { get; set; }
        public string? Destinataire { get; set; }
        public string? Priorite { get; set; }
        public string? InitieParRole { get; set; } = "ADMIN";
        public string? NomAuteur { get; set; } // Nom de l'utilisateur qui a pris l'action
    }

    public class AnalyseProduitDto
    {
        public int ProduitId { get; set; }
        public DateTime DateAnalyse { get; set; }
        public bool EstModeDegrade { get; set; }
        public MetriquesStockDto Metriques { get; set; } = new();
        public string Diagnostic { get; set; } = string.Empty;
        public ActionActiveDto? ActionActive { get; set; }
        public List<ActionRecommandeeIaDto> Actions { get; set; } = new();
    }

    public class ExecutionActionRequestDto
    {
        public int ProduitId { get; set; }
        public string TypeAction { get; set; } = string.Empty;
        public string ActionLabel { get; set; } = string.Empty;
        public Dictionary<string, string>? Params { get; set; }
    }
}