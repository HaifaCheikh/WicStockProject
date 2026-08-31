"""
rag.py
Recherche, pour une question donnée, les chunks de schéma et les exemples SQL
les plus pertinents (par similarité vectorielle), pour construire le contexte
injecté dans le prompt envoyé au LLM.
"""

from app.vectorstore import get_embedding, get_collections


def rechercher_contexte_schema(question: str, n_resultats: int = 6) -> list[str]:
    schema_collection, _ = get_collections()
    embedding = get_embedding(question)

    resultats = schema_collection.query(
        query_embeddings=[embedding],
        n_results=n_resultats,
    )
    return resultats.get("documents", [[]])[0]


def rechercher_exemples_sql(question: str, n_resultats: int = 3) -> list[dict]:
    _, sql_collection = get_collections()
    embedding = get_embedding(question)

    resultats = sql_collection.query(
        query_embeddings=[embedding],
        n_results=n_resultats,
    )

    documents = resultats.get("documents", [[]])[0]
    metadatas = resultats.get("metadatas", [[]])[0]

    return [{"question": doc, "sql": meta.get("sql", "")} for doc, meta in zip(documents, metadatas)]


def trouver_meilleur_match_catalogue(question: str) -> dict | None:
    """
    Recherche l'entrée du catalogue la plus proche dans ChromaDB par similarité vectorielle.
    Retourne un dict contenant l'id, SQL, typeGraphique, rolesAutorises, distance et score_similarite.
    """
    import json
    _, sql_collection = get_collections()
    embedding = get_embedding(question)

    resultats = sql_collection.query(
        query_embeddings=[embedding],
        n_results=1,
        include=["documents", "metadatas", "distances"],
    )

    documents = resultats.get("documents", [[]])[0]
    metadatas = resultats.get("metadatas", [[]])[0]
    distances = resultats.get("distances", [[]])[0]

    if not documents or not metadatas:
        return None

    meta = metadatas[0]
    doc = documents[0]
    dist = distances[0] if distances else 0.0

    roles_raw = meta.get("rolesAutorises", '["RESPONSABLE_STOCK_PRODUCTION", "ADMIN"]')
    try:
        roles_list = json.loads(roles_raw) if isinstance(roles_raw, str) else roles_raw
    except Exception:
        roles_list = ["RESPONSABLE_STOCK_PRODUCTION", "ADMIN"]

    type_graphique = meta.get("typeGraphique") or None

    return {
        "id": meta.get("id", "inconnu"),
        "question_modele": doc,
        "sql": meta.get("sql", ""),
        "typeGraphique": type_graphique if type_graphique != "" else None,
        "rolesAutorises": roles_list,
        "distance": float(dist),
        "score_similarite": round(max(0.0, 1.0 - float(dist)), 3),
    }


def construire_contexte_complet(question: str) -> dict:
    chunks_schema = rechercher_contexte_schema(question)
    exemples_sql = rechercher_exemples_sql(question)

    contexte_schema_texte = "\n".join(f"- {c}" for c in chunks_schema)
    exemples_texte = "\n".join(
        f'Question: "{ex["question"]}"\nSQL: {ex["sql"]}\n' for ex in exemples_sql
    )

    return {
        "schema_context": contexte_schema_texte,
        "few_shot_examples": exemples_texte,
    }
