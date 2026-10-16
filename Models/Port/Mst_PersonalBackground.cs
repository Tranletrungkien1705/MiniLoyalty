using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;

namespace MiniLoyalty.Models.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — bảng danh mục thành phần bản thân Mst_PersonalBackground (#188).
//
// Nguồn tra cứu (chỉ đọc):
//   - Model cột:  12.Dev.Common/idn.Loyalty.Common/Models/HRM/Mst_PersonalBackground.cs:9-18 (đúng 7 cột)
//   - DDL:        Không có DDL trong repo — PK = PersonalBGCode suy từ draft distinct và inner join (MasterData.cs:44982, :45025)
//                 Ghi chú: "khong co DDL — kieu cot can xac nhan"
//   - SQL select: idn.Loyalty.Biz/MasterData.cs (Mst_PersonalBackground_GetX — bản LIVE, SQL excerpt :44978-45030)
//
// ĐẶC ĐIỂM BẢNG (đúng 7 cột):
//   PersonalBGCode (PK, string not null), NetworkID (string?), PersonalBGName (string?),
//   PersonalBGDesc (string?), FlagActive (string?), LogLUDTimeUTC (DateTime?), LogLUBy (string?).
// ===========================================================================================

/// <summary>
/// Danh mục thành phần bản thân — port 1:1 bảng <c>Mst_PersonalBackground</c>.
/// Model nguồn: Mst_PersonalBackground.cs:9-18. PK = PersonalBGCode.
/// Không có DDL — kiểu cột cần xác nhận. PK suy từ draft distinct và inner join (MasterData.cs:44982, :45025).
/// Đúng 7 cột: PersonalBGCode, NetworkID, PersonalBGName, PersonalBGDesc, FlagActive, LogLUDTimeUTC, LogLUBy.
/// </summary>
public class Mst_PersonalBackground
{
    public string PersonalBGCode { get; set; } = "";     // Mã thành phần bản thân — PK (khong co DDL — kieu cot can xac nhan)
    public string? NetworkID { get; set; }               // Mã mạng
    public string? PersonalBGName { get; set; }          // Tên thành phần bản thân
    public string? PersonalBGDesc { get; set; }          // Mô tả thành phần bản thân
    public string? FlagActive { get; set; }              // Trạng thái hoạt động '1'/'0'
    public DateTime? LogLUDTimeUTC { get; set; }         // Thời điểm cập nhật UTC
    public string? LogLUBy { get; set; }                 // Người cập nhật
}

/// <summary>Cấu hình EF cho <see cref="Mst_PersonalBackground"/> — tên bảng Mst_PersonalBackground + khoá chính PersonalBGCode.</summary>
public class Mst_PersonalBackgroundConfig : IEntityTypeConfiguration<Mst_PersonalBackground>
{
    public void Configure(EntityTypeBuilder<Mst_PersonalBackground> b)
    {
        b.ToTable("Mst_PersonalBackground");
        b.HasKey(x => x.PersonalBGCode);                 // PK = PersonalBGCode (khong co DDL — kieu cot can xac nhan)
        b.Property(x => x.PersonalBGCode).IsRequired();
    }
}

/// <summary>
/// Dữ liệu mẫu cho bảng Mst_PersonalBackground. Idempotent: đã có dữ liệu thì bỏ qua.
/// Dữ liệu demo (comment "du lieu demo"): PBG01..PBG05.
/// Thứ tự Order => 44 (danh mục Mst_* dùng 10–99).
/// </summary>
public class Mst_PersonalBackgroundSeeder : IPortSeeder
{
    public int Order => 44;   // danh mục Mst_* dùng 10–99

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<Mst_PersonalBackground>().AnyAsync()) return;

        var seedTime = new DateTime(2026, 10, 16, 12, 0, 0, DateTimeKind.Utc);
        db.Set<Mst_PersonalBackground>().AddRange(
            // Dữ liệu demo (comment "du lieu demo") — khong co DDL — kieu cot can xac nhan
            new Mst_PersonalBackground { PersonalBGCode = "PBG01", PersonalBGName = "Nong dan", PersonalBGDesc = "Thanh phan nong dan", FlagActive = "1", NetworkID = "DEMO", LogLUBy = "seed", LogLUDTimeUTC = seedTime },
            new Mst_PersonalBackground { PersonalBGCode = "PBG02", PersonalBGName = "Cong nhan", PersonalBGDesc = "Thanh phan cong nhan", FlagActive = "1", NetworkID = "DEMO", LogLUBy = "seed", LogLUDTimeUTC = seedTime },
            new Mst_PersonalBackground { PersonalBGCode = "PBG03", PersonalBGName = "Vien chuc", PersonalBGDesc = "Thanh phan vien chuc", FlagActive = "1", NetworkID = "DEMO", LogLUBy = "seed", LogLUDTimeUTC = seedTime },
            new Mst_PersonalBackground { PersonalBGCode = "PBG04", PersonalBGName = "Kinh doanh", PersonalBGDesc = "Thanh phan kinh doanh", FlagActive = "1", NetworkID = "DEMO", LogLUBy = "seed", LogLUDTimeUTC = seedTime },
            new Mst_PersonalBackground { PersonalBGCode = "PBG05", PersonalBGName = "Khac", PersonalBGDesc = "Thanh phan khac", FlagActive = "0", NetworkID = "DEMO", LogLUBy = "seed", LogLUDTimeUTC = seedTime }
        );
        await db.SaveChangesAsync();
    }
}
