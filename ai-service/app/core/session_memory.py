"""
session_memory.py
Agent Mémoire / Session — WicStock AI Multi-Agents (v2).

Responsabilités :
- Stocker et récupérer l'état conversationnel (state machine) par session_id
- Appliquer le TTL de 15 minutes (reset à IDLE + invalidation du dataset)
- Mémoriser les préférences graphiques par TYPE de question (persistantes, non affectées par TTL)
- Fournir la suggestion "Comme la dernière fois ?"
- Documenter la gestion de concurrence multi-onglets

Note sur la concurrence multi-onglets :
    Le session_id DOIT être généré côté frontend à l'ouverture du composant chat
    (ex: Guid.NewGuid().ToString() en Blazor), pas déduit de l'utilisateur.
    Ainsi, deux onglets du même utilisateur ont des sessions distinctes et ne
    se polluent pas mutuellement.
"""

import time
import re
from typing import Any, Dict, List, Optional

from app.models.agent_schemas import ConversationState, SessionStateDto, ChartDto

# TTL de session en secondes (15 minutes d'inactivité → reset à IDLE)
SESSION_TTL_SECONDS: int = 15 * 60


# ---------------------------------------------------------------------------
# Registre en mémoire (dict process-local ; suffisant pour un worker uvicorn)
# Pour un déploiement multi-worker, remplacer par Redis ou une base partagée.
# ---------------------------------------------------------------------------
_sessions: Dict[str, SessionStateDto] = {}


# ---------------------------------------------------------------------------
# API publique
# ---------------------------------------------------------------------------

def get_or_create(
    session_id: str,
    role: str = "CLIENT",
    utilisateur_id: Optional[int] = None,
    produit_id: Optional[int] = None,
) -> SessionStateDto:
    """
    Retourne la session existante (ou en crée une nouvelle).
    Applique le TTL AVANT de retourner l'état : si la session est expirée,
    l'état est remis à IDLE et le dataset invalidé (les préférences sont conservées).
    """
    now = time.time()

    if session_id not in _sessions:
        _sessions[session_id] = SessionStateDto(
            session_id=session_id,
            role=role,
            utilisateur_id=utilisateur_id,
            produit_id=produit_id,
            last_activity_ts=now,
        )
    else:
        session = _sessions[session_id]
        # Mise à jour des métadonnées de connexion
        session.role = role
        if utilisateur_id is not None:
            session.utilisateur_id = utilisateur_id
        if produit_id is not None:
            session.produit_id = produit_id

        # Vérification TTL — AVANT d'exposer l'état courant
        _apply_ttl(session, now)

    return _sessions[session_id]


def save(session: SessionStateDto, update_ts: bool = True) -> None:
    """Persiste la session mise à jour.
    
    Args:
        update_ts: Si True (défaut), rafraîchit last_activity_ts.
                   Mettre False pour conserver un timestamp fictif (tests TTL).
    """
    if update_ts:
        session.last_activity_ts = time.time()
    _sessions[session.session_id] = session


def set_state(session_id: str, state: ConversationState) -> None:
    """Raccourci pour changer uniquement l'état de la state machine."""
    if session_id in _sessions:
        _sessions[session_id].state = state
        _sessions[session_id].last_activity_ts = time.time()


def reset_to_idle(session_id: str, reason: str = "manual") -> None:
    """
    Remet la session à IDLE et invalide le dataset en cache.
    Les préférences persistantes (preferences_par_type) sont conservées.
    """
    if session_id in _sessions:
        session = _sessions[session_id]
        session.state = ConversationState.IDLE
        session.derniere_question = None
        session.derniere_requete_sql = None
        session.derniers_resultats_db = None
        session.dernier_chart = None
        session.dernier_type_graphique = None
        session.derniers_titre = None
        session.chart_eligible = False
        session.last_activity_ts = time.time()


def delete(session_id: str) -> None:
    """Supprime complètement une session (reset mémoire)."""
    _sessions.pop(session_id, None)


# ---------------------------------------------------------------------------
# Gestion des préférences par type de question (persistantes)
# ---------------------------------------------------------------------------

def save_preference(
    session_id: str,
    question_type: str,
    chart_type: str,
    colors: Optional[List[str]] = None,
) -> None:
    """
    Mémorise la préférence graphique pour un type de question donné.
    Crée la session si elle n'existe pas encore.
    Exemple : save_preference("sess1", "top_produits", "donut", ["#8B5CF6", "#F59E0B"])
    """
    if session_id not in _sessions:
        _sessions[session_id] = SessionStateDto(session_id=session_id)
    prefs = _sessions[session_id].preferences_par_type
    prefs[question_type] = {
        "chart_type": chart_type,
        "colors": colors or [],
    }


