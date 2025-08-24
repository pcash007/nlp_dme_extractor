# DmeExtractorAgent (C#)

A .NET 9 agent that:
- Calls the Python DME Extractor service
- Posts the result to a Notification API
- Runs either as a CLI (one-off) or an HTTP server exposing `/process`

## Layout
- `src/DmeExtractorAgent`: app source (EntryPoint + minimal API)
- `tests/DmeExtractorAgent.Tests`: xUnit tests
- `DmeExtractorAgent.sln`: solution at `csharp/`

## Prerequisites
- .NET 9 SDK
- Python DME Extractor running (for real calls), e.g. `docker run -p 8000:8000 <image>`

## Build
```bash
# From csharp/
dotnet build DmeExtractorAgent.sln -c Debug
```

## Test
```bash
# From csharp/
dotnet test DmeExtractorAgent.sln -c Debug
```

## Run (one-off CLI)
```bash
# From csharp/
DOTNET_ENVIRONMENT=Production \
dotnet run --project src/DmeExtractorAgent -- \
	"Doctor: Order CPAP (E0601) for 3 months due to OSA." \
	--threshold 0.5 \
	--nlp-url http://localhost:8000 \
	--notifications-url https://example.com
```

### Run (one-off CLI) with file input
```bash
# From csharp/
DOTNET_ENVIRONMENT=Production \
dotnet run --project src/DmeExtractorAgent -- \
	--file "/absolute/path/to/note.txt" \
	--nlp-url http://localhost:8000 \
	--notifications-url https://example.com
```

Notes:
- When `--file` is provided alongside CLI text, the file contents take precedence and the free-text argument is ignored.

## Run (HTTP server)
```bash
# From csharp/
DOTNET_ENVIRONMENT=Production \
dotnet run --project src/DmeExtractorAgent --serve 

# Then POST (default Kestrel http port 5000)
curl -s http://localhost:5000/process \
	-H 'Content-Type: application/json' \
	-d '{"text":"Doctor: Order CPAP (E0601) for 3 months due to OSA."}'
```

## Configuration
You can set via CLI flags or configuration providers (e.g., `appsettings.json`, environment variables):
- `Agent:NlpExtractorUrl` (required)
- `Agent:NotificationUrl` (required)
- `Agent:Threshold` (optional, default: `0.45`)

Logging: Serilog writes to console in both modes.
