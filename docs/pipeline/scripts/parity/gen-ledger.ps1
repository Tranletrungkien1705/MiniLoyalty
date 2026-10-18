# Sinh PARITY_PROGRESS.md cho MiniLoyalty tu danh sach action nguon (13.ClientGate\V10\idn.Loyalty.WebAPI).
# Chi tao moi khi file chua co; neu da co thi chi bao so lieu, KHONG ghi de tien do.
param(
  [string]$Repo   = '',
  [string]$CtrlDir = ''
)
. "$PSScriptRoot\profile.ps1"
if (-not $Repo) { $Repo = "C:\Users\Public\Pictures\kientran_private\repos\$($PJ.Name)" }
if (-not $CtrlDir) { $CtrlDir = $PJ.Gate }
$ErrorActionPreference = 'Stop'
$out = Join-Path $Repo 'PARITY_PROGRESS.md'
$re  = '^\s*public\s+(async\s+)?(ActionResult|JsonResult|IHttpActionResult|HttpResponseMessage|Task<\w+>|\w+Result)\s+(\w+)\s*\('
$rows = foreach ($f in Get-ChildItem $CtrlDir -Filter *Controller.cs | Sort-Object Name) {
  if ($f.BaseName -in 'ApiController', 'BaseController', 'ApiControllerBase', 'ApiWebControllerBase') { continue }
  foreach ($m in Select-String -LiteralPath $f.FullName -Pattern $re) {
    '{0}/{1}' -f $f.BaseName, $m.Matches[0].Groups[3].Value
  }
}
$rows = @($rows | Select-Object -Unique)
if (Test-Path $out) { "Da co $out - khong ghi de. Nguon hien co $($rows.Count) action."; return }

$sb = [Text.StringBuilder]::new()
[void]$sb.AppendLine("# PARITY_PROGRESS — port 1-1 ``$($PJ.SrcName)`` → $($PJ.Name)")
[void]$sb.AppendLine()
[void]$sb.AppendLine("Nguồn mẫu số: ``$CtrlDir`` (mỗi dòng = 1 action public). Sinh bằng ``scripts\parity\gen-ledger.ps1`` ngày $(Get-Date -Format 'yyyy-MM-dd').")
[void]$sb.AppendLine()
[void]$sb.AppendLine("**Mẫu số (N)**: $($rows.Count)")
[void]$sb.AppendLine()
[void]$sb.AppendLine("**Tiến độ hiện tại**: 0/$($rows.Count) (0%)")
[void]$sb.AppendLine()
[void]$sb.AppendLine('Trạng thái: `Chưa làm` · `Đã làm` (có commit, review PASS) · `Đã có` (đã port từ trước, có bằng chứng) · `Bỏ` (scaffold/không nghiệp vụ, ghi lý do) · `Cần xem tay`.')
[void]$sb.AppendLine()
[void]$sb.AppendLine('| # | Hạng mục | Trạng thái | Commit |')
[void]$sb.AppendLine('|---|---|---|---|')
$i = 0
foreach ($r in $rows) { $i++; [void]$sb.AppendLine("| $i | $r | Chưa làm | - |") }
[IO.File]::WriteAllText($out, $sb.ToString(), [Text.UTF8Encoding]::new($false))
"Da tao $out voi $($rows.Count) hang muc."
