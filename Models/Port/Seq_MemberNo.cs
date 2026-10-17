using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MiniLoyalty.Models.Port;

/// <summary>
/// Entity sinh sequence cho mã hội viên MemberNo 6 số (Card.cs:27568-27640).
/// </summary>
public class Seq_MemberNo
{
    public long Id { get; set; }
    public string? Remark { get; set; }
}

/// <summary>
/// Cấu hình EF cho <see cref="Seq_MemberNo"/> — bảng Seq_MemberNo, PK Id tự tăng.
/// </summary>
public class Seq_MemberNoConfig : IEntityTypeConfiguration<Seq_MemberNo>
{
    public void Configure(EntityTypeBuilder<Seq_MemberNo> b)
    {
        b.ToTable("Seq_MemberNo");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedOnAdd();
    }
}
