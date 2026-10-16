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
// PORT 1:1 — Sys_GroupController/Search (#307).
//
// Nguồn gate: Sys_GroupController.cs:27-135 -> WA_Sys_Group_Get
//   - Nhận OrgID, KeyWord, FlagActive, Ft_PageIndex = 0, Ft_PageSize = 10
//   - Hàm lọc strWhereClause_Sys_Group (:917-932):
//     * KeyWord được map vào Sys_GroupUI.KeyWord (:56), nhưng hàm lọc chỉ kiểm tra data.GroupCode (:922-925).
//       Do GroupCode không được map -> điều kiện này KHÔNG BAO GIỜ được kích hoạt -> KeyWord BỎ QUA hoàn toàn.
//     * OrgID map vào Sys_GroupUI.OrgID (:57) nhưng hàm lọc KHÔNG HỀ kiểm tra -> BỎ QUA hoàn toàn.
//     * FlagActive lọc bằng toán tử = nguyên chuỗi (:928).
//   - Gate map kết quả (:85-110): Lấy Lst_Sys_Group, map đếm user từ Lst_Sys_UserInGroup theo GroupCode.
//     Trả ra mảng object gồm 7 trường: GroupCode, GroupName, MST, FlagActive, LogLUDTimeUTC, LogLUBy, QtyUserInGroup.
//
// Nguồn API: SysGroupController.cs:20-91
// Nguồn biz: System.cs:2510-2625 (WAS_Sys_Group_Get) -> :2171-2508 (Sys_Group_Get_New20191102)
//   - Draft (:2304-2319): select distinct identity(bigint, 0, 1) MyIdxSeq, sg.GroupCode ... order by sg.GroupCode asc
//   - Summary (:2322): select Count(0) MyCount from #tbl_Sys_Group_Filter_Draft
//   - Select cuối (:2356-2363): select t.MyIdxSeq, sg.* from #tbl_Sys_Group_Filter t inner join Sys_Group sg ... order by t.MyIdxSeq asc
//
// Bỏ qua có chủ đích (loại Đ):
//   - CheckGatewayAuthentication: Sys_GroupController.cs:36, SysGroupController.cs:43-48
//   - CheckAuthorize: Sys_GroupController.cs:37, Sys_User_CheckAuthorize (System.cs:2236-2249)
//   - Sys_Access_CheckDenyV30: System.cs:2268-2272
//   - Ghi RequestLog/ReturnLog: System.cs:2217-2224, 2498-2505
// ============================================================================================

/// <summary>
/// Port 1:1 Sys_GroupController.cs — Sys_Group
/// </summary>
[ApiController]
[Route("api/sys_group")]
[Produces("application/json")]
public class Sys_GroupControllerPort : ControllerBase
{
    private readonly AppDbContext _db;

