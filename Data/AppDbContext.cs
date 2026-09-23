using Microsoft.EntityFrameworkCore;
using MiniLoyalty.Models;

namespace MiniLoyalty.Data;

public class AppDbContext : DbContext
{
    private readonly Guid _orgId;

    // ITenantContext được inject (scoped). Chốt OrgId ngay lúc dựng context (sau khi middleware đã set).
    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenant) : base(options)
        => _orgId = tenant.OrgId;

    public DbSet<Org> Orgs => Set<Org>();
    public DbSet<RankTier> RankTiers => Set<RankTier>();
    public DbSet<Member> Members => Set<Member>();
    public DbSet<PointTransaction> PointTransactions => Set<PointTransaction>();
    public DbSet<MemberDiscountTransaction> MemberDiscountTransactions => Set<MemberDiscountTransaction>();
    public DbSet<MemberVoucherTransaction> MemberVoucherTransactions => Set<MemberVoucherTransaction>();
    public DbSet<Reward> Rewards => Set<Reward>();
    public DbSet<ServicePolicy> ServicePolicies => Set<ServicePolicy>();
    public DbSet<Promotion> Promotions => Set<Promotion>();
    public DbSet<MemberPromotionUse> MemberPromotionUses => Set<MemberPromotionUse>();
    public DbSet<MemberColumnChange> MemberColumnChanges => Set<MemberColumnChange>();
    public DbSet<MemberChangeRequest> MemberChangeRequests => Set<MemberChangeRequest>();
    public DbSet<MemberChangeRequestDtl> MemberChangeRequestDtls => Set<MemberChangeRequestDtl>();
    public DbSet<CardException> CardExceptions => Set<CardException>();
    public DbSet<CardExceptionDealer> CardExceptionDealers => Set<CardExceptionDealer>();
    public DbSet<RankHistory> RankHistories => Set<RankHistory>();
    public DbSet<MemberRegister> MemberRegisters => Set<MemberRegister>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        if (Database.IsNpgsql()) b.HasDefaultSchema("miniloyalty");
        b.Entity<Org>().HasIndex(x => x.ApiKey).IsUnique();
        b.Entity<RankTier>().Property(x => x.DiscountPercent).HasPrecision(5, 2);
        b.Entity<Member>(e =>
        {
            e.HasIndex(x => x.Code).IsUnique();
            e.HasOne(x => x.RankTier).WithMany().HasForeignKey(x => x.RankTierId);
            e.HasQueryFilter(x => x.OrgId == _orgId);           // cô lập theo tenant
        });
        b.Entity<PointTransaction>(e =>
        {
            e.Property(x => x.AmountChTotal).HasPrecision(18, 2);
            e.HasOne(x => x.Member).WithMany(x => x.Transactions).HasForeignKey(x => x.MemberId);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<ServicePolicy>(e =>
        {
            e.Property(x => x.ConvertValue).HasPrecision(18, 2);
            e.HasOne(x => x.RankTier).WithMany().HasForeignKey(x => x.RankTierId);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<Reward>().HasQueryFilter(x => x.OrgId == _orgId);
        b.Entity<MemberDiscountTransaction>(e =>
        {
            e.Property(x => x.PolicyDiscountRate).HasPrecision(5, 2);
            e.Property(x => x.AmountForDC).HasPrecision(18, 2);
            e.Property(x => x.DiscountAmount).HasPrecision(18, 2);
            e.HasOne(x => x.Member).WithMany(x => x.Discounts).HasForeignKey(x => x.MemberId);
            e.HasOne(x => x.CardTypeApply).WithMany().HasForeignKey(x => x.CardTypeApplyId);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<MemberVoucherTransaction>(e =>
        {
            e.HasOne(x => x.Member).WithMany(x => x.Vouchers).HasForeignKey(x => x.MemberId);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<Promotion>(e =>
        {
            e.HasIndex(x => x.Code).IsUnique();
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<MemberPromotionUse>(e =>
        {
            e.HasOne(x => x.Member).WithMany(x => x.PromotionUses).HasForeignKey(x => x.MemberId);
            e.HasOne(x => x.PromotionNav).WithMany().HasForeignKey(x => x.PromotionId);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<MemberColumnChange>(e =>
        {
            e.HasIndex(x => x.ColumnCode).IsUnique();
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<MemberChangeRequest>(e =>
        {
            e.HasIndex(x => x.RequestNo).IsUnique();
            e.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<MemberChangeRequestDtl>(e =>
        {
            e.HasOne(x => x.Request).WithMany(x => x.Details).HasForeignKey(x => x.RequestId);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<CardException>(e =>
        {
            e.HasIndex(x => x.CardNo).IsUnique();
            e.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId);
            e.HasOne(x => x.CardTypeUse).WithMany().HasForeignKey(x => x.CardTypeUseId);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<CardExceptionDealer>(e =>
        {
            e.HasOne(x => x.CardException).WithMany(x => x.Dealers).HasForeignKey(x => x.CardExceptionId);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<RankHistory>(e =>
        {
            e.HasIndex(x => x.CardRankNo).IsUnique();
            e.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId);
            e.HasOne(x => x.RankTierBefore).WithMany().HasForeignKey(x => x.RankTierIdBefore);
            e.HasOne(x => x.RankTierAfter).WithMany().HasForeignKey(x => x.RankTierIdAfter);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<MemberRegister>(e =>
        {
            e.HasIndex(x => x.ReqMemberRegisterCode).IsUnique();
            e.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
    }

    // Tự đóng dấu OrgId cho mọi bản ghi mới thuộc tenant hiện tại (khỏi sửa từng service).
    public override int SaveChanges()
    {
        StampOrg();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        StampOrg();
        return base.SaveChangesAsync(ct);
    }

    private void StampOrg()
    {
        foreach (var entry in ChangeTracker.Entries<IOrgOwned>())
            if (entry.State == EntityState.Added && entry.Entity.OrgId == Guid.Empty)
                entry.Entity.OrgId = _orgId;
    }
}
