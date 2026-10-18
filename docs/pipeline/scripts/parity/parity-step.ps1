# Pipeline port 1-1 MiniLoyalty - chay SONG SONG 2 agent viet code (agy/Gemini va CodeGPT), Claude review + chot.
# Moi agent co worktree rieng: repos\MiniLoyalty-<worker> (nhanh parity/w-<worker>). Repo chinh (parity/auto) chi nhan
# commit da duoc Claude duyet (cherry-pick). Hai agent khong bao gio cung sua 1 controller nguon (khoa theo controller).
#
#   -Action run    : nhan 1 hang muc, agent viet, build/test, ghi result_<worker>.json roi THOAT (=> danh thuc Claude)
#   -Action fix    : agent sua tiep hang muc dang do theo file nhan xet -FixFile
#   -Action commit : Claude da duyet -> commit trong worktree, cherry-pick vao parity/auto, cap nhat so tien do
#   -Action drop   : bo hang muc dang do (reset worktree), ghi ly do vao skipped.json
#   -Action status : in trang thai 2 worker
param(
  [ValidateSet('prep','run','fix','commit','drop','status')][string]$Action = 'run',
  [ValidateSet('agy','codegpt','agy2','agy3','agy4','agy5','agy6','gweb','gweb2','gweb3','gweb4','gweb5')][string]$Worker = 'agy',
  [string]$Item = '',
  [string]$FixFile = '',
  [string]$Message = '',
  [string]$Reason = '',
  [string]$Status = 'done',          # commit: done | daco | bo
  [string]$AgyModel = 'gemini-3.8-flash-high',
  # Engine viet code cho slot nay (mac dinh = ten worker). '-Worker codegpt -Engine agy' = slot/worktree codegpt chay agy
  # khi model CodeGPT treo lien tuc (do that 16/10 04:24-04:46: #80 'stream stalled' 20 phut khong sua file nao).
  [ValidateSet('','agy','codegpt','gweb','rtr')][string]$Engine = '',
  [switch]$DryRun
)
$GwAgent = ($env:GW_AGENT -ne '0')   # mac dinh gweb chay che do AGENT (vong lap cong cu); GW_AGENT=0 -> che do 1 luot cu
if (-not $Engine -and $env:PARITY_ENGINE_FORCE -and $Worker -like 'agy*') { $Engine = $env:PARITY_ENGINE_FORCE }
if (-not $Engine) { $Engine = $(if ($Worker -like 'agy*') { 'agy' } elseif ($Worker -like 'gweb*') { 'gweb' } else { $Worker }) }
if ($env:PARITY_ENGINE_FORCE -and $Engine -eq 'agy' -and $Worker -like 'agy*') { $Engine = $env:PARITY_ENGINE_FORCE }   # lead truyen -Engine agy tuong minh -> van ep qua router neu slot duoc khoi dong voi PARITY_ENGINE_FORCE
if ($Worker -eq 'gweb2') { $env:GW_PORT = '9223'; $env:GW_TAG = 'b' }
if ($Worker -eq 'gweb3') { $env:GW_PORT = '9224'; $env:GW_TAG = 'c' }   # gweb3 = tai khoan thu 3 (Chrome cong 9224)   # gweb2 = Gemini web TAI KHOAN THU 2 (Chrome cong 9223, khoa/phien rieng)   # agy2/agy3/agy4 = them luong chay agy song song
if ($Worker -eq 'gweb4') { $env:GW_PORT = '9225'; $env:GW_TAG = 'd' }   # gweb4 = tai khoan thu 4 (Chrome cong 9225)
if ($Worker -eq 'gweb5') { $env:GW_PORT = '9226'; $env:GW_TAG = 'e' }   # gweb5 = tai khoan thu 5 (Chrome cong 9226)
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)
$OutputEncoding = [Text.UTF8Encoding]::new($false)
$env:GIT_TERMINAL_PROMPT = '0'

. "$PSScriptRoot\profile.ps1"
$Root    = 'C:\Users\Public\Pictures\kientran_private'
$Repo    = "$Root\repos\$($PJ.Name)"
$WT      = "$Root\repos\$($PJ.Name)-$Worker"
$WBranch = "parity/w-$Worker"
$Source  = $PJ.Source
$Gate    = $PJ.Gate
$ApiDir  = $PJ.ApiDir
$BizDir  = $PJ.BizDir
$Rules   = $PJ.Rules
$Lessons = "$Root\docs\LESSONS_AGENTS.md"
# CodeGPT chi doc duoc file TRONG workspace Cursor (kientran_private\repos) - doc ngoai tra 403 (do that 00:15 #88).
# -> ban SAO chi-doc trong repos\_docs, dong bo tu ban goc moi luot. Ban goc van la docs\ (nguon su that).
if ($Worker -eq 'codegpt') {
  $mirror = "$Root\repos\_docs"; New-Item -ItemType Directory -Force $mirror | Out-Null
  Copy-Item $Rules "$mirror\$(Split-Path $Rules -Leaf)" -Force
  $Rules = "$mirror\$(Split-Path $Rules -Leaf)"
  if (Test-Path $Lessons) { Copy-Item $Lessons "$mirror\LESSONS_AGENTS.md" -Force; $Lessons = "$mirror\LESSONS_AGENTS.md" }
}
$Branch  = 'parity/auto'
$Here    = Split-Path -Parent $MyInvocation.MyCommand.Path
$Run     = "$Repo\.parity-run"
$Claims  = "$Run\claims"
$Ledger  = "$Repo\PARITY_PROGRESS.md"
$SkipF   = "$Run\skipped.json"
$CoolF   = "$Run\cooldown_$Worker.json"
$CurF    = "$Run\current_$Worker.json"
$ResF    = "$Run\result_$Worker.json"
$Agy     = "$env:LOCALAPPDATA\agy\bin\agy.exe"
$Utf8    = [Text.UTF8Encoding]::new($false)
$ChuaLam = 'Ch' + [char]0x01B0 + 'a l' + [char]0x00E0 + 'm'
$StatusText = @{ done = [char]0x0110 + [char]0x00E3 + ' l' + [char]0x00E0 + 'm'; daco = [char]0x0110 + [char]0x00E3 + ' c' + [char]0x00F3; bo = 'B' + [char]0x1ECF }
New-Item -ItemType Directory -Force "$Run\logs", $Claims | Out-Null

function Log([string]$m) { $l = "[{0:yyyy-MM-dd HH:mm:ss}] [{1}] {2}" -f (Get-Date), $Worker, $m; Write-Output $l; [IO.File]::AppendAllText("$Run\run.log", $l + "`r`n", $Utf8) }
# PS 5.1: stderr cua lenh native (git in tien trinh ra stderr) bi bien thanh loi khi ErrorActionPreference=Stop -> tam ha xuong
function GitIn([string]$dir) {
  $a = $args; $eap = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
  try { $o = & git -C $dir @a 2>&1 | ForEach-Object { "$_" } } finally { $ErrorActionPreference = $eap }
  if ($LASTEXITCODE) { throw "git -C $dir $($a -join ' ') loi: $o" }
  $o
}
function ReadJson([string]$f) { if (Test-Path $f) { Get-Content $f -Raw -Encoding UTF8 | ConvertFrom-Json } else { $null } }
function WriteJson([string]$f, $o) { [IO.File]::WriteAllText($f, ($o | ConvertTo-Json -Depth 6), $Utf8) }
function Get-Skipped { $h = @{}; $j = ReadJson $SkipF; if ($j) { foreach ($p in $j.PSObject.Properties) { $h[$p.Name] = $p.Value } }; $h }

