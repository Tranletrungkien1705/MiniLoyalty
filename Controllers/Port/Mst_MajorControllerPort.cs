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
// PORT 1:1 — màn danh mục chuyên ngành (Mst_Major) của dự án nguồn 2022.7.Loyalty (#168, #169).
//
// Nguồn controller (tầng gate):
//   Mst_MajorController.cs:25-105     Search -> WA_Mst_Major_Get
//   Mst_MajorController.cs:107-167    GetAllActive -> WA_Mst_Major_Get (#169)
//   Mst_MajorController.cs:171-186    strWhereClause_Mst_Major (mệnh đề lọc Search)
//   Mst_MajorController.cs:188-199    strWhereClause_Mst_Major_Active (mệnh đề lọc GetAllActive)
// Nguồn API:
//   MstMajorController.cs:17-88       WA_Mst_Major_Get
// Nguồn biz:
//   MasterData.cs:40476-40566         WAS_Mst_Major_Get
//   MasterData.cs:40346-40474         Mst_Major_Get
//   MasterData.cs:40189-40344         Mst_Major_GetX (SQL :40242-40295)
//
// GIỮ ĐÚNG NGUỒN:
//   * Endpoint là POST (nguồn [AcceptVerbs("POST")]).
//   * Lọc KeyWord: like '%kw%' trên MajorName (Mst_MajorController.cs:176-179).
//     CHỈ lọc MajorName — KHÔNG lọc MajorCode/MajorDesc/MajorGroupCode.
//   * Lọc FlagActive: toán tử '=' (Mst_MajorController.cs:180-183) — KHÔNG IN.
//     Không tách dấu phẩy; "0,1" -> so sánh bằng chuỗi "0,1" -> 0 dòng.
//   * OrgID truyền vào (:25, :55) nhưng strWhereClause_Mst_Major (:171-186) KHÔNG dùng OrgID
//     và SQL biz (:40244-40330) cũng KHÔNG lọc OrgID/NetworkID -> không lọc.
//   * Chia trang theo mm.MajorCode asc (MasterData.cs:40251), select cuối sắp theo t.MyIdxSeq asc (MasterData.cs:40290).
//   * MyIdxSeq 0-based identity (MasterData.cs:40245).
//   * PageSize = 0 -> DataList rỗng, PageCount = 0 (gate :80-88).
//   * DDL: khong co DDL trong repo — PK = MajorCode (suy từ join, DDL chưa xác minh).
//
// Bỏ qua có chủ đích (loại Đ):
//   - CheckGatewayAuthentication: gate :34, API :40-45
//   - CheckAuthorize: gate :35 (hệ thống đích chưa có hệ phân quyền)
//   - Tài khoản viết cứng GwUserCode/GwPassword tại gate :62-63 — KHÔNG chép giá trị (bài học A.10)
//   - Ghi RequestLog/ReturnLog: MasterData.cs:40386-40394, 40463-40471
//   - Sys_User_CheckAuthentication, Sys_Access_CheckDenyV30, drAbilityOfUser: MasterData.cs:40397-40408, 40228, 40240
// ============================================================================================

/// <summary>Port 1:1 <c>Mst_MajorSearch</c> — tham số lọc + phân trang của màn (Mst_MajorController.cs:25, :52-57).</summary>
public class MstMajorSearchReq
{
    public string? OrgID { get; set; }               // Tổ chức — gate nhận (:25, :55) nhưng strWhereClause_Mst_Major (:171-186) KHÔNG dùng OrgID -> không lọc
    public string? KeyWord { get; set; }             // Từ khoá tìm theo tên chuyên ngành (MajorName) — :25, :54, :176-179
    public string? FlagActive { get; set; }          // Cờ hoạt động, toán tử '=' — :25, :56, :180-183
    public int Ft_PageIndex { get; set; } = 0;       // Trang (0-based) — :25, :46
    public int Ft_PageSize { get; set; } = 10;       // Số dòng/trang — :25, :47
}

[ApiController]
[Route("api/mst_major")]
[Produces("application/json")]
public class Mst_MajorControllerPort : ControllerBase
{
    private readonly AppDbContext _db;

