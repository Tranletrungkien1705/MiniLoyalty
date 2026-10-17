using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;

namespace MiniLoyalty.Models.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — Rpt_PeriodSummary (#284).
//
// Nguồn tra cứu (chỉ đọc):
//   - Model cột: 12.Dev.Common/idn.Loyalty.Common/Models/Rpt_PeriodSummary.cs:1-34
//   - DDL thật:  11.BackEnd/V10/05.Refs.Biz/Migrate/20221006.QUYNHNTN/20221707.QUYNHNTN.LOYALTY/Rpt_PeriodSummary.sql:71-95
//                20230731.z11.AlterTable.Rpt_PeriodSummary.sql (thêm LCol)
//                20240624.z10.Rpt_PeriodSummary.AddColumn.sql (thêm JCol)
//   - Biz LIVE:  11.BackEnd/V10/idn.Loyalty.Biz/Report.cs:4340-4941 (WAS_Rpt_PeriodSummary_Get, Rpt_PeriodSummary_Get, Rpt_PeriodSummary_GetX)
// ===========================================================================================

/// <summary>
/// Bảng báo cáo đối soát tổng hợp kỳ — port 1:1 Rpt_PeriodSummary (Rpt_PeriodSummary.sql:71-95).
/// PK kết hợp: (PeriodMonth, DLCPCode).
/// </summary>
public class Rpt_PeriodSummary
{
    public string PeriodMonth { get; set; } = "";       // Tháng đối soát (PK phần 1) — Rpt_PeriodSummary.sql:72, :94
    public string DLCPCode { get; set; } = "";          // Mã đại lý giao dịch (PK phần 2) — Rpt_PeriodSummary.sql:73, :94
    public string? NetworkID { get; set; }              // Mã Network — Rpt_PeriodSummary.sql:74
    public string? OrgID { get; set; }                  // Mã Org — Rpt_PeriodSummary.sql:75
    public double? ACol { get; set; }                   // Số điểm tiêu dùng Hội viên tích được từ Đại lý trong kỳ — :76
    public double? BCol { get; set; }                   // Số điểm Hội viên sử dụng tại Đại lý trong kỳ — :77
    public double? CCol { get; set; }                   // Số điểm Đại lý chuyển vào quỹ chung vì đặc cách hội viên — :78
    public double? DCol { get; set; }                   // Số điểm Đại lý được nhận từ quỹ chung theo hội viên đặc cách ĐL khác — :79
    public double? ECol { get; set; }                   // Số điểm Đại lý được nhận từ quỹ chung theo hội viên đặc cách từ HTV — :80
    public double? FCol { get; set; }                   // Tổng nộp vào quỹ chung — :81
    public double? GCol { get; set; }                   // Số điểm Quỹ chung chuyển cho ĐL — :82
    public double? HCol { get; set; }                   // Tổng số điểm Hủy hội viên của tháng đối soát (tính lại = HCol + LCol) — :83, Report.cs:4754
    public double? ICol { get; set; }                   // Tổng số điểm hết hạn lũy kế tính đến tháng đối soát (tính tất cả) — :84
    public double? JCol { get; set; }                   // Tổng số điểm hết hạn lũy kế tính đến tháng đối soát (chỉ tính lũy kế trong 1 năm) — 20240624.z10
    public double? KCol { get; set; }                   // Điểm tiêu dùng của hội viên hủy trong kỳ — :85
    public double? K1Col { get; set; }                  // Số điểm ĐL chuyển vào quỹ hội viên theo Dịch vụ ưu đãi do Bán hạng HV từ ĐL khác — Model :26
    public double? K2Col { get; set; }                  // Số điểm ĐL nhận được từ quỹ hội viên theo Dịch vụ ưu đãi do Bán hạng HV từ ĐL khác — Model :27
    public double? LCol { get; set; }                   // Tổng số điểm hết hạn của tháng đối soát — 20230731.z11, Report.cs:4760
    public double? PCol { get; set; }                   // Tổng số điểm ĐL chuyển vào quỹ HV — Model :29
    public double? QCol { get; set; }                   // Tổng số điểm quỹ HV chuyển cho ĐL — Model :30
    public DateTime? LogLUDTimeUTC { get; set; }        // Thời gian cập nhật cuối UTC — Rpt_PeriodSummary.sql:86
    public string? LogLUBy { get; set; }                // Người cập nhật cuối — Rpt_PeriodSummary.sql:87
}

/// <summary>
/// Cấu hình EF cho <see cref="Rpt_PeriodSummary"/> — bảng Rpt_PeriodSummary, PK composite (PeriodMonth, DLCPCode).
/// </summary>
public class Rpt_PeriodSummaryConfig : IEntityTypeConfiguration<Rpt_PeriodSummary>
{
    public void Configure(EntityTypeBuilder<Rpt_PeriodSummary> b)
    {
        b.ToTable("Rpt_PeriodSummary");
        b.HasKey(x => new { x.PeriodMonth, x.DLCPCode });
        b.Property(x => x.PeriodMonth).HasMaxLength(50);
        b.Property(x => x.DLCPCode).HasMaxLength(50);
        b.Property(x => x.NetworkID).HasMaxLength(50);
        b.Property(x => x.OrgID).HasMaxLength(50);
        b.Property(x => x.LogLUBy).HasMaxLength(50);
    }
}

/// <summary>
/// Seeder dữ liệu mẫu cho Rpt_PeriodSummary (#284).
/// Order => 78 (chưa dùng trong dải 10-99 theo Bài học A.4).
/// Idempotent: kiểm tra DLCPCode == 'DL01' trước khi thêm.
/// </summary>
public class Rpt_PeriodSummarySeeder : IPortSeeder
{
    public int Order => 78;

    public async Task SeedAsync(AppDbContext db)
    {
        if (!await db.Set<Rpt_PeriodSummary>().AnyAsync(x => x.DLCPCode == "DL01"))
        {
            db.Set<Rpt_PeriodSummary>().AddRange(
                // Dòng 1: PeriodMonth = "2026-03", DLCPCode = "DL01" (HCol tính toán trả về = 20 + 30 = 50.0)
                new Rpt_PeriodSummary
                {
                    PeriodMonth = "2026-03",
                    DLCPCode = "DL01",
                    NetworkID = "DEMO",
                    OrgID = "ORG01",
                    ACol = 1000.0,
                    BCol = 500.0,
                    CCol = 100.0,
                    DCol = 50.0,
                    ECol = 0.0,
                    FCol = 100.0,
                    GCol = 50.0,
                    HCol = 20.0,
                    ICol = 10.0,
                    JCol = 5.0,
                    KCol = 15.0,
                    LCol = 30.0,
                    LogLUDTimeUTC = new DateTime(2026, 3, 31, 10, 0, 0, DateTimeKind.Utc),
                    LogLUBy = "seed"
                },
                // Dòng 2: PeriodMonth = "2026-03", DLCPCode = "DL02" (HCol tính toán trả về = 40 + 60 = 100.0)
                new Rpt_PeriodSummary
                {
                    PeriodMonth = "2026-03",
                    DLCPCode = "DL02",
                    NetworkID = "DEMO",
                    OrgID = "ORG01",
                    ACol = 2000.0,
                    BCol = 1000.0,
                    CCol = 200.0,
                    DCol = 100.0,
                    ECol = 0.0,
                    FCol = 200.0,
                    GCol = 100.0,
                    HCol = 40.0,
                    ICol = 20.0,
                    JCol = 10.0,
                    KCol = 30.0,
                    LCol = 60.0,
                    LogLUDTimeUTC = new DateTime(2026, 3, 31, 11, 0, 0, DateTimeKind.Utc),
                    LogLUBy = "seed"
                },
                // Dòng 3: PeriodMonth = "2026-04", DLCPCode = "DL01" (HCol tính toán trả về = 50 + 70 = 120.0)
                new Rpt_PeriodSummary
                {
                    PeriodMonth = "2026-04",
                    DLCPCode = "DL01",
                    NetworkID = "DEMO",
                    OrgID = "ORG01",
                    ACol = 3000.0,
                    BCol = 1500.0,
                    CCol = 300.0,
                    DCol = 150.0,
                    ECol = 0.0,
                    FCol = 300.0,
                    GCol = 150.0,
                    HCol = 50.0,
                    ICol = 25.0,
                    JCol = 15.0,
                    KCol = 45.0,
                    LCol = 70.0,
                    LogLUDTimeUTC = new DateTime(2026, 4, 30, 9, 0, 0, DateTimeKind.Utc),
                    LogLUBy = "seed"
                }
            );
            await db.SaveChangesAsync();
        }
    }
}
