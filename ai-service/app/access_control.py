"""
access_control.py
Filet de sécurité applicatif : force un filtre WHERE UtilisateurId = ...
sur les requêtes touchant HistoriqueVentes quand l'utilisateur a le rôle CLIENT,
pour qu'il ne puisse jamais voir les commandes d'un autre client.

Limite connue : approche par manipulation de texte, pas une vraie politique de
sécurité au niveau base de données. Pour une version production, préférer une
Row-Level Security SQL Server. Suffisant et défendable pour un projet de stage.
"""

import re


def appliquer_filtre_client(sql: str, utilisateur_id: int) -> str:
    if "HistoriqueVentes" not in sql:
        return sql

    alias_match = re.search(
        r"HistoriqueVentes\s+(?:AS\s+)?([a-zA-Z_][a-zA-Z0-9_]*)", sql, re.IGNORECASE
    )
    alias = alias_match.group(1) if alias_match else "HistoriqueVentes"

    filtre = f"{alias}.UtilisateurId = {utilisateur_id}"

    if re.search(r"\bWHERE\b", sql, re.IGNORECASE):
        return re.sub(r"\bWHERE\b", f"WHERE {filtre} AND", sql, count=1, flags=re.IGNORECASE)

    match_clause_suivante = re.search(r"\b(ORDER BY|GROUP BY)\b", sql, re.IGNORECASE)
    if match_clause_suivante:
        index = match_clause_suivante.start()
        return sql[:index] + f"WHERE {filtre} " + sql[index:]

    return sql.rstrip(";") + f" WHERE {filtre}"
