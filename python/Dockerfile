# DME Extractor Service - Dockerfile
# Installs from pyproject.toml (PEP 621). No requirements.txt used.

FROM python:3.11-slim

ENV PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1

WORKDIR /app

# System deps often needed by spaCy/medspacy/scispacy/torch wheels
RUN apt-get update && apt-get install -y --no-install-recommends \
    build-essential gcc \
 && rm -rf /var/lib/apt/lists/*

# Copy project metadata and only the necessary source code
COPY pyproject.toml ./
COPY src/dme ./src/dme
COPY src/app ./src/app
COPY src/__init__.py ./src/__init__.py

# Install project and dependencies
RUN pip install --no-cache-dir --upgrade pip \
 && pip install --no-cache-dir .

# Ensure the English model is available for spaCy
RUN python -m spacy download en_core_web_sm || true

# Expose service port
EXPOSE 8000

# Run the FastAPI app via uvicorn from the src directory
WORKDIR /app/src
CMD ["python", "-m", "uvicorn", "app.service:app", "--host", "0.0.0.0", "--port", "8000"]
