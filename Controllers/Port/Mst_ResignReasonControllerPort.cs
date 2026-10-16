using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniLoyalty.Data;
using MiniLoyalty.Models.Port;

namespace MiniLoyalty.Controllers.Port;

public class MstResignReasonSearchReq
{
    public string? OrgID { get; set; }
    public string? KeyWord { get; set; }
    public string? FlagActive { get; set; }
    public int Ft_PageIndex { get; set; } = 0;
    public int Ft_PageSize { get; set; } = 10;
}

[ApiController]
[Route("api/mst_resignreason")]
[Produces("application/json")]
public class Mst_ResignReasonControllerPort : ControllerBase
{
    private readonly AppDbContext _db;

    public Mst_ResignReasonControllerPort(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] MstResignReasonSearchReq? req)
    {
        var r = req ?? new MstResignReasonSearchReq();
        var pageIndex = r.Ft_PageIndex;
        var pageSize = r.Ft_PageSize;

        // Tinh RecordStart = PageIndex*PageSize, RecordCount = PageSize (Mst_ResignReasonController.cs:67-68)
        var start = (long)pageIndex * pageSize;
        var (itemCount, rows) = await GetXAsync(r.OrgID, r.KeyWord, r.FlagActive, start, pageSize);

        // PageCount chi tinh khi DataList KHONG rong; doi PageSize = 0 thanh 10 (Mst_ResignReasonController.cs:81-85)
        var ps = pageSize == 0 ? 10 : pageSize;
        var pageCount = rows.Count == 0 ? 0 : (itemCount % ps == 0 ? itemCount / ps : itemCount / ps + 1);

        return Ok(new { Success = true, Data = new PageInfo<object>(rows, itemCount, pageCount, pageIndex, pageSize) });
    }

    private async Task<(int itemCount, List<object> rows)> GetXAsync(string? orgId, string? keyWord, string? flagActive, long recordStart, long recordCount)
    {
        var q = _db.Set<Mst_ResignReason>().AsNoTracking().AsQueryable();

        // OrgID: loc OrgID = ... (Mst_ResignReasonController.cs:781)
        if (!string.IsNullOrWhiteSpace(orgId))
        {
            // toán tử '=' (Mst_ResignReasonController.cs:779-782): không phân biệt hoa/thường như SQL Server
            var oid = PortQuery.Equal(orgId.Trim());
            q = q.Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), oid, PortQuery.Esc));
        }

        // KeyWord: loc ResignReason like '%...%' (Mst_ResignReasonController.cs:777)
        if (!string.IsNullOrWhiteSpace(keyWord))
        {
            var kw = PortQuery.Contains(keyWord);
            q = q.Where(x => EF.Functions.Like((x.ResignReason ?? "").ToLower(), kw, PortQuery.Esc));
        }

        // FlagActive: cat chuoi bang dau phay, dung toan tu IN (Mst_ResignReasonController.cs:789-794)
        if (!string.IsNullOrWhiteSpace(flagActive))
        {
            var faArr = flagActive.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToList();
            q = q.Where(x => x.FlagActive != null && faArr.Contains(x.FlagActive));
        }

        // Draft: order by mrr.OrgID asc, mrr.ReasonID asc (MasterData.cs:24676-24678)
        var ordered = q.OrderBy(x => x.OrgID).ThenBy(x => x.ReasonID);
        var itemCount = await ordered.CountAsync(); // MyCount = so dong draft (MasterData.cs:24682)

        // Lay tu [start, start+count-1]
        var page = (recordCount <= 0 || recordStart < 0)
            ? new List<Mst_ResignReason>()
            : await ordered.Skip((int)Math.Min(recordStart, int.MaxValue)).Take((int)Math.Min(recordCount, int.MaxValue)).ToListAsync();

        // Select cuoi: left join Mst_NNT mnnt on t.OrgID = mnnt.OrgID (MasterData.cs:24719-24720)
        var orgIds = page.Select(p => p.OrgID).Distinct().ToList();
        // left join Mst_NNT theo OrgID (MasterData.cs:24719-24721): một OrgID có thể có nhiều dòng NNT -> lấy dòng đầu theo MST (như #197), tránh ToDictionary ném lỗi trùng khoá
        var nntRows = await _db.Set<Mst_NNT>().AsNoTracking()
            .Where(n => orgIds.Contains(n.OrgID))
            .OrderBy(n => n.MST)
            .ToListAsync();
        var nntDict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var n in nntRows) { if (n.OrgID != null && !nntDict.ContainsKey(n.OrgID)) nntDict[n.OrgID] = n.NNTFullName; }

        var rows = page
            .Select((x, i) => (object)new
            {
                // select t.MyIdxSeq ... order by t.MyIdxSeq asc (MasterData.cs:24712, 24723)
                MyIdxSeq = recordStart + i,
                x.ReasonID,
                x.OrgID,
                x.NetworkID,
                x.ResignReason,
                x.ReasonDesc,
                x.FlagActive,
                x.Remark,
                x.LogLUDTimeUTC,
                x.LogLUBy,
                mnnt_NNTFullName = nntDict.GetValueOrDefault(x.OrgID) // mnnt.NNTFullName mnnt_NNTFullName (MasterData.cs:24714)
            })
            .ToList();

        return (itemCount, rows);
    }
}
