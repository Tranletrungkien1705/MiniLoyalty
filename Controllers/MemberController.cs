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
        ViewBag.Discounts = await svc.DiscountsAsync(id);
        ViewBag.Vouchers = await svc.VouchersAsync(id);
        ViewBag.Promotions = await svc.PromotionsAsync();
        ViewBag.PromotionUses = await svc.PromotionUsesAsync(id);
        return View(m);
    }

    public IActionResult Create() => View(new Member());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Member model)
    {
        if (string.IsNullOrWhiteSpace(model.Name)) { TempData["Error"] = "Cần tên hội viên."; return View(model); }
        var id = await svc.CreateAsync(model);
        // Thưởng điểm giới thiệu cho người giới thiệu (nếu hội viên mới có khai báo).
        var (ok, msg) = await svc.AwardIntroductionAsync(id);
        TempData["Success"] = ok ? $"Đã tạo hội viên. {msg}" : "Đã tạo hội viên.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AwardIntro(int id)
    {
        var (ok, msg) = await svc.AwardIntroductionAsync(id);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> BuyNewCar(int id, int? points, string? note)
    {
        try
        {
            var tx = await svc.AwardBuyNewCarAsync(id, points, note);
            TempData["Success"] = $"Đã tặng {tx.Points:N0} điểm mua xe mới.";
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ServiceTurn(int id, int qty, string? note)
    {
        if (qty <= 0) { TempData["Error"] = "Số lượt phải > 0."; return RedirectToAction(nameof(Details), new { id }); }
        var tx = await svc.RecordServiceTurnAsync(id, qty, note);
        TempData["Success"] = $"Đã ghi nhận {tx.QtyVisit} lượt dịch vụ.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Consumption(int id, decimal amount, string? note)
    {
        if (amount <= 0) { TempData["Error"] = "Doanh thu dịch vụ phải > 0."; return RedirectToAction(nameof(Details), new { id }); }
        var tx = await svc.RecordConsumptionAsync(id, amount, note);
        TempData["Success"] = $"Đã tích {tx.Points:N0} điểm tiêu dùng dịch vụ ({tx.AmountChTotal:N0}đ).";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Discount(int id, decimal amount, string? note)
    {
        if (amount <= 0) { TempData["Error"] = "Doanh thu dịch vụ phải > 0."; return RedirectToAction(nameof(Details), new { id }); }
        var tx = await svc.ApplyServiceDiscountAsync(id, amount, note);
        TempData["Success"] = $"Đã áp chiết khấu {tx.PolicyDiscountRate:0.##}% — giảm {tx.DiscountAmount:N0}đ.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AwardVoucher(int id, int points, string? voucherCode, string? note)
    {
        if (points <= 0) { TempData["Error"] = "Điểm voucher phải > 0."; return RedirectToAction(nameof(Details), new { id }); }
        var tx = await svc.AwardVoucherAsync(id, points, voucherCode, note);
        TempData["Success"] = $"Đã tặng {tx.Points:N0} điểm voucher.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UseVoucher(int id, int points, string? voucherCode, string? note)
    {
        var (ok, msg) = await svc.UseVoucherAsync(id, points, voucherCode, note);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UsePromotion(int id, int promotionId, string? note)
    {
        var (ok, msg) = await svc.UsePromotionAsync(id, promotionId, note);
        TempData[ok ? "Success" : "Error"] = msg;
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
