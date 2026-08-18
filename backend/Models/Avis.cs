using static WicStock_.Models.Enums;

namespace WicStock_.Models
{
    public class Avis
    {
        public int Id { get; set; }

        public int CommandeId { get; set; }
        public HistoriqueVente? Commande { get; set; }

        public int ProduitId { get; set; }
        public Produit? Produit { get; set; }

        public int ClientId { get; set; }
        public Utilisateur? Client { get; set; }

        /// <summary>
        /// Note de 1 à 5
        /// </summary>
        public int Note { get; set; }

        public string? Commentaire { get; set; }

        public DateTime DateCreation { get; set; } = DateTime.Now;

        public StatutAvis Statut { get; set; } = StatutAvis.PUBLIE;

        public bool EstMasque { get; set; } = false;
    }
}
