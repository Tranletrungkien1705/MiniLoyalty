using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;

namespace MiniLoyalty.Models.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — bảng tham số chương trình khuyến mại Mst_ParamPromotion (#183).
//
// Nguồn tra cứu (chỉ đọc):
//   - Model cột: 12.Dev.Common/idn.Loyalty.Common/Models/Loyalty/Mst_ParamPromotion.cs:9-21
//                (10 cột bảng; PrProgramName là cột của bảng nhưng khi SELECT ở GetX lại lấy từ
//                 Mst_PromotionProgram qua LEFT JOIN — xem comment dưới).
//   - DDL thật: 11.BackEnd/V10/05.Refs.Biz/Migrate/20221006.QUYNHNTN/20221026.CreateDatabase.sql:322-333
//               (đúng 10 cột), PK composite (PrProgramCode, ParamPrType) tại :337-340.
//               FK ParamPrType -> Mst_ParamPromotionType(ParamPrType) tại :364-371 — chỉ ghi comment.
//   - SQL select: idn.Loyalty.Biz/Master.Loyalty.cs:12427-12598 (Mst_ParamPromotion_GetX — bản LIVE).
//
// QUY ƯỚC KIỂU:
//   Varchar50/200/400 -> string? | MyStatus -> string? | MyDateTime -> DateTime? | MyValue -> double?
// ===========================================================================================

/// <summary>
/// Bảng tham số chương trình khuyến mại — port 1:1 bảng <c>Mst_ParamPromotion</c>.
/// DDL: 20221026.CreateDatabase.sql:322-333. Đúng 10 cột theo DDL.
/// PK composite (PrProgramCode, ParamPrType) tại 20221026.CreateDatabase.sql:337-340.
/// </summary>
public class Mst_ParamPromotion
{
    public string PrProgramCode { get; set; } = "";             // Mã chương trình khuyến mại — Varchar50 NOT NULL (PK phần 1)
    public string ParamPrType { get; set; } = "";               // Loại áp dụng — Varchar50 NOT NULL (PK phần 2)
    public string NetworkID { get; set; } = "";                 // NetworkID — Varchar50 NOT NULL
    public string? PrProgramName { get; set; }                  // Tên chương trình — Varchar200 (khi SELECT ở GetX lấy từ Mst_PromotionProgram, xem comment)
    public double? QtyDateBefore { get; set; }                  // Số ngày trước — MyValue (double?)
    public double? QtyDateAfter { get; set; }                   // Số ngày sau — MyValue (double?)
    public string? FlagActive { get; set; }                     // Cờ hoạt động '1'/'0' — MyStatus
    public string? Remark { get; set; }                         // Ghi chú — Varchar400
    public DateTime? LogLUDTimeUTC { get; set; }                // Thời điểm cập nhật UTC — MyDateTime
    public string? LogLUBy { get; set; }                        // Người cập nhật — Varchar50
}

/// <summary>
/// Cấu hình EF cho <see cref="Mst_ParamPromotion"/> — tên bảng + PK composite (PrProgramCode, ParamPrType)
/// + các cột NOT NULL đúng DDL nguồn 20221026.CreateDatabase.sql:322-333.
/// </summary>
public class Mst_ParamPromotionConfig : IEntityTypeConfiguration<Mst_ParamPromotion>
{
    public void Configure(EntityTypeBuilder<Mst_ParamPromotion> b)
    {
        b.ToTable("Mst_ParamPromotion");
        // PK composite (PrProgramCode, ParamPrType) — 20221026.CreateDatabase.sql:337-340
        b.HasKey(x => new { x.PrProgramCode, x.ParamPrType });
        b.Property(x => x.PrProgramCode).IsRequired();
        b.Property(x => x.ParamPrType).IsRequired();
        b.Property(x => x.NetworkID).IsRequired();
        // FK ParamPrType -> Mst_ParamPromotionType(ParamPrType) (20221026.CreateDatabase.sql:364-371) — chỉ ghi comment,
        // KHÔNG tạo navigation EF (bảng đích Mst_ParamPromotionType đã có entity riêng ở Mst_BirthPolicy.cs).
    }
}

/// <summary>
/// Dữ liệu mẫu khởi tạo cho bảng <see cref="Mst_ParamPromotion"/>.
/// Idempotent (kiểm tra trước khi chèn), Order = 44 (danh mục Mst_* dùng 10–99).
/// NetworkID 'DEMO', LogLUBy 'seed', LogLUDTimeUTC cố định giảm dần để thứ tự hoàn toàn xác định.
/// 6 dòng: PR01/TYPE_A, PR01/TYPE_B, PR02/TYPE_A, PR02/TYPE_B, PR03/TYPE_A, PR_NONE/TYPE_A.
/// </summary>
public class Mst_ParamPromotionSeeder : IPortSeeder
{
    public int Order => 24;   // danh mục Mst_* dùng 10–99

    public async Task SeedAsync(AppDbContext db)
    {
        if (!await db.Set<Mst_ParamPromotion>().AnyAsync())
        {
            db.Set<Mst_ParamPromotion>().AddRange(
                new Mst_ParamPromotion
                {
                    PrProgramCode = "PR01",
                    ParamPrType = "TYPE_A",
                    NetworkID = "DEMO",
                    PrProgramName = "Ten param 1",
                    QtyDateBefore = 5.0,
                    QtyDateAfter = 10.0,
                    FlagActive = "1",
                    Remark = "Ghi chu 1",
                    LogLUBy = "seed",
                    LogLUDTimeUTC = new DateTime(2026, 10, 10, 15, 0, 0, DateTimeKind.Utc),
                },
                new Mst_ParamPromotion
                {
                    PrProgramCode = "PR01",
                    ParamPrType = "TYPE_B",
                    NetworkID = "DEMO",
                    PrProgramName = "Ten param 2",
                    QtyDateBefore = 3.0,
                    QtyDateAfter = 7.0,
                    FlagActive = "1",
                    Remark = "Ghi chu 2",
                    LogLUBy = "seed",
                    LogLUDTimeUTC = new DateTime(2026, 10, 10, 14, 0, 0, DateTimeKind.Utc),
                },
                new Mst_ParamPromotion
                {
                    PrProgramCode = "PR02",
                    ParamPrType = "TYPE_A",
                    NetworkID = "DEMO",
                    PrProgramName = "Ten param 3",
                    QtyDateBefore = 1.0,
                    QtyDateAfter = 2.0,
                    FlagActive = "1",
                    Remark = "Ghi chu 3",
                    LogLUBy = "seed",
                    LogLUDTimeUTC = new DateTime(2026, 10, 10, 13, 0, 0, DateTimeKind.Utc),
                },
                new Mst_ParamPromotion
                {
                    PrProgramCode = "PR02",
                    ParamPrType = "TYPE_B",
                    NetworkID = "DEMO",
                    PrProgramName = "Ten param 4",
                    QtyDateBefore = 0.0,
                    QtyDateAfter = 0.0,
                    FlagActive = "1",
                    Remark = "Ghi chu 4",
                    LogLUBy = "seed",
                    LogLUDTimeUTC = new DateTime(2026, 10, 10, 12, 0, 0, DateTimeKind.Utc),
                },
                new Mst_ParamPromotion
                {
                    PrProgramCode = "PR03",
                    ParamPrType = "TYPE_A",
                    NetworkID = "DEMO",
                    PrProgramName = "Ten param 5",
                    QtyDateBefore = 15.0,
                    QtyDateAfter = 30.0,
                    FlagActive = "0",
                    Remark = "Ghi chu 5",
                    LogLUBy = "seed",
                    LogLUDTimeUTC = new DateTime(2026, 10, 10, 11, 0, 0, DateTimeKind.Utc),
                },
                new Mst_ParamPromotion
                {
                    PrProgramCode = "PR_NONE",
                    ParamPrType = "TYPE_A",
                    NetworkID = "DEMO",
                    PrProgramName = "Ten khong map",
                    QtyDateBefore = 2.0,
                    QtyDateAfter = 4.0,
                    FlagActive = "0",
                    Remark = "Ghi chu 6",
                    LogLUBy = "seed",
                    LogLUDTimeUTC = new DateTime(2026, 10, 10, 10, 0, 0, DateTimeKind.Utc),
                }
            );
            await db.SaveChangesAsync();
        }
    }
}
