param([string]$UnityEditor = 'E:/Unity Hub/6000.3.10f1/Editor')
$ErrorActionPreference = 'Stop'
$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$verificationRoot = Join-Path $projectRoot 'Temp/MonsterNeuralUnityVerification'
foreach ($folder in @('Assets/Editor', 'Assets/Script/monster', 'Assets/Resources/MonsterCombat', 'Packages', 'ProjectSettings', 'Reports/MonsterAI/neural-policy')) {
    New-Item -ItemType Directory -Path (Join-Path $verificationRoot $folder) -Force | Out-Null
}
# Isolate model verification from the user's open scene and player save data.
foreach ($file in @('MonsterNeuralPolicyModel.cs', 'MonsterNeuralPolicyPresenter.cs', 'MonsterBehaviorModel.cs', 'MonsterCombatModel.cs', 'MonsterSquadModel.cs')) {
    Copy-Item -LiteralPath (Join-Path $projectRoot "Assets/Script/monster/$file") -Destination (Join-Path $verificationRoot "Assets/Script/monster/$file")
}
foreach ($file in @('Assets/Editor/MonsterNeuralPolicyVerification.cs', 'Assets/Resources/MonsterCombat/monster_attack_mlp.onnx',
    'ProjectSettings/ProjectVersion.txt', 'Reports/MonsterAI/neural-policy/unity_probes.json')) {
    Copy-Item -LiteralPath (Join-Path $projectRoot $file) -Destination (Join-Path $verificationRoot $file)
}
# Use the installed package versions and their cached dependencies, without changing the real manifest.
$dependencies = @{}
$pending = [System.Collections.Generic.Queue[string]]::new()
$pending.Enqueue('com.unity.ai.inference')
$pending.Enqueue('com.unity.modules.jsonserialize')
while ($pending.Count -gt 0) {
    $packageName = $pending.Dequeue()
    if ($dependencies.ContainsKey($packageName)) { continue }
    if ($packageName.StartsWith('com.unity.modules.')) { $dependencies[$packageName] = '1.0.0'; continue }
    $cached = @(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Library/PackageCache') -Directory | Where-Object { $_.Name.StartsWith($packageName + '@') })
    if ($cached.Count -ne 1) { throw "Expected one cached package for $packageName" }
    $dependencies[$packageName] = 'file:' + $cached[0].FullName.Replace('\', '/')
    $package = Get-Content -LiteralPath (Join-Path $cached[0].FullName 'package.json') -Raw | ConvertFrom-Json
    foreach ($dependency in $package.dependencies.PSObject.Properties) { $pending.Enqueue($dependency.Name) }
}
$manifestText = @{ dependencies = $dependencies } | ConvertTo-Json -Depth 5
[System.IO.File]::WriteAllText((Join-Path $verificationRoot 'Packages/manifest.json'), $manifestText, [System.Text.UTF8Encoding]::new($false))
$logPath = Join-Path $verificationRoot 'verification.log'
$arguments = @('-batchmode', '-nographics', '-projectPath', ('"' + $verificationRoot + '"'),
    '-executeMethod', 'MonsterNeuralPolicyVerification.Verify', '-quit', '-logFile', ('"' + $logPath + '"'))
$process = Start-Process -FilePath (Join-Path $UnityEditor 'Unity.exe') -ArgumentList $arguments -WindowStyle Hidden -PassThru
Write-Output "Verification process: $($process.Id)"
Write-Output "Log: $logPath"
Write-Output "Result: $verificationRoot/Reports/MonsterAI/neural-policy/unity_verification.json"
