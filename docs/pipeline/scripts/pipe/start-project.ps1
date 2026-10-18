# Khoi dong pipeline cho 1 du an (ho so trong scripts\parity\profile.ps1). Dung: start-project.ps1 -Project MiniGate [-RouterSlots agy2,agy4] [-AgySlots agy]
# Slot agy = agy truc tiep; RouterSlots = chay qua 9router (engine rtr, tiet kiem han muc agy). Dung bang file <repo>\.parity-run\STOP.
param([Parameter(Mandatory)][string]$Project, [string[]]$AgySlots = @('agy'), [string[]]$RouterSlots = @('agy2', 'agy3', 'agy4', 'agy5'), [switch]$Template)
$Root = 'C:\Users\Public\Pictures\kientran_private'; $Par = "$Root\scripts\parity"
$env:PIPE_PROJECT = $Project; $env:LEAD_TEMPLATE = $(if ($Template) { '1' } else { '' })
$Run = "$Root\repos\$Project\.parity-run"; New-Item -ItemType Directory -Force $Run | Out-Null
Remove-Item "$Run\STOP", "$Run\STOP_LEAD" -Force -EA SilentlyContinue
function Spawn([string]$a) { Start-Process -WindowStyle Hidden -FilePath powershell.exe -ArgumentList $a | Out-Null }
$env:PARITY_ENGINE_FORCE = ''
Spawn "-NoProfile -ExecutionPolicy Bypass -File `"$Par\lead.ps1`" -Action loop -FeedOnly $($AgySlots -join ',')"
Start-Sleep 2
$env:PARITY_ENGINE_FORCE = 'rtr'
foreach ($w in $RouterSlots) { Spawn "-NoProfile -ExecutionPolicy Bypass -File `"$Par\feeder.ps1`" -Acct b -Workers $w"; Start-Sleep 2 }
"Da khoi dong $Project : agy=$($AgySlots -join ',') router=$($RouterSlots -join ',')"
Spawn "-NoProfile -ExecutionPolicy Bypass -File `"$Root\scripts\pipe\auto-loop.ps1`" -Project $Project"
