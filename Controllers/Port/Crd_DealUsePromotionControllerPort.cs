using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MiniLoyalty.Data;
using MiniLoyalty.Models;
using MiniLoyalty.Models.Port;

namespace MiniLoyalty.Controllers.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — Crd_DealUsePromotionController (Save #022, Search #023).
//
// Nguồn tra cứu (chỉ đọc):
//   - Gate:     13.ClientGate/V10/idn.Loyalty.WebAPI/Controllers/Crd_DealUsePromotionController.cs:32-120 (Save)
//               Crd_DealUsePromotionController.cs:124-203 (Search), :460-477 (strWhereClause_Crd_Card)
//   - API:      11.BackEnd/V10/idn.Loyalty.Biz.Web/Controllers/CrdDealUsePromotionController.cs:23-93 (Save)
//               CrdDealUsePromotionController.cs:169-239 (Search WA_Crd_Card_GetForPromotion)
//   - Biz LIVE: 11.BackEnd/V10/idn.Loyalty.Biz/Master.Loyalty.cs:16214-16309 (WAS_Crd_DealUsePromotion_Save)
//               Master.Loyalty.cs:16061-16212 (Crd_DealUsePromotion_Save)
//               Master.Loyalty.cs:15549-16059 (Crd_DealUsePromotion_SaveX)
//               Transaction.AddPoint.cs:1072-1484 (Crd_DealUsePromotion_PerformX)
//               Card.cs:13271-13339 (Crd_Member_UpdateDTime)
//               Master.Loyalty.cs:16790-16903 (WAS_Crd_Card_GetForPromotion)
//               Master.Loyalty.cs:16904-17062 (Crd_Card_GetForPromotion)
//               Master.Loyalty.cs:17063-17460 (Crd_Card_GetForPromotionX)
// ===========================================================================================

/// <summary>
/// DTO yêu cầu lưu thông tin giao dịch sử dụng ưu đãi Crd_DealUsePromotion/Save.
/// </summary>
public class Crd_DealUsePromotionSaveReq
{
    public Crd_DealUsePromotionInput? Crd_DealUsePromotion { get; set; }
    public List<Crd_DealUsePromotionDtlInput>? Lst_Crd_DealUsePromotionDtl { get; set; }
}

public class Crd_DealUsePromotionInput
{
    public string? DealUsePrmNo { get; set; }
    public string? DLCPCode { get; set; }
    public string? UsePrmDTime { get; set; }
    public string? CardNo { get; set; }
    public string? CardType { get; set; }
}

public class Crd_DealUsePromotionDtlInput
{
    public string? DealUsePrmNo { get; set; }
    public string? PrProgramCode { get; set; }
    public double? QtyPr { get; set; }
    public double? QtyPrUsed { get; set; }
    public double? QtyPrChTotal { get; set; }
}

/// <summary>
/// DTO yêu cầu tìm kiếm thẻ hội viên để sử dụng ưu đãi Crd_DealUsePromotion/Search (#023).
/// Nguồn gate: Crd_DealUsePromotionController.cs:124.
/// </summary>
public class Crd_DealUsePromotionSearchReq
{
    public string? PhoneNo { get; set; }
    public string? MemberNo { get; set; }
    public int Ft_PageIndex { get; set; } = 0;
    public int Ft_PageSize { get; set; } = 10;
}

/// <summary>
/// DTO thông tin thẻ hội viên trả về từ Crd_DealUsePromotion/Search (#023).
/// Chứa đầy đủ thuộc tính từ Crd_Card và Crd_Member theo model nguồn Crd_Card.cs:1-120.
/// </summary>
public class Crd_DealUsePromotionSearchItem
{
    public long? MyIdxSeq { get; set; }

    // Crd_Card fields (Crd_Card.cs:11-54)
    public string? CardNo { get; set; }
    public string? CardNoPrev { get; set; }
    public string? NetworkID { get; set; }
    public string? MemberNo { get; set; }
    public string? DLCodeExceptionally { get; set; }
    public string? CardSourceCode { get; set; }
    public string? RankPolicyCode { get; set; }
    public double? PointBonus { get; set; }
    public double? PointPurchaseAddCar { get; set; }
    public DateTime? EffDateStart { get; set; }
    public DateTime? EffDateEnd { get; set; }
    public double? PointTotal { get; set; }
    public double? PointBlock { get; set; }
    public double? PointAvail { get; set; }
    public double? AmountTotal { get; set; }
    public double? AmountBlock { get; set; }
    public double? AmountAvail { get; set; }
    public double? QtyVisitTotal { get; set; }
    public double? QtyVisitBlock { get; set; }
    public double? QtyVisitAvail { get; set; }
    public double? PointCardRank { get; set; }
    public double? TotalAmountPeriod { get; set; }
    public string? CardTypeUse { get; set; }
    public string? CardTypeInit { get; set; }
    public string? CardTypeUsePrev { get; set; }
    public string? CardTypeInitPrev { get; set; }
    public DateTime? CardActiveDate { get; set; }
    public string? FlagExceptionally { get; set; }
    public string? RankActionType { get; set; }
    public string? CardTypeUseExpect { get; set; }
    public string? CardTypeInitExpect { get; set; }
    public DateTime? CreateDTimeUTC { get; set; }
    public string? CreateBy { get; set; }
    public DateTime? LUDTimeUTC { get; set; }
    public string? LUBy { get; set; }
    public DateTime? ApproveDTimeUTC { get; set; }
    public string? ApproveBy { get; set; }
    public DateTime? CancelDTimeUTC { get; set; }
    public string? CancelBy { get; set; }
    public string? CardStatus { get; set; }
    public string? Remark { get; set; }
    public DateTime? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }

    // Crd_Member fields (Crd_Card.cs:56-120)
    public string? MemberName { get; set; }
    public string? MemberStatus { get; set; }
    public string? IDCardNo { get; set; }
    public string? MemberGender { get; set; }
    public string? MemAddress { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? PhoneNo { get; set; }
    public string? CarNo { get; set; }
    public string? VIN { get; set; }
    public string? ModelCode { get; set; }
    public string? ModelName { get; set; }
    public string? BrandName { get; set; }
    public string? ProvinceCode { get; set; }
    public string? ProvinceName { get; set; }
    public string? DistrictCode { get; set; }
    public string? DistrictName { get; set; }
    public DateTime? WarrantyExpiryDate { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public DateTime? MemberActiveDate { get; set; }
    public DateTime? InactiveDTimeUTC { get; set; }
    public double? PointBegin { get; set; }
    public double? PointEnd { get; set; }
    public double? RevenueMiss { get; set; }
    public double? QtyVisitMiss { get; set; }
    public string? cc_FlagShow { get; set; }
    public string? cm_FlagShow { get; set; }
    public double? AmountEnd { get; set; }
    public string? mdcp_DLCPCode { get; set; }
    public string? mdcp_DLCPName { get; set; }
    public string? mcm_ModelName { get; set; }
    public double? PointExpiry { get; set; }
    public double? AmountRef { get; set; }
    public double? ConvertValue { get; set; }
    public double? ConvertPoint { get; set; }
    public double? PolicyAmountUnit { get; set; }
    public double? PolicyPointUnit { get; set; }
    public string? PolicyCode { get; set; }
    public string? ParamCode { get; set; }
    public string? ParamPointUnit { get; set; }
    public string? ParamAmountUnit { get; set; }
    public double? PointAccumulated { get; set; }
    public double? PointUse { get; set; }
    public string? TradeMarkCode { get; set; }
    public string? DLCodeRequest { get; set; }
    public double? PointVoucher { get; set; }
    public string? PurchaseType { get; set; }
    public string? PurchaseTypeName { get; set; }
}

/// <summary>
/// DTO yêu cầu lấy thông tin ưu đãi và giao dịch cho hội viên Crd_DealUsePromotion/GetForMemberNo (#024).
/// Nguồn gate: Crd_DealUsePromotionController.cs:208.
/// </summary>
public class Crd_DealUsePromotionGetForMemberNoReq
{
    public string? CardNo { get; set; }
    public string? IDCardNo { get; set; }
    public string? UsePrmDTimeFrom { get; set; }
    public string? UsePrmDTimeTo { get; set; }
    public string? DLCode { get; set; }
    public string? MemberNo { get; set; }
    public string? VIN { get; set; }
    public string? PhoneNo { get; set; }
}

/// <summary>
/// DTO chi tiết chương trình ưu đãi trả về từ GetForMemberNo.
/// Nguồn model: 12.Dev.Common/idn.Loyalty.Common/Models/Crd_DealUsePromotionDtl.cs
/// Nguồn SQL: Master.Loyalty.cs:15330-15426.
/// </summary>
public class Crd_DealUsePromotionGetForMemberNoDtlItem
{
    public string? IDCardNo { get; set; }
    public string? CardNo { get; set; }
    public string? DealUsePrmNo { get; set; }
    public string? PrProgramCode { get; set; }
    public string? PrProgramName { get; set; }
    public DateTime? EffDateStart { get; set; }
    public string? ListDealer { get; set; }
    public string? FlagShow { get; set; }
    public string? Remark { get; set; }
    public double? QtyPr { get; set; }
    public double? QtyPrUsed { get; set; }
    public double? QtyRemain { get; set; }
    public double? QtyPrChTotal { get; set; }
    public string? Unit { get; set; }
}

/// <summary>
/// DTO thông tin giao dịch trả về từ GetForMemberNo.
/// Nguồn model: 12.Dev.Common/idn.Loyalty.Common/Models/Loyalty/Crd_CardTransaction.cs
/// Nguồn SQL: Master.Loyalty.cs:15440-15456.
/// </summary>
public class Crd_DealUsePromotionGetForMemberNoTxItem
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
    public string? UsePrmDTime { get; set; }            // Định dạng chuỗi yyyy-MM-dd theo Master.Loyalty.cs:14701
    public string? PrProgramCode { get; set; }
    public DateTime? EffDateStart { get; set; }
    public DateTime? EffDateEnd { get; set; }
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
    public string? CreateDTimeUTC { get; set; }         // Định dạng chuỗi yyyy-MM-dd theo Master.Loyalty.cs:14700
    public string? CreateBy { get; set; }
    public DateTime? LUDTimeUTC { get; set; }
    public string? LUBy { get; set; }
    public DateTime? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
    public string? PrProgramName { get; set; }          // Cột bổ sung từ Mst_PromotionProgram
    public string? Unit { get; set; }                   // Cột bổ sung từ Mst_PromotionProgramDtl
    public string? Remark { get; set; }                 // Cột bổ sung từ Mst_PromotionProgramDtl
    public string? CarNo { get; set; }
    public string? PhoneNo { get; set; }
    public string? MemberName { get; set; }
    public DateTime? PointExpiryDTime { get; set; }
    public double? PointChTotalInit { get; set; }
    public double? AmountChTotalInit { get; set; }
    public double? PointChTotalDiffer { get; set; }
    public double? AmountChTotalDiffer { get; set; }
    public double? AmountAvailPrevious { get; set; }
    public string? DLCodeExceptionally { get; set; }
    public double DiscountRate { get; set; }
    public double DiscountAmount { get; set; }
    public double DiscountRevenue { get; set; }
}

[ApiController]
[Route("api/crd_dealusepromotion")]
public class Crd_DealUsePromotionControllerPort : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public Crd_DealUsePromotionControllerPort(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    /// <summary>
    /// Chuẩn hoá mã tham số: Trim + ToUpper, rỗng -> null (TUtils.CUtils.StdParam).
    /// </summary>
    private static string? StdParam(object? val)
    {
        var s = Convert.ToString(val)?.Trim();
        return string.IsNullOrEmpty(s) ? null : s.ToUpperInvariant();
    }

    /// <summary>
    /// Chuẩn hoá chuỗi thời gian yyyy-MM-dd HH:mm:ss (TUtils.CUtils.StdDTime).
    /// </summary>
    private static string? StdDTime(object? val)
    {
        if (val == null) return null;
        if (val is DateTime dt) return dt.ToString("yyyy-MM-dd HH:mm:ss");
        var s = Convert.ToString(val)?.Trim();
        if (string.IsNullOrEmpty(s)) return null;
        if (DateTime.TryParse(s, out var parsed)) return parsed.ToString("yyyy-MM-dd HH:mm:ss");
        return s;
    }

    /// <summary>
    /// Port 1:1 action Save — Ghi nhận sử dụng ưu đãi Crd_DealUsePromotion.
    /// Nguồn: Crd_DealUsePromotionController.cs:32-120 (gate), Master.Loyalty.cs:15549-16309 (biz).
    /// </summary>
    [HttpPost("save")]
    public async Task<IActionResult> Save([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Crd_DealUsePromotionSaveReq? req)
    {
        // 1. Kiểm tra body rỗng / null (Gate Crd_DealUsePromotionController.cs:41-48, Bài học A.15, A.21)
        if (req == null || req.Crd_DealUsePromotion == null)
        {
            return PortResult.Fail("Crd_DealUsePromotion_Save", "Check.strJson", null);
        }

        var input = req.Crd_DealUsePromotion;

        // 2. Sinh Sequence DealUsePrmNo trước khi mở transaction (Gate:65-86, Seq.cs:147, Bài học A.15)
        long seq = await PortSeq.NextAsync<Seq_DealUsePrmNo>(_db);
        string generatedDealUsePrmNo = PortSeq.Build("{0}DEALUSEPRMNO.{3}.{1:00000}{2}", "", seq, 100000, "", DateTime.UtcNow);
        input.DealUsePrmNo = generatedDealUsePrmNo;

        // 3. Chuẩn hoá tham số đầu vào (Master.Loyalty.cs:15598-15602)
        string strDealUsePrmNo = StdParam(generatedDealUsePrmNo) ?? "";
        string? strDLCPCode = StdParam(input.DLCPCode);
        string? strUsePrmDTime = StdDTime(input.UsePrmDTime);
        string? strCardNo = StdParam(input.CardNo);
        string? strCardType = StdParam(input.CardType);

        DateTime dtimeSys = DateTime.UtcNow;
        DateTime? parsedUsePrmDTime = null;
        if (!string.IsNullOrEmpty(strUsePrmDTime) && DateTime.TryParse(strUsePrmDTime, out var dtParsed))
        {
            parsedUsePrmDTime = dtParsed;
        }

        // 4. Mở 1 transaction duy nhất cho toàn bộ quá trình lưu (Master.Loyalty.cs:16099)
        await using var tx = await _db.Database.BeginTransactionAsync();

        try
        {
            // Guard 1: Crd_DealUsePromotion_CheckDB (Master.Loyalty.cs:15609, :14547-14611)
            // Kiểm tra mã giao dịch chưa tồn tại với FlagExist = "0"
            var existingDeal = await _db.Set<Crd_DealUsePromotion>().AsNoTracking()
                .FirstOrDefaultAsync(x => EF.Functions.Like((x.DealUsePrmNo ?? "").ToLower(), PortQuery.Equal(strDealUsePrmNo), PortQuery.Esc));
            if (existingDeal != null)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Crd_DealUsePromotion_CheckDB_DealUsePrmNoExist", "Check.DealUsePrmNo", strDealUsePrmNo);
            }

            // Guard 2: Crd_Card_CheckDB (Master.Loyalty.cs:15617, Card.cs:21229-21340)
            // Kiểm tra CardNo tồn tại với FlagExist = "1"
            var card = await _db.CrdCards.AsNoTracking()
                .FirstOrDefaultAsync(x => EF.Functions.Like((x.CardNo ?? "").ToLower(), PortQuery.Equal(strCardNo ?? ""), PortQuery.Esc));
            if (card == null)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Crd_Card_CheckDB_CardNotFound", "Check.CardNo", strCardNo);
            }

            // Kiểm tra CardStatus thuộc "APPROVE"
            if (!string.Equals(card.CardStatus, "APPROVE", StringComparison.OrdinalIgnoreCase))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Crd_Card_CheckDB_CardStatusNotMatched",
                    "Check.CardNo", strCardNo,
                    "Check.strCardStatusListToCheck", "APPROVE",
                    "DB.CardStatus", card.CardStatus);
            }

            // Kiểm tra thời hạn thẻ: EffDateStart > SysDate, EffDateEnd < SysDate (Card.cs:21319-21340)
            var sysDateStr = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var effDateStartStr = card.EffDateStart?.ToString("yyyy-MM-dd");
            var effDateEndStr = card.EffDateEnd?.ToString("yyyy-MM-dd");

            if (!string.IsNullOrEmpty(effDateStartStr) && effDateStartStr.CompareTo(sysDateStr) > 0)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Crd_Card_CheckDB_InvalidEffDateStartAfterSysDate",
                    "Check.EffDateStart", effDateStartStr,
                    "Check.SysDate", sysDateStr);
            }

            if (!string.IsNullOrEmpty(effDateEndStr) && effDateEndStr.CompareTo(sysDateStr) < 0)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Crd_Card_CheckDB_InvalidEffDateEndBeforeSysDate",
                    "Check.EffDateEnd", effDateEndStr,
                    "Check.SysDate", sysDateStr);
            }

            // Guard 3: Mst_DealerCP_CheckDB (Master.Loyalty.cs:15625, :2870-2930)
            // Kiểm tra DLCPCode tồn tại với FlagExist = "1"
            var dealer = await _db.Set<Mst_DealerCP>().AsNoTracking()
                .FirstOrDefaultAsync(x => EF.Functions.Like((x.DLCPCode ?? "").ToLower(), PortQuery.Equal(strDLCPCode ?? ""), PortQuery.Esc));
            if (dealer == null)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_DealerCP_CheckDB_DLNotFound", "Check.DLCPCode", strDLCPCode);
            }

            // Kiểm tra FlagActive == "1"
            if (!string.Equals(dealer.FlagActive, "1", StringComparison.OrdinalIgnoreCase))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_DealerCP_CheckDB_FlagActiveNotMatched",
                    "Check.DLCPCode", strDLCPCode,
                    "Check.strFlagActiveListToCheck", "1",
                    "DB.FlagActive", dealer.FlagActive);
            }

            // Guard 4: Kiểm tra tồn tại table detail Crd_DealUsePromotionDtl (Master.Loyalty.cs:15676-15686)
            if (req.Lst_Crd_DealUsePromotionDtl == null)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Crd_DealUsePromotion_SaveX_Input_Crd_DealUsePromotionDtlTblNotFound", "Check.TableName", "Crd_DealUsePromotionDtl");
            }

            var networkId = _config["Biz_NetworkID"] ?? card.NetworkID ?? "1001";
            var orgId = _config["Biz_OrgID"] ?? "1001";
            var waUserCode = Request.Headers["WAUserCode"].FirstOrDefault() ?? "port";

            var detailEntities = new List<Crd_DealUsePromotionDtl>();

            // Guard 5: Duyệt từng dòng chi tiết (Master.Loyalty.cs:15714-15750)
            for (int nScan = 0; nScan < req.Lst_Crd_DealUsePromotionDtl.Count; nScan++)
            {
                var dtl = req.Lst_Crd_DealUsePromotionDtl[nScan];
                var strPrProgramCode = StdParam(dtl.PrProgramCode);
                if (string.IsNullOrEmpty(strPrProgramCode))
                {
                    await tx.RollbackAsync();
                    return PortResult.Fail("Mst_PromotionProgram_CheckDB_PrProgramCodeNotFound", "Check.PrProgramCode", null);
                }

                // Mst_PromotionProgram_CheckDB (Master.Loyalty.cs:15720, :13092-13178)
                var program = await _db.Set<Mst_PromotionProgram>().AsNoTracking()
                    .FirstOrDefaultAsync(x => EF.Functions.Like((x.PrProgramCode ?? "").ToLower(), PortQuery.Equal(strPrProgramCode), PortQuery.Esc));
                if (program == null)
                {
                    await tx.RollbackAsync();
                    return PortResult.Fail("Mst_PromotionProgram_CheckDB_PrProgramCodeNotFound", "Check.PrProgramCode", strPrProgramCode);
                }

                if (!string.Equals(program.FlagActive, "1", StringComparison.OrdinalIgnoreCase))
                {
                    await tx.RollbackAsync();
                    return PortResult.Fail("Mst_PromotionProgram_CheckDB_FlagActiveNotMatched",
                        "Check.PrProgramCode", strPrProgramCode,
                        "Check.strFlagActiveListToCheck", "1",
                        "DB.FlagActive", program.FlagActive);
                }

                // FlagShow: EffDateStart <= dateSys <= EffDateEnd (Master.Loyalty.cs:13107-13109, :13165-13177)
                // Lỗi copy-paste giữ nguyên: Mst_BirthPolicy_CheckDB_FlagShowNotMatched (Bài học A.13)
                var dateSys = DateTime.UtcNow.AddHours(7);
                bool flagShow = program.EffDateStart <= dateSys && dateSys <= program.EffDateEnd;
                if (!flagShow)
                {
                    await tx.RollbackAsync();
                    return PortResult.Fail("Mst_BirthPolicy_CheckDB_FlagShowNotMatched",
                        "Check.BirthPolicyNo", strPrProgramCode,
                        "Check.strFlagShowListToCheck", "1",
                        "DB.FlagShow", "0");
                }

                // Kiểm tra QtyPrChTotal <= 0 (Master.Loyalty.cs:15730-15740)
                double dblQtyPrChTotal = dtl.QtyPrChTotal ?? 0.0;
                if (dblQtyPrChTotal <= 0)
                {
                    await tx.RollbackAsync();
                    return PortResult.Fail("Crd_DealUsePromotion_Save_Input_InvalidQtyPrChTotal",
                        "Check.Error", "QtyPrChTotal <= 0",
                        "PrProgramCode", dtl.QtyPrChTotal);
                }

                detailEntities.Add(new Crd_DealUsePromotionDtl
                {
                    DealUsePrmNo = strDealUsePrmNo,
                    PrProgramCode = strPrProgramCode,
                    NetworkID = networkId,
                    OrgID = orgId,
                    QtyPr = dtl.QtyPr,
                    QtyPrUsed = dtl.QtyPrUsed,
                    QtyPrChTotal = dblQtyPrChTotal,
                    LogLUDTimeUTC = dtimeSys,
                    LogLUBy = waUserCode
                });
            }

            // 5. Ghi nhận vào DB trong transaction (Master.Loyalty.cs:15787-15862)
            var dealUsePromotion = new Crd_DealUsePromotion
            {
                DealUsePrmNo = strDealUsePrmNo,
                NetworkID = networkId,
                OrgID = orgId,
                DLCPCode = strDLCPCode,
                DealSerRONo = "",
                UsePrmDTime = parsedUsePrmDTime ?? dtimeSys,
                ProjectCode = "LOYALTY",
                CardNo = strCardNo,
                CardType = strCardType ?? card.CardTypeUse,
                LogLUDTimeUTC = dtimeSys,
                LogLUBy = waUserCode
            };

            _db.Set<Crd_DealUsePromotion>().Add(dealUsePromotion);
            _db.Set<Crd_DealUsePromotionDtl>().AddRange(detailEntities);
            await _db.SaveChangesAsync();

            // Guard 6: Kiểm tra hạn mức theo hạng thẻ (Master.Loyalty.cs:15865-15916)
            // mppd.Qty < f.TotalQtyPrChTotal
            var effectiveCardType = strCardType ?? card.CardTypeUse ?? "";
            var distinctProgramCodes = detailEntities.Select(x => x.PrProgramCode).Distinct().ToList();

            foreach (var pCode in distinctProgramCodes)
            {
                var totalQtyPrChTotal = await (
                    from cdup in _db.Set<Crd_DealUsePromotion>()
                    join cdupl in _db.Set<Crd_DealUsePromotionDtl>() on cdup.DealUsePrmNo equals cdupl.DealUsePrmNo
                    where cdup.CardNo == strCardNo && cdupl.PrProgramCode == pCode
                    select (double?)cdupl.QtyPrChTotal
                ).SumAsync() ?? 0.0;

                var mppd = await _db.Set<Mst_PromotionProgramDtl>().AsNoTracking()
                    .FirstOrDefaultAsync(x => x.PrProgramCode == pCode && x.CardType == effectiveCardType);

                if (mppd != null && mppd.Qty.HasValue && mppd.Qty.Value < totalQtyPrChTotal)
                {
                    await tx.RollbackAsync();
                    return PortResult.Fail("Crd_DealUsePromotion_Save_Input_InvalidQtyPrChTotal",
                        "Check.Error", "mppd.Qty < f.TotalQtyPrChTotal",
                        "PrProgramCode", pCode);
                }
            }

            // Guard 7: Nâng cấp check luật trong 1 ngày + 1 chương trình + 1 Mã hội viên (Master.Loyalty.cs:15919-16004)
            // Nếu tổng QtyPrChTotal trong ngày > 1 ném ErridnLoyalty.Crd_DealUsePromotion_Save_Input_QtyPrChTotalOver
            var dealDate = (parsedUsePrmDTime ?? dtimeSys).Date;
            var memberNo = card.MemberNo;

            foreach (var dtl in detailEntities)
            {
                var pCode = dtl.PrProgramCode;

                var sameDayDeals = await (
                    from cdup in _db.Set<Crd_DealUsePromotion>()
                    join cc in _db.CrdCards on cdup.CardNo equals cc.CardNo
                    join cdupd in _db.Set<Crd_DealUsePromotionDtl>() on cdup.DealUsePrmNo equals cdupd.DealUsePrmNo
                    where cc.MemberNo == memberNo
                       && cdupd.PrProgramCode == pCode
                       && cdup.UsePrmDTime.HasValue
                       && cdup.UsePrmDTime.Value.Date == dealDate
                    select new
                    {
                        cdup.UsePrmDTime,
                        cc.MemberNo,
                        cdupd.PrProgramCode,
                        cdup.DLCPCode,
                        QtyPrChTotal = cdupd.QtyPrChTotal ?? 0.0
                    }
                ).ToListAsync();

                var sumQty = sameDayDeals.Sum(x => x.QtyPrChTotal);
                if (sumQty > 1.0)
                {
                    await tx.RollbackAsync();
                    return PortResult.Fail("Crd_DealUsePromotion_Save_Input_QtyPrChTotalOver",
                        "Check.Error", "mppd.QtyPrChTotal > 1",
                        "MemberNo", memberNo,
                        "PrProgramCode", pCode,
                        "UsePrmDTime", strUsePrmDTime,
                        "DLCPCode", strDLCPCode,
                        "QtyPrChTotal", sumQty);
                }
            }

            // 6. Side-effect 1: Crd_DealUsePromotion_PerformX (Transaction.AddPoint.cs:1072-1484)
            // Thêm giao dịch tích điểm ưu đãi vào Crd_CardTransaction
            var paramSys = await _db.Set<Mst_ParamSys>().AsNoTracking()
                .FirstOrDefaultAsync(x => x.ParamCode == "UNITPOINTTOMONEY");
            double paramAmountUnit = 1.0;
            if (paramSys != null && double.TryParse(paramSys.ParamValue, out var pVal))
            {
                paramAmountUnit = pVal;
            }

            foreach (var dtl in detailEntities)
            {
                var cardTx = new CrdCardTransaction
                {
                    RefNo = strDealUsePrmNo,
                    ProjectCode = "LOYALTY",
                    NetworkID = networkId,
                    OrgID = orgId,
                    DLCode = strDLCPCode,
                    MemberNo = card.MemberNo,
                    CardNo = strCardNo ?? "",
                    CardSourceCode = card.CardSourceCode,
                    CardTypeUse = card.CardTypeUse,
                    CardTypeInit = card.CardTypeInit,
                    DealPointType = LoyaltyConst.DealPointType.PrProgram,
                    PrProgramCode = dtl.PrProgramCode,
                    EffDateStart = card.EffDateStart,
                    EffDateEnd = card.EffDateEnd,
                    PointChTotal = 0.0,
                    PointChBlock = 0.0,
                    AmountChTotal = 0.0,
                    AmountChBlock = 0.0,
                    QtyVisitChTotal = 0.0,
                    QtyVisitChBlock = 0.0,
                    QtyPrChTotal = dtl.QtyPrChTotal,
                    QtyPrChBlock = 0.0,
                    QtyPrUsed = dtl.QtyPrUsed,
                    PointRef = 0.0,
                    AmountRef = 0.0,
                    PolicyPointUnit = 0.0,
                    PolicyAmountUnit = 0.0,
                    PolicyCode = "UNITPOINTTOMONEY",
                    ParamPointUnit = 1.0,
                    ParamAmountUnit = paramAmountUnit,
                    ParamCode = "UNITPOINTTOMONEY",
                    FlagViewUI = "1",
                    FlagPoint = "0",
                    FlagAmount = "0",
                    FlagPr = "1",
                    FunctionName = "Crd_DealUsePromotion_PerformX",
                    FunctionActionType = "ADD",
                    FunctionRemark = "Tích điểm ưu đãi",
                    RefType = "CRD_CARD",
                    UsePrmDTime = parsedUsePrmDTime ?? dtimeSys,
                    CreateDTimeUTC = dtimeSys,
                    CreateBy = waUserCode,
                    LUDTimeUTC = dtimeSys,
                    LUBy = waUserCode,
                    LogLUDTimeUTC = dtimeSys,
                    LogLUBy = waUserCode
                };
                _db.CrdCardTransactions.Add(cardTx);
            }

            // 7. Side-effect 2: Crd_Member_UpdateDTime (Card.cs:13271-13339)
            // Cập nhật LUDTimeUTC, LUBy, LogLUDTimeUTC, LogLUBy của hội viên trong Crd_Member
            var member = await _db.CrdMembers.FirstOrDefaultAsync(x => x.MemberNo == card.MemberNo);
            if (member != null)
            {
                member.LUDTimeUTC = dtimeSys;
                member.LUBy = waUserCode;
                member.LogLUDTimeUTC = dtimeSys;
                member.LogLUBy = waUserCode;
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            // Thành công trả về Success = true, Data = null (Gate :106-107)
            return Ok(new { Success = true, Data = (object?)null });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return PortResult.Fail("Crd_DealUsePromotion_Save", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Port 1:1 action Search — Tìm kiếm thẻ hội viên để sử dụng ưu đãi Crd_DealUsePromotion/Search (#023).
    /// Nguồn: Crd_DealUsePromotionController.cs:124-203 (gate), CrdDealUsePromotionController.cs:169-239 (API),
    /// Master.Loyalty.cs:16790-17460 (biz WAS_Crd_Card_GetForPromotion, Crd_Card_GetForPromotion, Crd_Card_GetForPromotionX).
    /// </summary>
    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Crd_DealUsePromotionSearchReq? req)
    {
        req ??= new Crd_DealUsePromotionSearchReq();

        // 1. Join Crd_Card và Crd_Member (Master.Loyalty.cs:17192-17193)
        // và Guard cứng CardStatus == 'APPROVE' (Master.Loyalty.cs:17196)
        var query = from cc in _db.CrdCards
                    join cm in _db.CrdMembers on cc.MemberNo equals cm.MemberNo
                    where cc.CardStatus == "APPROVE"
                    select new { cc, cm };

        // 2. Lọc PhoneNo trên Crd_Member với toán tử '=' nguyên chuỗi (Crd_DealUsePromotionController.cs:470-473)
        if (!string.IsNullOrWhiteSpace(req.PhoneNo))
        {
            var p_PhoneNo = PortQuery.Equal(req.PhoneNo);
            query = query.Where(x => EF.Functions.Like((x.cm.PhoneNo ?? "").ToLower(), p_PhoneNo, PortQuery.Esc));
        }

        // 3. Lọc MemberNo trên Crd_Member với toán tử '=' nguyên chuỗi (Crd_DealUsePromotionController.cs:466-469)
        if (!string.IsNullOrWhiteSpace(req.MemberNo))
        {
            var p_MemberNo = PortQuery.Equal(req.MemberNo);
            query = query.Where(x => EF.Functions.Like((x.cm.MemberNo ?? "").ToLower(), p_MemberNo, PortQuery.Esc));
        }

        // 4. Sắp xếp: order by cc.LogLUDTimeUTC desc, cc.CardNo desc (Master.Loyalty.cs:17197, :17246)
        var ordered = query.OrderByDescending(x => x.cc.LogLUDTimeUTC).ThenByDescending(x => x.cc.CardNo);

        // 5. Đếm tổng số bản ghi MyCount (#tbl_Crd_Card_Filter_Draft, Master.Loyalty.cs:17201)
        var itemCount = await ordered.CountAsync();

        // 6. Xử lý phân trang theo Bài học A.12 và A.7:
        var pageSize = req.Ft_PageSize;
        var pageIndex = req.Ft_PageIndex;
        var ps = pageSize <= 0 ? 10 : pageSize;
        var pageCount = (pageSize <= 0 || pageIndex < 0 || itemCount == 0) ? 0 : (itemCount % ps == 0 ? itemCount / ps : itemCount / ps + 1);

        List<Crd_DealUsePromotionSearchItem> dataList;

        if (pageSize <= 0 || pageIndex < 0 || itemCount == 0)
        {
            dataList = new List<Crd_DealUsePromotionSearchItem>();
        }
        else
        {
            long recordStart = (long)pageIndex * pageSize;
            long recordCount = pageSize;

            var rows = await ordered
                .Skip((int)Math.Min(recordStart, int.MaxValue))
                .Take((int)Math.Min(recordCount, int.MaxValue))
                .ToListAsync();

            dataList = rows.Select((x, idx) => MapToSearchItem(x.cc, x.cm, recordStart + idx)).ToList();
        }

        var pageInfo = new PageInfo<Crd_DealUsePromotionSearchItem>(
            dataList,
            itemCount,
            pageCount,
            pageIndex,
            pageSize
        );

        return Ok(new { Success = true, Data = pageInfo });
    }

    /// <summary>
    /// Lấy thông tin ưu đãi và giao dịch cho hội viên — port 1:1 Crd_DealUsePromotionController/GetForMemberNo (#024).
    /// Nguồn:
    ///   - Gate: Crd_DealUsePromotionController.cs:208-293 (GetForMemberNo), :479-529 (strWhereClause_Crd_Card_GetForMember)
    ///   - API: CrdDealUsePromotionController.cs:241-309 (CrdDealUsePromotion_GetForPromotion)
    ///   - Biz: Master.Loyalty.cs:14613-14736 (WAS_Crd_DealUsePromotion_GetForPromotion)
    ///          Master.Loyalty.cs:14737-14892 (Crd_DealUsePromotion_GetForPromotion)
    ///          Master.Loyalty.cs:15192-15548 (Crd_DealUsePromotion_GetForPromotionX - bản LIVE)
    /// </summary>
    [HttpPost("getformemberno")]
    public async Task<IActionResult> GetForMemberNo(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Crd_DealUsePromotionGetForMemberNoReq? req)
    {
        // 1. Phân tích khoảng thời gian lọc nếu có (Gate Crd_DealUsePromotionController.cs:495-509)
        DateTime? usePrmDTimeFrom = null;
        if (!string.IsNullOrWhiteSpace(req?.UsePrmDTimeFrom) && DateTime.TryParse(req.UsePrmDTimeFrom, out var dtFrom))
        {
            usePrmDTimeFrom = dtFrom.Date;
        }

        DateTime? usePrmDTimeTo = null;
        if (!string.IsNullOrWhiteSpace(req?.UsePrmDTimeTo) && DateTime.TryParse(req.UsePrmDTimeTo, out var dtTo))
        {
            // StdDTimeEndDay: 23:59:59 (Master.Loyalty.cs:14698, gate :503-509)
            usePrmDTimeTo = dtTo.Date.AddDays(1).AddTicks(-1);
        }

        // 2. Xây dựng bảng tạm Filter_Draft (Master.Loyalty.cs:15244-15267):
        //    select distinct cm.MemberNo, cm.IDCardNo, cm.CardTypeUse, cm.DLCodeRegis, cc.CardNo
        //    from Crd_Member cm left join Crd_CardTransaction cct on cm.MemberNo = cct.MemberNo
        //    where (1=1) zzB_Where_strFilter_zzE
        //    Toán tử lọc: IDCardNo, MemberNo, PhoneNo, VIN dùng = (PortQuery.Equal, không phân biệt hoa/thường trên SQLite).
        //    LƯU Ý NGUỒN: CardNo và DLCode bị comment out ở gate (:486-489, :510-513) nên KHÔNG lọc theo CardNo và DLCode!
        var memberQuery = _db.Set<CrdMember>().AsQueryable();

        if (!string.IsNullOrWhiteSpace(req?.IDCardNo))
        {
            var val = PortQuery.Equal(req.IDCardNo);
            memberQuery = memberQuery.Where(cm => EF.Functions.Like((cm.IDCardNo ?? "").ToLower(), val, PortQuery.Esc));
        }

        if (!string.IsNullOrWhiteSpace(req?.MemberNo))
        {
            var val = PortQuery.Equal(req.MemberNo);
            memberQuery = memberQuery.Where(cm => EF.Functions.Like((cm.MemberNo ?? "").ToLower(), val, PortQuery.Esc));
        }

        if (!string.IsNullOrWhiteSpace(req?.PhoneNo))
        {
            var val = PortQuery.Equal(req.PhoneNo);
            memberQuery = memberQuery.Where(cm => EF.Functions.Like((cm.PhoneNo ?? "").ToLower(), val, PortQuery.Esc));
        }

        if (!string.IsNullOrWhiteSpace(req?.VIN))
        {
            var val = PortQuery.Equal(req.VIN);
            memberQuery = memberQuery.Where(cm => EF.Functions.Like((cm.VIN ?? "").ToLower(), val, PortQuery.Esc));
        }

        // Nếu có lọc UsePrmDTimeFrom / UsePrmDTimeTo thì join cct (Master.Loyalty.cs:15263-15266, gate :495-509)
        if (usePrmDTimeFrom != null || usePrmDTimeTo != null)
        {
            var txFilterQuery = _db.Set<CrdCardTransaction>().AsQueryable();
            if (usePrmDTimeFrom != null)
            {
                txFilterQuery = txFilterQuery.Where(cct => cct.UsePrmDTime >= usePrmDTimeFrom.Value);
            }
            if (usePrmDTimeTo != null)
            {
                txFilterQuery = txFilterQuery.Where(cct => cct.UsePrmDTime <= usePrmDTimeTo.Value);
            }

            memberQuery = from cm in memberQuery
                          join cct in txFilterQuery on cm.MemberNo equals cct.MemberNo
                          select cm;
        }

        var filteredMembers = await memberQuery
            .Select(cm => new
            {
                cm.MemberNo,
                cm.IDCardNo,
                cm.CardTypeUse,
                cm.DLCodeRegis
            })
            .Distinct()
            .ToListAsync();

        if (filteredMembers.Count == 0)
        {
            // Không tìm thấy hội viên nào: trả về 2 mảng rỗng [] (Gate :230-231, :278-279)
            return Ok(new
            {
                Success = true,
                Data = new
                {
                    Lst_Crd_DealUsePromotionDtl = new List<Crd_DealUsePromotionGetForMemberNoDtlItem>(),
                    Lst_Crd_CardTransaction = new List<Crd_DealUsePromotionGetForMemberNoTxItem>()
                }
            });
        }

        // Lấy CardNo mới nhất cho từng hội viên (Master.Loyalty.cs:15252-15260: select top 1 cc.CardNo order by cc.CreateDTimeUTC desc)
        var memberNos = filteredMembers.Select(m => m.MemberNo).ToList();
        var cards = await _db.Set<CrdCard>()
            .Where(c => memberNos.Contains(c.MemberNo))
            .OrderByDescending(c => c.CreateDTimeUTC)
            .ToListAsync();

        var memberCardMap = cards
            .GroupBy(c => c.MemberNo)
            .ToDictionary(g => g.Key, g => g.First().CardNo);

        var filterDraft = filteredMembers.Select(m => new
        {
            m.MemberNo,
            m.IDCardNo,
            CardTypeUse = m.CardTypeUse ?? "",
            DLCodeRegis = m.DLCodeRegis ?? "",
            CardNo = memberCardMap.TryGetValue(m.MemberNo, out var cardNo) ? cardNo : ""
        }).ToList();

        // 3. Tính danh sách chương trình ưu đãi hợp lệ Lst_Crd_DealUsePromotionDtl (Master.Loyalty.cs:15298-15432)
        // 3a. Tính tổng số lượt đã sử dụng theo (PrProgramCode, CardType, CardNo) (Master.Loyalty.cs:15303-15325)
        var draftCardNos = filterDraft.Select(f => f.CardNo).Where(c => !string.IsNullOrEmpty(c)).Distinct().ToList();
        var draftCardTypes = filterDraft.Select(f => f.CardTypeUse).Where(ct => !string.IsNullOrEmpty(ct)).Distinct().ToList();

        var deals = await (
            from cdup in _db.Set<Crd_DealUsePromotion>()
            join cdupl in _db.Set<Crd_DealUsePromotionDtl>() on cdup.DealUsePrmNo equals cdupl.DealUsePrmNo
            where draftCardNos.Contains(cdup.CardNo ?? "") && draftCardTypes.Contains(cdup.CardType ?? "")
            select new
            {
                cdupl.PrProgramCode,
                CardType = cdup.CardType ?? "",
                CardNo = cdup.CardNo ?? "",
                QtyPrChTotal = cdupl.QtyPrChTotal ?? 0.0
            }
        ).ToListAsync();

        var totalUsedMap = deals
            .Where(d => filterDraft.Any(f => f.CardTypeUse == d.CardType && f.CardNo == d.CardNo))
            .GroupBy(d => new { d.PrProgramCode, d.CardType, d.CardNo })
            .ToDictionary(
                g => (g.Key.PrProgramCode, g.Key.CardType, g.Key.CardNo),
                g => g.Sum(x => x.QtyPrChTotal)
            );

        // 3b. Lấy chương trình khuyến mại còn hiệu lực theo ngày hệ thống UTC+7 (Master.Loyalty.cs:15365, :15410, :15428)
        var dateSys = DateTime.UtcNow.AddHours(7);

        var programs = await (
            from mpp in _db.Set<Mst_PromotionProgram>()
            join mppd in _db.Set<Mst_PromotionProgramDtl>() on mpp.PrProgramCode equals mppd.PrProgramCode
            where mpp.EffDateStart <= dateSys && mpp.EffDateEnd >= dateSys
                  && draftCardTypes.Contains(mppd.CardType)
            select new
            {
                mpp.PrProgramCode,
                mpp.PrProgramName,
                mpp.EffDateStart,
                mpp.FlagAllDL,
                mppd.CardType,
                Qty = mppd.Qty ?? 0.0,
                mppd.Unit,
                mppd.Remark
            }
        ).ToListAsync();

        // Lấy danh sách đại lý chỉ định trong Mst_PromotionProgramSpec (Master.Loyalty.cs:15341-15348, :15384-15391)
        var specProgramCodes = programs.Where(p => p.FlagAllDL != "1").Select(p => p.PrProgramCode).Distinct().ToList();
        var allSpecs = await _db.Set<Mst_PromotionProgramSpec>()
            .Where(s => specProgramCodes.Contains(s.PrProgramCode))
            .ToListAsync();

        var specDealerMap = allSpecs
            .GroupBy(s => s.PrProgramCode)
            .ToDictionary(
                g => g.Key,
                g => string.Join(",", g.Select(x => x.DLCode))
            );

        var promotionDtlList = new List<Crd_DealUsePromotionGetForMemberNoDtlItem>();

        // Tập 1: Áp dụng tất cả đại lý (FlagAllDL = '1') (Master.Loyalty.cs:15329-15367)
        foreach (var ccf in filterDraft)
        {
            var matchedPrograms = programs.Where(p => p.FlagAllDL == "1" && p.CardType == ccf.CardTypeUse);
            foreach (var p in matchedPrograms)
            {
                totalUsedMap.TryGetValue((p.PrProgramCode, p.CardType, ccf.CardNo), out var used);
                var remain = p.Qty - used;
                if (remain > 0)
                {
                    promotionDtlList.Add(new Crd_DealUsePromotionGetForMemberNoDtlItem
                    {
                        PrProgramCode = p.PrProgramCode,
                        PrProgramName = p.PrProgramName,
                        EffDateStart = p.EffDateStart,
                        Remark = null,
                        QtyPr = p.Qty,
                        QtyPrChTotal = 0.0,
                        QtyRemain = remain,
                        QtyPrUsed = used,
                        Unit = p.Unit,
                        ListDealer = "Tất cả"
                    });
                }
            }
        }

        // Tập 2: Chỉ áp dụng đại lý chỉ định (FlagAllDL != '1') (Master.Loyalty.cs:15372-15411)
        // Điều kiện: mpps.DLCode = ccf.DLCodeRegis
        foreach (var ccf in filterDraft)
        {
            var matchedPrograms = programs.Where(p => p.FlagAllDL != "1" && p.CardType == ccf.CardTypeUse);
            foreach (var p in matchedPrograms)
            {
                bool dealerMatched = allSpecs.Any(s => s.PrProgramCode == p.PrProgramCode && s.DLCode == ccf.DLCodeRegis);
                if (dealerMatched)
                {
                    totalUsedMap.TryGetValue((p.PrProgramCode, p.CardType, ccf.CardNo), out var used);
                    var remain = p.Qty - used;
                    if (remain > 0)
                    {
                        promotionDtlList.Add(new Crd_DealUsePromotionGetForMemberNoDtlItem
                        {
                            PrProgramCode = p.PrProgramCode,
                            PrProgramName = p.PrProgramName,
                            EffDateStart = p.EffDateStart,
                            Remark = null,
                            QtyPr = p.Qty,
                            QtyPrChTotal = 0.0,
                            QtyRemain = remain,
                            QtyPrUsed = used,
                            Unit = p.Unit,
                            ListDealer = specDealerMap.TryGetValue(p.PrProgramCode, out var dlList) ? dlList : ""
                        });
                    }
                }
            }
        }

        // Loại trùng chương trình (Master.Loyalty.cs:15330, :15372 select distinct)
        promotionDtlList = promotionDtlList
            .GroupBy(x => x.PrProgramCode)
            .Select(g => g.First())
            .ToList();

        // 4. Lấy danh sách giao dịch Lst_Crd_CardTransaction (Master.Loyalty.cs:15438-15456)
        //    from Crd_CardTransaction cct inner join Filter_Draft t on cct.MemberNo = t.MemberNo
        //    left join Mst_PromotionProgramDtl mppd on cct.PrProgramCode = mppd.PrProgramCode and cct.CardTypeUse = mppd.CardType
        //    inner join Mst_PromotionProgram mpp on mppd.PrProgramCode = mpp.PrProgramCode
        //    where cct.DealPointType = 'PRPROGRAM' (LoyaltyConst.DealPointType.PrProgram)
        var memberNoList = filterDraft.Select(f => f.MemberNo).Distinct().ToList();

        var txQuery = from cct in _db.Set<CrdCardTransaction>()
                      where memberNoList.Contains(cct.MemberNo)
                            && cct.DealPointType == LoyaltyConst.DealPointType.PrProgram
                      join mppd in _db.Set<Mst_PromotionProgramDtl>()
                          on new { cct.PrProgramCode, CardType = cct.CardTypeUse }
                          equals new { mppd.PrProgramCode, CardType = (string?)mppd.CardType }
                      join mpp in _db.Set<Mst_PromotionProgram>()
                          on mppd.PrProgramCode equals mpp.PrProgramCode
                      select new
                      {
                          cct,
                          mpp.PrProgramName,
                          mppd.Unit,
                          mppd.Remark
                      };

        var txRows = await txQuery.ToListAsync();

        var cardTransactionList = txRows
            .GroupBy(x => x.cct.AutoId)
            .Select(g =>
            {
                var row = g.First();
                var t = row.cct;
                return new Crd_DealUsePromotionGetForMemberNoTxItem
                {
                    AutoId = t.AutoId,
                    RefNo = t.RefNo,
                    ProjectCode = t.ProjectCode,
                    NetworkID = t.NetworkID,
                    OrgID = t.OrgID,
                    DLCode = t.DLCode,
                    MemberNo = t.MemberNo,
                    CardNo = t.CardNo,
                    CardSourceCode = t.CardSourceCode,
                    CardTypeUse = t.CardTypeUse,
                    CardTypeInit = t.CardTypeInit,
                    DealPointType = t.DealPointType,
                    UsePrmDTime = t.UsePrmDTime?.ToString("yyyy-MM-dd"),
                    PrProgramCode = t.PrProgramCode,
                    EffDateStart = t.EffDateStart,
                    EffDateEnd = t.EffDateEnd,
                    PointChTotal = t.PointChTotal,
                    PointChBlock = t.PointChBlock,
                    AmountChTotal = t.AmountChTotal,
                    AmountChBlock = t.AmountChBlock,
                    QtyVisitChTotal = t.QtyVisitChTotal,
                    QtyVisitChBlock = t.QtyVisitChBlock,
                    QtyPrChTotal = t.QtyPrChTotal,
                    QtyPrChBlock = t.QtyPrChBlock,
                    QtyPrUsed = t.QtyPrUsed,
                    PointRef = t.PointRef,
                    AmountRef = t.AmountRef,
                    PolicyPointUnit = t.PolicyPointUnit,
                    PolicyAmountUnit = t.PolicyAmountUnit,
                    PolicyCode = t.PolicyCode,
                    ParamPointUnit = t.ParamPointUnit,
                    ParamAmountUnit = t.ParamAmountUnit,
                    ParamCode = t.ParamCode,
                    FlagViewUI = t.FlagViewUI,
                    FlagPoint = t.FlagPoint,
                    FlagAmount = t.FlagAmount,
                    FlagPr = t.FlagPr,
                    FunctionName = t.FunctionName,
                    FunctionActionType = t.FunctionActionType,
                    FunctionRemark = t.FunctionRemark,
                    RefType = t.RefType,
                    PointChRankTotal = t.PointChRankTotal,
                    RefCode00 = t.RefCode00,
                    RefCode01 = t.RefCode01,
                    RefCode02 = t.RefCode02,
                    RefCode03 = t.RefCode03,
                    RefCode04 = t.RefCode04,
                    RefCode05 = t.RefCode05,
                    RefCode06 = t.RefCode06,
                    RefCode07 = t.RefCode07,
                    RefCode08 = t.RefCode08,
                    RefCode09 = t.RefCode09,
                    CreateDTimeUTC = t.CreateDTimeUTC?.ToString("yyyy-MM-dd"),
                    CreateBy = t.CreateBy,
                    LUDTimeUTC = t.LUDTimeUTC,
                    LUBy = t.LUBy,
                    LogLUDTimeUTC = t.LogLUDTimeUTC,
                    LogLUBy = t.LogLUBy,
                    PrProgramName = row.PrProgramName,
                    Unit = row.Unit,
                    Remark = row.Remark,
                    PointExpiryDTime = t.PointExpiryDTime,
                    PointChTotalInit = t.PointChTotalInit,
                    AmountChTotalInit = t.AmountChTotalInit,
                    PointChTotalDiffer = t.PointChTotalDiffer,
                    AmountChTotalDiffer = t.AmountChTotalDiffer,
                    AmountAvailPrevious = t.AmountAvailPrevious,
                    DLCodeExceptionally = t.DLCodeExceptionally
                };
            })
            .ToList();

        // 5. Trả phản hồi thành công (Gate Crd_DealUsePromotionController.cs:275-280)
        return Ok(new
        {
            Success = true,
            Data = new
            {
                Lst_Crd_DealUsePromotionDtl = promotionDtlList,
                Lst_Crd_CardTransaction = cardTransactionList
            }
        });
    }

    private static Crd_DealUsePromotionSearchItem MapToSearchItem(CrdCard cc, CrdMember cm, long? idx = null)
    {
        return new Crd_DealUsePromotionSearchItem
        {
            MyIdxSeq = idx,

            // Crd_Card fields (Crd_Card.cs:11-54)
            CardNo = cc.CardNo,
            CardNoPrev = cc.CardNoPrev,
            NetworkID = cc.NetworkID,
            MemberNo = cc.MemberNo,
            DLCodeExceptionally = cc.DLCodeExceptionally,
            CardSourceCode = cc.CardSourceCode,
            RankPolicyCode = cc.RankPolicyCode,
            PointBonus = cc.PointBonus,
            PointPurchaseAddCar = cc.PointPurchaseAddCar,
            EffDateStart = cc.EffDateStart,
            EffDateEnd = cc.EffDateEnd,
            PointTotal = cc.PointTotal,
            PointBlock = cc.PointBlock,
            PointAvail = cc.PointAvail,
            AmountTotal = cc.AmountTotal,
            AmountBlock = cc.AmountBlock,
            AmountAvail = cc.AmountAvail,
            QtyVisitTotal = cc.QtyVisitTotal,
            QtyVisitBlock = cc.QtyVisitBlock,
            QtyVisitAvail = cc.QtyVisitAvail,
            PointCardRank = cc.PointCardRank,
            TotalAmountPeriod = cc.TotalAmountPeriod,
            CardTypeUse = cc.CardTypeUse,
            CardTypeInit = cc.CardTypeInit,
            CardTypeUsePrev = cc.CardTypeUsePrev,
            CardTypeInitPrev = cc.CardTypeInitPrev,
            CardActiveDate = cc.CardActiveDate,
            FlagExceptionally = cc.FlagExceptionally,
            RankActionType = cc.RankActionType,
            CardTypeUseExpect = cc.CardTypeUseExpect,
            CardTypeInitExpect = cc.CardTypeInitExpect,
            CreateDTimeUTC = cc.CreateDTimeUTC,
            CreateBy = cc.CreateBy,
            LUDTimeUTC = cc.LUDTimeUTC,
            LUBy = cc.LUBy,
            ApproveDTimeUTC = cc.ApproveDTimeUTC,
            ApproveBy = cc.ApproveBy,
            CancelDTimeUTC = cc.CancelDTimeUTC,
            CancelBy = cc.CancelBy,
            CardStatus = cc.CardStatus,
            Remark = cc.Remark,
            LogLUDTimeUTC = cc.LogLUDTimeUTC,
            LogLUBy = cc.LogLUBy,

            // Crd_Member fields (Crd_Card.cs:56-120)
            MemberName = cm.MemberName,
            MemberStatus = cm.MemberStatus,
            IDCardNo = cm.IDCardNo,
            MemberGender = cm.MemberGender,
            MemAddress = cm.MemAddress,
            DateOfBirth = cm.DateOfBirth,
            PhoneNo = cm.PhoneNo,
            CarNo = cm.CarNo,
            VIN = cm.VIN,
            ModelCode = cm.ModelCode,
            ModelName = cm.ModelName,
            TradeMarkCode = cm.TradeMarkCode,
            ProvinceCode = cm.ProvinceCode,
            DistrictCode = cm.DistrictCode,
            WarrantyExpiryDate = cm.WarrantyExpiryDate,
            InvoiceDate = cm.InvoiceDate,
            MemberActiveDate = cm.MemberActiveDate,
            InactiveDTimeUTC = cm.InactiveDTimeUTC,
            PointBegin = cm.PointBegin,
            PointEnd = cm.PointEnd,
            AmountEnd = cm.AmountEnd,
            PointVoucher = cm.PointVoucher,
            PurchaseType = cm.PurchaseType,
            mdcp_DLCPCode = cm.DLCodeRegis,
            DLCodeRequest = cm.DLCodeRegis,
            mcm_ModelName = cm.ModelName
        };
    }
}
