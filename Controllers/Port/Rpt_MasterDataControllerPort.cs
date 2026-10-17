using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniLoyalty.Data;
using MiniLoyalty.Models;
using MiniLoyalty.Models.Port;

namespace MiniLoyalty.Controllers.Port;

// ============================================================================================
// PORT 1:1 — Rpt_MasterDataController/Search (#282) & ExportExcel (#283)
//
// Nguồn tầng Gate:
//   - D:\idocNet\2022.7.Loyalty\13.ClientGate\V10\idn.Loyalty.WebAPI\Controllers\Rpt_MasterDataController.cs:29-131 (Search)
//   - D:\idocNet\2022.7.Loyalty\13.ClientGate\V10\idn.Loyalty.WebAPI\Controllers\Rpt_MasterDataController.cs:135-248 (ExportExcel)
//   - D:\idocNet\2022.7.Loyalty\13.ClientGate\V10\idn.Loyalty.WebAPI\Controllers\Rpt_MasterDataController.cs:252-269 (strWhereClause_Rpt_MasterData)
//   - D:\idocNet\2022.7.Loyalty\13.ClientGate\V10\idn.Loyalty.WebAPI\Controllers\Rpt_MasterDataController.cs:273-321 (DicColums - 43 cột HTV)
//   - D:\idocNet\2022.7.Loyalty\13.ClientGate\V10\idn.Loyalty.WebAPI\Controllers\Rpt_MasterDataController.cs:322-359 (DicColumsSpec - 32 cột Spec)
// Nguồn tầng API:
//   - D:\idocNet\2022.7.Loyalty\11.BackEnd\V10\idn.Loyalty.Biz.Web\Controllers\RptMasterDataController.cs:91-158 (WA_Rpt_MasterData_Get)
// Nguồn tầng Biz LIVE:
//   - D:\idocNet\2022.7.Loyalty\11.BackEnd\V10\idn.Loyalty.Biz\Report.cs:9937-10049 (WAS_Rpt_MasterData_Get)
//   - D:\idocNet\2022.7.Loyalty\11.BackEnd\V10\idn.Loyalty.Biz\Report.cs:10050-10201 (Rpt_MasterData_Get)
//   - D:\idocNet\2022.7.Loyalty\11.BackEnd\V10\idn.Loyalty.Biz\Report.cs:10202-10937 (Rpt_MasterData_GetX)
//
// Logic nghiệp vụ và quy ước port:
//   1. Tham số đầu vào: DLCPCode, RptDateType, RptDateMonth, RptDateQuarter, RptDateYear.
//   2. Chuẩn hoá tham số:
//      - strRptDateType: StdStr(objRptDateType) (Report.cs:10251)
//      - strRptDateMonth: StdParam(objRptDateMonth) (Report.cs:10252)
//      - strRptDateQuarter: StdParam(objRptDateQuarter) (Report.cs:10253)
//      - strRptDateYear: StdParam(objRptDateYear) (Report.cs:10254)
//      - strDLCode: StdParam(objDLCode) (Report.cs:10255)
//   3. Guard checks:
//      - Nếu strRptDateType rỗng -> nem ErridnLoyalty.Rpt_MasterData_Get_InvalidRptDateType (Report.cs:10433)
//      - Nếu strRptDateYear rỗng -> nem ErridnLoyalty.Rpt_MasterData_Get_InvalidRptDateYear (Report.cs:10424)
//      - Nếu strRptDateType không thuộc ("Month", "Quarter", "Year") -> nem ErridnLoyalty.Rpt_MasterData_Get_InvalidRptDateType (Report.cs:10415)
//      - Nếu strRptDateType == "Quarter" mà strRptDateQuarter không thuộc ("1", "2", "3", "4") -> nem ErridnLoyalty.Rpt_MasterData_Get_InvalidRptDateQuarter (Report.cs:10360)
//   4. Điều kiện lọc ngày cứng (hard-coded): rmd.PeriodMonth > '2023-06-01' (Report.cs:1126, 1163, 1201, 1428)
//   5. Khi strRptDateType == "Month":
//      - Nếu !string.IsNullOrEmpty(strRptDateMonth) -> rmd.PeriodMonth = '{strRptDateYear}-{strRptDateMonth}-01' (Report.cs:939)
//      - Query Rpt_MasterData: inner join Rpt_MasterDataSpec trên PeriodMonth (Report.cs:1116-1127)
//      - Query Rpt_MasterDataSpec: inner join Rpt_MasterData trên PeriodMonth (Report.cs:1153-1164)
//      - Lọc DLCode: nếu !string.IsNullOrEmpty(strDLCode) và strDLCode != "HTV" -> rmds.DLCPCode = strDLCode (Report.cs:915-920). Nếu strDLCode == "HTV" thì không lọc DLCPCode ở Biz.
//   6. Trả kết quả tại Gate (Rpt_MasterDataController.cs:100-117) cho Search:
//      - Nếu !string.IsNullOrEmpty(DLCPCode):
//        * Nếu DLCPCode != "HTV" -> Data = new { Lst_Rpt_MasterDataSpec = list_Rpt_MasterDataSpec }
//        * Nếu DLCPCode == "HTV" -> Data = null (LỖI NGUỒN GIỮ NGUYÊN: Gate bỏ sót trường hợp HTV, không gán Data, Gate :100-109)
//      - Nếu string.IsNullOrEmpty(DLCPCode) -> Data = new { Lst_Rpt_MasterData = list_Rpt_MasterData, Lst_Rpt_MasterDataSpec = list_Rpt_MasterDataSpec }
//      - Khi danh sách rỗng (count == 0), trường tương ứng mang giá trị null thay vì mảng rỗng [] (Gate :67-74)
//   7. Quy tắc xuất file Excel tại Gate (Rpt_MasterDataController.cs:205-235) cho ExportExcel:
//      - Kiểm tra danh sách rỗng (:205-210):
//        Neu (list_Rpt_MasterData == null || list_Rpt_MasterData.Count == 0) || (list_Rpt_MasterDataSpec == null || list_Rpt_MasterDataSpec.Count == 0)
//        -> ném ServiceException("Không có dữ liệu") -> bắt tại catch (:237-241) trả 400 PortResult.Fail("Rpt_MasterData_ExportExcel", "Message", "Không có dữ liệu").
//      - Phân nhánh xuất Excel (:212-235):
//        * Neu DLCPCode == "HTV": dùng DicColums() (43 cột), prefix "Rpt_MasterData", sheet "Rpt_MasterData", xuất list_Rpt_MasterData.
//        * Nguoc lai (DLCPCode != "HTV" hoặc null/rỗng): dùng DicColumsSpec() (32 cột), prefix "Rpt_MasterDataSpec", sheet "Rpt_MasterDataSpec", xuất list_Rpt_MasterDataSpec.
//      - Trả về Success = true, Data = { Url = PortExcel.GetRequestBaseAddress(Request) + virtualPath }.
// ============================================================================================

