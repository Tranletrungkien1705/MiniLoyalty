using Microsoft.AspNetCore.Mvc;
using MiniLoyalty.Services;

namespace MiniLoyalty.Controllers;

/// <summary>
/// Màn hình tính toán thẻ / lộ trình lên hạng (Crd_Card_Calc, Card.cs): chọn 1 hội viên để xem hạng hiện tại
/// và phần còn thiếu (doanh thu dịch vụ + số lượt dịch vụ) để lên hạng kế tiếp. Đây là công cụ tư vấn cho
/// đại lý/hội viên biết cần chi thêm bao nhiêu để nâng hạng.
/// </summary>
public class CardCalcController(ILoyaltyService svc) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int? memberId)
    {
        ViewBag.Members = await svc.MembersAsync(null, null);
        ViewBag.MemberId = memberId;
        if (memberId is { } id)
            ViewBag.Result = await svc.CardCalcAsync(id);
        return View();
    }
}
