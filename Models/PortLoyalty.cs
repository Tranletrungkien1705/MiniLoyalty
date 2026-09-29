namespace MiniLoyalty.Models;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty (hệ Loyalty idocNet) — 4 màn lõi.
//
// Nguồn tra cứu (chỉ đọc):
//   - Entity/cột:  2022.7.Loyalty/12.Dev.Common/idn.Loyalty.Common/Models/Loyalty/Crd_*.cs
//   - DDL thật:    2022.7.Loyalty/11.BackEnd/V10/05.Refs.Biz/Migrate/Script.HuongTTT/
//                     20221108.z11.Crd_Member.Crd_Card.sql:34   (Crd_Card)
//                     20221108.z11.Crd_Member.Crd_Card.sql:89   (Crd_Member)
//                     20221108.z11.Crd_Member.Crd_Card.sql:215  (Crd_CardTransaction)
//                     20260817.CreTb.Crd_MemberDiscountTransaction.sql:13 (Crd_MemberDiscountTransaction)
//   - SQL select:  idn.Loyalty.Biz/Card.cs:11352            (Crd_Member_GetX)
//                  idn.Loyalty.Biz/Master.Loyalty.cs:19484  (Crd_Card_GetX_New20251229 — bản LIVE)
//                  idn.Loyalty.Biz/Master.Loyalty.cs:21441  (Crd_CardTransaction_GetX)
//                  idn.Loyalty.Biz/Transaction.cs:5424      (Crd_MemberDiscountTransaction_GetX)
//   - Taxonomy:    12.Dev.Common/idn.Loyalty.Constants/Const.Main.BE.cs:1592 (CardType I/N/S/G/P)
//                                                             :1528 (DealPointType)
//                                                             :1519 (CardSourceCode)
//                                                             :1560 (MemberStatus) :1550 (RegisStatus)
//
// QUY ƯỚC KIỂU (ánh xạ user-defined type của SQL Server nguồn):
//   Varchar50/100/200/400 -> string?      | MyDateTime -> DateTime?
//   MyValue / float       -> double?      (CỐ Ý: không dùng decimal vì EF+SQLite map decimal -> TEXT,
//                                          làm SUM/so sánh sai; nguồn cũng khai double ở
//                                          Crd_CardTransaction.cs:91-93 cho 3 cột chiết khấu)
//   MyStatus              -> string?      (cờ '1'/'0'/'-1' HOẶC mã trạng thái 'APPROVE'/'PENDING'/'CANCEL')
//
// CỐ Ý ĐƠN GIẢN HOÁ (khai báo, không im lặng): 4 bảng này KHÔNG implement IOrgOwned (Guid OrgId)
//   như Member/Reward của app đích, vì nguồn đã có sẵn cặp cột phân vùng riêng NetworkID + OrgID
//   (Crd_Member.cs:12-13) và thêm một cột `OrgId` kiểu Guid sẽ chỉ khác `OrgID` ở chữ hoa/thường
//   -> dễ đụng tên cột. Phân vùng dữ liệu cho 4 bảng ported dùng chính NetworkID/OrgID của nguồn.
// ===========================================================================================

