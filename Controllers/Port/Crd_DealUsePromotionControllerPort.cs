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
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — Crd_DealUsePromotionController/Save (#022).
//
// Nguồn tra cứu (chỉ đọc):
//   - Gate:     13.ClientGate/V10/idn.Loyalty.WebAPI/Controllers/Crd_DealUsePromotionController.cs:32-120
//   - API:      11.BackEnd/V10/idn.Loyalty.Biz.Web/Controllers/CrdDealUsePromotionController.cs:23-93
//   - Biz LIVE: 11.BackEnd/V10/idn.Loyalty.Biz/Master.Loyalty.cs:16214-16309 (WAS_Crd_DealUsePromotion_Save)
//               Master.Loyalty.cs:16061-16212 (Crd_DealUsePromotion_Save)
//               Master.Loyalty.cs:15549-16059 (Crd_DealUsePromotion_SaveX)
//               Transaction.AddPoint.cs:1072-1484 (Crd_DealUsePromotion_PerformX)
//               Card.cs:13271-13339 (Crd_Member_UpdateDTime)
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
}
