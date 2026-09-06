namespace WicStock.Web.Models.Dtos
{
    /// <summary>
    /// Représente un article dans le panier local (localStorage).
    /// Le prix est un snapshot au moment de l'ajout au panier.
    /// </summary>
    public class CartItemDto
    {
        public int ProduitId { get; set; }
        public string Nom { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string? Categorie { get; set; }
        public string? TypeTissu { get; set; }

        /// <summary>Prix original (avant promo).</summary>
        public decimal PrixUnitaire { get; set; }

        /// <summary>Prix effectif appliqué (avec promo si applicable).</summary>
        public decimal PrixEffectif { get; set; }

        public bool EstEnPromotion { get; set; }
        public int RemisePourcentage { get; set; }

        public int Quantite { get; set; } = 1;
        public int QuantiteDisponible { get; set; }
        public bool DisponibleSurCommande { get; set; }

        public decimal SousTotal => PrixEffectif * Quantite;
    }

    /// <summary>DTO envoyé au backend pour une commande multi-articles.</summary>
    public class CommandeMultiCreateClientDto
    {
        public List<LigneCommandeClientDto> Lignes { get; set; } = new();
        public string? AdresseLivraison { get; set; }
        public string? CodePostal { get; set; }
        public string? Ville { get; set; }
        public string? Pays { get; set; }
        public DateTime? DateSouhaitee { get; set; }
    }

    public class LigneCommandeClientDto
    {
        public int ProduitId { get; set; }
        public int Quantite { get; set; }
    }

    /// <summary>Réponse succès du backend après création de commande.</summary>
    public class CommandeMultiResultClientDto
    {
        public int CommandeId { get; set; }
        public decimal MontantTotal { get; set; }
        public int NombreLignes { get; set; }
        public bool EstSurCommande { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<LigneResultClientDto> Lignes { get; set; } = new();
    }

    public class LigneResultClientDto
    {
        public int ProduitId { get; set; }
        public string ProduitNom { get; set; } = string.Empty;
        public string ProduitReference { get; set; } = string.Empty;
        public int Quantite { get; set; }
        public decimal PrixUnitaire { get; set; }
        public bool EstSurCommande { get; set; }
        public decimal SousTotal => PrixUnitaire * Quantite;
    }

    /// <summary>Réponse d'erreur 409 — rupture de stock détaillée.</summary>
    public class CommandeStockErrorClientDto
    {
        public string Message { get; set; } = string.Empty;
        public List<LigneStockErrorClientDto> LignesEnErreur { get; set; } = new();
    }

    public class LigneStockErrorClientDto
    {
        public int ProduitId { get; set; }
        public string ProduitNom { get; set; } = string.Empty;
        public string ProduitReference { get; set; } = string.Empty;
        public int QuantiteDemandee { get; set; }
        public int QuantiteDisponible { get; set; }
        public bool EstSurCommande { get; set; }
    }
}
