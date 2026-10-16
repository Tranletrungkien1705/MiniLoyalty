using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MiniLoyalty.Models.Port;

/// <summary>
/// Bảng danh sách đại lý sử dụng đặc cách theo kỳ thẻ — port 1:1 Crd_CardDealerUseException (Card.cs:27083-27162).
/// PK composite: (CardNo, DealerCode).
/// </summary>
public class Crd_CardDealerUseException
{
    public string CardNo { get; set; } = "";
    public string DealerCode { get; set; } = "";
    public string? NetworkID { get; set; }
    public string? Remark { get; set; }
    public DateTime? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
}

/// <summary>
/// Cấu hình EF cho <see cref="Crd_CardDealerUseException"/> — bảng Crd_CardDealerUseException, PK (CardNo, DealerCode).
/// </summary>
public class Crd_CardDealerUseExceptionConfig : IEntityTypeConfiguration<Crd_CardDealerUseException>
{
    public void Configure(EntityTypeBuilder<Crd_CardDealerUseException> b)
    {
        b.ToTable("Crd_CardDealerUseException");
        b.HasKey(x => new { x.CardNo, x.DealerCode });
    }
}
