using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

[ApiController]
[Route("api/rpt_periodsummary")]
public class Rpt_PeriodSummaryControllerPort : ControllerBase
{
    private readonly AppDbContext _db;

    public Rpt_PeriodSummaryControllerPort(AppDbContext db)
    {
        _db = db;
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
}


