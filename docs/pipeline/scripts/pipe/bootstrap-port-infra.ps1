# Cai ha tang port 1-1 (PortResult/PortQuery/PortSeq/PortExcel/PortFilters/PortInfra + PortSchema tu tao bang) vao 1 repo Mini* moi.
# Nguon mau: MiniLoyalty (parity/auto). Chay 1 lan cho moi du an. Khong dung vao master: tao nhanh parity/auto.
# Dung: bootstrap-port-infra.ps1 -Project MiniGate
param([Parameter(Mandatory)][string]$Project, [string]$From = 'MiniLoyalty', [string]$ErrPrefix = 'ErridnLoyalty')
$ErrorActionPreference = 'Stop'
$Root = 'C:\Users\Public\Pictures\kientran_private\repos'; $Src = "$Root\$From"; $Dst = "$Root\$Project"; $Utf8 = [Text.UTF8Encoding]::new($false)
if (-not (Test-Path "$Dst\.git")) { throw "Khong thay repo $Dst" }
$cur = (& git -C $Dst branch --show-current).Trim()
if ($cur -ne 'parity/auto') {
  if (& git -C $Dst status --porcelain) { throw "$Project dang co thay doi chua commit - xu ly truoc" }
  $has = & git -C $Dst branch --list parity/auto
  if ($has) { & git -C $Dst checkout parity/auto | Out-Null } else { & git -C $Dst checkout -b parity/auto | Out-Null }
}
New-Item -ItemType Directory -Force "$Dst\Data", "$Dst\Controllers\Port", "$Dst\Models\Port" | Out-Null
foreach ($f in 'PortResult', 'PortQuery', 'PortSeq', 'PortExcel', 'PortFilters', 'PortInfra', 'SqliteUnicodeLower') {
  $t = [IO.File]::ReadAllText("$Src\Data\$f.cs", [Text.Encoding]::UTF8) -replace $From, $Project
  [IO.File]::WriteAllText("$Dst\Data\$f.cs", $t, $Utf8)
}
# PortSchema: cat EnsureNewTablesAsync + AddMissingColumnsAsync tu Seeder.cs cua MiniLoyalty
$L = [IO.File]::ReadAllLines("$Src\Data\Seeder.cs", [Text.Encoding]::UTF8)
$s = ($L | Select-String 'private static async Task EnsureNewTablesAsync').LineNumber[0]
$e = ($L | Select-String 'private static async Task SeedCrdDemoAsync').LineNumber[0]
$body = ($L[($s - 1)..($e - 6)] -join "`r`n")   # tru doan doc comment cua SeedCrdDemoAsync
$body = $body -replace 'private static async Task EnsureNewTablesAsync', 'public static async Task EnsureAsync'
$body = $body -replace '(?s)var legacy = new\[\][^;]+;\s*', 'var legacy = Array.Empty<Type>();'
$body = $body -replace 'await SeedCrdDemoAsync\(db\);\s*', ''
$body = $body -replace '\(e\.ClrType\.Namespace \?\? ""\)\.StartsWith\("MiniLoyalty\.Models\.Port"\)', "(e.ClrType.Namespace ?? `"`").StartsWith(`"$Project.Models.Port`")"
$body = $body -replace 'typeof\(Seeder\)\.Assembly', 'typeof(PortSchema).Assembly'
$code = "using Microsoft.EntityFrameworkCore;`r`n`r`nnamespace $Project.Data;`r`n`r`n/// <summary>Tu tao bang/cot cho moi entity trong $Project.Models.Port + chay IPortSeeder (xem PortInfra.cs).</summary>`r`npublic static class PortSchema`r`n{`r`n$body`r`n}`r`n"
[IO.File]::WriteAllText("$Dst\Data\PortSchema.cs", $code, $Utf8)
# csproj: ClosedXML
$cp = Get-ChildItem "$Dst\*.csproj" | Select-Object -First 1
$x = [IO.File]::ReadAllText($cp.FullName)
if ($x -notmatch 'ClosedXML') { $tag = '<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite"'; $x = $x.Replace($tag, '<PackageReference Include="ClosedXML" Version="0.104.2" />' + "`r`n    " + $tag); [IO.File]::WriteAllText($cp.FullName, $x, $Utf8) }
# --- hook tu dong (neu da co thi bo qua) ---
$ctx = "$Dst\Data\AppDbContext.cs"
if ((Test-Path $ctx) -and ([IO.File]::ReadAllText($ctx) -notmatch 'ApplyConfigurationsFromAssembly')) {
  $c = [IO.File]::ReadAllText($ctx, [Text.Encoding]::UTF8); $i = $c.IndexOf('protected override void OnModelCreating')
  if ($i -ge 0) { $o = $c.IndexOf('{', $i); $d = 0; for ($k = $o; $k -lt $c.Length; $k++) { if ($c[$k] -eq '{') { $d++ } elseif ($c[$k] -eq '}') { $d--; if ($d -eq 0) { break } } }
    $c = $c.Insert($k, "`r`n        b.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);   // entity port 1-1 (Data/PortInfra.cs)`r`n    "); [IO.File]::WriteAllText($ctx, $c, $Utf8) }
}
$sd = "$Dst\Data\Seeder.cs"
if ((Test-Path $sd) -and ([IO.File]::ReadAllText($sd) -notmatch 'PortSchema')) {
  $c = [IO.File]::ReadAllText($sd, [Text.Encoding]::UTF8)
  if ($c -match 'await MigratePostgresAsync\(db\);') { $c = $c -replace '(await MigratePostgresAsync\(db\);)', "`$1`r`n        await PortSchema.EnsureAsync(db);   // bang/seed port 1-1 (Data/PortSchema.cs)" }
  elseif ($c -match 'await db\.Database\.EnsureCreatedAsync\(\);') { $c = $c -replace '(await db\.Database\.EnsureCreatedAsync\(\);)', "`$1`r`n        await PortSchema.EnsureAsync(db);   // bang/seed port 1-1 (Data/PortSchema.cs)" }
  [IO.File]::WriteAllText($sd, $c, $Utf8)
}
$pg = "$Dst\Program.cs"
if ((Test-Path $pg) -and ([IO.File]::ReadAllText($pg) -notmatch 'PortJsonResultFilter')) {
  $c = [IO.File]::ReadAllText($pg, [Text.Encoding]::UTF8)
  $c = $c.Replace('builder.Services.AddControllersWithViews();', "builder.Services.AddControllersWithViews(o =>`r`n{`r`n    o.Filters.Add<PortJsonResultFilter>();   // controller port 1-1: JSON PascalCase + ngoai le -> 400 PortResult.Fail`r`n    o.Filters.Add<PortExceptionFilter>();`r`n});")
  if ($c -notmatch "using $Project\.Data;") { $c = "using $Project.Data;`r`n" + $c }
  [IO.File]::WriteAllText($pg, $c, $Utf8)
}
# ma loi: tien to rieng cua du an nguon
$pr = "$Dst\Data\PortResult.cs"; $c = [IO.File]::ReadAllText($pr, [Text.Encoding]::UTF8)
$c = $c.Replace('"ErridnLoyalty." + errorCode', "`"$ErrPrefix.`" + errorCode"); [IO.File]::WriteAllText($pr, $c, $Utf8)
"Da cai ha tang vao $Project. CON LAI (tay, 1 lan): (1) AppDbContext.OnModelCreating them b.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly); (2) Seeder.SeedAsync cuoi them await PortSchema.EnsureAsync(db); (3) Program.cs AddControllersWithViews(o => { o.Filters.Add<PortJsonResultFilter>(); o.Filters.Add<PortExceptionFilter>(); }); (4) dotnet build."
