using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;

namespace MiniLoyalty.Models.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — chính sách sinh nhật Mst_BirthPolicy (#70).
//
// Nguồn tra cứu (chỉ đọc):
//   - Model cột header: 12.Dev.Common/idn.Loyalty.Common/Models/Loyalty/Mst_BirthPolicy.cs:9-25
//     (chú ý: model nguồn có FlagPoint/FlagVoucher/FlagShow — KHÔNG phải cột bảng DB).
//   - DDL thật header:  11.BackEnd/V10/05.Refs.Biz/Migrate/20221006.QUYNHNTN/20221026.CreateDatabase.sql:399-418 (12 cột).
//     * Chọn bản 20221026.CreateDatabase.sql:399-418 vì khớp dữ liệu thật (đủ 12 cột, có EffDateEnd, UpdateDTime, UpdateBy)
//       thay vì bản cũ Mst_BirthPolicy.sql:96 thiếu EffDateEnd; khớp với 20260731.z11.Mst_BirthPolicy.Disable.sql.
//     * PK BirthPolicyNo tại :416-419.
//   - DDL thật Dtl:     11.BackEnd/V10/05.Refs.Biz/Migrate/20221006.QUYNHNTN/20221026.CreateDatabase.sql:378-394 (7 cột).
//     * PK composite (BirthPolicyNo, CardType) tại :390-393.
//     * 3 cột voucher bổ sung từ migration 2026: VoucherValue, VoucherPointVCLimit, VoucherExpireDays
//       (20260518.z11.RankRN_BirthVoucher.sql:114-130 / 20260723.z13.BirthPolicyDtl_VoucherMaster.sql) => tổng 10 cột.
//     * FK CardType -> Mst_CardType (:422-428) — chỉ ghi comment, chưa cấu hình FK navigation EF.
//     * FK BirthPolicyNo -> Mst_BirthPolicy (:431-435) — chỉ ghi comment, chưa cấu hình FK navigation EF.
//   - SQL select:       idn.Loyalty.Biz/Master.Loyalty.cs:10877-11085 (Mst_BirthPolicy_GetX — bản LIVE).
//
// QUY ƯỚC KIỂU (ánh xạ user-defined type của SQL Server nguồn):
//   Varchar50/200/400 -> string? | MyValue / decimal(28,8) -> double? | MyStatus -> string? | MyDateTime -> DateTime / DateTime?
// ===========================================================================================

/// <summary>
/// Bảng header chính sách sinh nhật — port 1:1 bảng <c>Mst_BirthPolicy</c>.
/// DDL: 20221026.CreateDatabase.sql:399-418. Đúng 12 cột theo DDL.
/// PK = BirthPolicyNo tại 20221026.CreateDatabase.sql:416-419.
/// Model nguồn có các trường FlagPoint, FlagVoucher, FlagShow nhưng không phải cột bảng trong DB.
/// FlagShow là cột tính lúc truy vấn theo @dateSys (Master.Loyalty.cs:10984-10996).
/// </summary>
public class Mst_BirthPolicy
{
    public string BirthPolicyNo { get; set; } = "";             // Mã chính sách sinh nhật — Varchar50 NOT NULL (PK)
    public string NetworkID { get; set; } = "";                  // NetworkID — Varchar50 NOT NULL
    public string? ParamPrType { get; set; }                     // Loại ưu đãi — Varchar50
    public string? BirthPolicyName { get; set; }                 // Tên chương trình sinh nhật — Varchar200
    public DateTime EffDateStart { get; set; }                   // Ngày bắt đầu hiệu lực — MyDateTime NOT NULL
    public DateTime EffDateEnd { get; set; }                     // Ngày kết thúc hiệu lực — MyDateTime NOT NULL
    public string? FlagActive { get; set; }                      // Cờ hoạt động '1'/'0' — MyStatus
    public string? Remark { get; set; }                          // Ghi chú — Varchar400
    public DateTime? UpdateDTime { get; set; }                   // Thời điểm cập nhật — MyDateTime NULL
    public string? UpdateBy { get; set; }                        // Người cập nhật — Varchar50 NULL
    public DateTime? LogLUDTimeUTC { get; set; }                 // Thời điểm cập nhật UTC — MyDateTime
    public string? LogLUBy { get; set; }                         // Người cập nhật — Varchar50
}

/// <summary>
/// Cấu hình EF cho <see cref="Mst_BirthPolicy"/> — tên bảng + PK BirthPolicyNo
/// + các cột NOT NULL đúng DDL nguồn 20221026.CreateDatabase.sql:399-418.
/// </summary>
public class Mst_BirthPolicyConfig : IEntityTypeConfiguration<Mst_BirthPolicy>
{
    public void Configure(EntityTypeBuilder<Mst_BirthPolicy> b)
    {
        b.ToTable("Mst_BirthPolicy");
        // PK BirthPolicyNo — 20221026.CreateDatabase.sql:416-419
        b.HasKey(x => x.BirthPolicyNo);
        b.Property(x => x.BirthPolicyNo).IsRequired();
        b.Property(x => x.NetworkID).IsRequired();
        b.Property(x => x.EffDateStart).IsRequired();
        b.Property(x => x.EffDateEnd).IsRequired();
    }
}

/// <summary>
/// Bảng chi tiết chính sách sinh nhật — port 1:1 bảng <c>Mst_BirthPolicyDtl</c>.
/// DDL: 20221026.CreateDatabase.sql:378-394 (7 cột) + 3 cột bổ sung từ migration 2026 => 10 cột.
/// PK composite = (BirthPolicyNo, CardType) tại 20221026.CreateDatabase.sql:390-393.
/// FK CardType -> Mst_CardType (20221026.CreateDatabase.sql:422-428) — chỉ ghi comment, chưa cấu hình FK navigation EF.
/// FK BirthPolicyNo -> Mst_BirthPolicy (20221026.CreateDatabase.sql:431-435) — chỉ ghi comment, chưa cấu hình FK navigation EF.
/// </summary>
public class Mst_BirthPolicyDtl
{
    public string BirthPolicyNo { get; set; } = "";             // Mã chính sách sinh nhật — Varchar50 NOT NULL (PK component 1)
    public string CardType { get; set; } = "";                   // Hạng thẻ — Varchar50 NOT NULL (PK component 2)
    public string NetworkID { get; set; } = "";                  // NetworkID — Varchar50 NOT NULL
    public double? Point { get; set; }                           // Điểm tặng — MyValue (double?)
    public string? Remark { get; set; }                          // Ghi chú — Varchar400
    public DateTime? LogLUDTimeUTC { get; set; }                 // Thời điểm cập nhật UTC — MyDateTime
    public string? LogLUBy { get; set; }                         // Người cập nhật — Varchar50

    // 3 cột bổ sung từ migration 2026 (20260518.z11.RankRN_BirthVoucher.sql:114-130 / 20260723.z13.BirthPolicyDtl_VoucherMaster.sql):
    public double? VoucherValue { get; set; }                    // Giá trị voucher — decimal(28,8) null
    public double? VoucherPointVCLimit { get; set; }             // Giới hạn điểm đổi voucher — decimal(28,8) null
    public int? VoucherExpireDays { get; set; }                  // Hạn sử dụng voucher (ngày) — int null
}

/// <summary>
/// Cấu hình EF cho <see cref="Mst_BirthPolicyDtl"/> — tên bảng + PK composite (BirthPolicyNo, CardType)
/// + các cột NOT NULL đúng DDL nguồn 20221026.CreateDatabase.sql:378-394.
/// </summary>
public class Mst_BirthPolicyDtlConfig : IEntityTypeConfiguration<Mst_BirthPolicyDtl>
{
    public void Configure(EntityTypeBuilder<Mst_BirthPolicyDtl> b)
    {
        b.ToTable("Mst_BirthPolicyDtl");
        // PK composite = (BirthPolicyNo, CardType) — 20221026.CreateDatabase.sql:390-393
        b.HasKey(x => new { x.BirthPolicyNo, x.CardType });
        b.Property(x => x.BirthPolicyNo).IsRequired();
        b.Property(x => x.CardType).IsRequired();
        b.Property(x => x.NetworkID).IsRequired();
        // FK CardType -> Mst_CardType (20221026.CreateDatabase.sql:422-428) — chỉ ghi comment.
        // FK BirthPolicyNo -> Mst_BirthPolicy (20221026.CreateDatabase.sql:431-435) — chỉ ghi comment.
    }
}

/// <summary>
/// Dữ liệu mẫu cho 2 bảng Mst_BirthPolicy và Mst_BirthPolicyDtl. Idempotent: đã có dòng nào thì bỏ qua.
/// Thứ tự Order => 27 (danh mục Mst_* dùng 10–99).
/// Dữ liệu demo gồm:
/// - Header (3 dòng):
///   * BP01: 'Sinh nhật 2026' (2026-01-01..2100-01-01, FlagActive '1', LogLUDTimeUTC 2026-10-10 10:00:00 UTC)
///   * BP02: 'Sinh nhật 2025' (2025-01-01..2025-12-31, FlagActive '0', LogLUDTimeUTC 2026-10-10 09:00:00 UTC)
///   * BP03: 'Sinh nhật 2027' (2027-01-01..2100-01-01, FlagActive '1', LogLUDTimeUTC 2026-10-10 11:00:00 UTC)
/// - Dtl (4 dòng):
///   * BP01 × S: Point 0
///   * BP01 × G: Point 100000, VoucherValue 500000, VoucherExpireDays 30
///   * BP01 × P: Point 200000
///   * BP03 × G: Point 150000
/// </summary>
public class Mst_BirthPolicySeeder : IPortSeeder
{
    public int Order => 27; // Danh mục Mst_* dùng 10–99

    public async Task SeedAsync(AppDbContext db)
    {
        if (!await db.Set<Mst_BirthPolicy>().AnyAsync())
        {
            db.Set<Mst_BirthPolicy>().AddRange(
                // BP01: Active '1', LogLUDTimeUTC 10:00:00 UTC
                new Mst_BirthPolicy
                {
                    BirthPolicyNo = "BP01",
                    NetworkID = "DEMO",
                    ParamPrType = "NGAYSINHNHAT",
                    BirthPolicyName = "Sinh nhật 2026",
                    EffDateStart = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    EffDateEnd = new DateTime(2100, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    FlagActive = "1",
                    Remark = "Chính sách sinh nhật 2026",
                    LogLUBy = "seed",
                    LogLUDTimeUTC = new DateTime(2026, 10, 10, 10, 0, 0, DateTimeKind.Utc),
                },
                // BP02: Inactive '0', LogLUDTimeUTC 09:00:00 UTC
                new Mst_BirthPolicy
                {
                    BirthPolicyNo = "BP02",
                    NetworkID = "DEMO",
                    ParamPrType = "NGAYSINHNHAT",
                    BirthPolicyName = "Sinh nhật 2025",
                    EffDateStart = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    EffDateEnd = new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                    FlagActive = "0",
                    Remark = "Chính sách sinh nhật 2025 (hết hạn)",
                    LogLUBy = "seed",
                    LogLUDTimeUTC = new DateTime(2026, 10, 10, 9, 0, 0, DateTimeKind.Utc),
                },
                // BP03: Active '1', LogLUDTimeUTC 11:00:00 UTC
                new Mst_BirthPolicy
                {
                    BirthPolicyNo = "BP03",
                    NetworkID = "DEMO",
                    ParamPrType = "NGAYSINHNHAT",
                    BirthPolicyName = "Sinh nhật 2027",
                    EffDateStart = new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    EffDateEnd = new DateTime(2100, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    FlagActive = "1",
                    Remark = "Chính sách sinh nhật 2027",
                    LogLUBy = "seed",
                    LogLUDTimeUTC = new DateTime(2026, 10, 10, 11, 0, 0, DateTimeKind.Utc),
                }
            );
        }

        if (!await db.Set<Mst_BirthPolicyDtl>().AnyAsync())
        {
            db.Set<Mst_BirthPolicyDtl>().AddRange(
                // BP01 × S: Point 0
                new Mst_BirthPolicyDtl
                {
                    BirthPolicyNo = "BP01",
                    CardType = "S",
                    NetworkID = "DEMO",
                    Point = 0,
                    Remark = "Dtl BP01 hạng S",
                    LogLUBy = "seed",
                    LogLUDTimeUTC = new DateTime(2026, 10, 10, 10, 0, 0, DateTimeKind.Utc),
                },
                // BP01 × G: Point 100000, VoucherValue 500000, VoucherExpireDays 30
                new Mst_BirthPolicyDtl
                {
                    BirthPolicyNo = "BP01",
                    CardType = "G",
                    NetworkID = "DEMO",
                    Point = 100000,
                    VoucherValue = 500000,
                    VoucherExpireDays = 30,
                    Remark = "Dtl BP01 hạng G",
                    LogLUBy = "seed",
                    LogLUDTimeUTC = new DateTime(2026, 10, 10, 10, 0, 0, DateTimeKind.Utc),
                },
                // BP01 × P: Point 200000
                new Mst_BirthPolicyDtl
                {
                    BirthPolicyNo = "BP01",
                    CardType = "P",
                    NetworkID = "DEMO",
                    Point = 200000,
                    Remark = "Dtl BP01 hạng P",
                    LogLUBy = "seed",
                    LogLUDTimeUTC = new DateTime(2026, 10, 10, 10, 0, 0, DateTimeKind.Utc),
                },
                // BP03 × G: Point 150000
                new Mst_BirthPolicyDtl
                {
                    BirthPolicyNo = "BP03",
                    CardType = "G",
                    NetworkID = "DEMO",
                    Point = 150000,
                    Remark = "Dtl BP03 hạng G",
                    LogLUBy = "seed",
                    LogLUDTimeUTC = new DateTime(2026, 10, 10, 11, 0, 0, DateTimeKind.Utc),
                }
            );
        }

        await db.SaveChangesAsync();
    }
}
