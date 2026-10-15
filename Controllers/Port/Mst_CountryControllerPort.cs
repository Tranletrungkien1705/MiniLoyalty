using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniLoyalty.Data;
using MiniLoyalty.Models.Port;

namespace MiniLoyalty.Controllers.Port;

// ============================================================================================
// PORT 1:1 — màn danh mục quốc gia (Mst_Country) của dự án nguồn 2022.7.Loyalty (#109).
//
// Nguồn controller (tầng gate):
//   Mst_CountryController.cs:25-85     GetAllActive -> WA_Mst_Country_Get (#109)
//   Mst_CountryController.cs:89-100    strWhereClause_Mst_Country_Active (mệnh đề lọc GetAllActive)
// Nguồn API:
//   MstCountryController.cs:20-91      WA_Mst_Country_Get
// Nguồn biz:
//   Master.cs:3987-4086                WAS_Mst_Country_Get
//   Master.cs:3298-3526                Mst_Country_Get (SQL excerpt :3386-3432)
//
// GIỮ ĐÚNG NGUỒN:
//   * Endpoint là POST (nguồn [AcceptVerbs("POST")]).
//   * GetAllActive: không nhận body (:25), Ft_RecordStart = "0", Ft_RecordCount = "123456000" (:41-42, :60-61).
//   * GetAllActive: Mệnh đề lọc strWhereClause_Mst_Country_Active (:89-100):
//     - FlagActive: toán tử '=' (:96) — CÓ TÁC DỤNG, gate truyền FlagActive = "1" (:47).
//     - OrgID: gate nhận (:37, :58) nhưng strWhereClause_Mst_Country_Active KHÔNG lọc OrgID.
//   * Chia trang theo CountryCode asc (:3393 order by mc.CountryCode asc).
//   * Select cuối: t.MyIdxSeq, mc.* (:3427-3432 inner join trên CountryCode, order by t.MyIdxSeq asc).
//   * MyIdxSeq 0-based identity (:3387).
//   * Trả Data GetAllActive: lst_Mst_Country (danh sách trần, null nếu rỗng — :44, :67-72).
//
// Bỏ qua có chủ đích (loại Đ):
//   - CheckGatewayAuthentication: gate Mst_CountryController.cs:34; API MstCountryController.cs:43
//   - CheckAuthorize: gate Mst_CountryController.cs:35 (hệ thống đích chưa có hệ phân quyền)
//   - Ghi RequestLog/ReturnLog: Master.cs:3339, 3516
//   - Sys_Access_CheckDenyV30: Master.cs:3356
// ============================================================================================

[ApiController]
[Route("api/mst_country")]
[Produces("application/json")]
public class Mst_CountryControllerPort(AppDbContext db) : ControllerBase
{
    // ============================================================================================
    // GetAllActive — nguồn Mst_CountryController.cs:25-85 (#109).
    // Mệnh đề lọc: strWhereClause_Mst_Country_Active (Mst_CountryController.cs:89-100).
    // Gate:
    //   - Không nhận body: gọi trực tiếp [HttpPost("getallactive")].
    //   - Gọi strWhereClause_Mst_Country_Active với { FlagActive = "1" } (:45-48).
    //   - Trong strWhereClause_Mst_Country_Active (:94-97), FlagActive được lọc với toán tử '=' (CÓ TÁC DỤNG).
    //   - Ft_RecordStart = "0", Ft_RecordCount = "123456000" (:41-42, :60-61) -> lấy toàn bộ quốc gia có cờ hoạt động '1'.
    //   - Trả Data = lst_Mst_Country (danh sách trần, null nếu rỗng — :44, :67-72).
    // ============================================================================================
    [HttpPost("getallactive")]
    public async Task<IActionResult> GetAllActive()
    {
        // Nguồn Mst_CountryController.cs:45-48: strWhereClause_Mst_Country_Active với FlagActive = "1".
        // Ft_RecordStart = "0", Ft_RecordCount = "123456000" (gate :41-42, :60-61).
        var (_, rows) = await GetXAsync("1", 0, 123456000);

        // Nguồn Mst_CountryController.cs:67-72:
        // if (objRT_Mst_Country.Lst_Mst_Country != null && objRT_Mst_Country.Lst_Mst_Country.Count > 0)
        //     lst_Mst_Country = objRT_Mst_Country.Lst_Mst_Country;
        // objJsonResultEntry.Data = lst_Mst_Country; (danh sách trần, null nếu rỗng)
        return Ok(new { Success = true, Data = rows.Count > 0 ? rows : null });
    }

    /// <summary>
    /// Hàm dùng chung lấy dữ liệu Mst_Country hỗ trợ phân trang và tìm kiếm.
    /// Nguồn SQL: Master.cs:3386-3432 (Mst_Country_Get).
    /// </summary>
    public async Task<(int ItemCount, List<object> Rows)> GetXAsync(
        string? flagActive,
        long recordStart,
        long recordCount)
    {
        var q = db.Set<Mst_Country>().AsNoTracking().AsQueryable();

        // FlagActive -> toán tử '=' (Mst_CountryController.cs:94-97):
        // EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal(flagActive), PortQuery.Esc)
        if (!string.IsNullOrWhiteSpace(flagActive))
        {
            var fa = PortQuery.Equal(flagActive);
            q = q.Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), fa, PortQuery.Esc));
        }

        // Chia trang theo CountryCode asc (Mst_Country_Get SQL: :3393 order by mc.CountryCode asc).
        var ordered = q.OrderBy(x => x.CountryCode);
        var itemCount = await ordered.CountAsync();

        // MyIdxSeq = số thứ tự 0-based trong thứ tự CountryCode (identity(bigint, 0, 1) ở SQL nguồn :3387).
        // Lấy dòng [recordStart, recordStart+recordCount) theo thứ tự CountryCode.
        var page = recordCount <= 0
            ? new List<Mst_Country>()
            : await ordered.Skip((int)recordStart).Take((int)recordCount).ToListAsync();

        // Select cuối theo t.MyIdxSeq asc (:3432 order by t.MyIdxSeq asc) — select t.MyIdxSeq, mc.*.
        var rows = page
            .Select((x, i) => (object)new
            {
                MyIdxSeq = recordStart + i,
                x.CountryCode,
                x.CountryName,
                x.FlagActive,
                x.LogLUDTimeUTC,
                x.LogLUBy,
            })
            .ToList();

        return (itemCount, rows);
    }
}
