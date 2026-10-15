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
// PORT 1:1 — màn danh mục chính sách quy đổi tiền sang điểm (Mst_PolicyMoneyToPointService) của dự án nguồn 2022.7.Loyalty (#194, #195).
//
// Nguồn controller (tầng gate):
//   Mst_PolicyMoneyToPointServiceController.cs:24-133     GetAllActive -> WA_Mst_PolicyMoneyToPointService_Get (#194)
//   Mst_PolicyMoneyToPointServiceController.cs:134-210    GetByPolicyCode -> WA_Mst_PolicyMoneyToPointService_Get (#195)
//   Mst_PolicyMoneyToPointServiceController.cs:214-239    strWhereClause_Mst_PolicyMoneyToPointService (mệnh đề lọc GetAllActive)
//   Mst_PolicyMoneyToPointServiceController.cs:240-255    strWhereClause_Mst_PolicyMoneyToPointService_By_PolicyCode (mệnh đề lọc GetByPolicyCode)
// Nguồn API:
//   MstPolicyMoneyToPointServiceController.cs:94-164     WA_Mst_PolicyMoneyToPointService_Get
// Nguồn biz:
//   Master.Loyalty.cs:18347-18460                        WAS_Mst_PolicyMoneyToPointService_Get
//   Master.Loyalty.cs:18461-18617                        Mst_PolicyMoneyToPointService_Get
//   Master.Loyalty.cs:18618-18820                        Mst_PolicyMoneyToPointService_GetX (bản LIVE)
//
// ĐẶC ĐIỂM VÀ QUY TẮC NGHIỆP VỤ NGUỒN:
//   * Endpoint là POST (nguồn gate Mst_PolicyMoneyToPointServiceController.cs:24, :134).
//   * GetAllActive (#194): không nhận body (:24), Ft_RecordStart = "0", Ft_RecordCount = "123456000" (:66-67).
//     Dòng //FlagActive = "1" bị comment tại gate (:53), hàm lọc strWhereClause_Mst_PolicyMoneyToPointService
//     không lọc OrgID nên không lọc gì => trả toàn bộ chính sách (kể cả inactive).
//     Trả Data là mảng trần (gate :113-120), rỗng thì null.
//   * GetByPolicyCode (#195): [HttpPost("getbypolicycode")], body { PolicyCode }, EmptyBodyBehavior.Allow (gate :134-210).
//     - PolicyCode rỗng -> KHÔNG truy vấn, Data = { Mst_PolicyMoneyToPointService: null, Lst_Mst_PolicyMoneyToPointService: null } (:153-154, :193-197).
//     - Ngược lại lọc strWhereClause_Mst_PolicyMoneyToPointService_By_PolicyCode CHỈ với PolicyCode -> PolicyCode = (:247;
//       nhánh FlagActive :249-252 không truyền -> không lọc FlagActive -> chính sách inactive như P02 VẪN lấy được).
//       Ft_RecordStart = "0", Ft_RecordCount = "1" (:173-174).
//       Xin CẢ header và Dtl: Rt_Cols_Mst_PolicyMoneyToPointService = "*", Rt_Cols_Mst_PolicyMoneyToPointServiceDtl = "*" (:177-178).
//     - Kết quả (:182-197):
//       Mst_PolicyMoneyToPointService = header[0] hoặc null.
//       Lst_Mst_PolicyMoneyToPointService = danh sách DTL (nguồn đặt nhầm tên biến, GIỮ NGUYÊN tên trường JSON) hoặc null nếu rỗng.
//     - Biz GetX (:18742-18751): Dtl = t.MyIdxSeq, mpmtpsl.* từ draft inner join Dtl on PolicyCode, order by mpmtpsl.LogLUDTimeUTC desc
//       (hòa trùng: sắp phụ CardType asc, ghi comment).
//   * Bỏ qua có chủ đích (loại Đ):
//     - drAbilityOfUser (Master.Loyalty.cs:18661) nguồn gọi nhưng không dùng (:18673 bị comment) -> bỏ qua, ghi comment.
//     - FK CardType -> Mst_CardType, FK PolicyCode -> Mst_PolicyMoneyToPointService (20221026.CreateDatabase.sql:656-670)
//       chỉ ghi chú comment, chưa cấu hình FK navigation EF.
//     - CheckGatewayAuthentication, CheckAuthorize (gate :37-38, :143-144), Sys_User_CheckAuthorize (:18527-18540):
//       hệ thống đích chưa có hệ phân quyền.
//     - Ghi RequestLog/ReturnLog (:18506-18513, :18607-18614).
// ============================================================================================

