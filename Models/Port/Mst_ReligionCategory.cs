using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;

namespace MiniLoyalty.Models.Port;

/// <summary>
/// Danh mục tôn giáo — port 1:1 bảng <c>Mst_ReligionCategory</c>.
/// Model nguồn: Mst_ReligionCategory.cs:10-18. Bảng có 7 cột.
/// PK = ReligionCode (suy từ draft/join: MasterData.cs:42025, 42068).
/// </summary>
public class Mst_ReligionCategory
{
    public string ReligionCode { get; set; } = "";           // Mã tôn giáo — PK
    public string? NetworkID { get; set; }
    public string? ReligionName { get; set; }                // Tôn giáo
    public string? ReligionDesc { get; set; }                // Mô tả
    public string? FlagActive { get; set; }                  // Trạng thái '1'/'0'
    public DateTime? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
}

public class Mst_ReligionCategoryConfig : IEntityTypeConfiguration<Mst_ReligionCategory>
{
    public void Configure(EntityTypeBuilder<Mst_ReligionCategory> b)
    {
        b.ToTable("Mst_ReligionCategory");
        b.HasKey(x => x.ReligionCode);
        b.Property(x => x.ReligionCode).IsRequired();
    }
}

public class Mst_ReligionCategorySeeder : IPortSeeder
{
    public int Order => 61; // theo đề bài (#231)

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<Mst_ReligionCategory>().AnyAsync()) return;

        var logTime = new DateTime(2026, 10, 16, 10, 0, 0, DateTimeKind.Utc);
        db.Set<Mst_ReligionCategory>().AddRange(
            new Mst_ReligionCategory { ReligionCode = "BUD", ReligionName = "Phật giáo", FlagActive = "1", NetworkID = "DEMO", LogLUBy = "seed", LogLUDTimeUTC = logTime },
            new Mst_ReligionCategory { ReligionCode = "CAT", ReligionName = "Công giáo", FlagActive = "1", NetworkID = "DEMO", LogLUBy = "seed", LogLUDTimeUTC = logTime },
            new Mst_ReligionCategory { ReligionCode = "CHR", ReligionName = "Tin lành", FlagActive = "0", NetworkID = "DEMO", LogLUBy = "seed", LogLUDTimeUTC = logTime },
            new Mst_ReligionCategory { ReligionCode = "ISL", ReligionName = "Hồi giáo", FlagActive = "1", NetworkID = "DEMO", LogLUBy = "seed", LogLUDTimeUTC = logTime }
        );
        await db.SaveChangesAsync();
    }
}
