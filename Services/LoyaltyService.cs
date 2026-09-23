using Microsoft.EntityFrameworkCore;
using MiniLoyalty.Data;
using MiniLoyalty.Models;

namespace MiniLoyalty.Services;

public record LoyaltyDash(int Members, int ActivePoints, int LifetimeIssued,
    List<(string Rank, string Color, int Count)> ByRank);

/// <summary>Kết quả 1 lần chạy job tặng điểm sinh nhật.</summary>
public record BirthdayRunResult(int Awarded, int Points, List<(string Code, string Name, int Points)> Details);

/// <summary>Kết quả 1 lần chạy job phát voucher sinh nhật (DealPointType=VOUCHERTSN).</summary>
public record BirthdayVoucherRunResult(DateTime Date, int Issued, int Points, List<(string Code, string Name, int Points, DateTime Expiry)> Details);

/// <summary>Kết quả 1 lần chạy job hết hạn điểm.</summary>
public record ExpiryRunResult(DateTime Date, int Members, int Points, List<(string Code, string Name, int Points)> Details);

/// <summary>Kết quả 1 lần chạy job xét hạng cuối kỳ (nâng / duy trì / xuống hạng).</summary>
public record RankKeepDownRunResult(DateTime Date, int Up, int Kept, int Down, List<(string Code, string Name, string From, string To, string Action)> Details);

/// <summary>Kết quả tính điểm khuyến mại bán hàng Creta (WA_Crd_MemberRegis_CalcPointBuyCreta).</summary>
public record CretaBuyCarResult(bool Eligible, int PointBuyCreta, string Reason);

/// <summary>
/// Kết quả tra cứu hội viên cho DMS (Crd_MemberController.GetDetailForDMS): tìm hội viên theo biển số (CarNo)
/// hoặc số khung (VIN), kèm cờ FlagIsDLQuery cho biết đại lý (DLCPCode) đã từng đăng ký/tra cứu hội viên này chưa
/// (dựa trên Map_QueryDealer_Member). Member = null nếu không tìm thấy.
/// </summary>
public record DmsMemberLookup(Member? Member, int FlagIsDLQuery);

/// <summary>
/// Kết quả tính điểm khuyến mại bán hàng Creta (WA_Crd_MemberRegis_CalcPointBuyCreta):
/// Eligible = hội viên đủ điều kiện nhận điểm; Points = số điểm HTV tặng (0 nếu không đủ điều kiện);
/// Reason = lý do (đủ điều kiện hoặc lý do không đủ).
/// </summary>
public record CretaBuyCarCheck(bool Eligible, int Points, string Reason);

public interface ILoyaltyService
{
    Task<List<Member>> MembersAsync(string? q, int? rankId);
    Task<Member?> GetAsync(int id);
    Task<Member?> GetByPhoneAsync(string phone);
    Task<DmsMemberLookup> GetDetailForDmsAsync(string? carNo, string? vin, string? dlcpCode);
    Task<int> CreateAsync(Member m);
    Task<PointTransaction> EarnAsync(int memberId, int points, PointTxType type, string? note, string? refNo);
    Task<PointTransaction> EarnFromPurchaseAsync(int memberId, decimal amount, string? refNo);
    Task<(bool ok, string msg)> RedeemAsync(int memberId, int rewardId);
    Task<List<RankTier>> RanksAsync();
    Task<List<Reward>> RewardsAsync(bool activeOnly = true);
    Task<LoyaltyDash> DashboardAsync();
    Task<BirthdayRunResult> RunBirthdayJobAsync(DateTime? today = null);
    Task<BirthdayVoucherRunResult> RunBirthdayVoucherJobAsync(DateTime? today = null);
    Task<ExpiryRunResult> RunExpiryJobAsync(DateTime? today = null);
    Task<RankKeepDownRunResult> RunRankKeepDownJobAsync(DateTime? today = null);
    Task<(bool ok, string msg)> AwardIntroductionAsync(int newMemberId);
    Task<PointTransaction> AwardBuyNewCarAsync(int memberId, int? points = null, string? refNo = null);
    Task<PointTransaction> AwardKmbhAsync(int memberId, int? points = null, string? refNo = null);
    Task<PointTransaction> AwardOpenCardAsync(int memberId, int? points = null, string? refNo = null);
    Task<PointTransaction> RecordServiceTurnAsync(int memberId, int qty, string? refNo);
    Task<PointTransaction> RecordConsumptionAsync(int memberId, decimal amount, string? refNo);
    Task<PointTransaction> RecordPointIncreaseAsync(int memberId, int rankPoints, string? refNo);
    Task<List<ServicePolicy>> ServicePoliciesAsync();
    Task<MemberDiscountTransaction> ApplyServiceDiscountAsync(int memberId, decimal amount, string? refNo);
    Task<List<MemberDiscountTransaction>> DiscountsAsync(int memberId);
    Task<MemberVoucherTransaction> AwardVoucherAsync(int memberId, int points, string? voucherCode, string? refNo, DateTime? expiry = null);
    Task<(bool ok, string msg)> UseVoucherAsync(int memberId, int points, string? voucherCode, string? refNo);
    Task<List<MemberVoucherTransaction>> VouchersAsync(int memberId);
    Task<List<Promotion>> PromotionsAsync(bool activeOnly = true);
    Task<(bool ok, string msg)> UsePromotionAsync(int memberId, int promotionId, string? refNo);
    Task<(bool ok, string msg)> RecordPromotionUseAsync(int memberId, int promotionId, int qty, string? refNo);
    Task<List<MemberPromotionUse>> PromotionUsesAsync(int memberId);
    Task<(bool ok, string msg)> InactivateMemberAsync(int memberId, string? remark, string? by = null);
    Task<PointTransaction> AdjustPointsBySupportAsync(int memberId, int points, string? reason, string? refNo);
    Task<List<MemberColumnChange>> ChangeableColumnsAsync();
    Task<List<MemberChangeRequest>> ChangeRequestsAsync(ChangeRequestStatus? status = null);
    Task<MemberChangeRequest?> ChangeRequestAsync(int id);
    Task<(bool ok, string msg, int id)> CreateChangeRequestAsync(int memberId, ChangeRequestType type, string? dlCode, string? remark, List<(string ColumnCode, string? ValueNew)> details);
    Task<(bool ok, string msg)> ApproveChangeRequestAsync(int requestId, string? remarkHtv, string? by = null, string? dlcpCode = null);
    Task<(bool ok, string msg)> FinishChangeRequestAsync(int requestId, string? remarkHtv, string? by = null, string? dlcpCode = null);
    Task<(bool ok, string msg)> RejectChangeRequestAsync(int requestId, string? remarkHtv, string? by = null, string? dlcpCode = null);
    /// <summary>Đơn vị duyệt (ApproveDLCode) đã khóa của đề nghị + cờ FlagApproveAF (user có quyền duyệt không).</summary>
    Task<(string? approveUnit, bool flagApproveAf)> ApproveUnitAsync(int requestId, string? dlcpCode);
    Task<List<CardException>> CardExceptionsAsync(CardExceptionStatus? status = null);
    Task<CardException?> CardExceptionAsync(int id);
    Task<(bool ok, string msg, int id)> RequestCardExceptionAsync(int memberId, string? dlCodeExceptionally, string? remark, List<string> dealerCodes);
    Task<(bool ok, string msg)> ApproveCardExceptionAsync(int id, string? remarkHtv, string? by = null);
    Task<(bool ok, string msg)> RejectCardExceptionAsync(int id, string? remarkHtv, string? by = null);
    Task<List<RankHistory>> RankHistoryAsync(int? memberId = null);
    Task<List<MemberRegister>> MemberRegistersAsync(MemberRegisterStatus? status = null);
    Task<MemberRegister?> MemberRegisterAsync(int id);
    Task<(bool ok, string msg, int id)> CreateMemberRegisterAsync(MemberRegister req);
    Task<(bool ok, string msg)> ApproveMemberRegisterAsync(int id, string? remark, string? by = null);
    Task<(bool ok, string msg)> FinishMemberRegisterAsync(int id, string? by = null);
    Task<(bool ok, string msg)> CancelMemberRegisterAsync(int id, string? remark, string? by = null);
    Task<(bool ok, string msg)> RejectMemberRegisterAsync(int id, string? remark, string? by = null);
    Task<List<DealerMemberLink>> DealerMemberLinksAsync(string? dlcpCode = null, int? memberId = null);
    Task<(bool ok, string msg, int id)> LinkDealerMemberAsync(string dlcpCode, int memberId, int networkId = 0, string? remark = null, string? by = null);
    Task<List<PrmCarNew>> PrmCarNewsAsync(PrmCarNewStatus? status = null, string? dlcpCode = null);
    Task<PrmCarNew?> PrmCarNewAsync(int id);
    Task<(bool ok, string msg, int id)> CreatePrmCarNewAsync(PrmCarNew prm);
    Task<(bool ok, string msg)> ApprovePrmCarNewAsync(int id, string? remark, string? by = null);
    Task<(bool ok, string msg)> FinishPrmCarNewAsync(int id, string? remark, string? by = null);
    Task<(bool ok, string msg)> CancelPrmCarNewAsync(int id, string? remark, string? by = null);
    Task<PrmCarNew?> CalcPrmCarNewAsync(string dlcpCode, string? modelCode, DateTime? today = null);
    Task<List<PrmCarRecommend>> PrmCarRecommendsAsync(PrmCarRecommendStatus? status = null, string? dlcpCode = null);
    Task<PrmCarRecommend?> PrmCarRecommendAsync(int id);
    Task<(bool ok, string msg, int id)> CreatePrmCarRecommendAsync(PrmCarRecommend prm);
    Task<(bool ok, string msg)> ApprovePrmCarRecommendAsync(int id, string? remark, string? by = null);
    Task<(bool ok, string msg)> FinishPrmCarRecommendAsync(int id, string? remark, string? by = null);
    Task<(bool ok, string msg)> CancelPrmCarRecommendAsync(int id, string? remark, string? by = null);
    Task<PrmCarRecommend?> CalcPrmCarRecommendAsync(string dlcpCode, string? modelCode, DateTime? today = null);
    Task<CretaBuyCarResult> CalcPointBuyCretaAsync(string? modelCode, string? cardTypeUse, DateTime? deliveryDate, string? idCardNo, string? dealNo, int? memberId = null);
    Task<List<ExpenseTypePolicy>> ExpenseTypePoliciesAsync();
    Task<(bool ok, string msg, int id)> SaveExpenseTypePolicyAsync(ExpenseTypePolicy policy);
    Task<List<PrmVoucherNewCar>> PrmVoucherNewCarsAsync(PrmVoucherNewCarStatus? status = null);
    Task<PrmVoucherNewCar?> PrmVoucherNewCarAsync(int id);
    Task<(bool ok, string msg, int id)> CreatePrmVoucherNewCarAsync(PrmVoucherNewCar prm);
    Task<(bool ok, string msg)> ApprovePrmVoucherNewCarAsync(int id, string? remark, string? by = null);
    Task<(bool ok, string msg)> FinishPrmVoucherNewCarAsync(int id, string? remark, string? by = null);
    Task<(bool ok, string msg)> CancelPrmVoucherNewCarAsync(int id, string? remark, string? by = null);
    Task<PrmVoucherNewCar?> CalcPrmVoucherNewCarAsync(string? modelCode, DateTime? today = null);
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

