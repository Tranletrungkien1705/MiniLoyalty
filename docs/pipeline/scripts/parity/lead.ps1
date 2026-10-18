# "CON DUNG GIUA" cho pipeline port 1-1 MiniLoyalty: agent LEAD (agy + Claude Sonnet 5.5) lam trung gian giua LINH (agy/CodeGPT) va CLAUDE (Code).
#   - soan DEBAI.md (co model du phong), nap viec cho linh ranh, tu thu lai linh bi loi (toi da MaxAttempts roi drop),
#   - bat agent PHU (docs/QA) theo hang doi aux_queue.json (chay tach roi, khong danh thuc Claude rieng le),
#   - gom ket qua thanh DIGEST, CHI danh thuc Claude khi du lo / qua han / bat thuong / xung dot / du ket qua phu.
# CO CHE AN TOAN (khong lap vo han): MaxLoopMin; MaxAttempts moi hang muc; ngat mach khi lead that bai lien tiep / >=2 linh het han muc;
# PAUSE_FEED tam dung nap viec; phat hien linh treo; file STOP_LEAD de dung ngay.
# LEAD KHONG BAO GIO commit, khong sua PARITY_PROGRESS.md, khong sua luat/script/prompt (chi DE XUAT). Claude duyet va chot.
param(
  [ValidateSet('loop','digest','feed')][string]$Action = 'loop',
  [string[]]$Workers = @('agy', 'agy2', 'agy3', 'agy4', 'agy5', 'agy6', 'codegpt', 'gweb', 'gweb2', 'gweb3', 'gweb4', 'gweb5'),   # TAT CA slot (theo doi + digest)
  [string[]]$FeedOnly = @('agy', 'agy2', 'agy3', 'codegpt', 'gweb3'),   # slot ma tien trinh NAY soan de bai + nap (cac slot khac do feeder.ps1 lo)
  [string[]]$LeadModels = @('gemini-3.8-flash-high', 'claude-sonnet-4-6', 'gemini-web'),   # soan de bai bang agy (song song duoc, tai khoan Pro moi) truoc; web chi du phong (khoa lane tranh voi code gweb)
  [int]$MinBatch = 3,
  [int]$MaxWaitMin = 12,
  [int]$MaxLoopMin = 45,
  [int]$PollSec = 30,
  [int]$MaxAttempts = 3,
  [int]$StuckMin = 40,
  [int]$AuxWakeMin = 20,
  [switch]$NoFeed
)
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)
$OutputEncoding = [Text.UTF8Encoding]::new($false)
$env:GIT_TERMINAL_PROMPT = '0'

$Root    = 'C:\Users\Public\Pictures\kientran_private'
. "$PSScriptRoot\profile.ps1"
$Repo    = "$Root\repos\$($PJ.Name)"
$Run     = "$Repo\.parity-run"
$Step    = "$Root\scripts\parity\parity-step.ps1"
$AuxStep = "$Root\scripts\parity\aux-agent.ps1"
$Source  = $PJ.Source
$Rules   = $PJ.Rules
$Lessons = "$Root\docs\LESSONS_AGENTS.md"
$Agy     = "$env:LOCALAPPDATA\agy\bin\agy.exe"
$Utf8    = [Text.UTF8Encoding]::new($false)
$SubDir = "$Run\sub"; $DigDir = "$Run\digest"; $ConfDir = "$Run\conflicts"; $PropDir = "$Run\proposals"
foreach ($d in @($SubDir, $DigDir, $ConfDir, $PropDir)) { if (-not (Test-Path $d)) { $null = New-Item -ItemType Directory -Path $d -Force } }
$StopF   = "$Run\STOP_LEAD"
$PauseF  = "$Run\PAUSE_FEED"
$StateF  = "$Run\lead_state.json"
$QueueF  = "$Run\aux_queue.json"
$Allowed = '^(Controllers/Port/|Models/Port/)'
$AuxRoles = @('doc1', 'doc2', 'qa1', 'qa2')

function Log([string]$m) { $l = "[{0:yyyy-MM-dd HH:mm:ss}] [lead] {1}" -f (Get-Date), $m; Write-Output $l; [IO.File]::AppendAllText("$Run\run.log", $l + "`r`n", $Utf8) }
function ReadJson([string]$f) { if (Test-Path $f) { try { Get-Content $f -Raw -Encoding UTF8 | ConvertFrom-Json } catch { $null } } else { $null } }
function Cur([string]$w) { ReadJson "$Run\current_$w.json" }
function Res([string]$w) { ReadJson "$Run\result_$w.json" }
function Get-State { $s = ReadJson $StateF; $h = @{ attempts = @{}; leadFails = 0; modelSkip = @{} }; if ($s) { if ($s.leadFails) { $h.leadFails = [int]$s.leadFails }; if ($s.attempts) { foreach ($p in $s.attempts.PSObject.Properties) { $h.attempts[$p.Name] = [int]$p.Value } }; if ($s.modelSkip) { foreach ($p in $s.modelSkip.PSObject.Properties) { $h.modelSkip[$p.Name] = [string]$p.Value } } }; $h }
function Save-State($h) { [IO.File]::WriteAllText($StateF, ($h | ConvertTo-Json -Depth 5), $Utf8) }
$script:State = Get-State
$script:Anomalies = New-Object System.Collections.ArrayList
function Anomaly([string]$m) { if (-not $script:Anomalies.Contains($m)) { $null = $script:Anomalies.Add($m) } }