/// <summary>
/// Hội viên — port 1:1 bảng <c>Crd_Member</c>.
/// Nguồn cột: Models/Loyalty/Crd_Member.cs:11-120 · DDL: 20221108.z11.Crd_Member.Crd_Card.sql:89-166.
/// PK = MemberNo (DDL:169 XPKCrd_Member).
/// </summary>
public class CrdMember
{
    public string MemberNo { get; set; } = "";       // Mã hội viên — Crd_Member.cs:11 (PK)
    public string? NetworkID { get; set; }           // NetworkID — :12
    public string? OrgID { get; set; }               // OrgID — :13
    public string? DLCodeRegis { get; set; }         // Đại lý đăng ký hội viên — :14
    public string? DLCodeExceptionally { get; set; } // Đại lý đặc cách — :15
    public DateTime? RegistrationDate { get; set; }  // Ngày đăng ký hội viên — :16
    public string? CustomerCode { get; set; }        // Mã khách hàng — :17
    public string? ProvinceCode { get; set; }        // Mã Tỉnh/TP — :18
    public string? DistrictCode { get; set; }        // Mã Quận/huyện — :19
    public string? UserCode_Member { get; set; }     // :20
    public string? MemberName { get; set; }          // Tên hội viên/Tên khách hàng — :21
    public string? MemberGender { get; set; }        // Giới tính — :22
    public string? IDCardNo { get; set; }            // CMT/CCCD — :23
    public string? PhoneNo { get; set; }             // Số điện thoại — :24
    public DateTime? DateOfBirth { get; set; }       // Ngày sinh — :25
    public string? MemAddress { get; set; }          // Địa chỉ — :26
    public string? CarNo { get; set; }               // Biển số xe — :27
    public string? VIN { get; set; }                 // Số khung — :28
    public string? ModelCode { get; set; }           // Mã model — :29
    public string? ModelName { get; set; }           // Tên model — :30
    public string? TradeMarkCode { get; set; }       // Hiệu xe — :31
    public DateTime? EffDateStart { get; set; }      // Ngày hiệu lực từ — :32
    public DateTime? EffDateEnd { get; set; }        // Ngày hiệu lực đến — :33
    public double? PointTotal { get; set; }          // Điểm giao dịch trong kỳ — :34
    public double? PointBlock { get; set; }          // :35
    public double? PointAvail { get; set; }          // :36
    public double? AmountTotal { get; set; }         // Tiền giao dịch trong kỳ — :37
    public double? AmountBlock { get; set; }         // :38
    public double? AmountAvail { get; set; }         // Doanh thu xét hạng trong kỳ — :39
    public double? QtyVisitTotal { get; set; }       // Số lần ghé thăm trong kỳ — :40
    public double? QtyVisitBlock { get; set; }       // :41
    public double? QtyVisitAvail { get; set; }       // Lượt dịch vụ — :42
    public double? PointBegin { get; set; }          // Điểm đầu kỳ — :43
    public double? PointEnd { get; set; }            // Điểm cuối kỳ (điểm tích lũy tiêu dùng) — :44
    public double? AmountBegin { get; set; }         // Giá trị tiền đầu kỳ — :45
    public double? AmountEnd { get; set; }           // Giá trị tiền cuối kỳ — :46
    public double? QtyVisitBegin { get; set; }       // Số lần ghé thăm đầu kỳ — :47
    public double? QtyVisitEnd { get; set; }         // Số lần ghé thăm cuối kỳ — :48
    public double? PointCardRank { get; set; }       // Điểm xét hạng trong kỳ — :49
    public string? CardTypeUse { get; set; }         // Hạng thẻ sử dụng (I/N/S/G/P) — :50
    public string? CardTypeInit { get; set; }        // Hạng thẻ thực (I/N/S/G/P) — :51
    public DateTime? MemberActiveDate { get; set; }  // Ngày kích hoạt hội viên — :52
    public string? FlagExceptionally { get; set; }   // Cờ đặc cách — :53
    public string? MemberNoIntro { get; set; }       // Mã hội viên giới thiệu — :54
    public string? CarNoIntro { get; set; }          // Biển số hội viên giới thiệu — :55
    public double? PointIntro { get; set; }          // Tặng điểm giới thiệu mua xe — :56
    public double? PointBuyCar { get; set; }         // Tặng điểm mua xe mới — :57
    public double? PointBuyCreta { get; set; }       // Điểm khuyến mại bán hàng (chỉ điểm tiêu dùng) — :58
    public double? PointVoucherNewCar { get; set; }  // Điểm Voucher xe mới (hạng sử dụng) — :59
    public double? PointVoucher { get; set; }        // Tổng điểm Voucher còn lại (hạng sử dụng) — :60
    public double? PointActualVoucher { get; set; }  // Tổng điểm Voucher còn lại (hạng thực) — :61
    public double? PointActualVCNewCar { get; set; } // Giá trị voucher tặng mua xe mới (hạng thực) — :62
    public string? DealNo { get; set; }              // Số giao dịch bán lẻ — :63
    public string? TVBHName { get; set; }            // Tên TVBH — :64
    public DateTime? CreateDTimeUTC { get; set; }    // Thời điểm tạo đăng ký/hội viên — :65
    public string? CreateBy { get; set; }            // :66
    public DateTime? LUDTimeUTC { get; set; }        // :67
    public string? LUBy { get; set; }                // :68
    public DateTime? InactiveDTimeUTC { get; set; }  // Thời điểm Inactive hội viên — :69
    public string? InactiveBy { get; set; }          // :70
    public DateTime? RegisAppr1DTimeUTC { get; set; }// Thời điểm duyệt 1 — :71
    public string? RegisAppr1By { get; set; }        // :72
    public DateTime? RegisAppr2DTimeUTC { get; set; }// Thời điểm duyệt 2 — :73
    public string? RegisAppr2By { get; set; }        // :74
    public DateTime? RegisFinishDTimeUTC { get; set; } // Thời điểm kết thúc đăng ký — :75
    public string? RegisFinishBy { get; set; }       // :76
    public DateTime? RegisCancelDTimeUTC { get; set; } // Thời điểm huỷ đăng ký — :77
    public string? RegisCancelBy { get; set; }       // :78
    public string? MemberStatus { get; set; }        // PENDING/APPROVE/CANCEL — :79-82 · Const.Main.BE.cs:1560
    public string? RegisStatus { get; set; }         // PENDING/APPROVE1/APPROVE2/FINISH/CANCEL — :83-88 · Const.Main.BE.cs:1550
    public string? Remark { get; set; }              // Ghi chú — :89
    public DateTime? LogLUDTimeUTC { get; set; }     // :90
    public string? LogLUBy { get; set; }             // :91
    public string? MemberFilePath { get; set; }      // Đường dẫn file upload — :106
    public string? MemberFileName { get; set; }      // Tên file — :107
    public string? SourceRegisType { get; set; }     // Nguồn mở thẻ ON/OFF — :114 · Const.Main.BE.cs:1573
    public string? ReqMemberRegisterCodeSys { get; set; } // Số yêu cầu mở thẻ online — :115
    public string? ReqMemberRegisterCodeOS { get; set; }  // Số yêu cầu mở thẻ online (mã đối tác gửi) — :116
    public string? PurchaseType { get; set; }        // BUYNEW/BUYADD/BUYUSED/EXCHANGE — :117 · Const.Main.BE.cs:1602
    public DateTime? WarrantyExpiryDate { get; set; }// Ngày hết hạn bảo hành — :119
    public DateTime? InvoiceDate { get; set; }       // Ngày hóa đơn (DLS_DealDetail.CusInvoiceDate) — :120

