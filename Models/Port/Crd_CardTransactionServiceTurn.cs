using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MiniLoyalty.Data;

namespace MiniLoyalty.Models.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — Crd_CardTransactionServiceTurn (#017).
//
// Nguồn tra cứu (chỉ đọc):
//   - Gate:     13.ClientGate/V10/idn.Loyalty.WebAPI/Controllers/Crd_CardTransactionServiceTurnController.cs:33-118
//   - API:      11.BackEnd/V10/idn.Loyalty.Biz.Web/Controllers/CrdCardTransactionServiceTurnController.cs:20-87
//   - Biz LIVE: 11.BackEnd/V10/idn.Loyalty.Biz/Report.cs:12527-12776 (WAS_Crd_CardTransactionServiceTurn_Get,
//               Crd_CardTransactionServiceTurn_Get, Crd_CardTransactionServiceTurn_GetX)
//   - Model:    12.Dev.Common/idn.Loyalty.Common/Models/Crd_CardTransactionServiceTurn.cs:1-25 (13 thuộc tính)
//   - DDL:      Nguồn không có bảng vật lý Crd_CardTransactionServiceTurn trong DB; đây là kết quả truy vấn
//               tổng hợp (SELECT) từ Crd_DealSerRO, Crd_CardTransaction (SERVICETURN & POINTUSE),
//               Crd_DealSerRODtl (ROREPAIR) và Mst_DealerCP.
//               Dùng DTO [NotMapped] theo đúng reviewer note (không tạo bảng giả trong DB).
// ===========================================================================================

/// <summary>
/// Báo cáo giao dịch lượt dịch vụ thẻ — port 1:1 Crd_CardTransactionServiceTurn.
/// Model nguồn: Crd_CardTransactionServiceTurn.cs:1-25 (13 thuộc tính).
/// </summary>
[NotMapped]
public class Crd_CardTransactionServiceTurn
{
    public long? STT { get; set; }                           // STT (số thứ tự) — Crd_CardTransactionServiceTurn.cs:11
    public string? DLCode { get; set; }                      // Mã đại lý — :12
    public string? DLCPName { get; set; }                    // Tên đại lý — :13
    public long AutoId { get; set; }                         // Mã giao dịch (PK) — :14
    public string? CreateDate { get; set; }                  // Thời gian giao dịch (yyyy-MM-dd quy đổi giờ VN UTC+7) — :15
    public string? CardNo { get; set; }                      // Mã kỳ xét hạng — :16
    public string? MemberNo { get; set; }                    // Mã hội viên — :17
    public string? CardTypeUse { get; set; }                 // Hạng sử dụng — :18
    public string? RefNo { get; set; }                       // Số tham chiếu (số báo giá RO) — :19
    public double? AmountAvailPrevious { get; set; }         // Điểm trước tiêu — :20
    public double? AmountChTotal { get; set; }               // Điểm tiêu trong lượt dịch vụ — :21
    public double? Amount { get; set; }                      // Tổng tiền KH (từ Crd_DealSerRODtl.Amount) — :22
    public double? QtyVisitChTotal { get; set; }             // Số lượt DV — :23
    public DateTime? CreateDTimeUTC { get; set; }            // Thời điểm tạo UTC (phục vụ lọc & sắp xếp nội bộ)
}

/// <summary>
/// Seeder dữ liệu mẫu cho các bảng gốc phục vụ báo cáo Crd_CardTransactionServiceTurn (#017).
/// Seed các bảng thật: Mst_DealerCP, Crd_DealSerRO, Crd_DealSerRODtl, CrdCardTransaction.
/// Order => 97 (chưa sử dụng trong khoảng 10–99).
/// Idempotent: kiểm tra dữ liệu trước khi thêm.
/// </summary>
public class Crd_CardTransactionServiceTurnSeeder : IPortSeeder
{
    public int Order => 97;

    public async Task SeedAsync(AppDbContext db)
    {
        // 1. Mst_DealerCP: DL_HN, DL_SG
        var dealerSet = db.Set<Mst_DealerCP>();
        if (!await dealerSet.AnyAsync(x => x.DLCPCode == "DL_HN"))
        {
            dealerSet.Add(new Mst_DealerCP
            {
                DLCPCode = "DL_HN",
                DLCPName = "Đại lý Hà Nội",
                NetworkID = "1001",
                FlagActive = "1",
                LogLUBy = "seed",
                LogLUDTimeUTC = DateTime.UtcNow
            });
        }
        if (!await dealerSet.AnyAsync(x => x.DLCPCode == "DL_SG"))
        {
            dealerSet.Add(new Mst_DealerCP
            {
                DLCPCode = "DL_SG",
                DLCPName = "Đại lý Sài Gòn",
                NetworkID = "1001",
                FlagActive = "1",
                LogLUBy = "seed",
                LogLUDTimeUTC = DateTime.UtcNow
            });
        }
        await db.SaveChangesAsync();

        // 2. Crd_DealSerRO: RO_ST_001 .. RO_ST_006
        var roSet = db.Set<Crd_DealSerRO>();
        void AddROIfNotExists(string roNo, string dlCode, string memberNo, string cardNo)
        {
            if (!roSet.Any(x => x.DealSerRONo == roNo))
            {
                roSet.Add(new Crd_DealSerRO
                {
                    DealSerRONo = roNo,
                    DLCode = dlCode,
                    MemberNo = memberNo,
                    CardNo = cardNo,
                    DealSerROStatus = "APPROVE",
                    NetworkID = "1001",
                    LogLUBy = "seed",
                    LogLUDTimeUTC = DateTime.UtcNow
                });
            }
        }

        AddROIfNotExists("RO_ST_001", "DL_HN", "MB_ST_01", "CARD_ST_01");
        AddROIfNotExists("RO_ST_002", "DL_SG", "MB_ST_02", "CARD_ST_02");
        AddROIfNotExists("RO_ST_003", "DL_HN", "MB_ST_01", "CARD_ST_01");
        AddROIfNotExists("RO_ST_004", "DL_HN", "MB_ST_01", "CARD_ST_01"); // Nhiều POINTUSE
        AddROIfNotExists("RO_ST_005", "DL_UNKNOWN", "MB_ST_01", "CARD_ST_01"); // ĐL không có trong Mst_DealerCP
        AddROIfNotExists("RO_ST_006", "DL_HN", "MB_ST_01", "CARD_ST_01"); // Trước mốc 2023-06-30 17:00:00
        await db.SaveChangesAsync();

        // 3. Crd_DealSerRODtl: chi phí sửa chữa ROREPAIR
        var rodSet = db.Set<Crd_DealSerRODtl>();
        void AddRODtlIfNotExists(string roNo, double amount)
        {
            if (!rodSet.Any(x => x.DealSerRONo == roNo && x.ExpenseType == "ROREPAIR"))
            {
                rodSet.Add(new Crd_DealSerRODtl
                {
                    DealSerRONo = roNo,
                    ExpenseType = "ROREPAIR",
                    Amount = amount,
                    NetworkID = "1001",
                    LogLUBy = "seed",
                    LogLUDTimeUTC = DateTime.UtcNow
                });
            }
        }

        AddRODtlIfNotExists("RO_ST_001", 1500000);
        AddRODtlIfNotExists("RO_ST_002", 2500000);
        AddRODtlIfNotExists("RO_ST_003", 0);
        AddRODtlIfNotExists("RO_ST_004", 800000);
        AddRODtlIfNotExists("RO_ST_005", 500000);
        AddRODtlIfNotExists("RO_ST_006", 600000);
        await db.SaveChangesAsync();

        // 4. CrdCardTransaction: SERVICETURN & POINTUSE
        var cctSet = db.Set<CrdCardTransaction>();
        void AddCctIfNotExists(long autoId, string refNo, string dealPointType, string dlCode,
            double? amountAvailPrev, double? amountChTotal, double? qtyVisit, DateTime createDTimeUtc)
        {
            if (!cctSet.Any(x => x.AutoId == autoId))
            {
                cctSet.Add(new CrdCardTransaction
                {
                    AutoId = autoId,
                    RefNo = refNo,
                    DealPointType = dealPointType,
                    DLCode = dlCode,
                    MemberNo = "MB_ST_01",
                    CardNo = "CARD_ST_01",
                    CardTypeUse = "VIP",
                    AmountAvailPrevious = amountAvailPrev,
                    AmountChTotal = amountChTotal,
                    QtyVisitChTotal = qtyVisit,
                    CreateDTimeUTC = createDTimeUtc,
                    NetworkID = "1001"
                });
            }
        }

        // RO_ST_001: 1 SERVICETURN + 1 POINTUSE (-50)
        AddCctIfNotExists(2001, "RO_ST_001", "SERVICETURN", "DL_HN", 500, null, 1, DateTime.Parse("2026-03-01T08:00:00Z").ToUniversalTime());
        AddCctIfNotExists(2002, "RO_ST_001", "POINTUSE", "DL_HN", null, -50, null, DateTime.Parse("2026-03-01T08:00:00Z").ToUniversalTime());

        // RO_ST_002: 1 SERVICETURN, không có POINTUSE
        AddCctIfNotExists(2003, "RO_ST_002", "SERVICETURN", "DL_SG", 1200, null, 2, DateTime.Parse("2026-04-15T10:00:00Z").ToUniversalTime());

        // RO_ST_003: 1 SERVICETURN với AmountAvailPrevious = 0 & Amount = 0
        AddCctIfNotExists(2004, "RO_ST_003", "SERVICETURN", "DL_HN", 0, null, 1, DateTime.Parse("2026-05-20T12:00:00Z").ToUniversalTime());

        // RO_ST_004: 1 SERVICETURN + 2 POINTUSE (nhân dòng kiểm tra parity: -30 và -20)
        AddCctIfNotExists(2005, "RO_ST_004", "SERVICETURN", "DL_HN", 300, null, 1, DateTime.Parse("2026-06-01T08:00:00Z").ToUniversalTime());
        AddCctIfNotExists(2006, "RO_ST_004", "POINTUSE", "DL_HN", null, -30, null, DateTime.Parse("2026-06-01T08:05:00Z").ToUniversalTime());
        AddCctIfNotExists(2007, "RO_ST_004", "POINTUSE", "DL_HN", null, -20, null, DateTime.Parse("2026-06-01T08:10:00Z").ToUniversalTime());

        // RO_ST_005: DL_UNKNOWN không có trong Mst_DealerCP (bị loại khi INNER JOIN mdcp)
        AddCctIfNotExists(2008, "RO_ST_005", "SERVICETURN", "DL_UNKNOWN", 100, null, 1, DateTime.Parse("2026-07-01T08:00:00Z").ToUniversalTime());

        // RO_ST_006: Tạo trước mốc 2023-06-30 17:00:00 UTC (bị loại khi lọc minDTimeUtc)
        AddCctIfNotExists(2009, "RO_ST_006", "SERVICETURN", "DL_HN", 100, null, 1, DateTime.Parse("2023-06-30T16:59:59Z").ToUniversalTime());

        await db.SaveChangesAsync();
    }
}
