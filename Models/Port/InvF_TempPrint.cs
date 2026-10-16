using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;

namespace MiniLoyalty.Models.Port;

public class InvF_TempPrint
{
    public string IF_TempPrintNo { get; set; } = "";
    public string? NetworkID { get; set; }
    public string? TempPrintType { get; set; }
    public string? TempPrintName { get; set; }
    public string? TempPrintDesc { get; set; }
    public string? PathFile { get; set; }
    public string? IsDefault { get; set; }
    public string? FlagActive { get; set; }
    public DateTime? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
}

public class InvF_TempPrintConfig : IEntityTypeConfiguration<InvF_TempPrint>
{
    public void Configure(EntityTypeBuilder<InvF_TempPrint> b)
    {
        b.ToTable("InvF_TempPrint");
        b.HasKey(x => x.IF_TempPrintNo);
        b.Property(x => x.IF_TempPrintNo).IsRequired();
    }
}

public class InvF_TempPrintSeeder : IPortSeeder
{
    public int Order => 63;

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<InvF_TempPrint>().AnyAsync()) return;

        var logTime = new DateTime(2026, 10, 10, 10, 0, 0, DateTimeKind.Utc);
        db.Set<InvF_TempPrint>().AddRange(
            new InvF_TempPrint { IF_TempPrintNo = "TP01", NetworkID = "DEMO", TempPrintType = "TP01", TempPrintName = "Mau in hoa don 1", TempPrintDesc = "Mo ta mau in 1", PathFile = "Reports/TP01.frx", IsDefault = "1", FlagActive = "1", LogLUBy = "seed", LogLUDTimeUTC = logTime },
            new InvF_TempPrint { IF_TempPrintNo = "TP02", NetworkID = "DEMO", TempPrintType = "TP02", TempPrintName = "Mau in hoa don 2", TempPrintDesc = "Mo ta mau in 2", PathFile = "Reports/TP02.frx", IsDefault = "0", FlagActive = "1", LogLUBy = "seed", LogLUDTimeUTC = logTime }
        );
        await db.SaveChangesAsync();
    }
}
