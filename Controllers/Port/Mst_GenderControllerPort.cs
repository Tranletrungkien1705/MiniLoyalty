using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniLoyalty.Data;
using MiniLoyalty.Models.Port;

namespace MiniLoyalty.Controllers.Port;

// ============================================================================================
// PORT 1:1 — màn danh mục giới tính (Mst_Gender) của dự án nguồn 2022.7.Loyalty (#154).
//
// Nguồn controller (tầng gate):
//   Mst_GenderController.cs:25-105    Search -> WA_Mst_Gender_Get
//   Mst_GenderController.cs:171-195   strWhereClause_Mst_Gender (mệnh đề lọc Search)
// Nguồn API:
//   MstGenderController.cs:20-89      WA_Mst_Gender_Get
// Nguồn biz:
//   MasterData.cs:29670-29768         WAS_Mst_Gender_Get
//   MasterData.cs:29770-29940         Mst_Gender_Get
//   MasterData.cs:29942-30095         Mst_Gender_GetX (bản LIVE)
//
// ĐẶC ĐIỂM VÀ QUY TẮC NGHIỆP VỤ NGUỒN:
//   * Endpoint là POST (nguồn gate Mst_GenderController.cs:25).
//   * Body: { OrgID, KeyWord, FlagActive, Ft_PageIndex = 0, Ft_PageSize = 10 } (gate :25).
//   * Lọc KeyWord: `Gender like '%kw%'` — cột tên là Gender, KHÔNG phải GenderName (gate :178).
//   * Lọc FlagActive: danh sách tách dấu phẩy `FlagActive IN ('1','0')` (gate :184-192);
//     dòng `=` (:180-183) BỊ COMMENT ở nguồn nên KHÔNG chạy. KHÁC Ethnic/Discipline (dùng `=`).
//   * OrgID: gate nhận (:25, :55) nhưng strWhereClause_Mst_Gender (:171-195) KHÔNG dùng OrgID -> KHÔNG lọc.
//   * Phân trang: Ft_RecordStart = pageIndex * pageSize, Ft_RecordCount = pageSize (gate :50-51, :69-70).
//     MyIdxSeq 0-based identity (SQL MasterData.cs:29996).
//     Thứ tự draft: `order by mg.GenderCode asc` (:30002). Select cuối sắp theo t.MyIdxSeq asc (:30041).
//   * PageSize = 0 -> DataList rỗng (nFilterRecordEnd = start-1), PageCount = 0 (gate :81-89).
//   * DDL: khong co DDL trong repo — PK = GenderCode (khoá draft), kieu cot can xac nhan.
//
// Bỏ qua có chủ đích (loại Đ):
//   * CheckGatewayAuthentication: gate :34, API :40-46 (hệ thống đích chưa có gateway auth).
//   * CheckAuthorize: gate :35 (hệ thống đích chưa có hệ phân quyền).
//   * Sys_User_CheckAuthorize: MasterData.cs:29844-29857.
//   * Ghi RequestLog/ReturnLog: MasterData.cs:29825-29832, 29929-29937.
// ============================================================================================

/// <summary>Port 1:1 <c>Mst_GenderSearch</c> — tham số lọc + phân trang của màn (Mst_GenderController.cs:25, 52-57).</summary>
public class MstGenderSearchReq
{
    public string? OrgID { get; set; }               // Tổ chức — gate nhận (:25, :55) nhưng strWhereClause_Mst_Gender (:171-195) KHÔNG dùng OrgID -> không lọc
    public string? KeyWord { get; set; }             // Từ khoá tìm theo tên giới tính (Gender) — :25, :54, :178
    public string? FlagActive { get; set; }          // Cờ hoạt động, danh sách phân cách dấu phẩy (toán tử IN) — :25, :56, :184-192
    public int Ft_PageIndex { get; set; } = 0;       // Trang (0-based) — :25, :46
    public int Ft_PageSize { get; set; } = 10;       // Số dòng/trang — :25, :48
}

[ApiController]
[Route("api/mst_gender")]
[Produces("application/json")]
public class Mst_GenderControllerPort : ControllerBase
{
    private readonly AppDbContext _db;

