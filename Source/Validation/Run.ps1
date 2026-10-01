param([ValidateSet('biotech','all-dlc','reload')][string]$Profile = 'biotech', [ValidateSet('full','remaining','duplication')][string]$Selection = 'full')
$ErrorActionPreference='Stop'
$modRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$gameRoot=[IO.Path]::GetFullPath((Join-Path $modRoot '..\..'))
$testRoot=Join-Path $modRoot ('Source\obj\Validation-'+$Profile+$(if($Selection -eq 'duplication'){'-duplication'}else{''}))
$dataRoot=Join-Path $testRoot 'game-data'
$production=Join-Path $modRoot 'Assemblies\MarvelUnification.dll'
$backup=Join-Path $testRoot 'MarvelUnification.production.dll'
if(Get-Process RimWorldWin64 -ErrorAction SilentlyContinue){throw 'An existing RimWorld session is running.'}
New-Item -ItemType Directory -Path (Join-Path $dataRoot 'Config') -Force | Out-Null
$resultPath=Join-Path $testRoot 'results.txt'
if(Test-Path -LiteralPath $resultPath){Remove-Item -LiteralPath $resultPath}
$dlcs='<li>ludeon.rimworld.biotech</li>'
if($Profile -in @('all-dlc','reload')){$dlcs='<li>ludeon.rimworld.royalty</li><li>ludeon.rimworld.ideology</li><li>ludeon.rimworld.biotech</li><li>ludeon.rimworld.anomaly</li><li>ludeon.rimworld.odyssey</li>'}
"<?xml version=`"1.0`" encoding=`"utf-8`"?><ModsConfigData><version>1.6.4871</version><activeMods><li>brrainz.harmony</li><li>ludeon.rimworld</li>$dlcs<li>drake.marvelunification</li><li>brrainz.rimbridgeserver</li></activeMods><knownExpansions><li>ludeon.rimworld.royalty</li><li>ludeon.rimworld.ideology</li><li>ludeon.rimworld.biotech</li><li>ludeon.rimworld.anomaly</li><li>ludeon.rimworld.odyssey</li></knownExpansions></ModsConfigData>" | Set-Content -LiteralPath (Join-Path $dataRoot 'Config\ModsConfig.xml') -Encoding utf8
$normalRoot=Join-Path $env:USERPROFILE 'AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios'
function Get-ProtectedFiles {
    $table=@{}
    Get-ChildItem -LiteralPath (Join-Path $normalRoot 'Config'),(Join-Path $normalRoot 'Saves') -File -Recurse -ErrorAction SilentlyContinue | ForEach-Object { $table[$_.FullName]=(Get-FileHash -LiteralPath $_.FullName).Hash }
    return $table
}
$protected=Get-ProtectedFiles
$owned=$null
Copy-Item -LiteralPath $production -Destination $backup
try {
    & (Join-Path $PSScriptRoot '..\Build.ps1') -OutputFile (Join-Path $testRoot 'TestBuild\MarvelUnification.dll') -ExtraSources @((Join-Path $PSScriptRoot 'LiveSuite.cs'))
    Copy-Item -LiteralPath (Join-Path $testRoot 'TestBuild\MarvelUnification.dll') -Destination $production
    $arguments=@('-quicktest',('-savedatafolder='+$dataRoot),('-validation-profile='+$Profile),('-validation-selection='+$Selection),'-logFile',(Join-Path $testRoot 'Player.log'),'-screen-fullscreen','0','-screen-width','1600','-screen-height','900')
    if($Profile -eq 'reload') {
        Copy-Item -LiteralPath (Join-Path $modRoot 'Source\obj\Validation-all-dlc\game-data\Saves') -Destination $dataRoot -Recurse -Force
        Copy-Item -LiteralPath (Join-Path $modRoot 'Source\obj\Validation-all-dlc\StateExpected.tsv') -Destination $testRoot -Force
        $arguments[0]='-quicktest'
    }
    $owned=Start-Process -FilePath (Join-Path $gameRoot 'RimWorldWin64.exe') -ArgumentList $arguments -WindowStyle Hidden -PassThru
    $owned.Id | Set-Content -LiteralPath (Join-Path $testRoot 'pid.txt')
    $deadline=[DateTime]::UtcNow.AddSeconds(600)
    while(!$owned.WaitForExit(1000)){if([DateTime]::UtcNow -gt $deadline){throw 'The isolated test exceeded its ten-minute startup/session bound.'}}
} finally {
    if($owned -and !$owned.HasExited){Stop-Process -Id $owned.Id -Force}
    Copy-Item -LiteralPath $backup -Destination $production
    $after=Get-ProtectedFiles
    $changed=@($protected.Keys | Where-Object { !$after.ContainsKey($_) -or $after[$_] -ne $protected[$_] })
    $added=@($after.Keys | Where-Object { !$protected.ContainsKey($_) })
    @{ProductionDllRestored=((Get-FileHash -LiteralPath $production).Hash -eq (Get-FileHash -LiteralPath $backup).Hash);NormalDataUnchanged=($changed.Count -eq 0 -and $added.Count -eq 0);Changed=$changed;Added=$added} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $testRoot 'ProtectionReport.json')
}
if(!(Test-Path -LiteralPath $resultPath)){throw 'The isolated session did not produce a result file.'}
$results=Get-Content -LiteralPath $resultPath
$failures=@($results | Where-Object {$_ -like 'FAIL *'})
if($failures.Count -gt 0){$failures | Write-Output;throw "Validation failed; see $resultPath"}
if(!($results | Where-Object {$_ -like '*SUITE COMPLETE'})){throw 'The isolated suite did not finish.'}
Write-Output "Validation passed: $(@($results | Where-Object {$_ -like 'PASS *'}).Count) checks. Results: $resultPath"
