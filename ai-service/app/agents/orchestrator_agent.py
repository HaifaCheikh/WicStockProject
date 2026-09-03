"""
orchestrator_agent.py
Agent Orchestrateur — WicStock AI Multi-Agents (v2).

Point d'entrée unique du pipeline. Gère la state machine conversationnelle.

Ordre de traitement à chaque message entrant :
  1. Vérifier le TTL de session (reset si expiré, via session_memory)
  2. Lire l'état courant depuis SessionMemory
  3. Si état ≠ IDLE → déléguer au PreferenceAgent (quick reply ou texte libre)
     - Exception : si le message ressemble à une nouvelle question analytique forte
       → réinitialiser l'état à IDLE et traiter comme une nouvelle requête
  4. Si état == IDLE → classifier l'intention → router vers l'agent approprié

State machine :
  IDLE
    → question analytique        → NL2SQL + Executor + Validator
                                   → si chart_eligible : AWAITING_FORMAT_CHOICE
                                   → sinon : IDLE (texte direct)
  AWAITING_FORMAT_CHOICE
    → "Texte simple"             → IDLE
    → "Graphique"                → AWAITING_CHART_TYPE
  AWAITING_CHART_TYPE
    → type reconnu               → AWAITING_COLOR_CHOICE
    → texte libre                → parsing → AWAITING_COLOR_CHOICE
    → nouvelle question forte    → IDLE (reset) → NL2SQL
  AWAITING_COLOR_CHOICE
    → palette/couleur            → ChartDto final → IDLE
    → texte libre                → parsing → ChartDto final → IDLE
    → nouvelle question forte    → IDLE (reset) → NL2SQL
"""

import re
import time
from typing import Optional, List

from app.models.agent_schemas import (
    AgentIntent,
    AgentResponse,
    AssistantResponseDto,
    ChartPreferences,
    ChartDto,
    ConversationState,
    QuickOptionDto,
    SessionStateDto,
)
from app.agents.nl2sql_agent import NL2SQLAgent
from app.guards.sql_guard_agent import SQLGuardAgent
from app.agents.preference_agent import PreferenceAgent
from app.services.chart_builder import ChartBuilder
from app.agents.surstock_agent import SurstockAgent
from app.services.surstock_data_fetcher import find_product_in_catalog, fetch_surstock_metrics
import app.core.session_memory as session_memory
from app.core.trace_logger import TraceLogger
from app.ollama_client import synthetiser_reponse_naturelle, MODEL_NAME as OLLAMA_MODEL

# ------------------------------------------------------------------
# Mots-clés déclencheurs d'une nouvelle question analytique (forte)
# Si détectés pendant un état d'attente → reset à IDLE + nouvelle requête
# ------------------------------------------------------------------
_STRONG_ANALYTICAL_TRIGGERS = [
    r"combien\s+d[e']",
    r"quels?\s+",
    r"quelles?\s+",
    r"quel\s+",
    r"quelle\s+",
    r"liste\s+d[eu]s?\s+",
    r"donne.?moi\s+les",
    r"affiche\s+les",
    r"montre.?moi",
    r"chiffre.?d.affaire",
    r"surstock",
    r"rupture",
    r"commandes?\s+en",
    r"produits?\s+les\s+plus",
    r"top\s+\d+",
    r"actions?\s*",
    r"recommandations?\s*",
    r"que\s+faire",
    r"pourquoi\s+",
    r"comment\s+",
]


