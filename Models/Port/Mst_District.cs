using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;

namespace MiniLoyalty.Models.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — bảng danh mục quận/huyện Mst_District (#139).
//
// Nguồn tra cứu (chỉ đọc):
//   - Model cột:  12.Dev.Common/idn.Loyalty.Common/Models/Mst_District.cs:9-24 (7 cột)
//   - DDL:        Không có DDL trong repo — kiểu cột cần xác nhận
//   - SQL select: idn.Loyalty.Biz/Master.cs (Mst_District_Get — bản LIVE, SQL excerpt :1883-1929)
//
// ĐẶC ĐIỂM BẢNG (đúng 7 cột):
//   DistrictCode (PK, string not null), ProvinceCode (string?), NetworkID (string?),
//   DistrictName (string?), FlagActive (string?), LogLUDTimeUTC (DateTime?), LogLUBy (string?).
//   Comment: "khong co DDL trong repo — kieu cot can xac nhan".
// ===========================================================================================

/// <summary>
/// Danh mục quận/huyện — port 1:1 bảng <c>Mst_District</c>.
/// Model nguồn: Mst_District.cs:9-24. PK = DistrictCode.
/// Không có DDL trong repo — kiểu cột cần xác nhận.
/// Đúng 7 cột: DistrictCode, ProvinceCode, NetworkID, DistrictName, FlagActive, LogLUDTimeUTC, LogLUBy.
/// </summary>
public class Mst_District
{
    public string DistrictCode { get; set; } = "";        // Mã quận/huyện — PK (không có DDL trong repo — kiểu cột cần xác nhận)
    public string? ProvinceCode { get; set; }             // Mã tỉnh/thành
    public string? NetworkID { get; set; }                // Mã mạng
    public string? DistrictName { get; set; }             // Tên quận/huyện
    public string? FlagActive { get; set; }               // Cờ hoạt động '1'/'0'
    public DateTime? LogLUDTimeUTC { get; set; }          // Thời điểm log cập nhật UTC
    public string? LogLUBy { get; set; }                  // Người cập nhật
}

/// <summary>Cấu hình EF cho <see cref="Mst_District"/> — tên bảng Mst_District + khoá chính DistrictCode.</summary>
public class Mst_DistrictConfig : IEntityTypeConfiguration<Mst_District>
{
    public void Configure(EntityTypeBuilder<Mst_District> b)
    {
        b.ToTable("Mst_District");
        b.HasKey(x => x.DistrictCode);                    // PK = DistrictCode
        b.Property(x => x.DistrictCode).IsRequired();
    }
}

/// <summary>
/// Dữ liệu mẫu cho bảng Mst_District. Idempotent: đã có dữ liệu thì bỏ qua.
/// Dữ liệu demo (comment "du lieu demo"): HN01, HN02, HCM01, DN01, CT01.
/// Thứ tự Order => 16 (danh mục Mst_* dùng 10–99).
/// </summary>
public class Mst_DistrictSeeder : IPortSeeder
{
    public int Order => 16;   // danh mục Mst_* dùng 10–99

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<Mst_District>().AnyAsync()) return;

        db.Set<Mst_District>().AddRange(
            // Dữ liệu demo (comment "du lieu demo") — không có DDL trong repo, kiểu cột và dữ liệu tĩnh cần xác nhận
            new Mst_District { DistrictCode = "HN01", DistrictName = "Ba Đình", ProvinceCode = "HN", FlagActive = "1", NetworkID = "6403834000", LogLUBy = "seed" },
            new Mst_District { DistrictCode = "HN02", DistrictName = "Hoàn Kiếm", ProvinceCode = "HN", FlagActive = "1", NetworkID = "6403834000", LogLUBy = "seed" },
            new Mst_District { DistrictCode = "HCM01", DistrictName = "Quận 1", ProvinceCode = "HCM", FlagActive = "1", NetworkID = "6403834000", LogLUBy = "seed" },
            new Mst_District { DistrictCode = "DN01", DistrictName = "Hải Châu", ProvinceCode = "DN", FlagActive = "1", NetworkID = "6403834000", LogLUBy = "seed" },
            new Mst_District { DistrictCode = "CT01", DistrictName = "Ninh Kiều", ProvinceCode = "CT", FlagActive = "0", NetworkID = "6403834000", LogLUBy = "seed" }
        );
        await db.SaveChangesAsync();
    }
}
