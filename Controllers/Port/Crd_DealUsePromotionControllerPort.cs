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
