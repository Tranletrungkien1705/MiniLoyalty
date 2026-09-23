namespace MiniLoyalty.Models;

public enum PointTxType { Earn = 0, Redeem = 1, Birthday = 2, Adjust = 3, Expiry = 4, Introduction = 5, ServiceTurn = 6, Discount = 7, PointUse = 8, Sale = 9, Consumption = 10, Kmbh = 11, PrProgram = 12, PointIncrease = 13, OpenCard = 14, Support = 15 }

/// <summary>Phân loại ghi nhận sử dụng ưu đãi (Crd_DealUsePromotion): POINTUSE trừ điểm, PRPROGRAM chỉ ghi nhận (không đổi điểm).</summary>
public enum PromotionUseKind { PointUse = 0, PrProgram = 1 }

/// <summary>Loại giao dịch điểm voucher (Crd_MemberVoucherTransaction.DealPointType).</summary>
public enum VoucherTxType { Award = 0, Use = 1, BirthdayVoucher = 2 }   // VOUCHERXM = tặng, VOUCHERSD = sử dụng, VOUCHERTSN = voucher sinh nhật

/// <summary>Trạng thái hội viên (Crd_Member.MemberStatus): PENDING → APPROVE → CANCEL.</summary>
public enum MemberStatus { Pending = 0, Approve = 1, Cancel = 2 }   // TConst.MemberStatus.Pending/Approve/Cancel

/// <summary>Trạng thái thẻ (Crd_Card.CardStatus): PENDING → APPROVE → CANCEL.</summary>
public enum CardStatus { Pending = 0, Approve = 1, Cancel = 2 }   // TConst.CardStatus.Pending/Approve/Cancel

/// <summary>Trạng thái yêu cầu thay đổi thông tin (Crd_MemberChangeInfo.RequestStatus): PENDING → APPROVE → FINISH, hoặc CANCEL khi từ chối.</summary>
public enum ChangeRequestStatus { Pending = 0, Approve = 1, Finish = 2, Cancel = 3 }   // TConst.RequestStatus.Pending/Approve/Finish/Cancel

/// <summary>Loại yêu cầu thay đổi (Crd_MemberChangeInfo.RequestType): ChangeInfo = đổi thông tin, CancelMember = huỷ hội viên.</summary>
public enum ChangeRequestType { ChangeInfo = 0, CancelMember = 1 }   // TConst.RequestType.ChangeInfo/CancelMember
/// <summary>Trạng thái yêu cầu đặc cách thẻ (Crd_Card.CardStatus của kỳ thẻ đặc cách): PENDING → APPROVE, hoặc CANCEL khi từ chối.</summary>
public enum CardExceptionStatus { Pending = 0, Approve = 1, Cancel = 2 }   // TConst.CardStatus.Pending/Approve/Cancel

/// <summary>
/// Trạng thái yêu cầu đăng ký hội viên (Req_MemberRegister.ReqMemberRegisterStatus / TConst.ReqMemberRegisterStatus):
/// PENDING (đại lý gửi) → APPROVE (HTV duyệt) → FINISH (hoàn tất đăng ký), hoặc CANCEL/REJECT khi huỷ/từ chối.
/// </summary>
public enum MemberRegisterStatus { Pending = 0, Approve = 1, Finish = 2, Cancel = 3, Reject = 4 }   // TConst.ReqMemberRegisterStatus

/// <summary>
/// Hành động xét hạng (Crd_CardRank.FunctionActionType / TConst.RankActionType): ghi lại kết quả
/// mỗi lần job xét hạng cuối kỳ xử lý 1 hội viên — UP nâng hạng, KEEP duy trì, DOWN xuống hạng.
/// </summary>
public enum RankActionType { Up = 0, Keep = 1, Down = 2 }   // TConst.RankActionType.Up/Keep/Down

/// <summary>
/// Trạng thái chương trình tặng điểm xe mới (Prm_CarNew.PRMCNStatus / TConst.PRMCNStatus):
/// PENDING (đại lý tạo) → APPROVE (HTV duyệt) → FINISH (hoàn tất, chương trình có hiệu lực), hoặc CANCEL khi huỷ.
/// </summary>
public enum PrmCarNewStatus { Pending = 0, Approve = 1, Finish = 2, Cancel = 3 }   // TConst.PRMCNStatus

