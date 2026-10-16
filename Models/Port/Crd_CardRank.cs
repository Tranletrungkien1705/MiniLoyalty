using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;
using MiniLoyalty.Models;

namespace MiniLoyalty.Models.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — bảng Crd_CardRank (#008).
//
// Nguồn tra cứu (chỉ đọc):
//   - Model cột: 12.Dev.Common/idn.Loyalty.Common/Models/Loyalty/Crd_CardRank.cs:9-33
//   - DDL thật:  11.BackEnd/V10/05.Refs.Biz/Migrate/20221026.z11.UpdDB.51.CardRank.Loyalty.sql:294-324
//                PK CardRankNo tại :322 (XPKCrd_CardRank).
// ===========================================================================================

/// <summary>
/// Bảng lịch sử xét hạng thẻ Crd_CardRank — port 1:1 DDL 20221026.z11.UpdDB.51.CardRank.Loyalty.sql:294-324.
/// PK = CardRankNo.
/// </summary>
public class Crd_CardRank
{
    public string CardRankNo { get; set; } = "";             // Mã đợt xét hạng — Varchar50 NOT NULL (PK)
    public string NetworkID { get; set; } = "";              // NetworkID — Varchar50 NOT NULL
    public string OrgID { get; set; } = "";                  // OrgID — Varchar50 NOT NULL
    public string? RankPolicyCode { get; set; }              // Mã chính sách xếp hạng — Varchar50
    public string CardSourceCode { get; set; } = "";         // Loại thẻ nguồn — Varchar50 NOT NULL
    public string DealPointType { get; set; } = "";          // Loại giao dịch điểm — Varchar50 NOT NULL
    public string FunctionName { get; set; } = "";           // Tên chức năng — Varchar200 NOT NULL
    public string FunctionActionType { get; set; } = "";     // Loại thao tác chức năng — Varchar200 NOT NULL
    public string? FunctionRemark { get; set; }              // Ghi chú chức năng — Varchar200
    public string RefType { get; set; } = "";                // Loại tham chiếu — Varchar100 NOT NULL
    public string? RefCode00 { get; set; }                   // Varchar100
    public string? RefCode01 { get; set; }                   // Varchar100
    public string? RefCode02 { get; set; }                   // Varchar100
    public string? RefCode03 { get; set; }                   // Varchar100
    public string? RefCode04 { get; set; }                   // Varchar100
    public string? RefCode05 { get; set; }                   // Varchar100
    public DateTime? CreateDTimeUTC { get; set; }            // Thời điểm tạo — MyDateTime NOT NULL
    public string CreateBy { get; set; } = "";               // Người tạo — Varchar50 NOT NULL
    public DateTime? LUDTimeUTC { get; set; }                // Thời điểm cập nhật cuối — MyDateTime NOT NULL
    public string LUBy { get; set; } = "";                   // Người cập nhật cuối — Varchar50 NOT NULL
    public DateTime? LogLUDTimeUTC { get; set; }             // MyDateTime
    public string? LogLUBy { get; set; }                     // Varchar50
}

public class Crd_CardRankConfig : IEntityTypeConfiguration<Crd_CardRank>
{
    public void Configure(EntityTypeBuilder<Crd_CardRank> b)
    {
        b.ToTable("Crd_CardRank");
        b.HasKey(x => x.CardRankNo);
    }
}

/// <summary>
/// Seeder dữ liệu mẫu cho Crd_CardRank, Crd_CardBefore, Crd_CardAfter (#008).
/// Order => 52 (trong dải 10-99 chưa dùng theo bài học A.4). Idempotent.
/// </summary>
public class Crd_CardRankSeeder : IPortSeeder
{
    public int Order => 52;

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<Crd_CardRank>().AnyAsync(x => x.CardRankNo == "CR001")) return;

        // 1. Đảm bảo có CrdMember và CrdCard tương ứng (CARD001 + MB001)
        if (!await db.CrdMembers.AnyAsync(m => m.MemberNo == "MB001"))
        {
            db.CrdMembers.Add(new CrdMember
            {
                MemberNo = "MB001",
                NetworkID = "1001",
                OrgID = "1001",
                DLCodeRegis = "DL001",
                MemberName = "Nguyen Van A",
                IDCardNo = "001099000001",
                PhoneNo = "0901111111",
                MemberStatus = "APPROVE",
                CardTypeUse = "GOLD",
                CardTypeInit = "GOLD",
                CreateDTimeUTC = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc),
                CreateBy = "seed",
                LUDTimeUTC = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc),
                LUBy = "seed"
            });
        }
        if (!await db.CrdCards.AnyAsync(c => c.CardNo == "CARD001"))
        {
            db.CrdCards.Add(new CrdCard
            {
                CardNo = "CARD001",
                MemberNo = "MB001",
                NetworkID = "1001",
                CardTypeUse = "GOLD",
                CardTypeInit = "GOLD",
                CardStatus = "APPROVE",
                CreateDTimeUTC = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc),
                CreateBy = "seed",
                LUDTimeUTC = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc),
                LUBy = "seed"
            });
        }

        // Bản ghi 1: CR001 (khớp Ca 1: CARD001 + MB001, và Ca 2: CreateDTimeUTC 2026-01-15 trong khoảng 2026-01-01 đến 2026-01-31)
        var dtime1 = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);
        db.Set<Crd_CardRank>().Add(new Crd_CardRank
        {
            CardRankNo = "CR001",
            NetworkID = "1001",
            OrgID = "1001",
            RankPolicyCode = "RP001",
            CardSourceCode = "RENEW",
            DealPointType = "NORMAL",
            FunctionName = "Crd_CardRank",
            FunctionActionType = "CALC",
            RefType = "SYSTEM",
            CreateDTimeUTC = dtime1,
            CreateBy = "seed",
            LUDTimeUTC = dtime1,
            LUBy = "seed"
        });

        db.Set<Crd_CardBefore>().Add(new Crd_CardBefore
        {
            CardRankNo = "CR001",
            CardNo = "CARD001",
            NetworkID = "1001",
            MemberNo = "MB001",
            CardSourceCode = "RENEW",
            CardTypeUse = "SILVER",
            CardTypeInit = "SILVER",
            PointCardRank = 100,
            QtyVisitTotal = 5,
            CreateDTimeUTC = dtime1,
            CreateBy = "seed",
            LUDTimeUTC = dtime1,
            LUBy = "seed"
        });

        db.Set<Crd_CardAfter>().Add(new Crd_CardAfter
        {
            CardRankNo = "CR001",
            CardNo = "CARD001",
            NetworkID = "1001",
            MemberNo = "MB001",
            CardSourceCode = "RENEW",
            CardTypeUse = "GOLD",
            CardTypeInit = "GOLD",
            CreateDTimeUTC = dtime1,
            CreateBy = "seed",
            LUDTimeUTC = dtime1,
            LUBy = "seed"
        });

        // Bản ghi 2: CR002 (tháng 2/2026, để kiểm chứng bộ lọc Ca 2 loại bỏ đúng bản ghi ngoài khoảng ngày)
        var dtime2 = new DateTime(2026, 2, 15, 10, 0, 0, DateTimeKind.Utc);
        if (!await db.CrdMembers.AnyAsync(m => m.MemberNo == "MB002"))
        {
            db.CrdMembers.Add(new CrdMember
            {
                MemberNo = "MB002",
                NetworkID = "1001",
                OrgID = "1001",
                DLCodeRegis = "DL002",
                MemberName = "Tran Thi B",
                IDCardNo = "001099000002",
                PhoneNo = "0902222222",
                MemberStatus = "APPROVE",
                CardTypeUse = "SILVER",
                CardTypeInit = "SILVER",
                CreateDTimeUTC = dtime2,
                CreateBy = "seed",
                LUDTimeUTC = dtime2,
                LUBy = "seed"
            });
        }
        if (!await db.CrdCards.AnyAsync(c => c.CardNo == "CARD002"))
        {
            db.CrdCards.Add(new CrdCard
            {
                CardNo = "CARD002",
                MemberNo = "MB002",
                NetworkID = "1001",
                CardTypeUse = "SILVER",
                CardTypeInit = "SILVER",
                CardStatus = "APPROVE",
                CreateDTimeUTC = dtime2,
                CreateBy = "seed",
                LUDTimeUTC = dtime2,
                LUBy = "seed"
            });
        }

        db.Set<Crd_CardRank>().Add(new Crd_CardRank
        {
            CardRankNo = "CR002",
            NetworkID = "1001",
            OrgID = "1001",
            RankPolicyCode = "RP001",
            CardSourceCode = "RENEW",
            DealPointType = "NORMAL",
            FunctionName = "Crd_CardRank",
            FunctionActionType = "CALC",
            RefType = "SYSTEM",
            CreateDTimeUTC = dtime2,
            CreateBy = "seed",
            LUDTimeUTC = dtime2,
            LUBy = "seed"
        });

        db.Set<Crd_CardBefore>().Add(new Crd_CardBefore
        {
            CardRankNo = "CR002",
            CardNo = "CARD002",
            NetworkID = "1001",
            MemberNo = "MB002",
            CardSourceCode = "RENEW",
            CardTypeUse = "SILVER",
            CardTypeInit = "SILVER",
            PointCardRank = 50,
            QtyVisitTotal = 2,
            CreateDTimeUTC = dtime2,
            CreateBy = "seed",
            LUDTimeUTC = dtime2,
            LUBy = "seed"
        });

        db.Set<Crd_CardAfter>().Add(new Crd_CardAfter
        {
            CardRankNo = "CR002",
            CardNo = "CARD002",
            NetworkID = "1001",
            MemberNo = "MB002",
            CardSourceCode = "RENEW",
            CardTypeUse = "SILVER",
            CardTypeInit = "SILVER",
            CreateDTimeUTC = dtime2,
            CreateBy = "seed",
            LUDTimeUTC = dtime2,
            LUBy = "seed"
        });

        await db.SaveChangesAsync();
    }
}
