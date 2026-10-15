using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;

namespace MiniLoyalty.Models.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — bảng chính sách xếp hạng Mst_RankPolicy (#227).
//
// Nguồn tra cứu (chỉ đọc):
//   - Model cột:  12.Dev.Common/idn.Loyalty.Common/Models/Loyalty/Mst_RankPolicy.cs:9-28
//   - DDL thật:   11.BackEnd/V10/05.Refs.Biz/Migrate/20221006.QUYNHNTN/20221026.CreateDatabase.sql:204-222
//                 PK (CardType, RankPolicyCode) tại :226-228.
//                 FK CardType -> Mst_CardType (20221026.CreateDatabase.sql:273) — chỉ ghi comment.
//   - SQL select: idn.Loyalty.Biz/Master.Loyalty.cs:7697-7872 (Mst_RankPolicy_GetX — bản LIVE)
//
// QUY ƯỚC KIỂU (ánh xạ user-defined type của SQL Server nguồn):
//   Varchar50/400 -> string?   | MyValue -> double?   | MyStatus -> string?   | MyDateTime -> DateTime?
// ===========================================================================================

/// <summary>
/// Chính sách xếp hạng thẻ — port 1:1 bảng <c>Mst_RankPolicy</c>.
/// DDL: 20221026.CreateDatabase.sql:204-222.
/// PK = (CardType, RankPolicyCode) tại 20221026.CreateDatabase.sql:226-228.
/// FK CardType -> Mst_CardType (20221026.CreateDatabase.sql:273) — chỉ ghi comment.
/// Đúng 17 cột theo DDL nguồn.
/// </summary>
public class Mst_RankPolicy
{
    public string CardType { get; set; } = "";             // Mã hạng thẻ — Varchar50 NOT NULL (PK component 1)
    public string RankPolicyCode { get; set; } = "";       // Mã chính sách xếp hạng — Varchar50 NOT NULL (PK component 2)
    public string NetworkID { get; set; } = "";            // NetworkID — Varchar50 NOT NULL
    public double? Value { get; set; }                     // Giá trị ngưỡng của chính sách — MyValue (chú ý: màn hình Search lấy Value của Mst_CardType)
    public double? PointUpBegin { get; set; }              // Tích lũy điểm nâng hạng từ — MyValue
    public double? PointUpEnd { get; set; }                // Tích lũy điểm nâng hạng đến — MyValue
    public double? QtyVisitUpBegin { get; set; }           // Số lần ghé thăm để nâng hạng từ — MyValue
    public double? QtyVisitUpEnd { get; set; }             // Số lần ghé thăm để nâng hạng đến — MyValue
    public double? PointKeepBegin { get; set; }            // Tích lũy điểm duy trì từ — MyValue
    public double? PointKeepEnd { get; set; }              // Tích lũy điểm duy trì đến — MyValue
    public double? QtyVisitKeepBegin { get; set; }         // Số lần ghé thăm để duy trì từ — MyValue
    public double? QtyVisitKeepEnd { get; set; }           // Số lần ghé thăm để duy trì đến — MyValue
    public double? QtyMonth { get; set; }                  // Thời gian duy trì (tháng) — MyValue
    public string? FlagActive { get; set; }                // Cờ hoạt động '1'/'0' — MyStatus
    public string? Remark { get; set; }                    // Ghi chú — Varchar400
    public DateTime? LogLUDTimeUTC { get; set; }           // Thời điểm cập nhật UTC — MyDateTime
    public string? LogLUBy { get; set; }                   // Người cập nhật — Varchar50
}

/// <summary>
/// Cấu hình EF cho <see cref="Mst_RankPolicy"/> — tên bảng + PK composite (CardType, RankPolicyCode)
/// + 3 cột NOT NULL đúng DDL nguồn 20221026.CreateDatabase.sql:204-228.
/// </summary>
public class Mst_RankPolicyConfig : IEntityTypeConfiguration<Mst_RankPolicy>
{
    public void Configure(EntityTypeBuilder<Mst_RankPolicy> b)
    {
        b.ToTable("Mst_RankPolicy");
        // PK (CardType, RankPolicyCode) — 20221026.CreateDatabase.sql:226-228
        b.HasKey(x => new { x.CardType, x.RankPolicyCode });
        b.Property(x => x.CardType).IsRequired();
        b.Property(x => x.RankPolicyCode).IsRequired();
        b.Property(x => x.NetworkID).IsRequired();
        // FK CardType -> Mst_CardType (20221026.CreateDatabase.sql:273) — chỉ ghi comment, không cấu hình FK navigation EF.
    }
}

/// <summary>
/// Dữ liệu mẫu cho bảng Mst_RankPolicy. Idempotent: đã có dòng nào thì bỏ qua.
/// Thứ tự Order => 25 (danh mục Mst_* dùng 10–99, chạy sau Mst_CardTypeSeeder có Order=11).
/// Dữ liệu demo gồm 6 dòng:
/// - RP2026: N (15:00), I (14:00), S (13:00), G (12:00), P (11:00) — FlagActive '1'.
/// - RP2025: S (10:00) — FlagActive '0'.
/// </summary>
public class Mst_RankPolicySeeder : IPortSeeder
{
    public int Order => 25; // danh mục Mst_* dùng 10–99

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<Mst_RankPolicy>().AnyAsync()) return;

        db.Set<Mst_RankPolicy>().AddRange(
            // RP2026: N — FlagActive '1' (LogLUDTimeUTC cố định để thứ tự sắp xếp xác định)
            new Mst_RankPolicy
            {
                CardType = "N",
                RankPolicyCode = "RP2026",
                NetworkID = "DEMO",
                Value = 999, // Value chính sách = 999 để chứng minh màn hình Search lấy Value từ Mst_CardType (5)
                PointUpBegin = 500, // Value hạng (5) * 100
                PointUpEnd = 599,   // +99
                QtyVisitUpBegin = 0,
                QtyVisitUpEnd = 0,
                PointKeepBegin = 0,
                PointKeepEnd = 0,
                QtyVisitKeepBegin = 0,
                QtyVisitKeepEnd = 0,
                QtyMonth = 12,
                FlagActive = "1",
                Remark = "Dữ liệu demo RP2026 hạng N",
                LogLUDTimeUTC = new DateTime(2026, 10, 10, 15, 0, 0, DateTimeKind.Utc),
                LogLUBy = "seed"
            },
            // RP2026: I — FlagActive '1'
            new Mst_RankPolicy
            {
                CardType = "I",
                RankPolicyCode = "RP2026",
                NetworkID = "DEMO",
                Value = 999,
                PointUpBegin = 1000, // 10 * 100
                PointUpEnd = 1099,
                QtyVisitUpBegin = 0,
                QtyVisitUpEnd = 0,
                PointKeepBegin = 0,
                PointKeepEnd = 0,
                QtyVisitKeepBegin = 0,
                QtyVisitKeepEnd = 0,
                QtyMonth = 12,
                FlagActive = "1",
                Remark = "Dữ liệu demo RP2026 hạng I",
                LogLUDTimeUTC = new DateTime(2026, 10, 10, 14, 0, 0, DateTimeKind.Utc),
                LogLUBy = "seed"
            },
            // RP2026: S — FlagActive '1'
            new Mst_RankPolicy
            {
                CardType = "S",
                RankPolicyCode = "RP2026",
                NetworkID = "DEMO",
                Value = 999,
                PointUpBegin = 2000, // 20 * 100
                PointUpEnd = 2099,
                QtyVisitUpBegin = 0,
                QtyVisitUpEnd = 0,
                PointKeepBegin = 0,
                PointKeepEnd = 0,
                QtyVisitKeepBegin = 0,
                QtyVisitKeepEnd = 0,
                QtyMonth = 12,
                FlagActive = "1",
                Remark = "Dữ liệu demo RP2026 hạng S",
                LogLUDTimeUTC = new DateTime(2026, 10, 10, 13, 0, 0, DateTimeKind.Utc),
                LogLUBy = "seed"
            },
            // RP2026: G — FlagActive '1'
            new Mst_RankPolicy
            {
                CardType = "G",
                RankPolicyCode = "RP2026",
                NetworkID = "DEMO",
                Value = 999,
                PointUpBegin = 3000, // 30 * 100
                PointUpEnd = 3099,
                QtyVisitUpBegin = 0,
                QtyVisitUpEnd = 0,
                PointKeepBegin = 0,
                PointKeepEnd = 0,
                QtyVisitKeepBegin = 0,
                QtyVisitKeepEnd = 0,
                QtyMonth = 12,
                FlagActive = "1",
                Remark = "Dữ liệu demo RP2026 hạng G",
                LogLUDTimeUTC = new DateTime(2026, 10, 10, 12, 0, 0, DateTimeKind.Utc),
                LogLUBy = "seed"
            },
            // RP2026: P — FlagActive '1' (hạng P trong Mst_CardType là inactive '0')
            new Mst_RankPolicy
            {
                CardType = "P",
                RankPolicyCode = "RP2026",
                NetworkID = "DEMO",
                Value = 999,
                PointUpBegin = 4000, // 40 * 100
                PointUpEnd = 4099,
                QtyVisitUpBegin = 0,
                QtyVisitUpEnd = 0,
                PointKeepBegin = 0,
                PointKeepEnd = 0,
                QtyVisitKeepBegin = 0,
                QtyVisitKeepEnd = 0,
                QtyMonth = 12,
                FlagActive = "1",
                Remark = "Dữ liệu demo RP2026 hạng P",
                LogLUDTimeUTC = new DateTime(2026, 10, 10, 11, 0, 0, DateTimeKind.Utc),
                LogLUBy = "seed"
            },
            // RP2025: S — FlagActive '0'
            new Mst_RankPolicy
            {
                CardType = "S",
                RankPolicyCode = "RP2025",
                NetworkID = "DEMO",
                Value = 999,
                PointUpBegin = 2000, // 20 * 100
                PointUpEnd = 2099,
                QtyVisitUpBegin = 0,
                QtyVisitUpEnd = 0,
                PointKeepBegin = 0,
                PointKeepEnd = 0,
                QtyVisitKeepBegin = 0,
                QtyVisitKeepEnd = 0,
                QtyMonth = 12,
                FlagActive = "0",
                Remark = "Dữ liệu demo RP2025 hạng S (inactive)",
                LogLUDTimeUTC = new DateTime(2026, 10, 10, 10, 0, 0, DateTimeKind.Utc),
                LogLUBy = "seed"
            }
        );
        await db.SaveChangesAsync();
    }
}

