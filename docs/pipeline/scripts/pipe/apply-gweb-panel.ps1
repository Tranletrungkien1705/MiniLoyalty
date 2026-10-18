# Cai bang gweb vao dashboard 9router (usage + quota). Chay 1 lan (va sau moi lan 'npx 9router' cap nhat ban moi). Sao luu *.bak, go bo: -Undo.
param([switch]$Undo)
$Root = 'C:\Users\Public\Pictures\kientran_private'
$app = (Get-ChildItem "$env:LOCALAPPDATA\npm-cache\_npx\*\node_modules\9router\app" -Directory | Select-Object -First 1).FullName
if (-not $app) { throw 'Khong thay thu muc 9router\app trong npm-cache' }
$line = 'try { require("./gweb-inject.js"); } catch (e) { console.error("[gweb-inject]", e && e.message); }'
foreach ($f in 'server.js', 'custom-server.js') {
  $p = "$app\$f"
  if ($Undo) { if (Test-Path "$p.bak") { Copy-Item "$p.bak" $p -Force }; continue }
  if (-not (Test-Path "$p.bak")) { Copy-Item $p "$p.bak" }
  $t = [IO.File]::ReadAllText($p)
  if ($t -notmatch 'gweb-inject') { [IO.File]::WriteAllText($p, $line + "`n" + $t, [Text.UTF8Encoding]::new($false)) }
}
if (-not $Undo) { Copy-Item "$Root\scripts\pipe\gweb-inject.js" "$app\gweb-inject.js" -Force }
& "$Root\scripts\pipe\start-router.ps1"
try { "gweb status: " + (Invoke-WebRequest 'http://127.0.0.1:20128/__gweb/status' -UseBasicParsing -TimeoutSec 8).StatusCode } catch { "9router/bang gweb chua len: $($_.Exception.Message)" }
