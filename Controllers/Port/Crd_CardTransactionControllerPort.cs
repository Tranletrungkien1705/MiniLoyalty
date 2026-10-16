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

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty:
//   - Search (#12) — Crd_CardTransactionController.cs:32-113
//   - SearchDetailCrdDealUsePromotion (#13) — Crd_CardTransactionController.cs:114-187
//   - Export (#14) — Crd_CardTransactionController.cs:312-395
//
// Nguồn tra cứu (chỉ đọc):
//   - Gate: Crd_CardTransactionController.cs:312-395 (Export), :399-418 (GetImportDicColums)
//   - API:  CrdCardTransactionController.cs:24-91 (WA_Crd_CardTransaction_Get)
//   - BIZ:  Master.Loyalty.cs:21144-21253 (WAS_Crd_CardTransaction_Get)
//           Master.Loyalty.cs:21254-21408 (Crd_CardTransaction_Get)
//           Master.Loyalty.cs:21409-21817 (Crd_CardTransaction_GetX — bản LIVE)
// ===========================================================================================

/// <summary>
/// DTO tham số tìm kiếm giao dịch thẻ — port 1:1 Crd_CardTransactionSearch (gate :32-72, :191-308).
/// </summary>
public class Crd_CardTransactionDetailPromotionReq
{
    public string? CreateDTimeUTCFrom { get; set; }
    public string? CreateDTimeUTCTo { get; set; }
    public string? CardType { get; set; }
    public string? MemberNo { get; set; }
    public string? PhoneNo { get; set; }
    public string? PrProgramCode { get; set; }
    public string? DLCode { get; set; }
    public string? UsePrmDTimeFrom { get; set; }
    public string? UsePrmDTimeTo { get; set; }
}

/// <summary>
/// DTO tham số xuất Excel giao dịch thẻ — port 1:1 gate Crd_CardTransactionController.cs:312.
/// </summary>
public class Crd_CardTransactionExportReq
{
    public string? CreateDTimeUTCFrom { get; set; }     // Gate :312 — Thời gian thực hiện từ
    public string? CreateDTimeUTCTo { get; set; }       // Gate :312 — Thời gian thực hiện đến
    public string? CardType { get; set; }               // Gate :312 — Hạng thẻ (ánh xạ sang CardTypeUse)
    public string? CardTypeUse { get; set; }            // Hạng thẻ sử dụng (dự phòng)
    public string? MemberNo { get; set; }               // Gate :312 — Mã thẻ hội viên
    public string? PhoneNo { get; set; }                // Gate :312 — Số điện thoại
    public string? PrProgramCode { get; set; }          // Gate :312 — Mã chương trình
    public string? DLCode { get; set; }                 // Gate :312 — Mã đại lý thực hiện
    public string? OrgID { get; set; }                  // Bài học A.14 — OrgID tùy chọn
    public string? NetworkID { get; set; }              // Mã mạng
}

public class Crd_CardTransactionSearchReq
{
    public string? CreateDTimeUTCFrom { get; set; }     // Gate :32 — Thời gian thực hiện từ
    public string? CreateDTimeUTCTo { get; set; }       // Gate :32 — Thời gian thực hiện đến
    public string? EffDateStartFrom { get; set; }       // Gate :32 — Ngày hiệu lực từ
    public string? EffDateStartTo { get; set; }         // Gate :32 — Ngày hiệu lực đến
    public string? CardNo { get; set; }                 // Gate :32 — Số thẻ
    public string? IDCardNo { get; set; }               // Gate :33 — Số CMT/CCCD
    public string? MemberNo { get; set; }               // Gate :33 — Mã hội viên
    public string? PointRefFrom { get; set; }           // Gate :33 — Điểm tham chiếu từ
    public string? PointRefTo { get; set; }             // Gate :33 — Điểm tham chiếu đến
    public string? DealPointType { get; set; }          // Gate :33 — Loại giao dịch điểm
    public string? DLCode { get; set; }                 // Gate :33 — Mã đại lý thực hiện
    public string? PointChRankTotalFrom { get; set; }   // Gate :34 — Điểm xét hạng từ
    public string? PointChRankTotalTo { get; set; }     // Gate :34 — Điểm xét hạng đến
    public string? AmountChTotalFrom { get; set; }      // Gate :34 — Tiền giao dịch từ
    public string? AmountChTotalTo { get; set; }        // Gate :34 — Tiền giao dịch đến

    public string? PrProgramCode { get; set; }          // strWhereClause :198 — Mã chương trình ưu đãi
    public string? FlagPoint { get; set; }              // strWhereClause :212 — Cờ điểm
    public string? PhoneNo { get; set; }                // strWhereClause :246 — Số điện thoại
    public string? CardType { get; set; }               // Gate :114 SearchDetailCrdDealUsePromotion — Hạng thẻ
    public string? CardTypeUse { get; set; }            // strWhereClause :250 — Hạng thẻ sử dụng
    public string? UsePrmDTimeFrom { get; set; }        // strWhereClause :297 — Ngày dùng ưu đãi từ
    public string? UsePrmDTimeTo { get; set; }          // strWhereClause :302 — Ngày dùng ưu đãi đến

    public string? OrgID { get; set; }                  // Bài học A.14 — OrgID tùy chọn
    public string? NetworkID { get; set; }              // Mã mạng

    public int Ft_PageIndex { get; set; } = 0;          // Phân trang
    public int Ft_PageSize { get; set; } = 10;          // Phân trang (default 10)
    public string? Ft_RecordStart { get; set; }
    public string? Ft_RecordCount { get; set; }
}

