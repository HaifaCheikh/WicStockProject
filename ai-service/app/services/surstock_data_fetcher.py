"""
surstock_data_fetcher.py
Module utilitaire pour la récupération et le calcul des métriques de surstock 100% SQL.

Traçabilité des métriques :
- nom_produit                                   : Produits.Nom
- categorie                                     : Produits.Categorie
- stock_actuel                                  : Stocks.QuantiteActuelle
- seuil_surstock                                : (CASE WHEN Stocks.SeuilAlerte IS NULL OR Stocks.SeuilAlerte <= 0 THEN 10 ELSE Stocks.SeuilAlerte END)
- pourcentage_au_dessus_du_seuil                : round(((stock - seuil) / seuil) * 100, 1)
- jours_depuis_derniere_sortie                  : DATEDIFF(day, MAX(DateVente), GETDATE()) sur HistoriqueVentes
- taux_ecoulement_90_jours                      : (ventes_90j / (stock + ventes_90j)) * 100
- taux_ecoulement_moyen_categorie_90_jours      : Moyenne SQL (AVG) sur la catégorie dans Produits / Stocks / Ventes
- nb_references_similaires_en_surstock          : COUNT des produits de la même catégorie avec stock > 100
- est_tendance_categorie                        : nb_references_similaires_en_surstock >= 1
- duree_ecoulement_moyenne_produits_similaires  : int(stock / (ventes_90j / 90)) si ventes_90j > 0 sinon None
- valeur_stock_immobilisee                      : stock_actuel * PrixUnitaire
- cout_possession_estime_mensuel                : None (pas de colonne de coût de stockage en BD)
"""

import re
from typing import Dict, Any, Optional, Tuple, List
from app.guards.sql_guard_agent import SQLGuardAgent


def find_product_in_catalog(
    user_message: str,
    sql_guard: SQLGuardAgent,
    role: str = "CLIENT",
) -> Optional[Dict[str, Any]]:
    """
    Recherche un produit du catalogue mentionné dans le message utilisateur.
    Retourne un dict {'id': int, 'nom': str, 'categorie': str, 'prix': float} ou None si non trouvé.
    """
    sql_query = "SELECT p.Id, p.Nom, p.Categorie, p.PrixUnitaire FROM Produits p WHERE p.EstArchive = 0"
    success, sql_clean, results, err_msg, tables, ms = sql_guard.validate_and_execute(sql_query, role=role)

    if not success or not results:
        return None

    msg_lower = user_message.lower()

    # 1. Matching exact ou par inclusion sur le nom complet
    for row in results:
        nom = str(row.get("Nom", ""))
        if nom.lower() in msg_lower:
            return {
                "id": int(row["Id"]),
                "nom": nom,
                "categorie": str(row.get("Categorie", "")),
                "prix": float(row.get("PrixUnitaire", 0.0)),
            }

    # 2. Matching par mots clés significatifs (au moins un mot discriminant du nom)
    mots_ignores = {
        "pourquoi", "le", "la", "les", "du", "de", "des", "est", "en", "surstock",
        "analyse", "diagnostic", "recommandation", "recommandations", "recommandee", "recommandees",
        "actions", "action", "actios", "quel", "quels", "quelle", "quelles", "qules",
        "pour", "ce", "un", "une", "produit", "produits", "sont", "que", "faire", "quoi"
    }
    words_in_msg = set(re.findall(r"\b[a-zA-Z0-9_-]+\b", msg_lower)) - mots_ignores

    for row in results:
        nom = str(row.get("Nom", ""))
        nom_words = set(re.findall(r"\b[a-zA-Z0-9_-]+\b", nom.lower()))
        # Si un mot significatif du produit (ex: "denim", "shirt", "jean") apparaît dans le message
        if nom_words and nom_words.intersection(words_in_msg):
            return {
                "id": int(row["Id"]),
                "nom": nom,
                "categorie": str(row.get("Categorie", "")),
                "prix": float(row.get("PrixUnitaire", 0.0)),
            }

    return None


