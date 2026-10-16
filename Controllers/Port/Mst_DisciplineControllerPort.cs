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
// PORT 1:1 — màn danh mục hình thức kỷ luật (Mst_Discipline) của dự án nguồn 2022.7.Loyalty (#132, #133).
//
// Nguồn controller (tầng gate):
//   Mst_DisciplineController.cs:26-106         Search -> WA_Mst_Discipline_Get (#132)
//   Mst_DisciplineController.cs:108-171        GetByDisciplineCode -> WA_Mst_Discipline_Get (#133)
//   Mst_DisciplineController.cs:523-538        strWhereClause_Mst_Discipline (mệnh đề lọc Search)
//   Mst_DisciplineController.cs:540-555        strWhereClause_Mst_Discipline_By_DisciplineCode (mệnh đề lọc GetByDisciplineCode)
// Nguồn API:
//   MstDisciplineController.cs:17-88          WA_Mst_Discipline_Get
// Nguồn biz:
//   MasterData.cs:34241-34331                 WAS_Mst_Discipline_Get
//   MasterData.cs:34111-34239                 Mst_Discipline_Get
//   MasterData.cs:33954-34109                 Mst_Discipline_GetX (SQL :34007-34059)
//
// GIỮ ĐÚNG NGUỒN:
//   * Endpoint là POST (nguồn [AcceptVerbs("POST")]).
//   * Lọc KeyWord: like '%kw%' trên DisciplineName (Mst_DisciplineController.cs:530).
//   * Lọc FlagActive: toán tử '=' (Mst_DisciplineController.cs:534) — KHÔNG IN.
//     Không tách dấu phẩy; "0,1" -> so sánh bằng chuỗi "0,1" -> 0 dòng.
//   * OrgID truyền vào (:26, :56) nhưng strWhereClause_Mst_Discipline (:523-538) KHÔNG dùng OrgID -> không lọc.
//   * Chia trang theo md.DisciplineCode asc (MasterData.cs:34016), select cuối sắp theo t.MyIdxSeq asc (MasterData.cs:34055).
//   * MyIdxSeq 0-based identity (MasterData.cs:34010).
//   * PageSize = 0 -> DataList rỗng, PageCount = 0 (gate :81-89).
//   * DDL: khong co DDL trong repo — PK = DisciplineCode (khoá draft), kieu cot can xac nhan.
//   * GetByDisciplineCode: [HttpPost("getbydisciplinecode")], body { DisciplineCode } (gate :108-171).
//     DisciplineCode rỗng -> Data null, không truy vấn (:128, :158). Ngược lại lọc DisciplineCode = (Like + PortQuery.Equal, :547).
//     FlagActive không truyền (:130-133) nên hàng FlagActive '0' vẫn lấy.
//     Ft_RecordStart = "0", Ft_RecordCount = "1" (:145-146). Trả Lst_Mst_Discipline[0] hoặc null (:152-155).
//
// Bỏ qua có chủ đích (loại Đ):
//   - CheckGatewayAuthentication: gate :35, :117, API :40-45
//   - CheckAuthorize: gate :36, :118 (hệ thống đích chưa có hệ phân quyền)
//   - Ghi RequestLog/ReturnLog: MasterData.cs:34152-34159, 34229-34236
//   - Sys_Access_CheckDenyV30, Sys_User_CheckAuthentication
// ============================================================================================

/// <summary>Port 1:1 <c>Mst_DisciplineSearch</c> — tham số lọc + phân trang của màn (Mst_DisciplineController.cs:26, 53-58).</summary>
public class MstDisciplineSearchReq
{
    public string? OrgID { get; set; }               // Tổ chức — gate nhận (:26, :56) nhưng strWhereClause_Mst_Discipline (:523-538) KHÔNG dùng OrgID -> không lọc
    public string? KeyWord { get; set; }             // Từ khoá tìm theo tên hình thức kỷ luật (DisciplineName) — :26, :55, :530
    public string? FlagActive { get; set; }          // Cờ hoạt động, toán tử '=' — :26, :57, :534
    public int Ft_PageIndex { get; set; } = 0;       // Trang (0-based) — :26, :47
    public int Ft_PageSize { get; set; } = 10;       // Số dòng/trang — :26, :48
}

/// <summary>Port 1:1 <c>GetByDisciplineCode</c> — body chỉ có DisciplineCode (Mst_DisciplineController.cs:108-171).</summary>
public class MstDisciplineGetByDisciplineCodeReq
{
    public string? DisciplineCode { get; set; }       // Mã hình thức kỷ luật cần lấy — :108, :128-133
}

[ApiController]
[Route("api/mst_discipline")]
[Produces("application/json")]
public class Mst_DisciplineControllerPort : ControllerBase
{
    private readonly AppDbContext _db;