/// <summary>
/// DTO dòng kết quả chi tiết giao dịch — port 1:1 Crd_CardTransaction model nguồn (Master.Loyalty.cs:21527-21619, :21706-21783).
/// Đầy đủ 66 thuộc tính theo select của hàm biz Crd_CardTransaction_GetX.
/// </summary>
public class Crd_CardTransactionSearchResult
{
    public long AutoId { get; set; }
    public string? RefNo { get; set; }
    public string? ProjectCode { get; set; }
    public string? NetworkID { get; set; }
    public string? OrgID { get; set; }
    public string? DLCode { get; set; }
    public string? MemberNo { get; set; }
    public string? CardNo { get; set; }
    public string? CardSourceCode { get; set; }
    public string? CardTypeUse { get; set; }
    public string? CardTypeInit { get; set; }
    public string? DealPointType { get; set; }
    public string? PrProgramCode { get; set; }
    public DateTime? EffDateStart { get; set; }
    public DateTime? EffDateEnd { get; set; }
    public DateTime? UsePrmDTime { get; set; }
    public double? PointChTotal { get; set; }
    public double? PointChBlock { get; set; }
    public double? AmountChTotal { get; set; }
    public double? AmountChBlock { get; set; }
    public double? QtyVisitChTotal { get; set; }
    public double? QtyVisitChBlock { get; set; }
    public double? QtyPrChTotal { get; set; }
    public double? QtyPrChBlock { get; set; }
    public double? QtyPrUsed { get; set; }
    public double? PointRef { get; set; }
    public double? AmountRef { get; set; }
    public double? PolicyPointUnit { get; set; }
    public double? PolicyAmountUnit { get; set; }
    public string? PolicyCode { get; set; }
    public double? ParamPointUnit { get; set; }
    public double? ParamAmountUnit { get; set; }
    public string? ParamCode { get; set; }
    public string? FlagViewUI { get; set; }
    public string? FlagPoint { get; set; }
    public string? FlagAmount { get; set; }
    public string? FlagPr { get; set; }
    public string? FunctionName { get; set; }
    public string? FunctionActionType { get; set; }
    public string? FunctionRemark { get; set; }
    public string? RefType { get; set; }
    public double? PointChRankTotal { get; set; }
    public string? RefCode00 { get; set; }
    public string? RefCode01 { get; set; }
    public string? RefCode02 { get; set; }
    public string? RefCode03 { get; set; }
    public string? RefCode04 { get; set; }
    public string? RefCode05 { get; set; }
    public string? RefCode06 { get; set; }
    public string? RefCode07 { get; set; }
    public string? RefCode08 { get; set; }
    public string? RefCode09 { get; set; }
    public string? CreateDTimeUTC { get; set; } // Biz :21221 định dạng yyyy-MM-dd giờ VN
    public string? CreateBy { get; set; }
    public DateTime? LUDTimeUTC { get; set; }
    public string? LUBy { get; set; }
    public DateTime? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
    public string? PointExpiryDTime { get; set; } // Biz :21222 định dạng yyyy-MM-dd
    public double? AmountChTotalInit { get; set; }
    public double? AmountChTotalDiffer { get; set; }
    public double? AmountAvailPrevious { get; set; }
    public DateTime? PointExpiryDTimeSync { get; set; }
    public double? PointChTotalInit { get; set; }
    public double? PointChTotalDiffer { get; set; }

    // Thông tin từ Mst_PromotionProgram
    public string? PrProgramName { get; set; }
    public string? Remark { get; set; }
    // Thông tin từ Mst_PromotionProgramDtl
    public string? Unit { get; set; }
    // Thông tin từ Crd_Member
    public string? CarNo { get; set; }
    public string? PhoneNo { get; set; }
    public string? MemberName { get; set; }
    // Thông tin từ Crd_Card / Crd_MemberDiscountTransaction
    public string? DLCodeExceptionally { get; set; }

    // 3 cột chiết khấu (Master.Loyalty.cs:21600-21602, :21780-21782)
    public double DiscountRate { get; set; }
    public double DiscountAmount { get; set; }
    public double DiscountRevenue { get; set; }
}

