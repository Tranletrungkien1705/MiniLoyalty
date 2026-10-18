# Gui 1 de bai cho CodeGPT qua HTTP bridge (54112), chay het cac vong tool, LUU MOI tin cua agent.
# Ap 4 ban va da tra gia that (docs\codegpt\LUAT_CHAY_CODEGPT_150.md, QUY_TAC_CHO_CODEGPT.md muc 5):
#  1. Invoke-WebRequest -TimeoutSec KHONG chan duoc doc body stream tren PS 5.1 -> goi trong Start-Job + Wait-Job -Timeout.
#  2. Luu moi message role=assistant ra file (khong chi tin cuoi).
#  3. continueTurn=False KHONG co nghia la xong: gap 'Paused after N consecutive tool steps' hoac thieu dong
#     marker (-DoneMarker) thi tu gui 'continue' (toi da -MaxContinue lan).
#  4. Chong 429: jitter truoc moi lan ban, backoff 30/60/90..120s khi RATE_LIMIT/busy, khong ban khi con run 'running'.
# Exit: 0 = co tra loi chua marker; 3 = bridge chet; 4 = 429/rate limit that; 5 = loi khac; 6 = het gio/het continue ma chua co marker;
#       7 = BUSY (luong bi chiem, huy khong duoc) - KHONG phai het han muc.
param(
  [Parameter(Mandatory)][string]$PromptFile,
  [Parameter(Mandatory)][string]$OutFile,
  [string]$SessionId = ('parity-' + (Get-Date -Format 'yyyyMMddHHmmss')),
  [string]$Model = 'deepseek-v4.1-flash',
  [string]$Provider = 'codegptplusbeta',
  [string]$DoneMarker = 'TOM TAT',
  [int]$MaxRounds = 40,
  [int]$MaxContinue = 6,
  [int]$RoundTimeoutSec = 600,    # 10 phut/vong (do that: vong > 10 phut thuong la vong lap) - qa1/qa2 de xuat, Claude duyet 16/10
  [int]$MaxMinutes = 25           # tong toi da 25 phut (truoc 45; codegpt tb 12-22 phut, co ca ket 35-48 phut)
)
$ErrorActionPreference = 'Stop'
$Utf8 = [Text.UTF8Encoding]::new($false)
$Port = 54112
$Driver = 54113   # doan driver trong URL bi bridge bo qua (PLAYBOOK muc 2) - chi can Referer dung dang
$Base = "http://127.0.0.1:$Port/$Driver"
$Referer = "$Base/"
$MsgFile = [IO.Path]::ChangeExtension($OutFile, '.messages.md')

function Log([string]$m) { [IO.File]::AppendAllText($OutFile, ("[{0:HH:mm:ss}] {1}`r`n" -f (Get-Date), $m), $Utf8) }
function Get-Running {
  try {
    $r = Invoke-WebRequest "$Base/api/runs" -Headers @{ Referer = $Referer } -UseBasicParsing -TimeoutSec 10
    $j = [Text.Encoding]::UTF8.GetString($r.RawContentStream.ToArray()) | ConvertFrom-Json
    # dau phay: giu mang rong khi tra ve (PowerShell bung @() thanh $null -> tung bao nham BRIDGE_DEAD)
    , @($j.runs | Where-Object { $_.status -eq 'running' })
  } catch { $null }
}

# HUY run phia server (POST /api/runs/{id}/cancel - da doc ma extension va chay thu that 16/10). Chi huy run DANG chay.
function Cancel-Stale([int]$MinAgeMs, [string]$Why) {
  try {
    $r = Invoke-WebRequest "$Base/api/runs" -Headers @{ Referer = $Referer } -UseBasicParsing -TimeoutSec 10
    $j = [Text.Encoding]::UTF8.GetString($r.RawContentStream.ToArray()) | ConvertFrom-Json
    foreach ($x in @($j.runs | Where-Object { $_.status -eq 'running' -and $_.ageMs -ge $MinAgeMs })) {
      Log ("HUY run {0} (age={1:N1} phut, msgs={2}): {3}" -f $x.runId, ($x.ageMs / 60000), $x.messageCount, $Why)
      try { $null = Invoke-WebRequest "$Base/api/runs/$($x.runId)/cancel" -Method POST -Headers @{ Referer = $Referer } -ContentType 'application/json' -Body '{}' -UseBasicParsing -TimeoutSec 15 } catch { Log "  huy loi: $($_.Exception.Message)" }
    }
  } catch { }
}

