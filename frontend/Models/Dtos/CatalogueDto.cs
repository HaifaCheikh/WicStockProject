namespace WicStock.Web.Models.Dtos
{
    public class CatalogueProduitDto
    {
        public int Id { get; set; }
        public string Reference { get; set; } = "";
        public string Nom { get; set; } = "";
        public string? TypeTissu { get; set; }
        public string? Categorie { get; set; }
        public decimal PrixUnitaire { get; set; } = 0;
        public string? ImageUrl { get; set; }
        public int QuantiteStock { get; set; } = 0;
        public int QuantiteActuelle { get; set; } = 0;
        public int QuantiteDisponible { get; set; } = 0;
        public int SeuilAlerte { get; set; } = 10;
        public bool DisponibleSurCommande { get; set; } = false;
        public bool EstStockFaible { get; set; } = false;

        // Rupture réelle (stock à 0 et non commandable)
        public bool EstEnRupture => GetQuantiteEffective() <= 0 && !DisponibleSurCommande;

        // Stock faible (entre 1 et seuil d'alerte inclus)
        public bool EstStockFaibleEffectif => EstStockFaible || (GetQuantiteEffective() > 0 && GetQuantiteEffective() <= SeuilAlerte);

        // Produit commandable hors stock
        public bool EstSurCommande => GetQuantiteEffective() <= 0 && DisponibleSurCommande;

        // Le client peut ouvrir le formulaire de commande
        public bool PeutCommander => GetQuantiteEffective() > 0 || DisponibleSurCommande;

        private int GetQuantiteEffective() => QuantiteDisponible > 0 ? QuantiteDisponible : QuantiteActuelle;

        // 🏷️ Promotion IA confirmée
        public int RemisePourcentage { get; set; } = 0;
        public DateTime? DateFinPromotion { get; set; }
        public bool EstEnPromotion { get; set; } = false;
        public decimal PrixPromo { get; set; } = 0;

        public decimal PrixEffectif => EstEnPromotion && PrixPromo > 0 ? PrixPromo : PrixUnitaire;
    }

    public class CommandeCreateDto
    {
        public int ProduitId { get; set; }
        public int QuantiteVendue { get; set; }
        public decimal PrixUnitaire { get; set; }
        public DateTime? DateSouhaitee { get; set; }
    }

    public class MaCommandeDto
    {
        public int Id { get; set; }
        public DateTime DateVente { get; set; }
        public int QuantiteVendue { get; set; }
        public decimal PrixUnitaire { get; set; }
        public string StatutCommande { get; set; } = "ACCEPTEE";
        public string? Statut { get; set; }
        public bool EstSurCommande { get; set; }
        public int ProduitId { get; set; }
        public string? ProduitNom { get; set; }
        public string? ProduitReference { get; set; }
        public string? ProduitCategorie { get; set; }
        public string? ProduitImageUrl { get; set; }
        public DateTime? DateSouhaitee { get; set; }
        public DateTime? DateEstimeePreparation { get; set; }
        public decimal TotalCommande { get; set; }
    }

    public class CommandeManagerDto
    {
        public int Id { get; set; }
        public DateTime DateVente { get; set; }
        public int QuantiteVendue { get; set; }
        public decimal PrixUnitaire { get; set; }
        public string StatutCommande { get; set; } = "ACCEPTEE";
        public string? Statut { get; set; }
        public bool EstSurCommande { get; set; }
        public int ProduitId { get; set; }
        public string? ProduitNom { get; set; }
        public string? ProduitReference { get; set; }
        public int? UtilisateurId { get; set; }
        public string? ClientNom { get; set; }
        public string? ClientEmail { get; set; }
        public DateTime? DateSouhaitee { get; set; }
        public DateTime? DateEstimeePreparation { get; set; }
        public int? ResponsableId { get; set; }
        public string? ResponsableNom { get; set; }
        public int? LivreurId { get; set; }
        public string? LivreurNom { get; set; }
        public DateTime? DatePaiement { get; set; }
        public decimal TotalCommande => QuantiteVendue * PrixUnitaire;
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
}

