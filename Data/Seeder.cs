using Microsoft.EntityFrameworkCore;
using MiniLoyalty.Models;

namespace MiniLoyalty.Data;

public static class Seeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await db.Database.EnsureCreatedAsync();
        await MigratePostgresAsync(db);   // DB cloud cũ: thêm Orgs + cột OrgId nếu thiếu

        // Org mặc định (tenant cho dữ liệu seed + UI không kèm ApiKey).
        if (!await db.Orgs.AnyAsync(o => o.Id == TenantContext.DefaultOrgId))
        {
            db.Orgs.Add(new Org { Id = TenantContext.DefaultOrgId, Name = "Demo Loyalty", ApiKey = TenantContext.DefaultApiKey });
            await db.SaveChangesAsync();
        }

        if (!await db.RankTiers.AnyAsync())
        {
            // PointKeepBegin/QtyVisitKeepBegin = ngưỡng DUY TRÌ hạng trong kỳ (Mst_RankPolicy) — dùng cho job xét hạng cuối kỳ.
            // PointUpBegin/QtyVisitUpBegin = ngưỡng NÂNG hạng trong kỳ — đạt cả hai thì lên hạng kế tiếp (UP).
            db.RankTiers.AddRange(
                new RankTier { Name = "Thành viên", MinLifetimePoints = 0, DiscountPercent = 0, BirthdayPoints = 50, ColorHex = "#94a3b8", SortOrder = 0, PointKeepBegin = 0, QtyVisitKeepBegin = 0, PointUpBegin = 300, QtyVisitUpBegin = 1 },
                new RankTier { Name = "Bạc", MinLifetimePoints = 500, DiscountPercent = 3, BirthdayPoints = 100, ColorHex = "#9ca3af", SortOrder = 1, PointKeepBegin = 300, QtyVisitKeepBegin = 1, PointUpBegin = 1000, QtyVisitUpBegin = 2 },
                new RankTier { Name = "Vàng", MinLifetimePoints = 2000, DiscountPercent = 5, BirthdayPoints = 200, ColorHex = "#f59e0b", SortOrder = 2, PointKeepBegin = 1000, QtyVisitKeepBegin = 2, PointUpBegin = 2500, QtyVisitUpBegin = 3 },
                new RankTier { Name = "Bạch kim", MinLifetimePoints = 5000, DiscountPercent = 8, BirthdayPoints = 300, ColorHex = "#6366f1", SortOrder = 3, PointKeepBegin = 2500, QtyVisitKeepBegin = 3, PointUpBegin = 5000, QtyVisitUpBegin = 4 },
                new RankTier { Name = "Kim cương", MinLifetimePoints = 10000, DiscountPercent = 10, BirthdayPoints = 500, ColorHex = "#06b6d4", SortOrder = 4, PointKeepBegin = 5000, QtyVisitKeepBegin = 4, PointUpBegin = 0, QtyVisitUpBegin = 0 });
            await db.SaveChangesAsync();
        }
        if (!await db.Rewards.AnyAsync())
        {
            db.Rewards.AddRange(
                new Reward { Name = "Voucher giảm 50.000đ", PointCost = 500, Description = "Áp dụng đơn từ 300.000đ" },
                new Reward { Name = "Voucher giảm 100.000đ", PointCost = 900, Description = "Áp dụng đơn từ 500.000đ" },
                new Reward { Name = "Freeship toàn quốc", PointCost = 300, Description = "Miễn phí vận chuyển 1 đơn" },
                new Reward { Name = "Quà sinh nhật đặc biệt", PointCost = 1500, Description = "Set quà tặng thành viên VIP", Stock = 20 });
            await db.SaveChangesAsync();
        }
        if (!await db.Members.AnyAsync())
        {
            var tiers = await db.RankTiers.OrderBy(t => t.SortOrder).ToListAsync();
            RankTier Rank(int lp) => tiers.Last(t => lp >= t.MinLifetimePoints);
            int n = 0;
            Member M(string name, string phone, int lp, int bal)
            {
                n++;
                var r = Rank(lp);
                return new Member
                {
                    Code = $"HV{DateTime.Now:yy}{n:D5}", Name = name, Phone = phone, Points = bal, LifetimePoints = lp,
                    RankTierId = r.Id, JoinedAt = DateTime.Now.AddMonths(-n * 2),
                    Dob = new DateTime(1990 + n, ((n * 3) % 12) + 1, ((n * 5) % 27) + 1),
                    // Kỳ xét hạng đã hết hạn (EffDateEnd quá khứ) → job xét hạng cuối kỳ sẽ xử lý.
                    // PointCardRank/QtyVisitAvail: có người đạt ngưỡng duy trì, có người không (để minh hoạ KEEP/DOWN).
                    EffDateStart = DateTime.Today.AddMonths(-12), EffDateEnd = DateTime.Today.AddDays(-1), CardSourceCode = "NEW",
                    PointCardRank = lp / 2, QtyVisitAvail = n % 4,
                    Transactions = [ new PointTransaction { Type = PointTxType.Adjust, Points = lp, BalanceAfter = bal, Note = "Số dư đầu kỳ", CreatedAt = DateTime.Now.AddMonths(-n * 2) } ]
                };
            }
            db.Members.AddRange(
                M("Nguyễn Văn An", "0901111111", 12500, 3200),
                M("Trần Thị Bình", "0902222222", 6200, 1800),
                M("Lê Hoàng Cường", "0903333333", 2400, 900),
                M("Phạm Thu Dung", "0904444444", 700, 350),
                M("Vũ Minh Đức", "0905555555", 120, 120)
            );
            // Hội viên đạt ngưỡng NÂNG hạng trong kỳ (Bạc: PointUpBegin=1000, QtyVisitUpBegin=2) → minh hoạ UP.
            var up = M("Đỗ Thị Hoa", "0906666666", 800, 400);
            up.PointCardRank = 1200; up.QtyVisitAvail = 2;
            db.Members.Add(up);
            // Hội viên có sinh nhật đúng hôm nay — để minh hoạ job tặng điểm sinh nhật.
            var bd = db.Members.Local.First();
            bd.Dob = new DateTime(1992, DateTime.Today.Month, DateTime.Today.Day);
            // Hội viên mới có khai báo người giới thiệu (MemberNoIntro) + điểm thưởng (PointIntro)
            // — để minh hoạ nghiệp vụ thưởng điểm giới thiệu (DealPointType=INTRODUCTION).
            var intro = M("Hoàng Văn Khoa", "0907777777", 0, 0);
            intro.MemberNoIntro = db.Members.Local.First().Code;   // người giới thiệu = hội viên đầu tiên
            intro.PointIntro = 200;
            db.Members.Add(intro);
            // Ghi nhận lượt dịch vụ (DealPointType=SERVICETURN) — minh hoạ nghiệp vụ cộng lượt vào QtyVisitAvail.
            var svcTurn = M("Bùi Thanh Tùng", "0908888888", 300, 300);
            svcTurn.QtyVisitAvail = 2;
            svcTurn.Transactions.Add(new PointTransaction { Type = PointTxType.ServiceTurn, Points = 0, QtyVisit = 2, BalanceAfter = 300, Note = "Ghi nhận 2 lượt dịch vụ", RefNo = "RO-DEMO-001" });
            db.Members.Add(svcTurn);
            // Giao dịch chiết khấu dịch vụ (DealPointType=DISCOUNTRO) — minh hoạ áp % chiết khấu theo hạng.
            var disc = M("Ngô Thị Lan", "0909999999", 2600, 1200);
            disc.Discounts.Add(new MemberDiscountTransaction
            {
                RefNo = "RO-DEMO-002", CardTypeApplyId = disc.RankTierId,
                PolicyDiscountRate = Rank(2600).DiscountPercent, AmountForDC = 2_000_000,
                DiscountAmount = Math.Round(2_000_000 * Rank(2600).DiscountPercent / 100m, 0, MidpointRounding.AwayFromZero),
                CreatedAt = DateTime.Now.AddDays(-3)
            });
            db.Members.Add(disc);
            // Điểm voucher xe mới (DealPointType=VOUCHERXM/VOUCHERSD) — minh hoạ tặng + sử dụng điểm voucher.
            var vch = M("Đinh Quốc Bảo", "0910111111", 1500, 700);
            vch.PointVoucher = 3000;
            vch.Vouchers.Add(new MemberVoucherTransaction
            {
                Type = VoucherTxType.Award, Points = 5000, BalanceAfter = 5000, VoucherCode = "VCH-NEWCAR-2026",
                RefNo = "VOUCHERXM-DEMO-001", ExpiryDate = DateTime.Today.AddMonths(12),
                Note = "Tặng điểm voucher xe mới (VCH-NEWCAR-2026)", CreatedAt = DateTime.Now.AddDays(-10)
            });
            vch.Vouchers.Add(new MemberVoucherTransaction
            {
                Type = VoucherTxType.Use, Points = -2000, BalanceAfter = 3000, VoucherCode = "VCH-NEWCAR-2026",
                RefNo = "VOUCHERSD-DEMO-001", Note = "Sử dụng điểm voucher (VCH-NEWCAR-2026)", CreatedAt = DateTime.Now.AddDays(-4)
            });
            db.Members.Add(vch);
            await db.SaveChangesAsync();
        }
    }

    /// <summary>
    /// DB Postgres cloud đã tồn tại (EnsureCreated bỏ qua): tạo bảng Orgs + thêm cột OrgId nếu thiếu,
    /// backfill dữ liệu cũ về Org mặc định. Idempotent (IF NOT EXISTS).
    /// </summary>
    private static async Task MigratePostgresAsync(AppDbContext db)
    {
        if (!db.Database.IsNpgsql()) return;
        var def = TenantContext.DefaultOrgId;
        var sql = new[]
        {
            "CREATE TABLE IF NOT EXISTS miniloyalty.\"Orgs\" (\"Id\" uuid PRIMARY KEY, \"Name\" text NOT NULL DEFAULT '', \"ApiKey\" text NOT NULL DEFAULT '', \"CreatedAt\" timestamp NOT NULL DEFAULT now())",
            "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_Orgs_ApiKey\" ON miniloyalty.\"Orgs\" (\"ApiKey\")",
            $"ALTER TABLE miniloyalty.\"Members\" ADD COLUMN IF NOT EXISTS \"OrgId\" uuid NOT NULL DEFAULT '{def}'",
            $"ALTER TABLE miniloyalty.\"PointTransactions\" ADD COLUMN IF NOT EXISTS \"OrgId\" uuid NOT NULL DEFAULT '{def}'",
            $"ALTER TABLE miniloyalty.\"Rewards\" ADD COLUMN IF NOT EXISTS \"OrgId\" uuid NOT NULL DEFAULT '{def}'",
        };
        foreach (var s in sql)
            try { await db.Database.ExecuteSqlRawAsync(s); } catch { }
    }
}
