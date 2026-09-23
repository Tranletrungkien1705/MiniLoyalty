using Microsoft.EntityFrameworkCore;
using MiniLoyalty.Data;
using MiniLoyalty.Models;

namespace MiniLoyalty.Services;

public record LoyaltyDash(int Members, int ActivePoints, int LifetimeIssued,
    List<(string Rank, string Color, int Count)> ByRank);

/// <summary>Kết quả 1 lần chạy job tặng điểm sinh nhật.</summary>
public record BirthdayRunResult(int Awarded, int Points, List<(string Code, string Name, int Points)> Details);

/// <summary>Kết quả 1 lần chạy job hết hạn điểm.</summary>
public record ExpiryRunResult(DateTime Date, int Members, int Points, List<(string Code, string Name, int Points)> Details);

/// <summary>Kết quả 1 lần chạy job xét hạng cuối kỳ (nâng / duy trì / xuống hạng).</summary>
public record RankKeepDownRunResult(DateTime Date, int Up, int Kept, int Down, List<(string Code, string Name, string From, string To, string Action)> Details);

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
    Task<BirthdayRunResult> RunBirthdayJobAsync(DateTime? today = null);
    Task<ExpiryRunResult> RunExpiryJobAsync(DateTime? today = null);
    Task<RankKeepDownRunResult> RunRankKeepDownJobAsync(DateTime? today = null);
    Task<(bool ok, string msg)> AwardIntroductionAsync(int newMemberId);
    Task<PointTransaction> RecordServiceTurnAsync(int memberId, int qty, string? refNo);
    Task<MemberDiscountTransaction> ApplyServiceDiscountAsync(int memberId, decimal amount, string? refNo);
    Task<List<MemberDiscountTransaction>> DiscountsAsync(int memberId);
}

public class LoyaltyService(AppDbContext db) : ILoyaltyService
{
    public const int VndPerPoint = 1000;   // 1 điểm / 1.000đ
    public const int PointValidityMonths = 12;   // điểm cộng có hạn dùng 12 tháng (PointExpiryDTime)

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
        if (points > 0) m.LifetimePoints += points;     // chỉ điểm dương mới tính xếp hạng
        await RecomputeRankAsync(m);
        var tx = new PointTransaction { MemberId = memberId, Type = type, Points = points, BalanceAfter = m.Points, Note = note, RefNo = refNo };
        if (points > 0) tx.ExpiresAt = DateTime.Now.AddMonths(PointValidityMonths);   // điểm dương có hạn dùng
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

    /// <summary>
    /// Job tặng điểm sinh nhật (DealPointType = BIRTHDAY): tặng điểm cho hội viên có ngày sinh
    /// trùng ngày chạy (so khớp MM-dd), theo số điểm của hạng hiện tại. Mỗi hội viên chỉ nhận
    /// 1 lần/năm (chặn trùng bằng giao dịch BIRTHDAY đã có trong năm). Idempotent.
    /// </summary>
    public async Task<BirthdayRunResult> RunBirthdayJobAsync(DateTime? today = null)
    {
        var d = (today ?? DateTime.Today).Date;
        var year = d.Year;
        var members = await db.Members.Include(m => m.RankTier).ToListAsync();
        var details = new List<(string, string, int)>();
        var total = 0;

        foreach (var m in members)
        {
            if (m.Dob is not { } dob || dob.Month != d.Month || dob.Day != d.Day) continue;
            var pts = m.RankTier?.BirthdayPoints ?? 0;
            if (pts <= 0) continue;

            // Đã nhận điểm sinh nhật trong năm nay chưa? (chặn tặng trùng)
            var already = await db.PointTransactions.AnyAsync(t =>
                t.MemberId == m.Id && t.Type == PointTxType.Birthday && t.CreatedAt.Year == year);
            if (already) continue;

            await EarnAsync(m.Id, pts, PointTxType.Birthday, $"Tặng điểm sinh nhật {d:dd/MM} (hạng {m.RankTier?.Name})", null);
            details.Add((m.Code, m.Name, pts));
            total += pts;
        }
        return new BirthdayRunResult(details.Count, total, details);
    }

