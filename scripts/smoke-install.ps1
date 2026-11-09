# Installs a packed release as a player would and checks that Gengfu starts and opens its window.
# release.yml runs it on the CI machine before publishing; it runs the same on a test PC where
# Gengfu isn't installed.
#   scripts/smoke-install.ps1 -Releases <folder holding Gengfu-win-Setup.exe> [-InstallTo <folder>]
param(
    [Parameter(Mandatory)] [string] $Releases,
    [string] $InstallTo = (Join-Path $env:LOCALAPPDATA 'Gengfu')
)
$ErrorActionPreference = 'Stop'

# Setup registers Gengfu for this Windows user whatever folder it installs to, so a run here would
# take over an installed Gengfu's entry in Installed apps and its Start menu shortcut.
if (Test-Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\Gengfu') {
    throw "Gengfu is installed for this Windows user. Run the smoke install where it isn't: CI or a clean test PC."
}

# The installed Gengfu keeps its settings in a folder of its own, never the user's.
$env:GENGFU_HOME = Join-Path ([IO.Path]::GetTempPath()) "gengfu-smoke-$([guid]::NewGuid().ToString('N'))"

$setup = Start-Process (Join-Path $Releases 'Gengfu-win-Setup.exe') -ArgumentList '--silent', '--installto', "`"$InstallTo`"" -PassThru -Wait
if ($setup.ExitCode -ne 0) {
    throw "Setup failed with exit code $($setup.ExitCode)."
}

# Starts Gengfu the way its shortcuts do, waits for its window and closes it as a player would. A
# silent install doesn't start Gengfu; the shortcuts' target hands over to the copy in current\ and
# exits, so that copy is the one to watch. Gengfu writes its settings file on its first save and
# again as it closes, so the file shows which folder it ran on.
function Test-Start([string] $Settings) {
    $current = Join-Path $InstallTo 'current'
    Start-Process (Join-Path $InstallTo 'Gengfu.exe') | Out-Null
    $deadline = (Get-Date).AddSeconds(60)
    do {
        Start-Sleep -Seconds 1
        $app = Get-Process Gengfu -ErrorAction SilentlyContinue |
            Where-Object { $_.Path -like "$current\*" -and $_.MainWindowHandle -ne 0 } |
            Select-Object -First 1
    } until ($app -or (Get-Date) -gt $deadline)
    if (-not $app) {
        throw "Gengfu didn't open its window within a minute."
    }
    Write-Output "Gengfu $((Get-Item $app.Path).VersionInfo.ProductVersion) installed in $InstallTo opened its window, its settings in $Settings."

    $null = $app.CloseMainWindow()
    if (-not $app.WaitForExit(15000)) {
        $app.Kill()
        throw "Gengfu didn't close within 15 s of its window closing."
    }
    if (-not (Test-Path (Join-Path $Settings 'settings.json'))) {
        throw "Gengfu didn't keep its settings in $Settings."
    }
}

Test-Start $env:GENGFU_HOME
Remove-Item $env:GENGFU_HOME -Recurse -Force

# On CI, where nobody's settings live, Gengfu starts once more as a player's copy does: on its usual
# folder and with its updates, which a copy on a folder of its own skips. A crash in that wiring is
# the one failure an update can't repair.
if ($env:GITHUB_ACTIONS -eq 'true') {
    Remove-Item Env:GENGFU_HOME
    Test-Start (Join-Path $env:APPDATA 'Gengfu')
}
