"""
sql_validator.py
Vérifie, AVANT exécution, que la requête générée par le LLM :
- est bien un SELECT (rien d'autre)
- n'utilise que des tables/colonnes réellement existantes
Supporte la syntaxe PostgreSQL avec guillemets doubles ("TableName").
"""

import re
from app.schema_reference import SCHEMA, TABLES_INTERDITES_CLIENT

FORBIDDEN_KEYWORDS = [
    "INSERT", "UPDATE", "DELETE", "DROP", "ALTER",
    "TRUNCATE", "EXEC", "EXECUTE", "MERGE", "CREATE",
    "GRANT", "REVOKE",
]


class ValidationError(Exception):
    pass


def _strip_quotes(name: str) -> str:
    """Supprime les guillemets doubles PostgreSQL autour d'un identifiant."""
    return name.strip('"').strip("'").strip("`")


def extraire_alias_tables(sql: str) -> dict[str, str]:
    """
    Construit un dictionnaire {alias: nom_table} à partir des clauses FROM/JOIN.
    Supporte les identifiants entre guillemets doubles (PostgreSQL).
    """
    alias_map: dict[str, str] = {}
    # Reconnaît: FROM "TableName" alias  ou  FROM TableName alias
    pattern = re.compile(
        r'(?:FROM|JOIN)\s+"?([a-zA-Z_][a-zA-Z0-9_]*)"?\s+(?:AS\s+)?"?([a-zA-Z_][a-zA-Z0-9_]*)"?',
        re.IGNORECASE,
    )
    for match in pattern.finditer(sql):
        table = _strip_quotes(match.group(1))
        alias_raw = match.group(2) or ""
        alias = _strip_quotes(alias_raw)
        alias_map[table] = table
        if alias and alias.upper() not in ("WHERE", "ON", "GROUP", "ORDER", "LEFT", "RIGHT", "INNER", "OUTER"):
            alias_map[alias] = table
    return alias_map


def valider_requete_sql(sql: str, role: str | None = None) -> str:
    """
    Valide la requête et retourne la version nettoyée si tout est correct.
    Lève ValidationError avec un message explicite sinon.
    """
    sql_propre = sql.strip().rstrip(";")
    upper = sql_propre.upper()

    if not upper.lstrip().startswith("SELECT"):
        raise ValidationError("Seules les requêtes SELECT sont autorisées.")

    for mot_interdit in FORBIDDEN_KEYWORDS:
        if re.search(rf"\b{mot_interdit}\b", upper):
            raise ValidationError(f"Mot-clé interdit détecté dans la requête : {mot_interdit}")

    alias_map = extraire_alias_tables(sql_propre)

    for alias, table in alias_map.items():
        if table not in SCHEMA:
            # Tentative sans casse (PostgreSQL est case-insensitive sur les identifiants non quotés)
            match_insensitive = next((k for k in SCHEMA if k.lower() == table.lower()), None)
            if match_insensitive:
                alias_map[alias] = match_insensitive
                table = match_insensitive
            else:
                raise ValidationError(
                    f"Table inconnue : '{table}'. Tables autorisées : {', '.join(SCHEMA.keys())}"
                )
        if role == "CLIENT" and table in TABLES_INTERDITES_CLIENT:
            raise ValidationError(
                f"Accès refusé : la table '{table}' n'est pas accessible pour le rôle CLIENT."
            )

    # Validation des colonnes - ne valider que les colonnes non-quotées pour éviter les faux positifs
    colonnes_utilisees = re.findall(
        r'\b([a-zA-Z_][a-zA-Z0-9_]*)\.([a-zA-Z_][a-zA-Z0-9_]*)\b', sql_propre
    )
    for alias, colonne in colonnes_utilisees:
        table = alias_map.get(alias)
        if table is None:
            continue  # alias non résolu (ex: fonction SQL agrégée), on ignore
        colonnes_valides = SCHEMA.get(table, [])
        if colonne not in colonnes_valides:
            # Tentative case-insensitive
            if not any(c.lower() == colonne.lower() for c in colonnes_valides):
                raise ValidationError(
                    f"Colonne inexistante : '{alias}.{colonne}'. "
                    f"La table '{table}' possède : {', '.join(colonnes_valides)}"
                )

    return sql_propre
