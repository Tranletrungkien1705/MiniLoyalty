using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;

namespace MiniLoyalty.Models.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — bảng danh mục loại giấy tờ tuỳ thân Mst_GovIDType (#156).
//
// Nguồn tra cứu (chỉ đọc):
//   - Model cột:  12.Dev.Common/idn.Loyalty.Common/Models/Mst_GovIDType.cs:9-24 (7 cột)
//   - DDL:        Không có DDL trong repo — kiểu cột cần xác nhận, PK suy ra từ GetX
//   - SQL select: idn.Loyalty.Biz/Master.cs (Mst_GovIDType_GetX — bản LIVE :32456-32609)
//
// ĐẶC ĐIỂM BẢNG (đúng 7 cột):
//   GovIDType (PK, string not null), NetworkID (string?), GovIDTypeName (string?),
//   Remark (string?), FlagActive (string?), LogLUDTimeUTC (DateTime?), LogLUBy (string?).
// ===========================================================================================

/// <summary>
/// Danh mục loại giấy tờ tuỳ thân — port 1:1 bảng <c>Mst_GovIDType</c>.
/// Model nguồn: Mst_GovIDType.cs:9-24. PK = GovIDType (suy ra từ inner join GetX Master.cs:32511, :32554).
/// Không có DDL trong repo — kiểu cột varchar/string, LogLUDTimeUTC là DateTime?.
/// Đúng 7 cột: GovIDType, NetworkID, GovIDTypeName, Remark, FlagActive, LogLUDTimeUTC, LogLUBy.
/// </summary>
public class Mst_GovIDType
{
    public string GovIDType { get; set; } = "";           // Mã loại giấy tờ tuỳ thân — PK (suy ra từ GetX Master.cs:32511, :32554)
    public string? NetworkID { get; set; }                // Mã mạng
    public string? GovIDTypeName { get; set; }            // Tên loại giấy tờ tuỳ thân
    public string? Remark { get; set; }                   // Ghi chú
    public string? FlagActive { get; set; }               // Cờ hoạt động '1'/'0'
    public DateTime? LogLUDTimeUTC { get; set; }          // Thời điểm log cập nhật UTC
    public string? LogLUBy { get; set; }                  // Người cập nhật
}

/// <summary>Cấu hình EF cho <see cref="Mst_GovIDType"/> — tên bảng Mst_GovIDType + khoá chính GovIDType.</summary>
public class Mst_GovIDTypeConfig : IEntityTypeConfiguration<Mst_GovIDType>
{
    public void Configure(EntityTypeBuilder<Mst_GovIDType> b)
    {
        b.ToTable("Mst_GovIDType");
        b.HasKey(x => x.GovIDType);                       // PK = GovIDType
        b.Property(x => x.GovIDType).IsRequired();
    }
}

/// <summary>
/// Dữ liệu mẫu cho bảng Mst_GovIDType. Idempotent: đã có dữ liệu thì bỏ qua.
/// Dữ liệu demo (Order => 40 theo đề bài): CCCD, CMND, GPLX, GTK, HC (GTK FlagActive '0').
/// </summary>
public class Mst_GovIDTypeSeeder : IPortSeeder
{
    public int Order => 40;   // danh mục Mst_* dùng 10–99, đề bài quy định Order 40

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<Mst_GovIDType>().AnyAsync()) return;

        var fixedTime = new DateTime(2026, 10, 10, 10, 0, 0, DateTimeKind.Utc);

        db.Set<Mst_GovIDType>().AddRange(
            // Dữ liệu demo (comment "du lieu demo") — không có DDL trong repo, kiểu cột và dữ liệu tĩnh cần xác nhận
            new Mst_GovIDType { GovIDType = "CCCD", GovIDTypeName = "Căn cước công dân", FlagActive = "1", NetworkID = "DEMO", Remark = "Seed demo", LogLUDTimeUTC = fixedTime, LogLUBy = "seed" },
            new Mst_GovIDType { GovIDType = "CMND", GovIDTypeName = "Chứng minh nhân dân", FlagActive = "1", NetworkID = "DEMO", Remark = "Seed demo", LogLUDTimeUTC = fixedTime, LogLUBy = "seed" },
            new Mst_GovIDType { GovIDType = "GPLX", GovIDTypeName = "Giấy phép lái xe", FlagActive = "1", NetworkID = "DEMO", Remark = "Seed demo", LogLUDTimeUTC = fixedTime, LogLUBy = "seed" },
            new Mst_GovIDType { GovIDType = "GTK",  GovIDTypeName = "Giấy tờ khác", FlagActive = "0", NetworkID = "DEMO", Remark = "Seed demo", LogLUDTimeUTC = fixedTime, LogLUBy = "seed" },
            new Mst_GovIDType { GovIDType = "HC",   GovIDTypeName = "Hộ chiếu", FlagActive = "1", NetworkID = "DEMO", Remark = "Seed demo", LogLUDTimeUTC = fixedTime, LogLUBy = "seed" }
        );
        await db.SaveChangesAsync();
    }
}
