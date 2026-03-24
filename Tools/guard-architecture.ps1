python "$PSScriptRoot/dev.py" guard-architecture
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
