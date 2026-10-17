using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MiniLoyalty.Data;
using MiniLoyalty.Models;

// Type alias tái sử dụng entity CrdMemberDiscountTransaction có sẵn trong Models/PortLoyalty.cs:273-307
// theo PORT_RULES dòng 39 và Bài học A.24 (tránh tạo entity thứ 2 trùng bảng).
using Crd_MemberDiscountTransaction = MiniLoyalty.Models.CrdMemberDiscountTransaction;

namespace MiniLoyalty.Models.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — Crd_MemberDiscountTransaction (#055).
//
// Nguồn tra cứu (chỉ đọc):
//   - Model cột: 12.Dev.Common/idn.Loyalty.Common/Models/Loyalty/Crd_MemberDiscountTransaction.cs:9-44 (32 cột)
//   - DDL:       11.BackEnd/V10/05.Refs.Biz/Migrate/Script.HuongTTT/20260817.CreTb.Crd_MemberDiscountTransaction.sql:13-46
//   - Gate:      13.ClientGate/V10/idn.Loyalty.WebAPI/Controllers/Crd_MemberDiscountTransactionController.cs:31-138
//   - Filter:    Crd_MemberDiscountTransactionController.cs:142-179 (strWhereClause_Crd_MemberDiscountTransaction)
//   - API:       11.BackEnd/V10/idn.Loyalty.Biz.Web/Controllers/CrdMemberDiscountTransactionController.cs:22-89
//   - Biz LIVE:  11.BackEnd/V10/idn.Loyalty.Biz/Transaction.cs:5177-5629 (WAS_Crd_MemberDiscountTransaction_Get,
//                Crd_MemberDiscountTransaction_Get, Crd_MemberDiscountTransaction_GetX)
//
// GHI CHÚ QUAN TRỌNG:
//   - Bảng Crd_MemberDiscountTransaction đã có entity CrdMemberDiscountTransaction tại Models/PortLoyalty.cs:273-307
//     và được cấu hình trong AppDbContext.cs. TUYỆT ĐỐI không tạo thêm entity thứ 2 để tránh trùng mapping.
// ===========================================================================================

/// <summary>
/// Seeder dữ liệu mẫu cho Crd_MemberDiscountTransaction (#055), gồm 2 CrdMember và 6 CrdMemberDiscountTransaction.
/// Thứ tự Order => 77 (chọn trong dải 10-99 chưa dùng theo Bài học A.4).
/// Idempotent: kiểm tra AutoId == 55001 trước khi thêm.
/// </summary>
public class Crd_MemberDiscountTransactionSeeder : IPortSeeder
{
    public int Order => 77;

    public async Task SeedAsync(AppDbContext db)
    {
        const string net = "DEMO";
        const string org = "DEMO";
        const string user = "seed";

        // 1. Tạo 2 hội viên trong CrdMember để thỏa mãn inner join Crd_Member (Transaction.cs:5480-5481)
        if (!await db.Set<CrdMember>().AnyAsync(x => x.MemberNo == "MB5501"))
        {
            db.Set<CrdMember>().Add(new CrdMember
            {
                MemberNo = "MB5501",
                MemberName = "Nguyen Van A",
                PhoneNo = "0905501001",
                DLCodeRegis = "DL55",
                NetworkID = net,
                OrgID = org,
                LogLUBy = user
            });
        }

        if (!await db.Set<CrdMember>().AnyAsync(x => x.MemberNo == "MB5502"))
        {
            db.Set<CrdMember>().Add(new CrdMember
            {
                MemberNo = "MB5502",
                MemberName = "Tran Thi B",
                PhoneNo = "0905501002",
                DLCodeRegis = "DL55",
                NetworkID = net,
                OrgID = org,
                LogLUBy = user
            });
        }

        // Tạo thẻ CrdCard tương ứng nếu chưa có
        if (!await db.Set<CrdCard>().AnyAsync(x => x.CardNo == "CD5501"))
        {
            db.Set<CrdCard>().Add(new CrdCard
            {
                CardNo = "CD5501",
                MemberNo = "MB5501",
                NetworkID = net
            });
        }

        if (!await db.Set<CrdCard>().AnyAsync(x => x.CardNo == "CD5502"))
        {
            db.Set<CrdCard>().Add(new CrdCard
            {
                CardNo = "CD5502",
                MemberNo = "MB5502",
                NetworkID = net
            });
        }

        await db.SaveChangesAsync();

        // 2. Tạo 6 giao dịch trong CrdMemberDiscountTransaction
        // Idempotent: kiểm tra db.Set<CrdMemberDiscountTransaction>().AnyAsync(x => x.AutoId == 55001)
        if (!await db.Set<CrdMemberDiscountTransaction>().AnyAsync(x => x.AutoId == 55001))
        {
            db.Set<CrdMemberDiscountTransaction>().AddRange(
                // Dòng 1: AutoId 55001, RefNo "RO5501", MemberNo "MB5501", CardNo "CD5501", DealPointType "DISCOUNTRO", CreateDate "2026-03-01", PointChTotal 100000.0, AmountForDC 1000000.0, DLCode "DL55", LogLUDTimeUTC "2026-03-01T10:00:00Z"
                new CrdMemberDiscountTransaction
                {
                    AutoId = 55001,
                    RefNo = "RO5501",
                    MemberNo = "MB5501",
                    CardNo = "CD5501",
                    DealPointType = "DISCOUNTRO",
                    CreateDate = DateTime.Parse("2026-03-01"),
                    PointChTotal = 100000.0,
                    AmountForDC = 1000000.0,
                    DLCode = "DL55",
                    LogLUDTimeUTC = DateTime.Parse("2026-03-01T10:00:00Z").ToUniversalTime(),
                    NetworkID = net,
                    OrgID = org,
                    LogLUBy = user
                },
                // Dòng 2: AutoId 55002, RefNo "RO5502", MemberNo "MB5501", CardNo "CD5501", DealPointType "DISCOUNTRO", CreateDate "2026-03-15", PointChTotal 200000.0, AmountForDC 2000000.0, DLCode "DL55", LogLUDTimeUTC "2026-03-15T10:00:00Z"
                new CrdMemberDiscountTransaction
                {
                    AutoId = 55002,
                    RefNo = "RO5502",
                    MemberNo = "MB5501",
                    CardNo = "CD5501",
                    DealPointType = "DISCOUNTRO",
                    CreateDate = DateTime.Parse("2026-03-15"),
                    PointChTotal = 200000.0,
                    AmountForDC = 2000000.0,
                    DLCode = "DL55",
                    LogLUDTimeUTC = DateTime.Parse("2026-03-15T10:00:00Z").ToUniversalTime(),
                    NetworkID = net,
                    OrgID = org,
                    LogLUBy = user
                },
                // Dòng 3: AutoId 55003, RefNo "RO5503", MemberNo "MB5501", CardNo "CD5501", DealPointType "DISCOUNTRO", CreateDate "2026-03-25", PointChTotal 300000.0, AmountForDC 3000000.0, DLCode "DL55", LogLUDTimeUTC "2026-03-25T10:00:00Z"
                new CrdMemberDiscountTransaction
                {
                    AutoId = 55003,
                    RefNo = "RO5503",
                    MemberNo = "MB5501",
                    CardNo = "CD5501",
                    DealPointType = "DISCOUNTRO",
                    CreateDate = DateTime.Parse("2026-03-25"),
                    PointChTotal = 300000.0,
                    AmountForDC = 3000000.0,
                    DLCode = "DL55",
                    LogLUDTimeUTC = DateTime.Parse("2026-03-25T10:00:00Z").ToUniversalTime(),
                    NetworkID = net,
                    OrgID = org,
                    LogLUBy = user
                },
                // Dòng 4: AutoId 55004, RefNo "RO5504", MemberNo "MB5502", CardNo "CD5502", DealPointType "DISCOUNTRO", CreateDate "2026-03-10", PointChTotal 150000.0, AmountForDC 1500000.0, DLCode "DL55", LogLUDTimeUTC "2026-03-10T10:00:00Z"
                new CrdMemberDiscountTransaction
                {
                    AutoId = 55004,
                    RefNo = "RO5504",
                    MemberNo = "MB5502",
                    CardNo = "CD5502",
                    DealPointType = "DISCOUNTRO",
                    CreateDate = DateTime.Parse("2026-03-10"),
                    PointChTotal = 150000.0,
                    AmountForDC = 1500000.0,
                    DLCode = "DL55",
                    LogLUDTimeUTC = DateTime.Parse("2026-03-10T10:00:00Z").ToUniversalTime(),
                    NetworkID = net,
                    OrgID = org,
                    LogLUBy = user
                },
                // Dòng 5: AutoId 55005, RefNo "RO5505", MemberNo "MB5501", CardNo "CD5501", DealPointType "SALES", CreateDate "2026-03-05", PointChTotal 50000.0, AmountForDC 500000.0, DLCode "DL55", LogLUDTimeUTC "2026-03-05T10:00:00Z" (DealPointType khác DISCOUNTRO)
                new CrdMemberDiscountTransaction
                {
                    AutoId = 55005,
                    RefNo = "RO5505",
                    MemberNo = "MB5501",
                    CardNo = "CD5501",
                    DealPointType = "SALES",
                    CreateDate = DateTime.Parse("2026-03-05"),
                    PointChTotal = 50000.0,
                    AmountForDC = 500000.0,
                    DLCode = "DL55",
                    LogLUDTimeUTC = DateTime.Parse("2026-03-05T10:00:00Z").ToUniversalTime(),
                    NetworkID = net,
                    OrgID = org,
                    LogLUBy = user
                },
                // Dòng 6: AutoId 55006, RefNo "RO5506", MemberNo "MB_GHOST", CardNo "CD5506", DealPointType "DISCOUNTRO", CreateDate "2026-03-05", PointChTotal 50000.0, AmountForDC 500000.0, DLCode "DL55", LogLUDTimeUTC "2026-03-05T10:00:00Z" (Hội viên không có trong CrdMember)
                new CrdMemberDiscountTransaction
                {
                    AutoId = 55006,
                    RefNo = "RO5506",
                    MemberNo = "MB_GHOST",
                    CardNo = "CD5506",
                    DealPointType = "DISCOUNTRO",
                    CreateDate = DateTime.Parse("2026-03-05"),
                    PointChTotal = 50000.0,
                    AmountForDC = 500000.0,
                    DLCode = "DL55",
                    LogLUDTimeUTC = DateTime.Parse("2026-03-05T10:00:00Z").ToUniversalTime(),
                    NetworkID = net,
                    OrgID = org,
                    LogLUBy = user
                }
            );

            await db.SaveChangesAsync();
        }
    }
}
