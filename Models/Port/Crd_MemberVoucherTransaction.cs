using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;
using MiniLoyalty.Models;

namespace MiniLoyalty.Models.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — Crd_MemberVoucherTransactionController/Search (#056).
//
// Nguồn tra cứu (chỉ đọc):
//   - Model:         12.Dev.Common/idn.Loyalty.Common/Models/Loyalty/Crd_MemberVoucherTransaction.cs:1-54 (41 thuộc tính)
//   - DDL Transaction: 11.BackEnd/V10/05.Refs.Biz/Migrate/Script.HuongTTT/20250918.z11.Crd_MemberVoucherTransaction.sql:57-97
//                    PK AutoId (bigint IDENTITY).
//   - DDL Voucher:   11.BackEnd/V10/05.Refs.Biz/Migrate/Script.HuongTTT/20250918.z11.Crd_MemberVoucherTransaction.sql:141-163
//                    PK composite (MemberNo, VoucherNo).
//   - Biz SQL:       11.BackEnd/V10/idn.Loyalty.Biz/Transaction.cs:4396-4614 (Crd_MemberVoucherTransaction_GetX)
// ===========================================================================================

/// <summary>
/// Bảng giao dịch voucher của hội viên — port 1:1 bảng Crd_MemberVoucherTransaction.
/// Nguồn cột: Crd_MemberVoucherTransaction.cs:1-54 · DDL: 20250918.z11.Crd_MemberVoucherTransaction.sql:57-97.
/// PK = AutoId (bigint IDENTITY).
/// </summary>
public class Crd_MemberVoucherTransaction
{
    public long AutoId { get; set; }                               // Khoá chính tự tăng (DDL :58)
    public string RefNo { get; set; } = "";                        // Mã giao dịch (Crd_MemberVoucherTransaction.cs:12)
    public string? NetworkID { get; set; }                         // NetworkID (:13)
    public string? OrgID { get; set; }                             // OrgID (:14)
    public string? DLCode { get; set; }                            // Đại lý giao dịch (:15)
    public string MemberNo { get; set; } = "";                     // Mã hội viên (:16)
    public string? CardNo { get; set; }                            // Mã thẻ hội viên (:17)
    public string? CardTypeUse { get; set; }                       // Hạng thẻ sử dụng (:18)
    public string? CardTypeInit { get; set; }                      // Hạng thẻ thực (:19)
    public string? DealPointType { get; set; }                     // Loại giao dịch (:20)
    public string? PrmVoucherCode { get; set; }                    // Mã voucher (:21)
    public DateTime? EffDateStart { get; set; }                    // Ngày hiệu lực CT voucher từ (:22)
    public DateTime? EffDateEnd { get; set; }                      // Ngày hiệu lực CT voucher đến (:23)
    public double? PointChTotal { get; set; }                      // Điểm voucher tích/tiêu (Tổng giá trị) - hạng sử dụng (:24)
    public double? PointActualChTotal { get; set; }                // Điểm voucher tích/tiêu (Tổng giá trị) - hạng thực (:25)
    public double? PointVCRemain { get; set; }                     // Điểm còn lại của voucher (Giá trị còn lại) - hạng sử dụng (:26)
    public double? PointActualVCRemain { get; set; }               // Điểm còn lại của voucher (Giá trị còn lại) - hạng thực (:27)
    public double? PointVCLimit { get; set; }                      // Điểm sử dụng max của voucher (:28)
    public DateTime? PointExpiryDate { get; set; }                 // Ngày hết hạn sử dụng điểm voucher (:29)
    public double? PointVCMemberRemain { get; set; }               // Tổng điểm voucher còn lại của hội viên - hạng sử dụng (:30)
    public double? PointActualVCMemberRM { get; set; }             // Tổng điểm voucher còn lại của hội viên - hạng thực (:31)
    public double? PointVCRO { get; set; }                         // Tổng điểm voucher sử dụng của báo giá - hạng sử dụng (:32)
    public double? PointActualVCRO { get; set; }                   // Tổng điểm voucher sử dụng của báo giá - hạng thực (:33)
    public double? QtyUseVCLimit { get; set; }                     // Max số lần sử dụng voucher (:34)
    public double? QtyUseVCRemain { get; set; }                    // Số lần sử dụng voucher còn lại (:35)
    public string? RefCode00 { get; set; }                         // Số tham chiếu (:36)
    public string? RefCode01 { get; set; }                         // (:37)
    public string? RefCode02 { get; set; }                         // (:38)
    public string? RefCode03 { get; set; }                         // (:39)
    public string? RefCode04 { get; set; }                         // (:40)
    public string? RefCode05 { get; set; }                         // (:41)
    public DateTime? CreateDate { get; set; }                      // Ngày thực hiện (:42)
    public DateTime? CreateDTimeUTC { get; set; }                  // (:43)
    public string? CreateBy { get; set; }                          // (:44)
    public DateTime? LUDTimeUTC { get; set; }                      // (:45)
    public string? LUBy { get; set; }                              // (:46)
    public DateTime? LogLUDTimeUTC { get; set; }                   // (:47)
    public string? LogLUBy { get; set; }                           // (:48)

    // Thuộc tính không ánh xạ vào bảng DB (theo Crd_MemberVoucherTransaction.cs:49-51 và SQL draft query)
    [NotMapped] public long? MyIdxSeq { get; set; }                // Số thứ tự từ truy vấn draft
    [NotMapped] public string? cm_MemberName { get; set; }         // Tên Hội viên (:49)
    [NotMapped] public string? cm_CarNo { get; set; }              // Biển số xe (:50)
    [NotMapped] public string? ic_VoucherNo { get; set; }          // Mã Voucher sử dụng (:51)
}

/// <summary>
/// Cấu hình Entity Framework cho bảng Crd_MemberVoucherTransaction.
/// </summary>
public class Crd_MemberVoucherTransactionConfig : IEntityTypeConfiguration<Crd_MemberVoucherTransaction>
{
    public void Configure(EntityTypeBuilder<Crd_MemberVoucherTransaction> builder)
    {
        builder.ToTable("Crd_MemberVoucherTransaction");
        builder.HasKey(x => x.AutoId);
        builder.Property(x => x.AutoId).ValueGeneratedOnAdd();
        builder.Ignore(x => x.MyIdxSeq);
        builder.Ignore(x => x.cm_MemberName);
        builder.Ignore(x => x.cm_CarNo);
        builder.Ignore(x => x.ic_VoucherNo);
    }
}

/// <summary>
/// Bảng ghi nhận danh sách voucher của hội viên (giá trị hiện tại của voucher).
/// Nguồn: DDL 20250918.z11.Crd_MemberVoucherTransaction.sql:141-163 (14 thuộc tính).
/// PK composite = (MemberNo, VoucherNo).
/// </summary>
public class Crd_MemberVoucher
{
    public string MemberNo { get; set; } = "";                     // Mã hội viên (DDL :142)
    public string VoucherNo { get; set; } = "";                    // Mã voucher (DDL :143)
    public string? NetworkID { get; set; }                         // Mã mạng lưới (DDL :144)
    public double? PointVCTotal { get; set; }                      // Tổng điểm voucher (DDL :145)
    public double? PointVCRemain { get; set; }                     // Điểm voucher còn lại (DDL :146)
    public double? PointVCLimit { get; set; }                      // Điểm sử dụng tối đa (DDL :147)
    public DateTime? PointExpireDate { get; set; }                 // Ngày hết hạn (DDL :148)
    public string? FlagActive { get; set; }                        // Trạng thái kích hoạt (DDL :149)
    public DateTime? CreateDTimeUTC { get; set; }                  // Thời điểm tạo (DDL :150)
    public string? CreateBy { get; set; }                          // Người tạo (DDL :151)
    public DateTime? LUDTimeUTC { get; set; }                      // Thời điểm sửa (DDL :152)
    public string? LUBy { get; set; }                              // Người sửa (DDL :153)
    public DateTime? LogLUDTimeUTC { get; set; }                   // Thời điểm ghi log UTC (DDL :154)
    public string? LogLUBy { get; set; }                           // Người ghi log (DDL :155)
}

/// <summary>
/// Cấu hình Entity Framework cho bảng Crd_MemberVoucher.
/// </summary>
public class Crd_MemberVoucherConfig : IEntityTypeConfiguration<Crd_MemberVoucher>
{
    public void Configure(EntityTypeBuilder<Crd_MemberVoucher> builder)
    {
        builder.ToTable("Crd_MemberVoucher");
        builder.HasKey(x => new { x.MemberNo, x.VoucherNo });
    }
}

/// <summary>
/// Seeder dữ liệu mẫu cho Crd_MemberVoucherTransaction và Crd_MemberVoucher.
/// Order => 75 (danh mục Mst_* dùng 10-99; 75 chưa dùng trong dải 10-99). Idempotent.
/// </summary>
public class Crd_MemberVoucherTransactionSeeder : IPortSeeder
{
    public int Order => 58;

    public async Task SeedAsync(AppDbContext db)
    {
        // 1. Đảm bảo CrdMember có MB5601 và MB5602
        if (!await db.Set<CrdMember>().AnyAsync(x => x.MemberNo == "MB5601"))
        {
            db.Set<CrdMember>().Add(new CrdMember
            {
                MemberNo = "MB5601",
                MemberName = "Nguyen Van A",
                PhoneNo = "0905600001",
                CarNo = "30A-56001",
                NetworkID = "DEMO",
                OrgID = "DEMO",
                MemberStatus = "APPROVE",
                CreateDTimeUTC = new DateTime(2026, 5, 10, 10, 0, 0, DateTimeKind.Utc),
                CreateBy = "seed",
                LogLUDTimeUTC = new DateTime(2026, 5, 10, 10, 0, 0, DateTimeKind.Utc),
                LogLUBy = "seed"
            });
        }

        if (!await db.Set<CrdMember>().AnyAsync(x => x.MemberNo == "MB5602"))
        {
            db.Set<CrdMember>().Add(new CrdMember
            {
                MemberNo = "MB5602",
                MemberName = "Tran Thi B",
                PhoneNo = "0905600002",
                CarNo = "30A-56002",
                NetworkID = "DEMO",
                OrgID = "DEMO",
                MemberStatus = "APPROVE",
                CreateDTimeUTC = new DateTime(2026, 5, 12, 10, 0, 0, DateTimeKind.Utc),
                CreateBy = "seed",
                LogLUDTimeUTC = new DateTime(2026, 5, 12, 10, 0, 0, DateTimeKind.Utc),
                LogLUBy = "seed"
            });
        }

        // 2. Bảng phụ Crd_MemberVoucher (1 dòng theo đề bài P2)
        if (!await db.Set<Crd_MemberVoucher>().AnyAsync(x => x.MemberNo == "MB5601" && x.VoucherNo == "VC01"))
        {
            db.Set<Crd_MemberVoucher>().Add(new Crd_MemberVoucher
            {
                MemberNo = "MB5601",
                VoucherNo = "VC01",
                NetworkID = "DEMO",
                PointVCRemain = 50.0,
                FlagActive = "1",
                CreateDTimeUTC = new DateTime(2026, 5, 10, 10, 0, 0, DateTimeKind.Utc),
                CreateBy = "seed",
                LogLUDTimeUTC = new DateTime(2026, 5, 10, 10, 0, 0, DateTimeKind.Utc),
                LogLUBy = "seed"
            });
        }

        // 3. Bảng Crd_MemberVoucherTransaction (4 dòng theo đề bài P2)
        if (!await db.Set<Crd_MemberVoucherTransaction>().AnyAsync(x => x.RefNo == "VC01" && x.MemberNo == "MB5601"))
        {
            db.Set<Crd_MemberVoucherTransaction>().AddRange(
                // Dòng 1: MB5601, VC01, PointVCRemain 80.0 (override thành 50.0 khi join Crd_MemberVoucher)
                new Crd_MemberVoucherTransaction
                {
                    AutoId = 1,
                    RefNo = "VC01",
                    MemberNo = "MB5601",
                    CardNo = "CD5601",
                    CardTypeUse = "N",
                    CardTypeInit = "N",
                    DealPointType = "VOUCHERSD",
                    PrmVoucherCode = "PRMVC01",
                    EffDateStart = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    EffDateEnd = new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc),
                    PointChTotal = 100.0,
                    PointVCRemain = 80.0,
                    CreateDate = new DateTime(2026, 5, 10, 0, 0, 0, DateTimeKind.Utc),
                    LogLUDTimeUTC = new DateTime(2026, 5, 10, 10, 0, 0, DateTimeKind.Utc),
                    NetworkID = "DEMO",
                    OrgID = "DEMO",
                    DLCode = "DL01",
                    CreateDTimeUTC = new DateTime(2026, 5, 10, 10, 0, 0, DateTimeKind.Utc),
                    CreateBy = "seed",
                    LogLUBy = "seed"
                },
                // Dòng 2: MB5601, VC02, PointVCRemain 150.0 (không có trong Crd_MemberVoucher -> giữ 150.0)
                new Crd_MemberVoucherTransaction
                {
                    AutoId = 2,
                    RefNo = "VC02",
                    MemberNo = "MB5601",
                    CardNo = "CD5601",
                    CardTypeUse = "N",
                    CardTypeInit = "N",
                    DealPointType = "VOUCHERXM",
                    PrmVoucherCode = "PRMVC02",
                    EffDateStart = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    EffDateEnd = new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc),
                    PointChTotal = 200.0,
                    PointVCRemain = 150.0,
                    CreateDate = new DateTime(2026, 5, 15, 0, 0, 0, DateTimeKind.Utc),
                    LogLUDTimeUTC = new DateTime(2026, 5, 15, 10, 0, 0, DateTimeKind.Utc),
                    NetworkID = "DEMO",
                    OrgID = "DEMO",
                    DLCode = "DL01",
                    CreateDTimeUTC = new DateTime(2026, 5, 15, 10, 0, 0, DateTimeKind.Utc),
                    CreateBy = "seed",
                    LogLUBy = "seed"
                },
                // Dòng 3: MB5601, VC03, PointVCRemain 250.0
                new Crd_MemberVoucherTransaction
                {
                    AutoId = 3,
                    RefNo = "VC03",
                    MemberNo = "MB5601",
                    CardNo = "CD5601",
                    CardTypeUse = "N",
                    CardTypeInit = "N",
                    DealPointType = "VOUCHERSD",
                    PrmVoucherCode = "PRMVC03",
                    EffDateStart = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    EffDateEnd = new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc),
                    PointChTotal = 300.0,
                    PointVCRemain = 250.0,
                    CreateDate = new DateTime(2026, 5, 20, 0, 0, 0, DateTimeKind.Utc),
                    LogLUDTimeUTC = new DateTime(2026, 5, 20, 10, 0, 0, DateTimeKind.Utc),
                    NetworkID = "DEMO",
                    OrgID = "DEMO",
                    DLCode = "DL01",
                    CreateDTimeUTC = new DateTime(2026, 5, 20, 10, 0, 0, DateTimeKind.Utc),
                    CreateBy = "seed",
                    LogLUBy = "seed"
                },
                // Dòng 4: MB5602, VC04, PointVCRemain 100.0
                new Crd_MemberVoucherTransaction
                {
                    AutoId = 4,
                    RefNo = "VC04",
                    MemberNo = "MB5602",
                    CardNo = "CD5602",
                    CardTypeUse = "N",
                    CardTypeInit = "N",
                    DealPointType = "VOUCHERSD",
                    PrmVoucherCode = "PRMVC04",
                    EffDateStart = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    EffDateEnd = new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc),
                    PointChTotal = 150.0,
                    PointVCRemain = 100.0,
                    CreateDate = new DateTime(2026, 5, 12, 0, 0, 0, DateTimeKind.Utc),
                    LogLUDTimeUTC = new DateTime(2026, 5, 12, 10, 0, 0, DateTimeKind.Utc),
                    NetworkID = "DEMO",
                    OrgID = "DEMO",
                    DLCode = "DL01",
                    CreateDTimeUTC = new DateTime(2026, 5, 12, 10, 0, 0, DateTimeKind.Utc),
                    CreateBy = "seed",
                    LogLUBy = "seed"
                }
            );
        }

        await db.SaveChangesAsync();
    }
}
