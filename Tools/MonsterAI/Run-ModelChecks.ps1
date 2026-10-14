param([string]$UnityEditor = 'E:/Unity Hub/6000.3.10f1/Editor')
$ErrorActionPreference = 'Stop'
$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$testDirectory = Join-Path ([System.IO.Path]::GetTempPath()) ('outpost-ai-checks-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $testDirectory | Out-Null
$outputAssembly = Join-Path $testDirectory 'MonsterChecks.dll'
$sources = @(
    'Assets/Script/monster/MonsterLearningModel.cs',
    'Assets/Script/monster/MonsterCombatTelemetryModel.cs',
    'Assets/Script/monster/MonsterSquadModel.cs',
    'Assets/Script/save/GameSaveModel.cs',
    'Tools/MonsterAI/ModelChecks.cs'
) | ForEach-Object { Join-Path $projectRoot $_ }
# These stores now depend on Unity. Import only their actual filename constants for the pure save-scope test.
$constantStubs = @()
foreach ($contract in @(
    @{ Path='Assets/Script/Progress/ProgressTracker.cs'; Type='ProgressTracker'; Field='SaveFileName' },
    @{ Path='Assets/Script/Farming/FarmSaveStore.cs'; Type='FarmSaveStore'; Field='FilePrefix' }
)) {
    $sourceText = Get-Content -LiteralPath (Join-Path $projectRoot $contract.Path) -Raw
    $constantPattern = 'public const string ' + $contract.Field + '\s*=\s*("[^"]+")\s*;'
    if ($sourceText -notmatch $constantPattern) { throw ('Missing filename contract: ' + $contract.Type) }
    $constantStubs += ('public static class ' + $contract.Type + ' { public const string ' + $contract.Field + ' = ' + $Matches[1] + '; }')
}
$constantStubPath = Join-Path $testDirectory 'SaveFilenameContracts.cs'
Set-Content -LiteralPath $constantStubPath -Value $constantStubs -Encoding UTF8
$sources += $constantStubPath
$reference = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/mscorlib.dll'
$coreReference = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/System.Core.dll'
& "$UnityEditor/Data/NetCoreRuntime/dotnet.exe" exec "$UnityEditor/Data/DotNetSdkRoslyn/csc.dll" /nologo /target:library /nostdlib "/r:$reference" "/r:$coreReference" "/out:$outputAssembly" $sources
if ($LASTEXITCODE -ne 0) { throw 'Model checks failed to compile.' }
Add-Type -Path $outputAssembly
[ModelChecks]::Run()
