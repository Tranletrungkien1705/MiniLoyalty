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
// PORT 1:1 — màn danh mục tham số hệ thống (Mst_ParamSys) của dự án nguồn 2022.7.Loyalty.
//
// Nguồn controller (tầng gate):
//   Mst_ParamSysController.cs:26-135 GetAllActive -> WA_Mst_ParamSys_Get
//   Mst_ParamSysController.cs:138-163 strWhereClause_Mst_ParamSys (hàm tạo điều kiện lọc)
// Nguồn API:
//   MstParamSysController.cs:19-90 WA_Mst_ParamSys_Get
// Nguồn biz:
//   Master.Loyalty.cs:12603 (WAS_Mst_ParamSys_Get) -> :12707 (Mst_ParamSys_Get)
//   -> :12861-13020 (Mst_ParamSys_GetX — SQL thật)
//
// GIỮ ĐÚNG NGUỒN:
//   * Endpoint là POST (nguồn [AcceptVerbs("POST")]).
//   * GetAllActive: gate truyền OrgID và FlagActive = "1" (:52-56). Tuy nhiên trong
//     strWhereClause_Mst_ParamSys (:138-163) KHÔNG sử dụng OrgID (chỉ xét KeyWord và FlagActive).
//     Do đó GetAllActive chỉ lọc FlagActive = "1" và KHÔNG lọc OrgID.
//   * Sắp xếp theo LogLUDTimeUTC desc (:12926, :12965). Hai dòng cùng thời điểm LogLUDTimeUTC:
//     nguồn không định thứ tự khi trùng LogLUDTimeUTC, ở đây sắp phụ thêm ParamCode asc để kết quả ổn định.
//   * Kết quả rỗng -> Data = null (:49, :75-77, :113-119).
//   * Mỗi dòng kết quả trả về MyIdxSeq + 6 cột của Mst_ParamSys (:12960-12961).
// ============================================================================================

[ApiController]
[Route("api/mst_paramsys")]
[Produces("application/json")]
public class Mst_ParamSysControllerPort(AppDbContext db) : ControllerBase
{
    // ============================================================================================
    // GetAllActive — nguồn Mst_ParamSysController.cs:26-135.
    // Gate: truyền OrgID = orgUserUI.OrgId (:54) và FlagActive = "1" (:55) vào strWhereClause_Mst_ParamSys (:52-56).
    // Hàm strWhereClause_Mst_ParamSys (:138-163) KHÔNG lọc OrgID.
    // Ft_RecordStart = "0", Ft_RecordCount = "123456000" (BaseController.cs:30-31, Mst_ParamSysController.cs:68-69).
    // Khi rỗng trả Data = null (:49, :75-77, :113-119).
    // ============================================================================================
    [HttpPost("getallactive")]
    public async Task<IActionResult> GetAllActive()
    {
        // GetAllActive gọi GetXAsync(null, "1", 0, 123456000); KHÔNG lọc OrgID (comment Mst_ParamSysController.cs:54)
        var (_, rows) = await GetXAsync(null, "1", 0, 123456000);

        // Nguồn: listMst_ParamSys = null khi rỗng (:49, :75-77), Data = listMst_ParamSys (:113, :119)
        return Ok(new { Success = true, Data = rows.Count > 0 ? rows : null });
    }

    /// <summary>
    /// Hàm đọc dữ liệu tương ứng Mst_ParamSys_GetX (Master.Loyalty.cs:12861-13020) để các action sau tái sử dụng.
    /// Lọc: KeyWord like trên ParamCode (:143-146) bằng PortQuery.Contains;
    ///      FlagActive IN (:152-160): tách dấu phẩy, ToLowerInvariant, ds.Contains((x.FlagActive ?? "").ToLower()).
    /// Sắp xếp: LogLUDTimeUTC desc (:12926, :12965); hai dòng cùng thời điểm LogLUDTimeUTC thì sắp phụ thêm ParamCode asc.
    /// Trả về (itemCount, rows): itemCount là tổng số dòng sau lọc; rows là danh sách phân trang kèm MyIdxSeq.
    /// </summary>
    private async Task<(int itemCount, List<object> rows)> GetXAsync(string? keyWord, string? flagActiveCsv, long recordStart, long recordCount)
    {
        var q = db.Set<Mst_ParamSys>().AsNoTracking().AsQueryable();

        // KeyWord like trên ParamCode (Mst_ParamSysController.cs:143-146) bằng PortQuery.Contains
        if (!string.IsNullOrWhiteSpace(keyWord))
        {
            var kw = PortQuery.Contains(keyWord);
            q = q.Where(x => EF.Functions.Like((x.ParamCode ?? "").ToLower(), kw, PortQuery.Esc));
        }

        // FlagActive IN (Mst_ParamSysController.cs:152-160): tách dấu phẩy, ToLowerInvariant
        if (!string.IsNullOrWhiteSpace(flagActiveCsv))
        {
            var ds = flagActiveCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => s.ToLowerInvariant())
                .ToList();
            if (ds.Count > 0)
            {
                q = q.Where(x => ds.Contains((x.FlagActive ?? "").ToLower()));
            }
        }

        // Sắp xếp theo LogLUDTimeUTC desc (Master.Loyalty.cs:12926, :12965).
        // Nguồn không định thứ tự khi trùng LogLUDTimeUTC; sắp phụ thêm ParamCode asc để kết quả ổn định.
        var ordered = q.OrderByDescending(x => x.LogLUDTimeUTC)
                       .ThenBy(x => x.ParamCode);

        var itemCount = await ordered.CountAsync();

        // Cắt trang theo recordStart và recordCount (nFilterRecordStart..nFilterRecordEnd tại Master.Loyalty.cs:12897-12898, :12939-12940)
        var page = (recordCount <= 0 || recordStart < 0)
            ? new List<Mst_ParamSys>()
            : await ordered.Skip((int)Math.Min(recordStart, int.MaxValue)).Take((int)Math.Min(recordCount, int.MaxValue)).ToListAsync();

        // Kết quả gồm MyIdxSeq + 6 cột của Mst_ParamSys (Master.Loyalty.cs:12960-12961)
        var rows = page
            .Select((x, i) => (object)new
            {
                MyIdxSeq = recordStart + i,
                x.ParamCode,
                x.NetworkID,
                x.ParamValue,
                x.FlagActive,
                x.LogLUDTimeUTC,
                x.LogLUBy,
            })
            .ToList();

        return (itemCount, rows);
    }
}
