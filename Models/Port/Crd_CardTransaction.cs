using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MiniLoyalty.Data;
using MiniLoyalty.Models;

namespace MiniLoyalty.Models.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — Crd_CardTransaction (#012).
//
// Nguồn tra cứu (chỉ đọc):
//   - Model cột: 12.Dev.Common/idn.Loyalty.Common/Models/Loyalty/Crd_CardTransaction.cs:11-93
//   - DDL thật:  11.BackEnd/V10/05.Refs.Biz/Migrate/Script.HuongTTT/20221108.z11.Crd_Member.Crd_Card.sql:215-280
//                (và 20221206.z11.UpdDB.40.Transaction.sql:6-69)
//   - SQL select: idn.Loyalty.Biz/Master.Loyalty.cs:21409-21817 (Crd_CardTransaction_GetX)
//
// GHI CHÚ QUAN TRỌNG:
//   - 4 bảng Crd_* lõi đã được định nghĩa tại Models/PortLoyalty.cs (CrdMember, CrdCard, CrdCardTransaction,
//     CrdMemberDiscountTransaction) và ánh xạ sẵn trong AppDbContext.cs theo PORT_RULES_MiniLoyalty.md dòng 39.
//   - Khai báo type alias để đồng bộ tên gọi Crd_* chuẩn nguồn cho các controller port.
// ===========================================================================================

// Type aliases cho Crd_*
public class Crd_CardTransactionRef
{
    // Giữ tham chiếu mô hình chuẩn
}

/// <summary>
/// Seeder dữ liệu mẫu cho Crd_CardTransaction (#012), kết nối Crd_Member, Crd_Card, Mst_PromotionProgram,
/// Mst_PromotionProgramDtl và Crd_MemberDiscountTransaction.
/// Order => 50 (đề bài yêu cầu Order 12 nhưng Order 12 đã được Mst_DealerCP sử dụng, chọn 50 chưa dùng theo bài học A.4).
/// Idempotent: kiểm tra CardNo = 'CARD01' trước khi seed.
/// </summary>
public class Crd_CardTransactionSeeder : IPortSeeder
{
    // Đề bài yêu cầu Order => 12 nhưng 12 đã bị Mst_DealerCPSeeder chiếm, chọn 50 chưa dùng trong 10-99 theo bài học A.4
    public int Order => 50;

    public async Task SeedAsync(AppDbContext db)
    {
        // Kiểm tra idempotent theo CARD01
        if (await db.Set<CrdCardTransaction>().AnyAsync(x => x.CardNo == "CARD01"))
            return;

        const string net = "DEMO";
        const string org = "DEMO";
        var now = DateTime.UtcNow;

        // 1. Đảm bảo có dữ liệu Crd_Member (MEM01, MEM02)
        if (!await db.Set<CrdMember>().AnyAsync(x => x.MemberNo == "MEM01"))
        {
            db.Set<CrdMember>().Add(new CrdMember
            {
                MemberNo = "MEM01",
                NetworkID = net,
                OrgID = org,
                MemberName = "Nguyễn Văn A",
                PhoneNo = "0901234567",
                CarNo = "30A-99999",
                IDCardNo = "001234567890",
                DLCodeRegis = "DL01",
                CustomerCode = "KH001",
                CardTypeUse = "N",
                CardTypeInit = "N",
                MemberStatus = LoyaltyConst.MemberStatus.Approve,
                RegisStatus = LoyaltyConst.RegisStatus.Finish,
                RegistrationDate = now.AddMonths(-6),
                CreateDTimeUTC = now.AddMonths(-6),
                CreateBy = "seed",
                LogLUDTimeUTC = now.AddMonths(-6),
                LogLUBy = "seed"
            });
        }

        if (!await db.Set<CrdMember>().AnyAsync(x => x.MemberNo == "MEM02"))
        {
            db.Set<CrdMember>().Add(new CrdMember
            {
                MemberNo = "MEM02",
                NetworkID = net,
                OrgID = org,
                MemberName = "Trần Thị B",
                PhoneNo = "0902345678",
                CarNo = "30A-88888",
                IDCardNo = "001234567891",
                DLCodeRegis = "DL02",
                CustomerCode = "KH002",
                CardTypeUse = "S",
                CardTypeInit = "S",
                MemberStatus = LoyaltyConst.MemberStatus.Approve,
                RegisStatus = LoyaltyConst.RegisStatus.Finish,
                RegistrationDate = now.AddMonths(-4),
                CreateDTimeUTC = now.AddMonths(-4),
                CreateBy = "seed",
                LogLUDTimeUTC = now.AddMonths(-4),
                LogLUBy = "seed"
            });
        }

        // 2. Đảm bảo có dữ liệu Crd_Card (CARD01, CARD02)
        if (!await db.Set<CrdCard>().AnyAsync(x => x.CardNo == "CARD01"))
        {
            db.Set<CrdCard>().Add(new CrdCard
            {
                CardNo = "CARD01",
                MemberNo = "MEM01",
                NetworkID = net,
                DLCodeExceptionally = "DL_EXCP",
                RankPolicyCode = "RP2026",
                CardTypeUse = "N",
                CardTypeInit = "N",
                CardStatus = LoyaltyConst.CardStatus.Approve,
                EffDateStart = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EffDateEnd = new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc),
                CreateDTimeUTC = now.AddMonths(-6),
                CreateBy = "seed",
                LogLUDTimeUTC = now.AddMonths(-6),
                LogLUBy = "seed"
            });
        }

        if (!await db.Set<CrdCard>().AnyAsync(x => x.CardNo == "CARD02"))
        {
            db.Set<CrdCard>().Add(new CrdCard
            {
                CardNo = "CARD02",
                MemberNo = "MEM02",
                NetworkID = net,
                DLCodeExceptionally = null,
                RankPolicyCode = "RP2026",
                CardTypeUse = "S",
                CardTypeInit = "S",
                CardStatus = LoyaltyConst.CardStatus.Approve,
                EffDateStart = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EffDateEnd = new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc),
                CreateDTimeUTC = now.AddMonths(-4),
                CreateBy = "seed",
                LogLUDTimeUTC = now.AddMonths(-4),
                LogLUBy = "seed"
            });
        }

        // 3. Seed Crd_CardTransaction (3 dòng theo đề bài P2: CARD01, CARD02)
        // Dòng 1: CARD01, MEM01, PrProgramCode PR01, CardTypeUse N
        db.Set<CrdCardTransaction>().Add(new CrdCardTransaction
        {
            RefNo = "RO26100001",
            ProjectCode = "LOYALTY",
            NetworkID = net,
            OrgID = org,
            DLCode = "DL01",
            MemberNo = "MEM01",
            CardNo = "CARD01",
            CardSourceCode = LoyaltyConst.CardSourceCode.New,
            CardTypeUse = "N",
            CardTypeInit = "N",
            DealPointType = LoyaltyConst.DealPointType.Consumption,
            PrProgramCode = "PR01",
            EffDateStart = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EffDateEnd = new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc),
            PointChTotal = 100d,
            PointChBlock = 0d,
            AmountChTotal = 1_000_000d,
            AmountChBlock = 0d,
            QtyVisitChTotal = 1d,
            QtyVisitChBlock = 0d,
            QtyPrChTotal = 2d,
            QtyPrChBlock = 0d,
            QtyPrUsed = 1d,
            PointRef = 100d,
            AmountRef = 1_000_000d,
            PolicyPointUnit = 1d,
            PolicyAmountUnit = 10_000d,
            PolicyCode = "RP2026",
            ParamPointUnit = 1d,
            ParamAmountUnit = 1d,
            ParamCode = "STD",
            FlagViewUI = "1",
            FlagPoint = "1",
            FlagAmount = "1",
            FlagPr = "0",
            FunctionName = "Crd_CardTransaction_Seed",
            FunctionActionType = "ADD",
            FunctionRemark = "Seed giao dịch tiêu dùng 1",
            RefType = "RO",
            PointChRankTotal = 50d,
            RefCode00 = "RO01",
            CreateDTimeUTC = new DateTime(2026, 10, 10, 8, 0, 0, DateTimeKind.Utc),
            CreateBy = "seed",
            LUDTimeUTC = now,
            LUBy = "seed",
            LogLUDTimeUTC = new DateTime(2026, 10, 10, 8, 0, 0, DateTimeKind.Utc),
            LogLUBy = "seed",
            PointExpiryDTime = new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc),
            AmountChTotalInit = 1_000_000d,
            AmountChTotalDiffer = 0d,
            AmountAvailPrevious = 0d,
            PointChTotalInit = 100d,
            PointChTotalDiffer = 0d,
            DLCodeExceptionally = "DL_EXCP"
        });

        // Dòng 2: CARD02, MEM02, PrProgramCode PR02, CardTypeUse S
        db.Set<CrdCardTransaction>().Add(new CrdCardTransaction
        {
            RefNo = "RO26100002",
            ProjectCode = "LOYALTY",
            NetworkID = net,
            OrgID = org,
            DLCode = "DL02",
            MemberNo = "MEM02",
            CardNo = "CARD02",
            CardSourceCode = LoyaltyConst.CardSourceCode.New,
            CardTypeUse = "S",
            CardTypeInit = "S",
            DealPointType = LoyaltyConst.DealPointType.PointIncrease,
            PrProgramCode = "PR02",
            EffDateStart = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EffDateEnd = new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc),
            PointChTotal = 200d,
            PointChBlock = 0d,
            AmountChTotal = 2_000_000d,
            AmountChBlock = 0d,
            QtyVisitChTotal = 2d,
            QtyVisitChBlock = 0d,
            QtyPrChTotal = 1d,
            QtyPrChBlock = 0d,
            QtyPrUsed = 1d,
            PointRef = 200d,
            AmountRef = 2_000_000d,
            PolicyPointUnit = 1d,
            PolicyAmountUnit = 10_000d,
            PolicyCode = "RP2026",
            ParamPointUnit = 1d,
            ParamAmountUnit = 1d,
            ParamCode = "STD",
            FlagViewUI = "1",
            FlagPoint = "1",
            FlagAmount = "1",
            FlagPr = "0",
            FunctionName = "Crd_CardTransaction_Seed",
            FunctionActionType = "ADD",
            FunctionRemark = "Seed giao dịch tích điểm 2",
            RefType = "RO",
            PointChRankTotal = 100d,
            RefCode00 = "RO02",
            CreateDTimeUTC = new DateTime(2026, 10, 12, 9, 0, 0, DateTimeKind.Utc),
            CreateBy = "seed",
            LUDTimeUTC = now,
            LUBy = "seed",
            LogLUDTimeUTC = new DateTime(2026, 10, 12, 9, 0, 0, DateTimeKind.Utc),
            LogLUBy = "seed",
            PointExpiryDTime = new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc),
            AmountChTotalInit = 2_000_000d,
            AmountChTotalDiffer = 0d,
            AmountAvailPrevious = 0d,
            PointChTotalInit = 200d,
            PointChTotalDiffer = 0d,
            DLCodeExceptionally = null
        });

        // Dòng 3: CARD02, MEM02, DealPointType CONSUMPTION
        db.Set<CrdCardTransaction>().Add(new CrdCardTransaction
        {
            RefNo = "RO26100003",
            ProjectCode = "LOYALTY",
            NetworkID = net,
            OrgID = org,
            DLCode = "DL01",
            MemberNo = "MEM02",
            CardNo = "CARD02",
            CardSourceCode = LoyaltyConst.CardSourceCode.New,
            CardTypeUse = "S",
            CardTypeInit = "S",
            DealPointType = LoyaltyConst.DealPointType.Consumption,
            PrProgramCode = "PR02",
            EffDateStart = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EffDateEnd = new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc),
            PointChTotal = 300d,
            PointChBlock = 0d,
            AmountChTotal = 3_000_000d,
            AmountChBlock = 0d,
            QtyVisitChTotal = 1d,
            QtyVisitChBlock = 0d,
            QtyPrChTotal = 1d,
            QtyPrChBlock = 0d,
            QtyPrUsed = 0d,
            PointRef = 300d,
            AmountRef = 3_000_000d,
            PolicyPointUnit = 1d,
            PolicyAmountUnit = 10_000d,
            PolicyCode = "RP2026",
            ParamPointUnit = 1d,
            ParamAmountUnit = 1d,
            ParamCode = "STD",
            FlagViewUI = "1",
            FlagPoint = "1",
            FlagAmount = "1",
            FlagPr = "0",
            FunctionName = "Crd_CardTransaction_Seed",
            FunctionActionType = "ADD",
            FunctionRemark = "Seed giao dịch tiêu dùng 3",
            RefType = "RO",
            PointChRankTotal = 150d,
            RefCode00 = "RO03",
            CreateDTimeUTC = new DateTime(2026, 10, 14, 11, 0, 0, DateTimeKind.Utc),
            CreateBy = "seed",
            LUDTimeUTC = now,
            LUBy = "seed",
            LogLUDTimeUTC = new DateTime(2026, 10, 14, 11, 0, 0, DateTimeKind.Utc),
            LogLUBy = "seed",
            PointExpiryDTime = new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc),
            AmountChTotalInit = 3_000_000d,
            AmountChTotalDiffer = 0d,
            AmountAvailPrevious = 0d,
            PointChTotalInit = 300d,
            PointChTotalDiffer = 0d,
            DLCodeExceptionally = null
        });

        // 4. Seed Crd_MemberDiscountTransaction (1 dòng phục vụ union DealPointType = 'DISCOUNTRO')
        if (!await db.Set<CrdMemberDiscountTransaction>().AnyAsync(x => x.RefNo == "RO-DC01"))
        {
            db.Set<CrdMemberDiscountTransaction>().Add(new CrdMemberDiscountTransaction
            {
                RefNo = "RO-DC01",
                NetworkID = net,
                OrgID = org,
                DLCode = "DL01",
                MemberNo = "MEM01",
                CardNo = "CARD01",
                CardTypeUse = "N",
                CardTypeInit = "N",
                CardTypeApply = "N",
                DealPointType = LoyaltyConst.DealPointType.DiscountRO,
                PolicyCode = "POL01",
                PolicyDiscountRate = 10d,
                DLCodeExceptionally = "DL_EXCP",
                FlagExceptionally = "0",
                EffDateStart = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EffDateEnd = new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc),
                AmountForDC = 500_000d,
                PointChTotal = 50_000d,
                CreateDate = new DateTime(2026, 10, 15, 10, 0, 0, DateTimeKind.Utc),
                CreateDTimeUTC = new DateTime(2026, 10, 15, 10, 0, 0, DateTimeKind.Utc),
                CreateBy = "seed",
                LUDTimeUTC = now,
                LUBy = "seed",
                LogLUDTimeUTC = new DateTime(2026, 10, 15, 10, 0, 0, DateTimeKind.Utc),
                LogLUBy = "seed"
            });
        }

        await db.SaveChangesAsync();
    }
}
