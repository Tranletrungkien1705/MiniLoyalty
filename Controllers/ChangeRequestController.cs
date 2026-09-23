using Microsoft.AspNetCore.Mvc;
using MiniLoyalty.Models;
using MiniLoyalty.Services;

namespace MiniLoyalty.Controllers;

/// <summary>
/// Màn hình yêu cầu thay đổi thông tin hội viên (Crd_MemberChangeInfo): hội viên/đại lý đề nghị đổi
/// thông tin cá nhân, đi qua luồng duyệt PENDING → APPROVE → FINISH (hoặc CANCEL khi từ chối).
/// </summary>
public class ChangeRequestController(ILoyaltyService svc) : Controller
{
    public async Task<IActionResult> Index(ChangeRequestStatus? status)
    {
        ViewBag.Status = status;
        return View(await svc.ChangeRequestsAsync(status));
    }

    public async Task<IActionResult> Details(int id)
    {
        var r = await svc.ChangeRequestAsync(id);
        if (r == null) return NotFound();
        return View(r);
    }

    public async Task<IActionResult> Create(int? memberId)
    {
        ViewBag.Columns = await svc.ChangeableColumnsAsync();
        ViewBag.Members = await svc.MembersAsync(null, null);
        ViewBag.MemberId = memberId;
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int memberId, ChangeRequestType requestType, string? dlCode, string? remark,
        string[]? columnCode, string[]? valueNew)
    {
        var details = new List<(string, string?)>();
        if (columnCode != null)
            for (var i = 0; i < columnCode.Length; i++)
                if (!string.IsNullOrWhiteSpace(columnCode[i]))
                    details.Add((columnCode[i], i < (valueNew?.Length ?? 0) ? valueNew![i] : null));

        var (ok, msg, id) = await svc.CreateChangeRequestAsync(memberId, requestType, dlCode, remark, details);
        TempData[ok ? "Success" : "Error"] = msg;
        return ok ? RedirectToAction(nameof(Details), new { id }) : RedirectToAction(nameof(Create), new { memberId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, string? remarkHtv)
    {
        var (ok, msg) = await svc.ApproveChangeRequestAsync(id, remarkHtv, User?.Identity?.Name);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Finish(int id, string? remarkHtv)
    {
        var (ok, msg) = await svc.FinishChangeRequestAsync(id, remarkHtv, User?.Identity?.Name);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string? remarkHtv)
    {
        var (ok, msg) = await svc.RejectChangeRequestAsync(id, remarkHtv, User?.Identity?.Name);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Details), new { id });
    }
}
