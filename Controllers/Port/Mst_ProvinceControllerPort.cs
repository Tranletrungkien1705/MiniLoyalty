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
// PORT 1:1 — màn danh mục tỉnh/thành (Mst_Province) của dự án nguồn 2022.7.Loyalty (#213, #214, #215).
//
// Nguồn controller (tầng gate):
//   Mst_ProvinceController.cs:31-111   Search              -> WA_Mst_Province_Get
//   Mst_ProvinceController.cs:113-176  GetByProvinceCode   -> WA_Mst_Province_Get
//   Mst_ProvinceController.cs:178-241  GetAllActive        -> WA_Mst_Province_Get
//   Mst_ProvinceController.cs:735-759  strWhereClause_Mst_Province (mệnh đề lọc Search, GetAllActive)
//   Mst_ProvinceController.cs:761-776  strWhereClause_Mst_Province_By_ProvinceCode (mệnh đề lọc By ProvinceCode)
// Nguồn API:
//   MstProvinceController.cs:20-91     WA_Mst_Province_Get
// Nguồn biz:
//   Master.Loyalty.cs (WAS_Mst_Province_Get -> Mst_Province_Get -> Mst_Province_Get SQL: :442-488)
//
// GIỮ ĐÚNG NGUỒN:
//   * Endpoint là POST (nguồn [AcceptVerbs("POST")]).
//   * Lọc KeyWord: like '%x%' trên ProvinceName (:740-743).
//   * Lọc FlagActive: danh sách in ('a','b') (:748-756).
//   * Lọc ProvinceCode: toán tử = trên ProvinceCode (:768).
//   * OrgID: gate nhận (:31, :61) nhưng strWhereClause_Mst_Province (:735-759) KHÔNG lọc OrgID.
//   * GetByProvinceCode: ProvinceCode rỗng -> không truy vấn, trả Data = null (:132-133, :163).
//   * GetByProvinceCode: không truyền FlagActive (:135-138) -> cờ FlagActive '0' vẫn lấy được.
//   * Chia trang theo mp.ProvinceCode asc (:449), select cuối sắp theo t.MyIdxSeq asc (:488).
//   * MyIdxSeq 0-based identity (:443).
//   * PageSize = 0 -> DataList rỗng, PageCount = 0 (gate :86-94).
// ============================================================================================

/// <summary>Port 1:1 <c>Mst_ProvinceSearch</c> — tham số lọc + phân trang của màn (Mst_ProvinceController.cs:31, 58-63).</summary>
public class MstProvinceSearchReq
{
    public string? OrgID { get; set; }               // Tổ chức — gate nhận (:31, :61) nhưng strWhereClause_Mst_Province (:735-759) KHÔNG lọc OrgID
    public string? KeyWord { get; set; }             // Từ khoá tìm theo tên tỉnh/thành (ProvinceName) — :60, :740-743
    public string? FlagActive { get; set; }          // Cờ hoạt động, danh sách phân cách dấu phẩy — :62, :748-756
    public int Ft_PageIndex { get; set; } = 0;       // Trang (0-based) — :31, :52
    public int Ft_PageSize { get; set; } = 10;       // Số dòng/trang — :31, :53
}

/// <summary>Port 1:1 <c>GetByProvinceCode</c> — body chỉ có ProvinceCode (Mst_ProvinceController.cs:113-176).</summary>
public class MstProvinceGetByProvinceCodeReq
{
    public string? ProvinceCode { get; set; }         // Mã tỉnh/thành cần lấy — :135-138
}

