"""
main.py
WicStock AI — API FastAPI Multi-Agents (v2)

Endpoints :
  POST /chat            → Pipeline principal (nouveau DTO AssistantResponseDto)
  POST /ask             → Legacy / déprécié (redirige vers la même logique, retourne AgentResponse)
  POST /customize-chart → Legacy / déprécié (couvert par le cycle /chat)
  POST /expliquer-action
  POST /analyser-surstock
  POST /reset-memoire
  POST /rebuild-vectorstore
  GET  /health
"""

import os
os.environ["ANONYMIZED_TELEMETRY"] = "False"
import logging

logging.getLogger("chromadb.telemetry").setLevel(logging.CRITICAL)
logging.getLogger("chromadb.telemetry.product.posthog").setLevel(logging.CRITICAL)
logging.getLogger("httpx").setLevel(logging.WARNING)

from datetime import datetime
from fastapi import FastAPI, Response
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
from typing import Optional, List

from app.vectorstore import build_vectorstore
from app.agents.orchestrator_agent import OrchestratorAgent
from app.agents.surstock_agent import SurstockAgent
from app.models.agent_schemas import (
    AssistantResponseDto,
    ChartPreferences,
    AgentResponse,
    AgentIntent,
)
from app.ollama_client import generer_explication_action, reinitialiser_memoire
import app.core.session_memory as session_memory

# ---------------------------------------------------------------------------
# Application FastAPI
# ---------------------------------------------------------------------------

app = FastAPI(
    title="WicStock AI — Multi-Agents System",
    version="2.0.0",
    description=(
        "Architecture WicStock AI : "
        "4 Agents décisionnels (Orchestrateur, NL2SQL, Preference, Surstock) · "
        "1 Agent technique de sécurité/exécution (SQLGuardAgent) · 1 Service interne (ChartBuilder)"
    ),
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=[
        "https://localhost:7121",
        "http://localhost:7121",
        "http://localhost:5000",
        "https://localhost:5001",
        "*",
    ],
    allow_methods=["*"],
    allow_headers=["*"],
    allow_credentials=True,
)

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger("WicStockAI.MultiAgents")

# ---------------------------------------------------------------------------
# Instanciation des agents
# ---------------------------------------------------------------------------

orchestrator = OrchestratorAgent()
surstock_agent = SurstockAgent()


# ---------------------------------------------------------------------------
# Modèles de requête
# ---------------------------------------------------------------------------

class ChatRequest(BaseModel):
    """Requête pour l'endpoint principal /chat (v2)."""
    session_id: str
    message: str
    role: Optional[str] = None
    utilisateur_id: Optional[int] = None
    produit_id: Optional[int] = None


class QuestionRequest(BaseModel):
    """Requête legacy pour /ask (v1 — déprécié)."""
    session_id: str
    question: str
    role: Optional[str] = None
    utilisateur_id: Optional[int] = None
    produit_id: Optional[int] = None
    chart_preferences: Optional[ChartPreferences] = None


class CustomizeChartRequest(BaseModel):
    """Requête legacy pour /customize-chart (déprécié)."""
    session_id: str
    question: Optional[str] = None
    type_graphique: Optional[str] = None
    couleurs: Optional[List[str]] = None
    palette: Optional[str] = None
    titre: Optional[str] = None


class ExplicationActionRequest(BaseModel):
    nom_produit: str
    type_risque: str
    score_risque: float
    quantite_actuelle: int
    type_action: str


class AnalyseSurstockRequest(BaseModel):
    produit_id: int
    nom_produit: str
    stock_actuel: int
    est_en_surstock: bool           # True si stock >= 500 u. ET inactivite >= 21j (regle double-condition)
    surplus_unites: int             # max(0, stock_actuel - 500) si est_en_surstock, sinon 0
    seuil_surstock: int             # Toujours 500 (retro-compat, non utilise pour le calcul)
    pourcentage_au_dessus_du_seuil: float  # Retro-compat, derive de surplus_unites
    jours_depuis_derniere_sortie: int
    taux_ecoulement_90_jours: float
    categorie: str
    taux_ecoulement_moyen_categorie_90_jours: float
    est_tendance_categorie: bool
    nb_references_similaires_en_surstock: int
    duree_ecoulement_moyenne_produits_similaires: int
    valeur_stock_immobilisee: float
    cout_possession_estime_mensuel: float


# ---------------------------------------------------------------------------
# Événements cycle de vie
# ---------------------------------------------------------------------------

