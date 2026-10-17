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
using MiniLoyalty.Models.Port;

namespace MiniLoyalty.Controllers.Port;

/// <summary>
/// Port 1:1 Crd_MemberVoucherTransactionController.cs — Search
/// Chuỗi gọi nguồn:
///   Gate: Crd_MemberVoucherTransactionController.cs:31-134 (Search), :396-433 (strWhereClause_Crd_MemberVoucherTransaction)
///   API:  CrdMemberVoucherTransactionController.cs:23-90 (WA_Crd_MemberVoucherTransaction_Get)
///   Biz:  Transaction.cs:4149-4242 (WAS_Crd_MemberVoucherTransaction_Get) -> :4243-4395 (Crd_MemberVoucherTransaction_Get) -> :4396-4614 (Crd_MemberVoucherTransaction_GetX)
///
/// Bỏ qua có chủ đích (loại Đ):
///   - CheckGatewayAuthentication: Crd_MemberVoucherTransactionController.cs:47, CrdMemberVoucherTransactionController.cs:42
///   - CheckAuthorize: Crd_MemberVoucherTransactionController.cs:48
///   - Sys_User_CheckAuthorize: Transaction.cs:4307-4320
///   - Ghi RequestLog/ReturnLog: Transaction.cs:4286-4293, 4385-4392
/// </summary>
[ApiController]
[Route("api/crd_membervouchertransaction")]
public class Crd_MemberVoucherTransactionControllerPort : ControllerBase
{
    private readonly AppDbContext _db;

    public Crd_MemberVoucherTransactionControllerPort(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Tìm kiếm giao dịch voucher của hội viên.
    /// Nguồn: Crd_MemberVoucherTransactionController.cs:31-134 (Search).
    /// </summary>
    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Crd_MemberVoucherTransactionSearchReq? req)
    {
        // 1. Chuẩn hoá chuỗi đầu vào theo StdStr (Crd_MemberVoucherTransactionController.cs:53-56)
        var strMemberNo = string.IsNullOrWhiteSpace(req?.MemberNo) ? null : req.MemberNo.Trim();
        var strCreateDateFrom = string.IsNullOrWhiteSpace(req?.CreateDateFrom) ? null : req.CreateDateFrom.Trim();
        var strCreateDateTo = string.IsNullOrWhiteSpace(req?.CreateDateTo) ? null : req.CreateDateTo.Trim();
        var strDealPointType = string.IsNullOrWhiteSpace(req?.DealPointType) ? null : req.DealPointType.Trim();

        // 2. Guard validation theo đúng thứ tự tầng gate (Crd_MemberVoucherTransactionController.cs:57-72):
        // CreateDateFrom rỗng -> ném ServiceException("invalid_CreateDateFrom", "invalid_CreateDateFrom") (:57-60)
        if (string.IsNullOrEmpty(strCreateDateFrom))
        {
            return PortResult.Fail("invalid_CreateDateFrom", "Check.CreateDateFrom", req?.CreateDateFrom);
        }

        // CreateDateTo bị comment out trong mã nguồn gate (:61-64) -> KHÔNG kiểm tra lỗi nếu rỗng.

        // DealPointType rỗng -> ném ServiceException("invalid_DealPointType", "invalid_DealPointType") (:65-68)
        if (string.IsNullOrEmpty(strDealPointType))
        {
            return PortResult.Fail("invalid_DealPointType", "Check.DealPointType", req?.DealPointType);
        }

        // MemberNo rỗng -> ném ServiceException("invalid_MemberNo", "invalid_MemberNo") (:69-72)
        if (string.IsNullOrEmpty(strMemberNo))
        {
            return PortResult.Fail("invalid_MemberNo", "Check.MemberNo", req?.MemberNo);
        }

        // 3. Xây dựng truy vấn lọc kết hợp inner join Crd_Member và left join Crd_MemberVoucher
        // Nguồn draft: Transaction.cs:4446-4458 (from Crd_MemberVoucherTransaction mvt inner join Crd_Member cm on mvt.MemberNo = cm.MemberNo)
        var baseQuery = from mvt in _db.Set<Crd_MemberVoucherTransaction>()
                        join cm in _db.Set<CrdMember>() on mvt.MemberNo equals cm.MemberNo
                        select mvt;

        // Lọc MemberNo: toán tử '=' (Crd_MemberVoucherTransactionController.cs:402-405)
        baseQuery = baseQuery.Where(x => EF.Functions.Like((x.MemberNo ?? "").ToLower(), PortQuery.Equal(strMemberNo), PortQuery.Esc));

        // Lọc CreateDateFrom: toán tử '>=' (Crd_MemberVoucherTransactionController.cs:406-409)
        if (DateTime.TryParse(strCreateDateFrom, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dFrom)
            || DateTime.TryParse(strCreateDateFrom, out dFrom))
        {
            baseQuery = baseQuery.Where(x => x.CreateDate >= dFrom);
        }

        // Lọc CreateDateTo: toán tử '<=' (Crd_MemberVoucherTransactionController.cs:410-413)
        if (!string.IsNullOrEmpty(strCreateDateTo))
        {
            if (DateTime.TryParse(strCreateDateTo, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dTo)
                || DateTime.TryParse(strCreateDateTo, out dTo))
            {
                baseQuery = baseQuery.Where(x => x.CreateDate <= dTo);
            }
        }

        // Lọc PointChTotalFrom: toán tử '>=' (Crd_MemberVoucherTransactionController.cs:414-417)
        if (!string.IsNullOrWhiteSpace(req?.PointChTotalFrom)
            && double.TryParse(req.PointChTotalFrom.Trim().Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var ptFrom))
        {
            baseQuery = baseQuery.Where(x => x.PointChTotal >= ptFrom);
        }

        // Lọc PointChTotalTo: toán tử '<=' (Crd_MemberVoucherTransactionController.cs:418-421)
        if (!string.IsNullOrWhiteSpace(req?.PointChTotalTo)
            && double.TryParse(req.PointChTotalTo.Trim().Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var ptTo))
        {
            baseQuery = baseQuery.Where(x => x.PointChTotal <= ptTo);
        }

        // Lọc DealPointType: toán tử 'in', tách mảng theo dấu phẩy (Crd_MemberVoucherTransactionController.cs:422-430)
        var dealTypes = strDealPointType.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => s.ToLowerInvariant())
            .ToList();
        baseQuery = baseQuery.Where(x => dealTypes.Contains((x.DealPointType ?? "").ToLower()));

