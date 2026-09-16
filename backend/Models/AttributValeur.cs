using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace WicStock_.Models
{
    [Index(nameof(Type), nameof(Valeur), IsUnique = true)]
    public class AttributValeur
    {
        public int Id { get; set; }

        /// <summary>
        /// Type d'attribut : "Genre", "Taille", ou "Couleur".
        /// </summary>
        [Required, MaxLength(30)]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Libellé affiché : "Homme", "M", "Bleu indigo", etc.
        /// </summary>
        [Required, MaxLength(100)]
        public string Valeur { get; set; } = string.Empty;

        /// <summary>
        /// Code couleur hexadécimal (utilisé uniquement si Type == "Couleur").
        /// </summary>
        [MaxLength(10)]
        public string? CodeHex { get; set; }

        /// <summary>
        /// Ordre d'affichage dans les listes déroulantes.
        /// </summary>
        public int Ordre { get; set; } = 0;

        /// <summary>
        /// Soft-delete : si false, masqué des nouveaux formulaires tout en restant lisible dans les anciennes commandes.
        /// </summary>
        public bool Actif { get; set; } = true;
    }
}
