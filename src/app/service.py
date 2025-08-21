from __future__ import annotations
from fastapi import FastAPI
from pydantic import BaseModel

from dme.core import extract_dme_json
from dme.config import DME_TERMS
from dme.nlp import get_nlp

app = FastAPI(title="DME Extractor Service")


class ExtractRequest(BaseModel):
    text: str


@app.on_event("startup")
async def _startup():
    # Warm up NLP to avoid first-request latency
    get_nlp(DME_TERMS)


@app.post("/extract")
async def extract(req: ExtractRequest):
    return extract_dme_json(req.text)
