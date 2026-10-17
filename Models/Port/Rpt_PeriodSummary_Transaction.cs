using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;
using MiniLoyalty.Models;

namespace MiniLoyalty.Models.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — Rpt_PeriodSummary_Transaction (#287).
//
// Nguồn tra cứu (chỉ đọc):
//   - Model cột: 12.Dev.Common/idn.Loyalty.Common/Models/Rpt_PeriodSummary_Transaction.cs:9-37 (24 thuộc tính)
//   - DDL thật:  11.BackEnd/V10/05.Refs.Biz/Migrate/20221006.QUYNHNTN/20221707.QUYNHNTN.LOYALTY/Rpt_PeriodSummary.sql:5-32
//                19 cột vật lý trong bảng, PK composite 2 cột: (PeriodMonth, AutoID) — :30-31
//   - Biz LIVE:  11.BackEnd/V10/idn.Loyalty.Biz/Report.cs:5785-5890 (WAS_Rpt_PeriodSummary_Transaction_Get),
//                :6053-6275 (Rpt_PeriodSummary_Transaction_GetX)
//                Inner join Crd_CardTransaction cct on t.AutoID = cct.AutoID để lấy CreateDTimeUTC và CardNo.
// ===========================================================================================

/// <summary>
/// Bảng giao dịch đối soát tổng hợp kỳ — port 1:1 Rpt_PeriodSummary_Transaction (Rpt_PeriodSummary.sql:5-32).
/// PK composite 2 cột: (PeriodMonth, AutoID).
/// Model gồm 24 thuộc tính (19 cột vật lý DDL + 3 cột điểm tích model + 2 cột từ join Crd_CardTransaction).
/// </summary>
public class Rpt_PeriodSummary_Transaction
{
    public string PeriodMonth { get; set; } = "";          // Tháng đối soát (PK phần 1) — DDL :6, :30
    public long AutoID { get; set; }                       // Số giao dịch (PK phần 2) — DDL :7, :30
    public string RefNo { get; set; } = "";                // Mã giao dịch — DDL :8
    public string? NetworkID { get; set; }                 // Mã Network — DDL :9
    public string? OrgID { get; set; }                     // Mã Org — DDL :10
    public string? DealPointType { get; set; }             // Loại điểm — DDL :11
    public string? DLCode { get; set; }                    // Đại lý giao dịch — DDL :12
    public string? RefCode07 { get; set; }                 // Số tham chiếu — DDL :13
    public string? MemberNo { get; set; }                  // Mã hội viên — DDL :14
    public string? MemberName { get; set; }                // Tên hội viên — DDL :15
    public string? CardTypeUse { get; set; }               // Hạng sử dụng — DDL :16
    public string? CardTypeInit { get; set; }              // Hạng thực — DDL :17
    public string? DLCodeExceptionally { get; set; }       // ĐL YC đặc cách — DDL :18
    public double? AmountChTotal { get; set; }             // Số điểm tích hạng sử dụng — DDL :19
    public double? AmountChTotalInit { get; set; }         // Số điểm tích hạng thực — DDL :20
    public double? AmountChTotalDiffer { get; set; }       // Số điểm chênh hạng — DDL :21
    public DateTime? PointExpiryDTime { get; set; }        // Hạn sử dụng điểm — DDL :22
    public DateTime? LogLUDTimeUTC { get; set; }           // Thời gian cập nhật cuối UTC — DDL :23
    public string? LogLUBy { get; set; }                   // Người cập nhật cuối — DDL :24

    // 3 cột điểm tích từ model C# nguồn (không có trong DDL vật lý 19 cột) — Rpt_PeriodSummary_Transaction.cs:25-27
    public double? PointChTotal { get; set; }              // Số điểm tích hạng sử dụng
    public double? PointChTotalInit { get; set; }          // Số điểm tích hạng thực
    public double? PointChTotalDiffer { get; set; }        // Số điểm chênh hạng

    // 2 cột từ join Crd_CardTransaction — Report.cs:6157-6158, :5854-5861
    public string? CreateDTimeUTC { get; set; }            // Ngày giao dịch (định dạng yyyy-MM-dd giờ VN UTC+7)
    public string? CardNo { get; set; }                    // Mã kỳ xét hạng / số thẻ
}

/// <summary>
/// Cấu hình EF cho <see cref="Rpt_PeriodSummary_Transaction"/> — bảng Rpt_PeriodSummary_Transaction, PK composite (PeriodMonth, AutoID).
/// </summary>
public class Rpt_PeriodSummary_TransactionConfig : IEntityTypeConfiguration<Rpt_PeriodSummary_Transaction>
{
    public void Configure(EntityTypeBuilder<Rpt_PeriodSummary_Transaction> b)
    {
        b.ToTable("Rpt_PeriodSummary_Transaction");
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
/// Seeder dữ liệu mẫu cho Rpt_PeriodSummary_Transaction (#287).
/// Order => 83 (chưa dùng trong dải 10-99 theo Bài học A.4).
/// DEMO, idempotent.
/// </summary>
public class Rpt_PeriodSummary_TransactionSeeder : IPortSeeder
{
    public int Order => 83;

    public async Task SeedAsync(AppDbContext db)
    {
        // 1. Kiểm tra / bổ sung CrdCardTransaction tương ứng nếu chưa có
        var tx1 = await db.Set<CrdCardTransaction>().FirstOrDefaultAsync(x => x.RefNo == "TX287_01");
        if (tx1 == null)
        {
            tx1 = new CrdCardTransaction
            {
                RefNo = "TX287_01",
                CardNo = "CD01",
                MemberNo = "MB01",
                CreateDTimeUTC = new DateTime(2026, 3, 10, 10, 0, 0, DateTimeKind.Utc)
            };
            db.Set<CrdCardTransaction>().Add(tx1);
        }

        var tx2 = await db.Set<CrdCardTransaction>().FirstOrDefaultAsync(x => x.RefNo == "TX287_02");
        if (tx2 == null)
        {
            tx2 = new CrdCardTransaction
            {
                RefNo = "TX287_02",
                CardNo = "CD02",
                MemberNo = "MB02",
                CreateDTimeUTC = new DateTime(2026, 3, 15, 14, 0, 0, DateTimeKind.Utc)
            };
            db.Set<CrdCardTransaction>().Add(tx2);
        }

        var tx3 = await db.Set<CrdCardTransaction>().FirstOrDefaultAsync(x => x.RefNo == "TX287_03");
        if (tx3 == null)
        {
            tx3 = new CrdCardTransaction
            {
                RefNo = "TX287_03",
                CardNo = "CD03",
                MemberNo = "MB03",
                CreateDTimeUTC = new DateTime(2026, 4, 1, 8, 0, 0, DateTimeKind.Utc)
            };
            db.Set<CrdCardTransaction>().Add(tx3);
        }

        await db.SaveChangesAsync();

        // 2. Seed 3 dòng vào Rpt_PeriodSummary_Transaction
        if (!await db.Set<Rpt_PeriodSummary_Transaction>().AnyAsync(x => x.RefNo == "TX287_01"))
        {
            db.Set<Rpt_PeriodSummary_Transaction>().AddRange(
                // Dòng 1: PeriodMonth '2026-03', AutoID AutoId_1, RefNo 'TX287_01', DLCode 'DL01', DLCodeExceptionally 'DL01', DealPointType 'CONSUMPTION', MemberNo 'MB01', MemberName 'Nguyễn Văn A', AmountChTotal 1000.0, LogLUDTimeUTC '2026-03-15 10:00:00Z'.
                new Rpt_PeriodSummary_Transaction
                {
                    PeriodMonth = "2026-03",
                    AutoID = tx1.AutoId,
                    RefNo = "TX287_01",
                    DLCode = "DL01",
                    DLCodeExceptionally = "DL01",
                    DealPointType = "CONSUMPTION",
                    MemberNo = "MB01",
                    MemberName = "Nguyễn Văn A",
                    AmountChTotal = 1000.0,
                    LogLUDTimeUTC = new DateTime(2026, 3, 15, 10, 0, 0, DateTimeKind.Utc),
                    LogLUBy = "seed"
                },
                // Dòng 2: PeriodMonth '2026-03', AutoID AutoId_2, RefNo 'TX287_02', DLCode 'DL02', DLCodeExceptionally 'DL02', DealPointType 'CONSUMPTION', MemberNo 'MB02', MemberName 'Trần Thị B', AmountChTotal 2000.0, LogLUDTimeUTC '2026-03-16 10:00:00Z'.
                new Rpt_PeriodSummary_Transaction
                {
                    PeriodMonth = "2026-03",
                    AutoID = tx2.AutoId,
                    RefNo = "TX287_02",
                    DLCode = "DL02",
                    DLCodeExceptionally = "DL02",
                    DealPointType = "CONSUMPTION",
                    MemberNo = "MB02",
                    MemberName = "Trần Thị B",
                    AmountChTotal = 2000.0,
                    LogLUDTimeUTC = new DateTime(2026, 3, 16, 10, 0, 0, DateTimeKind.Utc),
                    LogLUBy = "seed"
                },
                // Dòng 3: PeriodMonth '2026-04', AutoID AutoId_3, RefNo 'TX287_03', DLCode 'DL01', DLCodeExceptionally 'DL01', DealPointType 'CONSUMPTION', MemberNo 'MB03', MemberName 'Lê Văn C', AmountChTotal 3000.0, LogLUDTimeUTC '2026-04-02 10:00:00Z'.
                new Rpt_PeriodSummary_Transaction
                {
                    PeriodMonth = "2026-04",
                    AutoID = tx3.AutoId,
                    RefNo = "TX287_03",
                    DLCode = "DL01",
                    DLCodeExceptionally = "DL01",
                    DealPointType = "CONSUMPTION",
                    MemberNo = "MB03",
                    MemberName = "Lê Văn C",
                    AmountChTotal = 3000.0,
                    LogLUDTimeUTC = new DateTime(2026, 4, 2, 10, 0, 0, DateTimeKind.Utc),
                    LogLUBy = "seed"
                }
            );
            await db.SaveChangesAsync();
        }
    }
}