[IO.File]::WriteAllText($OutFile, "== cg-run session=$SessionId model=$Model ==`r`n", $Utf8)
[IO.File]::WriteAllText($MsgFile, "# CodeGPT session $SessionId`r`n", $Utf8)
$running = Get-Running
if ($null -eq $running) { Log 'BRIDGE_DEAD: GET /api/runs khong tra loi (Cursor/VS Code co mo workspace kientran_private?)'; exit 3 }

# Run cu bi KET (chay > 15 phut, thuong la vong lap) -> HUY truoc khi ban viec moi
Cancel-Stale 900000 'run cu > 15 phut, nghi lap/ket'
$running = Get-Running; if ($null -eq $running) { Log 'BRIDGE_DEAD sau khi huy'; exit 3 }

# Khong ban khi con run dang chay (tai khoan economy chi 1 luong) - cho toi da 5 phut roi huy run cu
$waitUntil = (Get-Date).AddMinutes(5)
while ($running.Count -gt 0 -and (Get-Date) -lt $waitUntil) {
  $idle = ($running | ForEach-Object { $_.idleMs } | Measure-Object -Minimum).Minimum
  Log "con $($running.Count) run 'running' (idleMs min=$idle) -> cho 30s"
  Start-Sleep 30; $running = Get-Running; if ($null -eq $running) { Log 'BRIDGE_DEAD giua luc cho'; exit 3 }
}
if ($running.Count -gt 0) {
  Cancel-Stale 0 'van con run sau 5 phut cho -> huy de nhuong luong'
  Start-Sleep 5; $running = Get-Running
  if ($null -eq $running -or $running.Count -gt 0) { Log 'BUSY: khong giai phong duoc luong sau khi huy'; exit 7 }
}

$job = {
  param($url, $referer, $body)
  $res = Invoke-WebRequest $url -Method POST -ContentType 'application/json' -Headers @{ Referer = $referer } -Body ([Text.Encoding]::UTF8.GetBytes($body)) -UseBasicParsing -TimeoutSec 3600
  [Text.Encoding]::UTF8.GetString($res.RawContentStream.ToArray())
}

