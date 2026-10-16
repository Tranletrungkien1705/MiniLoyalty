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
// PORT 1:1 — màn danh mục chuyên ngành (Mst_MajorGroup) của dự án nguồn 2022.7.Loyalty (#170, #171).
//
// Nguồn controller (tầng gate):
//   Mst_MajorGroupController.cs:25-105   Search              -> WA_Mst_MajorGroup_Get (#170)
//   Mst_MajorGroupController.cs:107-167  GetAllActive        -> WA_Mst_MajorGroup_Get (#171)
//   Mst_MajorGroupController.cs:171-186  strWhereClause_Mst_MajorGroup (mệnh đề lọc Search)
//   Mst_MajorGroupController.cs:188-199  strWhereClause_Mst_MajorGroup_Active (mệnh đề lọc GetAllActive)
// Nguồn API:
//   MstMajorGroupController.cs:17-88     WA_Mst_MajorGroup_Get
// Nguồn biz:
//   MasterData.cs:39174-39264            WAS_Mst_MajorGroup_Get
//   MasterData.cs:39044-39172            Mst_MajorGroup_Get
//   MasterData.cs:38887-39042            Mst_MajorGroup_GetX (SQL :38940-39027)
//
// GIỮ ĐÚNG NGUỒN:
//   * Endpoint là POST (nguồn [AcceptVerbs("POST")]).
//   * Lọc KeyWord: like '%kw%' trên MajorGroupName (Mst_MajorGroupController.cs:176-179),
//     KHÔNG lọc MajorGroupCode hay MajorGroupDesc.
//   * Lọc FlagActive: toán tử '=' nguyên chuỗi (Mst_MajorGroupController.cs:180-183, :195),
//     KHÔNG IN, KHÔNG tách dấu phẩy ("0,1" -> 0 dòng). GetAllActive lọc cứng FlagActive = "1" (:129).
//   * OrgID: gate nhận (Mst_MajorGroupController.cs:25, :55, :67, :119, :140) nhưng strWhereClause_Mst_MajorGroup
//     và strWhereClause_Mst_MajorGroup_Active và SQL biz KHÔNG dùng OrgID -> KHÔNG lọc.
//   * GetAllActive trả Data là danh sách trần (không bọc PageInfo như Search); khi rỗng (0 dòng) thì Data = null (:126, :149-154).
//   * Phân trang GetAllActive: Ft_RecordStart = 0, Ft_RecordCount = 123456000 (BaseController.cs:30-31, Mst_MajorGroupController.cs:142-143).
//   * Draft sắp xếp theo mmg.MajorGroupCode asc (MasterData.cs:38949).
//   * Select cuối sắp xếp theo t.MyIdxSeq asc (MasterData.cs:38988).
//   * MyIdxSeq 0-based identity (MasterData.cs:38944).
//   * PageSize = 0 -> DataList rỗng [], PageCount = 0 (gate :80-90).
//   * Chuẩn hoá input: gate chỉ gọi CUtils.StrValue, biz KHÔNG gọi StdParam/StdStr/StdFlag.
//
// Bỏ qua có chủ đích (loại Đ):
//   * CheckGatewayAuthentication: gate :34, :116, API :40 (hệ thống đích chưa có hệ gateway)
//   * CheckAuthorize: gate :35, :117 (hệ thống đích chưa có hệ phân quyền)
//   * Tài khoản viết cứng tại Mst_MajorGroupController.cs:62-63, :135-136 — không chép giá trị (cấm chép mật khẩu)
//   * Sys_User_CheckAuthentication (MasterData.cs:39095-39099) và Sys_Access_CheckDenyV30 (:39102-39106)
//     đã bị comment ở nguồn.
// ============================================================================================

/// <summary>Port 1:1 <c>Mst_MajorGroupSearch</c> — tham số lọc + phân trang (Mst_MajorGroupController.cs:25, 52-57).</summary>
public class MstMajorGroupSearchReq
{
    public string? OrgID { get; set; }               // Tổ chức — gate nhận (:25, :55) nhưng strWhereClause_Mst_MajorGroup (:171-186) và SQL biz KHÔNG lọc OrgID
    public string? KeyWord { get; set; }             // Từ khoá tìm theo tên chuyên ngành (MajorGroupName) — :25, :54, :178
    public string? FlagActive { get; set; }          // Cờ hoạt động, toán tử '=' — :25, :56, :182
    public int Ft_PageIndex { get; set; } = 0;       // Trang (0-based) — :25, :46
    public int Ft_PageSize { get; set; } = 10;       // Số dòng/trang — :25, :47
}

[ApiController]
[Route("api/mst_majorgroup")]
[Produces("application/json")]
public class Mst_MajorGroupControllerPort : ControllerBase
{
    private readonly AppDbContext _db;

