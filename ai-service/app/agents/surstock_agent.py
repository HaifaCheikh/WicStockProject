"""
surstock_agent.py
Agent Spécialiste : Diagnostic de surstock & recommandations d'actions anti-gaspillage.
"""

from typing import Optional
import json
import re
import ollama

MODEL_NAME = "qwen3:1.7b"


class SurstockAgent:
    def __init__(self, model_name: str = MODEL_NAME):
        self.model_name = model_name

    # Seuil fixe de surstock en unités (doit rester cohérent avec MetriquesStockService.cs).
    SEUIL_SURSTOCK_UNITES: int = 500

    def diagnostiquer(
        self,
        nom_produit: str,
        categorie: str,
        stock_actuel: int,
        est_en_surstock: bool,  # True si stock >= 500 u. ET inactivité >= 21 jours (règle double-condition)
        jours_depuis_derniere_sortie: int,
        taux_ecoulement_90_jours: float,
        taux_ecoulement_moyen_categorie_90_jours: float,
        est_tendance_categorie: bool,
        nb_references_similaires_en_surstock: int,
        duree_ecoulement_moyenne_produits_similaires: Optional[int] = None,
        valeur_stock_immobilisee: float = 0.0,
        cout_possession_estime_mensuel: Optional[float] = None,
    ) -> dict:
        """
        Génère un diagnostic comparatif et 2-3 actions concrètes basées sur des données strictes.

        Le surplus est calculé comme max(0, stock_actuel - 500) uniquement si est_en_surstock est True.
        Un produit avec un gros stock mais moins de 21 jours d'inactivité aura est_en_surstock=False
        et donc surplus=0, conformément à la règle double-condition définie dans MetriquesStockService.
        """
        # Surplus : calculé localement depuis la constante fixe — cohérent avec le backend C#.
        surplus = max(0, stock_actuel - self.SEUIL_SURSTOCK_UNITES) if est_en_surstock else 0

        # Part du surstock dans le stock total, TOUJOURS bornee entre 0 et 100%.
        # Remplace l'ancien pourcentage_au_dessus_du_seuil (surplus/seuil*100), qui explosait
        # sans limite pour les gros stocks (ex: stock=4000, seuil=500 => +700%, confus a l'affichage).
        # Coherent avec MetriquesStockService.cs (PourcentagePartSurstock) et DashboardController.cs.
        pourcentage_part_surstock = (
            round((surplus / float(stock_actuel)) * 100.0, 1) if surplus > 0 and stock_actuel > 0 else 0.0
        )
        seuil_surstock = self.SEUIL_SURSTOCK_UNITES
        contexte_tendance = (
            f"Ce surstock touche également {nb_references_similaires_en_surstock} autre(s) référence(s) de la catégorie {categorie} (tendance générale)."
            if est_tendance_categorie
            else f"La catégorie {categorie} conserve un écoulement moyen de {taux_ecoulement_moyen_categorie_90_jours}%. Ce ralentissement est donc propre à cette référence."
        )

        duree_promo_jours = 14
        cout_txt = (
            f" (coût de possession : {cout_possession_estime_mensuel} DT/mois)"
            if cout_possession_estime_mensuel is not None
            else ""
        )

        prix_unitaire = (valeur_stock_immobilisee / float(stock_actuel)) if stock_actuel > 0 else 0.0

        if surplus > 0:
            # Diagnostic exprimé en unités + part bornée du stock (ex: "87% du stock"),
            # plus lisible qu'un pourcentage non borné du type "+700%".
            info_surplus = f" (seuil de surstock : {seuil_surstock} u. ; surplus : {surplus} u., soit {pourcentage_part_surstock}% du stock actuel)"
            diag_surplus = f", soit un surplus de {surplus} u. ({pourcentage_part_surstock}% du stock actuel dépasse le seuil recommandé de {seuil_surstock} u.)"
            justif_promo = f"Permet de résorber les {surplus} unités en surstock (promotion valable {duree_promo_jours} jours)."
            # IMPORTANT (coherence metier, alignee sur AnalyseSurstockService.cs) : Promotion et
            # Recyclage sont deux actions ALTERNATIVES, l'utilisateur n'en choisit qu'une seule.
            # Chacune doit donc, a elle seule, resorber la TOTALITE du surplus -- on ne prend
            # plus seulement 50% du surplus pour le recyclage (incoherent avec la promotion 100%).
            quantite_recyclage = max(10, min(surplus, stock_actuel))
            label_recyclage = f"Marquer {quantite_recyclage} unités pour recyclage"
        else:
            # Pas en surstock (est_en_surstock=False) : actions préventives génériques
            info_surplus = f" (seuil de surstock : {seuil_surstock} u. — inactivité < 21 jours, pas encore en surstock)"
            diag_surplus = ""
            justif_promo = f"Permet d'accélérer l'écoulement des stocks (promotion valable {duree_promo_jours} jours)."
            quantite_recyclage = max(10, int(stock_actuel * 0.25))
            label_recyclage = f"Marquer {quantite_recyclage} unités pour recyclage"

        valeur_recyclage_partielle = round(quantite_recyclage * prix_unitaire, 2)
        justif_recyclage = f"Libère une valeur immobilisée de {valeur_recyclage_partielle} DT en marquant {quantite_recyclage} unités{cout_txt}."

        prompt = f"""Tu es un expert en gestion de stock textile anti-gaspillage. Rédige un diagnostic professionnel (2 phrases max) et 2 à 3 actions concrètes pour le produit "{nom_produit}".

DONNÉES DU PRODUIT (STRICTES ET VÉRIFIÉES EN SQL) :
- Produit : {nom_produit} ({categorie})
- Stock actuel : {stock_actuel} unités{info_surplus}
- Inactivité : aucune vente depuis {jours_depuis_derniere_sortie} jours
- Taux d'écoulement produit 90j : {taux_ecoulement_90_jours}% (vs {taux_ecoulement_moyen_categorie_90_jours}% pour la catégorie)
- Diagnostic de la catégorie : {contexte_tendance}
- Valeur stock immobilisée : {valeur_stock_immobilisee} DT{cout_txt}

CONSIGNES DE RÉDACTION :
1. Rédige un diagnostic clair (1-2 phrases) expliquant le volume en stock, l'inactivité ({jours_depuis_derniere_sortie} jours sans vente) et précisant s'il s'agit d'un cas isolé ou d'une tendance de la catégorie {categorie}. Ne mentionne pas de surplus si le surplus est 0. N'utilise JAMAIS de pourcentage non borné type "+700%" ; exprime le surplus en unités et/ou en part bornée du stock actuel (ex: "87% du stock").
2. Propose 2 à 3 actions parmi : PROMOTION_CIBLEE, RECYCLAGE_ANTICIPE, NOTIFICATION_PRODUCTION.
3. Pour PROMOTION_CIBLEE : indique qu'elle résorbe le surplus et mentionne la durée de validité de la promotion (ex: "promotion valable 14 jours"). Ne mentionne JAMAIS de durée d'écoulement sans action de plusieurs centaines ou milliers de jours.

RÉPONDS UNIQUEMENT AU FORMAT JSON SUIVANT (sans texte autour, sans markdown) :
{{
  "diagnostic": "Le stock de {nom_produit} présente {stock_actuel} unités{diag_surplus}, sans vente depuis {jours_depuis_derniere_sortie} jours. {contexte_tendance}",
  "actions": [
    {{
      "typeAction": "PROMOTION_CIBLEE",
      "label": "Créer une promotion ciblée (-20%)",
      "justification": "{justif_promo}"
    }},
    {{
      "typeAction": "RECYCLAGE_ANTICIPE",
      "label": "{label_recyclage}",
      "justification": "{justif_recyclage}"
    }}
  ]
}}"""

        try:
            reponse = ollama.chat(
                model=self.model_name,
                messages=[{"role": "user", "content": prompt}],
                options={"temperature": 0.1},
            )
            texte_brut = reponse["message"]["content"].strip()
            texte_brut = re.sub(r"<think>.*?</think>", "", texte_brut, flags=re.DOTALL).strip()
            match_json = re.search(r"\{.*\}", texte_brut, re.DOTALL)
            if match_json:
                texte_brut = match_json.group(0)

            resultat = json.loads(texte_brut)
            return {
                "succes": True,
                "diagnostic": resultat.get("diagnostic", ""),
                "actions": resultat.get("actions", []),
            }
        except Exception as e:
            return {
                "succes": False,
                "diagnostic": f"Le stock de {nom_produit} présente {stock_actuel} unités{diag_surplus}, sans vente depuis {jours_depuis_derniere_sortie} jours. {contexte_tendance}",
                "actions": [
                    {
                        "typeAction": "PROMOTION_CIBLEE",
                        "label": "Créer une promotion ciblée (-20%)",
                        "justification": justif_promo,
                    },
                    {
                        "typeAction": "RECYCLAGE_ANTICIPE",
                        "label": label_recyclage,
                        "justification": justif_recyclage,
                    }
                ],
                "erreur": str(e),
            }
