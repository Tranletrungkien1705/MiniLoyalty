using Microsoft.AspNetCore.Mvc;
using MiniLoyalty.Models;
using MiniLoyalty.Services;

namespace MiniLoyalty.Controllers;

public class MemberController(ILoyaltyService svc) : Controller
{
    public async Task<IActionResult> Index(string? q, int? rankId)
    {
        ViewBag.Ranks = await svc.RanksAsync();
        ViewBag.Q = q; ViewBag.RankId = rankId;
        return View(await svc.MembersAsync(q, rankId));
    }

    public async Task<IActionResult> Details(int id)
    {
        var m = await svc.GetAsync(id);
        if (m == null) return NotFound();
        ViewBag.Rewards = await svc.RewardsAsync();
        return View(m);
    }

    public IActionResult Create() => View(new Member());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Member model)
    {
        if (string.IsNullOrWhiteSpace(model.Name)) { TempData["Error"] = "Cần tên hội viên."; return View(model); }
        var id = await svc.CreateAsync(model);
        TempData["Success"] = "Đã tạo hội viên.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Earn(int id, decimal amount, string? note)
    {
        if (amount <= 0) { TempData["Error"] = "Số tiền phải > 0."; return RedirectToAction(nameof(Details), new { id }); }
        var tx = await svc.EarnFromPurchaseAsync(id, amount, note);
        TempData["Success"] = $"Đã tích {tx.Points} điểm.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Adjust(int id, int points, string? note)
    {
        await svc.EarnAsync(id, points, PointTxType.Adjust, string.IsNullOrWhiteSpace(note) ? "Điều chỉnh thủ công" : note, null);
        TempData["Success"] = "Đã điều chỉnh điểm.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Redeem(int id, int rewardId)
    {
        var (ok, msg) = await svc.RedeemAsync(id, rewardId);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Details), new { id });
    }
}
