using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniLoyalty.Data;
using MiniLoyalty.Models.Port;

namespace MiniLoyalty.Controllers.Port;

// ============================================================================================
// PORT 1:1 — màn danh mục loại thẻ của dự án nguồn 2022.7.Loyalty.
//
// Nguồn controller (tầng gate):
//   Mst_CardTypeController.cs:25-105   Search  ->  WA_Mst_CardType_Get
//   Mst_CardTypeController.cs:283-308  strWhereClause_Mst_CardType (dựng mệnh đề lọc)
// Nguồn API:  MstCardTypeController.cs:239-308  WA_Mst_CardType_Get
// Nguồn biz:  Master.Loyalty.cs:5329 (WAS_Mst_CardType_Get) -> :5430 (Mst_CardType_Get)
//             -> :5583-5737 (Mst_CardType_GetX — SQL thật)
//
// GIỮ ĐÚNG NGUỒN:
//   * Endpoint là POST (nguồn [AcceptVerbs("POST")]).
//   * Lọc KeyWord: like '%x%' trên CardTypeName (:290).
//   * Lọc FlagActive: danh sách `in ('a','b')` (:299-304).
//   * Chia trang theo CardType asc (:5643) rồi sắp trang theo Value asc (:5683).
// ============================================================================================

/// <summary>Port 1:1 <c>Mst_CardTypeSearch</c> — tham số lọc + phân trang của màn.</summary>
public class MstCardTypeSearchReq
{
    public string? KeyWord { get; set; }             // Từ khoá tìm theo tên loại thẻ — Mst_CardTypeController.cs:54
    public string? FlagActive { get; set; }          // Cờ hoạt động, danh sách phân cách dấu phẩy — :55
    public int Ft_PageIndex { get; set; } = 0;       // Trang (0-based) — :46
    public int Ft_PageSize { get; set; } = 10;       // Số dòng/trang — :47
}

[ApiController]
[Route("api/mst_cardtype")]
[Produces("application/json")]
public class Mst_CardTypeControllerPort(AppDbContext db) : ControllerBase
{
    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody] MstCardTypeSearchReq? req)
    {
        var r = req ?? new MstCardTypeSearchReq();
        var pageIndex = Math.Max(0, r.Ft_PageIndex);
        var pageSize = r.Ft_PageSize;

        var q = db.Set<Mst_CardType>().AsNoTracking().AsQueryable();

        // KeyWord -> like '%x%' trên CardTypeName (Mst_CardTypeController.cs:290).
        // Nguồn chạy SQL Server collation KHÔNG phân biệt hoa/thường ⇒ hạ chữ cả 2 phía rồi LIKE (bẫy #18/#31).
        if (!string.IsNullOrWhiteSpace(r.KeyWord))
        {
            var kw = PortQuery.Contains(r.KeyWord);
            q = q.Where(x => EF.Functions.Like((x.CardTypeName ?? "").ToLower(), kw, PortQuery.Esc));
        }

        // FlagActive -> `in ('a','b')` (Mst_CardTypeController.cs:299-304): tách dấu phẩy, bỏ khoảng trắng, hạ chữ.
        if (!string.IsNullOrWhiteSpace(r.FlagActive))
        {
            var ds = r.FlagActive.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => s.ToLowerInvariant()).ToList();
            q = q.Where(x => ds.Contains((x.FlagActive ?? "").ToLower()));
        }

        // Chia trang theo CardType asc (Master.Loyalty.cs:5643 — `order by mcb.CardType asc`).
        var ordered = q.OrderBy(x => x.CardType);
        var itemCount = await ordered.CountAsync();

        // MyIdxSeq = số thứ tự 0-based trong thứ tự CardType (identity(bigint,0,1) ở :5637).
        // Lấy dòng [PageIndex*PageSize, +PageSize) theo thứ tự CardType.
        // (EF không dịch được Select kèm chỉ số dòng ⇒ cắt trang bằng Skip/Take rồi tự gán MyIdxSeq trong bộ nhớ.)
        var start = (long)pageIndex * pageSize;
        var page = pageSize <= 0
            ? new List<Mst_CardType>()
            : await ordered.Skip((int)start).Take(pageSize).ToListAsync();

        // Rồi sắp trang đó theo Value asc (Master.Loyalty.cs:5683 — `order by mcb.Value asc`) trước khi trả về.
        var dataList = page
            .Select((x, i) => new { x, idx = start + i })
            .OrderBy(t => t.x.Value)
            .Select(t => (object)new
            {
                MyIdxSeq = t.idx,
                t.x.CardType,
                t.x.NetworkID,
                t.x.CardTypeName,
                t.x.Value,
                t.x.FlagActive,
                t.x.LogLUDTimeUTC,
                t.x.LogLUBy,
            })
            .ToList();

        // PageSize=0 -> DataList rỗng (nguồn: nFilterRecordEnd = start-1, :5594 + gate).
        // PageCount = 0 khi DataList rỗng, ngược lại ceil(ItemCount / ps) với ps = PageSize (PageSize=0 thì 10) — gate :80-88.
        var ps = pageSize == 0 ? 10 : pageSize;
        var pageCount = dataList.Count == 0 ? 0 : (itemCount % ps == 0 ? itemCount / ps : itemCount / ps + 1);

        return Ok(new { Success = true, Data = new PageInfo<object>(dataList, itemCount, pageCount, pageIndex, pageSize) });
    }
}
