[CmdletBinding()]
param(
    [ValidateSet('Open', 'Test', 'Build', 'ImportArt')][string]$Action = 'Open',
    [string]$Editor = $env:HIDEIT_UNITY_EDITOR
)

$ErrorActionPreference = 'Stop'
$hideItProject = Split-Path -Parent $PSScriptRoot
$hideItVersion = '6000.6.10f1'
$hideItCandidates = @(
    $Editor,
    (Join-Path $hideItProject '.tools\Unity\Editor\Unity.exe'),
    "C:\Program Files\Unity\Hub\Editor\$hideItVersion\Editor\Unity.exe",
    "D:\Program Files\Unity\Hub\Editor\$hideItVersion\Editor\Unity.exe"
)
$hideItEditor = $hideItCandidates | Where-Object { $_ -and (Test-Path -LiteralPath $_ -PathType Leaf) } | Select-Object -First 1
if (!$hideItEditor) {
    Write-Host "Unity $hideItVersion is required. Install it using Unity Hub, activate your Unity Personal license, then run this again."
    Write-Host 'A custom Editor location can be supplied with -Editor or HIDEIT_UNITY_EDITOR.'
    exit 2
}

$hideItArgs = @('-projectPath', $hideItProject)
if ($Action -eq 'Open') {
    Start-Process -FilePath $hideItEditor -ArgumentList @('-projectPath', ('"' + $hideItProject + '"'))
    exit 0
}

$hideItResults = Join-Path $hideItProject 'TestResults'
New-Item -ItemType Directory -Path $hideItResults -Force | Out-Null
$hideItArgs += @('-batchmode', '-logFile', (Join-Path $hideItResults ($Action.ToLowerInvariant() + '.log')))
if ($Action -eq 'Test') {
    # PlayMode physics tests need the graphics device for the actual Standard-shader scene.
    $hideItArgs += @('-runTests', '-testPlatform', 'PlayMode', '-testResults', (Join-Path $hideItResults 'playmode.xml'))
} elseif ($Action -eq 'ImportArt') {
    $hideItArgs += @('-quit', '-executeMethod', 'HideIT.Editor.ArtPrototypeImporter.BuildCatalog')
} else {
    $hideItArgs += @('-quit', '-executeMethod', 'HideIT.Editor.ProjectSetup.BuildWindows', '-buildTarget', 'Win64')
}
# Wait for the editor itself. Start-Process -Wait also waits for child processes,
# which may outlive the editor during licensing or crash-handler initialization.
$hideItQuotedArgs = $hideItArgs | ForEach-Object { '"' + $_ + '"' }
$hideItProcess = Start-Process -FilePath $hideItEditor -ArgumentList $hideItQuotedArgs -PassThru -WindowStyle Hidden
$hideItProcess.WaitForExit()
$hideItCode = $hideItProcess.ExitCode
if ($hideItCode -ne 0) { throw "Unity $Action failed ($hideItCode). See TestResults." }
if ($Action -eq 'Test') {
    $hideItReport = Join-Path $hideItResults 'playmode.xml'
    if (!(Test-Path -LiteralPath $hideItReport)) { throw 'Unity did not produce the test report.' }
    [xml]$hideItXml = Get-Content -LiteralPath $hideItReport -Raw
    if ($hideItXml.'test-run'.result -ne 'Passed') { throw 'Physics tests failed. See TestResults/playmode.xml.' }
}
Write-Host "Hide iT $Action completed."
