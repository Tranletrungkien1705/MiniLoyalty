using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MiniLoyalty.Data;
using MiniLoyalty.Models.Port;

namespace MiniLoyalty.Controllers.Port;

// ============================================================================================
// PORT 1:1 — màn danh mục phòng ban (Mst_Department) của dự án nguồn 2022.7.Loyalty (#122, #123).
//
// Nguồn controller (tầng gate):
//   Mst_DepartmentController.cs:30-109   Search -> WA_Mst_Department_Get (#122)
//   Mst_DepartmentController.cs:110-218  GetAllActive -> WA_Mst_Department_Get (#123)
//   Mst_DepartmentController.cs:877-905  strWhereClause_Mst_Department (mệnh đề lọc Search, GetAllActive)
// Nguồn API:
//   MstDepartmentController.cs:20-89     WA_Mst_Department_Get
// Nguồn biz:
//   Master.cs:29450-29548                WAS_Mst_Department_Get
//   Master.cs:29549-29701                Mst_Department_Get
//   Master.cs:30396-30568                Mst_Department_GetX (SQL :30449-30515)
//
// GIỮ ĐÚNG NGUỒN:
//   * Endpoint là POST (nguồn Mst_DepartmentController.cs:30, :110).
//   * Lọc KeyWord: like '%kw%' trên DepartmentName (Mst_DepartmentController.cs:884).
//   * Lọc OrgID: tham số CLIENT ở Search (:30, :888), hoặc OrgID từ user đăng nhập ở GetAllActive (:138).
//     Toán tử '=' trên OrgID.
//   * Lọc FlagActive: tách dấu phẩy -> danh sách IN ('a','b') (Mst_DepartmentController.cs:894-902).
//     Dòng '=' (:890-893) BỊ COMMENT trong nguồn nên KHÔNG chạy.
//   * Chia trang theo md.DepartmentCode asc (Master.cs:30457), select cuối sắp theo t.MyIdxSeq asc (Master.cs:30514).
//   * MyIdxSeq 0-based identity (Master.cs:30450).
//   * PageSize = 0 -> DataList rỗng, PageCount = 0 (gate :84-94).
//   * GetAllActive: không body, Ft_RecordStart = "0", Ft_RecordCount = "123456000" (gate :152-153).
//     FlagActive = "1" (:139). OrgID lấy từ user đăng nhập orgUserUI.OrgId (:138).
//     Đích chưa có user đăng nhập: lấy từ IConfiguration["Biz_OrgID"]; nếu không cấu hình (null/rỗng)
//     thì GetXAsync bỏ nhánh lọc OrgID -> lấy mọi tổ chức (GAP: nguồn lọc theo tổ chức của user đăng nhập).
//     Khối Ext/tree (:162-193) bị comment ở nguồn -> trả Data là mảng trần list Mst_Department (null nếu rỗng) (:199-204).
//   * Cột tính md_DepartmentNameParent: left join Mst_Department f on t.DepartmentCodeParent = f.DepartmentCode (:30512-30513),
//     lấy tên phòng ban cấp trên f.DepartmentName (null nếu không có cấp trên).
//   * Cột tính ss_QtyStaff: nguồn left join count StaffCode từ Staff_MapDepartment group by DepartmentCode (:30490-30499, :30506).
//     Bảng Staff_MapDepartment KHÔNG có ở đích, không có DDL, không có trong ledger -> ss_QtyStaff = 0 cho mọi dòng.
//     GHI GAP: khi port Staff_MapDepartment phải đếm thật.
//   * DDL: không có DDL trong repo nguồn — PK = DepartmentCode (khoá draft), kiểu cột cần xác nhận.
//
// Bỏ qua có chủ đích (loại Đ):
//   - CheckGatewayAuthentication: gate :39, :123, API :40-46
//   - CheckAuthorize: gate :40, :124 (hệ thống đích chưa có hệ phân quyền)
//   - Ghi RequestLog/ReturnLog: Master.cs:29593-29600, 29691-29698
//   - Sys_User_CheckAuthorize / Sys_Access_CheckDenyV30: Master.cs:29606-29620, 29630-29634
// ============================================================================================

