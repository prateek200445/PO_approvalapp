"""
Step 2: Embed schema-catalog.json embeddingText with all-MiniLM-L6-v2 (FastEmbed/ONNX).
Writes schema-embeddings.json for later .NET / retrieval use.

Uses FastEmbed by default (lightweight, no PyTorch). Falls back to sentence-transformers
if FASTEMBED is unavailable and ST is installed.
"""

from __future__ import annotations

import json
import sys
from datetime import datetime, timezone
from pathlib import Path

import numpy as np

CATALOG_PATH = Path(__file__).with_name("schema-catalog.json")
OUTPUT_PATH = Path(__file__).with_name("schema-embeddings.json")
# FastEmbed model id for MiniLM (same family as sentence-transformers/all-MiniLM-L6-v2)
FASTEMBED_MODEL = "sentence-transformers/all-MiniLM-L6-v2"
ST_MODEL = "sentence-transformers/all-MiniLM-L6-v2"


def encode_texts(texts: list[str]) -> tuple[np.ndarray, str, bool]:
    """Return (vectors[n, dim], model_name, normalized)."""
    try:
        from fastembed import TextEmbedding

        print(f"Loading FastEmbed model {FASTEMBED_MODEL} ...")
        embedder = TextEmbedding(model_name=FASTEMBED_MODEL)
        vectors = np.array(list(embedder.embed(texts)), dtype=float)
        # FastEmbed MiniLM outputs are typically already L2-normalized
        norms = np.linalg.norm(vectors, axis=1, keepdims=True)
        norms[norms == 0] = 1.0
        vectors = vectors / norms
        return vectors, FASTEMBED_MODEL, True
    except Exception as fe_err:
        print(f"FastEmbed unavailable ({fe_err}); trying sentence-transformers ...")
        from sentence_transformers import SentenceTransformer

        print(f"Loading SentenceTransformer {ST_MODEL} ...")
        model = SentenceTransformer(ST_MODEL)
        vectors = model.encode(
            texts,
            normalize_embeddings=True,
            show_progress_bar=True,
            convert_to_numpy=True,
        )
        return np.asarray(vectors, dtype=float), ST_MODEL, True


def main() -> int:
    if not CATALOG_PATH.exists():
        print(f"Missing catalog: {CATALOG_PATH}", file=sys.stderr)
        return 1

    catalog = json.loads(CATALOG_PATH.read_text(encoding="utf-8"))
    objects = catalog.get("objects") or []
    if not objects:
        print("Catalog has no objects", file=sys.stderr)
        return 1

    texts: list[str] = []
    meta: list[dict] = []
    for obj in objects:
        text = (obj.get("embeddingText") or "").strip()
        if not text:
            print(f"Skipping {obj.get('id')}: empty embeddingText", file=sys.stderr)
            continue
        texts.append(text)
        meta.append(
            {
                "id": obj["id"],
                "objectName": obj["objectName"],
                "objectType": obj.get("objectType"),
                "domain": obj.get("domain"),
                "embeddingText": text,
            }
        )

    print(f"Encoding {len(texts)} chunks ...")
    vectors, model_name, normalized = encode_texts(texts)

    chunks = []
    for i, m in enumerate(meta):
        vec = vectors[i].astype(float).tolist()
        chunks.append({**m, "embedding": vec, "dimensions": len(vec)})

    payload = {
        "version": "1.0.0",
        "model": model_name,
        "backend": "fastembed-or-sentence-transformers",
        "normalized": normalized,
        "similarity": "cosine (dot product if normalized)",
        "createdAtUtc": datetime.now(timezone.utc).isoformat(),
        "sourceCatalog": CATALOG_PATH.name,
        "catalogVersion": catalog.get("version"),
        "chunkCount": len(chunks),
        "dimensions": int(vectors.shape[1]) if len(chunks) else 0,
        "chunks": chunks,
    }
    OUTPUT_PATH.write_text(json.dumps(payload, indent=2), encoding="utf-8")
    print(f"Wrote {OUTPUT_PATH} ({len(chunks)} chunks, dim={payload['dimensions']})")

    probes = [
        "How many purchase orders are pending approval?",
        "Show pending bill payments for a vendor",
        "What items are on this indent?",
        "How many work orders are pending?",
        "PO amount and company for a purchase order",
    ]
    print("\nRetrieval smoke test (top 3):")
    matrix = np.array([c["embedding"] for c in chunks], dtype=float)

    # Re-embed probes with same backend
    try:
        from fastembed import TextEmbedding

        embedder = TextEmbedding(model_name=FASTEMBED_MODEL)
        q_vecs = np.array(list(embedder.embed(probes)), dtype=float)
        norms = np.linalg.norm(q_vecs, axis=1, keepdims=True)
        norms[norms == 0] = 1.0
        q_vecs = q_vecs / norms
    except Exception:
        from sentence_transformers import SentenceTransformer

        model = SentenceTransformer(ST_MODEL)
        q_vecs = model.encode(probes, normalize_embeddings=True, convert_to_numpy=True)

    for qi, q in enumerate(probes):
        scores = matrix @ q_vecs[qi]
        top = np.argsort(-scores)[:3]
        print(f"\nQ: {q}")
        for rank, idx in enumerate(top, 1):
            c = chunks[int(idx)]
            print(
                f"  {rank}. {c['objectName']:28} domain={c['domain']:8} score={scores[idx]:.4f}"
            )

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
