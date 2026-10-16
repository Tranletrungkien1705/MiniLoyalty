using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;

namespace MiniLoyalty.Models.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — bảng Crd_DealSerRO (#019).
//
// Nguồn tra cứu (chỉ đọc):
//   - Gate:     13.ClientGate/V10/idn.Loyalty.WebAPI/Controllers/Crd_DealSerROController.cs:29-109
//   - Model:    12.Dev.Common/idn.Loyalty.Common/Models/Loyalty/Crd_DealSerRO.cs:1-58 (45 thuộc tính)
//   - Biz/SQL:  11.BackEnd/V10/idn.Loyalty.Biz/Card.Deal.cs:99-581 (WAS_Crd_DealSerRO_Get, Crd_DealSerRO_Get, Crd_DealSerRO_GetX)
//   - PK:       Card.Deal.cs:43 (Crd_DealSerRO_CheckDB) và FK trong Migrate/Script.HuongTTT/20250918.z11.Crd_MemberVoucherTransaction.sql:220
//
// ĐẶC ĐIỂM BẢNG (đúng 45 thuộc tính theo model nguồn):
//   DealSerRONo (PK, string not null), NetworkID, OrgID, DLCode, MemberNo, CardNo, DealPointType,
//   CardType, CardTypeExpect, ParamCode, PolicyCode, TotalAmountRO, AmountRO, AmountUse,
//   PointTotal, AmountTotal, PointEnd, AmountEnd, EffDateEnd, PointAvail, PolicyPointUnit,
//   PolicyAmountUnit, ParamPointUnit, ParamAmountUnit, DealSerROStatus, FlagOnlyPoint, Remark,
//   CreateDTimeUTC, CreateBy, LUDTimeUTC, LUBy, ApprBy, ApprDTimeUTC, UpdateDTimeUTC, UpdateBy,
//   LogLUDTimeUTC, LogLUBy, PlateNo, VIN, PointRankTotal, PointConsumptionPrm, PointVoucher,
//   PointActualVoucher, TotalDiscount, TotalAmountForDC.
//   (Thuộc tính RankPolicyCode ở dòng :18 bị comment out trong mã nguồn nên không đưa vào).
// ===========================================================================================

/// <summary>
/// Báo giá dịch vụ sửa chữa / bảo dưỡng (Repair Order) — port 1:1 bảng <c>Crd_DealSerRO</c>.
/// Model nguồn: Crd_DealSerRO.cs:1-58. PK = DealSerRONo.
/// </summary>
public class Crd_DealSerRO
{
    public string DealSerRONo { get; set; } = "";        // Số báo giá dịch vụ (PK) — Crd_DealSerRO.cs:11
    public string? NetworkID { get; set; }               // NetworkID — :12
    public string? OrgID { get; set; }                   // OrgID — :13
    public string? DLCode { get; set; }                  // Mã đại lý — :14
    public string? MemberNo { get; set; }                // Mã hội viên — :15
    public string? CardNo { get; set; }                  // Số thẻ — :16
    public string? DealPointType { get; set; }           // Loại giao dịch — :17
    public string? CardType { get; set; }                // Hạng thẻ — :19
    public string? CardTypeExpect { get; set; }          // Hạng thẻ dự kiến — :20
    public string? ParamCode { get; set; }               // Mã tham số — :21
    public string? PolicyCode { get; set; }              // Mã chính sách — :22
    public double? TotalAmountRO { get; set; }           // Giá trị báo giá — :23
    public double? AmountRO { get; set; }                // Giá trị báo giá tích điểm / Tiền tích xét hạng — :24
    public double? AmountUse { get; set; }               // Số tiền sử dụng từ báo giá / Tiền sử dụng — :25
    public double? PointTotal { get; set; }              // Điểm được tặng khi làm báo giá / Điểm tích tiêu dùng — :26
    public double? AmountTotal { get; set; }             // Tiền được tặng khi làm báo giá / Tiền tích tiêu dùng — :27
    public double? PointEnd { get; set; }                // Điểm tích lũy — :28
    public double? AmountEnd { get; set; }               // Giá trị tích lũy — :29
    public DateTime? EffDateEnd { get; set; }            // Ngày hết hạn — :30
    public double? PointAvail { get; set; }              // Điểm trong kỳ của thẻ — :31
    public double? PolicyPointUnit { get; set; }         // Điểm đơn vị theo chính sách — :32
    public double? PolicyAmountUnit { get; set; }        // :33
    public double? ParamPointUnit { get; set; }          // :34
    public double? ParamAmountUnit { get; set; }         // :35
    public string? DealSerROStatus { get; set; }         // :36
    public string? FlagOnlyPoint { get; set; }           // :37
    public string? Remark { get; set; }                  // :38
    public DateTime? CreateDTimeUTC { get; set; }        // :39
    public string? CreateBy { get; set; }                // :40
    public DateTime? LUDTimeUTC { get; set; }            // :41
    public string? LUBy { get; set; }                    // :42
    public string? ApprBy { get; set; }                  // :43
    public DateTime? ApprDTimeUTC { get; set; }          // :44
    public DateTime? UpdateDTimeUTC { get; set; }        // :45
    public string? UpdateBy { get; set; }                // :46
    public DateTime? LogLUDTimeUTC { get; set; }         // Thời điểm cập nhật log UTC — :47
    public string? LogLUBy { get; set; }                 // Người cập nhật log — :48
    public string? PlateNo { get; set; }                 // Biển số xe từ CarSV — :49
    public string? VIN { get; set; }                     // VIN từ CarSV — :50
    public double? PointRankTotal { get; set; }          // Điểm tích xét hạng — :51
    public double? PointConsumptionPrm { get; set; }     // Điểm tích tiêu dùng khuyến mại — :52
    public double? PointVoucher { get; set; }            // Tổng điểm voucher sử dụng - hạng sử dụng — :53
    public double? PointActualVoucher { get; set; }      // Tổng điểm voucher sử dụng - hạng thực — :54
    public double? TotalDiscount { get; set; }           // Tổng giá trị chiết khấu — :55
    public double? TotalAmountForDC { get; set; }        // Tổng giá trị theo RO dùng để tính chiết khấu — :56
}

/// <summary>Cấu hình EF cho <see cref="Crd_DealSerRO"/> — ánh xạ bảng Crd_DealSerRO và khoá chính DealSerRONo.</summary>
public class Crd_DealSerROConfig : IEntityTypeConfiguration<Crd_DealSerRO>
{
    public void Configure(EntityTypeBuilder<Crd_DealSerRO> b)
    {
        b.ToTable("Crd_DealSerRO");
        b.HasKey(x => x.DealSerRONo);
        b.Property(x => x.DealSerRONo).IsRequired();
    }
}

/// <summary>
/// Dữ liệu mẫu cho bảng Crd_DealSerRO (P2). Idempotent: kiểm tra AnyAsync trước khi Add.
/// Order => 53 (chưa dùng trong dải 10–99).
/// </summary>
public class Crd_DealSerROSeeder : IPortSeeder
{
    public int Order => 53;

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<Crd_DealSerRO>().AnyAsync()) return;

        db.Set<Crd_DealSerRO>().AddRange(
            new Crd_DealSerRO
            {
                DealSerRONo = "RO001",
                MemberNo = "MB01",
                CardNo = "CARD01",
                DLCode = "DL01",
                OrgID = "ORG01",
                NetworkID = "DEMO",
                TotalAmountRO = 1000000,
                AmountRO = 1000000,
                PointTotal = 100,
                DealSerROStatus = "APPROVE",
                LogLUDTimeUTC = DateTime.Parse("2026-01-01T10:00:00Z").ToUniversalTime(),
                LogLUBy = "seed"
            },
            new Crd_DealSerRO
            {
                DealSerRONo = "RO002",
                MemberNo = "MB01",
                CardNo = "CARD02",
                DLCode = "DL01",
                OrgID = "ORG01",
                NetworkID = "DEMO",
                TotalAmountRO = 2000000,
                AmountRO = 2000000,
                PointTotal = 200,
                DealSerROStatus = "APPROVE",
                LogLUDTimeUTC = DateTime.Parse("2026-01-02T10:00:00Z").ToUniversalTime(),
                LogLUBy = "seed"
            },
            new Crd_DealSerRO
            {
                DealSerRONo = "RO003",
                MemberNo = "MB02",
                CardNo = "CARD03",
                DLCode = "DL02",
                OrgID = "ORG02",
                NetworkID = "DEMO",
                TotalAmountRO = 3000000,
                AmountRO = 3000000,
                PointTotal = 300,
                DealSerROStatus = "PENDING",
                LogLUDTimeUTC = DateTime.Parse("2026-01-03T10:00:00Z").ToUniversalTime(),
                LogLUBy = "seed"
            }
        );

        await db.SaveChangesAsync();
    }
}
