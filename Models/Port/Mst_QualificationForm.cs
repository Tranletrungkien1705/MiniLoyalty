using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;

namespace MiniLoyalty.Models.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — bảng danh mục Mst_QualificationForm (#225).
// Nguồn tra cứu:
//   - Model cột:  12.Dev.Common/idn.Loyalty.Common/Models/HRM/Mst_QualificationForm.cs:9-17
//   - SQL select: MasterData.cs (Mst_QualificationForm_GetX :44031-44186)
// ĐẶC ĐIỂM BẢNG (đúng 6 cột):
//   QualificationFormCode (PK), NetworkID, QualificationFormName, FlagActive, LogLUDTimeUTC, LogLUBy.
// ===========================================================================================

public class Mst_QualificationForm
{
    public string QualificationFormCode { get; set; } = "";     // PK 
    public string? NetworkID { get; set; }
    public string? QualificationFormName { get; set; }          // Tên
    public string? FlagActive { get; set; }                     // Trạng thái '0' hoặc '1'
    public DateTime? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
}

public class Mst_QualificationFormConfig : IEntityTypeConfiguration<Mst_QualificationForm>
{
    public void Configure(EntityTypeBuilder<Mst_QualificationForm> b)
    {
        b.ToTable("Mst_QualificationForm");
        b.HasKey(x => x.QualificationFormCode);
        b.Property(x => x.QualificationFormCode).IsRequired();
    }
}

public class Mst_QualificationFormSeeder : IPortSeeder
{
    public int Order => 28;

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<Mst_QualificationForm>().AnyAsync()) return;

        var logTime = new DateTime(2026, 10, 16, 10, 0, 0, DateTimeKind.Utc);
        db.Set<Mst_QualificationForm>().AddRange(
            new Mst_QualificationForm { QualificationFormCode = "QF1", QualificationFormName = "Dao tao noi bo", FlagActive = "1", NetworkID = "DEMO", LogLUBy = "seed", LogLUDTimeUTC = logTime },
            new Mst_QualificationForm { QualificationFormCode = "QF2", QualificationFormName = "Dao tao ngoai", FlagActive = "1", NetworkID = "DEMO", LogLUBy = "seed", LogLUDTimeUTC = logTime },
            new Mst_QualificationForm { QualificationFormCode = "QF3", QualificationFormName = "Tu hoc", FlagActive = "0", NetworkID = "DEMO", LogLUBy = "seed", LogLUDTimeUTC = logTime }
        );
        await db.SaveChangesAsync();
    }
}
