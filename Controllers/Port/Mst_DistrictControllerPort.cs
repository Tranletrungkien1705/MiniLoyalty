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
// PORT 1:1 — màn danh mục quận/huyện (Mst_District) của dự án nguồn 2022.7.Loyalty (#139).
//
// Nguồn controller (tầng gate):
//   Mst_DistrictController.cs:31-111   Search              -> WA_Mst_District_Get
//   Mst_DistrictController.cs:818-833  strWhereClause_Mst_District (mệnh đề lọc Search)
// Nguồn API:
//   MstDistrictController.cs:20-91     WA_Mst_District_Get
// Nguồn biz:
//   Master.cs:2584-2708                WAS_Mst_District_Get
//   Master.cs:1801-2023                Mst_District_Get (SQL excerpt :1883-1929)
//
// GIỮ ĐÚNG NGUỒN:
//   * Endpoint là POST (nguồn [AcceptVerbs("POST")]).
//   * Lọc KeyWord: like '%x%' trên DistrictName (:823-826).
//   * Lọc FlagActive: toán tử = (:827-830) — KHÁC Province dùng IN ('a','b').
//   * OrgID: gate nhận (:31, :61) nhưng strWhereClause_Mst_District (:818-833) KHÔNG lọc OrgID.
//   * Chia trang theo md.DistrictCode asc (:1890), select cuối sắp theo t.MyIdxSeq asc (:1929).
//   * Select cuối: t.MyIdxSeq, md.* (:1923-1929) — KHÔNG join Mst_Province.
//   * MyIdxSeq 0-based identity (:1884).
//   * PageSize = 0 -> DataList rỗng, PageCount = 0 (gate :86-94).
//
// Bỏ qua có chủ đích (loại Đ):
//   - CheckGatewayAuthentication: gate Mst_DistrictController.cs:40; API MstDistrictController.cs:42-49
//   - CheckAuthorize: gate Mst_DistrictController.cs:41 (hệ thống đích chưa có hệ phân quyền)
//   - Nguồn gọi bằng tài khoản hệ thống viết cứng tại MstDistrictController.cs — không chép giá trị
//   - Ghi RequestLog/ReturnLog: Master.cs:1841-1849, 2700-2705
//   - Sys_User_CheckAuthorize: Master.cs:2623-2636
//   - LogLUBy gán cố định "port" do hệ thống đích chưa có session người dùng
// ============================================================================================

/// <summary>Port 1:1 <c>Mst_DistrictSearch</c> — tham số lọc + phân trang của màn (Mst_DistrictController.cs:31, 58-63).</summary>
public class MstDistrictSearchReq
{
    public string? OrgID { get; set; }               // Tổ chức — gate nhận (:31, :61) nhưng strWhereClause_Mst_District (:818-833) KHÔNG lọc OrgID
    public string? KeyWord { get; set; }             // Từ khoá tìm theo tên quận/huyện (DistrictName) — :60, :823-826
    public string? FlagActive { get; set; }          // Cờ hoạt động — :62, :827-830 (toán tử =)
    public int Ft_PageIndex { get; set; } = 0;       // Trang (0-based) — :31, :52
    public int Ft_PageSize { get; set; } = 10;       // Số dòng/trang — :31, :53
}

