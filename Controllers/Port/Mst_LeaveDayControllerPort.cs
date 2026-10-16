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
// PORT 1:1 — màn danh mục ngày nghỉ phép (Mst_LeaveDay) của dự án nguồn 2022.7.Loyalty (#164).
//
// Nguồn controller (tầng gate):
//   Mst_LeaveDayController.cs:25-105   Search -> WA_Mst_LeaveDay_Get
//   Mst_LeaveDayController.cs:171-186  strWhereClause_Mst_LeaveDay (mệnh đề lọc Search)
// Nguồn API:
//   MstLeaveDayController.cs:17-88     WA_Mst_LeaveDay_Get
// Nguồn biz:
//   MasterData.cs:30453-30543          WAS_Mst_LeaveDay_Get
//   MasterData.cs:30323-30451          Mst_LeaveDay_Get (transaction luôn Rollback :30414, :30422, :30437 - hàm đọc, không ghi DB)
//   MasterData.cs:30166-30321          Mst_LeaveDay_GetX (SQL :30219-30270)
// Nguồn model (từ điển cột):
//   Mst_LeaveDay.cs:9-21               10 thuộc tính (LeaveDayCodeSys, NetworkID, NormalLeaveDay, MinDevotedYear, LeaveDaySpecial, LeaveDayJob, FlagActive, Remark, LogLUDTimeUTC, LogLUBy)
//
// GIỮ ĐÚNG NGUỒN:
//   * Endpoint là POST (nguồn [AcceptVerbs("POST")]).
//   * Lọc KeyWord: like '%kw%' trên NormalLeaveDay (Mst_LeaveDayController.cs:176-179). CHỈ tìm NormalLeaveDay, KHÔNG tìm LeaveDayCodeSys, Remark.
//   * Lọc FlagActive: toán tử '=' nguyên giá trị StrValue (Mst_LeaveDayController.cs:180-183), KHÔNG tách dấu phẩy, KHÔNG phải IN.
//   * OrgID: gate nhận OrgID (:25, :55) nhưng hàm lọc strWhereClause_Mst_LeaveDay (:171-186) KHÔNG dùng -> OrgID KHÔNG lọc gì.
//   * SQL không lọc NetworkID/OrgID trong WHERE (MasterData.cs:30219-30306).
//   * Chuẩn hoá: gate KHÔNG gọi StdParam/StdStr/StdFlag cho KeyWord/FlagActive (:176-183) -> không Trim/ToUpper.
//   * SQL Server không phân biệt hoa/thường -> dùng Like trên ToLower() kèm escape '\'.
//   * Draft: select distinct identity(bigint,0,1) MyIdxSeq, mld.LeaveDayCodeSys từ Mst_LeaveDay sắp xếp mld.LeaveDayCodeSys asc (MasterData.cs:30220-30229).
//   * MyCount: Count(0) của draft trước khi cắt trang (MasterData.cs:30232).
//   * Cắt trang: MyIdxSeq >= start và MyIdxSeq <= end với end = start + count - 1 (MasterData.cs:30200-30201, :30235-30243).
//   * Theo bài học A.12: KHÔNG clamp Ft_PageIndex âm về 0 (recordCount <= 0 || recordStart < 0 -> trang rỗng, PageCount = 0, giữ nguyên PageIndex đã gửi).
//   * Select cuối: inner join draft đã cắt trang với Mst_LeaveDay theo LeaveDayCodeSys, sắp xếp theo t.MyIdxSeq asc (MasterData.cs:30259-30269).
//   * Mỗi dòng trả đúng 11 cột: MyIdxSeq + 10 cột model. MyIdxSeq tính lại từ 0 sau khi lọc.
//   * PageCount: CHỈ tính khi DataList KHÔNG rỗng (Mst_LeaveDayController.cs:80-88). Khi DataList rỗng -> PageCount = 0.
//   * DataList rỗng là mảng rỗng `[]`, KHÔNG null (Mst_LeaveDayController.cs:48-51 new List<TModels.Mst_LeaveDay>()).
//   * PK = LeaveDayCodeSys (suy từ join/distinct MasterData.cs:30223, :30266 và Mst_LeaveDay_CheckDB :30114, DDL chưa xác minh).
//
// Bỏ qua có chủ đích (loại Đ):
//   * CheckGatewayAuthentication / CheckAuthorize (gate Mst_LeaveDayController.cs:34-35),
//     Sys_User_CheckAuthentication (:30374-30378), Sys_Access_CheckDenyV30 (:30381-30385), drAbilityOfUser (:30205, :30217)
//     đều bị COMMENT ở nguồn -> KHÔNG port (hệ thống đích chưa có hệ phân quyền).
//   * Ghi RequestLog / ReturnLog (MasterData.cs:30364-30371, :30441-30448).
//   * Transaction: Mst_LeaveDay_Get mở BeginTransaction (:30361) nhưng luôn Rollback (:30414, :30422, :30437) vì là hàm đọc.
// ============================================================================================

