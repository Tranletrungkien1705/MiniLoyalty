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
// PORT 1:1 — màn danh mục dân tộc (Mst_EthnicCategory) của dự án nguồn 2022.7.Loyalty (#149).
//
// Nguồn controller (tầng gate):
//   Mst_EthnicCategoryController.cs:25-105     Search -> WA_Mst_EthnicCategory_Get (#149)
//   Mst_EthnicCategoryController.cs:171-186    strWhereClause_Mst_EthnicCategory (mệnh đề lọc Search)
// Nguồn API:
//   MstEthnicCategoryController.cs:17-88      WA_Mst_EthnicCategory_Get
// Nguồn biz:
//   MasterData.cs:41808-41898                 WAS_Mst_EthnicCategory_Get
//   MasterData.cs:41678-41806                 Mst_EthnicCategory_Get
//   MasterData.cs:41521-41676                 Mst_EthnicCategory_GetX (SQL :41574-41626)
//
// GIỮ ĐÚNG NGUỒN:
//   * Endpoint là POST (nguồn [AcceptVerbs("POST")]).
//   * Lọc KeyWord: like '%kw%' trên EthnicName (Mst_EthnicCategoryController.cs:178).
//   * Lọc FlagActive: toán tử '=' (Mst_EthnicCategoryController.cs:182) — KHÔNG IN.
//     Không tách dấu phẩy; "0,1" -> so sánh bằng chuỗi "0,1" -> 0 dòng.
//   * OrgID truyền vào (:25, :55) nhưng strWhereClause_Mst_EthnicCategory (:171-186) KHÔNG dùng OrgID -> không lọc.
//   * Chia trang theo mec.EthnicCode asc (MasterData.cs:41583), select cuối sắp theo t.MyIdxSeq asc (MasterData.cs:41622).
//   * MyIdxSeq 0-based identity (MasterData.cs:41577).
//   * PageSize = 0 -> DataList rỗng, PageCount = 0 (gate :80-90).
//   * DDL: khong co DDL trong repo — PK = EthnicCode (khoá draft), kieu cot can xac nhan.
//
// Bỏ qua có chủ đích (loại Đ):
//   - CheckGatewayAuthentication: gate :34, API :40-45
//   - CheckAuthorize: gate :35 (hệ thống đích chưa có hệ phân quyền)
//   - Ghi RequestLog/ReturnLog: MasterData.cs:41719-41726, 41796-41803
//   - Sys_Access_CheckDenyV30, Sys_User_CheckAuthentication
// ============================================================================================

/// <summary>Port 1:1 <c>Mst_EthnicCategorySearch</c> — tham số lọc + phân trang của màn (Mst_EthnicCategoryController.cs:25, 52-57).</summary>
public class MstEthnicCategorySearchReq
{
    public string? OrgID { get; set; }               // Tổ chức — gate nhận (:25, :55) nhưng strWhereClause_Mst_EthnicCategory (:171-186) KHÔNG dùng OrgID -> không lọc
    public string? KeyWord { get; set; }             // Từ khoá tìm theo tên dân tộc (EthnicName) — :25, :54, :178
    public string? FlagActive { get; set; }          // Cờ hoạt động, toán tử '=' — :25, :56, :182
    public int Ft_PageIndex { get; set; } = 0;       // Trang (0-based) — :25, :46
    public int Ft_PageSize { get; set; } = 10;       // Số dòng/trang — :25, :47
}

[ApiController]
[Route("api/mst_ethniccategory")]
[Produces("application/json")]
public class Mst_EthnicCategoryControllerPort : ControllerBase
{
    private readonly AppDbContext _db;

    public Mst_EthnicCategoryControllerPort(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] MstEthnicCategorySearchReq? req)
    {
        var r = req ?? new MstEthnicCategorySearchReq();
        var pageIndex = Math.Max(0, r.Ft_PageIndex);
        var pageSize = r.Ft_PageSize;

        // Search gọi GetXAsync với start = PageIndex * PageSize, count = PageSize (giữ nguyên hành vi cũ nguồn :50-51, :69-70).
        var start = (long)pageIndex * pageSize;
        var (itemCount, rows) = await GetXAsync(r.KeyWord, r.FlagActive, start, pageSize);

        // PageSize=0 -> DataList rỗng (nguồn: nFilterRecordEnd = start-1, SQL biz + gate :80-90).
        // PageCount = 0 khi DataList rỗng, ngược lại ceil(ItemCount / ps) với ps = PageSize (PageSize=0 thì 10) — gate :80-90.
        var ps = pageSize == 0 ? 10 : pageSize;
        var pageCount = rows.Count == 0 ? 0 : (itemCount % ps == 0 ? itemCount / ps : itemCount / ps + 1);

        return Ok(new { Success = true, Data = new PageInfo<object>(rows, itemCount, pageCount, pageIndex, pageSize) });
    }

    /// <summary>
    /// Lọc + đếm + chia trang theo EthnicCode asc — dùng cho Search.
    /// Trả (itemCount, rows): itemCount = tổng số dòng sau lọc (MyCount, SQL :41587); rows = trang đã cắt kèm MyIdxSeq 0-based (:41617).
    /// </summary>
    private async Task<(int itemCount, List<object> rows)> GetXAsync(string? keyWord, string? flagActive, long recordStart, long recordCount)
    {
        var q = _db.Set<Mst_EthnicCategory>().AsNoTracking().AsQueryable();

        // KeyWord -> like '%kw%' trên EthnicName (Mst_EthnicCategoryController.cs:178).
        // Nguồn chạy SQL Server collation KHÔNG phân biệt hoa/thường ⇒ hạ chữ cả 2 phía rồi LIKE (bẫy #18/#31).
        if (!string.IsNullOrWhiteSpace(keyWord))
        {
            var kw = PortQuery.Contains(keyWord);
            q = q.Where(x => EF.Functions.Like((x.EthnicName ?? "").ToLower(), kw, PortQuery.Esc));
        }

        // FlagActive -> toán tử '=' trên FlagActive (Mst_EthnicCategoryController.cs:182) — KHÔNG IN.
        // Nguồn dùng '=' ⇒ so sánh bằng chuỗi không phân biệt hoa/thường bằng Like + PortQuery.Equal (không tách dấu phẩy).
        if (!string.IsNullOrWhiteSpace(flagActive))
        {
            var fa = PortQuery.Equal(flagActive);
            q = q.Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), fa, PortQuery.Esc));
        }

        // Chia trang theo EthnicCode asc (Mst_EthnicCategory_GetX SQL: MasterData.cs:41583 order by mec.EthnicCode asc).
        var ordered = q.OrderBy(x => x.EthnicCode);
        var itemCount = await ordered.CountAsync();

        // MyIdxSeq = số thứ tự 0-based trong thứ tự EthnicCode (identity(bigint, 0, 1) ở SQL nguồn :41577).
        // Lấy dòng [recordStart, recordStart+recordCount) theo thứ tự EthnicCode.
        var page = recordCount <= 0
            ? new List<Mst_EthnicCategory>()
            : await ordered.Skip((int)recordStart).Take((int)recordCount).ToListAsync();

        // Select cuối theo t.MyIdxSeq asc (MasterData.cs:41622 order by t.MyIdxSeq asc).
        var rows = page
            .Select((x, i) => (object)new
            {
                MyIdxSeq = recordStart + i,
                x.EthnicCode,
                x.NetworkID,
                x.EthnicName,
                x.EthnicDesc,
                x.FlagActive,
                x.LogLUDTimeUTC,
                x.LogLUBy,
            })
            .ToList();

        return (itemCount, rows);
    }
}
