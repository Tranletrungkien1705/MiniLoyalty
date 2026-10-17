using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;
using MiniLoyalty.Models;

namespace MiniLoyalty.Models.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — Rpt_PeriodSummary_TransactionExceptionally (#288).
//
// Nguồn tra cứu (chỉ đọc):
//   - Model cột: 12.Dev.Common/idn.Loyalty.Common/Models/Rpt_PeriodSummary_TransactionExceptionally.cs:9-35 (23 thuộc tính)
//   - DDL thật:  11.BackEnd/V10/05.Refs.Biz/Migrate/20221006.QUYNHNTN/20221707.QUYNHNTN.LOYALTY/Rpt_PeriodSummary.sql:38-65
//                19 cột vật lý trong bảng, PK composite 2 cột: (PeriodMonth, AutoID) — :62-65
//   - Biz LIVE:  11.BackEnd/V10/idn.Loyalty.Biz/Report.cs:5785-5909 (WAS_Rpt_PeriodSummary_Transaction_Get),
//                :6053-6275 (Rpt_PeriodSummary_Transaction_GetX)
//                Inner join với bảng draft #tbl_Rpt_PeriodSummary_Transaction_Filter trên (PeriodMonth, DLCodeExceptionally, AutoID),
//                left join Crd_CardTransaction cct trên t.AutoID = cct.AutoID để lấy CreateDTimeUTC và CardNo.
// ===========================================================================================

/// <summary>
/// Bảng giao dịch đặc cách đối soát tổng hợp kỳ — port 1:1 Rpt_PeriodSummary_TransactionExceptionally (Rpt_PeriodSummary.sql:38-65).
/// PK composite 2 cột: (PeriodMonth, AutoID).
/// Model gồm 24 thuộc tính (19 cột vật lý DDL + 3 cột điểm tích model + 2 cột từ join Crd_CardTransaction).
/// </summary>
public class Rpt_PeriodSummary_TransactionExceptionally
{
    public string PeriodMonth { get; set; } = "";          // Tháng đối soát (PK phần 1) — DDL :39, :64
    public long AutoID { get; set; }                       // Số giao dịch (PK phần 2) — DDL :40, :64
    public string RefNo { get; set; } = "";                // Mã giao dịch — DDL :41
    public string? NetworkID { get; set; }                 // Mã Network — DDL :42
    public string? OrgID { get; set; }                     // Mã Org — DDL :43
    public string? DealPointType { get; set; }             // Loại điểm — DDL :44
    public string? DLCode { get; set; }                    // Đại lý giao dịch — DDL :45
    public string? RefCode07 { get; set; }                 // Số tham chiếu — DDL :46
    public string? MemberNo { get; set; }                  // Mã hội viên — DDL :47
    public string? MemberName { get; set; }                // Tên hội viên — DDL :48
    public string? CardTypeUse { get; set; }               // Hạng sử dụng — DDL :49
    public string? CardTypeInit { get; set; }              // Hạng thực — DDL :50
    public string? DLCodeExceptionally { get; set; }       // ĐL YC đặc cách — DDL :51
    public double? AmountChTotal { get; set; }             // Số điểm tích hạng sử dụng — DDL :52
    public double? AmountChTotalInit { get; set; }         // Số điểm tích hạng thực — DDL :53
    public double? AmountChTotalDiffer { get; set; }       // Số điểm chênh hạng — DDL :54
    public DateTime? PointExpiryDTime { get; set; }        // Hạn sử dụng điểm — DDL :55
    public DateTime? LogLUDTimeUTC { get; set; }           // Thời gian cập nhật cuối UTC — DDL :56
    public string? LogLUBy { get; set; }                   // Người cập nhật cuối — DDL :57

    // 3 cột điểm tích từ model C# nguồn (không có trong DDL vật lý 19 cột) — Rpt_PeriodSummary_TransactionExceptionally.cs:24-26
    public double? PointChTotal { get; set; }              // Số điểm tích hạng sử dụng
    public double? PointChTotalInit { get; set; }          // Số điểm tích hạng thực
    public double? PointChTotalDiffer { get; set; }        // Số điểm chênh hạng

    // 2 cột từ join Crd_CardTransaction — Report.cs:6202-6203, :5874-5879
    public string? CreateDTimeUTC { get; set; }            // Ngày giao dịch (định dạng yyyy-MM-dd giờ VN UTC+7)
    public string? CardNo { get; set; }                    // Mã kỳ xét hạng / số thẻ
}

/// <summary>
/// Cấu hình EF cho <see cref="Rpt_PeriodSummary_TransactionExceptionally"/> — bảng Rpt_PeriodSummary_TransactionExceptionally, PK composite (PeriodMonth, AutoID).
/// </summary>
public class Rpt_PeriodSummary_TransactionExceptionallyConfig : IEntityTypeConfiguration<Rpt_PeriodSummary_TransactionExceptionally>
{
    public void Configure(EntityTypeBuilder<Rpt_PeriodSummary_TransactionExceptionally> b)
    {
        b.ToTable("Rpt_PeriodSummary_TransactionExceptionally");
        b.HasKey(x => new { x.PeriodMonth, x.AutoID });
        b.Property(x => x.PeriodMonth).HasMaxLength(50);
        b.Property(x => x.RefNo).HasMaxLength(50);
        b.Property(x => x.NetworkID).HasMaxLength(50);
        b.Property(x => x.OrgID).HasMaxLength(50);
        b.Property(x => x.DealPointType).HasMaxLength(50);
        b.Property(x => x.DLCode).HasMaxLength(50);
        b.Property(x => x.RefCode07).HasMaxLength(50);
        b.Property(x => x.MemberNo).HasMaxLength(50);
        b.Property(x => x.MemberName).HasMaxLength(200);
        b.Property(x => x.CardTypeUse).HasMaxLength(50);
        b.Property(x => x.CardTypeInit).HasMaxLength(50);
        b.Property(x => x.DLCodeExceptionally).HasMaxLength(50);
        b.Property(x => x.LogLUBy).HasMaxLength(50);

        // Bỏ qua các thuộc tính không có trong bảng vật lý DDL (join và model bổ sung)
        b.Ignore(x => x.PointChTotal);
        b.Ignore(x => x.PointChTotalInit);
        b.Ignore(x => x.PointChTotalDiffer);
        b.Ignore(x => x.CreateDTimeUTC);
        b.Ignore(x => x.CardNo);
    }
}

/// <summary>
/// Seeder dữ liệu mẫu cho Rpt_PeriodSummary_TransactionExceptionally (#288).
/// Order => 84 (chưa dùng trong dải 10-99 theo Bài học A.4, chạy ngay sau Order 83 của Rpt_PeriodSummary_Transaction).
/// DEMO, idempotent.
/// </summary>
public class Rpt_PeriodSummary_TransactionExceptionallySeeder : IPortSeeder
{
    public int Order => 84;

    public async Task SeedAsync(AppDbContext db)
    {
        // 1. Kiểm tra trước: nếu chưa có CrdCardTransaction tương ứng thì seed 5 dòng CrdCardTransaction (hoặc lấy AutoId đã có)
        var tx1 = await db.Set<CrdCardTransaction>().FirstOrDefaultAsync(x => x.RefNo == "TX288_01");
        if (tx1 == null)
        {
            tx1 = new CrdCardTransaction
            {
                RefNo = "TX288_01",
                CardNo = "CD01",
                MemberNo = "MB01",
                CreateDTimeUTC = new DateTime(2026, 3, 10, 10, 0, 0, DateTimeKind.Utc)
            };
            db.Set<CrdCardTransaction>().Add(tx1);
        }

        var tx2 = await db.Set<CrdCardTransaction>().FirstOrDefaultAsync(x => x.RefNo == "TX288_02");
        if (tx2 == null)
        {
            tx2 = new CrdCardTransaction
            {
                RefNo = "TX288_02",
                CardNo = "CD02",
                MemberNo = "MB02",
                CreateDTimeUTC = new DateTime(2026, 3, 15, 14, 0, 0, DateTimeKind.Utc)
            };
            db.Set<CrdCardTransaction>().Add(tx2);
        }

        var tx3 = await db.Set<CrdCardTransaction>().FirstOrDefaultAsync(x => x.RefNo == "TX288_03");
        if (tx3 == null)
        {
            tx3 = new CrdCardTransaction
            {
                RefNo = "TX288_03",
                CardNo = "CD03",
                MemberNo = "MB03",
                CreateDTimeUTC = new DateTime(2026, 4, 1, 8, 0, 0, DateTimeKind.Utc)
            };
            db.Set<CrdCardTransaction>().Add(tx3);
        }

        var tx4 = await db.Set<CrdCardTransaction>().FirstOrDefaultAsync(x => x.RefNo == "TX288_04");
        if (tx4 == null)
        {
            tx4 = new CrdCardTransaction
            {
                RefNo = "TX288_04",
                CardNo = "CD04",
                MemberNo = "MB04",
                CreateDTimeUTC = new DateTime(2026, 3, 20, 9, 0, 0, DateTimeKind.Utc)
            };
            db.Set<CrdCardTransaction>().Add(tx4);
        }

        var tx5 = await db.Set<CrdCardTransaction>().FirstOrDefaultAsync(x => x.RefNo == "TX288_05");
        if (tx5 == null)
        {
            tx5 = new CrdCardTransaction
            {
                RefNo = "TX288_05",
                CardNo = "CD05",
                MemberNo = "MB05",
                CreateDTimeUTC = new DateTime(2026, 3, 22, 11, 0, 0, DateTimeKind.Utc)
            };
            db.Set<CrdCardTransaction>().Add(tx5);
        }

        await db.SaveChangesAsync();

        // 2. Kiểm tra / seed 5 dòng tương ứng vào Rpt_PeriodSummary_Transaction (bảng cha để lọc draft)
        if (!await db.Set<Rpt_PeriodSummary_Transaction>().AnyAsync(x => x.RefNo == "TX288_01"))
        {
            db.Set<Rpt_PeriodSummary_Transaction>().AddRange(
                // Dòng 1: PeriodMonth '2026-03', AutoID AutoId_1, RefNo 'TX288_01', DLCode 'DL01', DLCodeExceptionally 'DL02', DealPointType 'CONSUMPTION', MemberNo 'MB01', MemberName 'Nguyễn Văn A', AmountChTotal 1000.0, LogLUDTimeUTC '2026-03-15 10:00:00Z'.
                new Rpt_PeriodSummary_Transaction
                {
                    PeriodMonth = "2026-03",
                    AutoID = tx1.AutoId,
                    RefNo = "TX288_01",
                    DLCode = "DL01",
                    DLCodeExceptionally = "DL02",
                    DealPointType = "CONSUMPTION",
                    MemberNo = "MB01",
                    MemberName = "Nguyễn Văn A",
                    AmountChTotal = 1000.0,
                    LogLUDTimeUTC = new DateTime(2026, 3, 15, 10, 0, 0, DateTimeKind.Utc),
                    LogLUBy = "seed"
                },
                // Dòng 2: PeriodMonth '2026-03', AutoID AutoId_2, RefNo 'TX288_02', DLCode 'DL03', DLCodeExceptionally 'DL02', DealPointType 'CONSUMPTION', MemberNo 'MB02', MemberName 'Trần Thị B', AmountChTotal 2000.0, LogLUDTimeUTC '2026-03-16 10:00:00Z'.
                new Rpt_PeriodSummary_Transaction
                {
                    PeriodMonth = "2026-03",
                    AutoID = tx2.AutoId,
                    RefNo = "TX288_02",
                    DLCode = "DL03",
                    DLCodeExceptionally = "DL02",
                    DealPointType = "CONSUMPTION",
                    MemberNo = "MB02",
                    MemberName = "Trần Thị B",
                    AmountChTotal = 2000.0,
                    LogLUDTimeUTC = new DateTime(2026, 3, 16, 10, 0, 0, DateTimeKind.Utc),
                    LogLUBy = "seed"
                },
                // Dòng 3: PeriodMonth '2026-04', AutoID AutoId_3, RefNo 'TX288_03', DLCode 'DL01', DLCodeExceptionally 'DL02', DealPointType 'CONSUMPTION', MemberNo 'MB03', MemberName 'Lê Văn C', AmountChTotal 3000.0, LogLUDTimeUTC '2026-04-02 10:00:00Z'.
                new Rpt_PeriodSummary_Transaction
                {
                    PeriodMonth = "2026-04",
                    AutoID = tx3.AutoId,
                    RefNo = "TX288_03",
                    DLCode = "DL01",
                    DLCodeExceptionally = "DL02",
                    DealPointType = "CONSUMPTION",
                    MemberNo = "MB03",
                    MemberName = "Lê Văn C",
                    AmountChTotal = 3000.0,
                    LogLUDTimeUTC = new DateTime(2026, 4, 2, 10, 0, 0, DateTimeKind.Utc),
                    LogLUBy = "seed"
                },
                // Dòng 4: PeriodMonth '2026-03', AutoID AutoId_4, RefNo 'TX288_04', DLCode 'DL02', DLCodeExceptionally 'DL02', DealPointType 'CONSUMPTION', MemberNo 'MB04', MemberName 'Phạm Văn D', AmountChTotal 4000.0, LogLUDTimeUTC '2026-03-20 10:00:00Z'. (DLCode == DLCodeExceptionally -> sẽ bị loại).
                new Rpt_PeriodSummary_Transaction
                {
                    PeriodMonth = "2026-03",
                    AutoID = tx4.AutoId,
                    RefNo = "TX288_04",
                    DLCode = "DL02",
                    DLCodeExceptionally = "DL02",
                    DealPointType = "CONSUMPTION",
                    MemberNo = "MB04",
                    MemberName = "Phạm Văn D",
                    AmountChTotal = 4000.0,
                    LogLUDTimeUTC = new DateTime(2026, 3, 20, 10, 0, 0, DateTimeKind.Utc),
                    LogLUBy = "seed"
                },
                // Dòng 5: PeriodMonth '2026-03', AutoID AutoId_5, RefNo 'TX288_05', DLCode 'DL01', DLCodeExceptionally 'DL02', DealPointType 'INTRODUCTION', MemberNo 'MB05', MemberName 'Hoàng Thị E', AmountChTotal 5000.0, LogLUDTimeUTC '2026-03-22 10:00:00Z'. (DealPointType == 'INTRODUCTION' -> sẽ bị loại bởi DealPointType = 'CONSUMPTION').
                new Rpt_PeriodSummary_Transaction
                {
                    PeriodMonth = "2026-03",
                    AutoID = tx5.AutoId,
                    RefNo = "TX288_05",
                    DLCode = "DL01",
                    DLCodeExceptionally = "DL02",
                    DealPointType = "INTRODUCTION",
                    MemberNo = "MB05",
                    MemberName = "Hoàng Thị E",
                    AmountChTotal = 5000.0,
                    LogLUDTimeUTC = new DateTime(2026, 3, 22, 10, 0, 0, DateTimeKind.Utc),
                    LogLUBy = "seed"
                }
            );
            await db.SaveChangesAsync();
        }

        // 3. Seed 5 dòng vào Rpt_PeriodSummary_TransactionExceptionally
        if (!await db.Set<Rpt_PeriodSummary_TransactionExceptionally>().AnyAsync(x => x.RefNo == "TX288_01"))
        {
            db.Set<Rpt_PeriodSummary_TransactionExceptionally>().AddRange(
                // Dòng 1: PeriodMonth '2026-03', AutoID AutoId_1, RefNo 'TX288_01', NetworkID '1001', OrgID 'ORG01', DLCode 'DL01', DLCodeExceptionally 'DL02', DealPointType 'CONSUMPTION', MemberNo 'MB01', MemberName 'Nguyễn Văn A', CardTypeUse 'GOLD', CardTypeInit 'SILVER', AmountChTotal 1000.0, AmountChTotalInit 800.0, AmountChTotalDiffer 200.0, LogLUDTimeUTC '2026-03-15 10:00:00Z', LogLUBy 'seed'.
                new Rpt_PeriodSummary_TransactionExceptionally
                {
                    PeriodMonth = "2026-03",
                    AutoID = tx1.AutoId,
                    RefNo = "TX288_01",
                    NetworkID = "1001",
                    OrgID = "ORG01",
                    DLCode = "DL01",
                    DLCodeExceptionally = "DL02",
                    DealPointType = "CONSUMPTION",
                    MemberNo = "MB01",
                    MemberName = "Nguyễn Văn A",
                    CardTypeUse = "GOLD",
                    CardTypeInit = "SILVER",
                    AmountChTotal = 1000.0,
                    AmountChTotalInit = 800.0,
                    AmountChTotalDiffer = 200.0,
                    LogLUDTimeUTC = new DateTime(2026, 3, 15, 10, 0, 0, DateTimeKind.Utc),
                    LogLUBy = "seed"
                },
                // Dòng 2: PeriodMonth '2026-03', AutoID AutoId_2, RefNo 'TX288_02', NetworkID '1001', OrgID 'ORG01', DLCode 'DL03', DLCodeExceptionally 'DL02', DealPointType 'CONSUMPTION', MemberNo 'MB02', MemberName 'Trần Thị B', CardTypeUse 'PLATINUM', CardTypeInit 'GOLD', AmountChTotal 2000.0, AmountChTotalInit 1500.0, AmountChTotalDiffer 500.0, LogLUDTimeUTC '2026-03-16 10:00:00Z', LogLUBy 'seed'.
                new Rpt_PeriodSummary_TransactionExceptionally
                {
                    PeriodMonth = "2026-03",
                    AutoID = tx2.AutoId,
                    RefNo = "TX288_02",
                    NetworkID = "1001",
                    OrgID = "ORG01",
                    DLCode = "DL03",
                    DLCodeExceptionally = "DL02",
                    DealPointType = "CONSUMPTION",
                    MemberNo = "MB02",
                    MemberName = "Trần Thị B",
                    CardTypeUse = "PLATINUM",
                    CardTypeInit = "GOLD",
                    AmountChTotal = 2000.0,
                    AmountChTotalInit = 1500.0,
                    AmountChTotalDiffer = 500.0,
                    LogLUDTimeUTC = new DateTime(2026, 3, 16, 10, 0, 0, DateTimeKind.Utc),
                    LogLUBy = "seed"
                },
                // Dòng 3: PeriodMonth '2026-04', AutoID AutoId_3, RefNo 'TX288_03', NetworkID '1001', OrgID 'ORG01', DLCode 'DL01', DLCodeExceptionally 'DL02', DealPointType 'CONSUMPTION', MemberNo 'MB03', MemberName 'Lê Văn C', CardTypeUse 'GOLD', CardTypeInit 'SILVER', AmountChTotal 3000.0, AmountChTotalInit 2500.0, AmountChTotalDiffer 500.0, LogLUDTimeUTC '2026-04-02 10:00:00Z', LogLUBy 'seed'.
                new Rpt_PeriodSummary_TransactionExceptionally
                {
                    PeriodMonth = "2026-04",
                    AutoID = tx3.AutoId,
                    RefNo = "TX288_03",
                    NetworkID = "1001",
                    OrgID = "ORG01",
                    DLCode = "DL01",
                    DLCodeExceptionally = "DL02",
                    DealPointType = "CONSUMPTION",
                    MemberNo = "MB03",
                    MemberName = "Lê Văn C",
                    CardTypeUse = "GOLD",
                    CardTypeInit = "SILVER",
                    AmountChTotal = 3000.0,
                    AmountChTotalInit = 2500.0,
                    AmountChTotalDiffer = 500.0,
                    LogLUDTimeUTC = new DateTime(2026, 4, 2, 10, 0, 0, DateTimeKind.Utc),
                    LogLUBy = "seed"
                },
                // Dòng 4: PeriodMonth '2026-03', AutoID AutoId_4, RefNo 'TX288_04', NetworkID '1001', OrgID 'ORG01', DLCode 'DL02', DLCodeExceptionally 'DL02', DealPointType 'CONSUMPTION', MemberNo 'MB04', MemberName 'Phạm Văn D', CardTypeUse 'SILVER', CardTypeInit 'SILVER', AmountChTotal 4000.0, AmountChTotalInit 4000.0, AmountChTotalDiffer 0.0, LogLUDTimeUTC '2026-03-20 10:00:00Z', LogLUBy 'seed'.
                new Rpt_PeriodSummary_TransactionExceptionally
                {
                    PeriodMonth = "2026-03",
                    AutoID = tx4.AutoId,
                    RefNo = "TX288_04",
                    NetworkID = "1001",
                    OrgID = "ORG01",
                    DLCode = "DL02",
                    DLCodeExceptionally = "DL02",
                    DealPointType = "CONSUMPTION",
                    MemberNo = "MB04",
                    MemberName = "Phạm Văn D",
                    CardTypeUse = "SILVER",
                    CardTypeInit = "SILVER",
                    AmountChTotal = 4000.0,
                    AmountChTotalInit = 4000.0,
                    AmountChTotalDiffer = 0.0,
                    LogLUDTimeUTC = new DateTime(2026, 3, 20, 10, 0, 0, DateTimeKind.Utc),
                    LogLUBy = "seed"
                },
                // Dòng 5: PeriodMonth '2026-03', AutoID AutoId_5, RefNo 'TX288_05', NetworkID '1001', OrgID 'ORG01', DLCode 'DL01', DLCodeExceptionally 'DL02', DealPointType 'INTRODUCTION', MemberNo 'MB05', MemberName 'Hoàng Thị E', CardTypeUse 'GOLD', CardTypeInit 'SILVER', AmountChTotal 5000.0, AmountChTotalInit 4000.0, AmountChTotalDiffer 1000.0, LogLUDTimeUTC '2026-03-22 10:00:00Z', LogLUBy 'seed'.
                new Rpt_PeriodSummary_TransactionExceptionally
                {
                    PeriodMonth = "2026-03",
                    AutoID = tx5.AutoId,
                    RefNo = "TX288_05",
                    NetworkID = "1001",
                    OrgID = "ORG01",
                    DLCode = "DL01",
                    DLCodeExceptionally = "DL02",
                    DealPointType = "INTRODUCTION",
                    MemberNo = "MB05",
                    MemberName = "Hoàng Thị E",
                    CardTypeUse = "GOLD",
                    CardTypeInit = "SILVER",
                    AmountChTotal = 5000.0,
                    AmountChTotalInit = 4000.0,
                    AmountChTotalDiffer = 1000.0,
                    LogLUDTimeUTC = new DateTime(2026, 3, 22, 10, 0, 0, DateTimeKind.Utc),
                    LogLUBy = "seed"
                }
            );
            await db.SaveChangesAsync();
        }
    }
}
