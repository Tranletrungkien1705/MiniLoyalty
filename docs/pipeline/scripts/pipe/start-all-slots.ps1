# Khoi dong LAI toan bo slot cho 1 du an. LUU Y: dung moi lead/feeder/auto-loop dang chay (cua BAT KY du an nao) roi chay lai cho du an nay (1 du an hoat dong tai 1 thoi diem).
# Agent dang chay do (parity-step) KHONG bi dung. Dung: start-all-slots.ps1 -Project MiniContract
param([Parameter(Mandatory)][string]$Project, [string[]]$Direct = @('agy', 'agy6'), [string[]]$Router = @('agy2', 'agy3', 'agy4', 'agy5'), [string[]]$Gweb = @('gweb', 'gweb2', 'gweb3', 'gweb5'), [string[]]$Other = @('codegpt'), [switch]$SharedCtrl)
$Root = 'C:\Users\Public\Pictures\kientran_private'; $Par = "$Root\scripts\parity"
Get-CimInstance Win32_Process | Where-Object { $_.Name -match 'powershell' -and $_.CommandLine -match '(lead|feeder|auto-loop)\.ps1' -and $_.CommandLine -notmatch 'Get-CimInstance' } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -EA SilentlyContinue }
$Run = "$Root\repos\$Project\.parity-run"; New-Item -ItemType Directory -Force $Run | Out-Null
Remove-Item "$Run\STOP", "$Run\STOP_LEAD" -Force -EA SilentlyContinue
[IO.File]::WriteAllText("$Run\slots.json", (@{ direct = $Direct; router = $Router; gweb = $Gweb; other = $Other } | ConvertTo-Json), [Text.UTF8Encoding]::new($false))
$env:PIPE_PROJECT = $Project; $env:LEAD_TEMPLATE = '1'; $env:PIPE_SHARED_CTRL = $(if ($SharedCtrl) { '1' } else { '0' })
function Spawn([string]$a) { Start-Process -WindowStyle Hidden -FilePath powershell.exe -ArgumentList $a | Out-Null; Start-Sleep 2 }
$env:PARITY_ENGINE_FORCE = ''
foreach ($w in $Direct + $Other + $Gweb) { Spawn "-NoProfile -ExecutionPolicy Bypass -File `"$Par\feeder.ps1`" -Acct b -Workers $w" }
$env:PARITY_ENGINE_FORCE = 'rtr'
foreach ($w in $Router) { Spawn "-NoProfile -ExecutionPolicy Bypass -File `"$Par\feeder.ps1`" -Acct b -Workers $w" }
$env:PARITY_ENGINE_FORCE = ''
Spawn "-NoProfile -ExecutionPolicy Bypass -File `"$Root\scripts\pipe\auto-loop.ps1`" -Project $Project"
"Da khoi dong $Project : direct($($Direct -join ',')) router($($Router -join ',')) gweb($($Gweb -join ',')) other($($Other -join ',')) + auto-loop"
