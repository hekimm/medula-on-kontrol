param([string]$ProjectName='medula-kabul-tam')
$ErrorActionPreference='Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
$oracleContainer="$ProjectName-oracle-1"
$webContainer="$ProjectName-web-1"
docker cp scripts/verify-runtime.sh "${oracleContainer}:/tmp/medula-verify-runtime.sh"
if($LASTEXITCODE -ne 0){throw 'Oracle doğrulama betiği kopyalanamadı.'}
$deadline=[DateTime]::UtcNow.AddMinutes(3)
do {
    $evidence=docker exec $oracleContainer bash /tmp/medula-verify-runtime.sh 2>&1
    $successful=$LASTEXITCODE -eq 0 -and ($evidence -join "`n").Contains('COMPLETED_JOBS=3')
    if($successful){break}
    if([DateTime]::UtcNow -ge $deadline){$evidence; throw 'Boş kurulum ve üç dönem kontrolü doğrulanamadı.'}
    Start-Sleep -Seconds 3
} while($true)
$imageId=docker inspect $webContainer --format '{{.Image}}'
if($LASTEXITCODE -ne 0){throw 'Kabul imajı belirlenemedi.'}
$record="VERIFIED_AT_UTC=$([DateTime]::UtcNow.ToString('o'))`nPROJECT=$ProjectName`nIMAGE_ID=$imageId`n"+($evidence -join "`n")+"`n"
New-Item -ItemType Directory -Force -Path 'artifacts/verification' | Out-Null
[IO.File]::WriteAllText((Join-Path (Get-Location) 'artifacts/verification/docker-acceptance.txt'),$record)
Write-Output $record
