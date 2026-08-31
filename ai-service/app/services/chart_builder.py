"""
chart_builder.py
Service interne — n'est PAS un agent autonome. Invoqué uniquement par PreferenceAgent
pour construire la représentation graphique (ChartDto) finale.
"""

import re
from typing import List, Dict, Any, Optional, Tuple
from app.ollama_client import synthetiser_reponse_naturelle
from app.models.agent_schemas import ChartDto, ChartPreferences

PALETTES_COULEURS: Dict[str, List[str]] = {
    "wicstock": ["#2B4C7E", "#F59E0B", "#1E6B4C", "#C85A32", "#475569", "#D4A373", "#0D9488", "#406BA8", "#6366F1", "#0284C7"],
    "denim_or": ["#2B4C7E", "#F59E0B", "#406BA8", "#D97706", "#16233A", "#FBBF24"],
    "denim": ["#2B4C7E", "#406BA8", "#1E3A8A", "#3B82F6", "#60A5FA", "#93C5FD"],
    "eco": ["#10B981", "#059669", "#34D399", "#6EE7B7", "#047857", "#A7F3D0"],
    "moderne": ["#2B4C7E", "#F59E0B", "#6366F1", "#8B5CF6", "#3B82F6", "#EC4899", "#14B8A6"],
    "sunset": ["#F59E0B", "#EF4444", "#EC4899", "#8B5CF6", "#F97316", "#D97706"],
    "ocean": ["#0EA5E9", "#0284C7", "#0369A1", "#06B6D4", "#38BDF8", "#7DD3FC"],
    "pastel": ["#A5B4FC", "#FBCFE8", "#BAE6FD", "#BBF7D0", "#FDE68A", "#DDD6FE"],
    "corporate": ["#2B4C7E", "#1E293B", "#334155", "#475569", "#64748B", "#0284C7"],
    "violet": ["#8B5CF6", "#7C3AED", "#6D28D9", "#A78BFA", "#C4B5FD"],
    "bleu": ["#2B4C7E", "#3B82F6", "#2563EB", "#1D4ED8", "#60A5FA", "#93C5FD"],
    "vert": ["#10B981", "#059669", "#047857", "#34D399", "#6EE7B7"],
    "rouge": ["#EF4444", "#DC2626", "#B91C1C", "#F87171", "#FCA5A5"],
    "orange": ["#F59E0B", "#F97316", "#EA580C", "#C2410C", "#FB923C", "#FDBA74"],
}

NOMS_COULEURS_FR = {
    "denim": "#2B4C7E",
    "bleu denim": "#2B4C7E",
    "jaune": "#F59E0B",
    "jaunes": "#F59E0B",
    "jaune orange": "#F59E0B",
    "jaune orangé": "#F59E0B",
    "jaune orangee": "#F59E0B",
    "jaune d'or": "#F59E0B",
    "ambre": "#D97706",
    "violet": "#8B5CF6",
    "violette": "#8B5CF6",
    "violettes": "#8B5CF6",
    "pourpre": "#7C3AED",
    "bleu": "#2B4C7E",
    "bleue": "#2B4C7E",
    "bleues": "#2B4C7E",
    "cyan": "#06B6D4",
    "vert": "#10B981",
    "verte": "#10B981",
    "vertes": "#10B981",
    "émeraude": "#059669",
    "emeraude": "#059669",
    "orange": "#F59E0B",
    "oranges": "#F59E0B",
    "rouge": "#EF4444",
    "rouges": "#EF4444",
    "rose": "#EC4899",
    "roses": "#EC4899",
    "gris": "#64748B",
    "grise": "#64748B",
    "grises": "#64748B",
    "noir": "#1E293B",
    "noire": "#1E293B",
    "noires": "#1E293B",
    "or": "#D97706",
    "doré": "#D97706",
    "dore": "#D97706",
    "dorée": "#D97706",
    "doree": "#D97706",
    "dorés": "#D97706",
    "dores": "#D97706",
    "dorées": "#D97706",
    "dorees": "#D97706",
}


