# Trich NGUYEN VAN chuoi nguon cua 1 hang muc (gate -> API -> WAS_ -> ham biz -> ban _New<ngay> dang duoc GOI)
# ra 1 file co so dong, de Claude (bo nao) doc va viet DEBAI cho agent - agent khong phai tu do file biz 30.000 dong.
# Lan theo LOI GOI THAT (khong theo ten) nen ban LIVE la ban duoc goi; bo qua *.sync-conflict-*, Delete.*, *.bak.
param(
  [Parameter(Mandatory)][string]$Item,      # vd Crd_MemberController/Save
  [Parameter(Mandatory)][string]$OutFile,
  [int]$MaxLinesPerFunc = 900,
  [int]$Depth = 3
)
$ErrorActionPreference = 'Stop'
$Utf8 = [Text.UTF8Encoding]::new($false)
. "$PSScriptRoot\profile.ps1"
$Source = $PJ.Source
$Gate   = $PJ.Gate
$ApiDir = $PJ.ApiDir
$BizDir = $PJ.BizDir
$TwoTier = ($PJ.Mode -eq '2tier')
$sb = [Text.StringBuilder]::new()
function W([string]$s) { [void]$sb.AppendLine($s) }

$cache = @{}
function Lines([string]$f) { if (-not $cache[$f]) { $cache[$f] = [IO.File]::ReadAllLines($f, [Text.Encoding]::UTF8) }; $cache[$f] }
# Lay tron than ham bat dau tu dong khai bao (1-based) bang dem ngoac nhon (bo qua ngoac trong chuoi/chu thich dong)
function Body([string]$f, [int]$start) {
  $L = Lines $f; $depth = 0; $opened = $false; $end = $start
  for ($i = $start - 1; $i -lt $L.Count; $i++) {
    $t = $L[$i] -replace '//.*$', '' -replace '"([^"\\]|\\.)*"', '""' -replace "'([^'\\]|\\.)*'", "''"
    foreach ($ch in $t.ToCharArray()) { if ($ch -eq '{') { $depth++; $opened = $true } elseif ($ch -eq '}') { $depth-- } }
    $end = $i + 1
    if ($opened -and $depth -le 0) { break }
  }
  [pscustomobject]@{ Start = $start; End = $end }
}
function Dump([string]$title, [string]$f, [int]$start) {
  $b = Body $f $start; $L = Lines $f
  $n = $b.End - $b.Start + 1
  W ''; W "======== $title ========"; W "FILE: ${f}:$($b.Start)-$($b.End) ($n dong)"
  $stop = [Math]::Min($b.End, $b.Start + $MaxLinesPerFunc - 1)
  for ($i = $b.Start; $i -le $stop; $i++) { W ("{0,6}| {1}" -f $i, $L[$i - 1]) }
  if ($stop -lt $b.End) { W "   ... CAT: con $($b.End - $stop) dong, doc tiep ${f}:$($stop + 1)-$($b.End)" }
  $b
}

$bizFiles = @(Get-ChildItem $BizDir -Recurse -Filter *.cs | Where-Object { $_.Name -notmatch 'sync-conflict|^Delete\.|\.bak|AssemblyInfo' -and $_.FullName -notmatch '\\(bin|obj)\\' })
function Find-Def([string]$name) {
  $hits = @($bizFiles | Select-String -Pattern "^\s*(public|private|protected|internal)\s+[\w<>\[\],\s]+\s$name\s*\(" )
  $hits
}

$ctrl, $act = $Item -split '/'
W "# NGUON NGUYEN VAN cho hang muc $Item  (sinh luc $(Get-Date -Format 'yyyy-MM-dd HH:mm'))"
W "# Duoc trich bang cach LAN THEO LOI GOI: gate -> service WA_ -> API -> _biz.WAS_ -> ham biz -> ban _New dang duoc goi."

