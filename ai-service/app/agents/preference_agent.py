"""
preference_agent.py
Agent Preference — WicStock AI Multi-Agents (v2).

Responsabilité : gérer le dialogue de personnalisation graphique en 3 temps.

Cycle de personnalisation :
  Étape 1 (AWAITING_FORMAT_CHOICE) :
    → Propose [Texte simple] [Graphique]
  Étape 2 (AWAITING_CHART_TYPE) :
    → Propose [Barres] [Donut] [Ligne] [Aire] [Autre…]
    → Si "Autre…" : champ libre → parsing du type demandé
  Étape 3 (AWAITING_COLOR_CHOICE) :
    → Propose [Palette WicStock] [Éco Vert] [Mode sombre] [Sunset] [Personnaliser…]
    → Si "Personnaliser…" : champ libre → parsing couleurs + type

Règles importantes :
- Jamais de re-requête SQL pour un changement de format/couleur
- En cas de texte libre non interprétable → clarification (même état, pas d'avancée)
- Les préférences mémorisées sont proposées en priorité ("Comme la dernière fois ?")
"""

import re
from typing import Any, Dict, List, Optional, Tuple

from app.models.agent_schemas import (
    AgentIntent,
    AssistantResponseDto,
    ChartDto,
    ChartPreferences,
    ConversationState,
    QuickOptionDto,
    SessionStateDto,
)
from app.services.chart_builder import ChartBuilder, PALETTES_COULEURS, NOMS_COULEURS_FR
import app.core.session_memory as session_memory

# ------------------------------------------------------------------
# Constantes — réponses attendues par état
# ------------------------------------------------------------------

# Valeurs canoniques acceptées à l'étape FORMAT_CHOICE
FORMAT_TEXT_VALUES = {
    "texte", "texte simple", "text", "non", "sans graphique",
    "texte uniquement", "juste le texte", "pas de graphique",
}
FORMAT_CHART_VALUES = {
    "graphique", "graph", "chart", "oui", "oui en graphique",
    "afficher en graphique", "sous forme de graphique", "visuel",
    "ok", "yes", "allez-y", "allons-y", "vas-y", "c'est bon",
    "parfait", "super", "d'accord", "daccord", "bien sur",
    "bien sûr", "montre", "affiche",
}

# Valeurs canoniques acceptées à l'étape CHART_TYPE_CHOICE
CHART_TYPE_MAP = {
    # Donut / Pie / Camembert / Secteur / Anneau
    "donut": "donut", "donuts": "donut", "anneau": "donut", "anneaux": "donut",
    "camembert": "donut", "camemberts": "donut", "pie": "donut", "pies": "donut",
    "secteur": "donut", "secteurs": "donut", "tarte": "donut", "tartes": "donut",

    # Ligne / Courbe / Tendance / Évolution
    "ligne": "line", "lignes": "line", "line": "line", "lines": "line",
    "courbe": "line", "courbes": "line",
    "tendance": "line", "evolution": "line", "évolution": "line",

    # Boxplot / Boîte à moustaches
    "boxplot": "boxplot", "box plot": "boxplot", "boite a moustaches": "boxplot", "boîte à moustaches": "boxplot",
    "boite a moustache": "boxplot", "boîte à moustache": "boxplot", "moustaches": "boxplot", "moustache": "boxplot",

    # Waterfall / Cascade
    "waterfall": "waterfall", "cascade": "waterfall",

    # Jauge / Gauge
    "jauge": "gauge", "gauge": "gauge", "tachymetre": "gauge", "tachymètre": "gauge",

    # Radar / Spider / Toile
    "radar": "radar", "spider": "radar", "toile": "radar",

    # Treemap / Tree map
    "treemap": "treemap", "tree map": "treemap",

    # Entonnoir / Funnel
    "entonnoir": "funnel", "funnel": "funnel",

    # Nuage / Scatter / Bulles
    "nuage": "scatter", "nuage de points": "scatter", "scatter": "scatter", "bulles": "scatter", "bulle": "scatter",

    # Barres / Histogrammes / Batons
    "barres": "bar", "barre": "bar", "bar": "bar", "bars": "bar",
    "histogramme": "bar", "histogrammes": "bar", "colonnes": "bar", "colonne": "bar",
    "batons": "bar", "bâtons": "bar",
}

