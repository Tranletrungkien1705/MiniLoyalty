using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniLoyalty.Data;
using MiniLoyalty.Models.Port;

namespace MiniLoyalty.Controllers.Port;

/// <summary>
/// Tham số Search (Mst_ReligionCategoryController.cs:25, 52-57).
/// </summary>
public class Mst_ReligionCategorySearchReq
{
    public string? OrgID { get; set; }               // Tổ chức (Mst_ReligionCategoryController.cs:25). Nguồn nhận vào nhưng không lọc.
    public string? KeyWord { get; set; }             // Từ khoá lọc trên ReligionName (Mst_ReligionCategoryController.cs:176-179)
    public string? FlagActive { get; set; }          // Cờ hoạt động, lọc '=' (Mst_ReligionCategoryController.cs:180-183)
    public int Ft_PageIndex { get; set; } = 0;       // Mst_ReligionCategoryController.cs:46
    public int Ft_PageSize { get; set; } = 10;       // Mst_ReligionCategoryController.cs:47
}

[ApiController]
[Route("api/mst_religioncategory")]
[Produces("application/json")]
public class Mst_ReligionCategoryControllerPort : ControllerBase
{
    private readonly AppDbContext _db;

    public Mst_ReligionCategoryControllerPort(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] Mst_ReligionCategorySearchReq? req)
    {
        var r = req ?? new Mst_ReligionCategorySearchReq();
        var pageIndex = r.Ft_PageIndex;
        var pageSize = r.Ft_PageSize;

        // Bước 1: Gọi hàm GetXAsync làm draft + select cuối
        // Draft (MasterData.cs:42043-42044): start = RecordStart, count = RecordCount (Mst_ReligionCategoryController.cs:69-70)
        var start = (long)pageIndex * pageSize;
        var (itemCount, rows) = await GetXAsync(r.KeyWord, r.FlagActive, start, pageSize);

        // Tính pageCount (Mst_ReligionCategoryController.cs:83-87): Nếu pageSize = 0 thì gán ngầm = 10 để chia
        var ps = pageSize == 0 ? 10 : pageSize;
        var pageCount = rows.Count == 0 ? 0 : (itemCount % ps == 0 ? itemCount / ps : itemCount / ps + 1);

        // Trả DataList, ItemCount, PageCount (Mst_ReligionCategoryController.cs:89-92)
        return Ok(new { Success = true, Data = new PageInfo<object>(rows, itemCount, pageCount, pageIndex, pageSize) });
    }

    [HttpPost("getallactive")]
    public async Task<IActionResult> GetAllActive()
    {
        // Gate gọi cứng FlagActive="1" (Mst_ReligionCategoryController.cs:128-130).
        // RecordStart, RecordCount truyền xuống get data (Mst_ReligionCategoryController.cs:142-143).
        var (_, rows) = await GetXAsync(null, "1", 0, 123456000);

        // Data = lst_Mst_ReligionCategory (DANH SÁCH TRẦN, trả về null nếu rỗng) (Mst_ReligionCategoryController.cs:149-154).
        return Ok(new { Success = true, Data = rows.Count > 0 ? rows : null });
    }

    private async Task<(int itemCount, List<object> rows)> GetXAsync(string? keyWord, string? flagActive, long recordStart, long recordCount)
    {
        var q = _db.Set<Mst_ReligionCategory>().AsNoTracking().AsQueryable();

        // 1. OrgID: Nguồn có nhận vào Mst_ReligionCategorySearch nhưng KHÔNG hề sinh vào strWhereClause_Mst_ReligionCategory 
        // (Mst_ReligionCategoryController.cs:171-186) -> OrgID bỏ qua không lọc.

        // 2. KeyWord: lọc trên ReligionName like '%...%' (Mst_ReligionCategoryController.cs:178)
        if (!string.IsNullOrWhiteSpace(keyWord))
        {
            var kw = PortQuery.Contains(keyWord);
            q = q.Where(x => EF.Functions.Like((x.ReligionName ?? "").ToLower(), kw, PortQuery.Esc));
        }

        // 3. FlagActive: dùng = (nguồn Mst_ReligionCategoryController.cs:182), không dùng IN.
        if (!string.IsNullOrWhiteSpace(flagActive))
        {
            // Theo đề bài: "FlagActive dung ==."
            q = q.Where(x => x.FlagActive == flagActive);
        }

        // Bước Draft (MasterData.cs:42021-42031): order by mrc.ReligionCode asc
        var ordered = q.OrderBy(x => x.ReligionCode);
        var itemCount = await ordered.CountAsync();

        // Cắt trang từ [recordStart, recordStart+recordCount-1] (MasterData.cs:42043-42044)
        var page = (recordCount <= 0 || recordStart < 0)
            ? new List<Mst_ReligionCategory>()
            : await ordered.Skip((int)Math.Min(recordStart, int.MaxValue)).Take((int)Math.Min(recordCount, int.MaxValue)).ToListAsync();

        // Bước Select cuối (MasterData.cs:42061-42070): select t.MyIdxSeq, mrc.* order by t.MyIdxSeq asc
        var rows = page
            .Select((x, i) => (object)new
            {
                MyIdxSeq = recordStart + i,
                x.ReligionCode,
                x.NetworkID,
                x.ReligionName,
                x.ReligionDesc,
                x.FlagActive,
                x.LogLUDTimeUTC,
                x.LogLUBy
            })
            .ToList();

        return (itemCount, rows);
    }
}
