using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;

namespace MiniLoyalty.Models.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — Rpt_PeriodSummaryDtl (#285).
//
// Nguồn tra cứu (chỉ đọc):
//   - Model cột: 12.Dev.Common/idn.Loyalty.Common/Models/Rpt_PeriodSummaryDtl.cs:9-34 (23 cột)
//   - DDL thật:  11.BackEnd/V10/05.Refs.Biz/Migrate/20230317.Rpt_PeriodSummaryDtl_New.sql:4-36
//                20250930.z11.Rpt_PeriodSummaryDtl.AddCol.sql:29-55
//                PK composite 3 cột: (PeriodMonth, DLCPCode, PeriodMonthDay) — :34-35
//   - Biz LIVE:  11.BackEnd/V10/idn.Loyalty.Biz/Report.cs:4945-5372 (WAS_Rpt_PeriodSummaryDtl_Get, Rpt_PeriodSummaryDtl_Get, Rpt_PeriodSummaryDtl_GetX)
// ===========================================================================================

/// <summary>
/// Bảng chi tiết báo cáo đối soát tổng hợp kỳ — port 1:1 Rpt_PeriodSummaryDtl (20230317.Rpt_PeriodSummaryDtl_New.sql:4-36).
/// PK composite 3 cột: (PeriodMonth, DLCPCode, PeriodMonthDay).
/// </summary>
public class Rpt_PeriodSummaryDtl
{
    public string PeriodMonth { get; set; } = "";          // Tháng đối soát (PK phần 1) — DDL :5, :35
    public string DLCPCode { get; set; } = "";             // Mã đại lý giao dịch (PK phần 2) — DDL :6, :35
    public string PeriodMonthDay { get; set; } = "";       // Ngày giao dịch (PK phần 3) — DDL :7, :35
    public string? NetworkID { get; set; }                 // Mã Network — DDL :8
    public string? OrgID { get; set; }                     // Mã Org — DDL :9
    public double? ACol { get; set; }                      // Số điểm HV tích tại ĐL trong kỳ — DDL :10
    public double? BCol { get; set; }                      // Số điểm HV sử dụng tại ĐL trong kỳ — DDL :11
    public double? CCol { get; set; }                      // Số điểm ĐL chuyển vào quỹ chung vì đặc cách HV — DDL :12
    public double? DCol { get; set; }                      // Số điểm ĐL được nhận từ quỹ chung theo HV đặc cách của ĐL khác — DDL :13
    public double? ECol { get; set; }                      // Điểm ĐL chuyển vào quỹ chung tích từ HTV — DDL :14
    public double? FCol { get; set; }                      // Tổng số điểm ĐL chuyển vào quỹ chung (cũ) — DDL :15
    public double? GCol { get; set; }                      // Tổng số điểm Quỹ chung chuyển cho ĐL (cũ) — DDL :18
    public double? HCol { get; set; }                      // Tổng số điểm Hủy hội viên của tháng đối soát (tính lại = HCol + LCol) — DDL :19, Report.cs:5302
    public double? ICol { get; set; }                      // Tổng số điểm hết hạn lũy kế năm tính đến tháng đối soát (tính tất cả) — DDL :20
    public double? JCol { get; set; }                      // Tổng số điểm hết hạn lũy kế tính đến tháng đối soát (chỉ tính lũy kế trong 1 năm) — Model :25
    public double? KCol { get; set; }                      // Điểm tiêu dùng của hội viên hủy trong kỳ — Model :26
    public double? K1Col { get; set; }                     // Số điểm ĐL chuyển vào quỹ hội viên theo Dịch vụ ưu đãi do Bán hạng HV từ ĐL khác — Model :27
    public double? K2Col { get; set; }                     // Số điểm ĐL nhận được từ quỹ hội viên theo Dịch vụ ưu đãi do Bán hạng HV từ ĐL khác — Model :28
    public double? LCol { get; set; }                      // Tổng số điểm hết hạn của tháng đối soát — DDL :16, Report.cs:5302
    public double? PCol { get; set; }                      // Tổng số điểm ĐL chuyển vào quỹ HV — Model :30
    public double? QCol { get; set; }                      // Tổng số điểm quỹ HV chuyển cho ĐL — DDL :21, Model :31
    public DateTime? LogLUDTimeUTC { get; set; }           // Thời gian cập nhật cuối UTC — DDL :27
    public string? LogLUBy { get; set; }                   // Người cập nhật cuối — DDL :28
}

/// <summary>
/// Cấu hình EF cho <see cref="Rpt_PeriodSummaryDtl"/> — bảng Rpt_PeriodSummaryDtl, PK composite (PeriodMonth, DLCPCode, PeriodMonthDay).
/// </summary>
public class Rpt_PeriodSummaryDtlConfig : IEntityTypeConfiguration<Rpt_PeriodSummaryDtl>
{
    public void Configure(EntityTypeBuilder<Rpt_PeriodSummaryDtl> b)
    {
        b.ToTable("Rpt_PeriodSummaryDtl");
        b.HasKey(x => new { x.PeriodMonth, x.DLCPCode, x.PeriodMonthDay });
        b.Property(x => x.PeriodMonth).HasMaxLength(50);
        b.Property(x => x.DLCPCode).HasMaxLength(50);
        b.Property(x => x.PeriodMonthDay).HasMaxLength(50);
        b.Property(x => x.NetworkID).HasMaxLength(50);
        b.Property(x => x.OrgID).HasMaxLength(50);
        b.Property(x => x.LogLUBy).HasMaxLength(50);
    }
}

/// <summary>
/// Seeder dữ liệu mẫu cho Rpt_PeriodSummaryDtl (#285).
/// Order => 81 (chưa dùng trong dải 10-99 theo Bài học A.4).
/// Idempotent: kiểm tra DLCPCode == 'DL01' trước khi thêm.
/// </summary>
public class Rpt_PeriodSummaryDtlSeeder : IPortSeeder
{
    public int Order => 81;

    public async Task SeedAsync(AppDbContext db)
    {
        if (!await db.Set<Rpt_PeriodSummaryDtl>().AnyAsync(x => x.DLCPCode == "DL01"))
        {
            db.Set<Rpt_PeriodSummaryDtl>().AddRange(
                // Dòng 1: PeriodMonth '2026-03', DLCPCode 'DL01', PeriodMonthDay '2026-03-15', ACol 100.0, BCol 50.0, HCol 10.0, LCol 15.0 (HCol tính trả về = 10.0 + 15.0 = 25.0)
                new Rpt_PeriodSummaryDtl
                {
                    PeriodMonth = "2026-03",
                    DLCPCode = "DL01",
                    PeriodMonthDay = "2026-03-15",
                    NetworkID = "DEMO",
                    OrgID = "ORG01",
                    ACol = 100.0,
                    BCol = 50.0,
                    HCol = 10.0,
                    LCol = 15.0,
                    LogLUDTimeUTC = new DateTime(2026, 3, 15, 10, 0, 0, DateTimeKind.Utc),
                    LogLUBy = "seed"
                },
                // Dòng 2: PeriodMonth '2026-03', DLCPCode 'DL02', PeriodMonthDay '2026-03-20', ACol 200.0, BCol 100.0, HCol 20.0, LCol 30.0 (HCol tính trả về = 20.0 + 30.0 = 50.0)
                new Rpt_PeriodSummaryDtl
                {
                    PeriodMonth = "2026-03",
                    DLCPCode = "DL02",
                    PeriodMonthDay = "2026-03-20",
                    NetworkID = "DEMO",
                    OrgID = "ORG01",
                    ACol = 200.0,
                    BCol = 100.0,
                    HCol = 20.0,
                    LCol = 30.0,
                    LogLUDTimeUTC = new DateTime(2026, 3, 20, 11, 0, 0, DateTimeKind.Utc),
                    LogLUBy = "seed"
                },
                // Dòng 3: PeriodMonth '2026-04', DLCPCode 'DL01', PeriodMonthDay '2026-04-10', ACol 300.0, BCol 150.0, HCol 30.0, LCol 40.0 (HCol tính trả về = 30.0 + 40.0 = 70.0)
                new Rpt_PeriodSummaryDtl
                {
                    PeriodMonth = "2026-04",
                    DLCPCode = "DL01",
                    PeriodMonthDay = "2026-04-10",
                    NetworkID = "DEMO",
                    OrgID = "ORG01",
                    ACol = 300.0,
                    BCol = 150.0,
                    HCol = 30.0,
                    LCol = 40.0,
                    LogLUDTimeUTC = new DateTime(2026, 4, 10, 9, 0, 0, DateTimeKind.Utc),
                    LogLUBy = "seed"
                }
            );
            await db.SaveChangesAsync();
        }
    }
}