# Valeurs canoniques acceptées à l'étape COLOR_CHOICE
PALETTE_MAP = {
    "palette wicstock": "wicstock",
    "wicstock": "wicstock",
    "palette principale": "wicstock",
    "éco vert": "eco",
    "eco vert": "eco",
    "vert": "vert",
    "palette nature": "eco",
    "mode sombre": "corporate",
    "corporate": "corporate",
    "sombre": "corporate",
    "dark": "corporate",
    "palette sombre": "corporate",
    "sunset": "sunset",
    "coucher de soleil": "sunset",
    "palette chaude": "sunset",
    "ocean": "ocean",
    "océan": "ocean",
    "pastel": "pastel",
    "moderne": "moderne",
    "violet": "violet",
    "bleu": "bleu",
    "orange": "orange",
    "rouge": "rouge",
}

# Préfixe spécial pour la suggestion "Comme la dernière fois ?"
LAST_TIME_PREFIX = "__last_time__"


class PreferenceAgent:
    """
    Gère la personnalisation interactive du format et des couleurs du graphique.
    Ne relance JAMAIS de requête SQL.
    """

    def __init__(self):
        self._chart_builder = ChartBuilder()

    # ------------------------------------------------------------------
    # Vérification si le message est un choix de personnalisation valide
    # ------------------------------------------------------------------

    def is_valid_customization_input(self, user_message: str, state: ConversationState) -> bool:
        """
        Vérifie si le message de l'utilisateur correspond réellement à un choix de format/graphique/couleur
        pour l'état de personnalisation courant.
        Si False -> l'orchestrateur sait qu'il s'agit d'une NOUVELLE question et réinitialise l'état à IDLE.
        """
        msg_lower = user_message.strip().lower()

        # 1. Option "Comme la dernière fois"
        if msg_lower.startswith(LAST_TIME_PREFIX):
            return True

        # 2. Texte simple / annulation
        if msg_lower in FORMAT_TEXT_VALUES or any(kw in msg_lower for kw in FORMAT_TEXT_VALUES):
            return True

        # 3. Graphique / oui / confirmation
        if msg_lower in FORMAT_CHART_VALUES or any(kw in msg_lower for kw in FORMAT_CHART_VALUES):
            return True

        # 4. Type de graphique reconnu
        if self._parse_chart_type(msg_lower) or self._parse_free_text_chart(user_message)[0]:
            return True

        # 5. Mots-clés de couleur ou palette
        if self._parse_colors_from_text(user_message) or self._parse_palette(msg_lower):
            return True

        # 6. Mots-clés explicites de personnalisation graphique
        customization_keywords = ["forme", "type", "graphique", "chart", "visuel", "afficher", "couleur", "couleurs", "palette", "barre", "barres", "donut", "ligne", "lignes"]
        if any(kw in msg_lower for kw in customization_keywords):
            return True

        return False

    # ------------------------------------------------------------------
    # Point d'entrée principal
    # ------------------------------------------------------------------

    def handle(
        self,
        user_message: str,
        session: SessionStateDto,
        question_type: str,
    ) -> AssistantResponseDto:
        """
        Traite le message utilisateur pour la personnalisation de graphique / format / couleurs.
        Gère aussi bien les quick replies cliqués que les phrases libres ("change la forme en donut", "mode sombre", etc.).
        """
        msg_lower = user_message.strip().lower()

        # 1. Texte simple / annulation graphique
        if msg_lower in FORMAT_TEXT_VALUES or any(kw in msg_lower for kw in FORMAT_TEXT_VALUES):
            session.state = ConversationState.IDLE
            session_memory.save(session)
            return AssistantResponseDto(
                text="Résultats affichés en texte simple.",
                chart=None,
                pending_state=None,
                options=None,
                agent_source="PreferenceAgent",
                intent=AgentIntent.FORMAT_CHOICE.value,
            )

        # 2. Cas "Comme la dernière fois ?"
        if msg_lower.startswith(LAST_TIME_PREFIX):
            return self._apply_last_time_preference(msg_lower, session, question_type)

        # 3. Détection du type de graphique
        chart_type = self._parse_chart_type(msg_lower)
        if not chart_type:
            chart_type_free, _ = self._parse_free_text_chart(user_message)
            chart_type = chart_type_free

        # 4. Détection des couleurs ou palettes
        colors_from_text = self._parse_colors_from_text(user_message)
        palette_key = None

        # Si l'utilisateur donne 2 couleurs ou plus (ex: bleu, rouge, jaune, orange, vert), on utilise cette liste exacte de couleurs
        if len(colors_from_text) >= 2:
            colors = colors_from_text
            palette_key = None
        else:
            # Ne pas considérer "vert" ou "bleu" comme un nom de palette si une seule couleur spécifique a été demandée dans une phrase
            possible_palette = self._parse_palette(msg_lower)
            if possible_palette and possible_palette in ("wicstock", "eco", "corporate", "sunset", "ocean", "pastel", "moderne"):
                palette_key = possible_palette
                colors = list(PALETTES_COULEURS[palette_key])
            else:
                colors = colors_from_text if colors_from_text else (list(PALETTES_COULEURS[possible_palette]) if possible_palette else None)

        # 5. Routing selon les éléments extraits

        # A) Type ET Couleurs/Palette spécifiés -> Construire le graphique final
        if chart_type and colors:
            session.dernier_type_graphique = chart_type
            return self._build_final_chart(
                session=session,
                question_type=question_type,
                chart_type=chart_type,
                colors=colors,
                palette_name=palette_key,
            )

        # B) Seul le type de graphique est spécifié
        if chart_type:
            session.dernier_type_graphique = chart_type
            session_memory.save(session)

            if session.state in (ConversationState.AWAITING_FORMAT_CHOICE, ConversationState.AWAITING_CHART_TYPE):
                return self._ask_color_choice(session, question_type, chart_type)

            existing_colors = (
                session.dernier_chart.colors
                if session.dernier_chart and session.dernier_chart.colors
                else (session.dernier_chart.custom_palette if session.dernier_chart and session.dernier_chart.custom_palette else [])
            )
            return self._build_final_chart(
                session=session,
                question_type=question_type,
                chart_type=chart_type,
                colors=existing_colors or colors or [],
                palette_name=palette_key,
            )

        # C) Seules les couleurs / palettes sont spécifiées
        if colors or palette_key:
            current_type = session.dernier_type_graphique or "bar"
            return self._build_final_chart(
                session=session,
                question_type=question_type,
                chart_type=current_type,
                colors=colors or [],
                palette_name=palette_key,
            )

        # D) Demande générique de modification des couleurs
        if any(kw in msg_lower for kw in ["couleur", "couleurs", "palette"]):
            return self._ask_color_choice(
                session, question_type, session.dernier_type_graphique or "bar"
            )

        # E) Demande générique de modification de forme/type/graphique
        if any(kw in msg_lower for kw in ["forme", "type", "graphique", "chart", "visuel", "afficher"]):
            return self._ask_chart_type(session, question_type)

        # F) Fallbacks de clarification selon l'état si rien n'a été reconnu
        if session.state == ConversationState.AWAITING_FORMAT_CHOICE:
            return self._clarification_format(session)
        if session.state == ConversationState.AWAITING_CHART_TYPE:
            return self._clarification_chart_type(session, question_type)
        if session.state == ConversationState.AWAITING_COLOR_CHOICE:
            return self._clarification_color(
                session, question_type, session.dernier_type_graphique or "bar"
            )

        return self._ask_chart_type(session, question_type)







    def propose_format_choice(
        self,
        session: SessionStateDto,
        question_type: str,
        result_text: str,
    ) -> AssistantResponseDto:
        """
        Appelé par l'Orchestrateur après une requête SQL réussie, pour proposer
        Texte simple / Graphique (avec éventuellement "Comme la dernière fois ?").
        """
        last_time = session_memory.build_last_time_options(session.session_id, question_type)

        options: List[QuickOptionDto] = []

        # Priorité : suggestion "Comme la dernière fois ?" si disponible
        if last_time:
            options.append(QuickOptionDto(
                label=last_time["label"],
                value=last_time["value"],
            ))

        options += [
            QuickOptionDto(label="Texte simple", value="texte simple"),
            QuickOptionDto(label="Graphique", value="graphique"),
        ]

        session.state = ConversationState.AWAITING_FORMAT_CHOICE
        session_memory.save(session)

        return AssistantResponseDto(
            text=result_text + "\n\n**Souhaitez-vous afficher ces résultats sous forme de graphique ?**",
            chart=None,
            pending_state=ConversationState.AWAITING_FORMAT_CHOICE.value,
            options=options,
            agent_source="PreferenceAgent",
            intent=AgentIntent.FORMAT_CHOICE.value,
        )

    def _handle_format_choice(
        self,
        msg_lower: str,
        session: SessionStateDto,
        question_type: str,
    ) -> AssistantResponseDto:
        """Traite la réponse à l'étape FORMAT_CHOICE."""

        # Cas "Comme la dernière fois ?" — value = "__last_time__<type>"
        if msg_lower.startswith(LAST_TIME_PREFIX):
            return self._apply_last_time_preference(msg_lower, session, question_type)

        # Texte simple
        if msg_lower in FORMAT_TEXT_VALUES or any(kw in msg_lower for kw in FORMAT_TEXT_VALUES):
            session.state = ConversationState.IDLE
            session_memory.save(session)
            return AssistantResponseDto(
                text="Résultats affichés en texte simple.",
                chart=None,
                pending_state=None,
                options=None,
                agent_source="PreferenceAgent",
                intent=AgentIntent.FORMAT_CHOICE.value,
            )

        # Graphique
        if msg_lower in FORMAT_CHART_VALUES or any(kw in msg_lower for kw in FORMAT_CHART_VALUES):
            return self._ask_chart_type(session, question_type)

        # Texte non reconnu → clarification (rester dans le même état)
        return self._clarification_format(session)

    # ------------------------------------------------------------------
    # Étape 2 : choix du type de graphique
    # ------------------------------------------------------------------

    def _ask_chart_type(
        self,
        session: SessionStateDto,
        question_type: str,
    ) -> AssistantResponseDto:
        """Propose les types de graphique disponibles."""
        options: List[QuickOptionDto] = [
            QuickOptionDto(label="📊 Barres", value="barres"),
            QuickOptionDto(label="🍩 Donut", value="donut"),
            QuickOptionDto(label="📈 Ligne", value="ligne"),
            QuickOptionDto(label="📦 Boxplot", value="boxplot"),
            QuickOptionDto(label="🌊 Waterfall", value="waterfall"),
            QuickOptionDto(label="🎯 Jauge", value="jauge"),
            QuickOptionDto(label="🕸️ Radar", value="radar"),
            QuickOptionDto(label="🗃️ Treemap", value="treemap"),
            QuickOptionDto(label="🔻 Entonnoir", value="funnel"),
            QuickOptionDto(label="🫧 Nuage", value="scatter"),
            QuickOptionDto(label="✏️ Autre", value="autre", is_free_text=True),
        ]

        session.state = ConversationState.AWAITING_CHART_TYPE
        session_memory.save(session)

        return AssistantResponseDto(
            text="Quel type de graphique souhaitez-vous ?",
            chart=None,
            pending_state=ConversationState.AWAITING_CHART_TYPE.value,
            options=options,
            agent_source="PreferenceAgent",
            intent=AgentIntent.CHART_TYPE_CHOICE.value,
        )

    def _handle_chart_type(
        self,
        msg_lower: str,
        raw_message: str,
        session: SessionStateDto,
        question_type: str,
    ) -> AssistantResponseDto:
        """Traite la réponse à l'étape CHART_TYPE_CHOICE."""

        # Chercher un type reconnu dans la map
        chart_type = self._parse_chart_type(msg_lower)

        if not chart_type:
            chart_type_free, _ = self._parse_free_text_chart(raw_message)
            chart_type = chart_type_free

        # Fallback intelligent : si l'utilisateur saisit une valeur personnalisée non reconnue (ex: "boxplot", "pareto"),
        # on accepte sa demande et on utilise l'équivalent visuel le plus adapté ("bar").
        if not chart_type and raw_message.strip():
            chart_type = "bar"

        if chart_type:
            session.dernier_type_graphique = chart_type
            session_memory.save(session)
            return self._ask_color_choice(session, question_type, chart_type)

        return self._clarification_chart_type(session, question_type)



    def _parse_chart_type(self, msg_lower: str) -> Optional[str]:
        """Résout le message en type de graphique canonique, avec fallback intelligent."""
        if not msg_lower:
            return None

        # Match direct dans la map
        if msg_lower in CHART_TYPE_MAP:
            return CHART_TYPE_MAP[msg_lower]

        # Recherche par contenance
        for kw, chart_type in CHART_TYPE_MAP.items():
            if kw in msg_lower:
                return CHART_TYPE_MAP[kw]

        # Mots-clés suggérant une répartition circulaire
        if any(w in msg_lower for w in ["part", "pourcentage", "proportion", "répartition"]):
            return "donut"

        # Mots-clés suggérant une évolution temporelle
        if any(w in msg_lower for w in ["temps", "mois", "jour", "annee", "année", "date", "historique"]):
            return "line"

        return None



    def _ask_color_choice(
        self,
        session: SessionStateDto,
        question_type: str,
        chart_type: str,
    ) -> AssistantResponseDto:
        """Propose les palettes de couleurs disponibles."""
        options: List[QuickOptionDto] = [
            QuickOptionDto(label="🎨 WicStock (Denim + Ambre)", value="palette wicstock"),
            QuickOptionDto(label="🌿 Éco Vert", value="éco vert"),
            QuickOptionDto(label="🌙 Mode sombre", value="mode sombre"),
            QuickOptionDto(label="🌅 Sunset", value="sunset"),
            QuickOptionDto(label="✏️ Personnaliser…", value="personnaliser", is_free_text=True),
        ]

        session.state = ConversationState.AWAITING_COLOR_CHOICE
        session_memory.save(session)

        return AssistantResponseDto(
            text=f"Quelle palette de couleurs pour votre graphique **{chart_type}** ?",
            chart=None,
            pending_state=ConversationState.AWAITING_COLOR_CHOICE.value,
            options=options,
            agent_source="PreferenceAgent",
            intent=AgentIntent.COLOR_CHOICE.value,
        )

    def _handle_color_choice(
        self,
        msg_lower: str,
        raw_message: str,
        session: SessionStateDto,
        question_type: str,
    ) -> AssistantResponseDto:
        """Traite la réponse à l'étape COLOR_CHOICE et produit le graphique final."""

        chart_type = session.dernier_type_graphique or "bar"

        # 1. Palette nommée
        palette_key = self._parse_palette(msg_lower)
        if palette_key:
            colors = list(PALETTES_COULEURS[palette_key])
            return self._build_final_chart(
                session=session,
                question_type=question_type,
                chart_type=chart_type,
                colors=colors,
                palette_name=palette_key,
            )

        # 2. Couleurs nommées en français ou hex
        colors = self._parse_colors_from_text(raw_message)
        if colors:
            return self._build_final_chart(
                session=session,
                question_type=question_type,
                chart_type=chart_type,
                colors=colors,
                palette_name=None,
            )

        # 3. Texte libre complexe (ex: "violet et doré dégradé")
        _, colors_free = self._parse_free_text_chart(raw_message)
        if colors_free:
            return self._build_final_chart(
                session=session,
                question_type=question_type,
                chart_type=chart_type,
                colors=colors_free,
                palette_name=None,
            )

        # 4. Impossible à résoudre → clarification, même état
        return self._clarification_color(session, question_type, chart_type)

    def _parse_palette(self, msg_lower: str) -> Optional[str]:
        """Résout le message en clé de palette."""
        if msg_lower in PALETTE_MAP:
            return PALETTE_MAP[msg_lower]

        for kw, palette_key in PALETTE_MAP.items():
            if kw in msg_lower:
                return palette_key

        return None

    def _parse_colors_from_text(self, text: str) -> List[str]:
        """Extrait des codes hex ou noms de couleurs dans l'ordre exact où ils apparaissent dans le texte."""
        if not text:
            return []

        text_lower = text.lower()

        color_lookup = {
            "bleu": "#2563EB", "bleue": "#2563EB", "bleus": "#2563EB",
            "rouge": "#EF4444", "rouges": "#EF4444",
            "jaune": "#F59E0B", "jaunes": "#F59E0B",
            "orange": "#F97316", "oranges": "#F97316",
            "vert": "#10B981", "verte": "#10B981", "verts": "#10B981", "vertes": "#10B981",
            "violet": "#8B5CF6", "violette": "#8B5CF6", "violets": "#8B5CF6",
            "noir": "#1E293B", "noire": "#1E293B", "noirs": "#1E293B",
            "blanc": "#FFFFFF", "blanche": "#FFFFFF",
            "gris": "#64748B", "grise": "#64748B",
            "rose": "#EC4899", "roses": "#EC4899",
            "marron": "#78350F", "brun": "#78350F",
            "turquoise": "#06B6D4", "cyan": "#06B6D4",
            "doré": "#D97706", "dore": "#D97706", "or": "#D97706",
            "ambre": "#F59E0B", "amber": "#F59E0B",
            "denim": "#2B4C7E", "marine": "#1E3A63",
        }

        matches = []

        # 1. Codes HEX
        for m in re.finditer(r"#(?:[0-9a-fA-F]{6}|[0-9a-fA-F]{3})\b", text):
            matches.append((m.start(), m.group(0)))

        # 2. Noms de couleurs
        for name, hex_val in color_lookup.items():
            for m in re.finditer(rf"\b{re.escape(name)}\b", text_lower):
                matches.append((m.start(), hex_val))

        # Trier par position d'apparition dans la phrase
        matches.sort(key=lambda x: x[0])

        # Dédupliquer les positions identiques si présent
        seen = set()
        colors = []
        for pos, hex_code in matches:
            if (pos, hex_code) not in seen:
                seen.add((pos, hex_code))
                colors.append(hex_code)

        return colors





    def _parse_free_text_chart(self, text: str) -> Tuple[Optional[str], List[str]]:
        """
        Parse un texte libre complexe pour extraire type de graphique ET couleurs.
        Ex: "aire empilée dégradé bleu" → ("area", ["#2B4C7E", ...])
        Ex: "violet et doré"            → (None, ["#8B5CF6", "#D97706"])
        """
        text_lower = text.lower()

        # Type de graphique
        chart_type = self._parse_chart_type(text_lower)

        # Couleurs
        colors = self._parse_colors_from_text(text)

        # Si rien trouvé, tenter les noms de palettes dans le texte libre
        if not colors:
            for kw, palette_key in PALETTE_MAP.items():
                if kw in text_lower:
                    colors = list(PALETTES_COULEURS.get(palette_key, []))
                    break

        return chart_type, colors

    # ------------------------------------------------------------------
    # Construction du graphique final (avec mémorisation des prefs)
    # ------------------------------------------------------------------

    def _build_final_chart(
        self,
        session: SessionStateDto,
        question_type: str,
        chart_type: str,
        colors: List[str],
        palette_name: Optional[str],
    ) -> AssistantResponseDto:
        """Construit le ChartDto final et fournit les options séparées de personnalisation (Forme et Couleur)."""

        # Validation du type de graphique (bar, donut, line, area, boxplot)
        if chart_type not in ("bar", "donut", "line", "boxplot", "waterfall", "gauge", "radar", "treemap", "funnel", "scatter"):
            chart_type = "bar"

        if not session.derniers_resultats_db:
            session.state = ConversationState.IDLE
            session_memory.save(session)
            return AssistantResponseDto(
                text="Les données ne sont plus disponibles. Veuillez relancer votre requête.",
                chart=None,
                pending_state=None,
                options=None,
                agent_source="PreferenceAgent",
            )

        prefs = ChartPreferences(
            type_graphique=chart_type,
            couleurs=colors if colors else None,
            palette=palette_name,
        )

        chart_dto = self._chart_builder.construire_chart_dto(
            type_graphique=chart_type,
            titre=session.derniers_titre or session.derniere_question or "Analyse",
            resultats_db=session.derniers_resultats_db,
            preferences=prefs,
        )

        # Mémoriser les préférences pour la session
        session_memory.save_preference(
            session_id=session.session_id,
            question_type=question_type,
            chart_type=chart_type,
            colors=colors,
        )

        session.dernier_chart = chart_dto
        session.dernier_type_graphique = chart_type
        session.state = ConversationState.IDLE
        session_memory.save(session)

        # Traduction française propre du nom de type de graphique
        chart_label_fr_map = {
            "bar": "Barres",
            "donut": "Donut",
            "line": "Ligne",
            "boxplot": "Boxplot",
            "waterfall": "Waterfall",
            "gauge": "Jauge",
            "radar": "Radar",
            "treemap": "Treemap",
            "funnel": "Entonnoir",
            "scatter": "Nuage de points",
        }
        chart_label_fr = chart_label_fr_map.get(chart_type, "Barres")

        desc_couleurs = ""
        if palette_name:
            nom_pal_fr = {
                "wicstock": "Palette principale",
                "eco": "Palette nature",
                "corporate": "Palette sombre",
                "sunset": "Palette chaude",
            }.get(palette_name.lower(), palette_name.capitalize())
            desc_couleurs = f" avec la **{nom_pal_fr}**"
        elif colors:
            desc_couleurs = " avec vos couleurs personnalisées"

        # Options séparées (Forme et Couleur) pour modifier le graphique à tout moment
        options_interactives = [
            # Formes du graphique
            QuickOptionDto(label="Barres", value="barres"),
            QuickOptionDto(label="Donut", value="donut"),
            QuickOptionDto(label="Ligne", value="ligne"),
            QuickOptionDto(label="Boxplot", value="boxplot"),
            QuickOptionDto(label="Waterfall", value="waterfall"),
            QuickOptionDto(label="Jauge", value="jauge"),
            QuickOptionDto(label="Radar", value="radar"),
            QuickOptionDto(label="Treemap", value="treemap"),
            QuickOptionDto(label="Entonnoir", value="funnel"),
            QuickOptionDto(label="Nuage", value="scatter"),
            QuickOptionDto(label="Autres…", value="autre", is_free_text=True),

            # Palettes de couleurs
            QuickOptionDto(label="Palette principale", value="palette wicstock"),
            QuickOptionDto(label="Palette nature", value="éco vert"),
            QuickOptionDto(label="Palette sombre", value="mode sombre"),
            QuickOptionDto(label="Palette chaude", value="sunset"),
            QuickOptionDto(label="Personnalisée", value="personnaliser", is_free_text=True),
        ]
        return AssistantResponseDto(
            text=f"Voici vos résultats en graphique **{chart_label_fr}**{desc_couleurs}.",
            chart=chart_dto,
            pending_state="CHART_ACTIVE",
            options=options_interactives,
            agent_source="PreferenceAgent",
            intent=AgentIntent.COLOR_CHOICE.value,
        )


    def _apply_last_time_preference(
        self,
        msg_lower: str,
        session: SessionStateDto,
        question_type: str,
    ) -> AssistantResponseDto:
        """Applique directement les préférences mémorisées."""
        # Extraire le type depuis la valeur "__last_time__<type>"
        chart_type = msg_lower.replace(LAST_TIME_PREFIX, "").strip()
        if not chart_type or chart_type not in {"bar", "donut", "line", "boxplot", "waterfall", "gauge", "radar", "treemap", "funnel", "scatter"}:
            chart_type = "bar"

        pref = session_memory.get_preference(session.session_id, question_type)
        colors = pref.get("colors", []) if pref else []

        return self._build_final_chart(
            session=session,
            question_type=question_type,
            chart_type=chart_type,
            colors=colors,
            palette_name=None,
        )

    # ------------------------------------------------------------------
    # Messages de clarification (on reste dans le même état)
    # ------------------------------------------------------------------

    def _ask_format(
        self,
        session: SessionStateDto,
        question_type: str,
    ) -> AssistantResponseDto:
        """Repropose le choix de format (fallback)."""
        return AssistantResponseDto(
            text="Souhaitez-vous afficher ces résultats sous forme de graphique ou en texte simple ?",
            chart=None,
            pending_state=ConversationState.AWAITING_FORMAT_CHOICE.value,
            options=[
                QuickOptionDto(label="Texte simple", value="texte simple"),
                QuickOptionDto(label="Graphique", value="graphique"),
            ],
            agent_source="PreferenceAgent",
            intent=AgentIntent.FORMAT_CHOICE.value,
        )

    def _clarification_format(self, session: SessionStateDto) -> AssistantResponseDto:
        """Demande de clarification à l'étape FORMAT_CHOICE."""
        return AssistantResponseDto(
            text='Je n\'ai pas bien compris votre choix. Répondez **"Texte simple"** ou **"Graphique"**.',
            chart=None,
            pending_state=ConversationState.AWAITING_FORMAT_CHOICE.value,
            options=[
                QuickOptionDto(label="Texte simple", value="texte simple"),
                QuickOptionDto(label="Graphique", value="graphique"),
            ],
            agent_source="PreferenceAgent",
            intent=AgentIntent.FORMAT_CHOICE.value,
        )

    def _clarification_chart_type(
        self,
        session: SessionStateDto,
        question_type: str,
    ) -> AssistantResponseDto:
        """Demande de clarification à l'étape CHART_TYPE_CHOICE."""
        return AssistantResponseDto(
            text=(
                "🤔 Je n'ai pas reconnu ce type de graphique. "
                "Choisissez parmi : **Barres**, **Donut**, **Ligne**, **Boxplot**, **Waterfall**, **Jauge**, **Radar**, **Treemap**, **Entonnoir**, **Nuage**, "
                "ou décrivez librement (ex: *\"courbe bleue\"*)."
            ),
            chart=None,
            pending_state=ConversationState.AWAITING_CHART_TYPE.value,
            options=[
                QuickOptionDto(label="📊 Barres", value="barres"),
                QuickOptionDto(label="🍩 Donut", value="donut"),
                QuickOptionDto(label="📈 Ligne", value="ligne"),
                QuickOptionDto(label="✏️ Autre…", value="autre", is_free_text=True),
            ],
            agent_source="PreferenceAgent",
        )

    def _clarification_color(
        self,
        session: SessionStateDto,
        question_type: str,
        chart_type: str,
    ) -> AssistantResponseDto:
        """Demande de clarification à l'étape COLOR_CHOICE."""
        return AssistantResponseDto(
            text=(
                "🤔 Je n'ai pas reconnu ces couleurs. "
                "Essayez un nom comme *\"violet\"*, *\"bleu et doré\"*, un code hex (#8B5CF6), "
                "ou choisissez une palette prédéfinie."
            ),
            chart=None,
            pending_state=ConversationState.AWAITING_COLOR_CHOICE.value,
            options=[
                QuickOptionDto(label="🎨 WicStock (Denim + Ambre)", value="palette wicstock"),
                QuickOptionDto(label="🌿 Éco Vert", value="éco vert"),
                QuickOptionDto(label="🌙 Mode sombre", value="mode sombre"),
                QuickOptionDto(label="🌅 Sunset", value="sunset"),
                QuickOptionDto(label="✏️ Personnaliser…", value="personnaliser", is_free_text=True),
            ],
            agent_source="PreferenceAgent",
        )
