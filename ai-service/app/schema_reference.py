"""
schema_reference.py
Source de vérité du schéma réel de WicStockDb, utilisée pour valider
que les requêtes SQL générées par le LLM n'inventent aucune table/colonne.
"""

SCHEMA: dict[str, list[str]] = {
    "Produits": [
        "Id", "Reference", "Nom", "TypeTissu", "Categorie",
        "CycleDeVie", "PrixUnitaire", "DateCreation", "ImageUrl", "DisponibleSurCommande", "EstArchive",
    ],
    "Stocks": [
        "Id", "QuantiteActuelle", "SeuilAlerte", "SeuilSurstock", "Emplacement",
        "DateMiseAJour", "ProduitId",
    ],
    "MouvementsStock": [
        "Id", "Type", "Quantite", "Date", "Motif", "StockId",
    ],
    "HistoriqueVentes": [
        "Id", "DateVente", "QuantiteVendue", "PrixUnitaire",
        "StatutCommande", "Statut", "DatePaiement", "DateLivraison",
        "DateConfirmation", "PaymentIntentId", "EstSurCommande",
        "ProduitId", "UtilisateurId",
    ],
    "HistoriqueProductions": [
        "Id", "DateProduction", "QuantiteProduite", "ProduitId",
    ],
    "Alertes": [
        "Id", "TypeRisque", "DateDetection", "Statut",
        "NiveauCriticite", "ProduitId", "UtilisateurId",
    ],
    "PrevisionsEtatProduit": [
        "Id", "ProduitId", "TypeRisquePredit", "ScoreRisque",
        "QuantitePredite", "HorizonJours", "DateCalcul",
    ],
    "ActionsRecommandees": [
        "Id", "ProduitId", "TypeAction", "TexteGenere", "DateGeneration",
        "Source", "PrevisionEtatProduitId", "UtilisateurId",
    ],
    "Utilisateurs": [
        "Id", "Nom", "Prenom", "Email", "Telephone",
        "MotDePasseHash", "Role",
    ],
    "Avis": [
        "Id", "CommandeId", "ProduitId", "ClientId", "Note",
        "Commentaire", "DateCreation", "Statut", "EstMasque",
    ],
    "Reclamations": [
        "Id", "CommandeId", "ProduitId", "ClientId", "Motif",
        "Description", "PhotosUrls", "DateCreation", "Statut",
        "ReponseAdmin", "DateReponse",
    ],
}

# Tables interdites d'accès pour un rôle CLIENT (données internes sensibles)
TABLES_INTERDITES_CLIENT = {
    "Utilisateurs", "Alertes", "PrevisionsEtatProduit",
    "ActionsRecommandees", "MouvementsStock",
}