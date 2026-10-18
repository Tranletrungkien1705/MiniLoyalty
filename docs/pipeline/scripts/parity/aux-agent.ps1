# Agent PHU cho pipeline port 1-1 MiniLoyalty: 2 agent VIET TAI LIEU (doc1, doc2) + 2 agent KIEM THU (qa1, qa2). Deu chay agy (Gemini).
#   doc1/doc2 : viet tai lieu vao worktree repos\MiniLoyalty-docs (nhanh docs/parity, thu muc docs\). Claude review roi -Action commit-docs.
#   qa1/qa2   : worktree rieng repos\MiniLoyalty-<role> (detached tai parity/auto). KHONG sua code; ghi bao cao loi vao
#               repos\MiniLoyalty\.parity-run\qa\<role>\<stamp>_QA.md de Claude kiem tra.
# Script CHI PHAT HIEN va BAO CAO vi pham (khong tu reset/clean/doi nhanh) - Claude quyet dinh xu ly.
#   -Action run         : cho agent chay 1 luot theo -Focus roi THOAT (=> danh thuc Claude). Ghi aux_<role>.json
#   -Action commit-docs : Claude da duyet -> commit thu muc docs\ cua worktree docs vao nhanh docs/parity (KHONG push)
#   -Action status      : in trang thai 4 vai tro
param(
  [ValidateSet('run','commit-docs','status')][string]$Action = 'run',
  [ValidateSet('doc1','doc2','qa1','qa2')][string]$Role = 'doc1',
  [string]$Focus = '',
  [string]$FocusFile = '',
  [string]$Message = '',
  [string]$AgyModel = 'gemini-3.8-flash-high',
  [ValidateSet('','agy','codegpt')][string]$Engine = ''   # '' = QA chay CodeGPT (agy het han muc), doc chay agy
)
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)
$OutputEncoding = [Text.UTF8Encoding]::new($false)
$env:GIT_TERMINAL_PROMPT = '0'

$Root   = 'C:\Users\Public\Pictures\kientran_private'
$Repo   = "$Root\repos\MiniLoyalty"
$Source = 'D:\idocNet\2022.7.Loyalty'
$Run    = "$Repo\.parity-run"
$Here   = Split-Path -Parent $MyInvocation.MyCommand.Path
$Agy    = "$env:LOCALAPPDATA\agy\bin\agy.exe"
$Utf8   = [Text.UTF8Encoding]::new($false)
$IsDoc  = $Role -like 'doc*'
$WT     = if ($IsDoc) { "$Root\repos\MiniLoyalty-docs" } else { "$Root\repos\MiniLoyalty-$Role" }
# Luu y: 'aux' la ten thiet bi DANH RIENG cua Windows -> khong dat ten thu muc la aux. Dung 'sub' / 'briefs'.
if ($FocusFile -and (Test-Path $FocusFile)) { $Focus = [IO.File]::ReadAllText($FocusFile, [Text.Encoding]::UTF8) }
$AuxDir = "$Run\sub"
foreach ($d in @($AuxDir, "$Run\qa\qa1", "$Run\qa\qa2")) { if (-not (Test-Path $d)) { $null = New-Item -ItemType Directory -Path $d -Force } }
$ResF   = "$AuxDir\aux_$Role.json"
$QaPort = @{ qa1 = 8410; qa2 = 8450 }

function Log([string]$m) { $l = "[{0:yyyy-MM-dd HH:mm:ss}] [{1}] {2}" -f (Get-Date), $Role, $m; Write-Output $l; [IO.File]::AppendAllText("$Run\run.log", $l + "`r`n", $Utf8) }
function GitIn([string]$dir) {
  $a = $args; $eap = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
  try { $o = & git -C $dir @a 2>&1 | ForEach-Object { "$_" } } finally { $ErrorActionPreference = $eap }
  if ($LASTEXITCODE) { throw "git -C $dir $($a -join ' ') loi: $o" }
  $o
}
function WriteJson([string]$f, $o) { [IO.File]::WriteAllText($f, ($o | ConvertTo-Json -Depth 6), $Utf8) }

