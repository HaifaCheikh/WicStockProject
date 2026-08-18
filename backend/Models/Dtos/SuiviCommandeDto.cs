namespace WicStock_.Models.Dtos
{
    public class SuiviCommandeDto
    {
        public int Id { get; set; }
        public int ProduitId { get; set; }
        public string? ProduitNom { get; set; }
        public string? ProduitReference { get; set; }
        public string? ProduitImageUrl { get; set; }
        public int QuantiteVendue { get; set; }
        public decimal PrixUnitaire { get; set; }
        public string Statut { get; set; } = string.Empty;
        public string StatutCommande { get; set; } = string.Empty;
        public bool EstSurCommande { get; set; }
        public DateTime DateVente { get; set; }
        public DateTime? DateSouhaitee { get; set; }
        public DateTime? DateConfirmation { get; set; }
        public DateTime? DateDebutPreparation { get; set; }
        public DateTime? DateEstimeePreparation { get; set; }
        public DateTime? DatePrete { get; set; }
        public DateTime? DatePaiement { get; set; }
        public DateTime? DateLivraison { get; set; }
        public string? PaymentIntentId { get; set; }
        public string? AdresseLivraison { get; set; }
        public string? CodePostal { get; set; }
        public string? Ville { get; set; }
        public string? Pays { get; set; }
        public int? ResponsableId { get; set; }
        public string? ResponsableNom { get; set; }
    }

    public class AssignerCommandeDto
    {
        public int ResponsableId { get; set; }
    }

    public class ResponsableDto
    {
        public int Id { get; set; }
        public string Nom { get; set; } = string.Empty;
        public string Prenom { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}