param(
    [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe'
)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
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
try {
    Invoke-UnityDelivery -Arguments @('-projectPath', $stagingRoot, '-executeMethod', 'SailingOut.Editor.DeliveryBuilder.ExportStandaloneAssets') -LogName 'delivery-export.log'
    Invoke-UnityDelivery -Arguments @('-projectPath', $cleanRoot, '-importPackage', $env:SAILINGOUT_EXPORT_PATH) -LogName 'delivery-import.log'
    Invoke-UnityDelivery -Arguments @('-projectPath', $cleanRoot, '-executeMethod', 'SailingOut.Editor.DeliveryBuilder.ValidateImportedDemo') -LogName 'delivery-validate.log'
    Write-Output "Package: $env:SAILINGOUT_EXPORT_PATH"
    Write-Output "Clean project: $cleanRoot"
} finally {
    Remove-Item Env:\SAILINGOUT_EXPORT_PATH -ErrorAction SilentlyContinue
}
