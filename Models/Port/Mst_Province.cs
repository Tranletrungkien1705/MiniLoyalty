using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;

namespace MiniLoyalty.Models.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — bảng danh mục tỉnh/thành Mst_Province (#213).
//
// Nguồn tra cứu (chỉ đọc):
//   - Model cột:  12.Dev.Common/idn.Loyalty.Common/Models/Mst_Province.cs:9-22
//   - DDL:        Không có DDL trong repo — kiểu cột cần xác nhận
//   - SQL select: idn.Loyalty.Biz/Master.Loyalty.cs (Mst_Province_Get — bản LIVE, SQL excerpt :442-488)
//
// ĐẶC ĐIỂM BẢNG (đúng 6 cột):
//   ProvinceCode (PK, string not null), NetworkID (string?), ProvinceName (string?),
//   FlagActive (string?), LogLUDTimeUTC (DateTime?), LogLUBy (string?).
//   Comment: "khong co DDL trong repo — kieu cot can xac nhan".
// ===========================================================================================

/// <summary>
/// Danh mục tỉnh/thành — port 1:1 bảng <c>Mst_Province</c>.
/// Model nguồn: Mst_Province.cs:9-22. PK = ProvinceCode.
/// Không có DDL trong repo — kiểu cột cần xác nhận.
/// Đúng 6 cột: ProvinceCode, NetworkID, ProvinceName, FlagActive, LogLUDTimeUTC, LogLUBy.
/// </summary>
public class Mst_Province
{
    public string ProvinceCode { get; set; } = "";        // Mã tỉnh/thành — PK (không có DDL trong repo — kiểu cột cần xác nhận)
    public string? NetworkID { get; set; }                // Mã mạng
    public string? ProvinceName { get; set; }             // Tên tỉnh/thành
    public string? FlagActive { get; set; }               // Cờ hoạt động '1'/'0'
    public DateTime? LogLUDTimeUTC { get; set; }          // Thời điểm log cập nhật UTC
    public string? LogLUBy { get; set; }                  // Người cập nhật
}

/// <summary>Cấu hình EF cho <see cref="Mst_Province"/> — tên bảng Mst_Province + khoá chính ProvinceCode.</summary>
public class Mst_ProvinceConfig : IEntityTypeConfiguration<Mst_Province>
{
    public void Configure(EntityTypeBuilder<Mst_Province> b)
    {
        b.ToTable("Mst_Province");
        b.HasKey(x => x.ProvinceCode);                    // PK = ProvinceCode
        b.Property(x => x.ProvinceCode).IsRequired();
    }
}

/// <summary>
/// Dữ liệu mẫu cho bảng Mst_Province. Idempotent: đã có dữ liệu thì bỏ qua.
/// Dữ liệu demo (comment "du lieu demo"): HN, HCM, DN, HP, CT.
/// Thứ tự Order => 15 (danh mục Mst_* dùng 10–99).
/// </summary>
public class Mst_ProvinceSeeder : IPortSeeder
{
    public int Order => 15;   // danh mục Mst_* dùng 10–99

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<Mst_Province>().AnyAsync()) return;

        db.Set<Mst_Province>().AddRange(
            // Dữ liệu demo (comment "du lieu demo") — không có DDL trong repo, kiểu cột và dữ liệu tĩnh cần xác nhận
            new Mst_Province { ProvinceCode = "HN", ProvinceName = "Hà Nội", FlagActive = "1", NetworkID = "6403834000", LogLUBy = "seed" },
            new Mst_Province { ProvinceCode = "HCM", ProvinceName = "Hồ Chí Minh", FlagActive = "1", NetworkID = "6403834000", LogLUBy = "seed" },
            new Mst_Province { ProvinceCode = "DN", ProvinceName = "Đà Nẵng", FlagActive = "1", NetworkID = "6403834000", LogLUBy = "seed" },
            new Mst_Province { ProvinceCode = "HP", ProvinceName = "Hải Phòng", FlagActive = "1", NetworkID = "6403834000", LogLUBy = "seed" },
            new Mst_Province { ProvinceCode = "CT", ProvinceName = "Cần Thơ", FlagActive = "0", NetworkID = "6403834000", LogLUBy = "seed" }
        );
        await db.SaveChangesAsync();
    }
}