/// <summary>Hạng thẻ — xếp theo điểm tích lũy trọn đời (lifetime), kèm % chiết khấu.</summary>
public class RankTier
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int MinLifetimePoints { get; set; }
    public decimal DiscountPercent { get; set; }
    public int BirthdayPoints { get; set; }   // điểm tặng sinh nhật theo hạng (Mst_BirthPolicyDtl.Point)
    // Voucher sinh nhật theo hạng (Mst_BirthPolicyDtl.VoucherValue/VoucherExpireDays):
    // VoucherValue > 0 → phát 1 voucher/năm cho hội viên hạng này vào ngày sinh nhật.
    public int BirthdayVoucherPoints { get; set; }      // Mst_BirthPolicyDtl.VoucherValue — điểm voucher tặng sinh nhật
    public int BirthdayVoucherExpireDays { get; set; }  // Mst_BirthPolicyDtl.VoucherExpireDays — số ngày voucher hiệu lực
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
    // Điểm khuyến mại bán hàng (Crd_Member.PointBuyCreta): HTV (nhà sản xuất) tặng thêm điểm khi
    // hội viên mua xe thuộc chương trình khuyến mại. Khi đủ điều kiện, hệ thống cộng PointBuyCreta
    // điểm (DealPointType=KMBH) — khác với SALES (điểm thưởng của đại lý).
    public int PointBuyCreta { get; set; }       // Crd_Member.PointBuyCreta — điểm khuyến mại bán hàng (HTV)
    // Số giao dịch mua xe (Crd_Member.DealNo): dùng để chặn áp dụng trùng chương trình tặng điểm
    // khuyến mại bán hàng Creta cho cùng một giao dịch giao xe.
    public string? DealNo { get; set; }          // Crd_Member.DealNo — số giao dịch mua xe
    // Điểm tặng mở thẻ mới (Crd_Member.PointOpenCard): khi hội viên hoàn tất đăng ký (Finish),
    // hệ thống tặng PointOpenCard điểm chào mừng (DealPointType=OPENCARD, DLCode=HTV).
    public int PointOpenCard { get; set; }       // Crd_Member.PointOpenCard — điểm tặng mở thẻ mới

    // Dữ liệu kỳ xét hạng hiện tại (Crd_Member/Crd_Card): reset về 0 khi sang kỳ mới.
    public int PointCardRank { get; set; }     // Crd_Member.PointCardRank — điểm xét hạng tích trong kỳ
    public int QtyVisitAvail { get; set; }     // Crd_Member.QtyVisitAvail — số lượt dịch vụ trong kỳ
    public DateTime? EffDateStart { get; set; }   // Crd_Card.EffDateStart — đầu kỳ xét hạng
    public DateTime? EffDateEnd { get; set; }     // Crd_Card.EffDateEnd — cuối kỳ xét hạng
    public string CardSourceCode { get; set; } = "NEW";   // Crd_Card.CardSourceCode: NEW/UP/DOWN/KEEP/RENEW

    // Trạng thái hội viên/thẻ (Crd_Member.MemberStatus, Crd_Card.CardStatus).
    // Khi hội viên bị vô hiệu hoá (Crd_Member_InActiveX): MemberStatus = Cancel, thẻ bị huỷ (CardStatus = Cancel),
    // và toàn bộ điểm còn lại bị đặt hết hạn ngay (PointExpiryDTime = cuối tháng).
    public MemberStatus Status { get; set; } = MemberStatus.Approve;   // Crd_Member.MemberStatus
    public CardStatus CardStatus { get; set; } = CardStatus.Approve;   // Crd_Card.CardStatus
    public DateTime? InactiveAt { get; set; }      // Crd_Member.InactiveDTimeUTC — thời điểm vô hiệu hoá
    public string? InactiveBy { get; set; }        // Crd_Member.InactiveBy — người vô hiệu hoá
    public string? Remark { get; set; }            // Crd_Member.Remark — lý do vô hiệu hoá (audit)

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
    public decimal AmountChTotal { get; set; } // Crd_CardTransaction.AmountChTotal — doanh thu dịch vụ (CONSUMPTION)
    public int PointChRankTotal { get; set; }  // Crd_CardTransaction.PointChRankTotal — điểm xét hạng cộng thêm (CONSUMPTION)
    public string? PrProgramCode { get; set; } // Crd_CardTransaction.PrProgramCode — mã chương trình ưu đãi (PRPROGRAM)
    public int QtyPrChTotal { get; set; }      // Crd_CardTransaction.QtyPrChTotal — số lượng ưu đãi ghi nhận (PRPROGRAM)
    public int QtyPrUsed { get; set; }         // Crd_CardTransaction.QtyPrUsed — số lượng ưu đãi đã dùng (PRPROGRAM)
    // Hỗ trợ điều chỉnh điểm (DealPointType=SUPPORT): nhân viên hỗ trợ cộng/trừ điểm thủ công.
    public string? DLCode { get; set; }        // Crd_CardTransaction.DLCode — đại lý thực hiện (SUPPORT = hỗ trợ idocNet)
    public string? FunctionRemark { get; set; } // Crd_CardTransaction.FunctionRemark — lý do điều chỉnh (audit)
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

