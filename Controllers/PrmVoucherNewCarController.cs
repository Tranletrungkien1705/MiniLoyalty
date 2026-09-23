using Microsoft.AspNetCore.Mvc;
using MiniLoyalty.Models;
using MiniLoyalty.Services;

namespace MiniLoyalty.Controllers;

/// <summary>
/// Màn hình chương trình tặng điểm voucher xe mới (Prm_VoucherNewCar): HTV cấu hình chương trình tặng
/// ĐIỂM VOUCHER cho khách mua xe mới theo dòng xe (ModelCode), đi qua luồng duyệt PENDING → APPROVE → FINISH
/// (hoặc CANCEL khi huỷ). Khi FINISH, chương trình có hiệu lực trong khoảng [EffDateStart, EffDateEnd]
/// và được tra cứu qua Prm_VoucherNewCar_CalcPrmX. Điểm voucher KHÔNG dùng để xét hạng.
/// </summary>
public class PrmVoucherNewCarController(ILoyaltyService svc) : Controller
{
    public async Task<IActionResult> Index(PrmVoucherNewCarStatus? status)
    {
        ViewBag.Status = status;
        return View(await svc.PrmVoucherNewCarsAsync(status));
    }

    public async Task<IActionResult> Details(int id)
    {
        var p = await svc.PrmVoucherNewCarAsync(id);
        if (p == null) return NotFound();
        return View(p);
    }

    public IActionResult Create() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PrmVoucherNewCar prm, string? specModels, string? specPoints, string? specUseLimits)
    {
        // Dòng xe + điểm voucher + điểm sử dụng tối đa nhập dạng các chuỗi song song (mỗi dòng 1 giá trị),
        // chỉ dùng khi KHÔNG áp dụng tất cả dòng xe.
        if (!prm.FlagAllModel && !string.IsNullOrWhiteSpace(specModels))
        {
            var models = specModels.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var points = (specPoints ?? "").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var useLimits = (specUseLimits ?? "").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            for (var i = 0; i < models.Length; i++)
            {
                var pt = i < points.Length && int.TryParse(points[i], out var v) ? v : 0;
                var ul = i < useLimits.Length && int.TryParse(useLimits[i], out var u) ? u : 0;
                prm.Specs.Add(new PrmVoucherNewCarSpec { Idx = i + 1, ModelCode = models[i] });
                prm.Details.Add(new PrmVoucherNewCarDtl { Idx = i + 1, PointVoucher = pt, PointUseLimit = ul });
            }
        }
        var (ok, msg, id) = await svc.CreatePrmVoucherNewCarAsync(prm);
        TempData[ok ? "Success" : "Error"] = msg;
        return ok ? RedirectToAction(nameof(Details), new { id }) : RedirectToAction(nameof(Create));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, string? remark)
    {
        var (ok, msg) = await svc.ApprovePrmVoucherNewCarAsync(id, remark, User?.Identity?.Name);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Finish(int id, string? remark)
    {
        var (ok, msg) = await svc.FinishPrmVoucherNewCarAsync(id, remark, User?.Identity?.Name);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string? remark)
    {
        var (ok, msg) = await svc.CancelPrmVoucherNewCarAsync(id, remark, User?.Identity?.Name);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Details), new { id });
    }
}
