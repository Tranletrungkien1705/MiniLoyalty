# Khoi dong 9router DUNG cach cu: node server.js trong thu muc app, chi nghe 127.0.0.1:20128 (khong dung cli.js: no tao crash loop 0xC0000005 khi chay an).
$Root = 'C:\Users\Public\Pictures\kientran_private'
$p = (Get-NetTCPConnection -State Listen -LocalPort 20128 -EA SilentlyContinue | Select-Object -First 1).OwningProcess
if ($p) { Stop-Process -Id $p -Force -EA SilentlyContinue; Start-Sleep 3 }
$app = (Get-ChildItem "$env:LOCALAPPDATA\npm-cache\_npx\*\node_modules\9router\app" -Directory | Select-Object -First 1).FullName
$env:PORT = '20128'; $env:HOSTNAME = '127.0.0.1'
$pwf = "$Root\9router\dashboard-password.txt"; if (Test-Path $pwf) { $env:INITIAL_PASSWORD = (Get-Content $pwf -Raw).Trim() }   # mat khau dashboard (thieu bien nay router dung mac dinh 123456)
Start-Process -WindowStyle Hidden -WorkingDirectory $app node -ArgumentList '--dns-result-order=ipv4first', 'server.js' -RedirectStandardOutput "$Root\9router\srv.out.log" -RedirectStandardError "$Root\9router\srv.err.log"
Start-Sleep 15
if (Get-NetTCPConnection -State Listen -LocalPort 20128 -EA SilentlyContinue) { 'router len' } else { 'router CHUA len' }
