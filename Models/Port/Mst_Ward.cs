using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;

namespace MiniLoyalty.Models.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — bảng danh mục phường/xã Mst_Ward (#246).
//
// Nguồn tra cứu (chỉ đọc):
//   - Model cột:  12.Dev.Common/idn.Loyalty.Common/Models/CustomerCentrer/Mst_Ward.cs (8 cột)
//   - DDL:        Không có DDL trong repo — kiểu cột cần xác nhận
//   - SQL select: idn.Loyalty.Biz/Master.1.cs (Mst_Ward_GetX — bản LIVE, SQL excerpt :6054-6100)
//
// ĐẶC ĐIỂM BẢNG (đúng 8 cột):
//   WardCode, NetworkID, ProvinceCode, DistrictCode, WardName, FlagActive, LogLUDTimeUTC, LogLUBy.
//   Khoá chính ghép: (ProvinceCode, DistrictCode, WardCode) — suy luận theo Mst_District, cần xác nhận khi port GetWardDetail.
//   Comment: "khong co DDL trong repo — kieu cot can xac nhan".
// ===========================================================================================

/// <summary>
/// Danh mục phường/xã — port 1:1 bảng <c>Mst_Ward</c>.
/// Model nguồn: Mst_Ward.cs (8 cột). PK ghép = (ProvinceCode, DistrictCode, WardCode) — suy luận theo Mst_District, cần xác nhận khi port GetWardDetail.
/// Không có DDL trong repo — kiểu cột cần xác nhận.
/// Đúng 8 cột: WardCode, NetworkID, ProvinceCode, DistrictCode, WardName, FlagActive, LogLUDTimeUTC, LogLUBy.
/// </summary>
public class Mst_Ward
{
    public string WardCode { get; set; } = "";            // Mã phường/xã — PK ghép (suy luận theo Mst_District, cần xác nhận khi port GetWardDetail)
    public string? NetworkID { get; set; }                // Mã mạng
    public string ProvinceCode { get; set; } = "";        // Mã tỉnh/thành — PK ghép (suy luận theo Mst_District, cần xác nhận khi port GetWardDetail)
    public string DistrictCode { get; set; } = "";        // Mã quận/huyện — PK ghép (suy luận theo Mst_District, cần xác nhận khi port GetWardDetail)
    public string? WardName { get; set; }                 // Tên phường/xã
    public string? FlagActive { get; set; }               // Cờ hoạt động '1'/'0'
    public DateTime? LogLUDTimeUTC { get; set; }          // Thời điểm log cập nhật UTC
    public string? LogLUBy { get; set; }                  // Người cập nhật
}

/// <summary>Cấu hình EF cho <see cref="Mst_Ward"/> — tên bảng Mst_Ward + khoá chính ghép (ProvinceCode, DistrictCode, WardCode).</summary>
public class Mst_WardConfig : IEntityTypeConfiguration<Mst_Ward>
{
    public void Configure(EntityTypeBuilder<Mst_Ward> b)
    {
        b.ToTable("Mst_Ward");
        // Khoá chính ghép (ProvinceCode, DistrictCode, WardCode) — suy luận theo Mst_District, cần xác nhận khi port GetWardDetail
        b.HasKey(x => new { x.ProvinceCode, x.DistrictCode, x.WardCode });
        b.Property(x => x.ProvinceCode).IsRequired();
        b.Property(x => x.DistrictCode).IsRequired();
        b.Property(x => x.WardCode).IsRequired();
    }
}

/// <summary>
/// Dữ liệu mẫu cho bảng Mst_Ward. Idempotent: đã có dữ liệu thì bỏ qua.
/// Dữ liệu demo (comment "du lieu demo") — khớp seed tỉnh/huyện:
///   HN/HN01/HN0101 'Phúc Xá' '1' · HN/HN01/HN0102 'Trúc Bạch' '1' ·
///   HN/HN02/HN0201 'Hàng Bạc' '1' · HCM/HCM01/HCM0101 'Bến Nghé' '1' · CT/CT01/CT0101 'Tân An' '0';
///   NetworkID '6403834000', LogLUBy 'seed'.
/// Thứ tự Order => 17 (danh mục Mst_* dùng 10–99).
/// </summary>
public class Mst_WardSeeder : IPortSeeder
{
    public int Order => 17;   // danh mục Mst_* dùng 10–99

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<Mst_Ward>().AnyAsync()) return;

        db.Set<Mst_Ward>().AddRange(
            // Dữ liệu demo (comment "du lieu demo") — không có DDL trong repo, kiểu cột và dữ liệu tĩnh cần xác nhận
            new Mst_Ward { ProvinceCode = "HN", DistrictCode = "HN01", WardCode = "HN0101", WardName = "Phúc Xá", FlagActive = "1", NetworkID = "6403834000", LogLUBy = "seed" },
            new Mst_Ward { ProvinceCode = "HN", DistrictCode = "HN01", WardCode = "HN0102", WardName = "Trúc Bạch", FlagActive = "1", NetworkID = "6403834000", LogLUBy = "seed" },
            new Mst_Ward { ProvinceCode = "HN", DistrictCode = "HN02", WardCode = "HN0201", WardName = "Hàng Bạc", FlagActive = "1", NetworkID = "6403834000", LogLUBy = "seed" },
            new Mst_Ward { ProvinceCode = "HCM", DistrictCode = "HCM01", WardCode = "HCM0101", WardName = "Bến Nghé", FlagActive = "1", NetworkID = "6403834000", LogLUBy = "seed" },
            new Mst_Ward { ProvinceCode = "CT", DistrictCode = "CT01", WardCode = "CT0101", WardName = "Tân An", FlagActive = "0", NetworkID = "6403834000", LogLUBy = "seed" }
        );
        await db.SaveChangesAsync();
    }
}
