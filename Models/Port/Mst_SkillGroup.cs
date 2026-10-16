using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;

namespace MiniLoyalty.Models.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — bảng danh mục Mst_SkillGroup (#243).
// Nguồn tra cứu:
//   - Model cột:  12.Dev.Common/idn.Loyalty.Common/Models/HRM/Mst_SkillGroup.cs:9-17
//   - SQL select: MasterData.cs (Mst_SkillGroup_GetX :43584-43739)
// ĐẶC ĐIỂM BẢNG (đúng 6 cột):
//   SkillGroupCode (PK), NetworkID, SkillGroupName, FlagActive, LogLUDTimeUTC, LogLUBy.
// ===========================================================================================

public class Mst_SkillGroup
{
    public string SkillGroupCode { get; set; } = "";     // PK - Mã nhóm kỹ năng (Mst_SkillGroup.cs:11)
    public string? NetworkID { get; set; }               // NetworkID (Mst_SkillGroup.cs:12)
    public string? SkillGroupName { get; set; }          // Tên nhóm kỹ năng (Mst_SkillGroup.cs:13)
    public string? FlagActive { get; set; }              // Trạng thái '0' hoặc '1' (Mst_SkillGroup.cs:14)
    public DateTime? LogLUDTimeUTC { get; set; }         // Thời gian cập nhật cuối UTC (Mst_SkillGroup.cs:15)
    public string? LogLUBy { get; set; }                 // Người cập nhật cuối (Mst_SkillGroup.cs:16)
}

public class Mst_SkillGroupConfig : IEntityTypeConfiguration<Mst_SkillGroup>
{
    public void Configure(EntityTypeBuilder<Mst_SkillGroup> b)
    {
        b.ToTable("Mst_SkillGroup");
        b.HasKey(x => x.SkillGroupCode);
        b.Property(x => x.SkillGroupCode).IsRequired();
    }
}

public class Mst_SkillGroupSeeder : IPortSeeder
{
    public int Order => 46; // Thứ tự chạy seeder cho danh mục Mst_*

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<Mst_SkillGroup>().AnyAsync()) return;

        var logTime = new DateTime(2026, 10, 16, 10, 0, 0, DateTimeKind.Utc);
        db.Set<Mst_SkillGroup>().AddRange(
            new Mst_SkillGroup { SkillGroupCode = "SG01", SkillGroupName = "Bán hàng", FlagActive = "1", NetworkID = "DEMO", LogLUBy = "seed", LogLUDTimeUTC = logTime },
            new Mst_SkillGroup { SkillGroupCode = "SG02", SkillGroupName = "Chăm sóc khách hàng", FlagActive = "1", NetworkID = "DEMO", LogLUBy = "seed", LogLUDTimeUTC = logTime },
            new Mst_SkillGroup { SkillGroupCode = "SG03", SkillGroupName = "Kỹ thuật viên", FlagActive = "0", NetworkID = "DEMO", LogLUBy = "seed", LogLUDTimeUTC = logTime },
            new Mst_SkillGroup { SkillGroupCode = "SG04", SkillGroupName = "Vận hành kho", FlagActive = "1", NetworkID = "DEMO", LogLUBy = "seed", LogLUDTimeUTC = logTime }
        );
        await db.SaveChangesAsync();
    }
}