    public Mst_GenderControllerPort(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] MstGenderSearchReq? req)
    {
        var r = req ?? new MstGenderSearchReq();
        var pageIndex = r.Ft_PageIndex;
        var pageSize = r.Ft_PageSize;

        // Search gọi GetXAsync với start = PageIndex * PageSize, count = PageSize (giữ nguyên hành vi nguồn :50-51, :69-70).
        var start = (long)pageIndex * pageSize;
        var (itemCount, rows) = await GetXAsync(r.KeyWord, r.FlagActive, start, pageSize);

        // PageSize=0 -> DataList rỗng (nguồn: nFilterRecordEnd = start-1, SQL biz + gate :81-89).
        // PageCount = 0 khi DataList rỗng, ngược lại ceil(ItemCount / ps) với ps = PageSize (PageSize=0 thì 10) — gate :81-89.
        var ps = pageSize == 0 ? 10 : pageSize;
        var pageCount = rows.Count == 0 ? 0 : (itemCount % ps == 0 ? itemCount / ps : itemCount / ps + 1);

        return Ok(new { Success = true, Data = new PageInfo<object>(rows, itemCount, pageCount, pageIndex, pageSize) });
    }

    /// <summary>
    /// Lọc + đếm + chia trang theo GenderCode asc — dùng cho Search (và GetAllActive sau này).
    /// Trả (itemCount, rows): itemCount = tổng số dòng sau lọc (MyCount, SQL :30006); rows = trang đã cắt kèm MyIdxSeq 0-based (:30035-30041).
    /// </summary>
    private async Task<(int itemCount, List<object> rows)> GetXAsync(string? keyWord, string? flagActive, long recordStart, long recordCount)
    {
        // lower() Unicode tren SQLite duoc dang ky MOT LAN o ha tang (Data/SqliteUnicodeLower.cs) - khong dang ky trong action.
        var q = _db.Set<Mst_Gender>().AsNoTracking().AsQueryable();

        // KeyWord -> like '%kw%' trên Gender (Mst_GenderController.cs:178).
        // Nguồn chạy SQL Server collation KHÔNG phân biệt hoa/thường ⇒ hạ chữ cả 2 phía rồi LIKE (bẫy #18/#31).
        if (!string.IsNullOrWhiteSpace(keyWord))
        {
            var kw = PortQuery.Contains(keyWord);
            q = q.Where(x => EF.Functions.Like((x.Gender ?? "").ToLower(), kw, PortQuery.Esc));
        }

        // FlagActive -> in ('a','b') (Mst_GenderController.cs:184-192): tách dấu phẩy, bỏ khoảng trắng, hạ chữ.
        // Dòng `=` (:180-183) BỊ COMMENT ở nguồn nên KHÔNG dùng. KHÁC Ethnic/Discipline (dùng `=`).
        if (!string.IsNullOrWhiteSpace(flagActive))
        {
            var ds = flagActive.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => s.ToLowerInvariant())
                .ToList();
            if (ds.Count > 0)
            {
                q = q.Where(x => ds.Contains((x.FlagActive ?? "").ToLower()));
            }
        }

        // Chia trang theo GenderCode asc (Mst_Gender_GetX SQL: MasterData.cs:30002 order by mg.GenderCode asc).
        var ordered = q.OrderBy(x => x.GenderCode);
        var itemCount = await ordered.CountAsync();

        // MyIdxSeq = số thứ tự 0-based trong thứ tự GenderCode (identity(bigint, 0, 1) ở SQL nguồn :29996).
        // Lấy dòng [recordStart, recordStart+recordCount) theo thứ tự GenderCode.
        var page = (recordCount <= 0 || recordStart < 0)
            ? new List<Mst_Gender>()
            : await ordered.Skip((int)Math.Min(recordStart, int.MaxValue)).Take((int)Math.Min(recordCount, int.MaxValue)).ToListAsync();

        // Select cuối theo t.MyIdxSeq asc (MasterData.cs:30041 order by t.MyIdxSeq asc).
        var rows = page
            .Select((x, i) => (object)new
            {
                MyIdxSeq = recordStart + i,
                x.GenderCode,
                x.NetworkID,
                x.OrgID,
                x.Gender,
                x.FlagActive,
                x.LogLUDTimeUTC,
                x.LogLUBy,
            })
            .ToList();

        return (itemCount, rows);
    }
}
