"""
vectorstore.py
Gère la base vectorielle ChromaDB pour WicStock AI.
Utilise un embedding local léger (hash-based) quand Ollama n'est pas disponible.
"""

import json
import os
import logging
import hashlib

os.environ["ANONYMIZED_TELEMETRY"] = "False"
logging.getLogger("chromadb.telemetry").setLevel(logging.CRITICAL)
logging.getLogger("chromadb.telemetry.product.posthog").setLevel(logging.CRITICAL)

import chromadb
from chromadb.config import Settings

EMBEDDING_MODEL = "nomic-embed-text"
CHROMA_PATH = os.path.join(os.path.dirname(__file__), "..", "chroma_db")
DATA_DIR = os.path.join(os.path.dirname(__file__), "..", "data")
SCHEMA_FILE = os.path.join(DATA_DIR, "schema_description.json")
SQL_EXAMPLES_FILE = os.path.join(DATA_DIR, "sql_examples.json")

OLLAMA_AVAILABLE = False


def _simple_embedding(text: str, dim: int = 128) -> list:
    """
    Embedding local léger basé sur des caractéristiques linguistiques du texte.
    Fonctionne sans Ollama ni GPU. Assez précis pour la recherche RAG par mots-clés.
    """
    text = text.lower()
    vec = [0.0] * dim

    keywords = [
        "stock", "produit", "quantite", "categorie", "rupture", "surstock",
        "commande", "vente", "prix", "client", "utilisateur", "alerte",
        "livraison", "tissu", "reference", "total", "historique", "statut",
        "date", "mouvement", "entree", "sortie", "seuil", "prevision",
        "action", "recommandation", "obsolescence", "stockfaible", "optimal",
        "select", "from", "where", "join", "group", "order", "count", "sum",
        "avg", "max", "min", "having", "distinct", "limit", "and", "or",
        "disponible", "archive", "promotion", "remise", "paiement", "avis",
        "reclamation", "score", "risque", "categorie", "tissu", "coton",
        "polyester", "laine", "soie", "jean", "robe", "pantalon", "chemise",
        "chiffre", "affaires", "revenu", "benefice", "marge", "ecoulement",
        "taux", "jours", "semaine", "mois", "annee", "periode", "derniere",
    ]

    for i, kw in enumerate(keywords):
        if kw in text:
            idx = i % dim
            vec[idx] += 1.0

    h = hashlib.md5(text.encode("utf-8")).digest()
    for i, b in enumerate(h):
        vec[i % dim] += (b - 128) / 256.0

    words = text.split()
    for w in words:
        idx = abs(hash(w)) % dim
        vec[idx] += 0.5

    norm = sum(x * x for x in vec) ** 0.5 or 1.0
    return [x / norm for x in vec]


def get_embedding(text: str) -> list:
    """Essaie Ollama, retombe sur l'embedding local si indisponible."""
    global OLLAMA_AVAILABLE
    if OLLAMA_AVAILABLE:
        try:
            import ollama
            response = ollama.embeddings(model=EMBEDDING_MODEL, prompt=text)
            return response["embedding"]
        except Exception:
            OLLAMA_AVAILABLE = False
            print("[vectorstore] Ollama indisponible, bascule sur embedding local.")
    return _simple_embedding(text)


def _check_ollama():
    """Teste si Ollama est disponible au démarrage."""
    global OLLAMA_AVAILABLE
    try:
        import ollama
        ollama.embeddings(model=EMBEDDING_MODEL, prompt="test")
        OLLAMA_AVAILABLE = True
        print("[vectorstore] Ollama détecté — embeddings sémantiques activés.")
    except Exception:
        OLLAMA_AVAILABLE = False
        print("[vectorstore] Ollama non disponible — embedding local activé (mode cloud).")


def get_chroma_client():
    return chromadb.PersistentClient(
        path=CHROMA_PATH,
        settings=Settings(anonymized_telemetry=False)
    )


def build_vectorstore(force_rebuild: bool = False):
    _check_ollama()
    client = get_chroma_client()
    existing_collections = [c.name for c in client.list_collections()]

    if force_rebuild:
        for name in ["schema_knowledge", "sql_examples"]:
            if name in existing_collections:
                client.delete_collection(name)
        existing_collections = []

    if "schema_knowledge" not in existing_collections:
        schema_collection = client.create_collection(
            name="schema_knowledge", metadata={"hnsw:space": "cosine"}
        )
        try:
            with open(SCHEMA_FILE, "r", encoding="utf-8-sig") as f:
                schema_data = json.load(f)
            for chunk in schema_data["chunks"]:
                embedding = get_embedding(chunk["contenu"])
                schema_collection.add(
                    ids=[chunk["id"]],
                    embeddings=[embedding],
                    documents=[chunk["contenu"]],
                    metadatas=[{"type": chunk["type"]}],
                )
            print(f"[vectorstore] {len(schema_data['chunks'])} chunks de schema ingeres.")
        except Exception as ex:
            print(f"[vectorstore] WARN schema: {ex}")
    else:
        print("[vectorstore] Collection schema_knowledge existante, ingestion ignoree.")

    if "sql_examples" not in existing_collections:
        sql_collection = client.create_collection(
            name="sql_examples", metadata={"hnsw:space": "cosine"}
        )
        try:
            with open(SQL_EXAMPLES_FILE, "r", encoding="utf-8-sig") as f:
                sql_examples = json.load(f)
            for i, example in enumerate(sql_examples):
                embedding = get_embedding(example["question"])
                example_id = example.get("id", f"sql-example-{i}")
                roles_str = json.dumps(
                    example.get("rolesAutorises", ["RESPONSABLE_STOCK_PRODUCTION", "ADMIN"])
                )
                type_graphique = example.get("typeGraphique") or ""
                sql_collection.add(
                    ids=[f"sql-{i}-{example_id}"],
                    embeddings=[embedding],
                    documents=[example["question"]],
                    metadatas=[{
                        "id": example_id,
                        "sql": example["sql"],
                        "typeGraphique": type_graphique,
                        "rolesAutorises": roles_str,
                    }],
                )
            print(f"[vectorstore] {len(sql_examples)} exemples SQL ingeres.")
        except Exception as ex:
            print(f"[vectorstore] WARN sql_examples: {ex}")
    else:
        print("[vectorstore] Collection sql_examples existante, ingestion ignoree.")


def get_collections():
    client = get_chroma_client()
    schema_collection = client.get_collection(name="schema_knowledge")
    sql_collection = client.get_collection(name="sql_examples")
    return schema_collection, sql_collection
