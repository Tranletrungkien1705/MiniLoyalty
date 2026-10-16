using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;
using MiniLoyalty.Models;

namespace MiniLoyalty.Models.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — Crd_DealUsePromotionController/Save (#022).
//
// Nguồn tra cứu (chỉ đọc):
//   - Header model:  12.Dev.Common/idn.Loyalty.Common/Models/Crd_DealUsePromotion.cs:1-18
//   - Header DDL:    11.BackEnd/V10/05.Refs.Biz/Migrate/20221006.QUYNHNTN/20221026.CreateDatabase.sql:574-592
//                    PK DealUsePrmNo.
//   - Detail model:  12.Dev.Common/idn.Loyalty.Common/Models/Crd_DealUsePromotionDtl.cs:1-36
//   - Detail DDL:    11.BackEnd/V10/05.Refs.Biz/Migrate/20221006.QUYNHNTN/20221026.CreateDatabase.sql:553-568
//                    PK composite (DealUsePrmNo, PrProgramCode).
//   - Sequence:      11.BackEnd/V10/idn.Loyalty.Biz/Seq.cs:147
//                    Seq_DealUsePrmNo, format "{0}DEALUSEPRMNO.{3}.{1:00000}{2}", max 100000.
// ===========================================================================================

/// <summary>
/// Bảng lưu thông tin giao dịch sử dụng ưu đãi — port 1:1 bảng Crd_DealUsePromotion (#022).
/// DDL: 20221026.CreateDatabase.sql:574-592. PK = DealUsePrmNo.
/// </summary>
public class Crd_DealUsePromotion
{
    public string DealUsePrmNo { get; set; } = "";             // PK — Mã lần giao dịch ưu đãi
    public string? NetworkID { get; set; }                      // NetworkID
    public string? OrgID { get; set; }                          // OrgID
    public string? DLCPCode { get; set; }                       // Mã đại lý
    public string? DealSerRONo { get; set; }                    // Số RO dịch vụ
    public DateTime? UsePrmDTime { get; set; }                  // Thời gian sử dụng dịch vụ
    public string? ProjectCode { get; set; }                    // Mã dự án
    public string? CardNo { get; set; }                         // Số thẻ (mã kỳ xét hạng)
    public string? CardType { get; set; }                       // Mã hạng thẻ
    public DateTime? LogLUDTimeUTC { get; set; }                // Thời điểm cập nhật UTC
    public string? LogLUBy { get; set; }                        // Người cập nhật
}

public class Crd_DealUsePromotionConfig : IEntityTypeConfiguration<Crd_DealUsePromotion>
{
    public void Configure(EntityTypeBuilder<Crd_DealUsePromotion> b)
    {
        b.ToTable("Crd_DealUsePromotion");
        b.HasKey(x => x.DealUsePrmNo);
        b.Property(x => x.DealUsePrmNo).IsRequired();
    }
}

/// <summary>
/// Bảng chi tiết lần sử dụng ưu đãi — port 1:1 bảng Crd_DealUsePromotionDtl (#022).
/// DDL: 20221026.CreateDatabase.sql:553-568. PK composite = (DealUsePrmNo, PrProgramCode).
/// </summary>
public class Crd_DealUsePromotionDtl
{
    public string DealUsePrmNo { get; set; } = "";             // PK composite 1 — Mã lần giao dịch ưu đãi
    public string PrProgramCode { get; set; } = "";            // PK composite 2 — Mã chương trình ưu đãi
    public string? NetworkID { get; set; }                      // NetworkID
    public string? OrgID { get; set; }                          // OrgID
    public double? QtyPr { get; set; }                          // Số lượng ưu đãi
    public double? QtyPrUsed { get; set; }                      // Số lượng ưu đãi đã sử dụng
    public double? QtyPrChTotal { get; set; }                   // Số lượng ưu đãi sử dụng lần này
    public DateTime? LogLUDTimeUTC { get; set; }                // Thời điểm cập nhật UTC
    public string? LogLUBy { get; set; }                        // Người cập nhật
}

public class Crd_DealUsePromotionDtlConfig : IEntityTypeConfiguration<Crd_DealUsePromotionDtl>
{
    public void Configure(EntityTypeBuilder<Crd_DealUsePromotionDtl> b)
    {
        b.ToTable("Crd_DealUsePromotionDtl");
        b.HasKey(x => new { x.DealUsePrmNo, x.PrProgramCode });
        b.Property(x => x.DealUsePrmNo).IsRequired();
        b.Property(x => x.PrProgramCode).IsRequired();
    }
}

/// <summary>
/// Sequence sinh số DealUsePrmNo — bảng Seq_DealUsePrmNo (Seq.cs:147).
/// Format: "{0}DEALUSEPRMNO.{3}.{1:00000}{2}", max 100000.
/// </summary>
public class Seq_DealUsePrmNo
{
    public long AutoId { get; set; }
    public string? Remark { get; set; }
}

public class Seq_DealUsePrmNoConfig : IEntityTypeConfiguration<Seq_DealUsePrmNo>
{
    public void Configure(EntityTypeBuilder<Seq_DealUsePrmNo> b)
    {
        b.ToTable("Seq_DealUsePrmNo");
        b.HasKey(x => x.AutoId);
        b.Property(x => x.AutoId).ValueGeneratedOnAdd();
    }
}

/// <summary>
/// Seeder dữ liệu mẫu cho Crd_DealUsePromotion (#022).
/// Order => 72 (chưa dùng trong dải 10-99). Idempotent.
/// </summary>
public class Crd_DealUsePromotionSeeder : IPortSeeder
{
    public int Order => 72;

    public async Task SeedAsync(AppDbContext db)
    {
        var now = DateTime.UtcNow;
        const string net = "1001";
        const string org = "1001";

        // 1. Crd_Member tương ứng với CARD_DEMO_01
        if (!await db.Set<CrdMember>().AnyAsync(x => x.MemberNo == "MEM_DEMO_01"))
        {
            db.Set<CrdMember>().Add(new CrdMember
            {
                MemberNo = "MEM_DEMO_01",
                NetworkID = net,
                OrgID = org,
                DLCodeRegis = "DL_HN01",
                MemberName = "Nguyễn Văn Demo",
                PhoneNo = "0901999999",
                CarNo = "30A-12345",
                VIN = "VIN_DEMO_001",
                MemberStatus = "APPROVE",
                RegisStatus = "FINISH",
                CardTypeUse = "GOLD",
                CardTypeInit = "GOLD",
                EffDateStart = now.AddYears(-1),
                EffDateEnd = now.AddYears(1),
                CreateDTimeUTC = now,
                CreateBy = "seed",
                LUDTimeUTC = now,
                LUBy = "seed",
                LogLUDTimeUTC = now,
                LogLUBy = "seed"
            });
        }

        // 2. Crd_Card CARD_DEMO_01 (CardStatus = APPROVE, còn hạn)
        if (!await db.Set<CrdCard>().AnyAsync(x => x.CardNo == "CARD_DEMO_01"))
        {
            db.Set<CrdCard>().Add(new CrdCard
            {
                CardNo = "CARD_DEMO_01",
                MemberNo = "MEM_DEMO_01",
                NetworkID = net,
                CardStatus = "APPROVE",
                CardTypeUse = "GOLD",
                CardTypeInit = "GOLD",
                CardSourceCode = "NEW",
                EffDateStart = now.AddYears(-1),
                EffDateEnd = now.AddYears(1),
                CreateDTimeUTC = now,
                CreateBy = "seed",
                LUDTimeUTC = now,
                LUBy = "seed",
                LogLUDTimeUTC = now,
                LogLUBy = "seed"
            });
        }

        // 2b. Crd_Card CARD_DEMO_PENDING (CardStatus = PENDING phục vụ Ca 3)
        if (!await db.Set<CrdCard>().AnyAsync(x => x.CardNo == "CARD_DEMO_PENDING"))
        {
            db.Set<CrdCard>().Add(new CrdCard
            {
                CardNo = "CARD_DEMO_PENDING",
                MemberNo = "MEM_DEMO_01",
                NetworkID = net,
                CardStatus = "PENDING",
                CardTypeUse = "GOLD",
                CardTypeInit = "GOLD",
                CardSourceCode = "NEW",
                EffDateStart = now.AddYears(-1),
                EffDateEnd = now.AddYears(1),
                CreateDTimeUTC = now,
                CreateBy = "seed",
                LUDTimeUTC = now,
                LUBy = "seed",
                LogLUDTimeUTC = now,
                LogLUBy = "seed"
            });
        }

        // 3. Mst_DealerCP DL_HN01 (FlagActive = 1)
        if (!await db.Set<Mst_DealerCP>().AnyAsync(x => x.DLCPCode == "DL_HN01"))
        {
            db.Set<Mst_DealerCP>().Add(new Mst_DealerCP
            {
                DLCPCode = "DL_HN01",
                DLCPName = "Đại lý Hà Nội 01",
                NetworkID = net,
                FlagActive = "1",
                LogLUDTimeUTC = now,
                LogLUBy = "seed"
            });
        }

        // 4. Mst_PromotionProgram PRM_DEMO_01 (FlagActive = 1, còn hạn)
        if (!await db.Set<Mst_PromotionProgram>().AnyAsync(x => x.PrProgramCode == "PRM_DEMO_01"))
        {
            db.Set<Mst_PromotionProgram>().Add(new Mst_PromotionProgram
            {
                PrProgramCode = "PRM_DEMO_01",
                PrProgramName = "Chương trình ưu đãi demo 01",
                NetworkID = net,
                OrgID = org,
                EffDateStart = now.AddYears(-1),
                EffDateEnd = now.AddYears(1),
                FlagActive = "1",
                FlagAllDL = "1",
                LogLUDTimeUTC = now,
                LogLUBy = "seed"
            });
        }

        // 4b. Mst_PromotionProgram PRM_DEMO_EXPIRED (FlagActive = 1, hết hạn hiệu lực phục vụ Ca 6)
        if (!await db.Set<Mst_PromotionProgram>().AnyAsync(x => x.PrProgramCode == "PRM_DEMO_EXPIRED"))
        {
            db.Set<Mst_PromotionProgram>().Add(new Mst_PromotionProgram
            {
                PrProgramCode = "PRM_DEMO_EXPIRED",
                PrProgramName = "Chương trình ưu đãi hết hạn",
                NetworkID = net,
                OrgID = org,
                EffDateStart = now.AddYears(-2),
                EffDateEnd = now.AddYears(-1),
                FlagActive = "1",
                FlagAllDL = "1",
                LogLUDTimeUTC = now,
                LogLUBy = "seed"
            });
        }

        // 5. Mst_PromotionProgramDtl PRM_DEMO_01, CardType = GOLD, Qty = 10.0
        if (!await db.Set<Mst_PromotionProgramDtl>().AnyAsync(x => x.PrProgramCode == "PRM_DEMO_01" && x.CardType == "GOLD"))
        {
            db.Set<Mst_PromotionProgramDtl>().Add(new Mst_PromotionProgramDtl
            {
                PrProgramCode = "PRM_DEMO_01",
                CardType = "GOLD",
                NetworkID = net,
                OrgID = org,
                Qty = 10.0,
                Unit = "Lượt",
                FlagActive = "1",
                LogLUDTimeUTC = now,
                LogLUBy = "seed"
            });
        }

        // 5b. Mst_PromotionProgramDtl PRM_DEMO_EXPIRED, CardType = GOLD, Qty = 10.0
        if (!await db.Set<Mst_PromotionProgramDtl>().AnyAsync(x => x.PrProgramCode == "PRM_DEMO_EXPIRED" && x.CardType == "GOLD"))
        {
            db.Set<Mst_PromotionProgramDtl>().Add(new Mst_PromotionProgramDtl
            {
                PrProgramCode = "PRM_DEMO_EXPIRED",
                CardType = "GOLD",
                NetworkID = net,
                OrgID = org,
                Qty = 10.0,
                Unit = "Lượt",
                FlagActive = "1",
                LogLUDTimeUTC = now,
                LogLUBy = "seed"
            });
        }

        // 6. Đảm bảo có tham số hệ thống UNITPOINTTOMONEY
        if (!await db.Set<Mst_ParamSys>().AnyAsync(x => x.ParamCode == "UNITPOINTTOMONEY"))
        {
            db.Set<Mst_ParamSys>().Add(new Mst_ParamSys
            {
                ParamCode = "UNITPOINTTOMONEY",
                NetworkID = net,
                ParamValue = "1",
                FlagActive = "1",
                LogLUDTimeUTC = now,
                LogLUBy = "seed"
            });
        }

        await db.SaveChangesAsync();
    }
}

/// <summary>
/// Seeder dữ liệu mẫu phục vụ kiểm thử Crd_DealUsePromotionController/Search (#023).
/// Order => 73 (chưa dùng trong dải 10-99). Idempotent.
/// </summary>
public class Crd_DealUsePromotionSearchSeeder : IPortSeeder
{
    public int Order => 73;

    public async Task SeedAsync(AppDbContext db)
    {
        const string net = "1001";
        const string user = "seed";

        var members = new[]
        {
            new { MemberNo = "MEM_PRM_01", MemberName = "Nguyen Van A", PhoneNo = "0988000001", MemberStatus = "APPROVE", LogLUDTime = DateTime.Parse("2026-10-01T10:00:00Z", null, System.Globalization.DateTimeStyles.RoundtripKind) },
            new { MemberNo = "MEM_PRM_02", MemberName = "Tran Thi B", PhoneNo = "0988000002", MemberStatus = "APPROVE", LogLUDTime = DateTime.Parse("2026-10-02T10:00:00Z", null, System.Globalization.DateTimeStyles.RoundtripKind) },
            new { MemberNo = "MEM_PRM_03", MemberName = "Le Van C", PhoneNo = "0988000003", MemberStatus = "APPROVE", LogLUDTime = DateTime.Parse("2026-10-03T10:00:00Z", null, System.Globalization.DateTimeStyles.RoundtripKind) },
            new { MemberNo = "MEM_PRM_04", MemberName = "Pham Van D", PhoneNo = "0988000001", MemberStatus = "APPROVE", LogLUDTime = DateTime.Parse("2026-10-04T10:00:00Z", null, System.Globalization.DateTimeStyles.RoundtripKind) },
        };

        foreach (var m in members)
        {
            if (!await db.Set<CrdMember>().AnyAsync(x => x.MemberNo == m.MemberNo))
            {
                db.Set<CrdMember>().Add(new CrdMember
                {
                    MemberNo = m.MemberNo,
                    MemberName = m.MemberName,
                    PhoneNo = m.PhoneNo,
                    MemberStatus = m.MemberStatus,
                    NetworkID = net,
                    CreateDTimeUTC = m.LogLUDTime,
                    CreateBy = user,
                    LUDTimeUTC = m.LogLUDTime,
                    LUBy = user,
                    LogLUDTimeUTC = m.LogLUDTime,
                    LogLUBy = user
                });
            }
        }

        var cards = new[]
        {
            new { CardNo = "CARD_PRM_01", MemberNo = "MEM_PRM_01", CardStatus = "APPROVE", CardTypeUse = "GOLD", LogLUDTime = DateTime.Parse("2026-10-01T10:00:00Z", null, System.Globalization.DateTimeStyles.RoundtripKind) },
            new { CardNo = "CARD_PRM_02", MemberNo = "MEM_PRM_02", CardStatus = "APPROVE", CardTypeUse = "SILVER", LogLUDTime = DateTime.Parse("2026-10-02T10:00:00Z", null, System.Globalization.DateTimeStyles.RoundtripKind) },
            new { CardNo = "CARD_PRM_03", MemberNo = "MEM_PRM_03", CardStatus = "APPROVE", CardTypeUse = "PLATINUM", LogLUDTime = DateTime.Parse("2026-10-03T10:00:00Z", null, System.Globalization.DateTimeStyles.RoundtripKind) },
            new { CardNo = "CARD_PRM_04", MemberNo = "MEM_PRM_04", CardStatus = "PENDING", CardTypeUse = "GOLD", LogLUDTime = DateTime.Parse("2026-10-04T10:00:00Z", null, System.Globalization.DateTimeStyles.RoundtripKind) },
        };

        foreach (var c in cards)
        {
            if (!await db.Set<CrdCard>().AnyAsync(x => x.CardNo == c.CardNo))
            {
                db.Set<CrdCard>().Add(new CrdCard
                {
                    CardNo = c.CardNo,
                    MemberNo = c.MemberNo,
                    CardStatus = c.CardStatus,
                    CardTypeUse = c.CardTypeUse,
                    CardTypeInit = c.CardTypeUse,
                    CardSourceCode = "NEW",
                    NetworkID = net,
                    CreateDTimeUTC = c.LogLUDTime,
                    CreateBy = user,
                    LUDTimeUTC = c.LogLUDTime,
                    LUBy = user,
                    LogLUDTimeUTC = c.LogLUDTime,
                    LogLUBy = user
                });
            }
        }

        await db.SaveChangesAsync();
    }
}

/// <summary>
/// Seeder dữ liệu mẫu cho Crd_DealUsePromotionController/GetForMemberNo (#024).
/// Thứ tự Order => 74 (chưa dùng trong dải 10-99). Idempotent.
/// </summary>
public class Crd_DealUsePromotionGetForMemberNoSeeder : IPortSeeder
{
    public int Order => 74;

    public async Task SeedAsync(AppDbContext db)
    {
        const string net = "1001";
        const string org = "1001";
        const string user = "seed";

        var d2024_01_01 = DateTime.Parse("2024-01-01T00:00:00Z", null, System.Globalization.DateTimeStyles.RoundtripKind);
        var d2025_01_01 = DateTime.Parse("2025-01-01T00:00:00Z", null, System.Globalization.DateTimeStyles.RoundtripKind);
        var d2026_01_01 = DateTime.Parse("2026-01-01T00:00:00Z", null, System.Globalization.DateTimeStyles.RoundtripKind);
        var d2026_01_02 = DateTime.Parse("2026-01-02T00:00:00Z", null, System.Globalization.DateTimeStyles.RoundtripKind);
        var d2026_01_03 = DateTime.Parse("2026-01-03T00:00:00Z", null, System.Globalization.DateTimeStyles.RoundtripKind);
        var d2026_05_10 = DateTime.Parse("2026-05-10T10:00:00Z", null, System.Globalization.DateTimeStyles.RoundtripKind);
        var d2026_05_15 = DateTime.Parse("2026-05-15T10:00:00Z", null, System.Globalization.DateTimeStyles.RoundtripKind);
        var d2026_06_15 = DateTime.Parse("2026-06-15T14:00:00Z", null, System.Globalization.DateTimeStyles.RoundtripKind);
        var d2026_07_01 = DateTime.Parse("2026-07-01T09:00:00Z", null, System.Globalization.DateTimeStyles.RoundtripKind);
        var d2026_08_20 = DateTime.Parse("2026-08-20T11:00:00Z", null, System.Globalization.DateTimeStyles.RoundtripKind);
        var d2027_12_31 = DateTime.Parse("2027-12-31T23:59:59Z", null, System.Globalization.DateTimeStyles.RoundtripKind);

        // 1. CrdMember (MEM_GFM_01, MEM_GFM_02, MEM_GFM_03)
        var members = new[]
        {
            new { MemberNo = "MEM_GFM_01", MemberName = "Tran Van GFM1", PhoneNo = "0911000001", IDCardNo = "001090000001", VIN = "VIN_GFM_001", CardTypeUse = "GOLD", DLCodeRegis = "DL_GFM_01" },
            new { MemberNo = "MEM_GFM_02", MemberName = "Le Thi GFM2", PhoneNo = "0911000002", IDCardNo = "001090000002", VIN = "VIN_GFM_002", CardTypeUse = "SILVER", DLCodeRegis = "DL_GFM_02" },
            new { MemberNo = "MEM_GFM_03", MemberName = "Pham Van GFM3", PhoneNo = "0911000003", IDCardNo = "001090000003", VIN = "VIN_GFM_003", CardTypeUse = "PLATINUM", DLCodeRegis = "DL_GFM_03" },
        };

        foreach (var m in members)
        {
            if (!await db.Set<CrdMember>().AnyAsync(x => x.MemberNo == m.MemberNo))
            {
                db.Set<CrdMember>().Add(new CrdMember
                {
                    MemberNo = m.MemberNo,
                    MemberName = m.MemberName,
                    PhoneNo = m.PhoneNo,
                    IDCardNo = m.IDCardNo,
                    VIN = m.VIN,
                    CardTypeUse = m.CardTypeUse,
                    CardTypeInit = m.CardTypeUse,
                    DLCodeRegis = m.DLCodeRegis,
                    MemberStatus = LoyaltyConst.MemberStatus.Approve,
                    RegisStatus = LoyaltyConst.RegisStatus.Finish,
                    NetworkID = net,
                    OrgID = org,
                    CreateDTimeUTC = d2026_01_01,
                    CreateBy = user,
                    LUDTimeUTC = d2026_01_01,
                    LUBy = user,
                    LogLUDTimeUTC = d2026_01_01,
                    LogLUBy = user
                });
            }
        }

        // 2. CrdCard (CARD_GFM_01, CARD_GFM_02, CARD_GFM_03)
        var cards = new[]
        {
            new { CardNo = "CARD_GFM_01", MemberNo = "MEM_GFM_01", CardTypeUse = "GOLD", CreateDTimeUTC = d2026_01_01 },
            new { CardNo = "CARD_GFM_02", MemberNo = "MEM_GFM_02", CardTypeUse = "SILVER", CreateDTimeUTC = d2026_01_02 },
            new { CardNo = "CARD_GFM_03", MemberNo = "MEM_GFM_03", CardTypeUse = "PLATINUM", CreateDTimeUTC = d2026_01_03 },
        };

        foreach (var c in cards)
        {
            if (!await db.Set<CrdCard>().AnyAsync(x => x.CardNo == c.CardNo))
            {
                db.Set<CrdCard>().Add(new CrdCard
                {
                    CardNo = c.CardNo,
                    MemberNo = c.MemberNo,
                    CardStatus = LoyaltyConst.CardStatus.Approve,
                    CardTypeUse = c.CardTypeUse,
                    CardTypeInit = c.CardTypeUse,
                    CardSourceCode = LoyaltyConst.CardSourceCode.New,
                    NetworkID = net,
                    EffDateStart = d2025_01_01,
                    EffDateEnd = d2027_12_31,
                    CreateDTimeUTC = c.CreateDTimeUTC,
                    CreateBy = user,
                    LUDTimeUTC = c.CreateDTimeUTC,
                    LUBy = user,
                    LogLUDTimeUTC = c.CreateDTimeUTC,
                    LogLUBy = user
                });
            }
        }

        // 3. Mst_DealerCP (DL_GFM_01, DL_GFM_02)
        var dealers = new[]
        {
            new { DLCPCode = "DL_GFM_01", DLCPName = "Dai ly GFM 01" },
            new { DLCPCode = "DL_GFM_02", DLCPName = "Dai ly GFM 02" },
        };

        foreach (var dl in dealers)
        {
            if (!await db.Set<Mst_DealerCP>().AnyAsync(x => x.DLCPCode == dl.DLCPCode))
            {
                db.Set<Mst_DealerCP>().Add(new Mst_DealerCP
                {
                    DLCPCode = dl.DLCPCode,
                    DLCPName = dl.DLCPName,
                    FlagActive = "1",
                    NetworkID = net,
                    LogLUDTimeUTC = d2026_01_01,
                    LogLUBy = user
                });
            }
        }

        // 4. Mst_PromotionProgram
        var programs = new[]
        {
            new { PrProgramCode = "PRM_GFM_ALL", PrProgramName = "Chuong trinh tat ca dai ly", FlagAllDL = "1", EffDateStart = d2025_01_01, EffDateEnd = d2027_12_31 },
            new { PrProgramCode = "PRM_GFM_SPEC", PrProgramName = "Chuong trinh chi dinh dai ly", FlagAllDL = "0", EffDateStart = d2025_01_01, EffDateEnd = d2027_12_31 },
            new { PrProgramCode = "PRM_GFM_EXPIRED", PrProgramName = "Chuong trinh het han", FlagAllDL = "1", EffDateStart = d2024_01_01, EffDateEnd = d2025_01_01 },
            new { PrProgramCode = "PRM_GFM_USEDUP", PrProgramName = "Chuong trinh da dung het", FlagAllDL = "1", EffDateStart = d2025_01_01, EffDateEnd = d2027_12_31 },
        };

        foreach (var p in programs)
        {
            if (!await db.Set<Mst_PromotionProgram>().AnyAsync(x => x.PrProgramCode == p.PrProgramCode))
            {
                db.Set<Mst_PromotionProgram>().Add(new Mst_PromotionProgram
                {
                    PrProgramCode = p.PrProgramCode,
                    PrProgramName = p.PrProgramName,
                    NetworkID = net,
                    OrgID = org,
                    FlagAllDL = p.FlagAllDL,
                    FlagActive = "1",
                    EffDateStart = p.EffDateStart,
                    EffDateEnd = p.EffDateEnd,
                    LogLUDTimeUTC = d2026_01_01,
                    LogLUBy = user
                });
            }
        }

        // Mst_PromotionProgramDtl
        var programDtls = new[]
        {
            new { PrProgramCode = "PRM_GFM_ALL", CardType = "GOLD", Qty = 10.0, Unit = "Luot", Remark = "Uu dai rua xe" },
            new { PrProgramCode = "PRM_GFM_ALL", CardType = "SILVER", Qty = 5.0, Unit = "Luot", Remark = "Uu dai rua xe" },
            new { PrProgramCode = "PRM_GFM_SPEC", CardType = "GOLD", Qty = 8.0, Unit = "Goi", Remark = "Uu dai bao duong" },
            new { PrProgramCode = "PRM_GFM_EXPIRED", CardType = "GOLD", Qty = 10.0, Unit = "Luot", Remark = "Uu dai het han" },
            new { PrProgramCode = "PRM_GFM_USEDUP", CardType = "GOLD", Qty = 3.0, Unit = "Luot", Remark = "Uu dai da dung het" },
        };

        foreach (var pd in programDtls)
        {
            if (!await db.Set<Mst_PromotionProgramDtl>().AnyAsync(x => x.PrProgramCode == pd.PrProgramCode && x.CardType == pd.CardType))
            {
                db.Set<Mst_PromotionProgramDtl>().Add(new Mst_PromotionProgramDtl
                {
                    PrProgramCode = pd.PrProgramCode,
                    CardType = pd.CardType,
                    Qty = pd.Qty,
                    Unit = pd.Unit,
                    Remark = pd.Remark,
                    FlagActive = "1",
                    NetworkID = net,
                    OrgID = org,
                    LogLUDTimeUTC = d2026_01_01,
                    LogLUBy = user
                });
            }
        }

        // Mst_PromotionProgramSpec
        if (!await db.Set<Mst_PromotionProgramSpec>().AnyAsync(x => x.PrProgramCode == "PRM_GFM_SPEC" && x.DLCode == "DL_GFM_01"))
        {
            db.Set<Mst_PromotionProgramSpec>().Add(new Mst_PromotionProgramSpec
            {
                PrProgramCode = "PRM_GFM_SPEC",
                DLCode = "DL_GFM_01",
                FlagActive = "1",
                NetworkID = net,
                OrgID = org,
                LogLUDTimeUTC = d2026_01_01,
                LogLUBy = user
            });
        }

        // 5. Crd_DealUsePromotion & Crd_DealUsePromotionDtl
        var deals = new[]
        {
            new { DealUsePrmNo = "DEAL_GFM_01", CardNo = "CARD_GFM_01", CardType = "GOLD", DLCPCode = "DL_GFM_01", DealSerRONo = "RO_GFM_01", UsePrmDTime = d2026_05_10, PrProgramCode = "PRM_GFM_ALL", QtyPr = 10.0, QtyPrChTotal = 2.0 },
            new { DealUsePrmNo = "DEAL_GFM_02", CardNo = "CARD_GFM_01", CardType = "GOLD", DLCPCode = "DL_GFM_01", DealSerRONo = "RO_GFM_02", UsePrmDTime = d2026_05_15, PrProgramCode = "PRM_GFM_USEDUP", QtyPr = 3.0, QtyPrChTotal = 3.0 },
        };

        foreach (var dl in deals)
        {
            if (!await db.Set<Crd_DealUsePromotion>().AnyAsync(x => x.DealUsePrmNo == dl.DealUsePrmNo))
            {
                db.Set<Crd_DealUsePromotion>().Add(new Crd_DealUsePromotion
                {
                    DealUsePrmNo = dl.DealUsePrmNo,
                    CardNo = dl.CardNo,
                    CardType = dl.CardType,
                    DLCPCode = dl.DLCPCode,
                    DealSerRONo = dl.DealSerRONo,
                    ProjectCode = "PRJ",
                    NetworkID = net,
                    OrgID = org,
                    UsePrmDTime = dl.UsePrmDTime,
                    LogLUDTimeUTC = dl.UsePrmDTime,
                    LogLUBy = user
                });
            }

            if (!await db.Set<Crd_DealUsePromotionDtl>().AnyAsync(x => x.DealUsePrmNo == dl.DealUsePrmNo && x.PrProgramCode == dl.PrProgramCode))
            {
                db.Set<Crd_DealUsePromotionDtl>().Add(new Crd_DealUsePromotionDtl
                {
                    DealUsePrmNo = dl.DealUsePrmNo,
                    PrProgramCode = dl.PrProgramCode,
                    QtyPr = dl.QtyPr,
                    QtyPrUsed = 0.0,
                    QtyPrChTotal = dl.QtyPrChTotal,
                    NetworkID = net,
                    OrgID = org,
                    LogLUDTimeUTC = dl.UsePrmDTime,
                    LogLUBy = user
                });
            }
        }

        // 6. CrdCardTransaction (TX_GFM_001, TX_GFM_002, TX_GFM_003, TX_GFM_004)
        var transactions = new[]
        {
            new { RefNo = "TX_GFM_001", MemberNo = "MEM_GFM_01", CardNo = "CARD_GFM_01", CardTypeUse = "GOLD", DealPointType = LoyaltyConst.DealPointType.PrProgram, PrProgramCode = (string?)"PRM_GFM_ALL", UsePrmDTime = (DateTime?)d2026_05_10, CreateDTimeUTC = d2026_05_10, QtyPrChTotal = (double?)1.0, PointChTotal = (double?)null, DLCode = "DL_GFM_01", FuncAction = "Use", RefType = "PROMOTION" },
            new { RefNo = "TX_GFM_002", MemberNo = "MEM_GFM_01", CardNo = "CARD_GFM_01", CardTypeUse = "GOLD", DealPointType = LoyaltyConst.DealPointType.PrProgram, PrProgramCode = (string?)"PRM_GFM_SPEC", UsePrmDTime = (DateTime?)d2026_06_15, CreateDTimeUTC = d2026_06_15, QtyPrChTotal = (double?)1.0, PointChTotal = (double?)null, DLCode = "DL_GFM_01", FuncAction = "Use", RefType = "PROMOTION" },
            new { RefNo = "TX_GFM_003", MemberNo = "MEM_GFM_01", CardNo = "CARD_GFM_01", CardTypeUse = "GOLD", DealPointType = LoyaltyConst.DealPointType.PointUse, PrProgramCode = (string?)null, UsePrmDTime = (DateTime?)d2026_07_01, CreateDTimeUTC = d2026_07_01, QtyPrChTotal = (double?)null, PointChTotal = (double?)100.0, DLCode = "DL_GFM_01", FuncAction = "AddPoint", RefType = "POINT" },
            new { RefNo = "TX_GFM_004", MemberNo = "MEM_GFM_02", CardNo = "CARD_GFM_02", CardTypeUse = "SILVER", DealPointType = LoyaltyConst.DealPointType.PrProgram, PrProgramCode = (string?)"PRM_GFM_ALL", UsePrmDTime = (DateTime?)d2026_08_20, CreateDTimeUTC = d2026_08_20, QtyPrChTotal = (double?)1.0, PointChTotal = (double?)null, DLCode = "DL_GFM_02", FuncAction = "Use", RefType = "PROMOTION" },
        };

        foreach (var tx in transactions)
        {
            if (!await db.Set<CrdCardTransaction>().AnyAsync(x => x.RefNo == tx.RefNo))
            {
                db.Set<CrdCardTransaction>().Add(new CrdCardTransaction
                {
                    RefNo = tx.RefNo,
                    MemberNo = tx.MemberNo,
                    CardNo = tx.CardNo,
                    CardTypeUse = tx.CardTypeUse,
                    CardTypeInit = tx.CardTypeUse,
                    DealPointType = tx.DealPointType,
                    PrProgramCode = tx.PrProgramCode,
                    UsePrmDTime = tx.UsePrmDTime,
                    CreateDTimeUTC = tx.CreateDTimeUTC,
                    QtyPrChTotal = tx.QtyPrChTotal,
                    PointChTotal = tx.PointChTotal,
                    ProjectCode = "PRJ",
                    NetworkID = net,
                    OrgID = org,
                    DLCode = tx.DLCode,
                    CardSourceCode = LoyaltyConst.CardSourceCode.New,
                    EffDateStart = d2025_01_01,
                    EffDateEnd = d2027_12_31,
                    FunctionName = "Crd_DealUsePromotion",
                    FunctionActionType = tx.FuncAction,
                    RefType = tx.RefType,
                    CreateBy = user,
                    LogLUDTimeUTC = tx.CreateDTimeUTC,
                    LogLUBy = user
                });
            }
        }

        await db.SaveChangesAsync();
    }
}
