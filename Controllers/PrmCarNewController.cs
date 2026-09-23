using Microsoft.AspNetCore.Mvc;
using MiniLoyalty.Models;
using MiniLoyalty.Services;

namespace MiniLoyalty.Controllers;

/// <summary>
/// Màn hình chương trình tặng điểm xe mới (Prm_CarNew): HTV/đại lý cấu hình chương trình tặng điểm cho
/// hội viên mua xe mới theo đại lý (DLCPCode) và dòng xe (ModelCode), đi qua luồng duyệt
/// PENDING → APPROVE → FINISH (hoặc CANCEL khi huỷ). Khi FINISH, chương trình có hiệu lực trong
/// khoảng [EffDateStart, EffDateEnd] và được tra cứu qua Prm_CarNew_CalcPrmX.
/// </summary>
public class PrmCarNewController(ILoyaltyService svc) : Controller
{
    public async Task<IActionResult> Index(PrmCarNewStatus? status, string? dlcpCode)
    {
        ViewBag.Status = status;
        ViewBag.DlcpCode = dlcpCode;
        return View(await svc.PrmCarNewsAsync(status, dlcpCode));
    }

    public async Task<IActionResult> Details(int id)
    {
        var p = await svc.PrmCarNewAsync(id);
        if (p == null) return NotFound();
        return View(p);
    }

    public IActionResult Create() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PrmCarNew prm, string? specModels, string? specPoints)
    {
        // Dòng xe + điểm nhập dạng 2 chuỗi song song (mỗi dòng 1 dòng xe), chỉ dùng khi KHÔNG áp dụng tất cả dòng xe.
        if (!prm.FlagAllModel && !string.IsNullOrWhiteSpace(specModels))
        {
            var models = specModels.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var points = (specPoints ?? "").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            for (var i = 0; i < models.Length; i++)
            {
                var pt = i < points.Length && int.TryParse(points[i], out var v) ? v : 0;
                prm.Specs.Add(new PrmCarNewSpec { Idx = i + 1, ModelCode = models[i], PointVal = pt });
            }
        }
        var (ok, msg, id) = await svc.CreatePrmCarNewAsync(prm);
        TempData[ok ? "Success" : "Error"] = msg;
        return ok ? RedirectToAction(nameof(Details), new { id }) : RedirectToAction(nameof(Create));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, string? remark)
    {
        var (ok, msg) = await svc.ApprovePrmCarNewAsync(id, remark, User?.Identity?.Name);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Finish(int id, string? remark)
    {
        var (ok, msg) = await svc.FinishPrmCarNewAsync(id, remark, User?.Identity?.Name);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string? remark)
    {
        var (ok, msg) = await svc.CancelPrmCarNewAsync(id, remark, User?.Identity?.Name);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Details), new { id });
    }
}