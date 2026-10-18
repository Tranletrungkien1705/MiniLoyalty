# Ghi repos\gweb-stats.json cho bang gweb tren dashboard 9router (/dashboard/usage, /dashboard/quota). Goi boi auto-loop moi chu ky.
param([string[]]$Projects = @('MiniContract', 'MiniWMS', 'MiniService', 'MiniGate', 'MiniLoyalty'))
$Root = 'C:\Users\Public\Pictures\kientran_private'; $Repos = "$Root\repos"; $today = (Get-Date).ToString('yyyy-MM-dd')
$chrome = @(); $pf = "$Root\scripts\pipe\chrome-ports.txt"; $ports = if (Test-Path $pf) { @(Get-Content $pf | Where-Object { $_ -match '^\d+$' }) } else { @('9222') }
foreach ($p in $ports) { $up = $false; $pages = 0; try { $r = Invoke-WebRequest "http://127.0.0.1:$p/json" -UseBasicParsing -TimeoutSec 4; $up = $true; $pages = @(($r.Content | ConvertFrom-Json) | Where-Object { $_.type -eq 'page' }).Count } catch { }; $chrome += [ordered]@{ port = [int]$p; up = $up; pages = $pages } }
$out = @()
foreach ($pj in $Projects) {
  $run = "$Repos\$pj\.parity-run"; if (-not (Test-Path "$run\run.log")) { continue }
  $log = @(Get-Content "$run\run.log" -Tail 6000 -Encoding UTF8 -EA SilentlyContinue)
  $subj = @(& git -C "$Repos\$pj" log parity/auto --format=%s -1500 2>$null)
  $workers = @{}
  foreach ($ln in $log) { if ($ln -match '^\[(\d{4}-\d\d-\d\d) [\d:]+\] \[(gweb\d*)\]\s+(.*)$') { $d = $Matches[1]; $w = $Matches[2]; $t = $Matches[3]
      if (-not $workers.ContainsKey($w)) { $workers[$w] = @{ started = 0; ok = 0; err = 0; quota = 0 } }
      if ($d -eq $today) { if ($t -match '^BAT DAU') { $workers[$w].started++ } elseif ($t -match 'agent -> OK') { $workers[$w].ok++ } elseif ($t -match 'agent -> QUOTA') { $workers[$w].quota++ } elseif ($t -match 'ERROR') { $workers[$w].err++ } } } }
  foreach ($w in @($workers.Keys) + @(Get-ChildItem "$run\current_gweb*.json" -EA SilentlyContinue | ForEach-Object { $_.BaseName -replace '^current_', '' }) | Select-Object -Unique) {
    if (-not $workers.ContainsKey($w)) { $workers[$w] = @{ started = 0; ok = 0; err = 0; quota = 0 } }
    $cur = $null; if (Test-Path "$run\current_$w.json") { $c = Get-Content "$run\current_$w.json" -Raw -Encoding UTF8 | ConvertFrom-Json; $cur = "#$($c.no) $($c.item)" }
    $cool = $null; if (Test-Path "$run\cooldown_$w.json") { $c = Get-Content "$run\cooldown_$w.json" -Raw | ConvertFrom-Json; if ([datetime]$c.until -gt (Get-Date)) { $cool = "$($c.why) toi $(([datetime]$c.until).ToString('MM-dd HH:mm'))" } }
    $done = @($subj | Where-Object { $_ -match "agent $w;" }).Count
    $out += [ordered]@{ project = $pj; worker = $w; current = $cur; cooldown = $cool; done_total = $done; today = $workers[$w] }
  }
}
$json = [ordered]@{ updated = (Get-Date).ToString('s'); chrome = $chrome; slots = $out } | ConvertTo-Json -Depth 6
[IO.File]::WriteAllText("$Repos\gweb-stats.json", $json, [Text.UTF8Encoding]::new($false))
