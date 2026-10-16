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
// PORT 1:1 — màn danh mục loại nghỉ phép (Mst_LeaveType) của dự án nguồn 2022.7.Loyalty (#166, #167).
//
// Nguồn controller (tầng gate):
//   Mst_LeaveTypeController.cs:25-105         Search -> WA_Mst_LeaveType_Get (#166)
//   Mst_LeaveTypeController.cs:107-167        GetAllActive -> WA_Mst_LeaveType_Get (#167)
//   Mst_LeaveTypeController.cs:171-186        strWhereClause_Mst_LeaveType (hàm lọc Search)
//   Mst_LeaveTypeController.cs:188-199        strWhereClause_Mst_LeaveType_Active (hàm lọc GetAllActive)
// Nguồn API:
//   MstLeaveTypeController.cs:17-88           WA_Mst_LeaveType_Get (mã lỗi mặc định "WA_Mst_LeaveType_Get" :25)
// Nguồn biz:
//   MasterData.cs:31878-31968                 WAS_Mst_LeaveType_Get (mã lỗi mặc định TError.ErridnLoyalty.WAS_Mst_LeaveType_Get :31895)
//   MasterData.cs:31748-31876                 Mst_LeaveType_Get (mã lỗi mặc định TError.ErridnLoyalty.Mst_LeaveType_Get :31769)
//   MasterData.cs:31591-31746                 Mst_LeaveType_GetX (SQL 2 bước :31644-31695)
// Nguồn model (từ điển cột):
//   12.Dev.Common\idn.Loyalty.Common\Models\HRM\Mst_LeaveType.cs:11-19 (đúng 9 cột model)
//
// GIỮ ĐÚNG NGUỒN:
//   * Endpoint là POST (nguồn gate Mst_LeaveTypeController.cs:25, :107).
//   * Lọc KeyWord: like '%kw%' trên LeaveType (Mst_LeaveTypeController.cs:176-179).
//     CẢNH BÁO: KeyWord CHỈ lọc trên cột LeaveType, KHÔNG lọc LeaveTypeCode hay LeaveTypeDesc.
//   * Lọc FlagActive: toán tử '=' nguyên chuỗi (Mst_LeaveTypeController.cs:180-183, :195) — KHÔNG IN, KHÔNG tách dấu phẩy.
//     "0,1" -> so sánh bằng chuỗi "0,1" -> 0 dòng. GetAllActive truyền cứng "1" (:129).
//   * OrgID: gate nhận OrgID (:25, :55) và đọc header NetworkID/OrgID (:36-37, :66-67, :118-119),
//     nhưng strWhereClause_Mst_LeaveType (:171-186), strWhereClause_Mst_LeaveType_Active (:188-199)
//     và SQL biz KHÔNG dùng OrgID hay NetworkID để lọc -> không lọc.
//   * Chuẩn hoá: gate chỉ gọi CUtils.StrValue (:178, :182, :195), KHÔNG gọi StdParam/StdStr/StdFlag trên KeyWord hay FlagActive.
//   * Chia trang theo mlt.LeaveTypeCode asc (MasterData.cs:31653), select cuối sắp theo t.MyIdxSeq asc (MasterData.cs:31692).
//   * MyIdxSeq 0-based identity (MasterData.cs:31647).
//   * GetAllActive: trả danh sách phẳng List<Mst_LeaveType> khi có dữ liệu, trả Data = null khi rỗng (Mst_LeaveTypeController.cs:126, 149-154), không bọc PageInfo.
//   * Search: PageSize = 0 -> DataList rỗng `[]`, PageCount = 0, ItemCount = MyCount (gate :80-88).
//   * DDL: không có DDL trong repo (source_excerpt.txt:113) -> PK = LeaveTypeCode suy từ draft distinct và inner join
//     (MasterData.cs:31648, :31691), DDL chưa xác minh.
//   * Transaction: Mst_LeaveType_Get mở transaction (:31786) nhưng luôn RollbackSafety (:31839, :31847, :31862)
//     vì chỉ là hàm đọc -> port không mở transaction.
//   * Tài khoản viết cứng tại Mst_LeaveTypeController.cs:62-63, :135-136 (GwUserCode, GwPassword) — CẤM chép giá trị.
//
// Bỏ qua có chủ đích (loại Đ):
//   * CheckGatewayAuthentication (:34, :116), CheckAuthorize (:35, :117) — hệ thống đích chưa có hệ phân quyền.
//   * Sys_User_CheckAuthentication (:31801-31805), Sys_Access_CheckDenyV30 (:31808-31810), drAbilityOfUser (:31630, :31642).
//   * Ghi RequestLog/ReturnLog: MasterData.cs:31789-31796, 31866-31873.
// ============================================================================================

