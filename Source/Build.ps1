param(
    [string]$RimWorldDir,
    [string]$HarmonyPath,
    [string]$CompilerPath,
    [string]$OutputFile,
    [string[]]$ExtraSources = @()
)
$ErrorActionPreference = 'Stop'
$modRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if (!$RimWorldDir) { $RimWorldDir = [IO.Path]::GetFullPath((Join-Path $modRoot '..\..')) }
if (!$HarmonyPath) { $HarmonyPath = [IO.Path]::GetFullPath((Join-Path $RimWorldDir '..\..\workshop\content\294100\2009463077\Current\Assemblies\0Harmony.dll')) }
if (!$CompilerPath) {
    $candidates = @(
        'C:\Program Files\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe',
        'C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\Roslyn\csc.exe',
        (Join-Path $modRoot '..\Rimworld-Multiple-Man-X-Gene\packages\Microsoft.Net.Compilers.Toolset.4.8.0\tasks\net472\csc.exe')
    )
    $CompilerPath = $candidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
}
if (!$CompilerPath -or !(Test-Path -LiteralPath $CompilerPath)) { throw 'Pass -CompilerPath with a current Roslyn csc.exe, or build MarvelUnification.csproj with a .NET SDK.' }
if (!(Test-Path -LiteralPath $HarmonyPath)) { throw 'Harmony DLL missing. Pass -HarmonyPath.' }
if (!$OutputFile) { $OutputFile = Join-Path $modRoot 'Assemblies\MarvelUnification.dll' }
New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($OutputFile)),(Join-Path $PSScriptRoot 'obj') -Force | Out-Null
$managed = Join-Path $RimWorldDir 'RimWorldWin64_Data\Managed'
$referenceNames = @('mscorlib.dll','System.dll','System.Core.dll','System.Xml.dll','System.Xml.Linq.dll','System.Net.Http.dll','netstandard.dll','System.Runtime.dll','Assembly-CSharp.dll')
$references = @($referenceNames | ForEach-Object { Join-Path $managed $_ })
$references += @(Get-ChildItem -LiteralPath $managed -Filter 'UnityEngine*.dll' | Select-Object -ExpandProperty FullName)
$references += $HarmonyPath
$sources = @(Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'Modules'),(Join-Path $PSScriptRoot 'Shared'),(Join-Path $PSScriptRoot 'Properties') -Filter '*.cs' -Recurse | Sort-Object FullName | Select-Object -ExpandProperty FullName)
$sources += $ExtraSources
$argsFile = Join-Path $PSScriptRoot 'obj\build.rsp'
$compilerArgs = @('/nostdlib+','/target:library','/optimize+','/deterministic+','/unsafe+','/langversion:latest','/nowarn:0649,8632',('/out:"'+$OutputFile+'"'))
$compilerArgs += @($references | ForEach-Object { '/reference:"'+$_+'"' })
$compilerArgs += @($sources | ForEach-Object { '"'+$_+'"' })
$compilerArgs | Set-Content -LiteralPath $argsFile -Encoding utf8
& $CompilerPath /noconfig ('@'+$argsFile)
if ($LASTEXITCODE -ne 0) { throw "Compilation failed ($LASTEXITCODE)." }
Write-Output "Built $OutputFile from $($sources.Count) source files."
