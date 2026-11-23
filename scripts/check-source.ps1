[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$hideItProject = Split-Path -Parent $PSScriptRoot

# PowerShell 7 ships Roslyn. This is syntax validation, not a Unity compilation.
Add-Type -TypeDefinition 'public static class HideItSourceValidationMarker {}'
$hideItErrors = @()
$hideItSources = Get-ChildItem -LiteralPath (Join-Path $hideItProject 'Assets') -Recurse -Filter '*.cs'
foreach ($hideItSource in $hideItSources) {
    $hideItTree = [Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText(
        [string](Get-Content -LiteralPath $hideItSource.FullName -Raw),
        [Microsoft.CodeAnalysis.CSharp.CSharpParseOptions]::Default,
        $hideItSource.FullName,
        [System.Text.Encoding]::UTF8,
        [System.Threading.CancellationToken]::None
    )
    $hideItErrors += @($hideItTree.GetDiagnostics() | Where-Object { $_.Severity -eq 'Error' })
}
foreach ($hideItJson in (Get-ChildItem -LiteralPath (Join-Path $hideItProject 'Assets'),(Join-Path $hideItProject 'Packages') -Recurse -File |
        Where-Object { $_.Extension -in @('.json', '.asmdef') })) {
    Get-Content -LiteralPath $hideItJson.FullName -Raw | ConvertFrom-Json | Out-Null
}
foreach ($hideItScript in (Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.ps1')) {
    $hideItTokens = $null
    $hideItScriptErrors = $null
    [System.Management.Automation.Language.Parser]::ParseFile($hideItScript.FullName, [ref]$hideItTokens, [ref]$hideItScriptErrors) | Out-Null
    $hideItErrors += @($hideItScriptErrors)
}
if ($hideItErrors.Count -gt 0) {
    $hideItErrors | ForEach-Object { Write-Error $_.ToString() -ErrorAction Continue }
    exit 1
}
Write-Host "PASS: $($hideItSources.Count) C# files parsed; JSON and PowerShell syntax valid."
Write-Host 'Unity compilation and physics tests must still be run separately.'
