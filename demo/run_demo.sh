#!/usr/bin/env bash
set -euo pipefail

# Demo script: starts the Python extractor service and runs the C# agent with --file input

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PY_DIR="$ROOT_DIR/python"
CS_DIR="$ROOT_DIR/csharp"
NOTE_FILE="$ROOT_DIR/demo/drnote.txt"

if ! command -v python3 >/dev/null 2>&1; then
  echo "python3 is required but not found in PATH" >&2
  exit 1
fi

if ! command -v dotnet >/dev/null 2>&1; then
  echo ".NET SDK (dotnet) is required but not found in PATH" >&2
  exit 1
fi

if [ ! -f "$NOTE_FILE" ]; then
  echo "Missing input file: $NOTE_FILE" >&2
  exit 1
fi

echo "==> Setting up Python virtual environment"
if [ ! -d "$PY_DIR/.venv" ]; then
  python3 -m venv "$PY_DIR/.venv"
fi
source "$PY_DIR/.venv/bin/activate"

echo "==> Installing Python dependencies (editable)"
cd "$PY_DIR"
python -m pip install --upgrade pip >/dev/null
pip install -e .

echo "==> Starting Python extractor (FastAPI/Uvicorn) on http://127.0.0.1:8000"
UVICORN_CMD=(python -m uvicorn app.service:app --app-dir src --host 127.0.0.1 --port 8000)
"${UVICORN_CMD[@]}" &
UVICORN_PID=$!

cleanup() {
  echo "==> Stopping extractor (pid=$UVICORN_PID)"
  kill "$UVICORN_PID" 2>/dev/null || true
}
trap cleanup EXIT

echo -n "==> Waiting for extractor to be ready"
for i in {1..90}; do
  if curl -sSf http://127.0.0.1:8000/docs >/dev/null 2>&1; then
    echo " - ready"
    break
  fi
  echo -n "."
  sleep 0.5
done

echo "==> Running DME Extractor Agent with --file: $NOTE_FILE"
cd "$CS_DIR"
export DOTNET_ENVIRONMENT=Production
dotnet run --project src/DmeExtractorAgent -- --file "$NOTE_FILE"