    /// <summary>
    /// Tra cứu hội viên cho DMS (Crd_MemberController.GetDetailForDMS): tìm hội viên theo biển số (CarNo)
    /// HOẶC số khung (VIN) — khớp chính xác. Kèm FlagIsDLQuery = 1 nếu đại lý (DLCPCode) đã có liên kết
    /// Map_QueryDealer_Member với hội viên này (đã đăng ký/tra cứu), ngược lại = 0. Trả Member = null nếu không thấy.
    /// </summary>
    public async Task<DmsMemberLookup> GetDetailForDmsAsync(string? carNo, string? vin, string? dlcpCode)
    {
        carNo = carNo?.Trim(); vin = vin?.Trim();
        if (string.IsNullOrEmpty(carNo) && string.IsNullOrEmpty(vin))
            return new DmsMemberLookup(null, 0);

        var m = await db.Members.Include(x => x.RankTier).FirstOrDefaultAsync(x =>
            (!string.IsNullOrEmpty(carNo) && x.CarNo == carNo) ||
            (!string.IsNullOrEmpty(vin) && x.VIN == vin));
        if (m == null) return new DmsMemberLookup(null, 0);

        // FlagIsDLQuery: đại lý đã từng đăng ký/tra cứu hội viên này chưa (Map_QueryDealer_Member).
        var flag = 0;
        if (!string.IsNullOrWhiteSpace(dlcpCode))
            flag = await db.DealerMemberLinks.AnyAsync(l => l.DLCPCode == dlcpCode && l.MemberId == m.Id) ? 1 : 0;
        return new DmsMemberLookup(m, flag);
    }

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
    /// Job phát voucher sinh nhật (DealPointType = VOUCHERTSN, Crd_MemberVoucherTransaction):
    /// với hội viên có ngày sinh trùng ngày chạy (so khớp MM-dd) và hạng hiện tại có cấu hình voucher
    /// (Mst_BirthPolicyDtl.VoucherValue &gt; 0), phát 1 voucher/năm do đại lý SUPPORT phát hành:
    /// cộng VoucherValue điểm vào Member.PointVoucher (không dùng xét hạng), hạn dùng = ngày chạy + VoucherExpireDays.
    /// Idempotent: mỗi hội viên chỉ nhận 1 voucher/năm (chặn bằng giao dịch VOUCHERTSN đã có theo RefNo "BVS.&lt;năm&gt;.&lt;mã&gt;").
    /// </summary>
    public async Task<BirthdayVoucherRunResult> RunBirthdayVoucherJobAsync(DateTime? today = null)
    {
        var d = (today ?? DateTime.Today).Date;
        var year = d.Year;
        var members = await db.Members.Include(m => m.RankTier).ToListAsync();
        var details = new List<(string, string, int, DateTime)>();
        var total = 0;

        foreach (var m in members)
        {
            if (m.Dob is not { } dob || dob.Month != d.Month || dob.Day != d.Day) continue;
            var pts = m.RankTier?.BirthdayVoucherPoints ?? 0;
            if (pts <= 0) continue;   // hạng không cấu hình voucher → không phát
            // Đã nhận voucher sinh nhật trong năm nay chưa? (chặn phát trùng)
            var refNo = $"BVS.{year}.{m.Code}";
            var already = await db.MemberVoucherTransactions.AnyAsync(t =>
                t.MemberId == m.Id && t.Type == VoucherTxType.BirthdayVoucher && t.RefNo == refNo);
            if (already) continue;

            var days = m.RankTier?.BirthdayVoucherExpireDays ?? 0;
            var expiry = d.AddDays(days);
            m.PointVoucher += pts;
            db.MemberVoucherTransactions.Add(new MemberVoucherTransaction
            {
                MemberId = m.Id, Type = VoucherTxType.BirthdayVoucher, Points = pts, BalanceAfter = m.PointVoucher,
                VoucherCode = $"BV.{year}.{m.Code}", RefNo = refNo, ExpiryDate = expiry,
                Note = $"Voucher sinh nhật {d:dd/MM} (hạng {m.RankTier?.Name}, đại lý SUPPORT)"
            });
            details.Add((m.Code, m.Name, pts, expiry));
            total += pts;
        }
        if (details.Count > 0) await db.SaveChangesAsync();
        return new BirthdayVoucherRunResult(d, details.Count, total, details);
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

            // Ảnh chụp TRƯỚC khi xét (Crd_MemberBefore/Crd_CardBefore) — lấy trước khi reset kỳ mới.
            var pointCardRankBefore = m.PointCardRank;
            var qtyVisitBefore = m.QtyVisitAvail;

            m.RankTierId = target.Id;
            m.CardSourceCode = action;
            m.EffDateStart = d;
            m.EffDateEnd = d.AddMonths(12);   // kỳ xét hạng 12 tháng
            m.PointCardRank = 0;              // reset điểm xét hạng cho kỳ mới
            m.QtyVisitAvail = 0;              // reset lượt dịch vụ cho kỳ mới

            // Ghi lịch sử xét hạng (Crd_CardRank, DealPointType=LOYALTY): audit trail UP/KEEP/DOWN
            // kèm ảnh chụp TRƯỚC/SAU của hội viên — để tra cứu vì sao lên/xuống hạng kỳ này.
            db.RankHistories.Add(new RankHistory
            {
                CardRankNo = $"CRK.{d:yyyyMMdd}.{m.Code}",
                MemberId = m.Id,
                RankPolicyCode = "DEFAULT",
                Action = action switch { "UP" => RankActionType.Up, "KEEP" => RankActionType.Keep, _ => RankActionType.Down },
                CardSourceCode = action,
                DealPointType = "LOYALTY",
                FunctionName = "RunRankKeepDownJobAsync",
                FunctionRemark = $"Xét hạng cuối kỳ {d:dd/MM/yyyy}: {current.Name} → {target.Name}",
                RankTierIdBefore = current.Id,
                PointCardRankBefore = pointCardRankBefore,
                QtyVisitBefore = qtyVisitBefore,
                RankTierIdAfter = target.Id,
                PointCardRankAfter = 0,
                QtyVisitAfter = 0,
                EffDateStart = m.EffDateStart,
                EffDateEnd = m.EffDateEnd,
                CreatedAt = DateTime.Now,
                CreatedBy = "SYSTEM"
            });

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
    /// Tặng điểm mua xe mới (DealPointType = SALES, Crd_Member_PerformBuyNewCarX): khi hội viên mua xe mới,
    /// hệ thống cộng số điểm thưởng mua xe (Crd_Member.PointBuyCar) vào điểm khả dụng + điểm tích lũy trọn đời
    /// (điểm dương nên có ảnh hưởng xét hạng), ghi 1 giao dịch SALES. Điểm có hạn dùng cuối năm kế tiếp.
    /// Idempotent: mỗi hội viên chỉ thưởng 1 lần (chặn bằng giao dịch SALES đã có theo RefNo).
    /// </summary>
    public async Task<PointTransaction> AwardBuyNewCarAsync(int memberId, int? points = null, string? refNo = null)
    {
        var m = await db.Members.FirstOrDefaultAsync(x => x.Id == memberId) ?? throw new KeyNotFoundException();
        var pts = points ?? m.PointBuyCar;
        if (pts <= 0) throw new ArgumentException("Điểm thưởng mua xe phải > 0.", nameof(points));

        var rn = string.IsNullOrWhiteSpace(refNo) ? $"NEW.{m.Code}" : refNo;
        // Đã thưởng điểm mua xe cho hội viên này chưa? (chặn thưởng trùng)
        var already = await db.PointTransactions.AnyAsync(t =>
            t.MemberId == m.Id && t.Type == PointTxType.Sale && t.RefNo == rn);
        if (already) throw new InvalidOperationException("Đã thưởng điểm mua xe cho hội viên này rồi.");

        return await EarnAsync(m.Id, pts, PointTxType.Sale, $"Tặng điểm mua xe mới ({pts:N0} điểm)", rn);
    }

    /// <summary>
    /// Điểm khuyến mại bán hàng (DealPointType = KMBH, Crd_Member_PerformBuyCretaX): HTV (nhà sản xuất)
    /// tặng thêm điểm cho hội viên mua xe thuộc chương trình khuyến mại. Cộng số điểm khuyến mại
    /// (Crd_Member.PointBuyCreta) vào điểm khả dụng + điểm tích lũy trọn đời (điểm dương nên ảnh hưởng
    /// xét hạng), ghi 1 giao dịch KMBH. Điểm có hạn dùng cuối năm kế tiếp (PointExpiryDTime).
    /// Idempotent: mỗi hội viên chỉ thưởng 1 lần (chặn bằng giao dịch KMBH đã có theo RefNo).
    /// </summary>
    public async Task<PointTransaction> AwardKmbhAsync(int memberId, int? points = null, string? refNo = null)
    {
        var m = await db.Members.FirstOrDefaultAsync(x => x.Id == memberId) ?? throw new KeyNotFoundException();
        var pts = points ?? m.PointBuyCreta;
        if (pts <= 0) throw new ArgumentException("Điểm khuyến mại bán hàng phải > 0.", nameof(points));

        var rn = string.IsNullOrWhiteSpace(refNo) ? $"KMBH.{m.Code}" : refNo;
        // Đã thưởng điểm khuyến mại cho hội viên này chưa? (chặn thưởng trùng)
        var already = await db.PointTransactions.AnyAsync(t =>
            t.MemberId == m.Id && t.Type == PointTxType.Kmbh && t.RefNo == rn);
        if (already) throw new InvalidOperationException("Đã thưởng điểm khuyến mại bán hàng cho hội viên này rồi.");

        return await EarnAsync(m.Id, pts, PointTxType.Kmbh, $"Điểm khuyến mại bán hàng HTV ({pts:N0} điểm)", rn);
    }

    /// <summary>
    /// Tặng điểm mở thẻ mới (DealPointType = OPENCARD, Crd_Member_PerformOpenCardX / Crd_Member_Finish):
    /// khi hội viên hoàn tất đăng ký (Finish), hệ thống tặng số điểm chào mừng (Crd_Member.PointOpenCard)
    /// do đại lý HTV phát hành. Cộng điểm khả dụng + điểm tích lũy trọn đời (điểm dương nên ảnh hưởng
    /// xét hạng), ghi 1 giao dịch OPENCARD. Điểm có hạn dùng cuối năm kế tiếp (PointExpiryDTime).
    /// Idempotent: mỗi hội viên chỉ tặng 1 lần (chặn bằng giao dịch OPENCARD đã có theo RefNo).
    /// </summary>
    public async Task<PointTransaction> AwardOpenCardAsync(int memberId, int? points = null, string? refNo = null)
    {
        var m = await db.Members.FirstOrDefaultAsync(x => x.Id == memberId) ?? throw new KeyNotFoundException();
        var pts = points ?? m.PointOpenCard;
        if (pts <= 0) throw new ArgumentException("Điểm tặng mở thẻ phải > 0.", nameof(points));

        var rn = string.IsNullOrWhiteSpace(refNo) ? $"MT.{m.Code}" : refNo;
        // Đã tặng điểm mở thẻ cho hội viên này chưa? (chặn tặng trùng)
        var already = await db.PointTransactions.AnyAsync(t =>
            t.MemberId == m.Id && t.Type == PointTxType.OpenCard && t.RefNo == rn);
        if (already) throw new InvalidOperationException("Đã tặng điểm mở thẻ cho hội viên này rồi.");

        return await EarnAsync(m.Id, pts, PointTxType.OpenCard, $"Tặng điểm mở thẻ mới ({pts:N0} điểm, đại lý HTV)", rn);
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
    /// Tích điểm tiêu dùng (DealPointType = CONSUMPTION, Crd_DealSerRO_Add / Crd_CardTransaction_InCrease_Save):
    /// hội viên đưa xe vào đại lý làm dịch vụ (RO) → quy đổi doanh thu dịch vụ thành điểm theo chính sách
    /// của hạng thẻ hiện tại (Mst_PolicyMoneyToPointServiceDtl: cứ ConvertValue đồng = ConvertPoint điểm).
    /// Cộng điểm khả dụng + điểm tích lũy trọn đời (điểm dương nên ảnh hưởng xét hạng), cộng 1 lượt dịch vụ
    /// (QtyVisitAvail), ghi 1 giao dịch CONSUMPTION kèm doanh thu (AmountChTotal) + điểm xét hạng (PointChRankTotal).
    /// Điểm cộng có hạn dùng 12 tháng (PointExpiryDTime).
    /// </summary>
    public async Task<PointTransaction> RecordConsumptionAsync(int memberId, decimal amount, string? refNo)
    {
        if (amount <= 0) throw new ArgumentException("Doanh thu dịch vụ phải > 0.", nameof(amount));
        var m = await db.Members.Include(x => x.RankTier).FirstOrDefaultAsync(x => x.Id == memberId)
            ?? throw new KeyNotFoundException();

        // Chính sách quy đổi của hạng hiện tại (mặc định 1.000đ = 1 điểm nếu chưa cấu hình).
        var policy = await db.ServicePolicies.FirstOrDefaultAsync(p => p.RankTierId == m.RankTierId && p.IsActive);
        var convertValue = policy?.ConvertValue > 0 ? policy.ConvertValue : VndPerPoint;
        var convertPoint = policy?.ConvertPoint > 0 ? policy.ConvertPoint : 1;
        var pts = (int)Math.Floor(amount / convertValue * convertPoint);

        m.Points += pts;
        m.LifetimePoints += pts;      // điểm dương → tính xếp hạng
        m.PointCardRank += pts;       // điểm xét hạng tích trong kỳ
        m.QtyVisitAvail += 1;         // mỗi lần vào xưởng = 1 lượt dịch vụ
        await RecomputeRankAsync(m);

        var tx = new PointTransaction
        {
            MemberId = memberId, Type = PointTxType.Consumption, Points = pts, BalanceAfter = m.Points,
            AmountChTotal = amount, PointChRankTotal = pts, QtyVisit = 1,
            Note = $"Tích điểm tiêu dùng dịch vụ {amount:N0}đ ({convertValue:N0}đ = {convertPoint} điểm)",
            RefNo = refNo, ExpiresAt = DateTime.Now.AddMonths(PointValidityMonths)
        };
        db.PointTransactions.Add(tx);
        await db.SaveChangesAsync();
        return tx;
    }

    public Task<List<ServicePolicy>> ServicePoliciesAsync() =>
        db.ServicePolicies.Include(p => p.RankTier).OrderBy(p => p.RankTierId).ToListAsync();

    /// <summary>
    /// Tích điểm xét hạng nhập tay (DealPointType = POINTINCREASE, Crd_CardTransactionInCrease):
    /// cộng thêm ĐIỂM XÉT HẠNG (PointChRankTotal) cho hội viên ngoài luồng tự động — dùng khi hỗ trợ
    /// migrate dữ liệu hoặc bù điểm bị thiếu. KHÔNG cộng điểm khả dụng (PointChTotal = 0), chỉ cộng
    /// điểm xét hạng tích trong kỳ (Member.PointCardRank) để phục vụ xét hạng cuối kỳ.
    /// Ghi 1 giao dịch POINTINCREASE (Points = 0, PointChRankTotal = rankPoints).
    /// </summary>
    public async Task<PointTransaction> RecordPointIncreaseAsync(int memberId, int rankPoints, string? refNo)
    {
        if (rankPoints <= 0) throw new ArgumentException("Điểm xét hạng cộng thêm phải > 0.", nameof(rankPoints));
        var m = await db.Members.FirstOrDefaultAsync(x => x.Id == memberId) ?? throw new KeyNotFoundException();
        m.PointCardRank += rankPoints;   // chỉ cộng điểm xét hạng trong kỳ, KHÔNG đổi điểm khả dụng
        var tx = new PointTransaction
        {
            MemberId = memberId, Type = PointTxType.PointIncrease, Points = 0, PointChRankTotal = rankPoints,
            BalanceAfter = m.Points, Note = $"Tích điểm xét hạng (hỗ trợ) +{rankPoints:N0} điểm", RefNo = refNo
        };
        db.PointTransactions.Add(tx);
        await db.SaveChangesAsync();
        return tx;
    }

    /// <summary>
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

    /// <summary>
    /// Tặng điểm voucher xe mới (DealPointType = VOUCHERXM, Crd_MemberVoucherTransaction): HTV tặng
    /// điểm voucher cho hội viên mua xe mới theo chương trình. Cộng vào Member.PointVoucher
    /// (Crd_Member.PointVoucher) — tách biệt điểm tiêu dùng, KHÔNG dùng để xét hạng.
    /// </summary>
    public async Task<MemberVoucherTransaction> AwardVoucherAsync(int memberId, int points, string? voucherCode, string? refNo, DateTime? expiry = null)
    {
        if (points <= 0) throw new ArgumentException("Điểm voucher phải > 0.", nameof(points));
        var m = await db.Members.FirstOrDefaultAsync(x => x.Id == memberId) ?? throw new KeyNotFoundException();
        m.PointVoucher += points;
        var tx = new MemberVoucherTransaction
        {
            MemberId = memberId, Type = VoucherTxType.Award, Points = points, BalanceAfter = m.PointVoucher,
            VoucherCode = voucherCode, RefNo = refNo, ExpiryDate = expiry,
            Note = $"Tặng điểm voucher xe mới{(string.IsNullOrWhiteSpace(voucherCode) ? "" : $" ({voucherCode})")}"
        };
        db.MemberVoucherTransactions.Add(tx);
        await db.SaveChangesAsync();
        return tx;
    }

    /// <summary>
    /// Sử dụng điểm voucher (DealPointType = VOUCHERSD, Crd_MemberVoucherTransaction): hội viên dùng
    /// điểm voucher để quy đổi tại đại lý. Trừ vào Member.PointVoucher; chặn nếu vượt số dư voucher.
    /// </summary>
    public async Task<(bool ok, string msg)> UseVoucherAsync(int memberId, int points, string? voucherCode, string? refNo)
    {
        if (points <= 0) return (false, "Số điểm voucher sử dụng phải > 0.");
        var m = await db.Members.FirstOrDefaultAsync(x => x.Id == memberId);
        if (m == null) return (false, "Không tìm thấy hội viên.");
        if (m.PointVoucher < points) return (false, $"Không đủ điểm voucher (cần {points:N0}, có {m.PointVoucher:N0}).");

        m.PointVoucher -= points;
        db.MemberVoucherTransactions.Add(new MemberVoucherTransaction
        {
            MemberId = memberId, Type = VoucherTxType.Use, Points = -points, BalanceAfter = m.PointVoucher,
            VoucherCode = voucherCode, RefNo = refNo,
            Note = $"Sử dụng điểm voucher{(string.IsNullOrWhiteSpace(voucherCode) ? "" : $" ({voucherCode})")}"
        });
        await db.SaveChangesAsync();
        return (true, $"Đã sử dụng {points:N0} điểm voucher. Còn lại {m.PointVoucher:N0}.");
    }

    public Task<List<MemberVoucherTransaction>> VouchersAsync(int memberId) =>
        db.MemberVoucherTransactions
            .Where(v => v.MemberId == memberId)
            .OrderByDescending(v => v.CreatedAt).ToListAsync();

    public Task<List<Promotion>> PromotionsAsync(bool activeOnly = true) =>
        (activeOnly ? db.Promotions.Where(p => p.IsActive) : db.Promotions).OrderBy(p => p.PointCost).ToListAsync();

    /// <summary>
    /// Sử dụng điểm đổi ưu đãi (DealPointType = POINTUSE, Crd_DealUsePromotion / WA_Crd_DealUsePromotion_Save):
    /// hội viên dùng điểm khả dụng (PointAvail) để đổi một chương trình ưu đãi tại đại lý.
    /// Trừ điểm khả dụng theo giá điểm của ưu đãi, giảm số lượng ưu đãi còn lại, ghi 1 giao dịch POINTUSE
    /// (PointChTotal &lt; 0) kèm mã ưu đãi (PrProgramCode). KHÔNG đổi điểm tích lũy trọn đời (không ảnh hưởng xét hạng).
    /// </summary>
    public async Task<(bool ok, string msg)> UsePromotionAsync(int memberId, int promotionId, string? refNo)
    {
        var m = await db.Members.FirstOrDefaultAsync(x => x.Id == memberId);
        if (m == null) return (false, "Không tìm thấy hội viên.");
        var p = await db.Promotions.FirstOrDefaultAsync(x => x.Id == promotionId);
        if (p == null || !p.IsActive) return (false, "Ưu đãi không khả dụng.");
        if (p.Qty <= 0) return (false, "Ưu đãi đã hết.");
        if (m.Points < p.PointCost) return (false, $"Không đủ điểm (cần {p.PointCost:N0}, có {m.Points:N0}).");

        m.Points -= p.PointCost;   // trừ điểm khả dụng (PointAvail); KHÔNG đụng LifetimePoints
        p.Qty--;
        db.PointTransactions.Add(new PointTransaction
        {
            MemberId = memberId, Type = PointTxType.PointUse, Points = -p.PointCost, BalanceAfter = m.Points,
            Note = $"Sử dụng ưu đãi: {p.Name}", RefNo = refNo
        });
        db.MemberPromotionUses.Add(new MemberPromotionUse
        {
            MemberId = memberId, PromotionId = p.Id, PrProgramCode = p.Code,
            Points = -p.PointCost, BalanceAfter = m.Points, RefNo = refNo,
            Note = $"Sử dụng ưu đãi {p.Code} ({p.Name})"
        });
        await db.SaveChangesAsync();
        return (true, $"Đã sử dụng ưu đãi \"{p.Name}\" (-{p.PointCost:N0} điểm). Còn lại {m.Points:N0} điểm.");
    }

    /// <summary>
    /// Ghi nhận sử dụng ưu đãi KHÔNG trừ điểm (DealPointType = PRPROGRAM, Crd_DealUsePromotion):
    /// hội viên dùng một chương trình ưu đãi (voucher giảm giá, tặng phụ kiện...) mà ưu đãi không quy về điểm.
    /// Chỉ GHI NHẬN số lượng ưu đãi đã dùng (QtyPrChTotal/QtyPrUsed) để tracking, KHÔNG đổi điểm khả dụng
    /// (PointChTotal = 0) và KHÔNG đổi điểm tích lũy trọn đời. Giảm số lượng ưu đãi còn lại (Qty).
    /// </summary>
    public async Task<(bool ok, string msg)> RecordPromotionUseAsync(int memberId, int promotionId, int qty, string? refNo)
    {
        if (qty <= 0) qty = 1;
        var m = await db.Members.FirstOrDefaultAsync(x => x.Id == memberId);
        if (m == null) return (false, "Không tìm thấy hội viên.");
        var p = await db.Promotions.FirstOrDefaultAsync(x => x.Id == promotionId);
        if (p == null || !p.IsActive) return (false, "Ưu đãi không khả dụng.");
        if (p.Qty < qty) return (false, $"Ưu đãi không đủ số lượng (cần {qty}, còn {p.Qty}).");

        p.Qty -= qty;   // giảm số lượng ưu đãi còn lại; KHÔNG trừ điểm
        db.PointTransactions.Add(new PointTransaction
        {
            MemberId = memberId, Type = PointTxType.PrProgram, Points = 0, BalanceAfter = m.Points,
            PrProgramCode = p.Code, QtyPrChTotal = qty, QtyPrUsed = qty,
            Note = $"Ghi nhận sử dụng ưu đãi: {p.Name} (x{qty})", RefNo = refNo
        });
        db.MemberPromotionUses.Add(new MemberPromotionUse
        {
            MemberId = memberId, PromotionId = p.Id, Kind = PromotionUseKind.PrProgram, PrProgramCode = p.Code,
            Points = 0, BalanceAfter = m.Points, QtyPrChTotal = qty, QtyPrUsed = qty, RefNo = refNo,
            Note = $"Ghi nhận sử dụng ưu đãi {p.Code} ({p.Name}) x{qty}"
        });
        await db.SaveChangesAsync();
        return (true, $"Đã ghi nhận sử dụng ưu đãi \"{p.Name}\" (x{qty}), không trừ điểm.");
    }

    public Task<List<MemberPromotionUse>> PromotionUsesAsync(int memberId) =>
        db.MemberPromotionUses.Include(u => u.PromotionNav)
            .Where(u => u.MemberId == memberId)
            .OrderByDescending(u => u.CreatedAt).ToListAsync();

    /// <summary>
    /// Vô hiệu hoá hội viên (Crd_Member_InActiveX, Card.cs): khi hội viên ngừng tham gia (ví dụ bán xe cũ
    /// cho khách khác), hệ thống:
    ///   1. Đặt MemberStatus = Cancel, ghi InactiveAt/InactiveBy/Remark (audit).
    ///   2. Huỷ thẻ đang hiệu lực (CardStatus = Cancel).
    ///   3. Đặt toàn bộ điểm còn lại hết hạn ngay: các giao dịch cộng điểm chưa xử lý (ExpiresAt &gt; cuối tháng)
    ///      được dời hạn về cuối tháng hiện tại (PointExpiryDTime = StdDateEndOfMonth), để job hết hạn điểm
    ///      thu hồi phần điểm còn lại. KHÔNG trừ điểm trực tiếp ở đây (giữ đúng luồng hệ nguồn).
    /// Idempotent: hội viên đã ở trạng thái Cancel thì bỏ qua.
    /// </summary>
    public async Task<(bool ok, string msg)> InactivateMemberAsync(int memberId, string? remark, string? by = null)
    {
        var m = await db.Members.FirstOrDefaultAsync(x => x.Id == memberId);
        if (m == null) return (false, "Không tìm thấy hội viên.");
        if (m.Status == MemberStatus.Cancel) return (false, "Hội viên đã ở trạng thái vô hiệu hoá.");

        var now = DateTime.Now;
        var endOfMonth = new DateTime(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month));

        m.Status = MemberStatus.Cancel;      // Crd_Member.MemberStatus = CANCEL
        m.CardStatus = CardStatus.Cancel;    // Crd_Card.CardStatus = CANCEL (huỷ thẻ đang hiệu lực)
        m.InactiveAt = now;                  // Crd_Member.InactiveDTimeUTC
        m.InactiveBy = by;                   // Crd_Member.InactiveBy
        m.Remark = remark;                   // Crd_Member.Remark

        // Dời hạn các giao dịch cộng điểm chưa xử lý về cuối tháng → job hết hạn sẽ thu hồi điểm còn lại.
        var pending = await db.PointTransactions
            .Where(t => t.MemberId == m.Id && t.Points > 0 && t.ExpiresAt != null && t.ExpiresAt > endOfMonth)
            .ToListAsync();
        foreach (var t in pending) t.ExpiresAt = endOfMonth;

        await db.SaveChangesAsync();
        return (true, $"Đã vô hiệu hoá hội viên {m.Code} ({m.Name}); huỷ thẻ và đặt {pending.Count} giao dịch điểm hết hạn cuối tháng.");
    }

    /// <summary>
    /// Điều chỉnh điểm hỗ trợ (DealPointType = SUPPORT, Crd_CardTransaction): nhân viên hỗ trợ idocNet
    /// cộng/trừ điểm thủ công cho hội viên trong các trường hợp đặc biệt (khiếu nại, sai sót, bù điểm).
    /// Ghi 1 giao dịch SUPPORT với DLCode = "SUPPORT" (đại lý hỗ trợ) và FunctionRemark = lý do điều chỉnh (audit).
    /// Điểm dương cộng vào điểm khả dụng + điểm tích lũy trọn đời (ảnh hưởng xét hạng) và có hạn dùng 12 tháng;
    /// điểm âm chỉ trừ điểm khả dụng (không đụng điểm tích lũy trọn đời), chặn nếu vượt số dư.
    /// </summary>
    public async Task<PointTransaction> AdjustPointsBySupportAsync(int memberId, int points, string? reason, string? refNo)
    {
        if (points == 0) throw new ArgumentException("Số điểm điều chỉnh phải khác 0.", nameof(points));
        var m = await db.Members.FirstOrDefaultAsync(x => x.Id == memberId) ?? throw new KeyNotFoundException();
        if (points < 0 && m.Points + points < 0)
            throw new InvalidOperationException($"Không đủ điểm để trừ (cần {-points:N0}, có {m.Points:N0}).");

        m.Points += points;
        if (points > 0) m.LifetimePoints += points;   // chỉ điểm dương mới tính xếp hạng
        await RecomputeRankAsync(m);

        var tx = new PointTransaction
        {
            MemberId = memberId, Type = PointTxType.Support, Points = points, BalanceAfter = m.Points,
            DLCode = "SUPPORT", FunctionRemark = reason,
            Note = $"Điều chỉnh điểm hỗ trợ {(points > 0 ? "+" : "")}{points:N0}{(string.IsNullOrWhiteSpace(reason) ? "" : $" — {reason}")}",
            RefNo = refNo
        };
        if (points > 0) tx.ExpiresAt = DateTime.Now.AddMonths(PointValidityMonths);   // điểm dương có hạn dùng
        db.PointTransactions.Add(tx);
        await db.SaveChangesAsync();
        return tx;
    }

    /// <summary>
    /// Danh mục cột được phép đề nghị thay đổi (Crd_MemberColumnChange): whitelist các cột của Crd_Member
    /// mà hội viên/đại lý được phép đổi qua yêu cầu Crd_MemberChangeInfo.
    /// </summary>
    public Task<List<MemberColumnChange>> ChangeableColumnsAsync() =>
        db.MemberColumnChanges.Where(c => c.IsActive).OrderBy(c => c.Id).ToListAsync();

    /// <summary>Danh sách yêu cầu thay đổi thông tin (Crd_MemberChangeInfo), lọc theo trạng thái nếu có.</summary>
    public async Task<List<MemberChangeRequest>> ChangeRequestsAsync(ChangeRequestStatus? status = null)
    {
        var q = db.MemberChangeRequests.Include(r => r.Member).Include(r => r.Details).AsQueryable();
        if (status.HasValue) q = q.Where(r => r.Status == status.Value);
        var list = await q.ToListAsync();
        return list.OrderByDescending(r => r.CreatedAt).ToList();
    }

    public Task<MemberChangeRequest?> ChangeRequestAsync(int id) =>
        db.MemberChangeRequests.Include(r => r.Member).Include(r => r.Details)
            .FirstOrDefaultAsync(r => r.Id == id);

    /// <summary>
    /// Tạo yêu cầu thay đổi thông tin hội viên (Crd_MemberChangeInfo_SaveX): hội viên/đại lý đề nghị đổi
    /// một số cột (theo whitelist Crd_MemberColumnChange) kèm giá trị mới. Yêu cầu khởi tạo ở trạng thái
    /// PENDING, mỗi dòng chi tiết cũng PENDING. Chặn tạo trùng khi đã có yêu cầu PENDING/APPROVE cùng loại
    /// cho hội viên (giống hệ nguồn). Sinh RequestNo dạng CRQ.&lt;năm&gt;.&lt;mã hội viên&gt;.&lt;seq&gt;.
    /// </summary>
    public async Task<(bool ok, string msg, int id)> CreateChangeRequestAsync(
        int memberId, ChangeRequestType type, string? dlCode, string? remark,
        List<(string ColumnCode, string? ValueNew)> details)
    {
        var m = await db.Members.FirstOrDefaultAsync(x => x.Id == memberId);
        if (m == null) return (false, "Không tìm thấy hội viên.", 0);
        if (m.Status == MemberStatus.Cancel) return (false, "Hội viên đã bị vô hiệu hoá.", 0);

        // Chỉ 1 yêu cầu đang mở (PENDING/APPROVE) cho mỗi hội viên + loại yêu cầu.
        var open = await db.MemberChangeRequests.AnyAsync(r =>
            r.MemberId == memberId && r.RequestType == type &&
            (r.Status == ChangeRequestStatus.Pending || r.Status == ChangeRequestStatus.Approve));
        if (open) return (false, "Đã có yêu cầu đang xử lý (PENDING/APPROVE) cho hội viên này.", 0);

        // Đổi thông tin: phải có ít nhất 1 dòng và mọi cột phải nằm trong whitelist.
        if (type == ChangeRequestType.ChangeInfo)
        {
            details = details.Where(d => !string.IsNullOrWhiteSpace(d.ColumnCode)).ToList();
            if (details.Count == 0) return (false, "Cần ít nhất 1 cột đề nghị thay đổi.", 0);
            var allowed = await db.MemberColumnChanges.Where(c => c.IsActive).Select(c => c.ColumnCode).ToListAsync();
            var bad = details.FirstOrDefault(d => !allowed.Contains(d.ColumnCode));
            if (bad.ColumnCode != null) return (false, $"Cột {bad.ColumnCode} không được phép thay đổi.", 0);
        }

        // Đơn vị duyệt (Crd_MemberChangeInfo.ApproveDLCode): KHÓA tại thời điểm TẠO đề nghị.
        // Chỉ ChangeInfo mới khóa đại lý phát sinh lượt xét hạng gần nhất của hội viên
        // (CONSUMPTION/SERVICETURN, loại HTV/SUPPORT); CancelMember để rỗng (chỉ HTV duyệt).
        string? approveDLCode = null;
        if (type == ChangeRequestType.ChangeInfo)
        {
            approveDLCode = await db.PointTransactions
                .Where(t => t.MemberId == memberId
                    && (t.Type == PointTxType.Consumption || t.Type == PointTxType.ServiceTurn)
                    && t.DLCode != null && t.DLCode != "HTV" && t.DLCode != "SUPPORT")
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => t.DLCode)
                .FirstOrDefaultAsync();
        }

        var seq = await db.MemberChangeRequests.CountAsync() + 1;
        var req = new MemberChangeRequest
        {
            RequestNo = $"CRQ.{DateTime.Now:yyyy}.{m.Code}.{seq:D3}",
            MemberId = memberId, RequestType = type, Status = ChangeRequestStatus.Pending,
            DLCodeRequest = dlCode, ApproveDLCode = approveDLCode, Remark = remark, CreatedAt = DateTime.Now
        };
        foreach (var d in details)
            req.Details.Add(new MemberChangeRequestDtl { ColumnCode = d.ColumnCode, ColumnValueNew = d.ValueNew });
        db.MemberChangeRequests.Add(req);
        await db.SaveChangesAsync();
        return (true, $"Đã tạo yêu cầu {req.RequestNo} (PENDING).", req.Id);
    }