    public Mst_MajorControllerPort(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] MstMajorSearchReq? req)
    {
        var r = req ?? new MstMajorSearchReq();
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

    /// <summary>
    /// Port 1:1 <c>Mst_MajorController.GetAllActive</c> (nguồn gate Mst_MajorController.cs:107-167, #169).
    /// Nguồn gọi <c>strWhereClause_Mst_Major_Active</c> với <c>FlagActive = "1"</c> (Mst_MajorController.cs:127-130) ⇒ lọc
    /// <c>FlagActive = "1"</c> (:193-196, toán tử '=', KHÔNG IN, KHÔNG tách dấu phẩy).
    /// <c>OrgID</c>/<c>NetworkID</c> được đọc từ header (:118-119) và gán vào request (:139-140),
    /// nhưng hàm lọc (:188-199) và SQL biz (MasterData.cs:40244-40330) KHÔNG lọc OrgID/NetworkID ⇒ KHÔNG lọc.
    /// Không nhận tham số input; phân trang gửi từ gate: Ft_RecordStart = "0", Ft_RecordCount = "123456000" (gate :142-143, BaseController.cs:30-31).
    /// Tái sử dụng <see cref="GetXAsync"/> (bài học A.3): sắp xếp theo mm.MajorCode asc (MasterData.cs:40251),
    /// select cuối sắp theo t.MyIdxSeq asc (MasterData.cs:40290), mỗi dòng 9 trường (MyIdxSeq + 8 cột Mst_Major).
    /// Trả <c>Ok(new { Success = true, Data = ... })</c> — Data là danh sách trần List&lt;Mst_Major&gt;, KHÔNG bọc PageInfo;
    /// khi rỗng (0 dòng) trả <c>null</c>, KHÔNG phải <c>[]</c> (gate Mst_MajorController.cs:126, 149-154).
    /// </summary>
    [HttpPost("getallactive")]
    public async Task<IActionResult> GetAllActive()
    {
        // Nguồn: Ft_RecordStart = "0", Ft_RecordCount = "123456000" (gate Mst_MajorController.cs:142-143, BaseController.cs:30-31).
        // FlagActive = "1" (gate Mst_MajorController.cs:129, :195) ⇒ lọc toán tử '=' với giá trị "1"; không lọc KeyWord/OrgID/NetworkID.
        var (_, rows) = await GetXAsync(null, "1", 0, 123456000);

        // Nguồn Mst_MajorController.cs:126, 149-154: Data là danh sách trần, rỗng ⇒ null (KHÔNG phải []).
        return Ok(new { Success = true, Data = rows.Count > 0 ? rows : null });
    }

    /// <summary>
    /// Lọc + đếm + chia trang theo MajorCode asc — dùng cho Search (và các action sau tái sử dụng, bài học A.3).
    /// Trả (itemCount, rows): itemCount = tổng số dòng sau lọc (MyCount, SQL :40255); rows = trang đã cắt kèm MyIdxSeq 0-based (:40245).
    /// </summary>
    private async Task<(int itemCount, List<object> rows)> GetXAsync(string? keyWord, string? flagActive, long recordStart, long recordCount)
    {
        var q = _db.Set<Mst_Major>().AsNoTracking().AsQueryable();

        // KeyWord -> like '%kw%' trên MajorName (Mst_MajorController.cs:176-179).
        // Nguồn chạy SQL Server collation KHÔNG phân biệt hoa/thường ⇒ hạ chữ cả 2 phía rồi LIKE (bẫy #18/#31).
        if (!string.IsNullOrWhiteSpace(keyWord))
        {
            var kw = PortQuery.Contains(keyWord);
            q = q.Where(x => EF.Functions.Like((x.MajorName ?? "").ToLower(), kw, PortQuery.Esc));
        }

        // FlagActive -> toán tử '=' trên FlagActive (Mst_MajorController.cs:180-183) — KHÔNG IN.
        // Nguồn dùng '=' ⇒ so sánh bằng chuỗi không phân biệt hoa/thường bằng Like + PortQuery.Equal (không tách dấu phẩy).
        if (!string.IsNullOrWhiteSpace(flagActive))
        {
            var fa = PortQuery.Equal(flagActive);
            q = q.Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), fa, PortQuery.Esc));
        }

        // Chia trang theo MajorCode asc (Mst_Major_GetX SQL: MasterData.cs:40251 order by mm.MajorCode asc).
        var ordered = q.OrderBy(x => x.MajorCode);
        var itemCount = await ordered.CountAsync();

        // MyIdxSeq = số thứ tự 0-based trong thứ tự MajorCode (identity(bigint, 0, 1) ở SQL nguồn :40245).
        // Lấy dòng [recordStart, recordStart+recordCount) theo thứ tự MajorCode; không clamp âm (bài học A.12).
        var page = (recordCount <= 0 || recordStart < 0)
            ? new List<Mst_Major>()
            : await ordered.Skip((int)Math.Min(recordStart, int.MaxValue)).Take((int)Math.Min(recordCount, int.MaxValue)).ToListAsync();

        // Select cuối theo t.MyIdxSeq asc (MasterData.cs:40290 order by t.MyIdxSeq asc).
        // Mỗi dòng 9 trường = MyIdxSeq + 8 cột model Mst_Major (Mst_Major.cs:9-19).
        var rows = page
            .Select((x, i) => (object)new
            {
                MyIdxSeq = recordStart + i,
                x.MajorCode,
                x.NetworkID,
                x.MajorName,
                x.MajorDesc,
                x.MajorGroupCode,
                x.FlagActive,
                x.LogLUDTimeUTC,
                x.LogLUBy,
            })
            .ToList();

        return (itemCount, rows);
    }
}
