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
// PORT 1:1 — màn danh mục chính sách quy đổi tiền sang điểm (Mst_PolicyMoneyToPointService) của dự án nguồn 2022.7.Loyalty (#194).
//
// Nguồn controller (tầng gate):
//   Mst_PolicyMoneyToPointServiceController.cs:24-133     GetAllActive -> WA_Mst_PolicyMoneyToPointService_Get (#194)
//   Mst_PolicyMoneyToPointServiceController.cs:214-239    strWhereClause_Mst_PolicyMoneyToPointService (mệnh đề lọc)
// Nguồn API:
//   MstPolicyMoneyToPointServiceController.cs:94-164     WA_Mst_PolicyMoneyToPointService_Get
// Nguồn biz:
//   Master.Loyalty.cs:18347-18460                        WAS_Mst_PolicyMoneyToPointService_Get
//   Master.Loyalty.cs:18461-18617                        Mst_PolicyMoneyToPointService_Get
//   Master.Loyalty.cs:18618-18820                        Mst_PolicyMoneyToPointService_GetX (bản LIVE)
//
// ĐẶC ĐIỂM VÀ QUY TẮC NGHIỆP VỤ NGUỒN:
//   * Endpoint là POST (nguồn gate Mst_PolicyMoneyToPointServiceController.cs:24).
//   * GetAllActive: không nhận body (:24), Ft_RecordStart = "0", Ft_RecordCount = "123456000" (:66-67).
//   * ĐẶC BIỆT: Tại gate Mst_PolicyMoneyToPointServiceController.cs:53, dòng:
//         //FlagActive = "1",
//     BỊ COMMENT. Gate chỉ gán OrgID = orgUserUI.OrgId.ToString() (:52), nhưng hàm tạo điều kiện lọc
//     strWhereClause_Mst_PolicyMoneyToPointService (:214-239) CHỈ có 2 nhánh:
//       - KeyWord: like '%x%' trên PolicyCode (:219-222)
//       - FlagActive: in ('x', 'y') (:228-236)
//     Hàm lọc hoàn toàn KHÔNG xử lý OrgID. Do đó strWhereClause rỗng -> KHÔNG lọc bất kỳ điều kiện nào.
//     => GetAllActive trả về TẤT CẢ chính sách trong bảng, KỂ CẢ chính sách không hoạt động (FlagActive = '0').
//   * Khối Ext/tree tại gate (:76-108) BỊ COMMENT ở nguồn -> KHÔNG port.
//   * Dữ liệu trả về: mảng trần (gate :113-120: return Json(new { Success = true, Data = listMst_PolicyMoneyToPointService })),
//     rỗng thì null (:47, :73-75, :111).
//   * Nghiệp vụ trong Mst_PolicyMoneyToPointService_GetX:
//     - Bước 1 DRAFT (#tbl_Mst_PolicyMoneyToPointService_Filter_Draft, :18677-18686):
//       select distinct identity(bigint, 0, 1) MyIdxSeq, mpmtps.PolicyCode, mpmtps.LogLUDTimeUTC
//       từ Mst_PolicyMoneyToPointService với điều kiện lọc, sắp xếp mpmtps.LogLUDTimeUTC desc.
//       Cắt trang (#tbl_Mst_PolicyMoneyToPointService_Filter, :18693-18700): [recordStart, recordStart + recordCount - 1].
//     - Bước 2 SELECT HEADER (:18722-18729):
//       select t.MyIdxSeq, mpmtps.*
//       from #tbl_Mst_PolicyMoneyToPointService_Filter t
//       inner join Mst_PolicyMoneyToPointService mpmtps on t.PolicyCode = mpmtps.PolicyCode
//       order by mpmtps.LogLUDTimeUTC desc;
//       Chỉ xin Rt_Cols_Mst_PolicyMoneyToPointService = "*" (:70); KHÔNG xin Rt_Cols_Mst_PolicyMoneyToPointServiceDtl
//       -> Khối Dtl (:18742-18751) không chạy, kết quả chỉ gồm MyIdxSeq + 8 cột header.
//   * Bỏ qua có chủ đích (loại Đ):
//     - drAbilityOfUser (Master.Loyalty.cs:18661) nguồn gọi nhưng không dùng (:18673 bị comment) -> bỏ qua, ghi comment.
//     - FK CardType -> Mst_CardType, FK PolicyCode -> Mst_PolicyMoneyToPointService (20221026.CreateDatabase.sql:656-670)
//       chỉ ghi chú comment, chưa cấu hình FK navigation EF.
//     - CheckGatewayAuthentication, CheckAuthorize (gate :37-38), Sys_User_CheckAuthorize (:18527-18540):
//       hệ thống đích chưa có hệ phân quyền.
//     - Ghi RequestLog/ReturnLog (:18506-18513, :18607-18614).
// ============================================================================================

[ApiController]
[Route("api/mst_policymoneytopointservice")]
[Produces("application/json")]
public class Mst_PolicyMoneyToPointServiceControllerPort : ControllerBase
{
    private readonly AppDbContext _db;