class OrchestratorAgent:
    """Orchestrateur principal du pipeline multi-agents WicStock AI v2."""

    def __init__(self):
        self.nl2sql_agent = NL2SQLAgent()
        self.sql_guard = SQLGuardAgent()
        self.preference_agent = PreferenceAgent()
        self.chart_builder = ChartBuilder()
        self.surstock_agent = SurstockAgent()

    # ------------------------------------------------------------------
    # Endpoint principal — /chat
    # ------------------------------------------------------------------

    def handle_chat(
        self,
        session_id: str,
        message: str,
        role: str = "CLIENT",
        utilisateur_id: Optional[int] = None,
        produit_id: Optional[int] = None,
    ) -> AssistantResponseDto:
        """
        Pipeline principal /chat.
        Retourne un AssistantResponseDto avec texte + optionnellement chart ou options.
        """
        tl = TraceLogger()
        tl.start(session_id=session_id, role=role, user_message=message)

        # 1. Récupérer / créer la session (TTL appliqué en interne)
        session = session_memory.get_or_create(
            session_id=session_id,
            role=role,
            utilisateur_id=utilisateur_id,
            produit_id=produit_id,
        )

        current_state = session.state
        msg_lower = message.strip().lower()

        # 2. Si état ≠ IDLE → vérifier si le message est un choix de personnalisation valide pour cet état
        if current_state != ConversationState.IDLE:
            is_customization = self.preference_agent.is_valid_customization_input(message, current_state)
            intent_check = self._classify_intent(message, session)

            if not is_customization or self._is_strong_new_question(msg_lower) or intent_check != AgentIntent.DATA_ANALYSIS:
                tl.session_event(
                    f"User message '{message}' is not a valid customization choice for state {current_state.value}. "
                    "Resetting state to IDLE and processing as new request."
                )
                session_memory.reset_to_idle(session_id)
                session = session_memory.get_or_create(session_id, role, utilisateur_id, produit_id)
                current_state = ConversationState.IDLE
            else:
                # Déléguer au PreferenceAgent
                question_type = session_memory.resolve_question_type(
                    session.derniere_question or message
                )
                tl.orchestrator(
                    state=current_state.value,
                    intent="FORMAT_CHOICE / CHART_TYPE / COLOR_CHOICE",
                    next_agent="PreferenceAgent",
                )
                tl.preference_agent(
                    chart_eligible=session.chart_eligible,
                    next_state=current_state.value,
                    proposed_options=None,
                )
                response = self.preference_agent.handle(
                    user_message=message,
                    session=session,
                    question_type=question_type,
                )
                tl.response(
                    text=response.text,
                    options=[o.label for o in response.options] if response.options else None,
                    chart_type=response.chart.type if response.chart else None,
                )
                tl.end()
                return response

                        # 3. État IDLE -> vérifier si c'est une demande de personnalisation de graphique
        if self._is_chart_customization_request(message, session):
            if session.derniers_resultats_db:
                question_type = session_memory.resolve_question_type(
                    session.derniere_question or message
                )
                tl.orchestrator(
                    state="IDLE",
                    intent="CHART_CUSTOMIZATION",
                    next_agent="PreferenceAgent",
                )
                response = self.preference_agent.handle(
                    user_message=message,
                    session=session,
                    question_type=question_type,
                )
                tl.response(
                    text=response.text,
                    options=[o.label for o in response.options] if response.options else None,
                    chart_type=response.chart.type if response.chart else None,
                )
                tl.end()
                return response
            else:
                tl.response(text="Aucune donnée en session pour modifier le graphique.")
                tl.end()
                return AssistantResponseDto(
                    text="Il n'y a pas encore de données à afficher sous forme de graphique. Posez-moi d'abord une question sur vos données (ex: *\"Quels sont les produits en rupture de stock ?\"*).",
                    chart=None,
                    pending_state=None,
                    options=None,
                    agent_source="Orchestrateur",
                    intent=AgentIntent.CHART_CUSTOMIZATION.value,
                )

        # 4. État IDLE -> classifier l'intention
        intent = self._classify_intent(message, session)

        # --- Conversation générale / salutations ---
        if intent == AgentIntent.CHAT_GENERAL:
            tl.orchestrator(state="IDLE", intent="CHAT_GENERAL", next_agent="Orchestrateur")
            tl.response(text="Bonjour ! Je suis l'assistant WicStock.")
            tl.end()

            if role == "CLIENT":
                texte_bienvenue = (
                    "👋 Bonjour ! Je suis l'assistant intelligent de **WicStock**.\n\n"
                    "Je peux vous accompagner pour explorer le catalogue, vérifier les articles les mieux notés, "
                    "découvrir les produits sur commande, suivre l'état de vos commandes et réclamations, "
                    "ou afficher la répartition de vos achats.\n\n"
                    "**Comment puis-je vous aider ?**"
                )
                suggestions_list = [
                    "🛒 Quels sont les articles du catalogue ?",
                    "⭐ Quels sont les articles les mieux notés ?",
                    "🚚 Quels sont les articles disponibles sur commande ?",
                    "📦 Quel est l'état de mes commandes ?",
                    "💰 Quel est le montant total de mes commandes ?",
                    "📝 Quel est l'état de mes réclamations ?",
                    "📊 Répartition de mes commandes par statut",
                ]
            else:
                texte_bienvenue = (
                    "👋 Bonjour ! Je suis l'assistant intelligent de **WicStock**.\n\n"
                    "Je peux analyser vos stocks, votre chiffre d'affaires, vos commandes et surstocks, "
                    "et vous présenter les données sous forme de graphiques personnalisés (barres, donut, lignes…).\n\n"
                    "**Comment puis-je vous aider ?**"
                )
                suggestions_list = [
                    "📦 Quels sont les produits en surstock ?",
                    "💰 Quel est le chiffre d'affaires des 30 derniers jours ?",
                    "📊 Répartition des commandes par statut",
                    "⭐ Quels sont les articles les mieux notés ?",
                ]

            return AssistantResponseDto(
                text=texte_bienvenue,
                chart=None,
                pending_state=None,
                options=None,
                intent=AgentIntent.CHAT_GENERAL.value,
                agent_source="Orchestrateur",
                suggestions=suggestions_list,
            )

        # --- Diagnostic Surstock → SurstockAgent ---
        if intent == AgentIntent.SURSTOCK_DIAGNOSTIC:
            tl.orchestrator(state="IDLE", intent="SURSTOCK_DIAGNOSTIC", next_agent="SurstockAgent")

            # 1. Extraire le produit mentionné
            prod_info = find_product_in_catalog(message, self.sql_guard, role=role)

            if not prod_info:
                reponse_clarif = (
                    "🔍 **Diagnostic de Surstock**\n\n"
                    "Pour quel produit souhaitez-vous établir un diagnostic de surstock ?\n\n"
                    "Veuillez préciser le nom d'un produit (ex: *\"Pourquoi le Denim Shirt est en surstock ?\"*)."
                )
                tl.response(text=reponse_clarif)
                tl.end()
                return AssistantResponseDto(
                    text=reponse_clarif,
                    chart=None,
                    pending_state=None,
                    options=None,
                    intent=AgentIntent.SURSTOCK_DIAGNOSTIC.value,
                    agent_source="SurstockAgent",
                )

            # 2. Récupérer les métriques SQL pour ce produit
            donnees_metrics = fetch_surstock_metrics(prod_info["id"], self.sql_guard, role=role)

            if not donnees_metrics:
                reponse_err = f"⚠️ Impossible de récupérer les données de stock pour le produit **{prod_info['nom']}**."
                tl.response(text=reponse_err)
                tl.end()
                return AssistantResponseDto(
                    text=reponse_err,
                    chart=None,
                    pending_state=None,
                    options=None,
                    intent=AgentIntent.SURSTOCK_DIAGNOSTIC.value,
                    agent_source="SurstockAgent",
                )

            # 3. Lancer le diagnostic via SurstockAgent
            # Filtrer uniquement les paramètres attendus par SurstockAgent.diagnostiquer()
            _SURSTOCK_PARAMS = {
                "nom_produit", "categorie", "stock_actuel", "est_en_surstock",
                "jours_depuis_derniere_sortie", "taux_ecoulement_90_jours",
                "taux_ecoulement_moyen_categorie_90_jours", "est_tendance_categorie",
                "nb_references_similaires_en_surstock",
                "duree_ecoulement_moyenne_produits_similaires",
                "valeur_stock_immobilisee", "cout_possession_estime_mensuel",
            }
            donnees_diag = {k: v for k, v in donnees_metrics.items() if k in _SURSTOCK_PARAMS}
            res_diag = self.surstock_agent.diagnostiquer(**donnees_diag)

            nom_p = prod_info["nom"]
            diag_text = res_diag.get("diagnostic", "")
            actions = res_diag.get("actions", [])

            actions_formatted = "\n".join(
                f"- **{act.get('label', act.get('typeAction'))}** : {act.get('justification', '')}"
                for act in actions
            )

            texte_final = (
                f"📦 **Diagnostic — {nom_p}**\n\n"
                f"{diag_text}\n\n"
                f"**Actions recommandées :**\n"
                f"{actions_formatted}"
            )

            tl.surstock(
                nom_produit=nom_p,
                succes=res_diag.get("succes", False),
                nb_actions=len(actions),
            )
            tl.response(text=texte_final)
            tl.end()

            return AssistantResponseDto(
                text=texte_final,
                chart=None,
                pending_state=None,
                options=None,
                intent=AgentIntent.SURSTOCK_DIAGNOSTIC.value,
                agent_source="SurstockAgent",
            )

        # --- Analyse de données → pipeline NL2SQL complet ---
        tl.orchestrator(state="IDLE", intent="DATA_ANALYSIS", next_agent="NL2SQL")

        # Étape NL2SQL
        nl_result = self.nl2sql_agent.generer_ou_matcher_sql(
            question=message,
            session_id=session_id,
            role=role,
            utilisateur_id=utilisateur_id,
            produit_id=produit_id,
        )

        tl.nl2sql(
            docs_retrieved=1 if nl_result.get("succes") else 0,
            similarity_threshold=nl_result.get("score_similarite", 0.0),
            source=nl_result.get("source", "CATALOGUE_CERTIFIE"),
        )

        if not nl_result.get("succes"):
            tl.response(text=nl_result.get("message", "Impossible de traiter."))
            tl.end()
            return AssistantResponseDto(
                text=nl_result.get("message", "⚠️ Je n'ai pas pu interpréter votre demande. Pouvez-vous reformuler ?"),
                chart=None,
                pending_state=None,
                options=None,
                intent=AgentIntent.DATA_ANALYSIS.value,
                sql_genere=nl_result.get("sql_candidat"),
                agent_source="NL2SQLAgent",
            )

        sql_candidat = nl_result["sql_candidat"]
        type_graphique_recommande = nl_result.get("type_graphique")
        score = nl_result.get("score_similarite", 0.0)

        tl.sql(generated_sql=sql_candidat)

        # Étape SQL Guard (validation + exécution fusionnées)
        is_valid, sql_valide, resultats_db, error_msg, tables_used, exec_ms = (
            self.sql_guard.validate_and_execute(sql=sql_candidat, role=role)
        )

        tl.validator(
            select_pass=is_valid,
            rbac_pass=is_valid,
            role=role,
            tables=tables_used,
        )

        if not is_valid:
            if "n'est pas accessible" in (error_msg or ""):
                user_msg = "Cette information n'est pas accessible avec votre rôle actuel."
            else:
                _fallback_msg = "Cette requ\u00eate n'est pas autoris\u00e9e."
                user_msg = f"\U0001f6ab {error_msg or _fallback_msg}"

            tl.response(text=user_msg)
            tl.end()
            return AssistantResponseDto(
                text=user_msg,
                chart=None,
                pending_state=None,
                options=None,
                intent=AgentIntent.DATA_ANALYSIS.value,
                sql_genere=sql_candidat,
                agent_source="SQLGuardAgent (Validator)",
            )

        tl.executor(
            rows=len(resultats_db) if resultats_db else 0,
            execution_ms=exec_ms,
        )

        if resultats_db is None:
            tl.response(text=error_msg or "Erreur d'exécution SQL.")
            tl.end()
            _exec_fallback = "Erreur lors de l'ex\u00e9cution de la requ\u00eate."
            return AssistantResponseDto(
                text=f"\u26a0\ufe0f {error_msg or _exec_fallback}",
                chart=None,
                pending_state=None,
                options=None,
                intent=AgentIntent.DATA_ANALYSIS.value,
                sql_genere=sql_valide,
                agent_source="SQLGuardAgent (Executor)",
            )

        resultats_db = resultats_db or []

        # Synthèse textuelle (via LLM)
        reponse_texte = synthetiser_reponse_naturelle(message, resultats_db)

        # Mettre à jour la session avec les nouvelles données
        question_type = session_memory.resolve_question_type(message)
        session.derniere_question = message
        session.derniere_requete_sql = sql_valide
        session.derniers_resultats_db = resultats_db
        session.derniers_titre = message
        session.dernier_type_graphique = type_graphique_recommande or "bar"

        # Éligibilité au graphique
        chart_eligible = bool(resultats_db and len(resultats_db) >= 1 and type_graphique_recommande)
        session.chart_eligible = chart_eligible

        tl.preference_agent(
            chart_eligible=chart_eligible,
            next_state=(
                ConversationState.AWAITING_FORMAT_CHOICE.value
                if chart_eligible
                else ConversationState.IDLE.value
            ),
        )

        # Sauvegarder avant de déléguer au PreferenceAgent
        session_memory.save(session)

        if chart_eligible:
            # Déléguer la proposition format au PreferenceAgent
            response = self.preference_agent.propose_format_choice(
                session=session,
                question_type=question_type,
                result_text=reponse_texte,
            )
            tl.response(
                text=response.text,
                options=[o.label for o in response.options] if response.options else None,
            )
            tl.end()
            return response

        # Pas de graphique disponible → texte direct
        session.state = ConversationState.IDLE
        session_memory.save(session)

        tl.response(text=reponse_texte)
        tl.end()

        return AssistantResponseDto(
            text=reponse_texte,
            chart=None,
            pending_state=None,
            options=None,
            intent=AgentIntent.DATA_ANALYSIS.value,
            sql_genere=sql_valide,
            agent_source="NL2SQL+Executor",
        )

    # ------------------------------------------------------------------
    # Endpoint legacy — /ask (rétrocompatibilité)
    # Redirige vers handle_chat et convertit AssistantResponseDto → AgentResponse
    # ------------------------------------------------------------------

    def traiter_demande(
        self,
        session_id: str,
        question: str,
        role: str = "CLIENT",
        utilisateur_id: Optional[int] = None,
        produit_id: Optional[int] = None,
        chart_preferences: Optional[ChartPreferences] = None,
    ) -> AgentResponse:
        """
        Point d'entrée legacy pour /ask.
        Convertit le résultat de handle_chat en AgentResponse pour rétrocompatibilité.
        """
        import logging
        logging.getLogger("WicStockAI.MultiAgents").warning(
            "DEPRECATED: endpoint /ask → use /chat. "
            "This method will be removed in a future version."
        )

        # Si des préférences explicites sont passées (ex: /customize-chart)
        # et qu'il y a des données en session → appliquer directement
        session = session_memory.get_or_create(session_id, role, utilisateur_id, produit_id)
        if chart_preferences and session.derniers_resultats_db:
            prefs = chart_preferences
            type_cible = prefs.type_graphique or session.dernier_type_graphique or "bar"
            chart_dto = self.chart_builder.construire_chart_dto(
                type_graphique=type_cible,
                titre=session.derniers_titre or question,
                resultats_db=session.derniers_resultats_db,
                preferences=prefs,
            )
            session.dernier_chart = chart_dto
            if chart_dto:
                session.dernier_type_graphique = chart_dto.type
            session_memory.save(session)

            return AgentResponse(
                question=question,
                reponse=f"Voici vos résultats sous forme de graphique {type_cible.capitalize()}.",
                sql_genere=session.derniere_requete_sql,
                entree_id_catalogue="SESSION_IN_MEMORY",
                score_similarite=1.0,
                chart=chart_dto,
                resultats=session.derniers_resultats_db,
                agent_source="Agent 3 (Chart UI & Visualisation)",
                intent_detecte=AgentIntent.CHART_CUSTOMIZATION,
            )

        # Sinon → pipeline complet
        dto = self.handle_chat(
            session_id=session_id,
            message=question,
            role=role,
            utilisateur_id=utilisateur_id,
            produit_id=produit_id,
        )

        # Convertir les suggestions depuis les options quick reply (rétrocompat)
        suggestions = None
        if dto.options:
            suggestions = [o.label for o in dto.options]

        return AgentResponse(
            question=question,
            reponse=dto.text,
            sql_genere=dto.sql_genere,
            entree_id_catalogue=None,
            score_similarite=0.0,
            chart=dto.chart,
            resultats=None,
            agent_source=dto.agent_source or "Orchestrateur v2",
            intent_detecte=AgentIntent(dto.intent) if dto.intent else None,
            suggestions=suggestions,
        )

    def reinitialiser_session(self, session_id: str) -> None:
        """Supprime complètement la session (mémoire + état)."""
        session_memory.delete(session_id)

    # ------------------------------------------------------------------
    # Classification d'intention (IDLE seulement)
    # ------------------------------------------------------------------


    def _is_chart_customization_request(self, message: str, session: SessionStateDto) -> bool:
        """
        Détermine si le message est une demande de personnalisation de graphique ou de couleurs,
        ou s'il correspond aux valeurs des boutons d'options.
        """
        msg_lower = message.strip().lower()

        # Si c'est une question analytique forte ("combien de...", "quels sont..."), ce n'est pas une customisation
        if self._is_strong_new_question(msg_lower):
            return False

        # 1. Types de graphiques
        chart_types = [
            "barres", "barre", "bar", "bars", "histogramme", "colonnes", "colonne",
            "donut", "anneau", "camembert", "pie", "secteur", "tarte",
            "ligne", "lignes", "line", "courbe", "courbes",
            "boxplot", "box plot", "boite a moustaches", "boîte à moustaches", "moustaches", "moustache",
            "waterfall", "cascade", "jauge", "gauge", "tachymetre", "tachymètre",
            "radar", "spider", "treemap", "entonnoir", "funnel", "nuage", "scatter", "bulles", "bulle",
        ]
        if any(re.search(rf"\b{re.escape(ct)}\b", msg_lower) for ct in chart_types):
            return True

        # 2. Palettes et couleurs
        palettes = [
            "palette wicstock", "wicstock", "éco vert", "eco vert", "vert",
            "mode sombre", "corporate", "sombre", "dark", "sunset", "coucher de soleil",
            "ocean", "océan", "pastel", "moderne", "violet", "bleu", "orange", "rouge",
            "palette principale", "palette nature", "palette sombre", "palette chaude",
        ]
        if any(re.search(rf"\b{re.escape(p)}\b", msg_lower) for p in palettes):
            return True

        # 3. Code hexadécimal
        if re.search(r"#(?:[0-9a-fA-F]{3}){1,2}\b", message):
            return True

        # 4. Déclencheurs / mots-clés de personnalisation
        patterns = [
            "graphique", "chart", "visuel", "texte simple",
            "forme", "type", "couleur", "couleurs", "palette",
            "change", "changer", "modifier", "personnaliser", "affiche", "montre", "mets",
        ]
        if any(re.search(rf"\b{re.escape(p)}\b", msg_lower) for p in patterns):
            salutations = ["bonjour", "bonsoir", "salut", "hello", "hi", "coucou"]
            if not any(msg_lower.startswith(w) or msg_lower == w for w in salutations):
                return True

        return False

    def _classify_intent(self, message: str, session: SessionStateDto) -> AgentIntent:
        """
        Classe l'intention du message utilisateur.
        Appelé uniquement quand la session est en état IDLE.
        """
        q = message.strip().lower()

        # Salutations courtes
        salutations = [
            "bonjour", "bonsoir", "salut", "hello", "hi", "coucou",
            "aide-moi", "aide moi", "que peux-tu faire", "que peux tu faire",
            "qu'est-ce que tu peux faire",
        ]
        if any(q.startswith(w) or q == w for w in salutations) and len(q.split()) <= 6:
            return AgentIntent.CHAT_GENERAL

        # 1. Questions de liste ou de comptage génériques (ex: "Quels produits sont en surstock ?", "Liste des surstocks")
        # Ces questions doivent TOUJOURS exécuter la requête NL2SQL (DATA_ANALYSIS) et non le SurstockAgent
        list_count_patterns = [
            r"^\s*qu[ee]ls?\s+.*produits?\s+.*en\s+surstock",
            r"^\s*combien\s+de\s+produits",
            r"^\s*liste\s+d[eu]s?\s+produits",
            r"^\s*affiche\s+les\s+surstocks",
            r"^\s*donne.?moi\s+les\s+surstocks",
        ]
        action_diagnostic_keywords = [r"actio", r"recom", r"pourquoi", r"que\s+faire", r"quoi\s+faire", r"diagnostic", r"stratégi"]
        
        is_list_query = any(re.search(pat, q) for pat in list_count_patterns)
        has_action_keyword = any(re.search(pat, q) for pat in action_diagnostic_keywords)

        if is_list_query and not has_action_keyword:
            return AgentIntent.DATA_ANALYSIS

        # 2. Diagnostic et actions recommandées sur un produit spécifique (SurstockAgent)
        surstock_diagnostic_triggers = [
            r"actio",
            r"recom",
            r"propos",
            r"suggest",
            r"que\s+faire",
            r"quoi\s+faire",
            r"pourquoi.*surstock",
            r"analyse.*surstock",
            r"diagnostic",
            r"comment\s+(écouler|vendre|gérer|réduire)",
            r"pour\s+(ce|le|ce\s+produit|un)\s+produit",
            r"produit\s+.+:\s*\d+",  # Pattern pour format produit copié-collé: "Contasting suede jacket : 999"
        ]
        if any(re.search(pat, q) for pat in surstock_diagnostic_triggers):
            return AgentIntent.SURSTOCK_DIAGNOSTIC

        return AgentIntent.DATA_ANALYSIS

    def _is_strong_new_question(self, msg_lower: str) -> bool:
        """
        Détecte si le message est une nouvelle question analytique forte,
        même si la session est dans un état d'attente (AWAITING_*).
        Si oui → reset de l'état à IDLE.
        """
        for pattern in _STRONG_ANALYTICAL_TRIGGERS:
            if re.search(pattern, msg_lower):
                return True
        return False