    // Cột CÓ trong DDL và được trả về bởi `select cmb.*` (Card.cs:11460) nhưng model Crd_Member.cs
    // không liệt kê riêng — vẫn port để không bỏ quên cột.
    public DateTime? ApproveDTimeUTC { get; set; }   // DDL:154
    public string? ApproveBy { get; set; }           // DDL:155
    public DateTime? CancelDTimeUTC { get; set; }    // DDL:156
    public string? CancelBy { get; set; }            // DDL:157
    public string? CardSourceCode { get; set; }      // DDL:162
    public string? CardStatus { get; set; }          // DDL:163
    public string? FlagExceptionCard { get; set; }   // DDL:164
    public double? AmountCardRank { get; set; }      // DDL:165
}

/// <summary>
/// Thẻ hội viên / kỳ xét hạng — port 1:1 bảng <c>Crd_Card</c>.
/// Nguồn cột: Models/Loyalty/Crd_Card.cs:11-119 · DDL: 20221108.z11.Crd_Member.Crd_Card.sql:34-76.
/// PK = CardNo (DDL:81 XPKCrd_Card).
/// </summary>
public class CrdCard
{
    public string CardNo { get; set; } = "";         // Mã thẻ hội viên — Crd_Card.cs:11 (PK)
    public string? CardNoPrev { get; set; }          // Mã thẻ kỳ trước — :32
    public string? NetworkID { get; set; }           // :12
    public string MemberNo { get; set; } = "";       // Mã hội viên (FK Crd_Member) — :13 · DDL:196 FK_CrdMember_CrdCard
    public string? CardSourceCode { get; set; }      // DOWN/KEEP/NEW/RENEW/UP — :15 · Const.Main.BE.cs:1519
    public string? RankPolicyCode { get; set; }      // Mã chính sách xếp hạng — :16
    public string? DLCodeExceptionally { get; set; } // Đại lý đặc cách — :14
    public double? PointBonus { get; set; }          // Điểm thưởng — :17
    public double? PointPurchaseAddCar { get; set; } // Điểm thưởng mua thêm xe — :18
    public DateTime? EffDateStart { get; set; }      // Ngày bắt đầu kỳ xét hạng — :19
    public DateTime? EffDateEnd { get; set; }        // Ngày hết hạn kỳ xét hạng — :20
    public double? PointTotal { get; set; }          // Điểm tiêu dùng trong kỳ — :21
    public double? PointBlock { get; set; }          // :22
    public double? PointAvail { get; set; }          // Điểm tiêu dùng còn lại — :23
    public double? AmountTotal { get; set; }         // Giá trị tích lũy trong kỳ — :24
    public double? AmountBlock { get; set; }         // :25
    public double? AmountAvail { get; set; }         // Doanh thu xét hạng trong kỳ — :26
    public double? QtyVisitTotal { get; set; }       // Số lượt dịch vụ — :27
    public double? QtyVisitBlock { get; set; }       // :28
    public double? QtyVisitAvail { get; set; }       // :29
    public double? PointCardRank { get; set; }       // Điểm xét hạng — :30
    public double? TotalAmountPeriod { get; set; }   // :31
    public string? CardTypeUse { get; set; }         // Hạng sử dụng — :33
    public string? CardTypeInit { get; set; }        // Hạng thực — :34
    public string? CardTypeUsePrev { get; set; }     // Hạng sử dụng kỳ trước — :35
    public string? CardTypeInitPrev { get; set; }    // Hạng thực kỳ trước — :36
    public DateTime? CardActiveDate { get; set; }    // Ngày kích hoạt thẻ — :37
    public string? FlagExceptionally { get; set; }   // Cờ đặc cách — :38
    // KHÁC NGUỒN (khai báo, không im lặng): ở nguồn `RankActionType` (Crd_Card.cs:52) là cột TRANSIENT —
    // job xét hạng sinh ra bằng alias trong SQL (vd `, 'DOWN' RankActionType`, 20260902.Real.Migrate.RankDownSToI.sql:355),
    // KHÔNG có trong DDL Crd_Card. Ở đích lưu thành cột thật để màn thẻ hiện được "hành động hạng" mà không cần
    // job; giá trị hợp lệ theo Const.Main.BE.cs:1508-1517 (CALC/UP/DOWN/NORMAL/RENEW/KEEP/NEW).
    public string? RankActionType { get; set; }
    public string? CardTypeUseExpect { get; set; }   // Hạng sử dụng dự kiến — :53 · DDL:62
    public string? CardTypeInitExpect { get; set; }  // Hạng thực dự kiến — :54 · DDL:63
    public DateTime? CreateDTimeUTC { get; set; }    // Thời điểm tạo thẻ — :39
    public string? CreateBy { get; set; }            // :40
    public DateTime? LUDTimeUTC { get; set; }        // :41
    public string? LUBy { get; set; }                // :42
    public DateTime? ApproveDTimeUTC { get; set; }   // DDL:68 — trả về ở Crd_Card_GetX_New20251229 (Master.Loyalty.cs:19722)
    public string? ApproveBy { get; set; }           // DDL:69
    public DateTime? CancelDTimeUTC { get; set; }    // DDL:70
    public string? CancelBy { get; set; }            // DDL:71
    public string? CardStatus { get; set; }          // PENDING/APPROVE/CANCEL — :43-46 · Const.Main.BE.cs:1580
    public string? Remark { get; set; }              // :47
    public DateTime? LogLUDTimeUTC { get; set; }     // :48
    public string? LogLUBy { get; set; }             // :49
}