if ($Action -eq 'status') {
  foreach ($r in 'doc1', 'doc2', 'qa1', 'qa2') { "== $r"; if (Test-Path "$AuxDir\aux_$r.json") { Get-Content "$AuxDir\aux_$r.json" -Raw -Encoding UTF8 } else { '(chua chay)' } }
  exit 0
}

function Ensure-Worktree {
  if (Test-Path "$WT\.git") { return }
  Log "tao worktree $WT"
  if ($IsDoc) { $null = GitIn $Repo worktree add -f -B docs/parity $WT parity/auto }
  else { $null = GitIn $Repo worktree add -f --detach $WT parity/auto }
}

if ($Action -eq 'commit-docs') {
  if (-not $IsDoc) { throw 'commit-docs chi cho doc1/doc2' }
  Ensure-Worktree
  $null = GitIn $WT add docs
  if (-not (GitIn $WT diff --cached --name-only)) { Log 'khong co tai lieu moi de commit'; exit 0 }
  $n = @(GitIn $WT diff --cached --name-only).Count
  $msg = if ($Message) { $Message } else { "docs: cap nhat tai lieu port 1-1 ($n file)" }
  $null = GitIn $WT -c user.name=kientran1705 -c user.email=kien.tlt.1957@aptechlearning.edu.vn commit -q -m $msg -m 'Viet boi: agy (Gemini). Review: Claude.' -m 'Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>'
  $h = ((GitIn $WT rev-parse --short HEAD) | Out-String).Trim()
  Log "COMMIT-DOCS $h ($n file) nhanh docs/parity"
  exit 0
}

# ---------------- run ----------------
Ensure-Worktree
if (-not $IsDoc) {
  # QA test ban MOI NHAT cua parity/auto. Worktree QA dang ban (lan truoc de lai file) => dung lai de Claude xu ly, KHONG tu xoa.
  if (GitIn $WT status --porcelain) { throw "Worktree $WT dang co thay doi chua xu ly - Claude xem va don truoc khi chay lai" }
  $null = GitIn $WT checkout -q --detach parity/auto
}
$brief = [IO.File]::ReadAllText("$Here\briefs\BRIEF_$Role.md", [Text.Encoding]::UTF8)
$stamp = Get-Date -Format 'yyyyMMdd_HHmm'
$logF  = "$AuxDir\${Role}_$stamp.txt"
$qaOut = "$Run\qa\$Role\${stamp}_QA.md"
$port  = if ($QaPort[$Role]) { $QaPort[$Role] } else { 0 }
$head  = ((GitIn $WT rev-parse --short HEAD) | Out-String).Trim()
$br0   = ((GitIn $WT branch --show-current) | Out-String).Trim()

