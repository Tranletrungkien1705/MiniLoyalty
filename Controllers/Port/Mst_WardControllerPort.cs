using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniLoyalty.Data;
using MiniLoyalty.Models.Port;

namespace MiniLoyalty.Controllers.Port;

// ============================================================================================
// PORT 1:1 — màn danh mục phường/xã (Mst_Ward) của dự án nguồn 2022.7.Loyalty (#246, #247).
//
// Nguồn controller (tầng gate):
//   Mst_WardController.cs:24-84        GetAllActive -> WA_Mst_Ward_Get (#246)
//   Mst_WardController.cs:86-158       GetWardDetail -> WA_Mst_Ward_Get (#247)
//   Mst_WardController.cs:162-177      strWhereClause_Mst_Ward (mệnh đề lọc GetAllActive)
//   Mst_WardController.cs:179-198      strWhereClause_Mst_Ward_Detail (mệnh đề lọc GetWardDetail)
// Nguồn API:
//   MstWardController.cs:21-91         WA_Mst_Ward_Get
// Nguồn biz:
//   Master.1.cs:5755-5847              WAS_Mst_Ward_Get
//   Master.1.cs:5848-5998              Mst_Ward_Get
//   Master.1.cs:6000-6154              Mst_Ward_GetX (SQL excerpt :6054-6100)
//
// GIỮ ĐÚNG NGUỒN:
//   * Endpoint là POST (nguồn [AcceptVerbs("POST")]).
//   * GetAllActive: không nhận body (:24), Ft_RecordStart = "0", Ft_RecordCount = "123456000" (:41-42, :59-60).
//   * GetAllActive: Mệnh đề lọc strWhereClause_Mst_Ward (:162-177):
//     - KeyWord: like '%WardName%' (toán tử like :169)
//     - FlagActive: toán tử '=' (:173) — CÓ TÁC DỤNG, gate truyền FlagActive = "1" (:46)
//     - OrgID: gate nhận (:36, :57) nhưng strWhereClause_Mst_Ward KHÔNG lọc OrgID.
//   * GetWardDetail: body nhận { ProvinceCode, DistrictCode, FlagActive } (:86).
//     - Nếu ProvinceCode rỗng (null hoặc "") -> bỏ qua khối if (:106), listMst_Ward = null (:105)
//       => SetSuccess và objJsonResultEntry.Data = new { Lst_Mst_Ward = listMst_Ward } nằm NGOÀI if (:141-145)
//       -> trả Data = { Lst_Mst_Ward = null }.
//     - Nếu ProvinceCode không rỗng:
//       * Nguồn gán FlagActive = "1" nếu rỗng (:108-110).
//       * Gọi strWhereClause_Mst_Ward_Detail với { ProvinceCode, DistrictCode, FlagActive } (:112-117).
//       * Trong strWhereClause_Mst_Ward_Detail (:179-198):
//         + ProvinceCode = (toán tử = :184-187).
//         + DistrictCode = (toán tử = :188-191, có guard if (!IsNullOrEmpty(DistrictCode)) nên DistrictCode là tùy chọn).
//         + Dòng FlagActive BỊ COMMENT (:192-195) -> FlagActive KHÔNG có tác dụng lọc (phường '0' vẫn lấy được).
//       * Ft_RecordStart = "0", Ft_RecordCount = "123456000" (:129-130) -> lấy toàn bộ danh sách.
//       * Trả Data = new { Lst_Mst_Ward = rows.Count > 0 ? rows : null } (:136-139, :141-145).
//   * Chia trang theo WardCode asc (:6061 order by mo.WardCode asc).
//   * Select cuối: t.MyIdxSeq, mo.* (:6095-6100 inner join trên WardCode, order by t.MyIdxSeq asc).
//   * MyIdxSeq 0-based identity (:6055).
//   * Trả Data GetAllActive: lst_Mst_Ward (danh sách trần, null nếu rỗng — :43, :66-71).
//
// Bỏ qua có chủ đích (loại Đ):
//   - CheckGatewayAuthentication: gate Mst_WardController.cs:33, 95; API MstWardController.cs:43
//   - CheckAuthorize: gate Mst_WardController.cs:34, 96 (hệ thống đích chưa có hệ phân quyền)
//   - Ghi RequestLog/ReturnLog: Master.1.cs:5892, 5988
//   - Sys_User_CheckAuthorize: Master.1.cs:5911-5925
// ============================================================================================

/// <summary>Port 1:1 <c>GetWardDetail</c> — body nhận ProvinceCode, DistrictCode, FlagActive (Mst_WardController.cs:86-158).</summary>
public class MstWardGetWardDetailReq
{
    public string? ProvinceCode { get; set; }        // Mã tỉnh/thành — :86, :106, :114
    public string? DistrictCode { get; set; }        // Mã quận/huyện — :86, :115 (tùy chọn)
    public string? FlagActive { get; set; }          // Cờ hoạt động — :86, :108-110, :116 (nguồn gán "1" nếu rỗng nhưng strWhereClause_Mst_Ward_Detail bị comment không lọc)
}

