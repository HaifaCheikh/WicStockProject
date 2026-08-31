"""
vectorstore.py
GÃ¨re la base vectorielle ChromaDB : crÃ©ation des collections,
ingestion des chunks de schÃ©ma et des exemples SQL, calcul des embeddings via Ollama.
"""

import json
import os
import logging

# Désactiver la télémétrie anonyme ChromaDB / PostHog
os.environ["ANONYMIZED_TELEMETRY"] = "False"
logging.getLogger("chromadb.telemetry").setLevel(logging.CRITICAL)
logging.getLogger("chromadb.telemetry.product.posthog").setLevel(logging.CRITICAL)

import chromadb
from chromadb.config import Settings
import ollama

EMBEDDING_MODEL = "nomic-embed-text"

CHROMA_PATH = os.path.join(os.path.dirname(__file__), "..", "chroma_db")

DATA_DIR = os.path.join(os.path.dirname(__file__), "..", "data")
SCHEMA_FILE = os.path.join(DATA_DIR, "schema_description.json")
SQL_EXAMPLES_FILE = os.path.join(DATA_DIR, "sql_examples.json")


def get_embedding(text: str) -> list[float]:
    response = ollama.embeddings(model=EMBEDDING_MODEL, prompt=text)
    return response["embedding"]


def get_chroma_client():
    return chromadb.PersistentClient(
        path=CHROMA_PATH,
        settings=Settings(anonymized_telemetry=False)
    )


def build_vectorstore(force_rebuild: bool = False):
    client = get_chroma_client()
    existing_collections = [c.name for c in client.list_collections()]

    if force_rebuild:
        for name in ["schema_knowledge", "sql_examples"]:
            if name in existing_collections:
                client.delete_collection(name)
        existing_collections = []

    if "schema_knowledge" not in existing_collections:
        schema_collection = client.create_collection(name="schema_knowledge", metadata={"hnsw:space": "cosine"})
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
        print(f"[vectorstore] {len(schema_data['chunks'])} chunks de schÃ©ma ingÃ©rÃ©s.")
    else:
        print("[vectorstore] Collection schema_knowledge dÃ©jÃ  existante, ingestion ignorÃ©e.")

    if "sql_examples" not in existing_collections:
        sql_collection = client.create_collection(name="sql_examples", metadata={"hnsw:space": "cosine"})
        with open(SQL_EXAMPLES_FILE, "r", encoding="utf-8-sig") as f:
            sql_examples = json.load(f)

        for i, example in enumerate(sql_examples):
            embedding = get_embedding(example["question"])
            example_id = example.get("id", f"sql-example-{i}")
            roles_str = json.dumps(example.get("rolesAutorises", ["RESPONSABLE_STOCK_PRODUCTION", "ADMIN"]))
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
        print(f"[vectorstore] {len(sql_examples)} exemples SQL ingÃ©rÃ©s avec mÃ©tadonnÃ©es.")
    else:
        print("[vectorstore] Collection sql_examples dÃ©jÃ  existante, ingestion ignorÃ©e.")


def get_collections():
    client = get_chroma_client()
    schema_collection = client.get_collection(name="schema_knowledge")
    sql_collection = client.get_collection(name="sql_examples")
    return schema_collection, sql_collection