    public Sys_GroupControllerPort(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Port 1:1 Search action từ gate Sys_GroupController.cs:27-135 (#307).
    /// </summary>
    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Sys_GroupSearchReq? req)
    {
        var r = req ?? new Sys_GroupSearchReq();
        var pageIndex = r.Ft_PageIndex;
        var pageSize = r.Ft_PageSize;

        // Bắt đầu truy vấn trên bảng Sys_Group
        var q = _db.Set<Sys_Group>().AsNoTracking().AsQueryable();

        // Gate Sys_GroupController.cs:27 nhận OrgID, KeyWord, FlagActive
        // Hàm strWhereClause_Sys_Group (:917-932):
        // - KeyWord: map vào Sys_GroupUI.KeyWord nhưng hàm lọc chỉ check data.GroupCode -> BỎ QUA hoàn toàn
        // - OrgID: map vào Sys_GroupUI.OrgID nhưng hàm lọc KHÔNG HỀ check -> BỎ QUA hoàn toàn
        // - FlagActive: lọc bằng toán tử = nguyên chuỗi (nếu có giá trị)
        if (!string.IsNullOrEmpty(r.FlagActive))
        {
            var fa = PortQuery.Equal(r.FlagActive);
            q = q.Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), fa, PortQuery.Esc));
        }

        // Sắp xếp: Biz Sys_Group_Get_New20191102 (:2318) order by sg.GroupCode asc
        var ordered = q.OrderBy(x => x.GroupCode);

        // Tổng số bản ghi (select Count(0) MyCount from #tbl_Sys_Group_Filter_Draft)
        var itemCount = await ordered.CountAsync();

        // Phân trang theo bài học A.12 và A.7:
        // Ft_PageSize = 0 hoặc recordCount <= 0 hoặc recordStart < 0 -> DataList rỗng
        var recordStart = (long)pageIndex * pageSize;
        var recordCount = (long)pageSize;

        var pagedGroups = (recordCount <= 0 || recordStart < 0)
            ? new List<Sys_Group>()
            : await ordered.Skip((int)Math.Min(recordStart, int.MaxValue))
                           .Take((int)Math.Min(recordCount, int.MaxValue))
                           .ToListAsync();

        // Đếm user trong group từ bảng Sys_UserInGroup:
        // Gate (:85-110): map đếm số lượng user từ Lst_Sys_UserInGroup theo GroupCode
        var groupCodes = pagedGroups.Select(g => g.GroupCode).ToList();
        var userCounts = await _db.Set<Sys_UserInGroup>()
            .AsNoTracking()
            .Where(u => groupCodes.Contains(u.GroupCode))
            .GroupBy(u => u.GroupCode)
            .Select(g => new { GroupCode = g.Key, Qty = g.Count() })
            .ToDictionaryAsync(x => x.GroupCode, x => x.Qty);

        var dataList = pagedGroups.Select(item => new Sys_GroupSearchResultItem
        {
            GroupCode = item.GroupCode,
            GroupName = item.GroupName,
            MST = item.MST,
            FlagActive = item.FlagActive,
            LogLUDTimeUTC = item.LogLUDTimeUTC,
            LogLUBy = item.LogLUBy,
            // LỖI NGUỒN GIỮ NGUYÊN (Sys_GroupController.cs:87): Qty = x.FirstOrDefault().GroupCode.Count() = số KÝ TỰ của GroupCode (string.Count()), không phải số user;
            // chỉ có khi nhóm có >= 1 dòng Sys_UserInGroup (group by trên Lst_Sys_UserInGroup), ngược lại 0 (:90-95).
            QtyUserInGroup = userCounts.ContainsKey(item.GroupCode) ? item.GroupCode.Length : 0
        }).ToList();

        // Tính PageCount theo logic tầng gate (:113-117):
        // if (objRT_Sys_Group.Lst_Sys_Group != null && objRT_Sys_Group.Lst_Sys_Group.Count > 0)
        // {
        //     if (pageSize == 0) pageSize = PageSizeConfig; // PageSizeConfig = 10
        //     pageCount = (itemCount % pageSize == 0) ? itemCount / pageSize : itemCount / pageSize + 1;
        // }
        // Khi dataList rỗng -> pageCount = 0 (Bài học A.7).
        var pageCount = 0;
        if (dataList.Count > 0)
        {
            var ps = pageSize == 0 ? 10 : pageSize;
            pageCount = (itemCount % ps == 0) ? (itemCount / ps) : (itemCount / ps + 1);
        }

        var pageInfo = new PageInfo<Sys_GroupSearchResultItem>(dataList, itemCount, pageCount, pageIndex, pageSize);

        return Ok(new
        {
            Success = true,
            Data = pageInfo
        });
    }
}

/// <summary>
/// Tham số request cho action Search — Sys_GroupController.cs:27
/// </summary>
public class Sys_GroupSearchReq
{
    public string? OrgID { get; set; }
    public string? KeyWord { get; set; }
    public string? FlagActive { get; set; }
    public int Ft_PageIndex { get; set; } = 0;
    public int Ft_PageSize { get; set; } = 10;
}

/// <summary>
/// DTO từng dòng trong DataList trả về client (Gate Sys_GroupController.cs:97-106).
/// Gồm 7 trường: GroupCode, GroupName, MST, FlagActive, LogLUDTimeUTC, LogLUBy, QtyUserInGroup.
/// </summary>
public class Sys_GroupSearchResultItem
{
    public string GroupCode { get; set; } = "";
    public string? GroupName { get; set; }
    public string? MST { get; set; }
    public string? FlagActive { get; set; }
    public DateTime? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
    public int QtyUserInGroup { get; set; }
}
