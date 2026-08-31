"""Script pour reconstruire la base vectorielle ChromaDB depuis les fichiers JSON mis à jour."""
import sys
sys.path.insert(0, ".")

from app.vectorstore import build_vectorstore

print("Reconstruction de la base vectorielle...")
build_vectorstore(force_rebuild=True)
print("Terminé ! Vous pouvez relancer uvicorn.")
