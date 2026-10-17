using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniLoyalty.Data;
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
}
