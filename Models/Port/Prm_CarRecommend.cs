using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;

namespace MiniLoyalty.Models.Port;

public class Prm_CarRecommend
{
    public string PRMCRCodeSys { get; set; } = "";
    public string NetworkID { get; set; } = "";
    public string OrgID { get; set; } = "";
    public string DLCPCode { get; set; } = "";
    public string PRMCRCode { get; set; } = "";
    public string? PRMCRName { get; set; }
    public string? EffDateStart { get; set; }
    public string? EffDateEnd { get; set; }
    public string? FlagAllModel { get; set; }
    public double? PointValAllModel { get; set; }
    public DateTime? CreateDTimeUTC { get; set; }
    public string? CreateBy { get; set; }
    public DateTime? LUDTimeUTC { get; set; }
    public string? LUBy { get; set; }
    public DateTime? ApprDTimeUTC { get; set; }
    public string? ApprBy { get; set; }
    public DateTime? FinishDTimeUTC { get; set; }
    public string? FinishBy { get; set; }
    public DateTime? CancelDTimeUTC { get; set; }
    public string? CancelBy { get; set; }
    public string? PRMCRStatus { get; set; }
    public string? Remark { get; set; }
    public DateTime? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
    public string? FlagActive { get; set; }

    [NotMapped]
    public long? MyIdxSeq { get; set; }
    [NotMapped]
    public string? FlagShow { get; set; }
    [NotMapped]
    public string? ModelCode { get; set; }
}

public class Prm_CarRecommendConfig : IEntityTypeConfiguration<Prm_CarRecommend>
{
    public void Configure(EntityTypeBuilder<Prm_CarRecommend> b)
    {
        b.ToTable("Prm_CarRecommend");
        b.HasKey(x => x.PRMCRCodeSys);
        b.Property(x => x.PRMCRCodeSys).IsRequired();
    }
}

public class Prm_CarRecommendSeeder : IPortSeeder
{
    public int Order => 54; // DEBAI: chọn số trong khoảng 10–99 chưa sử dụng (35 đã dùng bởi Mst_ExpenseType)

    public async Task SeedAsync(AppDbContext db)
    {
        if (!await db.Set<Prm_CarRecommend>().AnyAsync())
        {
            db.Set<Prm_CarRecommend>().AddRange(
                new Prm_CarRecommend
                {
                    PRMCRCodeSys = "PCR01",
                    NetworkID = "DEMO",
                    OrgID = "ORG01",
                    DLCPCode = "DL01",
                    PRMCRCode = "PRM01",
                    PRMCRName = "Chương trình giới thiệu xe 01",
                    EffDateStart = "2026-01-01",
                    EffDateEnd = "2026-12-31",
                    FlagAllModel = "1",
                    PointValAllModel = 100,
                    CreateDTimeUTC = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
                    CreateBy = "seed",
                    LUDTimeUTC = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
                    LUBy = "seed",
                    PRMCRStatus = "FINISH",
                    LogLUBy = "seed",
                    LogLUDTimeUTC = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Prm_CarRecommend
                {
                    PRMCRCodeSys = "PCR02",
                    NetworkID = "DEMO",
                    OrgID = "ORG01",
                    DLCPCode = "DL01",
                    PRMCRCode = "PRM02",
                    PRMCRName = "Chương trình giới thiệu xe 02",
                    EffDateStart = "2025-01-01",
                    EffDateEnd = "2025-12-31",
                    FlagAllModel = "1",
                    PointValAllModel = 50,
                    CreateDTimeUTC = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
                    CreateBy = "seed",
                    LUDTimeUTC = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
                    LUBy = "seed",
                    PRMCRStatus = "FINISH",
                    LogLUBy = "seed",
                    LogLUDTimeUTC = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Prm_CarRecommend
                {
                    PRMCRCodeSys = "PCR03",
                    NetworkID = "DEMO",
                    OrgID = "ORG01",
                    DLCPCode = "DL01",
                    PRMCRCode = "PRM03",
                    PRMCRName = "Chương trình giới thiệu xe 03",
                    EffDateStart = "2026-10-01",
                    EffDateEnd = "2100-01-01",
                    FlagAllModel = "1",
                    PointValAllModel = 200,
                    CreateDTimeUTC = new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc),
                    CreateBy = "seed",
                    LUDTimeUTC = new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc),
                    LUBy = "seed",
                    PRMCRStatus = "NEW",
                    LogLUBy = "seed",
                    LogLUDTimeUTC = new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc)
                }
            );
            await db.SaveChangesAsync();
        }
    }
}

// Bảng Prm_CarRecommendDtl — DDL nguồn dùng tên cột PRMCNCodeSys (không phải PRMCRCodeSys) cho khoá liên kết (20221006.QUYNHNTN\...Promotion.sql:31-42).
public class Prm_CarRecommendDtl
{
    public string PRMCNCodeSys { get; set; } = "";
    public long Idx { get; set; }
    public string NetworkID { get; set; } = "";
    public string OrgID { get; set; } = "";
    public double? PointVal { get; set; }
    public string? PRMCNStatusDtl { get; set; }
    public string? Remark { get; set; }
    public DateTime? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
}