/// <summary>
/// Giao dịch điểm — port 1:1 bảng <c>Crd_CardTransaction</c>.
/// Nguồn cột: Models/Loyalty/Crd_CardTransaction.cs:11-93 · DDL: 20221108.z11.Crd_Member.Crd_Card.sql:215-280.
/// PK = AutoId IDENTITY (DDL:284).
/// </summary>
public class CrdCardTransaction
{
    public long AutoId { get; set; }                 // Số giao dịch (identity) — Crd_CardTransaction.cs:11
    public string? RefNo { get; set; }               // Số giao dịch tham chiếu — :12
    public string? ProjectCode { get; set; }         // :13
    public string? NetworkID { get; set; }           // :14
    public string? OrgID { get; set; }               // :15
    public string? DLCode { get; set; }              // Đại lý thực hiện — :16
    public string MemberNo { get; set; } = "";       // Mã hội viên — :17
    public string CardNo { get; set; } = "";         // Mã kỳ xét hạng — :18
    public string? CardSourceCode { get; set; }      // :19
    public string? CardTypeUse { get; set; }         // Hạng sử dụng — :20
    public string? CardTypeInit { get; set; }        // Hạng thực — :21
    public string? DealPointType { get; set; }       // Loại điểm — :22 · Const.Main.BE.cs:1528-1547
    public DateTime? UsePrmDTime { get; set; }       // :24
    public string? PrProgramCode { get; set; }       // Mã chương trình ưu đãi — :25
    public DateTime? EffDateStart { get; set; }      // :26
    public DateTime? EffDateEnd { get; set; }        // :27
    public double? PointChTotal { get; set; }        // :28
    public double? PointChBlock { get; set; }        // :29
    public double? AmountChTotal { get; set; }       // Số điểm hạng sử dụng — :30 (xem Master.Loyalty.cs:21748)
    public double? AmountChBlock { get; set; }       // :31
    public double? QtyVisitChTotal { get; set; }     // :32
    public double? QtyVisitChBlock { get; set; }     // :33
    public double? QtyPrChTotal { get; set; }        // Số lượng sử dụng ưu đãi — :34
    public double? QtyPrChBlock { get; set; }        // :35
    public double? QtyPrUsed { get; set; }           // :36
    public double? PointRef { get; set; }            // :37
    public double? AmountRef { get; set; }           // :38
    public double? PolicyPointUnit { get; set; }     // :39
    public double? PolicyAmountUnit { get; set; }    // :40
    public string? PolicyCode { get; set; }          // :41
    public double? ParamPointUnit { get; set; }      // :42
    public double? ParamAmountUnit { get; set; }     // :43
    public string? ParamCode { get; set; }           // :44
    public string? FlagViewUI { get; set; }          // :45
    public string? FlagPoint { get; set; }           // :46
    public string? FlagAmount { get; set; }          // :47
    public string? FlagPr { get; set; }              // :48
    public string? FunctionName { get; set; }        // :49
    public string? FunctionActionType { get; set; }  // :50
    public string? FunctionRemark { get; set; }      // :51
    public string? RefType { get; set; }             // :52
    public double? PointChRankTotal { get; set; }    // Điểm tích xét hạng — :79
    public string? RefCode00 { get; set; }           // :53
    public string? RefCode01 { get; set; }           // :54
    public string? RefCode02 { get; set; }           // :55
    public string? RefCode03 { get; set; }           // :56
    public string? RefCode04 { get; set; }           // :57
    public string? RefCode05 { get; set; }           // :58
    public string? RefCode06 { get; set; }           // :59
    public string? RefCode07 { get; set; }           // :60
    public string? RefCode08 { get; set; }           // :61
    public string? RefCode09 { get; set; }           // :62
    public DateTime? CreateDTimeUTC { get; set; }    // Ngày thực hiện — :63
    public string? CreateBy { get; set; }            // :64
    public DateTime? LUDTimeUTC { get; set; }        // :65
    public string? LUBy { get; set; }                // :66
    public DateTime? LogLUDTimeUTC { get; set; }     // :67
    public string? LogLUBy { get; set; }             // :68
    public DateTime? PointExpiryDTime { get; set; }  // Hạn sử dụng điểm — :80
    public DateTime? PointExpiryDTimeSync { get; set; } // DDL:279 — trả về ở Master.Loyalty.cs:21621
    public double? PointChTotalInit { get; set; }    // Điểm tiêu dùng của CardTypeInit — :81
    public double? AmountChTotalInit { get; set; }   // Giá trị tiêu dùng của CardTypeInit — :82
    public double? PointChTotalDiffer { get; set; }  // Số điểm chênh — :83
    public double? AmountChTotalDiffer { get; set; } // Giá trị chênh — :84
    public double? AmountAvailPrevious { get; set; } // Giá trị tiêu dùng còn lại trước khi tích — :85
    public string? DLCodeExceptionally { get; set; } // Đại lý đặc cách — :87 (nguồn lấy từ Crd_Card, Master.Loyalty.cs:21632)
}

