# Assessment Notes

- IDE - VS Code
- AI assistants - GitHub Copilot
- TODOs - Expand logging and strengthen error handling
- Limitations 
    - Some details may diverge from idiomatic C# and Python (My primary expertise is Java); This prototype prioritized end‑to‑end function over perfect conventions.
    - HTTP invocation and CLI; file upload is not implemented
- Notes
    - The notification endpoint defaults to the echo service at https://httpbin.org/post and can be changed via configuration. 
    - Solution built using AI-assisted workflows
- Jump to the [Quick start](#quick-start) section to get started



# NLP DME Extractor

This repository contains a domain-specific NLP solution that extracts Durable Medical Equipment (DME) mentions and related attributes from clinical text, and posts results to an external notification service.

It is composed of two parts:
- Python service: the NLP DME Extractor (CLI + HTTP API).
- C# agent: a lightweight orchestrator that calls the Python extractor and forwards results to a notification endpoint (CLI + HTTP API).

## Architecture

- Python DME Extractor uses Python-first NLP libraries and targeted regular expressions to extract signals (e.g., physician name, HCPCS codes, patient name, device attributes). Keyword lists and patterns are configurable.
- C# Agent exposes a simple HTTP endpoint (`/process`) and CLI. It calls the Python extractor and then posts the result to a configured Notification API.

High-level flow:
1) Input text → 2) Python extractor → 3) Extracted payload → 4) C# agent posts to Notification API

## Quick start

Prereqs:
- Python 3.10+
- .NET 9 SDK

Python service:
- See `python/README.md` for full details.
- Typical dev run:
    - `cd python` # from repo root folder
    - `python -m venv .poc`
    - `source .poc/bin/activate`  # Windows: .poc\\Scripts\\activate
    - `pip install -e .`
    - `python -m uvicorn app.service:app --app-dir src --host 0.0.0.0 --port 8000 --reload`

C# agent:
- See `csharp/README.md` for full details.
- Typical dev run (CLI mode):
    - `cd csharp` # from repo root folder
	- `dotnet run --project src/DmeExtractorAgent -- "Patient needs a CPAP with full face mask and humidifier. AHI > 20. Ordered by Dr. Cameron."`

- Typical dev run (server mode):
    - `cd csharp` # from repo root folder
	- `dotnet run --project src/DmeExtractorAgent -- --serve`
	- `curl -s http://localhost:5000/process -H 'Content-Type: application/json' -d '{"text":"Patient needs a CPAP with full face mask and humidifier. AHI > 20. Ordered by Dr. Cameron."}'`
    - The output of the curl command will be a simple JSON `{"posted":true}` if the POST to notification URL was successful
    - To see the payload that was returned from the DME Extractor and sent to notification, switch to the terminal where DmeExtractorAgent server is running.


## Usage

- Python CLI:
	- `python -m app.cli -c "Patient needs a CPAP with full face mask and humidifier."`

- Python HTTP API:
	- `POST /extract` with `{ "text": "..." }` to receive extracted data.

- C# Agent CLI (one-off):
	- `dotnet run --project csharp/src/DmeExtractorAgent -- "text here" --nlp-url http://localhost:8000 --notifications-url https://example.com --threshold 0.5`

- C# Agent HTTP API:
	- `POST /process` with `{ "text": "..." }` to trigger extract → notify pipeline.

## Configuration

The agent supports configuration via appsettings, environment variables, or CLI flags.

Required:
- `Agent:NlpExtractorUrl` (CLI: `--nlp-url`)
- `Agent:NotificationUrl` (CLI: `--notifications-url`)

Optional:
- `Agent:Threshold` (CLI: `--threshold`, default: `0.45`)

Environment variable mapping uses `__` to delimit sections, for example:
- `Agent__NlpExtractorUrl=http://localhost:8000`
- `Agent__NotificationUrl=https://example.com`

## Tests

- Python: `python -m unittest discover -s tests -p 'test_*.py'`
- C#: from `csharp/`, `dotnet test DmeExtractorAgent.sln -c Debug`

Tests include unit tests for core logic and integration tests for HTTP endpoints and process-level execution.

## Repository structure

```
python/
	src/app/...         # CLI and FastAPI service
	tests/              # Python unit tests
	README.md

csharp/
	src/DmeExtractorAgent/   # C# agent (EntryPoint, Services, Web)
	tests/DmeExtractorAgent.Tests/
	DmeExtractorAgent.sln
	README.md
```

## Notes

- First request to the Python service may take longer due to model initialization.
- The Python Dockerfile pulls spaCy `en_core_web_sm`; install manually if running locally and missing: `python -m spacy download en_core_web_sm`.

