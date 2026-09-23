using Microsoft.AspNetCore.Mvc;
using MiniLoyalty.Services;

namespace MiniLoyalty.Controllers;

/// <summary>
/// Màn hình chạy các job nghiệp vụ (tương đương Task Scheduler bên hệ nguồn).
/// Hiện có: tặng điểm sinh nhật (BIRTHDAY), hết hạn điểm (EXPIRY), xét hạng cuối kỳ (UP/KEEP/DOWN).
/// </summary>
public class JobController(ILoyaltyService svc) : Controller
{
    public IActionResult Index() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RunBirthday()
    {
        var r = await svc.RunBirthdayJobAsync();
        TempData["Success"] = r.Awarded == 0
            ? "Không có hội viên nào có sinh nhật hôm nay (hoặc đã nhận trong năm)."
            : $"Đã tặng điểm sinh nhật cho {r.Awarded} hội viên, tổng {r.Points:N0} điểm.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RunExpiry()
    {
        var r = await svc.RunExpiryJobAsync();
        TempData["Success"] = r.Members == 0
            ? "Không có điểm nào hết hạn hôm nay."
            : $"Đã trừ {r.Points:N0} điểm hết hạn của {r.Members} hội viên.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RunBirthdayVoucher()
    {
        var r = await svc.RunBirthdayVoucherJobAsync();
        TempData["Success"] = r.Issued == 0
            ? "Không có hội viên nào đủ điều kiện nhận voucher sinh nhật hôm nay (hoặc đã nhận trong năm)."
            : $"Đã phát voucher sinh nhật cho {r.Issued} hội viên, tổng {r.Points:N0} điểm voucher.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RunRankKeepDown()
    {
        var r = await svc.RunRankKeepDownJobAsync();
        TempData["Success"] = (r.Up + r.Kept + r.Down) == 0
            ? "Không có hội viên nào tới kỳ xét hạng."
            : $"Xét hạng cuối kỳ: {r.Up} nâng hạng, {r.Kept} duy trì, {r.Down} xuống hạng.";
        return RedirectToAction(nameof(Index));
    }
}