/// <summary>
/// Chính sách quy đổi tiền dịch vụ → điểm (Mst_PolicyMoneyToPointServiceDtl): theo hạng thẻ,
/// cứ ConvertValue đồng doanh thu dịch vụ thì được ConvertPoint điểm. Dùng cho giao dịch CONSUMPTION.
/// </summary>
public class ServicePolicy : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string PolicyCode { get; set; } = "DEFAULT";   // Mst_PolicyMoneyToPointService.PolicyCode
    public int RankTierId { get; set; }                   // Mst_PolicyMoneyToPointServiceDtl.CardType — hạng áp dụng
    public decimal ConvertValue { get; set; }             // Mst_PolicyMoneyToPointServiceDtl.ConvertValue — số tiền (đ)
    public int ConvertPoint { get; set; }                 // Mst_PolicyMoneyToPointServiceDtl.ConvertPoint — số điểm tương ứng
    public bool IsActive { get; set; } = true;            // Mst_PolicyMoneyToPointServiceDtl.FlagActive
    public RankTier? RankTier { get; set; }
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
/// Với Kind = PrProgram (DealPointType = PRPROGRAM): chỉ GHI NHẬN việc dùng ưu đãi (PointChTotal = 0,
/// không trừ điểm) để tracking số lần dùng — dùng QtyPrChTotal/QtyPrUsed.
/// </summary>
public class MemberPromotionUse : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public int MemberId { get; set; }
    public int PromotionId { get; set; }            // ưu đãi được sử dụng
    public PromotionUseKind Kind { get; set; } = PromotionUseKind.PointUse;   // POINTUSE (trừ điểm) / PRPROGRAM (chỉ ghi nhận)
    public string? PrProgramCode { get; set; }      // Crd_CardTransaction.PrProgramCode — mã ưu đãi
    public int Points { get; set; }                 // Crd_CardTransaction.PointChTotal — điểm bị trừ (âm); PRPROGRAM = 0
    public int BalanceAfter { get; set; }           // Crd_Card.PointAvail sau giao dịch
    public int QtyPrChTotal { get; set; }           // Crd_DealUsePromotionDtl.QtyPrChTotal — số lượng ưu đãi ghi nhận (PRPROGRAM)
    public int QtyPrUsed { get; set; }              // Crd_DealUsePromotionDtl.QtyPrUsed — số lượng ưu đãi đã dùng (PRPROGRAM)
    public string? RefNo { get; set; }              // Crd_DealUsePromotion.DealUsePrmNo — số phiếu sử dụng ưu đãi
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;   // Crd_DealUsePromotion.UsePrmDTime
    public Member Member { get; set; } = null!;
    public Promotion? PromotionNav { get; set; }
}

/// <summary>
/// Danh mục cột được phép thay đổi (Crd_MemberColumnChange): whitelist các cột của Crd_Member
/// mà hội viên/đại lý được phép đề nghị thay đổi qua yêu cầu Crd_MemberChangeInfo.
/// </summary>
public class MemberColumnChange : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string ColumnCode { get; set; } = "";   // Crd_MemberColumnChange.ColumnCode — mã cột (MemberName/PhoneNo/...)
    public string ColumnName { get; set; } = "";   // Crd_MemberColumnChange.ColumnName — tên hiển thị
    public bool IsActive { get; set; } = true;      // Crd_MemberColumnChange.FlagActive
}

