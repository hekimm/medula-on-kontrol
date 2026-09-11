$ErrorActionPreference='Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
dotnet run --project tools/MedulaOnKontrol.Benchmark -c Release -- --seed
if($LASTEXITCODE -ne 0){throw 'Performans kabul ölçütü sağlanmadı; artifacts/verification/performance.json dosyasını inceleyin.'}
