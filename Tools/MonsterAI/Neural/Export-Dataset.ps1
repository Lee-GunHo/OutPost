param([string]$UnityEditor = 'E:/Unity Hub/6000.3.10f1/Editor')
$ErrorActionPreference = 'Stop'
$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$exportDir = Join-Path $projectRoot 'Temp/MonsterNeuralExport'
New-Item -ItemType Directory -Path $exportDir -Force | Out-Null
$exePath = Join-Path $exportDir 'ExportDataset.exe'
$reference = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/mscorlib.dll'
$sources = @(
    (Join-Path $projectRoot 'Assets/Script/monster/MonsterNeuralPolicyModel.cs'),
    (Join-Path $PSScriptRoot 'ExportDataset.cs')
)
& "$UnityEditor/Data/NetCoreRuntime/dotnet.exe" exec "$UnityEditor/Data/DotNetSdkRoslyn/csc.dll" /nologo /target:exe /nostdlib "/r:$reference" "/out:$exePath" $sources
if ($LASTEXITCODE -ne 0) { throw 'Dataset exporter compilation failed.' }
& $exePath (Join-Path $projectRoot 'Temp/MonsterNeuralTrainingData/dataset.csv')
if ($LASTEXITCODE -ne 0) { throw 'Dataset export failed.' }