# ---------------- soan DEBAI bang agent LEAD (co model du phong) ----------------
function Find-Template([string]$item) {
  $act = ($item -split '/')[1]
  $done = @{}
  foreach ($line in [IO.File]::ReadAllLines("$Repo\PARITY_PROGRESS.md", $Utf8)) {
    $m = [regex]::Match($line, '^\|\s*(\d+)\s*\|\s*([^|]+?)\s*\|\s*(\S.*?)\s*\|\s*([0-9a-f]{7,})\s*\|')
    if ($m.Success -and $m.Groups[4].Value -ne '-') { $done[[int]$m.Groups[1].Value] = $m.Groups[2].Value }
  }
  foreach ($no in ($done.Keys | Sort-Object -Descending)) {
    if ($done[$no] -like "*/$act") {
      $dir = Get-ChildItem "$Run\logs" -Directory -Filter ("{0:D3}_*" -f $no) -ErrorAction SilentlyContinue | Select-Object -First 1
      if ($dir -and (Test-Path "$($dir.FullName)\DEBAI.md")) { return "$($dir.FullName)\DEBAI.md" }
    }
  }
  $null
}
# Che do MAU (LEAD_TEMPLATE=1): khong dung LLM soan de bai (cham, hay KHONG_RA_DEBAI). Tu dong:
#  - khong co ham biz trong excerpt, hoac biz CHI chuyen tiep sang dich vu ngoai (ClientService .Instance.WA_* / GetUrlCurrentApiNetwork) va khong co SQL => NO_BACKEND (Bo).
#  - nguoc lai ghi DEBAI.md khuon co dinh; agent tu doc source_excerpt va tu lap bang doi chieu (Claude review sau).
function Draft-Template([string]$w, $cur) {
  $dir = $cur.dir; $out = "$dir\DEBAI.md"; $ex = "$dir\source_excerpt.txt"
  $txt = if (Test-Path $ex) { Get-Content $ex -Raw -Encoding UTF8 } else { '' }
  $bizIdx = $txt.IndexOf('BIZ ')
  $biz = if ($bizIdx -ge 0) { $txt.Substring($bizIdx) } else { '' }
  $hasSql = $biz -match 'ExecQuery|ExecuteNonQuery|ExecSql|ExecuteScalar|SqlCommand|zzzzClause|\.SaveData\(|_cf\.db\.(ExecQuery|ExecuteNonQuery|ExecSql|SaveData)|\w+_CheckDB\('
  $fwd = $biz -match 'Services?\.Instance\.|ClientService|GetUrlCurrentApiNetwork|AccountService|HttpClient|RestClient|WebRequest|PostData|CmSys_InvalidOutSite'
  $fwdStrong = ($biz -match 'GetUrlCurrentApiNetwork') -and ($biz -match 'Services?\.Instance\.')   # lay URL mang roi goi dich vu ngoai = chuyen tiep, du wrapper co CheckDB
  if ($biz -and (($fwd -and -not $hasSql) -or $fwdStrong)) {   # chuyen tiep thuan tuy -> Bo TRUOC khi goi agent (0 token); khong co biz trong excerpt thi de agent xem
    $why = if (-not $biz) { 'excerpt khong co ham biz' } else { 'biz chi chuyen tiep sang dich vu ngoai (ClientService/HTTP), khong co SQL' }
    $ev = @($biz -split "`n" | Where-Object { $_ -match 'Services?\.Instance\.|ClientService|GetUrlCurrentApiNetwork|AccountService|HttpClient|RestClient|WebRequest|PostData' } | Select-Object -First 6)
    New-Item -ItemType Directory -Force $ConfDir | Out-Null
    [IO.File]::WriteAllText("$ConfDir\$($cur.no)_conflict.md", "# XUNG DOT #$($cur.no) $($cur.item) - de xuat BO (tu dong, che do mau)`r`n`r`nLy do: $why`r`n`r`nBang chung (trich tu source_excerpt):`r`n$($ev -join "`r`n")`r`n", $Utf8)
    [IO.File]::WriteAllText("$dir\NO_BACKEND.flag", '', $Utf8)
    return $true
  }
  $item = $cur.item; $no = $cur.no
  $doc = @"
# DE BAI #$no - $item (port 1-1 $($PJ.Name)) - KHUON TU DONG

Nguon NGUYEN VAN: $ex (cac muc 1 GATE/controller, 3.x BIZ). Luat: $Rules. Bai hoc: $Lessons. Tu doc het; mau thuan => dung theo muc 6 cua luat (CONFLICT), KHONG doan.

## P1 - TRA LOI NGAY DAU (tu doc nguon, moi y co file:dong)
1. Route + method + kieu body? 2. Cac buoc nghiep vu theo thu tu (token/Gw/CheckAuthorize, goi biz nao, nhanh re)? 3. Moi tham so dau vao: ten, chuan hoa (StdParam/StdStr/StdFlag), mac dinh? 4. Cac ma loi va dieu kien sinh ra (TError.*; chep dung ten)? 5. Hinh dang ket qua (JsonResultEntry/ServiceResult: Success, Data, ...) khi thanh cong va khi rong? 6. Bang nao doc/ghi, transaction? 7. Cho nao goi dich vu NGOAI (ClientService/HTTP) - chi liet ke, khong bia.
(Dap an de reviewer doi chieu: tu lap sau khi doc nguon)

## P2 - CON SO KY VONG
Tu tinh tay tu nguon cho tung ca kiem tra (it nhat: body rong, 1 ca moi guard, 1 ca thanh cong round-trip neu la ghi; 2 bo loc ra so dong khac nhau neu la doc). Seed du lieu mau bang `IPortSeeder` (Order chua dung - grep `Order =>` trong Models/Port va Controllers/Port; 10-99 danh muc, >=200 nghiep vu).

## P3 - RANG BUOC
File: Controllers/Port/<Controller>Port.cs (MOI controller 1 file), entity Models/Port/<Bang>.cs, test trong $($PJ.Name).Tests. Route /api/<controller-viet-thuong>/<action>, POST. Dung PortResult.Fail / PortQuery / PortSeq / PortExcel co san. KHONG gia tri tu che (mac dinh/alias/tham so ngoai nguon). Phan goi dich vu ngoai: bo qua va ghi GAP (khong mo phong). NEU toan bo tinh toan/du lieu nam o dich vu ngoai (biz chi goi *Service.Instance.* / DMS_WA_* roi tra ket qua): DUNG, tao NO_BACKEND.flag + conflicts\<so>_conflict.md, TUYET DOI KHONG tu che bang/ket qua/gia tri gia (so tra ve phai den tu nguon, khong tu dien). Chay that tren cong 8500-8599 voi DB SQLite tam. TEST SERVER THAT: neu viet test khoi dong server that, CAM dung cong co dinh - dung FreePort.Get() (da co trong <Du an>.Tests/FreePort.cs) cho PORT va dia chi 127.0.0.1; xem test hien co lam mau. TRUNG KIEU: nhieu controller dung CHUNG 1 ham biz (vd EQR/QRBrand/InBrand cung WA_MstSv_Mst_Network_GetMinVersion). TRUOC khi viet: grep Controllers/Port va Models/Port xem kieu (c_K_DT_Sys, c_K_DT_SysInfo, RT_*, entity MstSv_*) da co chua - co thi DUNG LAI, KHONG dinh nghia lai (trung ten = loi build/xung dot khi chot); entity chi them neu chua co file Models/Port/<Bang>.cs. TIET KIEM TOKEN: source_excerpt.txt DA chua day du chuoi gate->biz; doc NO 1 LAN roi lam, CAM mo lai/grep lai cac file nguon trung lap, CAM doc file biz khong nam trong excerpt tru khi excerpt bao thieu ham; viet code 1 luot, chay build+test+verify toi da 2 lan.

## P4 - KHUON BAO CAO
Ket thuc bang muc TOM TAT theo luat (TRANG_THAI, NGUON, FILE_DA_SUA, DOI_CHIEU K/T/S/D, VERIFY, GAP, CONFLICT_RISK).
"@
  [IO.File]::WriteAllText($out, $doc, $Utf8)
  $true
}