# ---------------- worktree ----------------
function Ensure-Worktree {
  if (-not (Test-Path "$WT\.git")) {
    Log "tao worktree $WT ($WBranch tu $Branch)"
    $null = GitIn $Repo worktree add -f -B $WBranch $WT $Branch
  }
}

# ---------------- chon + khoa hang muc ----------------
function Get-Todo {
  $skipped = Get-Skipped
  $rx = '^\|\s*(\d+)\s*\|\s*([^|]+?)\s*\|\s*' + [regex]::Escape($ChuaLam) + '\s*\|'
  $rows = foreach ($line in [IO.File]::ReadAllLines($Ledger, $Utf8)) {
    $m = [regex]::Match($line, $rx)
    if ($m.Success -and -not $skipped.ContainsKey($m.Groups[2].Value)) { [pscustomobject]@{ No = [int]$m.Groups[1].Value; Name = $m.Groups[2].Value } }
  }
  # Thu tu theo PHU THUOC (SOP muc 2 "gom truoc khi giao"): danh muc ma Crd_Member can -> Mst_ con lai -> Map_/Sys_
  # -> Crd_ -> Prm_/Req -> Rpt_ -> con lai. Trong 1 controller giu thu tu nguon (Search/Get thuong dung truoc).
  $first = 'Mst_Org','Mst_DealerCP','Mst_CardType','Mst_PurchaseType','Mst_ParamSys','Mst_Province','Mst_District','Mst_Ward','Mst_Country','Mst_CarBrand','Mst_CarModel','Map_DealerCP_Org','Map_QueryDealer_Member','Mst_RankPolicy','Mst_PolicyMoneyToPointService','Mst_PromotionProgram'
  $prio = { param($c) $i = [array]::IndexOf($first, $c); if ($i -ge 0) { $i } elseif ($c -like 'Mst_*') { 100 } elseif ($c -like 'Map_*' -or $c -like 'Sys_*') { 200 } elseif ($c -like 'Crd_*') { 300 } elseif ($c -like 'Prm_*' -or $c -like 'Req*') { 400 } elseif ($c -like 'Rpt_*') { 500 } else { 600 } }
  @($rows | Sort-Object @{ e = { & $prio (($_.Name -split '/')[0] -replace 'Controller$', '') } }, @{ e = { ($_.Name -split '/')[0] } }, No)
}
function Claim-Next {
  $active = @(Get-ChildItem $Claims -Filter *.claim -EA SilentlyContinue | ForEach-Object { Get-Content $_.FullName -Raw | ConvertFrom-Json })
  $otherCtrl = @($active | Where-Object { $_.worker -ne $Worker } | ForEach-Object { ($_.item -split '/')[0] })
  $mine = @($active | Where-Object { $_.worker -eq $Worker })
  if ($mine) { throw "Worker $Worker con giu hang muc $($mine[0].item) - commit/drop truoc" }
  $todo = Get-Todo
  if ($Item) { $todo = @($todo | Where-Object Name -eq $Item) }
  # Ai lam controller nao thi lam tiep controller do toi het (do xung dot + giu ngu canh)
  $affF = "$Run\affinity_$Worker.txt"
  if (-not $Item -and (Test-Path $affF)) {
    $aff = "$(Get-Content $affF -Raw)".Trim()
    $todo = @(@($todo | Where-Object { ($_.Name -split '/')[0] -eq $aff }) + @($todo | Where-Object { ($_.Name -split '/')[0] -ne $aff }))
  }
  foreach ($t in $todo) {
    if (($t.Name -split '/')[0] -in $otherCtrl -and $env:PIPE_SHARED_CTRL -ne '1') { continue }   # PIPE_SHARED_CTRL=1: nhieu worker cung controller (MobileGate: da so la chuyen tiep, hiem xung dot file)
    # gweb (Flash-Lite) chua du suc cho hang muc nhieu bang / bao cao (that bai #15): chi nhan Mst_/Map_/Sys_/Prm_/Req (tru khi chi dinh -Item)
    if ($Worker -like 'gweb*' -and -not $Item -and ($t.Name -match '^(Crd_|Rpt_|RptPE_)' -or ((Test-Path (Join-Path $Run 'gweb_skip.txt')) -and @(Get-Content (Join-Path $Run 'gweb_skip.txt')) -contains $t.Name) -or $t.Name -match '/(Save|Calc.*|Add.*|Approve.*|Appr.*|Cancel.*|Finish|Reject.*|Perform.*|Import|Create.*|Update.*|Delete.*|Sync.*|Print.*|Upload.*)$')) { continue }   # gweb chi nhan hang muc DOC (Search/Get/Export); ghi nhieu buoc de agy/codegpt
    $cf = "$Claims\{0:D3}.claim" -f $t.No
    try { $fs = [IO.File]::Open($cf, 'CreateNew', 'Write'); $b = $Utf8.GetBytes((@{ worker = $Worker; item = $t.Name; no = $t.No; at = (Get-Date).ToString('s') } | ConvertTo-Json -Compress)); $fs.Write($b, 0, $b.Length); $fs.Close() } catch { continue }
    [IO.File]::WriteAllText("$Run\affinity_$Worker.txt", ($t.Name -split '/')[0], $Utf8)
    return $t
  }
  $null
}

# ---------------- tim san vi tri nguon (de agent khoi do) ----------------
function Find-SourceMap([string]$name) {
  $ctrl, $act = $name -split '/'
  $lines = @()
  $gf = "$Gate\$ctrl.cs"
  $g = Select-String -LiteralPath $gf -Pattern "^\s*public\s+(?:async\s+|static\s+|virtual\s+|override\s+)*[\w<>\[\],\.\s]+?\s+$act\s*\(" | Select-Object -First 1
  if ($g) {
    $lines += "GATE: ${gf}:$($g.LineNumber)"
    $body = (Get-Content -LiteralPath $gf -Encoding UTF8)[($g.LineNumber - 1)..([Math]::Min($g.LineNumber + 120, (Get-Content -LiteralPath $gf).Count - 1))] -join "`n"
    $wa = [regex]::Matches($body, 'WA_\w+') | ForEach-Object Value | Select-Object -Unique -First 3
    if ($wa) { $lines += "GATE goi service: $($wa -join ', ')" }
    $base = ($ctrl -replace 'Controller$', '')
    $apiF = Get-ChildItem $ApiDir -Filter (($base -replace '_', '') + 'Controller.cs') -EA SilentlyContinue | Select-Object -First 1
    if ($apiF) {
      $lines += "API: $($apiF.FullName)"
      foreach ($w in $wa) { $h = Select-String -LiteralPath $apiF.FullName -Pattern "\b$w\b" | Select-Object -First 1; if ($h) { $lines += "  API action ${w}: $($apiF.FullName):$($h.LineNumber)" } }
    }
    $fn = "$base`_$act"
    $biz = Get-ChildItem $BizDir -Filter *.cs -Recurse -EA SilentlyContinue | Where-Object { $_.Name -notmatch 'sync-conflict|^Delete\.|\.bak' -and $_.FullName -notmatch '\\(bin|obj)\\' } |
      Select-String -Pattern "public\s+[\w<>\[\],\s]+\s(WAS_)?$fn\w*\s*\(" | Select-Object -First 8
    foreach ($b in $biz) { $lines += "BIZ ung vien: $($b.Path):$($b.LineNumber)  $($b.Line.Trim())" }
    if (-not $biz) { $lines += "BIZ: khong grep ra ham '$fn*' - tu lan theo API action" }
  } else { $lines += "GATE: khong thay action $act trong $gf" }
  $lines -join "`n"
}

