# One-time setup after cloning: wire the git hooks.
$ErrorActionPreference = "Stop"
Set-Location (Split-Path $PSScriptRoot -Parent)

git config core.hooksPath .githooks
Write-Host "Git hooks: .githooks (pre-commit: quick checks, pre-push: scripts/check.sh)"
