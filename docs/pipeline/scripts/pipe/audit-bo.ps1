# Kiem toan cac hang muc da ghi Bo: trich lai nguon bang extract-source MOI NHAT va phan loai (fwd/sql/fwdStrong) -> audit_bo.tsv. Hang muc "co SQL ma van Bo" / "khong thay chuyen tiep" = nghi Bo nham.
# Dung: audit-bo.ps1 -Project MiniGate
param([Parameter(Mandatory)][string]$Project)
$Root = 'C:\Users\Public\Pictures\kientran_private'; $Run = "$Root\repos\$Project\.parity-run"; $env:PIPE_PROJECT = $Project
$sk = Get-Content "$Run\skipped.json" -Raw -Encoding UTF8 | ConvertFrom-Json
$done = @{}; Select-String -Path "$Root\repos\$Project\PARITY_PROGRESS.md" -Pattern 'Đã làm' | ForEach-Object { $done[($_.Line -split '\|')[2].Trim()] = 1 }
$out = "$Run\audit_bo.tsv"; "item`tfwd`tsql`tfwdStrong`tbiz2`treason" | Set-Content $out -Encoding UTF8
$tmp = "$env:TEMP\audit_ex.txt"
foreach ($p in $sk.PSObject.Properties) {
  $name = $p.Name; if ($done[$name]) { continue }
  & powershell -NoProfile -ExecutionPolicy Bypass -File "$Root\scripts\parity\extract-source.ps1" -Item $name -OutFile $tmp 2>&1 | Out-Null
  $t = if (Test-Path $tmp) { Get-Content $tmp -Raw -Encoding UTF8 } else { '' }
  $i = $t.IndexOf('BIZ '); $biz = if ($i -ge 0) { $t.Substring($i) } else { '' }
  $fwd = $biz -match 'Services?\.Instance\.|ClientService|GetUrlCurrentApiNetwork|AccountService|HttpClient|RestClient|WebRequest|PostData|CmSys_InvalidOutSite'
  $sql = $biz -match 'ExecQuery|ExecuteNonQuery|ExecSql|ExecuteScalar|SqlCommand|zzzzClause|\.SaveData\(|_cf\.db\.(ExecQuery|ExecuteNonQuery|ExecSql|SaveData)|\w+_CheckDB\('
  $strong = ($biz -match 'GetUrlCurrentApiNetwork') -and ($biz -match 'Services?\.Instance\.')
  $b2 = ($t -match '3\.2 BIZ')
  "{0}`t{1}`t{2}`t{3}`t{4}`t{5}" -f $name, [int]$fwd, [int]$sql, [int]$strong, [int]$b2, ([string]$p.Value).Substring(0, [Math]::Min(60, ([string]$p.Value).Length)) | Add-Content $out -Encoding UTF8
  Remove-Item $tmp -Force -EA SilentlyContinue
}
"AUDIT xong $(Get-Date -Format s)" | Add-Content $out -Encoding UTF8
