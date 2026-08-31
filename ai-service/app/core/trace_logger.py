"""
trace_logger.py
Module de traçabilité terminal centralisé pour WicStock AI Multi-Agents (v2).

Format exact :
========== WICSTOCK AI ==========
USER: <message>

[ORCHESTRATOR]
State: IDLE
Intent: DATA_ANALYSIS
Agent → NL2SQL

[NL2SQL]
RAG search...
Context retrieved: 4 documents (similarity > 0.82)

[OLLAMA]
Model: qwen3:1.7b
Prompt tokens: 612

[SQL]
Generated: SELECT TOP 5 ...

[VALIDATOR]
SELECT: PASS
RBAC: PASS (role=Gestionnaire)
Tables: PASS (Ventes, Produits)

[EXECUTOR]
Rows: 5
Execution time: 84ms

[PREFERENCE AGENT]
chart_eligible: true
State → AWAITING_FORMAT_CHOICE

[RESPONSE]
Text: "..."
Options: [Texte simple] [Graphique]
==================================

Usage :
    from app.core.trace_logger import TraceLogger
    tl = TraceLogger()
    tl.start(session_id="abc", role="Gestionnaire", user_message="...")
    tl.orchestrator(state="IDLE", intent="DATA_ANALYSIS", next_agent="NL2SQL")
    tl.nl2sql(docs_retrieved=4, similarity_threshold=0.82)
    tl.ollama(model_name="qwen3:1.7b", prompt_tokens=612)
    tl.sql(generated_sql="SELECT ...")
    tl.validator(select_pass=True, rbac_pass=True, role="Gestionnaire", tables=["Ventes","Produits"])
    tl.executor(rows=5, execution_ms=84)
    tl.preference_agent(chart_eligible=True, next_state="AWAITING_FORMAT_CHOICE")
    tl.response(text="...", options=["Texte simple", "Graphique"])
    tl.session_event(message="Timeout: session reset to IDLE after 15min inactivity")
    tl.end()
"""

import sys
import time
from typing import Optional, List

# Encodage UTF-8 sous Windows
if sys.platform == "win32":
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass

_WIDTH = 50
_SEP_MAJOR = "=" * _WIDTH
_SEP_MINOR = "-" * _WIDTH


def _print(*args, **kwargs):
    """Wrapper print isolé pour faciliter les tests unitaires (capfd)."""
    print(*args, **kwargs, flush=True)


