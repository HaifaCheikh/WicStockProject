"""
sql_executor.py
Exécute une requête SQL déjà validée (sql_validator) sur la base WicStock.
Supporte PostgreSQL (psycopg2) et SQL Server (pyodbc).
"""

import os
import re

DATABASE_URL = os.getenv("DATABASE_URL") or os.getenv("ConnectionStrings__DefaultConnection") or os.getenv("SQLSERVER_CONNECTION_STRING")

MAX_LIGNES = 200


def _ajouter_limite(sql: str) -> str:
    upper = sql.upper()
    if "LIMIT" in upper or "TOP" in upper:
        return sql
    if upper.lstrip().startswith("SELECT DISTINCT"):
        return sql + f" LIMIT {MAX_LIGNES}"
    return sql + f" LIMIT {MAX_LIGNES}"


def _nettoyer_utf8(val):
    if isinstance(val, str):
        if "Ã" in val or "Â" in val:
            try:
                return val.encode("iso-8859-1").decode("utf-8")
            except Exception:
                pass
    return val


def executer_requete(sql: str) -> list[dict]:
    """Exécute la requête SQL et retourne les résultats sous forme de liste de dicts."""
    db_url = DATABASE_URL or ""

    if db_url.startswith("postgresql://") or db_url.startswith("postgres://"):
        try:
            import psycopg2
            import psycopg2.extras
            url = db_url.replace("postgres://", "postgresql://", 1)
            conn = psycopg2.connect(url)
            try:
                with conn.cursor(cursor_factory=psycopg2.extras.RealDictCursor) as cur:
                    cur.execute(_ajouter_limite(sql))
                    rows = cur.fetchall()
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
            finally:
                conn.close()
            except Exception as ex:
                print(f"[SQL EXECUTOR ODBC WARNING] {ex}")
                return []
        except Exception as ex:
            print(f"[SQL EXECUTOR WARNING] {ex}")
            return []
