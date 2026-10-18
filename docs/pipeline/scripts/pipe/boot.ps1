# Tu nap khi bat may / dang nhap Windows: 9router -> Chrome gweb (an) -> supervisor. Chay lai nhieu lan van an toan (kiem tra truoc khi khoi dong).
$Root = 'C:\Users\Public\Pictures\kientran_private'; $Log = "$Root\repos\supervisor.log"
function L([string]$m) { Add-Content $Log ("[{0:yyyy-MM-dd HH:mm:ss}] [boot] {1}" -f (Get-Date), $m) }
Start-Sleep 20
if (-not (Get-NetTCPConnection -State Listen -LocalPort 20128 -EA SilentlyContinue)) {
  $o = & "$Root\scripts\pipe\start-router.ps1"; L "9router: $o"
}
& powershell -NoProfile -ExecutionPolicy Bypass -File "$Root\scripts\pipe\chrome-clean.ps1" | Out-Null   # mo Chrome 9222 (an neu co chrome-headless.txt)
$sup = Get-CimInstance Win32_Process | Where-Object { $_.Name -match 'powershell' -and $_.CommandLine -match 'supervisor\.ps1' }
if (-not $sup -and -not (Test-Path "$Root\repos\SUPERVISOR_STOP")) { Start-Process -WindowStyle Hidden powershell -ArgumentList '-NoProfile -ExecutionPolicy Bypass -File', "$Root\scripts\pipe\supervisor.ps1", '-Push'; L 'supervisor khoi dong' }
