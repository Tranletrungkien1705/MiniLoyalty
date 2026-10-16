using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniLoyalty.Data;
using MiniLoyalty.Models.Port;

namespace MiniLoyalty.Controllers.Port;

public class MstResignReasonGetByReasonIDReq
{
    public string? ReasonID { get; set; }
    public string? OrgID { get; set; }
}

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
        var (itemCount, rows) = await GetXAsync(r.OrgID, null, r.KeyWord, r.FlagActive, start, pageSize);

        // PageCount chi tinh khi DataList KHONG rong; doi PageSize = 0 thanh 10 (Mst_ResignReasonController.cs:81-85)
        var ps = pageSize == 0 ? 10 : pageSize;
        var pageCount = rows.Count == 0 ? 0 : (itemCount % ps == 0 ? itemCount / ps : itemCount / ps + 1);

        return Ok(new { Success = true, Data = new PageInfo<object>(rows, itemCount, pageCount, pageIndex, pageSize) });
    }

    [HttpPost("getbyreasonid")]
    public async Task<IActionResult> GetByReasonID([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] MstResignReasonGetByReasonIDReq? req)
    {
        // gate (dòng 117): nếu ReasonID rỗng, bỏ qua gọi Biz và trả về mặc định là null.
        // OrgID nguồn lấy từ session (orgUserUI.OrgId, :132) — hệ đích không có session nên nhận OrgID tuỳ chọn trong body
        // (cùng quy ước với Search); không truyền thì không lọc OrgID. Nhận qua JSON body như mọi action GetBy* khác (#174, #185, #192).
        if (string.IsNullOrWhiteSpace(req?.ReasonID))
        {
            return Ok(new { Success = true, Data = (object?)null });
        }

        var (_, rows) = await GetXAsync(req.OrgID, req.ReasonID, null, null, 0, 1);
        return Ok(new { Success = true, Data = rows.FirstOrDefault() }); // gate (dòng 141-147): gán phần tử [0] hoặc null
    }

    [HttpPost("getallactive")]
    public async Task<IActionResult> GetAllActive()
    {
        // gate truyen: Ft_RecordStart, Ft_RecordCount mac dinh tu Controller, hieu ngam la lay tat ca vi khong phan trang (Mst_ResignReasonController.cs:197-198)
        // filter FlagActive = "1", khong truyen OrgID/KeyWord null (Mst_ResignReasonController.cs:182-185)
        var (_, rows) = await GetXAsync(null, null, null, "1", 0, int.MaxValue);

        // gate tra mang (lst_Mst_ResignReason), null neu rong (Mst_ResignReasonController.cs:204-209)
        return Ok(new { Success = true, Data = rows.Count > 0 ? rows : null });
    }

    private async Task<(int itemCount, List<object> rows)> GetXAsync(string? orgId, string? reasonId, string? keyWord, string? flagActive, long recordStart, long recordCount)
    {
        var q = _db.Set<Mst_ResignReason>().AsNoTracking().AsQueryable();

        // OrgID: loc OrgID = ... (Mst_ResignReasonController.cs:781, 809-812)
        if (!string.IsNullOrWhiteSpace(orgId))
        {
            // toán tử '=' (Mst_ResignReasonController.cs:779-782, 811): không phân biệt hoa/thường như SQL Server
            var oid = PortQuery.Equal(orgId.Trim());
            q = q.Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), oid, PortQuery.Esc));
        }

        // ReasonID: loc ReasonID = ... (Mst_ResignReasonController.cs:805-808)
        if (!string.IsNullOrWhiteSpace(reasonId))
        {
            var rid = PortQuery.Equal(reasonId.Trim());
            q = q.Where(x => EF.Functions.Like((x.ReasonID ?? "").ToLower(), rid, PortQuery.Esc));
        }

        // KeyWord: loc ResignReason like '%...%' (Mst_ResignReasonController.cs:777)
        if (!string.IsNullOrWhiteSpace(keyWord))
        {
            var kw = PortQuery.Contains(keyWord);
            q = q.Where(x => EF.Functions.Like((x.ResignReason ?? "").ToLower(), kw, PortQuery.Esc));
        }

        // FlagActive: cat chuoi bang dau phay, dung toan tu IN (Mst_ResignReasonController.cs:789-794) / toán tử = ở By_ReasonID (dòng 815)
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