/// <summary>Port 1:1 <c>Mst_DepartmentSearch</c> — tham số lọc + phân trang của màn (Mst_DepartmentController.cs:30, 57-61).</summary>
public class MstDepartmentSearchReq
{
    public string? OrgID { get; set; }               // Tổ chức — tham số client, toán tử '=' (:30, :59, :888)
    public string? KeyWord { get; set; }             // Từ khoá tìm theo tên phòng ban (DepartmentName), like '%kw%' (:30, :58, :884)
    public string? FlagActive { get; set; }          // Cờ hoạt động, danh sách phân cách dấu phẩy -> IN (:30, :60, :894-902)
    public int Ft_PageIndex { get; set; } = 0;       // Trang (0-based) (:30, :51)
    public int Ft_PageSize { get; set; } = 10;       // Số dòng/trang (:30, :52)
}

[ApiController]
[Route("api/mst_department")]
[Produces("application/json")]
public class Mst_DepartmentControllerPort : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public Mst_DepartmentControllerPort(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] MstDepartmentSearchReq? req)
    {
        var r = req ?? new MstDepartmentSearchReq();
        var pageIndex = Math.Max(0, r.Ft_PageIndex);
        var pageSize = r.Ft_PageSize;

        // Search gọi GetXAsync với start = PageIndex * PageSize, count = PageSize (giữ nguyên hành vi nguồn Mst_DepartmentController.cs:73-74).
        var start = (long)pageIndex * pageSize;
        var (itemCount, rows) = await GetXAsync(r.KeyWord, r.OrgID, r.FlagActive, start, pageSize);

        // PageSize=0 -> DataList rỗng (nguồn: nFilterRecordEnd = start-1, SQL biz + gate :84-92).
        // PageCount = 0 khi DataList rỗng, ngược lại ceil(ItemCount / ps) với ps = PageSize (PageSize=0 thì 10) — gate :87-92.
        var ps = pageSize == 0 ? 10 : pageSize;
        var pageCount = rows.Count == 0 ? 0 : (itemCount % ps == 0 ? itemCount / ps : itemCount / ps + 1);

        return Ok(new { Success = true, Data = new PageInfo<object>(rows, itemCount, pageCount, pageIndex, pageSize) });
    }

    /// <summary>
    /// Port 1:1 <c>Mst_DepartmentController.GetAllActive</c> (nguồn gate Mst_DepartmentController.cs:110-218).
    /// Nguồn gọi <c>strWhereClause_Mst_Department</c> với <c>OrgID = orgUserUI.OrgId</c>, <c>FlagActive = "1"</c> (:136-140) ⇒ lọc
    /// phòng ban đang hoạt động thuộc tổ chức của user đang đăng nhập.
    /// Không body: RecordStart = "0", RecordCount = "123456000" (nguồn Ft_RecordStart/Ft_RecordCount gate :152-153).
    /// Khối Ext/tree (:162-193) bị comment ở nguồn. Trả <c>Ok(new { Success = true, Data = ... })</c> — Data là MẢNG TRẦN,
    /// null nếu rỗng (nguồn :133 khởi tạo null, :159-161 gán list khi có dòng, :199-204 trả Json).
    /// Đích chưa có user: lấy OrgID từ cấu hình <c>_config["Biz_OrgID"]</c>; nếu không cấu hình (null/rỗng)
    /// thì GetXAsync bỏ nhánh lọc OrgID -> lấy mọi tổ chức (GAP: nguồn lọc theo tổ chức của user đang đăng nhập).
    /// Trả <c>Ok(new { Success = true, Data = rows.Count > 0 ? rows : null })</c>.
    /// </summary>
    [HttpPost("getallactive")]
    public async Task<IActionResult> GetAllActive()
    {
        // Nguồn: Ft_RecordStart = "0", Ft_RecordCount = "123456000" (gate :152-153).
        // Nguồn lọc: OrgID = orgUserUI.OrgId (:138), FlagActive = "1" (:139).
        // Đích chưa có user: lấy OrgID từ cấu hình Biz_OrgID (GAP: nguồn lọc theo user đăng nhập); null/rỗng -> mọi tổ chức.
        var (_, rows) = await GetXAsync(null, _config["Biz_OrgID"], "1", 0, 123456000);

        // Nguồn :199-204: Data = listMst_Department (mảng trần); rỗng ⇒ null (:133, :159-161).
        return Ok(new { Success = true, Data = rows.Count > 0 ? rows : null });
    }

    /// <summary>
    /// Lọc + đếm + chia trang theo DepartmentCode asc — dùng cho Search.
    /// Trả (itemCount, rows): itemCount = tổng số dòng sau lọc (MyCount, SQL :30461); rows = trang đã cắt kèm MyIdxSeq 0-based (:30503).
    /// </summary>
    private async Task<(int itemCount, List<object> rows)> GetXAsync(string? keyWord, string? orgIdEqual, string? flagActiveCsv, long recordStart, long recordCount)
    {
        var q = _db.Set<Mst_Department>().AsNoTracking().AsQueryable();

        // KeyWord -> like '%kw%' trên DepartmentName (Mst_DepartmentController.cs:884).
        // Nguồn chạy SQL Server collation KHÔNG phân biệt hoa/thường ⇒ hạ chữ cả 2 phía rồi LIKE (bẫy #18/#31).
        if (!string.IsNullOrWhiteSpace(keyWord))
        {
            var kw = PortQuery.Contains(keyWord);
            q = q.Where(x => EF.Functions.Like((x.DepartmentName ?? "").ToLower(), kw, PortQuery.Esc));
        }

        // OrgID -> `OrgID = 'x'` (Mst_DepartmentController.cs:888).
        // Nguồn dùng '=' ⇒ so sánh bằng chuỗi không phân biệt hoa/thường bằng Like + PortQuery.Equal (không wildcard).
        if (!string.IsNullOrWhiteSpace(orgIdEqual))
        {
            var orgId = PortQuery.Equal(orgIdEqual);
            q = q.Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), orgId, PortQuery.Esc));
        }

        // FlagActive -> in ('a','b') (Mst_DepartmentController.cs:894-902): tách dấu phẩy, bỏ khoảng trắng, hạ chữ.
        // Dòng '=' (:890-893) BỊ COMMENT ở nguồn nên KHÔNG dùng.
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

        // Chia trang theo DepartmentCode asc (Mst_Department_GetX SQL: Master.cs:30457 order by md.DepartmentCode asc).
        var ordered = q.OrderBy(x => x.DepartmentCode);
        var itemCount = await ordered.CountAsync();

        // MyIdxSeq = số thứ tự 0-based trong thứ tự DepartmentCode (identity(bigint, 0, 1) ở SQL nguồn :30450).
        // Lấy dòng [recordStart, recordStart+recordCount) theo thứ tự DepartmentCode.
        var page = recordCount <= 0
            ? new List<Mst_Department>()
            : await ordered.Skip((int)recordStart).Take((int)recordCount).ToListAsync();

        // Tra cứu tên phòng ban cấp trên md_DepartmentNameParent (left join Mst_Department f on t.DepartmentCodeParent = f.DepartmentCode :30512-30513).
        var parentCodes = page
            .Select(x => x.DepartmentCodeParent)
            .Where(c => !string.IsNullOrEmpty(c))
            .Distinct()
            .ToList();

        var parentMap = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        if (parentCodes.Count > 0)
        {
            var parents = await _db.Set<Mst_Department>()
                .AsNoTracking()
                .Where(x => parentCodes.Contains(x.DepartmentCode))
                .Select(x => new { x.DepartmentCode, x.DepartmentName })
                .ToListAsync();

            foreach (var p in parents)
            {
                parentMap[p.DepartmentCode] = p.DepartmentName;
            }
        }

        // Select cuối theo t.MyIdxSeq asc (Master.cs:30514 order by t.MyIdxSeq asc).
        // ss_QtyStaff: nguồn left join count từ Staff_MapDepartment (:30490-30499) -> chưa có ở đích -> gán 0 cho mọi dòng (GAP).
        var rows = page
            .Select((x, i) =>
            {
                string? parentName = !string.IsNullOrEmpty(x.DepartmentCodeParent) && parentMap.TryGetValue(x.DepartmentCodeParent, out var pn)
                    ? pn
                    : null;

                return (object)new
                {
                    MyIdxSeq = recordStart + i,
                    x.DepartmentCode,
                    x.NetworkID,
                    x.DepartmentCodeParent,
                    x.DepartmentBUCode,
                    x.DepartmentBUPattern,
                    x.DepartmentLevel,
                    x.MST,
                    x.OrgID,
                    x.DepartmentName,
                    x.DepartmentDesc,
                    x.FlagActive,
                    x.LogLUDTimeUTC,
                    x.LogLUBy,
                    md_DepartmentNameParent = parentName,
                    ss_QtyStaff = 0
                };
            })
            .ToList();

        return (itemCount, rows);
    }
}