/// <summary>
/// Yêu cầu thay đổi thông tin hội viên (Crd_MemberChangeInfo): hội viên/đại lý đề nghị đổi thông tin
/// cá nhân (tên, giới tính, CMND, SĐT, ngày sinh, địa chỉ, tỉnh/huyện, dòng xe, biển số, VIN...).
/// Đi qua luồng duyệt: PENDING (tạo) → APPROVE (duyệt) → FINISH (hoàn tất, áp thay đổi vào hội viên),
/// hoặc CANCEL khi bị từ chối. Mỗi yêu cầu gồm nhiều dòng chi tiết (MemberChangeRequestDtl).
/// </summary>
public class MemberChangeRequest : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string RequestNo { get; set; } = "";            // Crd_MemberChangeInfo.RequestNo — số yêu cầu
    public int MemberId { get; set; }                       // Crd_MemberChangeInfo.MemberNo — hội viên đề nghị
    public ChangeRequestType RequestType { get; set; } = ChangeRequestType.ChangeInfo;   // Crd_MemberChangeInfo.RequestType
    public ChangeRequestStatus Status { get; set; } = ChangeRequestStatus.Pending;       // Crd_MemberChangeInfo.RequestStatus
    public string? DLCodeRequest { get; set; }              // Crd_MemberChangeInfo.DLCodeRequest — đại lý gửi yêu cầu
    public DateTime CreatedAt { get; set; } = DateTime.Now; // Crd_MemberChangeInfo.CreateDTimeUTC
    public string? CreatedBy { get; set; }                  // Crd_MemberChangeInfo.CreateBy
    public DateTime? ApproveAt { get; set; }                // Crd_MemberChangeInfo.ApproveDTimeUTC
    public string? ApproveBy { get; set; }                  // Crd_MemberChangeInfo.ApproveBy
    public DateTime? FinishAt { get; set; }                 // Crd_MemberChangeInfo.FinishDTimeUTC
    public string? FinishBy { get; set; }                   // Crd_MemberChangeInfo.FinishBy
    public string? Remark { get; set; }                     // Crd_MemberChangeInfo.Remark — ghi chú đại lý
    public string? RemarkHTV { get; set; }                  // Crd_MemberChangeInfo.RemarkHTV — ghi chú HTV khi duyệt/từ chối

    public Member Member { get; set; } = null!;
    public List<MemberChangeRequestDtl> Details { get; set; } = [];
}

/// <summary>
/// Chi tiết yêu cầu thay đổi thông tin (Crd_MemberChangeInfoDtl): mỗi dòng là 1 cột đề nghị đổi
/// (ColumnCode) kèm giá trị mới (ColumnValueNew). Khi FINISH, chỉ các dòng đã APPROVE mới được áp vào hội viên.
/// </summary>
public class MemberChangeRequestDtl : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public int RequestId { get; set; }                      // Crd_MemberChangeInfoDtl.RequestNo
    public string ColumnCode { get; set; } = "";            // Crd_MemberChangeInfoDtl.ColumnCode — cột đề nghị đổi
    public string? ColumnValueNew { get; set; }             // Crd_MemberChangeInfoDtl.ColumnValueNew — giá trị mới
    public ChangeRequestStatus DtlStatus { get; set; } = ChangeRequestStatus.Pending;   // Crd_MemberChangeInfoDtl.RequestDtlStatus
    public string? Remark { get; set; }                     // Crd_MemberChangeInfoDtl.Remark
    public MemberChangeRequest Request { get; set; } = null!;
}