# ---------------- agent ----------------
function Test-Cooldown { $j = ReadJson $CoolF; $j -and [datetime]$j.until -gt (Get-Date) }
function Set-Cooldown([int]$min, [string]$why) { WriteJson $CoolF @{ until = (Get-Date).AddMinutes($min).ToString('o'); why = $why } }
function Invoke-AgyOnce([string]$PromptText, [string]$LogFile, [string]$Model) {
  # PS 5.1 tach sai tham so native khi chuoi nhieu dong co dau " (agy bao 'unexpected argument') -> de bai ghi ra FILE,
  # tham so -p chi la 1 cau ASCII khong dau nhay tro toi file do.
  $pf = $LogFile -replace '\.txt$', '.prompt.txt'
  [IO.File]::WriteAllText($pf, $PromptText, $Utf8)
  $short = "Doc TRON file $pf (de bai day du, UTF-8) roi lam DUNG va DU theo de bai do, ket thuc bang muc TOM TAT. Bat dau ngay."
  $a = @('-p', $short, '--model', $Model, '--add-dir', $Source, '--add-dir', "$Root\docs", '--add-dir', "$Root\repos\labs-audit", '--print-timeout', '40m', '--output-format', 'stream-json')
  Push-Location $WT; $eap = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
  try { $out = & $Agy @a 2>&1 | ForEach-Object { "$_" } | Out-String } finally { Pop-Location; $ErrorActionPreference = $eap }
  # stream-json: lay response (de cac regex/TOM TAT phia sau van chay tren van ban) + usage that cua agy -> <log>.usage.json (so token so voi router)
  $raw = $out
  try {
    $resLine = @($out -split "`r?`n" | Where-Object { $_ -match '"event":"result"' }) | Select-Object -Last 1
    if ($resLine) { $rj = $resLine | ConvertFrom-Json; $out = [string]$rj.result.response + "`r`n[agy usage] " + ($rj.result.usage | ConvertTo-Json -Compress); [IO.File]::WriteAllText("$LogFile.usage.json", ($rj.result.usage | ConvertTo-Json), $Utf8) }
  } catch { $out = $raw }
  if ($raw -match '(?i)RESOURCE_EXHAUSTED|quota (exceeded|exhausted)|out of (ai )?credits|rate limit exceeded') { [IO.File]::WriteAllText($LogFile, $raw, $Utf8); return 'QUOTA' }
  [IO.File]::WriteAllText($LogFile, $out, $Utf8)
  if ($out -match '(?i)RESOURCE_EXHAUSTED|quota (exceeded|exhausted)|out of (ai )?credits|rate limit exceeded') { return 'QUOTA' }
  if ($out -match 'headless mode cannot prompt') { return 'PERMISSION' }
  if ($out -match '(?m)^Error: (unexpected argument|unknown flag|invalid)') { return 'ERROR' }
  if ($out.Trim().Length -lt 20) { return 'EMPTY' }
  'OK'
}
# agy co 2 nhom han muc RIENG (Gemini | Claude+GPT-OSS). Gemini het -> chay luon model Claude (nhom kia), nho 20 phut de khong thu lai Gemini vo ich.
$AgyFallback = $(if ($env:AGY_FALLBACK_MODEL -ne $null) { $env:AGY_FALLBACK_MODEL } else { 'claude-sonnet-4-6' })   # tai khoan agy moi (vun75584): nhom Claude Sonnet 4.6 rieng han muc Gemini
function Invoke-Agy([string]$PromptText, [string]$LogFile) {
  $gf = Join-Path $Run 'agy_gemini_quota.json'; $gj = ReadJson $gf
  $useFb = ($AgyModel -like 'gemini*') -and $gj -and ([datetime]$gj.until -gt (Get-Date))
  $m = $(if ($useFb) { $AgyFallback } else { $AgyModel })
  $res = Invoke-AgyOnce $PromptText $LogFile $m
  if ($res -eq 'QUOTA' -and $m -like 'gemini*' -and $AgyFallback) {
    WriteJson $gf @{ until = (Get-Date).AddMinutes(20).ToString('o'); why = 'GEMINI_QUOTA' }
    $null = Log "  agy: nhom Gemini het han muc -> chuyen $AgyFallback (nhom Claude/GPT)"; $m = $AgyFallback
    $res = Invoke-AgyOnce $PromptText $LogFile $m
  }
  if ($res -eq 'OK' -and $cur) { $cur | Add-Member -Force NoteProperty model $m }
  @($res)[-1]
}
# Engine 'rtr' = Claude Code (claude.exe) tro vao 9router (combo parity-gemini) - slot du phong cho viec nhe, khong ton han muc agy Pro.
function Invoke-Rtr([string]$PromptText, [string]$LogFile) {
  $pf = $LogFile -replace '\.txt$', '.prompt.txt'; [IO.File]::WriteAllText($pf, $PromptText, $Utf8)
  $cl = (Get-ChildItem "$env:USERPROFILE\.vscode\extensions\anthropic.claude-code-*\resources\native-binary\claude.exe" | Sort-Object FullName | Select-Object -Last 1).FullName
  $short = "Doc TRON file $pf (de bai day du, UTF-8) roi lam DUNG va DU theo de bai do bang cong cu, ket thuc bang muc TOM TAT. Bat dau ngay."
  $env:ANTHROPIC_BASE_URL = 'http://127.0.0.1:20128'; $env:ANTHROPIC_AUTH_TOKEN = (Get-Content "$Root\9router\router-key.txt" -Raw).Trim()
  $env:ANTHROPIC_MODEL = $(if ($env:RTR_COMBO) { $env:RTR_COMBO } else { 'parity-gemini' }); $env:ANTHROPIC_SMALL_FAST_MODEL = $env:ANTHROPIC_MODEL
  $a = @('-p', $short, '--add-dir', $Source, '--add-dir', "$Root\docs", '--permission-mode', 'acceptEdits', '--tools', 'Read,Edit,Write,Glob,Grep,Bash', '--disable-slash-commands', '--strict-mcp-config', '--max-turns', '160', '--allowedTools', 'Read', 'Edit', 'Write', 'Glob', 'Grep', 'Bash(dotnet build:*)', 'Bash(dotnet test:*)', 'Bash(git diff:*)', 'Bash(git status:*)')
  Push-Location $WT; $eap = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
  try { $out = & $cl @a 2>&1 | ForEach-Object { "$_" } | Out-String } finally { Pop-Location; $ErrorActionPreference = $eap; Remove-Item Env:ANTHROPIC_AUTH_TOKEN, Env:ANTHROPIC_BASE_URL -EA SilentlyContinue }
  [IO.File]::WriteAllText($LogFile, $out, $Utf8)
  if ($out -match '(?i)rate.?limit(ed)?\b|too many requests|quota (exceeded|exhausted)|resource_exhausted|reset after|http 429|status[^\n]{0,8}429|API Error: 429') { return 'QUOTA' }
  if ($out.Trim().Length -lt 20) { return 'EMPTY' }
  'OK'
}
function Invoke-Cg([string]$PromptText, [string]$LogFile, [string]$Session) {
  $pf = $LogFile -replace '\.txt$', '.prompt.txt'; [IO.File]::WriteAllText($pf, $PromptText, $Utf8)
  & powershell -NoProfile -ExecutionPolicy Bypass -File "$Here\cg-run.ps1" -PromptFile $pf -OutFile $LogFile -SessionId $Session | Out-Null
  # 6 = agent co lam nhung chua ra muc TOM TAT sau het continue -> van dua Claude xem (diff la bang chung, khong phai bao cao)
  switch ($LASTEXITCODE) { 0 { 'OK' } 6 { 'OK' } 3 { 'BRIDGE_DEAD' } 4 { 'QUOTA' } 7 { 'BUSY' } default { 'ERROR' } }
}
# Gemini WEB (Chrome CDP 9222, profile rieng chrome-gemini-profile). Web KHONG doc/ghi file & khong chay lenh ->
# prompt tu chua het tai lieu + file hien co; agent tra ve "FILE: <duong dan>" + 1 khoi code day du/file; script ghi vao worktree.
function Invoke-Gw([string]$PromptText, [string]$LogFile) {
  New-Item -ItemType Directory -Force "$Run\sub" | Out-Null; [IO.File]::WriteAllText("$Run\sub\web_preamble.txt", ("===== FILE $Rules =====`n" + (Get-Content $Rules -Raw -Encoding UTF8) + "`n`n===== FILE $Lessons =====`n" + (Get-Content $Lessons -Raw -Encoding UTF8)), $Utf8)
  $pf = $LogFile -replace '\.txt$', '.prompt.txt'; $wo = $LogFile -replace '\.txt$', '.web.txt'
  [IO.File]::WriteAllText($pf, $PromptText, $Utf8)
  foreach ($x in @($wo, "$wo.blocks.json", "$wo.log")) { Remove-Item $x -Force -EA SilentlyContinue }
  Push-Location "$Here\gemini-web"; $eap = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
  try { & node gw-run.mjs $pf $wo 1500 gweb "$Run\sub\web_preamble.txt" 2>&1 | Out-Null; $code = $LASTEXITCODE } finally { Pop-Location; $ErrorActionPreference = $eap }
  if ($code -eq 3) { return 'BRIDGE_DEAD' }
  if ($code -eq 4) { return 'QUOTA' }
  if ($code -ne 0) { return 'ERROR' }
  $txt = [IO.File]::ReadAllText($wo, [Text.Encoding]::UTF8); [IO.File]::WriteAllText($LogFile, $txt, $Utf8)
  $blocks = @(); $bj = Get-Content "$wo.blocks.json" -Raw -Encoding UTF8 | ConvertFrom-Json; foreach ($b in $bj) { $blocks += , [string]$b }
  $names = @([regex]::Matches($txt, '(?m)^\s*FILE:\s*(\S+)\s*$') | ForEach-Object { $_.Groups[1].Value.Trim('`', '*', ' ') })
  if ($names.Count -eq 0) { Log '  gweb: khong co dong FILE: nao'; return 'OK' }   # chi tra loi/CAN_XEM_TAY, khong sua file
  if ($names.Count -ne $blocks.Count) { Log "  gweb: so FILE ($($names.Count)) != so khoi code ($($blocks.Count)) -> khong ghi file nao"; return 'ERROR' }
  for ($i = 0; $i -lt $names.Count; $i++) {
    $rel = $names[$i] -replace '\\', '/'
    if ($rel -notmatch '^(Controllers/Port|Models/Port)/[A-Za-z0-9_]+\.cs$') { Log "  gweb: BO QUA file ngoai pham vi: $rel"; continue }
    [IO.File]::WriteAllText((Join-Path $WT ($rel -replace '/', '\')), $blocks[$i].TrimEnd() + "`r`n", $Utf8); Log "  gweb: ghi $rel ($($blocks[$i].Length) ky tu)"
  }
  'OK'
}
# Gemini WEB che do AGENT (gw-agent.mjs): web ra lenh READ/GREP/FILE/RUN build/HTTP -> cong cu chay that -> tra ket qua vao CUNG chat. Web tu doc nguon, tu build, tu chay HTTP debug.
function Invoke-GwAgent([string]$PromptText, [string]$LogFile) {
  New-Item -ItemType Directory -Force "$Run\sub" | Out-Null; [IO.File]::WriteAllText("$Run\sub\web_preamble.txt", ("===== FILE $Rules =====`n" + (Get-Content $Rules -Raw -Encoding UTF8) + "`n`n===== FILE $Lessons =====`n" + (Get-Content $Lessons -Raw -Encoding UTF8)), $Utf8)
  $tf = $LogFile -replace '\.txt$', '.task.txt'; [IO.File]::WriteAllText($tf, $PromptText, $Utf8)
  foreach ($x in @("$dir\gw_agent.result", "$dir\gw_agent_summary.txt")) { Remove-Item $x -Force -EA SilentlyContinue }
  Push-Location "$Here\gemini-web"; $eap = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
  try { & node gw-agent.mjs $WT $tf $dir 20 "agent-$($cur.no)-r$($cur.round)-$(Get-Date -Format HHmmss)" "$Run\sub\web_preamble.txt" 8700 2>&1 | Out-Null; $code = $LASTEXITCODE } finally { Pop-Location; $ErrorActionPreference = $eap }
  if (Test-Path "$dir\gw_agent_summary.txt") { Copy-Item "$dir\gw_agent_summary.txt" $LogFile -Force } else { [IO.File]::WriteAllText($LogFile, "(gw-agent khong co TOM TAT, exit $code)", $Utf8) }
  if ($code -eq 3) { return 'BRIDGE_DEAD' }
  if ($code -eq 4) { return 'QUOTA' }
  if ($code -eq 0 -or $code -eq 6) { return 'OK' }   # 6 = het luot: van dua Claude xem diff that
  'ERROR'
}
function Test-Build([string]$LogFile) {
  Push-Location $WT; $eap = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
  try {
    $b = & dotnet build -c Release -clp:ErrorsOnly 2>&1 | ForEach-Object { "$_" } | Out-String; $ok = ($LASTEXITCODE -eq 0); $t = ''
    if ($ok) { $t = & dotnet test "$($PJ.Name).Tests" -c Release 2>&1 | ForEach-Object { "$_" } | Out-String; $ok = ($LASTEXITCODE -eq 0) }
  } finally { Pop-Location; $ErrorActionPreference = $eap }
  [IO.File]::WriteAllText($LogFile, ($b + "`n===== TEST =====`n" + $t), $Utf8)
  $ok
}
function Guard-WT([string]$head) {
  $br = ((GitIn $WT branch --show-current) | Out-String).Trim()
  if ($br -ne $WBranch) { Log "  CANH BAO: agent doi nhanh sang '$br' -> quay ve"; $null = GitIn $WT switch -f $WBranch }
  $now = ((GitIn $WT rev-parse HEAD) | Out-String).Trim()
  if ($now -ne $head) { Log "  CANH BAO: agent tu commit -> reset --soft $head"; $null = GitIn $WT reset -q --soft $head }
  $null = GitIn $WT checkout HEAD -- PARITY_PROGRESS.md
}

# ---------------- so tien do ----------------
function Update-Ledger([int]$No, [string]$Name, [string]$Hash, [string]$StText) {
  $txt = [IO.File]::ReadAllText($Ledger, $Utf8)
  $txt = [regex]::Replace($txt, "(?m)^\|\s*$No\s*\|\s*$([regex]::Escape($Name))\s*\|[^|]*\|[^|]*\|", "| $No | $Name | $StText | $Hash |")
  $done = ([regex]::Matches($txt, '(?m)^\|\s*\d+\s*\|[^|]+\|\s*(' + [regex]::Escape($StatusText.done) + '|' + [regex]::Escape($StatusText.daco) + ')\s*\|')).Count
  $n = [int]([regex]::Match($txt, '\(N\)\*\*:\s*(\d+)').Groups[1].Value)
  $pct = [math]::Round(100 * $done / $n, 2)
  $txt = [regex]::Replace($txt, '(\*\*Ti\S+ \S+ hi\S+ t\S+\*\*:\s*)\d+/\d+\s*\([\d.]+%\)', "`${1}$done/$n ($pct%)")
  [IO.File]::WriteAllText($Ledger, $txt, $Utf8)
  "$done/$n ($pct%)"
}

# =============================== MAIN ===============================
if ($Action -eq 'status') {
  foreach ($w in 'agy', 'codegpt', 'agy2', 'agy3', 'agy4', 'agy5', 'agy6', 'gweb', 'gweb2', 'gweb3', 'gweb4', 'gweb5') { "== $w"; ReadJson "$Run\result_$w.json" | ConvertTo-Json -Depth 4; ReadJson "$Run\cooldown_$w.json" | ConvertTo-Json }
  "== claims"; Get-ChildItem $Claims -Filter *.claim -EA SilentlyContinue | ForEach-Object { Get-Content $_.FullName -Raw }
  exit 0
}

$cur = ReadJson $CurF

if ($Action -eq 'commit') {
  if (-not $cur) { throw "Khong co hang muc dang do cho $Worker" }
  if (((GitIn $Repo branch --show-current) | Out-String).Trim() -ne $Branch) { throw "Repo chinh phai o $Branch" }
  if (GitIn $Repo status --porcelain) { throw 'Repo chinh chua sach' }
  $st = $StatusText[$Status]; if (-not $st) { throw "Status phai la done|daco|bo" }
  $normalized = $false
  # ---- CHUAN HOA TU DONG truoc khi commit (bai hoc LESSONS A.1/A.12) tren cac file .cs moi/sua trong Controllers\Port, Models\Port ----
  foreach ($line in @(GitIn $WT status --porcelain)) {
    $rel = ($line.Substring(3)).Trim('"') -replace '/', '\'
    if ($rel -notmatch '^(Controllers\\Port|Models\\Port)\\.+\.cs$') { continue }
    $abs = Join-Path $WT $rel; if (-not (Test-Path $abs)) { continue }
    $src = [IO.File]::ReadAllText($abs, $Utf8); $new = $src
    $new = $new.Replace('Math.Max(0, r.Ft_PageIndex)', 'r.Ft_PageIndex').Replace('Math.Max(0, req.Ft_PageIndex)', 'req.Ft_PageIndex')
    $new = $new.Replace('var page = recordCount <= 0', 'var page = (recordCount <= 0 || recordStart < 0)')
    $new = $new.Replace('.Skip((int)recordStart).Take((int)recordCount)', '.Skip((int)Math.Min(recordStart, int.MaxValue)).Take((int)Math.Min(recordCount, int.MaxValue))')
    if ($new -ne $src) { [IO.File]::WriteAllText($abs, $new, $Utf8); Log "  chuan hoa tu dong (phan trang) $rel"; $normalized = $true }
    if ($new -match 'CreateFunction\s*\(') { throw "CHAN COMMIT: $rel chua CreateFunction( - cam theo LESSONS A.1 (lower() Unicode da dang ky o ha tang). Go bo roi commit lai." }
  }
  # ---- Seeder Order khong trung (LESSONS A.4): so voi cac file Models\Port da co trong repo chinh; trung thi doi sang so trong dau tien 10-99 ----
  $usedOrders = @{}
  foreach ($mf in Get-ChildItem "$Repo\Models\Port" -Filter *.cs -ErrorAction SilentlyContinue) {
    foreach ($om in [regex]::Matches([IO.File]::ReadAllText($mf.FullName, $Utf8), 'public int Order =>\s*(\d+)')) { $usedOrders[[int]$om.Groups[1].Value] = $mf.Name }
  }
  foreach ($line in @(GitIn $WT status --porcelain)) {
    $rel = ($line.Substring(3)).Trim('"') -replace '/', '\'
    if ($rel -notmatch '^Models\\Port\\.+\.cs$') { continue }
    $abs = Join-Path $WT $rel; if (-not (Test-Path $abs)) { continue }
    $txt = [IO.File]::ReadAllText($abs, $Utf8); $myName = Split-Path $rel -Leaf; $changedO = $false
    foreach ($om in @([regex]::Matches($txt, 'public int Order =>\s*(\d+)'))) {
      $o = [int]$om.Groups[1].Value
      if ($usedOrders.ContainsKey($o) -and $usedOrders[$o] -ne $myName) {
        $free = (10..99 | Where-Object { -not $usedOrders.ContainsKey($_) } | Select-Object -First 1)
        Log "  Order $o trung voi $($usedOrders[$o]) -> doi $myName sang $free"
        $txt = [regex]::Replace($txt, "public int Order =>\s*$o", "public int Order => $free"); $usedOrders[$free] = $myName; $changedO = $true
      } else { $usedOrders[$o] = $myName }
    }
    if ($changedO) { [IO.File]::WriteAllText($abs, $txt, $Utf8); $normalized = $true }
  }
  if ($normalized) { if (-not (Test-Build "$($cur.dir)\build_norm.txt")) { throw "CHAN COMMIT: sau chuan hoa tu dong build/test hong - xem $($cur.dir)\build_norm.txt" } ; Log '  build/test sau chuan hoa: xanh' }
  $null = GitIn $WT add -A
  $hash = '-'
  if (GitIn $WT diff --cached --name-only) {
    $msg = if ($Message) { $Message } else { "feat(parity): port 1-1 $($cur.item) tu 13.ClientGate" }
    $null = GitIn $WT -c user.name=kientran1705 -c user.email=kien.tlt.1957@aptechlearning.edu.vn commit -q -m $msg -m "Port: $Worker ($($cur.model)). Review: Claude Opus 5.5 PASS. Log: .parity-run/logs/$(Split-Path $cur.dir -Leaf)" -m 'Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>'
    $c = ((GitIn $WT rev-parse HEAD) | Out-String).Trim()
    $r = & git -C $Repo cherry-pick $c 2>&1
    if ($LASTEXITCODE) { $null = & git -C $Repo cherry-pick --abort 2>&1; throw "CHERRY_PICK_XUNG_DOT: $r" }
    $hash = ((GitIn $Repo rev-parse --short HEAD) | Out-String).Trim()
  }
  $p = Update-Ledger ([int]$cur.no) $cur.item $hash $st
  $null = GitIn $Repo add PARITY_PROGRESS.md
  $null = GitIn $Repo -c user.name=kientran1705 -c user.email=kien.tlt.1957@aptechlearning.edu.vn commit -q -m "docs(parity): $($cur.item) -> $st ($p)" -m 'Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>'
  $null = GitIn $WT reset -q --hard $Branch
  Remove-Item ("$Claims\{0:D3}.claim" -f [int]$cur.no) -EA SilentlyContinue; Remove-Item $CurF -EA SilentlyContinue
  Log "CHOT #$($cur.no) $($cur.item) -> $st $hash | tien do $p"
  exit 0
}

if ($Action -eq 'drop') {
  if (-not $cur) { throw "Khong co hang muc dang do cho $Worker" }
  $null = GitIn $WT reset -q --hard $Branch; $null = GitIn $WT clean -qfd
  $sk = Get-Skipped; $sk[$cur.item] = $(if ($Reason) { $Reason } else { 'Claude bo qua' }); WriteJson $SkipF $sk
  Remove-Item ("$Claims\{0:D3}.claim" -f [int]$cur.no) -EA SilentlyContinue; Remove-Item $CurF -EA SilentlyContinue
  Log "BO #$($cur.no) $($cur.item): $($sk[$cur.item])"
  exit 0
}

if (Test-Cooldown) { $c = ReadJson $CoolF; Log "agent $Worker dang nghi toi $($c.until) ($($c.why))"; WriteJson $ResF @{ status = 'COOLDOWN'; until = $c.until }; exit 2 }
if ($Action -in 'run','fix' -and $Engine -eq 'codegpt' -and -not (Get-NetTCPConnection -State Listen -LocalPort 54112 -EA SilentlyContinue)) { Log 'bridge CodeGPT chet (54112)'; WriteJson $ResF @{ status = 'AGENT_DOWN'; why = 'bridge 54112' }; exit 2 }

Ensure-Worktree
if ($Action -eq 'prep') {
  # Buoc cua BO NAO: nhan hang muc + trich nguyen van nguon. Claude doc roi viet <dir>\DEBAI.md, sau do -Action run.
  if ($cur) { throw "Worker $Worker con hang muc $($cur.item) (stage $($cur.stage)) - xu ly truoc" }
  if (GitIn $WT status --porcelain) { $null = GitIn $WT reset -q --hard; $null = GitIn $WT clean -qfd }
  $null = GitIn $WT reset -q --hard $Branch
  if ($DryRun) { Get-Todo | Select-Object -First 15 | Format-Table No, Name -AutoSize | Out-String; exit 0 }
  $t = Claim-Next
  if (-not $t) { Log 'HET_VIEC (hoac moi hang muc con lai dang bi worker kia khoa)'; WriteJson $ResF @{ status = 'HET_VIEC' }; exit 0 }
  $dir = Join-Path "$Run\logs" ("{0:D3}_{1}" -f $t.No, ($t.Name -replace '[\\/:*?"<>|]', '_'))
  New-Item -ItemType Directory -Force $dir | Out-Null
  & powershell -NoProfile -ExecutionPolicy Bypass -File "$Here\extract-source.ps1" -Item $t.Name -OutFile "$dir\source_excerpt.txt" | Out-Null
  $cur = [pscustomobject]@{ item = $t.Name; no = $t.No; dir = $dir; head = ((GitIn $WT rev-parse HEAD) | Out-String).Trim(); round = 0; worker = $Worker; stage = 'prep'; model = $(if ($Engine -eq 'agy') { $AgyModel } else { 'deepseek-v4.1-flash' }) }
  WriteJson $CurF $cur
  WriteJson $ResF @{ status = 'CAN_DEBAI'; worker = $Worker; item = $t.Name; no = $t.No; dir = $dir; excerpt = "$dir\source_excerpt.txt"; debai = "$dir\DEBAI.md" }
  Log "PREP #$($t.No) $($t.Name) -> $dir\source_excerpt.txt (cho Claude viet DEBAI.md)"
  ReadJson $ResF | ConvertTo-Json
  exit 0
}
if ($Action -eq 'run') {
  if (-not $cur -or $cur.stage -ne 'prep') { throw "Worker $Worker chua co hang muc da prep - chay -Action prep truoc" }
  $dir = $cur.dir; $t = [pscustomobject]@{ No = $cur.no; Name = $cur.item }
  if (-not (Test-Path "$dir\DEBAI.md")) { throw "Thieu $dir\DEBAI.md - Claude phai viet de bai truoc" }
  $debai = [IO.File]::ReadAllText("$dir\DEBAI.md", [Text.Encoding]::UTF8)
  $cur.stage = 'run'
  $prompt = @"
CHE DO PIPELINE - port 1-1 hang muc $($t.Name) (dong #$($t.No)).
Thu muc lam viec: $WT (worktree rieng, nhanh $WBranch - giu nguyen nhanh). Shell: PowerShell.
Luat chung: $Rules (BAT BUOC doc het, ke ca muc 6 "CHI LAM THEO TAI LIEU - XUNG DOT THI DUNG"). Bai hoc tich luy tu cac luot truoc: $Lessons (doc TRUOC khi lam, ap dung het).
Nguon nguyen van da trich san: $dir\source_excerpt.txt (CHI doc dung khoang dong de bai chi, KHONG doc lai ca file biz).
Chi lam theo DE BAI + LUAT + BAI HOC. Neu de bai, luat, bai hoc va nguon MAU THUAN nhau, hoac de bai khong du de quyet: KHONG tu quyet, KHONG doan - ghi mau thuan vao $Run\conflicts\$($t.No)_conflict.md (neu thu muc chua co thi tao), ket thuc TOM TAT voi TRANG_THAI: CAN_XEM_TAY va dong "CONFLICT: <mo ta ngan>".
CAM: git commit/push/checkout/switch/reset/stash/clean; sua PARITY_PROGRESS.md; sua ngoai $WT; lam vuot pham vi de bai.
Lam DUNG va DU de bai duoi day, khong hon khong kem. Ket thuc bang muc TOM TAT dung khuon cua de bai.
$(if ($cur.partial) { 'CHU Y: lan chay truoc bi ngat giua chung - working tree CO THE CON thay doi do dang (git status / git diff de xem). Doc lai, sua cho DUNG de bai roi lam tiep; KHONG lam lai tu dau neu phan da co dung.' })

$debai
"@
} else {
  if (-not $cur) { throw "Khong co hang muc dang do cho $Worker" }
  $dir = $cur.dir; $cur.round = [int]$cur.round + 1; $t = [pscustomobject]@{ No = $cur.no; Name = $cur.item }
  $prompt = @"
CHE DO PIPELINE - VONG SUA $($cur.round) cho hang muc: $($cur.item) (dong #$($cur.no)).
Thu muc lam viec: $WT (nhanh $WBranch). Luat: $Rules (ke ca muc 6, xung dot thi dung). Bai hoc tich luy: $Lessons.
Working tree hien tai la ban port cua vong truoc (chua commit). Nhan xet cua reviewer can sua: $FixFile
De bai goc: $dir\DEBAI.md. Nguon nguyen van: $dir\source_excerpt.txt. Sua HET muc CHAN va NEN SUA, build + test + chay that lai.
CAM nhu luat (khong commit/push/doi nhanh/sua PARITY_PROGRESS.md, khong sua ngoai $WT). Ket thuc bang TOM TAT.
"@
}
if ($Engine -eq 'codegpt') { $prompt = "cd vao $WT truoc khi chay bat ky lenh nao. Shell la PowerShell.`n" + $prompt }
$cur.model = $(if ($Engine -eq 'rtr') { 'router:parity-gemini' } elseif ($Engine -eq 'agy') { $AgyModel } elseif ($Engine -eq 'gweb') { 'gemini-web' } else { 'deepseek-v4.1-flash' })
if ($Engine -eq 'gweb' -and $GwAgent) {
  $ctrlName = ($t.Name -split '/')[0]
  $fixTxt = if ($Action -eq 'fix' -and $FixFile -and (Test-Path $FixFile)) { "`n`n===== NHAN XET CUA REVIEWER (sua het) =====`n" + (Get-Content $FixFile -Raw -Encoding UTF8) } else { '' }
  $prompt = @"
VAI TRO: agent viet code port 1-1 $($PJ.Name) (.NET 8) cho hang muc $($t.Name) (dong #$($t.No)). Noi quy + bai hoc da dan o tren (doc ky). Ban KHONG co o dia, nhung co CONG CU qua giao thuc READ/GREP/LIST/FILE/RUN/HTTP (xem cuoi) - hay TU DOC nguon va file mau thay vi doan; TU build + TU chay HTTP de debug truoc khi DONE.
Tai lieu goi y de READ truoc: "READ: @dir/source_excerpt.txt" (nguon nguyen van da trich; @dir = thu muc nhat ky hang muc; viet dung "@dir/...", KHONG dung duong dan tuyet doi vi web nuot dau gach nguoc) va "READ: @dir/DEBAI.md", Data/PortResult.cs, Data/PortQuery.cs, Controllers/Port/${ctrlName}Port.cs (neu da co), mau: Controllers/Port/Mst_MajorControllerPort.cs + Models/Port/Mst_Major.cs. KHONG bia ten kieu/ham: GREP truoc khi dung. Entity chua co thi tao trong Models/Port theo mau (kem IPortSeeder neu can).
CHI ghi file duoi Controllers/Port, Models/Port, $($PJ.Name).Tests. Mau thuan/thieu thong tin => KHONG doan: dong "CONFLICT: ..." + TRANG_THAI: CAN_XEM_TAY trong DONE. Truoc DONE: RUN: build xanh, RUN: test xanh, va HTTP that cho MOI action cua hang muc (loi + thanh cong).
===== DE BAI =====
$debai$fixTxt
"@
} elseif ($Engine -eq 'gweb') {
  # Prompt tu-chua: nhung tai lieu + file hien co trong worktree + mau phong cach
  $ctrlName = ($t.Name -split '/')[0]
  $embed = @("$dir\source_excerpt.txt", "$Repo\Data\PortResult.cs", "$Repo\Data\PortQuery.cs")   # Rules+Lessons nam trong preamble (gui 1 lan/chat)
  $ex = Get-ChildItem "$WT\Controllers\Port" -Filter "$ctrlName*Port.cs" -EA SilentlyContinue | Select-Object -First 1
  if ($ex) { $embed += $ex.FullName; $mf = Join-Path "$WT\Models\Port" (($ctrlName -replace 'Controller$', '') + '.cs'); if (Test-Path $mf) { $embed += $mf } }
  else { $embed += @("$WT\Controllers\Port\Mst_MajorControllerPort.cs", "$WT\Models\Port\Mst_Major.cs") }   # mau phong cach cho controller moi
  $ctx = ($embed | Where-Object { $_ -and (Test-Path $_) } | ForEach-Object { "`n`n===== FILE $_ =====`n" + (Get-Content $_ -Raw -Encoding UTF8) }) -join ''
  $fixTxt = if ($Action -eq 'fix' -and $FixFile -and (Test-Path $FixFile)) { "`n`n===== NHAN XET CUA REVIEWER (sua het) =====`n" + (Get-Content $FixFile -Raw -Encoding UTF8) } else { '' }
  $prompt = @"
VAI TRO: agent viet code (port 1-1 $($PJ.Name), .NET 8) cho hang muc $($t.Name) (dong #$($t.No)). Ban la ban WEB: KHONG doc/ghi file, KHONG chay lenh; moi tai lieu va file hien co da dan o cuoi. Chi lam theo DE BAI + LUAT + BAI HOC; mau thuan/thieu thong tin thi KHONG doan: ghi dong "CONFLICT: ..." va TRANG_THAI: CAN_XEM_TAY.
DAU RA BAT BUOC: voi MOI file can sua/tao, viet 1 dong chinh xac "FILE: <duong dan tuong doi>" (vd FILE: Controllers/Port/Mst_PositionControllerPort.cs) roi NGAY SAU DO 1 khoi code (```csharp) chua NOI DUNG DAY DU cua file (khong rut gon, khong "..."). CHI duoc sua/tao file duoi Controllers/Port/ va Models/Port/. Khong viet dong "FILE:" nao khac trong cau tra loi. Sau cac file la muc TOM TAT dung khuon cua de bai; o VERIFY ghi "chua chay that (web) - Claude/QA se chay"; khong khang dinh da build/test.
Giu nguyen moi phan khac cua file hien co (chi them/sua dung phan de bai yeu cau). Comment tieng Viet co dau dan file:dong nguon. Khong dung CreateFunction.

===== DE BAI =====
$debai$fixTxt
$ctx
"@
}

$r = $cur.round
$log = "$dir\write_r${r}_$Worker.txt"
Log "BAT DAU #$($t.No) $($t.Name) | vong $r | model $($cur.model)"
WriteJson $CurF $cur
$t0 = Get-Date
$wres = 'ERROR'
for ($try = 1; $try -le 2; $try++) {
  if ($try -gt 1 -and (Test-Path $log)) { Copy-Item $log ($log -replace '\.txt$', "_try$($try - 1).txt") -Force }   # giu log lan truoc de chan doan
  $wres = if ($Engine -eq 'rtr') { Invoke-Rtr $prompt $log } elseif ($Engine -eq 'agy') { Invoke-Agy $prompt $log } elseif ($Engine -eq 'gweb') { if ($GwAgent) { @(Invoke-GwAgent $prompt $log) | Select-Object -Last 1 } else { @(Invoke-Gw $prompt $log) | Select-Object -Last 1 } } else { Invoke-Cg $prompt $log "parity-$($t.No)-r$r-t$try" }
  if ($wres -eq 'OK' -or $wres -eq 'QUOTA' -or $wres -eq 'PERMISSION') { break }
  Log "  lan $try -> $wres, thu lai sau 30s"; Start-Sleep 30
}
$mins = ((Get-Date) - $t0).TotalMinutes
Log ("  agent -> {0} ({1:N1} phut)" -f $wres, $mins)
if ($wres -ne 'OK') {
  $qm = 60; if ($wres -eq 'QUOTA' -and (Test-Path $log)) { $mm = [regex]::Match((Get-Content $log -Raw -Encoding UTF8), 'Resets in (?:(\d+)h)?(?:(\d+)m)?'); if ($mm.Success -and ($mm.Groups[1].Value -or $mm.Groups[2].Value)) { $qm = [math]::Min(10080, [int]($mm.Groups[1].Value -as [int]) * 60 + [int]($mm.Groups[2].Value -as [int]) + 1) } }   # doc 'Resets in XhYm' cua agy
  Set-Cooldown $(if ($wres -eq 'QUOTA') { $qm } elseif ($wres -eq 'BUSY') { 5 } else { 15 }) $wres
  if ($Action -eq 'run') {
    # Giu hang muc + DEBAI (stage quay ve 'prep') de chay lai -Action run la tiep tuc duoc, khong mat de bai Claude da viet.
    # Reset ve DUNG cur.head (KHONG ve $Branch): $Branch co the da tien len do worker kia commit -> HEAD lech cur.head
    # -> Guard-WT bao nham "agent tu commit" va diff lan code cua worker kia (su co that #88, 00:46).
    # KHONG xoa code dang do (CodeGPT mat ~30 phut/lan neu reset) - chi dua HEAD ve cur.head, giu working tree;
    # lan chay lai prompt se bao agent doc phan da co roi lam tiep. Claude van review diff that truoc khi commit.
    $null = GitIn $WT reset -q --soft $cur.head
    $cur.stage = 'prep'; $cur | Add-Member -Force NoteProperty partial $true; WriteJson $CurF $cur
  }
  WriteJson $ResF @{ status = 'AGENT_DOWN'; why = $wres; item = $t.Name }
  exit 2
}

Guard-WT $cur.head
$null = GitIn $WT add -A
$changed = @(GitIn $WT diff --cached --name-only)
$null = GitIn $WT diff --cached --output="$dir\diff_r$r.patch"
$buildOk = $false
if ($changed.Count) { $buildOk = Test-Build "$dir\build_r$r.txt" }
# gweb chi viet ban NHAP (khong build duoc): build loi -> agy chi SUA LOI BIEN DICH (re, it token), toi da 2 luot, roi moi toi Claude
if ($Engine -eq 'gweb' -and $changed.Count -and -not $buildOk) {
  for ($fx = 1; $fx -le 2 -and -not $buildOk; $fx++) {
    $errs = @(Get-Content "$dir\build_r$r.txt" -Encoding UTF8 | Where-Object { $_ -match 'error CS' } | ForEach-Object { $_ -replace '\s*\[[A-Za-z]:[^\]]*\]\s*$', '' -replace '^.*?(Controllers|Models)\\Port\\', '$1\Port\' } | Select-Object -Unique -First 40)
    $fp = @"
CHE DO SUA LOI BIEN DICH (rat ngan, KHONG viet lai tu dau). Thu muc: $WT (worktree, nhanh $WBranch). Shell: PowerShell.
Working tree la ban NHAP do agent web viet cho hang muc $($t.Name) (dong #$($t.No)); no KHONG build duoc. Nhiem vu DUY NHAT: sua cho 'dotnet build' qua, giu nguyen y dinh logic cua ban nhap va de bai ($dir\DEBAI.md, nguon $dir\source_excerpt.txt).
Loi hay gap: ten kieu/entity bia (tim dung ten that trong Models/Port, Data), thieu using, kieu cot (string vs DateTime?), ham helper khong ton tai (khong tu che - xem Data\PortResult.cs, PortQuery.cs, PortSeq). Ten kieu khong ton tai ma de bai can => tao entity trong Models/Port theo mau file ben canh, KHONG tao lop gia.
CAM: git commit/push/checkout/reset/stash/clean; sua ngoai $WT; doi logic/hanh vi. Chi sua trong Controllers/Port va Models/Port.
LOI BUILD:
$($errs -join "`n")
Ket thuc bang 3 dong TOM TAT: da sua gi / con loi nao / gia dinh.
"@
    Log "  gweb ban nhap build loi ($($errs.Count) loi) -> agy sua loi bien dich luot $fx"
    $fres = Invoke-Agy $fp "$dir\buildfix_r${r}_$fx.txt"
    if ($fres -ne 'OK') { Log "  agy sua loi -> $fres (de Claude xu ly)"; break }
    $null = GitIn $WT add -A
    $buildOk = Test-Build "$dir\build_r$r.txt"
  }
  $changed = @(GitIn $WT diff --cached --name-only)
  $null = GitIn $WT diff --cached --output="$dir\diff_r$r.patch"
}
$cur | Add-Member -Force NoteProperty minutes ([math]::Round($mins, 1))
WriteJson $CurF $cur
$res = [ordered]@{ status = 'CHO_CLAUDE'; worker = $Worker; item = $t.Name; no = $t.No; round = $r; dir = $dir; worktree = $WT
  changed = $changed; build = $buildOk; minutes = [math]::Round($mins, 1); at = (Get-Date).ToString('s') }
WriteJson $ResF $res
Log "  CHO_CLAUDE #$($t.No) $($t.Name) | file doi $($changed.Count) | build/test $buildOk"
$res | ConvertTo-Json -Depth 4
