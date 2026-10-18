# Ghi <repo>\.parity-run\status.md (ngan, doc nhanh): tien do, slot, bat thuong, can Claude. Goi boi auto-loop moi chu ky; in ra khi chay tay.
param([Parameter(Mandatory)][string]$Project, [switch]$Print)
$Root = 'C:\Users\Public\Pictures\kientran_private'; $Repo = "$Root\repos\$Project"; $Run = "$Repo\.parity-run"; $Utf8 = [Text.UTF8Encoding]::new($false)
$led = @(Get-Content "$Repo\PARITY_PROGRESS.md" -Encoding UTF8 | Where-Object { $_ -match '^\| \d+ \|' })
$sk = @{}; if (Test-Path "$Run\skipped.json") { (Get-Content "$Run\skipped.json" -Raw -Encoding UTF8 | ConvertFrom-Json).PSObject.Properties | ForEach-Object { $sk[$_.Name] = [string]$_.Value } }
$done = 0; $bo = 0; $todo = 0; foreach ($l in $led) { $p = $l -split '\|'; $n = $p[2].Trim(); if ($p.Count -gt 4 -and $p[4].Trim() -match '^[0-9a-f]{7}') { $done++ } elseif ($sk.ContainsKey($n)) { $bo++ } else { $todo++ } }
$cur = @(Get-ChildItem "$Run\current_*.json" -EA SilentlyContinue | ForEach-Object { $c = Get-Content $_.FullName -Raw | ConvertFrom-Json; "$($c.worker):#$($c.no)" })
$cool = @(Get-ChildItem "$Run\cooldown_*.json" -EA SilentlyContinue | ForEach-Object { $c = Get-Content $_.FullName -Raw | ConvertFrom-Json; if ([datetime]$c.until -gt (Get-Date)) { "$($_.BaseName -replace 'cooldown_','')($($c.why) toi $(([datetime]$c.until).ToString('MM-dd HH:mm')))" } })
$wait = @(Get-ChildItem "$Run\result_*.json" -EA SilentlyContinue | ForEach-Object { $r = Get-Content $_.FullName -Raw | ConvertFrom-Json; if ($r.status -eq 'CHO_CLAUDE') { "$($r.worker):#$($r.no)" } })
$feeders = @(Get-CimInstance Win32_Process -EA SilentlyContinue | Where-Object { $_.Name -match 'powershell' -and $_.CommandLine -match 'feeder\.ps1' -and $_.CommandLine -notmatch 'Get-CimInstance' }).Count
$loop = @(Get-CimInstance Win32_Process -EA SilentlyContinue | Where-Object { $_.Name -match 'powershell' -and $_.CommandLine -match 'auto-loop\.ps1' -and $_.CommandLine -notmatch 'Get-CimInstance' }).Count
$os = Get-CimInstance Win32_OperatingSystem
$last = @(& git -C $Repo log --oneline -300 2>$null | Where-Object { $_ -match 'feat\(parity\)' } | Select-Object -First 5 | ForEach-Object { $_.Substring(0, [Math]::Min(100, $_.Length)) })
$lastErr = @(Get-Content "$Run\run.log" -Tail 400 -Encoding UTF8 -EA SilentlyContinue | Where-Object { $_ -match 'LOI CHOT|RELEASE|BO\(can xem tay\)|QUOTA|AGENT_DOWN' } | Select-Object -Last 4 | ForEach-Object { $_.Substring(0, [Math]::Min(150, $_.Length)) })
$need = @(); if ($todo -eq 0 -and $cur.Count -eq 0) { $need += 'DU AN XONG - can Claude chuyen du an ke' }; if ($feeders -lt 3) { $need += "feeder chi con $feeders" }; if ($loop -lt 1) { $need += 'auto-loop chet' }; if ($wait.Count -gt 0) { $need += "cho Claude review: $($wait -join ',')" }
$md = @"
# $Project  ($(Get-Date -Format 'yyyy-MM-dd HH:mm'))
Tien do: **$done/$($led.Count)** da lam | $bo bo | $todo con lai
Slot dang giu: $($cur -join ' ')
Nghi/khoa: $($cool -join ' ')
Cho Claude: $($wait -join ' ')
Feeder: $feeders | auto-loop: $loop | RAM trong: $([int]($os.FreePhysicalMemory/1024)) MB
Can Claude: $(if ($need.Count) { $need -join '; ' } else { 'khong' })
Gan day:
$($last -join "`n")
Bat thuong:
$($lastErr -join "`n")
"@
[IO.File]::WriteAllText("$Run\status.md", $md, $Utf8)
if ($Print) { $md }
