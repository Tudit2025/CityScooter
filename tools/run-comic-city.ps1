param(
    [ValidateSet('Build','PlayMode','EditMode')][string]$Mode = 'Build',
    [string]$Unity = 'C:\Program Files\Unity\Hub\Editor\6000.3.28f1\Editor\Unity.exe'
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$scratchRoot = Join-Path $projectRoot 'Temp\ComicCityValidation'
New-Item -ItemType Directory -Force -Path $scratchRoot | Out-Null
foreach ($folder in @('Assets','Packages','ProjectSettings')) {
    & robocopy (Join-Path $projectRoot $folder) (Join-Path $scratchRoot $folder) /E /R:1 /W:1 /NFL /NDL /NJH /NJS /NP | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "Copy failed: $folder" }
}
# No Library/Temp sharing: an open editor keeps its scene and import database untouched.
$logFile = Join-Path $projectRoot "Logs\comic-city-$Mode.log"
$argsList = @('-batchmode','-projectPath',('"'+$scratchRoot+'"'),'-logFile',('"'+$logFile+'"'))
if ($Mode -eq 'Build') {
    $argsList += @('-executeMethod','CityScooter.EditorTools.Art.ComicCityBuilder.BuildBatch')
} else {
    $results = Join-Path $projectRoot "Logs\comic-city-$Mode-results.xml"
    $argsList += @('-runTests','-testPlatform',$Mode,'-testResults',('"'+$results+'"'))
}
$unityJob = Start-Process -FilePath $Unity -ArgumentList $argsList -WindowStyle Hidden -PassThru
$unityJob.WaitForExit()
if ($unityJob.ExitCode -ne 0) { throw "Unity failed ($($unityJob.ExitCode)). See $logFile" }
if ($Mode -eq 'Build') {
    # Only generated kit outputs, configured scene files and Unity-created .meta files return to the workspace.
    foreach ($folder in @('Assets\_Project\Art\ComicCity','Assets\_Project\Prefabs\ComicCity','Assets\_Project\Audio\ComicCity','art-src\comic-city','audio-src\comic-city')) {
        & robocopy (Join-Path $scratchRoot $folder) (Join-Path $projectRoot $folder) /E /R:1 /W:1 /NFL /NDL /NJH /NJS /NP | Out-Null
        if ($LASTEXITCODE -ge 8) { throw "Output copy failed: $folder" }
    }
    foreach ($relative in @('Assets\_Project\Art\ComicCity.meta','Assets\_Project\Prefabs\ComicCity.meta',
        'Assets\_Project\Data\Theme_ComicDownhill.asset','Assets\_Project\Data\Theme_ComicDownhill.asset.meta',
        'Assets\_Project\Data\Tuning_ComicDownhill.asset','Assets\_Project\Data\Tuning_ComicDownhill.asset.meta',
        'Assets\_Project\Scenes\Main.unity','Assets\_Project\Scenes\ComicCityPreview.unity','Assets\_Project\Scenes\ComicCityPreview.unity.meta',
        'Assets\_Project\Scenes\Main_PreComicPrototype.unity','Assets\_Project\Scenes\Main_PreComicPrototype.unity.meta')) {
        Copy-Item -LiteralPath (Join-Path $scratchRoot $relative) -Destination (Join-Path $projectRoot $relative) -Force
    }
    Get-ChildItem -LiteralPath (Join-Path $scratchRoot 'Assets') -Recurse -Filter '*.meta' | ForEach-Object {
        $relative = $_.FullName.Substring($scratchRoot.Length+1)
        $destination = Join-Path $projectRoot $relative
        if (-not (Test-Path -LiteralPath $destination) -and (Test-Path -LiteralPath $destination.Substring(0,$destination.Length-5))) {
            Copy-Item -LiteralPath $_.FullName -Destination $destination
        }
    }
}
if ($Mode -eq 'PlayMode') {
    foreach ($checkpoint in @('art-src\comic-city\previews\07-runtime-after-recycle.png','art-src\comic-city\previews\09-sidewalk-right.png','art-src\comic-city\previews\10-sidewalk-left.png','audio-src\comic-city\previews\Audio_Controls.png')) {
        $checkpointSource = Join-Path $scratchRoot $checkpoint
        if (Test-Path -LiteralPath $checkpointSource) {
            Copy-Item -LiteralPath $checkpointSource -Destination (Join-Path $projectRoot $checkpoint) -Force
        }
    }
}
Write-Output "Comic City $Mode completed. Log: $logFile"