# ---- Ngu canh luot truoc + yeu cau cai tien (moi luot phai tot hon luot truoc) ----
$HistF = "$AuxDir\history_$Role.jsonl"
$hist = @(); if (Test-Path $HistF) { $hist = @(Get-Content $HistF -Encoding UTF8 | Where-Object { $_.Trim() } | ForEach-Object { $_ | ConvertFrom-Json }) }
$iter = $hist.Count + 1
$prev = if ($hist.Count) { $hist[-1] } else { $null }
$prevCtx = if ($prev) { "LUOT TRUOC (#$($prev.iter), $($prev.at)): pham vi='$($prev.focus)'; ket qua=$($prev.status); $($prev.minutes) phut; file: $($prev.report). DOC LAI ket qua/nhat ky luot truoc, KHONG lap lai phan da lam xong va da dung; mo rong sang phan CHUA phu hoac sau hon." } else { 'Day la LUOT DAU: hay ghi lai so do co so (baseline) de cac luot sau so sanh.' }
$propF = "$Run\proposals\${Role}_$stamp.md"
New-Item -ItemType Directory -Force -Path "$Run\proposals", "$Run\conflicts" | Out-Null
$prompt = @"
CHE DO PIPELINE PHU - vai tro $Role - LUOT SO $iter. Thu muc lam viec: $WT (HEAD $head). Shell: PowerShell.
Luat chung port: $Root\docs\PORT_RULES_MiniLoyalty.md (doc het, ke ca muc 6 'CHI LAM THEO TAI LIEU - XUNG DOT THI DUNG'). Bai hoc tich luy: $Root\docs\LESSONS_AGENTS.md (doc TRUOC, ap dung het). Nguon goc (CHI DOC): $Source. So tien do: $Repo\PARITY_PROGRESS.md (CHI DOC).
$prevCtx
YEU CAU CAI TIEN: luot nay PHAI tot hon luot truoc it nhat 3 diem do duoc (vd do phu, so loi/van de THAT tim duoc, do chinh xac, thoi gian, so gia dinh khong xac minh giam). Ghi muc 'CAI TIEN' trong TOM TAT: ... diem cai tien + so lieu so voi luot truoc.
CHI LAM THEO TAI LIEU. Mau thuan giua cac tai lieu/nguon, hoac thieu thong tin buoc phai doan: KHONG tu quyet - ghi vao $Run\conflicts\${Role}_$stamp.md (nguon mau thuan kem file:dong, dieu da thu, de xuat) va noi trong TOM TAT; Claude se chot mot lan.
DE XUAT TOI UU (neu co): ghi vao $propF - noi dung: van de quan sat duoc (kem so lieu/bang chung), de xuat sua prompt/de bai/quy trinh/bai hoc cu the (noi can sua + doan van de xuat). Chi DE XUAT - CAM tu sua script, luat, prompt hay LESSONS; Claude duyet.
Nhat ky cac hang muc da port (de bai DEBAI.md, source_excerpt.txt, TOM TAT trong write_r*.txt): $Run\logs\<so>_<ten>\ (CHI DOC).
CAM: git commit/push/checkout/switch/reset/stash/clean; sua PARITY_PROGRESS.md; sua ngoai thu muc duoc phep o tren; ghi vao $Source; chep thong tin dang nhap/mat khau/token cua nguon vao BAT KY dau.
Pham vi luot nay (Claude giao): $Focus
$(if (-not $IsDoc) { "Cong chay app QA cua ban: $port..$($port + 39) (moi lan chay app dung 1 cong trong dai nay). Bao cao ghi vao DUNG file: $qaOut" })

$brief
"@
$pf = "$AuxDir\${Role}_$stamp.prompt.txt"
[IO.File]::WriteAllText($pf, $prompt, $Utf8)
$short = "Doc TRON file $pf (de bai day du, UTF-8) roi lam DUNG va DU theo de bai do, ket thuc bang muc TOM TAT. Bat dau ngay."
$a = @('-p', $short, '--model', $AgyModel, '--add-dir', $Source, '--add-dir', "$Root\docs", '--add-dir', "$Root\repos\labs-audit", '--add-dir', $Run, '--print-timeout', '45m')

