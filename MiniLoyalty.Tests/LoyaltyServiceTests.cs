using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MiniLoyalty.Data;
using MiniLoyalty.Models;
using MiniLoyalty.Services;
using Xunit;

namespace MiniLoyalty.Tests;

/// <summary>Test loyalty: tích điểm cộng dồn + số dư, tích từ mua hàng, tự lên hạng theo điểm trọn đời, đổi quà trừ điểm/kho.</summary>
public class LoyaltyServiceTests
{
    private static async Task<(AppDbContext db, ILoyaltyService svc, SqliteConnection conn)> NewSvc()
    {
        var conn = new SqliteConnection("DataSource=:memory:"); conn.Open();
        var opt = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(conn).Options;
        var db = new AppDbContext(opt, new TenantContext { OrgId = TenantContext.DefaultOrgId });
        db.Database.EnsureCreated();
        await Seeder.SeedAsync(db);   // nạp hạng thẻ + quà mẫu
        return (db, new LoyaltyService(db), conn);
    }

    private static async Task<int> NewMember(ILoyaltyService svc)
        => await svc.CreateAsync(new Member { Name = "KH A", Phone = "0900000001" });

    [Fact]
    public async Task Earn_AddsPoints_UpdatesBalance()
    {
        var (db, svc, conn) = await NewSvc(); using (conn)
        {
            var id = await NewMember(svc);
            var tx = await svc.EarnAsync(id, 500, PointTxType.Earn, "test", null);
            Assert.Equal(500, tx.Points);
            var m = await svc.GetAsync(id);
            Assert.Equal(500, m!.Points);
            Assert.Equal(500, m.LifetimePoints);
        }
    }

    [Fact]
    public async Task EarnFromPurchase_ConvertsAmountToPoints()
    {
        var (db, svc, conn) = await NewSvc(); using (conn)
        {
            var id = await NewMember(svc);
            var tx = await svc.EarnFromPurchaseAsync(id, 1_000_000, "HD1");   // 1 điểm / 1.000đ → 1000 điểm
            Assert.True(tx.Points > 0);
            Assert.Equal(tx.BalanceAfter, (await svc.GetAsync(id))!.Points);
        }
    }

    [Fact]
    public async Task Earn_AutoRankUp_ByLifetimePoints()
    {
        var (db, svc, conn) = await NewSvc(); using (conn)
        {
            var id = await NewMember(svc);
            var rankBefore = (await svc.GetAsync(id))!.RankTierId;
            await svc.EarnAsync(id, 1_000_000, PointTxType.Earn, "big", null);   // đủ điểm lên hạng cao nhất
            var rankAfter = (await svc.GetAsync(id))!.RankTierId;
            Assert.NotEqual(rankBefore, rankAfter);
        }
    }

    [Fact]
    public async Task Redeem_ChecksPoints_AndDecrementsStock()
    {
        var (db, svc, conn) = await NewSvc(); using (conn)
        {
            var id = await NewMember(svc);
            var reward = (await svc.RewardsAsync()).First();
            // chưa đủ điểm → chặn
            var (bad, _) = await svc.RedeemAsync(id, reward.Id);
            Assert.False(bad);
            // nạp đủ điểm rồi đổi
            await svc.EarnAsync(id, reward.PointCost + 100, PointTxType.Earn, null, null);
            var (ok, _) = await svc.RedeemAsync(id, reward.Id);
            Assert.True(ok);
            var m = await svc.GetAsync(id);
            Assert.Equal(100, m!.Points);   // trừ đúng điểm đổi
        }
    }

    [Fact]
    public async Task Dashboard_CountsMembers()
    {
        var (db, svc, conn) = await NewSvc(); using (conn)
        {
            var before = (await svc.DashboardAsync()).Members;
            await NewMember(svc);
            Assert.Equal(before + 1, (await svc.DashboardAsync()).Members);
        }
    }
}
