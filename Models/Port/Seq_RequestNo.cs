using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;

namespace MiniLoyalty.Models.Port;

/// <summary>
/// Bảng sinh sequence cho số đề nghị thay đổi thông tin hội viên — port 1:1 bảng Seq_RequestNo (Seq.cs:148, SeqType.RequestNo).
/// Format: {0}REQUESTNO.{3}.{1:00000}{2}, maxSeq: 100000.
/// PK = AutoId (bigint IDENTITY(1,1) tự tăng).
/// </summary>
public class Seq_RequestNo
{
    public long AutoId { get; set; }
    public string? Remark { get; set; }
}

/// <summary>
/// Cấu hình EF cho Seq_RequestNo — PK AutoId tự tăng.
/// </summary>
public class Seq_RequestNoConfig : IEntityTypeConfiguration<Seq_RequestNo>
{
    public void Configure(EntityTypeBuilder<Seq_RequestNo> b)
    {
        b.ToTable("Seq_RequestNo");
        b.HasKey(x => x.AutoId);
        b.Property(x => x.AutoId).ValueGeneratedOnAdd();
    }
}

/// <summary>
/// Seeder bổ sung cho Crd_MemberChangeInfoController/Add (#033).
/// Order => 98 (dải 10-99 chưa dùng, chạy sau Order 89/95 để cập nhật trạng thái hội viên MB002).
/// Idempotent. Đảm bảo MB001 có ít nhất một giao dịch xét hạng gần nhất (CONSUMPTION/SERVICETURN) tại DL01
/// và MB002 có MemberStatus = 'APPROVE' theo đúng tiền đề P2 của đề bài.
/// </summary>
public class Seq_RequestNoSeeder : IPortSeeder
{
    public int Order => 98;

    public async Task SeedAsync(AppDbContext db)
    {
        // 1. Đảm bảo MB001 có giao dịch xét hạng gần nhất tại DL01 (Card.cs:28904-28925)
        if (!await db.CrdCardTransactions.AnyAsync(x => x.MemberNo == "MB001" && (x.DealPointType == "CONSUMPTION" || x.DealPointType == "SERVICETURN")))
        {
            db.CrdCardTransactions.Add(new CrdCardTransaction
            {
                RefNo = "TX_MB001_ADD_01",
                ProjectCode = "LOYALTY",
                NetworkID = "1001",
                OrgID = "1001",
                DLCode = "DL01",
                MemberNo = "MB001",
                CardNo = "CARD_MB001_ADD",
                CardSourceCode = "NEW",
                CardTypeUse = "P",
                CardTypeInit = "P",
                DealPointType = "CONSUMPTION",
                PointChTotal = 100,
                AmountChTotal = 1000000,
                PointRef = 100,
                AmountRef = 1000000,
                CreateDTimeUTC = DateTime.UtcNow.AddDays(-1),
                CreateBy = "seed",
                LUDTimeUTC = DateTime.UtcNow,
                LUBy = "seed",
                LogLUDTimeUTC = DateTime.UtcNow,
                LogLUBy = "seed"
            });
            await db.SaveChangesAsync();
        }

        // 2. Đảm bảo MB002 có MemberStatus = 'APPROVE' theo đúng tiền đề P2 của đề bài #033
        var mb002 = await db.CrdMembers.FirstOrDefaultAsync(x => x.MemberNo == "MB002");
        if (mb002 != null && mb002.MemberStatus != "APPROVE")
        {
            mb002.MemberStatus = "APPROVE";
            await db.SaveChangesAsync();
        }
    }
}