/// <summary>
/// Port 1:1 Rpt_MasterDataController.cs — Rpt_MasterData / Rpt_MasterDataSpec
/// </summary>
[ApiController]
[Route("api/rpt_masterdata")]
[Produces("application/json")]
public class Rpt_MasterDataControllerPort : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public Rpt_MasterDataControllerPort(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    /// <summary>
    /// Tách logic truy vấn dữ liệu báo cáo dùng chung cho cả Search (#282) và ExportExcel (#283).
    /// Nguồn: gate Rpt_MasterDataController.cs:40-75, :166-204 -> API RptMasterDataController.cs:91-158
    /// -> Biz Report.cs:9937-10049 (WAS_Rpt_MasterData_Get), :10050-10201 (Rpt_MasterData_Get), :10202-10937 (Rpt_MasterData_GetX).
    /// </summary>
    private async Task<(List<Rpt_MasterData>? listMaster, List<Rpt_MasterDataSpec>? listSpec, IActionResult? error)> QueryReportDataAsync(Rpt_MasterDataSearchReq? req)
    {
        // 1. Chuẩn hoá tham số đầu vào theo đúng hàm nguồn (Report.cs:10251-10255)
        var strRptDateType = StdStr(req?.RptDateType);
        var strRptDateYear = StdParam(req?.RptDateYear);
        var strRptDateMonth = StdParam(req?.RptDateMonth);
        var strRptDateQuarter = StdParam(req?.RptDateQuarter);
        var strDLCode = StdParam(req?.DLCPCode);

        // 2. Kiểm tra điều kiện guard (Report.cs:10289-10438)
        // Guard 1: RptDateType rỗng -> Rpt_MasterData_Get_InvalidRptDateType (Report.cs:10433)
        if (string.IsNullOrEmpty(strRptDateType))
        {
            return (null, null, PortResult.Fail("Rpt_MasterData_Get_InvalidRptDateType"));
        }

        // Guard 2: RptDateYear rỗng -> Rpt_MasterData_Get_InvalidRptDateYear (Report.cs:10424)
        if (string.IsNullOrEmpty(strRptDateYear))
        {
            return (null, null, PortResult.Fail("Rpt_MasterData_Get_InvalidRptDateYear"));
        }

        // Guard 3: RptDateType không thuộc ("Month", "Quarter", "Year") -> Rpt_MasterData_Get_InvalidRptDateType (Report.cs:10415)
        if (!string.Equals(strRptDateType, "Month", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(strRptDateType, "Quarter", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(strRptDateType, "Year", StringComparison.OrdinalIgnoreCase))
        {
            return (null, null, PortResult.Fail("Rpt_MasterData_Get_InvalidRptDateType"));
        }

        // Guard 4: Nếu RptDateType == "Quarter" mà strRptDateQuarter không thuộc ("1", "2", "3", "4") -> Rpt_MasterData_Get_InvalidRptDateQuarter (Report.cs:10360)
        if (string.Equals(strRptDateType, "Quarter", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrEmpty(strRptDateQuarter))
            {
                if (strRptDateQuarter != "1" && strRptDateQuarter != "2" && strRptDateQuarter != "3" && strRptDateQuarter != "4")
                {
                    return (null, null, PortResult.Fail("Rpt_MasterData_Get_InvalidRptDateQuarter"));
                }
            }
        }

        // 3. Xây dựng truy vấn
        // Điều kiện cứng: rmd.PeriodMonth > '2023-06-01' (Report.cs:1126, 1163, 1201, 1428)
        var minDate = new DateTime(2023, 6, 1);
        var masterQuery = _db.Set<Rpt_MasterData>().AsNoTracking().Where(m => m.PeriodMonth > minDate);
        var specQuery = _db.Set<Rpt_MasterDataSpec>().AsNoTracking();

        // Lọc DLCode trên Rpt_MasterDataSpec:
        // Report.cs:915-920: if (!string.IsNullOrEmpty(strDLCode) && !StringEqual(strDLCode, "HTV")) rmds.DLCPCode = strDLCode
        if (!string.IsNullOrEmpty(strDLCode) && !string.Equals(strDLCode, "HTV", StringComparison.OrdinalIgnoreCase))
        {
            var p_dl = PortQuery.Equal(strDLCode);
            specQuery = specQuery.Where(s => EF.Functions.Like((s.DLCPCode ?? "").ToLower(), p_dl, PortQuery.Esc));
        }

        List<Rpt_MasterData>? list_Rpt_MasterData = null;
        List<Rpt_MasterDataSpec>? list_Rpt_MasterDataSpec = null;

        if (string.Equals(strRptDateType, "Month", StringComparison.OrdinalIgnoreCase))
        {
            // Lọc tháng: rmd.PeriodMonth = '{strRptDateYear}-{strRptDateMonth}-01' (Report.cs:939)
            if (!string.IsNullOrEmpty(strRptDateMonth))
            {
                if (int.TryParse(strRptDateYear, out int year) && int.TryParse(strRptDateMonth, out int month) && month >= 1 && month <= 12)
                {
                    var targetMonth = new DateTime(year, month, 1);
                    masterQuery = masterQuery.Where(m => m.PeriodMonth == targetMonth);
                }
                else
                {
                    masterQuery = masterQuery.Where(m => false);
                }
            }
            else
            {
                if (int.TryParse(strRptDateYear, out int year))
                {
                    var startYear = new DateTime(year, 1, 1);
                    var endYear = new DateTime(year, 12, 31, 23, 59, 59);
                    masterQuery = masterQuery.Where(m => m.PeriodMonth >= startYear && m.PeriodMonth <= endYear);
                }
            }

            // Inner join Rpt_MasterData và Rpt_MasterDataSpec theo PeriodMonth (Report.cs:1120-1121, 1157-1158)
            var joinedMaster = (from m in masterQuery
                                join s in specQuery on m.PeriodMonth equals s.PeriodMonth
                                select m).Distinct().OrderBy(m => m.PeriodMonth);

            var joinedSpec = (from s in specQuery
                              join m in masterQuery on s.PeriodMonth equals m.PeriodMonth
                              select s).Distinct().OrderBy(s => s.PeriodMonth).ThenBy(s => s.DLCPCode);

            var masterResult = await joinedMaster.ToListAsync();
            var specResult = await joinedSpec.ToListAsync();

            // Khi danh sách rỗng (count == 0), gán null thay vì mảng rỗng [] (Gate :67-74)
            if (masterResult.Count > 0) list_Rpt_MasterData = masterResult;
            if (specResult.Count > 0) list_Rpt_MasterDataSpec = specResult;
        }
        else // Quarter hoặc Year
        {
            int year = int.TryParse(strRptDateYear, out var y) ? y : 0;
            var periodMonths = new List<DateTime>();

            var now = DateTime.Now;
            var dtimePreviousMonth = now.AddMonths(-1);
            var dtimeFirstDayOfPreviousMonth = new DateTime(dtimePreviousMonth.Year, dtimePreviousMonth.Month, 1);
            var dtimeLastDayOfPreviousMonth = dtimeFirstDayOfPreviousMonth.AddMonths(1).AddDays(-1);

            string strPrevPeriodMonth = dtimeFirstDayOfPreviousMonth.ToString("yyyy-MM-01");
            string strLastPeriodMonth = "";
            DateTime dtFromLocal;
            DateTime dtToLocal;

            if (string.Equals(strRptDateType, "Quarter", StringComparison.OrdinalIgnoreCase))
            {
                int quarter = int.TryParse(strRptDateQuarter, out var q) ? q : 1;
                DateTime dtimeMonthFirst = new DateTime(year, 3 * (quarter - 1) + 1, 1);
                DateTime dtimeMonthLast = new DateTime(year, 3 * (quarter - 1) + 3, 1);
                string strLastMonthOfQuarter = dtimeMonthLast.ToString("yyyy-MM-01");
                strLastPeriodMonth = strLastMonthOfQuarter;

                dtFromLocal = dtimeMonthFirst.Date;

                if (string.Compare(strPrevPeriodMonth, strLastPeriodMonth, StringComparison.Ordinal) < 0)
                {
                    strLastPeriodMonth = strPrevPeriodMonth;
                    dtToLocal = dtimeLastDayOfPreviousMonth.Date.AddDays(1).AddSeconds(-1);
                }
                else
                {
                    DateTime dtimeLastDayOfQuarter = dtimeMonthLast.AddMonths(1).AddDays(-1);
                    dtToLocal = dtimeLastDayOfQuarter.Date.AddDays(1).AddSeconds(-1);
                }

                for (int i = 1; i <= 3; i++)
                {
                    periodMonths.Add(new DateTime(year, 3 * (quarter - 1) + i, 1));
                }

                // Biz: and (1=0) nếu tháng trước < tháng cuối kỳ (Report.cs:10352-10355)
                if (string.Compare(strPrevPeriodMonth, strLastMonthOfQuarter, StringComparison.Ordinal) < 0)
                {
                    periodMonths.Clear();
                }
            }
            else // Year
            {
                DateTime dtimeMonthFirst = new DateTime(year, 1, 1);
                DateTime dtimeMonthLast = new DateTime(year, 12, 1);
                string strLastMonthOfYear = dtimeMonthLast.ToString("yyyy-MM-01");
                strLastPeriodMonth = strLastMonthOfYear;

                dtFromLocal = dtimeMonthFirst.Date;

                if (string.Compare(strPrevPeriodMonth, strLastMonthOfYear, StringComparison.Ordinal) < 0)
                {
                    strLastPeriodMonth = strPrevPeriodMonth;
                    dtToLocal = dtimeLastDayOfPreviousMonth.Date.AddDays(1).AddSeconds(-1);
                }
                else
                {
                    DateTime dtimeLastDayOfYear = dtimeMonthLast.AddMonths(1).AddDays(-1);
                    dtToLocal = dtimeLastDayOfYear.Date.AddDays(1).AddSeconds(-1);
                }

                for (int i = 1; i <= 12; i++)
                {
                    periodMonths.Add(new DateTime(year, i, 1));
                }

                // Biz: and (1=0) nếu tháng trước < tháng cuối kỳ (Report.cs:10407-10410)
                if (string.Compare(strPrevPeriodMonth, strLastMonthOfYear, StringComparison.Ordinal) < 0)
                {
                    periodMonths.Clear();
                }
            }

            DateTime utcFrom = dtFromLocal.AddHours(-7);
            DateTime utcTo = dtToLocal.AddHours(-7);
            DateTime? targetTopMonth = DateTime.TryParse(strLastPeriodMonth, out var dtParsedTop) ? dtParsedTop : null;

            var qPeriodMaster = masterQuery.Where(m => m.PeriodMonth != null && periodMonths.Contains(m.PeriodMonth.Value));
            var qPeriodSpec = specQuery.Where(s => s.PeriodMonth != null && periodMonths.Contains(s.PeriodMonth.Value));

            var joinedMaster = await (from m in qPeriodMaster
                                      join s in qPeriodSpec on m.PeriodMonth equals s.PeriodMonth
                                      select m).Distinct().ToListAsync();

            var joinedSpec = await (from s in qPeriodSpec
                                    join m in qPeriodMaster on s.PeriodMonth equals m.PeriodMonth
                                    select s).Distinct().ToListAsync();

            if (joinedMaster.Count > 0 || joinedSpec.Count > 0)
            {
                // Truy vấn Crd_CardTransaction kết hợp Crd_Member để tính QtyMemberPointuse và QtyMemberUsepromotion
                // trong khoảng [utcFrom, utcTo] (Report.cs:1213-1306, fix_note.md #1)
                var pointUseTx = await (from cct in _db.CrdCardTransactions.AsNoTracking()
                                        join m in _db.CrdMembers.AsNoTracking() on cct.MemberNo equals m.MemberNo
                                        where cct.CreateDTimeUTC >= utcFrom
                                              && cct.CreateDTimeUTC <= utcTo
                                              && cct.DealPointType == "POINTUSE"
                                        select new { cct.DLCode, cct.MemberNo })
                                       .Distinct()
                                       .ToListAsync();

                int totalQtyMemberPointUse = pointUseTx.Select(x => x.MemberNo).Distinct(StringComparer.OrdinalIgnoreCase).Count();

                var dealerPointUseCounts = pointUseTx
                    .Where(x => !string.IsNullOrEmpty(x.DLCode))
                    .GroupBy(x => x.DLCode!, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

                var promoTx = await (from cct in _db.CrdCardTransactions.AsNoTracking()
                                     join m in _db.CrdMembers.AsNoTracking() on cct.MemberNo equals m.MemberNo
                                     where cct.CreateDTimeUTC >= utcFrom
                                           && cct.CreateDTimeUTC <= utcTo
                                           && cct.DealPointType == "PRPROGRAM"
                                     select new { cct.DLCode, cct.MemberNo })
                                    .Distinct()
                                    .ToListAsync();

                int totalQtyMemberUsePromotion = promoTx.Select(x => x.MemberNo).Distinct(StringComparer.OrdinalIgnoreCase).Count();

                var dealerPromoCounts = promoTx
                    .Where(x => !string.IsNullOrEmpty(x.DLCode))
                    .GroupBy(x => x.DLCode!, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

                if (joinedMaster.Count > 0)
                {
                    // Hàng Top: PeriodMonth = targetTopMonth chính xác; nếu không có thì các cột lũy kế = 0 (fix_note.md #2)
                    var topRow = targetTopMonth.HasValue ? joinedMaster.FirstOrDefault(x => x.PeriodMonth == targetTopMonth.Value) : null;

                    // Dòng tổng Quarter/Year không có PeriodMonth/NetworkID/OrgID (để null) (fix_note.md #3)
                    var aggregatedMaster = new Rpt_MasterData
                    {
                        PeriodMonth = null,
                        NetworkID = null,
                        OrgID = null,
                        // Sums (Report.cs:1340-1365)
                        QtyRegistrationCardS = joinedMaster.Sum(x => x.QtyRegistrationCardS ?? 0),
                        QtyRegistrationCardG = joinedMaster.Sum(x => x.QtyRegistrationCardG ?? 0),
                        QtyRegistrationCardP = joinedMaster.Sum(x => x.QtyRegistrationCardP ?? 0),
                        QtyCardUp = joinedMaster.Sum(x => x.QtyCardUp ?? 0),
                        QtyCardDown = joinedMaster.Sum(x => x.QtyCardDown ?? 0),
                        QtyCardKeep = joinedMaster.Sum(x => x.QtyCardKeep ?? 0),
                        QtyMemberCancel = joinedMaster.Sum(x => x.QtyMemberCancel ?? 0),
                        QtyConsumption = joinedMaster.Sum(x => x.QtyConsumption ?? 0),
                        QtyInCrease = joinedMaster.Sum(x => x.QtyInCrease ?? 0),
                        QtyServiceTurn = joinedMaster.Sum(x => x.QtyServiceTurn ?? 0),
                        AmountPointInCrease = joinedMaster.Sum(x => x.AmountPointInCrease ?? 0),
                        AmountPointUse = joinedMaster.Sum(x => x.AmountPointUse ?? 0),
                        QtyPointUse = joinedMaster.Sum(x => x.QtyPointUse ?? 0),
                        AmountConsumptionS = joinedMaster.Sum(x => x.AmountConsumptionS ?? 0),
                        AmountConsumptionG = joinedMaster.Sum(x => x.AmountConsumptionG ?? 0),
                        AmountConsumptionP = joinedMaster.Sum(x => x.AmountConsumptionP ?? 0),
                        AmountPointInCreaseS = joinedMaster.Sum(x => x.AmountPointInCreaseS ?? 0),
                        AmountPointInCreaseG = joinedMaster.Sum(x => x.AmountPointInCreaseG ?? 0),
                        AmountPointInCreaseP = joinedMaster.Sum(x => x.AmountPointInCreaseP ?? 0),
                        PointConsumption = joinedMaster.Sum(x => x.PointConsumption ?? 0),
                        PointSales = joinedMaster.Sum(x => x.PointSales ?? 0),
                        PointIntroduction = joinedMaster.Sum(x => x.PointIntroduction ?? 0),
                        PointBirthday = joinedMaster.Sum(x => x.PointBirthday ?? 0),
                        PointUse = joinedMaster.Sum(x => x.PointUse ?? 0),
                        PointExpiry = joinedMaster.Sum(x => x.PointExpiry ?? 0),
                        PointCancel = joinedMaster.Sum(x => x.PointCancel ?? 0),
                        // Top 1 cumulative (PeriodMonth == targetTopMonth, nếu không có thì 0) (Report.cs:1311-1328, 1380-1394, fix_note.md #2)
                        QtyCardS = topRow?.QtyCardS ?? 0,
                        QtyCardG = topRow?.QtyCardG ?? 0,
                        QtyCardP = topRow?.QtyCardP ?? 0,
                        QtyCardExceptionG = topRow?.QtyCardExceptionG ?? 0,
                        QtyCardExceptionP = topRow?.QtyCardExceptionP ?? 0,
                        QtyServiceDMSPaymented = topRow?.QtyServiceDMSPaymented ?? 0,
                        QtyServiceDMS = topRow?.QtyServiceDMS ?? 0,
                        QtyCarUseServiceDMS = topRow?.QtyCarUseServiceDMS ?? 0,
                        PointExpiryAll = topRow?.PointExpiryAll ?? 0,
                        PointCancelAll = topRow?.PointCancelAll ?? 0,
                        PointConsumptionAll = topRow?.PointConsumptionAll ?? 0,
                        QtyMemberNotBack6M = topRow?.QtyMemberNotBack6M ?? 0,
                        QtyMemberNotBack12M = topRow?.QtyMemberNotBack12M ?? 0,
                        QtyMemberBack6M = topRow?.QtyMemberBack6M ?? 0,
                        QtyService = topRow?.QtyService ?? 0,
                        // Số hội viên tiêu điểm / sử dụng ưu đãi trong kỳ (Report.cs:1395-1396, fix_note.md #1)
                        QtyMemberPointUse = totalQtyMemberPointUse,
                        QtyMemberUsePromotion = totalQtyMemberUsePromotion
                    };
                    list_Rpt_MasterData = new List<Rpt_MasterData> { aggregatedMaster };
                }

                if (joinedSpec.Count > 0)
                {
                    var specGroups = joinedSpec.GroupBy(x => x.DLCPCode, StringComparer.OrdinalIgnoreCase).ToList();
                    list_Rpt_MasterDataSpec = new List<Rpt_MasterDataSpec>();
                    foreach (var g in specGroups)
                    {
                        var topSpec = targetTopMonth.HasValue ? g.FirstOrDefault(x => x.PeriodMonth == targetTopMonth.Value) : null;
                        var dealerCode = g.Key;

                        // Dòng Spec tổng chỉ có DLCPCode + sum + cột Top + 2 cột member (PeriodMonth/NetworkID/OrgID để null) (fix_note.md #3)
                        var aggSpec = new Rpt_MasterDataSpec
                        {
                            PeriodMonth = null,
                            DLCPCode = dealerCode,
                            NetworkID = null,
                            OrgID = null,
                            // Sums (Report.cs:1458-1479)
                            QtyRegistrationCardS = g.Sum(x => x.QtyRegistrationCardS ?? 0),
                            QtyRegistrationCardG = g.Sum(x => x.QtyRegistrationCardG ?? 0),
                            QtyRegistrationCardP = g.Sum(x => x.QtyRegistrationCardP ?? 0),
                            QtyConsumption = g.Sum(x => x.QtyConsumption ?? 0),
                            QtyInCrease = g.Sum(x => x.QtyInCrease ?? 0),
                            QtyServiceTurn = g.Sum(x => x.QtyServiceTurn ?? 0),
                            AmountPointInCrease = g.Sum(x => x.AmountPointInCrease ?? 0),
                            AmountPointUse = g.Sum(x => x.AmountPointUse ?? 0),
                            QtyPointUse = g.Sum(x => x.QtyPointUse ?? 0),
                            AmountConsumptionS = g.Sum(x => x.AmountConsumptionS ?? 0),
                            AmountConsumptionG = g.Sum(x => x.AmountConsumptionG ?? 0),
                            AmountConsumptionP = g.Sum(x => x.AmountConsumptionP ?? 0),
                            AmountPointInCreaseS = g.Sum(x => x.AmountPointInCreaseS ?? 0),
                            AmountPointInCreaseG = g.Sum(x => x.AmountPointInCreaseG ?? 0),
                            AmountPointInCreaseP = g.Sum(x => x.AmountPointInCreaseP ?? 0),
                            PointConsumption = g.Sum(x => x.PointConsumption ?? 0),
                            PointSales = g.Sum(x => x.PointSales ?? 0),
                            PointIntroduction = g.Sum(x => x.PointIntroduction ?? 0),
                            PointBirthday = g.Sum(x => x.PointBirthday ?? 0),
                            PointUse = g.Sum(x => x.PointUse ?? 0),
                            PointExpiry = g.Sum(x => x.PointExpiry ?? 0),
                            PointCancel = g.Sum(x => x.PointCancel ?? 0),
                            // Top 1 cumulative (PeriodMonth == targetTopMonth, nếu không có thì 0) (Report.cs:1436-1443, 1497-1503, fix_note.md #2)
                            QtyServiceDMSPaymented = topSpec?.QtyServiceDMSPaymented ?? 0,
                            QtyServiceDMS = topSpec?.QtyServiceDMS ?? 0,
                            QtyCarUseServiceDMS = topSpec?.QtyCarUseServiceDMS ?? 0,
                            QtyService = topSpec?.QtyService ?? 0,
                            PointExpiryAll = topSpec?.PointExpiryAll ?? 0,
                            PointConsumptionAll = topSpec?.PointConsumptionAll ?? 0,
                            PointCancelAll = topSpec?.PointCancelAll ?? 0,
                            // Số hội viên tiêu điểm / sử dụng ưu đãi theo đại lý trong kỳ (Report.cs:1504-1505, fix_note.md #1)
                            QtyMemberPointUse = dealerPointUseCounts.TryGetValue(dealerCode, out var dPointUse) ? dPointUse : 0,
                            QtyMemberUsePromotion = dealerPromoCounts.TryGetValue(dealerCode, out var dPromo) ? dPromo : 0
                        };
                        list_Rpt_MasterDataSpec.Add(aggSpec);
                    }
                }
            }
        }

        return (list_Rpt_MasterData, list_Rpt_MasterDataSpec, null);
    }

    /// <summary>
    /// Port 1:1 action Search từ gate Rpt_MasterDataController.cs:29-131 (#282).
    /// </summary>
    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Rpt_MasterDataSearchReq? req)
    {
        var (list_Rpt_MasterData, list_Rpt_MasterDataSpec, error) = await QueryReportDataAsync(req);
        if (error != null)
        {
            return error;
        }

        var strDLCode = StdParam(req?.DLCPCode);

        // Định dạng trả kết quả Data ở Gate (Rpt_MasterDataController.cs:100-117)
        object? data = null;
        if (!string.IsNullOrEmpty(strDLCode))
        {
            if (!string.Equals(strDLCode, "HTV", StringComparison.OrdinalIgnoreCase))
            {
                data = new
                {
                    Lst_Rpt_MasterDataSpec = list_Rpt_MasterDataSpec
                };
            }
            // LỖI NGUỒN GIỮ NGUYÊN: Khi strDLCode == "HTV", Gate Rpt_MasterDataController.cs:100-109
            // không có nhánh gán Data, nên Data giữ nguyên null mặc định.
        }
        else
        {
            data = new
            {
                Lst_Rpt_MasterData = list_Rpt_MasterData,
                Lst_Rpt_MasterDataSpec = list_Rpt_MasterDataSpec
            };
        }

        return Ok(new
        {
            Success = true,
            Data = data
        });
    }

    /// <summary>
    /// Port 1:1 action ExportExcel từ gate Rpt_MasterDataController.cs:135-248 (#283).
    /// </summary>
    [HttpPost("exportexcel")]
    public async Task<IActionResult> ExportExcel([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Rpt_MasterDataSearchReq? req)
    {
        var (list_Rpt_MasterData, list_Rpt_MasterDataSpec, error) = await QueryReportDataAsync(req);
        if (error != null)
        {
            return error;
        }

        // 1. Kiểm tra danh sách rỗng (gate Rpt_MasterDataController.cs:205-210):
        // Nếu (list_Rpt_MasterData == null || list_Rpt_MasterData.Count == 0) || (list_Rpt_MasterDataSpec == null || list_Rpt_MasterDataSpec.Count == 0)
        // -> ném ServiceException("Không có dữ liệu") -> bắt tại catch (:237-241) trả 400 PortResult.Fail("Rpt_MasterData_ExportExcel", "Message", "Không có dữ liệu")
        if ((list_Rpt_MasterData == null || list_Rpt_MasterData.Count == 0)
            || (list_Rpt_MasterDataSpec == null || list_Rpt_MasterDataSpec.Count == 0))
        {
            return PortResult.Fail("Rpt_MasterData_ExportExcel", "Message", "Không có dữ liệu");
        }

        // 2. Chuẩn bị đường dẫn thư mục xuất file Excel (gate :216, :228)
        var webRoot = _env?.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
        {
            webRoot = _env != null
                ? Path.Combine(_env.ContentRootPath, "wwwroot")
                : Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        }

        var strDLCode = StdParam(req?.DLCPCode);
        string virtualPath;

        try
        {
            // 3. Phân nhánh xuất Excel theo DLCPCode (gate Rpt_MasterDataController.cs:212-235)
            if (string.Equals(strDLCode, "HTV", StringComparison.OrdinalIgnoreCase))
            {
                // Nhánh HTV: 43 cột từ DicColums(), prefix "Rpt_MasterData", sheet "Rpt_MasterData" (gate :214-217)
                var dicColNames = DicColums();
                string physicalPath;
                (physicalPath, virtualPath) = PortExcel.GenExcelExportFilePath(webRoot, "Rpt_MasterData");
                PortExcel.ExportToExcelFromList(list_Rpt_MasterData, dicColNames, physicalPath, "Rpt_MasterData");
            }
            else
            {
                // Nhánh Đại lý khác HTV (hoặc null/rỗng): 32 cột từ DicColumsSpec(), prefix "Rpt_MasterDataSpec", sheet "Rpt_MasterDataSpec" (gate :226-229)
                var dicColNames = DicColumsSpec();
                string physicalPath;
                (physicalPath, virtualPath) = PortExcel.GenExcelExportFilePath(webRoot, "Rpt_MasterDataSpec");
                PortExcel.ExportToExcelFromList(list_Rpt_MasterDataSpec, dicColNames, physicalPath, "Rpt_MasterDataSpec");
            }
        }
        catch (Exception ex)
        {
            // Bắt ngoại lệ nếu xuất file lỗi -> trả Fail (gate :237-241)
            return PortResult.Fail("Rpt_MasterData_ExportExcel", "Exception", ex.Message);
        }

        // 4. Trả URL tải file với Success = true (gate Rpt_MasterDataController.cs:218-222, :230-234)
        return Ok(new
        {
            Success = true,
            Data = new
            {
                Url = PortExcel.GetRequestBaseAddress(Request) + virtualPath
            }
        });
    }

    /// <summary>
    /// Từ điển 43 cột cho nhánh HTV (Rpt_MasterData) - Gate Rpt_MasterDataController.cs:273-321
    /// </summary>
    private static Dictionary<string, string> DicColums()
    {
        return new Dictionary<string, string>
        {
            { nameof(Rpt_MasterData.QtyRegistrationCardS), "Tổng thẻ hội viên phát hành mới trong kỳ hạng S" },
            { nameof(Rpt_MasterData.QtyRegistrationCardG), "Tổng thẻ hội viên phát hành mới đặc cách hạng G" },
            { nameof(Rpt_MasterData.QtyRegistrationCardP), "Tổng thẻ hội viên phát hành mới đặc cách hạng P" },
            { nameof(Rpt_MasterData.QtyCardUp), "Tổng lượt tăng hạng trong kỳ" },
            { nameof(Rpt_MasterData.QtyCardDown), "Tổng lượt duy trì hạng trong kỳ" },
            { nameof(Rpt_MasterData.QtyCardKeep), "Tổng lượt xuống hạng trong kỳ" },
            { nameof(Rpt_MasterData.QtyMemberCancel), "Tổng hội viên hủy trong kỳ" },
            { nameof(Rpt_MasterData.QtyCardS), "Tổng thẻ hội viên lũy kế hạng S" },
            { nameof(Rpt_MasterData.QtyCardG), "Tổng thẻ hội viên lũy kế hạng G" },
            { nameof(Rpt_MasterData.QtyCardP), "Tổng thẻ hội viên lũy kế hạng P" },
            { nameof(Rpt_MasterData.QtyCardExceptionG), "Tổng số hội viên lũy kế đặc cách hạng G" },
            { nameof(Rpt_MasterData.QtyCardExceptionP), "Tổng số hội viên lũy kế đặc cách hạng P" },
            { nameof(Rpt_MasterData.QtyServiceDMSPaymented), "Tổng lượt dịch vụ khách hàng trả tiền trong kỳ (DMS)" },
            { nameof(Rpt_MasterData.QtyServiceDMS), "Tổng lượt dịch vụ trong kỳ(DMS)" },
            { nameof(Rpt_MasterData.QtyCarUseServiceDMS), "Tổng số Xe làm dịch vụ trong kỳ(DMS)" },
            { nameof(Rpt_MasterData.QtyConsumption), "Tổng lượt dịch vụ tích điểm tiêu dùng trong kỳ" },
            { nameof(Rpt_MasterData.QtyInCrease), "Tổng lượt dịch vụ tích điểm xét hạng trong kỳ" },
            { nameof(Rpt_MasterData.QtyServiceTurn), "Tổng lượt dịch vụ không thể tiêu điểm" },
            { nameof(Rpt_MasterData.AmountPointInCrease), "Tổng doanh thu dịch vụ xét hạng trong kỳ" },
            { nameof(Rpt_MasterData.AmountPointUse), "Tổng doanh thu dịch vụ tiêu dùng trong kỳ" },
            { nameof(Rpt_MasterData.QtyPointUse), "Tổng lượt dịch vụ tiêu điểm trong kỳ" },
            { nameof(Rpt_MasterData.QtyMemberPointUse), "Tổng hội viên tiêu điểm dịch vụ trong kỳ" },
            { nameof(Rpt_MasterData.QtyMemberUsePromotion), "Tổng số hội viên sử dụng ưu đãi trong kỳ" },
            { nameof(Rpt_MasterData.AmountConsumptionS), "Tổng Doanh thu (tích điểm tiêu dùng) trong kỳ hạng S" },
            { nameof(Rpt_MasterData.AmountConsumptionG), "Tổng Doanh thu (tích điểm tiêu dùng) trong kỳ hạng G" },
            { nameof(Rpt_MasterData.AmountConsumptionP), "Tổng Doanh thu (tích điểm tiêu dùng) trong kỳ hạng P" },
            { nameof(Rpt_MasterData.AmountPointInCreaseS), "Tổng Doanh thu (tích điểm xét hạng) trong kỳ hạng S" },
            { nameof(Rpt_MasterData.AmountPointInCreaseG), "Tổng Doanh thu (tích điểm xét hạng) trong kỳ hạng G" },
            { nameof(Rpt_MasterData.AmountPointInCreaseP), "Tổng Doanh thu (tích điểm xét hạng) trong kỳ hạng P" },
            { nameof(Rpt_MasterData.PointConsumption), "Tổng điểm tiêu dùng tích dịch vụ trong kỳ" },
            { nameof(Rpt_MasterData.PointSales), "Tổng điểm tiêu dùng tích xe mới trong kỳ" },
            { nameof(Rpt_MasterData.PointIntroduction), "Tổng điểm tiêu dùng tích giới thiệu mua trong kỳ" },
            { nameof(Rpt_MasterData.PointBirthday), "Tổng điểm tiêu dùng tích sinh nhật trong kỳ" },
            { nameof(Rpt_MasterData.PointUse), "Tổng điểm tiêu dùng tiêu trong kỳ" },
            { nameof(Rpt_MasterData.PointExpiry), "Tổng số điểm hết hạn trong kỳ" },
            { nameof(Rpt_MasterData.PointExpiryAll), "Tổng số điểm hết hạn lũy kế" },
            { nameof(Rpt_MasterData.PointConsumptionAll), "Tổng điểm tiêu dùng lũy kế" },
            { nameof(Rpt_MasterData.QtyMemberNotBack6M), "Tổng thẻ hội viên > 6 tháng chưa quay lại làm dịch vụ" },
            { nameof(Rpt_MasterData.QtyMemberNotBack12M), "Tổng thẻ hội viên > 12 tháng chưa quay lại làm dịch vụ" },
            { nameof(Rpt_MasterData.QtyMemberBack6M), "Tổng thẻ hội viên làm dịch vụ 6 tháng gần nhất" },
            { nameof(Rpt_MasterData.QtyService), "Tổng lượt DV của HV 12 tháng gần nhất" },
            { nameof(Rpt_MasterData.PointCancel), "Tổng số điểm Hủy trong kỳ" },
            { nameof(Rpt_MasterData.PointCancelAll), "Tổng số điểm Hủy lũy kế" },
        };
    }

    /// <summary>
    /// Từ điển 32 cột cho nhánh Đại lý khác HTV (Rpt_MasterDataSpec) - Gate Rpt_MasterDataController.cs:322-359
    /// </summary>
    private static Dictionary<string, string> DicColumsSpec()
    {
        return new Dictionary<string, string>
        {
            { nameof(Rpt_MasterDataSpec.DLCPCode), "Đại lý" },
            { nameof(Rpt_MasterDataSpec.QtyRegistrationCardS), "Tổng thẻ hội viên phát hành mới trong kỳ hạng S" },
            { nameof(Rpt_MasterDataSpec.QtyRegistrationCardG), "Tổng thẻ hội viên phát hành mới đặc cách hạng G" },
            { nameof(Rpt_MasterDataSpec.QtyRegistrationCardP), "Tổng thẻ hội viên phát hành mới đặc cách hạng P" },
            { nameof(Rpt_MasterDataSpec.QtyServiceDMSPaymented), "Tổng lượt dịch vụ khách hàng trả tiền trong kỳ (DMS)" },
            { nameof(Rpt_MasterDataSpec.QtyServiceDMS), "Tổng lượt dịch vụ trong kỳ(DMS)" },
            { nameof(Rpt_MasterDataSpec.QtyCarUseServiceDMS), "Tổng số Xe làm dịch vụ trong kỳ(DMS)" },
            { nameof(Rpt_MasterDataSpec.QtyConsumption), "Tổng lượt dịch vụ tích điểm tiêu dùng trong kỳ" },
            { nameof(Rpt_MasterDataSpec.QtyInCrease), "Tổng lượt dịch vụ tích điểm xét hạng trong kỳ" },
            { nameof(Rpt_MasterDataSpec.QtyServiceTurn), "Tổng lượt dịch vụ không thể tiêu điểm" },
            { nameof(Rpt_MasterDataSpec.AmountPointInCrease), "Tổng doanh thu dịch vụ xét hạng trong kỳ" },
            { nameof(Rpt_MasterDataSpec.AmountPointUse), "Tổng doanh thu dịch vụ tiêu dùng trong kỳ" },
            { nameof(Rpt_MasterDataSpec.QtyPointUse), "Tổng lượt dịch vụ tiêu điểm trong kỳ" },
            { nameof(Rpt_MasterDataSpec.QtyMemberPointUse), "Tổng hội viên tiêu điểm dịch vụ trong kỳ" },
            { nameof(Rpt_MasterDataSpec.QtyMemberUsePromotion), "Tổng số hội viên sử dụng ưu đãi trong kỳ" },
            { nameof(Rpt_MasterDataSpec.AmountConsumptionS), "Tổng Doanh thu (tích điểm tiêu dùng) trong kỳ hạng S" },
            { nameof(Rpt_MasterDataSpec.AmountConsumptionG), "Tổng Doanh thu (tích điểm tiêu dùng) trong kỳ hạng G" },
            { nameof(Rpt_MasterDataSpec.AmountConsumptionP), "Tổng Doanh thu (tích điểm tiêu dùng) trong kỳ hạng P" },
            { nameof(Rpt_MasterDataSpec.AmountPointInCreaseS), "Tổng Doanh thu (tích điểm xét hạng) trong kỳ hạng S" },
            { nameof(Rpt_MasterDataSpec.AmountPointInCreaseG), "Tổng Doanh thu (tích điểm xét hạng) trong kỳ hạng G" },
            { nameof(Rpt_MasterDataSpec.AmountPointInCreaseP), "Tổng Doanh thu (tích điểm xét hạng) trong kỳ hạng P" },
            { nameof(Rpt_MasterDataSpec.PointConsumption), "Tổng điểm tiêu dùng tích dịch vụ trong kỳ" },
            { nameof(Rpt_MasterDataSpec.PointSales), "Tổng điểm tiêu dùng tích xe mới trong kỳ" },
            { nameof(Rpt_MasterDataSpec.PointIntroduction), "Tổng điểm tiêu dùng tích giới thiệu mua trong kỳ" },
            { nameof(Rpt_MasterDataSpec.PointBirthday), "Tổng điểm tiêu dùng tích sinh nhật trong kỳ" },
            { nameof(Rpt_MasterDataSpec.PointUse), "Tổng điểm tiêu dùng tiêu trong kỳ" },
            { nameof(Rpt_MasterDataSpec.PointExpiry), "Tổng số điểm hết hạn trong kỳ" },
            { nameof(Rpt_MasterDataSpec.PointExpiryAll), "Tổng số điểm hết hạn lũy kế" },
            { nameof(Rpt_MasterDataSpec.PointConsumptionAll), "Tổng điểm tiêu dùng lũy kế" },
            { nameof(Rpt_MasterDataSpec.QtyService), "Tổng lượt DV của HV 12 tháng gần nhất" },
            { nameof(Rpt_MasterDataSpec.PointCancel), "Tổng số điểm Hủy trong kỳ" },
            { nameof(Rpt_MasterDataSpec.PointCancelAll), "Tổng số điểm Hủy lũy kế" },
        };
    }

    /// <summary>
    /// Chuẩn hoá chuỗi mã theo CUtils.StdParam: Trim, viết HOA, rỗng -> null.
    /// </summary>
    private static string? StdParam(object? s)
    {
        if (s == null) return null;
        var str = s.ToString()?.Trim().ToUpperInvariant();
        return string.IsNullOrEmpty(str) ? null : str;
    }

    /// <summary>
    /// Chuẩn hoá chuỗi theo CUtils.StdStr: Trim, Normalize, rỗng -> null.
    /// </summary>
    private static string? StdStr(object? s)
    {
        if (s == null) return null;
        var str = s.ToString()?.Trim().Normalize();
        return string.IsNullOrEmpty(str) ? null : str;
    }
}

/// <summary>
/// DTO nhận tham số tìm kiếm báo cáo Rpt_MasterData - Gate Rpt_MasterDataController.cs:29
/// </summary>
public class Rpt_MasterDataSearchReq
{
    public string? DLCPCode { get; set; }
    public string? RptDateType { get; set; }
    public string? RptDateMonth { get; set; }
    public string? RptDateQuarter { get; set; }
    public string? RptDateYear { get; set; }
}
