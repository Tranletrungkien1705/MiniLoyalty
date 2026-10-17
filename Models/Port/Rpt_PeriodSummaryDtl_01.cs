using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;

namespace MiniLoyalty.Models.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — Rpt_PeriodSummaryDtl_01 (#286).
//
// Nguồn tra cứu (chỉ đọc):
//   - Model cột: 12.Dev.Common/idn.Loyalty.Common/Models/Rpt_PeriodSummaryDtl_01.cs:9-43 (32 cột)
//   - DDL thật:  11.BackEnd/V10/05.Refs.Biz/Migrate/Script.QuyenNL/20230731.z11.CreateTable.Rpt_PeriodSummaryDtl_01.sql:4-35
//                20250930.z11.Rpt_PeriodSummaryDtl_01.AddCol.sql:1-42
//                PK composite 3 cột: (PeriodMonth, DLCPCode, PeriodMonthDay) — :34-35
//   - Biz LIVE:  11.BackEnd/V10/idn.Loyalty.Biz/Report.cs:5376-5781 (WAS_Rpt_PeriodSummaryDtl_01_Get, Rpt_PeriodSummaryDtl_01_Get, Rpt_PeriodSummaryDtl_01_GetX)
// ===========================================================================================

/// <summary>
/// Bảng chi tiết báo cáo đối soát tổng hợp kỳ — port 1:1 Rpt_PeriodSummaryDtl_01 (20230731.z11.CreateTable.Rpt_PeriodSummaryDtl_01.sql:4-35).
/// PK composite 3 cột: (PeriodMonth, DLCPCode, PeriodMonthDay).
/// Model gồm 32 cột (12.Dev.Common/idn.Loyalty.Common/Models/Rpt_PeriodSummaryDtl_01.cs:9-43).
/// </summary>
public class Rpt_PeriodSummaryDtl_01
{
    public string PeriodMonth { get; set; } = "";          // Tháng đối soát (PK phần 1) — DDL :5, :35
    public string DLCPCode { get; set; } = "";             // Mã Đại lý (PK phần 2) — DDL :6, :35
    public string PeriodMonthDay { get; set; } = "";       // Ngày đối soát (PK phần 3) — DDL :7, :35
    public string? NetworkID { get; set; }                 // Mã Network — DDL :8
    public string? OrgID { get; set; }                     // Mã Org — DDL :9
    public double? ACol { get; set; }                      // Điểm tích HV do phát sinh làm dịch vụ tại ĐL trong kỳ — DDL :10
    public double? BCol { get; set; }                      // Điểm tích từ các chương trình ngoài Dịch vụ tại ĐL (Sinh nhật, giới thiệu…) — DDL :11
    public double? CCol { get; set; }                      // Điểm ĐL chuyển vào Quỹ Hội viên tích từ HTV (HTV đặc cách) — DDL :12
    public double? DCol { get; set; }                      // Tổng điểm tích phát sinh trong kỳ — DDL :13
    public double? ECol { get; set; }                      // Điểm tiêu có nguồn tích từ làm Dịch vụ tại ĐL — DDL :14
    public double? FCol { get; set; }                      // Điểm tiêu có nguồn tích từ các chương trình ngoài Dịch vụ tại ĐL (Sinh nhật, giới thiệu…) — DDL :15
    public double? LCol { get; set; }                      // Điểm tiêu có nguồn tích từ HTV (HTV đặc cách, các chương trình tặng điểm của HTV) — DDL :16
    public double? MCol { get; set; }                      // Điểm tiêu có nguồn tích từ ĐL khác — DDL :17
    public double? M1Col { get; set; }                     // Điểm tiêu có nguồn tích từ HTV (KMDV) — AddCol.sql :2
    public double? M2Col { get; set; }                     // Điểm tiêu có nguồn tích từ HTV (KMBH) — AddCol.sql :3
    public double? M3Col { get; set; }                     // Điểm tiêu có nguồn tích từ HTV (KMMT) — AddCol.sql :4
    public double? GCol { get; set; }                      // Tổng điểm tiêu — DDL :18
    public double? HCol { get; set; }                      // Số điểm ĐL chuyển vào quỹ hội viên vì đặc cách hội viên — DDL :19
    public double? H1Col { get; set; }                     // Số điểm ĐL chuyển vào quỹ hội viên vì Bán hạng hội viên — AddCol.sql :5
    public double? K1Col { get; set; }                     // Số điểm ĐL chuyển vào quỹ hội viên theo Dịch vụ ưu đãi do Bán hạng HV từ ĐL khác — AddCol.sql :6
    public double? K2Col { get; set; }                     // Số điểm ĐL nhận được từ quỹ hội viên theo Dịch vụ ưu đãi do Bán hạng HV từ ĐL khác — AddCol.sql :7
    public double? ICol { get; set; }                      // Số điểm ĐL nhận được từ quỹ hội viên theo đặc cách của ĐL khác — DDL :20
    public double? I1Col { get; set; }                     // Số điểm ĐL nhận được từ quỹ hội viên theo Bán hạng hội viên của ĐL khác — AddCol.sql :8
    public double? QCol { get; set; }                      // Tổng số điểm ĐL chuyển vào Quỹ Hội viên (Theo ngày) — DDL :21
    public double? RCol { get; set; }                      // Tổng số điểm Quỹ Hội viên chuyển cho ĐL (Theo ngày) — DDL :22
    public double? SCol { get; set; }                      // Kết quả đối soát theo kỳ (Ngày/Tháng) — DDL :23
    public double? TCol { get; set; }                      // Tổng số điểm hết hạn của tháng đối soát — DDL :24
    public double? UCol { get; set; }                      // Tổng điểm hết hạn do hủy thẻ tại tháng đối soát — DDL :25
    public double? VCol { get; set; }                      // Tổng số điểm hết hạn lũy kế năm tính đến tháng đối soát (tính tất cả) — DDL :26
    public double? WCol { get; set; }                      // Tổng số điểm hết hạn lũy kế tính đến tháng đối soát (chỉ tính lũy kế trong 1 năm) — Model :40
    public DateTime? LogLUDTimeUTC { get; set; }           // Thời gian cập nhật cuối UTC — DDL :27
    public string? LogLUBy { get; set; }                   // Người cập nhật cuối — DDL :28
}

/// <summary>
/// Cấu hình EF cho <see cref="Rpt_PeriodSummaryDtl_01"/> — bảng Rpt_PeriodSummaryDtl_01, PK composite (PeriodMonth, DLCPCode, PeriodMonthDay).
/// </summary>
public class Rpt_PeriodSummaryDtl_01Config : IEntityTypeConfiguration<Rpt_PeriodSummaryDtl_01>
{
    public void Configure(EntityTypeBuilder<Rpt_PeriodSummaryDtl_01> b)
    {
        b.ToTable("Rpt_PeriodSummaryDtl_01");
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
/// Seeder dữ liệu mẫu cho Rpt_PeriodSummaryDtl_01 (#286).
/// Order => 82 (chưa dùng trong dải 10-99 theo Bài học A.4).
/// Idempotent: kiểm tra DLCPCode == 'DL01' trước khi thêm.
/// </summary>
public class Rpt_PeriodSummaryDtl_01Seeder : IPortSeeder
{
    public int Order => 82;

    public async Task SeedAsync(AppDbContext db)
    {
        if (!await db.Set<Rpt_PeriodSummaryDtl_01>().AnyAsync(x => x.DLCPCode == "DL01"))
        {
            db.Set<Rpt_PeriodSummaryDtl_01>().AddRange(
                // Dòng 1: PeriodMonth '2026-03', DLCPCode 'DL01', PeriodMonthDay '2026-03-15', ACol 100.0, BCol 50.0, HCol 10.0, LCol 15.0, LogLUDTimeUTC '2026-03-15 10:00:00Z'.
                new Rpt_PeriodSummaryDtl_01
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
                // Dòng 2: PeriodMonth '2026-03', DLCPCode 'DL02', PeriodMonthDay '2026-03-20', ACol 200.0, BCol 100.0, HCol 20.0, LCol 30.0, LogLUDTimeUTC '2026-03-20 11:00:00Z'.
                new Rpt_PeriodSummaryDtl_01
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
                // Dòng 3: PeriodMonth '2026-04', DLCPCode 'DL01', PeriodMonthDay '2026-04-10', ACol 300.0, BCol 150.0, HCol 30.0, LCol 40.0, LogLUDTimeUTC '2026-04-10 09:00:00Z'.
                new Rpt_PeriodSummaryDtl_01
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
