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
// PORT 1:1 — Sys_GroupController:
//   - Search (#307)
//   - GetAllActive (#308)
//   - GetByGroupCode (#309)
//
// Nguồn gate: Sys_GroupController.cs:27-135 (Search), 136-243 (GetAllActive), 244-310 (GetByGroupCode) -> WA_Sys_Group_Get
//   - Search: Nhận OrgID, KeyWord, FlagActive, Ft_PageIndex = 0, Ft_PageSize = 10
//   - GetAllActive: Nhận FlagActive = "1" (:163), OrgID đưa vào request nhưng không lọc
//   - Hàm lọc strWhereClause_Sys_Group (:917-932):
//     * KeyWord được map vào Sys_GroupUI.KeyWord (:56), nhưng hàm lọc chỉ kiểm tra data.GroupCode (:922-925).
//       Do GroupCode không được map -> điều kiện này KHÔNG BAO GIỜ được kích hoạt -> KeyWord BỎ QUA hoàn toàn.
//     * OrgID map vào Sys_GroupUI.OrgID (:57, :162) nhưng hàm lọc KHÔNG HỀ kiểm tra -> BỎ QUA hoàn toàn.
//     * FlagActive lọc bằng toán tử = nguyên chuỗi (:928).
//   - Gate Search map kết quả (:85-110): Lấy Lst_Sys_Group, map đếm user từ Lst_Sys_UserInGroup theo GroupCode.
//     Trả ra mảng object gồm 7 trường: GroupCode, GroupName, MST, FlagActive, LogLUDTimeUTC, LogLUBy, QtyUserInGroup.
//   - Gate GetAllActive (:183-228): Trả listSys_Group trần (null nếu rỗng), không map Sys_GroupExt hay QtyUserInGroup.
//
// Nguồn API: SysGroupController.cs:20-91
// Nguồn biz: System.cs:2510-2625 (WAS_Sys_Group_Get) -> :2171-2508 (Sys_Group_Get_New20191102)
//   - Draft (:2304-2319): select distinct identity(bigint, 0, 1) MyIdxSeq, sg.GroupCode ... order by sg.GroupCode asc
//   - Summary (:2322): select Count(0) MyCount from #tbl_Sys_Group_Filter_Draft
//   - Select cuối (:2356-2363): select t.MyIdxSeq, sg.* from #tbl_Sys_Group_Filter t inner join Sys_Group sg ... order by t.MyIdxSeq asc
//
// Bỏ qua có chủ đích (loại Đ):
//   - CheckGatewayAuthentication: Sys_GroupController.cs:36, :149, SysGroupController.cs:43-48
//   - CheckAuthorize: Sys_GroupController.cs:37, :150, Sys_User_CheckAuthorize (System.cs:2236-2249)
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

    /// <summary>
    /// Port 1:1 GetAllActive action từ gate Sys_GroupController.cs:136-243 (#308).
    /// </summary>
    [HttpPost("getallactive")]
    public async Task<IActionResult> GetAllActive([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] object? req = null)
    {
        // Truy vấn Entity Sys_Group với FlagActive = "1"
        // Gate Sys_GroupController.cs:160-164 truyền FlagActive = "1"
        // Hàm lọc strWhereClause_Sys_Group (:926-929): FlagActive lọc bằng toán tử = nguyên chuỗi
        var q = _db.Set<Sys_Group>().AsNoTracking().AsQueryable();
        var fa = PortQuery.Equal("1");
        q = q.Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), fa, PortQuery.Esc));

        // Sắp xếp theo GroupCode asc (SQL System.cs:2318)
        var ordered = q.OrderBy(x => x.GroupCode);

        // Lấy từ chỉ mục 0 đến 123456000 (System.cs:2331-2332; gate Ft_RecordStart=0, Ft_RecordCount=123456000)
        var paged = await ordered.Skip(0).Take(123456000).ToListAsync();

        // Map DTO Sys_GroupGetAllActiveRes gồm 6 cột Sys_Group + MyIdxSeq (SQL System.cs:2305, 2356-2363)
        // GetAllActive gate không map Sys_GroupExt hay join Sys_UserInGroup nên không trả QtyUserInGroup
        var rows = paged.Select((x, i) => new Sys_GroupGetAllActiveRes
        {
            MyIdxSeq = i,
            GroupCode = x.GroupCode,
            GroupName = x.GroupName,
            FlagActive = x.FlagActive,
            LogLUDTimeUTC = x.LogLUDTimeUTC,
            LogLUBy = x.LogLUBy,
            MST = x.MST
        }).ToList();

        // Gate Sys_GroupController.cs:183, 220-230:
        // Data = listSys_Group (danh sách trần, null nếu rỗng)
        return Ok(new
        {
            Success = true,
            Data = rows.Count > 0 ? rows : null
        });
    }

    /// <summary>
    /// Port 1:1 GetByGroupCode action từ gate Sys_GroupController.cs:244-310 (#309).
    /// </summary>
    [HttpPost("getbygroupcode")]
    public async Task<IActionResult> GetByGroupCode([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Sys_GroupGetByGroupCodeReq? req)
    {
        // Gate Sys_GroupController.cs:265: nếu GroupCode rỗng -> trả Data = null (không gọi DB)
        if (req == null || string.IsNullOrWhiteSpace(req.GroupCode))
        {
            return Ok(new
            {
                Success = true,
                Data = (object?)null
            });
        }

        var groupCode = req.GroupCode.Trim();

        // Gate :267-289, Biz WAS_Sys_Group_Get System.cs:2550 -> Sys_Group_Get_New20191102 :2318
        // Truy vấn 1 bản ghi Sys_Group theo GroupCode bằng Like với toán tử = (không phân biệt hoa/thường)
        var matchedGroup = await _db.Set<Sys_Group>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => EF.Functions.Like((x.GroupCode ?? "").ToLower(), PortQuery.Equal(groupCode), PortQuery.Esc));

        // Gate :290-297: Nếu không tìm thấy nhóm -> Data = null
        if (matchedGroup == null)
        {
            return Ok(new
            {
                Success = true,
                Data = (object?)null
            });
        }

        // Biz System.cs:2374-2393: Truy vấn Sys_UserInGroup theo GroupCode
        // Left join Sys_Group (sg) để lấy sg_GroupCode, sg_GroupName, sg_FlagActive
        // Left join Sys_User (su) để lấy su_UserCode, su_UserName, su_FlagSysAdmin, su_FlagActive
        // (Entity Sys_User chưa có trong repo -> gán null cho các cột su_... và ghi GAP)
        var groupCodePattern = PortQuery.Equal(matchedGroup.GroupCode);
        var usersInGroup = await (from suig in _db.Set<Sys_UserInGroup>().AsNoTracking()
                                  where EF.Functions.Like((suig.GroupCode ?? "").ToLower(), groupCodePattern, PortQuery.Esc)
                                  join sg in _db.Set<Sys_Group>().AsNoTracking()
                                      on suig.GroupCode equals sg.GroupCode into sgJoin
                                  from sg in sgJoin.DefaultIfEmpty()
                                  select new Sys_UserInGroupRes
                                  {
                                      UserCode = suig.UserCode,
                                      GroupCode = suig.GroupCode,
                                      LogLUDTimeUTC = suig.LogLUDTimeUTC,
                                      LogLUBy = suig.LogLUBy,
                                      su_UserCode = null,
                                      su_UserName = null,
                                      su_FlagSysAdmin = null,
                                      su_FlagActive = null,
                                      sg_GroupCode = sg != null ? sg.GroupCode : null,
                                      sg_GroupName = sg != null ? sg.GroupName : null,
                                      sg_FlagActive = sg != null ? sg.FlagActive : null
                                  }).ToListAsync();

        var res = new Sys_GroupGetByGroupCodeRes
        {
            objSys_Group = matchedGroup,
            Lst_Sys_UserInGroup = usersInGroup
        };

        return Ok(new
        {
            Success = true,
            Data = res
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

/// <summary>
/// DTO trả về cho action GetAllActive (Sys_GroupController.cs:136-243, #308).
/// Gồm 6 cột của Sys_Group + MyIdxSeq (SQL System.cs:2356-2363).
/// </summary>
public class Sys_GroupGetAllActiveRes
{
    public long MyIdxSeq { get; set; }
    public string GroupCode { get; set; } = "";
    public string? GroupName { get; set; }
    public string? FlagActive { get; set; }
    public DateTime? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
    public string? MST { get; set; }
}

/// <summary>
/// DTO request cho action GetByGroupCode — Sys_GroupController.cs:244 (#309).
/// </summary>
public class Sys_GroupGetByGroupCodeReq
{
    public string? GroupCode { get; set; }
    public string? OrgID { get; set; }
}

/// <summary>
/// DTO response cho action GetByGroupCode (Gate Sys_GroupController.cs:293).
/// Gồm 2 property: objSys_Group và Lst_Sys_UserInGroup.
/// </summary>
public class Sys_GroupGetByGroupCodeRes
{
    public Sys_Group? objSys_Group { get; set; }
    public List<Sys_UserInGroupRes> Lst_Sys_UserInGroup { get; set; } = new();
}

/// <summary>
/// DTO từng dòng trong Lst_Sys_UserInGroup (SQL System.cs:2374-2393).
/// Gồm các trường cơ sở của Sys_UserInGroup và các cột mở rộng từ Sys_User và Sys_Group.
/// </summary>
public class Sys_UserInGroupRes
{
    public string UserCode { get; set; } = "";
    public string GroupCode { get; set; } = "";
    public DateTime? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
    // (suig.* không có cột MST)

    public string? su_UserCode { get; set; }
    public string? su_UserName { get; set; }
    public string? su_FlagSysAdmin { get; set; }
    public string? su_FlagActive { get; set; }

    public string? sg_GroupCode { get; set; }
    public string? sg_GroupName { get; set; }
    public string? sg_FlagActive { get; set; }
}