def fetch_surstock_metrics(
    product_id: int,
    sql_guard: SQLGuardAgent,
    role: str = "CLIENT",
) -> Optional[Dict[str, Any]]:
    """
    Exécute les requêtes SQL nécessaires pour constituer le dictionnaire de 13 métriques
    destiné à SurstockAgent.diagnostiquer().
    """
    # Assainissement anti-injection SQL des paramètres
    product_id_clean = int(product_id)
    categorie_clean = str(categorie if 'categorie' in locals() and categorie else '').replace("'", "''")

    # 1. Infos produit et stock courant
    sql_prod = (
        f"SELECT p.Id, p.Nom, p.Categorie, p.PrixUnitaire, s.QuantiteActuelle, "
        f"500 AS SeuilSurstock "
        f"FROM Produits p "
        f"JOIN Stocks s ON p.Id = s.ProduitId "
        f"WHERE p.Id = {product_id_clean}"
    )
    ok_p, _, res_p, _, _, _ = sql_guard.validate_and_execute(sql_prod, role=role)
    if not ok_p or not res_p:
        return None

    prod_row = res_p[0]
    nom_produit = str(prod_row["Nom"])
    categorie = str(prod_row["Categorie"])
    prix_unitaire = float(prod_row.get("PrixUnitaire", 0.0))
    stock_actuel = int(prod_row.get("QuantiteActuelle", 0))
    seuil_surstock = 500
    if seuil_surstock <= 0:
        seuil_surstock = 500

    surplus = max(0, stock_actuel - seuil_surstock)
    pourcentage_surplus = round((surplus / float(seuil_surstock)) * 100.0, 1) if surplus > 0 else 0.0
    valeur_stock_immobilisee = round(stock_actuel * prix_unitaire, 2)

    # 2. Historique des ventes et inactivité par rapport à DateCreation et DateVente
    sql_ventes = (
        f"SELECT "
        f"COALESCE(DATEDIFF(day, MAX(hv.DateVente), GETDATE()), DATEDIFF(day, p.DateCreation, GETDATE()), 0) AS JoursSansVente, "
        f"COALESCE(SUM(CASE WHEN hv.DateVente >= DATEADD(day, -90, GETDATE()) THEN hv.QuantiteVendue ELSE 0 END), 0) AS Ventes90j "
        f"FROM Produits p "
        f"LEFT JOIN HistoriqueVentes hv ON p.Id = hv.ProduitId "
        f"WHERE p.Id = {product_id_clean} "
        f"GROUP BY p.DateCreation"
    )
    ok_v, _, res_v, _, _, _ = sql_guard.validate_and_execute(sql_ventes, role=role)

    jours_sans_vente = 0
    ventes_90j = 0
    if ok_v and res_v:
        jours_sans_vente = max(0, int(res_v[0].get("JoursSansVente") or 0))
        ventes_90j = int(res_v[0].get("Ventes90j") or 0)

    # Règle Métier Anti-Faux Positif : Un produit ayant moins de 21 jours d'inactivité (3 semaines, ex: créé aujourd'hui 0j)
    # est un nouveau stock de lancement et NE DOIT PAS être considéré en surstock stagnant.
    SEUIL_INACTIVITE_MIN_JOURS = 21
    if jours_sans_vente < SEUIL_INACTIVITE_MIN_JOURS:
        surplus = 0
        pourcentage_surplus = 0.0

    # Taux écoulement produit 90j
    denom_prod = stock_actuel + ventes_90j
    taux_ecoulement_90_jours = (
        round((float(ventes_90j) / float(denom_prod)) * 100.0, 1) if denom_prod > 0 else 0.0
    )

    # Durée écoulement projetée du produit
    duree_ecoulement_moyenne_produits_similaires = None
    if ventes_90j > 0:
        vente_quotidienne = ventes_90j / 90.0
        duree_ecoulement_moyenne_produits_similaires = int(stock_actuel / vente_quotidienne)

    # 3. Références similaires en surstock dans la même catégorie
    # 3. Références similaires en surstock dans la même catégorie (Règle 2-conditions: stock >= 500 ET inactivité >= 21j)
    # Sanitisation categorie pour requetes suivantes
    categorie_clean = str(categorie).replace("'", "''")

    # 3. Références similaires en surstock dans la même catégorie (Règle 2-conditions: stock >= 500 ET inactivité >= 21j)
    # Prend en compte la date de sortie/vente la plus récente entre HistoriqueVentes ET MouvementsStock (SORTIE)
    sql_cat_surstock = (
        f"SELECT COUNT(*) AS NbSimilairesSurstock FROM ("
        f"  SELECT p2.Id "
        f"  FROM Produits p2 "
        f"  JOIN Stocks s2 ON p2.Id = s2.ProduitId "
        f"  LEFT JOIN HistoriqueVentes hv2 ON p2.Id = hv2.ProduitId "
        f"  LEFT JOIN MouvementsStock ms2 ON s2.Id = ms2.StockId AND ms2.Type = 'SORTIE' "
        f"  WHERE p2.Categorie = '{categorie_clean}' AND s2.QuantiteActuelle >= 500 AND p2.Id <> {product_id_clean} "
        f"  GROUP BY p2.Id, p2.DateCreation "
        f"  HAVING DATEDIFF(day, COALESCE("
        f"    CASE "
        f"      WHEN MAX(hv2.DateVente) IS NOT NULL AND MAX(ms2.Date) IS NOT NULL "
        f"        THEN (CASE WHEN MAX(hv2.DateVente) > MAX(ms2.Date) THEN MAX(hv2.DateVente) ELSE MAX(ms2.Date) END) "
        f"      ELSE COALESCE(MAX(hv2.DateVente), MAX(ms2.Date)) "
        f"    END, p2.DateCreation), GETDATE()) >= 21"
        f") sub"
    )
    ok_cs, _, res_cs, _, _, _ = sql_guard.validate_and_execute(sql_cat_surstock, role=role)
    nb_references_similaires_en_surstock = int(res_cs[0]["NbSimilairesSurstock"]) if ok_cs and res_cs else 0
    est_tendance_categorie = nb_references_similaires_en_surstock >= 1

    # 4. Taux écoulement moyen de la catégorie sur 90j
    sql_cat_avg = (
        f"SELECT p3.Id, s3.QuantiteActuelle, "
        f"COALESCE(SUM(hv3.QuantiteVendue), 0) AS Ventes90jCat "
        f"FROM Produits p3 "
        f"JOIN Stocks s3 ON p3.Id = s3.ProduitId "
        f"LEFT JOIN HistoriqueVentes hv3 ON p3.Id = hv3.ProduitId AND hv3.DateVente >= DATEADD(day, -90, GETDATE()) "
        f"WHERE p3.Categorie = '{categorie_clean}' "
        f"GROUP BY p3.Id, s3.QuantiteActuelle"
    )
    ok_ca, _, res_ca, _, _, _ = sql_guard.validate_and_execute(sql_cat_avg, role=role)

    taux_ecoulement_moyen_categorie_90_jours = 0.0
    if ok_ca and res_ca:
        taux_list = []
        for r in res_ca:
            stk = int(r.get("QuantiteActuelle", 0))
            vnt = int(r.get("Ventes90jCat", 0))
            den = stk + vnt
            if den > 0:
                taux_list.append((vnt / den) * 100.0)
        if taux_list:
            taux_ecoulement_moyen_categorie_90_jours = round(sum(taux_list) / len(taux_list), 1)

    est_en_surstock = (stock_actuel >= 500 and jours_sans_vente >= 21)

    return {
        "est_en_surstock": est_en_surstock,
        "surplus_unites": surplus,
        "nom_produit": nom_produit,
        "categorie": categorie,
        "stock_actuel": stock_actuel,
        "seuil_surstock": seuil_surstock,
        "pourcentage_au_dessus_du_seuil": pourcentage_surplus,
        "jours_depuis_derniere_sortie": jours_sans_vente,
        "taux_ecoulement_90_jours": taux_ecoulement_90_jours,
        "taux_ecoulement_moyen_categorie_90_jours": taux_ecoulement_moyen_categorie_90_jours,
        "est_tendance_categorie": est_tendance_categorie,
        "nb_references_similaires_en_surstock": nb_references_similaires_en_surstock,
        "duree_ecoulement_moyenne_produits_similaires": duree_ecoulement_moyenne_produits_similaires,
        "valeur_stock_immobilisee": valeur_stock_immobilisee,
        "cout_possession_estime_mensuel": None,
    }
