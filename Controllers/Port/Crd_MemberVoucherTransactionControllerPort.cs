using System;
using System.Collections.Generic;
using System.Globalization;
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

/// <summary>
/// Port 1:1 Crd_MemberVoucherTransactionController.cs — Search (#056), Report (#057), ExportRpt (#058)
/// Chuỗi gọi nguồn:
///   Gate: Crd_MemberVoucherTransactionController.cs:31-134 (Search), :135-258 (Report), :262-368 (ExportRpt), :372-392 (GetImportDicColums), :434-472 (strWhereClause_Crd_MemberVoucherTransaction_Report)
///   API:  CrdMemberVoucherTransactionController.cs:23-90 (WA_Crd_MemberVoucherTransaction_Get), :93-160 (WA_Crd_MemberVoucherTransaction_Rpt)
///   Biz:  Transaction.cs:4149-4242 (WAS_Crd_MemberVoucherTransaction_Get), :4618-4711 (WAS_Crd_MemberVoucherTransaction_Rpt) -> :4712-4865 (Crd_MemberVoucherTransaction_Rpt) -> :4866-5049 (Crd_MemberVoucherTransaction_RptX)
///
/// Bỏ qua có chủ đích (loại Đ):
///   - CheckGatewayAuthentication: Crd_MemberVoucherTransactionController.cs:47, :278, CrdMemberVoucherTransactionController.cs:42, :112
///   - CheckAuthorize: Crd_MemberVoucherTransactionController.cs:48, :279
///   - Sys_User_CheckAuthorize: Transaction.cs:4307-4320, :4776-4789
///   - Ghi RequestLog/ReturnLog: Transaction.cs:4286-4293, 4385-4392, 4755-4762, 4855-4862
/// </summary>
[ApiController]
[Route("api/crd_membervouchertransaction")]
public class Crd_MemberVoucherTransactionControllerPort : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment? _env;

    public Crd_MemberVoucherTransactionControllerPort(AppDbContext db, IWebHostEnvironment? env = null)
    {
        _db = db;
        _env = env;
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

    /// <summary>
    /// Báo cáo giao dịch voucher của hội viên.
    /// Nguồn: Crd_MemberVoucherTransactionController.cs:135-258 (Report).
    /// </summary>
    [HttpPost("report")]
    public async Task<IActionResult> Report([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Crd_MemberVoucherTransactionReportReq? req)
    {
        // 1. Chuẩn hoá tham số đầu vào: StdParam cho DLCode, DealPointType, RefNo, VoucherNo; StdDTime cho CreateDTimeUTCFrom, CreateDTimeUTCTo
        // Nguồn: Crd_MemberVoucherTransactionController.cs:159-164
        var strDLCode = string.IsNullOrWhiteSpace(req?.DLCode) ? null : req.DLCode.Trim().ToUpperInvariant();
        var strDealPointType = string.IsNullOrWhiteSpace(req?.DealPointType) ? null : req.DealPointType.Trim();
        var strRefNo = string.IsNullOrWhiteSpace(req?.RefNo) ? null : req.RefNo.Trim();
        var strVoucherNo = string.IsNullOrWhiteSpace(req?.VoucherNo) ? null : req.VoucherNo.Trim();
        var strCreateDTimeUTCFrom = string.IsNullOrWhiteSpace(req?.CreateDTimeUTCFrom) ? null : req.CreateDTimeUTCFrom.Trim();
        var strCreateDTimeUTCTo = string.IsNullOrWhiteSpace(req?.CreateDTimeUTCTo) ? null : req.CreateDTimeUTCTo.Trim();

        // 2. Guard validation bắt buộc (Crd_MemberVoucherTransactionController.cs:166-173):
        // CreateDTimeUTCFrom rỗng -> ném ServiceException("invalid_CreateDTimeUTCFrom", "invalid_CreateDTimeUTCFrom") (:166-169)
        if (string.IsNullOrEmpty(strCreateDTimeUTCFrom))
        {
            return PortResult.Fail("invalid_CreateDTimeUTCFrom", "Check.CreateDTimeUTCFrom", req?.CreateDTimeUTCFrom);
        }

        // CreateDTimeUTCTo rỗng -> ném ServiceException("invalid_CreateDTimeUTCTo", "invalid_CreateDTimeUTCTo") (:170-173)
        if (string.IsNullOrEmpty(strCreateDTimeUTCTo))
        {
            return PortResult.Fail("invalid_CreateDTimeUTCTo", "Check.CreateDTimeUTCTo", req?.CreateDTimeUTCTo);
        }

        // 3. Phân trang: chống tràn int và xử lý âm theo bài học A.12 (Crd_MemberVoucherTransactionController.cs:183-184, Transaction.cs:4897-4898)
        var pageIndex = req?.Ft_PageIndex ?? 0;
        var pageSize = req?.Ft_PageSize ?? 10;
        if (pageIndex < 0 || pageSize <= 0)
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

        // 4. Truy vấn kết hợp: Crd_MemberVoucherTransaction mvt left join Crd_Member cm left join RptPE_MemberVoucherDeCreaseInst rp
        // Nguồn draft: Transaction.cs:4919-4921, 4976-4981
        var baseQuery = from mvt in _db.Set<Crd_MemberVoucherTransaction>()
                        join cm in _db.Set<CrdMember>() on mvt.MemberNo equals cm.MemberNo into cmGroup
                        from cm in cmGroup.DefaultIfEmpty()
                        join rp in _db.Set<RptPE_MemberVoucherDeCreaseInst>() on mvt.AutoId.ToString() equals rp.RefNoDC into rpGroup
                        from rp in rpGroup.DefaultIfEmpty()
                        select new { mvt, cm, rp };

        // 5. Bộ lọc theo strWhereClause_Crd_MemberVoucherTransaction_Report (Crd_MemberVoucherTransactionController.cs:434-472):
        // DLCode: toán tử '=' (:443)
        if (!string.IsNullOrEmpty(strDLCode))
        {
            baseQuery = baseQuery.Where(x => EF.Functions.Like((x.mvt.DLCode ?? "").ToLower(), PortQuery.Equal(strDLCode), PortQuery.Esc));
        }

        // DealPointType: toán tử 'in', tách chuỗi theo dấu phẩy (:447-453)
        if (!string.IsNullOrEmpty(strDealPointType))
        {
            var dealTypes = strDealPointType.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => s.ToLowerInvariant())
                .ToList();
            baseQuery = baseQuery.Where(x => dealTypes.Contains((x.mvt.DealPointType ?? "").ToLower()));
        }

        // RefNo: toán tử '=' (:456)
        if (!string.IsNullOrEmpty(strRefNo))
        {
            baseQuery = baseQuery.Where(x => EF.Functions.Like((x.mvt.RefNo ?? "").ToLower(), PortQuery.Equal(strRefNo), PortQuery.Esc));
        }

        // VoucherNo: toán tử '=' trên bảng RptPE_MemberVoucherDeCreaseInst.VoucherNo (:460)
        if (!string.IsNullOrEmpty(strVoucherNo))
        {
            baseQuery = baseQuery.Where(x => x.rp != null && EF.Functions.Like((x.rp.VoucherNo ?? "").ToLower(), PortQuery.Equal(strVoucherNo), PortQuery.Esc));
        }

        // CreateDTimeUTCFrom: toán tử '>=' (:464)
        if (DateTime.TryParse(strCreateDTimeUTCFrom, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dFrom)
            || DateTime.TryParse(strCreateDTimeUTCFrom, out dFrom))
        {
            baseQuery = baseQuery.Where(x => x.mvt.CreateDTimeUTC >= dFrom);
        }

        // CreateDTimeUTCTo: toán tử '<=' (:468)
        if (DateTime.TryParse(strCreateDTimeUTCTo, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dTo)
            || DateTime.TryParse(strCreateDTimeUTCTo, out dTo))
        {
            baseQuery = baseQuery.Where(x => x.mvt.CreateDTimeUTC <= dTo);
        }

        // 6. Sắp xếp: order by cast(mvt.AutoId as bigint) asc (Transaction.cs:4924-4925, 4983-4984)
        var orderedQuery = baseQuery.OrderBy(x => x.mvt.AutoId);

        // 7. Phân trang: skip & take (Transaction.cs:4938-4939)
        long recordStart = (long)pageIndex * pageSize;
        var pagedQuery = orderedQuery
            .Skip((int)Math.Min(recordStart, int.MaxValue))
            .Take((int)Math.Min(pageSize, int.MaxValue));

        var rows = await pagedQuery.AsNoTracking().ToListAsync();

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

        // 8. Ánh xạ các trường NotMapped và trả về kết quả (Transaction.cs:4958-4974)
        var resultList = new List<Crd_MemberVoucherTransaction>(rows.Count);
        long seq = recordStart;
        foreach (var row in rows)
        {
            var item = row.mvt;
            item.MyIdxSeq = seq++;
            item.cm_MemberName = row.cm?.MemberName;
            item.cm_CarNo = row.cm?.CarNo;
            item.ic_VoucherNo = row.rp?.VoucherNo;
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

    /// <summary>
    /// Xuất báo cáo giao dịch voucher của hội viên ra file Excel — port 1:1 từ Crd_MemberVoucherTransactionController.ExportRpt (gate :262-368).
    /// </summary>
    [HttpPost("exportrpt")]
    public async Task<IActionResult> ExportRpt([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Crd_MemberVoucherTransactionExportRptReq? req)
    {
        // 1. Chuẩn hoá tham số đầu vào: StdParam cho DLCode, DealPointType, RefNo, VoucherNo; StdDTime cho CreateDTimeUTCFrom, CreateDTimeUTCTo
        // Nguồn gate: Crd_MemberVoucherTransactionController.cs:284-289
        var strDLCode = string.IsNullOrWhiteSpace(req?.DLCode) ? null : req.DLCode.Trim().ToUpperInvariant();
        var strDealPointType = string.IsNullOrWhiteSpace(req?.DealPointType) ? null : req.DealPointType.Trim();
        var strRefNo = string.IsNullOrWhiteSpace(req?.RefNo) ? null : req.RefNo.Trim();
        var strVoucherNo = string.IsNullOrWhiteSpace(req?.VoucherNo) ? null : req.VoucherNo.Trim();
        var strCreateDTimeUTCFrom = string.IsNullOrWhiteSpace(req?.CreateDTimeUTCFrom) ? null : req.CreateDTimeUTCFrom.Trim();
        var strCreateDTimeUTCTo = string.IsNullOrWhiteSpace(req?.CreateDTimeUTCTo) ? null : req.CreateDTimeUTCTo.Trim();

        // 2. Guard validation bắt buộc (Crd_MemberVoucherTransactionController.cs:291-298):
        // CreateDTimeUTCFrom rỗng -> ném ServiceException("invalid_CreateDTimeUTCFrom", "invalid_CreateDTimeUTCFrom") (:291-294)
        if (string.IsNullOrEmpty(strCreateDTimeUTCFrom))
        {
            return PortResult.Fail("invalid_CreateDTimeUTCFrom", "Check.CreateDTimeUTCFrom", req?.CreateDTimeUTCFrom);
        }

        // CreateDTimeUTCTo rỗng -> ném ServiceException("invalid_CreateDTimeUTCTo", "invalid_CreateDTimeUTCTo") (:295-298)
        if (string.IsNullOrEmpty(strCreateDTimeUTCTo))
        {
            return PortResult.Fail("invalid_CreateDTimeUTCTo", "Check.CreateDTimeUTCTo", req?.CreateDTimeUTCTo);
        }

        // 3. Truy vấn kết hợp: Crd_MemberVoucherTransaction mvt left join Crd_Member cm left join RptPE_MemberVoucherDeCreaseInst rp
        // Nguồn draft: Transaction.cs:4919-4921, 4976-4981
        var baseQuery = from mvt in _db.Set<Crd_MemberVoucherTransaction>()
                        join cm in _db.Set<CrdMember>() on mvt.MemberNo equals cm.MemberNo into cmGroup
                        from cm in cmGroup.DefaultIfEmpty()
                        join rp in _db.Set<RptPE_MemberVoucherDeCreaseInst>() on mvt.AutoId.ToString() equals rp.RefNoDC into rpGroup
                        from rp in rpGroup.DefaultIfEmpty()
                        select new { mvt, cm, rp };

        // 4. Bộ lọc theo strWhereClause_Crd_MemberVoucherTransaction_Report (Crd_MemberVoucherTransactionController.cs:434-472):
        // DLCode: toán tử '=' (:441-444)
        if (!string.IsNullOrEmpty(strDLCode))
        {
            baseQuery = baseQuery.Where(x => EF.Functions.Like((x.mvt.DLCode ?? "").ToLower(), PortQuery.Equal(strDLCode), PortQuery.Esc));
        }

        // DealPointType: toán tử 'in', tách chuỗi theo dấu phẩy (:445-453)
        if (!string.IsNullOrEmpty(strDealPointType))
        {
            var dealTypes = strDealPointType.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => s.ToLowerInvariant())
                .ToList();
            baseQuery = baseQuery.Where(x => dealTypes.Contains((x.mvt.DealPointType ?? "").ToLower()));
        }

        // RefNo: toán tử '=' (:454-457)
        if (!string.IsNullOrEmpty(strRefNo))
        {
            baseQuery = baseQuery.Where(x => EF.Functions.Like((x.mvt.RefNo ?? "").ToLower(), PortQuery.Equal(strRefNo), PortQuery.Esc));
        }

        // VoucherNo: toán tử '=' trên bảng RptPE_MemberVoucherDeCreaseInst.VoucherNo (:458-461)
        if (!string.IsNullOrEmpty(strVoucherNo))
        {
            baseQuery = baseQuery.Where(x => x.rp != null && EF.Functions.Like((x.rp.VoucherNo ?? "").ToLower(), PortQuery.Equal(strVoucherNo), PortQuery.Esc));
        }

        // CreateDTimeUTCFrom: toán tử '>=' (:462-465)
        if (DateTime.TryParse(strCreateDTimeUTCFrom, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dFrom)
            || DateTime.TryParse(strCreateDTimeUTCFrom, out dFrom))
        {
            baseQuery = baseQuery.Where(x => x.mvt.CreateDTimeUTC >= dFrom);
        }

        // CreateDTimeUTCTo: toán tử '<=' (:466-469)
        if (DateTime.TryParse(strCreateDTimeUTCTo, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dTo)
            || DateTime.TryParse(strCreateDTimeUTCTo, out dTo))
        {
            baseQuery = baseQuery.Where(x => x.mvt.CreateDTimeUTC <= dTo);
        }

        // 5. Sắp xếp: order by cast(mvt.AutoId as bigint) asc (Transaction.cs:4924-4925, 4983-4984)
        var orderedQuery = baseQuery.OrderBy(x => x.mvt.AutoId);

        // 6. Lấy toàn bộ bản ghi (không phân trang: Ft_RecordStart = "0", Ft_RecordCount = "123456000" tại gate :330-332)
        var rows = await orderedQuery.AsNoTracking().ToListAsync();

        // 7. Kiểm tra rỗng (gate :338, :352-355): nếu rỗng -> throw ServiceException("Không có dữ liệu") (:354)
        if (rows.Count == 0)
        {
            return PortResult.Fail("Crd_MemberVoucherTransaction_ExportRpt", "Message", "Không có dữ liệu");
        }

        // 8. Ánh xạ các trường NotMapped (Transaction.cs:4958-4974)
        var dataList = new List<Crd_MemberVoucherTransaction>(rows.Count);
        long seq = 0;
        foreach (var row in rows)
        {
            var item = row.mvt;
            item.MyIdxSeq = seq++;
            item.cm_MemberName = row.cm?.MemberName;
            item.cm_CarNo = row.cm?.CarNo;
            item.ic_VoucherNo = row.rp?.VoucherNo;
            dataList.Add(item);
        }

        // 9. Chuẩn bị đường dẫn thư mục xuất file Excel (gate :344)
        var webRoot = _env?.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
        {
            webRoot = _env != null
                ? Path.Combine(_env.ContentRootPath, "wwwroot")
                : Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        }

        // 10. Lấy từ điển 14 cột theo GetImportDicColums() trong Crd_MemberVoucherTransactionController.cs:372-392
        var dicColNames = GetImportDicColums();

        string virtualPath;
        try
        {
            // Sinh tên file: prefix = "Crd_MemberVoucherTransaction" (gate :344)
            string physicalPath;
            (physicalPath, virtualPath) = PortExcel.GenExcelExportFilePath(webRoot, "Crd_MemberVoucherTransaction");

            // Xuất danh sách ra Excel với sheet name "Crd_MemberVoucherTransaction" (gate :345)
            PortExcel.ExportToExcelFromList(dataList, dicColNames, physicalPath, "Crd_MemberVoucherTransaction");
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Crd_MemberVoucherTransaction_ExportRpt", "Exception", ex.Message);
        }

        // 11. Trả URL tải file với Success = true (gate :346-350)
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
    /// Từ điển 14 cột xuất Excel theo GetImportDicColums() tại gate Crd_MemberVoucherTransactionController.cs:372-392.
    /// </summary>
    private static Dictionary<string, string> GetImportDicColums()
    {
        return new Dictionary<string, string>
        {
            { "RefNo", "Số giao dịch" },                      // :376
            { "DLCode", "Đại lý giao dịch" },                  // :377
            { "RefCode00", "Số tham chiếu" },                  // :378
            { "MemberNo", "Mã hội viên" },                     // :379
            { "cm_MemberName", "Tên hội viên" },               // :380
            { "CardTypeUse", "Hạng sử dụng" },                 // :381
            { "CardTypeInit", "Hạng thực" },                   // :382
            { "CreateDate", "Ngày giao dịch" },                // :383
            { "DealPointType", "Loại điểm" },                  // :384
            { "PointChTotal", "Tổng giá trị" },                // :385
            { "ic_VoucherNo", "Mã Voucher sử dụng" },          // :386
            { "CardNo", "Mã kỳ xét hạng" },                    // :387
            { "cm_CarNo", "Biển số xe" },                      // :388
            { "PointExpiryDate", "Ngày hết hạn sử dụng" }      // :389
        };
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

/// <summary>
/// DTO tham số báo cáo giao dịch voucher hội viên.
/// Nguồn tham số gate: Crd_MemberVoucherTransactionController.cs:135-144.
/// </summary>
public class Crd_MemberVoucherTransactionReportReq
{
    public string? DLCode { get; set; }                            // Đại lý giao dịch (:136)
    public string? DealPointType { get; set; }                     // Loại giao dịch (:137)
    public string? RefNo { get; set; }                             // Số Giao dịch/Voucher (:138)
    public string? VoucherNo { get; set; }                         // Số Voucher sử dụng (:139)
    public string? CreateDTimeUTCFrom { get; set; }                // Thời gian giao dịch UTC từ (:140)
    public string? CreateDTimeUTCTo { get; set; }                  // Thời gian giao dịch UTC đến (:141)
    public int Ft_PageIndex { get; set; } = 0;                     // Trang hiện tại (:142)
    public int Ft_PageSize { get; set; } = 10;                     // Số bản ghi/trang (:143)
}

/// <summary>
/// DTO tham số xuất báo cáo giao dịch voucher hội viên ra Excel.
/// Nguồn tham số gate: Crd_MemberVoucherTransactionController.cs:262-269 (ExportRpt).
/// </summary>
public class Crd_MemberVoucherTransactionExportRptReq
{
    public string? DLCode { get; set; }                            // Đại lý giao dịch (:263)
    public string? DealPointType { get; set; }                     // Loại giao dịch (:264)
    public string? RefNo { get; set; }                             // Số Giao dịch/Voucher (:265)
    public string? VoucherNo { get; set; }                         // Số Voucher sử dụng (:266)
    public string? CreateDTimeUTCFrom { get; set; }                // Thời gian giao dịch UTC từ (:267)
    public string? CreateDTimeUTCTo { get; set; }                  // Thời gian giao dịch UTC đến (:268)
}