def nettoyer_titre_graphique(titre_raw: str) -> str:
    if not titre_raw:
        return "Analyse"
    t = titre_raw.strip().rstrip("?").strip()
    patterns_sub = [
        (r"^quels?\s+(?:sont\s+)?les?\s+", ""),
        (r"^quelles?\s+(?:sont\s+)?les?\s+", ""),
        (r"^quels?\s+(?:produits\s+)?sont\s+", "Produits "),
        (r"^quelles?\s+(?:commandes\s+)?sont\s+", "Commandes "),
        (r"^combien\s+d[e']\s*", ""),
        (r"^calculer\s+(?:le|la|les)?\s*", ""),
        (r"^donner\s+(?:le|la|les)?\s*", ""),
        (r"^afficher\s+(?:le|la|les)?\s*", ""),
        (r"^montrer\s+(?:le|la|les)?\s*", ""),
        (r"^list[ee]r\s+(?:le|la|les)?\s*", ""),
        (r"^obtenir\s+(?:le|la|les)?\s*", ""),
    ]
    for pat, repl in patterns_sub:
        if re.search(pat, t, re.IGNORECASE):
            t = re.sub(pat, repl, t, flags=re.IGNORECASE).strip()
            break
    if t:
        t = t[0].upper() + t[1:]
    return t or "Analyse"