/// <summary>
/// Lịch sử chính sách xếp hạng — port 1:1 bảng <c>Mst_RankPolicyHist</c>.
/// DDL: 20221026.z11.UpdDB.02.MasterData.Loyalty.sql:21-46.
/// PK = AutoId (bigint IDENTITY(1,1) PK tự tăng).
/// Gồm 17 cột như Mst_RankPolicy + AutoId (câu insert Master.Loyalty.cs:7028 có NetworkID).
/// Được ghi bởi Mst_RankPolicy_Perform (Master.Loyalty.cs:6997-7094).
/// </summary>
public class Mst_RankPolicyHist
{
    public long AutoId { get; set; }                       // AutoId — bigint IDENTITY(1,1) PK
    public string CardType { get; set; } = "";             // Mã hạng thẻ — Varchar50 NOT NULL
    public string RankPolicyCode { get; set; } = "";       // Mã chính sách xếp hạng — Varchar50 NOT NULL
    public string NetworkID { get; set; } = "";            // NetworkID — Varchar50 NOT NULL
    public double? Value { get; set; }                     // Giá trị ngưỡng của chính sách — MyValue
    public double? PointUpBegin { get; set; }              // Tích lũy điểm nâng hạng từ — MyValue
    public double? PointUpEnd { get; set; }                // Tích lũy điểm nâng hạng đến — MyValue
    public double? QtyVisitUpBegin { get; set; }           // Số lần ghé thăm để nâng hạng từ — MyValue
    public double? QtyVisitUpEnd { get; set; }             // Số lần ghé thăm để nâng hạng đến — MyValue
    public double? PointKeepBegin { get; set; }            // Tích lũy điểm duy trì từ — MyValue
    public double? PointKeepEnd { get; set; }              // Tích lũy điểm duy trì đến — MyValue
    public double? QtyVisitKeepBegin { get; set; }         // Số lần ghé thăm để duy trì từ — MyValue
    public double? QtyVisitKeepEnd { get; set; }           // Số lần ghé thăm để duy trì đến — MyValue
    public double? QtyMonth { get; set; }                  // Thời gian duy trì (tháng) — MyValue
    public string? FlagActive { get; set; }                // Cờ hoạt động '1'/'0' — MyStatus
    public string? Remark { get; set; }                    // Ghi chú — Varchar400
    public DateTime? LogLUDTimeUTC { get; set; }           // Thời điểm cập nhật UTC — MyDateTime
    public string? LogLUBy { get; set; }                   // Người cập nhật — Varchar50
}

