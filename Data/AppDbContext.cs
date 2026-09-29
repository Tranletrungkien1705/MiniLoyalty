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

    // ---- PORT 1:1 từ 2022.7.Loyalty — giữ NGUYÊN tên bảng nguồn Crd_* (xem Models/PortLoyalty.cs) ----
    public DbSet<CrdMember> CrdMembers => Set<CrdMember>();
    public DbSet<CrdCard> CrdCards => Set<CrdCard>();
    public DbSet<CrdCardTransaction> CrdCardTransactions => Set<CrdCardTransaction>();
    public DbSet<CrdMemberDiscountTransaction> CrdMemberDiscountTransactions => Set<CrdMemberDiscountTransaction>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        if (Database.IsNpgsql()) b.HasDefaultSchema("miniloyalty");
        b.Entity<Org>().HasIndex(x => x.ApiKey).IsUnique();
        b.Entity<RankTier>().Property(x => x.DiscountPercent).HasPrecision(5, 2);
        b.Entity<Member>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.Code }).IsUnique();   // unique theo tenant (không phải toàn cục) - tránh trùng mã giữa các org
            e.HasOne(x => x.RankTier).WithMany().HasForeignKey(x => x.RankTierId);
            e.HasQueryFilter(x => x.OrgId == _orgId);           // cô lập theo tenant
        });
        b.Entity<PointTransaction>(e =>
        {
            e.HasOne(x => x.Member).WithMany(x => x.Transactions).HasForeignKey(x => x.MemberId);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<Reward>().HasQueryFilter(x => x.OrgId == _orgId);

        // ---- PORT 1:1 2022.7.Loyalty: tên bảng + khoá đúng DDL nguồn ----
        // KHÔNG đặt HasQueryFilter theo tenant Guid cho 4 bảng này: nguồn phân vùng bằng
        // NetworkID/OrgID (Crd_Member.cs:12-13) — đã khai báo ở Models/PortLoyalty.cs.
        b.Entity<CrdMember>(e =>
        {
            e.ToTable("Crd_Member");
            e.HasKey(x => x.MemberNo);                  // DDL 20221108.z11.Crd_Member.Crd_Card.sql:169
            e.HasIndex(x => x.PhoneNo);
            e.HasIndex(x => x.CarNo);
            e.HasIndex(x => x.VIN);
        });
        b.Entity<CrdCard>(e =>
        {
            e.ToTable("Crd_Card");
            e.HasKey(x => x.CardNo);                    // DDL :81
            e.HasIndex(x => x.MemberNo);                // DDL :196 FK_CrdMember_CrdCard
        });
        b.Entity<CrdCardTransaction>(e =>
        {
            e.ToTable("Crd_CardTransaction");
            e.HasKey(x => x.AutoId);                    // DDL :284 (IDENTITY(1,1))
            e.Property(x => x.AutoId).ValueGeneratedOnAdd();
            e.HasIndex(x => new { x.MemberNo, x.CardNo });
            e.HasIndex(x => x.DealPointType);
        });
        b.Entity<CrdMemberDiscountTransaction>(e =>
        {
            e.ToTable("Crd_MemberDiscountTransaction");
            e.HasKey(x => x.AutoId);                    // DDL 20260817.CreTb....sql:51
            e.Property(x => x.AutoId).ValueGeneratedOnAdd();
            e.HasIndex(x => new { x.MemberNo, x.CardNo });
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
