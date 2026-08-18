using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WicStock_.Models.Dtos
{
    public class MetriquesStockDto
    {
        public int ProduitId { get; set; }
        public string NomProduit { get; set; } = string.Empty;
        public int StockActuel { get; set; }
        public int SeuilSurstock { get; set; }
        public double PourcentageAuDessusDuSeuil { get; set; }
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

    public class ActionRecommandeeDto
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string TypeAction { get; set; } = string.Empty; // PROMOTION_CIBLEE, RECYCLAGE_ANTICIPE, NOTIFICATION_PRODUCTION
        public string Label { get; set; } = string.Empty;
        public string Justification { get; set; } = string.Empty;
        public Dictionary<string, object>? Params { get; set; }
    }

    public class ActionActiveDto
    {
        public int Id { get; set; }
        public string TypeAction { get; set; } = string.Empty; // PROMOTION_CIBLEE, RECYCLAGE_ANTICIPE, NOTIFICATION_PRODUCTION
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
    }

    public class AnalyseProduitDto
    {
        public int ProduitId { get; set; }
        public DateTime DateAnalyse { get; set; } = DateTime.Now;
        public bool EstModeDegrade { get; set; } = false;
        public MetriquesStockDto Metriques { get; set; } = new();
        public string Diagnostic { get; set; } = string.Empty;
        public ActionActiveDto? ActionActive { get; set; }
        public List<ActionRecommandeeDto> Actions { get; set; } = new();
    }

    public class ExecutionActionRequestDto
    {
        public int ProduitId { get; set; }
        public string TypeAction { get; set; } = string.Empty;
        public string ActionLabel { get; set; } = string.Empty;
        public Dictionary<string, string>? Params { get; set; }
    }
}