/// <summary>
/// Cấu hình EF cho <see cref="Mst_RankPolicyHist"/> — PK AutoId tự tăng.
/// </summary>
public class Mst_RankPolicyHistConfig : IEntityTypeConfiguration<Mst_RankPolicyHist>
{
    public void Configure(EntityTypeBuilder<Mst_RankPolicyHist> b)
    {
        b.ToTable("Mst_RankPolicyHist");
        b.HasKey(x => x.AutoId);
        b.Property(x => x.AutoId).ValueGeneratedOnAdd();
        b.Property(x => x.CardType).IsRequired();
        b.Property(x => x.RankPolicyCode).IsRequired();
        b.Property(x => x.NetworkID).IsRequired();
    }
}

/// <summary>
/// Bảng sinh sequence cho mã chính sách xếp hạng — port 1:1 bảng <c>Seq_RankPolicyCode</c>.
/// DDL: 20221026.CreateDatabase.sql:306-315.
/// PK = AutoId (bigint IDENTITY(1,1) PK tự tăng).
/// </summary>
public class Seq_RankPolicyCode
{
    public long AutoId { get; set; }                       // AutoId — bigint IDENTITY(1,1) PK
    public string? Remark { get; set; }                    // Remark — Varchar50
}

/// <summary>
/// Cấu hình EF cho <see cref="Seq_RankPolicyCode"/> — PK AutoId tự tăng.
/// </summary>
public class Seq_RankPolicyCodeConfig : IEntityTypeConfiguration<Seq_RankPolicyCode>
{
    public void Configure(EntityTypeBuilder<Seq_RankPolicyCode> b)
    {
        b.ToTable("Seq_RankPolicyCode");
        b.HasKey(x => x.AutoId);
        b.Property(x => x.AutoId).ValueGeneratedOnAdd();
    }
}