/// <summary>Port 1:1 <c>GetByPolicyCode</c> — body nhận PolicyCode (Mst_PolicyMoneyToPointServiceController.cs:134).</summary>
public class MstPolicyMoneyToPointServiceGetByPolicyCodeReq
{
    public string? PolicyCode { get; set; }
}

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
    /// Port 1:1 <c>Mst_PolicyMoneyToPointServiceController.GetAllActive</c> (nguồn gate Mst_PolicyMoneyToPointServiceController.cs:24-133, #194).
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
    /// Port 1:1 <c>Mst_PolicyMoneyToPointServiceController.GetByPolicyCode</c> (nguồn gate Mst_PolicyMoneyToPointServiceController.cs:134-210, #195).
    /// Body JSON nhận PolicyCode, EmptyBodyBehavior.Allow.
    /// PolicyCode rỗng -> KHÔNG truy vấn, Data = { Mst_PolicyMoneyToPointService: null, Lst_Mst_PolicyMoneyToPointService: null } (gate :153-154, :193-197).
    /// Ngược lại lọc PolicyCode = (Like + PortQuery.Equal, :247); không lọc FlagActive -> chính sách inactive vẫn lấy được.
    /// Ft_RecordStart = "0", Ft_RecordCount = "1" (:173-174).
    /// Xin cả header và Dtl (Rt_Cols_* = "*", gate :177-178).
    /// Kết quả:
    ///   - Data.Mst_PolicyMoneyToPointService: header[0] hoặc null (:182-185).
    ///   - Data.Lst_Mst_PolicyMoneyToPointService: danh sách DTL (nguồn đặt nhầm tên biến, GIỮ NGUYÊN tên trường JSON) hoặc null nếu rỗng (:186-189, :196).
    /// </summary>
    [HttpPost("getbypolicycode")]
    public async Task<IActionResult> GetByPolicyCode(
        [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] MstPolicyMoneyToPointServiceGetByPolicyCodeReq? req)
    {
        var policyCode = req?.PolicyCode;

        // PolicyCode rỗng -> nguồn không truy vấn (:156) và trả cả 2 trường null (:153-154, :193-197).
        if (string.IsNullOrWhiteSpace(policyCode))
        {
            return Ok(new
            {
                Success = true,
                Data = new
                {
                    Mst_PolicyMoneyToPointService = (object?)null,
                    Lst_Mst_PolicyMoneyToPointService = (object?)null
                }
            });
        }

        // Lấy 1 dòng theo PolicyCode (Ft_RecordStart = "0", Ft_RecordCount = "1" — :173-174).
        // getDtl = true -> xin thêm danh sách Dtl (Rt_Cols_Mst_PolicyMoneyToPointServiceDtl = "*" — :178).
        // Nhánh FlagActive không truyền ở gate (:158-161) nên không lọc FlagActive -> chính sách inactive vẫn lấy được.
        var (_, rows, dtlRows) = await GetXAsync(null, null, 0, 1, policyCodeEqual: policyCode, getDtl: true);

        // Nguồn gate :182-189, :193-197:
        // Mst_PolicyMoneyToPointService = header[0] hoặc null.
        // Lst_Mst_PolicyMoneyToPointService = danh sách DTL (nguồn đặt nhầm tên biến Lst_Mst_PolicyMoneyToPointService
        // nhưng kiểu List<Mst_PolicyMoneyToPointServiceDtl>, GIỮ NGUYÊN tên trường JSON này) hoặc null nếu rỗng.
        var objMst_PolicyMoneyToPointService = rows.Count > 0 ? rows[0] : null;
        var lst_Mst_PolicyMoneyToPointService = dtlRows.Count > 0 ? dtlRows : null;

        return Ok(new
        {
            Success = true,
            Data = new
            {
                Mst_PolicyMoneyToPointService = objMst_PolicyMoneyToPointService,
                Lst_Mst_PolicyMoneyToPointService = lst_Mst_PolicyMoneyToPointService
            }
        });
    }

    /// <summary>
    /// Overload giữ nguyên tương thích cho GetAllActive (#194).
    /// </summary>
    private async Task<(int itemCount, List<object> rows)> GetXAsync(string? keyWord, string? flagActiveCsv, long recordStart, long recordCount)
    {
        var (itemCount, rows, _) = await GetXAsync(keyWord, flagActiveCsv, recordStart, recordCount, null, false);
        return (itemCount, rows);
    }

    /// <summary>
    /// Thực hiện các bước truy vấn nghiệp vụ của Mst_PolicyMoneyToPointService_GetX (Master.Loyalty.cs:18618-18820).
    /// Bước 1: Draft từ Mst_PolicyMoneyToPointService (lọc KeyWord like PolicyCode, FlagActive IN, PolicyCode = policyCodeEqual, order by LogLUDTimeUTC desc, cắt trang).
    /// Bước 2: Select header từ #tbl_Mst_PolicyMoneyToPointService_Filter join Mst_PolicyMoneyToPointService theo PolicyCode, order by LogLUDTimeUTC desc.
    /// Bước 3: Nếu getDtl = true, select chi tiết Dtl từ #tbl_Mst_PolicyMoneyToPointService_Filter join Mst_PolicyMoneyToPointServiceDtl theo PolicyCode,
    ///         order by LogLUDTimeUTC desc (hòa trùng: sắp phụ theo CardType asc, ghi comment theo đề bài) (Master.Loyalty.cs:18742-18751).
    /// drAbilityOfUser (Master.Loyalty.cs:18661) nguồn gọi nhưng không dùng (:18673 bị comment) -> bỏ qua, không port.
    /// </summary>
    private async Task<(int itemCount, List<object> rows, List<object> dtlRows)> GetXAsync(
        string? keyWord,
        string? flagActiveCsv,
        long recordStart,
        long recordCount,
        string? policyCodeEqual = null,
        bool getDtl = false)
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

        // Lọc PolicyCode: `PolicyCode = 'x'` (gate Mst_PolicyMoneyToPointServiceController.cs:247 — strWhereClause_Mst_PolicyMoneyToPointService_By_PolicyCode).
        // Nguồn dùng `=` trên SQL Server collation KHÔNG phân biệt hoa/thường ⇒ Like KHÔNG wildcard, hạ chữ 2 phía (PortQuery.Equal).
        if (!string.IsNullOrWhiteSpace(policyCodeEqual))
        {
            var pc = PortQuery.Equal(policyCodeEqual);
            q = q.Where(x => EF.Functions.Like((x.PolicyCode ?? "").ToLower(), pc, PortQuery.Esc));
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

        // -----------------------------------------------------------------------------------------
        // BƯỚC 3: SELECT DTL (Master.Loyalty.cs:18742-18751)
        // select t.MyIdxSeq, mpmtpsl.*
        // from #tbl_Mst_PolicyMoneyToPointService_Filter t
        //   inner join Mst_PolicyMoneyToPointServiceDtl mpmtpsl on t.PolicyCode = mpmtpsl.PolicyCode
        // order by mpmtpsl.LogLUDTimeUTC desc
        // -----------------------------------------------------------------------------------------
        var dtlRows = new List<object>();
        if (getDtl && draftPage.Count > 0)
        {
            var policyCodes = draftPage.Select(x => x.Policy.PolicyCode).ToList();
            var policyMap = draftPage.ToDictionary(
                x => x.Policy.PolicyCode,
                x => x.MyIdxSeq,
                StringComparer.OrdinalIgnoreCase);

            List<Mst_PolicyMoneyToPointServiceDtl> dtlList;
            if (policyCodes.Count == 1)
            {
                var targetCode = policyCodes[0];
                dtlList = await _db.Set<Mst_PolicyMoneyToPointServiceDtl>()
                    .AsNoTracking()
                    .Where(d => EF.Functions.Like((d.PolicyCode ?? "").ToLower(), PortQuery.Equal(targetCode), PortQuery.Esc))
                    .ToListAsync();
            }
            else
            {
                dtlList = await _db.Set<Mst_PolicyMoneyToPointServiceDtl>()
                    .AsNoTracking()
                    .Where(d => policyCodes.Contains(d.PolicyCode))
                    .ToListAsync();
            }

            // Sắp xếp Dtl theo mpmtpsl.LogLUDTimeUTC desc (Master.Loyalty.cs:18750).
            // Hòa trùng LogLUDTimeUTC: sắp phụ theo CardType asc, ghi comment theo đề bài (Master.Loyalty.cs:18750).
            dtlRows = dtlList
                .OrderByDescending(d => d.LogLUDTimeUTC)
                .ThenBy(d => d.CardType)
                .Select(d => (object)new
                {
                    MyIdxSeq = policyMap.TryGetValue(d.PolicyCode, out var seq) ? seq : 0L,
                    d.PolicyCode,
                    d.CardType,
                    d.NetworkID,
                    d.ConvertValue,
                    d.ConvertPoint,
                    d.ValueRankCardType,
                    d.FlagActive,
                    d.Remark,
                    d.LogLUBy,
                    d.LogLUDTimeUTC
                })
                .ToList();
        }

        return (itemCount, rows, dtlRows);
    }
}
