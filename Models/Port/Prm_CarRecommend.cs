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
