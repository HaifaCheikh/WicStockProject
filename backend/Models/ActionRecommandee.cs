using static WicStock_.Models.Enums;

namespace WicStock_.Models
{
    public class ActionRecommandee
    {
        public int Id { get; set; }

        public int ProduitId { get; set; }

        public TypeAction TypeAction { get; set; }
        public string TexteGenere { get; set; } = string.Empty;
        public DateTime DateGeneration { get; set; } = DateTime.Now;
        public string Source { get; set; } = string.Empty; 

        public int? PrevisionEtatProduitId { get; set; }
        public PrevisionEtatProduit? PrevisionEtatProduit { get; set; }

        // Utilisateur qui valide l'action (peut être vide au départ)
        public int? UtilisateurId { get; set; }
        public Utilisateur? Utilisateur { get; set; }
    }
}
