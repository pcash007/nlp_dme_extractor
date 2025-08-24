# PowerShell demo: start Python extractor and run C# agent with --file input
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$ROOT_DIR = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$PY_DIR   = Join-Path $ROOT_DIR 'python'
$CS_DIR   = Join-Path $ROOT_DIR 'csharp'
$NOTE_FILE = Join-Path $ROOT_DIR 'drnote.txt'

function Find-Python {
  $cands = @('py','python','python3')
  foreach ($c in $cands) {
    $cmd = Get-Command $c -ErrorAction SilentlyContinue
    if ($cmd) { return $c }
  }
  throw 'Python 3 is required but was not found in PATH.'
}

if (-not (Test-Path $NOTE_FILE)) { throw "Missing input file: $NOTE_FILE" }

$python = Find-Python
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
  throw '.NET SDK (dotnet) is required but was not found in PATH.'
}

Write-Host '==> Setting up Python virtual environment'
$venvPath = Join-Path $PY_DIR '.venv'
if (-not (Test-Path $venvPath)) { & $python -m venv $venvPath }
$venvPython = Join-Path $venvPath 'Scripts\python.exe'
if (-not (Test-Path $venvPython)) { throw "Venv python not found at $venvPython" }

Write-Host '==> Installing Python dependencies (editable)'
Push-Location $PY_DIR
& $venvPython -m pip install --upgrade pip | Out-Null
& $venvPython -m pip install -e .
Pop-Location

Write-Host '==> Starting Python extractor (Uvicorn) on http://127.0.0.1:8000'
$uvArgs = @('-m','uvicorn','app.service:app','--app-dir','src','--host','127.0.0.1','--port','8000')
$uvProc = Start-Process -FilePath $venvPython -ArgumentList $uvArgs -PassThru -WindowStyle Hidden

try {
  Write-Host -NoNewline '==> Waiting for extractor to be ready'
  $ready = $false
  for ($i=0; $i -lt 60; $i++) {
    try {
      $resp = Invoke-WebRequest -UseBasicParsing -Uri 'http://127.0.0.1:8000/docs' -TimeoutSec 2
      if ($resp.StatusCode -ge 200 -and $resp.StatusCode -lt 500) { $ready = $true; break }
    } catch {
      Start-Sleep -Milliseconds 500
    }
    Write-Host -NoNewline '.'
  }
  Write-Host ''
  if (-not $ready) { throw 'Extractor did not become ready in time.' }

  Write-Host "==> Running DME Extractor Agent with --file: $NOTE_FILE"
  $env:DOTNET_ENVIRONMENT = 'Production'
  Push-Location $CS_DIR
  & dotnet run --project 'src/DmeExtractorAgent' -- --file $NOTE_FILE
  Pop-Location
}
finally {
  if ($uvProc -and -not $uvProc.HasExited) {
    Write-Host "==> Stopping extractor (pid=$($uvProc.Id))"
    Stop-Process -Id $uvProc.Id -Force -ErrorAction SilentlyContinue
  }
}
