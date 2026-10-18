# Ho so du an cho pipeline port 1-1. Chon bang bien moi truong PIPE_PROJECT (mac dinh MiniLoyalty).
# Dot-source: . "$PSScriptRoot\profile.ps1"  -> tao $PJ (hashtable). Them du an moi = them 1 nhanh switch.
$Root0 = 'C:\Users\Public\Pictures\kientran_private'
$ProjName = $(if ($env:PIPE_PROJECT) { $env:PIPE_PROJECT } else { 'MiniLoyalty' })
$PJ = switch ($ProjName) {
  'MiniLoyalty' {
    $s = 'D:\idocNet\2022.7.Loyalty'
    @{ Name = 'MiniLoyalty'; Source = $s; Mode = '3tier'
       Gate = "$s\13.ClientGate\V10\idn.Loyalty.WebAPI\Controllers"; ApiDir = "$s\11.BackEnd\V10\idn.Loyalty.Biz.Web\Controllers"; BizDir = "$s\11.BackEnd\V10\idn.Loyalty.Biz"
       CommonDir = "$s\12.Dev.Common"; ErrPrefix = 'ErridnLoyalty'; SrcName = '2022.7.Loyalty'
       Rules = "$Root0\docs\PORT_RULES_MiniLoyalty.md"; TablePrefix = 'Crd|Mst|Prm|Map|Rpt|Sys|Inv|Req' }
  }
  'MiniGate' {
    $s = 'D:\idocNet\2019.9.MobileGate'; $v = "$s\Dev\V10"
    # MobileGate: 2 tang. Controller Biz.Web (action WA_<X>_<Y>, [AcceptVerbs("POST")]) goi thang _biz.WAS_* trong idN.MobileGate.Biz. Ban LIVE = idN.MobileGate.Biz.Web (khong dung *.WebDev).
    @{ Name = 'MiniGate'; Source = $s; Mode = '2tier'
       Gate = "$v\idN.MobileGate.Biz.Web\Controllers"; ApiDir = "$v\idN.MobileGate.Biz.Web\Controllers"; BizDir = "$v\idN.MobileGate.Biz"
       CommonDir = "$v\idN.MobileGate.Common"; ErrPrefix = 'ErridnMobileGate'; SrcName = '2019.9.MobileGate'
       Rules = "$Root0\docs\PORT_RULES_MiniGate.md"; TablePrefix = '' }
  }
  'MiniContract' {
    $s = 'D:\idocNet\2020.6.QContract'; $v = "$s\Dev\V10"
    # QContract: Website\Controllers (cong, ~255 action) -> ClientService -> Biz.Web\Controllers (API, WA_*) -> idn.Skycic.QContract.Biz. Ma loi: TError.ErridQContract.X = "Erridn.Skycic.QContract.X".
    @{ Name = 'MiniContract'; Source = $s; Mode = '3tier'
       Gate = "$v\idn.Skycic.QContract.Website\Controllers"; ApiDir = "$v\idn.Skycic.QContract.Biz.Web\Controllers"; BizDir = "$v\idn.Skycic.QContract.Biz"
       CommonDir = "$v\idn.Skycic.QContract.Common"; ErrPrefix = 'Erridn.Skycic.QContract'; SrcName = '2020.6.QContract'
       Rules = "$Root0\docs\PORT_RULES_MiniContract.md"; TablePrefix = 'Contract|Mst|Sys' }
  }
  'MiniWMS' {
    $s = 'D:\idocNet\2020.3.Skycic.Inventory'; $v = "$s\Dev\V10"
    # Inventory: Website\Controllers (cong) -> Biz.Web (API) -> idn.Skycic.Inventory.Biz. Ma loi ErridnInventory.X = "Erridn.Skycic.Inventory.X".
    @{ Name = 'MiniWMS'; Source = $s; Mode = '3tier'
       Gate = "$v\idn.Skycic.Inventory.Website\Controllers"; ApiDir = "$v\idn.Skycic.Inventory.Biz.Web\Controllers"; BizDir = "$v\idn.Skycic.Inventory.Biz"
       CommonDir = "$v\idn.Skycic.Inventory.Common"; ErrPrefix = 'Erridn.Skycic.Inventory'; SrcName = '2020.3.Skycic.Inventory'
       Rules = "$Root0\docs\PORT_RULES_MiniWMS.md"; TablePrefix = 'Inv|Mst|Sys|Map' }
  }
  'MiniService' {
    $s = 'D:\idocNet\2023.H.CarServices'
    # CarServices: WebAPI (cong, ~1179 action) goi ClientService; Biz = 11.BackEnd\V10\idn.CarService.Biz. Che do 2 tang (best-effort).
    @{ Name = 'MiniService'; Source = $s; Mode = '2tier'
       Gate = "$s\13.ClientGate\V10\idn.CarSv\idn.CarService.WebAPI\Controllers"; ApiDir = "$s\13.ClientGate\V10\idn.CarSv\idn.CarService.WebAPI\Controllers"; BizDir = "$s\11.BackEnd\V10\idn.CarService.Biz"
       CommonDir = "$s\12.Dev.Common\idn.CarService.Common"; ErrPrefix = 'ErridNCarService'; SrcName = '2023.H.CarServices'
       Rules = "$Root0\docs\PORT_RULES_MiniService.md"; TablePrefix = '' }
  }
  default { throw "PIPE_PROJECT '$ProjName' chua co ho so trong scripts\parity\profile.ps1" }
}
