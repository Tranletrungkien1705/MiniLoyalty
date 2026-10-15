using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniLoyalty.Data;
using MiniLoyalty.Models.Port;

namespace MiniLoyalty.Controllers.Port;

// ============================================================================================
// PORT 1:1 — màn danh mục hãng xe (Mst_CarBrand) của dự án nguồn 2022.7.Loyalty (#77).
//
// Nguồn controller (tầng gate):
//   Mst_CarBrandController.cs:25-105   Search              -> WA_Mst_CarBrand_Get
//   Mst_CarBrandController.cs:283-308  strWhereClause_Mst_CarBrand (mệnh đề lọc Search)
// Nguồn API:
//   MstCarBrandController.cs:238-307   WA_Mst_CarBrand_Get
// Nguồn biz:
//   Master.Loyalty.cs:93-193   WAS_Mst_CarBrand_Get
//   Master.Loyalty.cs:194-346  Mst_CarBrand_Get
//   Master.Loyalty.cs:347-501  Mst_CarBrand_GetX (SQL :400-447)
//
// GIỮ ĐÚNG NGUỒN:
//   * Endpoint là POST (nguồn [AcceptVerbs("POST")]).
//   * Lọc KeyWord: like '%x%' trên BrandName (:290).
//   * Lọc FlagActive: danh sách in ('a','b') (:297-305); dòng `=` (:293-296) BỊ COMMENT nên KHÔNG chạy.
//   * Chia trang theo mcb.BrandCode asc (:407), select cuối sắp theo t.MyIdxSeq asc (:441-447).
//   * MyIdxSeq 0-based identity (:401).
//   * PageSize = 0 -> DataList rỗng, PageCount = 0 (gate :83-94).
//
// Bỏ qua có chủ đích (loại Đ):
//   - CheckGatewayAuthentication: gate :34; API MstCarBrandController.cs:259-264
//   - CheckAuthorize: gate :35 (hệ thống đích chưa có hệ phân quyền)
//   - Nguồn gọi bằng tài khoản hệ thống viết cứng (tài khoản + mật khẩu viết cứng — KHÔNG chép giá trị),
//     đích chưa có phân quyền; KHÔNG chép thông tin đăng nhập vào mã
//   - Ghi RequestLog/ReturnLog: Master.Loyalty.cs:238-245, 336-343
//   - Sys_User_CheckAuthorize: Master.Loyalty.cs:251-264
// ============================================================================================

/// <summary>Port 1:1 <c>Mst_CarBrandSearch</c> — tham số lọc + phân trang của màn (Mst_CarBrandController.cs:25, 52-56).</summary>
public class MstCarBrandSearchReq
{
    public string? KeyWord { get; set; }             // Từ khoá tìm theo tên hãng xe (BrandName) — :54, :290
    public string? FlagActive { get; set; }          // Cờ hoạt động, danh sách phân cách dấu phẩy — :55, :297-305
    public int Ft_PageIndex { get; set; } = 0;       // Trang (0-based) — :25, :46
    public int Ft_PageSize { get; set; } = 10;       // Số dòng/trang — :25, :47
}

/// <summary>Port 1:1 <c>GetByBrandCode</c> — body chỉ có BrandCode (Mst_CarBrandController.cs:216-279).</summary>
public class MstCarBrandGetByBrandCodeReq
{
    public string? BrandCode { get; set; }            // Mã hãng xe cần lấy — :238-241
}

[ApiController]
[Route("api/mst_carbrand")]
[Produces("application/json")]
public class Mst_CarBrandControllerPort : ControllerBase
{
    private readonly AppDbContext _db;

