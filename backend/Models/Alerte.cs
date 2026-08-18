using static WicStock_.Models.Enums;

namespace WicStock_.Models
{
    public class Alerte
    {
        public int Id { get; set; }

        public TypeRisque TypeRisque { get; set; }
        public DateTime DateDetection { get; set; } = DateTime.Now;
        public StatutAlerte Statut { get; set; } = StatutAlerte.NON_TRAITEE;
        public int NiveauCriticite { get; set; }

        public int ProduitId { get; set; }
        public Produit? Produit { get; set; }

        // Utilisateur qui traite l'alerte (peut être vide au départ)
        public int? UtilisateurId { get; set; }
        public Utilisateur? Utilisateur { get; set; }
    }
}
