#!/usr/bin/env bash
# Prints a version's CHANGELOG section, the body of its GitHub Release; release.yml runs it.
# A test build without a section of its own, such as 0.1.0-rc.1, takes [Unreleased].
#   scripts/release-notes.sh <version>
set -euo pipefail
cd "$(dirname "$0")/.."
version="${1:?usage: scripts/release-notes.sh <version>}"

section() {
    awk -v heading="## [$1]" '
        index($0, heading) == 1 { found = 1; next }
        found && /^## \[/ { exit }
        found && !started && /^[[:space:]]*$/ { next }
        found { started = 1; print }
    ' CHANGELOG.md
}

notes="$(section "$version")"
if [ -z "${notes//[[:space:]]/}" ] && [[ "$version" == *-* ]]; then
    notes="$(section Unreleased)"
fi
if [ -z "${notes//[[:space:]]/}" ]; then
    echo "CHANGELOG.md has no entries for $version." >&2
    exit 1
fi
printf '%s\n' "$notes"
