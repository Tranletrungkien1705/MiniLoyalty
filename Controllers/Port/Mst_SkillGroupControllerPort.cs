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
// PORT 1:1 — Mst_SkillGroupController/Search (#243), GetAllActive (#244).
//
// Nguồn controller (tầng gate):
//   Mst_SkillGroupController.cs:25-105     Search -> WA_Mst_SkillGroup_Get
//   Mst_SkillGroupController.cs:107-167    GetAllActive -> WA_Mst_SkillGroup_Get
//   Mst_SkillGroupController.cs:171-186    strWhereClause_Mst_SkillGroup
//   Mst_SkillGroupController.cs:188-199    strWhereClause_Mst_SkillGroup_Active
// Nguồn API:
//   MstSkillGroupController.cs:17-88       WA_Mst_SkillGroup_Get
// Nguồn biz:
//   MasterData.cs:43871-43961              WAS_Mst_SkillGroup_Get
//   MasterData.cs:43741-43869              Mst_SkillGroup_Get
//   MasterData.cs:43584-43739              Mst_SkillGroup_GetX
// ============================================================================================

public class Mst_SkillGroupSearchReq
{
    public string? OrgID { get; set; }               // Gate nhận (:25, :55) nhưng strWhereClause_Mst_SkillGroup (:171-186) KHÔNG dùng -> KHÔNG lọc
    public string? KeyWord { get; set; }             // Từ khóa tìm theo SkillGroupName (:178)
    public string? FlagActive { get; set; }          // Cờ hoạt động, toán tử '=' nguyên chuỗi (:182)
    public int Ft_PageIndex { get; set; } = 0;       // :25, :46
    public int Ft_PageSize { get; set; } = 10;       // :25, :47
}

[ApiController]
[Route("api/mst_skillgroup")]
[Produces("application/json")]
public class Mst_SkillGroupControllerPort : ControllerBase
{
    private readonly AppDbContext _db;

    public Mst_SkillGroupControllerPort(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] Mst_SkillGroupSearchReq? req)
    {
        var r = req ?? new Mst_SkillGroupSearchReq();
        var pageIndex = r.Ft_PageIndex;
        var pageSize = r.Ft_PageSize;

        // Bước 1: Tính toán bắt đầu trang RecordStart = pageIndex * pageSize (Mst_SkillGroupController.cs:69-70)
        var start = (long)pageIndex * pageSize;
        var (itemCount, rows) = await GetXAsync(r.KeyWord, r.FlagActive, start, pageSize);

        // Nguồn xử lý page size và pageCount: gate :83-88
        // Nếu pageSize == 0 hoặc rows rỗng: PageCount = 0 (bài học A.7)
        var ps = pageSize == 0 ? 10 : pageSize;
        var pageCount = rows.Count == 0 ? 0 : (itemCount % ps == 0 ? itemCount / ps : itemCount / ps + 1);

        return Ok(new { Success = true, Data = new PageInfo<object>(rows, itemCount, pageCount, pageIndex, pageSize) });
    }

    /// <summary>
    /// Port 1:1 Mst_SkillGroupController.GetAllActive (#244).
    /// Nguồn gate: Mst_SkillGroupController.cs:107-167.
    /// Lọc FlagActive = "1" qua strWhereClause_Mst_SkillGroup_Active (:188-199, toán tử '=' :195).
    /// Không lọc KeyWord hay OrgID (:127-130).
    /// Ft_RecordStart, Ft_RecordCount lấy từ property của base class tương đương lấy tất cả (0 và 123456000) (:142-143).
    /// Gọi lại GetXAsync(null, "1", 0, 123456000) (biz MasterData.cs:43584-43739).
    /// Data trả về mảng trần danh sách Mst_SkillGroup, nếu rỗng trả về null (:126, :151-154).
    /// </summary>
    [HttpPost("getallactive")]
    public async Task<IActionResult> GetAllActive()
    {
        var (_, rows) = await GetXAsync(null, "1", 0, 123456000);
        return Ok(new { Success = true, Data = rows.Count > 0 ? rows : null });
    }

    private async Task<(int itemCount, List<object> rows)> GetXAsync(string? keyWord, string? flagActive, long recordStart, long recordCount)
    {
        var q = _db.Set<Mst_SkillGroup>().AsNoTracking().AsQueryable();

        // 1. KeyWord -> LIKE %...% trên SkillGroupName (Mst_SkillGroupController.cs:178)
        if (!string.IsNullOrWhiteSpace(keyWord))
        {
            var kw = PortQuery.Contains(keyWord);
            q = q.Where(x => EF.Functions.Like((x.SkillGroupName ?? "").ToLower(), kw, PortQuery.Esc));
        }

        // 2. FlagActive -> '=' nguyên chuỗi trên FlagActive (Mst_SkillGroupController.cs:182), KHÔNG IN/tách dấu phẩy
        if (!string.IsNullOrWhiteSpace(flagActive))
        {
            q = q.Where(x => x.FlagActive == flagActive);
        }

        // 3. OrgID: Tham số có truyền vào gate nhưng strWhereClause_Mst_SkillGroup (:171-186) KHÔNG dùng -> bỏ qua không lọc

        // Bước Draft (MasterData.cs:43639-43647):
        // select distinct identity(bigint, 0, 1) MyIdxSeq, msg.SkillGroupCode into #tbl_Mst_SkillGroup_Filter_Draft
        // order by msg.SkillGroupCode asc
        var ordered = q.OrderBy(x => x.SkillGroupCode);
        var itemCount = await ordered.CountAsync();

        // Cắt phân trang theo [recordStart, recordStart + recordCount - 1] (MasterData.cs:43658-43661)
        // Bài học A.12 và A.7: Khi recordCount <= 0 hoặc recordStart < 0 -> trả danh sách rỗng
        var page = (recordCount <= 0 || recordStart < 0)
            ? new List<Mst_SkillGroup>()
            : await ordered.Skip((int)Math.Min(recordStart, int.MaxValue)).Take((int)Math.Min(recordCount, int.MaxValue)).ToListAsync();

        // Bước Select cuối (MasterData.cs:43679-43686):
        // select t.MyIdxSeq, msg.* from #tbl_Mst_SkillGroup_Filter t inner join Mst_SkillGroup msg
        // on t.SkillGroupCode = msg.SkillGroupCode order by t.MyIdxSeq asc
        var rows = page
            .Select((x, i) => (object)new
            {
                MyIdxSeq = recordStart + i,
                x.SkillGroupCode,
                x.NetworkID,
                x.SkillGroupName,
                x.FlagActive,
                x.LogLUDTimeUTC,
                x.LogLUBy
            })
            .ToList();

        return (itemCount, rows);
    }
}