/// <summary>
/// Yêu cầu đặc cách thẻ (Crd_Card_RequestExceptionX / Crd_Card_ApprExceptionX): hội viên/đại lý đề nghị
/// cấp một KỲ THẺ ĐẶC CÁCH (FlagExceptionally=1) — thẻ mới kế thừa hạng thẻ hiện tại nhưng được phép
/// sử dụng tại một số đại lý chỉ định (Crd_CardDealerUseException). Khi tạo, hệ thống sinh 1 kỳ thẻ mới
/// ở trạng thái PENDING (CardNoPrev = kỳ thẻ cũ). Khi duyệt (Approve), kỳ thẻ đang hiệu lực bị huỷ
/// (CardStatus=Cancel) và kỳ thẻ đặc cách được kích hoạt (CardStatus=Approve).
/// </summary>
public class CardException : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public int MemberId { get; set; }                       // Crd_Card.MemberNo — hội viên đề nghị
    public string CardNo { get; set; } = "";                // Crd_Card.CardNo — mã kỳ thẻ đặc cách (mới)
    public string? CardNoPrev { get; set; }                 // Crd_Card.CardNoPrev — kỳ thẻ cũ (kỳ thẻ đang hiệu lực)
    public int CardTypeUseId { get; set; }                  // Crd_Card.CardTypeUse — hạng thẻ sử dụng (kế thừa hạng hiện tại)
    public string? DLCodeExceptionally { get; set; }        // Crd_Card.DLCodeExceptionally — đại lý đặc cách
    public CardExceptionStatus Status { get; set; } = CardExceptionStatus.Pending;   // Crd_Card.CardStatus
    public string? Remark { get; set; }                     // Crd_Card.Remark — ghi chú đại lý
    public string? RemarkHTV { get; set; }                  // ghi chú HTV khi duyệt/từ chối
    public DateTime CreatedAt { get; set; } = DateTime.Now; // Crd_Card.CreateDTimeUTC
    public DateTime? ApproveAt { get; set; }                // Crd_Card.ApproveDTimeUTC
    public string? ApproveBy { get; set; }                  // Crd_Card.ApproveBy
    public Member Member { get; set; } = null!;
    public RankTier? CardTypeUse { get; set; }
    public List<CardExceptionDealer> Dealers { get; set; } = [];   // Crd_CardDealerUseException — đại lý được dùng thẻ đặc cách
}

/// <summary>
/// Đại lý được phép sử dụng kỳ thẻ đặc cách (Crd_CardDealerUseException): danh sách đại lý chỉ định
/// theo kỳ thẻ đặc cách — "Danh sách đại lý sử dụng đặc cách theo kỳ thẻ".
/// </summary>
public class CardExceptionDealer : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public int CardExceptionId { get; set; }                // Crd_CardDealerUseException.CardNo (FK tới kỳ thẻ đặc cách)
    public string DealerCode { get; set; } = "";            // Crd_CardDealerUseException.DealerCode — mã đại lý
    public string? Remark { get; set; }                     // Crd_CardDealerUseException.Remark
    public CardException CardException { get; set; } = null!;
}

