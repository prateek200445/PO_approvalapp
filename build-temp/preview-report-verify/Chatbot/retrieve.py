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

# Keyword → domain hints for score boosting (lightweight domain router).
DOMAIN_HINTS: dict[str, list[str]] = {
    "PO": ["purchase order", " po ", "pending po", "approve po", "high value po"],
    "Payment": ["bill payment", "payment approval", "utr", "mrn paid", "payment draft"],
    "Ledger": ["ledger", "debtor", "creditor", "outstanding", "ageing", "aging", "msme"],
    "Sales": ["sales invoice", "export customer", "buyer", "credit note", "ebidta", "sales total"],
    "MRN": ["mrn", "store inward", "material receipt", "goods receipt"],
    "Stock": ["stock in hand", "warehouse", "godown", "reorder", "inventory", "stk"],
    "Production": ["loom", "weaving", "webbing", "roll", "production", "small bag", "ebd"],
    "PR": ["purchase req", "purchase requisition", " pr ", "quotation"],
    "JobWork": ["job work", "jobwork", "jwo", "jro", "jbin"],
    "Despatch": ["despatch", "dispatch", "packing list", "shipment"],
}


def boost_domain_scores(query: str, chunks: list, scores: np.ndarray) -> np.ndarray:
    q = f" {query.lower()} "
    boosted = scores.copy()
    for domain, keywords in DOMAIN_HINTS.items():
        if not any(kw in q for kw in keywords):
            continue
        for i, c in enumerate(chunks):
            if c.get("domain") == domain:
                boosted[i] += 0.08
    return boosted


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
    scores = boost_domain_scores(args.query, chunks, scores)
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