@app.on_event("startup")
def au_demarrage():
    print("[main] Construction / vérification de la base vectorielle ChromaDB...")
    build_vectorstore(force_rebuild=False)
    print("[main] Base vectorielle prête. Système Multi-Agents WicStock AI initialisé.")


# ---------------------------------------------------------------------------
# Health check
# ---------------------------------------------------------------------------

@app.get("/health")
def health_check():
    return {
        "status": "ok",
        "service": "WicStock AI Multi-Agents",
        "version": "2.0.0",
        "agents": {
            "decisionnels": [
                "Orchestrateur (app.agents.orchestrator_agent)",
                "NL2SQL (app.agents.nl2sql_agent)",
                "Preference (app.agents.preference_agent)",
                "Surstock (app.agents.surstock_agent — actif/routé)",
            ],
            "guards": [
                "SQLGuardAgent (app.guards.sql_guard_agent)",
            ],
            "services": [
                "ChartBuilder (app.services.chart_builder)",
            ],
        },
        "endpoints": {
            "/chat": "actif (v2)",
            "/ask": "déprécié → redirige vers /chat",
            "/customize-chart": "déprécié → couvert par le cycle /chat",
        },
    }


# ---------------------------------------------------------------------------
# POST /chat — endpoint principal (v2)
# ---------------------------------------------------------------------------

@app.post("/chat", response_model=AssistantResponseDto)
def chat(request: ChatRequest):
    """
    Endpoint principal v2 — pipeline complet multi-agents.

    Gère :
    - Les nouvelles questions analytiques (NL2SQL → Validator → Executor)
    - Les réponses aux quick replies (state machine: FORMAT_CHOICE, CHART_TYPE, COLOR_CHOICE)
    - La conversation générale / salutations

    Retourne un AssistantResponseDto unifié :
    - text + chart (réponse finale)
    - OU text + options (quick replies, en attente de choix utilisateur)
    """
    role = request.role or "CLIENT"
    horodatage = datetime.now().isoformat()

    response: AssistantResponseDto = orchestrator.handle_chat(
        session_id=request.session_id,
        message=request.message,
        role=role,
        utilisateur_id=request.utilisateur_id,
        produit_id=request.produit_id,
    )

    # Construire un label d'audit significatif même quand intent est None
    audit_intent = response.intent or (
        f"STATE:{response.pending_state}" if response.pending_state else "FINAL"
    )
    logger.info(
        f"[AUDIT {horodatage}] /chat session='{request.session_id}' "
        f"role='{role}' intent='{audit_intent}' "
        f"pending='{response.pending_state or 'none'}' agent='{response.agent_source}'"
    )

    return response


# ---------------------------------------------------------------------------
# POST /ask — legacy déprécié (v1 rétrocompatibilité)
# ---------------------------------------------------------------------------

@app.post("/ask")
def ask(request: QuestionRequest, response: Response):
    """
    Endpoint legacy v1 — déprécié.
    Redirige vers la même logique que /chat et retourne un AgentResponse (format v1).

    Utiliser /chat à la place pour bénéficier du nouveau DTO AssistantResponseDto
    et du système de quick replies.
    """
    logger.warning(
        "DEPRECATED: endpoint /ask appelé par session='%s'. "
        "Migrer vers /chat pour utiliser le nouveau DTO AssistantResponseDto.",
        request.session_id,
    )
    # En-tête de dépréciation HTTP (pour outillage frontend)
    response.headers["Deprecation"] = "true"
    response.headers["Sunset"] = "2026-12-31"
    response.headers["Link"] = '</chat>; rel="successor-version"'

    role = request.role or "CLIENT"
    horodatage = datetime.now().isoformat()

    agent_resp: AgentResponse = orchestrator.traiter_demande(
        session_id=request.session_id,
        question=request.question,
        role=role,
        utilisateur_id=request.utilisateur_id,
        produit_id=request.produit_id,
        chart_preferences=request.chart_preferences,
    )

    logger.info(
        f"[AUDIT {horodatage}] /ask session='{request.session_id}' "
        f"intent='{agent_resp.intent_detecte}' role='{role}' source='{agent_resp.agent_source}'"
    )

    # Construction du payload chart compatible avec le format attendu par l'ancien frontend
    chart_payload = None
    if agent_resp.chart:
        chart_payload = agent_resp.chart.dict()
        chart_payload["Colors"] = agent_resp.chart.colors
        chart_payload["couleurs"] = agent_resp.chart.colors
        chart_payload["options"] = {
            **(agent_resp.chart.options or {}),
            "colors": agent_resp.chart.colors,
        }

    return {
        "question": agent_resp.question,
        "reponse": agent_resp.reponse,
        "sql_genere": agent_resp.sql_genere,
        "entree_id_catalogue": agent_resp.entree_id_catalogue,
        "score_similarite": agent_resp.score_similarite,
        "chart": chart_payload,
        "resultats": agent_resp.resultats,
        "agent_source": agent_resp.agent_source,
        "intent_detecte": agent_resp.intent_detecte.value if agent_resp.intent_detecte else None,
        "suggestions": agent_resp.suggestions,
    }


