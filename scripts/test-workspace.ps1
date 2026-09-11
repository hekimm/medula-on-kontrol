$ErrorActionPreference='Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
& "$PSScriptRoot/verify-naming.ps1"
dotnet test tests/MedulaOnKontrol.UnitTests --collect:'XPlat Code Coverage' --logger 'trx;LogFileName=unit.trx' --results-directory artifacts/test-results
if($LASTEXITCODE -ne 0){throw 'Birim testleri başarısız.'}
python scripts/verify_coverage.py
if($LASTEXITCODE -ne 0){throw 'Kural satır kapsamı %100 değil.'}
dotnet test tests/MedulaOnKontrol.IntegrationTests --logger 'trx;LogFileName=oracle.trx' --results-directory artifacts/integration-results
if($LASTEXITCODE -ne 0){throw 'Oracle entegrasyon testleri başarısız.'}
npm run test:ui
if($LASTEXITCODE -ne 0){throw 'Arayüz testleri başarısız.'}
