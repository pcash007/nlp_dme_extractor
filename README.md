# DME Extractor

CLI and FastAPI service for extracting durable medical equipment (DME) mentions from clinical conversations.

## Install

- Python 3.10+
- Install package and dependencies from `pyproject.toml`:

```bash
pip install -e .
```

## CLI

- Use demo conversation:

```bash
python -m app.cli
```

- Provide your own conversation:

```bash
python -m app.cli -c "Patient needs a CPAP with full face mask and humidifier."
```

## Service

- Run locally:

```bash
python -m uvicorn app.service:app --app-dir src --host 0.0.0.0 --port 8000
```

- Request:

```bash
curl -s http://localhost:8000/extract -H "Content-Type: application/json" -d '{"text":"Patient needs a CPAP with full face mask..."}'
```

## Docker

- Build image (installs from `pyproject.toml`, no requirements.txt needed):

```bash
docker build -t dme-extractor:latest .
```

- Run container:

```bash
docker run --rm -p 8000:8000 dme-extractor:latest
```

## Tests

- Run unit tests:

```bash
python -m unittest discover -s tests -p 'test_*.py'
```

## Notes

- First request may take longer due to model initialization.
- spaCy English model is pulled in the Dockerfile (`en_core_web_sm`).
