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

/// <summary>
/// DTO tham số tìm kiếm cho Sys_AccessController.Search (gate Sys_AccessController.cs:27-109).
/// Nhận OrgID, KeyWord, FlagActive, Ft_PageIndex, Ft_PageSize qua JSON body (Bài học A.14).
/// </summary>
public class SysAccessSearchReq
{
    public string? OrgID { get; set; }
    public string? KeyWord { get; set; }
    public string? FlagActive { get; set; }
    public int Ft_PageIndex { get; set; } = 0;
    public int Ft_PageSize { get; set; } = 10;
}

/// <summary>
/// Port 1:1 Sys_AccessController — màn phân quyền truy cập hệ thống (#300 Search, #301 Get).
/// Nguồn Gate: D:\idocNet\2022.7.Loyalty\13.ClientGate\V10\idn.Loyalty.WebAPI\Controllers\Sys_AccessController.cs:27-109, 110-217
/// Nguồn API: D:\idocNet\2022.7.Loyalty\11.BackEnd\V10\idn.Loyalty.Biz.Web\Controllers\SysAccessController.cs:20-91 (WA_Sys_Access_Get)
/// Nguồn BIZ: D:\idocNet\2022.7.Loyalty\11.BackEnd\V10\idn.Loyalty.Biz\System.cs:757-850 (WAS_Sys_Access_Get)
///            D:\idocNet\2022.7.Loyalty\11.BackEnd\V10\idn.Loyalty.Biz\System.cs:290-512 (Sys_Access_Get)
/// </summary>
[ApiController]
[Route("api/sys_access")]
[Produces("application/json")]
public class Sys_AccessControllerPort : ControllerBase
{
    private readonly AppDbContext _db;