# 1) GATE
$gf = "$Gate\$ctrl.cs"
$g = Select-String -LiteralPath $gf -Pattern "^\s*public\s+(?:async\s+|static\s+|virtual\s+|override\s+)*[\w<>\[\],\.\s]+?\s+$act\s*\(" | Select-Object -First 1
if (-not $g) { W "KHONG THAY action $act trong $gf"; [IO.File]::WriteAllText($OutFile, $sb.ToString(), $Utf8); exit 1 }
$gb = Dump "1. GATE $ctrl.$act" $gf $g.LineNumber
$gtext = (Lines $gf)[($gb.Start - 1)..($gb.End - 1)] -join "`n"
$was = @([regex]::Matches($gtext, '\bWA_\w+') | ForEach-Object Value | Select-Object -Unique)
W ''; W "GATE goi service: $($was -join ', ')"

# 1b) Ham phu trong GATE ma action goi: loc/chuan hoa (strWhereClause_*, *_Return, *_Refine*) VA moi ham private/public cung file cong (vd RT_Contract_Contract_Get)
#     co dinh nghia trong file cong -> trich ca than ham (toi da 2 tang, 8 ham) de thay duoc lenh WA_* ben trong. Ten WA_* tim thay them vao $was.
$helperSeen = @{}; $helperQueue = [System.Collections.Generic.List[string]]::new(); $scanText = $gtext
$skipNames = @('View', 'Json', 'Redirect', 'RedirectToAction', 'Content', 'File', 'PartialView', 'Url', 'Request', 'Session', 'Convert', 'String', 'List', 'Dictionary', 'ToString', 'Add', 'Count', 'Any', 'Where', 'Select', 'Parse', 'Format', 'Trim', 'Contains', 'Replace', 'Substring', 'Split', 'Join', 'IsNullOrEmpty', 'Equals', 'Insert', 'Remove', 'Clear', 'Sort', 'ToList', 'ToArray', 'First', 'Last', 'GetType', 'GetNextTId')
for ($lvl2 = 0; $lvl2 -lt 2; $lvl2++) {
  $cands = @([regex]::Matches($scanText, '\b([A-Za-z_]\w{4,})\s*\(') | ForEach-Object { $_.Groups[1].Value } | Select-Object -Unique | Where-Object { -not $helperSeen[$_] -and $skipNames -notcontains $_ -and $_ -ne $act })
  $newText = ''
  foreach ($h in $cands) {
    if ($helperSeen.Count -ge 8) { break }
    $hf = $gf; $d = Select-String -LiteralPath $hf -Pattern "^\s*(private|public|protected|internal)\s+(?:async\s+|static\s+|virtual\s+)*[\w<>\.\[\],\s]+?\s+$h\s*\(" | Select-Object -First 1
    if (-not $d) { foreach ($bf in @(Get-ChildItem $Gate -Filter *.cs -EA SilentlyContinue | Where-Object { $_.Name -match 'Base|Helper|Common' -and $_.Name -notmatch 'sync-conflict|\.bak' })) { $d = Select-String -LiteralPath $bf.FullName -Pattern "^\s*(private|public|protected|internal)\s+(?:async\s+|static\s+|virtual\s+)*[\w<>\.\[\],\s]+?\s+$h\s*\(" | Select-Object -First 1; if ($d) { $hf = $bf.FullName; break } } }
    if (-not $d) { continue }
    $helperSeen[$h] = 1
    $hb = Dump "1b. GATE ham phu $h" $hf $d.LineNumber
    $newText += "`n" + ((Lines $hf)[($hb.Start - 1)..($hb.End - 1)] -join "`n")
  }
  if (-not $newText) { break }
  $scanText = $newText; $gtext += $newText
}
$was = @([regex]::Matches($gtext, '\bWA_\w+') | ForEach-Object Value | Select-Object -Unique)
W ''; W "GATE (gom ca ham phu) goi service: $($was -join ', ')"
# 1c) Model (tu dien cot) + DDL cua cac bang TModels.<X> ma gate dung
$CommonDir = "$Source\12.Dev.Common"
$modelsSeen = @{}
$tbRx = 'TModel\w*\.(?:RQ_|RT_)?((?:Crd|Mst|Prm|Map|Rpt|Sys|Inv|Req)\w+?)\b|\b(?:RQ|RT)_((?:Crd|Mst|Prm|Map|Rpt|Sys|Inv|Req)\w+?)\b|List<((?:Crd|Mst|Prm|Map|Rpt|Sys|Inv|Req)\w+?)>'
if ($TwoTier) { $tbRx = '(?!)' }   # 2 tang (MobileGate): khong co model TModels/DDL kieu Loyalty
foreach ($tb in @([regex]::Matches($gtext, $tbRx) | ForEach-Object { @($_.Groups[1].Value, $_.Groups[2].Value, $_.Groups[3].Value | Where-Object { $_ })[0] } | Where-Object { $_ -notmatch 'Search$|Service$' } | Select-Object -Unique)) {
  if ($modelsSeen[$tb]) { continue }; $modelsSeen[$tb] = 1
  $mf = Get-ChildItem $CommonDir -Recurse -Filter "$tb.cs" -EA SilentlyContinue | Where-Object { $_.FullName -match '\\Models\\' -and $_.FullName -notmatch '\\(bin|obj)\\|sync-conflict' } | Select-Object -First 1
  if ($mf) {
    $L = Lines $mf.FullName
    W ''; W "======== 1c. MODEL $tb (tu dien cot) ========"; W "FILE: $($mf.FullName) ($($L.Count) dong)"
    for ($i = 1; $i -le [Math]::Min($L.Count, 260); $i++) { W ("{0,6}| {1}" -f $i, $L[$i - 1]) }
  } else { W ''; W "MODEL: khong thay $tb.cs trong $CommonDir" }
  $sqlHits = @(Get-ChildItem $Source -Recurse -Filter *.sql -EA SilentlyContinue | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } |
    Select-String -Pattern "create\s+table\s+(\[?dbo\]?\.)?\[?$tb\]?\s*\(" | Select-Object -First 2)
  foreach ($s in $sqlHits) {
    $L = Lines $s.Path
    W ''; W "======== 1d. DDL $tb ========"; W "FILE: $($s.Path):$($s.LineNumber)"
    for ($i = $s.LineNumber; $i -le [Math]::Min($L.Count, $s.LineNumber + 110); $i++) { W ("{0,6}| {1}" -f $i, $L[$i - 1]); if ($i -gt $s.LineNumber -and $L[$i - 1] -match '^\s*(go|GO)\s*$') { break } }
  }
  if (-not $sqlHits) {
    W ''; W "DDL: khong co 'create table $tb' trong .sql cua repo -> dung MODEL lam tu dien cot"
    # Suy khoa chinh tu FK cua bang khac tro toi bang nay
    $fk = @(Get-ChildItem $Source -Recurse -Filter *.sql -EA SilentlyContinue | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } |
      Select-String -Pattern "references\s+(\[?dbo\]?\.)?\[?$tb\]?\s*\(" | Select-Object -First 3)
    foreach ($s in $fk) { W ("FK tro toi ${tb}: {0}:{1}: {2}" -f $s.Path, $s.LineNumber, $s.Line.Trim()) }
  }
}

