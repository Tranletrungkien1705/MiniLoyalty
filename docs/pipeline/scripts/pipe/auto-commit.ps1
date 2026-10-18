# Tu dong chot hang muc "an toan" de slot khong cho Claude. Dieu kien (TAT CA): result CHO_CLAUDE co doi file, file chi nam trong Controllers/Port|Models/Port|*.Tests,
# moi ma loi Fail("X") ton tai trong hang so nguon, khong co dau hieu tu che (Simulate/THROW_/mo phong/gia lap/mock/DL01), test xanh (build/test cua agent HOAC chay lai trong worktree dat),
# khong dinh nghia lai kieu da co o file khac (build xanh bao dam). Khong dat -> de nguyen cho Claude. Moi quyet dinh ghi run.log [auto-commit]. Dung boi auto-loop.
param([Parameter(Mandatory)][string]$Project)
$Root = 'C:\Users\Public\Pictures\kientran_private'; $Run = "$Root\repos\$Project\.parity-run"; $env:PIPE_PROJECT = $Project; $Utf8 = [Text.UTF8Encoding]::new($false)
$mutex = New-Object System.Threading.Mutex($false, "Global\auto-commit-$Project"); if (-not $mutex.WaitOne(0)) { return }
try {
$lastF = "$Run\auto_commit_last.json"; $last = @{}; if (Test-Path $lastF) { (Get-Content $lastF -Raw | ConvertFrom-Json).PSObject.Properties | ForEach-Object { $last[$_.Name] = [string]$_.Value } }
function Log([string]$m) { $k = ($m -split ' ')[1] + ($m -split ' ')[2]; if ($last[$k] -eq $m) { return }; $last[$k] = $m; [IO.File]::WriteAllText($lastF, ($last | ConvertTo-Json), $Utf8); [IO.File]::AppendAllText("$Run\run.log", ("[{0:yyyy-MM-dd HH:mm:ss}] [auto-commit] {1}`r`n" -f (Get-Date), $m), $Utf8) }
$src = switch ($Project) { 'MiniContract' { 'D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Constants' } 'MiniGate' { 'D:\idocNet\2019.9.MobileGate\Dev\V10\idN.MobileGate.Constants' } default { 'D:\idocNet\2022.7.Loyalty\12.Dev.Common\idn.Loyalty.Constants' } }
$constText = ''; foreach ($f in Get-ChildItem $src -Filter *.cs -Recurse -EA SilentlyContinue) { $constText += [IO.File]::ReadAllText($f.FullName) }
foreach ($rf in Get-ChildItem "$Run\result_*.json" -EA SilentlyContinue) {
  $r = Get-Content $rf.FullName -Raw -Encoding UTF8 | ConvertFrom-Json
  if ($r.status -ne 'CHO_CLAUDE') { continue }
  $chg = @(@($r.changed) | Where-Object { $_ -and $_ -notmatch 'NO_BACKEND\.flag$' })
  if ($chg.Count -eq 0) { continue }
  $w = $r.worker; $tag = "#$($r.no) $($r.item) ($w)"
  if (@($chg | Where-Object { $_ -notmatch '^(Controllers/Port/|Models/Port/|[\w\.]+\.Tests/)' }).Count) { Log "BO QUA $tag : file ngoai pham vi: $((@($chg | Where-Object { $_ -notmatch '^(Controllers/Port/|Models/Port/|[\w\.]+\.Tests/)' }) -join ', '))"; continue }
  $patch = "$($r.dir)\diff_r$($r.round).patch"; if (-not (Test-Path $patch)) { continue }
  $p = [IO.File]::ReadAllText($patch, [Text.Encoding]::UTF8)
  $add = ($p -split "`n" | Where-Object { $_ -match '^\+' -and $_ -notmatch '^\+\+\+' }) -join "`n"
  $prodAdd = ((($p -split "(?m)^diff --git ") | Where-Object { $_ -and $_ -notmatch '^a/[^\r\n]*\.Tests/' }) -join "`n" -split "`n" | Where-Object { $_ -match '^\+' -and $_ -notmatch '^\+\+\+' }) -join "`n"   # dau hieu tu che chi xet file san pham (khong xet du lieu mau trong test)
  $bad = @([regex]::Matches($prodAdd, 'Simulate|THROW_|mô phỏng|giả lập|\bmock\b|"DL01"|X-Simulate') | ForEach-Object Value | Select-Object -Unique)
  if ($bad.Count) { Log "BO QUA $tag : dau hieu tu che: $($bad -join ',')"; continue }
  $codes = @([regex]::Matches($add, 'Fail\("([A-Za-z0-9_\.]+)"') | ForEach-Object { ($_.Groups[1].Value -split '\.')[-1] } | Select-Object -Unique)
  $selfCode = (($r.item -split '/')[0] -replace 'Controller$', '') + '_' + ($r.item -split '/')[1]
  $miss = @($codes | Where-Object { $_ -ne $selfCode -and $constText -notmatch [regex]::Escape($_) })
  if ($miss.Count) { Log "BO QUA $tag : ma loi khong co trong nguon: $($miss -join ',')"; continue }
  $ok = [bool]$r.build
  if (-not $ok) {
    $wt = $r.worktree; if (-not $wt) { $wt = "$Root\repos\$Project-$w" }
    $t = (& dotnet test "$wt\$Project.Tests" -c Release --nologo 2>&1 | Out-String)
    $ok = ($t -match 'Passed!' -and $t -notmatch 'Failed!')
    if (-not $ok) { Log "BO QUA $tag : build/test chua xanh (chay lai)"; continue }
  }
  $eng = if ($w -like 'agy[2-5]') { 'rtr' } elseif ($w -like 'agy*') { 'agy' } else { $w }
  $out = (& powershell -NoProfile -ExecutionPolicy Bypass -File "$Root\scripts\parity\parity-step.ps1" -Action commit -Worker $w -Engine $eng -Message "feat(parity): port 1-1 $Project #$($r.no) $($r.item) (agent $w; tu dong chot: ma loi khop nguon, khong tu che, test xanh)" 2>&1 | Out-String)
  if ($out -match 'CHOT #') { Remove-Item $rf.FullName -Force -EA SilentlyContinue; Log "DA CHOT $tag" } else {
    # Chot loi (thuong: xung dot cherry-pick vi nen cu) -> don repo chinh ve sach + tra hang muc ve kho de lam lai tren nen moi
    $rp = "$Root\repos\$Project"
    if (Test-Path "$rp\.git\CHERRY_PICK_HEAD") { & git -C $rp cherry-pick --abort 2>&1 | Out-Null }
    if (& git -C $rp status --porcelain) { & git -C $rp reset -q --hard 2>&1 | Out-Null }
    & powershell -NoProfile -ExecutionPolicy Bypass -File "$Root\scripts\pipe\release.ps1" -Project $Project -Worker $w 2>&1 | Out-Null
    Remove-Item $rf.FullName -Force -EA SilentlyContinue
    Log "LOI CHOT $tag -> don repo + tra ve kho de lam lai: $(($out -split "`n" | Where-Object { $_ -match 'conflict|CONFLICT|loi' } | Select-Object -First 1))" }
}
} finally { $mutex.ReleaseMutex() }