    /// <summary>
    /// Duyệt yêu cầu thay đổi (Crd_MemberChangeInfo_ApproveX): chỉ duyệt được yêu cầu đang PENDING.
    /// Đặt RequestStatus = APPROVE, ghi ApproveAt/ApproveBy/RemarkHTV; các dòng chi tiết chuyển APPROVE.
    /// Chưa áp thay đổi vào hội viên (đó là bước FINISH).
    /// </summary>
    public async Task<(bool ok, string msg)> ApproveChangeRequestAsync(int requestId, string? remarkHtv, string? by = null, string? dlcpCode = null)
    {
        var r = await db.MemberChangeRequests.Include(x => x.Details).FirstOrDefaultAsync(x => x.Id == requestId);
        if (r == null) return (false, "Không tìm thấy yêu cầu.");
        if (r.Status != ChangeRequestStatus.Pending) return (false, "Chỉ duyệt được yêu cầu đang PENDING.");
        // Gate quyền duyệt (Crd_MemberChangeInfo_CheckApproveRight): chỉ HTV hoặc đại lý khớp Đơn vị duyệt đã khóa.
        var (rightOk, rightMsg) = CheckApproveRight(r, dlcpCode);
        if (!rightOk) return (false, rightMsg);

        r.Status = ChangeRequestStatus.Approve;
        r.ApproveAt = DateTime.Now;
        r.ApproveBy = by;
        r.RemarkHTV = remarkHtv;
        foreach (var d in r.Details) d.DtlStatus = ChangeRequestStatus.Approve;
        await db.SaveChangesAsync();
        return (true, $"Đã duyệt yêu cầu {r.RequestNo} (APPROVE).");
    }

