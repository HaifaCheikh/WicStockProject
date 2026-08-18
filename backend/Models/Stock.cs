namespace WicStock_.Models
{
    public class Stock
    {
        public int Id { get; set; }

        public int QuantiteActuelle { get; set; }
        public int SeuilAlerte { get; set; }
        public string Emplacement { get; set; } = string.Empty;
        public DateTime DateMiseAJour { get; set; } = DateTime.Now;

        // Clé étrangère (relation 1-1 avec Produit)
        public int ProduitId { get; set; }
        public Produit? Produit { get; set; }

        public List<MouvementStock> Mouvements { get; set; } = new();

        public bool EstSousLeSeuil()
        {
            return QuantiteActuelle < SeuilAlerte;
        }
    }
}
