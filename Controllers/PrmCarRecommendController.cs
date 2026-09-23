using Microsoft.AspNetCore.Mvc;
using MiniLoyalty.Models;
using MiniLoyalty.Services;

namespace MiniLoyalty.Controllers;

/// <summary>
/// Màn hình chương trình tặng điểm giới thiệu mua xe (Prm_CarRecommend): HTV/đại lý cấu hình chương trình
/// tặng điểm cho hội viên giới thiệu khách mua xe mới theo đại lý (DLCPCode) và dòng xe (ModelCode),
/// đi qua luồng duyệt PENDING → APPROVE → FINISH (hoặc CANCEL khi huỷ). Khi FINISH, chương trình có hiệu lực
/// trong khoảng [EffDateStart, EffDateEnd] và được tra cứu qua Prm_CarRecommend_CalcPrmX.
/// </summary>
public class PrmCarRecommendController(ILoyaltyService svc) : Controller
{
    public async Task<IActionResult> Index(PrmCarRecommendStatus? status, string? dlcpCode)
    {
        ViewBag.Status = status;
        ViewBag.DlcpCode = dlcpCode;
        return View(await svc.PrmCarRecommendsAsync(status, dlcpCode));
    }

    public async Task<IActionResult> Details(int id)
    {
        var p = await svc.PrmCarRecommendAsync(id);
        if (p == null) return NotFound();
        return View(p);
    }

    public IActionResult Create() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PrmCarRecommend prm, string? specModels, string? specPoints)
    {
        // Dòng xe + điểm nhập dạng 2 chuỗi song song (mỗi dòng 1 dòng xe), chỉ dùng khi KHÔNG áp dụng tất cả dòng xe.
        if (!prm.FlagAllModel && !string.IsNullOrWhiteSpace(specModels))
        {
            var models = specModels.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var points = (specPoints ?? "").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            for (var i = 0; i < models.Length; i++)
            {
                var pt = i < points.Length && int.TryParse(points[i], out var v) ? v : 0;
                prm.Specs.Add(new PrmCarRecommendSpec { Idx = i + 1, ModelCode = models[i] });
                prm.Details.Add(new PrmCarRecommendDtl { Idx = i + 1, PointVal = pt });
            }
        }
        var (ok, msg, id) = await svc.CreatePrmCarRecommendAsync(prm);
        TempData[ok ? "Success" : "Error"] = msg;
        return ok ? RedirectToAction(nameof(Details), new { id }) : RedirectToAction(nameof(Create));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, string? remark)
    {
        var (ok, msg) = await svc.ApprovePrmCarRecommendAsync(id, remark, User?.Identity?.Name);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Finish(int id, string? remark)
    {
        var (ok, msg) = await svc.FinishPrmCarRecommendAsync(id, remark, User?.Identity?.Name);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string? remark)
    {
        var (ok, msg) = await svc.CancelPrmCarRecommendAsync(id, remark, User?.Identity?.Name);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Details), new { id });
    }
}
