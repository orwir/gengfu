# Sourced by the gate and the hooks: sets $DOTNET to a dotnet that has the SDK global.json asks for.
# Order: $DOTNET_ROOT (CI), the per-user location of Microsoft's dotnet-install script, then PATH.

to_unix() {
    if command -v cygpath >/dev/null 2>&1; then cygpath -u "$1"; else printf '%s' "$1"; fi
}

DOTNET=""
for root in "${DOTNET_ROOT:-}" "${LOCALAPPDATA:+$LOCALAPPDATA/Microsoft/dotnet}" "$HOME/.dotnet"; do
    [ -n "$root" ] || continue
    root="$(to_unix "$root")"
    for exe in "$root/dotnet.exe" "$root/dotnet"; do
        if [ -x "$exe" ]; then DOTNET="$exe"; break 2; fi
    done
done
DOTNET="${DOTNET:-dotnet}"
export DOTNET_NOLOGO=1 DOTNET_CLI_TELEMETRY_OPTOUT=1
