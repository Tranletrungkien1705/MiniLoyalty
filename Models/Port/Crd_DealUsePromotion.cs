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