# 2) API
$base = $ctrl -replace 'Controller$', ''
$apiF = Get-ChildItem $ApiDir -Filter (($base -replace '_', '') + 'Controller.cs') | Select-Object -First 1
$queue = [System.Collections.Generic.List[string]]::new()
if ($TwoTier) { foreach ($m in [regex]::Matches($gtext, '_biz\.(\w+)\s*\(')) { if (-not $queue.Contains($m.Groups[1].Value)) { $queue.Add($m.Groups[1].Value) } }; $apiF = $null }   # 2 tang: action controller goi thang _biz.WAS_*
if ($apiF) {
  foreach ($w in $was) {
    $apiPath = $apiF.FullName
    $h = Select-String -LiteralPath $apiPath -Pattern "^\s*public\s+.*\s$w\s*\(" | Select-Object -First 1
    if (-not $h) {
      $any = Get-ChildItem $ApiDir -Filter *.cs -EA SilentlyContinue | Where-Object { $_.Name -notmatch 'sync-conflict|\.bak' } | Select-String -Pattern "^\s*public\s+.*\s$w\s*\(" | Select-Object -First 1
      if ($any) { $h = $any; $apiPath = $any.Path }
    }
    if (-not $h) { W "API: khong thay $w trong $ApiDir"; continue }
    $ab = Dump "2. API $w" $apiPath $h.LineNumber
    $atext = (Lines $apiPath)[($ab.Start - 1)..($ab.End - 1)] -join "`n"
    foreach ($m in [regex]::Matches($atext, '_biz\.(\w+)\s*\(')) { if (-not $queue.Contains($m.Groups[1].Value)) { $queue.Add($m.Groups[1].Value) } }
  }
} elseif (-not $TwoTier) { W "API: khong thay controller $(($base -replace '_', ''))Controller.cs trong $ApiDir" }

