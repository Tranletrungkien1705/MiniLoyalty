using Microsoft.EntityFrameworkCore;
using MiniLoyalty.Models;

namespace MiniLoyalty.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<RankTier> RankTiers => Set<RankTier>();
    public DbSet<Member> Members => Set<Member>();
    public DbSet<PointTransaction> PointTransactions => Set<PointTransaction>();
    public DbSet<Reward> Rewards => Set<Reward>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<RankTier>().Property(x => x.DiscountPercent).HasPrecision(5, 2);
        b.Entity<Member>(e =>
        {
            e.HasIndex(x => x.Code).IsUnique();
            e.HasOne(x => x.RankTier).WithMany().HasForeignKey(x => x.RankTierId);
        });
        b.Entity<PointTransaction>()
            .HasOne(x => x.Member).WithMany(x => x.Transactions).HasForeignKey(x => x.MemberId);
    }
}