public class Prm_CarRecommendDtlConfig : IEntityTypeConfiguration<Prm_CarRecommendDtl>
{
    public void Configure(EntityTypeBuilder<Prm_CarRecommendDtl> b)
    {
        b.ToTable("Prm_CarRecommendDtl");
        b.HasKey(x => new { x.PRMCNCodeSys, x.Idx });
        b.Property(x => x.PRMCNCodeSys).IsRequired();
        b.Property(x => x.NetworkID).IsRequired();
        b.Property(x => x.OrgID).IsRequired();
    }
}

// Bảng Prm_CarRecommendSpec (cùng quy ước cột PRMCNCodeSys).
public class Prm_CarRecommendSpec
{
    public string PRMCNCodeSys { get; set; } = "";
    public long Idx { get; set; }
    public string ModelCode { get; set; } = "";
    public string NetworkID { get; set; } = "";
    public string OrgID { get; set; } = "";
    public string? PRMCNStatusSpec { get; set; }
    public DateTime? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
}

public class Prm_CarRecommendSpecConfig : IEntityTypeConfiguration<Prm_CarRecommendSpec>
{
    public void Configure(EntityTypeBuilder<Prm_CarRecommendSpec> b)
    {
        b.ToTable("Prm_CarRecommendSpec");
        b.HasKey(x => new { x.PRMCNCodeSys, x.Idx });
        b.Property(x => x.PRMCNCodeSys).IsRequired();
        b.Property(x => x.ModelCode).IsRequired();
        b.Property(x => x.NetworkID).IsRequired();
        b.Property(x => x.OrgID).IsRequired();
    }
}

/// <summary>
/// Bảng sinh sequence cho PRMCRCodeSys — port 1:1 Seq_PRMCRCodeSys (Seq.cs:143, SeqType.PRMCRCodeSys; PK AutoId tự tăng).
/// </summary>
public class Seq_PRMCRCodeSys
{
    public long AutoId { get; set; }
    public string? Remark { get; set; }
}

public class Seq_PRMCRCodeSysConfig : IEntityTypeConfiguration<Seq_PRMCRCodeSys>
{
    public void Configure(EntityTypeBuilder<Seq_PRMCRCodeSys> b)
    {
        b.ToTable("Seq_PRMCRCodeSys");
        b.HasKey(x => x.AutoId);
        b.Property(x => x.AutoId).ValueGeneratedOnAdd();
    }
}

/// <summary>
/// Seeder bổ sung cho Prm_CarRecommend Save/Approve/Finish/Delete/Cancel (Order => 69).
/// </summary>
public class Prm_CarRecommendStatusSeeder : IPortSeeder
{
    public int Order => 69;

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<Prm_CarRecommend>().AnyAsync(x => x.PRMCRCodeSys == "PCR_P1")) return;
        var now = new DateTime(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);
        db.Set<Prm_CarRecommend>().AddRange(
            new Prm_CarRecommend { PRMCRCodeSys = "PCR_P1", NetworkID = "DEMO", OrgID = "ORG01", DLCPCode = "HTV", PRMCRCode = "PCR_P1", PRMCRName = "Cho duyet 1", EffDateStart = "2030-01-01", EffDateEnd = "2100-01-01", PRMCRStatus = "PENDING", CreateDTimeUTC = now, CreateBy = "seed", LUDTimeUTC = now, LUBy = "seed", LogLUDTimeUTC = now, LogLUBy = "seed" },
            new Prm_CarRecommend { PRMCRCodeSys = "PCR_P2", NetworkID = "DEMO", OrgID = "ORG01", DLCPCode = "HTV", PRMCRCode = "PCR_P2", PRMCRName = "Cho duyet 2", EffDateStart = "2030-02-01", EffDateEnd = "2100-01-01", PRMCRStatus = "PENDING", CreateDTimeUTC = now, CreateBy = "seed", LUDTimeUTC = now, LUBy = "seed", LogLUDTimeUTC = now, LogLUBy = "seed" },
            new Prm_CarRecommend { PRMCRCodeSys = "PCR_A1", NetworkID = "DEMO", OrgID = "ORG01", DLCPCode = "HTV", PRMCRCode = "PCR_A1", PRMCRName = "Da duyet 1", EffDateStart = "2030-03-01", EffDateEnd = "2100-01-01", PRMCRStatus = "APPROVE", CreateDTimeUTC = now, CreateBy = "seed", LUDTimeUTC = now, LUBy = "seed", LogLUDTimeUTC = now, LogLUBy = "seed" });
        await db.SaveChangesAsync();
    }
}
