using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;

namespace MiniLoyalty.Models.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — bảng danh mục loại chi phí Mst_ExpenseType (#151).
//
// Nguồn tra cứu (chỉ đọc):
//   - Model cột:  12.Dev.Common/idn.Loyalty.Common/Models/Loyalty/Mst_ExpenseType.cs:9-18 (7 cột)
//   - DDL:        11.BackEnd/.../Migrate/20221006.QUYNHNTN/20221026.CreateDatabase.sql:183-191 (7 cột)
//   - SQL select: idn.Loyalty.Biz/Master.Loyalty.cs:9190-9349 (Mst_ExpenseType_GetX — bản LIVE)
//
// ĐẶC ĐIỂM BẢNG (đúng 7 cột):
//   ExpenseType, NetworkID, ExpenseTypeName, FlagActive, Remark, LogLUDTimeUTC, LogLUBy.
//   Khoá chính: (ExpenseType, NetworkID) — DDL nguồn không khai PK, dùng khoá gộp cho EF.
// ===========================================================================================

/// <summary>
/// Danh mục loại chi phí — port 1:1 bảng <c>Mst_ExpenseType</c>.
/// Model nguồn: Mst_ExpenseType.cs (7 cột). DDL: 20221026.CreateDatabase.sql:183-191.
/// Đúng 7 cột: ExpenseType, NetworkID, ExpenseTypeName, FlagActive, Remark, LogLUDTimeUTC, LogLUBy.
/// </summary>
public class Mst_ExpenseType
{
    public string ExpenseType { get; set; } = "";         // Mã loại chi phí (Varchar50 NOT NULL) — PK phần 1
    public string NetworkID { get; set; } = "";           // Mã mạng (Varchar50 NOT NULL) — PK phần 2
    public string ExpenseTypeName { get; set; } = "";     // Tên loại chi phí (Varchar200 NOT NULL)
    public string? FlagActive { get; set; }               // Cờ hoạt động '1'/'0' (MyStatus)
    public string? Remark { get; set; }                   // Ghi chú (Varchar400)
    public DateTime? LogLUDTimeUTC { get; set; }          // Thời điểm log cập nhật UTC (MyDateTime)
    public string? LogLUBy { get; set; }                  // Người cập nhật (Varchar50)
}

/// <summary>Cấu hình EF cho <see cref="Mst_ExpenseType"/> — tên bảng Mst_ExpenseType + khoá gộp (ExpenseType, NetworkID).</summary>
public class Mst_ExpenseTypeConfig : IEntityTypeConfiguration<Mst_ExpenseType>
{
    public void Configure(EntityTypeBuilder<Mst_ExpenseType> b)
    {
        b.ToTable("Mst_ExpenseType");
        b.HasKey(x => new { x.ExpenseType, x.NetworkID }); // DDL không khai PK rõ ràng, dùng khoá gộp
        b.Property(x => x.ExpenseType).IsRequired().HasMaxLength(50);
        b.Property(x => x.NetworkID).IsRequired().HasMaxLength(50);
        b.Property(x => x.ExpenseTypeName).IsRequired().HasMaxLength(200);
        b.Property(x => x.Remark).HasMaxLength(400);
        b.Property(x => x.LogLUBy).HasMaxLength(50);
    }
}

/// <summary>
/// Dữ liệu mẫu cho bảng Mst_ExpenseType theo P2 đề bài #151. Idempotent: đã có dữ liệu thì bỏ qua.
/// Thứ tự Order => 35 (danh mục Mst_* dùng 10–99).
/// </summary>
public class Mst_ExpenseTypeSeeder : IPortSeeder
{
    public int Order => 35; // danh mục Mst_* dùng 10–99

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<Mst_ExpenseType>().AnyAsync()) return;

        db.Set<Mst_ExpenseType>().AddRange(
            new Mst_ExpenseType
            {
                ExpenseType = "EX01",
                NetworkID = "4020320001",
                ExpenseTypeName = "Chi phi dich vu",
                FlagActive = "1",
                Remark = null,
                LogLUDTimeUTC = DateTime.SpecifyKind(DateTime.Parse("2026-01-01 00:00:00"), DateTimeKind.Utc),
                LogLUBy = "seed"
            },
            new Mst_ExpenseType
            {
                ExpenseType = "EX02",
                NetworkID = "4020320001",
                ExpenseTypeName = "Chi phi phu tung",
                FlagActive = "1",
                Remark = null,
                LogLUDTimeUTC = DateTime.SpecifyKind(DateTime.Parse("2026-02-01 00:00:00"), DateTimeKind.Utc),
                LogLUBy = "seed"
            },
            new Mst_ExpenseType
            {
                ExpenseType = "EX03",
                NetworkID = "4020320001",
                ExpenseTypeName = "Chi phi cu (ngung)",
                FlagActive = "0",
                Remark = null,
                LogLUDTimeUTC = DateTime.SpecifyKind(DateTime.Parse("2026-03-01 00:00:00"), DateTimeKind.Utc),
                LogLUBy = "seed"
            }
        );
        await db.SaveChangesAsync();
    }
}