/// <summary>
/// Yêu cầu đăng ký hội viên (Req_MemberRegister): đại lý gửi thông tin khách hàng + xe để đề nghị cấp thẻ
/// hội viên mới. Đi qua luồng duyệt: PENDING (đại lý gửi) → APPROVE (HTV duyệt) → FINISH (hoàn tất đăng ký),
/// hoặc CANCEL (đại lý huỷ) / REJECT (HTV từ chối) khi không hợp lệ. Khi duyệt, hệ thống chặn nếu đã có hội viên
/// APPROVE trùng CarNo/VIN (tránh cấp trùng thẻ cho cùng một xe).
/// </summary>
public class MemberRegister : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string ReqMemberRegisterCode { get; set; } = "";   // Req_MemberRegister.ReqMemberRegisterCode — mã lượt đăng ký
    public string? DLCodeRegis { get; set; }                   // Req_MemberRegister.DLCodeRegis — đại lý đăng ký
    public DateTime RegisterDate { get; set; } = DateTime.Now; // Req_MemberRegister.RegisterDate — ngày HV đăng ký
    public string? VIN { get; set; }                           // Req_MemberRegister.VIN — số khung
    public string? CarNo { get; set; }                         // Req_MemberRegister.CarNo — biển số
    public string? TradeMarkName { get; set; }                 // Req_MemberRegister.TradeMarkName — hiệu xe
    public string? ModelName { get; set; }                     // Req_MemberRegister.ModelName — dòng xe
    public string CustomerName { get; set; } = "";            // Req_MemberRegister.CustomerName — tên khách hàng
    public string? CustomerPhoneNo { get; set; }               // Req_MemberRegister.CustomerPhoneNo — SĐT
    public DateTime? CustomerDateOfBirth { get; set; }         // Req_MemberRegister.CustomerDateOfBirth — ngày sinh
    public string? CustomerIDNo { get; set; }                  // Req_MemberRegister.CustomerIDNo — MST/CCCD
    public string? CustomerEmail { get; set; }                 // Req_MemberRegister.CustomerEmail — email
    public string? CustomerAddress { get; set; }               // Req_MemberRegister.CustomerAddress — địa chỉ
    public string? GenderCode { get; set; }                    // Req_MemberRegister.GenderCode — mã giới tính
    public string? ProvinceName { get; set; }                  // Req_MemberRegister.ProvinceName — tỉnh/TP
    public string? DistrictName { get; set; }                  // Req_MemberRegister.DistrictName — quận/huyện
    public string? MemberNoIntro { get; set; }                 // Req_MemberRegister.MemberNoIntro — mã hội viên người giới thiệu
    public MemberRegisterStatus Status { get; set; } = MemberRegisterStatus.Pending;   // Req_MemberRegister.ReqMemberRegisterStatus
    public string? Remark { get; set; }                        // Req_MemberRegister.Remark — ghi chú
    public DateTime CreatedAt { get; set; } = DateTime.Now;    // Req_MemberRegister.CreateDTimeUTC
    public string? CreatedBy { get; set; }                     // Req_MemberRegister.CreateBy
    public DateTime? ApproveAt { get; set; }                   // Req_MemberRegister.ApproveDTimeUTC
    public string? ApproveBy { get; set; }                     // Req_MemberRegister.ApproveBy
    public DateTime? CancelAt { get; set; }                    // Req_MemberRegister.CancelDTimeUTC
    public string? CancelBy { get; set; }                      // Req_MemberRegister.CancelBy
    public DateTime? RejectAt { get; set; }                    // Req_MemberRegister.RejectDTimeUTC
    public string? RejectBy { get; set; }                      // Req_MemberRegister.RejectBy
    public DateTime? FinishAt { get; set; }                    // Req_MemberRegister.cm_RegisFinishDTimeUTC — thời gian hoàn tất đăng ký
    public string? FinishBy { get; set; }                      // Req_MemberRegister.FinishBy — người hoàn tất đăng ký
    public int? MemberId { get; set; }                         // hội viên được tạo khi hoàn tất (FINISH)

    public Member? Member { get; set; }
}

/// <summary>
/// Liên kết Đại lý ↔ Hội viên (Map_QueryDealer_Member): ghi nhận đại lý nào đã đăng ký/tra cứu hội viên nào.
/// Khi hoàn tất đăng ký hội viên (Crd_Member_FinishX), hệ thống ghi 1 dòng map DLCPCode (đại lý đăng ký) ↔ MemberNo
/// kèm ngày tra cứu (QueryDate) và trạng thái hiệu lực (FlagActive). Dùng để biết hội viên thuộc đại lý nào
/// và phục vụ tra cứu "đại lý nào đã truy vấn hội viên nào".
/// </summary>
public class DealerMemberLink : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string DLCPCode { get; set; } = "";        // Map_QueryDealer_Member.DLCPCode — mã đại lý
    public int MemberId { get; set; }                  // Map_QueryDealer_Member.MemberNo — hội viên được liên kết
    public int NetworkID { get; set; }                 // Map_QueryDealer_Member.NetworkID — mã mạng/đại lý
    public DateTime QueryDate { get; set; } = DateTime.Now;   // Map_QueryDealer_Member.QueryDate — ngày tra cứu/liên kết
    public string? Remark { get; set; }                // Map_QueryDealer_Member.Remark — ghi chú
    public bool FlagActive { get; set; } = true;       // Map_QueryDealer_Member.FlagActive — còn hiệu lực
    public DateTime CreatedAt { get; set; } = DateTime.Now;   // Map_QueryDealer_Member.LogLUDTimeUTC
    public string? CreatedBy { get; set; }             // Map_QueryDealer_Member.LogLUBy

    public Member Member { get; set; } = null!;
}