def get_preference(
    session_id: str,
    question_type: str,
) -> Optional[Dict[str, Any]]:
    """
    Retourne la préférence sauvegardée pour ce type de question, ou None.
    Exemple de retour : {"chart_type": "donut", "colors": ["#8B5CF6", "#F59E0B"]}
    """
    if session_id not in _sessions:
        return None
    return _sessions[session_id].preferences_par_type.get(question_type)


def resolve_question_type(question: str) -> str:
    """
    Déduit un "type de question" normalisé depuis le texte utilisateur.
    Utilisé comme clé de mémorisation des préférences.
    Exemples :
      "Quels sont mes 5 produits les plus vendus ?" → "top_produits"
      "Répartition des commandes par statut"        → "repartition_commandes"
      "Chiffre d'affaires des 30 derniers jours"    → "chiffre_affaires"
    """
    q = question.lower()

    if re.search(r"(plus\s+vendu|top\s+produit|meilleure?\s+vente|top\s+\d+)", q):
        return "top_produits"
    if re.search(r"(surstock|sur.stock)", q):
        return "surstock"
    if re.search(r"(rupture|rupture.de.stock)", q):
        return "rupture_stock"
    if re.search(r"(chiffre.d.affaire|revenu|ca\b|recette)", q):
        return "chiffre_affaires"
    if re.search(r"(répartition|repartition|statut|par.statut|par.catégorie|par.categorie)", q):
        return "repartition"
    if re.search(r"(commande|commandes)", q):
        return "commandes"
    if re.search(r"(mouvement|historique|évolution|evolution|tendance)", q):
        return "evolution_stock"
    if re.search(r"(stock|inventaire|quantité|quantite)", q):
        return "stock"

    # Fallback : clé normalisée depuis les 3 premiers mots
    mots = re.findall(r"\b\w{3,}\b", q)[:3]
    return "_".join(mots) if mots else "general"


def build_last_time_options(
    session_id: str,
    question_type: str,
) -> Optional[Dict[str, Any]]:
    """
    Si une préférence existe pour ce type de question, retourne un dict décrivant
    la suggestion "Comme la dernière fois ?" prête à injecter dans les options.

    Retourne None si aucune préférence mémorisée.
    """
    pref = get_preference(session_id, question_type)
    if not pref:
        return None

    chart_type = pref.get("chart_type", "graphique")
    colors = pref.get("colors", [])
    colors_str = " + ".join(colors[:2]) if colors else ""
    label = f"⭐ Comme la dernière fois ({chart_type.capitalize()}"
    if colors_str:
        label += f", {colors_str}"
    label += ")"

    return {
        "chart_type": chart_type,
        "colors": colors,
        "label": label,
        "value": f"__last_time__{chart_type}",
    }


# ---------------------------------------------------------------------------
# Interne — application du TTL
# ---------------------------------------------------------------------------

def _apply_ttl(session: SessionStateDto, now: float) -> bool:
    """
    Applique le TTL si la session est inactive depuis plus de SESSION_TTL_SECONDS.
    Retourne True si un reset a été effectué.
    """
    if session.state == ConversationState.IDLE:
        # Pas besoin de reset si déjà IDLE
        return False

    inactive_seconds = now - session.last_activity_ts
    if inactive_seconds >= SESSION_TTL_SECONDS:
        _do_ttl_reset(session, inactive_seconds)
        return True

    return False


def _do_ttl_reset(session: SessionStateDto, inactive_seconds: float) -> None:
    """Effectue le reset TTL en loggant l'événement."""
    from app.core.trace_logger import TraceLogger
    tl = TraceLogger()
    minutes = inactive_seconds / 60
    tl.session_event(
        f"Timeout: session '{session.session_id}' reset to IDLE "
        f"after {minutes:.1f}min inactivity (TTL={SESSION_TTL_SECONDS}s). "
        "Dataset cache invalidated. Preferences preserved."
    )

    session.state = ConversationState.IDLE
    session.derniere_question = None
    session.derniere_requete_sql = None
    session.derniers_resultats_db = None
    session.dernier_chart = None
    session.dernier_type_graphique = None
    session.derniers_titre = None
    session.chart_eligible = False
    # NB : preferences_par_type NON effacé (persistant)
