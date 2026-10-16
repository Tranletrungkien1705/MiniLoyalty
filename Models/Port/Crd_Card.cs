using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;
using MiniLoyalty.Models;

namespace MiniLoyalty.Models.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — bảng Crd_Card (#007).
//
// Nguồn tra cứu (chỉ đọc):
//   - Model cột:  12.Dev.Common/idn.Loyalty.Common/Models/Loyalty/Crd_Card.cs:9-120
//   - DDL thật:   11.BackEnd/V10/05.Refs.Biz/Migrate/Script.HuongTTT/20221108.z11.Crd_Member.Crd_Card.sql:34-76
//                 PK CardNo tại :81.
// ===========================================================================================

/// <summary>
/// Thẻ hội viên / kỳ xét hạng — port 1:1 model Crd_Card.
/// Nguồn cột: Models/Loyalty/Crd_Card.cs:9-120 · DDL: 20221108.z11.Crd_Member.Crd_Card.sql:34-76.
/// PK = CardNo (DDL:81).
/// </summary>
public class Crd_Card
{
    // ---- Cột DDL (Crd_Card.sql:34-76) ----
    public string CardNo { get; set; } = "";             // Mã thẻ hội viên — Crd_Card.cs:11 (PK)
    public string? CardNoPrev { get; set; }              // Mã thẻ kỳ trước — :32
    public string? NetworkID { get; set; }               // NetworkID — :12
    public string MemberNo { get; set; } = "";           // Mã hội viên (FK Crd_Member) — :13 · DDL:196
    public string? CardSourceCode { get; set; }          // DOWN/KEEP/NEW/RENEW/UP — :15
    public string? RankPolicyCode { get; set; }          // Mã chính sách xếp hạng — :16
    public string? DLCodeExceptionally { get; set; }     // Đại lý đặc cách — :14
    public double? PointBonus { get; set; }              // Điểm thưởng — :17
    public double? PointPurchaseAddCar { get; set; }     // Điểm thưởng mua thêm xe — :18
    public DateTime? EffDateStart { get; set; }          // Ngày bắt đầu kỳ xét hạng — :19
    public DateTime? EffDateEnd { get; set; }            // Ngày hết hạn kỳ xét hạng — :20
    public double? PointTotal { get; set; }              // Điểm tiêu dùng trong kỳ — :21
    public double? PointBlock { get; set; }              // :22
    public double? PointAvail { get; set; }              // Điểm tiêu dùng còn lại — :23
    public double? AmountTotal { get; set; }             // Giá trị tích lũy trong kỳ — :24
    public double? AmountBlock { get; set; }             // :25
    public double? AmountAvail { get; set; }             // Doanh thu xét hạng trong kỳ — :26
    public double? QtyVisitTotal { get; set; }           // Số lượt dịch vụ — :27
    public double? QtyVisitBlock { get; set; }           // :28
    public double? QtyVisitAvail { get; set; }           // :29
    public double? PointCardRank { get; set; }           // Điểm xét hạng — :30
    public double? TotalAmountPeriod { get; set; }       // :31
    public string? CardTypeUse { get; set; }             // Hạng sử dụng — :33
    public string? CardTypeInit { get; set; }            // Hạng thực — :34
    public string? CardTypeUsePrev { get; set; }         // Hạng sử dụng kỳ trước — :35
    public string? CardTypeInitPrev { get; set; }        // Hạng thực kỳ trước — :36
    public DateTime? CardActiveDate { get; set; }        // Ngày kích hoạt thẻ — :37
    public string? FlagExceptionally { get; set; }       // Cờ đặc cách — :38
    public string? CardTypeUseExpect { get; set; }       // Hạng sử dụng dự kiến — :53 · DDL:62
    public string? CardTypeInitExpect { get; set; }      // Hạng thực dự kiến — :54 · DDL:63
    public string? RankActionType { get; set; }          // Loại hành động hạng: KEEP/DOWN/UP
    public DateTime? CreateDTimeUTC { get; set; }        // Thời điểm tạo thẻ — :39
    public string? CreateBy { get; set; }                // :40
    public DateTime? LUDTimeUTC { get; set; }            // :41
    public string? LUBy { get; set; }                    // :42
    public DateTime? ApproveDTimeUTC { get; set; }       // DDL:68
    public string? ApproveBy { get; set; }               // DDL:69
    public DateTime? CancelDTimeUTC { get; set; }        // DDL:70
    public string? CancelBy { get; set; }                // DDL:71
    public string? CardStatus { get; set; }              // PENDING/APPROVE/CANCEL — :43-46
    public string? Remark { get; set; }                  // :47
    public DateTime? LogLUDTimeUTC { get; set; }         // :48
    public string? LogLUBy { get; set; }                 // :49

    // ---- Cột mở rộng từ join Crd_Member & Master & RankPolicy (CardRank.cs:7460-7503) ----
    [NotMapped] public string? MemberName { get; set; }          // Tên hội viên — :56
    [NotMapped] public string? MemberStatus { get; set; }        // Trạng thái hội viên — :58
    [NotMapped] public string? IDCardNo { get; set; }            // Số CMND/CCCD — :59
    [NotMapped] public string? MemberGender { get; set; }        // Giới tính — :60
    [NotMapped] public string? MemAddress { get; set; }          // Địa chỉ — :61
    [NotMapped] public DateTime? DateOfBirth { get; set; }       // Ngày sinh — :62
    [NotMapped] public string? PhoneNo { get; set; }             // Số điện thoại — :63
    [NotMapped] public string? CarNo { get; set; }               // Biển số xe — :64
    [NotMapped] public string? VIN { get; set; }                 // Số khung — :65
    [NotMapped] public string? ProvinceName { get; set; }        // Tên tỉnh — :67
    [NotMapped] public string? ModelName { get; set; }           // Tên mẫu xe — :68
    [NotMapped] public string? ModelCode { get; set; }           // Mã mẫu xe — :69
    [NotMapped] public string? BrandName { get; set; }           // Hiệu xe — :70
    [NotMapped] public string? DistrictName { get; set; }        // Tên quận/huyện — :72
    [NotMapped] public string? ProvinceCode { get; set; }        // Mã tỉnh — :73
    [NotMapped] public string? DistrictCode { get; set; }        // Mã quận/huyện — :74
    [NotMapped] public DateTime? WarrantyExpiryDate { get; set; }// Hạn bảo hành — :75
    [NotMapped] public DateTime? InvoiceDate { get; set; }       // Ngày hóa đơn — :76
    [NotMapped] public DateTime? MemberActiveDate { get; set; }  // Ngày kích hoạt hội viên — :78
    [NotMapped] public DateTime? InactiveDTimeUTC { get; set; }  // Thời điểm Inactive — :80
    [NotMapped] public double? PointBegin { get; set; }          // Điểm đầu kỳ — :82
    [NotMapped] public double? PointEnd { get; set; }            // Điểm cuối kỳ — :83
    [NotMapped] public double? RevenueMiss { get; set; }         // Doanh thu còn thiếu tăng hạng — :85
    [NotMapped] public double? QtyVisitMiss { get; set; }        // Lượt dịch vụ còn thiếu tăng hạng — :86
    [NotMapped] public string? cc_FlagShow { get; set; }         // Cờ hiển thị thẻ — :87
    [NotMapped] public string? cm_FlagShow { get; set; }         // Cờ hiển thị hội viên — :91
    [NotMapped] public double? AmountEnd { get; set; }           // Giá trị sử dụng còn lại — :95
    [NotMapped] public string? mdcp_DLCPCode { get; set; }       // Mã đại lý đăng ký — :96 (DLCodeRegis :7482)
    [NotMapped] public string? mdcp_DLCPName { get; set; }       // Tên đại lý đăng ký — :97
    [NotMapped] public string? mcm_ModelName { get; set; }       // Tên model master — :99
    [NotMapped] public double? PointExpiry { get; set; }         // Điểm hết hạn — :101
    [NotMapped] public double? AmountRef { get; set; }           // Doanh thu tham chiếu — :102
    [NotMapped] public double? ConvertValue { get; set; }        // Giá trị quy đổi — :103
    [NotMapped] public double? ConvertPoint { get; set; }        // Điểm quy đổi — :104
    [NotMapped] public string? PolicyAmountUnit { get; set; }    // :105
    [NotMapped] public string? PolicyPointUnit { get; set; }     // :106
    [NotMapped] public string? PolicyCode { get; set; }          // :107
    [NotMapped] public string? ParamCode { get; set; }           // :108
    [NotMapped] public string? ParamPointUnit { get; set; }      // :109
    [NotMapped] public string? ParamAmountUnit { get; set; }     // :110
    [NotMapped] public double? PointAccumulated { get; set; }    // Điểm tích lũy — :112
    [NotMapped] public double? PointUse { get; set; }            // Điểm sử dụng — :113
    [NotMapped] public string? TradeMarkCode { get; set; }       // Hiệu xe — :115
    [NotMapped] public string? DLCodeRequest { get; set; }       // Đại lý yêu cầu — :116
    [NotMapped] public double? PointVoucher { get; set; }        // Điểm voucher — :117
    [NotMapped] public string? PurchaseType { get; set; }        // Loại mua xe — :118
    [NotMapped] public string? PurchaseTypeName { get; set; }    // Tên loại mua xe — :119

    // Các cột trả về từ bảng tạm tính hạng CardRank.cs:7470-7473, :7490
    [NotMapped] public double? QtyVisitUpBegin { get; set; }     // mrp.QtyVisitKeepBegin QtyVisitUpBegin — :7353, :7470
    [NotMapped] public double? PointKeepBegin { get; set; }      // mrp.PointKeepBegin PointKeepBegin — :7354, :7471
    [NotMapped] public string? MRP_CardType { get; set; }        // mrp.CardType MRP_CardType — :7355, :7472
    [NotMapped] public string? MRP_RankPolicyCode { get; set; }  // mrp.RankPolicyCode MRP_RankPolicyCode — :7356, :7473
    [NotMapped] public double? QtyVisitEnd { get; set; }         // cm.QtyVisitEnd QtyVisitEnd — :7490
}

public class Crd_CardConfig : IEntityTypeConfiguration<Crd_Card>
{
    public void Configure(EntityTypeBuilder<Crd_Card> b)
    {
        b.ToTable("Crd_Card");
        b.HasKey(x => x.CardNo);
        b.HasIndex(x => x.MemberNo);
        b.HasOne<MiniLoyalty.Models.CrdCard>()
         .WithOne()
         .HasForeignKey<Crd_Card>(x => x.CardNo);
    }
}

/// <summary>
/// Seed dữ liệu mẫu cho Crd_Card phục vụ tiêu chí tìm kiếm theo đề bài #007.
/// Order => 88, NetworkID 'DEMO', LogLUBy 'seed'. Idempotent.
/// </summary>
public class Crd_CardSeeder : IPortSeeder
{
    public int Order => 88;

    public async Task SeedAsync(AppDbContext db)
    {
        var now = new DateTime(2026, 10, 16, 12, 0, 0, DateTimeKind.Utc);
        var effStart = new DateTime(2026, 1, 1);
        var effEnd = new DateTime(2026, 12, 31);
        var effEndOther = new DateTime(2027, 6, 30);

        // 1. Đảm bảo Mst_RankPolicy có các chính sách phân cấp để tính hạng (CardRank.cs:7329-7455)
        var rankPolicies = await db.Set<Mst_RankPolicy>().ToListAsync();
        if (!rankPolicies.Any(p => p.CardType == "VIP" && p.RankPolicyCode == "RP2026"))
        {
            db.Set<Mst_RankPolicy>().Add(new Mst_RankPolicy
            {
                CardType = "VIP",
                RankPolicyCode = "RP2026",
                NetworkID = "DEMO",
                Value = 50,
                PointKeepBegin = 1000,
                QtyVisitKeepBegin = 2,
                PointUpBegin = 1500,
                PointUpEnd = 1999,
                QtyMonth = 12,
                FlagActive = "1",
                Remark = "Hạng VIP",
                LogLUDTimeUTC = now,
                LogLUBy = "seed"
            });
        }
        if (!rankPolicies.Any(p => p.CardType == "GOLD" && p.RankPolicyCode == "RP2026"))
        {
            db.Set<Mst_RankPolicy>().Add(new Mst_RankPolicy
            {
                CardType = "GOLD",
                RankPolicyCode = "RP2026",
                NetworkID = "DEMO",
                Value = 30,
                PointKeepBegin = 500,
                QtyVisitKeepBegin = 2,
                PointUpBegin = 1000,
                PointUpEnd = 1499,
                QtyMonth = 12,
                FlagActive = "1",
                Remark = "Hạng GOLD",
                LogLUDTimeUTC = now,
                LogLUBy = "seed"
            });
        }
        if (!rankPolicies.Any(p => p.CardType == "NORMAL" && p.RankPolicyCode == "RP2026"))
        {
            db.Set<Mst_RankPolicy>().Add(new Mst_RankPolicy
            {
                CardType = "NORMAL",
                RankPolicyCode = "RP2026",
                NetworkID = "DEMO",
                Value = 10,
                PointKeepBegin = 0,
                QtyVisitKeepBegin = 0,
                PointUpBegin = 500,
                PointUpEnd = 999,
                QtyMonth = 12,
                FlagActive = "1",
                Remark = "Hạng NORMAL",
                LogLUDTimeUTC = now,
                LogLUBy = "seed"
            });
        }

        // Cập nhật Value và ngưỡng duy trì cho các hạng N, I, S, G, P nếu đang là 999

        // 2. Hội viên mẫu cho demo
        if (!await db.CrdMembers.AnyAsync(m => m.MemberNo == "MEM01"))
        {
            db.CrdMembers.Add(new CrdMember
            {
                MemberNo = "MEM01",
                NetworkID = "DEMO",
                OrgID = "DEMO",
                DLCodeRegis = "VS058",
                MemberName = "Nguyễn Văn VIP",
                PhoneNo = "0901000001",
                CarNo = "30A-99999",
                VIN = "VIN000000001",
                MemberStatus = "APPROVE",
                CardTypeUse = "VIP",
                CardTypeInit = "VIP",
                EffDateStart = effStart,
                EffDateEnd = effEnd,
                FlagExceptionally = "0",
                PointCardRank = 1200, // >= 1000 -> KEEP
                QtyVisitAvail = 4,   // >= 2 -> KEEP
                PointAvail = 800,
                PointEnd = 1000,
                AmountEnd = 50_000_000,
                QtyVisitEnd = 5,
                CreateDTimeUTC = now,
                CreateBy = "seed",
                LUDTimeUTC = now,
                LUBy = "seed",
                LogLUDTimeUTC = now,
                LogLUBy = "seed"
            });
        }

        if (!await db.CrdMembers.AnyAsync(m => m.MemberNo == "MEM02"))
        {
            db.CrdMembers.Add(new CrdMember
            {
                MemberNo = "MEM02",
                NetworkID = "DEMO",
                OrgID = "DEMO",
                DLCodeRegis = "VS058",
                MemberName = "Trần Thị Gold",
                PhoneNo = "0902000002",
                CarNo = "30A-88888",
                VIN = "VIN000000002",
                MemberStatus = "APPROVE",
                CardTypeUse = "GOLD",
                CardTypeInit = "GOLD",
                EffDateStart = effStart,
                EffDateEnd = effEnd,
                FlagExceptionally = "0",
                PointCardRank = 200, // < 500 -> DOWN
                QtyVisitAvail = 1,   // < 2 -> DOWN
                PointAvail = 200,
                PointEnd = 300,
                AmountEnd = 20_000_000,
                QtyVisitEnd = 2,
                CreateDTimeUTC = now,
                CreateBy = "seed",
                LUDTimeUTC = now,
                LUBy = "seed",
                LogLUDTimeUTC = now,
                LogLUBy = "seed"
            });
        }

        if (!await db.CrdMembers.AnyAsync(m => m.MemberNo == "MEM03"))
        {
            db.CrdMembers.Add(new CrdMember
            {
                MemberNo = "MEM03",
                NetworkID = "DEMO",
                OrgID = "DEMO",
                DLCodeRegis = "VS045",
                MemberName = "Lê Văn Hủy",
                PhoneNo = "0903000003",
                CarNo = "30A-77777",
                VIN = "VIN000000003",
                MemberStatus = "CANCEL",
                CardTypeUse = "VIP",
                CardTypeInit = "VIP",
                EffDateStart = effStart,
                EffDateEnd = effEnd,
                FlagExceptionally = "0",
                PointCardRank = 0,
                QtyVisitAvail = 0,
                CreateDTimeUTC = now,
                CreateBy = "seed",
                LUDTimeUTC = now,
                LUBy = "seed",
                LogLUDTimeUTC = now,
                LogLUBy = "seed"
            });
        }

        if (!await db.CrdMembers.AnyAsync(m => m.MemberNo == "MEM04"))
        {
            db.CrdMembers.Add(new CrdMember
            {
                MemberNo = "MEM04",
                NetworkID = "DEMO",
                OrgID = "DEMO",
                DLCodeRegis = "VS065",
                MemberName = "Phạm Thị Đặc Cách",
                PhoneNo = "0904000004",
                CarNo = "30A-44444",
                VIN = "VIN000000004",
                MemberStatus = "APPROVE",
                CardTypeUse = "VIP",
                CardTypeInit = "VIP",
                EffDateStart = effStart,
                EffDateEnd = effEndOther, // 2027-06-30
                FlagExceptionally = "1",
                PointCardRank = 1500,
                QtyVisitAvail = 5,
                PointAvail = 900,
                PointEnd = 1200,
                AmountEnd = 60_000_000,
                QtyVisitEnd = 6,
                CreateDTimeUTC = now,
                CreateBy = "seed",
                LUDTimeUTC = now,
                LUBy = "seed",
                LogLUDTimeUTC = now,
                LogLUBy = "seed"
            });
        }

        // 3. Thẻ CrdCard tương ứng
        if (!await db.CrdCards.AnyAsync(c => c.CardNo == "CARD01"))
        {
            db.CrdCards.Add(new CrdCard
            {
                CardNo = "CARD01",
                MemberNo = "MEM01",
                NetworkID = "DEMO",
                CardTypeUse = "VIP",
                CardTypeInit = "VIP",
                CardStatus = "APPROVE",
                CardSourceCode = "NEW",
                RankPolicyCode = "RP2026",
                EffDateStart = effStart,
                EffDateEnd = effEnd,
                PointBonus = 100,
                PointTotal = 1000,
                PointAvail = 800,
                AmountTotal = 50_000_000,
                AmountAvail = 40_000_000,
                QtyVisitTotal = 5,
                QtyVisitAvail = 4,
                PointCardRank = 1200,
                TotalAmountPeriod = 50_000_000,
                CardActiveDate = effStart,
                FlagExceptionally = "0",
                RankActionType = "KEEP",
                CardTypeUseExpect = "VIP",
                CardTypeInitExpect = "VIP",
                CreateDTimeUTC = now,
                CreateBy = "seed",
                LUDTimeUTC = now,
                LUBy = "seed",
                LogLUDTimeUTC = now,
                LogLUBy = "seed",
                Remark = "Demo CARD01 VIP APPROVE (KEEP)"
            });
        }

        if (!await db.CrdCards.AnyAsync(c => c.CardNo == "CARD02"))
        {
            db.CrdCards.Add(new CrdCard
            {
                CardNo = "CARD02",
                MemberNo = "MEM02",
                NetworkID = "DEMO",
                CardTypeUse = "GOLD",
                CardTypeInit = "GOLD",
                CardStatus = "APPROVE",
                CardSourceCode = "NEW",
                RankPolicyCode = "RP2026",
                EffDateStart = effStart,
                EffDateEnd = effEnd,
                PointBonus = 50,
                PointTotal = 500,
                PointAvail = 200,
                AmountTotal = 25_000_000,
                AmountAvail = 15_000_000,
                QtyVisitTotal = 3,
                QtyVisitAvail = 1,
                PointCardRank = 200,
                TotalAmountPeriod = 25_000_000,
                CardActiveDate = effStart,
                FlagExceptionally = "0",
                RankActionType = "DOWN",
                CardTypeUseExpect = "NORMAL",
                CardTypeInitExpect = "NORMAL",
                CreateDTimeUTC = now,
                CreateBy = "seed",
                LUDTimeUTC = now,
                LUBy = "seed",
                LogLUDTimeUTC = now,
                LogLUBy = "seed",
                Remark = "Demo CARD02 GOLD APPROVE (DOWN)"
            });
        }

        if (!await db.CrdCards.AnyAsync(c => c.CardNo == "CARD03"))
        {
            db.CrdCards.Add(new CrdCard
            {
                CardNo = "CARD03",
                MemberNo = "MEM03",
                NetworkID = "DEMO",
                CardTypeUse = "VIP",
                CardTypeInit = "VIP",
                CardStatus = "CANCEL", // Khác APPROVE -> không bao giờ trả về
                CardSourceCode = "NEW",
                RankPolicyCode = "RP2026",
                EffDateStart = effStart,
                EffDateEnd = effEnd,
                PointBonus = 0,
                PointTotal = 0,
                PointAvail = 0,
                AmountTotal = 0,
                AmountAvail = 0,
                QtyVisitTotal = 0,
                QtyVisitAvail = 0,
                PointCardRank = 0,
                TotalAmountPeriod = 0,
                FlagExceptionally = "0",
                RankActionType = "DOWN",
                CardTypeUseExpect = "NORMAL",
                CardTypeInitExpect = "NORMAL",
                CreateDTimeUTC = now,
                CreateBy = "seed",
                LUDTimeUTC = now,
                LUBy = "seed",
                LogLUDTimeUTC = now,
                LogLUBy = "seed",
                Remark = "Demo CARD03 VIP CANCEL"
            });
        }

        if (!await db.CrdCards.AnyAsync(c => c.CardNo == "CARD04"))
        {
            db.CrdCards.Add(new CrdCard
            {
                CardNo = "CARD04",
                MemberNo = "MEM04",
                NetworkID = "DEMO",
                CardTypeUse = "VIP",
                CardTypeInit = "VIP",
                CardStatus = "APPROVE",
                CardSourceCode = "NEW",
                RankPolicyCode = "RP2026",
                EffDateStart = effStart,
                EffDateEnd = effEndOther, // 2027-06-30
                PointBonus = 200,
                PointTotal = 2000,
                PointAvail = 900,
                AmountTotal = 80_000_000,
                AmountAvail = 60_000_000,
                QtyVisitTotal = 6,
                QtyVisitAvail = 5,
                PointCardRank = 1500,
                TotalAmountPeriod = 80_000_000,
                CardActiveDate = effStart,
                FlagExceptionally = "1",
                RankActionType = "KEEP",
                CardTypeUseExpect = "VIP",
                CardTypeInitExpect = "VIP",
                CreateDTimeUTC = now,
                CreateBy = "seed",
                LUDTimeUTC = now,
                LUBy = "seed",
                LogLUDTimeUTC = now,
                LogLUBy = "seed",
                Remark = "Demo CARD04 VIP APPROVE đặc cách"
            });
        }

        await db.SaveChangesAsync();
    }
}
