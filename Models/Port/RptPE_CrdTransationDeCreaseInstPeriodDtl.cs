using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;
using MiniLoyalty.Models;

namespace MiniLoyalty.Models.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — RptPE_CrdTransationDeCreaseInstPeriodDtl (#290).
//
// Nguồn tra cứu (chỉ đọc):
//   - Model cột: 12.Dev.Common/idn.Loyalty.Common/Models/RptPE_CrdTransationDeCreaseInstPeriodDtl.cs:9-42 (31 thuộc tính)
//   - DDL thật:  11.BackEnd/V10/05.Refs.Biz/Migrate/Script.QuyenNL/20230731.z11.CreateTable.RptPE_CrdTransationDeCreaseInstPeriodDtl.sql:4-33
//                27 cột vật lý trong bảng, PK composite 6 cột:
//                (PeriodMonth, RptPEPDCInstNo, MemberNo, CardNo, RefNoIC, RefNoDC)
//   - Biz LIVE:  11.BackEnd/V10/idn.Loyalty.Biz/Report.cs:10941-11050 (WAS_RptPE_CrdTransationDeCreaseInstPeriodDtl_Get),
//                :11051-11190 (RptPE_CrdTransationDeCreaseInstPeriodDtl_Get),
//                :11191-11394 (RptPE_CrdTransationDeCreaseInstPeriodDtl_GetX)
//                Inner join Crd_CardTransaction cct on rpctipd.RefNoDC = cct.AutoID (lấy CardTypeUseDC, CardTypeInitDC, RefNo, CreateDTimeUTC_DC).
//                Inner join Crd_CardTransaction cct_ic on rpctipd.RefNoIC = cct_ic.AutoID (lấy CreateDTimeUTC_IC).
//                Inner join Crd_Member cm on rpctipd.MemberNo = cm.MemberNo (lấy MemberName).
// ===========================================================================================

/// <summary>
/// Bảng chi tiết giảm điểm kỳ — port 1:1 RptPE_CrdTransationDeCreaseInstPeriodDtl.
/// DDL: 20230731.z11.CreateTable.RptPE_CrdTransationDeCreaseInstPeriodDtl.sql:4-33 (27 cột vật lý).
/// PK composite 6 cột: (PeriodMonth, RptPEPDCInstNo, MemberNo, CardNo, RefNoIC, RefNoDC).
/// Model C# gồm 31 thuộc tính (27 cột vật lý DDL + 4 cột lấy từ join).
/// </summary>
public class RptPE_CrdTransationDeCreaseInstPeriodDtl
{
    // 27 cột vật lý trong bảng DDL
    public string PeriodMonth { get; set; } = "";               // Tháng đối soát (PK) — DDL :5
    public string RptPEPDCInstNo { get; set; } = "";            // Mã dòng báo cáo (PK) — DDL :6
    public string MemberNo { get; set; } = "";                  // Mã hội viên (PK) — DDL :7
    public string CardNo { get; set; } = "";                    // Mã thẻ / số thẻ (PK) — DDL :8
    public string RefNoIC { get; set; } = "";                   // Mã giao dịch tích điểm (PK) — DDL :9
    public string RefNoDC { get; set; } = "";                   // Mã giao dịch tiêu điểm (PK) — DDL :10
    public string NetworkID { get; set; } = "";                 // NetworkID — DDL :11
    public string? CardTypeUseIC { get; set; }                  // Hạng sử dụng tích điểm — DDL :12
    public string? CardTypeInitIC { get; set; }                 // Hạng thực tích điểm — DDL :13
    public string? CreateDateUTCIC { get; set; }                // Ngày giao dịch tích điểm (chuỗi yyyy-MM-dd sau quy đổi giờ VN) — DDL :14
    public double? PointInitIC { get; set; }                    // Điểm tích — DDL :15
    public string? DealPointTypeIC { get; set; }                // Loại tích điểm — DDL :16
    public double? AmountChTotalIC { get; set; }                // Số điểm tích giao dịch + điểm — DDL :17
    public double? AmountChTotalInitIC { get; set; }            // Số điểm tích hạng thực giao dịch + điểm — DDL :18
    public double? AmountChTotalDifferIC { get; set; }          // Số điểm chênh giao dịch + điểm — DDL :19
    public string? CreateDateUTCDC { get; set; }                // Ngày giao dịch tiêu điểm (chuỗi yyyy-MM-dd sau quy đổi giờ VN) — DDL :20
    public double? PointInitDC { get; set; }                    // Điểm tiêu — DDL :21
    public string? DealPointTypeDC { get; set; }                // Loại tiêu điểm — DDL :22
    public double? PointDeCrease { get; set; }                  // Tiền sử dụng trong giao dịch tiêu — DDL :23
    public double? PointDeCreaseHTV { get; set; }               // Tiền lấy từ đại lý đặc cách — DDL :24
    public double? PointDeCreaseDealer { get; set; }            // Tiền lấy từ ĐL giao dịch — DDL :25
    public string? DLCodeTranIC { get; set; }                   // Đại lý giao dịch tích điểm — DDL :26
    public string? DLCodeExceptionallyIC { get; set; }          // Đại lý đặc cách giao dịch tích điểm — DDL :27
    public string? DLCodeTranDC { get; set; }                   // Đại lý giao dịch tiêu điểm — DDL :28
    public string? DLCodeExceptionallyDC { get; set; }          // ĐL YC đặc cách tiêu điểm — DDL :29
    public DateTime? LogLUDTimeUTC { get; set; }                // Thời gian cập nhật cuối UTC — DDL :30
    public string? LogLUBy { get; set; }                        // Người cập nhật cuối — DDL :31

    // 4 cột từ join (Report.cs:11320-11325, 12.Dev.Common Model :17, :21, :22, :23)
    public string? RefNo { get; set; }                          // Số RO (cct.RefNo)
    public string? MemberName { get; set; }                     // Tên hội viên (cm.MemberName)
    public string? CardTypeUseDC { get; set; }                  // Hạng sử dụng tiêu điểm (cct.CardTypeUse)
    public string? CardTypeInitDC { get; set; }                 // Hạng thực tiêu điểm (cct.CardTypeInit)
}

/// <summary>
/// Cấu hình Entity Framework cho bảng RptPE_CrdTransationDeCreaseInstPeriodDtl.
/// </summary>
public class RptPE_CrdTransationDeCreaseInstPeriodDtlConfig : IEntityTypeConfiguration<RptPE_CrdTransationDeCreaseInstPeriodDtl>
{
    public void Configure(EntityTypeBuilder<RptPE_CrdTransationDeCreaseInstPeriodDtl> b)
    {
        b.ToTable("RptPE_CrdTransationDeCreaseInstPeriodDtl");
        b.HasKey(x => new { x.PeriodMonth, x.RptPEPDCInstNo, x.MemberNo, x.CardNo, x.RefNoIC, x.RefNoDC });

        b.Property(x => x.PeriodMonth).HasMaxLength(50);
        b.Property(x => x.RptPEPDCInstNo).HasMaxLength(50);
        b.Property(x => x.MemberNo).HasMaxLength(50);
        b.Property(x => x.CardNo).HasMaxLength(50);
        b.Property(x => x.RefNoIC).HasMaxLength(50);
        b.Property(x => x.RefNoDC).HasMaxLength(50);
        b.Property(x => x.NetworkID).HasMaxLength(50);
        b.Property(x => x.CardTypeUseIC).HasMaxLength(50);
        b.Property(x => x.CardTypeInitIC).HasMaxLength(50);
        b.Property(x => x.CreateDateUTCIC).HasMaxLength(50);
        b.Property(x => x.DealPointTypeIC).HasMaxLength(50);
        b.Property(x => x.CreateDateUTCDC).HasMaxLength(50);
        b.Property(x => x.DealPointTypeDC).HasMaxLength(50);
        b.Property(x => x.DLCodeTranIC).HasMaxLength(50);
        b.Property(x => x.DLCodeExceptionallyIC).HasMaxLength(50);
        b.Property(x => x.DLCodeTranDC).HasMaxLength(50);
        b.Property(x => x.DLCodeExceptionallyDC).HasMaxLength(50);
        b.Property(x => x.LogLUBy).HasMaxLength(50);

        // Bỏ qua 4 thuộc tính từ join (không có trong DDL vật lý 27 cột)
        b.Ignore(x => x.RefNo);
        b.Ignore(x => x.MemberName);
        b.Ignore(x => x.CardTypeUseDC);
        b.Ignore(x => x.CardTypeInitDC);
    }
}

/// <summary>
/// Seeder dữ liệu mẫu cho RptPE_CrdTransationDeCreaseInstPeriodDtl (#290).
/// Order => 92 (chưa dùng trong dải 10-99 theo Bài học A.4).
/// DEMO, idempotent.
/// </summary>
public class RptPE_CrdTransationDeCreaseInstPeriodDtlSeeder : IPortSeeder
{
    public int Order => 92;

    public async Task SeedAsync(AppDbContext db)
    {
        // 1. Kiểm tra / thêm CrdMember nếu chưa có
        if (!await db.Set<CrdMember>().AnyAsync(x => x.MemberNo == "MB290_01"))
        {
            db.Set<CrdMember>().Add(new CrdMember
            {
                MemberNo = "MB290_01",
                MemberName = "Nguyễn Văn An",
                NetworkID = "1001",
                OrgID = "1001"
            });
        }
        if (!await db.Set<CrdMember>().AnyAsync(x => x.MemberNo == "MB290_02"))
        {
            db.Set<CrdMember>().Add(new CrdMember
            {
                MemberNo = "MB290_02",
                MemberName = "Trần Thị Bình",
                NetworkID = "1001",
                OrgID = "1001"
            });
        }
        if (!await db.Set<CrdMember>().AnyAsync(x => x.MemberNo == "MB290_03"))
        {
            db.Set<CrdMember>().Add(new CrdMember
            {
                MemberNo = "MB290_03",
                MemberName = "Lê Văn Cường",
                NetworkID = "1001",
                OrgID = "1001"
            });
        }
        await db.SaveChangesAsync();

        // 2. Kiểm tra / thêm CrdCardTransaction (IC1, DC1, IC2, DC2, IC3, DC3)
        var txIC1 = await db.Set<CrdCardTransaction>().FirstOrDefaultAsync(x => x.RefNo == "TX290_IC01");
        if (txIC1 == null)
        {
            txIC1 = new CrdCardTransaction
            {
                RefNo = "TX290_IC01",
                CardNo = "CD290_01",
                MemberNo = "MB290_01",
                CreateDTimeUTC = new DateTime(2026, 3, 10, 3, 0, 0, DateTimeKind.Utc),
                NetworkID = "1001",
                OrgID = "1001"
            };
            db.Set<CrdCardTransaction>().Add(txIC1);
            await db.SaveChangesAsync();
        }

        var txDC1 = await db.Set<CrdCardTransaction>().FirstOrDefaultAsync(x => x.RefNo == "TX290_DC01");
        if (txDC1 == null)
        {
            txDC1 = new CrdCardTransaction
            {
                RefNo = "TX290_DC01",
                CardNo = "CD290_01",
                MemberNo = "MB290_01",
                CardTypeUse = "GOLD",
                CardTypeInit = "SILVER",
                CreateDTimeUTC = new DateTime(2026, 3, 15, 7, 0, 0, DateTimeKind.Utc),
                NetworkID = "1001",
                OrgID = "1001"
            };
            db.Set<CrdCardTransaction>().Add(txDC1);
            await db.SaveChangesAsync();
        }

        var txIC2 = await db.Set<CrdCardTransaction>().FirstOrDefaultAsync(x => x.RefNo == "TX290_IC02");
        if (txIC2 == null)
        {
            txIC2 = new CrdCardTransaction
            {
                RefNo = "TX290_IC02",
                CardNo = "CD290_02",
                MemberNo = "MB290_02",
                CreateDTimeUTC = new DateTime(2026, 3, 12, 1, 0, 0, DateTimeKind.Utc),
                NetworkID = "1001",
                OrgID = "1001"
            };
            db.Set<CrdCardTransaction>().Add(txIC2);
            await db.SaveChangesAsync();
        }

        var txDC2 = await db.Set<CrdCardTransaction>().FirstOrDefaultAsync(x => x.RefNo == "TX290_DC02");
        if (txDC2 == null)
        {
            txDC2 = new CrdCardTransaction
            {
                RefNo = "TX290_DC02",
                CardNo = "CD290_02",
                MemberNo = "MB290_02",
                CardTypeUse = "PLATINUM",
                CardTypeInit = "GOLD",
                CreateDTimeUTC = new DateTime(2026, 3, 20, 2, 0, 0, DateTimeKind.Utc),
                NetworkID = "1001",
                OrgID = "1001"
            };
            db.Set<CrdCardTransaction>().Add(txDC2);
            await db.SaveChangesAsync();
        }

        var txIC3 = await db.Set<CrdCardTransaction>().FirstOrDefaultAsync(x => x.RefNo == "TX290_IC03");
        if (txIC3 == null)
        {
            txIC3 = new CrdCardTransaction
            {
                RefNo = "TX290_IC03",
                CardNo = "CD290_03",
                MemberNo = "MB290_03",
                CreateDTimeUTC = new DateTime(2026, 4, 1, 2, 0, 0, DateTimeKind.Utc),
                NetworkID = "1001",
                OrgID = "1001"
            };
            db.Set<CrdCardTransaction>().Add(txIC3);
            await db.SaveChangesAsync();
        }

        var txDC3 = await db.Set<CrdCardTransaction>().FirstOrDefaultAsync(x => x.RefNo == "TX290_DC03");
        if (txDC3 == null)
        {
            txDC3 = new CrdCardTransaction
            {
                RefNo = "TX290_DC03",
                CardNo = "CD290_03",
                MemberNo = "MB290_03",
                CardTypeUse = "GOLD",
                CardTypeInit = "GOLD",
                CreateDTimeUTC = new DateTime(2026, 4, 5, 4, 0, 0, DateTimeKind.Utc),
                NetworkID = "1001",
                OrgID = "1001"
            };
            db.Set<CrdCardTransaction>().Add(txDC3);
            await db.SaveChangesAsync();
        }

        // 3. Kiểm tra / thêm dữ liệu mẫu RptPE_CrdTransationDeCreaseInstPeriodDtl (3 dòng)
        if (!await db.Set<RptPE_CrdTransationDeCreaseInstPeriodDtl>().AnyAsync(x => x.RptPEPDCInstNo == "INST290_01"))
        {
            db.Set<RptPE_CrdTransationDeCreaseInstPeriodDtl>().AddRange(
                new RptPE_CrdTransationDeCreaseInstPeriodDtl
                {
                    PeriodMonth = "2026-03",
                    RptPEPDCInstNo = "INST290_01",
                    MemberNo = "MB290_01",
                    CardNo = "CD290_01",
                    RefNoIC = txIC1.AutoId.ToString(),
                    RefNoDC = txDC1.AutoId.ToString(),
                    NetworkID = "1001",
                    CardTypeUseIC = "SILVER",
                    CardTypeInitIC = "STANDARD",
                    CreateDateUTCIC = new DateTime(2026, 3, 10).ToString("yyyy-MM-dd"),
                    CreateDateUTCDC = new DateTime(2026, 3, 15).ToString("yyyy-MM-dd"),
                    PointInitIC = 500.0,
                    PointInitDC = 300.0,
                    PointDeCrease = 200.0,
                    PointDeCreaseHTV = 100.0,
                    PointDeCreaseDealer = 100.0,
                    DLCodeTranIC = "DL02",
                    DLCodeTranDC = "DL02",
                    LogLUDTimeUTC = DateTime.Parse("2026-03-15T10:00:00Z", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal),
                    LogLUBy = "seed"
                },
                new RptPE_CrdTransationDeCreaseInstPeriodDtl
                {
                    PeriodMonth = "2026-03",
                    RptPEPDCInstNo = "INST290_02",
                    MemberNo = "MB290_02",
                    CardNo = "CD290_02",
                    RefNoIC = txIC2.AutoId.ToString(),
                    RefNoDC = txDC2.AutoId.ToString(),
                    NetworkID = "1001",
                    CardTypeUseIC = "GOLD",
                    CardTypeInitIC = "SILVER",
                    CreateDateUTCIC = new DateTime(2026, 3, 12).ToString("yyyy-MM-dd"),
                    CreateDateUTCDC = new DateTime(2026, 3, 20).ToString("yyyy-MM-dd"),
                    PointInitIC = 800.0,
                    PointInitDC = 500.0,
                    PointDeCrease = 400.0,
                    PointDeCreaseHTV = 200.0,
                    PointDeCreaseDealer = 200.0,
                    DLCodeTranIC = "DL03",
                    DLCodeTranDC = "DL02",
                    LogLUDTimeUTC = DateTime.Parse("2026-03-20T10:00:00Z", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal),
                    LogLUBy = "seed"
                },
                new RptPE_CrdTransationDeCreaseInstPeriodDtl
                {
                    PeriodMonth = "2026-04",
                    RptPEPDCInstNo = "INST290_03",
                    MemberNo = "MB290_03",
                    CardNo = "CD290_03",
                    RefNoIC = txIC3.AutoId.ToString(),
                    RefNoDC = txDC3.AutoId.ToString(),
                    NetworkID = "1001",
                    CardTypeUseIC = "PLATINUM",
                    CardTypeInitIC = "GOLD",
                    CreateDateUTCIC = new DateTime(2026, 4, 1).ToString("yyyy-MM-dd"),
                    CreateDateUTCDC = new DateTime(2026, 4, 5).ToString("yyyy-MM-dd"),
                    PointInitIC = 1000.0,
                    PointInitDC = 600.0,
                    PointDeCrease = 500.0,
                    PointDeCreaseHTV = 300.0,
                    PointDeCreaseDealer = 200.0,
                    DLCodeTranIC = "DL01",
                    DLCodeTranDC = "DL04",
                    LogLUDTimeUTC = DateTime.Parse("2026-04-05T10:00:00Z", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal),
                    LogLUBy = "seed"
                }
            );
            await db.SaveChangesAsync();
        }
    }
}
