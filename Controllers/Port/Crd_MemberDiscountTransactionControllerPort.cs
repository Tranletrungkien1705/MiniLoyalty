using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniLoyalty.Data;
using MiniLoyalty.Models;

// Type alias tái sử dụng entity CrdMemberDiscountTransaction từ Models/PortLoyalty.cs
using Crd_MemberDiscountTransaction = MiniLoyalty.Models.CrdMemberDiscountTransaction;

namespace MiniLoyalty.Controllers.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — Crd_MemberDiscountTransactionController.Search (#055).
//
// Nguồn tra cứu (chỉ đọc):
//   - Gate:      13.ClientGate/V10/idn.Loyalty.WebAPI/Controllers/Crd_MemberDiscountTransactionController.cs:31-138 (Search)
//   - Filter:    Crd_MemberDiscountTransactionController.cs:142-179 (strWhereClause_Crd_MemberDiscountTransaction)
//   - API:       11.BackEnd/V10/idn.Loyalty.Biz.Web/Controllers/CrdMemberDiscountTransactionController.cs:22-89 (WA_Crd_MemberDiscountTransaction_Get)
//   - Biz LIVE:  11.BackEnd/V10/idn.Loyalty.Biz/Transaction.cs:5177-5270 (WAS_Crd_MemberDiscountTransaction_Get)
//                -> :5271-5423 (Crd_MemberDiscountTransaction_Get)
//                -> :5424-5629 (Crd_MemberDiscountTransaction_GetX)
//
// ĐẶC ĐIỂM NGHIỆP VỤ:
//   - Chữ ký action gate: Search(CreateDateFrom, CreateDateTo, PointChTotalFrom, PointChTotalTo, MemberNo).
//   - Chuẩn hoá và guard (:52-63):
//       strMemberNo = TUtils.CUtils.StdStr(MemberNo);
//       strCreateDateFrom = TUtils.CUtils.StdStr(CreateDateFrom);
//       strCreateDateTo = TUtils.CUtils.StdStr(CreateDateTo);
//       strDealPointType = "DISCOUNTRO";
//       Nếu strCreateDateFrom rỗng -> ném ServiceException("invalid_CreateDateFrom", "invalid_CreateDateFrom") (:56-59).
//       Nếu strMemberNo rỗng -> ném ServiceException("invalid_MemberNo", "invalid_MemberNo") (:60-63).
//   - strWhereClause_Crd_MemberDiscountTransaction (:142-179):
//       MemberNo (=), CreateDateFrom (>=), CreateDateTo (<=), PointChTotalFrom (>=), PointChTotalTo (<=).
//       DealPointType: gate hard-code "DISCOUNTRO", lọc toán tử in ('DISCOUNTRO').
//       OrgID và KeyWord: KHÔNG nằm trong strWhereClause.
//   - Truy vấn Biz GetX Draft (:5473-5500):
//       inner join Crd_Member cm on mvt.MemberNo = cm.MemberNo (:5480-5481).
//       order by mvt.LogLUDTimeUTC desc (:5484-5485).
//   - Phân trang: Áp dụng chuẩn Bài học A.12 (chống tràn int, không clamp âm) và A.7 (Ft_PageSize = 0 trả DataList = [], PageCount = 0).
// ===========================================================================================

/// <summary>
/// DTO yêu cầu tìm kiếm giao dịch chiết khấu của hội viên.
/// </summary>
public class Crd_MemberDiscountTransactionSearchReq
{
    public string? MemberNo { get; set; }             // Mã hội viên — gate :36, :52, :60-63, :84
    public string? CreateDateFrom { get; set; }        // Ngày thực hiện từ — gate :32, :53, :56-59, :85
    public string? CreateDateTo { get; set; }          // Ngày thực hiện đến — gate :33, :54, :86
    public string? PointChTotalFrom { get; set; }      // Điểm giao dịch từ — gate :34, :87
    public string? PointChTotalTo { get; set; }        // Điểm giao dịch đến — gate :35, :88
}

[ApiController]
[Route("api/crd_memberdiscounttransaction")]
public class Crd_MemberDiscountTransactionControllerPort : ControllerBase
{
    private readonly AppDbContext _db;

    public Crd_MemberDiscountTransactionControllerPort(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Port 1:1 action Search — tìm kiếm giao dịch chiết khấu dịch vụ Crd_MemberDiscountTransaction.
    /// Nguồn: Crd_MemberDiscountTransactionController.cs:31-138 (gate), Transaction.cs:5177-5629 (biz).
    /// </summary>
    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Crd_MemberDiscountTransactionSearchReq? req)
    {
        // 1. Chuẩn hoá chuỗi đầu vào theo TUtils.CUtils.StdStr (gate :52-54)
        var strMemberNo = string.IsNullOrWhiteSpace(req?.MemberNo) ? null : req.MemberNo.Trim();
        var strCreateDateFrom = string.IsNullOrWhiteSpace(req?.CreateDateFrom) ? null : req.CreateDateFrom.Trim();
        var strCreateDateTo = string.IsNullOrWhiteSpace(req?.CreateDateTo) ? null : req.CreateDateTo.Trim();

        // 2. Guard validation theo đúng thứ tự tầng gate (Crd_MemberDiscountTransactionController.cs:56-63):
        // Kiểm tra CreateDateFrom trước: nếu null hoặc rỗng -> ném ServiceException("invalid_CreateDateFrom", ...) (:56-59)
        if (string.IsNullOrEmpty(strCreateDateFrom))
        {
            return PortResult.Fail("invalid_CreateDateFrom", "Check.CreateDateFrom", req?.CreateDateFrom);
        }

        // Kiểm tra MemberNo sau: nếu null hoặc rỗng -> ném ServiceException("invalid_MemberNo", ...) (:60-63)
        if (string.IsNullOrEmpty(strMemberNo))
        {
            return PortResult.Fail("invalid_MemberNo", "Check.MemberNo", req?.MemberNo);
        }

        // 3. Xây dựng truy vấn lọc kết hợp inner join Crd_Member trên MemberNo (Transaction.cs:5480-5481)
        var query = from mvt in _db.Set<CrdMemberDiscountTransaction>()
                    join cm in _db.Set<CrdMember>() on mvt.MemberNo equals cm.MemberNo
                    select mvt;

        // Lọc DealPointType: gate hard-code "DISCOUNTRO" (:55, :89) và gán in ('DISCOUNTRO') (:168-176)
        query = query.Where(x => EF.Functions.Like((x.DealPointType ?? "").ToLower(), "discountro", PortQuery.Esc));

        // Lọc MemberNo: toán tử '=' không phân biệt hoa/thường (gate :148-151)
        query = query.Where(x => EF.Functions.Like((x.MemberNo ?? "").ToLower(), PortQuery.Equal(strMemberNo), PortQuery.Esc));

        // Lọc CreateDateFrom: toán tử '>=' trên cột CreateDate (gate :152-155)
        if (DateTime.TryParse(strCreateDateFrom, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dFrom)
            || DateTime.TryParse(strCreateDateFrom, out dFrom))
        {
            query = query.Where(x => x.CreateDate >= dFrom);
        }

        // Lọc CreateDateTo: toán tử '<=' trên cột CreateDate (gate :156-159)
        if (!string.IsNullOrEmpty(strCreateDateTo))
        {
            if (DateTime.TryParse(strCreateDateTo, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dTo)
                || DateTime.TryParse(strCreateDateTo, out dTo))
            {
                query = query.Where(x => x.CreateDate <= dTo);
            }
        }

        // Lọc PointChTotalFrom: toán tử '>=' trên cột PointChTotal (gate :160-163)
        if (!string.IsNullOrWhiteSpace(req?.PointChTotalFrom)
            && double.TryParse(req.PointChTotalFrom.Trim().Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var ptFrom))
        {
            query = query.Where(x => x.PointChTotal >= ptFrom);
        }

        // Lọc PointChTotalTo: toán tử '<=' trên cột PointChTotal (gate :164-167)
        if (!string.IsNullOrWhiteSpace(req?.PointChTotalTo)
            && double.TryParse(req.PointChTotalTo.Trim().Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var ptTo))
        {
            query = query.Where(x => x.PointChTotal <= ptTo);
        }

        // Cột OrgID và KeyWord: KHÔNG lọc trong mệnh đề WHERE (khớp gate :142-179 và Biz :5473-5500)

        // 4. Sắp xếp: order by mvt.LogLUDTimeUTC desc (Transaction.cs:5484-5485)
        var ordered = query.OrderByDescending(x => x.LogLUDTimeUTC).ThenByDescending(x => x.AutoId);


        // 5. Gate :74-78,113-125: pageIndex = 0, pageSize = 0 (cố định, không nhận từ client); pageSize == 0 -> PageSizeConfig (10) chỉ để tính pageCount.
        var dataList = await ordered.ToListAsync();
        int itemCount = dataList.Count;
        const int pageSizeConfig = 10;
        int pageCount = (itemCount % pageSizeConfig == 0) ? itemCount / pageSizeConfig : itemCount / pageSizeConfig + 1;

        var pageInfo = new PageInfo<CrdMemberDiscountTransaction>(
            dataList,
            itemCount,
            pageCount,
            0,
            0
        );

        return Ok(new
        {
            Success = true,
            Data = pageInfo
        });
    }
}