    public Mst_MajorGroupControllerPort(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] MstMajorGroupSearchReq? req)
    {
        var r = req ?? new MstMajorGroupSearchReq();
        var pageIndex = r.Ft_PageIndex;
        var pageSize = r.Ft_PageSize;

        // Search gọi GetXAsync với start = PageIndex * PageSize, count = PageSize (giữ nguyên hành vi cũ nguồn Mst_MajorGroupController.cs:69-70).
        var start = (long)pageIndex * pageSize;
        var (itemCount, rows) = await GetXAsync(r.KeyWord, r.FlagActive, start, pageSize);

        // PageSize=0 -> DataList rỗng (nguồn: nFilterRecordEnd = start-1, SQL biz + gate Mst_MajorGroupController.cs:80-88).
        // PageCount = 0 khi DataList rỗng, ngược lại ceil(ItemCount / ps) với ps = PageSize (PageSize=0 thì 10) — gate :83-87.
        var ps = pageSize == 0 ? 10 : pageSize;
        var pageCount = rows.Count == 0 ? 0 : (itemCount % ps == 0 ? itemCount / ps : itemCount / ps + 1);

        return Ok(new { Success = true, Data = new PageInfo<object>(rows, itemCount, pageCount, pageIndex, pageSize) });
    }

    /// <summary>
    /// Lấy danh sách chuyên ngành đang hoạt động (FlagActive = "1") — port 1:1 GetAllActive (#171).
    /// Nguồn: Mst_MajorGroupController.cs:107-167.
    /// Không nhận body. Lọc FlagActive = "1" với toán tử '=' (Mst_MajorGroupController.cs:127-130, :195); OrgID không lọc.
    /// Ft_RecordStart = 0, Ft_RecordCount = 123456000 (BaseController.cs:30-31, Mst_MajorGroupController.cs:142-143).
    /// Trả Data là danh sách trần List&lt;Mst_MajorGroup&gt;; khi rỗng (0 dòng) thì Data = null (:126, :149-154).
    /// </summary>
    [HttpPost("getallactive")]
    public async Task<IActionResult> GetAllActive()
    {
        // GetAllActive gọi GetXAsync với KeyWord = null, FlagActive = "1", recordStart = 0, recordCount = 123456000
        // (Mst_MajorGroupController.cs:126-146; BaseController.cs:30-31).
        var (_, rows) = await GetXAsync(null, "1", 0, 123456000);

        // Nguồn Mst_MajorGroupController.cs:126, 149-154:
        // List<TModels.Mst_MajorGroup> lst_Mst_MajorGroup = null;
        // if (objRT_Mst_MajorGroup.Lst_Mst_MajorGroup != null && objRT_Mst_MajorGroup.Lst_Mst_MajorGroup.Count > 0)
        //     lst_Mst_MajorGroup = objRT_Mst_MajorGroup.Lst_Mst_MajorGroup;
        // objJsonResultEntry.Data = lst_Mst_MajorGroup;
        // Danh sách trần (không bọc PageInfo); khi rỗng (0 dòng) trả Data = null.
        return Ok(new { Success = true, Data = rows.Count > 0 ? rows : null });
    }

    /// <summary>
    /// Lọc + đếm + chia trang theo MajorGroupCode asc — dùng cho Search và GetAllActive.
    /// Trả (itemCount, rows): itemCount = tổng số dòng sau lọc (MyCount, SQL :38952); rows = trang đã cắt kèm MyIdxSeq 0-based (:38983).
    /// </summary>
    private async Task<(int itemCount, List<object> rows)> GetXAsync(string? keyWord, string? flagActive, long recordStart, long recordCount)
    {
        var q = _db.Set<Mst_MajorGroup>().AsNoTracking().AsQueryable();

        // KeyWord -> like '%kw%' trên MajorGroupName (Mst_MajorGroupController.cs:178).
        // Nguồn chạy SQL Server collation KHÔNG phân biệt hoa/thường ⇒ hạ chữ cả 2 phía rồi LIKE (bài học A.1, bẫy #18/#31).
        if (!string.IsNullOrWhiteSpace(keyWord))
        {
            var kw = PortQuery.Contains(keyWord);
            q = q.Where(x => EF.Functions.Like((x.MajorGroupName ?? "").ToLower(), kw, PortQuery.Esc));
        }

        // FlagActive -> toán tử '=' trên FlagActive (Mst_MajorGroupController.cs:182) — KHÔNG IN, KHÔNG tách dấu phẩy.
        // Nguồn dùng '=' ⇒ so sánh bằng chuỗi không phân biệt hoa/thường bằng Like + PortQuery.Equal.
        if (!string.IsNullOrWhiteSpace(flagActive))
        {
            var fa = PortQuery.Equal(flagActive);
            q = q.Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), fa, PortQuery.Esc));
        }

        // Chia trang theo MajorGroupCode asc (Mst_MajorGroup_GetX SQL: MasterData.cs:38949 order by mmg.MajorGroupCode asc).
        var ordered = q.OrderBy(x => x.MajorGroupCode);
        var itemCount = await ordered.CountAsync();

        // MyIdxSeq = số thứ tự 0-based trong thứ tự MajorGroupCode (identity(bigint, 0, 1) ở SQL nguồn MasterData.cs:38944).
        // Lấy dòng [recordStart, recordStart+recordCount) theo thứ tự MajorGroupCode (bài học A.12 chống tràn int và không clamp âm).
        var page = (recordCount <= 0 || recordStart < 0)
            ? new List<Mst_MajorGroup>()
            : await ordered.Skip((int)Math.Min(recordStart, int.MaxValue)).Take((int)Math.Min(recordCount, int.MaxValue)).ToListAsync();

        // Select cuối theo t.MyIdxSeq asc (MasterData.cs:38988 order by t.MyIdxSeq asc), gồm MyIdxSeq + 7 cột model.
        var rows = page
            .Select((x, i) => (object)new
            {
                MyIdxSeq = recordStart + i,
                x.MajorGroupCode,
                x.NetworkID,
                x.MajorGroupName,
                x.MajorGroupDesc,
                x.FlagActive,
                x.LogLUDTimeUTC,
                x.LogLUBy,
            })
            .ToList();

        return (itemCount, rows);
    }
}