    /// <summary>
    /// Job hết hạn điểm (DealPointType = EXPIRY): tìm các giao dịch cộng điểm đã quá hạn
    /// (ExpiresAt <= ngày chạy) và chưa bị trừ hết, trừ phần điểm còn lại khỏi số dư hội viên.
    /// Idempotent: giao dịch đã hết hạn được đánh dấu bằng cách gán ExpiresAt = null sau khi xử lý.
    /// </summary>
    public async Task<ExpiryRunResult> RunExpiryJobAsync(DateTime? today = null)
    {
        var d = (today ?? DateTime.Today).Date;
        var details = new List<(string, string, int)>();
        var total = 0;

        // Các giao dịch cộng điểm đã tới hạn (chưa xử lý).
        var due = await db.PointTransactions
            .Where(t => t.Points > 0 && t.ExpiresAt != null && t.ExpiresAt <= d)
            .ToListAsync();

        foreach (var group in due.GroupBy(t => t.MemberId))
        {
            var m = await db.Members.FirstOrDefaultAsync(x => x.Id == group.Key);
            if (m == null) continue;

            var expired = group.Sum(t => t.Points);
            if (expired <= 0) continue;
            if (expired > m.Points) expired = m.Points;   // không trừ quá số dư khả dụng
            if (expired <= 0) continue;

            m.Points -= expired;
            db.PointTransactions.Add(new PointTransaction
            {
                MemberId = m.Id, Type = PointTxType.Expiry, Points = -expired, BalanceAfter = m.Points,
                Note = $"Điểm hết hạn ({group.Count()} giao dịch)", RefNo = $"EXP.{d:yyyyMMdd}"
            });
            // Đánh dấu đã xử lý để lần chạy sau không trừ lại.
            foreach (var t in group) t.ExpiresAt = null;

            details.Add((m.Code, m.Name, expired));
            total += expired;
        }
        if (details.Count > 0) await db.SaveChangesAsync();
        return new ExpiryRunResult(d, details.Count, total, details);
    }

    /// <summary>
    /// Job xét hạng cuối kỳ (WA_Crd_Card_RankKeepDownAuto / Crd_Card_CalcRank, CardRank.cs): với mỗi hội viên,
    /// so điểm xét hạng trong kỳ (PointCardRank) và số lượt dịch vụ (QtyVisitAvail) với ngưỡng của hạng hiện tại
    /// (Mst_RankPolicy). Thứ tự xét giống hệ nguồn: UP trước, rồi KEEP, cuối cùng DOWN.
    ///   - Đạt ngưỡng NÂNG (PointUpBegin/QtyVisitUpBegin) → UP: lên 1 hạng liền kề cao hơn (trần = hạng cao nhất).
    ///   - Không nâng nhưng đạt ngưỡng DUY TRÌ (PointKeepBegin/QtyVisitKeepBegin) → KEEP: giữ nguyên hạng.
    ///   - Không đạt → DOWN: tụt xuống 1 hạng liền kề thấp hơn (sàn là hạng thấp nhất).
    /// Sang kỳ mới: reset PointCardRank/QtyVisitAvail về 0, đặt EffDateStart/End cho kỳ kế tiếp,
    /// ghi CardSourceCode = UP/KEEP/DOWN. Idempotent theo kỳ: hội viên đã xét trong kỳ (EffDateEnd &gt;= ngày chạy) bị bỏ qua.
    /// </summary>
    public async Task<RankKeepDownRunResult> RunRankKeepDownJobAsync(DateTime? today = null)
    {
        var d = (today ?? DateTime.Today).Date;
        var tiers = await db.RankTiers.OrderBy(t => t.SortOrder).ToListAsync();
        var members = await db.Members.Include(m => m.RankTier).ToListAsync();
        var details = new List<(string, string, string, string, string)>();
        int up = 0, kept = 0, down = 0;

        foreach (var m in members)
        {
            // Đã xét kỳ này rồi? (kỳ hiện tại còn hiệu lực tới tương lai) → bỏ qua, tránh xét trùng.
            if (m.EffDateEnd is { } end && end.Date >= d) continue;

            var current = m.RankTier ?? tiers.First();
            var idx = tiers.FindIndex(t => t.Id == current.Id);
            if (idx < 0) idx = 0;

            // Đạt ngưỡng NÂNG hạng của hạng hiện tại? (chỉ khi còn hạng cao hơn để lên)
            var canUp = idx < tiers.Count - 1
                && current.PointUpBegin > 0
                && m.PointCardRank >= current.PointUpBegin
                && m.QtyVisitAvail >= current.QtyVisitUpBegin;
            // Đạt ngưỡng DUY TRÌ hạng của hạng hiện tại?
            var canKeep = m.PointCardRank >= current.PointKeepBegin && m.QtyVisitAvail >= current.QtyVisitKeepBegin;

            RankTier target;
            string action;
            if (canUp) { target = tiers[idx + 1]; action = "UP"; up++; }
            else if (canKeep) { target = current; action = "KEEP"; kept++; }
            else { target = tiers[Math.Max(0, idx - 1)]; action = "DOWN"; down++; }

            m.RankTierId = target.Id;
            m.CardSourceCode = action;
            m.EffDateStart = d;
            m.EffDateEnd = d.AddMonths(12);   // kỳ xét hạng 12 tháng
            m.PointCardRank = 0;              // reset điểm xét hạng cho kỳ mới
            m.QtyVisitAvail = 0;              // reset lượt dịch vụ cho kỳ mới

            details.Add((m.Code, m.Name, current.Name, target.Name, action));
        }
        if (details.Count > 0) await db.SaveChangesAsync();
        return new RankKeepDownRunResult(d, up, kept, down, details);
    }