/// <summary>
/// Giao dịch chiết khấu dịch vụ (DISCOUNTRO) — port 1:1 bảng <c>Crd_MemberDiscountTransaction</c>.
/// Nguồn cột: Models/Loyalty/Crd_MemberDiscountTransaction.cs:11-42
/// DDL: Script.HuongTTT/20260817.CreTb.Crd_MemberDiscountTransaction.sql:13-46. PK = AutoId IDENTITY (:51).
/// Bảng RIÊNG (không nằm trong Crd_CardTransaction) — xem ghi chú nguồn Master.Loyalty.cs:21735.
/// </summary>
public class CrdMemberDiscountTransaction
{
    public long AutoId { get; set; }                 // Số giao dịch (identity) — :11 · DDL:14
    public string? RefNo { get; set; }               // Số tham chiếu (số RO) — :12
    public string? NetworkID { get; set; }           // :13
    public string? OrgID { get; set; }               // :14
    public string? DLCode { get; set; }              // Đại lý giao dịch — :15
    public string MemberNo { get; set; } = "";       // Mã hội viên — :16
    public string CardNo { get; set; } = "";         // Mã kỳ xét hạng — :17
    public string? CardTypeUse { get; set; }         // Hạng sử dụng — :18
    public string? CardTypeInit { get; set; }        // Hạng thực — :19
    public string? CardTypeApply { get; set; }       // Hạng áp dụng — :20
    public string? DealPointType { get; set; }       // Loại giao dịch (= DISCOUNTRO) — :21
    public string? PolicyCode { get; set; }          // Mã chính sách — :22
    public double? PolicyDiscountRate { get; set; }  // % chiết khấu theo chính sách — :23
    public string? DLCodeExceptionally { get; set; } // Mã đại lý đặc cách — :24
    public string? FlagExceptionally { get; set; }   // Cờ đặc cách — :25
    public DateTime? EffDateStart { get; set; }      // Ngày bắt đầu kỳ xét hạng — :26
    public DateTime? EffDateEnd { get; set; }        // Ngày kết thúc kỳ xét hạng — :27
    public double? PointChTotal { get; set; }        // Tổng tiền chiết khấu — :28
    public double? AmountForDC { get; set; }         // Doanh thu chiết khấu — :29
    public DateTime? CreateDate { get; set; }        // Ngày thực hiện — :30
    public string? RefCode00 { get; set; }           // :31
    public string? RefCode01 { get; set; }           // :32
    public string? RefCode02 { get; set; }           // :33
    public string? RefCode03 { get; set; }           // :34
    public string? RefCode04 { get; set; }           // :35
    public string? RefCode05 { get; set; }           // :36
    public DateTime? CreateDTimeUTC { get; set; }    // :37
    public string? CreateBy { get; set; }            // :38
    public DateTime? LUDTimeUTC { get; set; }        // :39
    public string? LUBy { get; set; }                // :40
    public DateTime? LogLUDTimeUTC { get; set; }     // :41
    public string? LogLUBy { get; set; }             // :42
}

