using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;

namespace MiniLoyalty.Models.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — bảng danh mục Mst_PurchaseType (loại mua hàng/mua xe).
//
// Nguồn tra cứu (chỉ đọc):
//   - Model cột:  12.Dev.Common/idn.Loyalty.Common/Models/Loyalty/Mst_PurchaseType.cs:9-15
//   - Hằng số mã: 12.Dev.Common/idn.Loyalty.Constants/Const.Main.BE.cs:1600-1605 (BUYADD, BUYNEW, BUYUSED, EXCHANGE)
//   - SQL select: idn.Loyalty.Biz/Master.Loyalty.cs:22129-22282 (Mst_PurchaseType_GetX — bản LIVE)
//
// ĐẶC ĐIỂM BẢNG:
//   - Đúng 4 cột: PurchaseType, PurchaseTypeName, Remark, FlagActive.
//   - Không có DDL trong repo; KHÔNG có NetworkID/LogLUDTimeUTC/LogLUBy trong model nguồn.
// ===========================================================================================

/// <summary>
/// Loại mua hàng / mua xe — port 1:1 bảng <c>Mst_PurchaseType</c>.
/// Model nguồn: Mst_PurchaseType.cs:9-15. PK = PurchaseType.
/// Đúng 4 cột, không có NetworkID / LogLUDTimeUTC / LogLUBy.
/// </summary>
public class Mst_PurchaseType
{
    public string PurchaseType { get; set; } = "";        // Mã loại mua hàng — PK (BUYADD, BUYNEW, BUYUSED, EXCHANGE)
    public string PurchaseTypeName { get; set; } = "";    // Tên loại mua hàng
    public string? Remark { get; set; }                   // Ghi chú
    public string? FlagActive { get; set; }               // Cờ hoạt động '1'/'0'
}

/// <summary>Cấu hình EF cho <see cref="Mst_PurchaseType"/> — tên bảng + khoá chính + cột bắt buộc.</summary>
public class Mst_PurchaseTypeConfig : IEntityTypeConfiguration<Mst_PurchaseType>
{
    public void Configure(EntityTypeBuilder<Mst_PurchaseType> b)
    {
        b.ToTable("Mst_PurchaseType");
        b.HasKey(x => x.PurchaseType);                    // PK = PurchaseType
        b.Property(x => x.PurchaseType).IsRequired();
        b.Property(x => x.PurchaseTypeName).IsRequired();
    }
}

/// <summary>
/// Dữ liệu mẫu cho danh mục loại mua hàng. Idempotent: đã có dòng nào thì bỏ qua.
/// 4 dòng theo hằng số mã: Const.Main.BE.cs:1600-1605.
/// Thứ tự Order => 13 (danh mục Mst_* dùng 10–99).
/// </summary>
public class Mst_PurchaseTypeSeeder : IPortSeeder
{
    public int Order => 13;   // danh mục Mst_* dùng 10–99

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<Mst_PurchaseType>().AnyAsync()) return;

        db.Set<Mst_PurchaseType>().AddRange(
            // Tên demo, cần xác nhận
            new Mst_PurchaseType { PurchaseType = "BUYADD", PurchaseTypeName = "Mua thêm", Remark = null, FlagActive = "1" },
            new Mst_PurchaseType { PurchaseType = "BUYNEW", PurchaseTypeName = "Mua mới", Remark = null, FlagActive = "1" },
            new Mst_PurchaseType { PurchaseType = "BUYUSED", PurchaseTypeName = "Mua xe cũ", Remark = null, FlagActive = "1" },
            new Mst_PurchaseType { PurchaseType = "EXCHANGE", PurchaseTypeName = "Đổi xe", Remark = null, FlagActive = "0" }
        );
        await db.SaveChangesAsync();
    }
}
