from __future__ import annotations
from typing import Any, Dict, List, Optional, Tuple

from .config import DME_TERMS
from .extractor import extract_params


_TOK = None
_ENC = None
_DEVICE = None


def _ensure_bert():
    global _TOK, _ENC, _DEVICE
    if _TOK and _ENC and _DEVICE:
        return _TOK, _ENC, _DEVICE
    # Lazy import heavy dependencies
    from transformers import AutoTokenizer, AutoModel
    import torch
    model_id = "emilyalsentzer/Bio_ClinicalBERT"
    _TOK = AutoTokenizer.from_pretrained(model_id)
    _ENC = AutoModel.from_pretrained(model_id, use_safetensors=True).eval()
    _DEVICE = torch.device("cuda" if torch.cuda.is_available() else "cpu")
    _ENC.to(_DEVICE)
    torch.set_grad_enabled(False)
    return _TOK, _ENC, _DEVICE


def _embed(texts: List[str] | str):
    import torch
    tok, enc, device = _ensure_bert()
    if isinstance(texts, str):
        texts = [texts]
    batch = tok(texts, return_tensors="pt", padding=True, truncation=True, max_length=64).to(device)
    h = enc(**batch).last_hidden_state
    mask = batch["attention_mask"].unsqueeze(-1)
    vec = (h * mask).sum(1) / mask.sum(1).clamp(min=1)
    return torch.nn.functional.normalize(vec, dim=1).cpu()


_CANON = [
    "cpap", "bipap", "oxygen concentrator", "nebulizer",
    "wheelchair", "walker", "hospital bed", "brace",
    "commode", "shower chair", "patient lift", "pulse oximeter"
]
_CANON_EMB = None


def _ensure_canon_emb():
    global _CANON_EMB
    if _CANON_EMB is None:
        _CANON_EMB = _embed([f"durable medical equipment: {c}" for c in _CANON])
    return _CANON_EMB


def canonicalize(surface: str, threshold: float = 0.55) -> Tuple[Optional[str], float]:
    v = _embed(surface)
    canon_emb = _ensure_canon_emb()
    import torch as _t
    sims = (v @ canon_emb.T).squeeze(0)
    idx = int(_t.argmax(sims))
    score = float(sims[idx])
    return (_CANON[idx], score) if score >= threshold else (None, score)


_DME_PROTOS = [
    "The text orders or discusses durable medical equipment, supplies, delivery, rental, or repair.",
    "Mentions CPAP, oxygen concentrator, wheelchair, walker, hospital bed or similar home equipment."
]
_NON_DME_PROTOS = ["The text discusses general clinical information such as diagnoses, medications, labs, or vitals."]
_PROTO_EMB = None


def _ensure_proto_emb():
    global _PROTO_EMB
    if _PROTO_EMB is None:
        _PROTO_EMB = _embed(_DME_PROTOS + _NON_DME_PROTOS)
    return _PROTO_EMB


def dme_sentence_score(sentence: str) -> float:
    s = _embed(sentence)
    proto = _ensure_proto_emb()
    import torch as _t
    dme_sim = float((_t.matmul(s, proto[: len(_DME_PROTOS)].T)).max().item())
    non_sim = float((_t.matmul(s, proto[len(_DME_PROTOS) :].T)).max().item())
    return dme_sim - 0.25 * non_sim


def split_turns(text: str):
    import re
    TURN_RE = re.compile(r"^\s*([A-Za-z][A-Za-z .-]{0,30}):\s*(.+)$", re.MULTILINE)
    turns = []
    for m in TURN_RE.finditer(text):
        turns.append({"speaker": m.group(1), "text": m.group(2).strip()})
    if not turns:
        turns = [{"speaker": "Unknown", "text": text.strip()}]
    return turns


def extract_dme_json(conversation_text: str, gate_threshold: float = 0.45) -> Dict[str, Any]:
    # Lazy import NLP so that importing this module doesn't require spaCy/medspaCy
    from .nlp import get_nlp
    nlp = get_nlp(DME_TERMS)
    turns = split_turns(conversation_text)

    # Names (simple heuristic with spaCy PERSONs)
    doc_full = nlp(conversation_text)
    doctor, patient = None, None
    import re
    NAME_DOC_RE = re.compile(r"\bDr\.?\s+([A-Z][a-z]+(?:\s+[A-Z][a-z]+)*)")
    if (m := NAME_DOC_RE.search(conversation_text)):
        doctor = f"Dr. {m.group(1)}"
    if patient is None:
        for ent in doc_full.ents:
            if ent.label_ == "PERSON":
                left = doc_full[max(ent.start - 2, 0) : ent.start].text
                if "Dr" not in left:
                    patient = ent.text
                    break

    mentions: List[Dict[str, Any]] = []
    for idx, turn in enumerate(turns):
        doc = nlp(turn["text"])
        for sent in doc.sents:
            score = dme_sentence_score(sent.text)
            likely_dme = score >= gate_threshold
            has_dme_entity = any(ent.label_ == "DME" for ent in sent.ents)
            if not (likely_dme or has_dme_entity):
                continue

            for ent in sent.ents:
                if ent.label_ != "DME":
                    continue
                attrs = {
                    "negated": bool(getattr(ent._, "is_negated", False)),
                    "uncertain": bool(getattr(ent._, "is_uncertain", False)),
                    "historical": bool(getattr(ent._, "is_historical", False)),
                    "family": bool(getattr(ent._, "is_family", False)),
                }
                canon, sem = canonicalize(ent.text)
                extra = extract_params(sent.text)
                mentions.append(
                    {
                        "equipment": canon or ent.text.lower(),
                        "surface": ent.text,
                        "semantic_score": round(sem, 3),
                        **attrs,
                        "reason": extra.get("reasons", []),
                        "parameters": {k: v for k, v in extra.items() if k != "reasons"},
                        "speaker": turn["speaker"],
                        "turn_index": idx,
                        "evidence": sent.text,
                    }
                )

    out, seen = [], set()
    for m in mentions:
        key = (m["turn_index"], m["equipment"], m["surface"].lower(), m["evidence"]) 
        if key in seen:
            continue
        seen.add(key)
        out.append(m)

    return {"doctor_name": doctor, "patient_name": patient, "mentions": out}
