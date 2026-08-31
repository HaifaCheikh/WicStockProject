"""
nl2sql_agent.py
Agent 2 : Compréhension du langage naturel, RAG contextuel et génération/sélection SQL.
"""

import re
from typing import Optional, Dict, Any
from app.rag import trouver_meilleur_match_catalogue, construire_contexte_complet
from app.ollama_client import extraire_sql, _construire_messages
import ollama

SEUIL_DISTANCE_MAX = 0.35  # Distance Cosine max pour le matching catalogue
MODEL_NAME = "qwen3:1.7b"


FORBIDDEN_PATTERNS_CLIENT = [
    r"\badmin\b", r"\badministrateur\b", r"\badministrateurs\b",
    r"\butilisateurs?\b", r"\bcomptes?\b", r"\bmot[s]?\s+de\s+passe\b",
    r"\bmouvements?\b", r"\balertes?\b", r"\bobsolescence\b",
    r"\bpr[eé]visions?\b", r"\bactions?\s+recommand[eé]es?\b"
]


def est_question_interdite_pour_client(question: str) -> bool:
    q_lower = question.lower()
    for pattern in FORBIDDEN_PATTERNS_CLIENT:
        if re.search(pattern, q_lower):
            return True
    return False


class NL2SQLAgent:
    def __init__(self, model_name: str = MODEL_NAME):
        self.model_name = model_name

    def generer_ou_matcher_sql(
        self,
        question: str,
        session_id: str,
        role: str = "CLIENT",
        utilisateur_id: Optional[int] = None,
        produit_id: Optional[int] = None,
    ) -> Dict[str, Any]:
        """
        Analyse la question utilisateur, recherche dans le catalogue certifié RAG ou
        génère une requête SQL adaptée avec substitution sécurisée des paramètres.
        """
        q_lower = question.lower()

        # Guardrail RBAC Strict : Si rôle CLIENT et question sur sujet interdit (admin, utilisateurs, alertes, etc.)
        if role == "CLIENT" and est_question_interdite_pour_client(question):
            return {
                "succes": False,
                "refus_role": True,
                "message": "Cette information n'est pas accessible avec votre rôle actuel.",
                "sql_candidat": None,
                "score_similarite": 0.0,
                "entree_id_catalogue": "REFUS_ROLE_CLIENT",
                "type_graphique": None,
            }

        # 1. Recherche par similarité sémantique dans le catalogue certifié
        match = trouver_meilleur_match_catalogue(question)

        # Si le catalogue certifié trouve un match (même avec distance modérée <= 0.65) dont le rôle est interdit
        if match and match.get("distance", 1.0) <= 0.65:
            roles_autorises = match.get("rolesAutorises", [])
            if role not in roles_autorises and "ALL" not in roles_autorises:
                return {
                    "succes": False,
                    "refus_role": True,
                    "message": "Cette information n'est pas accessible avec votre rôle actuel.",
                    "sql_candidat": None,
                    "score_similarite": match.get("score_similarite", 0.0),
                    "entree_id_catalogue": match.get("id"),
                    "type_graphique": None,
                }

        # Guardrail sémantique : Si la question parle des "plus vendus", ne jamais accepter "jamais vendus"
        if match:
            is_demande_plus_vendus = bool(re.search(r"\b(plus\s+vendus?|meilleures?\s+ventes?|top\s+ventes?)\b", q_lower))
            is_match_jamais_vendus = match["id"] == "produits-jamais-vendus" or "NOT EXISTS" in match.get("sql", "")
            if is_demande_plus_vendus and is_match_jamais_vendus:
                match = None  # Rejeter le faux match

        if match and match["distance"] <= SEUIL_DISTANCE_MAX:
            # Match certifié trouvé
            sql_template = match["sql"]
            roles_autorises = match["rolesAutorises"]
            entry_id = match["id"]
            score = match["score_similarite"]
            type_graphique = match["typeGraphique"]

            # Substitution des paramètres contextuels
            sql_final = sql_template
            if "@ProduitId" in sql_final:
                if produit_id is not None:
                    sql_final = sql_final.replace("@ProduitId", str(produit_id))
                else:
                    return {
                        "succes": False,
                        "parametre_manquant": "@ProduitId",
                        "message": "Veuillez sélectionner un produit pour afficher cette analyse.",
                        "sql_candidat": None,
                        "score_similarite": score,
                        "entree_id_catalogue": entry_id,
                        "type_graphique": None,
                    }

            if "@UtilisateurId" in sql_final:
                if utilisateur_id is not None:
                    sql_final = sql_final.replace("@UtilisateurId", str(utilisateur_id))
                else:
                    return {
                        "succes": False,
                        "parametre_manquant": "@UtilisateurId",
                        "message": "Identifiant utilisateur introuvable dans la session actuelle.",
                        "sql_candidat": None,
                        "score_similarite": score,
                        "entree_id_catalogue": entry_id,
                        "type_graphique": None,
                    }

            # Adaptation dynamique des dates relatives ("15 derniers jours", "60 jours", etc.)
            match_jours = re.search(r"(\d+)\s*(?:dernières?|derniers?)?\s*jours?", question, re.IGNORECASE)
            if match_jours:
                nb_jours = match_jours.group(1)
                sql_final = re.sub(
                    r"DATEADD\s*\(\s*DAY\s*,\s*-\d+\s*,",
                    f"DATEADD(DAY, -{nb_jours},",
                    sql_final,
                    flags=re.IGNORECASE,
                )

            # Adaptation dynamique du TOP N ("les 5 produits", "top 10", "3 premiers")
            match_top = re.search(r"(?:top|les)?\s*(\d+)\s*(?:premiers?|meilleurs?)?\s*(?:produits?|articles?|commandes?|ventes?)", question, re.IGNORECASE)
            if match_top:
                nb_top = match_top.group(1)
                if "TOP " in sql_final.upper():
                    sql_final = re.sub(r"\bTOP\s+\d+\b", f"TOP {nb_top}", sql_final, flags=re.IGNORECASE)
                elif sql_final.upper().lstrip().startswith("SELECT"):
                    sql_final = re.sub(r"^SELECT\s+", f"SELECT TOP {nb_top} ", sql_final, flags=re.IGNORECASE)

            return {
                "succes": True,
                "sql_candidat": sql_final,
                "score_similarite": score,
                "entree_id_catalogue": entry_id,
                "type_graphique": type_graphique,
                "source": "CATALOGUE_CERTIFIE",
            }

        # 2. Si pas de match direct sous le seuil, génération SQL contextuelle assistée par LLM
        contexte = construire_contexte_complet(question)
        messages = _construire_messages(session_id, question, contexte, role, utilisateur_id)

        try:
            reponse_llm = ollama.chat(
                model=self.model_name,
                messages=messages,
                options={"temperature": 0.1, "num_predict": 400},
            )
            texte_llm = reponse_llm["message"]["content"]
            sql_extrait = extraire_sql(texte_llm)

            if not sql_extrait:
                return {
                    "succes": False,
                    "message": "Votre question ne correspond à aucune analyse reconnue. Veuillez reformuler votre demande.",
                    "sql_candidat": None,
                    "score_similarite": match["score_similarite"] if match else 0.0,
                    "entree_id_catalogue": None,
                    "type_graphique": None,
                }

            # Détection du type de graphique recommandé par heuristique sur la question / SQL
            type_recommande = self._deduire_type_graphique(question, sql_extrait)

            return {
                "succes": True,
                "sql_candidat": sql_extrait,
                "score_similarite": 0.5,
                "entree_id_catalogue": "GENERATION_LLM",
                "type_graphique": type_recommande,
                "source": "LLM_GENERATION",
            }
        except Exception as e:
            return {
                "succes": False,
                "message": f"Erreur lors de la génération SQL : {str(e)}",
                "sql_candidat": None,
                "score_similarite": 0.0,
                "entree_id_catalogue": None,
                "type_graphique": None,
            }

    def _deduire_type_graphique(self, question: str, sql: str) -> Optional[str]:
        q_lower = question.lower()
        if any(w in q_lower for w in ["répartition", "repartition", "statut", "état", "etat", "catégorie", "categorie", "pourcentage", "part"]):
            return "donut"
        if any(w in q_lower for w in ["évolution", "evolution", "tendance", "par mois", "par jour", "historique", "temps"]):
            return "line"
        if any(w in q_lower for w in ["top", "plus vendu", "plus cher", "chiffre d'affaires", "ca", "quantité", "quantite"]):
            return "bar"
        if "GROUP BY" in sql.upper():
            return "bar"
        return None