Log "BAT DAU $Role | focus: $Focus | HEAD $head"
WriteJson $ResF @{ status = 'DANG_CHAY'; role = $Role; focus = $Focus; head = $head; log = $logF; start = (Get-Date).ToString('s') }
$t0 = Get-Date; $eap = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
Push-Location $WT
$useCg = ($Engine -eq 'codegpt') -or ($Engine -eq '' -and $Role -like 'qa*' -and $env:AUX_ENGINE -ne 'agy' -and (Get-NetTCPConnection -State Listen -LocalPort 54112 -EA SilentlyContinue))
try {
  if ($useCg) {
    $pf2 = Join-Path $AuxDir "${Role}_$stamp.cg.prompt.txt"; $of2 = Join-Path $AuxDir "${Role}_$stamp.cg.txt"
    [IO.File]::WriteAllText($pf2, "cd vao $WT truoc khi chay bat ky lenh nao. Shell la PowerShell.`n" + $prompt, $Utf8)
    Log "  QA chay bang CodeGPT (bridge 54112)"
    & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $Here "cg-run.ps1") -PromptFile $pf2 -OutFile $of2 -SessionId "aux-$Role-$stamp" -MaxMinutes 40 | Out-Null
    $cgCode = $LASTEXITCODE
    $out = $(if (Test-Path $of2) { Get-Content $of2 -Raw -Encoding UTF8 } else { '' })
    if ($cgCode -eq 4) { $out += "`nRESOURCE_EXHAUSTED (CodeGPT 429)" }
  } else {
  $out = & $Agy @a 2>&1 | ForEach-Object { "$_" } | Out-String
  # nhom Gemini het han muc -> chay lai bang model Claude (nhom han muc rieng)
  if ($out -match '(?i)RESOURCE_EXHAUSTED|quota (exceeded|exhausted)|out of (ai )?credits|rate limit exceeded' -and $AgyModel -like 'gemini*') {
    Log "  nhom Gemini het han muc -> chay lai bang claude-sonnet-5-5-medium"; $a[3] = 'claude-sonnet-5-5-medium'
    $out = & $Agy @a 2>&1 | ForEach-Object { "$_" } | Out-String
  }
  }
} finally { Pop-Location; $ErrorActionPreference = $eap }
[IO.File]::WriteAllText($logF, $out, $Utf8)
$mins = [math]::Round(((Get-Date) - $t0).TotalMinutes, 1)
$st = 'XONG'
if ($out -match '(?i)RESOURCE_EXHAUSTED|quota (exceeded|exhausted)|out of (ai )?credits|rate limit exceeded') { $st = 'QUOTA' }
elseif ($out -match 'headless mode cannot prompt') { $st = 'PERMISSION' }
elseif ($out.Trim().Length -lt 20) { $st = 'EMPTY' }

# Chi PHAT HIEN vi pham, khong tu sua
$br = ((GitIn $WT branch --show-current) | Out-String).Trim()
$now = ((GitIn $WT rev-parse --short HEAD) | Out-String).Trim()
$viPham = @()
if ($br -ne $br0) { $viPham += "doi nhanh: '$br0' -> '$br'" }
if ($now -ne $head) { $viPham += "HEAD doi $head -> $now (agent tu commit/checkout)" }
$changed = @(GitIn $WT status --porcelain)
if (-not $IsDoc -and $changed.Count) { $viPham += "QA sua $($changed.Count) file trong worktree (khong duoc phep)" }
$res = [ordered]@{ status = $st; role = $Role; focus = $Focus; head = $head; minutes = $mins; log = $logF; changed = $changed; vi_pham = $viPham
  report = $(if (-not $IsDoc) { $qaOut } else { $null }); report_exists = $(if (-not $IsDoc) { Test-Path $qaOut } else { $null }); at = (Get-Date).ToString('s') }
WriteJson $ResF $res
# Ghi lich su luot nay de luot sau doc lai (ngu canh tich luy)
$fshort = ($Focus -replace '\s+', ' '); if ($fshort.Length -gt 300) { $fshort = $fshort.Substring(0, 300) + '...' }
$rec = [ordered]@{ iter = $iter; at = (Get-Date).ToString('s'); focus = $fshort; status = $st; minutes = $mins; report = $(if ($IsDoc) { ($changed -join ' ') } else { $qaOut }); log = $logF }
[IO.File]::AppendAllText($HistF, (($rec | ConvertTo-Json -Compress) + "`r`n"), $Utf8)
Log ("  {0} ({1} phut) | file doi {2} | vi pham: {3}" -f $st, $mins, $changed.Count, $(if ($viPham) { $viPham -join '; ' } else { 'khong' }))
if ($st -ne 'XONG') { exit 2 }
$res | ConvertTo-Json -Depth 4
