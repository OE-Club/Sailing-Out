param(
    [string]$UnityPath
)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
$projectVersion = Join-Path $taskRoot 'ProjectSettings\ProjectVersion.txt'
$versionText = Get-Content -LiteralPath $projectVersion -Raw
if ($versionText -notmatch 'm_EditorVersionWithRevision: ([^ ]+) \(([^)]+)\)') {
    throw "Cannot read the pinned Unity editor version from $projectVersion."
}
$expectedProductVersion = "$($Matches[1])_$($Matches[2])"
if (-not $UnityPath) {
    $UnityPath = Join-Path 'C:\Program Files\Unity\Hub\Editor' "$($Matches[1])\Editor\Unity.exe"
}
if (-not (Test-Path -LiteralPath $UnityPath -PathType Leaf)) {
    throw "The pinned Unity editor is missing: $UnityPath"
}
$actualProductVersion = (Get-Item -LiteralPath $UnityPath).VersionInfo.ProductVersion
if ($actualProductVersion -ne $expectedProductVersion) {
    throw "Unity editor mismatch: expected $expectedProductVersion, found $actualProductVersion."
}
$deliveryRoot = Join-Path $taskRoot 'Delivery'
$logRoot = Join-Path $taskRoot 'Logs'
# Keep isolated projects outside the source project's Temp folder: Unity clears
# that folder whenever another editor session opens the source project.
$stagingRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('sailingout-delivery-' + [guid]::NewGuid().ToString('N'))
$cleanRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('sailingout-clean-import-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $deliveryRoot, $logRoot, $stagingRoot, $cleanRoot | Out-Null

function Invoke-UnityDelivery([string[]]$Arguments, [string]$LogName) {
    $allArguments = @('-batchmode', '-nographics', '-quit') + $Arguments + @('-logFile', (Join-Path $logRoot $LogName))
    $quotedArguments = $allArguments | ForEach-Object { '"' + $_ + '"' }
    $process = Start-Process -FilePath $UnityPath -ArgumentList $quotedArguments -PassThru -WindowStyle Hidden
    $process.WaitForExit()
    if ($process.ExitCode -ne 0) { throw "Unity delivery step failed ($LogName), exit $($process.ExitCode)." }
}

foreach ($project in @($stagingRoot, $cleanRoot)) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'DeliveryTemplate\Packages') -Destination $project -Recurse
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'DeliveryTemplate\ProjectSettings') -Destination $project -Recurse
    Copy-Item -LiteralPath $projectVersion -Destination (Join-Path $project 'ProjectSettings\ProjectVersion.txt') -Force
    New-Item -ItemType Directory -Force -Path (Join-Path $project 'Assets') | Out-Null
}
Copy-Item -LiteralPath (Join-Path $taskRoot 'Assets\SailingOut') -Destination (Join-Path $stagingRoot 'Assets') -Recurse
Copy-Item -LiteralPath (Join-Path $taskRoot 'Assets\SailingOut.meta') -Destination (Join-Path $stagingRoot 'Assets')
$moduleRoot = Join-Path $stagingRoot 'Assets\SailingOut\Module'
New-Item -ItemType Directory -Force -Path $moduleRoot | Out-Null
foreach ($folder in @('Runtime','Editor')) {
    Copy-Item -LiteralPath (Join-Path $taskRoot "Packages\com.sailingout.art-generation\$folder") -Destination $moduleRoot -Recurse
    Copy-Item -LiteralPath (Join-Path $taskRoot "Packages\com.sailingout.art-generation\$folder.meta") -Destination $moduleRoot
}
$env:SAILINGOUT_EXPORT_PATH = Join-Path $deliveryRoot 'SailingOut-ArtGeneration.unitypackage'
$env:SAILINGOUT_BUILD_PATH = Join-Path $taskRoot 'Builds\SailingOutDemo\SailingOutDemo.exe'
try {
    Invoke-UnityDelivery -Arguments @('-projectPath', $stagingRoot, '-executeMethod', 'SailingOut.Editor.DeliveryBuilder.ExportStandaloneAssets') -LogName 'delivery-export.log'
    Invoke-UnityDelivery -Arguments @('-projectPath', $cleanRoot, '-importPackage', $env:SAILINGOUT_EXPORT_PATH) -LogName 'delivery-import.log'
    Invoke-UnityDelivery -Arguments @('-projectPath', $cleanRoot, '-executeMethod', 'SailingOut.Editor.DeliveryBuilder.ValidateImportedDemo') -LogName 'delivery-validate.log'
    Invoke-UnityDelivery -Arguments @('-projectPath', $cleanRoot, '-executeMethod', 'SailingOut.Editor.DeliveryBuilder.BuildWindowsDemo') -LogName 'delivery-build.log'
    Write-Output "Package: $env:SAILINGOUT_EXPORT_PATH"
    Write-Output "Windows demo: $env:SAILINGOUT_BUILD_PATH"
    Write-Output "Clean project: $cleanRoot"
} finally {
    Remove-Item Env:\SAILINGOUT_EXPORT_PATH -ErrorAction SilentlyContinue
    Remove-Item Env:\SAILINGOUT_BUILD_PATH -ErrorAction SilentlyContinue
}