[ApiController]
[Route("api/crd_cardtransaction")]
[Produces("application/json")]
public class Crd_CardTransactionControllerPort : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment? _env;

    public Crd_CardTransactionControllerPort(AppDbContext db, IWebHostEnvironment? env = null)
    {
        _db = db;
        _env = env;
    }

    /// <summary>
    /// Port 1:1 Search — Crd_CardTransactionController.cs:32-113.
    /// Tra cứu danh sách giao dịch thẻ, kết hợp liên kết Crd_Member, Crd_Card, Mst_PromotionProgram,
    /// Mst_PromotionProgramDtl và union Crd_MemberDiscountTransaction khi DealPointType chứa DISCOUNTRO.
    /// </summary>
    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Crd_CardTransactionSearchReq? req)
    {
        var r = req ?? new Crd_CardTransactionSearchReq();
        if (!string.IsNullOrWhiteSpace(r.CardType) && string.IsNullOrWhiteSpace(r.CardTypeUse))
        {
            r.CardTypeUse = r.CardType;
        }
        return await SearchCoreAsync(r);
    }

    /// <summary>
    /// Port 1:1 Crd_CardTransactionController.SearchDetailCrdDealUsePromotion (#13, gate :114-187). Gate CHỈ nhận 9 tham số:
    /// CreateDTimeUTCFrom/To (StdDTimeBeginDay/EndDay), CardType (-> CardTypeUse), MemberNo, PhoneNo, PrProgramCode, DLCode, UsePrmDTimeFrom/To;
    /// DealPointType GHI ĐÈ cố định "PRPROGRAM" (:141) — KHÔNG nhận từ client; không phân trang (Ft_RecordStart = "0", Ft_RecordCount = mặc định 123456000);
    /// Data = { DataList = danh sách } (không PageInfo, không ItemCount), rỗng -> DataList null (:166-174). Dùng chung lõi truy vấn GetX với Search (#12).
    /// </summary>
    [HttpPost("searchdetailcrddealusepromotion")]
    public async Task<IActionResult> SearchDetailCrdDealUsePromotion([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Crd_CardTransactionDetailPromotionReq? req)
    {
        var r = req ?? new Crd_CardTransactionDetailPromotionReq();
        var core = new Crd_CardTransactionSearchReq
        {
            CreateDTimeUTCFrom = r.CreateDTimeUTCFrom,
            CreateDTimeUTCTo = r.CreateDTimeUTCTo,
            UsePrmDTimeFrom = r.UsePrmDTimeFrom,
            UsePrmDTimeTo = r.UsePrmDTimeTo,
            MemberNo = r.MemberNo,
            DealPointType = "PRPROGRAM",
            CardTypeUse = r.CardType,
            PhoneNo = r.PhoneNo,
            DLCode = r.DLCode,
            PrProgramCode = r.PrProgramCode,
            Ft_PageIndex = 0,
            Ft_PageSize = 123456000,
            Ft_RecordStart = "0",
            Ft_RecordCount = "123456000"
        };

        var res = await SearchCoreAsync(core);
        if (res is OkObjectResult ok && ok.Value != null)
        {
            var data = ok.Value.GetType().GetProperty("Data")?.GetValue(ok.Value);
            var list = data?.GetType().GetProperty("DataList")?.GetValue(data) as System.Collections.IList;
            return Ok(new { Success = true, Data = new { DataList = (list != null && list.Count > 0) ? list : null } });
        }
        return res;
    }

    /// <summary>
    /// Port 1:1 Export — Crd_CardTransactionController.cs:312-395 (#014).
    /// Tham số nhận qua body JSON: { CreateDTimeUTCFrom, CreateDTimeUTCTo, CardType, MemberNo, PhoneNo, PrProgramCode, DLCode }.
    /// Nguồn gate:
    /// - Tham số truyền vào được ánh xạ qua hàm lọc strWhereClause_Crd_CardTransaction (:333-342) với:
    ///   DealPointType = "PRPROGRAM" (:338) — cố định, không nhận từ client.
    ///   CreateDTimeUTCFrom + " 00:00:00", CreateDTimeUTCTo + " 23:59:59" (:335-336).
    ///   CardTypeUse = CardType (:339).
    /// - Phân trang: Ft_RecordStart = "0", Ft_RecordCount = mặc định 123456000 (:355-356).
    /// - Dữ liệu rỗng (rows null hoặc Count == 0) -> ném ServiceException("Không có dữ liệu") (:379-382)
    ///   -> trả về HTTP 400 ErridnLoyalty.Crd_CardTransaction_Export kèm Params Message = "Không có dữ liệu".
    /// - Có dữ liệu:
    ///   * Lấy từ điển cột từ GetImportDicColums() (:368, :399-418) gồm 14 cột.
    ///   * Sinh đường dẫn file: GenExcelExportFilePath("Crd_CardTransaction", ref url) (:370).
    ///   * Xuất file Excel: ExcelExport.ExportToExcelFromList(list, dicColNames, filePath, "Crd_CardTransaction") (:371).
    ///   * Trả về Success = true, Data = { Url = GetRequestBaseAddress() + url } (:372-376).
    /// </summary>
    [HttpPost("export")]
    public async Task<IActionResult> Export([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Crd_CardTransactionExportReq? req)
    {
        var r = req ?? new Crd_CardTransactionExportReq();
        var cardTypeUse = !string.IsNullOrWhiteSpace(r.CardType) ? r.CardType : r.CardTypeUse;

        var core = new Crd_CardTransactionSearchReq
        {
            CreateDTimeUTCFrom = string.IsNullOrWhiteSpace(r.CreateDTimeUTCFrom) ? null : (r.CreateDTimeUTCFrom.Contains(' ') ? r.CreateDTimeUTCFrom : r.CreateDTimeUTCFrom + " 00:00:00"),
            CreateDTimeUTCTo = string.IsNullOrWhiteSpace(r.CreateDTimeUTCTo) ? null : (r.CreateDTimeUTCTo.Contains(' ') ? r.CreateDTimeUTCTo : r.CreateDTimeUTCTo + " 23:59:59"),
            MemberNo = r.MemberNo,
            DealPointType = "PRPROGRAM", // Gate Crd_CardTransactionController.cs:338 cố định "PRPROGRAM"
            CardTypeUse = cardTypeUse,
            PhoneNo = r.PhoneNo,
            DLCode = r.DLCode,
            // PrProgramCode: gate Export (:312) nhận tham số nhưng KHÔNG đưa vào strWhereClause (:333-342) => bỏ qua (LESSONS A.18)
            OrgID = r.OrgID,
            NetworkID = r.NetworkID,
            Ft_PageIndex = 0,
            Ft_PageSize = 123456000,
            Ft_RecordStart = "0",
            Ft_RecordCount = "123456000"
        };

        var searchResult = await SearchCoreAsync(core);
        if (searchResult is not OkObjectResult ok || ok.Value == null)
        {
            return searchResult;
        }

        var dataObj = ok.Value.GetType().GetProperty("Data")?.GetValue(ok.Value);
        var pageInfo = dataObj as PageInfo<Crd_CardTransactionSearchResult>;
        var rows = pageInfo?.DataList;

        // Gate Crd_CardTransactionController.cs:364, 379-382:
        // if (objRT_Crd_CardTransactionCur.Lst_Crd_CardTransaction != null && objRT_Crd_CardTransactionCur.Lst_Crd_CardTransaction.Count > 0)
        // else throw new ServiceException("Không có dữ liệu");
        if (rows == null || rows.Count == 0)
        {
            return PortResult.Fail("Crd_CardTransaction_Export", "Message", "Không có dữ liệu");
        }

        // Tạo thư mục và đường dẫn file Excel xuất: GenExcelExportFilePath (gate :370)
        var webRoot = _env?.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
        {
            webRoot = _env != null ? Path.Combine(_env.ContentRootPath, "wwwroot") : Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        }

        // Lấy từ điển cột: gate Crd_CardTransactionController.cs:368 gọi GetImportDicColums() (14 cột)
        var dicColNames = GetImportDicColums();
        string virtualPath;
        try
        {
            var (physicalPath, vPath) = PortExcel.GenExcelExportFilePath(webRoot, "Crd_CardTransaction");
            virtualPath = vPath;

            // Ghi danh sách ra file Excel với sheet "Crd_CardTransaction" (gate :371)
            PortExcel.ExportToExcelFromList(rows, dicColNames, physicalPath, "Crd_CardTransaction");
        }
        catch (Exception ex)
        {
            // Gate :384-388 bắt ngoại lệ và trả Success=false
            return PortResult.Fail("Crd_CardTransaction_Export", "Exception", ex.Message);
        }

        // Trả URL tải file: Data = { Url = GetRequestBaseAddress() + url } (gate :372-376)
        return Ok(new
        {
            Success = true,
            Data = new
            {
                Url = PortExcel.GetRequestBaseAddress(Request) + virtualPath
            }
        });
    }

    private async Task<IActionResult> SearchCoreAsync(Crd_CardTransactionSearchReq r)
    {

        // 1. Base query trên Crd_CardTransaction join Crd_Member, Crd_Card, Mst_PromotionProgram, Mst_PromotionProgramDtl
        // Master.Loyalty.cs:21484-21486, :21604-21615
        var q = from cct in _db.Set<CrdCardTransaction>()
                join cm in _db.Set<CrdMember>() on cct.MemberNo equals cm.MemberNo
                join cc in _db.Set<CrdCard>() on new { cct.MemberNo, cct.CardNo } equals new { cc.MemberNo, cc.CardNo } into ccG
                from cc in ccG.DefaultIfEmpty()
                join mpp in _db.Set<Mst_PromotionProgram>() on cct.PrProgramCode equals mpp.PrProgramCode into mppG
                from mpp in mppG.DefaultIfEmpty()
                join mppd in _db.Set<Mst_PromotionProgramDtl>() on new { cct.PrProgramCode, CardType = cct.CardTypeUse } equals new { mppd.PrProgramCode, CardType = (string?)mppd.CardType } into mppdG
                from mppd in mppdG.DefaultIfEmpty()
                select new { cct, cm, cc, mpp, mppd };

        // 2. Áp dụng các điều kiện lọc từ strWhereClause_Crd_CardTransaction (Crd_CardTransactionController.cs:191-308)
        // PrProgramCode: = nguyên chuỗi (:198-201)
        if (!string.IsNullOrWhiteSpace(r.PrProgramCode))
        {
            var val = PortQuery.Equal(r.PrProgramCode);
            q = q.Where(x => EF.Functions.Like((x.cct.PrProgramCode ?? "").ToLower(), val, PortQuery.Esc));
        }

        // EffDateStartFrom / EffDateStartTo: >= / <= (:202-209)
        DateTime? dEffStartFrom = null;
        DateTime? dEffStartTo = null;
        if (!string.IsNullOrWhiteSpace(r.EffDateStartFrom) && DateTime.TryParse(r.EffDateStartFrom, out var ef1))
        {
            dEffStartFrom = ef1;
            q = q.Where(x => x.cct.EffDateStart >= dEffStartFrom);
        }
        if (!string.IsNullOrWhiteSpace(r.EffDateStartTo) && DateTime.TryParse(r.EffDateStartTo, out var ef2))
        {
            dEffStartTo = ef2;
            q = q.Where(x => x.cct.EffDateStart <= dEffStartTo);
        }

        // FlagPoint: = nguyên chuỗi (:210-213)
        if (!string.IsNullOrWhiteSpace(r.FlagPoint))
        {
            var val = PortQuery.Equal(r.FlagPoint);
            q = q.Where(x => EF.Functions.Like((x.cct.FlagPoint ?? "").ToLower(), val, PortQuery.Esc));
        }

        // PointRefFrom / PointRefTo: >= / <= (:215-222)
        if (!string.IsNullOrWhiteSpace(r.PointRefFrom) && double.TryParse(r.PointRefFrom, out var pRefFrom))
        {
            q = q.Where(x => (x.cct.PointRef ?? 0) >= pRefFrom);
        }
        if (!string.IsNullOrWhiteSpace(r.PointRefTo) && double.TryParse(r.PointRefTo, out var pRefTo))
        {
            q = q.Where(x => (x.cct.PointRef ?? 0) <= pRefTo);
        }

        // CardNo: = nguyên chuỗi (:223-226)
        if (!string.IsNullOrWhiteSpace(r.CardNo))
        {
            var val = PortQuery.Equal(r.CardNo);
            q = q.Where(x => EF.Functions.Like((x.cct.CardNo ?? "").ToLower(), val, PortQuery.Esc));
        }

        // DealPointType: tách dấu phẩy, dùng in (:227-235)
        List<string>? dealPointTypeList = null;
        bool isDiscountSearch = false;
        if (!string.IsNullOrWhiteSpace(r.DealPointType))
        {
            dealPointTypeList = r.DealPointType.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                              .Select(p => p.Trim().ToLowerInvariant())
                                              .ToList();
            if (dealPointTypeList.Count > 0)
            {
                q = q.Where(x => dealPointTypeList.Contains((x.cct.DealPointType ?? "").ToLower()));
            }

            if (r.DealPointType.ToUpperInvariant().Contains(LoyaltyConst.DealPointType.DiscountRO))
            {
                isDiscountSearch = true;
            }
        }

        // IDCardNo: = nguyên chuỗi trên Crd_Member (:237-240)
        if (!string.IsNullOrWhiteSpace(r.IDCardNo))
        {
            var val = PortQuery.Equal(r.IDCardNo);
            q = q.Where(x => EF.Functions.Like((x.cm.IDCardNo ?? "").ToLower(), val, PortQuery.Esc));
        }

        // DLCode: = nguyên chuỗi (:241-244)
        if (!string.IsNullOrWhiteSpace(r.DLCode))
        {
            var val = PortQuery.Equal(r.DLCode);
            q = q.Where(x => EF.Functions.Like((x.cct.DLCode ?? "").ToLower(), val, PortQuery.Esc));
        }

        // PhoneNo: = nguyên chuỗi trên Crd_Member (:245-248)
        if (!string.IsNullOrWhiteSpace(r.PhoneNo))
        {
            var val = PortQuery.Equal(r.PhoneNo);
            q = q.Where(x => EF.Functions.Like((x.cm.PhoneNo ?? "").ToLower(), val, PortQuery.Esc));
        }

        // CardTypeUse: = nguyên chuỗi (:249-252)
        if (!string.IsNullOrWhiteSpace(r.CardTypeUse))
        {
            var val = PortQuery.Equal(r.CardTypeUse);
            q = q.Where(x => EF.Functions.Like((x.cct.CardTypeUse ?? "").ToLower(), val, PortQuery.Esc));
        }

        // MemberNo: = nguyên chuỗi (:254-257)
        if (!string.IsNullOrWhiteSpace(r.MemberNo))
        {
            var val = PortQuery.Equal(r.MemberNo);
            q = q.Where(x => EF.Functions.Like((x.cct.MemberNo ?? "").ToLower(), val, PortQuery.Esc));
        }

        // CreateDTimeUTCFrom / CreateDTimeUTCTo: StdDTimeBeginDay / StdDTimeEndDay -> UTC (:258-278)
        DateTime? utcFrom = null;
        DateTime? utcTo = null;
        if (!string.IsNullOrWhiteSpace(r.CreateDTimeUTCFrom) && DateTime.TryParse(r.CreateDTimeUTCFrom, out var cdFrom))
        {
            utcFrom = cdFrom.Date.AddHours(-7); // Bắt đầu ngày Việt Nam đổi sang UTC
            q = q.Where(x => x.cct.CreateDTimeUTC >= utcFrom);
        }
        if (!string.IsNullOrWhiteSpace(r.CreateDTimeUTCTo) && DateTime.TryParse(r.CreateDTimeUTCTo, out var cdTo))
        {
            utcTo = cdTo.Date.AddDays(1).AddSeconds(-1).AddHours(-7); // Cuối ngày Việt Nam đổi sang UTC
            q = q.Where(x => x.cct.CreateDTimeUTC <= utcTo);
        }

        // AmountChTotalFrom / AmountChTotalTo: >= / <= (:280-287)
        if (!string.IsNullOrWhiteSpace(r.AmountChTotalFrom) && double.TryParse(r.AmountChTotalFrom, out var amtFrom))
        {
            q = q.Where(x => (x.cct.AmountChTotal ?? 0) >= amtFrom);
        }
        if (!string.IsNullOrWhiteSpace(r.AmountChTotalTo) && double.TryParse(r.AmountChTotalTo, out var amtTo))
        {
            q = q.Where(x => (x.cct.AmountChTotal ?? 0) <= amtTo);
        }

        // PointChRankTotalFrom / PointChRankTotalTo: >= / <= (:288-296)
        if (!string.IsNullOrWhiteSpace(r.PointChRankTotalFrom) && double.TryParse(r.PointChRankTotalFrom, out var rnkFrom))
        {
            q = q.Where(x => (x.cct.PointChRankTotal ?? 0) >= rnkFrom);
        }
        if (!string.IsNullOrWhiteSpace(r.PointChRankTotalTo) && double.TryParse(r.PointChRankTotalTo, out var rnkTo))
        {
            q = q.Where(x => (x.cct.PointChRankTotal ?? 0) <= rnkTo);
        }

        // UsePrmDTimeFrom / UsePrmDTimeTo: >= / <= (:297-305)
        if (!string.IsNullOrWhiteSpace(r.UsePrmDTimeFrom) && DateTime.TryParse(r.UsePrmDTimeFrom, out var prmFrom))
        {
            q = q.Where(x => x.cct.UsePrmDTime >= prmFrom);
        }
        if (!string.IsNullOrWhiteSpace(r.UsePrmDTimeTo) && DateTime.TryParse(r.UsePrmDTimeTo, out var prmTo))
        {
            q = q.Where(x => x.cct.UsePrmDTime <= prmTo);
        }

        // OrgID / NetworkID (Bài học A.14)
        if (!string.IsNullOrWhiteSpace(r.OrgID))
        {
            var val = PortQuery.Equal(r.OrgID);
            q = q.Where(x => EF.Functions.Like((x.cct.OrgID ?? "").ToLower(), val, PortQuery.Esc));
        }
        if (!string.IsNullOrWhiteSpace(r.NetworkID))
        {
            var val = PortQuery.Equal(r.NetworkID);
            q = q.Where(x => EF.Functions.Like((x.cct.NetworkID ?? "").ToLower(), val, PortQuery.Esc));
        }

        // 3. Phân trang và thực thi truy vấn
        var pageIndex = r.Ft_PageIndex;
        var pageSize = r.Ft_PageSize;
        var start = (long)pageIndex * pageSize;

        if (!isDiscountSearch)
        {
            // Sắp xếp: cct.LogLUDTimeUTC desc (Master.Loyalty.cs:21493, :21618)
            var ordered = q.OrderByDescending(x => x.cct.LogLUDTimeUTC);
            var itemCount = await ordered.CountAsync();

            var rows = (pageSize <= 0 || start < 0)
                ? new List<Crd_CardTransactionSearchResult>()
                : (await ordered.Skip((int)Math.Min(start, int.MaxValue)).Take((int)Math.Min(pageSize, int.MaxValue))
                                .Select(x => new
                                {
                                    cct = x.cct,
                                    CarNo = x.cm.CarNo,
                                    PhoneNo = x.cm.PhoneNo,
                                    MemberName = x.cm.MemberName,
                                    DLCodeExceptionally = x.cc != null ? x.cc.DLCodeExceptionally : null,
                                    PrProgramName = x.mpp != null ? x.mpp.PrProgramName : null,
                                    Remark = x.mpp != null ? x.mpp.Remark : null,
                                    Unit = x.mppd != null ? x.mppd.Unit : null
                                })
                                .ToListAsync())
                                .Select(x => MapToResult(x.cct, x.CarNo, x.PhoneNo, x.MemberName, x.DLCodeExceptionally, x.PrProgramName, x.Remark, x.Unit))
                                .ToList();

            var ps = pageSize <= 0 ? 0 : pageSize;
            var pageCount = (pageSize <= 0 || rows.Count == 0) ? 0 : (itemCount % ps == 0 ? itemCount / ps : itemCount / ps + 1);

            return Ok(new { Success = true, Data = new PageInfo<Crd_CardTransactionSearchResult>(rows, itemCount, pageCount, pageIndex, pageSize) });
        }
        else
        {
            // DealPointType có DISCOUNTRO -> union với Crd_MemberDiscountTransaction (Master.Loyalty.cs:21664-21791)
            var qDisc = from mvt in _db.Set<CrdMemberDiscountTransaction>()
                        join cm in _db.Set<CrdMember>() on mvt.MemberNo equals cm.MemberNo
                        select new { mvt, cm };

            if (!string.IsNullOrWhiteSpace(r.CardNo))
            {
                var val = PortQuery.Equal(r.CardNo);
                qDisc = qDisc.Where(x => EF.Functions.Like((x.mvt.CardNo ?? "").ToLower(), val, PortQuery.Esc));
            }
            if (!string.IsNullOrWhiteSpace(r.MemberNo))
            {
                var val = PortQuery.Equal(r.MemberNo);
                qDisc = qDisc.Where(x => EF.Functions.Like((x.mvt.MemberNo ?? "").ToLower(), val, PortQuery.Esc));
            }
            if (!string.IsNullOrWhiteSpace(r.DLCode))
            {
                var val = PortQuery.Equal(r.DLCode);
                qDisc = qDisc.Where(x => EF.Functions.Like((x.mvt.DLCode ?? "").ToLower(), val, PortQuery.Esc));
            }
            if (!string.IsNullOrWhiteSpace(r.CardTypeUse))
            {
                var val = PortQuery.Equal(r.CardTypeUse);
                qDisc = qDisc.Where(x => EF.Functions.Like((x.mvt.CardTypeUse ?? "").ToLower(), val, PortQuery.Esc));
            }
            if (!string.IsNullOrWhiteSpace(r.IDCardNo))
            {
                var val = PortQuery.Equal(r.IDCardNo);
                qDisc = qDisc.Where(x => EF.Functions.Like((x.cm.IDCardNo ?? "").ToLower(), val, PortQuery.Esc));
            }
            if (!string.IsNullOrWhiteSpace(r.PhoneNo))
            {
                var val = PortQuery.Equal(r.PhoneNo);
                qDisc = qDisc.Where(x => EF.Functions.Like((x.cm.PhoneNo ?? "").ToLower(), val, PortQuery.Esc));
            }
            if (dEffStartFrom.HasValue)
            {
                qDisc = qDisc.Where(x => x.mvt.EffDateStart >= dEffStartFrom.Value);
            }
            if (dEffStartTo.HasValue)
            {
                qDisc = qDisc.Where(x => x.mvt.EffDateStart <= dEffStartTo.Value);
            }
            if (utcFrom.HasValue)
            {
                qDisc = qDisc.Where(x => x.mvt.CreateDTimeUTC >= utcFrom.Value);
            }
            if (utcTo.HasValue)
            {
                qDisc = qDisc.Where(x => x.mvt.CreateDTimeUTC <= utcTo.Value);
            }
            if (!string.IsNullOrWhiteSpace(r.OrgID))
            {
                var val = PortQuery.Equal(r.OrgID);
                qDisc = qDisc.Where(x => EF.Functions.Like((x.mvt.OrgID ?? "").ToLower(), val, PortQuery.Esc));
            }
            if (!string.IsNullOrWhiteSpace(r.NetworkID))
            {
                var val = PortQuery.Equal(r.NetworkID);
                qDisc = qDisc.Where(x => EF.Functions.Like((x.mvt.NetworkID ?? "").ToLower(), val, PortQuery.Esc));
            }

            var list1 = (await q.Select(x => new
            {
                cct = x.cct,
                CarNo = x.cm.CarNo,
                PhoneNo = x.cm.PhoneNo,
                MemberName = x.cm.MemberName,
                DLCodeExceptionally = x.cc != null ? x.cc.DLCodeExceptionally : null,
                PrProgramName = x.mpp != null ? x.mpp.PrProgramName : null,
                Remark = x.mpp != null ? x.mpp.Remark : null,
                Unit = x.mppd != null ? x.mppd.Unit : null
            }).ToListAsync())
            .Select(x => MapToResult(x.cct, x.CarNo, x.PhoneNo, x.MemberName, x.DLCodeExceptionally, x.PrProgramName, x.Remark, x.Unit))
            .ToList();

            var list2 = (await qDisc.Select(x => new
            {
                mvt = x.mvt,
                CarNo = x.cm.CarNo,
                PhoneNo = x.cm.PhoneNo,
                MemberName = x.cm.MemberName
            }).ToListAsync())
            .Select(x => MapDiscountToResult(x.mvt, x.CarNo, x.PhoneNo, x.MemberName))
            .ToList();

            // Union all và sắp xếp theo LogLUDTimeUTC desc (Master.Loyalty.cs:21618, :21705)
            var combined = list1.Concat(list2).OrderByDescending(x => x.LogLUDTimeUTC).ToList();
            var itemCount = combined.Count;

            var page = (pageSize <= 0 || start < 0)
                ? new List<Crd_CardTransactionSearchResult>()
                : combined.Skip((int)Math.Min(start, int.MaxValue)).Take((int)Math.Min(pageSize, int.MaxValue)).ToList();

            var ps = pageSize <= 0 ? 0 : pageSize;
            var pageCount = (pageSize <= 0 || page.Count == 0) ? 0 : (itemCount % ps == 0 ? itemCount / ps : itemCount / ps + 1);

            return Ok(new { Success = true, Data = new PageInfo<Crd_CardTransactionSearchResult>(page, itemCount, pageCount, pageIndex, pageSize) });
        }
    }

    private static Crd_CardTransactionSearchResult MapToResult(
        CrdCardTransaction cct,
        string? carNo,
        string? phoneNo,
        string? memberName,
        string? dlCodeExceptionally,
        string? prProgramName,
        string? remark,
        string? unit)
    {
        return new Crd_CardTransactionSearchResult
        {
            AutoId = cct.AutoId,
            RefNo = cct.RefNo,
            ProjectCode = cct.ProjectCode,
            NetworkID = cct.NetworkID,
            OrgID = cct.OrgID,
            DLCode = cct.DLCode,
            MemberNo = cct.MemberNo,
            CardNo = cct.CardNo,
            CardSourceCode = cct.CardSourceCode,
            CardTypeUse = cct.CardTypeUse,
            CardTypeInit = cct.CardTypeInit,
            DealPointType = cct.DealPointType,
            PrProgramCode = cct.PrProgramCode,
            EffDateStart = cct.EffDateStart,
            EffDateEnd = cct.EffDateEnd,
            UsePrmDTime = cct.UsePrmDTime,
            PointChTotal = cct.PointChTotal,
            PointChBlock = cct.PointChBlock,
            AmountChTotal = cct.AmountChTotal,
            AmountChBlock = cct.AmountChBlock,
            QtyVisitChTotal = cct.QtyVisitChTotal,
            QtyVisitChBlock = cct.QtyVisitChBlock,
            QtyPrChTotal = cct.QtyPrChTotal,
            QtyPrChBlock = cct.QtyPrChBlock,
            QtyPrUsed = cct.QtyPrUsed,
            PointRef = cct.PointRef,
            AmountRef = cct.AmountRef,
            PolicyPointUnit = cct.PolicyPointUnit,
            PolicyAmountUnit = cct.PolicyAmountUnit,
            PolicyCode = cct.PolicyCode,
            ParamPointUnit = cct.ParamPointUnit,
            ParamAmountUnit = cct.ParamAmountUnit,
            ParamCode = cct.ParamCode,
            FlagViewUI = cct.FlagViewUI,
            FlagPoint = cct.FlagPoint,
            FlagAmount = cct.FlagAmount,
            FlagPr = cct.FlagPr,
            FunctionName = cct.FunctionName,
            FunctionActionType = cct.FunctionActionType,
            FunctionRemark = cct.FunctionRemark,
            RefType = cct.RefType,
            PointChRankTotal = cct.PointChRankTotal,
            RefCode00 = cct.RefCode00,
            RefCode01 = cct.RefCode01,
            RefCode02 = cct.RefCode02,
            RefCode03 = cct.RefCode03,
            RefCode04 = cct.RefCode04,
            RefCode05 = cct.RefCode05,
            RefCode06 = cct.RefCode06,
            RefCode07 = cct.RefCode07,
            RefCode08 = cct.RefCode08,
            RefCode09 = cct.RefCode09,
            // Biz :21221 định dạng CreateDTimeUTC sang yyyy-MM-dd giờ VN
            CreateDTimeUTC = cct.CreateDTimeUTC.HasValue ? cct.CreateDTimeUTC.Value.AddHours(7).ToString("yyyy-MM-dd") : null,
            CreateBy = cct.CreateBy,
            LUDTimeUTC = cct.LUDTimeUTC,
            LUBy = cct.LUBy,
            LogLUDTimeUTC = cct.LogLUDTimeUTC,
            LogLUBy = cct.LogLUBy,
            // Biz :21222 định dạng PointExpiryDTime sang yyyy-MM-dd
            PointExpiryDTime = cct.PointExpiryDTime.HasValue ? cct.PointExpiryDTime.Value.ToString("yyyy-MM-dd") : null,
            AmountChTotalInit = cct.AmountChTotalInit,
            AmountChTotalDiffer = cct.AmountChTotalDiffer,
            AmountAvailPrevious = cct.AmountAvailPrevious,
            PointExpiryDTimeSync = cct.PointExpiryDTimeSync,
            PointChTotalInit = cct.PointChTotalInit,
            PointChTotalDiffer = cct.PointChTotalDiffer,
            PrProgramName = prProgramName,
            Remark = remark,
            Unit = unit,
            CarNo = carNo,
            PhoneNo = phoneNo,
            MemberName = memberName,
            DLCodeExceptionally = dlCodeExceptionally,
            DiscountRate = 0d,
            DiscountAmount = 0d,
            DiscountRevenue = 0d
        };
    }

    private static Crd_CardTransactionSearchResult MapDiscountToResult(
        CrdMemberDiscountTransaction mvt,
        string? carNo,
        string? phoneNo,
        string? memberName)
    {
        return new Crd_CardTransactionSearchResult
        {
            AutoId = mvt.AutoId,
            RefNo = mvt.RefNo,
            ProjectCode = null,
            NetworkID = mvt.NetworkID,
            OrgID = mvt.OrgID,
            DLCode = mvt.DLCode,
            MemberNo = mvt.MemberNo,
            CardNo = mvt.CardNo,
            CardSourceCode = null,
            CardTypeUse = mvt.CardTypeUse,
            CardTypeInit = mvt.CardTypeInit,
            DealPointType = LoyaltyConst.DealPointType.DiscountRO,
            PrProgramCode = null,
            EffDateStart = mvt.EffDateStart,
            EffDateEnd = mvt.EffDateEnd,
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
            PolicyPointUnit = 0d,
            PolicyAmountUnit = 0d,
            PolicyCode = mvt.PolicyCode,
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
            RefCode00 = mvt.RefCode00,
            RefCode01 = mvt.RefCode01,
            RefCode02 = mvt.RefCode02,
            RefCode03 = mvt.RefCode03,
            RefCode04 = mvt.RefCode04,
            RefCode05 = mvt.RefCode05,
            RefCode06 = null,
            RefCode07 = null,
            RefCode08 = null,
            RefCode09 = null,
            CreateDTimeUTC = mvt.CreateDTimeUTC.HasValue ? mvt.CreateDTimeUTC.Value.AddHours(7).ToString("yyyy-MM-dd") : null,
            CreateBy = mvt.CreateBy,
            LUDTimeUTC = mvt.LUDTimeUTC,
            LUBy = mvt.LUBy,
            LogLUDTimeUTC = mvt.LogLUDTimeUTC,
            LogLUBy = mvt.LogLUBy,
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
            CarNo = carNo,
            PhoneNo = phoneNo,
            MemberName = memberName,
            DLCodeExceptionally = mvt.DLCodeExceptionally,
            DiscountRate = mvt.PolicyDiscountRate ?? 0d,
            DiscountAmount = mvt.PointChTotal ?? 0d,
            DiscountRevenue = mvt.AmountForDC ?? 0d
        };
    }

    /// <summary>
    /// Từ điển cột xuất Excel: gate Crd_CardTransactionController.cs:399-418 (GetImportDicColums).
    /// Giữ đúng thứ tự 14 cột từ nguồn:
    /// 1. RefNo ("Số giao dịch")
    /// 2. DLCode ("Đại lý thực hiện")
    /// 3. CreateDTimeUTC ("Ngày thực hiện")
    /// 4. MemberNo ("Mã thẻ hội viên")
    /// 5. MemberName ("Tên hội viên")
    /// 6. CardTypeUse ("Hạng sử dụng")
    /// 7. PhoneNo ("Số điện thoại")
    /// 8. CarNo ("Biển số xe")
    /// 9. CardNo ("Mã kỳ xét hạng")
    /// 10. PrProgramCode ("Mã chương trình")
    /// 11. PrProgramName ("Tên chương trình")
    /// 12. Remark ("Mô tả")
    /// 13. QtyPrChTotal ("Số lượng sử dụng")
    /// 14. Unit ("Đơn vị tính")
    /// </summary>
    private static Dictionary<string, string> GetImportDicColums()
    {
        return new Dictionary<string, string>
        {
            { "RefNo", "Số giao dịch" },
            { "DLCode", "Đại lý thực hiện" },
            { "CreateDTimeUTC", "Ngày thực hiện" },
            { "MemberNo", "Mã thẻ hội viên" },
            { "MemberName", "Tên hội viên" },
            { "CardTypeUse", "Hạng sử dụng" },
            { "PhoneNo", "Số điện thoại" },
            { "CarNo", "Biển số xe" },
            { "CardNo", "Mã kỳ xét hạng" },
            { "PrProgramCode", "Mã chương trình" },
            { "PrProgramName", "Tên chương trình" },
            { "Remark", "Mô tả" },
            { "QtyPrChTotal", "Số lượng sử dụng" },
            { "Unit", "Đơn vị tính" }
        };
    }
}
