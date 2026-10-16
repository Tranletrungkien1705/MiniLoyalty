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
// PORT 1:1 — màn danh mục trình độ học vấn (Mst_Qualification) của dự án nguồn 2022.7.Loyalty (#223).
//
// Nguồn controller (tầng gate):
//   Mst_QualificationController.cs:25-105    Search -> WA_Mst_Qualification_Get
//   Mst_QualificationController.cs:171-186   strWhereClause_Mst_Qualification (mệnh đề lọc Search)
// Nguồn API:
//   MstQualificationController.cs:17-88      WA_Mst_Qualification_Get
// Nguồn biz:
//   MasterData.cs:36846-36936                WAS_Mst_Qualification_Get
//   MasterData.cs:36716-36844                Mst_Qualification_Get
//   MasterData.cs:36559-36714                Mst_Qualification_GetX (SQL :36612-36645)
//
// GIỮ ĐÚNG NGUỒN:
//   * Endpoint là POST (nguồn [AcceptVerbs("POST")]).
//   * Lọc KeyWord: like '%kw%' trên QualificationName (Mst_QualificationController.cs:176-179).
//     CHỈ lọc QualificationName — KHÔNG lọc QualificationCode/Remark.
//   * Lọc FlagActive: toán tử '=' (Mst_QualificationController.cs:180-183) — KHÔNG IN.
//     Không tách dấu phẩy; "1,0" -> so sánh bằng chuỗi "1,0" -> 0 dòng.
//   * OrgID truyền vào (:25, :55) nhưng strWhereClause_Mst_Qualification (:171-186) KHÔNG dùng OrgID
//     và SQL biz (:36612-36645) cũng KHÔNG lọc OrgID/NetworkID -> không lọc.
//   * Chia trang theo mq.QualificationCode asc (MasterData.cs:36621), select cuối sắp theo t.MyIdxSeq asc (MasterData.cs:36660).
//   * MyIdxSeq 0-based identity (MasterData.cs:36615).
//   * PageSize = 0 -> DataList rỗng, PageCount = 0 (gate :80-88).
//   * DDL: khong co DDL trong repo — PK = QualificationCode (suy từ join, DDL chưa xác minh).
//
// Bỏ qua có chủ đích (loại Đ):
//   - CheckGatewayAuthentication: gate :34, API :40-45
//   - CheckAuthorize: gate :35 (hệ thống đích chưa có hệ phân quyền)
//   - Tài khoản viết cứng GwUserCode/GwPassword tại gate :62-63 — KHÔNG chép giá trị (bài học A.10)
//   - Ghi RequestLog/ReturnLog: MasterData.cs:36833-36841
//   - Sys_User_CheckAuthentication, Sys_Access_CheckDenyV30: MasterData.cs:36797-36808
// ============================================================================================

/// <summary>Port 1:1 <c>Mst_QualificationSearch</c> — tham số lọc + phân trang của màn (Mst_QualificationController.cs:25, :52-57).</summary>
public class MstQualificationSearchReq
{
    public string? OrgID { get; set; }               // Tổ chức — gate nhận (:25, :55) nhưng strWhereClause_Mst_Qualification (:171-186) KHÔNG dùng OrgID -> không lọc
    public string? KeyWord { get; set; }             // Từ khoá tìm theo tên trình độ (QualificationName) — :25, :54, :176-179
    public string? FlagActive { get; set; }          // Cờ hoạt động, toán tử '=' — :25, :56, :180-183
    public int Ft_PageIndex { get; set; } = 0;       // Trang (0-based) — :25, :46
    public int Ft_PageSize { get; set; } = 10;       // Số dòng/trang — :25, :47
}

[ApiController]
[Route("api/mst_qualification")]
[Produces("application/json")]
public class Mst_QualificationControllerPort : ControllerBase
{
    private readonly AppDbContext _db;