    public Mst_DisciplineControllerPort(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] MstDisciplineSearchReq? req)
    {
        var r = req ?? new MstDisciplineSearchReq();
        var pageIndex = Math.Max(0, r.Ft_PageIndex);
        var pageSize = r.Ft_PageSize;

        // Search gọi GetXAsync với start = PageIndex * PageSize, count = PageSize (giữ nguyên hành vi cũ nguồn :50-51, :70-71).
        var start = (long)pageIndex * pageSize;
        var (itemCount, rows) = await GetXAsync(r.KeyWord, r.FlagActive, start, pageSize);

        // PageSize=0 -> DataList rỗng (nguồn: nFilterRecordEnd = start-1, SQL biz + gate :81-89).
        // PageCount = 0 khi DataList rỗng, ngược lại ceil(ItemCount / ps) với ps = PageSize (PageSize=0 thì 10) — gate :81-89.
        var ps = pageSize == 0 ? 10 : pageSize;
        var pageCount = rows.Count == 0 ? 0 : (itemCount % ps == 0 ? itemCount / ps : itemCount / ps + 1);

        return Ok(new { Success = true, Data = new PageInfo<object>(rows, itemCount, pageCount, pageIndex, pageSize) });
    }

    // ============================================================================================
    // GetByDisciplineCode — nguồn Mst_DisciplineController.cs:108-171.
    // Gate: nếu DisciplineCode rỗng thì KHÔNG truy vấn (:128), objMst_Discipline giữ null (:127) ⇒ Data = null (:158).
    // Ngược lại gán DisciplineCode (:130-133) rồi gọi strWhereClause_Mst_Discipline_By_DisciplineCode (:540-555)
    // (lọc `DisciplineCode = 'x'` — KHÔNG truyền FlagActive nên hàng FlagActive '0' VẪN lấy được).
    // Ft_RecordStart = "0", Ft_RecordCount = "1" (:145-146) ⇒ lấy đúng 1 dòng, trả Lst_Mst_Discipline[0] (:152-155).
    // ============================================================================================
    [HttpPost("getbydisciplinecode")]
    public async Task<IActionResult> GetByDisciplineCode([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] MstDisciplineGetByDisciplineCodeReq? req)
    {
        var disciplineCode = req?.DisciplineCode;

        // DisciplineCode rỗng ⇒ nguồn không truy vấn (:128) và trả Data = null (:158).
        if (string.IsNullOrWhiteSpace(disciplineCode))
        {
            return Ok(new { Success = true, Data = (object?)null });
        }

        // Lấy 1 dòng theo DisciplineCode (Ft_RecordStart = "0", Ft_RecordCount = "1" — :145-146).
        var (_, rows) = await GetXAsync(null, null, 0, 1, disciplineCodeEqual: disciplineCode);

        // Nguồn trả Lst_Mst_Discipline[0] khi có dòng, ngược lại null (:152-155).
        return Ok(new { Success = true, Data = rows.Count > 0 ? rows[0] : null });
    }

    /// <summary>
    /// Lọc + đếm + chia trang theo DisciplineCode asc — dùng cho Search, GetByDisciplineCode.
    /// Trả (itemCount, rows): itemCount = tổng số dòng sau lọc (MyCount, SQL :34020); rows = trang đã cắt kèm MyIdxSeq 0-based (:34050).
    /// </summary>
    private async Task<(int itemCount, List<object> rows)> GetXAsync(string? keyWord, string? flagActive, long recordStart, long recordCount, string? disciplineCodeEqual = null)
    {
        var q = _db.Set<Mst_Discipline>().AsNoTracking().AsQueryable();

        // KeyWord -> like '%kw%' trên DisciplineName (Mst_DisciplineController.cs:530).
        // Nguồn chạy SQL Server collation KHÔNG phân biệt hoa/thường ⇒ hạ chữ cả 2 phía rồi LIKE (bẫy #18/#31).
        if (!string.IsNullOrWhiteSpace(keyWord))
        {
            var kw = PortQuery.Contains(keyWord);
            q = q.Where(x => EF.Functions.Like((x.DisciplineName ?? "").ToLower(), kw, PortQuery.Esc));
        }

        // DisciplineCode -> `DisciplineCode = 'x'` (Mst_DisciplineController.cs:547 — strWhereClause_Mst_Discipline_By_DisciplineCode).
        // Nguồn dùng `=` trên SQL Server collation KHÔNG phân biệt hoa/thường ⇒ Like KHÔNG wildcard, hạ chữ 2 phía (PortQuery.Equal).
        // FlagActive không được truyền ở GetByDisciplineCode (:130-133) nên hàng FlagActive '0' vẫn lấy được.
        if (!string.IsNullOrWhiteSpace(disciplineCodeEqual))
        {
            var dc = PortQuery.Equal(disciplineCodeEqual);
            q = q.Where(x => EF.Functions.Like((x.DisciplineCode ?? "").ToLower(), dc, PortQuery.Esc));
        }

        // FlagActive -> toán tử '=' trên FlagActive (Mst_DisciplineController.cs:534) — KHÔNG IN.
        // Nguồn dùng '=' ⇒ so sánh bằng chuỗi không phân biệt hoa/thường bằng Like + PortQuery.Equal (không tách dấu phẩy).
        if (!string.IsNullOrWhiteSpace(flagActive))
        {
            var fa = PortQuery.Equal(flagActive);
            q = q.Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), fa, PortQuery.Esc));
        }

        // Chia trang theo DisciplineCode asc (Mst_Discipline_GetX SQL: MasterData.cs:34016 order by md.DisciplineCode asc).
        var ordered = q.OrderBy(x => x.DisciplineCode);
        var itemCount = await ordered.CountAsync();

        // MyIdxSeq = số thứ tự 0-based trong thứ tự DisciplineCode (identity(bigint, 0, 1) ở SQL nguồn :34010).
        // Lấy dòng [recordStart, recordStart+recordCount) theo thứ tự DisciplineCode.
        var page = recordCount <= 0
            ? new List<Mst_Discipline>()
            : await ordered.Skip((int)recordStart).Take((int)recordCount).ToListAsync();

        // Select cuối theo t.MyIdxSeq asc (MasterData.cs:34055 order by t.MyIdxSeq asc).
        var rows = page
            .Select((x, i) => (object)new
            {
                MyIdxSeq = recordStart + i,
                x.DisciplineCode,
                x.NetworkID,
                x.DisciplineName,
                x.DisciplineDesc,
                x.FlagActive,
                x.LogLUDTimeUTC,
                x.LogLUBy,
            })
            .ToList();

        return (itemCount, rows);
    }
}
