using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;

namespace MiniLoyalty.Models.Port;

public class Mst_ResignReason
{
    // Cột cơ sở: 9 cột (Mst_ResignReason.cs:9-22)
    public string OrgID { get; set; } = "";
    public string ReasonID { get; set; } = "";
    public string? NetworkID { get; set; }
    public string? ResignReason { get; set; }
    public string? ReasonDesc { get; set; }
    public string? FlagActive { get; set; }
    public string? Remark { get; set; }
    public DateTime? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }

    // Cột join: 1 cột (Mst_ResignReason.cs:21)
    [NotMapped]
    public string? mnnt_NNTFullName { get; set; }
}

public class Mst_ResignReasonConfig : IEntityTypeConfiguration<Mst_ResignReason>
{
    public void Configure(EntityTypeBuilder<Mst_ResignReason> b)
    {
        b.ToTable("Mst_ResignReason");
        // Khóa chính (PK): OrgID, ReasonID (MasterData.cs:24716-24718)
        b.HasKey(x => new { x.OrgID, x.ReasonID });
    }
}

public class Mst_ResignReasonSeeder : IPortSeeder
{
    public int Order => 29; // DEMO, idempotent

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<Mst_ResignReason>().AnyAsync()) return;

        var now = new DateTime(2026, 10, 10, 10, 0, 0, DateTimeKind.Utc);
        db.Set<Mst_ResignReason>().AddRange(
            new Mst_ResignReason { OrgID = "4020320001", ReasonID = "R01", ResignReason = "Nghỉ hưu", FlagActive = "1", NetworkID = "DEMO", LogLUBy = "seed", LogLUDTimeUTC = now },
            new Mst_ResignReason { OrgID = "4020320001", ReasonID = "R02", ResignReason = "Chuyển công tác", FlagActive = "1", NetworkID = "DEMO", LogLUBy = "seed", LogLUDTimeUTC = now },
            new Mst_ResignReason { OrgID = "4020320001", ReasonID = "R03", ResignReason = "Việc gia đình", FlagActive = "0", NetworkID = "DEMO", LogLUBy = "seed", LogLUDTimeUTC = now },
            new Mst_ResignReason { OrgID = "4020320002", ReasonID = "R01", ResignReason = "Du học", FlagActive = "1", NetworkID = "DEMO", LogLUBy = "seed", LogLUDTimeUTC = now }
        );
        await db.SaveChangesAsync();
    }
}

/// <summary>
/// Bảng sinh sequence cho ReasonID — port 1:1 bảng Seq_ReasonID (Seq.cs:118, SeqType.ReasonID; PK AutoId tự tăng).
/// </summary>
public class Seq_ReasonID
{
    public long AutoId { get; set; }
    public string? Remark { get; set; }
}

public class Seq_ReasonIDConfig : IEntityTypeConfiguration<Seq_ReasonID>
{
    public void Configure(EntityTypeBuilder<Seq_ReasonID> b)
    {
        b.ToTable("Seq_ReasonID");
        b.HasKey(x => x.AutoId);
        b.Property(x => x.AutoId).ValueGeneratedOnAdd();
    }
}