function Draft-Debai([string]$w, $cur) {
  $dir = $cur.dir; $out = "$dir\DEBAI.md"
  if ($env:LEAD_TEMPLATE -eq '1') { return (Draft-Template $w $cur) }
  $tpl = Find-Template $cur.item
  $wt = "$Root\repos\$($PJ.Name)-$w"
  $ctrl = (($cur.item -split '/')[0])
  $existing = Get-ChildItem "$wt\Controllers\Port" -Filter "$ctrl*Port.cs" -ErrorAction SilentlyContinue | Select-Object -First 1
  $prompt = @"
VAI TRO: LEAD (tro ly dieu phoi) cua pipeline port 1-1 $($PJ.Name). SOAN DE BAI (DEBAI.md) cho hang muc $($cur.item) (dong #$($cur.no)) de agent LINH (Gemini Flash) viet code. Claude se review ket qua.
CHI LAM THEO TAI LIEU: $Rules (doc het, ke ca muc 6), $Lessons (bai hoc tich luy - AP DUNG HET, de bai KHONG duoc yeu cau dieu bai hoc cam), nguon $dir\source_excerpt.txt. Mau thuan giua cac tai lieu / thieu thong tin buoc phai doan: KHONG tu quyet - ghi muc "XUNG DOT" trong de bai (neu linh se gap) va dong "CONFLICT_RISK: ..." trong TOM TAT cua ban.
Worktree dich cua linh (CHI DOC): $wt. $(if ($existing) { "Controller port DA CO: $($existing.FullName) - doc de DUNG LAI ham co san (GetXAsync, CreateXAsync...), KHONG tao lai entity da co." } else { 'Chua co controller port cho controller nay -> de bai phai yeu cau tao entity + controller moi.' })
DE BAI MAU cung dang action (chi hoc KHUON, KHONG chep so lieu): $(if ($tpl) { $tpl } else { "$Run\logs\107_Mst_CertificateGroupController_Search\DEBAI.md" })
YEU CAU:
1. Doc TRON source_excerpt (gate, ham loc, model, biz). Moi khang dinh ve nguon phai co file:dong; khong chac thi ghi "Chua xac minh"; CAM bia.
2. Viet de bai theo khuon P1-P4 cua de bai mau (nguon, P1 cau hoi, P2 CON SO KY VONG tinh tay tung ca, P3 rang buoc gom ten file/route/cong chay that, P4 khuon bao cao).
3. Diem de sai: toan tu loc (= hay IN, co tach dau phay), cot KeyWord, thu tu sap xep, so cot model, PK, OrgID co loc khong, hinh dang Data khi rong (null hay mang), transaction/mot phan, ma loi, chuan hoa StdParam/StdStr/StdFlag. KHONG dat ky vong phu thuoc chu HOA CO DAU tren SQLite (xem bai hoc A.1).
4. Seeder: chon so Order CHUA DUNG - grep "Order =>" trong $wt\Models\Port va chon so 10-99 chua co (khong trung). Cong chay that: chon 1 cong trong dai 8500-8599 chua dung trong cac de bai khac.
5. Them cuoi muc P1 mot dong "(Dap an de reviewer doi chieu: ...)" voi dap an dung cua cac cau hoi P1 (viet tren MOT dong).
6. TU VIET - CAM doc/sao chep de bai cua hang muc KHAC ngoai de bai mau. GHI de bai vao dung file: $out
7. Ket thuc bang muc TOM TAT (3 dong): diem da xac dinh tu nguon, diem khong chac, CONFLICT_RISK (neu co).
8. KIEM TRA BACKEND TRUOC KHI VIET DE BAI: tu grep $($PJ.Source) (cac file .cs trong $($PJ.BizDir) va $($PJ.ApiDir)) xem co ham THAT cho hang muc nay khong (WA_<X>_<Action>, <X>_<Action>, <X>_<Action>X) theo ten controller/action o tren. Neu gate chi goi dich vu NGOAI (ClientService PostData) va backend KHONG co ham nao: TUYET DOI KHONG viet de bai va KHONG doan quy tac/ma loi. Thay vao do: ghi $ConfDir\$($cur.no)_conflict.md (dan chung cu file:dong cua gate va ket qua grep rong, de xuat 'bo') va tao file rong $dir\NO_BACKEND.flag. (Bai hoc LESSONS B.7; vi du #159.)
"@
  $pf = "$SubDir\lead_prompt_$($cur.no).txt"; [IO.File]::WriteAllText($pf, $prompt, $Utf8)
  $short = "Doc TRON file $pf (de bai day du, UTF-8) roi lam DUNG theo do. Bat dau ngay."
  foreach ($model in $LeadModels) {
    # Model vua het han muc (QUOTA) -> bo qua 30 phut, khong goi lai moi lan (tiet kiem ~vai giay + 1 luot loi moi de bai)
    if ($script:State.modelSkip.ContainsKey($model) -and [datetime]$script:State.modelSkip[$model] -gt (Get-Date)) { continue }
    if (Test-Path $out) { Remove-Item $out -Force -ErrorAction SilentlyContinue }
    if ($model -eq 'gemini-web') {
      # Gemini WEB (Chrome CDP 9222, tai khoan rieng, khong tru quota agy): khong doc duoc file -> NHUNG toan bo tai lieu vao prompt
      $wp = "$SubDir\lead_webprompt_$($cur.no).txt"; $wo = "$SubDir\lead_webout_$($cur.no).txt"
      $parts = @($prompt, "`n`n===== LUU Y CHO BAN WEB: ban KHONG doc duoc file/grep. Moi tai lieu da duoc dan ben duoi. Bo qua yeu cau ghi file va muc 8 (grep backend): thay bang - neu source_excerpt khong co doan BIZ/backend that thi ghi 'CONFLICT_RISK: thieu backend'. TRA VE DUY NHAT noi dung DEBAI.md (markdown, khong loi dan, khong boc code fence ngoai cung), ket thuc bang muc TOM TAT. =====")
      $exFiles = @(); if ($existing) { $exFiles += $existing.FullName; $mf = Join-Path "$wt\Models\Port" (($ctrl -replace 'Controller$', '') + '.cs'); if (Test-Path $mf) { $exFiles += $mf } }
      if ($exFiles.Count) { $parts += "`n`n===== LUU Y: cac file duoi day DA CO trong repo dich (controller + entity/seeder). De bai phai DUNG LAI ham/entity/seeder co san (KHONG tao lai seeder/entity, KHONG cap so Order moi), chi them action con thieu. =====" }
      $pre = "$SubDir\web_preamble.txt"; [IO.File]::WriteAllText($pre, ("===== FILE $Rules =====`n" + (Get-Content $Rules -Raw -Encoding UTF8) + "`n`n===== FILE $Lessons =====`n" + (Get-Content $Lessons -Raw -Encoding UTF8)), $Utf8)   # noi quy+bai hoc: gui 1 LAN/chat (gw-run.mjs --session)
      foreach ($f in @("$dir\source_excerpt.txt", $tpl) + $exFiles) { if ($f -and (Test-Path $f)) { $parts += "`n`n===== FILE $f =====`n" + (Get-Content $f -Raw -Encoding UTF8) } }
      [IO.File]::WriteAllText($wp, ($parts -join ''), $Utf8)
      if (Test-Path $wo) { Remove-Item $wo -Force }
      $eap = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
      Push-Location "$Root\scripts\parity\gemini-web"
      $oldP = $env:GW_PORT; $oldT = $env:GW_TAG
      if ($env:LEAD_ACCT -eq 'b' -and (Get-NetTCPConnection -State Listen -LocalPort 9223 -EA SilentlyContinue)) { $env:GW_PORT = '9223'; $env:GW_TAG = 'b' }   # LEAD_ACCT (bien moi truong cua tien trinh feeder) chon tai khoan soan de bai
      elseif ($env:LEAD_ACCT -eq 'c' -and (Get-NetTCPConnection -State Listen -LocalPort 9224 -EA SilentlyContinue)) { $env:GW_PORT = '9224'; $env:GW_TAG = 'c' }
      elseif ((Test-Path (Join-Path $Run "LEAD_ACCT_C")) -and (Get-NetTCPConnection -State Listen -LocalPort 9224 -EA SilentlyContinue)) { $env:GW_PORT = '9224'; $env:GW_TAG = 'c' }
      elseif ((Test-Path (Join-Path $Run "LEAD_ACCT_B")) -and (Get-NetTCPConnection -State Listen -LocalPort 9223 -EA SilentlyContinue)) { $env:GW_PORT = '9223'; $env:GW_TAG = 'b' }   # lead soan de bai bang tai khoan B (file LEAD_ACCT_B)
      try { & node gw-run.mjs $wp $wo 600 lead $pre 2>&1 | Out-Null; $code = $LASTEXITCODE } finally { $env:GW_PORT = $oldP; $env:GW_TAG = $oldT; Pop-Location; $ErrorActionPreference = $eap }
      $why = ''
      if ($code -ne 0) { $why = "WEB_EXIT_$code" }
      else {
        $txt = Get-Content $wo -Raw -Encoding UTF8
        if ($txt.Length -lt 800 -or $txt -notmatch 'P2') { $why = 'KHONG_RA_DEBAI' } else { [IO.File]::WriteAllText($out, $txt + "`r`n<!-- DEBAI do LEAD ($model) soan $(Get-Date -Format s); Claude chua review truoc khi chay -->`r`n", $Utf8); $script:State.leadFails = 0; Save-State $script:State; return $true }
      }
      if ($code -eq 4) { $script:State.modelSkip[$model] = (Get-Date).AddMinutes(30).ToString('o'); Save-State $script:State }
      Log "  lead model $model that bai ($why) cho #$($cur.no) -> thu model du phong"
      continue
    }
    $a = @('-p', $short, '--model', $model, '--add-dir', $Run, '--add-dir', "$Root\docs", '--add-dir', $Source, '--add-dir', $wt, '--print-timeout', '8m')   # 8 phut/model (Sonnet ~1p, Flash ~3p, Argon 3-7p); 15m x 3 model = tre toi 45 phut
    $eap = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
    try { $o = & $Agy @a 2>&1 | ForEach-Object { "$_" } | Out-String } finally { $ErrorActionPreference = $eap }
    [IO.File]::WriteAllText("$SubDir\lead_draft_$($cur.no)_$model.txt", $o, $Utf8)
    if (Test-Path "$dir\NO_BACKEND.flag") { Log "  lead phat hien KHONG co backend cho #$($cur.no) -> bo qua viec soan de bai"; $script:State.leadFails = 0; Save-State $script:State; return $true }
    $why = ''
    if ($o -match '(?i)RESOURCE_EXHAUSTED|quota (exceeded|exhausted)|out of (ai )?credits|rate limit exceeded') { $why = 'QUOTA' }
    elseif ($o -match 'headless mode cannot prompt') { $why = 'PERMISSION' }
    elseif ($o.Trim().Length -lt 20) { $why = 'EMPTY' }
    elseif (-not ((Test-Path $out) -and ((Get-Item $out).Length -gt 800) -and ((Get-Content $out -Raw -Encoding UTF8) -match 'P2'))) { $why = 'KHONG_RA_DEBAI' }
    if (-not $why) {
      [IO.File]::AppendAllText($out, "`r`n<!-- DEBAI do LEAD ($model) soan $(Get-Date -Format s); Claude chua review truoc khi chay -->`r`n", $Utf8)
      $script:State.leadFails = 0; Save-State $script:State
      return $true
    }
    if ($why -eq 'QUOTA') { $script:State.modelSkip[$model] = (Get-Date).AddMinutes(30).ToString('o'); Save-State $script:State }
    Log "  lead model $model that bai ($why) cho #$($cur.no) -> thu model du phong$(if ($why -eq 'QUOTA') { ' (bo qua model nay 30 phut)' })"
  }
  $false
}

# ---------------- nap viec / thu lai / drop ----------------
function Test-Paused {
  if (-not (Test-Path $PauseF)) { return $false }
  try { $u = [datetime](Get-Content $PauseF -Raw -Encoding UTF8).Trim(); if ($u -gt (Get-Date)) { return $true } } catch { }
  Remove-Item $PauseF -Force -ErrorAction SilentlyContinue; $false
}
function Check-QuotaBreaker {
  # >=2 linh dang trong cooldown vi QUOTA (het han muc that) -> tam dung nap viec 60 phut va bao Claude
  $n = 0
  foreach ($w in $Workers) { $cd = ReadJson "$Run\cooldown_$w.json"; if ($cd -and $cd.why -eq 'QUOTA' -and [datetime]$cd.until -gt (Get-Date)) { $n++ } }
  if ($n -ge 2 -and -not (Test-Path $PauseF)) {
    [IO.File]::WriteAllText($PauseF, (Get-Date).AddMinutes(60).ToString('o'), $Utf8)
    Log "NGAT MACH: $n linh het han muc -> tam dung nap viec 60 phut"
    Anomaly "NGAT MACH: $n linh het han muc (QUOTA); tam dung nap viec 60 phut (file PAUSE_FEED)"
  }
}
function Start-Detached([string[]]$args2, [string]$logf) {
  Start-Process -FilePath 'powershell.exe' -ArgumentList $args2 -WindowStyle Hidden -RedirectStandardOutput $logf -RedirectStandardError "$logf.err" | Out-Null
}
function Pending-Info {
  $n = 0; $oldest = 0
  foreach ($x in $Workers) { $c = Cur $x; $r = Res $x
    # chi tinh hang muc co THAY DOI THAT (diff rong = agent treo/khong ghi gi, dang chay lai -> khong goi Claude day de duyet)
    if ($c -and $r -and $r.status -eq 'CHO_CLAUDE' -and $r.item -eq $c.item -and @($r.changed).Count -gt 0 -and [int]$r.round -eq [int]$c.round) { $n++; $age = ((Get-Date) - [datetime]$r.at).TotalMinutes; if ($age -gt $oldest) { $oldest = $age } } }
  [pscustomobject]@{ N = $n; Oldest = $oldest }
}
function Feed-Workers {
  Check-QuotaBreaker
  $paused = Test-Paused   # PAUSE_FEED chi do het quota agy/codegpt -> gweb (Gemini web, tai khoan rieng) van duoc nap
  if ($paused -and ($Workers -notlike 'gweb*').Count -eq $Workers.Count) { return }
  foreach ($w in $Workers) {
    if ($FeedOnly.Count -and $FeedOnly -notcontains $w) { continue }
    if ($paused -and $w -notlike 'gweb*') { continue }
    # Danh thuc Claude KHONG duoc tre vi dang soan de bai: neu da du lo / qua han thi dung nap viec, de vong ngoai phat digest ngay.
    $pi = Pending-Info
    if (-not $env:FEEDER -and ($pi.N -ge $MinBatch -or ($pi.N -ge 1 -and $pi.Oldest -ge $MaxWaitMin))) { Log "  dung nap viec som: $($pi.N) hang muc cho review (cu nhat $([int]$pi.Oldest) phut)"; return }
    $cur = Cur $w; $r = Res $w
    # Phuc hoi khi lead cu chet giua chung: da prep (CAN_DEBAI) nhung chua co DEBAI sau >8 phut -> soan lai roi chay
    if ($cur -and $cur.stage -eq 'prep' -and -not $cur.partial -and $r -and $r.status -eq 'CAN_DEBAI' -and $r.item -eq $cur.item -and -not (Test-Path "$($cur.dir)\DEBAI.md") -and -not (Test-Path "$($cur.dir)\NO_BACKEND.flag") -and ((Get-Date) - (Get-Item "$Run\current_$w.json").LastWriteTime).TotalMinutes -gt 8) {
      Log "phuc hoi #$($cur.no) $($cur.item) tren slot ${w}: da prep nhung chua co de bai (lead truoc chet?) -> soan lai"
      if (Draft-Debai $w $cur) {
        if (Test-Path "$($cur.dir)\NO_BACKEND.flag") { $null = & powershell -NoProfile -ExecutionPolicy Bypass -File $Step -Action drop -Worker $w -Reason "BO TU DONG (lead): thieu nguon backend - xem conflicts"; continue }
        Start-Detached @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $Step, '-Action', 'run', '-Worker', $w, '-Engine', $(if ($false) { 'agy' } else { $(if ($w -like 'agy*') { 'agy' } elseif ($w -like 'gweb*') { 'gweb' } else { $w }) })) "$SubDir\run_${w}_$($cur.no).log"
      } else { Anomaly "PHUC HOI that bai: DEBAI #$($cur.no) cho $w" }
      continue
    }
    if ($cur) {
      # (a) hang muc dang do vi loi (stage=prep, partial) -> tu thu lai, toi da MaxAttempts roi DROP
      if ($cur.stage -eq 'prep' -and $cur.partial -and $r -and $r.status -eq 'AGENT_DOWN') {
        $cd = ReadJson "$Run\cooldown_$w.json"
        if ($cd -and [datetime]$cd.until -gt (Get-Date)) { continue }
        $key = "$w|$($cur.item)"
        $n = 1 + [int]$script:State.attempts[$key]; $script:State.attempts[$key] = $n; Save-State $script:State
        if ($n -gt $MaxAttempts) {
          Log "DROP #$($cur.no) $($cur.item): qua $MaxAttempts lan that bai"
          $null = & powershell -NoProfile -ExecutionPolicy Bypass -File $Step -Action drop -Worker $w -Reason "TU DONG DROP: qua $MaxAttempts lan that bai/het gio ($($r.why)); can Claude xem tay" 2>&1
          Anomaly "DROP tu dong #$($cur.no) $($cur.item) (slot $w) sau $MaxAttempts lan that bai ($($r.why))"
        } else {
          $eng = $w   # codegpt chay engine codegpt (nguoi dung xac nhan chay on 16/10); agy* -> agy
          Log "thu lai #$($cur.no) $($cur.item) tren slot $w bang engine $eng (lan $n/$MaxAttempts)"
          Start-Detached @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $Step, '-Action', 'run', '-Worker', $w, '-Engine', $(if ($eng -like 'agy*') { 'agy' } elseif ($eng -like 'gweb*') { 'gweb' } else { $eng })) "$SubDir\retry_${w}_$($cur.no).log"
        }
        continue
      }
      # (b) phat hien treo: dang chay qua StuckMin phut ma khong co ket qua moi
      if ($cur.stage -eq 'run') {
        $age = ((Get-Date) - (Get-Item "$Run\current_$w.json").LastWriteTime).TotalMinutes
        if ($age -gt $StuckMin -and -not ($r -and $r.status -eq 'CHO_CLAUDE')) { Anomaly "NGHI TREO: slot $w #$($cur.no) $($cur.item) da chay $([int]$age) phut khong co ket qua" }
      }
      continue
    }
    $cd = ReadJson "$Run\cooldown_$w.json"
    if ($cd -and [datetime]$cd.until -gt (Get-Date)) { continue }
    Log "nap viec cho $w"
    $null = & powershell -NoProfile -ExecutionPolicy Bypass -File $Step -Action prep -Worker $w 2>&1
    $r = Res $w
    if (-not $r -or $r.status -ne 'CAN_DEBAI') { Log "  $w : $(if ($r) { $r.status } else { 'khong ro' })"; if ($r -and $r.status -eq 'HET_VIEC' -and $w -notlike 'gweb*') { Anomaly "$w : HET_VIEC" }; continue }
    $cur = Cur $w
    $dbOk = @(Draft-Debai $w $cur) | Select-Object -Last 1   # lay gia tri CUOI (Save-State/Log trong ham co the lam ham tra mang -> luon truthy)
    if ($dbOk -ne $true -or -not (Test-Path "$($cur.dir)\DEBAI.md") -and -not (Test-Path "$($cur.dir)\NO_BACKEND.flag")) {
      $script:State.leadFails = 1 + [int]$script:State.leadFails; Save-State $script:State
      Log "  soan DEBAI #$($cur.no) THAT BAI o moi model (lead that bai lien tiep: $($script:State.leadFails))"
      Anomaly "DEBAI #$($cur.no) $($cur.item) cho $w : moi model lead deu that bai (da prep, chua chay)"
      if ($script:State.leadFails -ge 3) { Anomaly "NGAT MACH: lead that bai $($script:State.leadFails) hang muc lien tiep -> dung nap viec (PAUSE_FEED 60 phut)"; [IO.File]::WriteAllText($PauseF, (Get-Date).AddMinutes(60).ToString('o'), $Utf8) }
      continue
    }
    if (Test-Path "$($cur.dir)\NO_BACKEND.flag") {
      # Thieu nguon backend: tu dong BO (cung quyet dinh cua Claude voi #159-#161). Khong ton luot linh. Xung dot da ghi o $ConfDir.
      Log "  BO TU DONG #$($cur.no) $($cur.item): thieu nguon backend (xem conflicts\$($cur.no)_conflict.md)"
      $null = & powershell -NoProfile -ExecutionPolicy Bypass -File $Step -Action drop -Worker $w -Reason "BO TU DONG (lead): thieu nguon backend - xem conflicts\$($cur.no)_conflict.md; Claude co the xem lai"
      continue
    }
    Log "  DEBAI #$($cur.no) $($cur.item) xong -> chay $w"
    Start-Detached @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $Step, '-Action', 'run', '-Worker', $w, '-Engine', $(if ($false) { 'agy' } else { $(if ($w -like 'agy*') { 'agy' } elseif ($w -like 'gweb*') { 'gweb' } else { $w }) })) "$SubDir\run_${w}_$($cur.no).log"
  }
}

