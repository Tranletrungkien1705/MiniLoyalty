using Microsoft.AspNetCore.Mvc;
using MiniLoyalty.Services;

namespace MiniLoyalty.Controllers;

/// <summary>
/// Màn hình chạy các job nghiệp vụ (tương đương Task Scheduler bên hệ nguồn).
/// Hiện có: tặng điểm sinh nhật (BIRTHDAY), hết hạn điểm (EXPIRY).
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
}
