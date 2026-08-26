namespace MiniLoyalty.Models;

public enum PointTxType { Earn = 0, Redeem = 1, Birthday = 2, Adjust = 3, Expiry = 4 }

/// <summary>Hạng thẻ — xếp theo điểm tích lũy trọn đời (lifetime), kèm % chiết khấu.</summary>
public class RankTier
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int MinLifetimePoints { get; set; }
    public decimal DiscountPercent { get; set; }
    public string ColorHex { get; set; } = "#94a3b8";
    public int SortOrder { get; set; }
}

/// <summary>Hội viên.</summary>
public class Member : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public DateTime? Dob { get; set; }
    public int Points { get; set; }            // điểm khả dụng (đổi được)
    public int LifetimePoints { get; set; }    // điểm tích lũy trọn đời (xếp hạng)
    public int RankTierId { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.Now;

    public RankTier? RankTier { get; set; }
    public List<PointTransaction> Transactions { get; set; } = [];
}

/// <summary>Giao dịch điểm (tích/đổi/sinh nhật/điều chỉnh/hết hạn).</summary>
public class PointTransaction : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public int MemberId { get; set; }
    public PointTxType Type { get; set; }
    public int Points { get; set; }            // + tích, - đổi/hết hạn
    public int BalanceAfter { get; set; }
    public string? Note { get; set; }
    public string? RefNo { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public Member Member { get; set; } = null!;
}

/// <summary>Quà/voucher đổi bằng điểm.</summary>
public class Reward : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Name { get; set; } = "";
    public int PointCost { get; set; }
    public string? Description { get; set; }
    public int Stock { get; set; } = 100;
    public bool IsActive { get; set; } = true;
}