[ApiController]
[Route("api/mst_ward")]
[Produces("application/json")]
public class Mst_WardControllerPort(AppDbContext db) : ControllerBase
{
    // ============================================================================================
    // GetAllActive — nguồn Mst_WardController.cs:24-84 (#246).
    // Mệnh đề lọc: strWhereClause_Mst_Ward (Mst_WardController.cs:162-177).
    // Gate:
    //   - Không nhận body: gọi trực tiếp [HttpPost("getallactive")].
    //   - Gọi strWhereClause_Mst_Ward với { FlagActive = "1" } (:44-47).
    //   - Trong strWhereClause_Mst_Ward (:171-174), FlagActive được lọc với toán tử '=' (CÓ TÁC DỤNG).
    //   - Ft_RecordStart = "0", Ft_RecordCount = "123456000" (:41-42, :59-60) -> lấy toàn bộ phường/xã có cờ hoạt động '1'.
    //   - Trả Data = lst_Mst_Ward (danh sách trần, null nếu rỗng — :43, :66-71).
    // ============================================================================================
    [HttpPost("getallactive")]
    public async Task<IActionResult> GetAllActive()
    {
        // Nguồn Mst_WardController.cs:44-47: strWhereClause_Mst_Ward với FlagActive = "1".
        // Ft_RecordStart = "0", Ft_RecordCount = "123456000" (gate :41-42, :59-60).
        var (_, rows) = await GetXAsync(null, "1", 0, 123456000);

        // Nguồn Mst_WardController.cs:66-71:
        // if (objRT_Mst_Ward.Lst_Mst_Ward != null && objRT_Mst_Ward.Lst_Mst_Ward.Count > 0)
        //     lst_Mst_Ward = objRT_Mst_Ward.Lst_Mst_Ward;
        // objJsonResultEntry.Data = lst_Mst_Ward; (danh sách trần, null nếu rỗng)
        return Ok(new { Success = true, Data = rows.Count > 0 ? rows : null });
    }

    // ============================================================================================
    // GetWardDetail — nguồn Mst_WardController.cs:86-158 (#247).
    // Mệnh đề lọc: strWhereClause_Mst_Ward_Detail (Mst_WardController.cs:179-198).
    // Gate:
    //   - Body nhận { ProvinceCode, DistrictCode, FlagActive } (:86).
    //   - Nếu ProvinceCode rỗng (null hoặc "") -> bỏ qua khối if (:106), listMst_Ward = null (:105)
    //     => objJsonResultEntry.Data = new { Lst_Mst_Ward = null } (:141-145).
    //   - Nếu ProvinceCode không rỗng:
    //     * Nguồn gán: if (IsNullOrEmpty(FlagActive)) FlagActive = "1" (:108-110).
    //     * Gọi strWhereClause_Mst_Ward_Detail với { ProvinceCode, DistrictCode, FlagActive } (:112-117).
    //     * Trong strWhereClause_Mst_Ward_Detail (:179-198):
    //       - ProvinceCode: if (!CUtils.IsNullOrEmpty(data.ProvinceCode)) -> lọc '=' (:184-187).
    //       - DistrictCode: if (!CUtils.IsNullOrEmpty(data.DistrictCode)) -> lọc '=' (:188-191) (có guard, tùy chọn).
    //       - FlagActive: dòng lọc FlagActive bị COMMENT (:192-195) -> KHÔNG lọc cờ hoạt động (phường '0' vẫn lấy được).
    //     * Ft_RecordStart = "0", Ft_RecordCount = "123456000" (:129-130) -> lấy toàn bộ.
    //     * Trả Data = new { Lst_Mst_Ward = rows.Count > 0 ? rows : null } (:136-139, :141-145).
    // ============================================================================================
    [HttpPost("getwarddetail")]
    public async Task<IActionResult> GetWardDetail([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] MstWardGetWardDetailReq? req)
    {
        // Nguồn Mst_WardController.cs:105-106: Nếu ProvinceCode rỗng thì không truy vấn CSDL,
        // listMst_Ward giữ null (:105), Data được gán ngoài khối if (:141-145) -> { Lst_Mst_Ward = null }.
        if (string.IsNullOrWhiteSpace(req?.ProvinceCode))
        {
            return Ok(new { Success = true, Data = new { Lst_Mst_Ward = (List<object>?)null } });
        }

        // Gọi GetXAsync với Ft_RecordStart = "0", Ft_RecordCount = "123456000" (gate :129-130),
        // provinceCodeEqual = req.ProvinceCode, districtCodeEqual = req.DistrictCode.
        // Chú ý cảnh báo: Nguồn gán FlagActive = "1" nếu rỗng (:108-110) nhưng strWhereClause_Mst_Ward_Detail (:192-195)
        // bị comment không lọc FlagActive, do đó KHÔNG truyền FlagActive vào GetXAsync (truyền null) để trả mọi phường kể cả cờ '0'.
        // DistrictCode có guard if (!IsNullOrEmpty(DistrictCode)) ở nguồn (:188-191) nên req.DistrictCode rỗng/null sẽ không lọc theo DistrictCode.
        var (_, rows) = await GetXAsync(
            keyWord: null,
            flagActive: null,
            recordStart: 0,
            recordCount: 123456000,
            districtCodeEqual: string.IsNullOrWhiteSpace(req.DistrictCode) ? null : req.DistrictCode,
            provinceCodeEqual: req.ProvinceCode);

        // Nguồn Mst_WardController.cs:136-145: Data = new { Lst_Mst_Ward = rows.Count > 0 ? rows : null }
        return Ok(new { Success = true, Data = new { Lst_Mst_Ward = rows.Count > 0 ? rows : null } });
    }

