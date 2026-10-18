# Theo doi usage 9router (chi doc, local). Dung:
#   router-usage.ps1                      -> tong + theo provider/model + trung binh/request + ti le cache
#   router-usage.ps1 -Snapshot a.json     -> luu moc
#   router-usage.ps1 -Since a.json        -> chenh lech tu moc (do 1 tac vu cu the)
#   router-usage.ps1 -Watch 30            -> in delta moi 30 giay (Ctrl+C de dung)
param([string]$Snapshot = '', [string]$Since = '', [int]$Watch = 0, [string]$Base = 'http://127.0.0.1:20128')
$ErrorActionPreference = 'Stop'
$Root = 'C:\Users\Public\Pictures\kientran_private'
function Get-Usage {
  $pw = (Get-Content "$Root\9router\dashboard-password.txt" -Raw).Trim()
  $ws = New-Object Microsoft.PowerShell.Commands.WebRequestSession
  $null = Invoke-RestMethod "$Base/api/auth/login" -Method Post -ContentType 'application/json' -Body (@{ password = $pw } | ConvertTo-Json) -WebSession $ws
  Invoke-RestMethod "$Base/api/usage/history" -WebSession $ws
}
function Row($name, $o) {
  $req = [double]$o.requests; if ($req -eq 0) { return }
  $p = [double]$o.promptTokens; $c = [double]$o.completionTokens; $k = [double]$o.cachedTokens
  [pscustomobject]@{ Ten = $name; Req = $req; PromptTong = [long]$p; Cache = [long]$k; MoiKhongCache = [long]($p - $k)
    TB_Prompt_moi_req = [long]($p / $req); TB_Completion = [long]($c / $req); Cache_pct = $(if ($p) { [math]::Round(100 * $k / $p, 1) } else { 0 }); Cost = [math]::Round([double]$o.cost, 3) }
}
function Show($u) {
  Row 'TONG' ([pscustomobject]@{ requests = $u.totalRequests; promptTokens = $u.totalPromptTokens; completionTokens = $u.totalCompletionTokens; cachedTokens = $u.totalCachedTokens; cost = $u.totalCost }) | Format-Table -AutoSize
  "-- theo provider"; $u.byProvider.PSObject.Properties | ForEach-Object { Row $_.Name $_.Value } | Format-Table -AutoSize
  "-- theo model"; $u.byModel.PSObject.Properties | ForEach-Object { Row $_.Name $_.Value } | Format-Table -AutoSize
}
function Delta($a, $b) {
  $d = [pscustomobject]@{ requests = $b.totalRequests - $a.totalRequests; promptTokens = $b.totalPromptTokens - $a.totalPromptTokens
    completionTokens = $b.totalCompletionTokens - $a.totalCompletionTokens; cachedTokens = $b.totalCachedTokens - $a.totalCachedTokens; cost = $b.totalCost - $a.totalCost }
  Row 'DELTA' $d
}
if ($Snapshot) { Get-Usage | ConvertTo-Json -Depth 8 | Set-Content $Snapshot -Encoding UTF8; "Da luu moc $Snapshot"; return }
if ($Since) { $a = Get-Content $Since -Raw | ConvertFrom-Json; Delta $a (Get-Usage) | Format-List; return }
if ($Watch -gt 0) { $prev = Get-Usage; while ($true) { Start-Sleep $Watch; $cur = Get-Usage; Delta $prev $cur | Format-Table -HideTableHeaders; $prev = $cur } }
Show (Get-Usage)
