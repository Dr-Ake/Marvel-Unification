param([string]$OutputFile)
$ErrorActionPreference='Stop'
$modRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if(!$OutputFile){$OutputFile=Join-Path $modRoot 'Releases\Marvel-Unification.zip'}
New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($OutputFile)) -Force | Out-Null
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$relativeFiles=@('README.md','NOTICE.md','LICENSE','.gitignore','Source\Build.ps1','Source\Package.ps1','Source\MarvelUnification.csproj')
$folders=@('About','Assemblies','Defs','Languages','Patches','Sounds','Textures','Source\Modules','Source\Shared','Source\Properties')
foreach($folder in $folders){
    $base=Join-Path $modRoot $folder
    if(Test-Path -LiteralPath $base){$relativeFiles+=@(Get-ChildItem -LiteralPath $base -Recurse -File | ForEach-Object {$_.FullName.Substring($modRoot.Length+1)})}
}
$stream=[IO.File]::Open($OutputFile,[IO.FileMode]::Create,[IO.FileAccess]::ReadWrite)
$archive=[IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Create,$false)
try {
    foreach($relativeFile in ($relativeFiles | Sort-Object -Unique)){
        $path=Join-Path $modRoot $relativeFile
        [void][IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive,$path,('Marvel-Unification/'+$relativeFile.Replace('\','/')),[IO.Compression.CompressionLevel]::Optimal)
    }
} finally {$archive.Dispose();$stream.Dispose()}
Write-Output "Created $OutputFile"