    /// <summary>
    /// Thưởng điểm giới thiệu (DealPointType = INTRODUCTION, Crd_Member_Finish): khi hội viên mới
    /// hoàn tất đăng ký và có khai báo người giới thiệu (MemberNoIntro) kèm số điểm thưởng (PointIntro),
    /// hệ thống cộng PointIntro điểm cho NGƯỜI GIỚI THIỆU (không phải hội viên mới).
    /// Idempotent: mỗi hội viên mới chỉ thưởng 1 lần (chặn bằng giao dịch INTRODUCTION đã có theo RefNo).
    /// </summary>
    public async Task<(bool ok, string msg)> AwardIntroductionAsync(int newMemberId)
    {
        var m = await db.Members.FirstOrDefaultAsync(x => x.Id == newMemberId);
        if (m == null) return (false, "Không tìm thấy hội viên.");
        if (string.IsNullOrWhiteSpace(m.MemberNoIntro)) return (false, "Hội viên không khai báo người giới thiệu.");
        if (m.PointIntro <= 0) return (false, "Không có điểm thưởng giới thiệu.");

        // Người giới thiệu phải là hội viên đã tồn tại (đã hoàn tất đăng ký).
        var referrer = await db.Members.FirstOrDefaultAsync(x => x.Code == m.MemberNoIntro);
        if (referrer == null) return (false, $"Không tìm thấy người giới thiệu {m.MemberNoIntro}.");
        if (referrer.Id == m.Id) return (false, "Không thể tự giới thiệu chính mình.");

        // Đã thưởng cho hội viên mới này chưa? (chặn thưởng trùng)
        var refNo = $"INT.{m.Code}";
        var already = await db.PointTransactions.AnyAsync(t =>
            t.MemberId == referrer.Id && t.Type == PointTxType.Introduction && t.RefNo == refNo);
        if (already) return (false, "Đã thưởng điểm giới thiệu cho hội viên này rồi.");

        await EarnAsync(referrer.Id, m.PointIntro, PointTxType.Introduction,
            $"Thưởng điểm giới thiệu hội viên {m.Code} ({m.Name})", refNo);
        return (true, $"Đã thưởng {m.PointIntro:N0} điểm cho người giới thiệu {referrer.Code} ({referrer.Name}).");
    }

    /// <summary>
    /// Ghi nhận lượt dịch vụ (DealPointType = SERVICETURN, Crd_DealSerRO_Add): hội viên đưa xe vào
    /// đại lý làm dịch vụ → cộng số lượt vào QtyVisitAvail (Crd_Card.QtyVisitAvail) để phục vụ xét hạng
    /// theo lượt. KHÔNG cộng điểm (PointChTotal = 0). Mỗi lượt ghi 1 giao dịch SERVICETURN.
    /// </summary>
    public async Task<PointTransaction> RecordServiceTurnAsync(int memberId, int qty, string? refNo)
    {
        if (qty <= 0) qty = 1;
        var m = await db.Members.FirstOrDefaultAsync(x => x.Id == memberId) ?? throw new KeyNotFoundException();
        m.QtyVisitAvail += qty;   // cộng lượt dịch vụ trong kỳ (không đổi điểm)
        var tx = new PointTransaction
        {
            MemberId = memberId, Type = PointTxType.ServiceTurn, Points = 0, QtyVisit = qty,
            BalanceAfter = m.Points, Note = $"Ghi nhận {qty} lượt dịch vụ", RefNo = refNo
        };
        db.PointTransactions.Add(tx);
        await db.SaveChangesAsync();
        return tx;
    }

    /// <summary>
    /// Chiết khấu dịch vụ (DealPointType = DISCOUNTRO, Crd_MemberDiscountTransaction): khi hội viên
    /// dùng dịch vụ, áp % chiết khấu theo hạng thẻ hiện tại (RankTier.DiscountPercent) lên doanh thu
    /// dịch vụ. Ghi 1 giao dịch chiết khấu (không đổi điểm). Tiền chiết khấu = doanh thu × %/100.
    /// </summary>
    public async Task<MemberDiscountTransaction> ApplyServiceDiscountAsync(int memberId, decimal amount, string? refNo)
    {
        if (amount <= 0) throw new ArgumentException("Doanh thu dịch vụ phải > 0.", nameof(amount));
        var m = await db.Members.Include(x => x.RankTier).FirstOrDefaultAsync(x => x.Id == memberId)
            ?? throw new KeyNotFoundException();
        var rate = m.RankTier?.DiscountPercent ?? 0;
        var discount = Math.Round(amount * rate / 100m, 0, MidpointRounding.AwayFromZero);
        var tx = new MemberDiscountTransaction
        {
            MemberId = memberId, RefNo = refNo, CardTypeApplyId = m.RankTierId,
            PolicyDiscountRate = rate, AmountForDC = amount, DiscountAmount = discount
        };
        db.MemberDiscountTransactions.Add(tx);
        await db.SaveChangesAsync();
        return tx;
    }

    public Task<List<MemberDiscountTransaction>> DiscountsAsync(int memberId) =>
        db.MemberDiscountTransactions.Include(d => d.CardTypeApply)
            .Where(d => d.MemberId == memberId)
            .OrderByDescending(d => d.CreatedAt).ToListAsync();

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
