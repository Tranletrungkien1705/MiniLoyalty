using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;

namespace MiniLoyalty.Models.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — bảng Mst_TempPrintType (#245).
//
// Nguồn tra cứu (chỉ đọc):
//   - Model cột: 12.Dev.Common/idn.Loyalty.Common/Models/Loyalty/Mst_TempPrintType.cs:9-24 (7 cột)
//   - DDL: Không có 'create table Mst_TempPrintType' trong .sql của repo -> dùng MODEL làm từ điển cột.
//   - Khoá chính: TempPrintType (suy từ MasterData.cs:45834 "on t.TempPrintType = ifmo.TempPrintType")
//
// ĐẶC ĐIỂM BẢNG (đúng 7 cột):
//   TempPrintType (PK, string not null), NetworkID (string?), TempPrintName (string?),
//   Remark (string?), FlagActive (string?), LogLUDTimeUTC (DateTime?), LogLUBy (string?).
// ===========================================================================================

/// <summary>
/// Bảng loại mẫu in (Mst_TempPrintType) - port 1:1.
/// Model nguồn: Mst_TempPrintType.cs:9-24. PK = TempPrintType (suy luận từ SQL join).
/// </summary>
public class Mst_TempPrintType
{
    public string TempPrintType { get; set; } = ""; // Khoá chính
    public string? NetworkID { get; set; }
    public string? TempPrintName { get; set; }
    public string? Remark { get; set; }
    public string? FlagActive { get; set; }
    public DateTime? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
}

/// <summary>Cấu hình EF cho <see cref="Mst_TempPrintType"/>.</summary>
public class Mst_TempPrintTypeConfig : IEntityTypeConfiguration<Mst_TempPrintType>
{
    public void Configure(EntityTypeBuilder<Mst_TempPrintType> b)
    {
        b.ToTable("Mst_TempPrintType");
        b.HasKey(x => x.TempPrintType); // PK suy từ MasterData.cs:45834
        b.Property(x => x.TempPrintType).IsRequired();
    }
}

/// <summary>
/// Dữ liệu mẫu cho bảng Mst_TempPrintType. Idempotent.
/// </summary>
public class Mst_TempPrintTypeSeeder : IPortSeeder
{
    public int Order => 47; // danh mục Mst_* dùng 10-99

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<Mst_TempPrintType>().AnyAsync()) return;

        var logTime = new DateTime(2026, 10, 16, 10, 0, 0, DateTimeKind.Utc);
        db.Set<Mst_TempPrintType>().AddRange(
            new Mst_TempPrintType { TempPrintType = "T03", TempPrintName = "Print 3", FlagActive = "1", NetworkID = "1001", Remark = "R3", LogLUBy = "seed", LogLUDTimeUTC = logTime },
            new Mst_TempPrintType { TempPrintType = "T02", TempPrintName = "Print 2", FlagActive = "1", NetworkID = "1001", Remark = "R2", LogLUBy = "seed", LogLUDTimeUTC = logTime },
            new Mst_TempPrintType { TempPrintType = "T01", TempPrintName = "Print 1", FlagActive = "0", NetworkID = "1001", Remark = "R1", LogLUBy = "seed", LogLUDTimeUTC = logTime }
        );
        await db.SaveChangesAsync();
    }
}
