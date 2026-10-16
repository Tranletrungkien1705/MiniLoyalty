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
// PORT 1:1 — Mst_QualificationFormController/Search (#225).
//
// Nguồn controller (tầng gate):
//   Mst_QualificationFormController.cs:25-105     Search -> WA_Mst_QualificationForm_Get
//   Mst_QualificationFormController.cs:171-186    strWhereClause_Mst_QualificationForm
// Nguồn API:
//   MstQualificationFormController.cs:17-88       WA_Mst_QualificationForm_Get
// Nguồn biz:
//   MasterData.cs:44031-44186                     Mst_QualificationForm_GetX
// ============================================================================================

public class Mst_QualificationFormSearchReq
{
    public string? OrgID { get; set; }               // Gate nhận (:25, :55) nhưng strWhereClause_Mst_QualificationForm (:171-186) KHÔNG dùng -> KHÔNG lọc
    public string? KeyWord { get; set; }             // Từ khóa tìm theo QualificationFormName (:178)
    public string? FlagActive { get; set; }          // Cờ hoạt động, toán tử '=' nguyên chuỗi (:182)
    public int Ft_PageIndex { get; set; } = 0;       // :25, :46
    public int Ft_PageSize { get; set; } = 10;       // :25, :47
}

[ApiController]
[Route("api/mst_qualificationform")]
[Produces("application/json")]
public class Mst_QualificationFormControllerPort : ControllerBase
{
    private readonly AppDbContext _db;

    public Mst_QualificationFormControllerPort(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] Mst_QualificationFormSearchReq? req)
    {
        var r = req ?? new Mst_QualificationFormSearchReq();
        var pageIndex = r.Ft_PageIndex;
        var pageSize = r.Ft_PageSize;

        var start = (long)pageIndex * pageSize;
        var (itemCount, rows) = await GetXAsync(r.KeyWord, r.FlagActive, start, pageSize);

        // Nguồn xử lý page size: gate :83-88
        var ps = pageSize == 0 ? 10 : pageSize;
        var pageCount = rows.Count == 0 ? 0 : (itemCount % ps == 0 ? itemCount / ps : itemCount / ps + 1);

        return Ok(new { Success = true, Data = new PageInfo<object>(rows, itemCount, pageCount, pageIndex, pageSize) });
    }

    private async Task<(int itemCount, List<object> rows)> GetXAsync(string? keyWord, string? flagActive, long recordStart, long recordCount)
    {
        var q = _db.Set<Mst_QualificationForm>().AsNoTracking().AsQueryable();

        // KeyWord -> LIKE %...% trên QualificationFormName (Mst_QualificationFormController.cs:178)
        if (!string.IsNullOrWhiteSpace(keyWord))
        {
            var kw = PortQuery.Contains(keyWord);
            q = q.Where(x => EF.Functions.Like((x.QualificationFormName ?? "").ToLower(), kw, PortQuery.Esc));
        }

        // FlagActive -> '=' nguyên chuỗi trên FlagActive (Mst_QualificationFormController.cs:182), KHÔNG IN/tách dấu phẩy
        if (!string.IsNullOrWhiteSpace(flagActive))
        {
            var fa = PortQuery.Equal(flagActive);
            q = q.Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), fa, PortQuery.Esc));
        }

        // Chia trang theo QualificationFormCode asc (MasterData.cs:44093)
        var ordered = q.OrderBy(x => x.QualificationFormCode);
        var itemCount = await ordered.CountAsync();

        // Lấy đoạn [recordStart, recordStart+recordCount) với MyIdxSeq 0-based
        // MasterData.cs:44106-44107 -> t.MyIdxSeq >= start and t.MyIdxSeq <= end
        var page = (recordCount <= 0 || recordStart < 0)
            ? new List<Mst_QualificationForm>()
            : await ordered.Skip((int)Math.Min(recordStart, int.MaxValue)).Take((int)Math.Min(recordCount, int.MaxValue)).ToListAsync();

        // Select cuối order by t.MyIdxSeq asc trả về 6 cột thật + MyIdxSeq (MasterData.cs:44126-44132)
        var rows = page
            .Select((x, i) => (object)new
            {
                MyIdxSeq = recordStart + i,
                x.QualificationFormCode,
                x.NetworkID,
                x.QualificationFormName,
                x.FlagActive,
                x.LogLUDTimeUTC,
                x.LogLUBy
            })
            .ToList();

        return (itemCount, rows);
    }
}
