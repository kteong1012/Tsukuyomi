param(
    [switch]$Check
)

$args = @("$PSScriptRoot/dev.py", "generate-config")
if ($Check) {
    $args += "--check"
}

python @args
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
