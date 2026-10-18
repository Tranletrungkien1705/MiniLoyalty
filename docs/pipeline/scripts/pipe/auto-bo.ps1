# Xu ly TU DONG cac slot ket (khong cho Claude): chay moi 45 giay boi auto-loop.ps1. Moi quyet dinh ghi vao run.log (tien to [auto]).
#  A) CHO_CLAUDE khong doi file: co conflict (conflicts\<so>_conflict.md) voi bang chung dich vu ngoai, HOAC excerpt la chuyen tiep thuan (fwd && !sql) -> Bo.
#     Khong co bang chung -> tra hang muc ve kho (release) toi da 2 lan, lan 3 -> Bo "can xem tay".
#  B) Slot con current_*.json nhung KHONG co tien trinh parity-step nao va file khong doi > 20 phut (tru CHO_CLAUDE co doi file = cho Claude) -> release.
# Dung: auto-bo.ps1 -Project MiniGate
param([Parameter(Mandatory)][string]$Project)
$Root = 'C:\Users\Public\Pictures\kientran_private'; $Run = "$Root\repos\$Project\.parity-run"; $env:PIPE_PROJECT = $Project
$Step = "$Root\scripts\parity\parity-step.ps1"; $Utf8 = [Text.UTF8Encoding]::new($false)
function Log([string]$m) { $l = "[{0:yyyy-MM-dd HH:mm:ss}] [auto] {1}" -f (Get-Date), $m; [IO.File]::AppendAllText("$Run\run.log", $l + "`r`n", $Utf8) }
$relF = "$Run\release_count.json"; $rel = @{}; if (Test-Path $relF) { (Get-Content $relF -Raw | ConvertFrom-Json).PSObject.Properties | ForEach-Object { $rel[$_.Name] = [int]$_.Value } }
function Save-Rel { [IO.File]::WriteAllText($relF, ($rel | ConvertTo-Json), $Utf8) }
$fwdRx = 'Services?\.Instance\.|ClientService|GetUrlCurrentApiNetwork|AccountService|HttpClient|RestClient|WebRequest|PostData|CmSys_InvalidOutSite|dich vu ngoai|dịch vụ ngoài|MstSvRoute'
$sqlRx = 'ExecQuery|ExecuteNonQuery|ExecSql|ExecuteScalar|SqlCommand|zzzzClause|\.SaveData\(|_cf\.db\.(ExecQuery|ExecuteNonQuery|ExecSql|SaveData)|\w+_CheckDB\('
function Drop-Bo($w, $reason) { & powershell -NoProfile -ExecutionPolicy Bypass -File $Step -Action drop -Worker $w -Status bo -Reason $reason 2>&1 | Out-Null }
function Release($w, $c, $why) {
  # tra hang muc ve kho: don worktree, bo claim + current + result; KHONG ghi vao skipped
  $wt = "$Root\repos\$Project-$w"
  if (Test-Path "$wt\.git") { & git -C $wt reset -q --hard parity/auto 2>&1 | Out-Null; & git -C $wt clean -qfd 2>&1 | Out-Null }
  Remove-Item ("$Run\claims\{0:D3}.claim" -f [int]$c.no), "$Run\current_$w.json", "$Run\result_$w.json" -Force -EA SilentlyContinue
  Log "RELEASE #$($c.no) $($c.item) khoi $w : $why"
}
$procs = @(Get-CimInstance Win32_Process | Where-Object { $_.CommandLine -match 'parity-step\.ps1' -and $_.CommandLine -notmatch 'Get-CimInstance' } | ForEach-Object { $_.CommandLine })
foreach ($cf in Get-ChildItem "$Run\current_*.json" -EA SilentlyContinue) {
  $w = $cf.BaseName -replace '^current_', ''
  $c = Get-Content $cf.FullName -Raw -Encoding UTF8 | ConvertFrom-Json
  $rf = "$Run\result_$w.json"; $r = if (Test-Path $rf) { Get-Content $rf -Raw -Encoding UTF8 | ConvertFrom-Json } else { $null }
  $running = [bool]($procs | Where-Object { $_ -match "-Worker\s+$w\b" -and $_ -match '-Action\s+(run|fix)' })
  if ($r -and $r.status -eq 'CHO_CLAUDE' -and $r.item -eq $c.item) {
    if (@(@($r.changed) | Where-Object { $_ -and $_ -notmatch 'NO_BACKEND\.flag$|\.parity-run' }).Count -gt 0) { continue }   # co code that -> Claude review (bo qua file co NO_BACKEND.flag agent tao nham trong worktree)
    $dir = $c.dir; $cfl = "$Run\conflicts\$($c.no)_conflict.md"
    $ev = $false
    if (Test-Path $cfl) { $ev = ((Get-Content $cfl -Raw -Encoding UTF8) -match $fwdRx) }
    if (-not $ev -and (Test-Path "$dir\source_excerpt.txt")) { $t = Get-Content "$dir\source_excerpt.txt" -Raw -Encoding UTF8; $i = $t.IndexOf('BIZ '); $b = if ($i -ge 0) { $t.Substring($i) } else { '' }; if ($b -and (($b -match $fwdRx -and $b -notmatch $sqlRx) -or ($b -match 'GetUrlCurrentApiNetwork' -and $b -match 'Services?\.Instance\.'))) { $ev = $true } }
    if ($ev) { Drop-Bo $w "Chi chuyen tiep sang dich vu ngoai, khong co SQL/nghiep vu noi bo (tu dong, xem conflicts\$($c.no)_conflict.md)"; Remove-Item $rf -Force -EA SilentlyContinue; Log "BO #$($c.no) $($c.item) ($w) - chuyen tiep dich vu ngoai"; continue }
    $k = "$($c.no)"; $n = 1 + $(if ($rel.ContainsKey($k)) { $rel[$k] } else { 0 })
    if ($n -gt 2) { Drop-Bo $w "Agent 2 lan khong tao thay doi va khong co bang chung dich vu ngoai - can xem tay"; Remove-Item $rf -Force -EA SilentlyContinue; Log "BO(can xem tay) #$($c.no) $($c.item) ($w)"; continue }
    $rel[$k] = $n; Save-Rel; Release $w $c "khong doi file, thieu bang chung (lan $n)"; continue
  }
  # B) ket khong tien trinh
  if (-not $running) {
    $newest = @($cf, $(if (Test-Path $rf) { Get-Item $rf })) | Where-Object { $_ } | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    $age = ((Get-Date) - $newest.LastWriteTime).TotalMinutes
    if ($age -gt 20 -and -not ($r -and $r.status -eq 'CHO_CLAUDE')) {
      $k = "$($c.no)"; $n = 1 + $(if ($rel.ContainsKey($k)) { $rel[$k] } else { 0 })
      if ($n -gt 3) { Drop-Bo $w "Ket nhieu lan (khong tien trinh) - can xem tay"; Remove-Item $rf -Force -EA SilentlyContinue; Log "BO(ket) #$($c.no) $($c.item) ($w)"; continue }
      $rel[$k] = $n; Save-Rel; Release $w $c ("ket $([int]$age) phut khong tien trinh, trang thai " + $(if ($r) { $r.status } else { 'khong ro' }))
    }
  }
}