/// <summary>Port 1:1 <c>Mst_LeaveTypeSearch</c> — tham số lọc + phân trang (Mst_LeaveTypeController.cs:25, :52-57).</summary>
public class MstLeaveTypeSearchReq
{
    public string? OrgID { get; set; }               // Tổ chức — gate nhận (:25, :55) nhưng strWhereClause_Mst_LeaveType (:171-186) và SQL biz KHÔNG dùng OrgID -> không lọc
    public string? KeyWord { get; set; }             // Từ khoá tìm kiếm theo tên loại nghỉ phép (LeaveType) — :176-179 (KHÔNG lọc LeaveTypeCode, LeaveTypeDesc)
    public string? FlagActive { get; set; }          // Cờ hoạt động — toán tử '=' nguyên giá trị, KHÔNG tách dấu phẩy — :180-183
    public int Ft_PageIndex { get; set; } = 0;       // Trang (0-based) — :25, :46
    public int Ft_PageSize { get; set; } = 10;       // Số dòng/trang — :25, :47
}

[ApiController]
[Route("api/mst_leavetype")]
[Produces("application/json")]
public class Mst_LeaveTypeControllerPort : ControllerBase
{
    private readonly AppDbContext _db;

    public Mst_LeaveTypeControllerPort(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Port 1:1 <c>Mst_LeaveTypeController.Search</c> (nguồn gate Mst_LeaveTypeController.cs:25-105).
    /// </summary>
    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] MstLeaveTypeSearchReq? req)
    {
        var r = req ?? new MstLeaveTypeSearchReq();
        var pageIndex = r.Ft_PageIndex;
        var pageSize = r.Ft_PageSize;

        // Search gọi GetXAsync với start = (long)pageIndex * pageSize, count = pageSize (hành vi nguồn Mst_LeaveTypeController.cs:69-70).
        var start = (long)pageIndex * pageSize;
        var (itemCount, rows) = await GetXAsync(r.KeyWord, r.FlagActive, start, pageSize);

        // PageSize = 0 -> DataList rỗng (nguồn: nFilterRecordEnd = start - 1, SQL biz MasterData.cs:31625-31626, gate Mst_LeaveTypeController.cs:80-88).
        // PageCount chỉ tính khi DataList KHÔNG rỗng (gate Mst_LeaveTypeController.cs:80-88):
        // if (pageSize == 0) pageSize = PageSizeConfig; pageCount = (itemCount % pageSize == 0) ? itemCount / pageSize : itemCount / pageSize + 1;
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

    /// <summary>
    /// Port 1:1 <c>Mst_LeaveTypeController.GetAllActive</c> (nguồn gate Mst_LeaveTypeController.cs:107-167).
    /// Chuỗi gọi: Mst_LeaveTypeController.GetAllActive (gate :107-167) -> MstLeaveTypeController.WA_Mst_LeaveType_Get (API :17-88)
    /// -> MasterData.WAS_Mst_LeaveType_Get (biz :31878-31968) -> MasterData.Mst_LeaveType_Get (biz :31748-31876) -> MasterData.Mst_LeaveType_GetX (biz :31591-31746).
    /// Lọc FlagActive = "1" qua hàm strWhereClause_Mst_LeaveType_Active (Mst_LeaveTypeController.cs:188-199).
    /// Tái sử dụng GetXAsync(null, "1", 0, 123456000) (Ft_RecordStart = "0", Ft_RecordCount = "123456000" tại :142-143).
    /// Khi có dữ liệu trả về mảng phẳng; khi rỗng trả Data = null (Mst_LeaveTypeController.cs:126, 149-154), không bọc PageInfo.
    /// Tài khoản viết cứng tại Mst_LeaveTypeController.cs:135-136 (GwUserCode, GwPassword) — CẤM chép giá trị.
    /// </summary>
    [HttpPost("getallactive")]
    public async Task<IActionResult> GetAllActive()
    {
        var (_, rows) = await GetXAsync(null, "1", 0, 123456000);
        return Ok(new
        {
            Success = true,
            Data = rows.Count > 0 ? rows : null
        });
    }

    /// <summary>
    /// Hàm dùng chung GetXAsync: Lọc + đếm + chia trang theo LeaveTypeCode asc.
    /// Để các action sau (GetByLeaveTypeCode, GetAllActive...) tái sử dụng.
    /// Triển khai ĐÚNG 2 bước nghiệp vụ nguồn (MasterData.cs:31591-31746 Mst_LeaveType_GetX):
    /// 1. Draft distinct LeaveTypeCode sắp xếp theo LeaveTypeCode asc + MyIdxSeq 0-based. MyCount đếm số dòng draft (SQL :31644-31658).
    /// 2. Cắt trang theo MyIdxSeq [start, end], inner join lại với Mst_LeaveType theo LeaveTypeCode và sắp xếp theo t.MyIdxSeq asc (SQL :31660-31695).
    /// Trả về đúng 10 trường: MyIdxSeq (0-based) + 9 cột model nguồn Mst_LeaveType (Mst_LeaveType.cs:11-19).
    /// </summary>
    private async Task<(int itemCount, List<object> rows)> GetXAsync(string? keyWord, string? flagActive, long recordStart, long recordCount)
    {
        var q = _db.Set<Mst_LeaveType>().AsNoTracking().AsQueryable();

        // 1. Điều kiện lọc:
        // KeyWord -> like '%kw%' trên LeaveType (Mst_LeaveTypeController.cs:176-179).
        // CẢNH BÁO: KeyWord CHỈ lọc trên cột LeaveType, KHÔNG lọc trên LeaveTypeCode hay LeaveTypeDesc.
        // Nguồn chạy SQL Server collation không phân biệt hoa/thường -> hạ chữ ToLower() cả hai phía và LIKE escape '\'.
        // Nguồn không gọi StdParam/StdStr -> KHÔNG Trim/ToUpper.
        if (!string.IsNullOrWhiteSpace(keyWord))
        {
            var kw = PortQuery.Contains(keyWord);
            q = q.Where(x => EF.Functions.Like((x.LeaveType ?? "").ToLower(), kw, PortQuery.Esc));
        }

        // FlagActive -> toán tử '=' trên FlagActive (Mst_LeaveTypeController.cs:180-183) — KHÔNG IN, KHÔNG tách dấu phẩy.
        // Giữ nguyên giá trị StrValue, KHÔNG tách dấu phẩy, KHÔNG phải IN, KHÔNG Trim/ToUpper/StdFlag.
        // "0,1" -> so sánh bằng chuỗi "0,1" -> 0 dòng.
        // LIKE không wildcard biểu diễn '=' không phân biệt hoa/thường.
        if (!string.IsNullOrWhiteSpace(flagActive))
        {
            var fa = PortQuery.Equal(flagActive);
            q = q.Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), fa, PortQuery.Esc));
        }

        // OrgID: gate nhận OrgID (:25, :55) và đọc header NetworkID/OrgID (:36-37, :66-67),
        // nhưng strWhereClause_Mst_LeaveType (:171-186) và SQL biz KHÔNG dùng OrgID hay NetworkID để lọc -> không lọc.

        // Bước 1: Draft sắp xếp theo mlt.LeaveTypeCode asc (MasterData.cs:31646-31654).
        // MyIdxSeq = số thứ tự 0-based trong thứ tự LeaveTypeCode asc (identity(bigint, 0, 1) ở SQL nguồn :31647).
        var ordered = q.OrderBy(x => x.LeaveTypeCode);

        // MyCount: Count(0) của draft trước khi cắt trang (MasterData.cs:31657)
        var itemCount = await ordered.CountAsync();

        // Bước 2: Cắt trang (MasterData.cs:31625-31626, :31661-31668) và Select cuối (MasterData.cs:31684-31694).
        // Phân trang chống tràn int và KHÔNG clamp Ft_PageIndex âm về 0 (bài học A.12):
        var page = (recordCount <= 0 || recordStart < 0)
            ? new List<Mst_LeaveType>()
            : await ordered.Skip((int)Math.Min(recordStart, int.MaxValue)).Take((int)Math.Min(recordCount, int.MaxValue)).ToListAsync();

        // Select cuối theo t.MyIdxSeq asc (MasterData.cs:31692 order by t.MyIdxSeq asc).
        // Mỗi dòng trả về đúng 10 trường: MyIdxSeq (0-based) + 9 cột model Mst_LeaveType (Mst_LeaveType.cs:11-19).
        var rows = page
            .Select((x, i) => (object)new
            {
                MyIdxSeq = recordStart + i,
                x.LeaveTypeCode,
                x.NetworkID,
                x.LeaveType,
                x.LeaveTypeDesc,
                x.FlagUnPaid,
                x.FlagInsurance,
                x.FlagActive,
                x.LogLUDTimeUTC,
                x.LogLUBy,
            })
            .ToList();

        return (itemCount, rows);
    }
}
