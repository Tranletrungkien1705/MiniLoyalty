# Tom tat nhanh 1 ket qua CHO_CLAUDE de review: file doi, ma loi dung (co trong hang so nguon khong), dau hieu tu che, GAP, VERIFY. Dung: review-sum.ps1 -Project MiniContract -Worker agy2
param([Parameter(Mandatory)][string]$Project, [Parameter(Mandatory)][string]$Worker)
$Root = 'C:\Users\Public\Pictures\kientran_private'; $Run = "$Root\repos\$Project\.parity-run"
$r = Get-Content "$Run\result_$Worker.json" -Raw -Encoding UTF8 | ConvertFrom-Json
$d = $r.dir; $p = Get-Content "$d\diff_r$($r.round).patch" -Raw -Encoding UTF8
"== #$($r.no) $($r.item) [$Worker] build=$($r.build) round=$($r.round)"
"files: " + ((@($r.changed) | ForEach-Object { $_ }) -join ', ')
$add = ($p -split "`n" | Where-Object { $_ -match '^\+' -and $_ -notmatch '^\+\+\+' })
"dong them: $($add.Count)"
$prefixes = switch ($Project) { 'MiniContract' { 'Erridn.Skycic.QContract' } 'MiniGate' { 'ErridN.MobileGate' } default { 'ErridnLoyalty' } }
$codes = @([regex]::Matches(($add -join "`n"), 'Fail\("([A-Za-z0-9_\.]+)"') | ForEach-Object { $_.Groups[1].Value } | Select-Object -Unique)
$pj = (Get-Content "$Root\scripts\parity\profile.ps1" -Raw); 
$consts = $null
$src = switch ($Project) { 'MiniContract' { 'D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Constants' } 'MiniGate' { 'D:\idocNet\2019.9.MobileGate\Dev\V10\idN.MobileGate.Constants' } default { 'D:\idocNet\2022.7.Loyalty\12.Dev.Common\idn.Loyalty.Constants' } }
foreach ($c in $codes) { $last = ($c -split '\.')[-1]; $hit = (Get-ChildItem $src -Filter *.cs -Recurse -EA SilentlyContinue | Select-String -Pattern ([regex]::Escape($last)) -List -EA SilentlyContinue | Select-Object -First 1); "  ma loi $c : " + $(if ($hit) { 'CO trong nguon' } else { '!! KHONG thay trong hang so nguon' }) }
$sus = @($add | Where-Object { $_ -match 'Simulate|THROW_|mô phỏng|giả lập|mock|DummyData|"DL01"|"1001"|X-Simulate' })
"dau hieu tu che: " + $(if ($sus.Count) { ($sus | Select-Object -First 5) -join ' || ' } else { 'khong' })
$w = Get-ChildItem $d -Filter "write_r$($r.round)_*.txt" | Where-Object { $_.Name -notmatch 'prompt|usage|try' } | Select-Object -First 1
if ($w) { $t = Get-Content $w.FullName -Raw -Encoding UTF8; $i = $t.LastIndexOf('TOM TAT'); if ($i -ge 0) { $t = $t.Substring($i) }; ($t -split "`n" | Where-Object { $_ -match '^(GAP|CONFLICT|BUILD|TRANG_THAI)' } | Select-Object -First 6) -join "`n" }
