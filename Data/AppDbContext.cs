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
    public DbSet<Reward> Rewards => Set<Reward>();

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
            e.HasOne(x => x.Member).WithMany(x => x.Transactions).HasForeignKey(x => x.MemberId);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<Reward>().HasQueryFilter(x => x.OrgId == _orgId);
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