    public Sys_AccessControllerPort(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Tìm kiếm phân quyền truy cập hệ thống (Sys_AccessController.cs:27-109).
    /// </summary>
    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] SysAccessSearchReq? req)
    {
        var r = req ?? new SysAccessSearchReq();
        var pageIndex = r.Ft_PageIndex;
        var pageSize = r.Ft_PageSize;

        // Tính recordStart = PageIndex * PageSize (gate :73)
        var start = (long)pageIndex * pageSize;
        var (itemCount, rows) = await GetXAsync(r.OrgID, r.KeyWord, r.FlagActive, start, pageSize);

        // Tính pageCount theo gate :87-91. Nếu pageSize = 0 thì quy ước chia 10 hoặc trả 0 khi rỗng.
        // Theo bài học A.7: Khi Ft_PageSize = 0 thì DataList rỗng, PageCount = 0, ItemCount giữ nguyên tổng số.
        var ps = pageSize == 0 ? 10 : pageSize;
        var pageCount = rows.Count == 0 ? 0 : (itemCount % ps == 0 ? itemCount / ps : itemCount / ps + 1);

        return Ok(new { Success = true, Data = new PageInfo<object>(rows, itemCount, pageCount, pageIndex, pageSize) });
    }

    /// <summary>
    /// Port 1:1 Sys_AccessController.Get (gate :110-217) -> WA_Sys_Access_Get. Không nhận tham số từ client: Ft_RecordStart = "0", Ft_RecordCount = "123456000" (BaseController.cs:30-31),
    /// where = strWhereClause_Sys_Access(OrgID = session, FlagActive = "1") nhưng hàm lọc chỉ xét GroupCode/ObjectCode (không được gán) -> không lọc gì (xem GetXAsync).
    /// Data = danh sách (có thêm các cột so_* từ left join Sys_Object), rỗng -> null (gate :157-159, :197-204).
    /// </summary>
    [HttpPost("get")]
    public async Task<IActionResult> Get()
    {
        var (_, rows) = await GetXAsync(null, null, null, 0, 123456000);
        return Ok(new { Success = true, Data = rows.Count > 0 ? rows : null });
    }

    /// <summary>
    /// Thực hiện 2 bước theo đúng mã nguồn gốc BIZ (System.cs:290-512):
    /// Bước 1: Draft (#tbl_Sys_Access_Filter_Draft, System.cs:359-370)
    /// select distinct identity(bigint, 0, 1) MyIdxSeq, sa.GroupCode, sa.ObjectCode
    /// into #tbl_Sys_Access_Filter_Draft from Sys_Access sa where (1=1) ...
    /// order by sa.GroupCode asc, sa.ObjectCode asc
    /// 
    /// LỖI DI SẢN MÃ NGUỒN:
    /// Gate Sys_AccessController.cs:56-61 truyền KeyWord, OrgID, FlagActive vào hàm strWhereClause_Sys_Access (:795-810),
    /// nhưng hàm này lại kiểm tra data.GroupCode và data.ObjectCode (không hề được gán từ gate).
    /// Vì vậy mệnh đề WHERE luôn là (1=1), các tham số OrgID, KeyWord, FlagActive hoàn toàn bị bỏ qua.
    /// 
    /// Phân trang: Áp dụng bài học A.12 và A.7:
    /// - Ft_PageSize = 0 hoặc recordStart &lt; 0: trả danh sách rỗng, PageCount = 0, giữ nguyên ItemCount tổng số.
    /// - Ngược lại: Skip(start).Take(count) chống tràn số int.
    /// 
    /// Bước 2: Select cuối (System.cs:403-419)
    /// select t.MyIdxSeq, sa.*, so.ObjectCode so_ObjectCode, so.ObjectName so_ObjectName,
    /// so.ServiceCode so_ServiceCode, so.ObjectType so_ObjectType, so.FlagExecModal so_FlagExecModal,
    /// so.FlagActive so_FlagActive
    /// from #tbl_Sys_Access_Filter t
    /// inner join Sys_Access sa on t.GroupCode = sa.GroupCode and t.ObjectCode = sa.ObjectCode
    /// left join Sys_Object so on t.ObjectCode = so.ObjectCode
    /// order by t.MyIdxSeq asc
    /// 
    /// - Sys_Object ĐÃ port ở #317 -> left join thật (so_* lấy từ Sys_Object).
    /// - Phân quyền Sys_Access_CheckDeny (System.cs:332) được coi là GAP trong MiniLoyalty.
    /// </summary>
    private async Task<(int itemCount, List<object> rows)> GetXAsync(string? orgId, string? keyWord, string? flagActive, long recordStart, long recordCount)
    {
        var q = _db.Set<Sys_Access>().AsNoTracking().AsQueryable();

        // Nguồn gốc: Where 1=1 (KeyWord, OrgID, FlagActive không được gán vào GroupCode/ObjectCode của Sys_AccessUI)
        // Bước 1: Draft sắp xếp theo sa.GroupCode asc, sa.ObjectCode asc (System.cs:368-369)
        var ordered = q.OrderBy(x => x.GroupCode).ThenBy(x => x.ObjectCode);
        var itemCount = await ordered.CountAsync(); // Summary: Count(0) MyCount (System.cs:373)

        // Phân trang chuẩn theo bài học A.12 và A.7
        var page = (recordCount <= 0 || recordStart < 0)
            ? new List<Sys_Access>()
            : await ordered.Skip((int)Math.Min(recordStart, int.MaxValue)).Take((int)Math.Min(recordCount, int.MaxValue)).ToListAsync();

        // Bước 2: Select cuối - sắp xếp theo t.MyIdxSeq asc (System.cs:418)
        // left join Sys_Object so on t.ObjectCode = so.ObjectCode (System.cs:416-417): Sys_Object đã port ở #317 (PK ObjectCode -> tối đa 1 dòng/khớp)
        var objCodes = page.Select(p => p.ObjectCode).Distinct().ToList();
        var objs = objCodes.Count == 0
            ? new List<Sys_Object>()
            : await _db.Set<Sys_Object>().AsNoTracking().Where(o => objCodes.Contains(o.ObjectCode)).ToListAsync();
        var objMap = new Dictionary<string, Sys_Object>(StringComparer.OrdinalIgnoreCase);
        foreach (var o in objs) objMap[o.ObjectCode] = o;

        var rows = page
            .Select((x, i) =>
            {
                objMap.TryGetValue(x.ObjectCode, out var so);
                return (object)new
                {
                    MyIdxSeq = recordStart + i,
                    x.GroupCode,
                    x.ObjectCode,
                    x.LogLUDTimeUTC,
                    x.LogLUBy,
                    so_ObjectCode = so?.ObjectCode,
                    so_ObjectName = so?.ObjectName,
                    so_ServiceCode = so?.ServiceCode,
                    so_ObjectType = so?.ObjectType,
                    so_FlagExecModal = so?.FlagExecModal,
                    so_FlagActive = so?.FlagActive,
                };
            })
            .ToList();

        return (itemCount, rows);
    }
}