# 3) BIZ - lan theo loi goi, toi da $Depth tang, chi theo ham cung ho ten (tien to $base)
$done = @{}; $lvl = 0
$prefix = $(if ($TwoTier) { "(?:WAS_)?" + [regex]::Escape($base) } else { [regex]::Escape($base) })   # vd Crd_Member
while ($queue.Count -and $lvl -lt $Depth) {
  $lvl++; $next = [System.Collections.Generic.List[string]]::new()
  foreach ($fn in $queue) {
    if ($done[$fn]) { continue }; $done[$fn] = 1
    $defs = Find-Def $fn
    if (-not $defs) { W ''; W "BIZ: khong thay dinh nghia $fn"; continue }
    if ($defs.Count -gt 1) { W ''; W "BIZ: $fn co $($defs.Count) dinh nghia: $(($defs | ForEach-Object { "$($_.Path):$($_.LineNumber)" }) -join ' ; ') -> trich ban dau, KIEM ban nao duoc compile/goi" }
    $d = $defs[0]
    $bb = Dump "3.$lvl BIZ $fn" $d.Path $d.LineNumber
    $btext = (Lines $d.Path)[($bb.Start - 1)..($bb.End - 1)] -join "`n"
    $pfx = $(if ($TwoTier) { $tk = @(($fn -replace '_New\d+$', '') -split '_' | Where-Object { $_ -and $_ -notin 'WAS', 'WA' }); '(?i)\w*' + (($tk | ForEach-Object { [regex]::Escape($_) }) -join '_(?:WAS?_)?') } else { $prefix })
    foreach ($m in [regex]::Matches($btext, '\b(' + $pfx + '\w*)\s*\(')) {
      $c = $m.Groups[1].Value
      if ($c -ne $fn -and -not $done[$c] -and -not $next.Contains($c)) { $next.Add($c) }
    }
  }
  $queue = $next
}
if ($queue.Count) { W ''; W "CON HAM CHUA TRICH (vuot $Depth tang): $($queue -join ', ')" }

# 4) Tom tat may doc duoc - de Claude doi chieu khi review
$all = $sb.ToString()
$raise = ([regex]::Matches($all, 'CMyException\.Raise\(|throw new')).Count
$tables = @([regex]::Matches($all, '\b(Crd|Mst|Prm|Map|Rpt|Sys|Inv)_\w+') | ForEach-Object Value | Sort-Object -Unique)
$errs = @([regex]::Matches($all, 'ErridnLoyalty\.(\w+)|TError\.\w+\.(\w+)') | ForEach-Object Value | Sort-Object -Unique)
W ''; W '======== 4. TOM TAT MAY DEM (de doi chieu) ========'
W "So cho nem loi (CMyException.Raise / throw new): $raise"
W "Ma loi: $($errs -join ', ')"
W "Ten bang/ham ho Crd_/Mst_/Prm_/Map_/Rpt_/Sys_/Inv_ xuat hien: $($tables -join ', ')"
[IO.File]::WriteAllText($OutFile, $sb.ToString(), $Utf8)
"OK $OutFile ($((Get-Item $OutFile).Length) bytes)"
