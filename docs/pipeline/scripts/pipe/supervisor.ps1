# Giam sat KHONG can Claude: tu chuyen du an trong hang doi khi xong, tu khoi dong lai slot chet, day parity/auto len git, dung o han chot.
# Dung: Start-Process -WindowStyle Hidden powershell -ArgumentList '-NoProfile -ExecutionPolicy Bypass -File scripts\pipe\supervisor.ps1'
# Dung tay: tao file repos\SUPERVISOR_STOP. Nhat ky: repos\supervisor.log. Bao cao cuoi: docs\FINAL_REPORT.md
param([string[]]$Queue = @('MiniContract', 'MiniWMS', 'MiniService', 'MiniGate'), [datetime]$Deadline = [datetime]'2026-10-20T23:00:00', [int]$EverySec = 300,
  [string[]]$Router = @('agy', 'agy2', 'agy3', 'agy4', 'agy5', 'agy6'), [string[]]$Gweb = @('gweb'), [switch]$Push)
$Root = 'C:\Users\Public\Pictures\kientran_private'; $Repos = "$Root\repos"; $Utf8 = [Text.UTF8Encoding]::new($false); $Log = "$Repos\supervisor.log"
function L([string]$m) { [IO.File]::AppendAllText($Log, ("[{0:yyyy-MM-dd HH:mm:ss}] {1}`r`n" -f (Get-Date), $m), $Utf8) }
function Stat([string]$p) {
  & powershell -NoProfile -ExecutionPolicy Bypass -File "$Root\scripts\pipe\status.ps1" -Project $p 2>&1 | Out-Null
  $f = "$Repos\$p\.parity-run\status.md"; if (-not (Test-Path $f)) { return $null }
  $t = Get-Content $f -Raw -Encoding UTF8
  $m = [regex]::Match($t, 'Tien do: \*\*(\d+)/(\d+)\*\* da lam \| (\d+) bo \| (\d+) con lai'); if (-not $m.Success) { return $null }
  $cur = [regex]::Match($t, 'Slot dang giu: (.*)').Groups[1].Value.Trim()
  $fd = [regex]::Match($t, 'Feeder: (\d+) \| auto-loop: (\d+)')
  [pscustomobject]@{ Done = [int]$m.Groups[1].Value; Total = [int]$m.Groups[2].Value; Bo = [int]$m.Groups[3].Value; Todo = [int]$m.Groups[4].Value; Busy = ($cur.Length -gt 0); Feeders = [int]$fd.Groups[1].Value; Loop = [int]$fd.Groups[2].Value }
}
function Start-Proj([string]$p) {
  $a = @{ Project = $p; Direct = @(); Router = $Router; Gweb = $Gweb; Other = @() }
  if ($p -eq 'MiniGate') { $a.SharedCtrl = $true }
  & powershell -NoProfile -ExecutionPolicy Bypass -File "$Root\scripts\pipe\start-all-slots.ps1" @a 2>&1 | Out-Null
  L "START $p router=$($Router -join ',') gweb=$($Gweb -join ',')"
}
function Report([string]$why) {
  $sb = [Text.StringBuilder]::new(); [void]$sb.AppendLine("# Bao cao cuoi cua supervisor ($(Get-Date -Format 'yyyy-MM-dd HH:mm')) - $why`n")
  foreach ($p in $Queue) { $s = Stat $p; if ($s) { [void]$sb.AppendLine("- **$p**: $($s.Done)/$($s.Total) da lam, $($s.Bo) bo (forwarder/can xem tay), $($s.Todo) con lai") } }
  [IO.File]::WriteAllText("$Root\docs\FINAL_REPORT.md", $sb.ToString(), $Utf8); L "REPORT $why"
}
& powercfg /change standby-timeout-ac 0 2>&1 | Out-Null; & powercfg /change hibernate-timeout-ac 0 2>&1 | Out-Null; & powercfg /change monitor-timeout-ac 0 2>&1 | Out-Null
L "SUPERVISOR bat dau; han chot $Deadline; hang doi $($Queue -join ' > ')"
$lastStart = [datetime]'2000-01-01'; $lastPush = Get-Date; $curProj = ''
while ($true) {
  if (Test-Path "$Repos\SUPERVISOR_STOP") { Report 'dung tay'; break }
  if ((Get-Date) -gt $Deadline) {
    foreach ($p in $Queue) { New-Item -ItemType File -Force "$Repos\$p\.parity-run\STOP" | Out-Null }
    Get-CimInstance Win32_Process | Where-Object { $_.Name -match 'powershell' -and $_.CommandLine -match '(feeder|auto-loop)\.ps1' } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -EA SilentlyContinue }
    Report 'het han'; break }
  try {
    $target = $null
    foreach ($p in $Queue) { $s = Stat $p; if ($s -and ($s.Todo -gt 0 -or $s.Busy)) { $target = $p; $ts = $s; break } }
    if (-not $target) { Report 'hang doi da xong'; break }
    $minSince = ((Get-Date) - $lastStart).TotalMinutes
    if ($curProj -ne $target) { Start-Proj $target; $curProj = $target; $lastStart = Get-Date; L "CHUYEN sang $target ($($ts.Todo) con lai)" }
    elseif (($ts.Feeders -lt 3 -or $ts.Loop -lt 1) -and $minSince -gt 20) { Start-Proj $target; $lastStart = Get-Date; L "HOI SINH $target (feeder=$($ts.Feeders) loop=$($ts.Loop))" }
    else {
      # ket: khong co commit moi >3h va todo>0 -> khoi dong lai
      $last = [datetime](& git -C "$Repos\$target" log -1 --format=%cI parity/auto 2>$null)
      if (((Get-Date) - $last).TotalHours -gt 3 -and $minSince -gt 180) { Start-Proj $target; $lastStart = Get-Date; L "KHOI DONG LAI $target (3h khong co commit)" }
    }
    if ($Push -and ((Get-Date) - $lastPush).TotalHours -ge 6) {
      foreach ($p in $Queue) { & git -C "$Repos\$p" push origin parity/auto 2>&1 | Out-Null }
      $lastPush = Get-Date; L 'PUSH parity/auto'
    }
  } catch { L "LOI $($_.Exception.Message)" }
  Start-Sleep $EverySec
}
