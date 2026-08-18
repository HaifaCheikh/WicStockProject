using static WicStock_.Models.Enums;

namespace WicStock_.Models
{
    public class HistoriqueVente
    {
        public int Id { get; set; }

        public DateTime DateVente { get; set; }
        public int QuantiteVendue { get; set; }
        public decimal PrixUnitaire { get; set; }

        public string StatutCommande { get; set; } = "ACCEPTEE"; // ACCEPTEE, EN_ATTENTE, REFUSEE

        /// <summary>Cycle détaillé pour les commandes sur commande (stock insuffisant).</summary>
        public StatutCommandeDetaille? Statut { get; set; }

        /// <summary>Date souhaitée par le client (optionnelle).</summary>
        public DateTime? DateSouhaitee { get; set; }

        public DateTime? DateConfirmation { get; set; }

        public DateTime? DateDebutPreparation { get; set; }

        /// <summary>Date estimée de préparation renseignée par le responsable à la confirmation.</summary>
        public DateTime? DateEstimeePreparation { get; set; }

        public DateTime? DatePrete { get; set; }

        public DateTime? DatePaiement { get; set; }

        public DateTime? DateLivraison { get; set; }

        public string? PaymentIntentId { get; set; }

        public string? AdresseLivraison { get; set; }
        public string? CodePostal { get; set; }
        public string? Ville { get; set; }
        public string? Pays { get; set; }

        public bool EstSurCommande { get; set; } = false;

        public int ProduitId { get; set; }
        public Produit? Produit { get; set; }

        // Lien vers le client qui a passé la commande
        public int? UtilisateurId { get; set; }
        public Utilisateur? Utilisateur { get; set; }

        // Lien vers le responsable assigné (pour les commandes sur commande)
        public int? ResponsableId { get; set; }
        public Utilisateur? Responsable { get; set; }

        // Lien vers le livreur assigné (pour la livraison)
        public int? LivreurId { get; set; }
        public Utilisateur? Livreur { get; set; }
    }
}
