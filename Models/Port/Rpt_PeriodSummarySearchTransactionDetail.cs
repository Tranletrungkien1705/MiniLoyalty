using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MiniLoyalty.Data;
using MiniLoyalty.Models;

namespace MiniLoyalty.Models.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — Rpt_PeriodSummaryController.SearchTransactionDetail (#289).
//
// Nguồn tra cứu (chỉ đọc):
//   - Gate:     13.ClientGate/V10/idn.Loyalty.WebAPI/Controllers/Rpt_PeriodSummaryController.cs:445-530 (SearchTransactionDetail)
//   - Filter:   Rpt_PeriodSummaryController.cs:737-774 (strWhereClause_Crd_CardTransaction)
//   - Service:  13.ClientGate/V10/idn.Loyalty.ClientService/Services/Loyalty/Crd_CardTransactionService.cs:25-34 (WA_Crd_CardTransaction_Get)
//   - API:      11.BackEnd/V10/idn.Loyalty.Biz.Web/Controllers/CrdCardTransactionController.cs:24-60 (WA_Crd_CardTransaction_Get)
//   - Biz LIVE: 11.BackEnd/V10/idn.Loyalty.Biz/Master.Loyalty.cs:21144-21250 (WAS_Crd_CardTransaction_Get),
//               :21254-21408 (Crd_CardTransaction_Get), :21409-21817 (Crd_CardTransaction_GetX)
//
// GHI CHÚ:
//   - Các entity CrdMember, CrdCard, CrdCardTransaction, CrdMemberDiscountTransaction đã có sẵn trong Models/PortLoyalty.cs.
//   - File này chứa Seeder Rpt_PeriodSummarySearchTransactionDetailSeeder phục vụ kiểm thử #289.
// ===========================================================================================

/// <summary>
/// Seeder dữ liệu mẫu cho Rpt_PeriodSummarySearchTransactionDetail (#289).
/// Order => 90 (chưa dùng trong dải 10-99 theo Bài học A.4).
/// DEMO, idempotent.
/// </summary>
public class Rpt_PeriodSummarySearchTransactionDetailSeeder : IPortSeeder
{
    public int Order => 90;

    public async Task SeedAsync(AppDbContext db)
    {
        // 1. CrdMember: MB289_01, MB289_02
        if (!await db.Set<CrdMember>().AnyAsync(x => x.MemberNo == "MB289_01"))
        {
            db.Set<CrdMember>().Add(new CrdMember
            {
                MemberNo = "MB289_01",
                MemberName = "Nguyen Van A",
                PhoneNo = "0901234567",
                CarNo = "30A-12345",
                NetworkID = "1001",
                OrgID = "1001"
            });
        }

        if (!await db.Set<CrdMember>().AnyAsync(x => x.MemberNo == "MB289_02"))
        {
            db.Set<CrdMember>().Add(new CrdMember
            {
                MemberNo = "MB289_02",
                MemberName = "Tran Thi B",
                PhoneNo = "0907654321",
                CarNo = "29B-67890",
                NetworkID = "1001",
                OrgID = "1001"
            });
        }

        // 2. CrdCard: CD289_01, CD289_02
        if (!await db.Set<CrdCard>().AnyAsync(x => x.CardNo == "CD289_01"))
        {
            db.Set<CrdCard>().Add(new CrdCard
            {
                CardNo = "CD289_01",
                MemberNo = "MB289_01",
                DLCodeExceptionally = "DL289_EX1",
                NetworkID = "1001"
            });
        }

        if (!await db.Set<CrdCard>().AnyAsync(x => x.CardNo == "CD289_02"))
        {
            db.Set<CrdCard>().Add(new CrdCard
            {
                CardNo = "CD289_02",
                MemberNo = "MB289_02",
                DLCodeExceptionally = "DL289_EX2",
                NetworkID = "1001"
            });
        }

        // 3. CrdCardTransaction (3 bản ghi)
        if (!await db.Set<CrdCardTransaction>().AnyAsync(x => x.RefNo == "TX289_01"))
        {
            db.Set<CrdCardTransaction>().Add(new CrdCardTransaction
            {
                AutoId = 289001,
                RefNo = "TX289_01",
                MemberNo = "MB289_01",
                CardNo = "CD289_01",
                DLCode = "DL289_01",
                DealPointType = "POINTINCREASE",
                PointChRankTotal = 500,
                AmountChTotal = 100,
                CreateDTimeUTC = DateTime.SpecifyKind(DateTime.Parse("2026-08-15 03:00:00"), DateTimeKind.Utc),
                LogLUDTimeUTC = DateTime.SpecifyKind(DateTime.Parse("2026-08-15 10:00:00"), DateTimeKind.Utc),
                NetworkID = "1001",
                OrgID = "1001",
                CreateBy = "port",
                LogLUBy = "port"
            });
        }

        if (!await db.Set<CrdCardTransaction>().AnyAsync(x => x.RefNo == "TX289_02"))
        {
            db.Set<CrdCardTransaction>().Add(new CrdCardTransaction
            {
                AutoId = 289002,
                RefNo = "TX289_02",
                MemberNo = "MB289_01",
                CardNo = "CD289_01",
                DLCode = "DL289_01",
                DealPointType = "CONSUMPTION",
                PointChRankTotal = 0,
                AmountChTotal = 250,
                CreateDTimeUTC = DateTime.SpecifyKind(DateTime.Parse("2026-08-20 03:00:00"), DateTimeKind.Utc),
                LogLUDTimeUTC = DateTime.SpecifyKind(DateTime.Parse("2026-08-20 10:00:00"), DateTimeKind.Utc),
                NetworkID = "1001",
                OrgID = "1001",
                CreateBy = "port",
                LogLUBy = "port"
            });
        }

        if (!await db.Set<CrdCardTransaction>().AnyAsync(x => x.RefNo == "TX289_03"))
        {
            db.Set<CrdCardTransaction>().Add(new CrdCardTransaction
            {
                AutoId = 289003,
                RefNo = "TX289_03",
                MemberNo = "MB289_02",
                CardNo = "CD289_02",
                DLCode = "DL289_02",
                DealPointType = "SERVICE",
                PointChRankTotal = 0,
                AmountChTotal = 300,
                CreateDTimeUTC = DateTime.SpecifyKind(DateTime.Parse("2026-09-05 03:00:00"), DateTimeKind.Utc),
                LogLUDTimeUTC = DateTime.SpecifyKind(DateTime.Parse("2026-09-05 10:00:00"), DateTimeKind.Utc),
                NetworkID = "1001",
                OrgID = "1001",
                CreateBy = "port",
                LogLUBy = "port"
            });
        }

        // 4. CrdMemberDiscountTransaction (1 bản ghi phục vụ union)
        if (!await db.Set<CrdMemberDiscountTransaction>().AnyAsync(x => x.RefNo == "DC289_01"))
        {
            db.Set<CrdMemberDiscountTransaction>().Add(new CrdMemberDiscountTransaction
            {
                AutoId = 289101,
                RefNo = "DC289_01",
                MemberNo = "MB289_01",
                CardNo = "CD289_01",
                DLCode = "DL289_01",
                DealPointType = "DISCOUNTRO",
                PolicyDiscountRate = 10.0,
                PointChTotal = 150.0,
                AmountForDC = 1500.0,
                CreateDTimeUTC = DateTime.SpecifyKind(DateTime.Parse("2026-08-25 03:00:00"), DateTimeKind.Utc),
                LogLUDTimeUTC = DateTime.SpecifyKind(DateTime.Parse("2026-08-25 10:00:00"), DateTimeKind.Utc),
                NetworkID = "1001",
                OrgID = "1001",
                CreateBy = "port",
                LogLUBy = "port"
            });
        }

        await db.SaveChangesAsync();
    }
}
