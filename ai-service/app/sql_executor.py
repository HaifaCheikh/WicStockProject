"""
sql_executor.py
Exécute une requête SQL déjà validée (sql_validator) sur la base WicStock.
Supporte PostgreSQL (psycopg2) et SQL Server (pyodbc).
"""

import os
import re

DATABASE_URL = (
    os.getenv("DATABASE_URL")
    or os.getenv("ConnectionStrings__DefaultConnection")
    or os.getenv("SQLSERVER_CONNECTION_STRING")
    or "postgresql://postgres:VotreMotDePasseFort123!@localhost:5432/WicStockDb"
)

MAX_LIGNES = 200


def _ajouter_limite(sql: str) -> str:
    upper = sql.upper()
    if "LIMIT" in upper or "TOP" in upper:
        return sql
    return sql + f" LIMIT {MAX_LIGNES}"


def _nettoyer_utf8(val):
    if isinstance(val, str):
        if "Ã" in val or "Â" in val:
            try:
                return val.encode("iso-8859-1").decode("utf-8")
            except Exception:
                pass
    return val


TABLES_EXACTES = [
    "Produits", "Stocks", "MouvementsStock", "HistoriqueVentes",
    "HistoriqueProductions", "Alertes", "PrevisionsEtatProduit",
    "ActionsRecommandees", "Utilisateurs", "Avis", "Reclamations",
    "AttributsValeurs", "VariantesProduit", "LigneCommandes"
]

COLONNES_EXACTES = [
    "ProduitId", "QuantiteActuelle", "SeuilAlerte", "SeuilSurstock", "Emplacement",
    "DateMiseAJour", "EstArchive", "DisponibleSurCommande", "PrixUnitaire", "DateCreation",
    "TypeTissu", "Categorie", "CycleDeVie", "DateVente", "QuantiteVendue", "StatutCommande",
    "DatePaiement", "DateLivraison", "DateConfirmation", "PaymentIntentId", "EstSurCommande",
    "UtilisateurId", "DateProduction", "QuantiteProduite", "TypeRisque", "DateDetection",
    "NiveauCriticite", "TypeRisquePredit", "ScoreRisque", "QuantitePredite", "HorizonJours",
    "DateCalcul", "TypeAction", "TexteGenere", "DateGeneration", "PrevisionEtatProduitId",
    "CommandeId", "ClientId", "Commentaire", "EstMasque", "PhotosUrls", "ReponseAdmin",
    "DateReponse", "Reference", "Nom", "Role", "Email", "Telephone", "MotDePasseHash", "Id"
]


# Colonnes booléennes connues dans le schéma WicStock
COLONNES_BOOL = [
    "EstArchive", "DisponibleSurCommande", "EstMasque", "EstSurCommande", "DisponibleSurC"
]


def _adapter_sql_postgres(sql: str) -> str:
    """Adapte la requête SQL générée pour PostgreSQL (guillemets pour les tables et colonnes case-sensitive, types booléens)."""
    sql_adapte = sql
    for tbl in TABLES_EXACTES:
        sql_adapte = re.sub(rf'(?<!")\b{tbl}\b(?!")', f'"{tbl}"', sql_adapte, flags=re.IGNORECASE)
    for col in COLONNES_EXACTES:
        sql_adapte = re.sub(rf'(?<!")\b{col}\b(?!")', f'"{col}"', sql_adapte, flags=re.IGNORECASE)
    sql_adapte = re.sub(r'\bGETDATE\(\)', 'NOW()', sql_adapte, flags=re.IGNORECASE)
    # Convertir = 0 / = 1 en booléen UNIQUEMENT pour les colonnes booléennes connues
    # Utiliser un lookbehind négatif pour ne pas toucher aux comparaisons <= 0, >= 0, <> 0
    for bool_col in COLONNES_BOOL:
        quoted = f'"{bool_col}"'
        # Remplace: "ColBool" = 0  -->  "ColBool" = false
        sql_adapte = re.sub(
            rf'({re.escape(quoted)})\s*(?<![<>!])=\s*0\b',
            r'\1 = false',
            sql_adapte
        )
        sql_adapte = re.sub(
            rf'({re.escape(quoted)})\s*(?<![<>!])=\s*1\b',
            r'\1 = true',
            sql_adapte
        )
    return sql_adapte


def executer_requete(sql: str) -> list:
    """Exécute la requête SQL et retourne les résultats sous forme de liste de dicts."""
    db_url = DATABASE_URL or "postgresql://postgres:VotreMotDePasseFort123!@localhost:5432/WicStockDb"

    if db_url.startswith("postgresql://") or db_url.startswith("postgres://") or db_url.startswith("Host=") or db_url.startswith("host="):
        if db_url.startswith("Host=") or db_url.startswith("host="):
            db_url = "postgresql://postgres:VotreMotDePasseFort123!@localhost:5432/WicStockDb"
        
        sql_pg = _ajouter_limite(_adapter_sql_postgres(sql))
        print(f"[SQL EXECUTOR] Connexion PostgreSQL, SQL: {sql_pg[:100]}...")
        try:
            import psycopg2
            import psycopg2.extras
            conn = psycopg2.connect(db_url)
            try:
                with conn.cursor(cursor_factory=psycopg2.extras.RealDictCursor) as cur:
                    cur.execute(sql_pg)
                    rows = cur.fetchall()
                    print(f"[SQL EXECUTOR] {len(rows)} ligne(s) retournée(s).")
                    return [{_nettoyer_utf8(k): _nettoyer_utf8(v) for k, v in row.items()} for row in rows]
            finally:
                conn.close()
        except Exception as ex:
            print(f"[SQL EXECUTOR POSTGRES ERROR] {ex}")
            return []

    else:
        try:
            import pyodbc
            conn_str = db_url or "Driver={ODBC Driver 17 for SQL Server};Server=(localdb)\\mssqllocaldb;Database=WicStockDb;Trusted_Connection=yes;"
            conn = pyodbc.connect(conn_str)
            try:
                cursor = conn.cursor()
                cursor.execute(_ajouter_limite(sql))
                colonnes = [col[0] for col in cursor.description]
                lignes = cursor.fetchall()
                resultats = []
                for ligne in lignes:
                    row_dict = {_nettoyer_utf8(col): _nettoyer_utf8(val) for col, val in zip(colonnes, ligne)}
                    resultats.append(row_dict)
                return resultats
            except Exception as ex:
                print(f"[SQL EXECUTOR ODBC WARNING] {ex}")
                return []
            finally:
                conn.close()
        except Exception as ex:
            print(f"[SQL EXECUTOR WARNING] {ex}")
            return []
