namespace WicStock_.Models
{
    public class HistoriqueProduction
    {
        public int Id { get; set; }

        public DateTime DateProduction { get; set; }
        public int QuantiteProduite { get; set; }

        public int ProduitId { get; set; }
        public Produit? Produit { get; set; }
    }
}