    /// <summary>
    /// Hàm dùng chung lấy dữ liệu Mst_Ward hỗ trợ phân trang và tìm kiếm.
    /// Nguồn SQL: Master.1.cs:6054-6100 (Mst_Ward_GetX).
    /// </summary>
    public async Task<(int ItemCount, List<object> Rows)> GetXAsync(
        string? keyWord,
        string? flagActive,
        long recordStart,
        long recordCount,
        string? districtCodeEqual = null,
        string? provinceCodeEqual = null,
        string? wardCodeEqual = null)
    {
        var q = db.Set<Mst_Ward>().AsNoTracking().AsQueryable();

        // KeyWord -> like '%x%' trên WardName (Mst_WardController.cs:167-170).
        // Nguồn chạy SQL Server collation KHÔNG phân biệt hoa/thường ⇒ hạ chữ cả 2 phía rồi LIKE (bẫy #18/#31).
        if (!string.IsNullOrWhiteSpace(keyWord))
        {
            var kw = PortQuery.Contains(keyWord);
            q = q.Where(x => EF.Functions.Like((x.WardName ?? "").ToLower(), kw, PortQuery.Esc));
        }

        // WardCode -> `WardCode = 'x'`
        if (!string.IsNullOrWhiteSpace(wardCodeEqual))
        {
            var wc = PortQuery.Equal(wardCodeEqual);
            q = q.Where(x => EF.Functions.Like((x.WardCode ?? "").ToLower(), wc, PortQuery.Esc));
        }

        // DistrictCode -> `DistrictCode = 'x'`
        if (!string.IsNullOrWhiteSpace(districtCodeEqual))
        {
            var dc = PortQuery.Equal(districtCodeEqual);
            q = q.Where(x => EF.Functions.Like((x.DistrictCode ?? "").ToLower(), dc, PortQuery.Esc));
        }

        // ProvinceCode -> `ProvinceCode = 'x'`
        if (!string.IsNullOrWhiteSpace(provinceCodeEqual))
        {
            var pc = PortQuery.Equal(provinceCodeEqual);
            q = q.Where(x => EF.Functions.Like((x.ProvinceCode ?? "").ToLower(), pc, PortQuery.Esc));
        }

        // FlagActive -> toán tử '=' (Mst_WardController.cs:171-174):
        // EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal(flagActive), PortQuery.Esc)
        if (!string.IsNullOrWhiteSpace(flagActive))
        {
            var fa = PortQuery.Equal(flagActive);
            q = q.Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), fa, PortQuery.Esc));
        }

        // OrgID: nhận ở gate (Mst_WardController.cs:36, :57) nhưng strWhereClause_Mst_Ward (:162-177) KHÔNG lọc OrgID.

        // Chia trang theo WardCode asc (Mst_Ward_GetX SQL: :6061 order by mo.WardCode asc).
        var ordered = q.OrderBy(x => x.WardCode);
        var itemCount = await ordered.CountAsync();

        // MyIdxSeq = số thứ tự 0-based trong thứ tự WardCode (identity(bigint, 0, 1) ở SQL nguồn :6055).
        // Lấy dòng [recordStart, recordStart+recordCount) theo thứ tự WardCode.
        var page = (recordCount <= 0 || recordStart < 0)
            ? new List<Mst_Ward>()
            : await ordered.Skip((int)Math.Min(recordStart, int.MaxValue)).Take((int)Math.Min(recordCount, int.MaxValue)).ToListAsync();

        // Select cuối theo t.MyIdxSeq asc (:6100 order by t.MyIdxSeq asc) — select t.MyIdxSeq, mo.*.
        var rows = page
            .Select((x, i) => (object)new
            {
                MyIdxSeq = recordStart + i,
                x.WardCode,
                x.NetworkID,
                x.ProvinceCode,
                x.DistrictCode,
                x.WardName,
                x.FlagActive,
                x.LogLUDTimeUTC,
                x.LogLUBy,
            })
            .ToList();

        return (itemCount, rows);
    }
}
