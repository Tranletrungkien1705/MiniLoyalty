using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MiniLoyalty.Models.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — bảng Crd_CardBefore (#008).
//
// Nguồn tra cứu (chỉ đọc):
//   - Model cột: 12.Dev.Common/idn.Loyalty.Common/Models/Loyalty/Crd_CardBefore.cs:9-56
//   - DDL thật:  11.BackEnd/V10/05.Refs.Biz/Migrate/20221026.z11.UpdDB.51.CardRank.Loyalty.sql:150-201
//                PK (CardRankNo, CardNo) tại :199-200 (XPKCrd_CardBefore).
// ===========================================================================================

/// <summary>
/// Bảng thông tin thẻ trước khi xét hạng Crd_CardBefore — port 1:1 DDL 20221026.z11.UpdDB.51.CardRank.Loyalty.sql:150-201.
/// PK = (CardRankNo, CardNo).
/// </summary>
public class Crd_CardBefore
{
    public string CardRankNo { get; set; } = "";             // Mã đợt xét hạng — Varchar50 NOT NULL (PK 1)
    public string CardNo { get; set; } = "";                 // Mã thẻ hội viên — Varchar50 NOT NULL (PK 2)
    public string NetworkID { get; set; } = "";              // NetworkID — Varchar50 NOT NULL
    public string? CardNoPrev { get; set; }                  // Mã thẻ kỳ trước — Varchar50
    public string MemberNo { get; set; } = "";               // Mã hội viên — Varchar50 NOT NULL
    public string CardSourceCode { get; set; } = "";         // Loại thẻ nguồn — Varchar50 NOT NULL
    public string? RankPolicyCode { get; set; }              // Mã chính sách — Varchar50
    public string? DLCodeExceptionally { get; set; }         // Đại lý đặc cách — Varchar50
    public double? PointBonus { get; set; }                  // Điểm thưởng — MyValue
    public double? PointPurchaseAddCar { get; set; }         // Điểm thưởng mua xe — MyValue
    public DateTime? EffDateStart { get; set; }              // Ngày bắt đầu hiệu lực — MyDateTime
    public DateTime? EffDateEnd { get; set; }                // Ngày kết thúc hiệu lực — MyDateTime
    public double? PointTotal { get; set; }                  // Điểm tổng — MyValue
    public double? PointBlock { get; set; }                  // Điểm đóng băng — MyValue
    public double? PointAvail { get; set; }                  // Điểm khả dụng — MyValue
    public double? AmountTotal { get; set; }                 // Doanh thu tổng — MyValue
    public double? AmountBlock { get; set; }                 // Doanh thu đóng băng — MyValue
    public double? AmountAvail { get; set; }                 // Doanh thu khả dụng — MyValue
    public double? QtyVisitTotal { get; set; }               // Lượt ghé thăm tổng — MyValue
    public double? QtyVisitBlock { get; set; }               // Lượt ghé thăm đóng băng — MyValue
    public double? QtyVisitAvail { get; set; }               // Lượt dịch vụ khả dụng — MyValue
    public double? PointCardRank { get; set; }               // Điểm xét hạng — MyValue
    public double? TotalAmountPeriod { get; set; }           // Tổng tiền trong kỳ — MyValue
    public string CardTypeUse { get; set; } = "";            // Hạng sử dụng — Varchar50 NOT NULL
    public string CardTypeInit { get; set; } = "";           // Hạng thực — Varchar50 NOT NULL
    public string? CardTypeUsePrev { get; set; }             // Hạng sử dụng kỳ trước — Varchar50
    public string? CardTypeInitPrev { get; set; }            // Hạng thực kỳ trước — Varchar50
    public DateTime? CardActiveDate { get; set; }            // Ngày kích hoạt thẻ — MyDateTime
    public string? FlagExceptionally { get; set; }           // Cờ đặc cách — MyStatus
    public string? CardTypeUseExpect { get; set; }           // Hạng sử dụng dự kiến — Varchar50
    public string? CardTypeInitExpect { get; set; }          // Hạng thực dự kiến — Varchar50
    public DateTime? CreateDTimeUTC { get; set; }            // Thời điểm tạo — MyDateTime NOT NULL
    public string? CreateBy { get; set; }                    // Người tạo — Varchar50
    public DateTime? LUDTimeUTC { get; set; }                // Thời điểm cập nhật cuối — MyDateTime NOT NULL
    public string LUBy { get; set; } = "";                   // Người cập nhật cuối — Varchar50 NOT NULL
    public DateTime? ApproveDTimeUTC { get; set; }           // Thời điểm phê duyệt — MyDateTime
    public string? ApproveBy { get; set; }                   // Người phê duyệt — Varchar50
    public DateTime? CancelDTimeUTC { get; set; }            // Thời điểm hủy — MyDateTime
    public string? CancelBy { get; set; }                    // Người hủy — Varchar50
    public string? CardStatus { get; set; }                  // Trạng thái thẻ — MyStatus
    public string? Remark { get; set; }                      // Ghi chú — Varchar400
    public DateTime? LogLUDTimeUTC { get; set; }             // MyDateTime
    public string? LogLUBy { get; set; }                     // Varchar50
}

public class Crd_CardBeforeConfig : IEntityTypeConfiguration<Crd_CardBefore>
{
    public void Configure(EntityTypeBuilder<Crd_CardBefore> b)
    {
        b.ToTable("Crd_CardBefore");
        b.HasKey(x => new { x.CardRankNo, x.CardNo });
    }
}
