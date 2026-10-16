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
// PORT 1:1 — màn danh mục tình trạng gia đình (Mst_FamilyStatus) của dự án nguồn 2022.7.Loyalty (#152).
//
// Nguồn controller (tầng gate):
//   Mst_FamilyStatusController.cs:25-105   Search -> WA_Mst_FamilyStatus_Get
//   Mst_FamilyStatusController.cs:171-186  strWhereClause_Mst_FamilyStatus (mệnh đề lọc Search)
// Nguồn API:
//   MstFamilyStatusController.cs:17-88     WA_Mst_FamilyStatus_Get
// Nguồn biz:
//   MasterData.cs:42702-42792              WAS_Mst_FamilyStatus_Get
//   MasterData.cs:42572-42700              Mst_FamilyStatus_Get
//   MasterData.cs:42415-42570              Mst_FamilyStatus_GetX (SQL :42468-42519)
//
// GIỮ ĐÚNG NGUỒN:
//   * Endpoint là POST (nguồn [AcceptVerbs("POST")]).
//   * Lọc KeyWord: like '%kw%' trên FamilyStatusName (Mst_FamilyStatusController.cs:178).
//   * Lọc FlagActive: toán tử '=' (Mst_FamilyStatusController.cs:182) — KHÔNG IN.
//     Không tách dấu phẩy; "0,1" -> so sánh bằng chuỗi "0,1" -> 0 dòng.
//   * OrgID truyền vào (:25, :55) nhưng strWhereClause_Mst_FamilyStatus (:171-186) KHÔNG dùng OrgID -> không lọc.
//   * Chia trang theo mfs.FamilyStatusCode asc (MasterData.cs:42477), select cuối sắp theo t.MyIdxSeq asc (MasterData.cs:42516).
//   * MyIdxSeq 0-based identity (MasterData.cs:42471).
//   * PageSize = 0 -> DataList rỗng, PageCount = 0 (gate :83-90).
//   * DDL: khong co DDL trong repo — PK = FamilyStatusCode (khoa draft), kieu cot can xac nhan.
//
// Bỏ qua có chủ đích (loại Đ):
//   - CheckGatewayAuthentication: gate :34, API :40-45
//   - CheckAuthorize: gate :35 (hệ thống đích chưa có hệ phân quyền)
//   - Ghi RequestLog/ReturnLog: MasterData.cs:42613-42620, 42690-42697
// ============================================================================================

/// <summary>Port 1:1 <c>Mst_FamilyStatusSearch</c> — tham số lọc + phân trang của màn (Mst_FamilyStatusController.cs:25, 52-56).</summary>
public class MstFamilyStatusSearchReq
{
    public string? OrgID { get; set; }               // Tổ chức — gate nhận (:25, :55) nhưng strWhereClause_Mst_FamilyStatus (:171-186) KHÔNG dùng OrgID -> không lọc
    public string? KeyWord { get; set; }             // Từ khoá tìm theo tên tình trạng gia đình (FamilyStatusName) — :25, :54, :178
    public string? FlagActive { get; set; }          // Cờ hoạt động, toán tử '=' — :25, :56, :182
    public int Ft_PageIndex { get; set; } = 0;       // Trang (0-based) — :25, :46
    public int Ft_PageSize { get; set; } = 10;       // Số dòng/trang — :25, :47
}

[ApiController]
[Route("api/mst_familystatus")]
[Produces("application/json")]
public class Mst_FamilyStatusControllerPort : ControllerBase
{
    private readonly AppDbContext _db;