    /// <summary>
    /// Hoàn tất yêu cầu thay đổi (Crd_MemberChangeInfo_FinishX): chỉ hoàn tất được yêu cầu đang APPROVE.
    /// Với ChangeInfo: áp giá trị mới của các dòng đã APPROVE vào hội viên (theo whitelist cột), rồi đặt
    /// RequestStatus = FINISH. Với CancelMember: vô hiệu hoá hội viên (MemberStatus = Cancel, huỷ thẻ,
    /// dời hạn điểm về cuối tháng) — tái dùng InactivateMemberAsync. Ghi FinishAt/FinishBy/RemarkHTV.
    /// </summary>
    public async Task<(bool ok, string msg)> FinishChangeRequestAsync(int requestId, string? remarkHtv, string? by = null, string? dlcpCode = null)
    {
        var r = await db.MemberChangeRequests.Include(x => x.Details).FirstOrDefaultAsync(x => x.Id == requestId);
        if (r == null) return (false, "Không tìm thấy yêu cầu.");
        if (r.Status != ChangeRequestStatus.Approve) return (false, "Chỉ hoàn tất được yêu cầu đang APPROVE.");
        // Gate quyền duyệt (Crd_MemberChangeInfo_CheckApproveRight): chỉ HTV hoặc đại lý khớp Đơn vị duyệt đã khóa.
        var (rightOk, rightMsg) = CheckApproveRight(r, dlcpCode);
        if (!rightOk) return (false, rightMsg);

        var m = await db.Members.FirstOrDefaultAsync(x => x.Id == r.MemberId);
        if (m == null) return (false, "Không tìm thấy hội viên.");

        if (r.RequestType == ChangeRequestType.ChangeInfo)
        {
            // Áp giá trị mới của các dòng đã APPROVE vào hội viên (chỉ các cột trong whitelist).
            var allowed = await db.MemberColumnChanges.Where(c => c.IsActive).Select(c => c.ColumnCode).ToListAsync();
            foreach (var d in r.Details.Where(x => x.DtlStatus == ChangeRequestStatus.Approve))
            {
                if (!allowed.Contains(d.ColumnCode)) continue;
                ApplyColumn(m, d.ColumnCode, d.ColumnValueNew);
            }
        }
        else if (r.RequestType == ChangeRequestType.CancelMember)
        {
            if (m.Status != MemberStatus.Cancel)
            {
                var (ok, msg) = await InactivateMemberAsync(m.Id, r.Remark, by);
                if (!ok) return (false, msg);
            }
        }

        r.Status = ChangeRequestStatus.Finish;
        r.FinishAt = DateTime.Now;
        r.FinishBy = by;
        r.RemarkHTV = remarkHtv;
        await db.SaveChangesAsync();
        return (true, $"Đã hoàn tất yêu cầu {r.RequestNo} (FINISH).");
    }

    /// <summary>
    /// Từ chối yêu cầu thay đổi (Crd_MemberChangeInfo_RejectX): chỉ từ chối được yêu cầu đang PENDING/APPROVE.
    /// Đặt RequestStatus = CANCEL, ghi RemarkHTV. Không áp thay đổi vào hội viên.
    /// </summary>
    public async Task<(bool ok, string msg)> RejectChangeRequestAsync(int requestId, string? remarkHtv, string? by = null, string? dlcpCode = null)
    {
        var r = await db.MemberChangeRequests.FirstOrDefaultAsync(x => x.Id == requestId);
        if (r == null) return (false, "Không tìm thấy yêu cầu.");
        if (r.Status != ChangeRequestStatus.Pending && r.Status != ChangeRequestStatus.Approve)
            return (false, "Chỉ từ chối được yêu cầu đang PENDING/APPROVE.");
        // Gate quyền từ chối (Crd_MemberChangeInfo_CheckApproveRight): chỉ HTV hoặc đại lý khớp Đơn vị duyệt đã khóa.
        var (rightOk, rightMsg) = CheckApproveRight(r, dlcpCode);
        if (!rightOk) return (false, rightMsg);

        r.Status = ChangeRequestStatus.Cancel;
        r.RemarkHTV = remarkHtv;
        await db.SaveChangesAsync();
        return (true, $"Đã từ chối yêu cầu {r.RequestNo} (CANCEL).");
    }