# ---------------- agent phu theo hang doi (tach roi - khong danh thuc Claude rieng le) ----------------
# ---------------- LICH 24/7 cho agent phu: tu dua QA / tai lieu vao hang doi khi co controller MOI xong ----------------
function Get-DoneByController {
  $h = @{}
  foreach ($line in [IO.File]::ReadAllLines("$Repo\PARITY_PROGRESS.md", $Utf8)) {
    $m = [regex]::Match($line, '^\|\s*(\d+)\s*\|\s*([^|]+?)\s*\|\s*\u0110')
    if ($m.Success) { $c = ($m.Groups[2].Value -split '/')[0]; if (-not $h.ContainsKey($c)) { $h[$c] = New-Object Collections.ArrayList }; $null = $h[$c].Add([int]$m.Groups[1].Value) }
  }
  $h
}
function Auto-Enqueue-Aux {
  $qaF = "$Run\qa_state.json"; $qs = @{}; $j = ReadJson $qaF; if ($j) { foreach ($p in $j.PSObject.Properties) { $qs[$p.Name] = [int]$p.Value } }
  $q = @(ReadJson $QueueF | ForEach-Object { $_ } | Where-Object { $_ -and $_.role })
  $done = Get-DoneByController
  foreach ($role in $(if (Test-Path (Join-Path $Run "AUTO_QA")) { @('qa1', 'qa2') } else { @() })) {   # QA dung agy (han muc Gemini/Claude it): chi tu dong khi co file AUTO_QA
    if ($q | Where-Object { $_.role -eq $role }) { continue }
    $aj = ReadJson "$SubDir\aux_$role.json"; if ($aj -and $aj.status -eq 'DANG_CHAY') { continue }
    # chon controller co nhieu hang muc MOI (so voi lan QA truoc) nhat, chua nam trong hang doi
    $pick = $null; $best = 0
    foreach ($k in $done.Keys) { $new = $done[$k].Count - [int]$qs[$k]; $queued = $q | Where-Object { $_.focus -like "Controller $k *" }; if ($new -gt $best -and -not $queued) { $best = $new; $pick = $k } }
    if (-not $pick) { continue }
    $nums = ($done[$pick] | ForEach-Object { "#$_" }) -join ', '
    $focus = "Controller $pick (hang muc da port: $nums). CHAY THAT, khong chi build: (1) bat app tren DB tam, goi MOI action bang HTTP voi du lieu that (seed + tu tao), doi chieu tung buoc voi nguon (file:dong); (2) di qua LUONG NGHIEP VU nhu nguoi dung/UI goi that: Create -> Search/Get thay ban ghi vua tao -> Update -> Delete, Import/Export bang file xlsx that (mo file kiem tra cot/gia tri); (3) neu controller co man hinh trong wwwroot/*.html hoac clientapp thi mo bang Playwright (playwright-core o scripts\parity\gemini-web, Chrome that) thao tac nhu nguoi dung: nhap form, bam nut, kiem tra bang/thong bao loi hien dung; chup anh vao bao cao. Moi sai khac so voi nguon = 1 loi co tai hien."
    $q += [pscustomobject]@{ id = "auto_${role}_${pick}_$(Get-Date -Format HHmm)"; role = $role; focus = $focus }
    $qs[$pick] = $done[$pick].Count
    Log "LICH: dua QA $role vao hang doi: $pick ($nums)"
  }
  [IO.File]::WriteAllText($qaF, ($qs | ConvertTo-Json), $Utf8)
  $plain = @($q | ForEach-Object { [ordered]@{ id = [string]$_.id; role = [string]$_.role; focus = [string]$_.focus } })
  [IO.File]::WriteAllText($QueueF, $(if ($plain.Count) { ConvertTo-Json -InputObject $plain -Depth 4 } else { '[]' }), $Utf8)
  # tai lieu bang GEMINI WEB: 1 controller moi lan, chay nen, chi khi chua co file docs\api\<Controller>.md
  $dm = "$Run\sub\doc_gw_running.json"; $busy = $false
  if (Test-Path $dm) { $dj = ReadJson $dm; if ($dj -and ((Get-Date) - [datetime]$dj.at).TotalMinutes -lt 25) { $busy = $true } }
  $dl = Join-Path $Run "sub/doc_gw_last.txt"; if ((Test-Path $dl) -and ((Get-Date) - (Get-Item $dl).LastWriteTime).TotalMinutes -lt 12) { $busy = $true }   # tai lieu toi da 1 luot/12 phut: nhuong lan gweb cho viec soan de bai/code
  if (-not $busy) {
    $docDir = "$Root\repos\$($PJ.Name)-docs\docs\api"
    $c = $done.Keys | Where-Object { -not (Test-Path "$docDir\$_.md") -and (Test-Path "$Repo\Controllers\Port\$($_ -replace 'Controller$','')ControllerPort.cs") } | Sort-Object | Select-Object -First 1
    if ($c) {
      [IO.File]::WriteAllText($dl, "x", $Utf8); [IO.File]::WriteAllText($dm, (@{ ctrl = $c; at = (Get-Date).ToString('s') } | ConvertTo-Json), $Utf8)
      Log "LICH: viet tai lieu (gweb) $c"
      Start-Detached @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-Command', "& '$Root\scripts\parity\doc-gw.ps1' -Controller '$c'; Remove-Item '$dm' -Force -EA SilentlyContinue") "$SubDir\doc_gw_$c.log"
    }
  }
}
function Feed-Aux {
  try { Auto-Enqueue-Aux } catch { Log "Auto-Enqueue-Aux loi: $_" }
  if (-not (Test-Path $QueueF)) { return }
  # PS 5.1: ConvertFrom-Json tra MANG nhu 1 doi tuong duy nhat -> phai bung qua pipeline (neu khong ca mang bi coi la 1 phan tu).
  $parsed = ReadJson $QueueF
  $q = @($parsed | ForEach-Object { $_ } | Where-Object { $_ -and $_.role })
  if ($q.Count -eq 0) { return }
  foreach ($role in $AuxRoles) {
    $j = ReadJson "$SubDir\aux_$role.json"
    if ($j -and $j.status -eq 'DANG_CHAY') { $age = ((Get-Date) - (Get-Item "$SubDir\aux_$role.json").LastWriteTime).TotalMinutes; if ($age -lt 60) { continue } else { Anomaly "NGHI TREO: agent phu $role chay $([int]$age) phut" } }
    $item = $q | Where-Object { $_.role -eq $role } | Select-Object -First 1
    if (-not $item) { continue }
    $ff = "$SubDir\focus_$($item.id).txt"; [IO.File]::WriteAllText($ff, [string]$item.focus, $Utf8)
    Log "bat agent phu $role theo hang doi ($($item.id))"
    Start-Detached @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $AuxStep, '-Action', 'run', '-Role', $role, '-FocusFile', $ff) "$SubDir\aux_run_${role}_$($item.id).log"
    $rest = @($q | Where-Object { $_.id -ne $item.id })
    $plain = @($rest | ForEach-Object { [ordered]@{ id = [string]$_.id; role = [string]$_.role; focus = [string]$_.focus } })
    [IO.File]::WriteAllText($QueueF, $(if ($plain.Count) { ConvertTo-Json -InputObject $plain -Depth 4 } else { '[]' }), $Utf8)
    $q = $rest
  }
}

