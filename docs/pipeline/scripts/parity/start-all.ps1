# Khoi dong toan bo: lead chinh (theo doi + digest + nap slot agy) + moi slot 1 feeder rieng (soan de bai SONG SONG).
$Root = 'C:\Users\Public\Pictures\kientran_private'; $ps = 'powershell.exe'
function Spawn([string]$args2) { Start-Process -WindowStyle Hidden -FilePath $ps -ArgumentList $args2 | Out-Null }
Spawn "-NoProfile -ExecutionPolicy Bypass -File `"$Root\scripts\parity\lead.ps1`" -Action loop -FeedOnly agy"
foreach ($w in 'agy2', 'agy3', 'agy4', 'codegpt') { Spawn "-NoProfile -ExecutionPolicy Bypass -File `"$Root\scripts\parity\feeder.ps1`" -Acct b -Workers $w"; Start-Sleep 2 }
Spawn "-NoProfile -ExecutionPolicy Bypass -File `"$Root\scripts\parity\feeder.ps1`" -Acct b -Workers gweb,gweb2,gweb3"
