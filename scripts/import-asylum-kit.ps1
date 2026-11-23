param([Parameter(Mandatory)][string]$Archive)

$ErrorActionPreference = 'Stop'
$project = Split-Path -Parent $PSScriptRoot
$root = Join-Path $project 'Assets/HideIT/Resources/AsylumKit'
$zip = [IO.Compression.ZipFile]::OpenRead((Resolve-Path -LiteralPath $Archive).Path)
try {
    foreach ($folder in @('Models','Textures','Shaders')) {
        New-Item -Path (Join-Path $root $folder) -ItemType Directory -Force | Out-Null
    }
    $count = 0
    foreach ($entry in $zip.Entries) {
        $folder = switch -Regex ($entry.FullName) {
            '/models/fbx_unity/[^/]+\.fbx$' { 'Models'; break }
            '/textures/PSX_Asylum_Kit_atlas\.png$' { 'Textures'; break }
            '/engines/unity/PSX_Lit\.shader$' { 'Shaders'; break }
        }
        if (!$folder) { continue }
        [IO.Compression.ZipFileExtensions]::ExtractToFile($entry,(Join-Path (Join-Path $root $folder) $entry.Name),$true)
        $count++
    }
    if ($count -lt 24) { throw 'The archive is missing the free Asylum Kit models, atlas, or lit shader.' }
    Write-Output "Imported $count local Asylum Kit files."
} finally { $zip.Dispose() }
