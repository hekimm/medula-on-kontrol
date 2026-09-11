$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
Set-Location $projectRoot
$localOutput=Join-Path $projectRoot 'artifacts/local-web'
$listeners=Get-NetTCPConnection -LocalPort 5186 -State Listen -ErrorAction SilentlyContinue | Sort-Object OwningProcess -Unique
foreach($listener in $listeners){
    $owner=Get-CimInstance Win32_Process -Filter "ProcessId=$($listener.OwningProcess)"
    if($null -eq $owner){continue}
    if($owner.CommandLine -and $owner.CommandLine.Contains($localOutput.Replace('/','\'))){Stop-Process -Id $listener.OwningProcess}
    else {throw '5186 portu başka bir uygulama tarafından kullanılıyor.'}
}
dotnet publish src/MedulaOnKontrol.Web --no-restore -o $localOutput
if($LASTEXITCODE -ne 0){throw 'Derleme başarısız.'}
$env:MEDULA_SECRET_DIR=Join-Path $projectRoot 'secrets'
$arguments=@(('"'+(Join-Path $localOutput 'MedulaOnKontrol.Web.dll')+'"'),'--contentRoot',('"'+$localOutput+'"'),'--urls','http://localhost:5186')
$server=Start-Process -FilePath 'dotnet' -ArgumentList $arguments -WorkingDirectory $projectRoot -WindowStyle Hidden -RedirectStandardOutput (Join-Path $projectRoot 'artifacts/server.log') -RedirectStandardError (Join-Path $projectRoot 'artifacts/server-error.log') -PassThru
Write-Output "MEDULA yerel uygulaması başlatıldı: http://localhost:5186 (işlem $($server.Id))"
