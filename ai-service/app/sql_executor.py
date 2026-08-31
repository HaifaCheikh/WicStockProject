"""
sql_executor.py
Exécute une requête SQL déjà validée (sql_validator) sur la vraie base WicStockDb.
Ajoute automatiquement une limite de lignes (TOP) par sécurité si absente.
"""

import os
import re
import pyodbc

CONNECTION_STRING = os.getenv(
    "SQLSERVER_CONNECTION_STRING",
    "Driver={ODBC Driver 17 for SQL Server};"
    "Server=(localdb)\\mssqllocaldb;"
    "Database=WicStockDb;"
    "Trusted_Connection=yes;",
)

MAX_LIGNES = 200


def _ajouter_limite(sql: str) -> str:
    upper = sql.upper()
    if re.search(r"\bTOP\s+\d+", upper):
        return sql
    if upper.lstrip().startswith("SELECT DISTINCT"):
        return sql.replace("SELECT DISTINCT", f"SELECT DISTINCT TOP {MAX_LIGNES}", 1)
    return sql.replace("SELECT", f"SELECT TOP {MAX_LIGNES}", 1)


def _nettoyer_utf8(val):
    """Corrige les chaînes UTF-8 mal décodées ou doublement encodées (ex: AcceptÃ©e -> Acceptée)."""
    if isinstance(val, str):
        if "Ã" in val or "Â" in val:
            try:
                return val.encode("iso-8859-1").decode("utf-8")
            except Exception:
                pass
        if "\ufffd" in val:
            try:
                cleaned = val.encode("raw_unicode_escape").decode("cp1252")
                if "\ufffd" not in cleaned:
                    return cleaned
            except Exception:
                pass
    return val


def executer_requete(sql: str) -> list[dict]:
    """Exécute la requête (déjà validée en amont) et retourne les résultats sous forme de liste de dicts."""
    sql_limitee = _ajouter_limite(sql)

    conn = pyodbc.connect(CONNECTION_STRING)
    try:
        try:
            conn.setdecoding(pyodbc.SQL_CHAR, encoding="latin1")
            conn.setdecoding(pyodbc.SQL_WCHAR, encoding="utf-16le")
            conn.setencoding(encoding="utf-8")
        except Exception:
            pass

        cursor = conn.cursor()
        cursor.execute(sql_limitee)
        colonnes = [col[0] for col in cursor.description]
        lignes = cursor.fetchall()
        
        resultats = []
        for ligne in lignes:
            row_dict = {_nettoyer_utf8(col): _nettoyer_utf8(val) for col, val in zip(colonnes, ligne)}
            resultats.append(row_dict)
        return resultats
    finally:
        conn.close()
