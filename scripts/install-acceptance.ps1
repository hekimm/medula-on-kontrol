param([string]$ProjectName='medula-kabul-final', [int]$WebPort=5190, [int]$OraclePort=11525)
$ErrorActionPreference='Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
# Her proje adı ayrı test volume'ları kullanır.
$env:COMPOSE_PROJECT_NAME=$ProjectName
$env:MEDULA_SECRETS_VOLUME="$ProjectName-secrets"
$env:MEDULA_ORACLE_VOLUME="$ProjectName-oracle-data"
$env:MEDULA_LOGS_VOLUME="$ProjectName-logs"
$env:MEDULA_PORT="$WebPort"
$env:ORACLE_PORT="$OraclePort"
docker compose up -d --wait --wait-timeout 300 --no-build --pull never
if($LASTEXITCODE -ne 0){throw 'Kabul kurulumu başarısız.'}
Write-Output "Kabul kurulumu: http://localhost:$WebPort ; proje: $ProjectName"
