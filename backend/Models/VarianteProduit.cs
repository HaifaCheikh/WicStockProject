using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WicStock_.Models
{
    [Index(nameof(Reference), IsUnique = true)]
    public class VarianteProduit
    {
        public int Id { get; set; }

        public int ProduitId { get; set; }
        public Produit? Produit { get; set; }

        /// <summary>
        /// SKU unique de la variante (ex: TIS-DNM-01-H-M-BLEUINDIGO).
        /// </summary>
        [Required, MaxLength(100)]
        public string Reference { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Genre { get; set; }

        [MaxLength(50)]
        public string? Taille { get; set; }

        [MaxLength(50)]
        public string? Couleur { get; set; }

        /// <summary>
        /// Quantité en stock spécifique à cette variante.
        /// </summary>
        public int QuantiteActuelle { get; set; } = 0;

        /// <summary>
        /// Seuil d'alerte spécifique à cette variante.
        /// </summary>
        public int SeuilAlerte { get; set; } = 10;

        /// <summary>
        /// Prix de vente spécifique à cette variante (null = utilise PrixUnitaire du Produit).
        /// </summary>
        public decimal? PrixOverride { get; set; }

        [NotMapped]
        public decimal PrixEffectif => (PrixOverride.HasValue && PrixOverride.Value > 0)
            ? PrixOverride.Value
            : (Produit?.PrixUnitaire ?? 0);

        [NotMapped]
        public string CodeHexCouleur => AttributsProduit.GetHex(Couleur);
    }
}