/// <summary>Port 1:1 <c>Mst_LeaveDaySearch</c> — tham số lọc + phân trang của màn (Mst_LeaveDayController.cs:25, :52-57).</summary>
public class MstLeaveDaySearchReq
{
    public string? OrgID { get; set; }               // Mã tổ chức — gate nhận (:25, :55) nhưng strWhereClause_Mst_LeaveDay (:171-186) KHÔNG lọc OrgID
    public string? KeyWord { get; set; }             // Từ khoá tìm kiếm theo số ngày phép thông thường (NormalLeaveDay) — :176-179 (KHÔNG tìm LeaveDayCodeSys, Remark)
    public string? FlagActive { get; set; }          // Cờ hoạt động — toán tử '=' nguyên giá trị, KHÔNG tách dấu phẩy — :180-183
    public int Ft_PageIndex { get; set; } = 0;       // Trang (0-based) — :25, :46
    public int Ft_PageSize { get; set; } = 10;       // Số dòng/trang — :25, :47
}

[ApiController]
[Route("api/mst_leaveday")]
[Produces("application/json")]
public class Mst_LeaveDayControllerPort : ControllerBase
{
    private readonly AppDbContext _db;

    public Mst_LeaveDayControllerPort(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Port 1:1 <c>Mst_LeaveDayController.Search</c> (nguồn gate Mst_LeaveDayController.cs:25-105).
    /// </summary>
    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] MstLeaveDaySearchReq? req)
    {
        var r = req ?? new MstLeaveDaySearchReq();
        var pageIndex = r.Ft_PageIndex;
        var pageSize = r.Ft_PageSize;

        // Search gọi GetXAsync với start = PageIndex * PageSize, count = PageSize (hành vi nguồn Mst_LeaveDayController.cs:69-70).
        var start = (long)pageIndex * pageSize;
        var (itemCount, rows) = await GetXAsync(r.KeyWord, r.FlagActive, start, pageSize);

        // PageSize = 0 -> DataList rỗng (nguồn: nFilterRecordEnd = start - 1, SQL biz MasterData.cs:30200-30201, gate Mst_LeaveDayController.cs:80-88).
        // PageCount chỉ tính khi DataList KHÔNG rỗng (gate :80-88): ps = PageSize (nếu PageSize=0 thì lấy PageSizeConfig=10) -> (itemCount % ps == 0) ? itemCount / ps : itemCount / ps + 1.
        // Khi DataList rỗng -> PageCount = 0.
        var ps = pageSize == 0 ? 10 : pageSize;
        var pageCount = rows.Count == 0 ? 0 : (itemCount % ps == 0 ? itemCount / ps : itemCount / ps + 1);

        // Trả kết quả theo cấu trúc JsonResultEntry và PageInfo (gate :48-50, :91-92). DataList rỗng là mảng rỗng `[]`, KHÔNG null.
        return Ok(new
        {
            Success = true,
            Data = new PageInfo<object>(rows, itemCount, pageCount, pageIndex, pageSize)
        });
    }

    private static string EscapeLike(string s) => s.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    /// <summary>
    /// Lọc + đếm + chia trang theo LeaveDayCodeSys asc — dùng cho Search.
    /// Triển khai ĐÚNG 2 bước nghiệp vụ nguồn (MasterData.cs:30166-30321 Mst_LeaveDay_GetX):
    /// 1. Draft distinct LeaveDayCodeSys sắp xếp theo LeaveDayCodeSys asc + MyIdxSeq 0-based. MyCount đếm số dòng draft.
    /// 2. Cắt trang theo MyIdxSeq [start, end], inner join lại với Mst_LeaveDay theo LeaveDayCodeSys và sắp xếp theo t.MyIdxSeq asc.
    /// Trả về đúng 11 cột: MyIdxSeq + 10 cột model nguồn.
    /// </summary>
    private async Task<(int itemCount, List<object> rows)> GetXAsync(string? keyWord, string? flagActive, long recordStart, long recordCount)
    {
        var q = _db.Set<Mst_LeaveDay>().AsNoTracking().AsQueryable();

        // 1. Điều kiện lọc:
        // KeyWord -> like '%kw%' trên NormalLeaveDay (Mst_LeaveDayController.cs:176-179).
        // Nguồn chạy SQL Server collation không phân biệt hoa/thường -> hạ chữ ToLower() cả hai phía và LIKE escape '\'.
        // CẢNH BÁO: KeyWord CHỈ lọc trên NormalLeaveDay, KHÔNG lọc trên LeaveDayCodeSys hay Remark.
        // Nguồn không gọi StdParam/StdStr -> KHÔNG Trim/ToUpper.
        if (!string.IsNullOrEmpty(keyWord))
        {
            var kwLower = "%" + EscapeLike(keyWord.ToLower()) + "%";
            q = q.Where(x => EF.Functions.Like((x.NormalLeaveDay != null ? x.NormalLeaveDay.ToString()! : "").ToLower(), kwLower, "\\"));
        }

        // FlagActive -> toán tử '=' (Mst_LeaveDayController.cs:180-183).
        // Giữ nguyên giá trị StrValue, KHÔNG tách dấu phẩy, KHÔNG phải IN, KHÔNG Trim/ToUpper/StdFlag.
        // LIKE không wildcard biểu diễn '=' không phân biệt hoa/thường.
        if (!string.IsNullOrEmpty(flagActive))
        {
            var faLower = EscapeLike(flagActive.ToLower());
            q = q.Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), faLower, "\\"));
        }

        // OrgID: gate nhận OrgID (Mst_LeaveDayController.cs:25, :55) nhưng strWhereClause_Mst_LeaveDay (:171-186)
        // KHÔNG dùng -> OrgID KHÔNG lọc gì. Header OrgID/NetworkID cũng không vào WHERE (SQL không có NetworkID).

        // Bước 1: Draft (MasterData.cs:30220-30229):
        // select distinct identity(bigint, 0, 1) MyIdxSeq, mld.LeaveDayCodeSys
        // into #tbl_Mst_LeaveDay_Filter_Draft from Mst_LeaveDay mld where (1=1) <loc> order by mld.LeaveDayCodeSys asc
        var draftKeys = await q
            .Select(x => x.LeaveDayCodeSys)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();

        // MyCount: Count(0) của draft trước khi cắt trang (MasterData.cs:30232)
        var itemCount = draftKeys.Count;

        // Cắt trang: MyIdxSeq >= start và MyIdxSeq <= end với end = start + count - 1 (MasterData.cs:30200-30201, :30235-30243).
        // Theo bài học A.12: recordCount <= 0 || recordStart < 0 -> trang rỗng, không clamp.
        var pagedDraft = (recordCount <= 0 || recordStart < 0)
            ? new List<(long MyIdxSeq, string LeaveDayCodeSys)>()
            : draftKeys
                .Select((k, idx) => (MyIdxSeq: (long)idx, LeaveDayCodeSys: k))
                .Where(t => t.MyIdxSeq >= recordStart && t.MyIdxSeq <= recordStart + recordCount - 1)
                .ToList();

        if (pagedDraft.Count == 0)
        {
            return (itemCount, new List<object>());
        }

        var pagedKeys = pagedDraft.Select(t => t.LeaveDayCodeSys).ToList();

        // Bước 2: Select cuối INNER JOIN (MasterData.cs:30259-30269):
        // select t.MyIdxSeq, mld.* from #tbl_Mst_LeaveDay_Filter t inner join Mst_LeaveDay mld on t.LeaveDayCodeSys = mld.LeaveDayCodeSys order by t.MyIdxSeq asc
        // Mỗi dòng trả đúng 11 cột: MyIdxSeq + 10 cột model.
        var entities = await _db.Set<Mst_LeaveDay>().AsNoTracking()
            .Where(x => pagedKeys.Contains(x.LeaveDayCodeSys))
            .ToListAsync();

        var entityDict = entities.ToDictionary(x => x.LeaveDayCodeSys, StringComparer.OrdinalIgnoreCase);

        var rows = pagedDraft
            .Where(t => entityDict.ContainsKey(t.LeaveDayCodeSys))
            .OrderBy(t => t.MyIdxSeq)
            .Select(t =>
            {
                var mld = entityDict[t.LeaveDayCodeSys];
                return (object)new
                {
                    t.MyIdxSeq,
                    mld.LeaveDayCodeSys,
                    mld.NetworkID,
                    mld.NormalLeaveDay,
                    mld.MinDevotedYear,
                    mld.LeaveDaySpecial,
                    mld.LeaveDayJob,
                    mld.FlagActive,
                    mld.Remark,
                    mld.LogLUDTimeUTC,
                    mld.LogLUBy,
                };
            })
            .ToList();

        return (itemCount, rows);
    }
}
