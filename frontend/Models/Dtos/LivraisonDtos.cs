namespace WicStock.Web.Models.Dtos
{
    public class LivraisonCommandeDto
    {
        public int Id { get; set; }
        public DateTime DateVente { get; set; }
        public int QuantiteVendue { get; set; }
        public decimal PrixUnitaire { get; set; }
        public decimal TotalCommande => QuantiteVendue * PrixUnitaire;
        public string? Statut { get; set; }
        public string StatutCommande { get; set; } = "ACCEPTEE";
        public bool EstSurCommande { get; set; }

        public string ProduitNom { get; set; } = string.Empty;
        public string ProduitReference { get; set; } = string.Empty;
        public string? ProduitImageUrl { get; set; }

        // Coordonnées client pour la livraison
        public int? ClientId { get; set; }
        public string ClientNom { get; set; } = string.Empty;
        public string ClientEmail { get; set; } = string.Empty;
        public string? ClientTelephone { get; set; }
        public string? AdresseLivraison { get; set; }
        public string? CodePostal { get; set; }
        public string? Ville { get; set; }
        public string? Pays { get; set; }

        // Livreur assigné
        public int? LivreurId { get; set; }
        public string? LivreurNom { get; set; }

        public DateTime? DatePrete { get; set; }
        public DateTime? DatePaiement { get; set; }
        public DateTime? DateLivraison { get; set; }
    }

    public class AssignerLivreurDto
    {
        public int CommandeId { get; set; }
        public int LivreurId { get; set; }
    }

    public class LivreurInfoDto
    {
        public int Id { get; set; }
        public string Nom { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Telephone { get; set; }
    }
}

