using static WicStock_.Models.Enums;

namespace WicStock_.Models
{
    public class PrevisionEtatProduit
    {
        public int Id { get; set; }

        public int ProduitId { get; set; }
        public Produit? Produit { get; set; }

        public TypeRisque TypeRisquePredit { get; set; }
        public float ScoreRisque { get; set; }
        public int QuantitePredite { get; set; }
        public int HorizonJours { get; set; }
        public DateTime DateCalcul { get; set; } = DateTime.Now;

        public ActionRecommandee? ActionRecommandee { get; set; }
    }
}
