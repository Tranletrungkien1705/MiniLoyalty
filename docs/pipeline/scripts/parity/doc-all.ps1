# Tai tao tai lieu API cho TAT CA controller Port bang Gemini web (1 tai khoan Chrome moi luong).
# Dung: doc-all.ps1 -Stream a|b|c [-Stride 1]  (a=9222, b=9223, c=9224). -Stride 3 = chia 3 luong theo chi so; -Stride 1 = luong nay lam het.
# Bo qua controller co tai lieu moi hon ma nguon. Loi (CDP/ban) -> thu lai toi 2 lan sau 40 giay.
param([Parameter(Mandatory)][ValidateSet('a','b','c')][string]$Stream, [switch]$Force, [int]$Stride = 3)
$Root = 'C:\Users\Public\Pictures\kientran_private'; $Repo = "$Root\repos\MiniLoyalty"; $Docs = "$Root\repos\MiniLoyalty-docs\docs\api"
$env:GW_PORT = @{ a = '9222'; b = '9223'; c = '9224' }[$Stream]; $env:GW_TAG = $(if ($Stream -eq 'a') { '' } else { $Stream })
$all = @(Get-ChildItem "$Repo\Controllers\Port\*ControllerPort.cs" | ForEach-Object { $_.BaseName -replace 'Port$', '' } | Sort-Object)
$idx = $(if ($Stride -le 1) { 0 } else { @{ a = 0; b = 1; c = 2 }[$Stream] })
$fail = @()
for ($i = $idx; $i -lt $all.Count; $i += [Math]::Max(1, $Stride)) {
  $c = $all[$i]; $doc = "$Docs\$c.md"
  $srcT = [datetime]::Parse((& git -C $Repo log -1 --format=%cI parity/auto -- "Controllers/Port/${c}Port.cs"))
  if (-not $Force -and (Test-Path $doc) -and ((Get-Item $doc).LastWriteTime -gt $srcT)) { continue }
  $ok = $false
  foreach ($try in 1..3) {
    & powershell -NoProfile -ExecutionPolicy Bypass -File "$Root\scripts\parity\doc-gw.ps1" -Controller $c -Role "doc1" | Out-Null
    if ($LASTEXITCODE -eq 0) { $ok = $true; break }
    Start-Sleep 40
  }
  if (-not $ok) { $fail += $c }
  Start-Sleep 5
}
"DOC_ALL $Stream xong $(Get-Date -Format s) | loi: $($fail -join ', ')"
