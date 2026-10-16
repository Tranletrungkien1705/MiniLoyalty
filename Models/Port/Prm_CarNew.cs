using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;

namespace MiniLoyalty.Models.Port;

public class Prm_CarNew
{
    public string PRMCNCodeSys { get; set; } = "";
    public string NetworkID { get; set; } = "";
    public string OrgID { get; set; } = "";
    public string DLCPCode { get; set; } = "";
    public string PRMCNCode { get; set; } = "";
    public string? PRMCNName { get; set; }
    public string EffDateStart { get; set; } = "";
    public string EffDateEnd { get; set; } = "";
    public string? FlagAllModel { get; set; }
    public double? PointValAllModel { get; set; }
    public DateTime CreateDTimeUTC { get; set; }
    public string CreateBy { get; set; } = "";
    public DateTime LUDTimeUTC { get; set; }
    public string LUBy { get; set; } = "";
    public DateTime? ApprDTimeUTC { get; set; }
    public string? ApprBy { get; set; }
    public DateTime? FinishDTimeUTC { get; set; }
    public string? FinishBy { get; set; }
    public DateTime? CancelDTimeUTC { get; set; }
    public string? CancelBy { get; set; }
    public string? PRMCNStatus { get; set; }
    public string? Remark { get; set; }
    public DateTime? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }

    [NotMapped]
    public long? MyIdxSeq { get; set; }
    [NotMapped]
    public string? FlagShow { get; set; }
}

public class Prm_CarNewConfig : IEntityTypeConfiguration<Prm_CarNew>
{
    public void Configure(EntityTypeBuilder<Prm_CarNew> b)
    {
        b.ToTable("Prm_CarNew");
        b.HasKey(x => x.PRMCNCodeSys);
        b.Property(x => x.PRMCNCodeSys).IsRequired();
        b.Property(x => x.NetworkID).IsRequired();
        b.Property(x => x.OrgID).IsRequired();
        b.Property(x => x.DLCPCode).IsRequired();
        b.Property(x => x.PRMCNCode).IsRequired();
        b.Property(x => x.EffDateStart).IsRequired();
        b.Property(x => x.EffDateEnd).IsRequired();
        b.Property(x => x.CreateBy).IsRequired();
        b.Property(x => x.LUBy).IsRequired();
    }
}

public class Prm_CarNewSpec
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

public class Prm_CarNewSpecConfig : IEntityTypeConfiguration<Prm_CarNewSpec>
{
    public void Configure(EntityTypeBuilder<Prm_CarNewSpec> b)
    {
        b.ToTable("Prm_CarNewSpec");
        b.HasKey(x => new { x.PRMCNCodeSys, x.Idx, x.ModelCode });
        b.Property(x => x.PRMCNCodeSys).IsRequired();
        b.Property(x => x.ModelCode).IsRequired();
        b.Property(x => x.NetworkID).IsRequired();
        b.Property(x => x.OrgID).IsRequired();
    }
}

public class Prm_CarNewDtl
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

public class Prm_CarNewDtlConfig : IEntityTypeConfiguration<Prm_CarNewDtl>
{
    public void Configure(EntityTypeBuilder<Prm_CarNewDtl> b)
    {
        b.ToTable("Prm_CarNewDtl");
        b.HasKey(x => new { x.PRMCNCodeSys, x.Idx });
        b.Property(x => x.PRMCNCodeSys).IsRequired();
        b.Property(x => x.NetworkID).IsRequired();
        b.Property(x => x.OrgID).IsRequired();
    }
}

public class Prm_CarNewSeeder : IPortSeeder
{
    public int Order => 51;

    public async Task SeedAsync(AppDbContext db)
    {
        if (!await db.Set<Prm_CarNew>().AnyAsync())
        {
            db.Set<Prm_CarNew>().AddRange(
                new Prm_CarNew
                {
                    PRMCNCodeSys = "PCN01",
                    NetworkID = "DEMO",
                    OrgID = "ORG01",
                    DLCPCode = "DL01",
                    PRMCNCode = "PRM01",
                    PRMCNName = "Chương trình khuyến mãi xe mới 01",
                    EffDateStart = "2026-01-01",
                    EffDateEnd = "2026-12-31",
                    FlagAllModel = "1",
                    PointValAllModel = 100,
                    CreateDTimeUTC = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
                    CreateBy = "seed",
                    LUDTimeUTC = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
                    LUBy = "seed",
                    PRMCNStatus = "FINISH",
                    LogLUBy = "seed",
                    LogLUDTimeUTC = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Prm_CarNew
                {
                    PRMCNCodeSys = "PCN02",
                    NetworkID = "DEMO",
                    OrgID = "ORG01",
                    DLCPCode = "DL01",
                    PRMCNCode = "PRM02",
                    PRMCNName = "Chương trình khuyến mãi xe mới 02",
                    EffDateStart = "2025-01-01",
                    EffDateEnd = "2025-12-31",
                    FlagAllModel = "1",
                    PointValAllModel = 50,
                    CreateDTimeUTC = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
                    CreateBy = "seed",
                    LUDTimeUTC = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
                    LUBy = "seed",
                    PRMCNStatus = "FINISH",
                    LogLUBy = "seed",
                    LogLUDTimeUTC = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Prm_CarNew
                {
                    PRMCNCodeSys = "PCN03",
                    NetworkID = "DEMO",
                    OrgID = "ORG01",
                    DLCPCode = "DL01",
                    PRMCNCode = "PRM03",
                    PRMCNName = "Chương trình khuyến mãi xe mới 03",
                    EffDateStart = "2026-10-01",
                    EffDateEnd = "2026-10-31",
                    FlagAllModel = "1",
                    PointValAllModel = 200,
                    CreateDTimeUTC = new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc),
                    CreateBy = "seed",
                    LUDTimeUTC = new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc),
                    LUBy = "seed",
                    PRMCNStatus = "NEW",
                    LogLUBy = "seed",
                    LogLUDTimeUTC = new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc)
                }
            );
            await db.SaveChangesAsync();
        }
    }
}