        // Sắp xếp: mvt.LogLUDTimeUTC desc (Transaction.cs:4456-4457)
        var orderedQuery = baseQuery.OrderByDescending(x => x.LogLUDTimeUTC);

        // Left join Crd_MemberVoucher để override PointVCRemain (Transaction.cs:4508-4513, 4543-4545)
        var fullQuery = from mvt in orderedQuery
                        join mvc in _db.Set<Crd_MemberVoucher>()
                            on new { VoucherNo = mvt.RefNo, mvt.MemberNo } equals new { VoucherNo = mvc.VoucherNo, mvc.MemberNo } into mvcGroup
                        from mvc in mvcGroup.DefaultIfEmpty()
                        select new
                        {
                            Transaction = mvt,
                            VoucherRemain = (mvc != null && mvc.PointVCRemain != null) ? mvc.PointVCRemain : null
                        };

        var rows = await fullQuery.AsNoTracking().ToListAsync();

        // Nguồn Crd_MemberVoucherTransactionController.cs:82, :111-121:
        // Nếu không có dữ liệu -> Data = { DataList = null } (không phải [] hay PageInfo)
        if (rows.Count == 0)
        {
            return Ok(new
            {
                Success = true,
                Data = new
                {
                    DataList = (object?)null
                }
            });
        }

        var resultList = new List<Crd_MemberVoucherTransaction>(rows.Count);
        long seq = 0;
        foreach (var row in rows)
        {
            var item = row.Transaction;
            // Override PointVCRemain từ Crd_MemberVoucher nếu có giá trị
            if (row.VoucherRemain.HasValue)
            {
                item.PointVCRemain = row.VoucherRemain.Value;
            }
            item.MyIdxSeq = seq++;
            resultList.Add(item);
        }

        return Ok(new
        {
            Success = true,
            Data = new
            {
                DataList = resultList
            }
        });
    }
}

/// <summary>
/// DTO tham số tìm kiếm giao dịch voucher hội viên.
/// Nguồn tham số gate: Crd_MemberVoucherTransactionController.cs:31-38.
/// </summary>
public class Crd_MemberVoucherTransactionSearchReq
{
    public string? CreateDateFrom { get; set; }                    // Ngày thực hiện từ (:32)
    public string? CreateDateTo { get; set; }                      // Ngày thực hiện đến (:33)
    public string? PointChTotalFrom { get; set; }                  // Điểm giao dịch từ (:34)
    public string? PointChTotalTo { get; set; }                    // Điểm giao dịch đến (:35)
    public string? DealPointType { get; set; }                     // Loại giao dịch (:36)
    public string? MemberNo { get; set; }                          // Mã hội viên (:37)
}
