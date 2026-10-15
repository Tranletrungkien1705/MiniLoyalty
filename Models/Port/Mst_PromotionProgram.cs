using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;

namespace MiniLoyalty.Models.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — danh mục chương trình khuyến mại Mst_PromotionProgram (#205).
//
// Nguồn tra cứu (chỉ đọc):
//   - Model cột: 12.Dev.Common/idn.Loyalty.Common/Models/Loyalty/Mst_PromotionProgram.cs:9-30
//                (gồm 11 cột bảng + MyIdxSeq, LUDTime, LUBy, ListDealer là thuộc tính tính toán/hiển thị)
//   - DDL thật: 11.BackEnd/V10/05.Refs.Biz/Migrate/20221006.QUYNHNTN/20221026.CreateDatabase.sql
//               * Mst_PromotionProgram: :490-509 (đúng 11 cột theo DDL), PK PrProgramCode tại :506-509.
//               * Mst_PromotionProgramDtl: :466-484 (đúng 10 cột theo DDL), PK composite (PrProgramCode, CardType) tại :481-484.
//                 FK PrProgramCode -> Mst_PromotionProgram (:540-547) — chỉ ghi comment.
//               * Mst_PromotionProgramSpec: :444-460 (đúng 8 cột theo DDL), PK composite (DLCode, PrProgramCode) tại :457-460.
//                 FK PrProgramCode -> Mst_PromotionProgram (:512-519) — chỉ ghi comment.
//                 FK DLCode -> Mst_DealerCP.DLCPCode (:522-528) — chỉ ghi comment.
//   - SQL select: idn.Loyalty.Biz/Master.Loyalty.cs:14280-14543 (Mst_PromotionProgram_GetX — bản LIVE)
//
// QUY ƯỚC KIỂU:
//   Varchar50/200/400 -> string? | MyStatus -> string? | MyDateTime -> DateTime? (hoặc DateTime nếu NOT NULL) | MyValue -> double?
// ===========================================================================================

/// <summary>
/// Bảng header chương trình khuyến mại — port 1:1 bảng <c>Mst_PromotionProgram</c>.
/// DDL: 20221026.CreateDatabase.sql:490-509. Đúng 11 cột theo DDL.
/// PK = PrProgramCode tại 20221026.CreateDatabase.sql:506-509.
/// </summary>
public class Mst_PromotionProgram
{
    public string PrProgramCode { get; set; } = "";             // Mã chương trình khuyến mãi — Varchar50 NOT NULL (PK)
    public string NetworkID { get; set; } = "";                  // NetworkID — Varchar50 NOT NULL
    public string? OrgID { get; set; }                          // Mã tổ chức — Varchar50
    public string PrProgramName { get; set; } = "";             // Tên chương trình khuyến mãi — Varchar200 NOT NULL
    public DateTime EffDateStart { get; set; }                  // Ngày bắt đầu hiệu lực — MyDateTime NOT NULL
    public DateTime EffDateEnd { get; set; }                    // Ngày kết thúc hiệu lực — MyDateTime NOT NULL
    public string? FlagAllDL { get; set; }                      // Cờ áp dụng tất cả đại lý: '1'=Tất cả, '0'=Theo Spec — MyStatus
    public string? FlagActive { get; set; }                     // Cờ hoạt động: '1'=Kích hoạt, '0'=Không kích hoạt — MyStatus
    public string? Remark { get; set; }                         // Ghi chú — Varchar400
    public DateTime? LogLUDTimeUTC { get; set; }                // Thời điểm cập nhật UTC — MyDateTime
    public string? LogLUBy { get; set; }                        // Người cập nhật — Varchar50
}

/// <summary>
/// Cấu hình EF cho <see cref="Mst_PromotionProgram"/> — tên bảng + PK PrProgramCode
/// + các cột NOT NULL đúng DDL nguồn 20221026.CreateDatabase.sql:490-509.
/// </summary>
public class Mst_PromotionProgramConfig : IEntityTypeConfiguration<Mst_PromotionProgram>
{
    public void Configure(EntityTypeBuilder<Mst_PromotionProgram> b)
    {
        b.ToTable("Mst_PromotionProgram");
        // PK PrProgramCode — 20221026.CreateDatabase.sql:506-509
        b.HasKey(x => x.PrProgramCode);
        b.Property(x => x.PrProgramCode).IsRequired();
        b.Property(x => x.NetworkID).IsRequired();
        b.Property(x => x.PrProgramName).IsRequired();
        b.Property(x => x.EffDateStart).IsRequired();
        b.Property(x => x.EffDateEnd).IsRequired();
    }
}

/// <summary>
/// Bảng chi tiết chương trình khuyến mại — port 1:1 bảng <c>Mst_PromotionProgramDtl</c>.
/// DDL: 20221026.CreateDatabase.sql:466-484. Đúng 10 cột theo DDL.
/// PK composite = (PrProgramCode, CardType) tại 20221026.CreateDatabase.sql:481-484.
/// FK PrProgramCode -> Mst_PromotionProgram.PrProgramCode (20221026.CreateDatabase.sql:540-547) — chỉ ghi comment, không navigation EF.
/// </summary>
public class Mst_PromotionProgramDtl
{
    public string PrProgramCode { get; set; } = "";             // Mã chương trình — Varchar50 NOT NULL (PK component 1)
    public string CardType { get; set; } = "";                  // Hạng thẻ — Varchar50 NOT NULL (PK component 2)
    public string? NetworkID { get; set; }                      // NetworkID — Varchar50
    public string? OrgID { get; set; }                          // Mã tổ chức — nvarchar(10) NULL
    public double? Qty { get; set; }                            // Số lượng — MyValue (double?)
    public string? Unit { get; set; }                           // Đơn vị tính — Varchar50
    public string? FlagActive { get; set; }                     // Cờ hoạt động: '1'/'0' — MyStatus
    public string? Remark { get; set; }                         // Ghi chú — Varchar400
    public DateTime? LogLUDTimeUTC { get; set; }                // Thời điểm cập nhật UTC — MyDateTime
    public string? LogLUBy { get; set; }                        // Người cập nhật — Varchar50
}

/// <summary>
/// Cấu hình EF cho <see cref="Mst_PromotionProgramDtl"/> — tên bảng + PK composite (PrProgramCode, CardType)
/// + các cột NOT NULL đúng DDL nguồn 20221026.CreateDatabase.sql:466-484.
/// </summary>
public class Mst_PromotionProgramDtlConfig : IEntityTypeConfiguration<Mst_PromotionProgramDtl>
{
    public void Configure(EntityTypeBuilder<Mst_PromotionProgramDtl> b)
    {
        b.ToTable("Mst_PromotionProgramDtl");
        // PK (PrProgramCode, CardType) — 20221026.CreateDatabase.sql:481-484
        b.HasKey(x => new { x.PrProgramCode, x.CardType });
        b.Property(x => x.PrProgramCode).IsRequired();
        b.Property(x => x.CardType).IsRequired();
        // FK PrProgramCode -> Mst_PromotionProgram.PrProgramCode (20221026.CreateDatabase.sql:540-547) — chỉ ghi comment.
    }
}

/// <summary>
/// Bảng chỉ định đại lý cho chương trình khuyến mại — port 1:1 bảng <c>Mst_PromotionProgramSpec</c>.
/// DDL: 20221026.CreateDatabase.sql:444-460. Đúng 8 cột theo DDL.
/// PK composite = (DLCode, PrProgramCode) tại 20221026.CreateDatabase.sql:457-460.
/// FK PrProgramCode -> Mst_PromotionProgram.PrProgramCode (20221026.CreateDatabase.sql:512-519) — chỉ ghi comment.
/// FK DLCode -> Mst_DealerCP.DLCPCode (20221026.CreateDatabase.sql:522-528) — chỉ ghi comment.
/// </summary>
public class Mst_PromotionProgramSpec
{
    public string DLCode { get; set; } = "";                    // Mã đại lý — Varchar50 NOT NULL (PK component 1)
    public string PrProgramCode { get; set; } = "";             // Mã chương trình — Varchar50 NOT NULL (PK component 2)
    public string? NetworkID { get; set; }                      // NetworkID — Varchar50
    public string? OrgID { get; set; }                          // Mã tổ chức — Varchar50
    public string? FlagActive { get; set; }                     // Cờ hoạt động: '1'/'0' — MyStatus
    public string? Remark { get; set; }                         // Ghi chú — Varchar400
    public DateTime? LogLUDTimeUTC { get; set; }                // Thời điểm cập nhật UTC — MyDateTime
    public string? LogLUBy { get; set; }                        // Người cập nhật — Varchar50
}

/// <summary>
/// Cấu hình EF cho <see cref="Mst_PromotionProgramSpec"/> — tên bảng + PK composite (DLCode, PrProgramCode)
/// + các cột NOT NULL đúng DDL nguồn 20221026.CreateDatabase.sql:444-460.
/// </summary>
public class Mst_PromotionProgramSpecConfig : IEntityTypeConfiguration<Mst_PromotionProgramSpec>
{
    public void Configure(EntityTypeBuilder<Mst_PromotionProgramSpec> b)
    {
        b.ToTable("Mst_PromotionProgramSpec");
        // PK (DLCode, PrProgramCode) — 20221026.CreateDatabase.sql:457-460
        b.HasKey(x => new { x.DLCode, x.PrProgramCode });
        b.Property(x => x.DLCode).IsRequired();
        b.Property(x => x.PrProgramCode).IsRequired();
        // FK PrProgramCode -> Mst_PromotionProgram.PrProgramCode (20221026.CreateDatabase.sql:512-519) — chỉ ghi comment.
        // FK DLCode -> Mst_DealerCP.DLCPCode (20221026.CreateDatabase.sql:522-528) — chỉ ghi comment.
    }
}

/// <summary>
/// Dữ liệu mẫu khởi tạo cho 3 bảng Mst_PromotionProgram, Mst_PromotionProgramSpec, Mst_PromotionProgramDtl.
/// Idempotent (kiểm tra trước khi chèn), Order = 30.
/// </summary>
public class Mst_PromotionProgramSeeder : IPortSeeder
{
    public int Order => 30;

    public async Task SeedAsync(AppDbContext db)
    {
        // -----------------------------------------------------------------------------------------
        // Header: 3 bản ghi (PR01, PR02, PR03)
        // PR01: 2026-10-01..2026-10-31, FlagAllDL '1', FlagActive '1', LogLUDTimeUTC 2026-10-10 10:00:00 UTC
        // PR02: 2026-01-01..2026-12-31, FlagAllDL '0', FlagActive '1', LogLUDTimeUTC 2026-10-10 09:00:00 UTC
        // PR03: 2025-01-01..2025-12-31, FlagAllDL '0', FlagActive '0', LogLUDTimeUTC 2026-10-10 11:00:00 UTC
        // -----------------------------------------------------------------------------------------
        if (!await db.Set<Mst_PromotionProgram>().AnyAsync())
        {
            db.Set<Mst_PromotionProgram>().AddRange(
                new Mst_PromotionProgram
                {
                    PrProgramCode = "PR01",
                    PrProgramName = "Khuyến mãi tháng 10",
                    NetworkID = "DEMO",
                    OrgID = null,
                    EffDateStart = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
                    EffDateEnd = new DateTime(2026, 10, 31, 23, 59, 59, DateTimeKind.Utc),
                    FlagAllDL = "1",
                    FlagActive = "1",
                    Remark = "Khuyến mãi tháng 10",
                    LogLUBy = "seed",
                    LogLUDTimeUTC = new DateTime(2026, 10, 10, 10, 0, 0, DateTimeKind.Utc),
                },
                new Mst_PromotionProgram
                {
                    PrProgramCode = "PR02",
                    PrProgramName = "Ưu đãi đại lý",
                    NetworkID = "DEMO",
                    OrgID = null,
                    EffDateStart = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    EffDateEnd = new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc),
                    FlagAllDL = "0",
                    FlagActive = "1",
                    Remark = "Ưu đãi đại lý",
                    LogLUBy = "seed",
                    LogLUDTimeUTC = new DateTime(2026, 10, 10, 9, 0, 0, DateTimeKind.Utc),
                },
                new Mst_PromotionProgram
                {
                    PrProgramCode = "PR03",
                    PrProgramName = "Chương trình cũ",
                    NetworkID = "DEMO",
                    OrgID = null,
                    EffDateStart = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    EffDateEnd = new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc),
                    FlagAllDL = "0",
                    FlagActive = "0",
                    Remark = "Chương trình cũ",
                    LogLUBy = "seed",
                    LogLUDTimeUTC = new DateTime(2026, 10, 10, 11, 0, 0, DateTimeKind.Utc),
                }
            );
        }

        // -----------------------------------------------------------------------------------------
        // Spec: 2 bản ghi (DL03, PR02) · (DL01, PR02)
        // -----------------------------------------------------------------------------------------
        if (!await db.Set<Mst_PromotionProgramSpec>().AnyAsync())
        {
            db.Set<Mst_PromotionProgramSpec>().AddRange(
                new Mst_PromotionProgramSpec
                {
                    DLCode = "DL03",
                    PrProgramCode = "PR02",
                    NetworkID = "DEMO",
                    OrgID = null,
                    FlagActive = "1",
                    Remark = "Spec DL03 cho PR02",
                    LogLUBy = "seed",
                    LogLUDTimeUTC = new DateTime(2026, 10, 10, 9, 0, 0, DateTimeKind.Utc),
                },
                new Mst_PromotionProgramSpec
                {
                    DLCode = "DL01",
                    PrProgramCode = "PR02",
                    NetworkID = "DEMO",
                    OrgID = null,
                    FlagActive = "1",
                    Remark = "Spec DL01 cho PR02",
                    LogLUBy = "seed",
                    LogLUDTimeUTC = new DateTime(2026, 10, 10, 9, 0, 0, DateTimeKind.Utc),
                }
            );
        }

        // -----------------------------------------------------------------------------------------
        // Dtl: 2 bản ghi (PR01, N, Qty 2, Unit 'Lần') · (PR02, S, Qty 1, Unit 'Voucher')
        // -----------------------------------------------------------------------------------------
        if (!await db.Set<Mst_PromotionProgramDtl>().AnyAsync())
        {
            db.Set<Mst_PromotionProgramDtl>().AddRange(
                new Mst_PromotionProgramDtl
                {
                    PrProgramCode = "PR01",
                    CardType = "N",
                    NetworkID = "DEMO",
                    OrgID = null,
                    Qty = 2.0,
                    Unit = "Lần",
                    FlagActive = "1",
                    Remark = "Dtl PR01 hạng N",
                    LogLUBy = "seed",
                    LogLUDTimeUTC = new DateTime(2026, 10, 10, 10, 0, 0, DateTimeKind.Utc),
                },
                new Mst_PromotionProgramDtl
                {
                    PrProgramCode = "PR02",
                    CardType = "S",
                    NetworkID = "DEMO",
                    OrgID = null,
                    Qty = 1.0,
                    Unit = "Voucher",
                    FlagActive = "1",
                    Remark = "Dtl PR02 hạng S",
                    LogLUBy = "seed",
                    LogLUDTimeUTC = new DateTime(2026, 10, 10, 9, 0, 0, DateTimeKind.Utc),
                }
            );
        }

        await db.SaveChangesAsync();
    }
}
