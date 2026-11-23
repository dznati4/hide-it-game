[CmdletBinding()]
param([switch]$Lab)

$ErrorActionPreference = 'Stop'
$hideItProject = Split-Path -Parent $PSScriptRoot
$hideItSettings = Get-Content -LiteralPath (Join-Path $hideItProject 'ProjectSettings\ProjectSettings.asset') -Raw
$hideItVersion = [regex]::Match($hideItSettings, '(?m)^  bundleVersion: ([\d.]+)\s*$').Groups[1].Value
if (!$hideItVersion) { throw 'Could not read the build version.' }
$hideItExecutable = Join-Path $hideItProject "Builds\Playtest-$hideItVersion\Hide iT.exe"
if (!(Test-Path -LiteralPath $hideItExecutable)) { throw 'Build the Windows player first.' }
$hideItOutput = Join-Path $hideItProject $(if ($Lab) { 'TestResults\smoke-lab' } else { 'TestResults\smoke' })
New-Item -ItemType Directory -Path $hideItOutput -Force | Out-Null
$hideItResult = Join-Path $hideItOutput 'result.txt'
if (Test-Path -LiteralPath $hideItResult) { Remove-Item -LiteralPath $hideItResult }
$hideItArguments = @(
    '-force-d3d11', '-screen-fullscreen', '0', '-screen-width', '1280', '-screen-height', '720',
    '-hideit-smoke', '-hideit-output', ('"' + $hideItOutput + '"'),
    '-logFile', ('"' + (Join-Path $hideItOutput 'player.log') + '"')
)
if ($Lab) { $hideItArguments += '-hideit-lab' }
# Run rendered: Unity batch mode intentionally produces black screenshots.
# The automated mode leaves the cursor alone and ignores desktop keyboard input.
$hideItProcess = Start-Process -FilePath $hideItExecutable -ArgumentList $hideItArguments -PassThru
if (!$hideItProcess.WaitForExit(60000)) {
    $hideItProcess.Kill()
    throw 'The standalone acceptance run exceeded 60 seconds.'
}
if ($hideItProcess.ExitCode -ne 0 -or !(Test-Path -LiteralPath $hideItResult)) {
    if (Test-Path -LiteralPath $hideItResult) { Get-Content -LiteralPath $hideItResult | Write-Output }
    throw 'Standalone acceptance failed. See TestResults/smoke/player.log.'
}
$hideItOutcome = Get-Content -LiteralPath $hideItResult -Raw
if (!$hideItOutcome.StartsWith('PASS:')) { throw $hideItOutcome }
Write-Output $hideItOutcome
