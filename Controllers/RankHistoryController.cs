using Microsoft.AspNetCore.Mvc;
using MiniLoyalty.Services;

namespace MiniLoyalty.Controllers;

/// <summary>
/// Màn hình lịch sử xét hạng (Crd_CardRank, DealPointType=LOYALTY): mỗi lần job xét hạng cuối kỳ xử lý
/// một hội viên, hệ thống ghi 1 bản ghi lưu hành động (UP/KEEP/DOWN) kèm ảnh chụp hạng trước/sau.
/// Đây là audit trail của quá trình xét hạng — tra cứu vì sao hội viên lên/xuống hạng kỳ này.
/// </summary>
public class RankHistoryController(ILoyaltyService svc) : Controller
{
    public async Task<IActionResult> Index(int? memberId)
    {
        ViewBag.MemberId = memberId;
        ViewBag.Members = await svc.MembersAsync(null, null);
        return View(await svc.RankHistoryAsync(memberId));
    }
}