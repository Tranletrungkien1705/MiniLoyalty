# Don tab thua cua cac Chrome gweb (Chrome khoi phuc phien cu -> hang chuc tab Gemini/dashboard lam Playwright connectOverCDP treo > 150s).
# Giu DUNG 1 tab "https://gemini.google.com/app" (hoac tab Gemini bat ky neu khong co), dong het page khac. Khong dong khi tab dang co gw.lock cua cong do? (tab chay se la tab 'app/<id>' -> giu neu la tab duy nhat dang dung).
# Dung: chrome-clean.ps1 [-Ports 9222,9223,9224,9226]
param([int[]]$Ports = @())
if (-not $Ports.Count) { $pf = "$PSScriptRoot\chrome-ports.txt"; $Ports = $(if (Test-Path $pf) { @(Get-Content $pf | Where-Object { $_ -match '^\d+$' } | ForEach-Object { [int]$_ }) } else { @(9222) }) }
$prof = @{ 9222 = 'chrome-gemini-profile'; 9223 = 'chrome-gemini-profile-b'; 9224 = 'chrome-gemini-profile-c'; 9225 = 'chrome-gemini-profile-d'; 9226 = 'chrome-gemini-profile-e' }
foreach ($p in $Ports) {
  # TU PHUC HOI: Chrome cong nay chet/bi tat -> mo lai dung profile (dang nhap con trong profile). Chong mo lien tuc: moi cong toi da 1 lan / 3 phut.
  $up = $false; try { $up = ((Invoke-WebRequest "http://127.0.0.1:$p/json/version" -UseBasicParsing -TimeoutSec 5).StatusCode -eq 200) } catch { }
  if (-not $up -and $prof[$p]) {
    $stamp = "$env:TEMP\chrome_relaunch_$p.txt"
    if (-not (Test-Path $stamp) -or (((Get-Date) - (Get-Item $stamp).LastWriteTime).TotalMinutes -gt 3)) {
      Set-Content $stamp (Get-Date -Format s)
      $hl = @(); if (Test-Path "$PSScriptRoot\chrome-headless.txt") { $hl = @('--headless=new', '--window-size=1280,900') }   # co file chrome-headless.txt -> Chrome chay an, khong cua so
      Start-Process 'C:\Program Files\Google\Chrome\Application\chrome.exe' -ArgumentList ($hl + @("--remote-debugging-port=$p", "--user-data-dir=C:\Users\Public\Pictures\kientran_private\$($prof[$p])", '--no-first-run', '--disable-session-crashed-bubble', 'https://gemini.google.com/app'))
      "port $p : Chrome chet -> mo lai"
    }
    continue
  }
  try { $raw = (Invoke-WebRequest "http://127.0.0.1:$p/json" -UseBasicParsing -TimeoutSec 8).Content | ConvertFrom-Json } catch { continue }
  $pages = @($raw | Where-Object { $_.type -eq 'page' })
  if ($pages.Count -le 1) { continue }   # it tab thi khong dong (tranh dong tab dang chay lenh)
  $tag = @{ 9222 = ''; 9223 = '.b'; 9224 = '.c'; 9225 = '.d'; 9226 = '.e' }[$p]
  $lock = "C:\Users\Public\Pictures\kientran_private\scripts\parity\gemini-web\gw.lock$tag"
  if ((Test-Path $lock) -and (((Get-Date) - (Get-Item $lock).LastWriteTime).TotalMinutes -lt 12)) { continue }   # cong dang co lenh chay
  $keep = $pages | Where-Object { $_.url -eq 'https://gemini.google.com/app' } | Select-Object -First 1
  if (-not $keep) { $keep = $pages | Where-Object { $_.url -like 'https://gemini.google.com/*' } | Select-Object -First 1 }
  if (-not $keep) { $keep = $pages | Select-Object -First 1 }
  $n = 0
  foreach ($t in $pages) { if ($t.id -eq $keep.id) { continue }; try { Invoke-WebRequest "http://127.0.0.1:$p/json/close/$($t.id)" -UseBasicParsing -TimeoutSec 5 | Out-Null; $n++ } catch { } }
  "port $p : dong $n tab, giu $($keep.url)"
}
