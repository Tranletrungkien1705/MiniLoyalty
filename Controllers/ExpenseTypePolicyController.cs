using Microsoft.AspNetCore.Mvc;
using MiniLoyalty.Models;
using MiniLoyalty.Services;

namespace MiniLoyalty.Controllers;

/// <summary>
/// Màn hình quản lý đối tượng tích điểm dịch vụ (Mst_PolicyExpenseType): cấu hình cho từng LOẠI CHI PHÍ dịch vụ
/// (LOCAL nội bộ / ROINSURANCE bảo hiểm / ROREPAIR khách hàng / ROWARRANTY bảo hành) xem doanh thu loại đó có
/// được tích điểm tiêu dùng (FlagPoint), có tính điểm xét hạng (FlagPointRank), có tính 1 lượt dịch vụ
/// (FlagCountService), có áp chiết khấu (FlagDiscount + DiscountRate) hay không, kèm hệ số nhân doanh thu
/// (AmountRate) và trần điểm (MaxRankReviewPoint / MaxAccumulationPoint). Đây là bảng tham số dùng khi tính
/// điểm từ lệnh sửa chữa (Crd_DealSerRO_Add, Card.Deal.cs).
/// </summary>
public class ExpenseTypePolicyController(ILoyaltyService svc) : Controller
{
    public async Task<IActionResult> Index() => View(await svc.ExpenseTypePoliciesAsync());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(ExpenseTypePolicy policy)
    {
        var (ok, msg, _) = await svc.SaveExpenseTypePolicyAsync(policy);
        if (ok) TempData["Success"] = msg; else TempData["Error"] = msg;
        return RedirectToAction(nameof(Index));
    }
}
