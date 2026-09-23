using Microsoft.AspNetCore.Mvc;
using MiniLoyalty.Models;
using MiniLoyalty.Services;

namespace MiniLoyalty.Controllers;

/// <summary>
/// Màn hình yêu cầu đăng ký hội viên (Req_MemberRegister): đại lý gửi thông tin khách hàng + xe để
/// đề nghị cấp thẻ hội viên mới, đi qua luồng duyệt PENDING → APPROVE → FINISH
/// (hoặc CANCEL khi đại lý huỷ / REJECT khi HTV từ chối).
/// </summary>
public class MemberRegisterController(ILoyaltyService svc) : Controller
{
    public async Task<IActionResult> Index(MemberRegisterStatus? status)
    {
        ViewBag.Status = status;
        return View(await svc.MemberRegistersAsync(status));
    }

    public async Task<IActionResult> Details(int id)
    {
        var r = await svc.MemberRegisterAsync(id);
        if (r == null) return NotFound();
        return View(r);
    }

    public IActionResult Create() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MemberRegister req)
    {
        var (ok, msg, id) = await svc.CreateMemberRegisterAsync(req);
        TempData[ok ? "Success" : "Error"] = msg;
        return ok ? RedirectToAction(nameof(Details), new { id }) : RedirectToAction(nameof(Create));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, string? remark)
    {
        var (ok, msg) = await svc.ApproveMemberRegisterAsync(id, remark, User?.Identity?.Name);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Finish(int id)
    {
        var (ok, msg) = await svc.FinishMemberRegisterAsync(id, User?.Identity?.Name);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string? remark)
    {
        var (ok, msg) = await svc.CancelMemberRegisterAsync(id, remark, User?.Identity?.Name);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string? remark)
    {
        var (ok, msg) = await svc.RejectMemberRegisterAsync(id, remark, User?.Identity?.Name);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Details), new { id });
    }
}