/// <summary>
/// Taxonomy hằng số port 1:1 từ 12.Dev.Common/idn.Loyalty.Constants/Const.Main.BE.cs.
/// Dùng chuỗi đúng nguyên văn nguồn để guard không lệch (bẫy "message lệch condition").
/// </summary>
public static class LoyaltyConst
{
    /// <summary>Hạng thẻ — Const.Main.BE.cs:1592-1599. Thứ tự thấp→cao: I &lt; N &lt; S &lt; G &lt; P.</summary>
    public static class CardType
    {
        public const string I = "I";   // Hạng khởi tạo/thấp nhất
        public const string N = "N";
        public const string S = "S";
        public const string G = "G";
        public const string P = "P";   // Cao nhất
        /// <summary>Thứ tự xét hạng (chỉ số càng lớn càng cao). Dùng để so hạng, KHÔNG so chuỗi.</summary>
        public static readonly string[] Order = [I, N, S, G, P];
        public static int Rank(string? t) => Array.IndexOf(Order, (t ?? "").ToUpperInvariant());
    }

    /// <summary>Loại kỳ thẻ — Const.Main.BE.cs:1519-1526.</summary>
    public static class CardSourceCode
    {
        public const string Down = "DOWN"; public const string Keep = "KEEP";
        public const string New = "NEW"; public const string ReNew = "RENEW"; public const string Up = "UP";
    }

