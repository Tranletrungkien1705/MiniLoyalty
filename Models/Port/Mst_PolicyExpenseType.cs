using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;

namespace MiniLoyalty.Models.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — bảng chính sách chi phí Mst_PolicyExpenseType (#190).
//
// Nguồn tra cứu (chỉ đọc):
//   - Model cột:  12.Dev.Common/idn.Loyalty.Common/Models/Loyalty/Mst_PolicyExpenseType.cs:9-31 (17 thuộc tính: 16 cột DB + ExpenseTypeName)
//   - DDL:        11.BackEnd/.../Migrate/20221006.QUYNHNTN/20221026.CreateDatabase.sql:156-170 (13 cột ban đầu)
//                 PK composite (PolicyExpenseTypeNo, ExpenseType) tại :174-177.
//                 FK ExpenseType -> Mst_ExpenseType(ExpenseType) tại :264-270 (chỉ ghi comment).
//   - Alter DDL:  20260723.z16.PolicyExpenseType_Discount.sql:20,29 & 20260724.z11.Table.AddColumns.sql:3-7 (thêm FlagDiscount, DiscountRate)
//   - Biz code:   Master.Loyalty.cs:8371, 8414 (thêm FlagPointRank)
//   - SQL select: Master.Loyalty.cs:8499-8929 (WAS_Mst_PolicyExpenseType_Get, Mst_PolicyExpenseType_Get, Mst_PolicyExpenseType_GetX — bản LIVE)
//
// ĐẶC ĐIỂM BẢNG (đúng 16 cột DB):
//   PolicyExpenseTypeNo (varchar50 NOT NULL), ExpenseType (varchar50 NOT NULL), NetworkID (varchar50 NOT NULL),
//   ExpenseTypeNameActual (varchar200 NOT NULL), FlagPoint (MyStatus), FlagPointRank (MyStatus),
//   AmountRate (MyValue -> double?), MaxRankReviewPoint (MyValue -> double?), MaxAccumulationPoint (MyValue -> double?),
//   FlagCountService (MyStatus), FlagDiscount (MyStatus), DiscountRate (MyValue -> double?),
//   FlagActive (MyStatus), Remark (varchar400), LogLUDTimeUTC (MyDateTime -> DateTime?), LogLUBy (varchar50).
//   Khoá chính: composite PK (PolicyExpenseTypeNo, ExpenseType).
//   Cột ExpenseTypeName và MyIdxSeq không nằm trong bảng DB Mst_PolicyExpenseType (ExpenseTypeName lấy từ Mst_ExpenseType qua left join).
// ===========================================================================================

/// <summary>
/// Chính sách loại chi phí — port 1:1 bảng <c>Mst_PolicyExpenseType</c>.
/// Model nguồn: Mst_PolicyExpenseType.cs (17 thuộc tính). DDL: 20221026.CreateDatabase.sql:156-177 + alter scripts.
/// Đúng 16 cột DB trong bảng: PolicyExpenseTypeNo, ExpenseType, NetworkID, ExpenseTypeNameActual,
/// FlagPoint, FlagPointRank, AmountRate, MaxRankReviewPoint, MaxAccumulationPoint, FlagCountService,
/// FlagDiscount, DiscountRate, FlagActive, Remark, LogLUDTimeUTC, LogLUBy.
/// </summary>
public class Mst_PolicyExpenseType
{
    public string PolicyExpenseTypeNo { get; set; } = "";          // Mã chính sách chi phí (Varchar50 NOT NULL) — PK component 1 (:157, :174)
    public string ExpenseType { get; set; } = "";                  // Mã loại chi phí (Varchar50 NOT NULL) — PK component 2 (:158, :174)
    public string NetworkID { get; set; } = "";                    // Mã mạng (Varchar50 NOT NULL) — :159
    public string ExpenseTypeNameActual { get; set; } = "";        // Tên loại đối tượng thực tế (Varchar200 NOT NULL) — :160
    public string? FlagPoint { get; set; }                         // Đối tượng áp dụng (MyStatus) — :161
    public string? FlagPointRank { get; set; }                     // Cờ tính điểm dịch vụ (MyStatus) — Master.Loyalty.cs:8371, 8414
    public double? AmountRate { get; set; }                        // Tỉ lệ tích điểm (MyValue -> double?) — :162
    public double? MaxRankReviewPoint { get; set; }                // Điểm xét hạng tối đa (MyValue -> double?) — :163
    public double? MaxAccumulationPoint { get; set; }              // Điểm tích lũy tối đa (MyValue -> double?) — :164
    public string? FlagCountService { get; set; }                  // Cờ tính điểm dịch vụ (MyStatus) — :165
    public string? FlagDiscount { get; set; }                      // Có chiết khấu dịch vụ (MyStatus) — 20260723.z16...sql:20
    public double? DiscountRate { get; set; }                      // Tỉ lệ chiết khấu dịch vụ (0..100) (MyValue -> double?) — 20260723.z16...sql:29
    public string? FlagActive { get; set; }                        // Cờ hoạt động '1'/'0' (MyStatus) — :166
    public string? Remark { get; set; }                            // Ghi chú (Varchar400) — :167
    public DateTime? LogLUDTimeUTC { get; set; }                   // Thời điểm cập nhật UTC (MyDateTime) — :168
    public string? LogLUBy { get; set; }                           // Người cập nhật (Varchar50) — :169

    // Thuộc tính không ánh xạ vào bảng DB (lấy qua join hoặc sinh động khi truy vấn)
    [NotMapped]
    public string? ExpenseTypeName { get; set; }                   // Tên loại đối tượng lấy từ Mst_ExpenseType qua left join (Mst_PolicyExpenseType.cs:16, Master.Loyalty.cs:8859)

    [NotMapped]
    public long? MyIdxSeq { get; set; }                            // Thứ tự 0-based từ draft (#tbl_Mst_PolicyExpenseType_Filter_Draft, :8815)
}

/// <summary>
/// Cấu hình EF cho <see cref="Mst_PolicyExpenseType"/> — bảng DB Mst_PolicyExpenseType + khoá gộp (PolicyExpenseTypeNo, ExpenseType).
/// Bỏ qua ExpenseTypeName và MyIdxSeq để bảng DB có ĐÚNG 16 cột.
/// </summary>
public class Mst_PolicyExpenseTypeConfig : IEntityTypeConfiguration<Mst_PolicyExpenseType>
{
    public void Configure(EntityTypeBuilder<Mst_PolicyExpenseType> b)
    {
        b.ToTable("Mst_PolicyExpenseType");
        b.HasKey(x => new { x.PolicyExpenseTypeNo, x.ExpenseType }); // PK composite: 20221026.CreateDatabase.sql:174-177
        b.Property(x => x.PolicyExpenseTypeNo).IsRequired().HasMaxLength(50);
        b.Property(x => x.ExpenseType).IsRequired().HasMaxLength(50);
        b.Property(x => x.NetworkID).IsRequired().HasMaxLength(50);
        b.Property(x => x.ExpenseTypeNameActual).IsRequired().HasMaxLength(200);
        b.Property(x => x.FlagPoint).HasMaxLength(10);
        b.Property(x => x.FlagPointRank).HasMaxLength(10);
        b.Property(x => x.FlagCountService).HasMaxLength(10);
        b.Property(x => x.FlagDiscount).HasMaxLength(10);
        b.Property(x => x.FlagActive).HasMaxLength(10);
        b.Property(x => x.Remark).HasMaxLength(400);
        b.Property(x => x.LogLUBy).HasMaxLength(50);

        // Bỏ qua các thuộc tính không có cột tương ứng trong DB
        b.Ignore(x => x.ExpenseTypeName);
        b.Ignore(x => x.MyIdxSeq);
    }
}

/// <summary>
/// Dữ liệu mẫu cho bảng Mst_PolicyExpenseType theo mục P2 đề bài #190.
/// Idempotent: đã có dữ liệu thì return.
/// Thứ tự Order => 44 (chạy sau Mst_ExpenseTypeSeeder có Order=35, danh mục Mst_* dùng 10–99).
/// </summary>
public class Mst_PolicyExpenseTypeSeeder : IPortSeeder
{
    public int Order => 23; // Chạy sau Mst_ExpenseTypeSeeder (Order 35)

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<Mst_PolicyExpenseType>().AnyAsync()) return;

        db.Set<Mst_PolicyExpenseType>().AddRange(
            // Row 1: PET01, EX01, FlagActive '1', LogLUDTimeUTC 2026-10-10 15:00:00 (Join EX01 -> "Chi phi dich vu")
            new Mst_PolicyExpenseType
            {
                PolicyExpenseTypeNo = "PET01",
                ExpenseType = "EX01",
                NetworkID = "DEMO",
                ExpenseTypeNameActual = "Thuc te DV 01",
                FlagPoint = "1",
                FlagPointRank = "1",
                AmountRate = 0.05,
                MaxRankReviewPoint = 1000,
                MaxAccumulationPoint = 5000,
                FlagCountService = "1",
                FlagDiscount = "1",
                DiscountRate = 10,
                FlagActive = "1",
                Remark = "Seed demo",
                LogLUDTimeUTC = new DateTime(2026, 10, 10, 15, 0, 0, DateTimeKind.Utc),
                LogLUBy = "seed"
            },
            // Row 2: PET02, EX02, FlagActive '1', LogLUDTimeUTC 2026-10-10 14:00:00 (Join EX02 -> "Chi phi phu tung")
            new Mst_PolicyExpenseType
            {
                PolicyExpenseTypeNo = "PET02",
                ExpenseType = "EX02",
                NetworkID = "DEMO",
                ExpenseTypeNameActual = "Thuc te PT 02",
                FlagPoint = "1",
                FlagPointRank = "1",
                AmountRate = 0.05,
                MaxRankReviewPoint = 1000,
                MaxAccumulationPoint = 5000,
                FlagCountService = "1",
                FlagDiscount = "1",
                DiscountRate = 10,
                FlagActive = "1",
                Remark = "Seed demo",
                LogLUDTimeUTC = new DateTime(2026, 10, 10, 14, 0, 0, DateTimeKind.Utc),
                LogLUBy = "seed"
            },
            // Row 3: PET03, EX01, FlagActive '1', LogLUDTimeUTC 2026-10-10 13:00:00 (Join EX01 -> "Chi phi dich vu")
            new Mst_PolicyExpenseType
            {
                PolicyExpenseTypeNo = "PET03",
                ExpenseType = "EX01",
                NetworkID = "DEMO",
                ExpenseTypeNameActual = "Thuc te DV 03",
                FlagPoint = "1",
                FlagPointRank = "1",
                AmountRate = 0.05,
                MaxRankReviewPoint = 1000,
                MaxAccumulationPoint = 5000,
                FlagCountService = "1",
                FlagDiscount = "1",
                DiscountRate = 10,
                FlagActive = "1",
                Remark = "Seed demo",
                LogLUDTimeUTC = new DateTime(2026, 10, 10, 13, 0, 0, DateTimeKind.Utc),
                LogLUBy = "seed"
            },
            // Row 4: PET04, EX03, FlagActive '1', LogLUDTimeUTC 2026-10-10 12:00:00 (Join EX03 -> "Chi phi cu (ngung)")
            new Mst_PolicyExpenseType
            {
                PolicyExpenseTypeNo = "PET04",
                ExpenseType = "EX03",
                NetworkID = "DEMO",
                ExpenseTypeNameActual = "Thuc te Cu 04",
                FlagPoint = "1",
                FlagPointRank = "1",
                AmountRate = 0.05,
                MaxRankReviewPoint = 1000,
                MaxAccumulationPoint = 5000,
                FlagCountService = "1",
                FlagDiscount = "1",
                DiscountRate = 10,
                FlagActive = "1",
                Remark = "Seed demo",
                LogLUDTimeUTC = new DateTime(2026, 10, 10, 12, 0, 0, DateTimeKind.Utc),
                LogLUBy = "seed"
            },
            // Row 5: PET05, EX99, FlagActive '1', LogLUDTimeUTC 2026-10-10 11:00:00 (EX99 không có trong Mst_ExpenseType -> left join ExpenseTypeName = null)
            new Mst_PolicyExpenseType
            {
                PolicyExpenseTypeNo = "PET05",
                ExpenseType = "EX99",
                NetworkID = "DEMO",
                ExpenseTypeNameActual = "Thuc te Khac 05",
                FlagPoint = "1",
                FlagPointRank = "1",
                AmountRate = 0.05,
                MaxRankReviewPoint = 1000,
                MaxAccumulationPoint = 5000,
                FlagCountService = "1",
                FlagDiscount = "1",
                DiscountRate = 10,
                FlagActive = "1",
                Remark = "Seed demo",
                LogLUDTimeUTC = new DateTime(2026, 10, 10, 11, 0, 0, DateTimeKind.Utc),
                LogLUBy = "seed"
            },
            // Row 6: PET06, EX01, FlagActive '0', LogLUDTimeUTC 2026-10-10 10:00:00 (Join EX01 -> "Chi phi dich vu")
            new Mst_PolicyExpenseType
            {
                PolicyExpenseTypeNo = "PET06",
                ExpenseType = "EX01",
                NetworkID = "DEMO",
                ExpenseTypeNameActual = "Thuc te Inactive 06",
                FlagPoint = "1",
                FlagPointRank = "1",
                AmountRate = 0.05,
                MaxRankReviewPoint = 1000,
                MaxAccumulationPoint = 5000,
                FlagCountService = "1",
                FlagDiscount = "1",
                DiscountRate = 10,
                FlagActive = "0",
                Remark = "Seed demo",
                LogLUDTimeUTC = new DateTime(2026, 10, 10, 10, 0, 0, DateTimeKind.Utc),
                LogLUBy = "seed"
            }
        );
        await db.SaveChangesAsync();
    }
}