    public Mst_PolicyMoneyToPointServiceControllerPort(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Port 1:1 <c>Mst_PolicyMoneyToPointServiceController.GetAllActive</c> (nguồn gate Mst_PolicyMoneyToPointServiceController.cs:24-133).
    /// Không nhận body. Dòng FlagActive = "1" bị comment tại gate (:53), hàm lọc strWhereClause_Mst_PolicyMoneyToPointService
    /// (:214-239) không lọc OrgID nên không lọc gì => trả toàn bộ chính sách (kể cả inactive).
    /// Ft_RecordStart = "0", Ft_RecordCount = "123456000" (:66-67).
    /// Trả Data là mảng trần, null nếu rỗng (gate :113-120).
    /// </summary>
    [HttpPost("getallactive")]
    public async Task<IActionResult> GetAllActive()
    {
        // Nguồn: Ft_RecordStart = "0", Ft_RecordCount = "123456000" (gate Mst_PolicyMoneyToPointServiceController.cs:66-67).
        // Dòng //FlagActive = "1" bị comment (:53), KeyWord null -> gọi GetXAsync(null, null, 0, 123456000).
        var (_, rows) = await GetXAsync(null, null, 0, 123456000);

        // Nguồn :113-120: Data = listMst_PolicyMoneyToPointService (mảng trần, null nếu rỗng).
        return Ok(new { Success = true, Data = rows.Count > 0 ? rows : null });
    }

    /// <summary>
    /// Thực hiện 2 bước truy vấn nghiệp vụ của Mst_PolicyMoneyToPointService_GetX (Master.Loyalty.cs:18618-18820).
    /// Bước 1: Draft từ Mst_PolicyMoneyToPointService (lọc KeyWord like PolicyCode, FlagActive IN, order by LogLUDTimeUTC desc, cắt trang).
    /// Bước 2: Select header từ #tbl_Mst_PolicyMoneyToPointService_Filter join Mst_PolicyMoneyToPointService theo PolicyCode, order by LogLUDTimeUTC desc.
    /// drAbilityOfUser (Master.Loyalty.cs:18661) nguồn gọi nhưng không dùng (:18673 bị comment) -> bỏ qua, không port.
    /// Chuẩn bị sẵn cho các hạng mục sau (Search, GetByPolicyCode).
    /// </summary>
    private async Task<(int itemCount, List<object> rows)> GetXAsync(string? keyWord, string? flagActiveCsv, long recordStart, long recordCount)
    {
        // drAbilityOfUser (Master.Loyalty.cs:18661) nguồn gọi nhưng bị comment không dùng (:18673) -> không port, ghi comment.

        // -----------------------------------------------------------------------------------------
        // BƯỚC 1: DRAFT (#tbl_Mst_PolicyMoneyToPointService_Filter_Draft) — Master.Loyalty.cs:18677-18686
        // select distinct identity(bigint, 0, 1) MyIdxSeq, mpmtps.PolicyCode, mpmtps.LogLUDTimeUTC
        // from Mst_PolicyMoneyToPointService mpmtps where (1=1) zzB_Where_strFilter_zzE order by mpmtps.LogLUDTimeUTC desc
        // -----------------------------------------------------------------------------------------
        var q = _db.Set<Mst_PolicyMoneyToPointService>().AsNoTracking().AsQueryable();

        // Lọc KeyWord: `PolicyCode LIKE '%x%'` (gate Mst_PolicyMoneyToPointServiceController.cs:219-222).
        if (!string.IsNullOrWhiteSpace(keyWord))
        {
            var kw = PortQuery.Contains(keyWord);
            q = q.Where(x => EF.Functions.Like((x.PolicyCode ?? "").ToLower(), kw, PortQuery.Esc));
        }

        // Lọc FlagActive: danh sách phân cách dấu phẩy -> IN ('a','b') (gate Mst_PolicyMoneyToPointServiceController.cs:228-236).
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

        // Sắp xếp mpmtps.LogLUDTimeUTC desc (Master.Loyalty.cs:18685)
        // Hòa trùng LogLUDTimeUTC: sắp phụ theo PolicyCode asc để thứ tự xác định
        var draftList = await q
            .OrderByDescending(x => x.LogLUDTimeUTC)
            .ThenBy(x => x.PolicyCode)
            .ToListAsync();

        // MyCount = Count(0) của bảng draft (#tbl_Mst_PolicyMoneyToPointService_Filter_Draft) — :18689
        var itemCount = draftList.Count;

        // Cắt trang draft (#tbl_Mst_PolicyMoneyToPointService_Filter, :18693-18700):
        // t.MyIdxSeq >= @nFilterRecordStart and t.MyIdxSeq <= @nFilterRecordEnd
        // với nFilterRecordStart = recordStart, nFilterRecordEnd = recordStart + recordCount - 1 (:18655-18656).
        var draftPage = (recordCount <= 0 || recordStart >= draftList.Count)
            ? new List<(long MyIdxSeq, Mst_PolicyMoneyToPointService Policy)>()
            : draftList
                .Select((p, idx) => (MyIdxSeq: (long)idx, Policy: p))
                .Where(x => x.MyIdxSeq >= recordStart && x.MyIdxSeq < recordStart + recordCount)
                .ToList();

        // -----------------------------------------------------------------------------------------
        // BƯỚC 2: SELECT HEADER (Master.Loyalty.cs:18722-18729)
        // select t.MyIdxSeq, mpmtps.*
        // from #tbl_Mst_PolicyMoneyToPointService_Filter t
        //   inner join Mst_PolicyMoneyToPointService mpmtps on t.PolicyCode = mpmtps.PolicyCode
        // order by mpmtps.LogLUDTimeUTC desc
        // -----------------------------------------------------------------------------------------
        var rows = draftPage
            .OrderByDescending(x => x.Policy.LogLUDTimeUTC)
            .ThenBy(x => x.Policy.PolicyCode)
            .Select(x => (object)new
            {
                x.MyIdxSeq,
                x.Policy.PolicyCode,
                x.Policy.NetworkID,
                x.Policy.EffDateStart,
                x.Policy.EffDateEnd,
                x.Policy.FlagActive,
                x.Policy.Remark,
                x.Policy.LogLUBy,
                x.Policy.LogLUDTimeUTC
            })
            .ToList();

        return (itemCount, rows);
    }
}