    /// <summary>Loại điểm/giao dịch — Const.Main.BE.cs:1528-1547.</summary>
    public static class DealPointType
    {
        public const string Sales = "SALES";                    // Mua xe mới
        public const string Loyalty = "LOYALTY";
        public const string Introduction = "INTRODUCTION";      // Giới thiệu mua xe
        public const string TransferBenefits = "TRANSFERBENIFITS"; // (sai chính tả ở NGUỒN — giữ nguyên văn)
        public const string BirthDay = "BIRTHDAY";              // Sinh nhật
        public const string PrProgram = "PRPROGRAM";            // Ghi nhận sử dụng ưu đãi
        public const string PointIncrease = "POINTINCREASE";    // Tích điểm xét hạng
        public const string PointUse = "POINTUSE";              // Sử dụng điểm
        public const string Consumption = "CONSUMPTION";        // Tích điểm tiêu dùng (= tích điểm dịch vụ)
        public const string ServiceTurn = "SERVICETURN";        // Lượt dịch vụ
        public const string PointExpiry = "POINTEXPIRY";        // Điểm hết hạn
        public const string Support = "SUPPORT";                // Hỗ trợ
        public const string KMBH = "KMBH";                      // Điểm khuyến mại bán xe
        public const string VoucherXM = "VOUCHERXM";            // Tặng điểm voucher xe mới
        public const string VoucherSD = "VOUCHERSD";            // Sử dụng điểm voucher
        public const string DiscountRO = "DISCOUNTRO";          // Chiết khấu dịch vụ
        public const string VoucherTSN = "VOUCHERTSN";          // Voucher tặng điểm sinh nhật
    }

    /// <summary>Trạng thái hội viên — Const.Main.BE.cs:1560-1565.</summary>
    public static class MemberStatus
    { public const string Pending = "PENDING"; public const string Approve = "APPROVE"; public const string Cancel = "CANCEL"; }

    /// <summary>Trạng thái đăng ký hội viên — Const.Main.BE.cs:1550-1557.</summary>
    public static class RegisStatus
    {
        public const string Pending = "PENDING"; public const string Approve1 = "APPROVE1";
        public const string Approve2 = "APPROVE2"; public const string Finish = "FINISH"; public const string Cancel = "CANCEL";
    }

    /// <summary>Trạng thái thẻ — Const.Main.BE.cs:1580-1585.</summary>
    public static class CardStatus
    { public const string Pending = "PENDING"; public const string Approve = "APPROVE"; public const string Cancel = "CANCEL"; }

    /// <summary>Loại thẻ theo hành vi mua — Const.Main.BE.cs:1602-1608.</summary>
    public static class PurchaseType
    {
        public const string BuyAdd = "BUYADD"; public const string BuyNew = "BUYNEW";
        public const string BuyUsed = "BUYUSED"; public const string Exchange = "EXCHANGE";
    }

    /// <summary>
    /// Cờ hiển thị trạng thái: port 1:1 biểu thức CASE của nguồn.
    /// Card.cs:11487-11493 (cm_FlagShow) và Master.Loyalty.cs:19738-19749 (cc_FlagShow).
    /// PENDING -> '-1' (None, tránh nhầm với Inactive), APPROVE -> '1', CANCEL -> '0'.
    /// </summary>
    public static string? FlagShow(string? status) => (status ?? "").ToUpperInvariant() switch
    {
        MemberStatus.Pending => "-1",
        MemberStatus.Approve => "1",
        MemberStatus.Cancel => "0",
        _ => null,     // nguồn dùng CASE không ELSE => trả NULL cho giá trị lạ
    };
}
