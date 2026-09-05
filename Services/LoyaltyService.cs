using Microsoft.EntityFrameworkCore;
using MiniLoyalty.Data;
using MiniLoyalty.Models;

namespace MiniLoyalty.Services;

public record LoyaltyDash(int Members, int ActivePoints, int LifetimeIssued,
    List<(string Rank, string Color, int Count)> ByRank);

public interface ILoyaltyService
{
    Task<List<Member>> MembersAsync(string? q, int? rankId);
    Task<Member?> GetAsync(int id);
    Task<Member?> GetByPhoneAsync(string phone);
    Task<int> CreateAsync(Member m);
    Task<PointTransaction> EarnAsync(int memberId, int points, PointTxType type, string? note, string? refNo);
    Task<PointTransaction> EarnFromPurchaseAsync(int memberId, decimal amount, string? refNo);
    Task<(bool ok, string msg)> RedeemAsync(int memberId, int rewardId);
    Task<List<RankTier>> RanksAsync();
    Task<List<Reward>> RewardsAsync(bool activeOnly = true);
    Task<LoyaltyDash> DashboardAsync();
}

public class LoyaltyService(AppDbContext db) : ILoyaltyService
{
    public const int VndPerPoint = 1000;   // 1 điểm / 1.000đ

    public async Task<List<Member>> MembersAsync(string? q, int? rankId)
    {
        var query = db.Members.Include(m => m.RankTier).AsQueryable();
        if (rankId.HasValue) query = query.Where(m => m.RankTierId == rankId.Value);
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(m => m.Name.Contains(q) || m.Code.Contains(q) || (m.Phone ?? "").Contains(q));
        var list = await query.ToListAsync();
        return list.OrderByDescending(m => m.LifetimePoints).ToList();
    }

    public Task<Member?> GetAsync(int id) =>
        db.Members.Include(m => m.RankTier).Include(m => m.Transactions).FirstOrDefaultAsync(m => m.Id == id);

    public Task<Member?> GetByPhoneAsync(string phone) =>
        db.Members.Include(m => m.RankTier).FirstOrDefaultAsync(m => m.Phone == phone);

    public async Task<int> CreateAsync(Member m)
    {
        var count = await db.Members.CountAsync();
        m.Code = $"HV{DateTime.Now:yy}{count + 1:D5}";
        m.RankTierId = (await LowestRankAsync()).Id;
        db.Members.Add(m);
        await db.SaveChangesAsync();
        return m.Id;
    }

    public async Task<PointTransaction> EarnAsync(int memberId, int points, PointTxType type, string? note, string? refNo)
    {
        // FirstOrDefault (không Find) để áp query filter tenant — chặn tích điểm chéo tổ chức.
        var m = await db.Members.FirstOrDefaultAsync(x => x.Id == memberId) ?? throw new KeyNotFoundException();
        m.Points += points;
        if (m.Points < 0) m.Points = 0;                  // không cho điểm khả dụng âm (vd Adjust trừ quá số dư)
        if (points > 0) m.LifetimePoints += points;     // chỉ điểm dương mới tính xếp hạng
        await RecomputeRankAsync(m);
        var tx = new PointTransaction { MemberId = memberId, Type = type, Points = points, BalanceAfter = m.Points, Note = note, RefNo = refNo };
        db.PointTransactions.Add(tx);
        await db.SaveChangesAsync();
        return tx;
    }

    public Task<PointTransaction> EarnFromPurchaseAsync(int memberId, decimal amount, string? refNo)
    {
        var pts = (int)(amount / VndPerPoint);
        return EarnAsync(memberId, pts, PointTxType.Earn, $"Tích điểm mua hàng {amount:N0}đ", refNo);
    }

    public async Task<(bool ok, string msg)> RedeemAsync(int memberId, int rewardId)
    {
        var m = await db.Members.FirstOrDefaultAsync(x => x.Id == memberId) ?? throw new KeyNotFoundException();
        var r = await db.Rewards.FirstOrDefaultAsync(x => x.Id == rewardId);
        if (r == null || !r.IsActive) return (false, "Quà không khả dụng.");
        if (r.Stock <= 0) return (false, "Quà đã hết.");
        if (m.Points < r.PointCost) return (false, $"Không đủ điểm (cần {r.PointCost}, có {m.Points}).");

        m.Points -= r.PointCost;
        r.Stock--;
        db.PointTransactions.Add(new PointTransaction { MemberId = memberId, Type = PointTxType.Redeem, Points = -r.PointCost, BalanceAfter = m.Points, Note = $"Đổi: {r.Name}" });
        await db.SaveChangesAsync();
        return (true, $"Đã đổi \"{r.Name}\" (-{r.PointCost} điểm).");
    }

    public Task<List<RankTier>> RanksAsync() => db.RankTiers.OrderBy(t => t.SortOrder).ToListAsync();
    public Task<List<Reward>> RewardsAsync(bool activeOnly = true) =>
        (activeOnly ? db.Rewards.Where(r => r.IsActive) : db.Rewards).OrderBy(r => r.PointCost).ToListAsync();

    public async Task<LoyaltyDash> DashboardAsync()
    {
        var members = await db.Members.Include(m => m.RankTier).ToListAsync();
        var byRank = members.GroupBy(m => m.RankTier!)
            .OrderBy(g => g.Key.SortOrder)
            .Select(g => (g.Key.Name, g.Key.ColorHex, g.Count())).ToList();
        return new LoyaltyDash(members.Count, members.Sum(m => m.Points), members.Sum(m => m.LifetimePoints), byRank);
    }

    private async Task<RankTier> LowestRankAsync() =>
        (await db.RankTiers.OrderBy(t => t.SortOrder).FirstAsync());

    private async Task RecomputeRankAsync(Member m)
    {
        var tiers = await db.RankTiers.OrderBy(t => t.SortOrder).ToListAsync();
        var newTier = tiers.Last(t => m.LifetimePoints >= t.MinLifetimePoints);
        m.RankTierId = newTier.Id;
    }
}