$prompt = [IO.File]::ReadAllText($PromptFile, [Text.Encoding]::UTF8)
$messages = @(@{ role = 'user'; display = @(); content = $prompt })
$deadline = (Get-Date).AddMinutes($MaxMinutes)
$round = 0; $busyHits = 0; $continues = 0; $last = ''; $seen = 0
while ($round -lt $MaxRounds) {
  if ((Get-Date) -gt $deadline) { Log "TIMEOUT tong $MaxMinutes phut"; break }
  $round++
  Start-Sleep -Seconds (Get-Random -Minimum 1 -Maximum 8)   # jitter chong burst
  $body = @{ model = $Model; provider = $Provider; harness = 'codegpt'; sessionId = $SessionId; messages = $messages } | ConvertTo-Json -Depth 80 -Compress
  $j = Start-Job -ScriptBlock $job -ArgumentList "$Base/api/runs", $Referer, $body
  if (-not (Wait-Job $j -Timeout $RoundTimeoutSec)) {
    Stop-Job $j; Remove-Job $j -Force
    Log "ROUND $round TREO > ${RoundTimeoutSec}s -> Stop-Job + HUY run phia server"
    Cancel-Stale 0 "vong $round treo > ${RoundTimeoutSec}s"
    break
  }
  $raw = $null
  try { $raw = Receive-Job $j -ErrorAction Stop } catch { $raw = $null; $errMsg = $_.Exception.Message }
  Remove-Job $j -Force
  if (-not $raw) {
    Log "ROUND $round POST loi: $errMsg"
    if ($errMsg -match '429|RATE_LIMIT|CONCURRENCY|busy') { $busyHits++; if ($busyHits -gt 6) { exit 4 }; Start-Sleep ([math]::Min(120, 30 * $busyHits)); $round--; continue }
    if ($errMsg -match 'fetch failed|ECONNRESET|socket') { $busyHits++; if ($busyHits -gt 6) { exit 5 }; Start-Sleep 30; $round--; continue }
    exit 5
  }
  $frames = $raw -split "`n" | Where-Object { $_ }
  $err = $frames | Where-Object { $_ -match '"t"\s*:\s*"error"' } | Select-Object -Last 1
  if ($err) {
    $em = ($err | ConvertFrom-Json).message
    Log "ROUND $round error frame: $em"
    if ($em -match 'RATE_LIMIT|429|CONCURRENCY|busy|still running|fetch failed') { $busyHits++; if ($busyHits -gt 6) { exit 4 }; Start-Sleep ([math]::Min(120, 30 * $busyHits)); $round--; continue }
    # Loi tam cua model (do that 23:47: 'model stream stalled: no data within the inter-token budget') -> gui lai DUNG hoi thoai hien tai
    if ($em -match 'stalled|inter-token|timed? ?out|ECONNRESET|socket hang up|overloaded|502|503|not attached to a stream|Invalid state') { $busyHits++; if ($busyHits -gt 12) { exit 5 }; Start-Sleep 30; $round--; continue }
    exit 5
  }
  $ln = $frames | Where-Object { $_ -match '"t"\s*:\s*"final"' } | Select-Object -Last 1
  if (-not $ln) { Log "ROUND $round khong co frame final"; break }
  $final = $ln | ConvertFrom-Json
  $messages = @($final.messages)
  # luu moi tin assistant moi phat sinh
  for ($k = $seen; $k -lt $messages.Count; $k++) {
    $mm = $messages[$k]
    if ($mm.role -eq 'assistant') { $c = if ($mm.content -is [string]) { $mm.content } else { ($mm.content | ConvertTo-Json -Depth 10 -Compress) }; [IO.File]::AppendAllText($MsgFile, "`r`n---- round $round / msg $k ----`r`n$c`r`n", $Utf8) }
  }
  $seen = $messages.Count
  $lastMsg = $messages | Where-Object { $_.role -eq 'assistant' } | Select-Object -Last 1
  $last = if ($lastMsg.content -is [string]) { $lastMsg.content } else { "$($lastMsg.content)" }
  Log ("ROUND {0} continueTurn={1} pending={2} msgs={3} len={4}" -f $round, $final.continueTurn, $final.pending, $messages.Count, $last.Length)
  if ($final.pending) { $busyHits++; if ($busyHits -gt 6) { exit 4 }; Start-Sleep 60; continue }
  if ($final.continueTurn) { continue }
  # continueTurn=False: chi coi la xong khi co marker va khong bi tam dung
  $paused = $last -match 'Paused after \d+ consecutive tool steps'
  if ($paused -or $last -notmatch [regex]::Escape($DoneMarker)) {
    if ($continues -ge $MaxContinue) { Log "HET $MaxContinue lan continue ma chua co '$DoneMarker'"; break }
    $continues++
    $why = if ($paused) { 'bi tam dung o moc tool-step' } else { "thieu muc '$DoneMarker'" }
    Log "  $why -> gui continue ($continues/$MaxContinue)"
    $messages += @{ role = 'user'; display = @(); content = "continue. Lam tiep cho XONG hang muc theo de bai, roi ket thuc bang muc $DoneMarker dung mau. Chua xong thi noi ro chua xong, CAM bao xong." }
    continue
  }
  break
}
[IO.File]::AppendAllText($OutFile, "`r`n===== FINAL =====`r`n$last`r`n", $Utf8)
if ($last -match [regex]::Escape($DoneMarker)) { exit 0 } elseif ($last) { exit 6 } else { exit 5 }
