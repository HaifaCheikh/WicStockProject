"""
sql_guard_agent.py
Agent technique fusionné (Validator + Executor) — WicStock AI Multi-Agents (v2).
Applique des règles de sécurité fixes puis exécute la requête. Point d'entrée unique
pour toute interaction avec la base de données.
"""

import re
import time
from typing import Any, Dict, List, Optional, Tuple

from app.sql_validator import valider_requete_sql, ValidationError
from app.sql_executor import executer_requete


class SQLGuardAgent:
    """
    Agent technique fusionné de sécurité et d'exécution SQL.
    Validations appliquées :
    - SELECT strict (aucun DML/DDL)
    - RBAC : tables interdites selon le rôle
    - Tables et colonnes existantes (depuis schema_reference.py)
    Exécution :
    - ODBC direct sur la base SQL Server
    - Mesure précise du temps d'exécution (ms)
    """

    def __init__(self, timeout_seconds: int = 30):
        self.timeout_seconds = timeout_seconds

    def validate_and_execute(
        self,
        sql: str,
        role: str = "CLIENT",
    ) -> Tuple[bool, Optional[str], Optional[List[Dict[str, Any]]], Optional[str], List[str], float]:
        """
        Valide puis exécute une requête SQL.

        Returns:
            (success, sql_valide, results, error_message, tables_used, execution_ms)
        """
        tables_used = self._extract_tables(sql)

        # 1. Validation de la requête
        try:
            sql_valide = valider_requete_sql(sql, role=role)
        except ValidationError as e:
            return False, None, None, str(e), tables_used, 0.0
        except Exception as e:
            return False, None, None, f"Erreur inattendue lors de la validation : {str(e)}", tables_used, 0.0

        # 2. Exécution de la requête validée
        t0 = time.perf_counter()
        try:
            results = executer_requete(sql_valide)
            execution_ms = (time.perf_counter() - t0) * 1000
            return True, sql_valide, results, None, tables_used, execution_ms
        except Exception as e:
            execution_ms = (time.perf_counter() - t0) * 1000
            error_msg = f"Erreur lors de l'exécution de la requête : {str(e)}"
            return False, sql_valide, None, error_msg, tables_used, execution_ms

    def _extract_tables(self, sql: str) -> List[str]:
        """Extrait les noms de tables depuis les clauses FROM/JOIN."""
        pattern = re.compile(
            r"(?:FROM|JOIN)\s+([a-zA-Z_][a-zA-Z0-9_]*)",
            re.IGNORECASE,
        )
        tables = []
        for m in pattern.finditer(sql):
            table = m.group(1)
            # Exclure les sous-requêtes ou alias SQL communs
            if table.upper() not in ("SELECT", "WHERE", "ON", "AS"):
                if table not in tables:
                    tables.append(table)
        return tables