class ChartBuilder:
    """
    Service de construction des objets ChartDto et gestion des thèmes et couleurs.
    """

    def __init__(self):
        pass

    def extraire_preferences_prompt(self, prompt: str) -> ChartPreferences:
        """
        Extrait intelligemment les souhaits de personnalisation graphique depuis le texte de l'utilisateur.
        (ex: "Mets en camembert avec une couleur violette et dorée", "Change en barres bleues", "sous forme de graohique a barre").
        """
        p_lower = prompt.lower()
        prefs = ChartPreferences()

        # Type de graphique
        if any(w in p_lower for w in ["donut", "donuts", "anneau", "anneaux", "camembert", "pie", "secteur", "secteurs", "tarte"]):
            prefs.type_graphique = "donut"
        elif any(w in p_lower for w in ["barre", "barres", "bar", "bars", "histogramme", "histogrammes", "colonne", "colonnes", "bâton", "baton", "bâtons", "batons"]):
            prefs.type_graphique = "bar"
        elif any(w in p_lower for w in ["ligne", "lignes", "line", "lines", "courbe", "courbes"]):
            prefs.type_graphique = "line"

        # Palettes ou couleurs nommées
        couleurs_trouvees = []
        for nom_pal in PALETTES_COULEURS.keys():
            if f"palette {nom_pal}" in p_lower or f"thème {nom_pal}" in p_lower or f"theme {nom_pal}" in p_lower or f"couleur {nom_pal}" in p_lower:
                prefs.palette = nom_pal
                couleurs_trouvees = PALETTES_COULEURS[nom_pal]
                break

        if not couleurs_trouvees:
            # Recherche de couleurs spécifiques en français
            for fr_color, hex_val in NOMS_COULEURS_FR.items():
                if re.search(rf"\b{re.escape(fr_color)}\b", p_lower):
                    if hex_val not in couleurs_trouvees:
                        couleurs_trouvees.append(hex_val)

        # Recherche de codes HEX direct (#10B981, #FFF, etc.)
        hex_matches = re.findall(r"#(?:[0-9a-fA-F]{3}){1,2}\b", prompt)
        if hex_matches:
            couleurs_trouvees.extend(hex_matches)

        if couleurs_trouvees:
            prefs.couleurs = couleurs_trouvees

        return prefs

    def construire_chart_dto(
        self,
        type_graphique: Optional[str],
        titre: str,
        resultats_db: List[Dict[str, Any]],
        preferences: Optional[ChartPreferences] = None,
    ) -> Optional[ChartDto]:
        """
        Construit le DTO de graphique personnalisé et optimisé pour le rendu Blazor.
        """
        if not resultats_db:
            return None

        # Priorité au type demandé dans les préférences, sinon type par défaut
        type_final = (preferences.type_graphique if preferences and preferences.type_graphique else type_graphique)
        if not type_final or type_final not in ("bar", "donut", "line", "boxplot", "waterfall", "gauge", "radar", "treemap", "funnel", "scatter"):
            type_final = "bar"

        titre_final = (
            preferences.titre_personnalise
            if (preferences and preferences.titre_personnalise)
            else nettoyer_titre_graphique(titre)
        )

        labels = []
        series = []

        # Déterminer la limite de lignes
        max_rows = 8 if type_final == "bar" else 15
        if preferences and preferences.limite_lignes:
            max_rows = preferences.limite_lignes

        rows = resultats_db[:max_rows]

        for row in rows:
            label_val = (
                row.get("Label")
                or row.get("Nom")
                or row.get("StatutCommande")
                or row.get("Type")
                or next((str(v) for k, v in row.items() if isinstance(v, str)), "N/A")
            )
            val_numeric = (
                row.get("Valeur")
                or row.get("ChiffreAffaires")
                or row.get("Total")
                or row.get("QuantiteActuelle")
                or row.get("TotalVendu")
                or row.get("NombreSurstock")
                or next((float(v) for k, v in row.items() if isinstance(v, (int, float))), 0.0)
            )

            lbl_str = str(label_val)
            if type_final == "bar" and len(lbl_str) > 16:
                lbl_str = lbl_str[:15] + "…"

            labels.append(lbl_str)
            try:
                series.append(float(val_numeric))
            except (ValueError, TypeError):
                series.append(0.0)

        # Déterminer la palette de couleurs : Bleu denim #2B4C7E et Jaune orangé #F59E0B par défaut
        colors_final: Optional[List[str]] = None
        if preferences and preferences.couleurs:
            colors_final = list(preferences.couleurs)
        elif preferences and preferences.palette and preferences.palette in PALETTES_COULEURS:
            colors_final = list(PALETTES_COULEURS[preferences.palette])
        else:
            # Couleurs par défaut harmonieuses WicStock (Denim + Jaune orangé en priorité)
            colors_final = list(PALETTES_COULEURS["wicstock"])

        # S'assurer que chaque tranche/barre a sa couleur (couvre tous les labels)
        if colors_final and len(labels) > 0:
            expanded_colors = []
            for i in range(len(labels)):
                expanded_colors.append(colors_final[i % len(colors_final)])
            colors_final = expanded_colors

        # Détecter l'unité (DT pour montants, sinon unités)
        unit = "DT" if any(w in titre.lower() for w in ["chiffre", "ca", "revenu", "prix", "valeur"]) else "unités"

        return ChartDto(
            type=type_final,
            title=titre_final,
            labels=labels,
            series=series,
            colors=colors_final,
            custom_palette=colors_final,
            unit=unit,
            options={
                "colors": colors_final,
                "customPalette": colors_final,
                "animate": True,
                "showLegend": True,
            },
        )

    def generer_suggestions(
        self,
        question: str,
        type_graphique: Optional[str],
        resultats_db: Optional[List[Dict[str, Any]]],
    ) -> List[str]:
        """
        Génère des suggestions proactives d'interaction pour l'utilisateur.
        """
        suggestions = []
        if resultats_db and len(resultats_db) > 0:
            if not type_graphique:
                suggestions.append("📊 Afficher sous forme de graphique en barres")
                suggestions.append("🍩 Afficher sous forme de Donut")
            else:
                if type_graphique == "bar":
                    suggestions.append("🍩 Transformer ce graphique en Donut")
                    suggestions.append("🎨 Changer la palette en Vert Éco")
                elif type_graphique in ["donut", "pie"]:
                    suggestions.append("📊 Passer l'affichage en Barres")
                    suggestions.append("🎨 Appliquer une palette Moderne Indigo / Violet")
                elif type_graphique in ["line", "boxplot"]:
                    suggestions.append("📊 Comparer sous forme d'Histogramme")

        suggestions.append("🔍 Voir les détails pour un produit spécifique")
        return suggestions[:3]

    def synthetiser_reponse(self, question: str, resultats: List[Dict[str, Any]]) -> str:
        return synthetiser_reponse_naturelle(question, resultats)
