DME Extractor HTTP Service

Endpoints
- POST /extract
  - Request: { "text": "conversation transcript" }
  - Response: same JSON shape as dme_scimed.extract_dme_json

Run locally
- Install: pip install -e .
- Start server (from repo root):
  - python -m uvicorn app.service:app --app-dir src --host 0.0.0.0 --port 8000

Notes
- First request may take longer due to model initialization.
- Uses the existing dme_scimed pipeline to preserve output.

Docker
- Build: docker build -t dme-extractor:latest .
- Run: docker run --rm -p 8000:8000 dme-extractor:latest

