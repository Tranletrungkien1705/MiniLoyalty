using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;

namespace MiniLoyalty.Models.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — bảng danh mục chuyên ngành Mst_MajorGroup (#170).
//
// Nguồn tra cứu (chỉ đọc):
//   - Model cột:  12.Dev.Common/idn.Loyalty.Common/Models/HRM/Mst_MajorGroup.cs:1-19 (đúng 7 cột)
//   - DDL:        Không có DDL trong repo — PK = MajorGroupCode suy từ draft distinct và inner join (MasterData.cs:38944, :38987)
//                 Ghi chú: "khong co DDL — kieu cot can xac nhan"
//   - SQL select: idn.Loyalty.Biz/MasterData.cs (Mst_MajorGroup_GetX — bản LIVE, SQL excerpt :38940-39027)
//
// ĐẶC ĐIỂM BẢNG (đúng 7 cột):
//   MajorGroupCode (PK, string not null), NetworkID (string?), MajorGroupName (string?),
//   MajorGroupDesc (string?), FlagActive (string?), LogLUDTimeUTC (DateTime?), LogLUBy (string?).
// ===========================================================================================

/// <summary>
/// Danh mục nhóm ngành / chuyên ngành — port 1:1 bảng <c>Mst_MajorGroup</c>.
/// Model nguồn: Mst_MajorGroup.cs:11-17. PK = MajorGroupCode.
/// Không có DDL — kiểu cột cần xác nhận. PK suy từ draft distinct và inner join (MasterData.cs:38944, :38987).
/// Đúng 7 cột: MajorGroupCode, NetworkID, MajorGroupName, MajorGroupDesc, FlagActive, LogLUDTimeUTC, LogLUBy.
/// </summary>
public class Mst_MajorGroup
{
    public string MajorGroupCode { get; set; } = "";     // Mã chuyên ngành — PK (khong co DDL — kieu cot can xac nhan)
    public string? NetworkID { get; set; }               // Mã mạng
    public string? MajorGroupName { get; set; }          // Tên chuyên ngành
    public string? MajorGroupDesc { get; set; }          // Mô tả
    public string? FlagActive { get; set; }              // Trạng thái '1'/'0'
    public DateTime? LogLUDTimeUTC { get; set; }         // Thời điểm cập nhật UTC
    public string? LogLUBy { get; set; }                 // Người cập nhật
}

/// <summary>Cấu hình EF cho <see cref="Mst_MajorGroup"/> — tên bảng Mst_MajorGroup + khoá chính MajorGroupCode.</summary>
public class Mst_MajorGroupConfig : IEntityTypeConfiguration<Mst_MajorGroup>
{
    public void Configure(EntityTypeBuilder<Mst_MajorGroup> b)
    {
        b.ToTable("Mst_MajorGroup");
        b.HasKey(x => x.MajorGroupCode);                 // PK = MajorGroupCode (khong co DDL — kieu cot can xac nhan)
        b.Property(x => x.MajorGroupCode).IsRequired();
    }
}

/// <summary>
/// Dữ liệu mẫu cho bảng Mst_MajorGroup. Idempotent: đã có dữ liệu thì bỏ qua.
/// Dữ liệu demo (comment "du lieu demo"): MG01..MG06.
/// Thứ tự Order => 43 (danh mục Mst_* dùng 10–99).
/// </summary>
public class Mst_MajorGroupSeeder : IPortSeeder
{
    public int Order => 43;   // danh mục Mst_* dùng 10–99

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<Mst_MajorGroup>().AnyAsync()) return;

        var seedTime = new DateTime(2026, 10, 10, 10, 0, 0, DateTimeKind.Utc);
        db.Set<Mst_MajorGroup>().AddRange(
            // Dữ liệu demo (comment "du lieu demo") — khong co DDL — kieu cot can xac nhan
            new Mst_MajorGroup { MajorGroupCode = "MG01", MajorGroupName = "Cong nghe thong tin", MajorGroupDesc = "Mo ta MG01", FlagActive = "1", NetworkID = "DEMO", LogLUBy = "seed", LogLUDTimeUTC = seedTime },
            new Mst_MajorGroup { MajorGroupCode = "MG02", MajorGroupName = "Co khi dong luc", MajorGroupDesc = "Mo ta MG02", FlagActive = "1", NetworkID = "DEMO", LogLUBy = "seed", LogLUDTimeUTC = seedTime },
            new Mst_MajorGroup { MajorGroupCode = "MG03", MajorGroupName = "Dien tu vien thong", MajorGroupDesc = "Mo ta MG03", FlagActive = "1", NetworkID = "DEMO", LogLUBy = "seed", LogLUDTimeUTC = seedTime },
            new Mst_MajorGroup { MajorGroupCode = "MG04", MajorGroupName = "Kinh te doi ngoai", MajorGroupDesc = "Mo ta MG04", FlagActive = "1", NetworkID = "DEMO", LogLUBy = "seed", LogLUDTimeUTC = seedTime },
            new Mst_MajorGroup { MajorGroupCode = "MG05", MajorGroupName = "Quan tri kinh doanh", MajorGroupDesc = "Mo ta MG05", FlagActive = "1", NetworkID = "DEMO", LogLUBy = "seed", LogLUDTimeUTC = seedTime },
            new Mst_MajorGroup { MajorGroupCode = "MG06", MajorGroupName = "Nganh cu ngung dung", MajorGroupDesc = "Mo ta MG06", FlagActive = "0", NetworkID = "DEMO", LogLUBy = "seed", LogLUDTimeUTC = seedTime }
        );
        await db.SaveChangesAsync();
    }
}
