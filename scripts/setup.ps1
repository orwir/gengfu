# One-time setup after cloning: wire the git hooks and check that a .NET 10 SDK is reachable.
$ErrorActionPreference = "Stop"
Set-Location (Split-Path $PSScriptRoot -Parent)

git config core.hooksPath .githooks
Write-Host "Git hooks: .githooks (pre-commit: quick checks, pre-push: scripts/check.sh)"

$candidates = @(
    $(if ($env:DOTNET_ROOT) { Join-Path $env:DOTNET_ROOT "dotnet.exe" }),
    (Join-Path $env:LOCALAPPDATA "Microsoft\dotnet\dotnet.exe"),
    "dotnet"
) | Where-Object { $_ }

foreach ($dotnet in $candidates) {
    $sdks = & $dotnet --list-sdks 2>$null
    if ($sdks -match "^10\.") {
        Write-Host ".NET 10 SDK: $dotnet"
        exit 0
    }
}
Write-Warning "No .NET 10 SDK found. Install it from https://dotnet.microsoft.com/download/dotnet/10.0"
exit 1
