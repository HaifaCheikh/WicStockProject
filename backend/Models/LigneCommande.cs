using System.ComponentModel.DataAnnotations.Schema;

namespace WicStock_.Models
{
    /// <summary>
    /// Représente une ligne de commande dans une commande multi-articles.
    /// Le prix est un snapshot au moment de la commande — jamais recalculé après coup depuis Produit.
    /// </summary>
    public class LigneCommande
    {
        public int Id { get; set; }

        // FK vers la commande parente
        public int HistoriqueVenteId { get; set; }
        public HistoriqueVente? HistoriqueVente { get; set; }

        // FK vers le produit
        public int ProduitId { get; set; }
        public Produit? Produit { get; set; }

        // FK vers la variante spécifique (optionnelle pour compatibilité historique)
        public int? VarianteProduitId { get; set; }
        public VarianteProduit? VarianteProduit { get; set; }

        /// <summary>Quantité commandée pour ce produit.</summary>
        public int Quantite { get; set; }

        /// <summary>
        /// Prix unitaire au moment de la commande (snapshot immuable).
        /// Jamais recalculé depuis Produit.PrixUnitaire après création.
        /// </summary>
        public decimal PrixUnitaire { get; set; }

        /// <summary>Sous-total calculé : PrixUnitaire × Quantite.</summary>
        [NotMapped]
        public decimal SousTotal => PrixUnitaire * Quantite;

        /// <summary>Indique si ce produit était en rupture et commandé sur commande.</summary>
        public bool EstSurCommande { get; set; } = false;

        /// <summary>Snapshot des attributs choisis par le client au moment de la commande.</summary>
        public string? Genre { get; set; }
        public string? Taille { get; set; }
        public string? Couleur { get; set; }
    }
}
