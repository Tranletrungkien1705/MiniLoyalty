using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;

namespace MiniLoyalty.Models.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — chính sách quy đổi tiền sang điểm Mst_PolicyMoneyToPointService (#194).
//
// Nguồn tra cứu (chỉ đọc):
//   - Model cột header: 12.Dev.Common/idn.Loyalty.Common/Models/Loyalty/Mst_PolicyMoneyToPointService.cs:9-21
//   - Model cột dtl:    12.Dev.Common/idn.Loyalty.Common/Models/Loyalty/Mst_PolicyMoneyToPointServiceDtl.cs:9-23
//   - DDL thật:         11.BackEnd/V10/05.Refs.Biz/Migrate/20221006.QUYNHNTN/20221026.CreateDatabase.sql
//                       * Mst_PolicyMoneyToPointService: :634-651 (8 cột), PK PolicyCode tại :647-650.
//                       * Mst_PolicyMoneyToPointServiceDtl: :610-629 (10 cột), PK (PolicyCode, CardType) tại :625-628.
//                       * FK CardType -> Mst_CardType (:656-662) — chỉ ghi comment.
//                       * FK PolicyCode -> Mst_PolicyMoneyToPointService (:665-670) — chỉ ghi comment.
//   - SQL select:       idn.Loyalty.Biz/Master.Loyalty.cs:18618-18820 (Mst_PolicyMoneyToPointService_GetX — bản LIVE)
//
// QUY ƯỚC KIỂU (ánh xạ user-defined type của SQL Server nguồn):
//   Varchar50/400 -> string?   | MyValue -> double?   | MyStatus -> string?   | MyDateTime -> DateTime? (hoặc DateTime nếu NOT NULL)
// ===========================================================================================

/// <summary>
/// Bảng header chính sách quy đổi tiền sang điểm dịch vụ — port 1:1 bảng <c>Mst_PolicyMoneyToPointService</c>.
/// DDL: 20221026.CreateDatabase.sql:634-651. Đúng 8 cột theo DDL.
/// PK = PolicyCode tại 20221026.CreateDatabase.sql:647-650.
/// </summary>
public class Mst_PolicyMoneyToPointService
{
    public string PolicyCode { get; set; } = "";             // Mã chính sách — Varchar50 NOT NULL (PK)
    public string NetworkID { get; set; } = "";              // NetworkID — Varchar50 NOT NULL
    public DateTime EffDateStart { get; set; }               // Ngày bắt đầu hiệu lực — MyDateTime NOT NULL
    public DateTime EffDateEnd { get; set; }                 // Ngày kết thúc hiệu lực — MyDateTime NOT NULL
    public string? FlagActive { get; set; }                  // Cờ hoạt động '1'/'0' — MyStatus
    public string? Remark { get; set; }                      // Ghi chú — Varchar400
    public string? LogLUBy { get; set; }                     // Người cập nhật — Varchar50
    public DateTime? LogLUDTimeUTC { get; set; }             // Thời điểm cập nhật UTC — MyDateTime
}

/// <summary>
/// Cấu hình EF cho <see cref="Mst_PolicyMoneyToPointService"/> — tên bảng + PK PolicyCode
/// + các cột NOT NULL đúng DDL nguồn 20221026.CreateDatabase.sql:634-651.
/// </summary>
public class Mst_PolicyMoneyToPointServiceConfig : IEntityTypeConfiguration<Mst_PolicyMoneyToPointService>
{
    public void Configure(EntityTypeBuilder<Mst_PolicyMoneyToPointService> b)
    {
        b.ToTable("Mst_PolicyMoneyToPointService");
        // PK PolicyCode — 20221026.CreateDatabase.sql:647-650
        b.HasKey(x => x.PolicyCode);
        b.Property(x => x.PolicyCode).IsRequired();
        b.Property(x => x.NetworkID).IsRequired();
        b.Property(x => x.EffDateStart).IsRequired();
        b.Property(x => x.EffDateEnd).IsRequired();
    }
}

/// <summary>
/// Bảng chi tiết chính sách quy đổi tiền sang điểm dịch vụ — port 1:1 bảng <c>Mst_PolicyMoneyToPointServiceDtl</c>.
/// DDL: 20221026.CreateDatabase.sql:610-629. Đúng 10 cột theo DDL.
/// PK composite = (PolicyCode, CardType) tại 20221026.CreateDatabase.sql:625-628.
/// FK CardType -> Mst_CardType (20221026.CreateDatabase.sql:656-662) — chỉ ghi comment, không cấu hình FK navigation EF.
/// FK PolicyCode -> Mst_PolicyMoneyToPointService (20221026.CreateDatabase.sql:665-670) — chỉ ghi comment, không cấu hình FK navigation EF.
/// </summary>
public class Mst_PolicyMoneyToPointServiceDtl
{
    public string PolicyCode { get; set; } = "";             // Mã chính sách — Varchar50 NOT NULL (PK component 1)
    public string CardType { get; set; } = "";               // Hạng thẻ — Varchar50 NOT NULL (PK component 2)
    public string NetworkID { get; set; } = "";              // NetworkID — Varchar50 NOT NULL
    public double? ConvertValue { get; set; }                // Giá trị quy đổi — MyValue
    public double? ConvertPoint { get; set; }                // Điểm quy đổi — MyValue
    public double? ValueRankCardType { get; set; }           // Mốc tính lượt hạng thẻ — MyValue
    public string? FlagActive { get; set; }                  // Cờ hoạt động '1'/'0' — MyStatus
    public string? Remark { get; set; }                      // Ghi chú — Varchar400
    public string? LogLUBy { get; set; }                     // Người cập nhật — Varchar50
    public DateTime? LogLUDTimeUTC { get; set; }             // Thời điểm cập nhật UTC — MyDateTime
}

/// <summary>
/// Cấu hình EF cho <see cref="Mst_PolicyMoneyToPointServiceDtl"/> — tên bảng + PK composite (PolicyCode, CardType)
/// + các cột NOT NULL đúng DDL nguồn 20221026.CreateDatabase.sql:610-629.
/// </summary>
public class Mst_PolicyMoneyToPointServiceDtlConfig : IEntityTypeConfiguration<Mst_PolicyMoneyToPointServiceDtl>
{
    public void Configure(EntityTypeBuilder<Mst_PolicyMoneyToPointServiceDtl> b)
    {
        b.ToTable("Mst_PolicyMoneyToPointServiceDtl");
        // PK (PolicyCode, CardType) — 20221026.CreateDatabase.sql:625-628
        b.HasKey(x => new { x.PolicyCode, x.CardType });
        b.Property(x => x.PolicyCode).IsRequired();
        b.Property(x => x.CardType).IsRequired();
        b.Property(x => x.NetworkID).IsRequired();
        // FK CardType -> Mst_CardType (20221026.CreateDatabase.sql:656-662) — chỉ ghi comment, không cấu hình FK navigation EF.
        // FK PolicyCode -> Mst_PolicyMoneyToPointService (20221026.CreateDatabase.sql:665-670) — chỉ ghi comment, không cấu hình FK navigation EF.
    }
}

/// <summary>
/// Dữ liệu mẫu cho 2 bảng Mst_PolicyMoneyToPointService và Mst_PolicyMoneyToPointServiceDtl. Idempotent: đã có dòng nào thì bỏ qua.
/// Thứ tự Order => 26 (danh mục Mst_* dùng 10–99).
/// Dữ liệu demo gồm:
/// - Header (3 dòng):
///   * P01: EffDateStart 2026-01-01, EffDateEnd 2026-12-31, FlagActive '1', LogLUDTimeUTC 2026-10-10 10:00:00 UTC
///   * P02: EffDateStart 2025-01-01, EffDateEnd 2025-12-31, FlagActive '0' (inactive), LogLUDTimeUTC 2026-10-10 09:00:00 UTC
///   * P03: EffDateStart 2027-01-01, EffDateEnd 2027-12-31, FlagActive '1', LogLUDTimeUTC 2026-10-10 11:00:00 UTC
/// - Dtl (4 dòng):
///   * P01 × N: ConvertValue 100000, ConvertPoint 1, ValueRankCardType 5, FlagActive '1'
///   * P01 × I: ConvertValue 100000, ConvertPoint 2, ValueRankCardType 10, FlagActive '1'
///   * P01 × S: ConvertValue 100000, ConvertPoint 3, ValueRankCardType 20, FlagActive '1'
///   * P03 × G: ConvertValue 100000, ConvertPoint 5, ValueRankCardType 30, FlagActive '1'
/// </summary>
public class Mst_PolicyMoneyToPointServiceSeeder : IPortSeeder
{
    public int Order => 26; // Danh mục Mst_* dùng 10–99

    public async Task SeedAsync(AppDbContext db)
    {
        if (!await db.Set<Mst_PolicyMoneyToPointService>().AnyAsync())
        {
            db.Set<Mst_PolicyMoneyToPointService>().AddRange(
                // P01: Active '1', LogLUDTimeUTC 10:00:00 UTC
                new Mst_PolicyMoneyToPointService
                {
                    PolicyCode = "P01",
                    NetworkID = "DEMO",
                    EffDateStart = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    EffDateEnd = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                    FlagActive = "1",
                    Remark = "Chính sách quy đổi tiền sang điểm P01 (active)",
                    LogLUBy = "seed",
                    LogLUDTimeUTC = new DateTime(2026, 10, 10, 10, 0, 0, DateTimeKind.Utc),
                },
                // P02: Inactive '0', LogLUDTimeUTC 09:00:00 UTC
                new Mst_PolicyMoneyToPointService
                {
                    PolicyCode = "P02",
                    NetworkID = "DEMO",
                    EffDateStart = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    EffDateEnd = new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                    FlagActive = "0",
                    Remark = "Chính sách quy đổi tiền sang điểm P02 (inactive)",
                    LogLUBy = "seed",
                    LogLUDTimeUTC = new DateTime(2026, 10, 10, 9, 0, 0, DateTimeKind.Utc),
                },
                // P03: Active '1', LogLUDTimeUTC 11:00:00 UTC
                new Mst_PolicyMoneyToPointService
                {
                    PolicyCode = "P03",
                    NetworkID = "DEMO",
                    EffDateStart = new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    EffDateEnd = new DateTime(2027, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                    FlagActive = "1",
                    Remark = "Chính sách quy đổi tiền sang điểm P03 (active)",
                    LogLUBy = "seed",
                    LogLUDTimeUTC = new DateTime(2026, 10, 10, 11, 0, 0, DateTimeKind.Utc),
                }
            );
        }

        if (!await db.Set<Mst_PolicyMoneyToPointServiceDtl>().AnyAsync())
        {
            db.Set<Mst_PolicyMoneyToPointServiceDtl>().AddRange(
                // P01 × N
                new Mst_PolicyMoneyToPointServiceDtl
                {
                    PolicyCode = "P01",
                    CardType = "N",
                    NetworkID = "DEMO",
                    ConvertValue = 100000,
                    ConvertPoint = 1,
                    ValueRankCardType = 5,
                    FlagActive = "1",
                    Remark = "Dtl P01 hạng N",
                    LogLUBy = "seed",
                    LogLUDTimeUTC = new DateTime(2026, 10, 10, 10, 0, 0, DateTimeKind.Utc),
                },
                // P01 × I
                new Mst_PolicyMoneyToPointServiceDtl
                {
                    PolicyCode = "P01",
                    CardType = "I",
                    NetworkID = "DEMO",
                    ConvertValue = 100000,
                    ConvertPoint = 2,
                    ValueRankCardType = 10,
                    FlagActive = "1",
                    Remark = "Dtl P01 hạng I",
                    LogLUBy = "seed",
                    LogLUDTimeUTC = new DateTime(2026, 10, 10, 10, 0, 0, DateTimeKind.Utc),
                },
                // P01 × S
                new Mst_PolicyMoneyToPointServiceDtl
                {
                    PolicyCode = "P01",
                    CardType = "S",
                    NetworkID = "DEMO",
                    ConvertValue = 100000,
                    ConvertPoint = 3,
                    ValueRankCardType = 20,
                    FlagActive = "1",
                    Remark = "Dtl P01 hạng S",
                    LogLUBy = "seed",
                    LogLUDTimeUTC = new DateTime(2026, 10, 10, 10, 0, 0, DateTimeKind.Utc),
                },
                // P03 × G
                new Mst_PolicyMoneyToPointServiceDtl
                {
                    PolicyCode = "P03",
                    CardType = "G",
                    NetworkID = "DEMO",
                    ConvertValue = 100000,
                    ConvertPoint = 5,
                    ValueRankCardType = 30,
                    FlagActive = "1",
                    Remark = "Dtl P03 hạng G",
                    LogLUBy = "seed",
                    LogLUDTimeUTC = new DateTime(2026, 10, 10, 11, 0, 0, DateTimeKind.Utc),
                }
            );
        }

        await db.SaveChangesAsync();
    }
}