/// <summary>
/// Lịch sử xét hạng (Crd_CardRank, DealPointType=LOYALTY): mỗi lần job xét hạng cuối kỳ xử lý 1 hội viên,
/// hệ thống ghi 1 bản ghi lưu lại hành động (UP/KEEP/DOWN) kèm ảnh chụp TRƯỚC/SAU của hội viên và thẻ
/// (Crd_MemberBefore/Crd_CardBefore/Crd_MemberAfter/Crd_CardAfter). Đây là audit trail của quá trình xét hạng,
/// KHÔNG thay đổi điểm — chỉ để tra cứu "vì sao hội viên lên/xuống hạng kỳ này".
/// </summary>
public class RankHistory : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string CardRankNo { get; set; } = "";            // Crd_CardRank.CardRankNo — số bản ghi xét hạng
    public int MemberId { get; set; }                       // hội viên được xét (suy từ Crd_CardBefore/After)
    public string? RankPolicyCode { get; set; }             // Crd_CardRank.RankPolicyCode — mã chính sách xét hạng
    public RankActionType Action { get; set; }              // Crd_CardRank.FunctionActionType — UP/KEEP/DOWN
    public string CardSourceCode { get; set; } = "";        // Crd_CardRank.CardSourceCode — nguồn thẻ (UP/KEEP/DOWN)
    public string DealPointType { get; set; } = "LOYALTY";  // Crd_CardRank.DealPointType — luôn LOYALTY cho xét hạng
    public string FunctionName { get; set; } = "";          // Crd_CardRank.FunctionName — hàm xử lý (audit)
    public string? FunctionRemark { get; set; }             // Crd_CardRank.FunctionRemark — ghi chú xử lý

    // Ảnh chụp TRƯỚC khi xét (Crd_MemberBefore/Crd_CardBefore).
    public int RankTierIdBefore { get; set; }               // hạng thẻ trước khi xét
    public int PointCardRankBefore { get; set; }            // điểm xét hạng trong kỳ trước khi xét
    public int QtyVisitBefore { get; set; }                 // lượt dịch vụ trong kỳ trước khi xét
    // Ảnh chụp SAU khi xét (Crd_MemberAfter/Crd_CardAfter).
    public int RankTierIdAfter { get; set; }                // hạng thẻ sau khi xét
    public int PointCardRankAfter { get; set; }             // điểm xét hạng sau khi xét (đã reset)
    public int QtyVisitAfter { get; set; }                  // lượt dịch vụ sau khi xét (đã reset)
    public DateTime? EffDateStart { get; set; }             // Crd_Card.EffDateStart — đầu kỳ mới
    public DateTime? EffDateEnd { get; set; }               // Crd_Card.EffDateEnd — cuối kỳ mới

    public DateTime CreatedAt { get; set; } = DateTime.Now; // Crd_CardRank.CreateDTimeUTC
    public string? CreatedBy { get; set; }                  // Crd_CardRank.CreateBy

    public Member Member { get; set; } = null!;
    public RankTier? RankTierBefore { get; set; }
    public RankTier? RankTierAfter { get; set; }
}

