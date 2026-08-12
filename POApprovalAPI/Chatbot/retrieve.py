"""
Retrieve top-K schema chunks for a natural-language question.
Usage:
  python retrieve.py "How many POs are pending?" [--k 5]

Prints JSON to stdout:
  { "query": "...", "results": [ { id, objectName, domain, score, embeddingText }, ... ] }
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

import numpy as np

EMBEDDINGS_PATH = Path(__file__).with_name("schema-embeddings.json")
MODEL_NAME = "sentence-transformers/all-MiniLM-L6-v2"


def load_chunks():
    data = json.loads(EMBEDDINGS_PATH.read_text(encoding="utf-8"))
    chunks = data["chunks"]
    matrix = np.array([c["embedding"] for c in chunks], dtype=float)
    return data, chunks, matrix


def embed_query(text: str) -> np.ndarray:
    from fastembed import TextEmbedding

    embedder = TextEmbedding(model_name=MODEL_NAME)
    vec = np.array(list(embedder.embed([text])), dtype=float)[0]
    norm = np.linalg.norm(vec)
    if norm == 0:
        return vec
    return vec / norm


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("query")
    parser.add_argument("--k", type=int, default=5)
    args = parser.parse_args()

    if not EMBEDDINGS_PATH.exists():
        print(json.dumps({"error": f"Missing {EMBEDDINGS_PATH.name}"}))
        return 1

    _, chunks, matrix = load_chunks()
    qv = embed_query(args.query)
    scores = matrix @ qv
    k = max(1, min(args.k, len(chunks)))
    top = np.argsort(-scores)[:k]

    results = []
    for idx in top:
        c = chunks[int(idx)]
        results.append(
            {
                "id": c["id"],
                "objectName": c["objectName"],
                "objectType": c.get("objectType"),
                "domain": c.get("domain"),
                "score": float(scores[idx]),
                "embeddingText": c.get("embeddingText", ""),
            }
        )

    # ensure_ascii=True: escape ₹ etc. so Windows cp1252 stdout pipes never crash
    print(json.dumps({"query": args.query, "k": k, "results": results}, ensure_ascii=True))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