# ---------------- digest ----------------
function Section([string]$text, [string]$from, [string]$to) {
  $i = $text.LastIndexOf($from); if ($i -lt 0) { return '' }
  $j = $text.IndexOf($to, $i + $from.Length); if ($j -lt 0) { $j = [Math]::Min($text.Length, $i + 1500) }
  $text.Substring($i, [Math]::Min($j - $i, 1500)).Trim()
}
function Last-Digest { Get-ChildItem $DigDir -Filter 'digest_*.md' -ErrorAction SilentlyContinue | Sort-Object LastWriteTime | Select-Object -Last 1 }
function New-Files([string]$dir, [string]$filter) {
  $last = Last-Digest; $since = if ($last) { $last.LastWriteTime } else { [datetime]'2000-01-01' }
  @(Get-ChildItem $dir -Filter $filter -Recurse -File -ErrorAction SilentlyContinue | Where-Object { $_.LastWriteTime -gt $since })
}
function Build-Digest {
  $sb = New-Object Text.StringBuilder
  $pending = @(); $running = @(); $idle = @()
  foreach ($w in $Workers) {
    $c = Cur $w; $r = Res $w
    if (-not $c) { $idle += $w; continue }
    if ($r -and $r.status -eq 'CHO_CLAUDE' -and $r.item -eq $c.item -and @($r.changed).Count -gt 0 -and [int]$r.round -eq [int]$c.round) { $pending += $w } else { $running += $w }
  }
  [void]$sb.AppendLine("# DIGEST $(Get-Date -Format 'yyyy-MM-dd HH:mm') - cho review: $($pending.Count) | dang chay: $($running -join ', ') | ranh: $($idle -join ', ')")
  foreach ($w in $pending) {
    $r = Res $w; $c = Cur $w; $flags = @()
    $files = @($r.changed)
    $bad = @($files | Where-Object { $_ -notmatch $Allowed })
    if ($bad.Count) { $flags += "FILE NGOAI PHAM VI: $($bad -join ', ')" }
    if (-not $r.build) { $flags += 'BUILD/TEST KHONG XANH' }
    $wf = Get-ChildItem $r.dir -Filter ("write_r{0}_*.txt" -f $r.round) -ErrorAction SilentlyContinue | Where-Object { $_.Name -notmatch 'prompt|try|messages' } | Select-Object -First 1
    $txt = if ($wf) { Get-Content $wf.FullName -Raw -Encoding UTF8 } else { '' }
    if ($txt -notmatch 'TOM TAT') { $flags += 'THIEU MUC TOM TAT' }
    if ($txt -match 'TRANG_THAI:\s*(CAN_XEM_TAY|BO|DA_CO)') { $flags += "TRANG_THAI = $($Matches[1])" }
    # Chi coi la xung dot that khi noi dung KHONG bat dau bang 'Khong/None/N/A' (tranh co gia khi agent ghi 'CONFLICT: Khong ...')
    foreach ($cm in [regex]::Matches($txt, '(?m)^\s*(?:-\s*)?CONFLICT:\s*([^\r\n]+)')) {
      $cv = $cm.Groups[1].Value.Trim()
      if ($cv -notmatch '^(?i)(khong|không|none|n/a|no\b|-)') { $flags += "XUNG DOT: $cv" }
    }
    $diffF = Join-Path $r.dir ("diff_r{0}.patch" -f $r.round)
    $dtxt = if (Test-Path $diffF) { Get-Content $diffF -Raw -Encoding UTF8 } else { '' }
    if ($dtxt -match '(?i)(password|pwd|passwd|secret|bearer\s+[a-z0-9]|GwPassword\s*=\s*"[^"]+")\s*[=:]\s*["''][^"'']{3,}') { $flags += 'NGHI CHEP THONG TIN DANG NHAP (kiem diff)' }
    if ($dtxt -match '(?m)^\+.*(CreateFunction\(|\bthrow;)') { $flags += 'DIFF CHUA CreateFunction hoac throw; (xem bai hoc A.1/A.2)' }
    $deb = if (Test-Path "$($r.dir)\DEBAI.md") { Get-Content "$($r.dir)\DEBAI.md" -Raw -Encoding UTF8 } else { '' }
    $lead = if ($deb -match 'DEBAI do LEAD \(([^)]+)\)') { "LEAD soan ($($Matches[1]); chua Claude review)" } else { 'Claude soan' }
    [void]$sb.AppendLine("`n## #$($r.no) $($r.item) - slot $w - $($c.model) - $($r.minutes) phut - $lead")
    [void]$sb.AppendLine("- File: $($files -join ', ') | build/test: $($r.build) | diff: $diffF")
    [void]$sb.AppendLine("- Co: $(if ($flags) { ($flags -join ' ; ') } else { 'khong co co bat thuong' })")
    $p1 = Section $txt 'TRA LOI P1' 'TRANG_THAI'; if ($p1) { [void]$sb.AppendLine("- P1 (linh tra loi): $($p1 -replace '\s+',' ')") }
    $ans = [regex]::Match($deb, '\(Dap an de reviewer[^\r\n]*'); if ($ans.Success) { [void]$sb.AppendLine("- P1 (dap an LEAD ghi trong de bai): $($ans.Value -replace '\s+',' ')") }
    $ver = Section $txt 'VERIFY' 'BUILD='; if ($ver) { [void]$sb.AppendLine("- VERIFY: $($ver -replace '\s+',' ')") }
    $gap = Section $txt 'GAP:' "`n`n"; if ($gap) { [void]$sb.AppendLine("- $($gap -replace '\s+',' ')") }
  }
  foreach ($w in $running) { $c = Cur $w; [void]$sb.AppendLine("`n(dang chay) $w : #$($c.no) $($c.item)  stage=$($c.stage)") }
  foreach ($k in $AuxRoles) {
    $j = ReadJson "$SubDir\aux_$k.json"
    if ($j -and $j.status -ne 'DANG_CHAY') { [void]$sb.AppendLine("`n(phu $k) status=$($j.status) $($j.minutes) phut vi_pham=$($j.vi_pham -join ';') bao_cao=$($j.report)") }
  }
  $cf = @(New-Files $ConfDir '*.md' | Where-Object { (Get-Content $_.FullName -Raw -Encoding UTF8) -notmatch 'QUY.T .+ C.A CLAUDE' });   # bo qua xung dot Claude da quyet (co muc "QUYET DINH CUA CLAUDE") if ($cf.Count) { [void]$sb.AppendLine("`n## XUNG DOT CHO CLAUDE CHOT ($($cf.Count))"); foreach ($f in $cf) { [void]$sb.AppendLine("- $($f.FullName): $((Get-Content $f.FullName -TotalCount 6 -Encoding UTF8) -join ' | ')") } }
  $pf = @(New-Files $PropDir '*.md'); if ($pf.Count) { [void]$sb.AppendLine("`n## DE XUAT TOI UU (agent phu) CHO CLAUDE DUYET ($($pf.Count))"); foreach ($f in $pf) { [void]$sb.AppendLine("- $($f.FullName)") } }
  if ($script:Anomalies.Count) { [void]$sb.AppendLine("`n## BAT THUONG"); foreach ($a in $script:Anomalies) { [void]$sb.AppendLine("- $a") } }
  [pscustomobject]@{ Text = $sb.ToString(); Pending = $pending.Count; Running = $running.Count; Idle = $idle.Count; Conflicts = $cf.Count; Proposals = $pf.Count }
}
function Oldest-PendingMin {
  $min = 0
  foreach ($w in $Workers) { $r = Res $w; $c = Cur $w
    if ($c -and $r -and $r.status -eq 'CHO_CLAUDE') { $age = ((Get-Date) - [datetime]$r.at).TotalMinutes; if ($age -gt $min) { $min = $age } } }
  $min
}
function Aux-DoneCount {
  $last = Last-Digest; $since = if ($last) { $last.LastWriteTime } else { [datetime]'2000-01-01' }
  $n = 0
  foreach ($k in $AuxRoles) { $j = ReadJson "$SubDir\aux_$k.json"; if ($j -and $j.status -ne 'DANG_CHAY' -and $j.at -and [datetime]$j.at -gt $since) { $n++ } }
  $n
}
function Emit([string]$why) {
  $d = Build-Digest
  $f = "$DigDir\digest_{0:yyyyMMdd_HHmmss}.md" -f (Get-Date)
  [IO.File]::WriteAllText($f, "Ly do danh thuc Claude: $why`r`n`r`n" + $d.Text, $Utf8)
  Log "DANH THUC CLAUDE: $why -> $f"
  Write-Output ("Ly do danh thuc Claude: $why`n`n" + $d.Text)
}

