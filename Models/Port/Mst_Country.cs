using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;

namespace MiniLoyalty.Models.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — bảng danh mục quốc gia Mst_Country (#109).
//
// Nguồn tra cứu (chỉ đọc):
//   - Model cột:  12.Dev.Common/idn.Loyalty.Common/Models/Mst_Country.cs (5 cột)
//   - DDL:        Không có DDL trong repo — kiểu cột cần xác nhận
//   - SQL select: idn.Loyalty.Biz/Master.cs (Mst_Country_Get — SQL excerpt :3386-3432)
//
// ĐẶC ĐIỂM BẢNG (đúng 5 cột — KHÔNG có NetworkID):
//   CountryCode, CountryName, FlagActive, LogLUDTimeUTC, LogLUBy.
//   Khoá chính: CountryCode.
//   Comment: "khong co DDL trong repo — kieu cot can xac nhan".
// ===========================================================================================

/// <summary>
/// Danh mục quốc gia — port 1:1 bảng <c>Mst_Country</c>.
/// Model nguồn: Mst_Country.cs (5 cột — KHÔNG có NetworkID). PK = CountryCode.
/// Không có DDL trong repo — kiểu cột cần xác nhận.
/// Đúng 5 cột: CountryCode, CountryName, FlagActive, LogLUDTimeUTC, LogLUBy.
/// </summary>
public class Mst_Country
{
    public string CountryCode { get; set; } = "";         // Mã quốc gia — PK
    public string? CountryName { get; set; }              // Tên quốc gia
    public string? FlagActive { get; set; }               // Cờ hoạt động '1'/'0'
    public DateTime? LogLUDTimeUTC { get; set; }          // Thời điểm log cập nhật UTC
    public string? LogLUBy { get; set; }                  // Người cập nhật
}

/// <summary>Cấu hình EF cho <see cref="Mst_Country"/> — tên bảng Mst_Country + khoá chính CountryCode.</summary>
public class Mst_CountryConfig : IEntityTypeConfiguration<Mst_Country>
{
    public void Configure(EntityTypeBuilder<Mst_Country> b)
    {
        b.ToTable("Mst_Country");
        b.HasKey(x => x.CountryCode);
        b.Property(x => x.CountryCode).IsRequired();
    }
}

/// <summary>
/// Dữ liệu mẫu cho bảng Mst_Country. Idempotent: đã có dữ liệu thì bỏ qua.
/// Dữ liệu demo (comment "du lieu demo"):
///   VN 'Việt Nam' '1' · KR 'Hàn Quốc' '1' · JP 'Nhật Bản' '1' · US 'Hoa Kỳ' '0'; LogLUBy 'seed'.
/// Thứ tự Order => 18 (danh mục Mst_* dùng 10–99).
/// </summary>
public class Mst_CountrySeeder : IPortSeeder
{
    public int Order => 18;   // danh mục Mst_* dùng 10–99

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<Mst_Country>().AnyAsync()) return;

        db.Set<Mst_Country>().AddRange(
            // Dữ liệu demo (comment "du lieu demo") — không có DDL trong repo, kiểu cột và dữ liệu tĩnh cần xác nhận
            new Mst_Country { CountryCode = "VN", CountryName = "Việt Nam", FlagActive = "1", LogLUBy = "seed" },
            new Mst_Country { CountryCode = "KR", CountryName = "Hàn Quốc", FlagActive = "1", LogLUBy = "seed" },
            new Mst_Country { CountryCode = "JP", CountryName = "Nhật Bản", FlagActive = "1", LogLUBy = "seed" },
            new Mst_Country { CountryCode = "US", CountryName = "Hoa Kỳ", FlagActive = "0", LogLUBy = "seed" }
        );
        await db.SaveChangesAsync();
    }
}
