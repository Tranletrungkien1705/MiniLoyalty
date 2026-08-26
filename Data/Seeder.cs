using Microsoft.EntityFrameworkCore;
using MiniLoyalty.Models;

namespace MiniLoyalty.Data;

public static class Seeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await db.Database.EnsureCreatedAsync();

        if (!await db.RankTiers.AnyAsync())
        {
            db.RankTiers.AddRange(
                new RankTier { Name = "Thành viên", MinLifetimePoints = 0, DiscountPercent = 0, ColorHex = "#94a3b8", SortOrder = 0 },
                new RankTier { Name = "Bạc", MinLifetimePoints = 500, DiscountPercent = 3, ColorHex = "#9ca3af", SortOrder = 1 },
                new RankTier { Name = "Vàng", MinLifetimePoints = 2000, DiscountPercent = 5, ColorHex = "#f59e0b", SortOrder = 2 },
                new RankTier { Name = "Bạch kim", MinLifetimePoints = 5000, DiscountPercent = 8, ColorHex = "#6366f1", SortOrder = 3 },
                new RankTier { Name = "Kim cương", MinLifetimePoints = 10000, DiscountPercent = 10, ColorHex = "#06b6d4", SortOrder = 4 });
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
            await db.SaveChangesAsync();
        }
    }
}
