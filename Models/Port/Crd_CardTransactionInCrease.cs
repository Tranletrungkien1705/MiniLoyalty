using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;

namespace MiniLoyalty.Models.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — nhóm báo cáo "Giao dịch tăng điểm" (#015).
//
// Nguồn tra cứu (chỉ đọc):
//   - Gate: Crd_CardTransactionInCreaseController.cs:33-117 (Search)
//   - API:  CrdCardTransactionInCreaseController.cs:20-87 (WA_Crd_CardTransactionInCrease_Get)
//   - BIZ:  Report.cs:11903-12036 (WAS_Crd_CardTransactionInCrease_Get)
//           Report.cs:12037-12183 (Crd_CardTransactionInCrease_Get)
//           Report.cs:12184-12523 (Crd_CardTransactionInCrease_GetX — bản LIVE)
//   - Model cột (từ điển cột):
//           Crd_CardTransactionInCrease.cs:9-31
//           Crd_CardTransactionInCreaseInst.cs:9-34
//
// GHI CHÚ QUAN TRỌNG:
//   - Crd_CardTransaction (bảng gốc) đã có entity tại Models/PortLoyalty.cs (CrdCardTransaction)
//     và ánh xạ sẵn trong AppDbContext.cs — DÙNG LẠI, KHÔNG tạo entity thứ hai cùng bảng (bài học A.24).
//   - Crd_Member (CrdMember) và Mst_DealerCP (Mst_DealerCP) cũng đã có entity — DÙNG LẠI.
//   - 2 bảng kết quả Crd_CardTransactionInCrease / Crd_CardTransactionInCreaseInst KHÔNG phải bảng DB thật
//     (là bảng tạm #tbl_... của nguồn) nên KHÔNG khai IEntityTypeConfiguration — chỉ là DTO trả về.
//   - 2 bảng phụ RptPE_CrdTransationDeCreaseInst / RptPE_CrdTransationInCreaseHist là bảng thật của nguồn,
//     được join trong SQL (Report.cs:12355, :12406) — khai entity + cấu hình EF.
// ===========================================================================================

/// <summary>
/// Bảng phụ RptPE_CrdTransationDeCreaseInst — chi tiết khấu trừ điểm theo giao dịch tăng điểm.
/// Nguồn dùng ở Report.cs:12355-12356 (join cct_ic.AutoId = rpectdci.RefNoIC) và :12331, :12333, :12364.
/// Không có DDL trong repo nguồn -> dùng cột xuất hiện trong SQL làm từ điển cột.
/// </summary>
public class RptPE_CrdTransationDeCreaseInst
{
    /// <summary>Mã giao dịch tăng điểm (khoá chính) — Report.cs:12356 (RefNoIC).</summary>
    public long RefNoIC { get; set; }

    /// <summary>Mã giao dịch tiêu điểm tương ứng — Report.cs:12331 (RefNoDC).</summary>
    public long? RefNoDC { get; set; }

    /// <summary>Số điểm tiêu — Report.cs:12333 (PointDeCrease).</summary>
    public double? PointDeCrease { get; set; }

    /// <summary>Kỳ xét hạng (yyyy-MM) — Report.cs:12364 (PeriodMonth).</summary>
    public string? PeriodMonth { get; set; }
}

/// <summary>
/// Bảng phụ RptPE_CrdTransationInCreaseHist — lịch sử điểm hết hạn của giao dịch tăng điểm.
/// Nguồn dùng ở Report.cs:12406-12408 (left join t.AutoId = rpectich.RefNo) và :12403, :12409.
/// Không có DDL trong repo nguồn -> dùng cột xuất hiện trong SQL làm từ điển cột.
/// </summary>
public class RptPE_CrdTransationInCreaseHist
{
    /// <summary>Mã giao dịch tăng điểm (khoá chính) — Report.cs:12407 (RefNo).</summary>
    public long RefNo { get; set; }

    /// <summary>Điểm hết hạn — Report.cs:12403 (PointExpiry).</summary>
    public double? PointExpiry { get; set; }

    /// <summary>Ngày hết hạn sử dụng điểm — Report.cs:12409 (PointExpiryDTime).</summary>
    public DateTime? PointExpiryDTime { get; set; }
}

/// <summary>Cấu hình EF cho <see cref="RptPE_CrdTransationDeCreaseInst"/> (khoá chính RefNoIC).</summary>
public class RptPE_CrdTransationDeCreaseInstConfiguration : IEntityTypeConfiguration<RptPE_CrdTransationDeCreaseInst>
{
    public void Configure(EntityTypeBuilder<RptPE_CrdTransationDeCreaseInst> builder)
    {
        builder.ToTable("RptPE_CrdTransationDeCreaseInst");
        builder.HasKey(x => x.RefNoIC);
    }
}

/// <summary>Cấu hình EF cho <see cref="RptPE_CrdTransationInCreaseHist"/> (khoá chính RefNo).</summary>
public class RptPE_CrdTransationInCreaseHistConfiguration : IEntityTypeConfiguration<RptPE_CrdTransationInCreaseHist>
{
    public void Configure(EntityTypeBuilder<RptPE_CrdTransationInCreaseHist> builder)
    {
        builder.ToTable("RptPE_CrdTransationInCreaseHist");
        builder.HasKey(x => x.RefNo);
    }
}

/// <summary>
/// Seeder dữ liệu mẫu cho nhóm báo cáo giao dịch tăng điểm (#015).
/// Order => 53 (đề bài gợi ý 35 nhưng 35 đã bị Mst_ExpenseTypeSeeder chiếm; 51 đã bị Prm_CarNewSeeder chiếm;
/// chọn 53 chưa dùng trong 10–99 theo bài học A.4).
/// Idempotent: kiểm tra đã có dòng Crd_CardTransaction với RefNo = 'IC015-01' trước khi thêm.
/// </summary>
public class Crd_CardTransactionInCreaseSeeder : IPortSeeder
{
    // Đề bài gợi ý Order 35 nhưng 35 đã dùng; 51 đã dùng; chọn 53 chưa dùng trong 10–99 (bài học A.4).
    public int Order => 52;

    public async Task SeedAsync(AppDbContext db)
    {
        const string net = "DEMO";
        const string org = "DEMO";
        var now = DateTime.UtcNow;

        // 1. Đảm bảo có hội viên MEM01 (nếu seeder khác chưa tạo).
        if (!await db.Set<CrdMember>().AnyAsync(x => x.MemberNo == "MEM01"))
        {
            db.Set<CrdMember>().Add(new CrdMember
            {
                MemberNo = "MEM01",
                NetworkID = net,
                OrgID = org,
                MemberName = "Nguyễn Văn A",
                PhoneNo = "0901234567",
                CarNo = "30A-99999",
                IDCardNo = "001234567890",
                DLCodeRegis = "VS058",
                CustomerCode = "KH001",
                CardTypeUse = LoyaltyConst.CardType.N,
                CardTypeInit = LoyaltyConst.CardType.N,
                MemberStatus = LoyaltyConst.MemberStatus.Approve,
                RegisStatus = LoyaltyConst.RegisStatus.Finish,
                RegistrationDate = now.AddMonths(-6),
                CreateDTimeUTC = now.AddMonths(-6),
                CreateBy = "seed",
                LogLUDTimeUTC = now.AddMonths(-6),
                LogLUBy = "seed"
            });
        }

        // 2. Đảm bảo có đại lý VS058 (Mst_DealerCP) — nguồn inner join cct.DLCode = md.DLCPCode.
        if (!await db.Set<Mst_DealerCP>().AnyAsync(x => x.DLCPCode == "VS058"))
        {
            db.Set<Mst_DealerCP>().Add(new Mst_DealerCP
            {
                DLCPCode = "VS058",
                NetworkID = net,
                DLCPCodeParent = "HTV",
                DLCPBUCode = "BU01",
                DLCPBUPattern = "PAT01",
                DLCPLevel = "2",
                DLCPName = "Đại lý VS058",
                DLCPPhoneNo = "0243555555",
                FlagRoot = "0",
                FlagActive = "1",
                FlagBirthPoints = "1",
                Remark = "Đại lý mẫu #015",
                LogLUDTimeUTC = now,
                LogLUBy = "seed"
            });
        }

        // 3. Giao dịch tăng điểm (AmountChTotal > 0) — điều kiện nền của nguồn (Report.cs:12271).
        //    CreateDTimeUTC phải >= '2023-06-30 17:00:00' (Report.cs:12272).
        if (!await db.Set<CrdCardTransaction>().AnyAsync(x => x.RefNo == "IC015-01"))
        {
            db.Set<CrdCardTransaction>().Add(new CrdCardTransaction
            {
                RefNo = "IC015-01",
                ProjectCode = "LOYALTY",
                NetworkID = net,
                OrgID = org,
                DLCode = "VS058",
                MemberNo = "MEM01",
                CardNo = "CARD015-01",
                CardSourceCode = LoyaltyConst.CardSourceCode.New,
                CardTypeUse = LoyaltyConst.CardType.N,
                CardTypeInit = LoyaltyConst.CardType.N,
                DealPointType = LoyaltyConst.DealPointType.PointIncrease,
                EffDateStart = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EffDateEnd = new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc),
                PointChTotal = 1_000d,
                AmountChTotal = 1_000d,
                PointChRankTotal = 1_000d,
                PointChTotalInit = 1_000d,
                AmountChTotalInit = 1_000d,
                PointChTotalDiffer = 0d,
                AmountChTotalDiffer = 0d,
                AmountAvailPrevious = 0d,
                PointRef = 1_000d,
                AmountRef = 10_000_000d,
                PolicyPointUnit = 1d,
                PolicyAmountUnit = 10_000d,
                PolicyCode = "RP2026",
                ParamPointUnit = 1d,
                ParamAmountUnit = 1d,
                ParamCode = "STD",
                FlagViewUI = "1",
                FlagPoint = "1",
                FlagAmount = "1",
                FlagPr = "0",
                FunctionName = "Crd_CardTransactionInCrease_Seed",
                FunctionActionType = "ADD",
                RefType = "RO",
                PointExpiryDTime = new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc),
                CreateDTimeUTC = new DateTime(2026, 3, 15, 9, 0, 0, DateTimeKind.Utc),
                CreateBy = "seed",
                LUDTimeUTC = now,
                LUBy = "seed",
                LogLUDTimeUTC = new DateTime(2026, 3, 15, 9, 0, 0, DateTimeKind.Utc),
                LogLUBy = "seed",
                DLCodeExceptionally = null
            });
        }

        // 4. Giao dịch tăng điểm thứ 2 (MemberNo = MEM01, DLCode = VS058) — để ca lọc theo thời gian ra khác số dòng.
        if (!await db.Set<CrdCardTransaction>().AnyAsync(x => x.RefNo == "IC015-02"))
        {
            db.Set<CrdCardTransaction>().Add(new CrdCardTransaction
            {
                RefNo = "IC015-02",
                ProjectCode = "LOYALTY",
                NetworkID = net,
                OrgID = org,
                DLCode = "VS058",
                MemberNo = "MEM01",
                CardNo = "CARD015-02",
                CardSourceCode = LoyaltyConst.CardSourceCode.New,
                CardTypeUse = LoyaltyConst.CardType.N,
                CardTypeInit = LoyaltyConst.CardType.N,
                DealPointType = LoyaltyConst.DealPointType.PointIncrease,
                EffDateStart = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EffDateEnd = new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc),
                PointChTotal = 2_000d,
                AmountChTotal = 2_000d,
                PointChRankTotal = 2_000d,
                PointChTotalInit = 2_000d,
                AmountChTotalInit = 2_000d,
                PointChTotalDiffer = 0d,
                AmountChTotalDiffer = 0d,
                AmountAvailPrevious = 0d,
                PointRef = 2_000d,
                AmountRef = 20_000_000d,
                PolicyPointUnit = 1d,
                PolicyAmountUnit = 10_000d,
                PolicyCode = "RP2026",
                ParamPointUnit = 1d,
                ParamAmountUnit = 1d,
                ParamCode = "STD",
                FlagViewUI = "1",
                FlagPoint = "1",
                FlagAmount = "1",
                FlagPr = "0",
                FunctionName = "Crd_CardTransactionInCrease_Seed",
                FunctionActionType = "ADD",
                RefType = "RO",
                PointExpiryDTime = new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc),
                CreateDTimeUTC = new DateTime(2026, 6, 20, 9, 0, 0, DateTimeKind.Utc),
                CreateBy = "seed",
                LUDTimeUTC = now,
                LUBy = "seed",
                LogLUDTimeUTC = new DateTime(2026, 6, 20, 9, 0, 0, DateTimeKind.Utc),
                LogLUBy = "seed",
                DLCodeExceptionally = null
            });
        }

        await db.SaveChangesAsync();

        // 5. Bảng phụ RptPE_CrdTransationDeCreaseInst — chi tiết khấu trừ cho IC015-01 (RefNoIC).
        //    Nguồn join cct_ic.AutoId = rpectdci.RefNoIC (Report.cs:12356). AutoId là identity nên tra lại sau SaveChanges.
        var ic01 = await db.Set<CrdCardTransaction>().FirstOrDefaultAsync(x => x.RefNo == "IC015-01");
        var ic02 = await db.Set<CrdCardTransaction>().FirstOrDefaultAsync(x => x.RefNo == "IC015-02");
        if (ic01 != null && !await db.Set<RptPE_CrdTransationDeCreaseInst>().AnyAsync(x => x.RefNoIC == ic01.AutoId))
        {
            db.Set<RptPE_CrdTransationDeCreaseInst>().Add(new RptPE_CrdTransationDeCreaseInst
            {
                RefNoIC = ic01.AutoId,
                RefNoDC = ic02?.AutoId,
                PointDeCrease = 300d,
                PeriodMonth = "2026-03"
            });
        }

        // 6. Bảng phụ RptPE_CrdTransationInCreaseHist — điểm hết hạn cho IC015-02 (RefNo).
        if (ic02 != null && !await db.Set<RptPE_CrdTransationInCreaseHist>().AnyAsync(x => x.RefNo == ic02.AutoId))
        {
            db.Set<RptPE_CrdTransationInCreaseHist>().Add(new RptPE_CrdTransationInCreaseHist
            {
                RefNo = ic02.AutoId,
                PointExpiry = 200d,
                PointExpiryDTime = new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc)
            });
        }

        await db.SaveChangesAsync();
    }
}
