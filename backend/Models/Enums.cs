namespace WicStock_.Models
{
    public class Enums
    {
        public enum TypeMouvement
        {
            ENTREE,
            SORTIE,
            RETOUR,
            AJUSTEMENT
        }

        public enum TypeRisque
        {
            SURSTOCK,
            OBSOLESCENCE,
            RUPTURE
        }

        public enum StatutAlerte
        {
            NON_TRAITEE,
            EN_COURS,
            TRAITEE
        }

        public enum RoleUtilisateur
        {
            ADMIN,
            RESPONSABLE_STOCK_PRODUCTION,
            CLIENT,
            LIVREUR
        }

        public enum TypeAction
        {
            PROMOTION_CIBLEE,
            REDISTRIBUTION,
            RECYCLAGE_ANTICIPE,
            AUCUNE_ACTION
        }

        public enum TypeNotification
        {
            RUPTURE_STOCK,
            COMMANDE_EN_ATTENTE,
            COMMANDE_CONFIRMEE,
            ACTION_IA_A_VALIDER,
            NOUVEAU_PRODUIT,
            PAIEMENT_RECU,
            COMMANDE_LIVREE,
            COMMANDE_EN_LIVRAISON,
            COMMANDE_ASSIGNATION_LIVREUR
        }

        public enum StatutCommandeDetaille
        {
            EN_ATTENTE_CONFIRMATION,
            ACCEPTEE,
            CONFIRMEE,
            EN_PREPARATION,
            PRETE,
            PAYEE,
            EN_LIVRAISON,
            LIVREE,
            REFUSEE
        }

        public enum StatutAvis
        {
            EN_ATTENTE,
            PUBLIE,
            REJETE
        }

        public enum StatutReclamation
        {
            ENVOYEE,
            EN_COURS,
            RESOLUE,
            REJETEE
        }
    }
}
