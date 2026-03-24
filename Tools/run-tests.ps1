python "$PSScriptRoot/dev.py" run-tests
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
