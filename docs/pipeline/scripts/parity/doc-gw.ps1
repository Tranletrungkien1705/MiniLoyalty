# Viet tai lieu API cho 1 controller da port bang GEMINI WEB (1 luot hoi-dap, khong ton quota agy).
# Web khong doc file => script nhung san: controller port + entity + TOM TAT/nguon cua cac hang muc; sau do TU DOI CHIEU MAY MOC
# (route, ma loi ErridnLoyalty) va ghi canh bao vao <file>.check.txt. Ket qua vao worktree docs (nhanh docs/parity); Claude review roi
# aux-agent.ps1 -Action commit-docs. Dung: doc-gw.ps1 -Controller Mst_PositionController [-Role doc1]
param([Parameter(Mandatory)][string]$Controller, [string]$Role = 'doc1')
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)
$Root = 'C:\Users\Public\Pictures\kientran_private'; $Repo = "$Root\repos\MiniLoyalty"; $Run = "$Repo\.parity-run"; $Here = Split-Path -Parent $MyInvocation.MyCommand.Path
$Docs = "$Root\repos\MiniLoyalty-docs"; $Utf8 = [Text.UTF8Encoding]::new($false)
function Log([string]$m) { $l = "[{0:yyyy-MM-dd HH:mm:ss}] [{1}] {2}" -f (Get-Date), $Role, $m; Write-Output $l; [IO.File]::AppendAllText("$Run\run.log", $l + "`r`n", $Utf8) }
if (-not $env:GW_PORT) { if ((Test-Path (Join-Path $Run 'LEAD_ACCT_C')) -and (Get-NetTCPConnection -State Listen -LocalPort 9224 -EA SilentlyContinue)) { $env:GW_PORT = '9224'; $env:GW_TAG = 'c' }
elseif ((Test-Path (Join-Path $Run 'LEAD_ACCT_B')) -and (Get-NetTCPConnection -State Listen -LocalPort 9223 -EA SilentlyContinue)) { $env:GW_PORT = '9223'; $env:GW_TAG = 'b' } }   # tai lieu bang tai khoan B (nhuong tai khoan A cho viec code)
$base = $Controller -replace 'Controller$', ''
$ctrlF = "$Repo\Controllers\Port\${base}ControllerPort.cs"
if (-not (Test-Path $ctrlF)) { Log "khong thay $ctrlF (parity/auto chua co controller nay)"; exit 1 }
if (-not (Test-Path "$Docs\.git")) { & git -C $Repo worktree add -f -B docs/parity $Docs parity/auto | Out-Null }
$head = (& git -C $Repo rev-parse --short parity/auto).Trim()
$embed = @($ctrlF); foreach ($m in @("$Repo\Models\Port\$base.cs")) { if (Test-Path $m) { $embed += $m } }
$ctx = ($embed | ForEach-Object { "`n===== FILE $_ =====`n" + (Get-Content $_ -Raw -Encoding UTF8) }) -join ''
# TOM TAT cua cac hang muc thuoc controller (nhat ky logs\<so>_<ten>\write_r*.txt, ban moi nhat)
$sum = ''
Get-ChildItem "$Run\logs" -Directory | Where-Object { $_.Name -match ("^\d+_" + [regex]::Escape($base) + "_") } | ForEach-Object {
  $w = Get-ChildItem $_.FullName -Filter 'write_r*_*.txt' | Where-Object { $_.Name -notmatch 'prompt|web|try' } | Sort-Object LastWriteTime | Select-Object -Last 1
  if ($w) { $t = Get-Content $w.FullName -Raw -Encoding UTF8; $i = $t.LastIndexOf('TOM TAT'); if ($i -ge 0) { $t = $t.Substring($i) }; if ($t.Length -gt 3500) { $t = $t.Substring(0, 3500) }; $sum += "`n===== TOM TAT $($_.Name) =====`n$t" }
}
$brief = [IO.File]::ReadAllText("$Here\briefs\BRIEF_$Role.md", [Text.Encoding]::UTF8)
$brief = ($brief -split '## TỰ ĐỐI CHIẾU')[0]   # phan doi chieu may moc do script lam
$prompt = @"
VAI TRO: $Role - viet tai lieu API cho controller $Controller (phien ban ma $head). Ban la ban WEB: KHONG doc/ghi file, KHONG chay lenh; ma controller port, entity va TOM TAT nhat ky da dan o cuoi. CHI dua vao ma da dan - khong bia tham so/ma loi/vi du; khong chac thi ghi 'Chua xac minh'. Khong co ket qua chay that thi KHONG dua vi du response (ghi 'Chua co lan chay mau'), chi dung vi du request suy ra tu ma.
DAU RA BAT BUOC: dung 1 dong "FILE: docs/api/$Controller.md" roi NGAY SAU DO 1 khoi (``````markdown) chua toan bo tai lieu. Khong viet gi khac ngoai khoi do. Khong dung ky tu ``` ben trong tai lieu (dung khoi thut le 4 dau cach cho vi du JSON).
$brief
$ctx
$sum
"@
$sub = "$Run\sub"; $pf = "$sub\doc_gw_$base.prompt.txt"; $wo = "$sub\doc_gw_$base.web.txt"
[IO.File]::WriteAllText($pf, $prompt, $Utf8)
foreach ($x in @($wo, "$wo.blocks.json", "$wo.log")) { Remove-Item $x -Force -EA SilentlyContinue }
Log "BAT DAU doc (gweb) $Controller | HEAD $head"
Push-Location "$Here\gemini-web"
try { & node gw-run.mjs $pf $wo 900 doc-gw 2>&1 | Out-Null; $code = $LASTEXITCODE } finally { Pop-Location }
if ($code -ne 0) { Log "  gweb loi exit $code"; exit 2 }
$bj = @(Get-Content "$wo.blocks.json" -Raw -Encoding UTF8 | ConvertFrom-Json)
if ($bj.Count -lt 1) { Log '  khong co khoi markdown'; exit 2 }
$doc = [string]$bj[0]
New-Item -ItemType Directory -Force "$Docs\docs\api" | Out-Null
$out = "$Docs\docs\api\$Controller.md"
[IO.File]::WriteAllText($out, $doc.TrimEnd() + "`r`n", $Utf8)
# doi chieu may moc
$code = Get-Content $ctrlF -Raw -Encoding UTF8
$routes = @([regex]::Matches($code, '\[Http\w+\("([^"]+)"\)\]') | ForEach-Object { $_.Groups[1].Value } | Select-Object -Unique)
$errs = @([regex]::Matches($code, 'Fail\("([A-Za-z0-9_\.]+)"') | ForEach-Object { $_.Groups[1].Value } | Select-Object -Unique)
$warn = @()
foreach ($r in $routes) { if ($doc -notmatch [regex]::Escape($r)) { $warn += "THIEU route trong tai lieu: $r" } }
foreach ($e in ([regex]::Matches($doc, 'ErridnLoyalty\.([A-Za-z0-9_\.]+)') | ForEach-Object { $_.Groups[1].Value } | Select-Object -Unique)) { if ($code -notmatch [regex]::Escape($e)) { $warn += "MA LOI khong co trong ma: $e" } }
foreach ($e in $errs) { if ($doc -notmatch [regex]::Escape($e)) { $warn += "THIEU ma loi (co trong ma): $e" } }
if ($doc -match '(?i)file:///|\$\.\.\.\$|[A-Z]:\\') { $warn += 'co duong dan tuyet doi/file:///' }
[IO.File]::WriteAllText("$out.check.txt", $(if ($warn) { $warn -join "`r`n" } else { 'OK: route + ma loi khop ma' }), $Utf8)
Log ("  XONG {0} -> {1} | canh bao doi chieu: {2}" -f $Controller, $out, $warn.Count)
