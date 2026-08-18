using static WicStock_.Models.Enums;

namespace WicStock_.Models
{
    public class MouvementStock
    {
        public int Id { get; set; }

        public TypeMouvement Type { get; set; }
        public int Quantite { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
        public string Motif { get; set; } = string.Empty;

        // Clé étrangère
        public int StockId { get; set; }
        public Stock? Stock { get; set; }
    }
}
