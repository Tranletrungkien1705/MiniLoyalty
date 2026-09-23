namespace MiniLoyalty.Models;

public enum PointTxType { Earn = 0, Redeem = 1, Birthday = 2, Adjust = 3, Expiry = 4, Introduction = 5 }

/// <summary>Hạng thẻ — xếp theo điểm tích lũy trọn đời (lifetime), kèm % chiết khấu.</summary>
public class RankTier
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int MinLifetimePoints { get; set; }
    public decimal DiscountPercent { get; set; }
    public int BirthdayPoints { get; set; }   // điểm tặng sinh nhật theo hạng (Mst_BirthPolicyDtl.Point)
    public string ColorHex { get; set; } = "#94a3b8";
    public int SortOrder { get; set; }

    // Chính sách xét hạng cuối kỳ (Mst_RankPolicy): ngưỡng để DUY TRÌ hạng trong kỳ.
    // Không đạt ngưỡng duy trì → xuống 1 hạng (DOWN); đạt → giữ hạng (KEEP).
    public int PointKeepBegin { get; set; }      // Mst_RankPolicy.PointKeepBegin — điểm xét hạng tối thiểu để duy trì
    public int QtyVisitKeepBegin { get; set; }   // Mst_RankPolicy.QtyVisitKeepBegin — số lượt dịch vụ tối thiểu để duy trì

    // Chính sách xét hạng cuối kỳ (Mst_RankPolicy): ngưỡng để NÂNG hạng trong kỳ.
    // Đạt cả hai ngưỡng nâng → lên hạng kế tiếp (UP); ngược lại mới xét duy trì/xuống.
    public int PointUpBegin { get; set; }        // Mst_RankPolicy.PointUpBegin — điểm xét hạng tối thiểu để nâng hạng
    public int QtyVisitUpBegin { get; set; }     // Mst_RankPolicy.QtyVisitUpBegin — số lượt dịch vụ tối thiểu để nâng hạng
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

    // Giới thiệu mua xe (Crd_Member.MemberNoIntro/PointIntro): hội viên này do ai giới thiệu,
    // và số điểm thưởng cho người giới thiệu khi hội viên này hoàn tất đăng ký.
    public string? MemberNoIntro { get; set; }   // Crd_Member.MemberNoIntro — mã hội viên người giới thiệu
    public int PointIntro { get; set; }          // Crd_Member.PointIntro — điểm thưởng cho người giới thiệu

    // Dữ liệu kỳ xét hạng hiện tại (Crd_Member/Crd_Card): reset về 0 khi sang kỳ mới.
    public int PointCardRank { get; set; }     // Crd_Member.PointCardRank — điểm xét hạng tích trong kỳ
    public int QtyVisitAvail { get; set; }     // Crd_Member.QtyVisitAvail — số lượt dịch vụ trong kỳ
    public DateTime? EffDateStart { get; set; }   // Crd_Card.EffDateStart — đầu kỳ xét hạng
    public DateTime? EffDateEnd { get; set; }     // Crd_Card.EffDateEnd — cuối kỳ xét hạng
    public string CardSourceCode { get; set; } = "NEW";   // Crd_Card.CardSourceCode: NEW/UP/DOWN/KEEP/RENEW

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
    public DateTime? ExpiresAt { get; set; }   // điểm dương hết hạn vào thời điểm này (PointExpiryDTime)
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
