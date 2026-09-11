param([string]$Destination='artifacts/offline')
$ErrorActionPreference='Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
docker compose build
if($LASTEXITCODE -ne 0){throw 'İmaj derlemesi başarısız.'}
docker compose pull oracle
if($LASTEXITCODE -ne 0){throw 'Oracle imajı alınamadı.'}
$oracleImage = docker compose config --images | Where-Object { $_ -like 'gvenzl/oracle-xe:*' } | Select-Object -First 1
docker tag $oracleImage gvenzl/oracle-xe:21.3.0-slim-faststart
if($LASTEXITCODE -ne 0){throw 'Oracle çevrimdışı etiketi oluşturulamadı.'}
New-Item -ItemType Directory -Force -Path $Destination | Out-Null
docker save -o (Join-Path $Destination 'medula-images.tar') medula-prebilling:local gvenzl/oracle-xe:21.3.0-slim-faststart
if($LASTEXITCODE -ne 0){throw 'Çevrimdışı imaj paketi oluşturulamadı.'}
Copy-Item -LiteralPath 'docker/docker-compose.yml' -Destination (Join-Path $Destination 'compose.yaml')
Copy-Item -LiteralPath 'docker/010-runtime-user.sh' -Destination (Join-Path $Destination '010-runtime-user.sh')
$manifest=Join-Path $Destination 'compose.yaml'
$text=[IO.File]::ReadAllText($manifest)
$text=$text -replace '(?m)^    build:\r?\n      context: \.\.\r?\n      dockerfile: docker/Dockerfile\r?\n',''
# Çevrimdışı arşiv etiketle yüklenir; dosya bütünlüğü SHA-256 ile doğrulanır.
$text=$text -replace '(gvenzl/oracle-xe:21\.3\.0-slim-faststart)@sha256:[a-f0-9]+','$1'
[IO.File]::WriteAllText($manifest,$text)
Get-FileHash -Algorithm SHA256 (Join-Path $Destination 'medula-images.tar') | Format-List | Out-File (Join-Path $Destination 'SHA256.txt')
Write-Output "Çevrimdışı paket hazır: $Destination. Hedefte docker load -i medula-images.tar ve docker compose up --no-build --pull never komutlarını çalıştırın."