    /// <summary>
    /// Gate quyền duyệt (Crd_MemberChangeInfo_CheckApproveRight): chỉ HTV hoặc đại lý khớp Đơn vị duyệt
    /// (ApproveDLCode) đã khóa lúc tạo đề nghị mới được duyệt/hoàn tất/từ chối. HTV luôn thuộc đơn vị duyệt.
    /// Nếu dlcpCode rỗng (không xác định được đại lý của user) thì bỏ qua gate (giữ tương thích UI demo).
    /// </summary>
    private static (bool ok, string msg) CheckApproveRight(MemberChangeRequest r, string? dlcpCode)
    {
        if (string.IsNullOrWhiteSpace(dlcpCode)) return (true, "");
        if (string.Equals(dlcpCode, "HTV", StringComparison.OrdinalIgnoreCase)) return (true, "");
        if (string.Equals(r.ApproveDLCode, dlcpCode, StringComparison.OrdinalIgnoreCase)) return (true, "");
        return (false, $"Không có quyền duyệt: đơn vị duyệt đã khóa là '{r.ApproveDLCode ?? "(trống)"}', "
            + $"chỉ HTV hoặc đại lý này được duyệt.");
    }

    /// <summary>
    /// Đơn vị duyệt (ApproveDLCode) đã khóa của đề nghị + cờ FlagApproveAF (user có quyền duyệt không).
    /// Hiển thị đơn vị duyệt dạng "HTV,&lt;đại lý&gt;" (giống hệ nguồn); FlagApproveAF = true khi đề nghị
    /// đang PENDING/APPROVE và user là HTV hoặc khớp đại lý đã khóa.
    /// </summary>
    public async Task<(string? approveUnit, bool flagApproveAf)> ApproveUnitAsync(int requestId, string? dlcpCode)
    {
        var r = await db.MemberChangeRequests.FirstOrDefaultAsync(x => x.Id == requestId);
        if (r == null) return (null, false);
        var unit = string.IsNullOrWhiteSpace(r.ApproveDLCode) ? "HTV" : $"HTV,{r.ApproveDLCode}";
        var open = r.Status == ChangeRequestStatus.Pending || r.Status == ChangeRequestStatus.Approve;
        var (ok, _) = CheckApproveRight(r, dlcpCode);
        return (unit, open && ok);
    }

    /// <summary>Danh sách yêu cầu đặc cách thẻ (Crd_Card FlagExceptionally=1), lọc theo trạng thái nếu có.</summary>
    public async Task<List<CardException>> CardExceptionsAsync(CardExceptionStatus? status = null)
    {
        var q = db.CardExceptions.Include(x => x.Member).Include(x => x.CardTypeUse).Include(x => x.Dealers).AsQueryable();
        if (status.HasValue) q = q.Where(x => x.Status == status.Value);
        var list = await q.ToListAsync();
        return list.OrderByDescending(x => x.CreatedAt).ToList();
    }

    public Task<CardException?> CardExceptionAsync(int id) =>
        db.CardExceptions.Include(x => x.Member).Include(x => x.CardTypeUse).Include(x => x.Dealers)
            .FirstOrDefaultAsync(x => x.Id == id);

    /// <summary>
    /// Tạo yêu cầu đặc cách thẻ (Crd_Card_RequestExceptionX): hội viên/đại lý đề nghị cấp một KỲ THẺ ĐẶC CÁCH
    /// (FlagExceptionally=1) kế thừa hạng thẻ hiện tại, kèm danh sách đại lý chỉ định được dùng thẻ
    /// (Crd_CardDealerUseException). Kỳ thẻ mới sinh ở trạng thái PENDING (CardNoPrev = kỳ thẻ đang hiệu lực).
    /// Chặn khi hội viên đã có yêu cầu đặc cách đang PENDING (giống hệ nguồn), hoặc hội viên đã vô hiệu hoá.
    /// </summary>
    public async Task<(bool ok, string msg, int id)> RequestCardExceptionAsync(
        int memberId, string? dlCodeExceptionally, string? remark, List<string> dealerCodes)
    {
        var m = await db.Members.FirstOrDefaultAsync(x => x.Id == memberId);
        if (m == null) return (false, "Không tìm thấy hội viên.", 0);
        if (m.Status == MemberStatus.Cancel) return (false, "Hội viên đã bị vô hiệu hoá.", 0);

        dealerCodes = (dealerCodes ?? []).Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim()).Distinct().ToList();
        if (dealerCodes.Count == 0) return (false, "Cần ít nhất 1 đại lý được dùng thẻ đặc cách.", 0);

        // Chỉ 1 yêu cầu đặc cách đang mở (PENDING) cho mỗi hội viên.
        var open = await db.CardExceptions.AnyAsync(x => x.MemberId == memberId && x.Status == CardExceptionStatus.Pending);
        if (open) return (false, "Đã có yêu cầu đặc cách đang chờ duyệt (PENDING) cho hội viên này.", 0);

