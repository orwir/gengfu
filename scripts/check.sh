#!/usr/bin/env bash
# The full quality gate. The pre-push hook and CI both run this file, so they check the same thing.
set -euo pipefail
cd "$(dirname "$0")/.."
export DOTNET_NOLOGO=1 DOTNET_CLI_TELEMETRY_OPTOUT=1

echo "== build"
dotnet build Gengfu.slnx -c Release

# Format reads the code with its types, and TSLib's generated ones exist only once its source
# generator is built: in a fresh clone, as on CI, that takes the build above.
echo "== format"
dotnet format Gengfu.slnx --verify-no-changes --exclude lib

# Every test project runs, each measuring the project it mirrors. Only Gengfu.Core's lines have a
# floor; elsewhere coverage is shown, since the TeamSpeak connection's live half is checked on a
# real server.
for project in tests/*/*.Tests.csproj; do
    covered="$(basename "$project" .Tests.csproj)"
    # Coverage is counted by assembly, and Gengfu.App builds Gengfu.exe.
    assembly="$(dotnet msbuild "src/$covered/$covered.csproj" -getProperty:AssemblyName | tr -d '\r')"
    floor=""
    if [ "$covered" = Gengfu.Core ]; then
        floor="-p:Threshold=80 -p:ThresholdType=line"
    fi
    echo "== test $covered${floor:+ (coverage floor: 80% of its lines)}"
    # shellcheck disable=SC2086
    dotnet test "$project" -c Release --no-build \
        -p:CollectCoverage=true -p:Include="[$assembly]*" $floor
done
