using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;
using MiniLoyalty.Models;

namespace MiniLoyalty.Models.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — Bảng phụ RptPE_CrdMemberPointExpiryMember (#001).
// Nguồn tra cứu (chỉ đọc):
//   - Câu truy vấn Crd_Card_GetX_New20251229 (Master.Loyalty.cs:19556-19566)
//   - Tóm tắt điểm hết hạn của hội viên (PointExpiry)
// ===========================================================================================

/// <summary>
/// Bảng điểm hết hạn của hội viên — port 1:1 bảng <c>RptPE_CrdMemberPointExpiryMember</c>.
/// Dùng trong câu truy vấn Crd_Card_GetX_New20251229 (Master.Loyalty.cs:19556-19566).
/// </summary>
public class RptPE_CrdMemberPointExpiryMember
{
    public string MemberNo { get; set; } = "";          // Mã hội viên (PK)
    public double? PointExpiry { get; set; }             // Điểm hết hạn
    public string? NetworkID { get; set; }              // Mã mạng
    public string? LogLUBy { get; set; }                // Người cập nhật
    public DateTime? LogLUDTimeUTC { get; set; }        // Thời điểm cập nhật UTC
}

/// <summary>Cấu hình EF cho <see cref="RptPE_CrdMemberPointExpiryMember"/>.</summary>
public class RptPE_CrdMemberPointExpiryMemberConfig : IEntityTypeConfiguration<RptPE_CrdMemberPointExpiryMember>
{
    public void Configure(EntityTypeBuilder<RptPE_CrdMemberPointExpiryMember> b)
    {
        b.ToTable("RptPE_CrdMemberPointExpiryMember");
        b.HasKey(x => x.MemberNo);
        b.Property(x => x.MemberNo).HasMaxLength(50).IsRequired();
        b.Property(x => x.NetworkID).HasMaxLength(50);
        b.Property(x => x.LogLUBy).HasMaxLength(50);
    }
}

/// <summary>
/// Seeder dữ liệu mẫu cho hạng mục #001 (Crd_CardController/Search).
/// Chạy với Order => 89 (duy nhất trong 10–99).
/// Idempotent: kiểm tra đã có dữ liệu MemberNo = 'MB001' trước khi thêm.
/// KHÔNG sửa dữ liệu của seeder khác, chỉ thêm dòng mới khớp với Crd_Card/Crd_Member.
/// </summary>
public class RptPE_CrdMemberPointExpiryMemberSeeder : IPortSeeder
{
    public int Order => 89; // Duy nhất trong 10–99

    public async Task SeedAsync(AppDbContext db)
    {
        var now = DateTime.UtcNow;
        const string net = "DEMO";

        // 1. Dữ liệu hội viên MB001, MB002, MB003 theo đề bài #001
        if (!await db.CrdMembers.AnyAsync(x => x.MemberNo == "MB001"))
        {
            db.CrdMembers.AddRange(
                new CrdMember
                {
                    MemberNo = "MB001",
                    NetworkID = net,
                    OrgID = net,
                    DLCodeRegis = "DL01",
                    RegistrationDate = now.AddMonths(-6),
                    CustomerCode = "KH001",
                    ProvinceCode = "HN",
                    DistrictCode = "HN01",
                    MemberName = "Nguyễn Văn MB001",
                    MemberGender = "1",
                    IDCardNo = "001090001001",
                    PhoneNo = "0988111001",
                    DateOfBirth = new DateTime(1990, 1, 1),
                    MemAddress = "Hà Nội",
                    CarNo = "30A-99001",
                    VIN = "VINMB000000000001",
                    ModelCode = "ACC",
                    ModelName = "Accent",
                    TradeMarkCode = "HYU",
                    EffDateStart = now.AddMonths(-6),
                    EffDateEnd = now.AddMonths(6),
                    PointTotal = 1000,
                    PointAvail = 800,
                    PointBegin = 0,
                    PointEnd = 800,
                    AmountTotal = 10000000,
                    AmountAvail = 10000000,
                    AmountBegin = 0,
                    AmountEnd = 10000000,
                    QtyVisitTotal = 2,
                    QtyVisitAvail = 2,
                    PointCardRank = 1000,
                    AmountCardRank = 10000000,
                    CardTypeUse = "P",
                    CardTypeInit = "P",
                    MemberActiveDate = now.AddMonths(-6),
                    FlagExceptionally = "0",
                    FlagExceptionCard = "0",
                    PointVoucher = 50,
                    MemberStatus = "APPROVE",
                    CardStatus = "APPROVE",
                    CardSourceCode = "NEW",
                    PurchaseType = "BUYNEW",
                    CreateDTimeUTC = now.AddMonths(-6),
                    CreateBy = "seed",
                    LUDTimeUTC = now,
                    LUBy = "seed",
                    LogLUDTimeUTC = now,
                    LogLUBy = "seed"
                },
                new CrdMember
                {
                    MemberNo = "MB002",
                    NetworkID = net,
                    OrgID = net,
                    DLCodeRegis = "DL01",
                    RegistrationDate = now.AddMonths(-3),
                    CustomerCode = "KH002",
                    ProvinceCode = "HN",
                    DistrictCode = "HN01",
                    MemberName = "Trần Thị MB002",
                    MemberGender = "0",
                    IDCardNo = "001090001002",
                    PhoneNo = "0988111002",
                    DateOfBirth = new DateTime(1992, 5, 10),
                    MemAddress = "Hà Nội",
                    CarNo = "30A-99002",
                    VIN = "VINMB000000000002",
                    ModelCode = "ACC",
                    ModelName = "Accent",
                    TradeMarkCode = "HYU",
                    EffDateStart = now.AddMonths(-3),
                    EffDateEnd = now.AddMonths(9),
                    PointTotal = 500,
                    PointAvail = 500,
                    PointBegin = 0,
                    PointEnd = 500,
                    AmountTotal = 5000000,
                    AmountAvail = 5000000,
                    AmountBegin = 0,
                    AmountEnd = 5000000,
                    QtyVisitTotal = 1,
                    QtyVisitAvail = 1,
                    PointCardRank = 500,
                    AmountCardRank = 5000000,
                    CardTypeUse = "G",
                    CardTypeInit = "G",
                    MemberActiveDate = now.AddMonths(-3),
                    FlagExceptionally = "0",
                    FlagExceptionCard = "0",
                    PointVoucher = 0,
                    MemberStatus = "PENDING",
                    CardStatus = "PENDING",
                    CardSourceCode = "NEW",
                    PurchaseType = "BUYADD",
                    CreateDTimeUTC = now.AddMonths(-3),
                    CreateBy = "seed",
                    LUDTimeUTC = now,
                    LUBy = "seed",
                    LogLUDTimeUTC = now,
                    LogLUBy = "seed"
                },
                new CrdMember
                {
                    MemberNo = "MB003",
                    NetworkID = net,
                    OrgID = net,
                    DLCodeRegis = "DL01",
                    RegistrationDate = now.AddMonths(-1),
                    CustomerCode = "KH003",
                    ProvinceCode = "HN",
                    DistrictCode = "HN01",
                    MemberName = "Lê Văn MB003",
                    MemberGender = "1",
                    IDCardNo = "001090001003",
                    PhoneNo = "0988111003",
                    DateOfBirth = new DateTime(1988, 8, 20),
                    MemAddress = "Hà Nội",
                    CarNo = "30A-99003",
                    VIN = "VINMB000000000003",
                    ModelCode = "ACC",
                    ModelName = "Accent",
                    TradeMarkCode = "HYU",
                    EffDateStart = now.AddMonths(-1),
                    EffDateEnd = now.AddMonths(11),
                    PointTotal = 100,
                    PointAvail = 100,
                    PointBegin = 0,
                    PointEnd = 100,
                    AmountTotal = 1000000,
                    AmountAvail = 1000000,
                    AmountBegin = 0,
                    AmountEnd = 1000000,
                    QtyVisitTotal = 0,
                    QtyVisitAvail = 0,
                    PointCardRank = 100,
                    AmountCardRank = 1000000,
                    CardTypeUse = "S",
                    CardTypeInit = "S",
                    MemberActiveDate = now.AddMonths(-1),
                    FlagExceptionally = "0",
                    FlagExceptionCard = "0",
                    PointVoucher = 0,
                    MemberStatus = "CANCEL",
                    CardStatus = "CANCEL",
                    CardSourceCode = "NEW",
                    PurchaseType = "BUYUSED",
                    CreateDTimeUTC = now.AddMonths(-1),
                    CreateBy = "seed",
                    LUDTimeUTC = now,
                    LUBy = "seed",
                    LogLUDTimeUTC = now,
                    LogLUBy = "seed"
                }
            );
        }

        // 2. Thẻ CrdCard tương ứng (CD001, CD002, CD003)
        if (!await db.CrdCards.AnyAsync(x => x.CardNo == "CD001"))
        {
            db.CrdCards.AddRange(
                new CrdCard
                {
                    CardNo = "CD001",
                    MemberNo = "MB001",
                    NetworkID = net,
                    CardSourceCode = "NEW",
                    RankPolicyCode = "RP2026",
                    PointTotal = 1000,
                    PointAvail = 800,
                    AmountTotal = 10000000,
                    AmountAvail = 10000000,
                    QtyVisitTotal = 2,
                    QtyVisitAvail = 2,
                    PointCardRank = 1000,
                    TotalAmountPeriod = 10000000,
                    CardTypeUse = "P",
                    CardTypeInit = "P",
                    CardActiveDate = now.AddMonths(-6),
                    FlagExceptionally = "0",
                    CardStatus = "APPROVE",
                    ApproveDTimeUTC = now.AddMonths(-6),
                    ApproveBy = "seed",
                    CreateDTimeUTC = now.AddMonths(-6),
                    CreateBy = "seed",
                    LUDTimeUTC = now,
                    LUBy = "seed",
                    LogLUDTimeUTC = now,
                    LogLUBy = "seed"
                },
                new CrdCard
                {
                    CardNo = "CD002",
                    MemberNo = "MB002",
                    NetworkID = net,
                    CardSourceCode = "NEW",
                    RankPolicyCode = "RP2026",
                    PointTotal = 500,
                    PointAvail = 500,
                    AmountTotal = 5000000,
                    AmountAvail = 5000000,
                    QtyVisitTotal = 1,
                    QtyVisitAvail = 1,
                    PointCardRank = 500,
                    TotalAmountPeriod = 5000000,
                    CardTypeUse = "G",
                    CardTypeInit = "G",
                    CardActiveDate = now.AddMonths(-3),
                    FlagExceptionally = "0",
                    CardStatus = "PENDING",
                    CreateDTimeUTC = now.AddMonths(-3),
                    CreateBy = "seed",
                    LUDTimeUTC = now,
                    LUBy = "seed",
                    LogLUDTimeUTC = now,
                    LogLUBy = "seed"
                },
                new CrdCard
                {
                    CardNo = "CD003",
                    MemberNo = "MB003",
                    NetworkID = net,
                    CardSourceCode = "NEW",
                    RankPolicyCode = "RP2026",
                    PointTotal = 100,
                    PointAvail = 100,
                    AmountTotal = 1000000,
                    AmountAvail = 1000000,
                    QtyVisitTotal = 0,
                    QtyVisitAvail = 0,
                    PointCardRank = 100,
                    TotalAmountPeriod = 1000000,
                    CardTypeUse = "S",
                    CardTypeInit = "S",
                    CardActiveDate = now.AddMonths(-1),
                    FlagExceptionally = "0",
                    CardStatus = "CANCEL",
                    CancelDTimeUTC = now.AddMonths(-1),
                    CancelBy = "seed",
                    CreateDTimeUTC = now.AddMonths(-1),
                    CreateBy = "seed",
                    LUDTimeUTC = now,
                    LUBy = "seed",
                    LogLUDTimeUTC = now,
                    LogLUBy = "seed"
                }
            );
        }

        // 3. Map_QueryDealer_Member cho MB001
        var mqdmSet = db.Set<Map_QueryDealer_Member>();
        if (!await mqdmSet.AnyAsync(x => x.DLCPCode == "DL01" && x.MemberNo == "MB001"))
        {
            mqdmSet.Add(new Map_QueryDealer_Member
            {
                DLCPCode = "DL01",
                MemberNo = "MB001",
                NetworkID = net,
                QueryDate = now,
                FlagActive = "1",
                LogLUBy = "seed",
                LogLUDTimeUTC = now
            });
        }

        // 4. RptPE_CrdMemberPointExpiryMember cho MB001
        var rptSet = db.Set<RptPE_CrdMemberPointExpiryMember>();
        if (!await rptSet.AnyAsync(x => x.MemberNo == "MB001"))
        {
            rptSet.Add(new RptPE_CrdMemberPointExpiryMember
            {
                MemberNo = "MB001",
                PointExpiry = 50,
                NetworkID = net,
                LogLUBy = "seed",
                LogLUDTimeUTC = now
            });
        }

        // 5. CrdCardTransactions cho CD001
        if (!await db.CrdCardTransactions.AnyAsync(x => x.CardNo == "CD001"))
        {
            db.CrdCardTransactions.AddRange(
                new CrdCardTransaction
                {
                    RefNo = "RO001",
                    ProjectCode = "LOYALTY",
                    NetworkID = net,
                    OrgID = net,
                    DLCode = "DL01",
                    MemberNo = "MB001",
                    CardNo = "CD001",
                    CardSourceCode = "NEW",
                    CardTypeUse = "P",
                    CardTypeInit = "P",
                    DealPointType = "POINTINCREASE",
                    PointChTotal = 100,
                    AmountChTotal = 100,
                    PointRef = 100,
                    AmountRef = 5000000,
                    CreateDTimeUTC = now.AddDays(-10),
                    CreateBy = "seed",
                    LUDTimeUTC = now,
                    LUBy = "seed",
                    LogLUDTimeUTC = now,
                    LogLUBy = "seed"
                },
                new CrdCardTransaction
                {
                    RefNo = "RO002",
                    ProjectCode = "LOYALTY",
                    NetworkID = net,
                    OrgID = net,
                    DLCode = "DL01",
                    MemberNo = "MB001",
                    CardNo = "CD001",
                    CardSourceCode = "NEW",
                    CardTypeUse = "P",
                    CardTypeInit = "P",
                    DealPointType = "POINTUSE",
                    PointChTotal = 200,
                    AmountChTotal = 200,
                    PointRef = 0,
                    AmountRef = 0,
                    CreateDTimeUTC = now.AddDays(-5),
                    CreateBy = "seed",
                    LUDTimeUTC = now,
                    LUBy = "seed",
                    LogLUDTimeUTC = now,
                    LogLUBy = "seed"
                }
            );
        }

        // 6. Đảm bảo Mst_PolicyMoneyToPointServiceDtl có đủ các hạng P, G, S, VIP, GOLD, NORMAL
        var policyDtlSet = db.Set<Mst_PolicyMoneyToPointServiceDtl>();
        var neededTypes = new[] { "P", "G", "S", "VIP", "GOLD", "NORMAL" };
        foreach (var ct in neededTypes)
        {
            if (!await policyDtlSet.AnyAsync(x => x.CardType == ct && x.FlagActive == "1"))
            {
                policyDtlSet.Add(new Mst_PolicyMoneyToPointServiceDtl
                {
                    PolicyCode = "P01",
                    CardType = ct,
                    NetworkID = net,
                    ConvertValue = 100000,
                    ConvertPoint = 1,
                    FlagActive = "1",
                    LogLUBy = "seed",
                    LogLUDTimeUTC = now
                });
            }
        }

        // 7. Đảm bảo Mst_RankPolicy có các hạng P, G, S
        var rankPolicySet = db.Set<Mst_RankPolicy>();
        if (!await rankPolicySet.AnyAsync(x => x.CardType == "P"))
        {
            rankPolicySet.Add(new Mst_RankPolicy
            {
                RankPolicyCode = "RP2026",
                CardType = "P",
                NetworkID = net,
                PointUpBegin = 1000,
                PointUpEnd = 9999,
                QtyVisitUpBegin = 10,
                QtyVisitUpEnd = 99,
                FlagActive = "1",
                LogLUBy = "seed",
                LogLUDTimeUTC = now
            });
        }
        if (!await rankPolicySet.AnyAsync(x => x.CardType == "G"))
        {
            rankPolicySet.Add(new Mst_RankPolicy
            {
                RankPolicyCode = "RP2026",
                CardType = "G",
                NetworkID = net,
                PointUpBegin = 800,
                PointUpEnd = 999,
                QtyVisitUpBegin = 5,
                QtyVisitUpEnd = 9,
                FlagActive = "1",
                LogLUBy = "seed",
                LogLUDTimeUTC = now
            });
        }
        if (!await rankPolicySet.AnyAsync(x => x.CardType == "S"))
        {
            rankPolicySet.Add(new Mst_RankPolicy
            {
                RankPolicyCode = "RP2026",
                CardType = "S",
                NetworkID = net,
                PointUpBegin = 600,
                PointUpEnd = 799,
                QtyVisitUpBegin = 3,
                QtyVisitUpEnd = 4,
                FlagActive = "1",
                LogLUBy = "seed",
                LogLUDTimeUTC = now
            });
        }

        // 8. Đảm bảo Mst_DealerCP có DL01
        var dealerSet = db.Set<Mst_DealerCP>();
        if (!await dealerSet.AnyAsync(x => x.DLCPCode == "DL01"))
        {
            dealerSet.Add(new Mst_DealerCP
            {
                DLCPCode = "DL01",
                NetworkID = net,
                DLCPName = "Đại lý Hà Nội 01",
                FlagActive = "1",
                LogLUBy = "seed",
                LogLUDTimeUTC = now
            });
        }

        await db.SaveChangesAsync();
    }
}
