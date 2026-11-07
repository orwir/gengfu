#!/usr/bin/env bash
# The full quality gate. The pre-push hook and CI both run this file, so they check the same thing.
set -euo pipefail
cd "$(dirname "$0")/.."
. scripts/dotnet.sh

echo "== format"
"$DOTNET" format Gengfu.slnx --verify-no-changes --exclude lib

echo "== build"
"$DOTNET" build Gengfu.slnx -c Release

echo "== test (coverage floor: 80% of Gengfu.Core lines)"
"$DOTNET" test Gengfu.slnx -c Release --no-build \
    -p:CollectCoverage=true -p:Threshold=80 -p:ThresholdType=line -p:Include='[Gengfu.Core]*'
