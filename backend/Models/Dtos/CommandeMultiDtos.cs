namespace WicStock_.Models.Dtos
{
    // ====================================================================
    // DTOs pour la création d'une commande multi-articles (Boutique publique)
    // Le prix n'est JAMAIS envoyé par le frontend — recalculé côté serveur.
    // ====================================================================

    public class CommandeMultiCreateDto
    {
        /// <summary>Liste des lignes de la commande (au moins 1 ligne requise).</summary>
        public List<LigneCommandeCreateDto> Lignes { get; set; } = new();

        // Informations de livraison optionnelles
        public string? AdresseLivraison { get; set; }
        public string? CodePostal { get; set; }
        public string? Ville { get; set; }
        public string? Pays { get; set; }
        public DateTime? DateSouhaitee { get; set; }
    }

    public class LigneCommandeCreateDto
    {
        /// <summary>ID du produit à commander.</summary>
        public int ProduitId { get; set; }

        /// <summary>Quantité demandée (>= 1).</summary>
        public int Quantite { get; set; }
    }

    // ====================================================================
    // Réponses retournées après création de commande
    // ====================================================================

    public class CommandeMultiResultDto
    {
        public int CommandeId { get; set; }
        public decimal MontantTotal { get; set; }
        public int NombreLignes { get; set; }
        public bool EstSurCommande { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<LigneCommandeResultDto> Lignes { get; set; } = new();
    }

    public class LigneCommandeResultDto
    {
        public int ProduitId { get; set; }
        public string ProduitNom { get; set; } = string.Empty;
        public string ProduitReference { get; set; } = string.Empty;
        public int Quantite { get; set; }
        public decimal PrixUnitaire { get; set; }
        public decimal SousTotal => PrixUnitaire * Quantite;
        public bool EstSurCommande { get; set; }
    }

    // ====================================================================
    // Réponse d'erreur 409 — rupture de stock détaillée par ligne
    // ====================================================================

    public class CommandeStockErrorDto
    {
        public string Message { get; set; } = "Commande impossible : stock insuffisant pour certains articles.";
        public List<LigneStockErrorDto> LignesEnErreur { get; set; } = new();
    }

    public class LigneStockErrorDto
    {
        public int ProduitId { get; set; }
        public string ProduitNom { get; set; } = string.Empty;
        public string ProduitReference { get; set; } = string.Empty;
        public int QuantiteDemandee { get; set; }
        public int QuantiteDisponible { get; set; }
        public bool EstSurCommande { get; set; }
        public string Message =>
            EstSurCommande
                ? $"« {ProduitNom} » est commandable sur demande — pas de vérification de stock."
                : $"« {ProduitNom} » : {QuantiteDemandee} demandé(es), seulement {QuantiteDisponible} disponible(s).";
    }

    // ====================================================================
    // DTO pour la fiche détail d'une commande multi-articles (côté client)
    // ====================================================================

    public class MaCommandeMultiDto
    {
        public int Id { get; set; }
        public DateTime DateVente { get; set; }
        public decimal MontantTotal { get; set; }
        public string StatutCommande { get; set; } = string.Empty;
        public string? Statut { get; set; }
        public bool EstMultiLignes { get; set; }
        public DateTime? DateSouhaitee { get; set; }
        public List<LigneCommandeResultDto> Lignes { get; set; } = new();

        // Rétrocompatibilité mono-produit (EstMultiLignes = false)
        public string? ProduitNom { get; set; }
        public string? ProduitReference { get; set; }
        public string? ProduitImageUrl { get; set; }
        public int QuantiteVendue { get; set; }
        public decimal PrixUnitaire { get; set; }
    }
}
