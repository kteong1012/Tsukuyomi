python "$PSScriptRoot/dev.py" smoke-ui
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