    public Mst_FamilyStatusControllerPort(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] MstFamilyStatusSearchReq? req)
    {
        var r = req ?? new MstFamilyStatusSearchReq();
        var pageIndex = Math.Max(0, r.Ft_PageIndex);
        var pageSize = r.Ft_PageSize;

        // Search gọi GetXAsync với start = PageIndex * PageSize, count = PageSize (giữ nguyên hành vi cũ nguồn :50-51, :75).
        var start = (long)pageIndex * pageSize;
        var (itemCount, rows) = await GetXAsync(r.KeyWord, r.FlagActive, start, pageSize);

        // PageSize=0 -> DataList rỗng (nguồn: nFilterRecordEnd = start-1, SQL biz + gate :83-90).
        // PageCount = 0 khi DataList rỗng, ngược lại ceil(ItemCount / ps) với ps = PageSize (PageSize=0 thì 10) — gate :83-90.
        var ps = pageSize == 0 ? 10 : pageSize;
        var pageCount = rows.Count == 0 ? 0 : (itemCount % ps == 0 ? itemCount / ps : itemCount / ps + 1);

        return Ok(new { Success = true, Data = new PageInfo<object>(rows, itemCount, pageCount, pageIndex, pageSize) });
    }

    /// <summary>
    /// Lọc + đếm + chia trang theo FamilyStatusCode asc — dùng cho Search.
    /// Trả (itemCount, rows): itemCount = tổng số dòng sau lọc (MyCount, SQL :42481); rows = trang đã cắt kèm MyIdxSeq 0-based (:42511).
    /// </summary>
    private async Task<(int itemCount, List<object> rows)> GetXAsync(string? keyWord, string? flagActive, long recordStart, long recordCount)
    {
        // Trên SQLite, hàm lower() mặc định chỉ hỗ trợ ký tự ASCII, không hạ chữ được ký tự tiếng Việt (vd 'Đ' -> 'đ').
        // Đăng ký custom lower function trên SqliteConnection nếu là SQLite để tìm kiếm tiếng Việt không phân biệt hoa/thường (bẫy #18/#31).
        var conn = _db.Database.GetDbConnection();
        if (conn is Microsoft.Data.Sqlite.SqliteConnection sqliteConn)
        {
            sqliteConn.CreateFunction("lower", (string? s) => s == null ? "" : s.ToLowerInvariant());
        }

        var q = _db.Set<Mst_FamilyStatus>().AsNoTracking().AsQueryable();

        // KeyWord -> like '%kw%' trên FamilyStatusName (Mst_FamilyStatusController.cs:178).
        // Nguồn chạy SQL Server collation KHÔNG phân biệt hoa/thường ⇒ hạ chữ cả 2 phía rồi LIKE (bẫy #18/#31).
        if (!string.IsNullOrWhiteSpace(keyWord))
        {
            var kw = PortQuery.Contains(keyWord);
            q = q.Where(x => EF.Functions.Like((x.FamilyStatusName ?? "").ToLower(), kw, PortQuery.Esc));
        }

        // FlagActive -> toán tử '=' trên FlagActive (Mst_FamilyStatusController.cs:182) — KHÔNG IN.
        // Nguồn dùng '=' ⇒ so sánh bằng chuỗi không phân biệt hoa/thường bằng Like + PortQuery.Equal (không tách dấu phẩy).
        if (!string.IsNullOrWhiteSpace(flagActive))
        {
            var fa = PortQuery.Equal(flagActive);
            q = q.Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), fa, PortQuery.Esc));
        }

        // Chia trang theo FamilyStatusCode asc (Mst_FamilyStatus_GetX SQL: MasterData.cs:42477 order by mfs.FamilyStatusCode asc).
        var ordered = q.OrderBy(x => x.FamilyStatusCode);
        var itemCount = await ordered.CountAsync();

        // MyIdxSeq = số thứ tự 0-based trong thứ tự FamilyStatusCode (identity(bigint, 0, 1) ở SQL nguồn :42471).
        // Lấy dòng [recordStart, recordStart+recordCount) theo thứ tự FamilyStatusCode.
        var page = recordCount <= 0
            ? new List<Mst_FamilyStatus>()
            : await ordered.Skip((int)recordStart).Take((int)recordCount).ToListAsync();

        // Select cuối theo t.MyIdxSeq asc (MasterData.cs:42516 order by t.MyIdxSeq asc).
        var rows = page
            .Select((x, i) => (object)new
            {
                MyIdxSeq = recordStart + i,
                x.FamilyStatusCode,
                x.NetworkID,
                x.FamilyStatusName,
                x.FamilyStatusDesc,
                x.FlagActive,
                x.LogLUDTimeUTC,
                x.LogLUBy,
            })
            .ToList();

        return (itemCount, rows);
    }
}
