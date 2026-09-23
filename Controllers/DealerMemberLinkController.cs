using Microsoft.AspNetCore.Mvc;
using MiniLoyalty.Services;

namespace MiniLoyalty.Controllers;

/// <summary>
/// Màn hình liên kết Đại lý ↔ Hội viên (Map_QueryDealer_Member): ghi nhận đại lý nào đã đăng ký/tra cứu
/// hội viên nào. Khi hoàn tất đăng ký hội viên (Crd_Member_FinishX), hệ thống tự ghi 1 dòng map
/// DLCPCode (đại lý đăng ký) ↔ MemberNo kèm ngày tra cứu (QueryDate) và trạng thái hiệu lực (FlagActive).
/// </summary>
public class DealerMemberLinkController(ILoyaltyService svc) : Controller
{
    public async Task<IActionResult> Index(string? dlcpCode, int? memberId)
    {
        ViewBag.DlcpCode = dlcpCode;
        ViewBag.MemberId = memberId;
        ViewBag.Members = await svc.MembersAsync(null, null);
        return View(await svc.DealerMemberLinksAsync(dlcpCode, memberId));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string dlcpCode, int memberId, int networkId, string? remark)
    {
        var (ok, msg, _) = await svc.LinkDealerMemberAsync(dlcpCode, memberId, networkId, remark, User?.Identity?.Name);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Index));
    }
}