# Giu cho moi feeder slot luon song: doc <repo>\.parity-run\slots.json {direct:[],router:[],gweb:[],other:[]}; thieu tien trinh feeder cua slot nao thi khoi dong lai
# voi dung moi truong (PIPE_PROJECT, LEAD_TEMPLATE, PIPE_SHARED_CTRL; router => PARITY_ENGINE_FORCE=rtr). Dung boi auto-loop.ps1.
param([Parameter(Mandatory)][string]$Project)
$Root = 'C:\Users\Public\Pictures\kientran_private'; $Run = "$Root\repos\$Project\.parity-run"; $cfgF = "$Run\slots.json"
if (-not (Test-Path $cfgF) -or (Test-Path "$Run\STOP")) { return }
$cfg = Get-Content $cfgF -Raw | ConvertFrom-Json
$alive = @(Get-CimInstance Win32_Process | Where-Object { $_.Name -match 'powershell' -and $_.CommandLine -match 'feeder\.ps1' -and $_.CommandLine -notmatch 'Get-CimInstance' } | ForEach-Object { if ($_.CommandLine -match '-Workers\s+"?([\w,]+)') { $Matches[1] -split ',' } })
$env:PIPE_PROJECT = $Project; $env:LEAD_TEMPLATE = '1'; $env:PIPE_SHARED_CTRL = $(if ($cfg.sharedCtrl) { '1' } else { '0' })
foreach ($grp in 'direct', 'router', 'gweb', 'other') {
  foreach ($w in @($cfg.$grp)) {
    if (-not $w -or $alive -contains $w) { continue }
    $env:PARITY_ENGINE_FORCE = $(if ($grp -eq 'router') { 'rtr' } else { '' })
    Start-Process -WindowStyle Hidden powershell.exe -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File `"$Root\scripts\parity\feeder.ps1`" -Acct b -Workers $w"
    [IO.File]::AppendAllText("$Run\run.log", ("[{0:yyyy-MM-dd HH:mm:ss}] [auto] feeder {1} chet -> khoi dong lai`r`n" -f (Get-Date), $w), [Text.UTF8Encoding]::new($false))
    Start-Sleep 2
  }
}
