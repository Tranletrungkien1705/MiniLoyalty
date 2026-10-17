using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;

namespace MiniLoyalty.Models.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — Crd_MemberVoucherTransactionController/Report (#057).
//
// Nguồn tra cứu (chỉ đọc):
//   - DDL: 11.BackEnd/V10/05.Refs.Biz/Migrate/Script.HuongTTT/20250918.z11.Crd_MemberVoucherTransaction.sql:115-134, 165-166
//          11 cột: RptPEPDCInstNo, MemberNo, CardNo, RefNoIC, RefNoDC, NetworkID, PointDeCrease,
//                  CreateDTimeUTC, LogLUDTimeUTC, LogLUBy, VoucherNo.
//          PK composite: (RptPEPDCInstNo, MemberNo, CardNo, RefNoIC, RefNoDC).
// ===========================================================================================

/// <summary>
/// Bảng chi tiết giảm voucher — port 1:1 bảng RptPE_MemberVoucherDeCreaseInst.
/// DDL: 20250918.z11.Crd_MemberVoucherTransaction.sql:115-134, 165-166 (11 cột).
/// PK composite = (RptPEPDCInstNo, MemberNo, CardNo, RefNoIC, RefNoDC).
/// </summary>
public class RptPE_MemberVoucherDeCreaseInst
{
    public string RptPEPDCInstNo { get; set; } = "";               // DDL :116 (PK)
    public string MemberNo { get; set; } = "";                     // DDL :117 (PK)
    public string CardNo { get; set; } = "";                       // DDL :118 (PK)
    public string RefNoIC { get; set; } = "";                      // DDL :119 (PK)
    public string RefNoDC { get; set; } = "";                      // DDL :120 (PK - liên kết mvt.AutoId)
    public string NetworkID { get; set; } = "";                    // DDL :121
    public double? PointDeCrease { get; set; }                     // DDL :122 (MyValue)
    public DateTime CreateDTimeUTC { get; set; }                   // DDL :123 (MyDateTime NOT NULL)
    public DateTime? LogLUDTimeUTC { get; set; }                   // DDL :124 (MyDateTime)
    public string? LogLUBy { get; set; }                           // DDL :125 (Varchar50)
    public string? VoucherNo { get; set; }                         // DDL :166 (varchar50)
}

/// <summary>
/// Cấu hình Entity Framework cho bảng RptPE_MemberVoucherDeCreaseInst.
/// </summary>
public class RptPE_MemberVoucherDeCreaseInstConfig : IEntityTypeConfiguration<RptPE_MemberVoucherDeCreaseInst>
{
    public void Configure(EntityTypeBuilder<RptPE_MemberVoucherDeCreaseInst> builder)
    {
        builder.ToTable("RptPE_MemberVoucherDeCreaseInst");
        builder.HasKey(x => new { x.RptPEPDCInstNo, x.MemberNo, x.CardNo, x.RefNoIC, x.RefNoDC });
    }
}

/// <summary>
/// Seeder dữ liệu mẫu cho RptPE_MemberVoucherDeCreaseInst và dòng bổ sung cho Crd_MemberVoucherTransaction.
/// Order => 59. Idempotent.
/// </summary>
public class RptPE_MemberVoucherDeCreaseInstSeeder : IPortSeeder
{
    public int Order => 64;

    public async Task SeedAsync(AppDbContext db)
    {
        // 1. Dòng bổ sung dòng 5 trong Crd_MemberVoucherTransaction nếu chưa có (theo đề bài P2)
        if (!await db.Set<Crd_MemberVoucherTransaction>().AnyAsync(x => x.AutoId == 5))
        {
            db.Set<Crd_MemberVoucherTransaction>().Add(new Crd_MemberVoucherTransaction
            {
                AutoId = 5,
                RefNo = "VC05",
                MemberNo = "MB5602",
                CardNo = "CD5602",
                CardTypeUse = "N",
                CardTypeInit = "N",
                DealPointType = "VOUCHERSD",
                PrmVoucherCode = "PRMVC05",
                EffDateStart = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EffDateEnd = new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc),
                PointChTotal = 500.0,
                PointVCRemain = 500.0,
                CreateDate = new DateTime(2026, 5, 25, 0, 0, 0, DateTimeKind.Utc),
                LogLUDTimeUTC = new DateTime(2026, 5, 25, 10, 0, 0, DateTimeKind.Utc),
                NetworkID = "DEMO",
                OrgID = "DEMO",
                DLCode = "DL02",
                CreateDTimeUTC = new DateTime(2026, 5, 25, 10, 0, 0, DateTimeKind.Utc),
                CreateBy = "seed",
                LogLUBy = "seed"
            });
        }

        // 2. Dữ liệu mẫu RptPE_MemberVoucherDeCreaseInst (4 dòng cho RefNoDC 1, 2, 4, 5; RefNoDC 3 để null cho LEFT JOIN)
        if (!await db.Set<RptPE_MemberVoucherDeCreaseInst>().AnyAsync(x => x.RptPEPDCInstNo == "INST01"))
        {
            db.Set<RptPE_MemberVoucherDeCreaseInst>().AddRange(
                new RptPE_MemberVoucherDeCreaseInst
                {
                    RptPEPDCInstNo = "INST01",
                    MemberNo = "MB5601",
                    CardNo = "CD5601",
                    RefNoIC = "IC01",
                    RefNoDC = "1",
                    VoucherNo = "VN5601",
                    NetworkID = "DEMO",
                    PointDeCrease = 100.0,
                    CreateDTimeUTC = new DateTime(2026, 5, 10, 10, 0, 0, DateTimeKind.Utc),
                    LogLUDTimeUTC = new DateTime(2026, 5, 10, 10, 0, 0, DateTimeKind.Utc),
                    LogLUBy = "seed"
                },
                new RptPE_MemberVoucherDeCreaseInst
                {
                    RptPEPDCInstNo = "INST02",
                    MemberNo = "MB5601",
                    CardNo = "CD5601",
                    RefNoIC = "IC02",
                    RefNoDC = "2",
                    VoucherNo = "VN5602",
                    NetworkID = "DEMO",
                    PointDeCrease = 200.0,
                    CreateDTimeUTC = new DateTime(2026, 5, 15, 10, 0, 0, DateTimeKind.Utc),
                    LogLUDTimeUTC = new DateTime(2026, 5, 15, 10, 0, 0, DateTimeKind.Utc),
                    LogLUBy = "seed"
                },
                new RptPE_MemberVoucherDeCreaseInst
                {
                    RptPEPDCInstNo = "INST04",
                    MemberNo = "MB5602",
                    CardNo = "CD5602",
                    RefNoIC = "IC04",
                    RefNoDC = "4",
                    VoucherNo = "VN5604",
                    NetworkID = "DEMO",
                    PointDeCrease = 150.0,
                    CreateDTimeUTC = new DateTime(2026, 5, 12, 10, 0, 0, DateTimeKind.Utc),
                    LogLUDTimeUTC = new DateTime(2026, 5, 12, 10, 0, 0, DateTimeKind.Utc),
                    LogLUBy = "seed"
                },
                new RptPE_MemberVoucherDeCreaseInst
                {
                    RptPEPDCInstNo = "INST05",
                    MemberNo = "MB5602",
                    CardNo = "CD5602",
                    RefNoIC = "IC05",
                    RefNoDC = "5",
                    VoucherNo = "VN5605",
                    NetworkID = "DEMO",
                    PointDeCrease = 500.0,
                    CreateDTimeUTC = new DateTime(2026, 5, 25, 10, 0, 0, DateTimeKind.Utc),
                    LogLUDTimeUTC = new DateTime(2026, 5, 25, 10, 0, 0, DateTimeKind.Utc),
                    LogLUBy = "seed"
                }
            );
        }

        await db.SaveChangesAsync();
    }
}
