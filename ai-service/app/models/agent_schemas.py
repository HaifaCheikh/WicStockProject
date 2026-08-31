"""
agent_schemas.py
Modèles de données Pydantic pour l'architecture Multi-Agents WicStock AI (v2).

Contient :
- AgentIntent       : intentions détectées par l'Orchestrateur
- ConversationState : état de la state machine conversationnelle
- QuickOptionDto    : bouton cliquable renvoyé au frontend Blazor
- AssistantResponseDto : DTO unifié pour l'endpoint /chat
- ChartDto, ChartPreferences, SessionStateDto, AgentResponse (rétrocompat)
"""

from typing import Any, Optional, List, Dict
from pydantic import BaseModel, Field
from enum import Enum


# ---------------------------------------------------------------------------
# Intentions détectées par l'Orchestrateur
# ---------------------------------------------------------------------------

class AgentIntent(str, Enum):
    # Requête analytique → pipeline NL2SQL complet
    DATA_ANALYSIS = "DATA_ANALYSIS"
    # Anciens alias pour rétrocompatibilité interne
    SQL_ANALYSIS = "SQL_ANALYSIS"

    # Réponses à la state machine (quick replies cliqués)
    FORMAT_CHOICE = "FORMAT_CHOICE"          # "Texte simple" ou "Graphique"
    CHART_TYPE_CHOICE = "CHART_TYPE_CHOICE"  # "Donut", "Barres", etc.
    COLOR_CHOICE = "COLOR_CHOICE"            # "Palette A", "violet et doré", etc.

    # Agents spécialisés
    SURSTOCK_DIAGNOSTIC = "SURSTOCK_DIAGNOSTIC"
    CHART_CUSTOMIZATION = "CHART_CUSTOMIZATION"   # rétrocompat

    # Divers
    CHAT_GENERAL = "CHAT_GENERAL"
    GENERAL_CONVERSATION = "GENERAL_CONVERSATION"  # rétrocompat alias
    SESSION_EXPIRED = "SESSION_EXPIRED"


# ---------------------------------------------------------------------------
# State Machine conversationnelle
# ---------------------------------------------------------------------------

class ConversationState(str, Enum):
    IDLE = "IDLE"
    AWAITING_FORMAT_CHOICE = "AWAITING_FORMAT_CHOICE"
    AWAITING_CHART_TYPE = "AWAITING_CHART_TYPE"
    AWAITING_COLOR_CHOICE = "AWAITING_COLOR_CHOICE"


# ---------------------------------------------------------------------------
# DTO Quick Options (boutons dans la bulle de chat Blazor)
# ---------------------------------------------------------------------------

class QuickOptionDto(BaseModel):
    label: str = Field(..., description="Texte affiché sur le bouton, ex: '📊 Graphique'")
    value: str = Field(..., description="Valeur envoyée au backend au clic, ex: 'graphique'")
    is_free_text: bool = Field(
        default=False,
        description="Si true, un champ texte libre inline s'ouvre au clic (ex: 'Autre...', 'Personnaliser...')"
    )


# ---------------------------------------------------------------------------
# ChartDto et ChartPreferences (inchangés — rétrocompatibilité)
# ---------------------------------------------------------------------------

class ChartPreferences(BaseModel):
    type_graphique: Optional[str] = Field(None, description="bar, donut, pie, line, area")
    couleurs: Optional[List[str]] = Field(None, description="Liste de codes hexadécimaux ou noms de couleurs")
    palette: Optional[str] = Field(None, description="Nom de la palette (eco, moderne, pastel, sunset, ocean)")
    titre_personnalise: Optional[str] = None
    limite_lignes: Optional[int] = None
    trier_par_valeur: Optional[bool] = None


class ChartDto(BaseModel):
    type: str
    title: str
    labels: List[str]
    series: List[float]
    colors: Optional[List[str]] = None
    custom_palette: Optional[List[str]] = None
    unit: Optional[str] = None
    options: Optional[Dict[str, Any]] = None


