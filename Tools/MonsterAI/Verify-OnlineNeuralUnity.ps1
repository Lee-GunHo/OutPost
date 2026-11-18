param([string]$UnityEditor = 'E:/Unity Hub/6000.3.10f1/Editor')
$ErrorActionPreference = 'Stop'
$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$verificationRoot = Join-Path ([System.IO.Path]::GetTempPath()) 'OutPostOnlineNeuralVerification'
foreach ($folder in @('Assets/Editor', 'Assets/Script', 'Packages', 'ProjectSettings')) {
    New-Item -ItemType Directory -Path (Join-Path $verificationRoot $folder) -Force | Out-Null
}
foreach ($file in @('MonsterLearningModel.cs', 'MonsterOnlineNetworkModel.cs')) {
    Copy-Item -LiteralPath (Join-Path $projectRoot "Assets/Script/monster/$file") -Destination (Join-Path $verificationRoot "Assets/Script/$file")
}
Copy-Item -LiteralPath (Join-Path $projectRoot 'Assets/Editor/MonsterOnlineNeuralVerification.cs') -Destination (Join-Path $verificationRoot 'Assets/Editor')
Copy-Item -LiteralPath (Join-Path $projectRoot 'ProjectSettings/ProjectVersion.txt') -Destination (Join-Path $verificationRoot 'ProjectSettings')
[System.IO.File]::WriteAllText((Join-Path $verificationRoot 'Packages/manifest.json'), '{"dependencies":{"com.unity.modules.jsonserialize":"1.0.0"}}', [System.Text.UTF8Encoding]::new($false))
$logPath = Join-Path $verificationRoot 'verification.log'
$arguments = @('-batchmode', '-nographics', '-projectPath', ('"' + $verificationRoot + '"'),
    '-executeMethod', 'MonsterOnlineNeuralVerification.Verify', '-quit', '-logFile', ('"' + $logPath + '"'))
$process = Start-Process -FilePath (Join-Path $UnityEditor 'Unity.exe') -ArgumentList $arguments -WindowStyle Hidden -PassThru
Write-Output "Verification process: $($process.Id)"
Write-Output "Log: $logPath"
Write-Output "Result: $verificationRoot/Reports/MonsterAI/online-neural/unity_verification.json"