    public Mst_QualificationControllerPort(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] MstQualificationSearchReq? req)
    {
        var r = req ?? new MstQualificationSearchReq();
        var pageIndex = r.Ft_PageIndex;
        var pageSize = r.Ft_PageSize;

        // Search gọi GetXAsync với start = PageIndex * PageSize, count = PageSize (giữ nguyên hành vi cũ nguồn :69-70).
        var start = (long)pageIndex * pageSize;
        var (itemCount, rows) = await GetXAsync(r.KeyWord, r.FlagActive, start, pageSize);

        // PageSize=0 -> DataList rỗng (nguồn: nFilterRecordEnd = start-1, SQL biz + gate :80-88).
        // PageCount = 0 khi DataList rỗng, ngược lại ceil(ItemCount / ps) với ps = PageSize (PageSize=0 thì 10) — gate :80-88.
        var ps = pageSize == 0 ? 10 : pageSize;
        var pageCount = rows.Count == 0 ? 0 : (itemCount % ps == 0 ? itemCount / ps : itemCount / ps + 1);

        // Data là đối tượng PageInfo (KHÔNG null); DataList rỗng là mảng rỗng [] (KHÔNG null) — gate :48-51.
        return Ok(new { Success = true, Data = new PageInfo<object>(rows, itemCount, pageCount, pageIndex, pageSize) });
    }

    // ============================================================================================
    // GetAllActive — nguồn Mst_QualificationController.cs:107-167 (#224).
    // Mệnh đề lọc: strWhereClause_Mst_Qualification_Active (Mst_QualificationController.cs:188-199).
    // Gate:
    //   - Không nhận body: gọi trực tiếp [HttpPost("getallactive")].
    //   - Gọi strWhereClause_Mst_Qualification_Active với { FlagActive = "1" } (:127-130).
    //   - FlagActive: toán tử '=' (:195) — CÓ TÁC DỤNG, lọc FlagActive = '1'.
    //   - OrgID không được truyền vào mệnh đề lọc (:188-199) -> không lọc.
    //   - Ft_RecordStart = "0", Ft_RecordCount = "123456000" (:142-143).
    //   - Trả Data = lst_Mst_Qualification (danh sách trần, null nếu rỗng — :126, :149-154).
    // ============================================================================================
    [HttpPost("getallactive")]
    public async Task<IActionResult> GetAllActive()
    {
        // Gate gọi GetXAsync với FlagActive = "1", RecordStart = 0, RecordCount = 123456000 (:127-143).
        var (_, rows) = await GetXAsync(keyWord: null, flagActive: "1", recordStart: 0, recordCount: 123456000);

        // Data là danh sách trần lst_Mst_Qualification; rỗng thì null (gate :126, :149-154).
        return Ok(new { Success = true, Data = rows.Count > 0 ? rows : null });
    }

    /// <summary>
    /// Lọc + đếm + chia trang theo QualificationCode asc — dùng cho Search (bài học A.3).
    /// Trả (itemCount, rows): itemCount = tổng số dòng sau lọc (MyCount, SQL :36625); rows = trang đã cắt kèm MyIdxSeq 0-based (:36615).
    /// </summary>
    private async Task<(int itemCount, List<object> rows)> GetXAsync(string? keyWord, string? flagActive, long recordStart, long recordCount)
    {
        var q = _db.Set<Mst_Qualification>().AsNoTracking().AsQueryable();

        // KeyWord -> like '%kw%' trên QualificationName (Mst_QualificationController.cs:176-179).
        // Nguồn chạy SQL Server collation KHÔNG phân biệt hoa/thường ⇒ hạ chữ cả 2 phía rồi LIKE (bẫy #18/#31).
        if (!string.IsNullOrWhiteSpace(keyWord))
        {
            var kw = PortQuery.Contains(keyWord);
            q = q.Where(x => EF.Functions.Like((x.QualificationName ?? "").ToLower(), kw, PortQuery.Esc));
        }

        // FlagActive -> toán tử '=' trên FlagActive (Mst_QualificationController.cs:180-183) — KHÔNG IN.
        // Nguồn dùng '=' ⇒ so sánh bằng chuỗi không phân biệt hoa/thường bằng Like + PortQuery.Equal (không tách dấu phẩy).
        if (!string.IsNullOrWhiteSpace(flagActive))
        {
            var fa = PortQuery.Equal(flagActive);
            q = q.Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), fa, PortQuery.Esc));
        }

        // Chia trang theo QualificationCode asc (Mst_Qualification_GetX SQL: MasterData.cs:36621 order by mq.QualificationCode asc).
        var ordered = q.OrderBy(x => x.QualificationCode);
        var itemCount = await ordered.CountAsync();

        // MyIdxSeq = số thứ tự 0-based trong thứ tự QualificationCode (identity(bigint, 0, 1) ở SQL nguồn :36615).
        // Lấy dòng [recordStart, recordStart+recordCount) theo thứ tự QualificationCode; không clamp âm (bài học A.12).
        var page = (recordCount <= 0 || recordStart < 0)
            ? new List<Mst_Qualification>()
            : await ordered.Skip((int)Math.Min(recordStart, int.MaxValue)).Take((int)Math.Min(recordCount, int.MaxValue)).ToListAsync();

        // Select cuối theo t.MyIdxSeq asc (MasterData.cs:36660 order by t.MyIdxSeq asc).
        // Mỗi dòng 8 trường = MyIdxSeq + 7 cột model Mst_Qualification (Mst_Qualification.cs:9-18).
        var rows = page
            .Select((x, i) => (object)new
            {
                MyIdxSeq = recordStart + i,
                x.QualificationCode,
                x.NetworkID,
                x.QualificationName,
                x.FlagActive,
                x.Remark,
                x.LogLUDTimeUTC,
                x.LogLUBy,
            })
            .ToList();

        return (itemCount, rows);
    }
}