# ---------------------------------------------------------------------------
# DTO unifié pour l'endpoint /chat (nouveau)
# ---------------------------------------------------------------------------

class AssistantResponseDto(BaseModel):
    """
    DTO de réponse unique pour l'endpoint /chat.
    Le frontend Blazor utilise ce DTO pour afficher :
    - du texte + éventuellement un graphique final (chart != null)
    - OU une liste de boutons quick replies (options != null, pending_state != null)
    """
    text: str = Field(..., description="Texte de la réponse (toujours présent)")
    chart: Optional[ChartDto] = Field(
        default=None,
        description="Graphique à afficher (null si en attente de choix utilisateur)"
    )
    pending_state: Optional[str] = Field(
        default=None,
        description="État courant de la state machine si on attend une réponse utilisateur, ex: 'AWAITING_FORMAT_CHOICE'"
    )
    options: Optional[List[QuickOptionDto]] = Field(
        default=None,
        description="Boutons quick replies à afficher. Non null uniquement quand pending_state est non null."
    )
    # Métadonnées utiles pour le debug / audit frontend
    intent: Optional[str] = Field(default=None, description="Intention détectée, ex: 'DATA_ANALYSIS'")
    sql_genere: Optional[str] = Field(default=None, description="SQL exécuté (pour debug)")
    agent_source: Optional[str] = Field(default=None, description="Nom de l'agent ayant produit la réponse")
    suggestions: Optional[List[str]] = Field(default=None, description="Suggestions proactives (legacy)")


# ---------------------------------------------------------------------------
# SessionStateDto — état complet d'une session (utilisé par session_memory)
# ---------------------------------------------------------------------------

class SessionStateDto(BaseModel):
    session_id: str
    state: ConversationState = ConversationState.IDLE
    last_activity_ts: float = 0.0
    role: str = "CLIENT"
    utilisateur_id: Optional[int] = None
    produit_id: Optional[int] = None

    # Données en cache (invalidées au TTL reset)
    derniere_question: Optional[str] = None
    derniere_requete_sql: Optional[str] = None
    derniers_resultats_db: Optional[List[Dict[str, Any]]] = None
    dernier_chart: Optional[ChartDto] = None
    dernier_type_graphique: Optional[str] = None
    derniers_titre: Optional[str] = None
    chart_eligible: bool = False

    # Préférences persistantes par "type de question" (non affectées par TTL)
    # ex: {"top_produits": {"chart_type": "donut", "colors": ["#8B5CF6", "#F59E0B"]}}
    preferences_par_type: Dict[str, Dict[str, Any]] = Field(default_factory=dict)


# ---------------------------------------------------------------------------
# AgentSessionState — alias rétrocompatibilité (utilisé par l'ancien orchestrator)
# ---------------------------------------------------------------------------

class AgentSessionState(BaseModel):
    """Rétrocompatibilité — remplacé par SessionStateDto dans la v2."""
    session_id: str
    role: str = "CLIENT"
    utilisateur_id: Optional[int] = None
    produit_id: Optional[int] = None
    derniere_question: Optional[str] = None
    derniere_requete_sql: Optional[str] = None
    derniers_resultats_db: Optional[List[Dict[str, Any]]] = None
    dernier_chart: Optional[ChartDto] = None
    dernier_type_graphique: Optional[str] = None
    derniere_palette: Optional[List[str]] = None
    derniers_titre: Optional[str] = None


# ---------------------------------------------------------------------------
# AgentResponse — rétrocompatibilité (utilisé par /ask et l'ancien pipeline)
# ---------------------------------------------------------------------------

class AgentResponse(BaseModel):
    question: str
    reponse: str
    sql_genere: Optional[str] = None
    entree_id_catalogue: Optional[str] = None
    score_similarite: float = 0.0
    chart: Optional[ChartDto] = None
    resultats: Optional[List[Dict[str, Any]]] = None
    agent_source: str = "Orchestrateur"
    intent_detecte: Optional[AgentIntent] = None
    suggestions: Optional[List[str]] = None
