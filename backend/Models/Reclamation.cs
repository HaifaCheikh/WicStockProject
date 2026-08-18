using static WicStock_.Models.Enums;

namespace WicStock_.Models
{
    public class Reclamation
    {
        public int Id { get; set; }

        public int CommandeId { get; set; }
        public HistoriqueVente? Commande { get; set; }

        public int ProduitId { get; set; }
        public Produit? Produit { get; set; }

        public int ClientId { get; set; }
        public Utilisateur? Client { get; set; }

        /// <summary>
        /// Motif: Article défectueux, Mauvaise référence livrée, Quantité incorrecte, Produit non conforme à la description, Retard de livraison, Autre
        /// </summary>
        public string Motif { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// URLs des photos jointes séparées par des virgules ou JSON array
        /// </summary>
        public string? PhotosUrls { get; set; }

        public DateTime DateCreation { get; set; } = DateTime.Now;

        public StatutReclamation Statut { get; set; } = StatutReclamation.ENVOYEE;

        public string? ReponseAdmin { get; set; }

        public DateTime? DateReponse { get; set; }
    }
}
