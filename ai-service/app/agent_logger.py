"""
agent_logger.py
DÉPRÉCIÉ — remplacé par app/core/trace_logger.py (v2).

Ce module est conservé pour ne pas casser les éventuels imports existants.
Il réexporte les fonctions de trace_logger.py.
"""

import warnings
warnings.warn(
    "agent_logger.py est déprécié. Utiliser 'from app.core.trace_logger import TraceLogger' à la place.",
    DeprecationWarning,
    stacklevel=2,
)

# Réexport pour rétrocompatibilité
from app.core.trace_logger import (  # noqa: F401, E402
    log_nouvelle_requete,
    log_agent_orchestrateur,
    log_agent_nl2sql,
    log_sql_validator,
    log_sql_executor,
    log_agent_chart_ui,
    log_memoire_et_fin,
)
