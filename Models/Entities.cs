namespace MiniLoyalty.Models;

public enum PointTxType { Earn = 0, Redeem = 1, Birthday = 2, Adjust = 3, Expiry = 4, Introduction = 5, ServiceTurn = 6, Discount = 7, PointUse = 8, Sale = 9 }

/// <summary>Loại giao dịch điểm voucher (Crd_MemberVoucherTransaction.DealPointType).</summary>
public enum VoucherTxType { Award = 0, Use = 1 }   // VOUCHERXM = tặng, VOUCHERSD = sử dụng

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
    public int PointVoucher { get; set; }      // Crd_Member.PointVoucher — tổng điểm voucher còn lại (không dùng xét hạng)
    public int RankTierId { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.Now;

    // Giới thiệu mua xe (Crd_Member.MemberNoIntro/PointIntro): hội viên này do ai giới thiệu,
    // và số điểm thưởng cho người giới thiệu khi hội viên này hoàn tất đăng ký.
    public string? MemberNoIntro { get; set; }   // Crd_Member.MemberNoIntro — mã hội viên người giới thiệu
    public int PointIntro { get; set; }          // Crd_Member.PointIntro — điểm thưởng cho người giới thiệu
    // Tặng điểm mua xe mới (Crd_Member.PointBuyCar): số điểm thưởng khi hội viên mua xe mới.
    // Khi hoàn tất mua xe, hệ thống cộng PointBuyCar điểm (DealPointType=SALES).
    public int PointBuyCar { get; set; }         // Crd_Member.PointBuyCar — điểm thưởng mua xe mới

    // Dữ liệu kỳ xét hạng hiện tại (Crd_Member/Crd_Card): reset về 0 khi sang kỳ mới.
    public int PointCardRank { get; set; }     // Crd_Member.PointCardRank — điểm xét hạng tích trong kỳ
    public int QtyVisitAvail { get; set; }     // Crd_Member.QtyVisitAvail — số lượt dịch vụ trong kỳ
    public DateTime? EffDateStart { get; set; }   // Crd_Card.EffDateStart — đầu kỳ xét hạng
    public DateTime? EffDateEnd { get; set; }     // Crd_Card.EffDateEnd — cuối kỳ xét hạng
    public string CardSourceCode { get; set; } = "NEW";   // Crd_Card.CardSourceCode: NEW/UP/DOWN/KEEP/RENEW

    public RankTier? RankTier { get; set; }
    public List<PointTransaction> Transactions { get; set; } = [];
    public List<MemberDiscountTransaction> Discounts { get; set; } = [];
    public List<MemberVoucherTransaction> Vouchers { get; set; } = [];
    public List<MemberPromotionUse> PromotionUses { get; set; } = [];
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
    public int QtyVisit { get; set; }          // Crd_CardTransaction.QtyVisitChTotal — số lượt dịch vụ ghi nhận (SERVICETURN)
    public DateTime? ExpiresAt { get; set; }   // điểm dương hết hạn vào thời điểm này (PointExpiryDTime)
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public Member Member { get; set; } = null!;
}

/// <summary>
/// Giao dịch chiết khấu dịch vụ (Crd_MemberDiscountTransaction, DealPointType = DISCOUNTRO):
/// khi hội viên dùng dịch vụ, hệ thống áp % chiết khấu theo hạng thẻ hiện tại lên doanh thu dịch vụ.
/// </summary>
public class MemberDiscountTransaction : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public int MemberId { get; set; }
    public string? RefNo { get; set; }              // Crd_MemberDiscountTransaction.RefNo — số tham chiếu (số RO/HĐ)
    public int CardTypeApplyId { get; set; }        // Crd_MemberDiscountTransaction.CardTypeApply — hạng áp dụng chiết khấu
    public decimal PolicyDiscountRate { get; set; } // Crd_MemberDiscountTransaction.PolicyDiscountRate — % chiết khấu theo hạng
    public decimal AmountForDC { get; set; }        // Crd_MemberDiscountTransaction.AmountForDC — doanh thu dịch vụ tính chiết khấu
    public decimal DiscountAmount { get; set; }     // Crd_MemberDiscountTransaction.PointChTotal — tiền chiết khấu
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public Member Member { get; set; } = null!;
    public RankTier? CardTypeApply { get; set; }
}

/// <summary>
/// Giao dịch điểm voucher (Crd_MemberVoucherTransaction): tách biệt khỏi điểm tiêu dùng thường.
/// VOUCHERXM = HTV tặng điểm voucher xe mới (cộng), VOUCHERSD = hội viên sử dụng điểm voucher (trừ).
/// Điểm voucher KHÔNG dùng để xét hạng.
/// </summary>
public class MemberVoucherTransaction : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public int MemberId { get; set; }
    public VoucherTxType Type { get; set; }         // Crd_MemberVoucherTransaction.DealPointType (VOUCHERXM/VOUCHERSD)
    public string? RefNo { get; set; }              // Crd_MemberVoucherTransaction.RefNo — mã giao dịch
    public string? VoucherCode { get; set; }        // Crd_MemberVoucherTransaction.PrmVoucherCode — mã voucher
    public int Points { get; set; }                 // Crd_MemberVoucherTransaction.PointChTotal — điểm voucher tích/tiêu (+/-)
    public int BalanceAfter { get; set; }           // Crd_Member.PointVoucher sau giao dịch
    public DateTime? ExpiryDate { get; set; }       // Crd_MemberVoucherTransaction.PointExpiryDate — ngày hết hạn điểm voucher
    public string? Note { get; set; }
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

/// <summary>
/// Chương trình ưu đãi (Mst_PromotionProgram): ưu đãi hội viên có thể dùng điểm để đổi
/// (ví dụ: gói bảo dưỡng, phụ kiện). Mỗi ưu đãi có giá điểm (PointCost) và số lượng còn lại (Qty).
/// </summary>
public class Promotion : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Code { get; set; } = "";          // Mst_PromotionProgram.PrProgramCode
    public string Name { get; set; } = "";          // Mst_PromotionProgram.PrProgramName
    public int PointCost { get; set; }              // điểm cần để đổi ưu đãi
    public string? Description { get; set; }
    public int Qty { get; set; } = 100;             // Mst_PromotionProgramDtl.Qty — số lượng còn lại
    public bool IsActive { get; set; } = true;      // Mst_PromotionProgram.FlagActive
}

/// <summary>
/// Giao dịch sử dụng ưu đãi (Crd_DealUsePromotion): hội viên dùng điểm khả dụng để đổi ưu đãi tại đại lý.
/// Tương ứng Crd_CardTransaction với DealPointType = POINTUSE, PointChTotal &lt; 0 (trừ điểm).
/// </summary>
public class MemberPromotionUse : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public int MemberId { get; set; }
    public int PromotionId { get; set; }            // ưu đãi được sử dụng
    public string? PrProgramCode { get; set; }      // Crd_CardTransaction.PrProgramCode — mã ưu đãi
    public int Points { get; set; }                 // Crd_CardTransaction.PointChTotal — điểm bị trừ (âm)
    public int BalanceAfter { get; set; }           // Crd_Card.PointAvail sau giao dịch
    public string? RefNo { get; set; }              // Crd_DealUsePromotion.DealUsePrmNo — số phiếu sử dụng ưu đãi
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;   // Crd_DealUsePromotion.UsePrmDTime
    public Member Member { get; set; } = null!;
    public Promotion? PromotionNav { get; set; }
}