/// <summary>
/// Chương trình tặng điểm xe mới (Prm_CarNew): HTV/đại lý cấu hình chương trình tặng điểm cho hội viên
/// mua xe mới theo từng đại lý (DLCPCode) và dòng xe (ModelCode). Đi qua luồng duyệt
/// PENDING (tạo) → APPROVE (duyệt) → FINISH (hoàn tất, chương trình có hiệu lực), hoặc CANCEL khi huỷ.
/// Khi hoàn tất, chương trình áp dụng cho khoảng thời gian [EffDateStart, EffDateEnd]; nếu có chương trình
/// trước đó đang hiệu lực cùng đại lý thì bị cắt hiệu lực (EffDateEnd = ngày trước ngày bắt đầu chương trình mới).
/// </summary>
public class PrmCarNew : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string PRMCNCodeSys { get; set; } = "";        // Prm_CarNew.PRMCNCodeSys — mã hệ thống chương trình
    public string PRMCNCode { get; set; } = "";           // Prm_CarNew.PRMCNCode — mã chương trình
    public string PRMCNName { get; set; } = "";           // Prm_CarNew.PRMCNName — tên chương trình
    public string DLCPCode { get; set; } = "";            // Prm_CarNew.DLCPCode — đại lý áp dụng
    public DateTime EffDateStart { get; set; }             // Prm_CarNew.EffDateStart — ngày bắt đầu hiệu lực
    public DateTime EffDateEnd { get; set; }               // Prm_CarNew.EffDateEnd — ngày kết thúc hiệu lực
    public bool FlagAllModel { get; set; } = true;         // Prm_CarNew.FlagAllModel — áp dụng cho tất cả dòng xe
    public int PointValAllModel { get; set; }              // Prm_CarNew.PointValAllModel — điểm tặng khi áp dụng tất cả dòng xe
    public PrmCarNewStatus Status { get; set; } = PrmCarNewStatus.Pending;   // Prm_CarNew.PRMCNStatus
    public string? Remark { get; set; }                    // Prm_CarNew.Remark — ghi chú
    public DateTime CreatedAt { get; set; } = DateTime.Now;   // Prm_CarNew.CreateDTimeUTC
    public string? CreatedBy { get; set; }                 // Prm_CarNew.CreateBy
    public DateTime? ApproveAt { get; set; }               // Prm_CarNew.ApprDTimeUTC
    public string? ApproveBy { get; set; }                 // Prm_CarNew.ApprBy
    public DateTime? FinishAt { get; set; }                // Prm_CarNew.FinishDTimeUTC
    public string? FinishBy { get; set; }                  // Prm_CarNew.FinishBy
    public DateTime? CancelAt { get; set; }                // Prm_CarNew.CancelDTimeUTC
    public string? CancelBy { get; set; }                  // Prm_CarNew.CancelBy

    public List<PrmCarNewSpec> Specs { get; set; } = [];   // Prm_CarNewSpec — danh sách dòng xe + điểm (khi FlagAllModel = false)
}

/// <summary>
/// Dòng xe áp dụng chương trình tặng điểm xe mới (Prm_CarNewSpec): khi chương trình KHÔNG áp dụng cho tất cả
/// dòng xe (FlagAllModel = false), mỗi dòng là 1 dòng xe (ModelCode) kèm số điểm tặng (PointVal).
/// </summary>
public class PrmCarNewSpec : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public int PrmCarNewId { get; set; }                   // Prm_CarNewSpec.PRMCNCodeSys (FK tới chương trình)
    public int Idx { get; set; }                           // Prm_CarNewSpec.Idx — thứ tự dòng
    public string ModelCode { get; set; } = "";            // Prm_CarNewSpec.ModelCode — mã dòng xe
    public int PointVal { get; set; }                      // Prm_CarNewSpec.PointVal — điểm tặng cho dòng xe này
    public PrmCarNew PrmCarNew { get; set; } = null!;
}

/// <summary>
/// Chính sách tặng điểm khuyến mại bán hàng Creta (WA_Crd_MemberRegis_CalcPointBuyCreta, Card.cs):
/// HTV tặng điểm khuyến mại bán hàng (Crd_Member.PointBuyCreta) khi khách mua xe Creta thuộc chương trình.
/// Điều kiện (theo hệ nguồn): dòng xe (ModelCode) + hạng thẻ sử dụng (CardTypeUse) khớp, ngày giao xe
/// (DeliveryDate) nằm trong khoảng [EffDateStart, EffDateEnd], CCCD/MST chủ thẻ trùng với giao dịch mua xe,
/// và giao dịch giao xe chưa từng được áp dụng chương trình (chưa có hội viên nào cùng DealNo có PointBuyCreta &gt; 0).
/// </summary>
public class CretaBuyCarPolicy : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string PolicyCode { get; set; } = "CRETA";      // mã chính sách
    public string ModelCode { get; set; } = "SU2ID-CKD";   // dòng xe áp dụng (Creta)
    public string CardTypeUse { get; set; } = "P";        // hạng thẻ sử dụng phải khớp
    public int PointBuyCreta { get; set; } = 3_000_000;    // điểm khuyến mại tặng khi đủ điều kiện
    public DateTime EffDateStart { get; set; }             // ngày giao xe sớm nhất được áp dụng
    public DateTime EffDateEnd { get; set; }               // ngày giao xe muộn nhất được áp dụng
    public bool IsActive { get; set; } = true;             // FlagActive
}
