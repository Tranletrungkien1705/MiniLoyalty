using Microsoft.AspNetCore.Mvc;
using MiniLoyalty.Services;

namespace MiniLoyalty.Controllers;

/// <summary>
/// Màn hình tra cứu hội viên cho DMS (Crd_MemberController.GetDetailForDMS): DMS (hệ thống quản lý đại lý)
/// gọi loyalty để tìm hội viên theo biển số (CarNo) hoặc số khung (VIN) trước khi xe vào xưởng dịch vụ.
/// Kết quả kèm cờ FlagIsDLQuery cho biết đại lý (DLCPCode) đã từng đăng ký/tra cứu hội viên này chưa
/// (dựa trên Map_QueryDealer_Member).
/// </summary>
public class DmsLookupController(ILoyaltyService svc) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? carNo, string? vin, string? dlcpCode)
    {
        ViewBag.CarNo = carNo;
        ViewBag.Vin = vin;
        ViewBag.DlcpCode = dlcpCode;
        if (string.IsNullOrWhiteSpace(carNo) && string.IsNullOrWhiteSpace(vin))
            return View();

        var r = await svc.GetDetailForDmsAsync(carNo, vin, dlcpCode);
        ViewBag.Result = r;
        return View();
    }
}
