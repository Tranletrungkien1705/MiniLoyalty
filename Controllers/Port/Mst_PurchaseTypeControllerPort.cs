using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniLoyalty.Data;
using MiniLoyalty.Models.Port;

namespace MiniLoyalty.Controllers.Port;

// ============================================================================================
// PORT 1:1 — màn danh mục loại mua hàng (Mst_PurchaseType) của dự án nguồn 2022.7.Loyalty.
//
// Nguồn controller (tầng gate):
//   Mst_PurchaseTypeController.cs:26-94  GetAllActive -> WA_Mst_PurchaseType_Get
// Nguồn API:  MstPurchaseTypeController.cs:20-89  WA_Mst_PurchaseType_Get
// Nguồn biz:  Master.Loyalty.cs:21886 (WAS_Mst_PurchaseType_Get) -> :21984 (Mst_PurchaseType_Get)
//             -> :22129-22282 (Mst_PurchaseType_GetX — SQL thật)
//
// GIỮ ĐÚNG NGUỒN:
//   * Endpoint là POST (nguồn [AcceptVerbs("POST")]).
//   * GetAllActive: gate comment out mệnh đề FlagActive = "1" (:46-50), truyền Ft_WhereClause = "" (:63),
//     nên KHÔNG lọc gì cả, trả về tất cả bản ghi (kể cả FlagActive = '0').
//   * Sắp xếp theo PurchaseType asc (:22189, :22228).
//   * Kết quả rỗng -> Data = null (:45, :67-72, :78).
// ============================================================================================

[ApiController]
[Route("api/mst_purchasetype")]
[Produces("application/json")]
public class Mst_PurchaseTypeControllerPort(AppDbContext db) : ControllerBase
{
    // ============================================================================================
    // GetAllActive — nguồn Mst_PurchaseTypeController.cs:26-94.
    // Gate: đoạn gán FlagActive = "1" bị comment out (:46-50), gán cứng Ft_WhereClause = "" (:63).
    // Do đó KHÔNG lọc FlagActive (hàng FlagActive = '0' như EXCHANGE vẫn trả về).
    // Ft_RecordStart = "0", Ft_RecordCount = "123456000" (:61-62).
    // Khi không có dữ liệu, trả Data = null (:45, :67-72, :78).
    // ============================================================================================
    [HttpPost("getallactive")]
    public async Task<IActionResult> GetAllActive()
    {
        // GetAllActive gọi GetXAsync(0, 123456000), KHÔNG lọc gì (Mst_PurchaseTypeController.cs:61-64 Ft_WhereClause = "")
        var (_, rows) = await GetXAsync(0, 123456000);

        // Nguồn: listMst_PurchaseType = null khi rỗng (:45, :67-72), Data = listMst_PurchaseType (:78)
        return Ok(new { Success = true, Data = rows.Count > 0 ? rows : null });
    }

    /// <summary>
    /// Hàm đọc dữ liệu tương ứng Mst_PurchaseType_GetX (Master.Loyalty.cs:22129-22282) để các action sau tái sử dụng.
    /// Sắp xếp theo PurchaseType asc (:22189, :22228).
    /// Trả về (itemCount, rows): itemCount là tổng số dòng sau lọc; rows là danh sách phân trang kèm MyIdxSeq.
    /// </summary>
    private async Task<(int itemCount, List<object> rows)> GetXAsync(long recordStart, long recordCount)
    {
        var q = db.Set<Mst_PurchaseType>().AsNoTracking().AsQueryable();

        // Mst_PurchaseType_GetX (:22189) sắp xếp theo mpt.PurchaseType asc
        var ordered = q.OrderBy(x => x.PurchaseType);
        var itemCount = await ordered.CountAsync();

        // Cắt trang theo recordStart và recordCount (nFilterRecordStart..nFilterRecordEnd tại :22202-22203).
        var page = (recordCount <= 0 || recordStart < 0)
            ? new List<Mst_PurchaseType>()
            : await ordered.Skip((int)Math.Min(recordStart, int.MaxValue)).Take((int)Math.Min(recordCount, int.MaxValue)).ToListAsync();

        // Kết quả gồm MyIdxSeq + 4 cột của Mst_PurchaseType (:22223-22224), sắp theo MyIdxSeq asc (:22228)
        var rows = page
            .Select((x, i) => (object)new
            {
                MyIdxSeq = recordStart + i,
                x.PurchaseType,
                x.PurchaseTypeName,
                x.Remark,
                x.FlagActive,
            })
            .ToList();

        return (itemCount, rows);
    }
}
