# Vong nen: cu 45 giay chay auto-bo.ps1 cho 1 du an (ghi Bo cac hang muc chi chuyen tiep dich vu ngoai) de slot khong cho Claude. Dung bang file <repo>\.parity-run\STOP.
param([Parameter(Mandatory)][string]$Project, [int]$EverySec = 50)
$Root = 'C:\Users\Public\Pictures\kientran_private'; $Run = "$Root\repos\$Project\.parity-run"
while (-not (Test-Path "$Run\STOP")) {
  try { & powershell -NoProfile -ExecutionPolicy Bypass -File "$Root\scripts\pipe\auto-bo.ps1" -Project $Project 2>&1 | Out-Null } catch { }
  try { & powershell -NoProfile -ExecutionPolicy Bypass -File "$Root\scripts\pipe\chrome-clean.ps1" 2>&1 | Out-Null } catch { }
  try { & powershell -NoProfile -ExecutionPolicy Bypass -File "$Root\scripts\pipe\slot-watch.ps1" -Project $Project 2>&1 | Out-Null } catch { }
  try { & powershell -NoProfile -ExecutionPolicy Bypass -File "$Root\scripts\pipe\auto-commit.ps1" -Project $Project 2>&1 | Out-Null } catch { }
  try { & powershell -NoProfile -ExecutionPolicy Bypass -File "$Root\scripts\pipe\status.ps1" -Project $Project 2>&1 | Out-Null } catch { }
  try { & powershell -NoProfile -ExecutionPolicy Bypass -File "$Root\scripts\pipe\gweb-stats.ps1" 2>&1 | Out-Null } catch { }
  Start-Sleep $EverySec
}
