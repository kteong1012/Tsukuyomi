python "$PSScriptRoot/dev.py" validate-config
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