    public Mst_CarBrandControllerPort(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody] MstCarBrandSearchReq? req)
    {
        var r = req ?? new MstCarBrandSearchReq();
        var pageIndex = Math.Max(0, r.Ft_PageIndex);
        var pageSize = r.Ft_PageSize;

        // Search gọi GetXAsync với start = PageIndex * PageSize, count = PageSize (giữ nguyên hành vi cũ nguồn :68-69, :75).
        var start = (long)pageIndex * pageSize;
        var (itemCount, rows) = await GetXAsync(r.KeyWord, r.FlagActive, start, pageSize);

        // PageSize=0 -> DataList rỗng (nguồn: nFilterRecordEnd = start-1, SQL biz + gate :83-94).
        // PageCount = 0 khi DataList rỗng, ngược lại ceil(ItemCount / ps) với ps = PageSize (PageSize=0 thì 10) — gate :83-94.
        var ps = pageSize == 0 ? 10 : pageSize;
        var pageCount = rows.Count == 0 ? 0 : (itemCount % ps == 0 ? itemCount / ps : itemCount / ps + 1);

        return Ok(new { Success = true, Data = new PageInfo<object>(rows, itemCount, pageCount, pageIndex, pageSize) });
    }

    /// <summary>
    /// Port 1:1 <c>Mst_CarBrandController.GetAllActive</c> (nguồn gate :106-215).
    /// Nguồn gọi <c>strWhereClause_Mst_CarBrand</c> với <c>FlagActive = "1"</c> (:132-136) ⇒ lọc
    /// <c>FlagActive IN ('1')</c> (:297-305). <c>OrgID</c> chỉ được GÁN vào request (:134, :146),
    /// hàm lọc KHÔNG dùng OrgID ⇒ KHÔNG lọc theo OrgID.
    /// Không body: RecordStart = "0", RecordCount = "123456000" (nguồn Ft_RecordStart/Ft_RecordCount).
    /// Trả <c>Ok(new { Success = true, Data = ... })</c> — Data là MẢNG TRẦN, rỗng thì <c>null</c>
    /// (nguồn :129 khởi tạo null, :155-157 chỉ gán khi có dòng, :195-199 trả thẳng listMst_CarBrand).
    /// </summary>
    [HttpPost("getallactive")]
    public async Task<IActionResult> GetAllActive()
    {
        // Nguồn: Ft_RecordStart = "0", Ft_RecordCount = "123456000" (gate :148-149).
        // FlagActive = "1" (gate :135) ⇒ lọc IN ('1'); OrgID không lọc.
        var (_, rows) = await GetXAsync(null, "1", 0, 123456000);

        // Nguồn :195-199: Data = listMst_CarBrand (mảng trần); rỗng ⇒ null (:129, :155-157).
        return Ok(new { Success = true, Data = rows.Count > 0 ? rows : null });
    }

    // ============================================================================================
    // GetByBrandCode — nguồn Mst_CarBrandController.cs:216-279.
    // Gate: nếu BrandCode rỗng thì KHÔNG truy vấn (:236), objMst_CarBrand giữ null (:235) ⇒ Data = null (:266).
    // Ngược lại gán BrandCode (:238-241) rồi gọi strWhereClause_Mst_CarBrand_By_BrandCode (:309-328)
    // (lọc `BrandCode = 'x'` — KHÔNG truyền FlagActive nên hàng FlagActive '0' VẪN lấy được).
    // Ft_RecordStart = "0", Ft_RecordCount = "1" (:253-254) ⇒ lấy đúng 1 dòng, trả Lst_Mst_CarBrand[0] (:260-263).
    // ============================================================================================
    [HttpPost("getbybrandcode")]
    public async Task<IActionResult> GetByBrandCode([FromBody] MstCarBrandGetByBrandCodeReq? req)
    {
        var brandCode = req?.BrandCode;

        // BrandCode rỗng ⇒ nguồn không truy vấn (:236) và trả Data = null (:266).
        if (string.IsNullOrWhiteSpace(brandCode))
        {
            return Ok(new { Success = true, Data = (object?)null });
        }

        // Lấy 1 dòng theo BrandCode (Ft_RecordStart = "0", Ft_RecordCount = "1" — :253-254).
        var (_, rows) = await GetXAsync(null, null, 0, 1, brandCodeEqual: brandCode);

        // Nguồn trả Lst_Mst_CarBrand[0] khi có dòng, ngược lại null (:260-263).
        return Ok(new { Success = true, Data = rows.Count > 0 ? rows[0] : null });
    }

    /// <summary>
    /// Lọc + đếm + chia trang theo BrandCode asc — dùng cho Search.
    /// Trả (itemCount, rows): itemCount = tổng số dòng sau lọc (MyCount, SQL :411); rows = trang đã cắt kèm MyIdxSeq 0-based (:443).
    /// </summary>
    private async Task<(int itemCount, List<object> rows)> GetXAsync(string? keyWord, string? flagActiveCsv, long recordStart, long recordCount, string? brandCodeEqual = null)
    {
        var q = _db.Set<Mst_CarBrand>().AsNoTracking().AsQueryable();

        // KeyWord -> like '%x%' trên BrandName (Mst_CarBrandController.cs:290).
        // Nguồn chạy SQL Server collation KHÔNG phân biệt hoa/thường ⇒ hạ chữ cả 2 phía rồi LIKE (bẫy #18/#31).
        if (!string.IsNullOrWhiteSpace(keyWord))
        {
            var kw = PortQuery.Contains(keyWord);
            q = q.Where(x => EF.Functions.Like((x.BrandName ?? "").ToLower(), kw, PortQuery.Esc));
        }

        // BrandCode -> `BrandCode = 'x'` (Mst_CarBrandController.cs:316 — strWhereClause_Mst_CarBrand_By_BrandCode).
        // Nguồn dùng `=` trên SQL Server collation KHÔNG phân biệt hoa/thường ⇒ Like KHÔNG wildcard, hạ chữ 2 phía.
        // LƯU Ý: nhánh OrgID trong hàm lọc nguồn (:318-321) trỏ tới cột OrgID KHÔNG tồn tại ở Mst_CarBrand — mã chết,
        //         KHÔNG port. Nhánh FlagActive (:322-...) KHÔNG được truyền ở GetByBrandCode nên KHÔNG chạy.
        if (!string.IsNullOrWhiteSpace(brandCodeEqual))
        {
            var bc = PortQuery.Equal(brandCodeEqual);
            q = q.Where(x => EF.Functions.Like((x.BrandCode ?? "").ToLower(), bc, PortQuery.Esc));
        }

        // FlagActive -> in ('a','b') (Mst_CarBrandController.cs:297-305): tách dấu phẩy, bỏ khoảng trắng, hạ chữ.
        // Dòng `=` (:293-296) BỊ COMMENT ở nguồn nên KHÔNG dùng.
        if (!string.IsNullOrWhiteSpace(flagActiveCsv))
        {
            var ds = flagActiveCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => s.ToLowerInvariant())
                .ToList();
            if (ds.Count > 0)
            {
                q = q.Where(x => ds.Contains((x.FlagActive ?? "").ToLower()));
            }
        }

        // Chia trang theo BrandCode asc (Mst_CarBrand_GetX SQL: :407 order by mcb.BrandCode asc).
        var ordered = q.OrderBy(x => x.BrandCode);
        var itemCount = await ordered.CountAsync();

        // MyIdxSeq = số thứ tự 0-based trong thứ tự BrandCode (identity(bigint, 0, 1) ở SQL nguồn :401).
        // Lấy dòng [recordStart, recordStart+recordCount) theo thứ tự BrandCode.
        var page = recordCount <= 0
            ? new List<Mst_CarBrand>()
            : await ordered.Skip((int)recordStart).Take((int)recordCount).ToListAsync();

        // Select cuối theo t.MyIdxSeq asc (:441-447 order by t.MyIdxSeq asc).
        var rows = page
            .Select((x, i) => (object)new
            {
                MyIdxSeq = recordStart + i,
                x.BrandCode,
                x.NetworkID,
                x.BrandName,
                x.FlagActive,
                x.Remark,
                x.LogLUDTimeUTC,
                x.LogLUBy,
            })
            .ToList();

        return (itemCount, rows);
    }
}
