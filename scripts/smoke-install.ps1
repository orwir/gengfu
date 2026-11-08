# Installs a packed release as a player would and checks that Gengfu starts and opens its window.
# release.yml runs it on the CI machine before publishing; it runs the same on a test PC.
#   scripts/smoke-install.ps1 -Releases <folder holding Gengfu-win-Setup.exe> [-InstallTo <folder>]
param(
    [Parameter(Mandatory)] [string] $Releases,
    [string] $InstallTo = (Join-Path $env:LOCALAPPDATA 'Gengfu')
)
$ErrorActionPreference = 'Stop'

$setup = Start-Process (Join-Path $Releases 'Gengfu-win-Setup.exe') -ArgumentList '--silent', '--installto', "`"$InstallTo`"" -PassThru -Wait
if ($setup.ExitCode -ne 0) {
    throw "Setup failed with exit code $($setup.ExitCode)."
}

# A silent install doesn't start Gengfu; the shortcuts' target does. It hands over to the copy in
# current\ and exits, so that copy is the one to watch.
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
Write-Output "Gengfu $((Get-Item $app.Path).VersionInfo.ProductVersion) installed in $InstallTo and opened its window."

# Closing the window takes the way a player would; Gengfu leaves within a few seconds.
$null = $app.CloseMainWindow()
if (-not $app.WaitForExit(15000)) {
    $app.Kill()
    throw "Gengfu didn't close within 15 s of its window closing."
}