[ApiController]
[Route("api/mst_province")]
[Produces("application/json")]
public class Mst_ProvinceControllerPort(AppDbContext db) : ControllerBase
{
    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody] MstProvinceSearchReq? req)
    {
        var r = req ?? new MstProvinceSearchReq();
        var pageIndex = Math.Max(0, r.Ft_PageIndex);
        var pageSize = r.Ft_PageSize;

        // Search gọi GetXAsync với start = PageIndex * PageSize, count = PageSize (giữ nguyên hành vi cũ nguồn :50-51, :75-76).
        var start = (long)pageIndex * pageSize;
        // OrgID nhận ở request nhưng trong strWhereClause_Mst_Province (:735-759) KHÔNG lọc OrgID; provinceCodeEqual truyền null.
        var (itemCount, rows) = await GetXAsync(r.KeyWord, r.FlagActive, start, pageSize, null);

        // PageSize=0 -> DataList rỗng (nguồn: nFilterRecordEnd = start-1, SQL biz + gate :86-94).
        // PageCount = 0 khi DataList rỗng, ngược lại ceil(ItemCount / ps) với ps = PageSize (PageSize=0 thì 10) — gate :86-94.
        var ps = pageSize == 0 ? 10 : pageSize;
        var pageCount = rows.Count == 0 ? 0 : (itemCount % ps == 0 ? itemCount / ps : itemCount / ps + 1);

        return Ok(new { Success = true, Data = new PageInfo<object>(rows, itemCount, pageCount, pageIndex, pageSize) });
    }

    // ============================================================================================
    // GetByProvinceCode — nguồn Mst_ProvinceController.cs:113-176.
    // Gate: nếu ProvinceCode rỗng thì KHÔNG truy vấn (:133), objMst_Province giữ null (:132) ⇒ Data = null (:163).
    // Ngược lại gán ProvinceCode (:135-138) rồi gọi strWhereClause_Mst_Province_By_ProvinceCode (:761-776)
    // (lọc `ProvinceCode = 'x'` — KHÔNG truyền FlagActive nên hàng FlagActive '0' VẪN lấy được).
    // Ft_RecordStart = "0", Ft_RecordCount = "1" (:150-151) ⇒ lấy đúng 1 dòng, trả Lst_Mst_Province[0] (:157-160).
    // ============================================================================================
    [HttpPost("getbyprovincecode")]
    public async Task<IActionResult> GetByProvinceCode([FromBody] MstProvinceGetByProvinceCodeReq? req)
    {
        var provinceCode = req?.ProvinceCode;

        // ProvinceCode rỗng ⇒ nguồn không truy vấn (:133) và trả Data = null (:163).
        if (string.IsNullOrWhiteSpace(provinceCode))
        {
            return Ok(new { Success = true, Data = (object?)null });
        }

        // Lấy 1 dòng theo ProvinceCode (Ft_RecordStart = "0", Ft_RecordCount = "1" — :150-151).
        var (_, rows) = await GetXAsync(null, null, 0, 1, provinceCodeEqual: provinceCode);

        // Nguồn trả Lst_Mst_Province[0] khi có dòng, ngược lại null (:157-160).
        return Ok(new { Success = true, Data = rows.Count > 0 ? rows[0] : null });
    }

    // ============================================================================================
    // GetAllActive — nguồn Mst_ProvinceController.cs:178-241.
    // Lọc FlagActive = "1" qua strWhereClause_Mst_Province (toán tử IN :748-756).
    // OrgID chỉ gán vào request (:211), KHÔNG đưa vào where clause (:735-759).
    // Ft_RecordStart = "0", Ft_RecordCount = "123456000" (:213-214, BaseController.cs:30-31).
    // HÌNH DÁNG DATA KHÁC CÁC DANH MỤC KHÁC (như CardType):
    // Nguồn bọc trong object { Lst_Mst_Province = lst_Mst_Province } (:225-227),
    // nếu không có bản ghi nào thì lst_Mst_Province giữ null (:197, :220-222) => Data.Lst_Mst_Province = null.
    // ============================================================================================
    [HttpPost("getallactive")]
    public async Task<IActionResult> GetAllActive()
    {
        var (_, rows) = await GetXAsync(null, "1", 0, 123456000, null);
        return Ok(new { Success = true, Data = new { Lst_Mst_Province = rows.Count > 0 ? rows : null } });
    }

    /// <summary>
    /// Lọc + đếm + chia trang theo ProvinceCode asc — dùng chung cho Search, GetByProvinceCode và GetAllActive.
    /// Trả (itemCount, rows): itemCount = tổng số dòng sau lọc (MyCount, SQL :453); rows = trang đã cắt kèm MyIdxSeq 0-based (:443).
    /// </summary>
    private async Task<(int itemCount, List<object> rows)> GetXAsync(string? keyWord, string? flagActiveCsv, long recordStart, long recordCount, string? provinceCodeEqual)
    {
        var q = db.Set<Mst_Province>().AsNoTracking().AsQueryable();

        // KeyWord -> like '%x%' trên ProvinceName (Mst_ProvinceController.cs:740-743).
        // Nguồn chạy SQL Server collation KHÔNG phân biệt hoa/thường ⇒ hạ chữ cả 2 phía rồi LIKE (bẫy #18/#31).
        if (!string.IsNullOrWhiteSpace(keyWord))
        {
            var kw = PortQuery.Contains(keyWord);
            q = q.Where(x => EF.Functions.Like((x.ProvinceName ?? "").ToLower(), kw, PortQuery.Esc));
        }

        // ProvinceCode -> `ProvinceCode = 'x'` (Mst_ProvinceController.cs:768 — strWhereClause_Mst_Province_By_ProvinceCode).
        // Nguồn dùng `=` trên SQL Server collation KHÔNG phân biệt hoa/thường ⇒ Like KHÔNG wildcard, hạ chữ 2 phía.
        if (!string.IsNullOrWhiteSpace(provinceCodeEqual))
        {
            var pc = PortQuery.Equal(provinceCodeEqual);
            q = q.Where(x => EF.Functions.Like((x.ProvinceCode ?? "").ToLower(), pc, PortQuery.Esc));
        }

        // FlagActive -> in ('a','b') (Mst_ProvinceController.cs:748-756): tách dấu phẩy, bỏ khoảng trắng, hạ chữ.
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

        // Chia trang theo ProvinceCode asc (Mst_Province_Get SQL: :449 order by mp.ProvinceCode asc).
        var ordered = q.OrderBy(x => x.ProvinceCode);
        var itemCount = await ordered.CountAsync();

        // MyIdxSeq = số thứ tự 0-based trong thứ tự ProvinceCode (identity(bigint, 0, 1) ở SQL nguồn :443).
        // Lấy dòng [recordStart, recordStart+recordCount) theo thứ tự ProvinceCode.
        var page = recordCount <= 0
            ? new List<Mst_Province>()
            : await ordered.Skip((int)recordStart).Take((int)recordCount).ToListAsync();

        // Select cuối theo t.MyIdxSeq asc (:488 order by t.MyIdxSeq asc).
        var rows = page
            .Select((x, i) => (object)new
            {
                MyIdxSeq = recordStart + i,
                x.ProvinceCode,
                x.NetworkID,
                x.ProvinceName,
                x.FlagActive,
                x.LogLUDTimeUTC,
                x.LogLUBy,
            })
            .ToList();

        return (itemCount, rows);
    }
}
