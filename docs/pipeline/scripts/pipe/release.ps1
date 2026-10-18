# Tra hang muc dang giu cua 1 worker ve kho (don worktree, bo claim/current/result), KHONG ghi Bo. Dung: release.ps1 -Project MiniGate -Worker gweb5
param([Parameter(Mandatory)][string]$Project, [Parameter(Mandatory)][string]$Worker)
$Root = 'C:\Users\Public\Pictures\kientran_private'; $Run = "$Root\repos\$Project\.parity-run"
$c = Get-Content "$Run\current_$Worker.json" -Raw | ConvertFrom-Json
$wt = "$Root\repos\$Project-$Worker"
if (Test-Path "$wt\.git") { & git -C $wt reset -q --hard parity/auto 2>&1 | Out-Null; & git -C $wt clean -qfd 2>&1 | Out-Null }
Remove-Item ("$Run\claims\{0:D3}.claim" -f [int]$c.no), "$Run\current_$Worker.json", "$Run\result_$Worker.json" -Force -EA SilentlyContinue
"Da tra #$($c.no) $($c.item) khoi $Worker"
