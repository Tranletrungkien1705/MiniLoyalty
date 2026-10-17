using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MiniLoyalty.Models;
using MiniLoyalty.Models.Port;

namespace MiniLoyalty.Data;

/// <summary>
/// Engine xử lý nghiệp vụ giao dịch thẻ và điểm — port 1:1 từ idn.Loyalty.Biz/Transaction.cs (:37-700)
/// và Transaction.AddPoint.cs (:146-2306, :2660-3035).
/// Được thiết kế thành service dùng chung để Crd_MemberController/Finish (#53), SaveAndApprove (#47),
/// và các module tích điểm sau tái sử dụng.
/// </summary>
public static class PortCardTransactionEngine
{
    /// <summary>
    /// Tính ngày cuối cùng của tháng này ở năm sau — port 1:1 từ TUtils.CUtils.StdDateEndMonthOfNextYear (Utils.cs:199-209).
    /// Ví dụ: 2026-10-17 -> 2027-10-31.
    /// </summary>
    public static DateTime GetDateEndMonthOfNextYear(DateTime d)
    {
        var firstDayOfMonth = new DateTime(d.Year, d.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        return firstDayOfMonth.AddYears(1).AddMonths(1).AddDays(-1);
    }

    /// <summary>
    /// Đọc tham số quy đổi điểm sang tiền (UNITPOINTTOMONEY) từ bảng Mst_ParamSys — Transaction.AddPoint.cs:321.
    /// Nếu chưa có thì null (nguồn inner join Mst_ParamSys => không sinh giao dịch).
    /// </summary>
    public static async Task<double?> GetUnitPointToMoneyAsync(AppDbContext db)
    {
        var p = await db.Set<Mst_ParamSys>().AsNoTracking().FirstOrDefaultAsync(x => x.ParamCode == "UNITPOINTTOMONEY");
        if (p != null && double.TryParse(p.ParamValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
        {
            return v;
        }
        return null;
    }

    /// <summary>
    /// Hàm trung tâm thực hiện giao dịch thẻ — port 1:1 từ Transaction.cs:37-697 (Crd_CardTransaction_Perform).
    /// Ghi Crd_CardTransaction, cập nhật điểm Crd_Card và Crd_Member, kiểm tra ngưỡng MinValue*, cập nhật PointCardRank.
    /// </summary>
    public static async Task Crd_CardTransaction_PerformAsync(
        AppDbContext db,
        List<CrdCardTransaction> transactions,
        DateTime createDTime,
        string createBy,
        double dblMinValueTotal = 0.0,
        double dblMinValueBlock = int.MinValue,
        double dblMinValueAvail = 0.0,
        bool bIsPointExpiryOrCancel = false)
    {
        if (transactions == null || transactions.Count == 0) return;

        // 1. Lưu Crd_CardTransaction (Transaction.cs:150-277)
        db.Set<CrdCardTransaction>().AddRange(transactions);

        // 2. Tính tổng theo thẻ: Total_Card (Transaction.cs:115-130)
        var cardTotals = transactions
            .GroupBy(t => new { t.CardNo, t.MemberNo })
            .Select(g => new
            {
                g.Key.CardNo,
                g.Key.MemberNo,
                PointChTotal = g.Sum(x => x.PointChTotal ?? 0.0),
                PointChRankTotal = g.Sum(x => x.PointChRankTotal ?? 0.0),
                PointChBlock = g.Sum(x => x.PointChBlock ?? 0.0),
                AmountChTotal = g.Sum(x => x.AmountChTotal ?? 0.0),
                AmountChBlock = g.Sum(x => x.AmountChBlock ?? 0.0),
                QtyVisitChTotal = g.Sum(x => x.QtyVisitChTotal ?? 0.0),
                QtyVisitChBlock = g.Sum(x => x.QtyVisitChBlock ?? 0.0)
            })
            .ToList();

        // 3. Cập nhật Crd_Card (Transaction.cs:280-302)
        foreach (var ct in cardTotals)
        {
            var card = await db.Set<CrdCard>().FirstOrDefaultAsync(c => c.CardNo == ct.CardNo);
            if (card != null)
            {
                card.PointTotal = (card.PointTotal ?? 0.0) + ct.PointChTotal;
                card.PointBlock = (card.PointBlock ?? 0.0) + ct.PointChBlock;
                card.PointAvail = (card.PointTotal ?? 0.0) - (card.PointBlock ?? 0.0);

                card.AmountTotal = (card.AmountTotal ?? 0.0) + ct.AmountChTotal;
                card.AmountBlock = (card.AmountBlock ?? 0.0) + ct.AmountChBlock;
                card.AmountAvail = (card.AmountTotal ?? 0.0) - (card.AmountBlock ?? 0.0);

                card.QtyVisitTotal = (card.QtyVisitTotal ?? 0.0) + ct.QtyVisitChTotal;
                card.QtyVisitBlock = (card.QtyVisitBlock ?? 0.0) + ct.QtyVisitChBlock;
                card.QtyVisitAvail = (card.QtyVisitTotal ?? 0.0) - (card.QtyVisitBlock ?? 0.0);

                card.LogLUDTimeUTC = createDTime;
                card.LogLUBy = createBy;
                card.LUDTimeUTC = createDTime;
                card.LUBy = createBy;
            }
        }

        // 4. Tính tổng theo hội viên: Total_Member (Transaction.cs:133-147)
        var memberTotals = cardTotals
            .GroupBy(ct => ct.MemberNo)
            .Select(g => new
            {
                MemberNo = g.Key,
                PointChTotal = g.Sum(x => x.PointChTotal),
                PointChRankTotal = g.Sum(x => x.PointChRankTotal),
                PointChBlock = g.Sum(x => x.PointChBlock),
                AmountChTotal = g.Sum(x => x.AmountChTotal),
                AmountChBlock = g.Sum(x => x.AmountChBlock),
                QtyVisitChTotal = g.Sum(x => x.QtyVisitChTotal),
                QtyVisitChBlock = g.Sum(x => x.QtyVisitChBlock)
            })
            .ToList();

        // 5. Cập nhật Crd_Member (Transaction.cs:305-330)
        foreach (var mt in memberTotals)
        {
            var member = await db.Set<CrdMember>().FirstOrDefaultAsync(m => m.MemberNo == mt.MemberNo);
            if (member != null)
            {
                member.PointTotal = (member.PointTotal ?? 0.0) + mt.PointChTotal;
                member.PointBlock = (member.PointBlock ?? 0.0) + mt.PointChBlock;
                member.PointAvail = (member.PointTotal ?? 0.0) - (member.PointBlock ?? 0.0);

                member.AmountTotal = (member.AmountTotal ?? 0.0) + mt.AmountChTotal;
                member.AmountBlock = (member.AmountBlock ?? 0.0) + mt.AmountChBlock;
                member.AmountAvail = (member.AmountTotal ?? 0.0) - (member.AmountBlock ?? 0.0);

                member.QtyVisitTotal = (member.QtyVisitTotal ?? 0.0) + mt.QtyVisitChTotal;
                member.QtyVisitBlock = (member.QtyVisitBlock ?? 0.0) + mt.QtyVisitChBlock;
                member.QtyVisitAvail = (member.QtyVisitTotal ?? 0.0) - (member.QtyVisitBlock ?? 0.0);

                // Nguồn dòng 317-319: PointEnd = PointBegin + PointTotal + PointChTotal - PointBlock - PointChBlock
                member.PointEnd = (member.PointBegin ?? 0.0) + (member.PointAvail ?? 0.0);
                member.AmountEnd = (member.AmountBegin ?? 0.0) + (member.AmountAvail ?? 0.0);
                member.QtyVisitEnd = (member.QtyVisitBegin ?? 0.0) + (member.QtyVisitAvail ?? 0.0);

                member.LogLUDTimeUTC = createDTime;
                member.LogLUBy = createBy;
                member.LUDTimeUTC = createDTime;
                member.LUBy = createBy;
            }
        }

        // 6. Kiểm tra ngưỡng số lượng / điểm: Check Threshold (Transaction.cs:333-391)
        foreach (var ct in cardTotals)
        {
            var card = await db.Set<CrdCard>().FirstOrDefaultAsync(c => c.CardNo == ct.CardNo);
            if (card != null)
            {
                bool pointInvalid = (card.PointTotal < dblMinValueTotal || card.PointBlock < dblMinValueBlock || card.PointAvail < dblMinValueAvail);
                bool amountInvalid = (card.AmountTotal < dblMinValueTotal || card.AmountBlock < dblMinValueBlock || card.AmountAvail < dblMinValueAvail);
                bool qtyInvalid = (card.QtyVisitTotal < dblMinValueTotal || card.QtyVisitBlock < dblMinValueBlock || card.QtyVisitAvail < dblMinValueAvail);

                // Nguồn line 344-346: 3 điều kiện AND kết hợp
                if (pointInvalid && amountInvalid && qtyInvalid)
                {
                    throw new PortThresholdException(new object?[]
                    {
                        "Check.CardNo", card.CardNo, "Check.MemberNo", card.MemberNo,
                        "Check.PointTotal", card.PointTotal, "Check.MinValueTotal", dblMinValueTotal,
                        "Check.PointBlock", card.PointBlock, "Check.MinValueBlock", dblMinValueBlock,
                        "Check.PointAvail", card.PointAvail, "Check.MinValueAvail", dblMinValueAvail,
                        "Check.AmountTotal", card.AmountTotal, "Check.AmountBlock", card.AmountBlock, "Check.AmountAvail", card.AmountAvail,
                        "Check.QtyVisitTotal", card.QtyVisitTotal, "Check.QtyVisitBlock", card.QtyVisitBlock, "Check.QtyVisitAvail", card.QtyVisitAvail
                    });
                }
            }
        }

        // 7. Cập nhật PointCardRank nếu có loại giao dịch xét hạng (Transaction.cs:437-523)
        if (!bIsPointExpiryOrCancel)
        {
            var rankDealTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "TRANSFERBENIFITS", "SERVICE", "POINTINCREASE", "POINTUSE", "CONSUMPTION", "SERVICETURN"
            };

            var rankEligible = transactions
                .Where(t => rankDealTypes.Contains(t.DealPointType ?? "") && (t.PointChRankTotal ?? 0.0) != 0.0)
                .ToList();

            if (rankEligible.Count > 0)
            {
                foreach (var group in rankEligible.GroupBy(t => new { t.CardNo, t.MemberNo }))
                {
                    double totalRank = group.Sum(t => t.PointChRankTotal ?? 0.0);
                    var card = await db.Set<CrdCard>().FirstOrDefaultAsync(c => c.CardNo == group.Key.CardNo);
                    if (card != null)
                    {
                        card.PointCardRank = (card.PointCardRank ?? 0.0) + totalRank;
                    }
                    var member = await db.Set<CrdMember>().FirstOrDefaultAsync(m => m.MemberNo == group.Key.MemberNo);
                    if (member != null)
                    {
                        member.PointCardRank = (member.PointCardRank ?? 0.0) + totalRank;
                    }
                }
            }

            // 8. Cập nhật AmountCardRank cho POINTINCREASE (Transaction.cs:526-599)
            var amountRankEligible = transactions
                .Where(t => string.Equals(t.DealPointType, "POINTINCREASE", StringComparison.OrdinalIgnoreCase) && (t.AmountRef ?? 0.0) != 0.0)
                .ToList();

            if (amountRankEligible.Count > 0)
            {
                foreach (var group in amountRankEligible.GroupBy(t => t.MemberNo))
                {
                    double totalAmountRank = group.Sum(t => t.AmountRef ?? 0.0);
                    var member = await db.Set<CrdMember>().FirstOrDefaultAsync(m => m.MemberNo == group.Key);
                    if (member != null)
                    {
                        member.AmountCardRank = (member.AmountCardRank ?? 0.0) + totalAmountRank;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Tặng điểm mua xe mới — port 1:1 từ Transaction.AddPoint.cs:146-545 (Crd_Member_PerformBuyNewCarX).
    /// DealPointType = 'SALES', RefNo = 'NEW.{yyyyMMdd.HHmmss}', tích điểm PointBuyCar cho hội viên.
    /// </summary>
    public static async Task PerformBuyNewCarXAsync(
        AppDbContext db,
        CrdMember m,
        CrdCard card,
        double dblPointBuyCar,
        double unitPointToMoney,
        DateTime now,
        string user)
    {
        var dateEndMonthNextYear = GetDateEndMonthOfNextYear(now);
        var refNo = $"NEW.{now:yyyyMMdd.HHmmss}";

        var tx = new CrdCardTransaction
        {
            RefNo = refNo,
            ProjectCode = "LOYALTY",
            NetworkID = m.NetworkID,
            OrgID = m.OrgID,
            DLCode = m.DLCodeRegis,
            MemberNo = m.MemberNo,
            CardNo = card.CardNo,
            CardSourceCode = card.CardSourceCode,
            CardTypeUse = card.CardTypeUse,
            CardTypeInit = card.CardTypeInit,
            DealPointType = "SALES",
            PrProgramCode = null,
            EffDateStart = m.EffDateStart,
            EffDateEnd = m.EffDateEnd,
            PointChTotal = dblPointBuyCar,
            PointChRankTotal = 0.0,
            PointChBlock = 0.0,
            AmountChTotal = dblPointBuyCar * unitPointToMoney,
            AmountChBlock = 0.0,
            QtyVisitChTotal = 0.0,
            QtyVisitChBlock = 0.0,
            QtyPrChTotal = 0.0,
            QtyPrChBlock = 0.0,
            QtyPrUsed = 0.0,
            PointRef = dblPointBuyCar,
            AmountRef = 0.0,
            PolicyPointUnit = 0.0,
            PolicyAmountUnit = 0.0,
            PolicyCode = "UNITPOINTTOMONEY",
            ParamPointUnit = 1.0,
            ParamAmountUnit = unitPointToMoney,
            ParamCode = "UNITPOINTTOMONEY",
            FlagViewUI = "1",
            FlagPoint = "1",
            FlagAmount = "1",
            FlagPr = "0",
            FunctionName = "Crd_Member_Finish",
            FunctionActionType = "FINISH",
            FunctionRemark = "",
            RefType = "CRD_MEMBER",
            RefCode03 = card.CardNo,
            RefCode04 = "1",
            RefCode05 = card.CardStatus,
            PointExpiryDTime = dateEndMonthNextYear,
            AmountChTotalInit = 0.0,
            AmountChTotalDiffer = 0.0,
            AmountAvailPrevious = 0.0,
            CreateDTimeUTC = now,
            CreateBy = user,
            LUDTimeUTC = now,
            LUBy = user,
            LogLUDTimeUTC = now,
            LogLUBy = user
        };

        await Crd_CardTransaction_PerformAsync(
            db,
            new List<CrdCardTransaction> { tx },
            now,
            user,
            dblMinValueTotal: 0.0,
            dblMinValueBlock: int.MinValue,
            dblMinValueAvail: 0.0);
    }

    /// <summary>
    /// Tặng điểm giới thiệu mua xe — port 1:1 từ Transaction.AddPoint.cs:651-1487 (Crd_Member_PerformIntroX).
    /// DealPointType = 'INTRODUCTION', RefNo = 'INT.{yyyyMMdd.HHmmss}', tích điểm PointIntro cho hội viên giới thiệu (MemberNoIntro).
    /// </summary>
    public static async Task PerformIntroXAsync(
        AppDbContext db,
        CrdMember m,
        double dblPointIntro,
        double unitPointToMoney,
        DateTime now,
        string user)
    {
        if (string.IsNullOrWhiteSpace(m.MemberNoIntro)) return;

        // Tìm hội viên giới thiệu (Transaction.AddPoint.cs:738-745)
        var introMember = await db.Set<CrdMember>().FirstOrDefaultAsync(x => x.MemberNo == m.MemberNoIntro);
        if (introMember == null) return;

        // Tìm thẻ đang hoạt động gần nhất của hội viên giới thiệu (Transaction.AddPoint.cs:748-757)
        var introCard = await db.Set<CrdCard>()
            .Where(c => c.MemberNo == introMember.MemberNo && c.CardStatus == "APPROVE")
            .OrderByDescending(c => c.CreateDTimeUTC)
            .FirstOrDefaultAsync();
        if (introCard == null) return;

        var dateEndMonthNextYear = GetDateEndMonthOfNextYear(now);
        var refNo = $"INT.{now:yyyyMMdd.HHmmss}";

        var tx = new CrdCardTransaction
        {
            RefNo = refNo,
            ProjectCode = "LOYALTY",
            NetworkID = introMember.NetworkID,
            OrgID = m.OrgID,
            DLCode = m.DLCodeRegis,
            MemberNo = introMember.MemberNo,
            CardNo = introCard.CardNo,
            CardSourceCode = introCard.CardSourceCode,
            CardTypeUse = introCard.CardTypeUse,
            CardTypeInit = introCard.CardTypeInit,
            DealPointType = "INTRODUCTION",
            PrProgramCode = null,
            EffDateStart = introMember.EffDateStart,
            EffDateEnd = introMember.EffDateEnd,
            PointChTotal = dblPointIntro,
            PointChRankTotal = 0.0,
            PointChBlock = 0.0,
            AmountChTotal = dblPointIntro * unitPointToMoney,
            AmountChBlock = 0.0,
            QtyVisitChTotal = 0.0,
            QtyVisitChBlock = 0.0,
            QtyPrChTotal = 0.0,
            QtyPrChBlock = 0.0,
            QtyPrUsed = 0.0,
            PointRef = dblPointIntro,
            AmountRef = 0.0,
            PolicyPointUnit = 0.0,
            PolicyAmountUnit = 0.0,
            PolicyCode = "UNITPOINTTOMONEY",
            ParamPointUnit = 1.0,
            ParamAmountUnit = unitPointToMoney,
            ParamCode = "UNITPOINTTOMONEY",
            FlagViewUI = "1",
            FlagPoint = "1",
            FlagAmount = "1",
            FlagPr = "0",
            FunctionName = "Crd_Member_Finish",
            FunctionActionType = "FINISH",
            FunctionRemark = "",
            RefType = "CRD_MEMBER",
            RefCode03 = introCard.CardNo,
            RefCode04 = "1",
            RefCode05 = introCard.CardStatus,
            PointExpiryDTime = dateEndMonthNextYear,
            AmountChTotalInit = 0.0,
            AmountChTotalDiffer = 0.0,
            AmountAvailPrevious = 0.0,
            CreateDTimeUTC = now,
            CreateBy = user,
            LUDTimeUTC = now,
            LUBy = user,
            LogLUDTimeUTC = now,
            LogLUBy = user
        };

        await Crd_CardTransaction_PerformAsync(
            db,
            new List<CrdCardTransaction> { tx },
            now,
            user,
            dblMinValueTotal: 0.0,
            dblMinValueBlock: int.MinValue,
            dblMinValueAvail: 0.0);
    }

    /// <summary>
    /// Tặng điểm mở thẻ mới — port 1:1 từ Transaction.AddPoint.cs:1488-1885 (Crd_Member_PerformOpenCardX).
    /// DealPointType = 'OPENCARD', RefNo = 'MT.{yyyyMMdd.HHmmss}', DLCode = 'HTV'.
    /// </summary>
    public static async Task PerformOpenCardXAsync(
        AppDbContext db,
        CrdMember m,
        CrdCard card,
        double dblPointOpenCard,
        double unitPointToMoney,
        DateTime now,
        string user)
    {
        var dateEndMonthNextYear = GetDateEndMonthOfNextYear(now);
        var refNo = $"MT.{now:yyyyMMdd.HHmmss}";

        var tx = new CrdCardTransaction
        {
            RefNo = refNo,
            ProjectCode = "LOYALTY",
            NetworkID = m.NetworkID,
            OrgID = m.OrgID,
            DLCode = "HTV", // Nguồn Card.cs:1587
            MemberNo = m.MemberNo,
            CardNo = card.CardNo,
            CardSourceCode = card.CardSourceCode,
            CardTypeUse = card.CardTypeUse,
            CardTypeInit = card.CardTypeInit,
            DealPointType = "OPENCARD",
            PrProgramCode = null,
            EffDateStart = m.EffDateStart,
            EffDateEnd = m.EffDateEnd,
            PointChTotal = dblPointOpenCard,
            PointChRankTotal = 0.0,
            PointChBlock = 0.0,
            AmountChTotal = dblPointOpenCard * unitPointToMoney,
            AmountChBlock = 0.0,
            QtyVisitChTotal = 0.0,
            QtyVisitChBlock = 0.0,
            QtyPrChTotal = 0.0,
            QtyPrChBlock = 0.0,
            QtyPrUsed = 0.0,
            PointRef = dblPointOpenCard,
            AmountRef = 0.0,
            PolicyPointUnit = 0.0,
            PolicyAmountUnit = 0.0,
            PolicyCode = "UNITPOINTTOMONEY",
            ParamPointUnit = 1.0,
            ParamAmountUnit = unitPointToMoney,
            ParamCode = "UNITPOINTTOMONEY",
            FlagViewUI = "1",
            FlagPoint = "1",
            FlagAmount = "1",
            FlagPr = "0",
            FunctionName = "Crd_Member_Finish",
            FunctionActionType = "FINISH",
            FunctionRemark = "",
            RefType = "CRD_MEMBER",
            RefCode03 = card.CardNo,
            RefCode04 = "1",
            RefCode05 = card.CardStatus,
            PointExpiryDTime = dateEndMonthNextYear,
            AmountChTotalInit = 0.0,
            AmountChTotalDiffer = 0.0,
            AmountAvailPrevious = 0.0,
            CreateDTimeUTC = now,
            CreateBy = user,
            LUDTimeUTC = now,
            LUBy = user,
            LogLUDTimeUTC = now,
            LogLUBy = user
        };

        await Crd_CardTransaction_PerformAsync(
            db,
            new List<CrdCardTransaction> { tx },
            now,
            user,
            dblMinValueTotal: 0.0,
            dblMinValueBlock: int.MinValue,
            dblMinValueAvail: 0.0);
    }

    /// <summary>
    /// HTV tặng điểm khuyến mại bán hàng (Creta) — port 1:1 từ Transaction.AddPoint.cs:1886-2306 (Crd_Member_PerformBuyCretaX).
    /// Áp dụng khi CardTypeUse != CardTypeInit. DealPointType = 'KMBH', RefNo = 'KMBH.{yyyyMMdd.HHmmss}', DLCode = 'HTV'.
    /// </summary>
    public static async Task PerformBuyCretaXAsync(
        AppDbContext db,
        CrdMember m,
        CrdCard card,
        double dblPointBuyCreta,
        double unitPointToMoney,
        DateTime now,
        string user)
    {
        var dateEndMonthNextYear = GetDateEndMonthOfNextYear(now);
        var refNo = $"KMBH.{now:yyyyMMdd.HHmmss}";

        var tx = new CrdCardTransaction
        {
            RefNo = refNo,
            ProjectCode = "LOYALTY",
            NetworkID = m.NetworkID,
            OrgID = m.OrgID,
            DLCode = "HTV", // Nguồn Card.cs:2008
            MemberNo = m.MemberNo,
            CardNo = card.CardNo,
            CardSourceCode = card.CardSourceCode,
            CardTypeUse = card.CardTypeUse,
            CardTypeInit = card.CardTypeInit,
            DealPointType = "KMBH",
            PrProgramCode = null,
            EffDateStart = m.EffDateStart,
            EffDateEnd = m.EffDateEnd,
            PointChTotal = dblPointBuyCreta,
            PointChRankTotal = 0.0,
            PointChBlock = 0.0,
            AmountChTotal = dblPointBuyCreta * unitPointToMoney,
            AmountChBlock = 0.0,
            QtyVisitChTotal = 0.0,
            QtyVisitChBlock = 0.0,
            QtyPrChTotal = 0.0,
            QtyPrChBlock = 0.0,
            QtyPrUsed = 0.0,
            PointRef = dblPointBuyCreta,
            AmountRef = 0.0,
            PolicyPointUnit = 0.0,
            PolicyAmountUnit = 0.0,
            PolicyCode = "UNITPOINTTOMONEY",
            ParamPointUnit = 1.0,
            ParamAmountUnit = unitPointToMoney,
            ParamCode = "UNITPOINTTOMONEY",
            FlagViewUI = "1",
            FlagPoint = "1",
            FlagAmount = "1",
            FlagPr = "0",
            FunctionName = "Crd_Member_Finish",
            FunctionActionType = "FINISH",
            FunctionRemark = "",
            RefType = "CRD_MEMBER",
            RefCode03 = card.CardNo,
            RefCode04 = "1",
            RefCode05 = card.CardStatus,
            PointExpiryDTime = dateEndMonthNextYear,
            AmountChTotalInit = 0.0,
            AmountChTotalDiffer = 0.0,
            AmountAvailPrevious = 0.0,
            CreateDTimeUTC = now,
            CreateBy = user,
            LUDTimeUTC = now,
            LUBy = user,
            LogLUDTimeUTC = now,
            LogLUBy = user
        };

        await Crd_CardTransaction_PerformAsync(
            db,
            new List<CrdCardTransaction> { tx },
            now,
            user,
            dblMinValueTotal: 0.0,
            dblMinValueBlock: int.MinValue,
            dblMinValueAvail: 0.0);
    }

    /// <summary>
    /// Giao dịch voucher hội viên — port 1:1 từ Transaction.cs:3470-3800 (Crd_MemberVoucherTransaction_Perform_New20260813).
    /// </summary>
    public static async Task Crd_MemberVoucherTransaction_PerformAsync(
        AppDbContext db,
        List<Crd_MemberVoucherTransaction> transactions,
        DateTime createDTime,
        string createBy,
        double dblMinValueTotal = 0.0)
    {
        if (transactions == null || transactions.Count == 0) return;

        // 1. Thêm bản ghi Crd_MemberVoucherTransaction
        db.Set<Crd_MemberVoucherTransaction>().AddRange(transactions);

        // 2. Cập nhật Crd_Member (PointVoucher, PointActualVoucher) (Transaction.cs:3640-3653)
        var memberVoucherTotals = transactions
            .GroupBy(t => t.MemberNo)
            .Select(g => new
            {
                MemberNo = g.Key,
                PointChTotal = g.Sum(x => x.PointChTotal ?? 0.0),
                PointActualChTotal = g.Sum(x => x.PointActualChTotal ?? 0.0)
            })
            .ToList();

        foreach (var mvt in memberVoucherTotals)
        {
            var member = await db.Set<CrdMember>().FirstOrDefaultAsync(m => m.MemberNo == mvt.MemberNo);
            if (member != null)
            {
                member.PointVoucher = (member.PointVoucher ?? 0.0) + mvt.PointChTotal;
                member.PointActualVoucher = (member.PointActualVoucher ?? 0.0) + mvt.PointActualChTotal;
                member.LogLUDTimeUTC = createDTime;
                member.LogLUBy = createBy;
                member.LUDTimeUTC = createDTime;
                member.LUBy = createBy;
            }
        }

        // 3. Thêm mới Crd_MemberVoucher cho giao dịch tăng voucher (Transaction.cs:3747-3792)
        var voucherIncreaseDealTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "VOUCHERXM", "VOUCHERTSN"
        };

        foreach (var t in transactions)
        {
            if (voucherIncreaseDealTypes.Contains(t.DealPointType ?? ""))
            {
                var existsVC = await db.Set<Crd_MemberVoucher>()
                    .AnyAsync(v => v.MemberNo == t.MemberNo && v.VoucherNo == t.RefNo);

                if (!existsVC)
                {
                    db.Set<Crd_MemberVoucher>().Add(new Crd_MemberVoucher
                    {
                        MemberNo = t.MemberNo,
                        NetworkID = t.NetworkID,
                        VoucherNo = t.RefNo,
                        PointVCTotal = t.PointChTotal,
                        PointVCRemain = t.PointVCRemain,
                        PointVCLimit = t.PointVCLimit,
                        PointExpireDate = t.PointExpiryDate,
                        FlagActive = "1",
                        CreateDTimeUTC = createDTime,
                        CreateBy = createBy,
                        LUDTimeUTC = createDTime,
                        LUBy = createBy,
                        LogLUDTimeUTC = createDTime,
                        LogLUBy = createBy
                    });
                }
            }
        }
    }

    /// <summary>
    /// HTV tặng điểm Voucher xe mới — port 1:1 từ Transaction.AddPoint.cs:2660-3035 (Crd_Member_PerformVoucherNewCarX_New20260813).
    /// DealPointType = 'VOUCHERXM', RefNo = 'VCXM.{yyyyMMdd.HHmmss}', DLCode = 'HTV'.
    /// </summary>
    public static async Task PerformVoucherNewCarXAsync(
        AppDbContext db,
        CrdMember m,
        CrdCard card,
        double dblPointVoucherNewCar,
        DateTime now,
        string user)
    {
        var nowDateStr = now.ToString("yyyy-MM-dd");

        // 1. Tìm chương trình VoucherNewCar active (Transaction.AddPoint.cs:2750-2797)
        // Hiệu lực hôm nay, trạng thái FINISH, FlagAllModel = '1' hoặc FlagAllModel = '0' có Spec cho ModelCode của hội viên
        var prmList = await db.Set<Prm_VoucherNewCar>()
            .Where(p => p.PrmVoucherStatus == "FINISH"
                        && (string.IsNullOrEmpty(p.EffDateStart) || string.Compare(p.EffDateStart, nowDateStr) <= 0)
                        && (string.IsNullOrEmpty(p.EffDateEnd) || string.Compare(p.EffDateEnd, nowDateStr) >= 0))
            .OrderByDescending(p => p.FinishDTimeUTC)
            .ToListAsync();

        Prm_VoucherNewCar? selectedPrm = null;
        Prm_VoucherNewCarDtl? selectedDtl = null;

        foreach (var p in prmList)
        {
            if (p.FlagAllModel == "1")
            {
                selectedPrm = p;
                break;
            }
            else
            {
                var spec = await db.Set<Prm_VoucherNewCarSpec>()
                    .FirstOrDefaultAsync(s => s.PrmVoucherCode == p.PrmVoucherCode && s.ModelCode == m.ModelCode);
                if (spec != null)
                {
                    selectedPrm = p;
                    selectedDtl = await db.Set<Prm_VoucherNewCarDtl>()
                        .FirstOrDefaultAsync(d => d.PrmVoucherCode == p.PrmVoucherCode && d.Idx == spec.Idx);
                    break;
                }
            }
        }

        // Tính các giá trị điểm voucher và hạn dùng (Transaction.AddPoint.cs:2825-2855)
        double pointActualVoucher = dblPointVoucherNewCar;
        double pointUseLimit = 0.0;
        double qtyUseLimit = 0.0;
        DateTime pointExpiryDate;

        if (selectedPrm != null)
        {
            if (selectedPrm.FlagAllModel == "1")
            {
                pointActualVoucher = selectedPrm.PointActualVCAllModel ?? dblPointVoucherNewCar;
                pointUseLimit = selectedPrm.PointUseLimitAllModel ?? 0.0;
                qtyUseLimit = selectedPrm.QtyUseLimitAllModel ?? 0.0;
            }
            else if (selectedDtl != null)
            {
                pointActualVoucher = selectedDtl.PointActualVoucher ?? dblPointVoucherNewCar;
                pointUseLimit = selectedDtl.PointUseLimit ?? 0.0;
                qtyUseLimit = selectedDtl.QtyUseLimit ?? 0.0;
            }

            int validityPeriod = (int)(selectedPrm.ValidityPeriod ?? 365.0);
            var targetDate = now.Date.AddDays(validityPeriod);
            pointExpiryDate = new DateTime(targetDate.Year, targetDate.Month, DateTime.DaysInMonth(targetDate.Year, targetDate.Month), 23, 59, 59, DateTimeKind.Utc);
        }
        else
        {
            pointExpiryDate = GetDateEndMonthOfNextYear(now);
        }

        DateTime? prmStart = null;
        if (!string.IsNullOrEmpty(selectedPrm?.EffDateStart) && DateTime.TryParse(selectedPrm.EffDateStart, out var ps))
        {
            prmStart = ps;
        }

        DateTime? prmEnd = null;
        if (!string.IsNullOrEmpty(selectedPrm?.EffDateEnd) && DateTime.TryParse(selectedPrm.EffDateEnd, out var pe))
        {
            prmEnd = pe;
        }

        var refNo = $"VCXM.{now:yyyyMMdd.HHmmss}";

        var vTx = new Crd_MemberVoucherTransaction
        {
            RefNo = refNo,
            NetworkID = m.NetworkID,
            OrgID = m.OrgID,
            DLCode = "HTV",
            MemberNo = m.MemberNo,
            CardNo = card.CardNo,
            CardTypeUse = card.CardTypeUse,
            CardTypeInit = card.CardTypeInit,
            DealPointType = "VOUCHERXM",
            PrmVoucherCode = selectedPrm?.PrmVoucherCode,
            EffDateStart = prmStart,
            EffDateEnd = prmEnd,
            PointChTotal = dblPointVoucherNewCar,
            PointActualChTotal = pointActualVoucher,
            PointVCRemain = dblPointVoucherNewCar,
            PointActualVCRemain = pointActualVoucher,
            PointVCLimit = pointUseLimit,
            PointExpiryDate = pointExpiryDate,
            PointVCMemberRemain = m.PointVoucher ?? 0.0,
            PointActualVCMemberRM = m.PointActualVoucher ?? 0.0,
            PointVCRO = 0.0,
            PointActualVCRO = 0.0,
            QtyUseVCLimit = qtyUseLimit,
            QtyUseVCRemain = qtyUseLimit,
            CreateDate = now.Date,
            CreateDTimeUTC = now,
            CreateBy = user,
            LUDTimeUTC = now,
            LUBy = user,
            LogLUDTimeUTC = now,
            LogLUBy = user
        };

        await Crd_MemberVoucherTransaction_PerformAsync(
            db,
            new List<Crd_MemberVoucherTransaction> { vTx },
            now,
            user,
            dblMinValueTotal: 0.0);
    }
}

/// <summary>Ngưỡng điểm/số lượng vi phạm — nguồn throw Crd_CardTransaction_Perform_QtyChangeOverThreshold (Transaction.cs:~372).</summary>
public sealed class PortThresholdException : Exception
{
    public object?[] Params { get; }
    public PortThresholdException(object?[] p) : base("Crd_CardTransaction_Perform_QtyChangeOverThreshold") { Params = p; }
}
