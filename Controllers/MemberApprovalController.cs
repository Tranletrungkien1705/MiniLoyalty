using Microsoft.AspNetCore.Mvc;
using MiniLoyalty.Models;
using MiniLoyalty.Services;

namespace MiniLoyalty.Controllers;

/// <summary>
/// Màn hình duyệt ĐĂNG KÝ hội viên (Crd_Member.RegisStatus): vòng đời duyệt đăng ký hội viên
/// PENDING (đại lý gửi) → APPROVE1 (đại lý duyệt) → APPROVE2 (HTV duyệt) → FINISH (hoàn tất, kích hoạt hội viên).
/// Khác với màn "Đăng ký hội viên" (Req_MemberRegister — yêu cầu cấp thẻ mới).
/// </summary>
public class MemberApprovalController(ILoyaltyService svc) : Controller
{
    public async Task<IActionResult> Index(RegisStatus? status)
    {
        ViewBag.Status = status;
        return View(await svc.MemberApprovalsAsync(status));
    }

    public async Task<IActionResult> Details(int id)
    {
        var m = await svc.GetAsync(id);
        if (m == null) return NotFound();
        return View(m);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveByDealer(int id, string? remark)
    {
        var (ok, msg) = await svc.ApproveMemberByDealerAsync(id, remark, User?.Identity?.Name);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, string? remark)
    {
        var (ok, msg) = await svc.ApproveMemberAsync(id, remark, User?.Identity?.Name);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Finish(int id, string? remark)
    {
        var (ok, msg) = await svc.FinishMemberAsync(id, remark, User?.Identity?.Name);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Details), new { id });
    }
}
