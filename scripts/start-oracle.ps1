$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$secretDirectory = Join-Path $projectRoot 'secrets'
New-Item -ItemType Directory -Force -Path $secretDirectory | Out-Null
foreach ($secretName in @('oracle-password','app-password','runtime-password','admin-password','aes-key')) {
    $secretPath = Join-Path $secretDirectory $secretName
    if (-not (Test-Path -LiteralPath $secretPath)) {
        $bytes = New-Object byte[] 32
        $randomNumberGenerator = [System.Security.Cryptography.RandomNumberGenerator]::Create()
        $randomNumberGenerator.GetBytes($bytes)
        $value = if ($secretName -eq 'aes-key') { [Convert]::ToBase64String($bytes) } else { 'M9a' + ([BitConverter]::ToString($bytes).Replace('-','').Substring(0,24)) }
        [System.IO.File]::WriteAllText($secretPath,$value)
    }
}
$envPath = Join-Path $secretDirectory 'oracle.env'
$envText = "ORACLE_PASSWORD=$([IO.File]::ReadAllText((Join-Path $secretDirectory 'oracle-password')))" + "`nAPP_USER=MEDULA`nAPP_USER_PASSWORD=$([IO.File]::ReadAllText((Join-Path $secretDirectory 'app-password')))"
[IO.File]::WriteAllText($envPath,$envText)
$existing = docker ps -a --filter 'name=^medula-oracle$' --format '{{.Names}}'
if ($existing -eq 'medula-oracle') { docker start medula-oracle } else {
    docker run -d --name medula-oracle --env-file $envPath -p 127.0.0.1:11521:1521 --shm-size=1g -v medula-oracle-data:/opt/oracle/oradata gvenzl/oracle-xe:21.3.0-slim-faststart
}
if ($LASTEXITCODE -ne 0) { throw 'Oracle konteyneri başlatılamadı.' }