[ApiController]
[Route("api/mst_district")]
[Produces("application/json")]
public class Mst_DistrictControllerPort(AppDbContext db) : ControllerBase
{
    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody] MstDistrictSearchReq? req)
    {
        var r = req ?? new MstDistrictSearchReq();
        var pageIndex = Math.Max(0, r.Ft_PageIndex);
        var pageSize = r.Ft_PageSize;

        // Search gọi GetXAsync với start = PageIndex * PageSize, count = PageSize (giữ nguyên hành vi cũ nguồn :50-51, :75-76).
        var start = (long)pageIndex * pageSize;
        // OrgID nhận ở request nhưng trong strWhereClause_Mst_District (:818-833) KHÔNG lọc OrgID; districtCodeEqual truyền null.
        var (itemCount, rows) = await GetXAsync(r.KeyWord, r.FlagActive, start, pageSize, null);

        // PageSize=0 -> DataList rỗng (nguồn: nFilterRecordEnd = start-1, SQL biz + gate :86-94).
        // PageCount = 0 khi DataList rỗng, ngược lại ceil(ItemCount / ps) với ps = PageSize (PageSize=0 thì 10) — gate :86-94.
        var ps = pageSize == 0 ? 10 : pageSize;
        var pageCount = rows.Count == 0 ? 0 : (itemCount % ps == 0 ? itemCount / ps : itemCount / ps + 1);

        return Ok(new { Success = true, Data = new PageInfo<object>(rows, itemCount, pageCount, pageIndex, pageSize) });
    }

    /// <summary>
    /// Hàm dùng chung lấy dữ liệu Mst_District hỗ trợ phân trang và tìm kiếm.
    /// Nguồn SQL: Master.cs:1881-1934 (Mst_District_Get).
    /// </summary>
    public async Task<(int ItemCount, List<object> Rows)> GetXAsync(
        string? keyWord,
        string? flagActive,
        long recordStart,
        long recordCount,
        string? districtCodeEqual = null)
    {
        var q = db.Set<Mst_District>().AsNoTracking().AsQueryable();

        // KeyWord -> like '%x%' trên DistrictName (Mst_DistrictController.cs:823-826).
        // Nguồn chạy SQL Server collation KHÔNG phân biệt hoa/thường ⇒ hạ chữ cả 2 phía rồi LIKE (bẫy #18/#31).
        if (!string.IsNullOrWhiteSpace(keyWord))
        {
            var kw = PortQuery.Contains(keyWord);
            q = q.Where(x => EF.Functions.Like((x.DistrictName ?? "").ToLower(), kw, PortQuery.Esc));
        }

        // DistrictCode -> `DistrictCode = 'x'`
        // Nguồn dùng `=` trên SQL Server collation KHÔNG phân biệt hoa/thường ⇒ Like KHÔNG wildcard, hạ chữ 2 phía.
        if (!string.IsNullOrWhiteSpace(districtCodeEqual))
        {
            var dc = PortQuery.Equal(districtCodeEqual);
            q = q.Where(x => EF.Functions.Like((x.DistrictCode ?? "").ToLower(), dc, PortQuery.Esc));
        }

        // FlagActive -> toán tử '=' (Mst_DistrictController.cs:827-830) — KHÁC Province dùng IN:
        // EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal(flagActive), PortQuery.Esc)
        if (!string.IsNullOrWhiteSpace(flagActive))
        {
            var fa = PortQuery.Equal(flagActive);
            q = q.Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), fa, PortQuery.Esc));
        }

        // OrgID: nhận ở gate (Mst_DistrictController.cs:31, :61) nhưng strWhereClause_Mst_District (:818-833) KHÔNG lọc OrgID.

        // Chia trang theo DistrictCode asc (Mst_District_Get SQL: :1890 order by md.DistrictCode asc).
        var ordered = q.OrderBy(x => x.DistrictCode);
        var itemCount = await ordered.CountAsync();

        // MyIdxSeq = số thứ tự 0-based trong thứ tự DistrictCode (identity(bigint, 0, 1) ở SQL nguồn :1884).
        // Lấy dòng [recordStart, recordStart+recordCount) theo thứ tự DistrictCode.
        var page = recordCount <= 0
            ? new List<Mst_District>()
            : await ordered.Skip((int)recordStart).Take((int)recordCount).ToListAsync();

        // Select cuối theo t.MyIdxSeq asc (:1929 order by t.MyIdxSeq asc) — KHÔNG join Mst_Province.
        var rows = page
            .Select((x, i) => (object)new
            {
                MyIdxSeq = recordStart + i,
                x.DistrictCode,
                x.ProvinceCode,
                x.NetworkID,
                x.DistrictName,
                x.FlagActive,
                x.LogLUDTimeUTC,
                x.LogLUBy,
            })
            .ToList();

        return (itemCount, rows);
    }
}
