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
// PORT 1:1 — Mst_TempPrintTypeController/GetAllActive (#245).
//
// Nguồn gate: Mst_TempPrintTypeController.cs:31-95 -> WA_Mst_TempPrintType_Get
// Nguồn API: MstTempPrintTypeController.cs:19-89
// Nguồn biz: MasterData.cs:46057-46161 (WAS_Mst_TempPrintType_Get), :45891-46055, :45736-45889
//
// GIỮ ĐÚNG NGUỒN:
//   * Gate KHÔNG thiết lập `Ft_WhereClause` (:55-69) cho API backend, do đó KHÔNG CÓ filter nào
//     được truyền (kể cả FlagActive = "1" dù tên method là GetAllActive).
//   * Data trả về được bọc trong một Object `new { Lst_Mst_TempPrintType = ... }` (:79-82).
//     Nếu rỗng, trả về null cho thuộc tính này chứ không phải mảng rỗng [] (:52, :73-76).
//   * GetX sắp xếp theo TempPrintType desc (:45796).
// ============================================================================================

[ApiController]
[Route("api/mst_tempprinttype")]
[Produces("application/json")]
public class Mst_TempPrintTypeControllerPort : ControllerBase
{
    private readonly AppDbContext _db;

    public Mst_TempPrintTypeControllerPort(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Port 1:1 <c>Mst_TempPrintTypeController.GetAllActive</c> (nguồn gate Mst_TempPrintTypeController.cs:31-95, #245).
    /// </summary>
    [HttpPost("getallactive")]
    public async Task<IActionResult> GetAllActive()
    {
        // Gate KHÔNG set Ft_WhereClause (:55-69), nghĩa là không truyền điều kiện lọc FlagActive="1" (sẽ lấy toàn bộ).
        // Gate set Ft_RecordStart = "0" và Ft_RecordCount. Ta truyền 123456000 làm mốc mặc định giống gate cũ.
        var (_, rows) = await GetXAsync(null, null, 0, 123456000);

        // Data trả về là 1 object bọc ngoài, Lst_Mst_TempPrintType = null nếu rỗng (:52, :73-82)
        return Ok(new
        {
            Success = true,
            Data = new
            {
                Lst_Mst_TempPrintType = rows.Count > 0 ? rows : null
            }
        });
    }

    /// <summary>
    /// Hàm dùng chung để lấy dữ liệu (giữ lại theo thiết kế GetX của backend).
    /// </summary>
    private async Task<(int itemCount, List<object> rows)> GetXAsync(string? keyWord, string? flagActive, long recordStart, long recordCount)
    {
        var q = _db.Set<Mst_TempPrintType>().AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyWord))
        {
            var kw = PortQuery.Contains(keyWord);
            q = q.Where(x => EF.Functions.Like((x.TempPrintName ?? "").ToLower(), kw, PortQuery.Esc));
        }

        if (!string.IsNullOrWhiteSpace(flagActive))
        {
            var fa = PortQuery.Equal(flagActive);
            q = q.Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), fa, PortQuery.Esc));
        }

        // Sắp xếp theo TempPrintType desc (MasterData.cs:45796: order by ifmo.TempPrintType desc)
        var ordered = q.OrderByDescending(x => x.TempPrintType);
        var itemCount = await ordered.CountAsync();

        var page = (recordCount <= 0 || recordStart < 0)
            ? new List<Mst_TempPrintType>()
            : await ordered.Skip((int)Math.Min(recordStart, int.MaxValue)).Take((int)Math.Min(recordCount, int.MaxValue)).ToListAsync();

        var rows = page
            .Select((x, i) => (object)new
            {
                MyIdxSeq = recordStart + i,
                x.TempPrintType,
                x.NetworkID,
                x.TempPrintName,
                x.Remark,
                x.FlagActive,
                x.LogLUDTimeUTC,
                x.LogLUBy,
            })
            .ToList();

        return (itemCount, rows);
    }
}
