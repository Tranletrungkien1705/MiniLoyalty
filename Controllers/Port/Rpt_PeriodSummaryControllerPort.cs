using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniLoyalty.Data;
using MiniLoyalty.Models;
using MiniLoyalty.Models.Port;

namespace MiniLoyalty.Controllers.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — Rpt_PeriodSummaryController.Search (#284).
//
// Nguồn tra cứu (chỉ đọc):
//   - Gate:     13.ClientGate/V10/idn.Loyalty.WebAPI/Controllers/Rpt_PeriodSummaryController.cs:35-129 (Search)
//   - Filter:   Rpt_PeriodSummaryController.cs:613-630 (strWhereClause_Rpt_PeriodSummary)
//   - Service:  13.ClientGate/V10/idn.Loyalty.ClientService/Services/Loyalty/Rpt_PeriodSummaryService.cs:21-33
//   - API:      11.BackEnd/V10/idn.Loyalty.Biz.Web/Controllers/ReportController.cs:516-565 (WA_Rpt_PeriodSummary_Get)
//   - Biz LIVE: 11.BackEnd/V10/idn.Loyalty.Biz/Report.cs:4340-4464 (WAS_Rpt_PeriodSummary_Get),
//               :4465-4613 (Rpt_PeriodSummary_Get), :4614-4941 (Rpt_PeriodSummary_GetX)
//
// ĐẶC ĐIỂM NGHIỆP VỤ:
//   - Gate Search nhận: DLCPCode, PeriodMonth.
//   - strWhereClause_Rpt_PeriodSummary chỉ lọc:
//       + DLCode (map từ DLCPCode tại gate :59) với toán tử '=' (nguyên chuỗi) cho cột DLCPCode.
//       + PeriodMonth với toán tử '=' (nguyên chuỗi) cho cột PeriodMonth.
//       + KHÔNG lọc KeyWord, FlagActive hay OrgID.
//   - Sắp xếp: order by rps.LogLUDTimeUTC desc (Report.cs:4695, :4765).
//   - Công thức tính HCol: HCol = (rps.HCol ?? 0) + (rps.LCol ?? 0) (Report.cs:4754: "Tổng số điểm hết hạn của tháng đối soát = Điểm hết hạn của tháng + Điểm hủy của tháng").
//   - Các bảng chi tiết: Gate truyền Rt_Cols_Rpt_PeriodSummary = "*" và Rt_Cols_Rpt_PeriodSummaryDtl/Transaction/TransactionExceptionally = null (gate :83-86),
//     nên Lst_Rpt_PeriodSummaryDtl, Lst_Rpt_PeriodSummary_Transaction, Lst_Rpt_PeriodSummary_TransactionExceptionally luôn là null.
//   - Phản hồi: khi không có bản ghi nào, Lst_Rpt_PeriodSummary trả về null (gate :63, :92-95).
// ===========================================================================================

/// <summary>
/// DTO yêu cầu tìm kiếm báo cáo đối soát tổng hợp kỳ Rpt_PeriodSummary.
/// </summary>
public class Rpt_PeriodSummarySearchReq
{
    public string? DLCPCode { get; set; }
    public string? PeriodMonth { get; set; }
}

/// <summary>
/// DTO yêu cầu tìm kiếm giao dịch đặc cách đối soát tổng hợp kỳ Rpt_PeriodSummary_TransactionExceptionally (#288).
/// </summary>
public class Rpt_PeriodSummarySearchTransactionExceptionallyReq
{
    public string? PeriodMonth { get; set; }
    public string? DLCodeExceptionally { get; set; }
}

/// <summary>
/// DTO yêu cầu tìm kiếm báo cáo chi tiết giao dịch thẻ đối soát tổng hợp kỳ Rpt_PeriodSummary (#289).
/// Nguồn: Rpt_PeriodSummaryController.cs:445-530.
/// </summary>
public class Rpt_PeriodSummarySearchTransactionDetailReq
{
    public string? PeriodMonth { get; set; }
    public string? DLCode { get; set; }
    public string? DealPointType { get; set; }
    public string? CreateDTimeUTCFrom { get; set; }
    public string? CreateDTimeUTCTo { get; set; }
}

/// <summary>
/// DTO yêu cầu tìm kiếm báo cáo chi tiết giảm điểm kỳ RptPE_CrdTransationDeCreaseInstPeriodDtl (#290).
/// Nguồn: Rpt_PeriodSummaryController.cs:532-608.
/// </summary>
public class Rpt_PeriodSummarySearchRptPECrdTransationDeCreaseInstPeriodDtlReq
{
    public string? DLCPCode { get; set; }
    public string? PeriodMonth { get; set; }
    public string? CreateDTimeUTCFrom { get; set; }
    public string? CreateDTimeUTCTo { get; set; }
}

/// <summary>
/// DTO yêu cầu xuất Excel báo cáo đối soát chi tiết 3 sheet Rpt_PeriodSummary/ExportExcelRptDtl (#292).
/// Nguồn: Rpt_PeriodSummaryController.cs:881.
/// </summary>
public class Rpt_PeriodSummaryExportExcelRptDtlReq
{
    public string? DLCPCode { get; set; }
    public string? PeriodMonth { get; set; }
    public string? DLCodeExceptionally { get; set; }
}

[ApiController]
[Route("api/rpt_periodsummary")]
public class Rpt_PeriodSummaryControllerPort : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment? _env;

    public Rpt_PeriodSummaryControllerPort(AppDbContext db, IWebHostEnvironment? env = null)
    {
        _db = db;
        _env = env;
    }

    /// <summary>
    /// Port 1:1 action Search — tìm kiếm báo cáo đối soát tổng hợp kỳ Rpt_PeriodSummary.
    /// Nguồn: Rpt_PeriodSummaryController.cs:35-129 (gate), Report.cs:4614-4941 (biz).
    /// </summary>
    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Rpt_PeriodSummarySearchReq? req)
    {
        var query = _db.Set<Rpt_PeriodSummary>().AsNoTracking().AsQueryable();

        // 1. Lọc DLCPCode (hoặc DLCode) với toán tử '=' (nguyên chuỗi) — gate :618-621
        string? dlCode = req?.DLCPCode;
        if (!string.IsNullOrWhiteSpace(dlCode))
        {
            var p_dlCode = PortQuery.Equal(dlCode);
            query = query.Where(x => EF.Functions.Like(x.DLCPCode.ToLower(), p_dlCode, PortQuery.Esc));
        }

        // 2. Lọc PeriodMonth với toán tử '=' (nguyên chuỗi) — gate :623-627
        if (!string.IsNullOrWhiteSpace(req?.PeriodMonth))
        {
            var p_month = PortQuery.Equal(req.PeriodMonth);
            query = query.Where(x => EF.Functions.Like(x.PeriodMonth.ToLower(), p_month, PortQuery.Esc));
        }

        // 3. Sắp xếp: order by rps.LogLUDTimeUTC desc (Report.cs:4695, :4765)
        var list = await query.OrderByDescending(x => x.LogLUDTimeUTC).ToListAsync();

        // 4. Tính toán nghiệp vụ cột HCol: (rps.HCol + rps.LCol) — Report.cs:4754
        foreach (var item in list)
        {
            item.HCol = (item.HCol ?? 0) + (item.LCol ?? 0);
        }

        // 5. Định dạng trả về: Lst_Rpt_PeriodSummary là null khi rỗng (gate :63, :92-95)
        return Ok(new
        {
            Success = true,
            Data = new
            {
                Lst_Rpt_PeriodSummary = list.Count > 0 ? list : null,
                Lst_Rpt_PeriodSummaryDtl = (object?)null,
                Lst_Rpt_PeriodSummary_Transaction = (object?)null,
                Lst_Rpt_PeriodSummary_TransactionExceptionally = (object?)null
            }
        });
    }

    /// <summary>
    /// Port 1:1 action SearchDetail — tìm kiếm chi tiết báo cáo đối soát tổng hợp kỳ Rpt_PeriodSummaryDtl (#285).
    /// Nguồn tra cứu (chỉ đọc):
    ///   - Gate:     13.ClientGate/V10/idn.Loyalty.WebAPI/Controllers/Rpt_PeriodSummaryController.cs:136-210 (SearchDetail)
    ///   - Filter:   Rpt_PeriodSummaryController.cs:631-648 (strWhereClause_Rpt_PeriodSummaryDtl)
    ///   - Service:  13.ClientGate/V10/idn.Loyalty.ClientService/Services/Loyalty/Rpt_PeriodSummaryService.cs:21-39 (WA_Rpt_PeriodSummaryDtl_Get)
    ///   - API:      11.BackEnd/V10/idn.Loyalty.Biz.Web/Controllers/ReportController.cs:585-635 (WA_Rpt_PeriodSummaryDtl_Get)
    ///   - Biz LIVE: 11.BackEnd/V10/idn.Loyalty.Biz/Report.cs:4945-5372 (WAS_Rpt_PeriodSummaryDtl_Get, Rpt_PeriodSummaryDtl_Get, Rpt_PeriodSummaryDtl_GetX)
    ///
    /// ĐẶC ĐIỂM NGHIỆP VỤ:
    ///   - Gate SearchDetail nhận: DLCPCode, PeriodMonth.
    ///   - strWhereClause_Rpt_PeriodSummaryDtl (:631-648) chỉ lọc:
    ///       + DLCode (map từ DLCPCode tại gate :160) với toán tử '=' (nguyên chuỗi) cho cột DLCPCode.
    ///       + PeriodMonth với toán tử '=' (nguyên chuỗi) cho cột PeriodMonth.
    ///       + KHÔNG lọc KeyWord, FlagActive hay OrgID.
    ///   - Sắp xếp: order by rpsd.LogLUDTimeUTC desc (Report.cs:5254, :5317).
    ///   - Công thức tính HCol: HCol = (rpsd.HCol ?? 0) + (rpsd.LCol ?? 0) (Report.cs:5302).
    ///   - Phản hồi: khi không có bản ghi nào, Lst_Rpt_PeriodSummaryDtl trả về null (gate :163, :187-190, :193-197).
    /// </summary>
    [HttpPost("searchdetail")]
    public async Task<IActionResult> SearchDetail([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Rpt_PeriodSummarySearchReq? req)
    {
        var query = _db.Set<Rpt_PeriodSummaryDtl>().AsNoTracking().AsQueryable();

        // 1. Lọc DLCPCode (hoặc DLCode) với toán tử '=' (nguyên chuỗi, không phân biệt hoa thường) — gate :636-639
        string? dlCode = req?.DLCPCode;
        if (!string.IsNullOrWhiteSpace(dlCode))
        {
            var p_dlCode = PortQuery.Equal(dlCode);
            query = query.Where(x => EF.Functions.Like(x.DLCPCode.ToLower(), p_dlCode, PortQuery.Esc));
        }

        // 2. Lọc PeriodMonth với toán tử '=' (nguyên chuỗi, không phân biệt hoa thường) — gate :641-645
        if (!string.IsNullOrWhiteSpace(req?.PeriodMonth))
        {
            var p_month = PortQuery.Equal(req.PeriodMonth);
            query = query.Where(x => EF.Functions.Like(x.PeriodMonth.ToLower(), p_month, PortQuery.Esc));
        }

        // 3. Sắp xếp: order by rpsd.LogLUDTimeUTC desc (Report.cs:5254, :5317)
        var list = await query.OrderByDescending(x => x.LogLUDTimeUTC).ToListAsync();

        // 4. Tính toán nghiệp vụ cột HCol: (rpsd.HCol + rpsd.LCol) — Report.cs:5302
        foreach (var item in list)
        {
            item.HCol = (item.HCol ?? 0) + (item.LCol ?? 0);
        }

        // 5. Định dạng trả về: Lst_Rpt_PeriodSummaryDtl là null khi rỗng (gate :163, :187-190, :193-197)
        return Ok(new
        {
            Success = true,
            Data = new
            {
                Lst_Rpt_PeriodSummaryDtl = list.Count > 0 ? list : null
            }
        });
    }

    /// <summary>
    /// Port 1:1 action SearchDetail01 — tìm kiếm chi tiết báo cáo đối soát tổng hợp kỳ Rpt_PeriodSummaryDtl_01 (#286).
    /// Nguồn tra cứu (chỉ đọc):
    ///   - Gate:     13.ClientGate/V10/idn.Loyalty.WebAPI/Controllers/Rpt_PeriodSummaryController.cs:212-286 (SearchDetail01)
    ///   - Filter:   Rpt_PeriodSummaryController.cs:649-666 (strWhereClause_Rpt_PeriodSummaryDtl_01)
    ///   - Service:  13.ClientGate/V10/idn.Loyalty.ClientService/Services/Loyalty/Rpt_PeriodSummaryService.cs:21-27, :52-56 (WA_Rpt_PeriodSummaryDtl_01_Get)
    ///   - API:      11.BackEnd/V10/idn.Loyalty.Biz.Web/Controllers/ReportController.cs:656-724 (WA_Rpt_PeriodSummaryDtl_01_Get)
    ///   - Biz LIVE: 11.BackEnd/V10/idn.Loyalty.Biz/Report.cs:5376-5781 (WAS_Rpt_PeriodSummaryDtl_01_Get, Rpt_PeriodSummaryDtl_01_Get, Rpt_PeriodSummaryDtl_01_GetX)
    ///
    /// ĐẶC ĐIỂM NGHIỆP VỤ:
    ///   - Gate SearchDetail01 nhận: DLCPCode, PeriodMonth.
    ///   - strWhereClause_Rpt_PeriodSummaryDtl_01 (:649-666) chỉ lọc:
    ///       + DLCode (map từ DLCPCode tại gate :236) với toán tử '=' (nguyên chuỗi, không phân biệt hoa thường) cho cột DLCPCode.
    ///       + PeriodMonth với toán tử '=' (nguyên chuỗi, không phân biệt hoa thường) cho cột PeriodMonth.
    ///       + KHÔNG lọc KeyWord, FlagActive hay OrgID.
    ///   - Sắp xếp: order by rpsd.LogLUDTimeUTC desc (Report.cs:5685, :5726).
    ///   - Cột HCol: KHÔNG tính lại (giữ nguyên DB theo Report.cs:5721-5726).
    ///   - Phản hồi: khi không có bản ghi nào, Lst_Rpt_PeriodSummaryDtl_01 trả về null (gate :239, :263-266, :269-273).
    /// </summary>
    [HttpPost("searchdetail01")]
    public async Task<IActionResult> SearchDetail01([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Rpt_PeriodSummarySearchReq? req)
    {
        var query = _db.Set<Rpt_PeriodSummaryDtl_01>().AsNoTracking().AsQueryable();

        // 1. Lọc DLCPCode (hoặc DLCode) với toán tử '=' (nguyên chuỗi, không phân biệt hoa thường) — gate :654-657
        string? dlCode = req?.DLCPCode;
        if (!string.IsNullOrWhiteSpace(dlCode))
        {
            var p_dlCode = PortQuery.Equal(dlCode);
            query = query.Where(x => EF.Functions.Like(x.DLCPCode.ToLower(), p_dlCode, PortQuery.Esc));
        }

        // 2. Lọc PeriodMonth với toán tử '=' (nguyên chuỗi, không phân biệt hoa thường) — gate :659-663
        if (!string.IsNullOrWhiteSpace(req?.PeriodMonth))
        {
            var p_month = PortQuery.Equal(req.PeriodMonth);
            query = query.Where(x => EF.Functions.Like(x.PeriodMonth.ToLower(), p_month, PortQuery.Esc));
        }

        // 3. Sắp xếp: order by rpsd.LogLUDTimeUTC desc (Report.cs:5685, :5726)
        var list = await query.OrderByDescending(x => x.LogLUDTimeUTC).ToListAsync();

        // 4. Cột HCol: KHÔNG tính lại, giữ nguyên giá trị DB (Report.cs:5721-5726)

        // 5. Định dạng trả về: Lst_Rpt_PeriodSummaryDtl_01 là null khi rỗng (gate :239, :263-266, :269-273)
        return Ok(new
        {
            Success = true,
            Data = new
            {
                Lst_Rpt_PeriodSummaryDtl_01 = list.Count > 0 ? list : null
            }
        });
    }

    /// <summary>
    /// Port 1:1 action SearchTransaction — tìm kiếm giao dịch đối soát tổng hợp kỳ Rpt_PeriodSummary_Transaction (#287).
    /// Nguồn tra cứu (chỉ đọc):
    ///   - Gate:     13.ClientGate/V10/idn.Loyalty.WebAPI/Controllers/Rpt_PeriodSummaryController.cs:288-363 (SearchTransaction)
    ///   - Filter:   Rpt_PeriodSummaryController.cs:667-700 (strWhereClause_Rpt_PeriodSummary_Transaction)
    ///   - Service:  13.ClientGate/V10/idn.Loyalty.ClientService/Services/Loyalty/Rpt_PeriodSummaryService.cs:41-45 (WA_Rpt_PeriodSummary_Transaction_Get)
    ///   - API:      11.BackEnd/V10/idn.Loyalty.Biz.Web/Controllers/ReportController.cs:725-789 (WA_Rpt_PeriodSummary_Transaction_Get)
    ///   - Biz LIVE: 11.BackEnd/V10/idn.Loyalty.Biz/Report.cs:5785-5890 (WAS_Rpt_PeriodSummary_Transaction_Get),
    ///               :6053-6275 (Rpt_PeriodSummary_Transaction_GetX)
    ///
    /// ĐẶC ĐIỂM NGHIỆP VỤ:
    ///   - Gate SearchTransaction nhận: DLCPCode, PeriodMonth (:288).
    ///   - strWhereClause_Rpt_PeriodSummary_Transaction (:667-700) lọc:
    ///       + DLCode (map từ DLCPCode tại gate :312): toán tử '=' (nguyên chuỗi, không phân biệt hoa thường) trên cột DLCode (:674).
    ///       + PeriodMonth (gate :313): toán tử '=' (nguyên chuỗi, không phân biệt hoa thường) trên cột PeriodMonth (:690).
    ///       + KHÔNG lọc KeyWord, FlagActive hay OrgID.
    ///   - Sắp xếp: order by rpst.LogLUDTimeUTC desc (Report.cs:6117, :6167).
    ///   - Join: inner join Crd_CardTransaction cct on t.AutoID = cct.AutoID để lấy 2 cột CreateDTimeUTC và CardNo (:6164-6165).
    ///   - Định dạng CreateDTimeUTC: chuỗi yyyy-MM-dd sau khi quy đổi giờ VN (UTC+7) qua StdDTimeVN và StdDate (Report.cs:5854-5861).
    ///   - Phản hồi: khi không có bản ghi nào, Lst_Rpt_PeriodSummary_Transaction trả về null (gate :315, :340-343, :348).
    /// </summary>
    [HttpPost("searchtransaction")]
    public async Task<IActionResult> SearchTransaction([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Rpt_PeriodSummarySearchReq? req)
    {
        var query = _db.Set<Rpt_PeriodSummary_Transaction>().AsNoTracking().AsQueryable();

        // 1. Lọc DLCode (hoặc DLCPCode) với toán tử '=' (nguyên chuỗi, không phân biệt hoa thường) — gate :672-675
        string? dlCode = req?.DLCPCode;
        if (!string.IsNullOrWhiteSpace(dlCode))
        {
            var p_dlCode = PortQuery.Equal(dlCode);
            query = query.Where(x => x.DLCode != null && EF.Functions.Like(x.DLCode.ToLower(), p_dlCode, PortQuery.Esc));
        }

        // 2. Lọc PeriodMonth với toán tử '=' (nguyên chuỗi, không phân biệt hoa thường) — gate :687-691
        if (!string.IsNullOrWhiteSpace(req?.PeriodMonth))
        {
            var p_month = PortQuery.Equal(req.PeriodMonth);
            query = query.Where(x => EF.Functions.Like(x.PeriodMonth.ToLower(), p_month, PortQuery.Esc));
        }

        // 3. Inner join với CrdCardTransaction trên AutoID, sắp xếp LogLUDTimeUTC desc — Report.cs:6154-6169
        var joinedQuery = from rpst in query
                          join cct in _db.Set<CrdCardTransaction>().AsNoTracking()
                              on rpst.AutoID equals cct.AutoId
                          orderby rpst.LogLUDTimeUTC descending
                          select new
                          {
                              Item = rpst,
                              CardNo = cct.CardNo,
                              CreateDTimeUtc = cct.CreateDTimeUTC
                          };

        var rawList = await joinedQuery.ToListAsync();

        // 4. Format CreateDTimeUTC quy đổi giờ VN (UTC+7) định dạng yyyy-MM-dd — Report.cs:5854-5861
        var list = rawList.Select(x =>
        {
            var item = x.Item;
            item.CardNo = x.CardNo;
            if (x.CreateDTimeUtc.HasValue)
            {
                item.CreateDTimeUTC = x.CreateDTimeUtc.Value.ToString("yyyy-MM-dd HH:mm:ss");   // nguồn không đổi giờ (cct.CreateDTimeUTC trả nguyên)
            }
            return item;
        }).ToList();

        // 5. Định dạng trả về: Lst_Rpt_PeriodSummary_Transaction là null khi rỗng (gate :315, :340-343, :348)
        return Ok(new
        {
            Success = true,
            Data = new
            {
                Lst_Rpt_PeriodSummary_Transaction = list.Count > 0 ? list : null
            }
        });
    }

    /// <summary>
    /// Port 1:1 action SearchTransactionExceptionally — tìm kiếm giao dịch đặc cách đối soát tổng hợp kỳ Rpt_PeriodSummary_TransactionExceptionally (#288).
    /// Nguồn tra cứu (chỉ đọc):
    ///   - Gate:     13.ClientGate/V10/idn.Loyalty.WebAPI/Controllers/Rpt_PeriodSummaryController.cs:365-443 (SearchTransactionExceptionally)
    ///   - Filter:   Rpt_PeriodSummaryController.cs:667-700 (strWhereClause_Rpt_PeriodSummary_Transaction)
    ///   - Service:  13.ClientGate/V10/idn.Loyalty.ClientService/Services/Loyalty/Rpt_PeriodSummaryService.cs:41-45 (WA_Rpt_PeriodSummary_Transaction_Get)
    ///   - API:      11.BackEnd/V10/idn.Loyalty.Biz.Web/Controllers/ReportController.cs:725-789 (WA_Rpt_PeriodSummary_Transaction_Get)
    ///   - Biz LIVE: 11.BackEnd/V10/idn.Loyalty.Biz/Report.cs:5785-5909 (WAS_Rpt_PeriodSummary_Transaction_Get),
    ///               :6053-6275 (Rpt_PeriodSummary_Transaction_GetX)
    ///
    /// ĐẶC ĐIỂM NGHIỆP VỤ:
    ///   - Gate SearchTransactionExceptionally nhận: PeriodMonth, DLCodeExceptionally (:365).
    ///   - strWhereClause_Rpt_PeriodSummary_Transaction (:667-700) lọc trên bảng draft Rpt_PeriodSummary_Transaction:
    ///       + DLCodeExceptionally: toán tử '=' (nguyên chuỗi, không phân biệt hoa thường) trên cột DLCodeExceptionally (:696).
    ///       + PeriodMonth: toán tử '=' (nguyên chuỗi, không phân biệt hoa thường) trên cột PeriodMonth (:690).
    ///       + DealPointType: in ('INTRODUCTION', 'CONSUMPTION', 'BIRTHDAY', 'SALES', 'POINTUSE') (:677-685, gate :392).
    ///       + KHÔNG lọc KeyWord, FlagActive hay OrgID.
    ///   - Inner join với Rpt_PeriodSummary_TransactionExceptionally rpste trên (PeriodMonth, DLCodeExceptionally, AutoID) (:6206-6208).
    ///   - Left join với CrdCardTransaction cct trên t.AutoID = cct.AutoId để lấy CreateDTimeUTC và CardNo (:6209-6210).
    ///   - Điều kiện lọc trên rpste: rpste.DLCode != rpste.DLCodeExceptionally và rpste.DealPointType == "CONSUMPTION" (:6212-6213).
    ///   - Sắp xếp: order by rpste.LogLUDTimeUTC desc (:6215).
    ///   - Che giá trị 3 cột: AutoID = "N/A", RefNo = "N/A", DLCode = "N/A" theo đúng câu SQL nguồn (:6184, :6185, :6189).
    ///   - Định dạng CreateDTimeUTC: chuỗi yyyy-MM-dd sau khi quy đổi giờ VN (UTC+7) qua StdDTimeVN và StdDate (Report.cs:5874-5879).
    ///   - Phản hồi: khi không có bản ghi nào, Lst_Rpt_PeriodSummary_TransactionExceptionally trả về null (gate :395, :420-430).
    /// </summary>
    [HttpPost("searchtransactionexceptionally")]
    public async Task<IActionResult> SearchTransactionExceptionally([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Rpt_PeriodSummarySearchTransactionExceptionallyReq? req)
    {
        var allowedTypes = new[] { "INTRODUCTION", "CONSUMPTION", "BIRTHDAY", "SALES", "POINTUSE" };

        var draftQuery = _db.Set<Rpt_PeriodSummary_Transaction>().AsNoTracking().AsQueryable();

        // 1. DealPointType in ('INTRODUCTION', 'CONSUMPTION', 'BIRTHDAY', 'SALES', 'POINTUSE') — gate :392, Report.cs:677-685
        draftQuery = draftQuery.Where(x => x.DealPointType != null && allowedTypes.Contains(x.DealPointType));

        // 2. Lọc PeriodMonth với toán tử '=' (nguyên chuỗi, không phân biệt hoa thường) — gate :391, :690
        if (!string.IsNullOrWhiteSpace(req?.PeriodMonth))
        {
            var p_month = PortQuery.Equal(req.PeriodMonth);
            draftQuery = draftQuery.Where(x => EF.Functions.Like(x.PeriodMonth.ToLower(), p_month, PortQuery.Esc));
        }

        // 3. Lọc DLCodeExceptionally (hoặc fallback DLCPCode) với toán tử '=' (nguyên chuỗi, không phân biệt hoa thường) — gate :390, :696
        string? dlCodeExc = req?.DLCodeExceptionally;
        if (!string.IsNullOrWhiteSpace(dlCodeExc))
        {
            var p_dlExc = PortQuery.Equal(dlCodeExc);
            draftQuery = draftQuery.Where(x => x.DLCodeExceptionally != null && EF.Functions.Like(x.DLCodeExceptionally.ToLower(), p_dlExc, PortQuery.Esc));
        }

        // 4. Inner join với Rpt_PeriodSummary_TransactionExceptionally trên (PeriodMonth, DLCodeExceptionally, AutoID),
        //    left join với CrdCardTransaction trên AutoID,
        //    lọc rpste.DLCode != rpste.DLCodeExceptionally và rpste.DealPointType == "CONSUMPTION",
        //    sắp xếp rpste.LogLUDTimeUTC desc — Report.cs:6181-6215
        var joinedQuery = from t in draftQuery
                          join rpste in _db.Set<Rpt_PeriodSummary_TransactionExceptionally>().AsNoTracking()
                              on new { t.PeriodMonth, t.DLCodeExceptionally, t.AutoID }
                              equals new { rpste.PeriodMonth, rpste.DLCodeExceptionally, rpste.AutoID }
                          join cct in _db.Set<CrdCardTransaction>().AsNoTracking()
                              on t.AutoID equals cct.AutoId into cctGroup
                          from cct in cctGroup.DefaultIfEmpty()
                          where rpste.DLCode != rpste.DLCodeExceptionally
                             && rpste.DealPointType == "CONSUMPTION"
                          orderby rpste.LogLUDTimeUTC descending
                          select new
                          {
                              Item = rpste,
                              CardNo = cct != null ? cct.CardNo : null,
                              CreateDTimeUtc = cct != null ? cct.CreateDTimeUTC : null
                          };

        var rawList = await joinedQuery.ToListAsync();

        // 5. Che cột 'N/A' (AutoID, RefNo, DLCode) và format CreateDTimeUTC quy đổi giờ VN (UTC+7) định dạng yyyy-MM-dd — Report.cs:6184-6189, :5874-5879
        var list = rawList.Select(x => new
        {
            PeriodMonth = x.Item.PeriodMonth,
            AutoID = "N/A",
            RefNo = "N/A",
            NetworkID = x.Item.NetworkID,
            OrgID = x.Item.OrgID,
            DealPointType = x.Item.DealPointType,
            DLCode = "N/A",
            RefCode07 = x.Item.RefCode07,
            MemberNo = x.Item.MemberNo,
            MemberName = x.Item.MemberName,
            CardTypeUse = x.Item.CardTypeUse,
            CardTypeInit = x.Item.CardTypeInit,
            DLCodeExceptionally = x.Item.DLCodeExceptionally,
            AmountChTotal = x.Item.AmountChTotal,
            AmountChTotalInit = x.Item.AmountChTotalInit,
            AmountChTotalDiffer = x.Item.AmountChTotalDiffer,
            PointExpiryDTime = x.Item.PointExpiryDTime,
            LogLUDTimeUTC = x.Item.LogLUDTimeUTC,
            LogLUBy = x.Item.LogLUBy,
            PointChTotal = x.Item.PointChTotal,
            PointChTotalInit = x.Item.PointChTotalInit,
            PointChTotalDiffer = x.Item.PointChTotalDiffer,
            CreateDTimeUTC = x.CreateDTimeUtc.HasValue ? x.CreateDTimeUtc.Value.ToString("yyyy-MM-dd HH:mm:ss") : null,   // nguồn trả nguyên cct.CreateDTimeUTC
            CardNo = x.CardNo
        }).ToList();

        // 6. Định dạng trả về: Lst_Rpt_PeriodSummary_TransactionExceptionally là null khi rỗng (gate :395, :420-430)
        return Ok(new
        {
            Success = true,
            Data = new
            {
                Lst_Rpt_PeriodSummary_TransactionExceptionally = list.Count > 0 ? (object)list : null
            }
        });
    }

    /// <summary>
    /// Port 1:1 action SearchTransactionDetail — báo cáo chi tiết giao dịch thẻ (#289).
    /// Nguồn tra cứu (chỉ đọc):
    ///   - Gate:     13.ClientGate/V10/idn.Loyalty.WebAPI/Controllers/Rpt_PeriodSummaryController.cs:445-530 (SearchTransactionDetail)
    ///   - Filter:   Rpt_PeriodSummaryController.cs:737-774 (strWhereClause_Crd_CardTransaction)
    ///   - Service:  13.ClientGate/V10/idn.Loyalty.ClientService/Services/Loyalty/Crd_CardTransactionService.cs:25-34 (WA_Crd_CardTransaction_Get)
    ///   - API:      11.BackEnd/V10/idn.Loyalty.Biz.Web/Controllers/CrdCardTransactionController.cs:24-60 (WA_Crd_CardTransaction_Get)
    ///   - Biz LIVE: 11.BackEnd/V10/idn.Loyalty.Biz/Master.Loyalty.cs:21144-21250 (WAS_Crd_CardTransaction_Get),
    ///               :21254-21408 (Crd_CardTransaction_Get), :21409-21817 (Crd_CardTransaction_GetX)
    ///
    /// ĐẶC ĐIỂM NGHIỆP VỤ:
    ///   - Gate SearchTransactionDetail nhận: PeriodMonth, DLCode, DealPointType, CreateDTimeUTCFrom, CreateDTimeUTCTo (:445).
    ///   - strWhereClause_Crd_CardTransaction (:737-774) lọc:
    ///       + DLCode: toán tử '=' trên cột DLCode (:744).
    ///       + DealPointType: tách dấu phẩy, bọc nháy từng phần tử, toán tử 'in' (:749-754).
    ///       + CreateDTimeUTCFrom: StdDTimeBeginDay (00:00:00 VN) -> StdDTimeUTC (-7h), toán tử '>=' (:759-762).
    ///       + CreateDTimeUTCTo: StdDTimeEndDay (23:59:59 VN) -> StdDTimeUTC (-7h), toán tử '<=' (:766-769).
    ///       + PeriodMonth: nhận ở gate nhưng KHÔNG truyền vào hàm lọc và KHÔNG lọc (:467-475, :737-774).
    ///       + KHÔNG có tham số KeyWord.
    ///       + OrgID: nhận từ header gán vào WARQBase nhưng strWhereClause_Crd_CardTransaction KHÔNG lọc OrgID (:21487-21488 comment join NNT).
    ///   - Xử lý nghiệp vụ sau khi lấy danh sách (:503-509):
    ///       + Nếu DealPointType == "POINTINCREASE", ghi đè: AmountChTotal = PointChRankTotal.
    ///   - Sắp xếp: order by cct.LogLUDTimeUTC desc (:21493, :21618).
    ///   - Union DISCOUNTRO (:21664-21791): Nếu DealPointType có chứa "DISCOUNTRO", UNION ALL với Crd_MemberDiscountTransaction inner join Crd_Member.
    ///       Dòng chiết khấu: DealPointType = 'DISCOUNTRO', các cột điểm-hạng = 0,
    ///       DiscountRate = mvt.PolicyDiscountRate, DiscountAmount = mvt.PointChTotal, DiscountRevenue = mvt.AmountForDC.
    ///   - Định dạng ngày: CreateDTimeUTC quy đổi giờ VN StdDate(StdDTimeVN(...)) thành chuỗi yyyy-MM-dd; PointExpiryDTime thành yyyy-MM-dd (:21211-21223).
    ///   - Phản hồi: khi không có bản ghi nào, Lst_Crd_CardTransaction là null (KHÔNG phải []) (:476, :500, :515).
    /// </summary>
    [HttpPost("searchtransactiondetail")]
    public async Task<IActionResult> SearchTransactionDetail([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Rpt_PeriodSummarySearchTransactionDetailReq? req)
    {
        var allList = await GetTransactionDetailListAsync(req);

        // 9. Định dạng trả về: Lst_Crd_CardTransaction là null khi rỗng — gate :476, :500, :515
        return Ok(new
        {
            Success = true,
            Data = new
            {
                Lst_Crd_CardTransaction = allList.Count > 0 ? (object)allList : null
            }
        });
    }

    /// <summary>
    /// Port 1:1 action ExportExcelTransactionDetail — xuất Excel danh sách chi tiết giao dịch thẻ đối soát tổng hợp kỳ (#295).
    /// Nguồn tra cứu (chỉ đọc):
    ///   - Gate:     13.ClientGate/V10/idn.Loyalty.WebAPI/Controllers/Rpt_PeriodSummaryController.cs:1332-1428 (ExportExcelTransactionDetail)
    ///   - Filter:   Rpt_PeriodSummaryController.cs:737-774 (strWhereClause_Crd_CardTransaction)
    ///               + DLCode: toán tử '=' nguyên chuỗi (:744)
    ///               + DealPointType: toán tử 'in' tách dấu phẩy (:749-754)
    ///               + CreateDTimeUTCFrom: toán tử '>=' (:759-762)
    ///               + CreateDTimeUTCTo: toán tử '<=' (:766-769)
    ///   - Service:  13.ClientGate/V10/idn.Loyalty.ClientService/Services/Loyalty/Crd_CardTransactionService.cs:25-34 (WA_Crd_CardTransaction_Get)
    ///   - API:      11.BackEnd/V10/idn.Loyalty.Biz.Web/Controllers/CrdCardTransactionController.cs:24-60 (WA_Crd_CardTransaction_Get)
    ///   - Biz LIVE: 11.BackEnd/V10/idn.Loyalty.Biz/Master.Loyalty.cs:21144-21250 (WAS_Crd_CardTransaction_Get),
    ///               :21254-21408 (Crd_CardTransaction_Get),
    ///               :21409-21817 (Crd_CardTransaction_GetX)
    ///               Inner join Crd_Member, Crd_Card, union Crd_MemberDiscountTransaction khi DealPointType chứa 'DISCOUNTRO' (:21664-21791)
    ///               Sắp xếp: order by LogLUDTimeUTC desc (:21618)
    ///               Ghi đè: if (DealPointType == PointIncrease) AmountChTotal = PointChRankTotal (:1390-1396)
    ///   - Columns:  Rpt_PeriodSummaryController.cs:1541-1565 (GetImportDicColumsTransactionDtl - 19 cột)
    ///   - Sheet:    "Crd_CardTransaction" (:1401, :1403)
    ///   - Khi rỗng: throw new ServiceException("Không có dữ liệu") (:1414) -> HTTP 400 PortResult.Fail("Rpt_PeriodSummary_ExportExcelTransactionDetail", "Message", "Không có dữ liệu")
    ///   - Khi thành công: Success = true, Data = { Url = ... } (:1405-1409)
    /// </summary>
    [HttpPost("exportexceltransactiondetail")]
    public async Task<IActionResult> ExportExcelTransactionDetail([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Rpt_PeriodSummarySearchTransactionDetailReq? req)
    {
        var list = await GetTransactionDetailListAsync(req);

        // 1. Kiểm tra danh sách rỗng — gate :1414, :1420
        // Khi không có dữ liệu: throw ServiceException("Không có dữ liệu") -> gate catch :1420
        // Trên port trả HTTP 400: PortResult.Fail("Rpt_PeriodSummary_ExportExcelTransactionDetail", "Message", "Không có dữ liệu")
        if (list == null || list.Count == 0)
        {
            return PortResult.Fail("Rpt_PeriodSummary_ExportExcelTransactionDetail", "Message", "Không có dữ liệu");
        }

        // 2. Chuẩn bị đường dẫn thư mục xuất file Excel — gate :1401
        var webRoot = _env?.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
        {
            webRoot = _env != null
                ? Path.Combine(_env.ContentRootPath, "wwwroot")
                : Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        }

        string virtualPath;
        try
        {
            // 3. Lấy từ điển 19 cột từ GetImportDicColumsTransactionDtl(), sinh file và xuất Excel — gate :1398, :1401, :1403
            var dicColNames = GetImportDicColumsTransactionDtl();
            var (physicalPath, vPath) = PortExcel.GenExcelExportFilePath(webRoot, "Crd_CardTransaction");
            virtualPath = vPath;
            PortExcel.ExportToExcelFromList(list, dicColNames, physicalPath, "Crd_CardTransaction");
        }
        catch (Exception ex)
        {
            // Bắt ngoại lệ nếu ghi file lỗi -> trả Fail (gate :1420)
            return PortResult.Fail("Rpt_PeriodSummary_ExportExcelTransactionDetail", "Exception", ex.Message);
        }

        // 4. Trả URL tải file với Success = true — gate :1405-1409
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
    /// Helper dùng chung lấy danh sách chi tiết giao dịch cho SearchTransactionDetail (#289) và ExportExcelTransactionDetail (#295).
    /// </summary>
    private async Task<List<Crd_CardTransactionSearchResult>> GetTransactionDetailListAsync(Rpt_PeriodSummarySearchTransactionDetailReq? req)
    {
        var normalQuery = from cct in _db.Set<CrdCardTransaction>().AsNoTracking()
                          join cm in _db.Set<CrdMember>().AsNoTracking() on cct.MemberNo equals cm.MemberNo
                          join cc in _db.Set<CrdCard>().AsNoTracking() on new { cct.MemberNo, cct.CardNo } equals new { cc.MemberNo, cc.CardNo }
                          select new { cct, cm, cc };

        // 1. DLCode: toán tử '=' trên cột DLCode — gate :744
        if (!string.IsNullOrWhiteSpace(req?.DLCode))
        {
            var p_dlCode = PortQuery.Equal(req.DLCode);
            normalQuery = normalQuery.Where(x => x.cct.DLCode != null && EF.Functions.Like(x.cct.DLCode.ToLower(), p_dlCode, PortQuery.Esc));
        }

        // 2. DealPointType: tách dấu phẩy, bọc nháy từng phần tử, toán tử 'in' — gate :749-754
        List<string> dealTypes = new();
        bool wantDiscount = false;
        if (!string.IsNullOrWhiteSpace(req?.DealPointType))
        {
            dealTypes = req.DealPointType.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                         .Select(s => s.ToLowerInvariant())
                                         .ToList();
            if (dealTypes.Count > 0)
            {
                normalQuery = normalQuery.Where(x => x.cct.DealPointType != null && dealTypes.Contains(x.cct.DealPointType.ToLower()));
            }
            if (req.DealPointType.ToUpperInvariant().Contains(LoyaltyConst.DealPointType.DiscountRO))
            {
                wantDiscount = true;
            }
        }

        // 3. CreateDTimeUTCFrom: StdDTimeBeginDay -> StdDTimeUTC -> '>=' trên CreateDTimeUTC — gate :759-762
        DateTime? utcFrom = null;
        if (!string.IsNullOrWhiteSpace(req?.CreateDTimeUTCFrom) && DateTime.TryParse(req.CreateDTimeUTCFrom, out var dFrom))
        {
            utcFrom = dFrom.Date.AddHours(-7); // Bắt đầu ngày Việt Nam (UTC+7) chuyển sang UTC
            normalQuery = normalQuery.Where(x => x.cct.CreateDTimeUTC >= utcFrom);
        }

        // 4. CreateDTimeUTCTo: StdDTimeEndDay -> StdDTimeUTC -> '<=' trên CreateDTimeUTC — gate :766-769
        DateTime? utcTo = null;
        if (!string.IsNullOrWhiteSpace(req?.CreateDTimeUTCTo) && DateTime.TryParse(req.CreateDTimeUTCTo, out var dTo))
        {
            utcTo = dTo.Date.AddDays(1).AddSeconds(-1).AddHours(-7); // Cuối ngày Việt Nam (UTC+7) chuyển sang UTC
            normalQuery = normalQuery.Where(x => x.cct.CreateDTimeUTC <= utcTo);
        }

        // 5. PeriodMonth: nhận ở gate nhưng KHÔNG truyền vào hàm lọc và KHÔNG lọc — gate :467-475, :737-774

        var normalList = await normalQuery.Select(x => new Crd_CardTransactionSearchResult
        {
            AutoId = x.cct.AutoId,
            RefNo = x.cct.RefNo,
            ProjectCode = x.cct.ProjectCode,
            NetworkID = x.cct.NetworkID,
            OrgID = x.cct.OrgID,
            DLCode = x.cct.DLCode,
            MemberNo = x.cct.MemberNo,
            CardNo = x.cct.CardNo,
            CardSourceCode = x.cct.CardSourceCode,
            CardTypeUse = x.cct.CardTypeUse,
            CardTypeInit = x.cct.CardTypeInit,
            DealPointType = x.cct.DealPointType,
            PrProgramCode = x.cct.PrProgramCode,
            EffDateStart = x.cct.EffDateStart,
            EffDateEnd = x.cct.EffDateEnd,
            UsePrmDTime = x.cct.UsePrmDTime,
            PointChTotal = x.cct.PointChTotal,
            PointChBlock = x.cct.PointChBlock,
            AmountChTotal = x.cct.AmountChTotal,
            AmountChBlock = x.cct.AmountChBlock,
            QtyVisitChTotal = x.cct.QtyVisitChTotal,
            QtyVisitChBlock = x.cct.QtyVisitChBlock,
            QtyPrChTotal = x.cct.QtyPrChTotal,
            QtyPrChBlock = x.cct.QtyPrChBlock,
            QtyPrUsed = x.cct.QtyPrUsed,
            PointRef = x.cct.PointRef,
            AmountRef = x.cct.AmountRef,
            PolicyPointUnit = x.cct.PolicyPointUnit,
            PolicyAmountUnit = x.cct.PolicyAmountUnit,
            PolicyCode = x.cct.PolicyCode,
            ParamPointUnit = x.cct.ParamPointUnit,
            ParamAmountUnit = x.cct.ParamAmountUnit,
            ParamCode = x.cct.ParamCode,
            FlagViewUI = x.cct.FlagViewUI,
            FlagPoint = x.cct.FlagPoint,
            FlagAmount = x.cct.FlagAmount,
            FlagPr = x.cct.FlagPr,
            FunctionName = x.cct.FunctionName,
            FunctionActionType = x.cct.FunctionActionType,
            FunctionRemark = x.cct.FunctionRemark,
            RefType = x.cct.RefType,
            PointChRankTotal = x.cct.PointChRankTotal,
            RefCode00 = x.cct.RefCode00,
            RefCode01 = x.cct.RefCode01,
            RefCode02 = x.cct.RefCode02,
            RefCode03 = x.cct.RefCode03,
            RefCode04 = x.cct.RefCode04,
            RefCode05 = x.cct.RefCode05,
            RefCode06 = x.cct.RefCode06,
            RefCode07 = x.cct.RefCode07,
            RefCode08 = x.cct.RefCode08,
            RefCode09 = x.cct.RefCode09,
            CreateDTimeUTC = x.cct.CreateDTimeUTC.HasValue ? x.cct.CreateDTimeUTC.Value.ToString("yyyy-MM-dd HH:mm:ss") : null,   // gate :503-509 không đổi giờ
            CreateBy = x.cct.CreateBy,
            LUDTimeUTC = x.cct.LUDTimeUTC,
            LUBy = x.cct.LUBy,
            LogLUDTimeUTC = x.cct.LogLUDTimeUTC,
            LogLUBy = x.cct.LogLUBy,
            PointExpiryDTime = x.cct.PointExpiryDTime.HasValue ? x.cct.PointExpiryDTime.Value.ToString("yyyy-MM-dd") : null,
            AmountChTotalInit = x.cct.AmountChTotalInit,
            AmountChTotalDiffer = x.cct.AmountChTotalDiffer,
            AmountAvailPrevious = x.cct.AmountAvailPrevious,
            PointExpiryDTimeSync = x.cct.PointExpiryDTimeSync,
            PointChTotalInit = x.cct.PointChTotalInit,
            PointChTotalDiffer = x.cct.PointChTotalDiffer,
            PrProgramName = null,
            Remark = null,
            Unit = null,
            CarNo = x.cm.CarNo,
            PhoneNo = x.cm.PhoneNo,
            MemberName = x.cm.MemberName,
            DLCodeExceptionally = x.cc.DLCodeExceptionally,
            DiscountRate = 0d,
            DiscountAmount = 0d,
            DiscountRevenue = 0d
        }).ToListAsync();

        // 6. Union DISCOUNTRO khi DealPointType có chứa DISCOUNTRO — Master.Loyalty.cs:21664-21791
        var discountList = new List<Crd_CardTransactionSearchResult>();
        if (wantDiscount)
        {
            var discountQuery = from mvt in _db.Set<CrdMemberDiscountTransaction>().AsNoTracking()
                                join cm in _db.Set<CrdMember>().AsNoTracking() on mvt.MemberNo equals cm.MemberNo
                                select new { mvt, cm };

            if (!string.IsNullOrWhiteSpace(req?.DLCode))
            {
                var p_dlCode = PortQuery.Equal(req.DLCode);
                discountQuery = discountQuery.Where(x => x.mvt.DLCode != null && EF.Functions.Like(x.mvt.DLCode.ToLower(), p_dlCode, PortQuery.Esc));
            }

            if (dealTypes.Count > 0)
            {
                discountQuery = discountQuery.Where(x => x.mvt.DealPointType != null && dealTypes.Contains(x.mvt.DealPointType.ToLower()));
            }

            if (utcFrom.HasValue)
            {
                discountQuery = discountQuery.Where(x => x.mvt.CreateDTimeUTC >= utcFrom.Value);
            }

            if (utcTo.HasValue)
            {
                discountQuery = discountQuery.Where(x => x.mvt.CreateDTimeUTC <= utcTo.Value);
            }

            discountList = await discountQuery.Select(x => new Crd_CardTransactionSearchResult
            {
                AutoId = x.mvt.AutoId,
                RefNo = x.mvt.RefNo,
                ProjectCode = null,
                NetworkID = x.mvt.NetworkID,
                OrgID = x.mvt.OrgID,
                DLCode = x.mvt.DLCode,
                MemberNo = x.mvt.MemberNo,
                CardNo = x.mvt.CardNo,
                CardSourceCode = null,
                CardTypeUse = x.mvt.CardTypeUse,
                CardTypeInit = x.mvt.CardTypeInit,
                DealPointType = LoyaltyConst.DealPointType.DiscountRO,
                PrProgramCode = null,
                EffDateStart = x.mvt.EffDateStart,
                EffDateEnd = x.mvt.EffDateEnd,
                UsePrmDTime = null,
                PointChTotal = 0d,
                PointChBlock = 0d,
                AmountChTotal = 0d,
                AmountChBlock = 0d,
                QtyVisitChTotal = 0d,
                QtyVisitChBlock = 0d,
                QtyPrChTotal = 0d,
                QtyPrChBlock = 0d,
                QtyPrUsed = 0d,
                PointRef = 0d,
                AmountRef = 0d,
                PolicyPointUnit = null,
                PolicyAmountUnit = null,
                PolicyCode = x.mvt.PolicyCode,
                ParamPointUnit = null,
                ParamAmountUnit = null,
                ParamCode = null,
                FlagViewUI = null,
                FlagPoint = null,
                FlagAmount = null,
                FlagPr = null,
                FunctionName = null,
                FunctionActionType = null,
                FunctionRemark = null,
                RefType = null,
                PointChRankTotal = 0d,
                RefCode00 = x.mvt.RefCode00,
                RefCode01 = x.mvt.RefCode01,
                RefCode02 = x.mvt.RefCode02,
                RefCode03 = x.mvt.RefCode03,
                RefCode04 = x.mvt.RefCode04,
                RefCode05 = x.mvt.RefCode05,
                RefCode06 = null,
                RefCode07 = null,
                RefCode08 = null,
                RefCode09 = null,
                CreateDTimeUTC = x.mvt.CreateDTimeUTC.HasValue ? x.mvt.CreateDTimeUTC.Value.AddHours(7).ToString("yyyy-MM-dd") : null,
                CreateBy = x.mvt.CreateBy,
                LUDTimeUTC = x.mvt.LUDTimeUTC,
                LUBy = x.mvt.LUBy,
                LogLUDTimeUTC = x.mvt.LogLUDTimeUTC,
                LogLUBy = x.mvt.LogLUBy,
                PointExpiryDTime = null,
                AmountChTotalInit = 0d,
                AmountChTotalDiffer = 0d,
                AmountAvailPrevious = 0d,
                PointExpiryDTimeSync = null,
                PointChTotalInit = 0d,
                PointChTotalDiffer = 0d,
                PrProgramName = null,
                Remark = null,
                Unit = null,
                CarNo = x.cm.CarNo,
                PhoneNo = x.cm.PhoneNo,
                MemberName = x.cm.MemberName,
                DLCodeExceptionally = x.mvt.DLCodeExceptionally,
                DiscountRate = x.mvt.PolicyDiscountRate ?? 0d,
                DiscountAmount = x.mvt.PointChTotal ?? 0d,
                DiscountRevenue = x.mvt.AmountForDC ?? 0d
            }).ToListAsync();
        }

        // 7. Sắp xếp: order by LogLUDTimeUTC desc — Master.Loyalty.cs:21493, :21618
        var allList = normalList.Concat(discountList)
                                .OrderByDescending(x => x.LogLUDTimeUTC)
                                .ToList();

        // 8. Xử lý nghiệp vụ tại Gate: nếu DealPointType == "POINTINCREASE", AmountChTotal = PointChRankTotal — gate :503-509
        foreach (var item in allList)
        {
            if (string.Equals(item.DealPointType, LoyaltyConst.DealPointType.PointIncrease, StringComparison.OrdinalIgnoreCase))
            {
                item.AmountChTotal = item.PointChRankTotal;
            }
        }

        return allList;
    }

    /// <summary>
    /// Port 1:1 action SearchRptPECrdTransationDeCreaseInstPeriodDtl — tìm kiếm chi tiết giảm điểm kỳ (#290).
    /// Nguồn tra cứu (chỉ đọc):
    ///   - Gate:     13.ClientGate/V10/idn.Loyalty.WebAPI/Controllers/Rpt_PeriodSummaryController.cs:532-608 (SearchRptPECrdTransationDeCreaseInstPeriodDtl)
    ///   - Filter:   Rpt_PeriodSummaryController.cs:702-736 (strWhereClause_RptPE_CrdTransationDeCreaseInstPeriodDtl)
    ///   - Service:  13.ClientGate/V10/idn.Loyalty.ClientService/Services/Loyalty/Rpt_PeriodSummaryService.cs:46-50 (WA_RptPE_CrdTransationDeCreaseInstPeriodDtl_Get)
    ///   - API:      11.BackEnd/V10/idn.Loyalty.Biz.Web/Controllers/ReportController.cs:795-862 (WA_RptPE_CrdTransationDeCreaseInstPeriodDtl_Get)
    ///   - Biz LIVE: 11.BackEnd/V10/idn.Loyalty.Biz/Report.cs:10941-11050 (WAS_RptPE_CrdTransationDeCreaseInstPeriodDtl_Get),
    ///               :11051-11190 (RptPE_CrdTransationDeCreaseInstPeriodDtl_Get),
    ///               :11191-11394 (RptPE_CrdTransationDeCreaseInstPeriodDtl_GetX)
    ///
    /// ĐẶC ĐIỂM NGHIỆP VỤ:
    ///   - Gate nhận: DLCPCode, PeriodMonth, CreateDTimeUTCFrom, CreateDTimeUTCTo (:532).
    ///   - strWhereClause_RptPE_CrdTransationDeCreaseInstPeriodDtl (:702-736) lọc:
    ///       + DLCPCode: toán tử '=' trên cột DLCodeTranDC (:707-710).
    ///       + PeriodMonth: toán tử '=' trên cột PeriodMonth (:729-733).
    ///       + CreateDTimeUTCFrom: toán tử '>=' trên cột CreateDateUTCDC sau quy đổi StdDTimeBeginDay -> StdDTimeUTC -> StdDate (:712-719).
    ///       + CreateDTimeUTCTo: toán tử '<=' trên cột CreateDateUTCDC sau quy đổi StdDTimeEndDay -> StdDTimeUTC -> StdDate (:721-728).
    ///       + KHÔNG lọc KeyWord, FlagActive, OrgID.
    ///   - Inner join với CrdCardTransaction cct trên rpctipd.RefNoDC == cct.AutoId.ToString() (lấy CardTypeUseDC, CardTypeInitDC, RefNo, CreateDTimeUTC_DC).
    ///   - Inner join với CrdCardTransaction cct_ic trên rpctipd.RefNoIC == cct_ic.AutoId.ToString() (lấy CreateDTimeUTC_IC).
    ///   - Inner join với CrdMember cm trên rpctipd.MemberNo == cm.MemberNo (lấy MemberName).
    ///   - Sắp xếp: order by rpctipd.LogLUDTimeUTC desc (:11252, :11338).
    ///   - Cột DLCodeTranIC: nếu req.DLCPCode == "HTV" hoặc req.DLCPCode == item.DLCodeTranIC thì giữ nguyên, ngược lại hiển thị "ĐL khác" (:11309-11314).
    ///   - Format ngày CreateDateUTCIC và CreateDateUTCDC: chuỗi yyyy-MM-dd sau quy đổi giờ VN (+7h) từ CreateDTimeUTC tương ứng (:11012-11020).
    ///   - Phản hồi: khi không có bản ghi nào, Lst_RptPE_CrdTransationDeCreaseInstPeriodDtl trả về null (gate :562, :585, :593).
    /// </summary>
    [HttpPost("searchrptpecrdtransationdecreaseinstperioddtl")]
    public async Task<IActionResult> SearchRptPECrdTransationDeCreaseInstPeriodDtl([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Rpt_PeriodSummarySearchRptPECrdTransationDeCreaseInstPeriodDtlReq? req)
    {
        var query = _db.Set<RptPE_CrdTransationDeCreaseInstPeriodDtl>().AsNoTracking().AsQueryable();

        // 1. Lọc DLCPCode trên cột DLCodeTranDC với toán tử '=' (nguyên chuỗi, không phân biệt hoa thường) — gate :707-710
        if (!string.IsNullOrWhiteSpace(req?.DLCPCode))
        {
            var p_dlCode = PortQuery.Equal(req.DLCPCode);
            query = query.Where(x => x.DLCodeTranDC != null && EF.Functions.Like(x.DLCodeTranDC.ToLower(), p_dlCode, PortQuery.Esc));
        }

        // 2. Lọc PeriodMonth trên cột PeriodMonth với toán tử '=' (nguyên chuỗi, không phân biệt hoa thường) — gate :729-733
        if (!string.IsNullOrWhiteSpace(req?.PeriodMonth))
        {
            var p_month = PortQuery.Equal(req.PeriodMonth);
            query = query.Where(x => EF.Functions.Like(x.PeriodMonth.ToLower(), p_month, PortQuery.Esc));
        }

        // 3. Lọc CreateDTimeUTCFrom: StdDTimeBeginDay -> StdDTimeUTC -> StdDate, toán tử '>=' trên CreateDateUTCDC — gate :712-719
        if (!string.IsNullOrWhiteSpace(req?.CreateDTimeUTCFrom) && DateTime.TryParse(req.CreateDTimeUTCFrom, out var dFrom))
        {
            var strFrom = dFrom.Date.AddHours(-7).ToString("yyyy-MM-dd");   // StdDTimeBeginDay -> StdDTimeUTC (-7h) -> StdDate (gate :714-716)
            query = query.Where(x => x.CreateDateUTCDC != null && string.Compare(x.CreateDateUTCDC, strFrom) >= 0);
        }

        // 4. Lọc CreateDTimeUTCTo: StdDTimeEndDay -> StdDTimeUTC -> StdDate, toán tử '<=' trên CreateDateUTCDC — gate :721-728
        if (!string.IsNullOrWhiteSpace(req?.CreateDTimeUTCTo) && DateTime.TryParse(req.CreateDTimeUTCTo, out var dTo))
        {
            var strTo = dTo.Date.AddDays(1).AddSeconds(-1).AddHours(-7).ToString("yyyy-MM-dd");   // StdDTimeEndDay -> StdDTimeUTC (-7h) -> StdDate (gate :723-725)
            query = query.Where(x => x.CreateDateUTCDC != null && string.Compare(x.CreateDateUTCDC, strTo) <= 0);
        }

        // 5. Inner join với CrdCardTransaction (cct, cct_ic) và CrdMember (cm), sắp xếp LogLUDTimeUTC desc — Report.cs:11327-11339
        var joinedQuery = from rpctipd in query
                          join cct in _db.Set<CrdCardTransaction>().AsNoTracking()
                              on rpctipd.RefNoDC equals cct.AutoId.ToString()
                          join cct_ic in _db.Set<CrdCardTransaction>().AsNoTracking()
                              on rpctipd.RefNoIC equals cct_ic.AutoId.ToString()
                          join cm in _db.Set<CrdMember>().AsNoTracking()
                              on rpctipd.MemberNo equals cm.MemberNo
                          orderby rpctipd.LogLUDTimeUTC descending
                          select new
                          {
                              Item = rpctipd,
                              CardTypeUseDC = cct.CardTypeUse,
                              CardTypeInitDC = cct.CardTypeInit,
                              RefNo = cct.RefNo,
                              MemberName = cm.MemberName,
                              CreateDTimeUTC_DC = cct.CreateDTimeUTC,
                              CreateDTimeUTC_IC = cct_ic.CreateDTimeUTC
                          };

        var rawList = await joinedQuery.ToListAsync();

        // 6. Xử lý quyền xem đại lý cho cột DLCodeTranIC và format ngày CreateDateUTCIC / CreateDateUTCDC — Report.cs:11007-11023, :11308-11314
        var list = rawList.Select(x =>
        {
            var item = x.Item;
            item.CardTypeUseDC = x.CardTypeUseDC;
            item.CardTypeInitDC = x.CardTypeInitDC;
            item.RefNo = x.RefNo;
            item.MemberName = x.MemberName;

            // Format ngày yyyy-MM-dd sau quy đổi giờ VN (+7h)
            item.CreateDateUTCIC = x.CreateDTimeUTC_IC.HasValue ? x.CreateDTimeUTC_IC.Value.AddHours(7).ToString("yyyy-MM-dd") : null;
            item.CreateDateUTCDC = x.CreateDTimeUTC_DC.HasValue ? x.CreateDTimeUTC_DC.Value.AddHours(7).ToString("yyyy-MM-dd") : null;

            // Xử lý DLCodeTranIC theo quyền xem đại lý (so với DLCPCode)
            item.DLCodeTranIC = (req?.DLCPCode == "HTV" || string.Equals(req?.DLCPCode, item.DLCodeTranIC, StringComparison.OrdinalIgnoreCase))
                ? item.DLCodeTranIC
                : "ĐL khác";

            return item;
        }).ToList();

        // 7. Định dạng trả về: Lst_RptPE_CrdTransationDeCreaseInstPeriodDtl là null khi rỗng (gate :562, :585, :593)
        return Ok(new
        {
            Success = true,
            Data = new
            {
                Lst_RptPE_CrdTransationDeCreaseInstPeriodDtl = list.Count > 0 ? (object)list : null
            }
        });
    }

    /// <summary>
    /// Từ điển 10 cột xuất Excel báo cáo đối soát tổng hợp kỳ Rpt_PeriodSummary.
    /// Nguồn: Rpt_PeriodSummaryController.cs:1568-1587 (GetImportDicColumsRpt).
    /// </summary>
    private static Dictionary<string, string> GetImportDicColumsRpt()
    {
        return new Dictionary<string, string>
        {
            { nameof(Rpt_PeriodSummary.DLCPCode), "Đại lý" },
            { nameof(Rpt_PeriodSummary.ACol), "Số điểm HV tích tại  ĐL trong kỳ" }, // Nguồn có 2 khoảng trắng giữa "tại" và "ĐL"
            { nameof(Rpt_PeriodSummary.BCol), "Số điểm HV sử dụng tại ĐL trong kỳ" },
            { nameof(Rpt_PeriodSummary.CCol), "Số điểm ĐL chuyển vào quỹ HV vì đặc cách HV" },
            { nameof(Rpt_PeriodSummary.DCol), "Số điểm ĐL được nhận từ quỹ HV theo HV đặc cách của ĐL khác" },
            { nameof(Rpt_PeriodSummary.ECol), "Điểm ĐL chuyển vào quỹ chung tích từ HTV" },
            { nameof(Rpt_PeriodSummary.FCol), "Tổng số điểm ĐL chuyển vào quỹ chung" },
            { nameof(Rpt_PeriodSummary.HCol), "Tổng số điểm hết hiệu lực của tháng đối soát" },
            { nameof(Rpt_PeriodSummary.JCol), "Tổng số điểm hết hiệu lực lũy kế năm tính đến tháng đối soát" },
            { nameof(Rpt_PeriodSummary.KCol), "Điểm tiêu dùng của hội viên hủy trong tháng đối soát" }
        };
    }

    /// <summary>
    /// Port 1:1 action ExportExcelRpt — xuất Excel báo cáo đối soát tổng hợp kỳ Rpt_PeriodSummary (#291).
    /// Nguồn tra cứu (chỉ đọc):
    ///   - Gate:     13.ClientGate/V10/idn.Loyalty.WebAPI/Controllers/Rpt_PeriodSummaryController.cs:794-879 (ExportExcelRpt)
    ///   - Filter:   Rpt_PeriodSummaryController.cs:613-630 (strWhereClause_Rpt_PeriodSummary)
    ///   - Service:  13.ClientGate/V10/idn.Loyalty.ClientService/Services/Loyalty/Rpt_PeriodSummaryService.cs:21-33 (WA_Rpt_PeriodSummary_Get)
    ///   - API:      11.BackEnd/V10/idn.Loyalty.Biz.Web/Controllers/ReportController.cs:516-565 (WA_Rpt_PeriodSummary_Get)
    ///   - Biz LIVE: 11.BackEnd/V10/idn.Loyalty.Biz/Report.cs:4340-4464 (WAS_Rpt_PeriodSummary_Get),
    ///               :4465-4613 (Rpt_PeriodSummary_Get), :4614-4941 (Rpt_PeriodSummary_GetX)
    ///   - Columns:  Rpt_PeriodSummaryController.cs:1568-1587 (GetImportDicColumsRpt)
    ///
    /// ĐẶC ĐIỂM NGHIỆP VỤ:
    ///   - Gate ExportExcelRpt nhận: DLCPCode, PeriodMonth (:794).
    ///   - strWhereClause_Rpt_PeriodSummary (:613-630) lọc:
    ///       + DLCode (map từ DLCPCode tại gate :818): toán tử '=' (nguyên chuỗi, không phân biệt hoa thường) trên cột DLCPCode.
    ///       + PeriodMonth (gate :819): toán tử '=' (nguyên chuỗi, không phân biệt hoa thường) trên cột PeriodMonth.
    ///       + KHÔNG lọc KeyWord, FlagActive, OrgID.
    ///   - Sắp xếp: order by rps.LogLUDTimeUTC desc (Report.cs:4695, :4765).
    ///   - Tính toán nghiệp vụ cột HCol: HCol = (rps.HCol ?? 0) + (rps.LCol ?? 0) (Report.cs:4754).
    ///   - Guard khi danh sách rỗng: ném ServiceException("Không có dữ liệu") -> trả HTTP 400 ErridnLoyalty.Rpt_PeriodSummary_ExportExcelRpt (gate :865, :868-872).
    ///   - Xuất file: GenExcelExportFilePath("Rpt_PeriodSummary") và ExportToExcelFromList với 10 cột từ GetImportDicColumsRpt() (:852-854).
    ///   - Trả về: Success = true, Url = baseAddress + virtualPath (:856-860).
    /// </summary>
    [HttpPost("exportexcelrpt")]
    public async Task<IActionResult> ExportExcelRpt([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Rpt_PeriodSummarySearchReq? req)
    {
        var query = _db.Set<Rpt_PeriodSummary>().AsNoTracking().AsQueryable();

        // 1. Lọc DLCPCode (hoặc DLCode) với toán tử '=' (nguyên chuỗi, không phân biệt hoa thường) — gate :618-621, :818
        string? dlCode = req?.DLCPCode;
        if (!string.IsNullOrWhiteSpace(dlCode))
        {
            var p_dlCode = PortQuery.Equal(dlCode);
            query = query.Where(x => EF.Functions.Like(x.DLCPCode.ToLower(), p_dlCode, PortQuery.Esc));
        }

        // 2. Lọc PeriodMonth với toán tử '=' (nguyên chuỗi, không phân biệt hoa thường) — gate :623-627, :819
        if (!string.IsNullOrWhiteSpace(req?.PeriodMonth))
        {
            var p_month = PortQuery.Equal(req.PeriodMonth);
            query = query.Where(x => EF.Functions.Like(x.PeriodMonth.ToLower(), p_month, PortQuery.Esc));
        }

        // 3. Sắp xếp: order by rps.LogLUDTimeUTC desc — Report.cs:4695, :4765
        var list = await query.OrderByDescending(x => x.LogLUDTimeUTC).ToListAsync();

        // 4. Kiểm tra danh sách rỗng — gate :845, :863-866
        // Khi không có dữ liệu: throw ServiceException("Không có dữ liệu") -> gate catch :868-872
        // Ở port trả HTTP 400: PortResult.Fail("Rpt_PeriodSummary_ExportExcelRpt", "Message", "Không có dữ liệu")
        if (list == null || list.Count == 0)
        {
            return PortResult.Fail("Rpt_PeriodSummary_ExportExcelRpt", "Message", "Không có dữ liệu");
        }

        // 5. Tính toán nghiệp vụ lại cho cột HCol: (rps.HCol + rps.LCol) — Report.cs:4754
        // "Tổng số điểm hết hạn của tháng đối soát = Điểm hết hạn của tháng + Điểm hủy của tháng"
        foreach (var item in list)
        {
            item.HCol = (item.HCol ?? 0) + (item.LCol ?? 0);
        }

        // 6. Chuẩn bị đường dẫn thư mục xuất file Excel — gate :851-852
        var webRoot = _env?.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
        {
            webRoot = _env != null
                ? Path.Combine(_env.ContentRootPath, "wwwroot")
                : Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        }

        string virtualPath;
        try
        {
            // 7. Lấy từ điển 10 cột từ GetImportDicColumsRpt(), sinh file và xuất Excel — gate :849, :852, :854
            var dicColNames = GetImportDicColumsRpt();
            var (physicalPath, vPath) = PortExcel.GenExcelExportFilePath(webRoot, "Rpt_PeriodSummary");
            virtualPath = vPath;
            PortExcel.ExportToExcelFromList(list, dicColNames, physicalPath, "Rpt_PeriodSummary");
        }
        catch (Exception ex)
        {
            // Bắt ngoại lệ nếu ghi file lỗi -> trả Fail (gate :868-872)
            return PortResult.Fail("Rpt_PeriodSummary_ExportExcelRpt", "Exception", ex.Message);
        }

        // 8. Trả URL tải file với Success = true — gate :856-860
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
    /// Port 1:1 action ExportExcelRptDtl — xuất Excel báo cáo đối soát chi tiết 3 sheet (#292).
    /// Nguồn tra cứu (chỉ đọc):
    ///   - Gate:     13.ClientGate/V10/idn.Loyalty.WebAPI/Controllers/Rpt_PeriodSummaryController.cs:881-1041 (ExportExcelRptDtl)
    ///   - Filter:   Rpt_PeriodSummaryController.cs:631-648 (strWhereClause_Rpt_PeriodSummaryDtl),
    ///               :667-700 (strWhereClause_Rpt_PeriodSummary_Transaction)
    ///   - Service:  13.ClientGate/V10/idn.Loyalty.ClientService/Services/Loyalty/Rpt_PeriodSummaryService.cs:35-45
    ///   - API:      11.BackEnd/V10/idn.Loyalty.Biz.Web/Controllers/ReportController.cs:585-645, :725-789
    ///   - Biz LIVE: 11.BackEnd/V10/idn.Loyalty.Biz/Report.cs:4945-5372 (WAS_Rpt_PeriodSummaryDtl_Get -> Rpt_PeriodSummaryDtl_GetX),
    ///               :5785-6275 (WAS_Rpt_PeriodSummary_Transaction_Get -> Rpt_PeriodSummary_Transaction_GetX)
    ///
    /// ĐẶC ĐIỂM NGHIỆP VỤ:
    ///   - Gate ExportExcelRptDtl nhận: DLCPCode, PeriodMonth, DLCodeExceptionally (:881).
    ///   - Sheet 1: Rpt_PeriodSummaryDtl (:903-938)
    ///       + Lọc DLCPCode (toán tử '=' trên cột DLCPCode) và PeriodMonth (toán tử '=' trên cột PeriodMonth) (:631-648).
    ///       + Sắp xếp order by rpsd.LogLUDTimeUTC desc (:5254).
    ///       + Tính lại HCol = (rpsd.HCol ?? 0) + (rpsd.LCol ?? 0) (:5302).
    ///       + 13 cột từ điển theo GetImportDicColumsRptDtl() (:1590-1614).
    ///   - Sheet 2: Rpt_PeriodSummary_Transaction (:940-975)
    ///       + Lọc DLCode = DLCPCode (toán tử '=' trên cột DLCode) và PeriodMonth (toán tử '=' trên cột PeriodMonth) (:667-700).
    ///       + Join Crd_CardTransaction lấy CreateDTimeUTC và CardNo; sắp xếp order by rpst.LogLUDTimeUTC desc (:6167).
    ///       + 14 cột từ điển theo GetImportDicColumsTransaction() (:1518-1537).
    ///   - Sheet 3: Rpt_PeriodSummary_TransactionExceptionally (:977-1011)
    ///       + Lọc draft theo DLCodeExceptionally (=) và PeriodMonth (=) (:667-700).
    ///       + Inner join rpste trên (PeriodMonth, DLCodeExceptionally, AutoID); left join Crd_CardTransaction cct trên AutoID (:6206-6210).
    ///       + Điều kiện lọc trên rpste: rpste.DLCode != rpste.DLCodeExceptionally và rpste.DealPointType == "CONSUMPTION" (:6212-6213).
    ///       + Sắp xếp order by rpste.LogLUDTimeUTC desc (:6215).
    ///       + Che 3 cột: AutoID = "N/A", RefNo = "N/A", DLCode = "N/A" (:6184-6189).
    ///       + 14 cột từ điển theo GetImportDicColumsTransaction() (:1518-1537).
    ///   - Xuất 1 file Excel 3 sheet với prefix "Rpt_PeriodSummaryDtl", không ném lỗi khi rỗng (:1018-1028).
    /// </summary>
    [HttpPost("exportexcelrptdtl")]
    public async Task<IActionResult> ExportExcelRptDtl([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Rpt_PeriodSummaryExportExcelRptDtlReq? req)
    {
        // 1. Sheet 1 — Rpt_PeriodSummaryDtl (gate :903-938, biz :4945-5372)
        var q1 = _db.Set<Rpt_PeriodSummaryDtl>().AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(req?.DLCPCode))
        {
            var p_dl = PortQuery.Equal(req.DLCPCode);
            q1 = q1.Where(x => EF.Functions.Like(x.DLCPCode.ToLower(), p_dl, PortQuery.Esc));
        }
        if (!string.IsNullOrWhiteSpace(req?.PeriodMonth))
        {
            var p_m = PortQuery.Equal(req.PeriodMonth);
            q1 = q1.Where(x => EF.Functions.Like(x.PeriodMonth.ToLower(), p_m, PortQuery.Esc));
        }
        var list1 = await q1.OrderByDescending(x => x.LogLUDTimeUTC).ToListAsync();
        foreach (var item in list1)
        {
            item.HCol = (item.HCol ?? 0) + (item.LCol ?? 0);
        }

        // 2. Sheet 2 — Rpt_PeriodSummary_Transaction (gate :940-975, biz :5785-6275)
        var q2 = _db.Set<Rpt_PeriodSummary_Transaction>().AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(req?.DLCPCode))
        {
            var p_dl = PortQuery.Equal(req.DLCPCode);
            q2 = q2.Where(x => x.DLCode != null && EF.Functions.Like(x.DLCode.ToLower(), p_dl, PortQuery.Esc));
        }
        if (!string.IsNullOrWhiteSpace(req?.PeriodMonth))
        {
            var p_m = PortQuery.Equal(req.PeriodMonth);
            q2 = q2.Where(x => EF.Functions.Like(x.PeriodMonth.ToLower(), p_m, PortQuery.Esc));
        }
        var joinedQuery2 = from rpst in q2
                           join cct in _db.Set<CrdCardTransaction>().AsNoTracking()
                               on rpst.AutoID equals cct.AutoId
                           orderby rpst.LogLUDTimeUTC descending
                           select new
                           {
                               Item = rpst,
                               CardNo = cct.CardNo,
                               CreateDTimeUtc = cct.CreateDTimeUTC
                           };
        var rawList2 = await joinedQuery2.ToListAsync();
        var list2 = rawList2.Select(x =>
        {
            var item = x.Item;
            item.CardNo = x.CardNo;
            if (x.CreateDTimeUtc.HasValue)
            {
                item.CreateDTimeUTC = x.CreateDTimeUtc.Value.ToString("yyyy-MM-dd HH:mm:ss");
            }
            return item;
        }).ToList();

        // 3. Sheet 3 — Rpt_PeriodSummary_TransactionExceptionally (gate :977-1011, biz :6180-6217)
        var draftQuery = _db.Set<Rpt_PeriodSummary_Transaction>().AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(req?.PeriodMonth))
        {
            var p_m = PortQuery.Equal(req.PeriodMonth);
            draftQuery = draftQuery.Where(x => EF.Functions.Like(x.PeriodMonth.ToLower(), p_m, PortQuery.Esc));
        }
        if (!string.IsNullOrWhiteSpace(req?.DLCodeExceptionally))
        {
            var p_dlExc = PortQuery.Equal(req.DLCodeExceptionally);
            draftQuery = draftQuery.Where(x => x.DLCodeExceptionally != null && EF.Functions.Like(x.DLCodeExceptionally.ToLower(), p_dlExc, PortQuery.Esc));
        }
        var joinedQuery3 = from t in draftQuery
                           join rpste in _db.Set<Rpt_PeriodSummary_TransactionExceptionally>().AsNoTracking()
                               on new { t.PeriodMonth, t.DLCodeExceptionally, t.AutoID }
                               equals new { rpste.PeriodMonth, rpste.DLCodeExceptionally, rpste.AutoID }
                           join cct in _db.Set<CrdCardTransaction>().AsNoTracking()
                               on t.AutoID equals cct.AutoId into cctGroup
                           from cct in cctGroup.DefaultIfEmpty()
                           where rpste.DLCode != rpste.DLCodeExceptionally
                              && rpste.DealPointType == "CONSUMPTION"
                           orderby rpste.LogLUDTimeUTC descending
                           select new
                           {
                               Item = rpste,
                               CardNo = cct != null ? cct.CardNo : null,
                               CreateDTimeUtc = cct != null ? cct.CreateDTimeUTC : null
                           };
        var rawList3 = await joinedQuery3.ToListAsync();
        var list3 = rawList3.Select(x => new
        {
            AutoID = "N/A",
            DLCode = "N/A",
            RefNo = "N/A",
            MemberNo = x.Item.MemberNo,
            MemberName = x.Item.MemberName,
            CardTypeUse = x.Item.CardTypeUse,
            CardTypeInit = x.Item.CardTypeInit,
            DealPointType = x.Item.DealPointType,
            DLCodeExceptionally = x.Item.DLCodeExceptionally,
            AmountChTotal = x.Item.AmountChTotal,
            AmountChTotalInit = x.Item.AmountChTotalInit,
            AmountChTotalDiffer = x.Item.AmountChTotalDiffer,
            PointExpiryDTime = x.Item.PointExpiryDTime,
            CreateDTimeUTC = x.CreateDTimeUtc.HasValue ? x.CreateDTimeUtc.Value.ToString("yyyy-MM-dd HH:mm:ss") : null
        }).ToList();

        // 4. Chuẩn bị đường dẫn thư mục xuất file Excel (gate :1017-1019)
        var webRoot = _env?.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
        {
            webRoot = _env != null
                ? Path.Combine(_env.ContentRootPath, "wwwroot")
                : Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        }

        var (physicalPath, virtualPath) = PortExcel.GenExcelExportFilePath(webRoot, "Rpt_PeriodSummaryDtl");

        // 5. Ghi 1 workbook duy nhất gồm 3 sheet (gate :1020-1022)
        try
        {
            using var wb = new XLWorkbook();
            AddWorksheetFromList(wb, list1, GetImportDicColumsRptDtl(), "Rpt_PeriodSummaryDtl");
            AddWorksheetFromList(wb, list2, GetImportDicColumsTransaction(), "Rpt_PeriodSummary_Transaction");
            AddWorksheetFromList(wb, list3, GetImportDicColumsTransaction(), "Rpt_PeriodSummary_TransactionExceptionally");
            wb.SaveAs(physicalPath);
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Rpt_PeriodSummary_ExportExcelRptDtl", "Exception", ex.Message);
        }

        // 6. Trả URL tải file với Success = true (gate :1024-1028)
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
    /// Port 1:1 action ExportExcelRptDtl_New20230317 — xuất Excel báo cáo đối soát chi tiết 4 sheet (#293).
    /// Nguồn tra cứu (chỉ đọc):
    ///   - Gate:     13.ClientGate/V10/idn.Loyalty.WebAPI/Controllers/Rpt_PeriodSummaryController.cs:1042-1243 (ExportExcelRptDtl_New20230317)
    ///   - Filter:   Rpt_PeriodSummaryController.cs:649-666 (strWhereClause_Rpt_PeriodSummaryDtl_01),
    ///               :667-700 (strWhereClause_Rpt_PeriodSummary_Transaction),
    ///               :702-736 (strWhereClause_RptPE_CrdTransationDeCreaseInstPeriodDtl)
    ///   - Service:  13.ClientGate/V10/idn.Loyalty.ClientService/Services/Loyalty/Rpt_PeriodSummaryService.cs:21-27, :41-45, :46-50
    ///   - API:      11.BackEnd/V10/idn.Loyalty.Biz.Web/Controllers/ReportController.cs:656-724, :725-789, :795-862
    ///   - Biz LIVE: 11.BackEnd/V10/idn.Loyalty.Biz/Report.cs:5376-5781 (WAS_Rpt_PeriodSummaryDtl_01_Get -> Rpt_PeriodSummaryDtl_01_GetX),
    ///               :5785-6275 (WAS_Rpt_PeriodSummary_Transaction_Get -> Rpt_PeriodSummary_Transaction_GetX),
    ///               :10941-11394 (WAS_RptPE_CrdTransationDeCreaseInstPeriodDtl_Get -> RptPE_CrdTransationDeCreaseInstPeriodDtl_GetX)
    ///   - Columns:  Rpt_PeriodSummaryController.cs:1617-1649 (GetImportDicColumsRptDtl01 - 24 cột),
    ///               :1518-1537 (GetImportDicColumsTransaction - 14 cột),
    ///               :1652-1680 (GetImportDicColumsCrdTransationDeCreaseInstPeriodDtl - 22 cột)
    ///
    /// ĐẶC ĐIỂM NGHIỆP VỤ:
    ///   - Gate ExportExcelRptDtl_New20230317 nhận 3 tham số: DLCPCode, PeriodMonth, DLCodeExceptionally (:1042).
    ///   - Sheet 1: Rpt_PeriodSummaryDtl_01 (:1064-1100)
    ///       + Lọc DLCPCode (toán tử '=' trên cột DLCPCode) và PeriodMonth (toán tử '=' trên cột PeriodMonth) (:649-666).
    ///       + Sắp xếp order by rpsd.LogLUDTimeUTC desc (:5685, :5726).
    ///       + Cột HCol: KHÔNG tính lại (giữ nguyên DB theo Report.cs:5721-5726).
    ///       + 24 cột từ điển theo GetImportDicColumsRptDtl01() (:1617-1649).
    ///   - Sheet 2: Rpt_PeriodSummary_Transaction (:1102-1137)
    ///       + Lọc DLCode = DLCPCode (toán tử '=' trên cột DLCode) và PeriodMonth (toán tử '=' trên cột PeriodMonth) (:667-700).
    ///       + Join Crd_CardTransaction lấy CreateDTimeUTC và CardNo; sắp xếp order by rpst.LogLUDTimeUTC desc (:6167).
    ///       + 14 cột từ điển theo GetImportDicColumsTransaction() (:1518-1537).
    ///   - Sheet 3: Rpt_PeriodSummary_TransactionExceptionally (:1139-1173)
    ///       + Lọc draft theo DLCodeExceptionally (=) và PeriodMonth (=) (:667-700).
    ///       + Inner join rpste trên (PeriodMonth, DLCodeExceptionally, AutoID); left join Crd_CardTransaction cct trên AutoID (:6206-6210).
    ///       + Điều kiện lọc trên rpste: rpste.DLCode != rpste.DLCodeExceptionally và rpste.DealPointType == "CONSUMPTION" (:6212-6213).
    ///       + Sắp xếp order by rpste.LogLUDTimeUTC desc (:6215).
    ///       + Che 3 cột: AutoID = "N/A", RefNo = "N/A", DLCode = "N/A" (:6184-6189).
    ///       + 14 cột từ điển theo GetImportDicColumsTransaction() (:1518-1537).
    ///   - Sheet 4: RptPE_CrdTransationDeCreaseInstPeriodDtl (:1175-1211)
    ///       + Lọc DLCodeTranDC = DLCPCode (toán tử '=' trên cột DLCodeTranDC) và PeriodMonth (toán tử '=' trên cột PeriodMonth) (:702-736).
    ///       + Inner join cct trên RefNoDC == cct.AutoId.ToString(), cct_ic trên RefNoIC == cct_ic.AutoId.ToString(), cm trên MemberNo (:11327-11339).
    ///       + Sắp xếp order by rpctipd.LogLUDTimeUTC desc (:11252, :11338).
    ///       + Quyền xem đại lý DLCodeTranIC: nếu DLCPCode == "HTV" hoặc DLCPCode == item.DLCodeTranIC thì giữ nguyên, còn lại hiển thị "ĐL khác" (:11309-11314).
    ///       + Định dạng ngày CreateDateUTCIC và CreateDateUTCDC: chuỗi yyyy-MM-dd sau quy đổi giờ VN (+7h) từ CreateDTimeUTC tương ứng (:11012-11020).
    ///       + 22 cột từ điển theo GetImportDicColumsCrdTransationDeCreaseInstPeriodDtl() (:1652-1680).
    ///   - Xuất 1 file Excel 4 sheet với prefix "Rpt_PeriodSummaryDtl", khi dữ liệu rỗng KHÔNG ném lỗi, vẫn tạo file 4 sheet có header (:1219-1230).
    /// </summary>
    [HttpPost("exportexcelrptdtl_new20230317")]
    public async Task<IActionResult> ExportExcelRptDtl_New20230317([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Rpt_PeriodSummaryExportExcelRptDtlReq? req)
    {
        // 1. Sheet 1 — Rpt_PeriodSummaryDtl_01 (gate :1064-1100, biz :5376-5781)
        var q1 = _db.Set<Rpt_PeriodSummaryDtl_01>().AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(req?.DLCPCode))
        {
            var p_dl = PortQuery.Equal(req.DLCPCode);
            q1 = q1.Where(x => EF.Functions.Like(x.DLCPCode.ToLower(), p_dl, PortQuery.Esc));
        }
        if (!string.IsNullOrWhiteSpace(req?.PeriodMonth))
        {
            var p_m = PortQuery.Equal(req.PeriodMonth);
            q1 = q1.Where(x => EF.Functions.Like(x.PeriodMonth.ToLower(), p_m, PortQuery.Esc));
        }
        var list1 = await q1.OrderByDescending(x => x.LogLUDTimeUTC).ToListAsync();

        // 2. Sheet 2 — Rpt_PeriodSummary_Transaction (gate :1102-1137, biz :5785-6275)
        var q2 = _db.Set<Rpt_PeriodSummary_Transaction>().AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(req?.DLCPCode))
        {
            var p_dl = PortQuery.Equal(req.DLCPCode);
            q2 = q2.Where(x => x.DLCode != null && EF.Functions.Like(x.DLCode.ToLower(), p_dl, PortQuery.Esc));
        }
        if (!string.IsNullOrWhiteSpace(req?.PeriodMonth))
        {
            var p_m = PortQuery.Equal(req.PeriodMonth);
            q2 = q2.Where(x => EF.Functions.Like(x.PeriodMonth.ToLower(), p_m, PortQuery.Esc));
        }
        var joinedQuery2 = from rpst in q2
                           join cct in _db.Set<CrdCardTransaction>().AsNoTracking()
                               on rpst.AutoID equals cct.AutoId
                           orderby rpst.LogLUDTimeUTC descending
                           select new
                           {
                               Item = rpst,
                               CardNo = cct.CardNo,
                               CreateDTimeUtc = cct.CreateDTimeUTC
                           };
        var rawList2 = await joinedQuery2.ToListAsync();
        var list2 = rawList2.Select(x =>
        {
            var item = x.Item;
            item.CardNo = x.CardNo;
            if (x.CreateDTimeUtc.HasValue)
            {
                item.CreateDTimeUTC = x.CreateDTimeUtc.Value.ToString("yyyy-MM-dd HH:mm:ss");
            }
            return item;
        }).ToList();

        // 3. Sheet 3 — Rpt_PeriodSummary_TransactionExceptionally (gate :1139-1173, biz :6180-6217)
        var draftQuery = _db.Set<Rpt_PeriodSummary_Transaction>().AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(req?.PeriodMonth))
        {
            var p_m = PortQuery.Equal(req.PeriodMonth);
            draftQuery = draftQuery.Where(x => EF.Functions.Like(x.PeriodMonth.ToLower(), p_m, PortQuery.Esc));
        }
        if (!string.IsNullOrWhiteSpace(req?.DLCodeExceptionally))
        {
            var p_dlExc = PortQuery.Equal(req.DLCodeExceptionally);
            draftQuery = draftQuery.Where(x => x.DLCodeExceptionally != null && EF.Functions.Like(x.DLCodeExceptionally.ToLower(), p_dlExc, PortQuery.Esc));
        }
        var joinedQuery3 = from t in draftQuery
                           join rpste in _db.Set<Rpt_PeriodSummary_TransactionExceptionally>().AsNoTracking()
                               on new { t.PeriodMonth, t.DLCodeExceptionally, t.AutoID }
                               equals new { rpste.PeriodMonth, rpste.DLCodeExceptionally, rpste.AutoID }
                           join cct in _db.Set<CrdCardTransaction>().AsNoTracking()
                               on t.AutoID equals cct.AutoId into cctGroup
                           from cct in cctGroup.DefaultIfEmpty()
                           where rpste.DLCode != rpste.DLCodeExceptionally
                              && rpste.DealPointType == "CONSUMPTION"
                           orderby rpste.LogLUDTimeUTC descending
                           select new
                           {
                               Item = rpste,
                               CardNo = cct != null ? cct.CardNo : null,
                               CreateDTimeUtc = cct != null ? cct.CreateDTimeUTC : null
                           };
        var rawList3 = await joinedQuery3.ToListAsync();
        var list3 = rawList3.Select(x => new
        {
            AutoID = "N/A",
            DLCode = "N/A",
            RefNo = "N/A",
            MemberNo = x.Item.MemberNo,
            MemberName = x.Item.MemberName,
            CardTypeUse = x.Item.CardTypeUse,
            CardTypeInit = x.Item.CardTypeInit,
            DealPointType = x.Item.DealPointType,
            DLCodeExceptionally = x.Item.DLCodeExceptionally,
            AmountChTotal = x.Item.AmountChTotal,
            AmountChTotalInit = x.Item.AmountChTotalInit,
            AmountChTotalDiffer = x.Item.AmountChTotalDiffer,
            PointExpiryDTime = x.Item.PointExpiryDTime,
            CreateDTimeUTC = x.CreateDTimeUtc.HasValue ? x.CreateDTimeUtc.Value.ToString("yyyy-MM-dd HH:mm:ss") : null
        }).ToList();

        // 4. Sheet 4 — RptPE_CrdTransationDeCreaseInstPeriodDtl (gate :1175-1211, biz :10941-11394)
        var q4 = _db.Set<RptPE_CrdTransationDeCreaseInstPeriodDtl>().AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(req?.DLCPCode))
        {
            var p_dlCode = PortQuery.Equal(req.DLCPCode);
            q4 = q4.Where(x => x.DLCodeTranDC != null && EF.Functions.Like(x.DLCodeTranDC.ToLower(), p_dlCode, PortQuery.Esc));
        }
        if (!string.IsNullOrWhiteSpace(req?.PeriodMonth))
        {
            var p_month = PortQuery.Equal(req.PeriodMonth);
            q4 = q4.Where(x => EF.Functions.Like(x.PeriodMonth.ToLower(), p_month, PortQuery.Esc));
        }
        var joinedQuery4 = from rpctipd in q4
                           join cct in _db.Set<CrdCardTransaction>().AsNoTracking()
                               on rpctipd.RefNoDC equals cct.AutoId.ToString()
                           join cct_ic in _db.Set<CrdCardTransaction>().AsNoTracking()
                               on rpctipd.RefNoIC equals cct_ic.AutoId.ToString()
                           join cm in _db.Set<CrdMember>().AsNoTracking()
                               on rpctipd.MemberNo equals cm.MemberNo
                           orderby rpctipd.LogLUDTimeUTC descending
                           select new
                           {
                               Item = rpctipd,
                               CardTypeUseDC = cct.CardTypeUse,
                               CardTypeInitDC = cct.CardTypeInit,
                               RefNo = cct.RefNo,
                               MemberName = cm.MemberName,
                               CreateDTimeUTC_DC = cct.CreateDTimeUTC,
                               CreateDTimeUTC_IC = cct_ic.CreateDTimeUTC
                           };
        var rawList4 = await joinedQuery4.ToListAsync();
        var list4 = rawList4.Select(x =>
        {
            var item = x.Item;
            item.CardTypeUseDC = x.CardTypeUseDC;
            item.CardTypeInitDC = x.CardTypeInitDC;
            item.RefNo = x.RefNo;
            item.MemberName = x.MemberName;

            // Format ngày yyyy-MM-dd sau quy đổi giờ VN (+7h) — Report.cs:11012-11020
            item.CreateDateUTCIC = x.CreateDTimeUTC_IC.HasValue ? x.CreateDTimeUTC_IC.Value.AddHours(7).ToString("yyyy-MM-dd") : null;
            item.CreateDateUTCDC = x.CreateDTimeUTC_DC.HasValue ? x.CreateDTimeUTC_DC.Value.AddHours(7).ToString("yyyy-MM-dd") : null;

            // Xử lý DLCodeTranIC theo quyền xem đại lý (so với DLCPCode) — Report.cs:11309-11314
            item.DLCodeTranIC = (req?.DLCPCode == "HTV" || string.Equals(req?.DLCPCode, item.DLCodeTranIC, StringComparison.OrdinalIgnoreCase))
                ? item.DLCodeTranIC
                : "ĐL khác";

            return item;
        }).ToList();

        // 5. Chuẩn bị đường dẫn thư mục xuất file Excel (gate :1218-1220)
        var webRoot = _env?.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
        {
            webRoot = _env != null
                ? Path.Combine(_env.ContentRootPath, "wwwroot")
                : Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        }

        var (physicalPath, virtualPath) = PortExcel.GenExcelExportFilePath(webRoot, "Rpt_PeriodSummaryDtl");

        // 6. Ghi 1 workbook duy nhất gồm 4 sheet (gate :1221-1224)
        try
        {
            using var wb = new XLWorkbook();
            AddWorksheetFromList(wb, list1, GetImportDicColumsRptDtl01(), "Rpt_PeriodSummaryDtl_01");
            AddWorksheetFromList(wb, list2, GetImportDicColumsTransaction(), "Rpt_PeriodSummary_Transaction");
            AddWorksheetFromList(wb, list3, GetImportDicColumsTransaction(), "Rpt_PeriodSummary_TransactionExceptionally");
            AddWorksheetFromList(wb, list4, GetImportDicColumsCrdTransationDeCreaseInstPeriodDtl(), "RptPE_CrdTransationDeCreaseInstPeriodDtl");
            wb.SaveAs(physicalPath);
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Rpt_PeriodSummary_ExportExcelRptDtl_New20230317", "Exception", ex.Message);
        }

        // 7. Trả URL tải file với Success = true (gate :1226-1230)
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
    /// Port 1:1 action ExportExcelTransaction — xuất Excel danh sách giao dịch đối soát tổng hợp kỳ Rpt_PeriodSummary_Transaction (#294).
    /// Nguồn tra cứu (chỉ đọc):
    ///   - Gate:     13.ClientGate/V10/idn.Loyalty.WebAPI/Controllers/Rpt_PeriodSummaryController.cs:1244-1330 (ExportExcelTransaction)
    ///   - Filter:   Rpt_PeriodSummaryController.cs:667-700 (strWhereClause_Rpt_PeriodSummary_Transaction)
    ///               + DLCode (gán từ DLCPCode tại :1268): toán tử '=' nguyên chuỗi (:674)
    ///               + PeriodMonth: toán tử '=' nguyên chuỗi (:690)
    ///   - Service:  13.ClientGate/V10/idn.Loyalty.ClientService/Services/Loyalty/Rpt_PeriodSummaryService.cs:41-45 (WA_Rpt_PeriodSummary_Transaction_Get)
    ///   - API:      11.BackEnd/V10/idn.Loyalty.Biz.Web/Controllers/ReportController.cs:725-789 (WA_Rpt_PeriodSummary_Transaction_Get)
    ///   - Biz LIVE: 11.BackEnd/V10/idn.Loyalty.Biz/Report.cs:5785-5909 (WAS_Rpt_PeriodSummary_Transaction_Get),
    ///               :5910-6052 (Rpt_PeriodSummary_Transaction_Get),
    ///               :6053-6275 (Rpt_PeriodSummary_Transaction_GetX)
    ///               Inner join Crd_CardTransaction cct on t.AutoID = cct.AutoID (:6154-6169)
    ///               Sắp xếp: order by rpst.LogLUDTimeUTC desc (:6118, :6169)
    ///   - Columns:  Rpt_PeriodSummaryController.cs:1518-1537 (GetImportDicColumsTransaction - 14 cột)
    ///   - Sheet:    "Rpt_PeriodSummary_Transaction" (:1303, :1305)
    ///   - Khi rỗng: throw new ServiceException("Không có dữ liệu") (:1316) -> HTTP 400 PortResult.Fail("Rpt_PeriodSummary_ExportExcelTransaction", "Message", "Không có dữ liệu")
    ///   - Khi thành công: Success = true, Data = { Url = ... } (:1307-1311)
    /// </summary>
    [HttpPost("exportexceltransaction")]
    public async Task<IActionResult> ExportExcelTransaction([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Rpt_PeriodSummarySearchReq? req)
    {
        var query = _db.Set<Rpt_PeriodSummary_Transaction>().AsNoTracking().AsQueryable();

        // 1. Lọc DLCode (từ DLCPCode) với toán tử '=' (nguyên chuỗi, không phân biệt hoa thường) — gate :672-675, :1268
        string? dlCode = req?.DLCPCode;
        if (!string.IsNullOrWhiteSpace(dlCode))
        {
            var p_dlCode = PortQuery.Equal(dlCode);
            query = query.Where(x => x.DLCode != null && EF.Functions.Like(x.DLCode.ToLower(), p_dlCode, PortQuery.Esc));
        }

        // 2. Lọc PeriodMonth với toán tử '=' (nguyên chuỗi, không phân biệt hoa thường) — gate :687-691, :1269
        if (!string.IsNullOrWhiteSpace(req?.PeriodMonth))
        {
            var p_month = PortQuery.Equal(req.PeriodMonth);
            query = query.Where(x => EF.Functions.Like(x.PeriodMonth.ToLower(), p_month, PortQuery.Esc));
        }

        // 3. Inner join với CrdCardTransaction trên AutoID, sắp xếp LogLUDTimeUTC desc — Report.cs:6154-6169
        var joinedQuery = from rpst in query
                          join cct in _db.Set<CrdCardTransaction>().AsNoTracking()
                              on rpst.AutoID equals cct.AutoId
                          orderby rpst.LogLUDTimeUTC descending
                          select new
                          {
                              Item = rpst,
                              CardNo = cct.CardNo,
                              CreateDTimeUtc = cct.CreateDTimeUTC
                          };

        var rawList = await joinedQuery.ToListAsync();

        // 4. Format CreateDTimeUTC quy đổi định dạng ngày — Report.cs:5854-5861
        var list = rawList.Select(x =>
        {
            var item = x.Item;
            item.CardNo = x.CardNo;
            if (x.CreateDTimeUtc.HasValue)
            {
                item.CreateDTimeUTC = x.CreateDTimeUtc.Value.ToString("yyyy-MM-dd HH:mm:ss");
            }
            return item;
        }).ToList();

        // 5. Kiểm tra danh sách rỗng — gate :1316, :1322
        // Khi không có dữ liệu: throw ServiceException("Không có dữ liệu") -> gate catch :1322
        // Trên port trả HTTP 400: PortResult.Fail("Rpt_PeriodSummary_ExportExcelTransaction", "Message", "Không có dữ liệu")
        if (list == null || list.Count == 0)
        {
            return PortResult.Fail("Rpt_PeriodSummary_ExportExcelTransaction", "Message", "Không có dữ liệu");
        }

        // 6. Chuẩn bị đường dẫn thư mục xuất file Excel — gate :1303
        var webRoot = _env?.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
        {
            webRoot = _env != null
                ? Path.Combine(_env.ContentRootPath, "wwwroot")
                : Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        }

        string virtualPath;
        try
        {
            // 7. Lấy từ điển 14 cột từ GetImportDicColumsTransaction(), sinh file và xuất Excel — gate :1300, :1303, :1305
            var dicColNames = GetImportDicColumsTransaction();
            var (physicalPath, vPath) = PortExcel.GenExcelExportFilePath(webRoot, "Rpt_PeriodSummary_Transaction");
            virtualPath = vPath;
            PortExcel.ExportToExcelFromList(list, dicColNames, physicalPath, "Rpt_PeriodSummary_Transaction");
        }
        catch (Exception ex)
        {
            // Bắt ngoại lệ nếu ghi file lỗi -> trả Fail (gate :1322)
            return PortResult.Fail("Rpt_PeriodSummary_ExportExcelTransaction", "Exception", ex.Message);
        }

        // 8. Trả URL tải file với Success = true — gate :1307-1311
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
    /// Port 1:1 action ExportExcelTransactionExceptionally — xuất Excel giao dịch đặc cách đối soát tổng hợp kỳ Rpt_PeriodSummary_TransactionExceptionally (#296).
    /// Nguồn tra cứu (chỉ đọc):
    ///   - Gate:     13.ClientGate/V10/idn.Loyalty.WebAPI/Controllers/Rpt_PeriodSummaryController.cs:1429-1514 (ExportExcelTransactionExceptionally)
    ///   - Filter:   Rpt_PeriodSummaryController.cs:667-700 (strWhereClause_Rpt_PeriodSummary_Transaction)
    ///               + PeriodMonth: toán tử '=' nguyên chuỗi (:690)
    ///               + DLCodeExceptionally: toán tử '=' nguyên chuỗi (:696)
    ///   - Service:  13.ClientGate/V10/idn.Loyalty.ClientService/Services/Loyalty/Rpt_PeriodSummaryService.cs:41-45 (WA_Rpt_PeriodSummary_Transaction_Get)
    ///   - API:      11.BackEnd/V10/idn.Loyalty.Biz.Web/Controllers/ReportController.cs:725-789 (WA_Rpt_PeriodSummary_Transaction_Get)
    ///   - Biz LIVE: 11.BackEnd/V10/idn.Loyalty.Biz/Report.cs:5785-5909 (WAS_Rpt_PeriodSummary_Transaction_Get),
    ///               :5910-6052 (Rpt_PeriodSummary_Transaction_Get),
    ///               :6053-6275 (Rpt_PeriodSummary_Transaction_GetX)
    ///               + Lọc bảng draft Rpt_PeriodSummary_Transaction rpst theo PeriodMonth và DLCodeExceptionally (:6103-6145)
    ///               + Inner join Rpt_PeriodSummary_TransactionExceptionally rpste trên (PeriodMonth, DLCodeExceptionally, AutoID) (:6206-6208)
    ///               + Left join Crd_CardTransaction cct trên AutoID để lấy CreateDTimeUTC và CardNo (:6209-6210)
    ///               + Điều kiện lọc rpste: rpste.DLCode != rpste.DLCodeExceptionally (:6212) và rpste.DealPointType == "CONSUMPTION" (:6213)
    ///               + Sắp xếp: order by rpste.LogLUDTimeUTC desc (:6215)
    ///               + Che 3 cột thành 'N/A': AutoID = 'N/A' (:6184), RefNo = 'N/A' (:6185), DLCode = 'N/A' (:6189)
    ///   - Columns:  Rpt_PeriodSummaryController.cs:1518-1537 (GetImportDicColumsTransaction - 14 cột)
    ///   - Sheet:    "Rpt_PeriodSummary_TransactionExceptionally" (:1487, :1489)
    ///   - Khi rỗng: throw new ServiceException("Không có dữ liệu") (:1500) -> HTTP 400 PortResult.Fail("Rpt_PeriodSummary_ExportExcelTransactionExceptionally", "Message", "Không có dữ liệu")
    ///   - Khi thành công: Success = true, Data = { Url = ... } (:1491-1495)
    /// </summary>
    [HttpPost("exportexceltransactionexceptionally")]
    public async Task<IActionResult> ExportExcelTransactionExceptionally([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Rpt_PeriodSummarySearchTransactionExceptionallyReq? req)
    {
        var draftQuery = _db.Set<Rpt_PeriodSummary_Transaction>().AsNoTracking().AsQueryable();

        // 1. Lọc bảng draft Rpt_PeriodSummary_Transaction theo PeriodMonth và DLCodeExceptionally bằng PortQuery.Equal (toán tử '=', không phân biệt hoa thường)
        if (!string.IsNullOrWhiteSpace(req?.PeriodMonth))
        {
            var p_month = PortQuery.Equal(req.PeriodMonth);
            draftQuery = draftQuery.Where(x => EF.Functions.Like(x.PeriodMonth.ToLower(), p_month, PortQuery.Esc));
        }

        if (!string.IsNullOrWhiteSpace(req?.DLCodeExceptionally))
        {
            var p_dlExc = PortQuery.Equal(req.DLCodeExceptionally);
            draftQuery = draftQuery.Where(x => x.DLCodeExceptionally != null && EF.Functions.Like(x.DLCodeExceptionally.ToLower(), p_dlExc, PortQuery.Esc));
        }

        // 2. Inner join với Rpt_PeriodSummary_TransactionExceptionally trên (PeriodMonth, DLCodeExceptionally, AutoID)
        //    Left join với CrdCardTransaction trên AutoID để lấy CreateDTimeUTC
        //    Lọc trên rpste: rpste.DLCode != rpste.DLCodeExceptionally và rpste.DealPointType == "CONSUMPTION"
        //    Sắp xếp: orderby rpste.LogLUDTimeUTC descending
        var joinedQuery = from t in draftQuery
                          join rpste in _db.Set<Rpt_PeriodSummary_TransactionExceptionally>().AsNoTracking()
                              on new { t.PeriodMonth, t.DLCodeExceptionally, t.AutoID }
                              equals new { rpste.PeriodMonth, rpste.DLCodeExceptionally, rpste.AutoID }
                          join cct in _db.Set<CrdCardTransaction>().AsNoTracking()
                              on t.AutoID equals cct.AutoId into cctGroup
                          from cct in cctGroup.DefaultIfEmpty()
                          where rpste.DLCode != rpste.DLCodeExceptionally
                             && rpste.DealPointType == "CONSUMPTION"
                          orderby rpste.LogLUDTimeUTC descending
                          select new
                          {
                              Item = rpste,
                              CardNo = cct != null ? cct.CardNo : null,
                              CreateDTimeUtc = cct != null ? cct.CreateDTimeUTC : null
                          };

        var rawList = await joinedQuery.ToListAsync();

        // 3. Chiếu DTO: gán 3 cột AutoID = "N/A", RefNo = "N/A", DLCode = "N/A" theo đúng câu SQL nguồn (:6184, :6185, :6189). Cột CreateDTimeUTC định dạng yyyy-MM-dd HH:mm:ss
        var list = rawList.Select(x => new
        {
            AutoID = "N/A",
            DLCode = "N/A",
            RefNo = "N/A",
            MemberNo = x.Item.MemberNo,
            MemberName = x.Item.MemberName,
            CardTypeUse = x.Item.CardTypeUse,
            CardTypeInit = x.Item.CardTypeInit,
            DealPointType = x.Item.DealPointType,
            DLCodeExceptionally = x.Item.DLCodeExceptionally,
            AmountChTotal = x.Item.AmountChTotal,
            AmountChTotalInit = x.Item.AmountChTotalInit,
            AmountChTotalDiffer = x.Item.AmountChTotalDiffer,
            PointExpiryDTime = x.Item.PointExpiryDTime,
            CreateDTimeUTC = x.CreateDTimeUtc.HasValue ? x.CreateDTimeUtc.Value.ToString("yyyy-MM-dd HH:mm:ss") : null
        }).ToList();

        // 4. Kiểm tra danh sách rỗng — gate :1500, :1506
        // Khi không có dữ liệu: throw ServiceException("Không có dữ liệu") -> gate catch :1506
        // Trên port trả HTTP 400: PortResult.Fail("Rpt_PeriodSummary_ExportExcelTransactionExceptionally", "Message", "Không có dữ liệu")
        if (list == null || list.Count == 0)
        {
            return PortResult.Fail("Rpt_PeriodSummary_ExportExcelTransactionExceptionally", "Message", "Không có dữ liệu");
        }

        // 5. Chuẩn bị đường dẫn thư mục xuất file Excel — gate :1487
        var webRoot = _env?.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
        {
            webRoot = _env != null
                ? Path.Combine(_env.ContentRootPath, "wwwroot")
                : Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        }

        string virtualPath;
        try
        {
            // 6. Lấy từ điển 14 cột từ GetImportDicColumsTransaction(), sinh file và xuất Excel — gate :1484, :1487, :1489
            var dicColNames = GetImportDicColumsTransaction();
            var (physicalPath, vPath) = PortExcel.GenExcelExportFilePath(webRoot, "Rpt_PeriodSummary_TransactionExceptionally");
            virtualPath = vPath;
            using var wb = new XLWorkbook();
            AddWorksheetFromList(wb, list, dicColNames, "Rpt_PeriodSummary_TransactionExceptionally");
            wb.SaveAs(physicalPath);
        }
        catch (Exception ex)
        {
            // Bắt ngoại lệ nếu ghi file lỗi -> trả Fail (gate :1506)
            return PortResult.Fail("Rpt_PeriodSummary_ExportExcelTransactionExceptionally", "Exception", ex.Message);
        }

        // 7. Trả URL tải file với Success = true — gate :1491-1495
        return Ok(new
        {
            Success = true,
            Data = new
            {
                Url = PortExcel.GetRequestBaseAddress(Request) + virtualPath
            }
        });
    }

    private static void AddWorksheetFromList<T>(XLWorkbook wb, IEnumerable<T> rows, IReadOnlyDictionary<string, string> dicColNames, string sheetName)
    {
        var safeName = string.IsNullOrWhiteSpace(sheetName) ? "Sheet1" : (sheetName.Length > 31 ? sheetName[..31] : sheetName);
        var ws = wb.Worksheets.Add(safeName);
        var cols = dicColNames.ToList();
        for (var c = 0; c < cols.Count; c++)
        {
            ws.Cell(1, c + 1).Value = cols[c].Value;
        }

        var r = 2;
        foreach (var row in rows)
        {
            if (row is null) continue;
            var t = row.GetType();
            for (var c = 0; c < cols.Count; c++)
            {
                var p = t.GetProperty(cols[c].Key, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                var v = p?.GetValue(row);
                ws.Cell(r, c + 1).Value = v switch
                {
                    null => Blank.Value,
                    DateTime d => d,
                    double d => d,
                    int i => i,
                    long l => l,
                    decimal m => m,
                    _ => Convert.ToString(v) ?? ""
                };
            }
            r++;
        }
        ws.Columns().AdjustToContents();
    }

    private static Dictionary<string, string> GetImportDicColumsRptDtl()
    {
        return new Dictionary<string, string>
        {
            { nameof(Rpt_PeriodSummaryDtl.PeriodMonthDay), "Ngày giao dịch" },
            { nameof(Rpt_PeriodSummaryDtl.DLCPCode), "Mã đại lý" },
            { nameof(Rpt_PeriodSummaryDtl.ACol), "Số điểm HV tích tại  ĐL trong kỳ" },
            { nameof(Rpt_PeriodSummaryDtl.BCol), "Số điểm HV sử dụng tại ĐL trong kỳ" },
            { nameof(Rpt_PeriodSummaryDtl.CCol), "Số điểm ĐL chuyển vào quỹ HV vì đặc cách HV" },
            { nameof(Rpt_PeriodSummaryDtl.DCol), "Số điểm ĐL được nhận từ quỹ HV theo HV đặc cách của ĐL khác" },
            { nameof(Rpt_PeriodSummaryDtl.PCol), "Tổng số điểm ĐL chuyển vào quỹ HV" },
            { nameof(Rpt_PeriodSummaryDtl.QCol), "Tổng số điểm quỹ HV chuyển cho ĐL" },
            { nameof(Rpt_PeriodSummaryDtl.HCol), "Tổng số điểm hết\u00a0hiệu lực\u00a0của tháng đối soát" },
            { nameof(Rpt_PeriodSummaryDtl.JCol), "Tổng số điểm hết\u00a0hiệu lực\u00a0lũy kế năm tính đến tháng đối soát" },
            { nameof(Rpt_PeriodSummaryDtl.KCol), "Điểm tiêu dùng của hội viên hủy trong\u00a0tháng đối soát" },
            { nameof(Rpt_PeriodSummaryDtl.K1Col), "Số điểm ĐL chuyển vào quỹ hội viên theo Dịch vụ ưu đãi do Bán hạng HV từ ĐL khác" },
            { nameof(Rpt_PeriodSummaryDtl.K2Col), "Số điểm ĐL nhận được từ quỹ hội viên theo Dịch vụ ưu đãi do Bán hạng HV từ ĐL khác" }
        };
    }

    private static Dictionary<string, string> GetImportDicColumsRptDtl01()
    {
        return new Dictionary<string, string>
        {
            { nameof(Rpt_PeriodSummaryDtl_01.PeriodMonthDay), "Ngày giao dịch" },
            { nameof(Rpt_PeriodSummaryDtl_01.DLCPCode), "Mã đại lý" },
            { nameof(Rpt_PeriodSummaryDtl_01.ACol), "Điểm tích HV do phát sinh làm dịch vụ tại ĐL trong kỳ" },
            { nameof(Rpt_PeriodSummaryDtl_01.BCol), "Điểm tích từ các chương trình ngoài Dịch vụ tại ĐL (Sinh nhật, giới thiệu…)" },
            { nameof(Rpt_PeriodSummaryDtl_01.DCol), "Tổng điểm tích phát sinh trong kỳ" },
            { nameof(Rpt_PeriodSummaryDtl_01.ECol), "Điểm tiêu có nguồn tích từ làm Dịch vụ tại ĐL" },
            { nameof(Rpt_PeriodSummaryDtl_01.FCol), "Điểm tiêu có nguồn tích từ các chương trình ngoài Dịch vụ tại ĐL (Sinh nhật, giới thiệu…)" },
            { nameof(Rpt_PeriodSummaryDtl_01.MCol), "Điểm tiêu có nguồn tích từ ĐL khác" },
            { nameof(Rpt_PeriodSummaryDtl_01.M1Col), "Điểm tiêu có nguồn tích từ HTV (KMDV)" },
            { nameof(Rpt_PeriodSummaryDtl_01.M2Col), "Điểm tiêu có nguồn tích từ HTV (KMBH)" },
            { nameof(Rpt_PeriodSummaryDtl_01.M3Col), "Điểm tiêu có nguồn tích từ HTV (KMMT)" },
            { nameof(Rpt_PeriodSummaryDtl_01.GCol), "Tổng điểm tiêu của Đại lý(Nguồn tích không gồm HTV)" },
            { nameof(Rpt_PeriodSummaryDtl_01.HCol), "Số điểm ĐL chuyển vào quỹ hội viên vì đặc cách hội viên" },
            { nameof(Rpt_PeriodSummaryDtl_01.H1Col), "Số điểm ĐL chuyển vào quỹ hội viên vì Bán hạng hội viên" },
            { nameof(Rpt_PeriodSummaryDtl_01.K1Col), "Số điểm ĐL chuyển vào  quỹ hội viên theo Dịch vụ ưu đãi do Bán hạng HV từ ĐL khác" },
            { nameof(Rpt_PeriodSummaryDtl_01.ICol), "Số điểm ĐL nhận được từ quỹ hội viên theo đặc cách của ĐL khác" },
            { nameof(Rpt_PeriodSummaryDtl_01.I1Col), "Số điểm ĐL nhận được từ quỹ hội viên theo Bán hạng hội viên của ĐL khác" },
            { nameof(Rpt_PeriodSummaryDtl_01.K2Col), "Số điểm ĐL nhận được từ quỹ hội viên theo Dịch vụ ưu đãi do Bán hạng HV từ ĐL khác" },
            { nameof(Rpt_PeriodSummaryDtl_01.QCol), "Tổng số điểm ĐL chuyển vào Quỹ Hội viên (Theo ngày)" },
            { nameof(Rpt_PeriodSummaryDtl_01.RCol), "Tổng số điểm Quỹ Hội viên chuyển cho ĐL (Theo ngày)" },
            { nameof(Rpt_PeriodSummaryDtl_01.SCol), "Kết quả đối soát theo kỳ (Ngày/Tháng)" },
            { nameof(Rpt_PeriodSummaryDtl_01.TCol), "Tổng số điểm hết\u00a0hiệu lực\u00a0của tháng đối soát" },
            { nameof(Rpt_PeriodSummaryDtl_01.UCol), "Tổng điểm hết hạn do hủy thẻ tại tháng đối soát" },
            { nameof(Rpt_PeriodSummaryDtl_01.WCol), "Tổng số điểm hết\u00a0hiệu lực\u00a0lũy kế năm tính đến tháng đối soát" }
        };
    }

    private static Dictionary<string, string> GetImportDicColumsTransaction()
    {
        return new Dictionary<string, string>
        {
            { nameof(Rpt_PeriodSummary_Transaction.AutoID), "Số giao dịch" },
            { nameof(Rpt_PeriodSummary_Transaction.DLCode), "Mã Đại lý" },
            { nameof(Rpt_PeriodSummary_Transaction.RefNo), "Số tham chiếu" },
            { nameof(Rpt_PeriodSummary_Transaction.MemberNo), "Mã hội viên" },
            { nameof(Rpt_PeriodSummary_Transaction.MemberName), "Họ và tên" },
            { nameof(Rpt_PeriodSummary_Transaction.CardTypeUse), "Hạng sử dụng" },
            { nameof(Rpt_PeriodSummary_Transaction.CardTypeInit), "Hạng thực" },
            { nameof(Rpt_PeriodSummary_Transaction.DealPointType), "Loại điểm" },
            { nameof(Rpt_PeriodSummary_Transaction.DLCodeExceptionally), "Đại lý yêu cầu đặc cách" },
            { nameof(Rpt_PeriodSummary_Transaction.AmountChTotal), "Số điểm hạng sử dụng" },
            { nameof(Rpt_PeriodSummary_Transaction.AmountChTotalInit), "Số điểm hạng thực" },
            { nameof(Rpt_PeriodSummary_Transaction.AmountChTotalDiffer), "Số điểm chênh hạng" },
            { nameof(Rpt_PeriodSummary_Transaction.PointExpiryDTime), "Ngày hết hạn sử dụng điểm" },
            { nameof(Rpt_PeriodSummary_Transaction.CreateDTimeUTC), "Ngày giao dịch" }
        };
    }

    private static Dictionary<string, string> GetImportDicColumsCrdTransationDeCreaseInstPeriodDtl()
    {
        return new Dictionary<string, string>
        {
            { nameof(RptPE_CrdTransationDeCreaseInstPeriodDtl.RefNoDC), "Mã giao dịch tiêu điểm" },
            { nameof(RptPE_CrdTransationDeCreaseInstPeriodDtl.DLCodeTranDC), "Đại lý giao dịch tiêu điểm" },
            { nameof(RptPE_CrdTransationDeCreaseInstPeriodDtl.RptPEPDCInstNo), "Số tham chiếu" },
            { nameof(RptPE_CrdTransationDeCreaseInstPeriodDtl.MemberNo), "Mã hội viên" },
            { nameof(RptPE_CrdTransationDeCreaseInstPeriodDtl.MemberName), "Tên hội viên" },
            { nameof(RptPE_CrdTransationDeCreaseInstPeriodDtl.CardTypeUseDC), "Hạng sử dụng tiêu điểm" },
            { nameof(RptPE_CrdTransationDeCreaseInstPeriodDtl.CardTypeInitDC), "Hạng thực tiêu điểm" },
            { nameof(RptPE_CrdTransationDeCreaseInstPeriodDtl.DLCodeExceptionallyDC), "Đại lý yêu cầu đặc cách tiêu điểm" },
            { nameof(RptPE_CrdTransationDeCreaseInstPeriodDtl.CreateDateUTCDC), "Ngày giao dịch tiêu điểm" },
            { nameof(RptPE_CrdTransationDeCreaseInstPeriodDtl.DealPointTypeDC), "Loại tiêu điểm" },
            { nameof(RptPE_CrdTransationDeCreaseInstPeriodDtl.PointInitDC), "Điểm tiêu hạng sử dụng" },
            { nameof(RptPE_CrdTransationDeCreaseInstPeriodDtl.PointDeCreaseDealer), "Số điểm tiêu từ đại lý giao dịch TÍCH" },
            { nameof(RptPE_CrdTransationDeCreaseInstPeriodDtl.PointDeCreaseHTV), "Số điểm tiêu từ Đại lý đặc cách" },
            { nameof(RptPE_CrdTransationDeCreaseInstPeriodDtl.RefNoIC), "Mã giao dịch tích điểm" },
            { nameof(RptPE_CrdTransationDeCreaseInstPeriodDtl.DLCodeTranIC), "Đại lý giao dịch tích điểm" },
            { nameof(RptPE_CrdTransationDeCreaseInstPeriodDtl.DLCodeExceptionallyIC), "Đại lý đặc cách GD tích điểm" },
            { nameof(RptPE_CrdTransationDeCreaseInstPeriodDtl.CardTypeUseIC), "Hạng sử dụng tích điểm" },
            { nameof(RptPE_CrdTransationDeCreaseInstPeriodDtl.CardTypeInitIC), "Hạng thực tích điểm" },
            { nameof(RptPE_CrdTransationDeCreaseInstPeriodDtl.CreateDateUTCIC), "Ngày giao dịch tích điểm" },
            { nameof(RptPE_CrdTransationDeCreaseInstPeriodDtl.DealPointTypeIC), "Loại tích điểm" },
            { nameof(RptPE_CrdTransationDeCreaseInstPeriodDtl.AmountChTotalIC), "Điểm tích hạng sử dụng" },
            { nameof(RptPE_CrdTransationDeCreaseInstPeriodDtl.AmountChTotalInitIC), "Điểm tích hạng thực" }
        };
    }

    /// <summary>
    /// Từ điển 19 cột xuất Excel chi tiết giao dịch thẻ đối soát tổng hợp kỳ: gate Rpt_PeriodSummaryController.cs:1541-1565 (GetImportDicColumsTransactionDtl).
    /// </summary>
    private static Dictionary<string, string> GetImportDicColumsTransactionDtl()
    {
        return new Dictionary<string, string>
        {
            { nameof(Crd_CardTransactionSearchResult.AutoId), "Số giao dịch" },
            { nameof(Crd_CardTransactionSearchResult.DLCode), "Mã đại lý" },
            { nameof(Crd_CardTransactionSearchResult.RefNo), "Số tham chiếu" },
            { nameof(Crd_CardTransactionSearchResult.MemberNo), "Mã hội viên" },
            { nameof(Crd_CardTransactionSearchResult.MemberName), "Họ và tên" },
            { nameof(Crd_CardTransactionSearchResult.CardTypeUse), "Hạng sử dụng" },
            { nameof(Crd_CardTransactionSearchResult.CardTypeInit), "Hạng thực" },
            { nameof(Crd_CardTransactionSearchResult.DealPointType), "Loại điểm" },
            { nameof(Crd_CardTransactionSearchResult.DLCodeExceptionally), "Đại lý yêu cầu đặc cách" },
            { nameof(Crd_CardTransactionSearchResult.AmountChTotal), "Số điểm hạng sử dụng" },
            { nameof(Crd_CardTransactionSearchResult.AmountChTotalInit), "Số điểm hạng thực" },
            { nameof(Crd_CardTransactionSearchResult.AmountChTotalDiffer), "Số điểm chênh hạng" },
            { nameof(Crd_CardTransactionSearchResult.PointExpiryDTime), "Ngày hết hạn sử dụng điểm" },
            { nameof(Crd_CardTransactionSearchResult.CreateDTimeUTC), "Ngày giao dịch" },
            { nameof(Crd_CardTransactionSearchResult.CardNo), "Mã kỳ xét hạng" },
            { nameof(Crd_CardTransactionSearchResult.CarNo), "Biển số" },
            { nameof(Crd_CardTransactionSearchResult.DiscountRate), "% chiết khấu" },
            { nameof(Crd_CardTransactionSearchResult.DiscountAmount), "Tổng tiền chiết khấu" },
            { nameof(Crd_CardTransactionSearchResult.DiscountRevenue), "Doanh thu chiết khấu" }
        };
    }
}