# ---------------------------------------------------------------------------
# POST /customize-chart — legacy déprécié
# ---------------------------------------------------------------------------

@app.post("/customize-chart")
def customize_chart(request: CustomizeChartRequest, response: Response):
    """
    Endpoint legacy — déprécié.
    La personnalisation graphique est désormais gérée via le cycle /chat
    (quick replies AWAITING_CHART_TYPE → AWAITING_COLOR_CHOICE).
    """
    logger.warning(
        "DEPRECATED: /customize-chart appelé. Utiliser le cycle /chat à la place."
    )
    response.headers["Deprecation"] = "true"
    response.headers["Link"] = '</chat>; rel="successor-version"'

    prefs = ChartPreferences(
        type_graphique=request.type_graphique,
        couleurs=request.couleurs,
        palette=request.palette,
        titre_personnalise=request.titre,
    )
    prompt = request.question or f"Changer le format en {request.type_graphique or 'graphique'}"

    agent_resp = orchestrator.traiter_demande(
        session_id=request.session_id,
        question=prompt,
        chart_preferences=prefs,
    )

    return {
        "succes": True if agent_resp.chart else False,
        "message": agent_resp.reponse,
        "chart": agent_resp.chart.dict() if agent_resp.chart else None,
        "suggestions": agent_resp.suggestions,
    }


# ---------------------------------------------------------------------------
# POST /expliquer-action
# ---------------------------------------------------------------------------

@app.post("/expliquer-action")
def expliquer_action(request: ExplicationActionRequest):
    """Génère le texte explicatif d'une ActionRecommandee."""
    texte = generer_explication_action(
        nom_produit=request.nom_produit,
        type_risque=request.type_risque,
        score_risque=request.score_risque,
        quantite_actuelle=request.quantite_actuelle,
        type_action=request.type_action,
    )
    return {"texte_genere": texte}


# ---------------------------------------------------------------------------
# POST /analyser-surstock
# ---------------------------------------------------------------------------

@app.post("/analyser-surstock")
def analyser_surstock(request: AnalyseSurstockRequest):
    """Délégué à l'Agent Spécialiste Surstock."""
    res = surstock_agent.diagnostiquer(
        nom_produit=request.nom_produit,
        categorie=request.categorie,
        stock_actuel=request.stock_actuel,
        est_en_surstock=request.est_en_surstock,
        jours_depuis_derniere_sortie=request.jours_depuis_derniere_sortie,
        taux_ecoulement_90_jours=request.taux_ecoulement_90_jours,
        taux_ecoulement_moyen_categorie_90_jours=request.taux_ecoulement_moyen_categorie_90_jours,
        est_tendance_categorie=request.est_tendance_categorie,
        nb_references_similaires_en_surstock=request.nb_references_similaires_en_surstock,
        duree_ecoulement_moyenne_produits_similaires=request.duree_ecoulement_moyenne_produits_similaires,
        valeur_stock_immobilisee=request.valeur_stock_immobilisee,
        cout_possession_estime_mensuel=request.cout_possession_estime_mensuel,
    )
    return res


# ---------------------------------------------------------------------------
# POST /reset-memoire
# ---------------------------------------------------------------------------

@app.post("/reset-memoire")
def reset_memoire(session_id: str):
    """Réinitialise complètement la session (état + mémoire Ollama)."""
    reinitialiser_memoire(session_id)
    orchestrator.reinitialiser_session(session_id)
    return {
        "status": "memoire et session reinitialisees",
        "session_id": session_id,
    }


# ---------------------------------------------------------------------------
# POST /rebuild-vectorstore
# ---------------------------------------------------------------------------

@app.post("/rebuild-vectorstore")
def rebuild_vectorstore():
    """Force la reconstruction de la base vectorielle ChromaDB."""
    build_vectorstore(force_rebuild=True)
    return {"status": "vectorstore reconstruite"}
