# DME Extractor

CLI and HTTP/FastAPI service for extracting durable medical equipment (DME) mentions from clinical conversations. Uses spaCy, medSpaCy and clinicalBERT Natural Language Processing (NLP) libraries and Domain-specific pre-trained language model along with configurable Regular expressions to extract DME mentions and any other pertinent data.

## Quick start

Prereqs: Python 3.10+

```bash
# From python/
python -m venv .venv
source .venv/bin/activate  # Windows: .venv\\Scripts\\activate
pip install -e .
```

Run the CLI with a sample:

```bash
# From python/src
python -m app.cli
```

Run the CLI with your text:

```bash
# From python/src
python -m app.cli -c "Patient needs a CPAP with full face mask and humidifier."
```

## API service

Start the service (dev):

```bash
# From python/src
python -m uvicorn app.service:app --app-dir src --host 0.0.0.0 --port 8000 --reload
```

POST an extraction request:

```bash
curl -s http://localhost:8000/extract \
	-H "Content-Type: application/json" \
	-d '{"text":"Patient needs a CPAP with full face mask..."}'
```

Response (shape example):

```json
{
	"doctor_name": null,
	"patient_name": null,
	"mentions": []
}
```

Tip: change the port with `--port 8001` and call `http://localhost:8001/extract`.

## Docker

Build and run:

```bash
# From python/
docker build -t dme-extractor:latest .
docker run --rm -p 8000:8000 dme-extractor:latest
```

## Tests

```bash
python -m unittest discover -s tests -p 'test_*.py'
```

## Integrating with the C# agent

Point the agent to this service:

- Set `Agent:NlpExtractorUrl` (or `--nlp-url`) to `http://localhost:8000`.
- The agent posts to `/extract` with `{ "text": "..." }` and forwards results to its Notification API.

## Troubleshooting

- First request may take longer due to model initialization.
- If running locally (not Docker) and spaCy model is missing, install: `python -m spacy download en_core_web_sm`.
