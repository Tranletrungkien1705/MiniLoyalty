using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;

namespace MiniLoyalty.Models.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — bảng chi tiết báo giá dịch vụ Crd_DealSerRODtl (#021).
//
// Nguồn tra cứu (chỉ đọc):
//   - Model: 12.Dev.Common/idn.Loyalty.Common/Models/Loyalty/Crd_DealSerRODtl.cs:1-41
//   - Biz/SQL: 11.BackEnd/V10/idn.Loyalty.Biz/Card.Deal.cs:12480-12515, :12700-12750
//   - PK: (DealSerRONo, ExpenseType)
// ===========================================================================================

/// <summary>
/// Chi tiết báo giá dịch vụ (Repair Order Detail) — port 1:1 bảng <c>Crd_DealSerRODtl</c>.
/// Đúng 24 thuộc tính theo thiết kế chuẩn. Khóa chính composite (DealSerRONo, ExpenseType).
/// </summary>
public class Crd_DealSerRODtl
{
    public string DealSerRONo { get; set; } = "";             // Mã báo giá (PK component 1)
    public string ExpenseType { get; set; } = "";             // Khoản mục chi phí (PK component 2)
    public string? NetworkID { get; set; }                    // NetworkID
    public string? OrgID { get; set; }                        // OrgID
    public string? PolicyExpenseTypeNo { get; set; }          // Mã chính sách chi phí
    public double? Amount { get; set; }                       // Giá trị chi phí
    public string? FlagPoint { get; set; }                    // Cờ tích điểm ('1'/'0')
    public double? AmountRate { get; set; }                   // Tỷ lệ giá trị
    public string? FunctionName { get; set; }                 // Tên chức năng
    public string? FunctionActionType { get; set; }           // Loại thao tác chức năng
    public string? FunctionRemark { get; set; }               // Ghi chú chức năng
    public string? RefType { get; set; }                      // Loại tham chiếu
    public string? RefCode00 { get; set; }                    // Mã tham chiếu 00
    public string? RefCode01 { get; set; }                    // Mã tham chiếu 01
    public string? RefCode02 { get; set; }                    // Mã tham chiếu 02
    public string? RefCode03 { get; set; }                    // Mã tham chiếu 03
    public string? RefCode04 { get; set; }                    // Mã tham chiếu 04
    public string? RefCode05 { get; set; }                    // Mã tham chiếu 05
    public string? DealSerROStatusDtl { get; set; }           // Trạng thái dòng chi tiết
    public DateTime? CreateDTimeUTC { get; set; }             // Thời điểm tạo UTC
    public string? CreateBy { get; set; }                     // Người tạo
    public DateTime? LogLUDTimeUTC { get; set; }              // Thời điểm cập nhật log UTC
    public string? LogLUBy { get; set; }                      // Người cập nhật log
    public double? AmountForDC { get; set; }                  // Giá trị tính chiết khấu
}

/// <summary>
/// Cấu hình EF cho <see cref="Crd_DealSerRODtl"/> — ánh xạ bảng Crd_DealSerRODtl và khoá chính composite (DealSerRONo, ExpenseType).
/// </summary>
public class Crd_DealSerRODtlConfig : IEntityTypeConfiguration<Crd_DealSerRODtl>
{
    public void Configure(EntityTypeBuilder<Crd_DealSerRODtl> b)
    {
        b.ToTable("Crd_DealSerRODtl");
        b.HasKey(x => new { x.DealSerRONo, x.ExpenseType });
        b.Property(x => x.DealSerRONo).IsRequired();
        b.Property(x => x.ExpenseType).IsRequired();
    }
}

/// <summary>
/// Dữ liệu mẫu cho bảng Crd_DealSerRODtl và bổ sung dữ liệu nền tảng cho kiểm thử (#021). Idempotent.
/// Order => 54 (chạy sau Crd_DealSerROSeeder Order=53).
/// </summary>
public class Crd_DealSerRODtlSeeder : IPortSeeder
{
    public int Order => 93;

    public async Task SeedAsync(AppDbContext db)
    {
        // 1. Đảm bảo Mst_PolicyMoneyToPointService có P01 và Mst_PolicyMoneyToPointServiceDtl có cấu hình cho thẻ 'VIP'
        var policyHdrSet = db.Set<Mst_PolicyMoneyToPointService>();
        if (!await policyHdrSet.AnyAsync(x => x.PolicyCode == "P01"))
        {
            policyHdrSet.Add(new Mst_PolicyMoneyToPointService
            {
                PolicyCode = "P01",
                NetworkID = "DEMO",
                EffDateStart = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EffDateEnd = new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc),
                FlagActive = "1",
                Remark = "Chính sách quy đổi P01",
                LogLUBy = "seed",
                LogLUDTimeUTC = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var policyDtlSet = db.Set<Mst_PolicyMoneyToPointServiceDtl>();
        if (!await policyDtlSet.AnyAsync(x => x.PolicyCode == "P01" && x.CardType == "VIP"))
        {
            policyDtlSet.Add(new Mst_PolicyMoneyToPointServiceDtl
            {
                PolicyCode = "P01",
                CardType = "VIP",
                NetworkID = "DEMO",
                ConvertValue = 10000,
                ConvertPoint = 1,
                ValueRankCardType = 10,
                DiscountRate = 0,
                FlagActive = "1",
                Remark = "Chính sách quy đổi dịch vụ thẻ VIP",
                LogLUBy = "seed",
                LogLUDTimeUTC = DateTime.Parse("2026-10-10T10:00:00Z").ToUniversalTime()
            });
            await db.SaveChangesAsync();
        }

        // 2. Đảm bảo tham số UNITPOINTTOMONEY tồn tại trong Mst_ParamSys
        var paramSysSet = db.Set<Mst_ParamSys>();
        if (!await paramSysSet.AnyAsync(x => x.ParamCode == "UNITPOINTTOMONEY"))
        {
            paramSysSet.Add(new Mst_ParamSys
            {
                ParamCode = "UNITPOINTTOMONEY",
                NetworkID = "DEMO",
                ParamValue = "1",
                FlagActive = "1",
                LogLUBy = "seed",
                LogLUDTimeUTC = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        // 3. Đảm bảo hội viên MEM01 có đủ thông tin CarNo, VIN, AmountEnd, CardTypeUse cho kiểm thử
        var mem = await db.CrdMembers.FirstOrDefaultAsync(m => m.MemberNo == "MEM01");
        if (mem != null)
        {
            mem.VIN = "VIN000000001";
            mem.CarNo = "30A-99999";
            mem.AmountEnd = 50_000_000;
            mem.CardTypeUse = "VIP";
            mem.CardTypeInit ??= "VIP";
            mem.MemberStatus = "APPROVE";
            mem.RegisStatus = "FINISH";
        }
        else
        {
            db.CrdMembers.Add(new CrdMember
            {
                MemberNo = "MEM01",
                NetworkID = "DEMO",
                OrgID = "DEMO",
                MemberName = "Nguyễn Văn VIP",
                PhoneNo = "0901000001",
                CarNo = "30A-99999",
                VIN = "VIN000000001",
                MemberStatus = "APPROVE",
                RegisStatus = "FINISH",
                CardTypeUse = "VIP",
                CardTypeInit = "VIP",
                AmountEnd = 50_000_000,
                CreateDTimeUTC = DateTime.UtcNow,
                CreateBy = "seed",
                LogLUDTimeUTC = DateTime.UtcNow,
                LogLUBy = "seed"
            });
        }
        await db.SaveChangesAsync();

        // 4. Đảm bảo thẻ CARD01 tồn tại cho hội viên MEM01
        var card = await db.CrdCards.FirstOrDefaultAsync(c => c.CardNo == "CARD01");
        if (card == null)
        {
            db.CrdCards.Add(new CrdCard
            {
                CardNo = "CARD01",
                MemberNo = "MEM01",
                NetworkID = "DEMO",
                CardTypeUse = "VIP",
                CardTypeInit = "VIP",
                CardStatus = "APPROVE",
                EffDateStart = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EffDateEnd = new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc),
                CreateDTimeUTC = DateTime.UtcNow,
                CreateBy = "seed",
                LogLUDTimeUTC = DateTime.UtcNow,
                LogLUBy = "seed"
            });
            await db.SaveChangesAsync();
        }
        else
        {
            card.EffDateStart ??= new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            card.EffDateEnd ??= new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc);
            await db.SaveChangesAsync();
        }

        // 3. Seed dữ liệu mẫu cho Crd_DealSerRODtl
        var dtlSet = db.Set<Crd_DealSerRODtl>();
        if (await dtlSet.AnyAsync()) return;

        dtlSet.AddRange(
            new Crd_DealSerRODtl
            {
                DealSerRONo = "RO001",
                ExpenseType = "EX01",
                NetworkID = "DEMO",
                OrgID = "ORG01",
                PolicyExpenseTypeNo = "PET01",
                Amount = 1000000,
                FlagPoint = "1",
                AmountRate = 1.0,
                FunctionName = "Crd_DealSerRO_Add",
                FunctionActionType = "ADD",
                RefType = "CRD_DEALSERRO",
                DealSerROStatusDtl = "APPROVE",
                CreateDTimeUTC = DateTime.Parse("2026-01-01T10:00:00Z").ToUniversalTime(),
                CreateBy = "seed",
                LogLUDTimeUTC = DateTime.Parse("2026-01-01T10:00:00Z").ToUniversalTime(),
                LogLUBy = "seed",
                AmountForDC = 1000000
            },
            new Crd_DealSerRODtl
            {
                DealSerRONo = "RO002",
                ExpenseType = "EX01",
                NetworkID = "DEMO",
                OrgID = "ORG01",
                PolicyExpenseTypeNo = "PET01",
                Amount = 2000000,
                FlagPoint = "1",
                AmountRate = 1.0,
                FunctionName = "Crd_DealSerRO_Add",
                FunctionActionType = "ADD",
                RefType = "CRD_DEALSERRO",
                DealSerROStatusDtl = "APPROVE",
                CreateDTimeUTC = DateTime.Parse("2026-01-02T10:00:00Z").ToUniversalTime(),
                CreateBy = "seed",
                LogLUDTimeUTC = DateTime.Parse("2026-01-02T10:00:00Z").ToUniversalTime(),
                LogLUBy = "seed",
                AmountForDC = 2000000
            },
            new Crd_DealSerRODtl
            {
                DealSerRONo = "RO003",
                ExpenseType = "EX02",
                NetworkID = "DEMO",
                OrgID = "ORG02",
                PolicyExpenseTypeNo = "PET02",
                Amount = 3000000,
                FlagPoint = "1",
                AmountRate = 1.0,
                FunctionName = "Crd_DealSerRO_Add",
                FunctionActionType = "ADD",
                RefType = "CRD_DEALSERRO",
                DealSerROStatusDtl = "PENDING",
                CreateDTimeUTC = DateTime.Parse("2026-01-03T10:00:00Z").ToUniversalTime(),
                CreateBy = "seed",
                LogLUDTimeUTC = DateTime.Parse("2026-01-03T10:00:00Z").ToUniversalTime(),
                LogLUBy = "seed",
                AmountForDC = 3000000
            }
        );

        await db.SaveChangesAsync();
    }
}