class TraceLogger:
    """
    Logger de traçabilité structuré pour WicStock AI.
    Une instance par requête — instancier en début de pipeline, appeler .end() à la fin.
    """

    def __init__(self):
        self._t0: float = time.time()

    # ------------------------------------------------------------------
    # Bloc d'en-tête
    # ------------------------------------------------------------------

    def start(self, session_id: str, role: str, user_message: str) -> None:
        """Affiche le bloc d'en-tête de la requête."""
        _print(f"\n{_SEP_MAJOR}")
        _print("         WICSTOCK AI — NEW REQUEST")
        _print(_SEP_MAJOR)
        _print("\n[SESSION]")
        _print(f"  session_id : {session_id}")
        _print(f"  role       : {role}")
        _print(f"\nUSER:\n  {user_message}")

    # ------------------------------------------------------------------
    # Agents
    # ------------------------------------------------------------------

    def orchestrator(
        self,
        state: str,
        intent: str,
        next_agent: str,
    ) -> None:
        """Log le passage dans l'Orchestrateur."""
        _print(f"\n{_SEP_MINOR}")
        _print("[ORCHESTRATOR]")
        _print(f"  State  : {state}")
        _print(f"  Intent : {intent}")
        _print(f"  Agent  → {next_agent}")

    def nl2sql(
        self,
        docs_retrieved: int,
        similarity_threshold: float,
        source: str = "CATALOGUE_CERTIFIE",
    ) -> None:
        """Log le passage dans l'agent NL2SQL / RAG."""
        _print(f"\n{_SEP_MINOR}")
        _print("[NL2SQL]")
        _print(f"  RAG search...")
        _print(f"  Source   : {source}")
        _print(
            f"  Context retrieved : {docs_retrieved} document(s) "
            f"(similarity > {similarity_threshold:.2f})"
        )

    def ollama(self, model_name: str, prompt_tokens: Optional[int] = None) -> None:
        """Log l'appel au LLM Ollama."""
        _print(f"\n{_SEP_MINOR}")
        _print("[OLLAMA]")
        _print(f"  Model : {model_name}")
        if prompt_tokens is not None:
            _print(f"  Prompt tokens : {prompt_tokens}")

    def sql(self, generated_sql: str) -> None:
        """Log la requête SQL générée/sélectionnée."""
        _print(f"\n{_SEP_MINOR}")
        _print("[SQL]")
        # Affichage compact : max 120 chars sur une ligne
        sql_display = generated_sql.replace("\n", " ").strip()
        if len(sql_display) > 120:
            sql_display = sql_display[:117] + "..."
        _print(f"  Generated : {sql_display}")

    def validator(
        self,
        select_pass: bool,
        rbac_pass: bool,
        role: str = "",
        tables: Optional[List[str]] = None,
        security_pass: Optional[bool] = None,
    ) -> None:
        """Log la validation RBAC et sécurité SQL."""
        if security_pass is None:
            security_pass = select_pass and rbac_pass

        tables_str = (", ".join(tables) if tables else "N/A")
        role_str = f" (role={role})" if role else ""

        _print(f"\n{_SEP_MINOR}")
        _print("[VALIDATOR]")
        _print(f"  SELECT   : {'PASS' if select_pass else 'FAIL'}")
        _print(f"  RBAC     : {'PASS' if rbac_pass else 'FAIL'}{role_str}")
        _print(f"  Tables   : {'PASS' if security_pass else 'FAIL'} ({tables_str})")
        overall = select_pass and rbac_pass and security_pass
        _print(f"  Result   : {'VALID ✓' if overall else 'REJECTED ✗'}")

    def executor(self, rows: int, execution_ms: Optional[float] = None) -> None:
        """Log l'exécution SQL et le nombre de lignes retournées."""
        _print(f"\n{_SEP_MINOR}")
        _print("[EXECUTOR]")
        _print(f"  Rows : {rows}")
        if execution_ms is not None:
            _print(f"  Execution time : {execution_ms:.0f}ms")

    def preference_agent(
        self,
        chart_eligible: bool,
        next_state: str,
        proposed_options: Optional[List[str]] = None,
    ) -> None:
        """Log le passage dans le Preference Agent."""
        _print(f"\n{_SEP_MINOR}")
        _print("[PREFERENCE AGENT]")
        _print(f"  chart_eligible : {str(chart_eligible).lower()}")
        _print(f"  State → {next_state}")
        if proposed_options:
            opts_str = "  ".join(f"[{o}]" for o in proposed_options)
            _print(f"  Options : {opts_str}")

    def surstock(
        self,
        nom_produit: str,
        succes: bool,
        nb_actions: int,
    ) -> None:
        """Log le passage dans l'agent Surstock."""
        _print(f"\n{_SEP_MINOR}")
        _print("[SURSTOCK AGENT]")
        _print(f"  Produit   : {nom_produit}")
        _print(f"  Diagnostic: {'OK ✓' if succes else 'FALLBACK (LLM indisponible)'}")
        _print(f"  Actions   : {nb_actions} proposée(s)")

    def session_event(self, message: str) -> None:
        """Log un événement de session (TTL reset, expiration, etc.)."""
        _print(f"\n{_SEP_MINOR}")
        _print("[SESSION]")
        _print(f"  {message}")

    def response(
        self,
        text: str,
        options: Optional[List[str]] = None,
        chart_type: Optional[str] = None,
    ) -> None:
        """Log la réponse finale envoyée au frontend."""
        _print(f"\n{_SEP_MINOR}")
        _print("[RESPONSE]")
        # Tronquer le texte pour lisibilité
        text_display = text[:100] + "..." if len(text) > 100 else text
        _print(f"  Text : \"{text_display}\"")
        if options:
            opts_str = "  ".join(f"[{o}]" for o in options)
            _print(f"  Options : {opts_str}")
        if chart_type:
            _print(f"  Chart  : {chart_type.upper()}")

    def custom_block(self, tag: str, lines: List[str]) -> None:
        """Bloc de log personnalisé pour les agents spécialisés (ex: Surstock)."""
        _print(f"\n{_SEP_MINOR}")
        _print(f"[{tag.upper()}]")
        for line in lines:
            _print(f"  {line}")

    # ------------------------------------------------------------------
    # Bloc de clôture
    # ------------------------------------------------------------------

    def end(self) -> None:
        """Affiche le bloc de fin de requête avec le temps total."""
        elapsed_ms = (time.time() - self._t0) * 1000
        _print(f"\n{_SEP_MINOR}")
        _print("[COMPLETED]")
        _print(f"  Total pipeline time : {elapsed_ms:.0f}ms")
        _print(_SEP_MAJOR + "\n")


# ---------------------------------------------------------------------------
# Fonctions utilitaires (rétrocompatibilité avec agent_logger.py)
# Gardées pour éviter les imports cassés si certains modules les appellent encore.
# ---------------------------------------------------------------------------

def log_nouvelle_requete(session_id: str, role: str, question: str):
    """Rétrocompat agent_logger.py — utiliser TraceLogger à la place."""
    tl = TraceLogger()
    tl.start(session_id=session_id, role=role, user_message=question)


def log_agent_orchestrateur(intent: str, source: str, role: str):
    _print(f"\n{_SEP_MINOR}")
    _print(f"[ORCHESTRATOR] Intent={intent} | Agent→{source} | role={role}")


def log_agent_nl2sql(source_type: str, sql_genere: str, score: float = 1.0):
    _print(f"\n{_SEP_MINOR}")
    _print(f"[NL2SQL] Source={source_type} | Score={score:.2f}")
    _print(f"[SQL] {(sql_genere or 'NONE')[:100]}")


def log_sql_validator(rbac_pass: bool = True, security_pass: bool = True):
    _print(f"\n{_SEP_MINOR}")
    _print(f"[VALIDATOR] RBAC={'PASS' if rbac_pass else 'FAIL'} | Security={'PASS' if security_pass else 'FAIL'}")


def log_sql_executor(rows_count: int):
    _print(f"\n{_SEP_MINOR}")
    _print(f"[EXECUTOR] Rows={rows_count}")


def log_agent_chart_ui(chart_type, labels_count: int, colors=None):
    _print(f"\n{_SEP_MINOR}")
    if chart_type:
        _print(f"[CHART UI] type={chart_type.upper()} | labels={labels_count}")
    else:
        _print("[CHART UI] Waiting user confirmation...")


def log_memoire_et_fin():
    _print(f"\n{_SEP_MINOR}")
    _print("[MEMORY] Session state updated.")
    _print(_SEP_MAJOR + "\n")