if ($Action -eq 'digest') { Emit 'yeu cau xem digest'; exit 0 }
if ($Action -eq 'feed')   { Feed-Workers; if (-not $env:FEEDER) { Feed-Aux }; exit 0 }

# ---------------- loop ----------------
$t0 = Get-Date
Log "loop bat dau | MinBatch=$MinBatch MaxWait=$MaxWaitMin phut MaxAttempts=$MaxAttempts | workers: $($Workers -join ', ') | lead: $($LeadModels -join ' > ')"
while ($true) {
  if (Test-Path $StopF) { Emit 'co file STOP_LEAD'; exit 0 }
  if (-not $NoFeed) {
    try { Feed-Aux } catch { Anomaly "Feed aux loi: $_" }          # nhanh -> lam truoc, tranh doi hang doi agent phu trong luc soan de bai
    try { Feed-Workers } catch { Anomaly "Feed workers loi: $_" }
  }
  $d = Build-Digest
  $age = Oldest-PendingMin
  $last = Last-Digest
  $sinceDigest = if ($last) { ((Get-Date) - $last.LastWriteTime).TotalMinutes } else { 9999 }
  $down = @($Workers | Where-Object { $r = Res $_; $r -and $r.status -eq 'COOLDOWN' })
  if ($d.Pending -ge $MinBatch) { Emit "du lo: $($d.Pending) hang muc cho review"; exit 0 }
  if ($d.Pending -ge 1 -and $age -ge $MaxWaitMin) { Emit "hang muc cho review da $([int]$age) phut (>= $MaxWaitMin)"; exit 0 }
  if ($script:Anomalies.Count) { Emit 'co bat thuong'; exit 0 }
  if ($d.Conflicts -gt 0) { Emit "co $($d.Conflicts) xung dot can Claude chot"; exit 0 }
  $ad = Aux-DoneCount
  if ($ad -ge 2 -or ($ad -ge 1 -and $sinceDigest -ge $AuxWakeMin)) { Emit "agent phu co $ad ket qua moi"; exit 0 }
  if ($d.Proposals -gt 0 -and $sinceDigest -ge $AuxWakeMin) { Emit "co $($d.Proposals) de xuat toi uu cho Claude duyet"; exit 0 }
  if ($d.Running -eq 0 -and $d.Pending -eq 0 -and $d.Idle -eq $Workers.Count -and -not (Test-Paused)) { Emit 'tat ca linh ranh va khong nap them duoc viec (het viec?)'; exit 0 }
  if (((Get-Date) - $t0).TotalMinutes -ge $MaxLoopMin) { Emit "het $MaxLoopMin phut cho"; exit 0 }
  Start-Sleep -Seconds $PollSec
}
