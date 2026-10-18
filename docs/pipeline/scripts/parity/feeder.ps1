# Vong nap viec PHU: soan de bai + nap cho 1 nhom worker bang tai khoan Gemini web rieng (LEAD_ACCT) => soan de bai SONG SONG voi lead chinh.
# Dung: powershell -File feeder.ps1 -Acct b -Workers gweb,gweb2   (chay nen; dung bang file ...\.parity-run\STOP_LEAD)
param([string]$Acct = 'b', [string[]]$Workers = @('gweb'), [int]$EverySec = 40)
. "$PSScriptRoot\profile.ps1"
$Root = 'C:\Users\Public\Pictures\kientran_private'; $Run = Join-Path $Root "repos\$($PJ.Name)\.parity-run"
$env:LEAD_ACCT = $Acct; $env:FEEDER = '1'
$ws = @($Workers | ForEach-Object { $_ -split ',' } | Where-Object { $_ })
$lead = Join-Path $Root 'scripts\parity\lead.ps1'
while (-not (Test-Path (Join-Path $Run 'STOP_LEAD')) -and -not (Test-Path (Join-Path $Run 'STOP'))) {
  try { & $lead -Action feed -Workers $ws -FeedOnly $ws | Out-Null } catch { [IO.File]::AppendAllText((Join-Path $Run 'run.log'), "[feeder $Acct] loi: $_`r`n") }
  Start-Sleep -Seconds $EverySec
}
