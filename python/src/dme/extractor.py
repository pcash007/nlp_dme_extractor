from __future__ import annotations
from typing import Dict, Any, List

import re
from .config import REGEXES, MODIFIERS


def extract_params(sentence_text: str) -> Dict[str, Any]:
    s = sentence_text
    out: Dict[str, Any] = {}

    # duration
    m = REGEXES.get("duration").search(s) if REGEXES.get("duration") else None
    if m:
        try:
            out["duration"] = {"value": int(m.group(1)), "unit": m.group(2).lower()}
        except Exception:
            out["duration"] = m.group(0)

    # hcpcs
    m = REGEXES.get("hcpcs").findall(s) if REGEXES.get("hcpcs") else []
    if m:
        out["hcpcs"] = sorted({c if isinstance(c, str) else c[0] for c in m})

    # quantity
    m = REGEXES.get("quantity").search(s) if REGEXES.get("quantity") else None
    if m:
        out["quantity"] = m.group(1) if m.lastindex else m.group(0)

    # flow
    m = REGEXES.get("flow").search(s) if REGEXES.get("flow") else None
    if m:
        out["flow"] = m.group(0)

    # pressure
    m = REGEXES.get("pressure").search(s) if REGEXES.get("pressure") else None
    if m:
        out["pressure"] = m.group(0)

    # mask_type
    m = REGEXES.get("mask_type").search(s) if REGEXES.get("mask_type") else None
    if m:
        out["mask_type"] = m.group(1) if m.lastindex else m.group(0)

    # reasons
    reasons: List[str] = []
    rx = REGEXES.get("reason")
    if rx:
        for mm in rx.finditer(s):
            reasons.append(mm.group(0))
    if reasons:
        out["reasons"] = reasons

    # modifiers
    mods = [m for m in MODIFIERS if m.lower() in s.lower()]
    if mods:
        out["modifiers"] = mods

    return out
