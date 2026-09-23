using Microsoft.AspNetCore.Mvc;
using MiniLoyalty.Models;
using MiniLoyalty.Services;

namespace MiniLoyalty.Controllers;

/// <summary>
/// Màn hình yêu cầu đặc cách thẻ (Crd_Card_RequestExceptionX / Crd_Card_ApprExceptionX): hội viên/đại lý
/// đề nghị cấp một kỳ thẻ đặc cách (FlagExceptionally=1) kèm danh sách đại lý chỉ định được dùng thẻ.
/// Luồng duyệt: PENDING (tạo) → APPROVE (duyệt: huỷ kỳ thẻ cũ, kích hoạt kỳ thẻ đặc cách), hoặc CANCEL khi từ chối.
/// </summary>
public class CardExceptionController(ILoyaltyService svc) : Controller
{
    public async Task<IActionResult> Index(CardExceptionStatus? status)
    {
        ViewBag.Status = status;
        return View(await svc.CardExceptionsAsync(status));
    }

    public async Task<IActionResult> Details(int id)
    {
        var ex = await svc.CardExceptionAsync(id);
        if (ex == null) return NotFound();
        return View(ex);
    }

    public async Task<IActionResult> Create(int? memberId)
    {
        ViewBag.Members = await svc.MembersAsync(null, null);
        ViewBag.MemberId = memberId;
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int memberId, string? dlCodeExceptionally, string? remark, string? dealerCodes)
    {
        var codes = (dealerCodes ?? "")
            .Split(['\n', '\r', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
        var (ok, msg, id) = await svc.RequestCardExceptionAsync(memberId, dlCodeExceptionally, remark, codes);
        TempData[ok ? "Success" : "Error"] = msg;
        return ok ? RedirectToAction(nameof(Details), new { id }) : RedirectToAction(nameof(Create), new { memberId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, string? remarkHtv)
    {
        var (ok, msg) = await svc.ApproveCardExceptionAsync(id, remarkHtv, User?.Identity?.Name);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string? remarkHtv)
    {
        var (ok, msg) = await svc.RejectCardExceptionAsync(id, remarkHtv, User?.Identity?.Name);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Details), new { id });
    }
}
