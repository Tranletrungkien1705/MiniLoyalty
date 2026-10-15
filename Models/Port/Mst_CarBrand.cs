using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;

namespace MiniLoyalty.Models.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — bảng danh mục hãng xe Mst_CarBrand (#77).
//
// Nguồn tra cứu (chỉ đọc):
//   - Model cột:  12.Dev.Common/idn.Loyalty.Common/Models/Loyalty/Mst_CarBrand.cs:9-22
//   - DDL:        Không có DDL trong repo — kiểu cột cần xác nhận
//   - SQL select: idn.Loyalty.Biz/Master.Loyalty.cs (Mst_CarBrand_GetX — bản LIVE, SQL excerpt :400-447)
//
// ĐẶC ĐIỂM BẢNG (đúng 7 cột):
//   BrandCode (PK, string not null), NetworkID (string?), BrandName (string?),
//   FlagActive (string?), Remark (string?), LogLUDTimeUTC (DateTime?), LogLUBy (string?).
//   Comment: "khong co DDL trong repo — kieu cot can xac nhan".
// ===========================================================================================

/// <summary>
/// Danh mục hãng xe — port 1:1 bảng <c>Mst_CarBrand</c>.
/// Model nguồn: Mst_CarBrand.cs:9-22. PK = BrandCode.
/// Không có DDL trong repo — kiểu cột cần xác nhận.
/// Đúng 7 cột: BrandCode, NetworkID, BrandName, FlagActive, Remark, LogLUDTimeUTC, LogLUBy.
/// </summary>
public class Mst_CarBrand
{
    public string BrandCode { get; set; } = "";           // Mã hãng xe — PK (không có DDL trong repo — kiểu cột cần xác nhận)
    public string? NetworkID { get; set; }                // Mã mạng
    public string? BrandName { get; set; }                // Tên hãng xe
    public string? FlagActive { get; set; }               // Cờ hoạt động '1'/'0'
    public string? Remark { get; set; }                   // Ghi chú
    public DateTime? LogLUDTimeUTC { get; set; }          // Thời điểm log cập nhật UTC
    public string? LogLUBy { get; set; }                  // Người cập nhật
}

/// <summary>Cấu hình EF cho <see cref="Mst_CarBrand"/> — tên bảng Mst_CarBrand + khoá chính BrandCode.</summary>
public class Mst_CarBrandConfig : IEntityTypeConfiguration<Mst_CarBrand>
{
    public void Configure(EntityTypeBuilder<Mst_CarBrand> b)
    {
        b.ToTable("Mst_CarBrand");
        b.HasKey(x => x.BrandCode);                       // PK = BrandCode
        b.Property(x => x.BrandCode).IsRequired();
    }
}

/// <summary>
/// Dữ liệu mẫu cho bảng Mst_CarBrand. Idempotent: đã có dữ liệu thì bỏ qua.
/// Dữ liệu demo (comment "du lieu demo"): HYU, KIA, TOY, VIN (VIN FlagActive '0').
/// Thứ tự Order => 19 (danh mục Mst_* dùng 10–99).
/// </summary>
public class Mst_CarBrandSeeder : IPortSeeder
{
    public int Order => 19;   // danh mục Mst_* dùng 10–99

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<Mst_CarBrand>().AnyAsync()) return;

        db.Set<Mst_CarBrand>().AddRange(
            // Dữ liệu demo (comment "du lieu demo") — không có DDL trong repo, kiểu cột và dữ liệu tĩnh cần xác nhận
            new Mst_CarBrand { BrandCode = "HYU", BrandName = "Hyundai", FlagActive = "1", NetworkID = "6403834000", LogLUBy = "seed" },
            new Mst_CarBrand { BrandCode = "KIA", BrandName = "Kia", FlagActive = "1", NetworkID = "6403834000", LogLUBy = "seed" },
            new Mst_CarBrand { BrandCode = "TOY", BrandName = "Toyota", FlagActive = "1", NetworkID = "6403834000", LogLUBy = "seed" },
            new Mst_CarBrand { BrandCode = "VIN", BrandName = "VinFast", FlagActive = "0", NetworkID = "6403834000", LogLUBy = "seed" }
        );
        await db.SaveChangesAsync();
    }
}