        var seq = await db.CardExceptions.CountAsync() + 1;
        var ex = new CardException
        {
            MemberId = memberId,
            CardNo = $"CEX.{DateTime.Now:yyyy}.{m.Code}.{seq:D3}",   // Crd_Card.CardNo — kỳ thẻ đặc cách mới
            CardNoPrev = m.Code,                                     // Crd_Card.CardNoPrev — kỳ thẻ cũ (đang hiệu lực)
            CardTypeUseId = m.RankTierId,                            // kế thừa hạng thẻ hiện tại
            DLCodeExceptionally = dlCodeExceptionally,
            Status = CardExceptionStatus.Pending,
            Remark = remark,
            CreatedAt = DateTime.Now
        };
        foreach (var code in dealerCodes)
            ex.Dealers.Add(new CardExceptionDealer { DealerCode = code });
        db.CardExceptions.Add(ex);
        await db.SaveChangesAsync();
        return (true, $"Đã tạo yêu cầu đặc cách thẻ {ex.CardNo} cho {m.Code} ({dealerCodes.Count} đại lý).", ex.Id);
    }

    /// <summary>
    /// Duyệt yêu cầu đặc cách thẻ (Crd_Card_ApprExceptionX): kỳ thẻ đang hiệu lực bị huỷ (CardStatus=Cancel)
    /// và kỳ thẻ đặc cách được kích hoạt (CardStatus=Approve). Ghi ApproveAt/ApproveBy/RemarkHTV.
    /// Idempotent: chỉ duyệt được yêu cầu đang PENDING.
    /// </summary>
    public async Task<(bool ok, string msg)> ApproveCardExceptionAsync(int id, string? remarkHtv, string? by = null)
    {
        var ex = await db.CardExceptions.Include(x => x.Member).FirstOrDefaultAsync(x => x.Id == id);
        if (ex == null) return (false, "Không tìm thấy yêu cầu đặc cách.");
        if (ex.Status != CardExceptionStatus.Pending) return (false, "Chỉ duyệt được yêu cầu đang PENDING.");

        // Huỷ kỳ thẻ đang hiệu lực của hội viên (Crd_Card_CancelX) rồi kích hoạt kỳ thẻ đặc cách.
        if (ex.Member != null) ex.Member.CardStatus = CardStatus.Cancel;
        ex.Status = CardExceptionStatus.Approve;
        ex.ApproveAt = DateTime.Now;
        ex.ApproveBy = by;
        ex.RemarkHTV = remarkHtv;
        await db.SaveChangesAsync();
        return (true, $"Đã duyệt đặc cách thẻ {ex.CardNo}; huỷ kỳ thẻ cũ và kích hoạt kỳ thẻ đặc cách.");
    }

    /// <summary>
    /// Từ chối yêu cầu đặc cách thẻ: chỉ từ chối được yêu cầu đang PENDING. Đặt Status = CANCEL, ghi RemarkHTV.
    /// </summary>
    public async Task<(bool ok, string msg)> RejectCardExceptionAsync(int id, string? remarkHtv, string? by = null)
    {
        var ex = await db.CardExceptions.FirstOrDefaultAsync(x => x.Id == id);
        if (ex == null) return (false, "Không tìm thấy yêu cầu đặc cách.");
        if (ex.Status != CardExceptionStatus.Pending) return (false, "Chỉ từ chối được yêu cầu đang PENDING.");

        ex.Status = CardExceptionStatus.Cancel;
        ex.RemarkHTV = remarkHtv;
        await db.SaveChangesAsync();
        return (true, $"Đã từ chối yêu cầu đặc cách thẻ {ex.CardNo} (CANCEL).");
    }

    /// <summary>Áp 1 cột thay đổi vào hội viên theo ColumnCode (whitelist Crd_MemberColumnChange).</summary>
    private static void ApplyColumn(Member m, string columnCode, string? value)
    {
        switch (columnCode)
        {
            case "MemberName": m.Name = value ?? m.Name; break;
            case "PhoneNo": m.Phone = value; break;
            case "Email": m.Email = value; break;
            case "DateOfBirth":
                if (DateTime.TryParse(value, out var dob)) m.Dob = dob;
                break;
        }
    }

    public async Task<LoyaltyDash> DashboardAsync()
    {
        var members = await db.Members.Include(m => m.RankTier).ToListAsync();
        var byRank = members.GroupBy(m => m.RankTier!)
            .OrderBy(g => g.Key.SortOrder)
            .Select(g => (g.Key.Name, g.Key.ColorHex, g.Count())).ToList();
        return new LoyaltyDash(members.Count, members.Sum(m => m.Points), members.Sum(m => m.LifetimePoints), byRank);
    }

    /// <summary>
    /// Lịch sử xét hạng (Crd_CardRank, DealPointType=LOYALTY): danh sách các lần job xét hạng cuối kỳ
    /// đã xử lý hội viên (UP/KEEP/DOWN) kèm ảnh chụp hạng trước/sau. Lọc theo hội viên nếu có.
    /// </summary>
    public async Task<List<RankHistory>> RankHistoryAsync(int? memberId = null)
    {
        var q = db.RankHistories.Include(h => h.Member)
            .Include(h => h.RankTierBefore).Include(h => h.RankTierAfter).AsQueryable();
        if (memberId.HasValue) q = q.Where(h => h.MemberId == memberId.Value);
        var list = await q.ToListAsync();
        return list.OrderByDescending(h => h.CreatedAt).ToList();
    }

    /// <summary>Danh sách yêu cầu đăng ký hội viên (Req_MemberRegister), lọc theo trạng thái nếu có.</summary>
    public async Task<List<MemberRegister>> MemberRegistersAsync(MemberRegisterStatus? status = null)
    {
        var q = db.MemberRegisters.Include(r => r.Member).AsQueryable();
        if (status.HasValue) q = q.Where(r => r.Status == status.Value);
        var list = await q.ToListAsync();
        return list.OrderByDescending(r => r.CreatedAt).ToList();
    }

    public Task<MemberRegister?> MemberRegisterAsync(int id) =>
        db.MemberRegisters.Include(r => r.Member).FirstOrDefaultAsync(r => r.Id == id);

    /// <summary>
    /// Tạo yêu cầu đăng ký hội viên (Req_MemberRegister_SaveX): đại lý gửi thông tin khách hàng + xe
    /// để đề nghị cấp thẻ hội viên mới. Yêu cầu khởi tạo ở trạng thái PENDING. Sinh mã lượt đăng ký
    /// dạng MR.&lt;năm&gt;.&lt;seq&gt;. Chặn khi thiếu tên khách hàng.
    /// </summary>
    public async Task<(bool ok, string msg, int id)> CreateMemberRegisterAsync(MemberRegister req)
    {
        if (string.IsNullOrWhiteSpace(req.CustomerName)) return (false, "Cần tên khách hàng.", 0);

        var seq = await db.MemberRegisters.CountAsync() + 1;
        req.ReqMemberRegisterCode = $"MR.{DateTime.Now:yyyy}.{seq:D4}";
        req.Status = MemberRegisterStatus.Pending;
        req.RegisterDate = req.RegisterDate == default ? DateTime.Now : req.RegisterDate;
        req.CreatedAt = DateTime.Now;
        db.MemberRegisters.Add(req);
        await db.SaveChangesAsync();
        return (true, $"Đã tạo yêu cầu đăng ký {req.ReqMemberRegisterCode} (PENDING).", req.Id);
    }

    /// <summary>
    /// Duyệt yêu cầu đăng ký (Req_MemberRegister_ApprX): chỉ duyệt được yêu cầu đang PENDING.
    /// Trước khi duyệt, chặn nếu đã có hội viên APPROVE trùng CarNo/VIN (tránh cấp trùng thẻ cho cùng một xe).
    /// Đặt Status = APPROVE, ghi ApproveAt/ApproveBy/Remark.
    /// </summary>
    public async Task<(bool ok, string msg)> ApproveMemberRegisterAsync(int id, string? remark, string? by = null)
    {
        var r = await db.MemberRegisters.FirstOrDefaultAsync(x => x.Id == id);
        if (r == null) return (false, "Không tìm thấy yêu cầu đăng ký.");
        if (r.Status != MemberRegisterStatus.Pending) return (false, "Chỉ duyệt được yêu cầu đang PENDING.");

        // Chặn cấp trùng: đã có hội viên APPROVE cùng CarNo hoặc VIN.
        // (Hội viên lưu biển số/VIN ở trường Remark khi hoàn tất đăng ký — xem FinishMemberRegisterAsync.)
        var existed = await db.Members.FirstOrDefaultAsync(m => m.Status == MemberStatus.Approve
            && ((r.CarNo != null && r.CarNo != "" && m.Remark == r.CarNo) || (r.VIN != null && r.VIN != "" && m.Remark == r.VIN)));
        if (existed != null)
            return (false, $"Đã có hội viên {existed.Code} dùng biển số/VIN này (tránh cấp trùng thẻ).");

        r.Status = MemberRegisterStatus.Approve;
        r.ApproveAt = DateTime.Now;
        r.ApproveBy = by;
        r.Remark = remark;
        await db.SaveChangesAsync();
        return (true, $"Đã duyệt yêu cầu đăng ký {r.ReqMemberRegisterCode} (APPROVE).");
    }

    /// <summary>
    /// Hoàn tất yêu cầu đăng ký (Req_MemberRegister_FinishX): chỉ hoàn tất được yêu cầu đang APPROVE.
    /// Tạo hội viên mới từ thông tin khách hàng (mã HV..., hạng thấp nhất), gắn MemberId vào yêu cầu,
    /// đặt Status = FINISH, ghi FinishAt/FinishBy. Idempotent: yêu cầu đã FINISH thì bỏ qua.
    /// </summary>
    public async Task<(bool ok, string msg)> FinishMemberRegisterAsync(int id, string? by = null)
    {
        var r = await db.MemberRegisters.FirstOrDefaultAsync(x => x.Id == id);
        if (r == null) return (false, "Không tìm thấy yêu cầu đăng ký.");
        if (r.Status != MemberRegisterStatus.Approve) return (false, "Chỉ hoàn tất được yêu cầu đang APPROVE.");

        // Tạo hội viên mới từ thông tin đăng ký (Crd_Member_Finish).
        var count = await db.Members.CountAsync();
        var m = new Member
        {
            Code = $"HV{DateTime.Now:yy}{count + 1:D5}",
            Name = r.CustomerName,
            Phone = r.CustomerPhoneNo,
            Email = r.CustomerEmail,
            Dob = r.CustomerDateOfBirth,
            RankTierId = (await LowestRankAsync()).Id,
            JoinedAt = DateTime.Now,
            MemberNoIntro = r.MemberNoIntro,
            Remark = r.CarNo ?? r.VIN   // lưu biển số/VIN để chặn cấp trùng về sau
        };
        db.Members.Add(m);
        await db.SaveChangesAsync();

        r.MemberId = m.Id;
        r.Status = MemberRegisterStatus.Finish;
        r.FinishAt = DateTime.Now;
        r.FinishBy = by;
        await db.SaveChangesAsync();

        // Ghi liên kết Đại lý ↔ Hội viên (Map_QueryDealer_Member): đại lý đăng ký (DLCodeRegis) ↔ hội viên mới.
        if (!string.IsNullOrWhiteSpace(r.DLCodeRegis))
            await LinkDealerMemberAsync(r.DLCodeRegis!, m.Id, 0, "Đăng ký hội viên", by);

        return (true, $"Đã hoàn tất đăng ký {r.ReqMemberRegisterCode}; tạo hội viên {m.Code} ({m.Name}).");
    }

    /// <summary>
    /// Huỷ yêu cầu đăng ký (Req_MemberRegister_CancelX): đại lý huỷ yêu cầu đang PENDING/APPROVE.
    /// Đặt Status = CANCEL, ghi CancelAt/CancelBy/Remark.
    /// </summary>
    public async Task<(bool ok, string msg)> CancelMemberRegisterAsync(int id, string? remark, string? by = null)
    {
        var r = await db.MemberRegisters.FirstOrDefaultAsync(x => x.Id == id);
        if (r == null) return (false, "Không tìm thấy yêu cầu đăng ký.");
        if (r.Status != MemberRegisterStatus.Pending && r.Status != MemberRegisterStatus.Approve)
            return (false, "Chỉ huỷ được yêu cầu đang PENDING/APPROVE.");

        r.Status = MemberRegisterStatus.Cancel;
        r.CancelAt = DateTime.Now;
        r.CancelBy = by;
        r.Remark = remark;
        await db.SaveChangesAsync();
        return (true, $"Đã huỷ yêu cầu đăng ký {r.ReqMemberRegisterCode} (CANCEL).");
    }

    /// <summary>
    /// Từ chối yêu cầu đăng ký (Req_MemberRegister_RejectX): HTV từ chối yêu cầu đang APPROVE.
    /// Bắt buộc có lý do (Remark). Đặt Status = REJECT, ghi RejectAt/RejectBy/Remark.
    /// </summary>
    public async Task<(bool ok, string msg)> RejectMemberRegisterAsync(int id, string? remark, string? by = null)
    {
        var r = await db.MemberRegisters.FirstOrDefaultAsync(x => x.Id == id);
        if (r == null) return (false, "Không tìm thấy yêu cầu đăng ký.");
        if (r.Status != MemberRegisterStatus.Approve) return (false, "Chỉ từ chối được yêu cầu đang APPROVE.");
        if (string.IsNullOrWhiteSpace(remark)) return (false, "Cần lý do từ chối.");

        r.Status = MemberRegisterStatus.Reject;
        r.RejectAt = DateTime.Now;
        r.RejectBy = by;
        r.Remark = remark;
        await db.SaveChangesAsync();
        return (true, $"Đã từ chối yêu cầu đăng ký {r.ReqMemberRegisterCode} (REJECT).");
    }

    private async Task<RankTier> LowestRankAsync() =>
        (await db.RankTiers.OrderBy(t => t.SortOrder).FirstAsync());

    /// <summary>
    /// Liệt kê liên kết Đại lý ↔ Hội viên (Map_QueryDealer_Member), lọc theo đại lý và/hoặc hội viên.
    /// </summary>
    public async Task<List<DealerMemberLink>> DealerMemberLinksAsync(string? dlcpCode = null, int? memberId = null)
    {
        var q = db.DealerMemberLinks.Include(x => x.Member).AsQueryable();
        if (!string.IsNullOrWhiteSpace(dlcpCode)) q = q.Where(x => x.DLCPCode == dlcpCode);
        if (memberId is { } mid) q = q.Where(x => x.MemberId == mid);
        return await q.OrderByDescending(x => x.QueryDate).ThenBy(x => x.DLCPCode).ToListAsync();
    }

    /// <summary>
    /// Ghi liên kết Đại lý ↔ Hội viên (Map_QueryDealer_Member_Create): nếu đã có liên kết cùng đại lý + hội viên
    /// thì cập nhật ngày tra cứu/ghi chú (upsert), ngược lại tạo mới. Idempotent theo cặp (DLCPCode, MemberId).
    /// </summary>
    public async Task<(bool ok, string msg, int id)> LinkDealerMemberAsync(string dlcpCode, int memberId, int networkId = 0, string? remark = null, string? by = null)
    {
        if (string.IsNullOrWhiteSpace(dlcpCode)) return (false, "Cần mã đại lý (DLCPCode).", 0);
        var m = await db.Members.FirstOrDefaultAsync(x => x.Id == memberId);
        if (m == null) return (false, "Không tìm thấy hội viên.", 0);

        var link = await db.DealerMemberLinks.FirstOrDefaultAsync(x => x.DLCPCode == dlcpCode && x.MemberId == memberId);
        if (link != null)
        {
            link.QueryDate = DateTime.Now;
            link.NetworkID = networkId;
            link.Remark = remark;
            link.FlagActive = true;
            link.CreatedBy = by;
            await db.SaveChangesAsync();
            return (true, $"Đã cập nhật liên kết đại lý {dlcpCode} ↔ hội viên {m.Code}.", link.Id);
        }

        link = new DealerMemberLink
        {
            DLCPCode = dlcpCode.Trim(), MemberId = memberId, NetworkID = networkId,
            QueryDate = DateTime.Now, Remark = remark, FlagActive = true, CreatedBy = by
        };
        db.DealerMemberLinks.Add(link);
        await db.SaveChangesAsync();
        return (true, $"Đã liên kết đại lý {dlcpCode} ↔ hội viên {m.Code}.", link.Id);
    }

    /// <summary>
    /// Liệt kê chương trình tặng điểm xe mới (Prm_CarNew), lọc theo trạng thái và/hoặc đại lý.
    /// </summary>
    public async Task<List<PrmCarNew>> PrmCarNewsAsync(PrmCarNewStatus? status = null, string? dlcpCode = null)
    {
        var q = db.PrmCarNews.Include(x => x.Specs).AsQueryable();
        if (status.HasValue) q = q.Where(x => x.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(dlcpCode)) q = q.Where(x => x.DLCPCode == dlcpCode);
        var list = await q.ToListAsync();
        return list.OrderByDescending(x => x.CreatedAt).ToList();
    }

    public Task<PrmCarNew?> PrmCarNewAsync(int id) =>
        db.PrmCarNews.Include(x => x.Specs).FirstOrDefaultAsync(x => x.Id == id);

    /// <summary>
    /// Tạo chương trình tặng điểm xe mới (Prm_CarNew_SaveX): đại lý/HTV cấu hình chương trình tặng điểm
    /// cho hội viên mua xe mới theo đại lý (DLCPCode) và dòng xe. Khởi tạo ở trạng thái PENDING.
    /// Sinh mã hệ thống PRMCN.&lt;năm&gt;.&lt;seq&gt;. Chặn khi thiếu tên chương trình hoặc đại lý.
    /// </summary>
    public async Task<(bool ok, string msg, int id)> CreatePrmCarNewAsync(PrmCarNew prm)
    {
        if (string.IsNullOrWhiteSpace(prm.PRMCNName)) return (false, "Cần tên chương trình.", 0);
        if (string.IsNullOrWhiteSpace(prm.DLCPCode)) return (false, "Cần mã đại lý (DLCPCode).", 0);

        var seq = await db.PrmCarNews.CountAsync() + 1;
        prm.PRMCNCodeSys = $"PRMCN.{DateTime.Now:yyyy}.{seq:D4}";
        prm.PRMCNCode = prm.PRMCNCodeSys;
        prm.Status = PrmCarNewStatus.Pending;
        prm.EffDateStart = prm.EffDateStart == default ? DateTime.Today : prm.EffDateStart;
        prm.EffDateEnd = prm.EffDateEnd == default ? new DateTime(9999, 12, 31) : prm.EffDateEnd;
        prm.CreatedAt = DateTime.Now;
        db.PrmCarNews.Add(prm);
        await db.SaveChangesAsync();
        return (true, $"Đã tạo chương trình {prm.PRMCNCodeSys} (PENDING).", prm.Id);
    }

    /// <summary>
    /// Duyệt chương trình (Prm_CarNew_ApprX): chỉ duyệt được chương trình đang PENDING.
    /// Đặt Status = APPROVE, ghi ApproveAt/ApproveBy/Remark.
    /// </summary>
    public async Task<(bool ok, string msg)> ApprovePrmCarNewAsync(int id, string? remark, string? by = null)
    {
        var p = await db.PrmCarNews.FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return (false, "Không tìm thấy chương trình.");
        if (p.Status != PrmCarNewStatus.Pending) return (false, "Chỉ duyệt được chương trình đang PENDING.");

        p.Status = PrmCarNewStatus.Approve;
        p.ApproveAt = DateTime.Now;
        p.ApproveBy = by;
        p.Remark = remark;
        await db.SaveChangesAsync();
        return (true, $"Đã duyệt chương trình {p.PRMCNCodeSys} (APPROVE).");
    }

    /// <summary>
    /// Hoàn tất chương trình (Prm_CarNew_FinishX): chỉ hoàn tất được chương trình đang APPROVE.
    /// Đặt Status = FINISH (chương trình có hiệu lực). Nếu đã có chương trình FINISH đang hiệu lực cùng đại lý,
    /// cắt hiệu lực chương trình cũ (EffDateEnd = ngày trước ngày bắt đầu chương trình mới).
    /// </summary>
    public async Task<(bool ok, string msg)> FinishPrmCarNewAsync(int id, string? remark, string? by = null)
    {
        var p = await db.PrmCarNews.FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return (false, "Không tìm thấy chương trình.");
        if (p.Status != PrmCarNewStatus.Approve) return (false, "Chỉ hoàn tất được chương trình đang APPROVE.");

        p.Status = PrmCarNewStatus.Finish;
        p.FinishAt = DateTime.Now;
        p.FinishBy = by;
        if (!string.IsNullOrWhiteSpace(remark)) p.Remark = remark;
        await db.SaveChangesAsync();

        // Cắt hiệu lực chương trình FINISH trước đó đang hiệu lực cùng đại lý (Prm_CarNew_FinishX).
        var today = DateTime.Today;
        var prev = await db.PrmCarNews.FirstOrDefaultAsync(x => x.Id != p.Id && x.DLCPCode == p.DLCPCode
            && x.Status == PrmCarNewStatus.Finish && x.EffDateStart <= today && x.EffDateEnd >= today);
        if (prev != null)
        {
            prev.EffDateEnd = p.EffDateStart <= today ? today.AddDays(-1) : p.EffDateStart.AddDays(-1);
            await db.SaveChangesAsync();
        }

        return (true, $"Đã hoàn tất chương trình {p.PRMCNCodeSys} (FINISH) — chương trình có hiệu lực.");
    }

    /// <summary>
    /// Huỷ chương trình (Prm_CarNew_CancelX): huỷ được chương trình đang PENDING/APPROVE.
    /// Đặt Status = CANCEL, ghi CancelAt/CancelBy/Remark.
    /// </summary>
    public async Task<(bool ok, string msg)> CancelPrmCarNewAsync(int id, string? remark, string? by = null)
    {
        var p = await db.PrmCarNews.FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return (false, "Không tìm thấy chương trình.");
        if (p.Status != PrmCarNewStatus.Pending && p.Status != PrmCarNewStatus.Approve)
            return (false, "Chỉ huỷ được chương trình đang PENDING/APPROVE.");

        p.Status = PrmCarNewStatus.Cancel;
        p.CancelAt = DateTime.Now;
        p.CancelBy = by;
        p.Remark = remark;
        await db.SaveChangesAsync();
        return (true, $"Đã huỷ chương trình {p.PRMCNCodeSys} (CANCEL).");
    }

    /// <summary>
    /// Tra chương trình tặng điểm xe mới đang hiệu lực (Prm_CarNew_CalcPrmX): tìm chương trình FINISH
    /// của đại lý (DLCPCode) đang trong khoảng hiệu lực [EffDateStart, EffDateEnd] tại ngày xét.
    /// Nếu chương trình áp dụng cho tất cả dòng xe (FlagAllModel) thì trả về luôn; ngược lại chỉ trả về
    /// khi dòng xe (ModelCode) nằm trong danh sách Prm_CarNewSpec. Trả về null nếu không có chương trình phù hợp.
    /// </summary>
    public async Task<PrmCarNew?> CalcPrmCarNewAsync(string dlcpCode, string? modelCode, DateTime? today = null)
    {
        if (string.IsNullOrWhiteSpace(dlcpCode)) return null;
        var d = (today ?? DateTime.Today).Date;
        var active = await db.PrmCarNews.Include(x => x.Specs)
            .Where(x => x.DLCPCode == dlcpCode && x.Status == PrmCarNewStatus.Finish
                && x.EffDateStart <= d && x.EffDateEnd >= d)
            .OrderBy(x => x.EffDateStart)
            .FirstOrDefaultAsync();
        if (active == null) return null;
        if (active.FlagAllModel) return active;
        if (string.IsNullOrWhiteSpace(modelCode)) return null;
        return active.Specs.Any(s => s.ModelCode == modelCode) ? active : null;
    }

    /// <summary>
    /// Liệt kê chương trình tặng điểm giới thiệu mua xe (Prm_CarRecommend), lọc theo trạng thái và/hoặc đại lý.
    /// </summary>
    public async Task<List<PrmCarRecommend>> PrmCarRecommendsAsync(PrmCarRecommendStatus? status = null, string? dlcpCode = null)
    {
        var q = db.PrmCarRecommends.Include(x => x.Specs).Include(x => x.Details).AsQueryable();
        if (status.HasValue) q = q.Where(x => x.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(dlcpCode)) q = q.Where(x => x.DLCPCode == dlcpCode);
        var list = await q.ToListAsync();
        return list.OrderByDescending(x => x.CreatedAt).ToList();
    }

    public Task<PrmCarRecommend?> PrmCarRecommendAsync(int id) =>
        db.PrmCarRecommends.Include(x => x.Specs).Include(x => x.Details).FirstOrDefaultAsync(x => x.Id == id);

    /// <summary>
    /// Tạo chương trình tặng điểm giới thiệu mua xe (Prm_CarRecommend_SaveX): đại lý/HTV cấu hình chương trình
    /// tặng điểm cho hội viên giới thiệu khách mua xe mới theo đại lý (DLCPCode) và dòng xe. Khởi tạo ở trạng thái
    /// PENDING. Sinh mã hệ thống PRMCR.&lt;năm&gt;.&lt;seq&gt;. Chặn khi thiếu tên chương trình hoặc đại lý.
    /// </summary>
    public async Task<(bool ok, string msg, int id)> CreatePrmCarRecommendAsync(PrmCarRecommend prm)
    {
        if (string.IsNullOrWhiteSpace(prm.PRMCRName)) return (false, "Cần tên chương trình.", 0);
        if (string.IsNullOrWhiteSpace(prm.DLCPCode)) return (false, "Cần mã đại lý (DLCPCode).", 0);

        var seq = await db.PrmCarRecommends.CountAsync() + 1;
        prm.PRMCRCodeSys = $"PRMCR.{DateTime.Now:yyyy}.{seq:D4}";
        prm.PRMCRCode = prm.PRMCRCodeSys;
        prm.Status = PrmCarRecommendStatus.Pending;
        prm.EffDateStart = prm.EffDateStart == default ? DateTime.Today : prm.EffDateStart;
        prm.EffDateEnd = prm.EffDateEnd == default ? new DateTime(9999, 12, 31) : prm.EffDateEnd;
        prm.CreatedAt = DateTime.Now;
        db.PrmCarRecommends.Add(prm);
        await db.SaveChangesAsync();
        return (true, $"Đã tạo chương trình {prm.PRMCRCodeSys} (PENDING).", prm.Id);
    }

    /// <summary>
    /// Duyệt chương trình (Prm_CarRecommend_ApprX): chỉ duyệt được chương trình đang PENDING.
    /// Đặt Status = APPROVE, ghi ApproveAt/ApproveBy/Remark.
    /// </summary>
    public async Task<(bool ok, string msg)> ApprovePrmCarRecommendAsync(int id, string? remark, string? by = null)
    {
        var p = await db.PrmCarRecommends.FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return (false, "Không tìm thấy chương trình.");
        if (p.Status != PrmCarRecommendStatus.Pending) return (false, "Chỉ duyệt được chương trình đang PENDING.");

        p.Status = PrmCarRecommendStatus.Approve;
        p.ApproveAt = DateTime.Now;
        p.ApproveBy = by;
        p.Remark = remark;
        await db.SaveChangesAsync();
        return (true, $"Đã duyệt chương trình {p.PRMCRCodeSys} (APPROVE).");
    }

    /// <summary>
    /// Hoàn tất chương trình (Prm_CarRecommend_FinishX): chỉ hoàn tất được chương trình đang APPROVE.
    /// Đặt Status = FINISH (chương trình có hiệu lực). Nếu đã có chương trình FINISH đang hiệu lực cùng đại lý,
    /// cắt hiệu lực chương trình cũ (EffDateEnd = ngày trước ngày bắt đầu chương trình mới).
    /// </summary>
    public async Task<(bool ok, string msg)> FinishPrmCarRecommendAsync(int id, string? remark, string? by = null)
    {
        var p = await db.PrmCarRecommends.FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return (false, "Không tìm thấy chương trình.");
        if (p.Status != PrmCarRecommendStatus.Approve) return (false, "Chỉ hoàn tất được chương trình đang APPROVE.");

        p.Status = PrmCarRecommendStatus.Finish;
        p.FinishAt = DateTime.Now;
        p.FinishBy = by;
        if (!string.IsNullOrWhiteSpace(remark)) p.Remark = remark;
        await db.SaveChangesAsync();

        // Cắt hiệu lực chương trình FINISH trước đó đang hiệu lực cùng đại lý (Prm_CarRecommend_FinishX).
        var today = DateTime.Today;
        var prev = await db.PrmCarRecommends.FirstOrDefaultAsync(x => x.Id != p.Id && x.DLCPCode == p.DLCPCode
            && x.Status == PrmCarRecommendStatus.Finish && x.EffDateStart <= today && x.EffDateEnd >= today);
        if (prev != null)
        {
            prev.EffDateEnd = p.EffDateStart <= today ? today.AddDays(-1) : p.EffDateStart.AddDays(-1);
            await db.SaveChangesAsync();
        }

        return (true, $"Đã hoàn tất chương trình {p.PRMCRCodeSys} (FINISH) — chương trình có hiệu lực.");
    }

    /// <summary>
    /// Huỷ chương trình (Prm_CarRecommend_CancelX): huỷ được chương trình đang PENDING/APPROVE.
    /// Đặt Status = CANCEL, ghi CancelAt/CancelBy/Remark.
    /// </summary>
    public async Task<(bool ok, string msg)> CancelPrmCarRecommendAsync(int id, string? remark, string? by = null)
    {
        var p = await db.PrmCarRecommends.FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return (false, "Không tìm thấy chương trình.");
        if (p.Status != PrmCarRecommendStatus.Pending && p.Status != PrmCarRecommendStatus.Approve)
            return (false, "Chỉ huỷ được chương trình đang PENDING/APPROVE.");

        p.Status = PrmCarRecommendStatus.Cancel;
        p.CancelAt = DateTime.Now;
        p.CancelBy = by;
        p.Remark = remark;
        await db.SaveChangesAsync();
        return (true, $"Đã huỷ chương trình {p.PRMCRCodeSys} (CANCEL).");
    }

    /// <summary>
    /// Tra chương trình tặng điểm giới thiệu mua xe đang hiệu lực (Prm_CarRecommend_CalcPrmX): tìm chương trình
    /// FINISH của đại lý (DLCPCode) đang trong khoảng hiệu lực [EffDateStart, EffDateEnd] tại ngày xét.
    /// Nếu chương trình áp dụng cho tất cả dòng xe (FlagAllModel) thì trả về luôn; ngược lại chỉ trả về
    /// khi dòng xe (ModelCode) nằm trong danh sách Prm_CarRecommendSpec. Trả về null nếu không có chương trình phù hợp.
    /// </summary>
    public async Task<PrmCarRecommend?> CalcPrmCarRecommendAsync(string dlcpCode, string? modelCode, DateTime? today = null)
    {
        if (string.IsNullOrWhiteSpace(dlcpCode)) return null;
        var d = (today ?? DateTime.Today).Date;
        var active = await db.PrmCarRecommends.Include(x => x.Specs).Include(x => x.Details)
            .Where(x => x.DLCPCode == dlcpCode && x.Status == PrmCarRecommendStatus.Finish
                && x.EffDateStart <= d && x.EffDateEnd >= d)
            .OrderBy(x => x.EffDateStart)
            .FirstOrDefaultAsync();
        if (active == null) return null;
        if (active.FlagAllModel) return active;
        if (string.IsNullOrWhiteSpace(modelCode)) return null;
        return active.Specs.Any(s => s.ModelCode == modelCode) ? active : null;
    }

    /// <summary>
    /// Tính điểm khuyến mại bán hàng Creta (WA_Crd_MemberRegis_CalcPointBuyCreta, Card.cs): HTV tặng điểm
    /// khuyến mại bán hàng (Crd_Member.PointBuyCreta) khi khách mua xe Creta thuộc chương trình. Điều kiện
    /// (theo hệ nguồn): dòng xe (ModelCode) + hạng thẻ sử dụng (CardTypeUse) khớp chính sách đang hiệu lực,
    /// ngày giao xe (DeliveryDate) nằm trong khoảng [EffDateStart, EffDateEnd], CCCD/MST chủ thẻ trùng với
    /// giao dịch mua xe, và giao dịch giao xe (DealNo) chưa từng được áp dụng chương trình (chưa có hội viên
    /// nào cùng DealNo có PointBuyCreta &gt; 0). Trả về số điểm tặng (0 nếu không đủ điều kiện) kèm lý do.
    /// </summary>
    public async Task<CretaBuyCarResult> CalcPointBuyCretaAsync(
        string? modelCode, string? cardTypeUse, DateTime? deliveryDate, string? idCardNo, string? dealNo, int? memberId = null)
    {
        var policy = await db.CretaBuyCarPolicies
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.EffDateStart)
            .FirstOrDefaultAsync();
        if (policy == null) return new CretaBuyCarResult(false, 0, "Chưa cấu hình chính sách tặng điểm Creta.");

        // Dòng xe + hạng thẻ sử dụng phải khớp chính sách.
        if (!string.Equals(modelCode, policy.ModelCode, StringComparison.OrdinalIgnoreCase))
            return new CretaBuyCarResult(false, 0, $"Dòng xe {modelCode ?? "—"} không thuộc chương trình (cần {policy.ModelCode}).");
        if (!string.Equals(cardTypeUse, policy.CardTypeUse, StringComparison.OrdinalIgnoreCase))
            return new CretaBuyCarResult(false, 0, $"Hạng thẻ sử dụng {cardTypeUse ?? "—"} không đủ điều kiện (cần {policy.CardTypeUse}).");

        // Ngày giao xe phải nằm trong khoảng hiệu lực của chương trình.
        if (deliveryDate is not { } dd)
            return new CretaBuyCarResult(false, 0, "Thiếu ngày giao xe (DeliveryDate).");
        var d = dd.Date;
        if (d < policy.EffDateStart.Date || d > policy.EffDateEnd.Date)
            return new CretaBuyCarResult(false, 0, $"Ngày giao xe {d:dd/MM/yyyy} ngoài khoảng hiệu lực [{policy.EffDateStart:dd/MM/yyyy} – {policy.EffDateEnd:dd/MM/yyyy}].");

        // CCCD/MST chủ thẻ phải trùng với giao dịch mua xe.
        if (string.IsNullOrWhiteSpace(idCardNo))
            return new CretaBuyCarResult(false, 0, "Thiếu CCCD/MST chủ thẻ (IDCardNo).");

        // Giao dịch giao xe chưa từng được áp dụng chương trình (chưa có hội viên nào cùng DealNo có PointBuyCreta > 0).
        if (!string.IsNullOrWhiteSpace(dealNo))
        {
            var applied = await db.Members.AnyAsync(m =>
                m.PointBuyCreta > 0 && m.DealNo == dealNo && (memberId == null || m.Id != memberId.Value));
            if (applied)
                return new CretaBuyCarResult(false, 0, $"Giao dịch giao xe {dealNo} đã được áp dụng chương trình.");
        }

        return new CretaBuyCarResult(true, policy.PointBuyCreta,
            $"Đủ điều kiện tặng {policy.PointBuyCreta:N0} điểm khuyến mại bán hàng Creta (dòng {policy.ModelCode}, hạng {policy.CardTypeUse}).");
    }

    /// <summary>Tính lại hạng thẻ theo điểm tích lũy trọn đời (hạng cao nhất mà hội viên đạt ngưỡng).</summary>
    private async Task RecomputeRankAsync(Member m)
    {
        var tiers = await db.RankTiers.OrderBy(t => t.SortOrder).ToListAsync();
        var newTier = tiers.Last(t => m.LifetimePoints >= t.MinLifetimePoints);
        m.RankTierId = newTier.Id;
    }

    /// <summary>
    /// Liệt kê chính sách đối tượng tích điểm dịch vụ (Mst_PolicyExpenseType) — cấu hình cho từng loại chi phí
    /// dịch vụ (LOCAL/ROINSURANCE/ROREPAIR/ROWARRANTY) xem có tích điểm / tính điểm xét hạng / tính lượt dịch vụ /
    /// áp chiết khấu hay không. Sắp theo mã loại chi phí.
    /// </summary>
    public Task<List<ExpenseTypePolicy>> ExpenseTypePoliciesAsync() =>
        db.ExpenseTypePolicies.OrderBy(p => p.ExpenseType).ToListAsync();

    /// <summary>
    /// Lưu chính sách đối tượng tích điểm dịch vụ (Mst_PolicyExpenseType_SaveX, Master.Loyalty.cs):
    /// kiểm tra loại chi phí (ExpenseType) bắt buộc, DiscountRate trong [0,100], và luật chéo
    /// "FlagDiscount = 0 ⇒ DiscountRate phải = 0". Upsert theo ExpenseType (1 chính sách/loại chi phí).
    /// </summary>
    public async Task<(bool ok, string msg, int id)> SaveExpenseTypePolicyAsync(ExpenseTypePolicy policy)
    {
        if (string.IsNullOrWhiteSpace(policy.ExpenseType))
            return (false, "Cần loại chi phí (ExpenseType).", 0);
        if (policy.DiscountRate < 0 || policy.DiscountRate > 100)
            return (false, "Tỉ lệ chiết khấu phải trong khoảng 0..100.", 0);
        // Luật chéo (Mst_PolicyExpenseType_SaveX): không bật chiết khấu thì tỉ lệ phải = 0.
        if (!policy.FlagDiscount && policy.DiscountRate != 0)
            return (false, "Không bật chiết khấu (FlagDiscount=0) thì tỉ lệ chiết khấu phải = 0.", 0);

        var code = policy.ExpenseType.Trim();
        var existing = await db.ExpenseTypePolicies.FirstOrDefaultAsync(p => p.ExpenseType == code);
        if (existing == null)
        {
            policy.ExpenseType = code;
            policy.PolicyExpenseTypeNo = string.IsNullOrWhiteSpace(policy.PolicyExpenseTypeNo)
                ? $"PET.{DateTime.Now:yyyy}.{code}" : policy.PolicyExpenseTypeNo;
            policy.ExpenseTypeNameActual = string.IsNullOrWhiteSpace(policy.ExpenseTypeNameActual) ? code : policy.ExpenseTypeNameActual;
            policy.IsActive = true;
            db.ExpenseTypePolicies.Add(policy);
            await db.SaveChangesAsync();
            return (true, $"Đã tạo chính sách đối tượng tích điểm {code}.", policy.Id);
        }

        existing.ExpenseTypeNameActual = string.IsNullOrWhiteSpace(policy.ExpenseTypeNameActual) ? existing.ExpenseTypeNameActual : policy.ExpenseTypeNameActual;
        existing.FlagPoint = policy.FlagPoint;
        existing.FlagPointRank = policy.FlagPointRank;
        existing.FlagCountService = policy.FlagCountService;
        existing.FlagDiscount = policy.FlagDiscount;
        existing.AmountRate = policy.AmountRate;
        existing.MaxRankReviewPoint = policy.MaxRankReviewPoint;
        existing.MaxAccumulationPoint = policy.MaxAccumulationPoint;
        existing.DiscountRate = policy.DiscountRate;
        existing.Remark = policy.Remark;
        await db.SaveChangesAsync();
        return (true, $"Đã cập nhật chính sách đối tượng tích điểm {code}.", existing.Id);
    }

    /// <summary>
    /// Liệt kê chương trình tặng điểm voucher xe mới (Prm_VoucherNewCar), lọc theo trạng thái.
    /// </summary>
    public async Task<List<PrmVoucherNewCar>> PrmVoucherNewCarsAsync(PrmVoucherNewCarStatus? status = null)
    {
        var q = db.PrmVoucherNewCars.Include(x => x.Specs).Include(x => x.Details).AsQueryable();
        if (status.HasValue) q = q.Where(x => x.Status == status.Value);
        var list = await q.ToListAsync();
        return list.OrderByDescending(x => x.CreatedAt).ToList();
    }

    public Task<PrmVoucherNewCar?> PrmVoucherNewCarAsync(int id) =>
        db.PrmVoucherNewCars.Include(x => x.Specs).Include(x => x.Details).FirstOrDefaultAsync(x => x.Id == id);

    /// <summary>
    /// Tạo chương trình tặng điểm voucher xe mới (Prm_VoucherNewCar_SaveX, Promotion.cs): HTV cấu hình chương trình
    /// tặng điểm voucher cho khách mua xe mới theo dòng xe. Khởi tạo ở trạng thái PENDING. Sinh mã hệ thống
    /// PRMVC.&lt;năm&gt;.&lt;seq&gt;. Kiểm tra theo hệ nguồn: cần tên chương trình, ngày bắt đầu &lt;= ngày kết thúc,
    /// và điểm sử dụng tối đa (PointUseLimitAllModel) &lt;= điểm voucher tặng (PointVoucherAllModel).
    /// </summary>
    public async Task<(bool ok, string msg, int id)> CreatePrmVoucherNewCarAsync(PrmVoucherNewCar prm)
    {
        if (string.IsNullOrWhiteSpace(prm.PrmVoucherName)) return (false, "Cần tên chương trình.", 0);
        if (prm.EffDateStart == default) return (false, "Cần ngày bắt đầu hiệu lực (EffDateStart).", 0);
        if (prm.EffDateEnd == default) return (false, "Cần ngày kết thúc hiệu lực (EffDateEnd).", 0);
        if (prm.EffDateStart.Date > prm.EffDateEnd.Date)
            return (false, "Ngày bắt đầu hiệu lực phải <= ngày kết thúc hiệu lực.", 0);
        // Luật chéo (Prm_VoucherNewCar_SaveX): điểm sử dụng tối đa <= điểm voucher tặng.
        if (prm.PointUseLimitAllModel > prm.PointVoucherAllModel)
            return (false, "Điểm sử dụng tối đa (PointUseLimitAllModel) phải <= điểm voucher tặng (PointVoucherAllModel).", 0);

        var seq = await db.PrmVoucherNewCars.CountAsync() + 1;
        prm.PrmVoucherCode = $"PRMVC.{DateTime.Now:yyyy}.{seq:D4}";
        prm.Status = PrmVoucherNewCarStatus.Pending;
        prm.CreatedAt = DateTime.Now;
        db.PrmVoucherNewCars.Add(prm);
        await db.SaveChangesAsync();
        return (true, $"Đã tạo chương trình {prm.PrmVoucherCode} (PENDING).", prm.Id);
    }

    /// <summary>
    /// Duyệt chương trình (Prm_VoucherNewCar_ApprX): chỉ duyệt được chương trình đang PENDING.
    /// Đặt Status = APPROVE, ghi ApproveAt/ApproveBy/Remark.
    /// </summary>
    public async Task<(bool ok, string msg)> ApprovePrmVoucherNewCarAsync(int id, string? remark, string? by = null)
    {
        var p = await db.PrmVoucherNewCars.FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return (false, "Không tìm thấy chương trình.");
        if (p.Status != PrmVoucherNewCarStatus.Pending) return (false, "Chỉ duyệt được chương trình đang PENDING.");

        p.Status = PrmVoucherNewCarStatus.Approve;
        p.ApproveAt = DateTime.Now;
        p.ApproveBy = by;
        p.Remark = remark;
        await db.SaveChangesAsync();
        return (true, $"Đã duyệt chương trình {p.PrmVoucherCode} (APPROVE).");
    }

    /// <summary>
    /// Hoàn tất chương trình (Prm_VoucherNewCar_FinishX): chỉ hoàn tất được chương trình đang APPROVE.
    /// Đặt Status = FINISH (chương trình có hiệu lực). Nếu đã có chương trình FINISH đang hiệu lực,
    /// cắt hiệu lực chương trình cũ (EffDateEnd = ngày trước ngày bắt đầu chương trình mới).
    /// </summary>
    public async Task<(bool ok, string msg)> FinishPrmVoucherNewCarAsync(int id, string? remark, string? by = null)
    {
        var p = await db.PrmVoucherNewCars.FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return (false, "Không tìm thấy chương trình.");
        if (p.Status != PrmVoucherNewCarStatus.Approve) return (false, "Chỉ hoàn tất được chương trình đang APPROVE.");

        p.Status = PrmVoucherNewCarStatus.Finish;
        p.FinishAt = DateTime.Now;
        p.FinishBy = by;
        if (!string.IsNullOrWhiteSpace(remark)) p.Remark = remark;
        await db.SaveChangesAsync();

        // Cắt hiệu lực chương trình FINISH trước đó đang hiệu lực (Prm_VoucherNewCar_FinishX).
        var today = DateTime.Today;
        var prev = await db.PrmVoucherNewCars.FirstOrDefaultAsync(x => x.Id != p.Id
            && x.Status == PrmVoucherNewCarStatus.Finish && x.EffDateStart <= today && x.EffDateEnd >= today);
        if (prev != null)
        {
            prev.EffDateEnd = p.EffDateStart <= today ? today.AddDays(-1) : p.EffDateStart.AddDays(-1);
            await db.SaveChangesAsync();
        }

        return (true, $"Đã hoàn tất chương trình {p.PrmVoucherCode} (FINISH) — chương trình có hiệu lực.");
    }

    /// <summary>
    /// Huỷ chương trình (Prm_VoucherNewCar_CancelX): huỷ được chương trình đang PENDING/APPROVE.
    /// Đặt Status = CANCEL, ghi CancelAt/CancelBy/Remark.
    /// </summary>
    public async Task<(bool ok, string msg)> CancelPrmVoucherNewCarAsync(int id, string? remark, string? by = null)
    {
        var p = await db.PrmVoucherNewCars.FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return (false, "Không tìm thấy chương trình.");
        if (p.Status != PrmVoucherNewCarStatus.Pending && p.Status != PrmVoucherNewCarStatus.Approve)
            return (false, "Chỉ huỷ được chương trình đang PENDING/APPROVE.");

        p.Status = PrmVoucherNewCarStatus.Cancel;
        p.CancelAt = DateTime.Now;
        p.CancelBy = by;
        p.Remark = remark;
        await db.SaveChangesAsync();
        return (true, $"Đã huỷ chương trình {p.PrmVoucherCode} (CANCEL).");
    }

    /// <summary>
    /// Tra chương trình tặng điểm voucher xe mới đang hiệu lực (Prm_VoucherNewCar_CalcPrmX, Promotion.Calc.cs):
    /// tìm chương trình FINISH đang trong khoảng hiệu lực [EffDateStart, EffDateEnd] tại ngày xét. Nếu chương trình
    /// áp dụng cho tất cả dòng xe (FlagAllModel) thì trả về luôn; ngược lại chỉ trả về khi dòng xe (ModelCode)
    /// nằm trong danh sách Prm_VoucherNewCarSpec. Trả về null nếu không có chương trình phù hợp.
    /// </summary>
    public async Task<PrmVoucherNewCar?> CalcPrmVoucherNewCarAsync(string? modelCode, DateTime? today = null)
    {
        var d = (today ?? DateTime.Today).Date;
        var active = await db.PrmVoucherNewCars.Include(x => x.Specs).Include(x => x.Details)
            .Where(x => x.Status == PrmVoucherNewCarStatus.Finish && x.EffDateStart <= d && x.EffDateEnd >= d)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();
        if (active == null) return null;
        if (active.FlagAllModel) return active;
        if (string.IsNullOrWhiteSpace(modelCode)) return null;
        return active.Specs.Any(s => s.ModelCode == modelCode) ? active : null;
    }
}
