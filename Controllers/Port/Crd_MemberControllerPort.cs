using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniLoyalty.Data;
using MiniLoyalty.Models;
using MiniLoyalty.Models.Port;

namespace MiniLoyalty.Controllers.Port;

// ============================================================================================
// PORT 1:1 — Crd_MemberController/Search (#039).
//
// Nguồn tầng Gate:
//   Crd_MemberController.cs:30-140    Search -> Crd_MemberService.WA_Crd_Member_GetRegister
//   Crd_MemberController.cs:1494-1558 strWhereClause_Crd_Member (dựng mệnh đề lọc hội viên)
//   Crd_MemberController.cs:1560-1586 strWhereClause_Crd_MemberFlagShow (dựng mệnh đề lọc cm_FlagShow)
// Nguồn tầng API:
//   CrdMemberController.cs:97-166     WA_Crd_Member_GetRegister -> _biz.WAS_Crd_Member_GetRegister
// Nguồn tầng Biz:
//   Card.cs:19760-19858               WAS_Crd_Member_GetRegister
//   Card.cs:19860-20012               Crd_Member_GetRegister
//   Card.cs:20303-20600               Crd_Member_GetRegisterX_New20231019 (bản LIVE đang được gọi)
//
// GIỮ ĐÚNG NGUỒN:
//   * Endpoint POST: /api/crd_member/search
//   * Tham số nhận vào: OrgID, RegistrationDateFrom, RegistrationDateTo, MemberActiveDateFrom,
//     MemberActiveDateTo, DLCodeRegis, PhoneNo, MemberNo, RegisStatus, CarNo, MemberName,
//     VIN, cm_FlagShow, SourceRegisType, int Ft_PageIndex = 0, int Ft_PageSize = 10.
//     KHÔNG có KeyWord trong chữ ký gate (:30-31).
//   * OrgID có trong tham số gate nhưng strWhereClause_Crd_Member KHÔNG lọc OrgID (:1494-1558).
//   * Toán tử lọc: Toàn bộ trường chuỗi (kể cả MemberName) dùng toán tử "=", KHÔNG dùng LIKE,
//     KHÔNG có toán tử IN, KHÔNG tách dấu phẩy.
//   * cm_FlagShow: "1" -> MemberStatus = 'APPROVE', "0" -> MemberStatus = 'CANCEL',
//     "-1" -> MemberStatus = 'PENDING', giá trị khác bỏ qua (:1566-1581).
//   * Nối bảng: Crd_Member inner join Crd_Card cc theo cc.CardSourceCode = 'NEW' (Card.cs:20528-20530).
//   * Sắp xếp: cmb.MemberNo asc trong draft và t.MyIdxSeq asc ở select cuối (Card.cs:20369, 20546).
//   * cm_FlagShow ở kết quả chi tiết: case MemberStatus 'PENDING' -> '-1', 'APPROVE' -> '1', 'CANCEL' -> '0' (:20505-20511).
//   * MemberActiveDate ở kết quả chi tiết lấy từ cc.CardActiveDate (:20487).
//   * Phân trang: Chuẩn Bài học A.12 và A.7 (Ft_PageSize = 0 -> DataList [], PageCount = 0, ItemCount giữ nguyên).
//   * DTO: Crd_MemberSearchReq và Crd_MemberSearchItem đặt cùng file theo Bài học A.14, A.24.
// ============================================================================================

/// <summary>
/// Tham số tìm kiếm hội viên — port 1:1 từ tầng Gate Crd_MemberController.Search (Crd_MemberController.cs:30-31).
/// </summary>
public class Crd_MemberSearchReq
{
    public string? OrgID { get; set; }                    // Crd_MemberController.cs:30 (nhận nhưng không lọc trong strWhereClause)
    public string? RegistrationDateFrom { get; set; }     // :30
    public string? RegistrationDateTo { get; set; }       // :30
    public string? MemberActiveDateFrom { get; set; }     // :30
    public string? MemberActiveDateTo { get; set; }       // :30
    public string? DLCodeRegis { get; set; }              // :30
    public string? PhoneNo { get; set; }                  // :30
    public string? MemberNo { get; set; }                 // :30
    public string? RegisStatus { get; set; }              // :31
    public string? CarNo { get; set; }                    // :31
    public string? MemberName { get; set; }               // :31 (lọc toán tử =, không LIKE)
    public string? VIN { get; set; }                      // :31
    public string? cm_FlagShow { get; set; }              // :31 ("1" -> APPROVE, "0" -> CANCEL, "-1" -> PENDING)
    public string? SourceRegisType { get; set; }          // :31
    public string? MemberStatus { get; set; }             // strWhereClause_Crd_Member :1544
    public string? DLCPCode { get; set; }                 // strWhereClause_Crd_Member :1548
    public int Ft_PageIndex { get; set; } = 0;            // :31
    public int Ft_PageSize { get; set; } = 10;            // :31
}

/// <summary>
/// Tham số request lấy thông tin hội viên theo mã hội viên — Crd_MemberController.GetByMemberNo (:142).
/// </summary>
public class Crd_MemberGetByMemberNoReq
{
    public string? MemberNo { get; set; }
    public string? DLCPCode { get; set; }
}

/// <summary>
/// DTO phản hồi lấy thông tin hội viên — Data = { Crd_Member = ... } (Gate :201-204).
/// </summary>
public class Crd_MemberGetByMemberNoRes
{
    public Crd_MemberSearchItem? Crd_Member { get; set; }
}

/// <summary>
/// Tham số request lấy danh sách hội viên theo số điện thoại — Crd_MemberController.GetByPhoneNo (Gate :219).
/// </summary>
public class Crd_MemberGetByPhoneNoReq
{
    public string? PhoneNo { get; set; }
}

/// <summary>
/// Tham số request lấy thông tin đăng ký hội viên theo mã hội viên — Crd_MemberController.GetRegisterByMemberNo (Gate Crd_MemberController.cs:571).
/// </summary>
public class Crd_MemberGetRegisterByMemberNoReq
{
    public string? MemberNo { get; set; }
}


/// <summary>
/// Model từng dòng dữ liệu hội viên trả về ở DataList — port 1:1 từ model Crd_Member.cs (1-122) và select cuối (Card.cs:20400-20549).
/// </summary>
public class Crd_MemberSearchItem
{
    public long? MyIdxSeq { get; set; }                     // Thứ tự dòng (Card.cs:20403)
    public string? MemberNo { get; set; }                   // Mã hội viên — Crd_Member.cs:11 (PK)
    public string? NetworkID { get; set; }                  // NetworkID — :12
    public string? OrgID { get; set; }                      // OrgID — :13
    public string? DLCodeRegis { get; set; }                // Đại lý đăng ký hội viên — :14
    public string? DLCodeExceptionally { get; set; }        // Đại lý đặc cách — :15
    public DateTime? RegistrationDate { get; set; }         // Ngày đăng ký hội viên — :16
    public string? CustomerCode { get; set; }               // Mã khách hàng — :17
    public string? ProvinceCode { get; set; }               // Mã Tỉnh/TP — :18
    public string? DistrictCode { get; set; }               // Mã Quận/huyện — :19
    public string? UserCode_Member { get; set; }            // :20
    public string? MemberName { get; set; }                 // Tên hội viên/Tên khách hàng — :21
    public string? MemberGender { get; set; }               // Giới tính — :22
    public string? IDCardNo { get; set; }                   // CMT/CCCD — :23
    public string? PhoneNo { get; set; }                    // Số điện thoại — :24
    public DateTime? DateOfBirth { get; set; }              // Ngày sinh — :25
    public string? MemAddress { get; set; }                 // Địa chỉ — :26
    public string? CarNo { get; set; }                      // Biển số xe — :27
    public string? VIN { get; set; }                        // Số khung — :28
    public string? ModelCode { get; set; }                  // Mã model — :29
    public string? ModelName { get; set; }                  // Tên model — :30
    public string? TradeMarkCode { get; set; }              // Hiệu xe — :31
    public DateTime? EffDateStart { get; set; }             // Ngày hiệu lực từ — :32
    public DateTime? EffDateEnd { get; set; }               // Ngày hiệu lực đến — :33
    public double? PointTotal { get; set; }                 // Điểm giao dịch trong kỳ — :34
    public double? PointBlock { get; set; }                 // :35
    public double? PointAvail { get; set; }                 // :36
    public double? AmountTotal { get; set; }                // Tiền giao dịch trong kỳ — :37
    public double? AmountBlock { get; set; }                // :38
    public double? AmountAvail { get; set; }                // Doanh thu xét hạng trong kỳ — :39
    public double? QtyVisitTotal { get; set; }              // Số lần ghé thăm trong kỳ — :40
    public double? QtyVisitBlock { get; set; }              // :41
    public double? QtyVisitAvail { get; set; }              // Lượt dịch vụ — :42
    public double? PointBegin { get; set; }                 // Điểm đầu kỳ — :43
    public double? PointEnd { get; set; }                   // Điểm cuối kỳ - Điểm tích lũy tiêu dùng — :44
    public double? AmountBegin { get; set; }                // Giá trị tiền đầu kỳ — :45
    public double? AmountEnd { get; set; }                  // Giá trị tiền cuối kỳ — :46
    public double? QtyVisitBegin { get; set; }              // Số lần ghé thăm đầu kỳ — :47
    public double? QtyVisitEnd { get; set; }                // Số lần ghé thăm cuối kỳ — :48
    public double? PointCardRank { get; set; }              // Điểm xét hạng trong kỳ — :49
    public string? CardTypeUse { get; set; }                // Hạng thẻ sử dụng — :50
    public string? CardTypeInit { get; set; }               // Hạng thẻ thực — :51
    public DateTime? MemberActiveDate { get; set; }         // Ngày kích hoạt hội viên (lấy từ cc.CardActiveDate) — :52, Card.cs:20487
    public string? FlagExceptionally { get; set; }          // Cờ đặc cách — :53
    public string? MemberNoIntro { get; set; }              // Mã hội viên giới thiệu — :54
    public string? CarNoIntro { get; set; }                 // Biển số hội viên giới thiệu — :55
    public double? PointIntro { get; set; }                 // Tặng điểm giới thiệu mua xe — :56
    public double? PointBuyCar { get; set; }                // Tặng điểm mua xe mới — :57
    public double? PointBuyCreta { get; set; }              // HTV tặng điểm khuyến mại bán hàng — :58
    public double? PointVoucherNewCar { get; set; }         // HTV tặng điểm Voucher xe mới — :59
    public double? PointVoucher { get; set; }               // Tổng điểm Voucher còn lại (hạng sử dụng) — :60
    public double? PointActualVoucher { get; set; }         // Tổng điểm Voucher còn lại (hạng thực) — :61
    public double? PointActualVCNewCar { get; set; }        // Giá trị voucher tặng mua xe mới (hạng thực) — :62
    public string? DealNo { get; set; }                     // Số giao dịch bán lẻ — :63
    public string? TVBHName { get; set; }                   // Tên TVBH — :64
    public DateTime? CreateDTimeUTC { get; set; }           // Thời điểm tạo đăng ký hội viên/hội viên — :65
    public string? CreateBy { get; set; }                   // :66
    public DateTime? LUDTimeUTC { get; set; }               // :67
    public string? LUBy { get; set; }                       // :68
    public DateTime? InactiveDTimeUTC { get; set; }         // Thời điểm Inactive hội viên — :69
    public string? InactiveBy { get; set; }                 // :70
    public DateTime? RegisAppr1DTimeUTC { get; set; }       // Thời điểm duyệt 1 đăng ký hội viên — :71
    public string? RegisAppr1By { get; set; }               // :72
    public DateTime? RegisAppr2DTimeUTC { get; set; }       // Thời điểm duyệt 2 đăng ký hội viên — :73
    public string? RegisAppr2By { get; set; }               // :74
    public string? RegisFinishDTimeUTC { get; set; }        // Thời điểm kết thúc đăng ký hội viên (chuyển LocalTime +7) — :75, Gate :88-92
    public string? RegisFinishBy { get; set; }              // :76
    public DateTime? RegisCancelDTimeUTC { get; set; }      // Thời điểm huỷ đăng ký hội viên — :77
    public string? RegisCancelBy { get; set; }              // :78
    public DateTime? ApproveDTimeUTC { get; set; }          // DDL:155
    public string? ApproveBy { get; set; }                  // DDL:156
    public DateTime? CancelDTimeUTC { get; set; }           // DDL:157
    public string? CancelBy { get; set; }                   // DDL:158
    public string? MemberStatus { get; set; }               // Trạng thái hội viên: PENDING/APPROVE/CANCEL — :79-82
    public string? RegisStatus { get; set; }                // Trạng thái đăng ký: P/A1/A2/F/C — :83-88
    public string? Remark { get; set; }                     // Ghi chú — :89
    public DateTime? InvoiceDate { get; set; }              // Ngày hóa đơn — :120
    public DateTime? LogLUDTimeUTC { get; set; }            // :90
    public string? LogLUBy { get; set; }                    // :91

    // Các cột mở rộng từ phép join:
    public string? cc_CardNo { get; set; }                  // Mã kỳ xét hạng — :92, Card.cs:20482
    public string? mcs_CardSourceName { get; set; }         // Tên loại xét kỳ thẻ (GAP: Mst_CardSource chưa có entity) — :93
    public string? mpv_ProvinceCode { get; set; }           // Mã tỉnh/TP — Card.cs:20501
    public string? mpv_ProvinceName { get; set; }           // Tên tỉnh/TP — :94, Card.cs:20502
    public string? mdt_DistrictCode { get; set; }           // Mã quận/huyện — Card.cs:20503
    public string? mdt_DistrictName { get; set; }           // Tên quận/huyện — :95, Card.cs:20504
    public string? cm_FlagShow { get; set; }                // Trạng thái hội viên tính toán (-1, 1, 0) — :96-99, Card.cs:20505-20511
    public string? mdcp_DLCPCode { get; set; }              // Mã đại lý đăng ký hội viên — :100, Card.cs:20512
    public string? mdcp_DLCPName { get; set; }              // Tên đại lý đăng ký hội viên — :101, Card.cs:20513
    public string? mdcpe_DLCPCodeExcep { get; set; }        // Mã đại lý đăng ký hội viên đặc cách — :102, Card.cs:20514
    public string? mdcpe_DLCPNameExcep { get; set; }        // Tên đại lý đăng ký hội viên đặc cách — :103, Card.cs:20515
    public string? MemberFilePath { get; set; }             // Đường dẫn file upload — :106, Card.cs:20516
    public string? MemberFileName { get; set; }             // Tên file — :107, Card.cs:20517
    public string? MemberFileSpec { get; set; }             // Nội dung base 64 — :109
    public string? FlagFileUpload { get; set; }             // Cờ upload — :111
    public string? AttFileId { get; set; }                  // Cờ upload — :113
    public string? SourceRegisType { get; set; }            // Nguồn mở thẻ ON/OFF — :114, Card.cs:20518
    public string? ReqMemberRegisterCodeSys { get; set; }   // Số yêu cầu mở thẻ online hệ thống — :115, Card.cs:20519
    public string? ReqMemberRegisterCodeOS { get; set; }    // Số yêu cầu mở thẻ online Hyundaime (GAP: Req_MemberRegister) — :116, Card.cs:20520
    public string? PurchaseType { get; set; }               // Loại thẻ (BUYNEW/BUYADD/BUYUSED/EXCHANGE) — :117, Card.cs:20522
    public string? PurchaseTypeName { get; set; }           // Tên loại thẻ — :118, Card.cs:20523
    public DateTime? WarrantyExpiryDate { get; set; }       // Ngày hết hạn bảo hành — :119, Card.cs:20524

    public string? CardSourceCode { get; set; }             // Loại thẻ Crd_Card — :164
    public string? CardStatus { get; set; }                 // Trạng thái thẻ Crd_Card — :165
    public string? FlagExceptionCard { get; set; }          // Cờ đặc cách thẻ — :166
    public double? AmountCardRank { get; set; }             // :167
}

/// <summary>
/// Cặp (CrdMember, CrdCard) biểu diễn kết quả inner join Crd_Member và Crd_Card (cc.CardSourceCode = 'NEW').
/// </summary>
public record CrdMemberCardPair(CrdMember Member, CrdCard Card);

[ApiController]
[Route("api/crd_member")]
public class Crd_MemberControllerPort : ControllerBase
{
    private readonly AppDbContext _db;

    private readonly IWebHostEnvironment? _env;

    public Crd_MemberControllerPort(AppDbContext db, IWebHostEnvironment? env = null)
    {
        _db = db;
        _env = env;
    }

    /// <summary>
    /// Tìm kiếm hội viên phân trang — port 1:1 từ Crd_MemberController.Search (Crd_MemberController.cs:30-140).
    /// </summary>
    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Crd_MemberSearchReq? req)
    {
        req ??= new Crd_MemberSearchReq();

        // 1. Khởi tạo truy vấn:
        // Crd_Member inner join Crd_Card cc on cmb.MemberNo = cc.MemberNo and cc.CardSourceCode = 'NEW'
        // (Card.cs:20526-20530)
        var query = from cm in _db.Set<CrdMember>().AsNoTracking()
                    join cc in _db.Set<CrdCard>().AsNoTracking() on cm.MemberNo equals cc.MemberNo
                    where cc.CardSourceCode == "NEW"
                    select new { cm, cc };

        // 2. Áp dụng các điều kiện lọc từ strWhereClause_Crd_Member (Crd_MemberController.cs:1494-1558):

        // MemberNo: Crd_Member.MemberNo = (:1500-1503)
        if (!string.IsNullOrWhiteSpace(req.MemberNo))
        {
            var pat = PortQuery.Equal(req.MemberNo);
            query = query.Where(x => EF.Functions.Like((x.cm.MemberNo ?? "").ToLower(), pat, PortQuery.Esc));
        }

        // CarNo: Crd_Member.CarNo = (:1504-1507)
        if (!string.IsNullOrWhiteSpace(req.CarNo))
        {
            var pat = PortQuery.Equal(req.CarNo);
            query = query.Where(x => EF.Functions.Like((x.cm.CarNo ?? "").ToLower(), pat, PortQuery.Esc));
        }

        // RegistrationDateFrom: Crd_Member.RegistrationDate >= (:1508-1511)
        if (!string.IsNullOrWhiteSpace(req.RegistrationDateFrom) && DateTime.TryParse(req.RegistrationDateFrom, out var regFrom))
        {
            query = query.Where(x => x.cm.RegistrationDate >= regFrom.Date);
        }

        // RegistrationDateTo: Crd_Member.RegistrationDate <= (:1512-1515)
        if (!string.IsNullOrWhiteSpace(req.RegistrationDateTo) && DateTime.TryParse(req.RegistrationDateTo, out var regTo))
        {
            var endOfDay = regTo.TimeOfDay == TimeSpan.Zero && !req.RegistrationDateTo.Contains(":")
                ? regTo.Date.AddDays(1).AddSeconds(-1)
                : regTo;
            query = query.Where(x => x.cm.RegistrationDate <= endOfDay);
        }

        // MemberActiveDateFrom: Crd_Member.MemberActiveDate >= (:1516-1519)
        if (!string.IsNullOrWhiteSpace(req.MemberActiveDateFrom) && DateTime.TryParse(req.MemberActiveDateFrom, out var actFrom))
        {
            query = query.Where(x => (x.cm.MemberActiveDate ?? x.cc.CardActiveDate) >= actFrom.Date);
        }

        // MemberActiveDateTo: Crd_Member.MemberActiveDate <= (:1520-1523)
        if (!string.IsNullOrWhiteSpace(req.MemberActiveDateTo) && DateTime.TryParse(req.MemberActiveDateTo, out var actTo))
        {
            var endOfDay = actTo.TimeOfDay == TimeSpan.Zero && !req.MemberActiveDateTo.Contains(":")
                ? actTo.Date.AddDays(1).AddSeconds(-1)
                : actTo;
            query = query.Where(x => (x.cm.MemberActiveDate ?? x.cc.CardActiveDate) <= endOfDay);
        }

        // DLCodeRegis: Crd_Member.DLCodeRegis = (:1524-1527)
        if (!string.IsNullOrWhiteSpace(req.DLCodeRegis))
        {
            var pat = PortQuery.Equal(req.DLCodeRegis);
            query = query.Where(x => EF.Functions.Like((x.cm.DLCodeRegis ?? "").ToLower(), pat, PortQuery.Esc));
        }

        // PhoneNo: Crd_Member.PhoneNo = (:1528-1531)
        if (!string.IsNullOrWhiteSpace(req.PhoneNo))
        {
            var pat = PortQuery.Equal(req.PhoneNo);
            query = query.Where(x => EF.Functions.Like((x.cm.PhoneNo ?? "").ToLower(), pat, PortQuery.Esc));
        }

        // RegisStatus: Crd_Member.RegisStatus = (:1532-1535)
        if (!string.IsNullOrWhiteSpace(req.RegisStatus))
        {
            var pat = PortQuery.Equal(req.RegisStatus);
            query = query.Where(x => EF.Functions.Like((x.cm.RegisStatus ?? "").ToLower(), pat, PortQuery.Esc));
        }

        // MemberName: Crd_Member.MemberName = (:1536-1539) — LƯU Ý: toán tử "=", KHÔNG phải LIKE
        if (!string.IsNullOrWhiteSpace(req.MemberName))
        {
            var pat = PortQuery.Equal(req.MemberName);
            query = query.Where(x => EF.Functions.Like((x.cm.MemberName ?? "").ToLower(), pat, PortQuery.Esc));
        }

        // VIN: Crd_Member.VIN = (:1540-1543)
        if (!string.IsNullOrWhiteSpace(req.VIN))
        {
            var pat = PortQuery.Equal(req.VIN);
            query = query.Where(x => EF.Functions.Like((x.cm.VIN ?? "").ToLower(), pat, PortQuery.Esc));
        }

        // MemberStatus: Crd_Member.MemberStatus = (:1544-1547)
        if (!string.IsNullOrWhiteSpace(req.MemberStatus))
        {
            var pat = PortQuery.Equal(req.MemberStatus);
            query = query.Where(x => EF.Functions.Like((x.cm.MemberStatus ?? "").ToLower(), pat, PortQuery.Esc));
        }

        // DLCPCode: Map_QueryDealer_Member.DLCPCode = (:1548-1551)
        if (!string.IsNullOrWhiteSpace(req.DLCPCode))
        {
            var patDLCP = PortQuery.Equal(req.DLCPCode);
            query = query.Where(x => _db.Set<Map_QueryDealer_Member>().Any(m => m.MemberNo == x.cm.MemberNo && EF.Functions.Like((m.DLCPCode ?? "").ToLower(), patDLCP, PortQuery.Esc)));
        }

        // SourceRegisType: Crd_Member.SourceRegisType = (:1552-1555)
        if (!string.IsNullOrWhiteSpace(req.SourceRegisType))
        {
            var pat = PortQuery.Equal(req.SourceRegisType);
            query = query.Where(x => EF.Functions.Like((x.cm.SourceRegisType ?? "").ToLower(), pat, PortQuery.Esc));
        }

        // cm_FlagShow: Crd_Member.MemberStatus tương ứng (Crd_MemberController.cs:1560-1586):
        // "1" -> APPROVE, "0" -> CANCEL, "-1" -> PENDING
        if (!string.IsNullOrWhiteSpace(req.cm_FlagShow))
        {
            if (req.cm_FlagShow == "1")
            {
                var pat = PortQuery.Equal("APPROVE");
                query = query.Where(x => EF.Functions.Like((x.cm.MemberStatus ?? "").ToLower(), pat, PortQuery.Esc));
            }
            else if (req.cm_FlagShow == "0")
            {
                var pat = PortQuery.Equal("CANCEL");
                query = query.Where(x => EF.Functions.Like((x.cm.MemberStatus ?? "").ToLower(), pat, PortQuery.Esc));
            }
            else if (req.cm_FlagShow == "-1")
            {
                var pat = PortQuery.Equal("PENDING");
                query = query.Where(x => EF.Functions.Like((x.cm.MemberStatus ?? "").ToLower(), pat, PortQuery.Esc));
            }
        }

        // 3. Đếm tổng số bản ghi (MyCount — Card.cs:20373)
        var itemCount = await query.CountAsync();

        // 4. Xử lý phân trang theo Bài học A.12 và A.7:
        var pageSize = req.Ft_PageSize;
        var pageIndex = req.Ft_PageIndex;
        var ps = pageSize <= 0 ? 10 : pageSize;
        var pageCount = pageSize <= 0 ? 0 : (itemCount % ps == 0 ? itemCount / ps : itemCount / ps + 1);

        // Bài học A.7: Ft_PageSize = 0 -> DataList rỗng, PageCount = 0, ItemCount giữ nguyên
        if (pageSize <= 0 || pageIndex < 0 || itemCount == 0)
        {
            var emptyResult = new PageInfo<Crd_MemberSearchItem>(
                new List<Crd_MemberSearchItem>(),
                itemCount,
                pageSize == 0 ? 0 : pageCount,
                pageIndex,
                pageSize
            );
            return Ok(new { Success = true, Data = emptyResult });
        }

        // Phân trang và sắp xếp theo cmb.MemberNo asc (Card.cs:20369 và 20546)
        long recordStart = (long)pageIndex * pageSize;
        var pagedPairs = await query
            .OrderBy(x => x.cm.MemberNo)
            .Skip((int)Math.Min(recordStart, int.MaxValue))
            .Take((int)Math.Min(pageSize, int.MaxValue))
            .Select(x => new CrdMemberCardPair(x.cm, x.cc))
            .ToListAsync();

        if (pagedPairs.Count == 0)
        {
            var emptyResult = new PageInfo<Crd_MemberSearchItem>(
                new List<Crd_MemberSearchItem>(),
                itemCount,
                pageCount,
                pageIndex,
                pageSize
            );
            return Ok(new { Success = true, Data = emptyResult });
        }

        // 5. Nạp danh mục và ánh xạ chi tiết theo câu select (Card.cs:20400-20549)
        var dataList = await BuildMemberItemsAsync(pagedPairs, recordStart);

        var result = new PageInfo<Crd_MemberSearchItem>(
            dataList,
            itemCount,
            pageCount,
            pageIndex,
            pageSize
        );

        return Ok(new { Success = true, Data = result });
    }

    /// <summary>
    /// Ánh xạ danh sách (CrdMember, CrdCard) sang model Crd_MemberSearchItem kèm lookup các bảng danh mục
    /// theo câu truy vấn Select chi tiết (Card.cs:20400-20549).
    /// </summary>
    /// <summary>
    /// Port 1:1 Crd_MemberController.Export (gate Crd_MemberController.cs:1348-1450, #054): cùng bộ lọc và nguồn dữ liệu với Search
    /// (WA_Crd_Member_GetRegister), Ft_RecordStart = "0", lấy toàn bộ; RegisFinishDTimeUTC đã đổi giờ địa phương như Search (gate :1417-1423).
    /// Rỗng => ServiceException("Không có dữ liệu") (:1440); có dữ liệu => GetImportDicColums() (:1461-1488, 24 cột), sheet và tiền tố file "Crd_Member",
    /// trả Data = { Url = GetRequestBaseAddress() + url } (:1429-1438).
    /// </summary>
    [HttpPost("export")]
    public async Task<IActionResult> Export([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Crd_MemberSearchReq? req)
    {
        var r = req ?? new Crd_MemberSearchReq();
        r.Ft_PageIndex = 0;
        r.Ft_PageSize = int.MaxValue;
        var res = await Search(r);
        var dataObj = (res as OkObjectResult)?.Value?.GetType().GetProperty("Data")?.GetValue((res as OkObjectResult)!.Value);
        var rows = (dataObj as PageInfo<Crd_MemberSearchItem>)?.DataList;
        if (rows == null || rows.Count == 0)
        {
            return PortResult.Fail("Crd_Member_Export", "Message", "Không có dữ liệu");
        }

        var webRoot = _env?.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
        {
            webRoot = _env != null ? Path.Combine(_env.ContentRootPath, "wwwroot") : Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        }

        try
        {
            var (physicalPath, virtualPath) = PortExcel.GenExcelExportFilePath(webRoot, "Crd_Member");
            PortExcel.ExportToExcelFromList(rows, GetExportDicColums(), physicalPath, "Crd_Member");
            return Ok(new { Success = true, Data = new { Url = PortExcel.GetRequestBaseAddress(Request) + virtualPath } });
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Crd_Member_Export", "Exception", ex.Message);
        }
    }

    /// <summary>Từ điển cột Export — gate Crd_MemberController.cs:1461-1488 (GetImportDicColums, 24 cột, đúng thứ tự).</summary>
    private static Dictionary<string, string> GetExportDicColums() => new()
    {
        { "DLCodeRegis", "Đại lý phát hành" },
        { "MemberNo", "Mã thẻ hội viên" },
        { "CardTypeUse", "Hạng sử dụng" },
        { "CardTypeInit", "Hạng thực" },
        { "RegistrationDate", "Ngày đăng ký" },
        { "MemberActiveDate", "Ngày kích hoạt hội viên" },
        { "RegisStatus", "Trạng thái" },
        { "MemberName", "Tên khách hàng" },
        { "IDCardNo", "CMND/CCCD" },
        { "PhoneNo", "Số điện thoại" },
        { "mpv_ProvinceName", "Tỉnh/TP" },
        { "CarNo", "Biển số xe" },
        { "VIN", "Số VIN" },
        { "ModelName", "Model" },
        { "WarrantyExpiryDate", "Ngày hết hạn bảo hành" },
        { "InvoiceDate", "Ngày hóa đơn" },
        { "cc_CardNo", "Mã kỳ xét hạng" },
        { "EffDateStart", "Ngày bắt đầu kỳ xét hạng" },
        { "RegisFinishDTimeUTC", "Ngày duyệt F" },
        { "RegisFinishBy", "Người duyệt" },
        { "DLCodeExceptionally", "Đại lý YC đặc cách" },
        { "mcs_CardSourceName", "Loại xét kỳ thẻ" },
        { "SourceRegisType", "Nguồn mở thẻ" },
        { "ReqMemberRegisterCodeSys", "Mã lượt đăng ký" }
    };

    private async Task<List<Crd_MemberSearchItem>> BuildMemberItemsAsync(
        List<CrdMemberCardPair> pagedPairs,
        long recordStart)
    {
        var provinceCodes = pagedPairs.Select(p => p.Member.ProvinceCode).Where(c => !string.IsNullOrEmpty(c)).Distinct().ToList();
        var dlCodes = pagedPairs.Select(p => p.Member.DLCodeRegis).Where(c => !string.IsNullOrEmpty(c)).Distinct().ToList();
        var dlExcepCodes = pagedPairs.Select(p => p.Member.DLCodeExceptionally).Where(c => !string.IsNullOrEmpty(c)).Distinct().ToList();
        var allDealerCodes = dlCodes.Concat(dlExcepCodes).Distinct().ToList();
        var purchaseTypes = pagedPairs.Select(p => p.Member.PurchaseType).Where(c => !string.IsNullOrEmpty(c)).Distinct().ToList();

        // 1. Mst_Province (Card.cs:20533-20534)
        var provinces = await _db.Set<Mst_Province>().AsNoTracking()
            .Where(x => provinceCodes.Contains(x.ProvinceCode))
            .ToListAsync();
        var provinceMap = provinces
            .GroupBy(x => x.ProvinceCode)
            .ToDictionary(g => g.Key, g => g.First());

        // 2. Mst_District (Card.cs:20535-20537)
        var districts = await _db.Set<Mst_District>().AsNoTracking()
            .Where(x => provinceCodes.Contains(x.ProvinceCode))
            .ToListAsync();
        var districtMap = districts
            .GroupBy(x => $"{x.ProvinceCode}_{x.DistrictCode}")
            .ToDictionary(g => g.Key, g => g.First());

        // 3. Mst_DealerCP (Card.cs:20538-20541)
        var dealers = await _db.Set<Mst_DealerCP>().AsNoTracking()
            .Where(x => allDealerCodes.Contains(x.DLCPCode))
            .ToListAsync();
        var dealerMap = dealers
            .GroupBy(x => x.DLCPCode)
            .ToDictionary(g => g.Key, g => g.First());

        // 4. Mst_PurchaseType (Card.cs:20544-20545)
        var purchaseTypesList = await _db.Set<Mst_PurchaseType>().AsNoTracking()
            .Where(x => purchaseTypes.Contains(x.PurchaseType))
            .ToListAsync();
        var purchaseTypeMap = purchaseTypesList
            .GroupBy(x => x.PurchaseType)
            .ToDictionary(g => g.Key, g => g.First());

        var dataList = new List<Crd_MemberSearchItem>();
        for (int i = 0; i < pagedPairs.Count; i++)
        {
            var pair = pagedPairs[i];
            var cm = pair.Member;
            var cc = pair.Card;

            provinceMap.TryGetValue(cm.ProvinceCode ?? "", out var mpv);
            districtMap.TryGetValue($"{cm.ProvinceCode}_{cm.DistrictCode}", out var mdt);
            dealerMap.TryGetValue(cm.DLCodeRegis ?? "", out var mdcp);
            dealerMap.TryGetValue(cm.DLCodeExceptionally ?? "", out var mdcpe);
            purchaseTypeMap.TryGetValue(cm.PurchaseType ?? "", out var mpt);

            // Format RegisFinishDTimeUTC sang LocalTime theo utcOffset = 7 nếu có (Crd_MemberController.cs:88-92)
            string? regisFinishDTimeUTC = null;
            if (cm.RegisFinishDTimeUTC.HasValue)
            {
                regisFinishDTimeUTC = cm.RegisFinishDTimeUTC.Value.AddHours(7).ToString("yyyy-MM-dd HH:mm:ss");
            }

            // cm_FlagShow: (case when cmb.MemberStatus = 'PENDING' then '-1' when cmb.MemberStatus = 'APPROVE' then '1' when cmb.MemberStatus = 'CANCEL' then '0' end) (Card.cs:20505-20511)
            string? cm_FlagShow = cm.MemberStatus == "PENDING" ? "-1"
                                : (cm.MemberStatus == "APPROVE" ? "1"
                                : (cm.MemberStatus == "CANCEL" ? "0" : null));

            var item = new Crd_MemberSearchItem
            {
                MyIdxSeq = recordStart + i,
                MemberNo = cm.MemberNo,
                NetworkID = cm.NetworkID,
                OrgID = cm.OrgID,
                DLCodeRegis = cm.DLCodeRegis,
                DLCodeExceptionally = cm.DLCodeExceptionally,
                RegistrationDate = cm.RegistrationDate,
                CustomerCode = cm.CustomerCode,
                ProvinceCode = cm.ProvinceCode,
                DistrictCode = cm.DistrictCode,
                UserCode_Member = cm.UserCode_Member,
                MemberName = cm.MemberName,
                MemberGender = cm.MemberGender,
                IDCardNo = cm.IDCardNo,
                PhoneNo = cm.PhoneNo,
                DateOfBirth = cm.DateOfBirth,
                MemAddress = cm.MemAddress,
                CarNo = cm.CarNo,
                VIN = cm.VIN,
                ModelCode = cm.ModelCode,
                ModelName = cm.ModelName,
                TradeMarkCode = cm.TradeMarkCode,
                EffDateStart = cc.EffDateStart,
                EffDateEnd = cc.EffDateEnd,
                PointTotal = cm.PointTotal,
                PointBlock = cm.PointBlock,
                PointAvail = cm.PointAvail,
                AmountTotal = cm.AmountTotal,
                AmountBlock = cm.AmountBlock,
                AmountAvail = cm.AmountAvail,
                QtyVisitTotal = cm.QtyVisitTotal,
                QtyVisitBlock = cm.QtyVisitBlock,
                QtyVisitAvail = cm.QtyVisitAvail,
                PointBegin = cm.PointBegin,
                PointEnd = cm.PointEnd,
                AmountBegin = cm.AmountBegin,
                AmountEnd = cm.AmountEnd,
                QtyVisitBegin = cm.QtyVisitBegin,
                QtyVisitEnd = cm.QtyVisitEnd,
                PointCardRank = cm.PointCardRank,
                CardTypeUse = cc.CardTypeUse,
                CardTypeInit = cc.CardTypeInit,
                MemberActiveDate = cc.CardActiveDate, // Nguồn: cc.CardActiveDate MemberActiveDate
                FlagExceptionally = cm.FlagExceptionally,
                MemberNoIntro = cm.MemberNoIntro,
                CarNoIntro = cm.CarNoIntro,
                PointIntro = cm.PointIntro,
                PointBuyCar = cm.PointBuyCar,
                PointBuyCreta = cm.PointBuyCreta,
                PointVoucherNewCar = cm.PointVoucherNewCar,
                PointVoucher = cm.PointVoucher,
                PointActualVoucher = cm.PointActualVoucher,
                PointActualVCNewCar = cm.PointActualVCNewCar,
                DealNo = cm.DealNo,
                TVBHName = cm.TVBHName,
                CreateDTimeUTC = cm.CreateDTimeUTC,
                CreateBy = cm.CreateBy,
                LUDTimeUTC = cm.LUDTimeUTC,
                LUBy = cm.LUBy,
                InactiveDTimeUTC = cm.InactiveDTimeUTC,
                InactiveBy = cm.InactiveBy,
                RegisAppr1DTimeUTC = cm.RegisAppr1DTimeUTC,
                RegisAppr1By = cm.RegisAppr1By,
                RegisAppr2DTimeUTC = cm.RegisAppr2DTimeUTC,
                RegisAppr2By = cm.RegisAppr2By,
                RegisFinishDTimeUTC = regisFinishDTimeUTC,
                RegisFinishBy = cm.RegisFinishBy,
                RegisCancelDTimeUTC = cm.RegisCancelDTimeUTC,
                RegisCancelBy = cm.RegisCancelBy,
                ApproveDTimeUTC = cm.ApproveDTimeUTC,
                ApproveBy = cm.ApproveBy,
                CancelDTimeUTC = cm.CancelDTimeUTC,
                CancelBy = cm.CancelBy,
                MemberStatus = cm.MemberStatus,
                RegisStatus = cm.RegisStatus,
                Remark = cm.Remark,
                InvoiceDate = cm.InvoiceDate,
                LogLUDTimeUTC = cm.LogLUDTimeUTC,
                LogLUBy = cm.LogLUBy,
                cc_CardNo = cc.CardNo,
                mcs_CardSourceName = null, // GAP: Mst_CardSource chưa có entity
                mpv_ProvinceCode = mpv?.ProvinceCode ?? cm.ProvinceCode,
                mpv_ProvinceName = mpv?.ProvinceName,
                mdt_DistrictCode = mdt?.DistrictCode ?? cm.DistrictCode,
                mdt_DistrictName = mdt?.DistrictName,
                cm_FlagShow = cm_FlagShow,
                mdcp_DLCPCode = mdcp?.DLCPCode ?? cm.DLCodeRegis,
                mdcp_DLCPName = mdcp?.DLCPName,
                mdcpe_DLCPCodeExcep = mdcpe?.DLCPCode ?? cm.DLCodeExceptionally,
                mdcpe_DLCPNameExcep = mdcpe?.DLCPName,
                MemberFilePath = cm.MemberFilePath,
                MemberFileName = cm.MemberFileName,
                MemberFileSpec = null,
                FlagFileUpload = null,
                AttFileId = null,
                SourceRegisType = cm.SourceRegisType,
                ReqMemberRegisterCodeSys = cm.ReqMemberRegisterCodeSys,
                ReqMemberRegisterCodeOS = null, // GAP: Req_MemberRegister chưa có entity
                PurchaseType = cm.PurchaseType,
                PurchaseTypeName = mpt?.PurchaseTypeName,
                WarrantyExpiryDate = cm.WarrantyExpiryDate,
                CardSourceCode = cc.CardSourceCode,
                CardStatus = cc.CardStatus,
                FlagExceptionCard = cm.FlagExceptionCard,
                AmountCardRank = cm.AmountCardRank
            };
            dataList.Add(item);
        }

        return dataList;
    }

    /// <summary>
    /// Lấy thông tin chi tiết một hội viên theo mã hội viên — port 1:1 từ Crd_MemberController.GetByMemberNo.
    /// Nguồn Gate: Crd_MemberController.cs:142-217
    /// Nguồn hàm lọc: Crd_MemberController.cs:1494-1558 (strWhereClause_Crd_Member)
    /// Nguồn API: CrdMemberController.cs:245-314 (WA_Crd_Member_Get)
    /// Nguồn Biz: Card.cs:11097-11195 (WAS_Crd_Member_Get) -> Card.cs:11197-11349 (Crd_Member_Get) -> Card.cs:11351-11573 (Crd_Member_GetX)
    /// </summary>
    [HttpPost("getbymemberno")]
    public async Task<IActionResult> GetByMemberNo([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Crd_MemberGetByMemberNoReq? req)
    {
        // Gate :164: if (MemberNo != null && !TUtils.CUtils.IsNullOrEmpty(MemberNo))
        // Nếu req null hoặc MemberNo rỗng thì không gọi backend, trả về Crd_Member = null (Gate :163, :200-204)
        if (req == null || string.IsNullOrWhiteSpace(req.MemberNo))
        {
            return Ok(new { Success = true, Data = new { Crd_Member = (object?)null } });
        }

        // Truy vấn Crd_Member với điều kiện:
        // MemberStatus = 'APPROVE' (Gate :170, Biz :11421)
        // MemberNo = req.MemberNo (Gate :1500-1503, Biz :11542-11548)
        var patMemberNo = PortQuery.Equal(req.MemberNo);
        var patApprove = PortQuery.Equal("APPROVE");

        var query = _db.Set<CrdMember>().AsNoTracking()
            .Where(cm => EF.Functions.Like((cm.MemberStatus ?? "").ToLower(), patApprove, PortQuery.Esc))
            .Where(cm => EF.Functions.Like((cm.MemberNo ?? "").ToLower(), patMemberNo, PortQuery.Esc));

        // Nếu có DLCPCode: lọc kèm Map_QueryDealer_Member (Gate :1548-1551, Biz :11417-11420)
        if (!string.IsNullOrWhiteSpace(req.DLCPCode))
        {
            var patDLCP = PortQuery.Equal(req.DLCPCode);
            query = query.Where(cm => _db.Set<Map_QueryDealer_Member>().Any(m => m.MemberNo == cm.MemberNo && EF.Functions.Like((m.DLCPCode ?? "").ToLower(), patDLCP, PortQuery.Esc)));
        }

        // Sắp xếp theo cmb.MemberNo asc, lấy top 1 (Biz :11423, :11436-11438)
        var member = await query.OrderBy(cm => cm.MemberNo).FirstOrDefaultAsync();
        if (member == null)
        {
            return Ok(new { Success = true, Data = new { Crd_Member = (object?)null } });
        }

        // Lấy thẻ gần nhất theo cc.MemberNo = cmb.MemberNo order by cc.CreateDTimeUTC desc (Biz :11459-11467)
        var card = await _db.Set<CrdCard>().AsNoTracking()
            .Where(c => c.MemberNo == member.MemberNo)
            .OrderByDescending(c => c.CreateDTimeUTC)
            .FirstOrDefaultAsync();

        // Lookup danh mục: Mst_Province, Mst_District, Mst_DealerCP, Mst_PurchaseType (Biz :11498-11510)
        Mst_Province? mpv = null;
        if (!string.IsNullOrEmpty(member.ProvinceCode))
        {
            mpv = await _db.Set<Mst_Province>().AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProvinceCode == member.ProvinceCode);
        }

        Mst_District? mdt = null;
        if (!string.IsNullOrEmpty(member.ProvinceCode) && !string.IsNullOrEmpty(member.DistrictCode))
        {
            mdt = await _db.Set<Mst_District>().AsNoTracking()
                .FirstOrDefaultAsync(d => d.ProvinceCode == member.ProvinceCode && d.DistrictCode == member.DistrictCode);
        }

        Mst_DealerCP? mdcp = null;
        if (!string.IsNullOrEmpty(member.DLCodeRegis))
        {
            mdcp = await _db.Set<Mst_DealerCP>().AsNoTracking()
                .FirstOrDefaultAsync(d => d.DLCPCode == member.DLCodeRegis);
        }

        Mst_DealerCP? mdcpe = null;
        if (!string.IsNullOrEmpty(member.DLCodeExceptionally))
        {
            mdcpe = await _db.Set<Mst_DealerCP>().AsNoTracking()
                .FirstOrDefaultAsync(d => d.DLCPCode == member.DLCodeExceptionally);
        }

        Mst_PurchaseType? mpt = null;
        if (!string.IsNullOrEmpty(member.PurchaseType))
        {
            mpt = await _db.Set<Mst_PurchaseType>().AsNoTracking()
                .FirstOrDefaultAsync(pt => pt.PurchaseType == member.PurchaseType);
        }

        // Chuyển RegisFinishDTimeUTC sang giờ địa phương với utcOffset = 7 nếu có giá trị (Gate :194-197)
        string? regisFinishDTimeUTC = null;
        if (member.RegisFinishDTimeUTC.HasValue)
        {
            regisFinishDTimeUTC = member.RegisFinishDTimeUTC.Value.AddHours(7).ToString("yyyy-MM-dd HH:mm:ss");
        }

        // cm_FlagShow: case when MemberStatus = 'APPROVE' then '1' (Biz :11483-11488)
        string cm_FlagShow = "1";

        var item = new Crd_MemberSearchItem
        {
            MyIdxSeq = 0,
            MemberNo = member.MemberNo,
            NetworkID = member.NetworkID,
            OrgID = member.OrgID,
            DLCodeRegis = member.DLCodeRegis,
            DLCodeExceptionally = member.DLCodeExceptionally,
            RegistrationDate = member.RegistrationDate,
            CustomerCode = member.CustomerCode,
            ProvinceCode = member.ProvinceCode,
            DistrictCode = member.DistrictCode,
            UserCode_Member = member.UserCode_Member,
            MemberName = member.MemberName,
            MemberGender = member.MemberGender,
            IDCardNo = member.IDCardNo,
            PhoneNo = member.PhoneNo,
            DateOfBirth = member.DateOfBirth,
            MemAddress = member.MemAddress,
            CarNo = member.CarNo,
            VIN = member.VIN,
            ModelCode = member.ModelCode,
            ModelName = member.ModelName,
            TradeMarkCode = member.TradeMarkCode,
            EffDateStart = member.EffDateStart,
            EffDateEnd = member.EffDateEnd,
            PointTotal = member.PointTotal,
            PointBlock = member.PointBlock,
            PointAvail = member.PointAvail,
            AmountTotal = member.AmountTotal,
            AmountBlock = member.AmountBlock,
            AmountAvail = member.AmountAvail,
            QtyVisitTotal = member.QtyVisitTotal,
            QtyVisitBlock = member.QtyVisitBlock,
            QtyVisitAvail = member.QtyVisitAvail,
            PointBegin = member.PointBegin,
            PointEnd = member.PointEnd,
            AmountBegin = member.AmountBegin,
            AmountEnd = member.AmountEnd,
            QtyVisitBegin = member.QtyVisitBegin,
            QtyVisitEnd = member.QtyVisitEnd,
            PointCardRank = member.PointCardRank,
            CardTypeUse = member.CardTypeUse,
            CardTypeInit = member.CardTypeInit,
            MemberActiveDate = member.MemberActiveDate,
            FlagExceptionally = member.FlagExceptionally,
            MemberNoIntro = member.MemberNoIntro,
            CarNoIntro = member.CarNoIntro,
            PointIntro = member.PointIntro,
            PointBuyCar = member.PointBuyCar,
            PointBuyCreta = member.PointBuyCreta,
            PointVoucherNewCar = member.PointVoucherNewCar,
            PointVoucher = member.PointVoucher,
            PointActualVoucher = member.PointActualVoucher,
            PointActualVCNewCar = member.PointActualVCNewCar,
            DealNo = member.DealNo,
            TVBHName = member.TVBHName,
            CreateDTimeUTC = member.CreateDTimeUTC,
            CreateBy = member.CreateBy,
            LUDTimeUTC = member.LUDTimeUTC,
            LUBy = member.LUBy,
            InactiveDTimeUTC = member.InactiveDTimeUTC,
            InactiveBy = member.InactiveBy,
            RegisAppr1DTimeUTC = member.RegisAppr1DTimeUTC,
            RegisAppr1By = member.RegisAppr1By,
            RegisAppr2DTimeUTC = member.RegisAppr2DTimeUTC,
            RegisAppr2By = member.RegisAppr2By,
            RegisFinishDTimeUTC = regisFinishDTimeUTC,
            RegisFinishBy = member.RegisFinishBy,
            RegisCancelDTimeUTC = member.RegisCancelDTimeUTC,
            RegisCancelBy = member.RegisCancelBy,
            ApproveDTimeUTC = member.ApproveDTimeUTC,
            ApproveBy = member.ApproveBy,
            CancelDTimeUTC = member.CancelDTimeUTC,
            CancelBy = member.CancelBy,
            MemberStatus = member.MemberStatus,
            RegisStatus = member.RegisStatus,
            Remark = member.Remark,
            InvoiceDate = member.InvoiceDate,
            LogLUDTimeUTC = member.LogLUDTimeUTC,
            LogLUBy = member.LogLUBy,
            cc_CardNo = card?.CardNo,
            mcs_CardSourceName = null, // GAP: Mst_CardSource chưa có entity
            mpv_ProvinceCode = mpv?.ProvinceCode ?? member.ProvinceCode,
            mpv_ProvinceName = mpv?.ProvinceName,
            mdt_DistrictCode = mdt?.DistrictCode ?? member.DistrictCode,
            mdt_DistrictName = mdt?.DistrictName,
            cm_FlagShow = cm_FlagShow,
            mdcp_DLCPCode = mdcp?.DLCPCode,
            mdcp_DLCPName = mdcp?.DLCPName,
            mdcpe_DLCPCodeExcep = mdcpe?.DLCPCode,
            mdcpe_DLCPNameExcep = mdcpe?.DLCPName,
            MemberFilePath = member.MemberFilePath,
            MemberFileName = member.MemberFileName,
            MemberFileSpec = null,
            FlagFileUpload = null,
            AttFileId = null,
            SourceRegisType = member.SourceRegisType,
            ReqMemberRegisterCodeSys = member.ReqMemberRegisterCodeSys,
            ReqMemberRegisterCodeOS = null, // GAP: Req_MemberRegister chưa có entity
            PurchaseType = member.PurchaseType,
            PurchaseTypeName = mpt?.PurchaseTypeName,
            WarrantyExpiryDate = member.WarrantyExpiryDate,
            CardSourceCode = member.CardSourceCode,
            CardStatus = member.CardStatus,
            FlagExceptionCard = member.FlagExceptionCard,
            AmountCardRank = member.AmountCardRank
        };

        return Ok(new { Success = true, Data = new { Crd_Member = item } });
    }

    /// <summary>
    /// Lấy thông tin đăng ký của một hội viên theo mã hội viên — port 1:1 từ Crd_MemberController.GetRegisterByMemberNo (#044).
    /// Nguồn Gate: Crd_MemberController.cs:571-657 (GetRegisterByMemberNo)
    /// Nguồn hàm lọc: Crd_MemberController.cs:1494-1558 (strWhereClause_Crd_Member)
    /// Nguồn API: CrdMemberController.cs:97-166 (WA_Crd_Member_GetRegister)
    /// Nguồn Biz: Card.cs:19760-19858 (WAS_Crd_Member_GetRegister) -> Card.cs:19860-20012 (Crd_Member_GetRegister) -> Card.cs:20303-20600 (Crd_Member_GetRegisterX_New20231019 - bản LIVE)
    /// </summary>
    [HttpPost("getregisterbymemberno")]
    public async Task<IActionResult> GetRegisterByMemberNo([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Crd_MemberGetRegisterByMemberNoReq? req)
    {
        // Gate :594: if (MemberNo != null && !TUtils.CUtils.IsNullOrEmpty(MemberNo))
        // Nếu req null hoặc MemberNo rỗng: trả ngay Data = { Crd_Member = null } (Gate :594, :641-644)
        if (req == null || string.IsNullOrWhiteSpace(req.MemberNo))
        {
            return Ok(new { Success = true, Data = new { Crd_Member = (object?)null } });
        }

        // Truy vấn Crd_Member inner join Crd_Card cc on cmb.MemberNo = cc.MemberNo and cc.CardSourceCode = 'NEW' (Card.cs:20528-20530)
        // Lọc duy nhất cmb.MemberNo = req.MemberNo bằng toán tử '=' (Gate :1500-1503, Biz :20367-20368)
        // KHÔNG lọc MemberStatus (khác GetByMemberNo). Sắp xếp cmb.MemberNo asc, lấy top 1 (Biz :20335-20336, :20369, :20377-20384)
        var patMemberNo = PortQuery.Equal(req.MemberNo);

        var query = from cm in _db.Set<CrdMember>().AsNoTracking()
                    join cc in _db.Set<CrdCard>().AsNoTracking() on cm.MemberNo equals cc.MemberNo
                    where cc.CardSourceCode == "NEW"
                       && EF.Functions.Like((cm.MemberNo ?? "").ToLower(), patMemberNo, PortQuery.Esc)
                    orderby cm.MemberNo ascending
                    select new CrdMemberCardPair(cm, cc);

        var pair = await query.FirstOrDefaultAsync();
        if (pair == null)
        {
            // Không tìm thấy hội viên hoặc hội viên không có thẻ NEW (bị inner join loại bỏ) -> Crd_Member = null
            return Ok(new { Success = true, Data = new { Crd_Member = (object?)null } });
        }

        // Tái sử dụng BuildMemberItemsAsync để lookup danh mục và format RegisFinishDTimeUTC (utcOffset +7) (Card.cs:20400-20549, Gate :628-632)
        var items = await BuildMemberItemsAsync(new List<CrdMemberCardPair> { pair }, 0);
        var item = items[0];

        // Chuẩn hóa MemberFilePath: StdStr, nếu không rỗng thì format api/file/{filepath.Replace("\\", "/")}, nếu rỗng thì null (Gate :634-635)
        var filepath = item.MemberFilePath?.Trim();
        item.MemberFilePath = string.IsNullOrEmpty(filepath) ? null : PortExcel.GetRequestBaseAddress(Request) + $"api/file/{filepath.Replace("\\", "/")}";

        return Ok(new { Success = true, Data = new { Crd_Member = item } });
    }

    /// <summary>
    /// Lấy thông tin chi tiết một hội viên theo biển số xe — port 1:1 từ Crd_MemberController.GetByCarNo.
    /// Nguồn Gate: Crd_MemberController.cs:293-365 (GetByCarNo)
    /// Nguồn hàm lọc: Crd_MemberController.cs:1494-1558 (strWhereClause_Crd_Member)
    /// Nguồn API: CrdMemberController.cs:245-314 (WA_Crd_Member_Get)
    /// Nguồn Biz: Card.cs:11097-11195 (WAS_Crd_Member_Get) -> Card.cs:11197-11349 (Crd_Member_Get) -> Card.cs:11351-11573 (Crd_Member_GetX)
    /// </summary>
    [HttpPost("getbycarno")]
    public async Task<IActionResult> GetByCarNo([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Crd_MemberGetByCarNoReq? req)
    {
        // Gate :315: if (CarNo != null && !TUtils.CUtils.IsNullOrEmpty(MemberNo))
        // Nếu req null hoặc MemberNo rỗng thì không gọi backend, trả về Crd_Member = null (Gate :163, :200-204)
        if (req == null || string.IsNullOrWhiteSpace(req.CarNo))
        {
            return Ok(new { Success = true, Data = new { Crd_Member = (object?)null } });
        }

        // Truy vấn Crd_Member với điều kiện:
        // MemberStatus = 'APPROVE' (Gate :170, Biz :11421)
        // CarNo = req.CarNo (Gate :601-604, Biz :11542-11548)
        var patCarNo = PortQuery.Equal(req.CarNo);
        var patApprove = PortQuery.Equal("APPROVE");

        var query = _db.Set<CrdMember>().AsNoTracking()
            .Where(cm => EF.Functions.Like((cm.MemberStatus ?? "").ToLower(), patApprove, PortQuery.Esc))
            .Where(cm => EF.Functions.Like((cm.CarNo ?? "").ToLower(), patCarNo, PortQuery.Esc));

        // Sắp xếp theo cmb.MemberNo asc, lấy top 1 (Biz :11423, :11436-11438)
        var member = await query.OrderBy(cm => cm.MemberNo).FirstOrDefaultAsync();
        if (member == null)
        {
            return Ok(new { Success = true, Data = new { Crd_Member = (object?)null } });
        }

        // Lấy thẻ gần nhất theo cc.MemberNo = cmb.MemberNo order by cc.CreateDTimeUTC desc (Biz :11459-11467)
        var card = await _db.Set<CrdCard>().AsNoTracking()
            .Where(c => c.MemberNo == member.MemberNo)
            .OrderByDescending(c => c.CreateDTimeUTC)
            .FirstOrDefaultAsync();

        // Lookup danh mục: Mst_Province, Mst_District, Mst_DealerCP, Mst_PurchaseType (Biz :11498-11510)
        Mst_Province? mpv = null;
        if (!string.IsNullOrEmpty(member.ProvinceCode))
        {
            mpv = await _db.Set<Mst_Province>().AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProvinceCode == member.ProvinceCode);
        }

        Mst_District? mdt = null;
        if (!string.IsNullOrEmpty(member.ProvinceCode) && !string.IsNullOrEmpty(member.DistrictCode))
        {
            mdt = await _db.Set<Mst_District>().AsNoTracking()
                .FirstOrDefaultAsync(d => d.ProvinceCode == member.ProvinceCode && d.DistrictCode == member.DistrictCode);
        }

        Mst_DealerCP? mdcp = null;
        if (!string.IsNullOrEmpty(member.DLCodeRegis))
        {
            mdcp = await _db.Set<Mst_DealerCP>().AsNoTracking()
                .FirstOrDefaultAsync(d => d.DLCPCode == member.DLCodeRegis);
        }

        Mst_DealerCP? mdcpe = null;
        if (!string.IsNullOrEmpty(member.DLCodeExceptionally))
        {
            mdcpe = await _db.Set<Mst_DealerCP>().AsNoTracking()
                .FirstOrDefaultAsync(d => d.DLCPCode == member.DLCodeExceptionally);
        }

        Mst_PurchaseType? mpt = null;
        if (!string.IsNullOrEmpty(member.PurchaseType))
        {
            mpt = await _db.Set<Mst_PurchaseType>().AsNoTracking()
                .FirstOrDefaultAsync(pt => pt.PurchaseType == member.PurchaseType);
        }

        // Chuyển RegisFinishDTimeUTC sang giờ địa phương với utcOffset = 7 nếu có giá trị (Gate :194-197)
        string? regisFinishDTimeUTC = null;
        if (member.RegisFinishDTimeUTC.HasValue)
        {
            regisFinishDTimeUTC = member.RegisFinishDTimeUTC.Value.AddHours(7).ToString("yyyy-MM-dd HH:mm:ss");
        }

        // cm_FlagShow: case when MemberStatus = 'APPROVE' then '1' (Biz :11483-11488)
        string cm_FlagShow = "1";

        var item = new Crd_MemberSearchItem
        {
            MyIdxSeq = 0,
            MemberNo = member.MemberNo,
            NetworkID = member.NetworkID,
            OrgID = member.OrgID,
            DLCodeRegis = member.DLCodeRegis,
            DLCodeExceptionally = member.DLCodeExceptionally,
            RegistrationDate = member.RegistrationDate,
            CustomerCode = member.CustomerCode,
            ProvinceCode = member.ProvinceCode,
            DistrictCode = member.DistrictCode,
            UserCode_Member = member.UserCode_Member,
            MemberName = member.MemberName,
            MemberGender = member.MemberGender,
            IDCardNo = member.IDCardNo,
            PhoneNo = member.PhoneNo,
            DateOfBirth = member.DateOfBirth,
            MemAddress = member.MemAddress,
            CarNo = member.CarNo,
            VIN = member.VIN,
            ModelCode = member.ModelCode,
            ModelName = member.ModelName,
            TradeMarkCode = member.TradeMarkCode,
            EffDateStart = member.EffDateStart,
            EffDateEnd = member.EffDateEnd,
            PointTotal = member.PointTotal,
            PointBlock = member.PointBlock,
            PointAvail = member.PointAvail,
            AmountTotal = member.AmountTotal,
            AmountBlock = member.AmountBlock,
            AmountAvail = member.AmountAvail,
            QtyVisitTotal = member.QtyVisitTotal,
            QtyVisitBlock = member.QtyVisitBlock,
            QtyVisitAvail = member.QtyVisitAvail,
            PointBegin = member.PointBegin,
            PointEnd = member.PointEnd,
            AmountBegin = member.AmountBegin,
            AmountEnd = member.AmountEnd,
            QtyVisitBegin = member.QtyVisitBegin,
            QtyVisitEnd = member.QtyVisitEnd,
            PointCardRank = member.PointCardRank,
            CardTypeUse = member.CardTypeUse,
            CardTypeInit = member.CardTypeInit,
            MemberActiveDate = member.MemberActiveDate,
            FlagExceptionally = member.FlagExceptionally,
            MemberNoIntro = member.MemberNoIntro,
            CarNoIntro = member.CarNoIntro,
            PointIntro = member.PointIntro,
            PointBuyCar = member.PointBuyCar,
            PointBuyCreta = member.PointBuyCreta,
            PointVoucherNewCar = member.PointVoucherNewCar,
            PointVoucher = member.PointVoucher,
            PointActualVoucher = member.PointActualVoucher,
            PointActualVCNewCar = member.PointActualVCNewCar,
            DealNo = member.DealNo,
            TVBHName = member.TVBHName,
            CreateDTimeUTC = member.CreateDTimeUTC,
            CreateBy = member.CreateBy,
            LUDTimeUTC = member.LUDTimeUTC,
            LUBy = member.LUBy,
            InactiveDTimeUTC = member.InactiveDTimeUTC,
            InactiveBy = member.InactiveBy,
            RegisAppr1DTimeUTC = member.RegisAppr1DTimeUTC,
            RegisAppr1By = member.RegisAppr1By,
            RegisAppr2DTimeUTC = member.RegisAppr2DTimeUTC,
            RegisAppr2By = member.RegisAppr2By,
            RegisFinishDTimeUTC = regisFinishDTimeUTC,
            RegisFinishBy = member.RegisFinishBy,
            RegisCancelDTimeUTC = member.RegisCancelDTimeUTC,
            RegisCancelBy = member.RegisCancelBy,
            ApproveDTimeUTC = member.ApproveDTimeUTC,
            ApproveBy = member.ApproveBy,
            CancelDTimeUTC = member.CancelDTimeUTC,
            CancelBy = member.CancelBy,
            MemberStatus = member.MemberStatus,
            RegisStatus = member.RegisStatus,
            Remark = member.Remark,
            InvoiceDate = member.InvoiceDate,
            LogLUDTimeUTC = member.LogLUDTimeUTC,
            LogLUBy = member.LogLUBy,
            cc_CardNo = card?.CardNo,
            mcs_CardSourceName = null, // GAP: Mst_CardSource chưa có entity
            mpv_ProvinceCode = mpv?.ProvinceCode ?? member.ProvinceCode,
            mpv_ProvinceName = mpv?.ProvinceName,
            mdt_DistrictCode = mdt?.DistrictCode ?? member.DistrictCode,
            mdt_DistrictName = mdt?.DistrictName,
            cm_FlagShow = cm_FlagShow,
            mdcp_DLCPCode = mdcp?.DLCPCode,
            mdcp_DLCPName = mdcp?.DLCPName,
            mdcpe_DLCPCodeExcep = mdcpe?.DLCPCode,
            mdcpe_DLCPNameExcep = mdcpe?.DLCPName,
            MemberFilePath = member.MemberFilePath,
            MemberFileName = member.MemberFileName,
            MemberFileSpec = null,
            FlagFileUpload = null,
            AttFileId = null,
            SourceRegisType = member.SourceRegisType,
            ReqMemberRegisterCodeSys = member.ReqMemberRegisterCodeSys,
            ReqMemberRegisterCodeOS = null, // GAP: Req_MemberRegister chưa có entity
            PurchaseType = member.PurchaseType,
            PurchaseTypeName = mpt?.PurchaseTypeName,
            WarrantyExpiryDate = member.WarrantyExpiryDate,
            CardSourceCode = member.CardSourceCode,
            CardStatus = member.CardStatus,
            FlagExceptionCard = member.FlagExceptionCard,
            AmountCardRank = member.AmountCardRank
        };

        return Ok(new { Success = true, Data = new { Crd_Member = item } });
    }

    /// <summary>
    /// Lấy danh sách thông tin hội viên theo số điện thoại — port 1:1 từ Crd_MemberController.GetByPhoneNo (#041).
    /// Nguồn Gate: Crd_MemberController.cs:219-291 (GetByPhoneNo)
    /// Nguồn hàm lọc: Crd_MemberController.cs:1494-1558 (strWhereClause_Crd_Member)
    /// Nguồn API: CrdMemberController.cs:245-314 (WA_Crd_Member_Get)
    /// Nguồn Biz: Card.cs:11097-11195 (WAS_Crd_Member_Get) -> Card.cs:11197-11349 (Crd_Member_Get) -> Card.cs:11351-11573 (Crd_Member_GetX)
    /// </summary>
    [HttpPost("getbyphoneno")]
    public async Task<IActionResult> GetByPhoneNo([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Crd_MemberGetByPhoneNoReq? req)
    {
        // Gate :242: if (PhoneNo != null && !TUtils.CUtils.IsNullOrEmpty(PhoneNo))
        // Nếu req null hoặc PhoneNo rỗng -> bỏ qua gọi service, trả về ngay Success = true, Data = { Lst_Crd_Member = null } (Gate :242, :274-278)
        if (req == null || string.IsNullOrWhiteSpace(req.PhoneNo))
        {
            return Ok(new { Success = true, Data = new { Lst_Crd_Member = (object?)null } });
        }

        // Truy vấn Crd_Member với điều kiện lọc:
        // MemberStatus = 'APPROVE' (Gate :247, Biz :11421)
        // PhoneNo = req.PhoneNo (toán tử =, không LIKE, không tách dấu phẩy; Gate :1528-1531, Biz :11542-11548)
        var patPhoneNo = PortQuery.Equal(req.PhoneNo);
        var patApprove = PortQuery.Equal("APPROVE");

        var members = await _db.Set<CrdMember>().AsNoTracking()
            .Where(cm => EF.Functions.Like((cm.MemberStatus ?? "").ToLower(), patApprove, PortQuery.Esc))
            .Where(cm => EF.Functions.Like((cm.PhoneNo ?? "").ToLower(), patPhoneNo, PortQuery.Esc))
            .OrderBy(cm => cm.MemberNo) // Sắp xếp cmb.MemberNo asc (Biz :11423, :11511)
            .ToListAsync();

        // Nếu không có bản ghi nào: list_Crd_Member vẫn là null (Gate :241, :268-272, :275-278)
        if (members.Count == 0)
        {
            return Ok(new { Success = true, Data = new { Lst_Crd_Member = (object?)null } });
        }

        var dataList = new List<Crd_MemberSearchItem>();
        for (int i = 0; i < members.Count; i++)
        {
            var member = members[i];

            // Subquery top 1 Crd_Card theo cc.MemberNo = cmb.MemberNo order by cc.CreateDTimeUTC desc (Biz :11459-11467)
            var card = await _db.Set<CrdCard>().AsNoTracking()
                .Where(c => c.MemberNo == member.MemberNo)
                .OrderByDescending(c => c.CreateDTimeUTC)
                .FirstOrDefaultAsync();

            // Lookup danh mục: Mst_Province, Mst_District, Mst_DealerCP, Mst_PurchaseType (Biz :11498-11510)
            Mst_Province? mpv = null;
            if (!string.IsNullOrEmpty(member.ProvinceCode))
            {
                mpv = await _db.Set<Mst_Province>().AsNoTracking()
                    .FirstOrDefaultAsync(p => p.ProvinceCode == member.ProvinceCode);
            }

            Mst_District? mdt = null;
            if (!string.IsNullOrEmpty(member.ProvinceCode) && !string.IsNullOrEmpty(member.DistrictCode))
            {
                mdt = await _db.Set<Mst_District>().AsNoTracking()
                    .FirstOrDefaultAsync(d => d.ProvinceCode == member.ProvinceCode && d.DistrictCode == member.DistrictCode);
            }

            Mst_DealerCP? mdcp = null;
            if (!string.IsNullOrEmpty(member.DLCodeRegis))
            {
                mdcp = await _db.Set<Mst_DealerCP>().AsNoTracking()
                    .FirstOrDefaultAsync(d => d.DLCPCode == member.DLCodeRegis);
            }

            Mst_DealerCP? mdcpe = null;
            if (!string.IsNullOrEmpty(member.DLCodeExceptionally))
            {
                mdcpe = await _db.Set<Mst_DealerCP>().AsNoTracking()
                    .FirstOrDefaultAsync(d => d.DLCPCode == member.DLCodeExceptionally);
            }

            Mst_PurchaseType? mpt = null;
            if (!string.IsNullOrEmpty(member.PurchaseType))
            {
                mpt = await _db.Set<Mst_PurchaseType>().AsNoTracking()
                    .FirstOrDefaultAsync(pt => pt.PurchaseType == member.PurchaseType);
            }

            // Chuyển RegisFinishDTimeUTC sang chuỗi ngày giờ nếu có giá trị
            string? regisFinishDTimeUTC = null;
            if (member.RegisFinishDTimeUTC.HasValue)
            {
                regisFinishDTimeUTC = member.RegisFinishDTimeUTC.Value.AddHours(7).ToString("yyyy-MM-dd HH:mm:ss");
            }

            // cm_FlagShow: case when cmb.MemberStatus = 'APPROVE' then '1' (Biz :11484-11489)
            string cm_FlagShow = "1";

            var item = new Crd_MemberSearchItem
            {
                MyIdxSeq = i, // Identity tương đương MyIdxSeq trong #tbl_Crd_Member_Filter_Draft (Biz :11411, :11457)
                MemberNo = member.MemberNo,
                NetworkID = member.NetworkID,
                OrgID = member.OrgID,
                DLCodeRegis = member.DLCodeRegis,
                DLCodeExceptionally = member.DLCodeExceptionally,
                RegistrationDate = member.RegistrationDate,
                CustomerCode = member.CustomerCode,
                ProvinceCode = member.ProvinceCode,
                DistrictCode = member.DistrictCode,
                UserCode_Member = member.UserCode_Member,
                MemberName = member.MemberName,
                MemberGender = member.MemberGender,
                IDCardNo = member.IDCardNo,
                PhoneNo = member.PhoneNo,
                DateOfBirth = member.DateOfBirth,
                MemAddress = member.MemAddress,
                CarNo = member.CarNo,
                VIN = member.VIN,
                ModelCode = member.ModelCode,
                ModelName = member.ModelName,
                TradeMarkCode = member.TradeMarkCode,
                EffDateStart = member.EffDateStart,
                EffDateEnd = member.EffDateEnd,
                PointTotal = member.PointTotal,
                PointBlock = member.PointBlock,
                PointAvail = member.PointAvail,
                AmountTotal = member.AmountTotal,
                AmountBlock = member.AmountBlock,
                AmountAvail = member.AmountAvail,
                QtyVisitTotal = member.QtyVisitTotal,
                QtyVisitBlock = member.QtyVisitBlock,
                QtyVisitAvail = member.QtyVisitAvail,
                PointBegin = member.PointBegin,
                PointEnd = member.PointEnd,
                AmountBegin = member.AmountBegin,
                AmountEnd = member.AmountEnd,
                QtyVisitBegin = member.QtyVisitBegin,
                QtyVisitEnd = member.QtyVisitEnd,
                PointCardRank = member.PointCardRank,
                CardTypeUse = member.CardTypeUse,
                CardTypeInit = member.CardTypeInit,
                MemberActiveDate = member.MemberActiveDate,
                FlagExceptionally = member.FlagExceptionally,
                MemberNoIntro = member.MemberNoIntro,
                CarNoIntro = member.CarNoIntro,
                PointIntro = member.PointIntro,
                PointBuyCar = member.PointBuyCar,
                PointBuyCreta = member.PointBuyCreta,
                PointVoucherNewCar = member.PointVoucherNewCar,
                PointVoucher = member.PointVoucher,
                PointActualVoucher = member.PointActualVoucher,
                PointActualVCNewCar = member.PointActualVCNewCar,
                DealNo = member.DealNo,
                TVBHName = member.TVBHName,
                CreateDTimeUTC = member.CreateDTimeUTC,
                CreateBy = member.CreateBy,
                LUDTimeUTC = member.LUDTimeUTC,
                LUBy = member.LUBy,
                InactiveDTimeUTC = member.InactiveDTimeUTC,
                InactiveBy = member.InactiveBy,
                RegisAppr1DTimeUTC = member.RegisAppr1DTimeUTC,
                RegisAppr1By = member.RegisAppr1By,
                RegisAppr2DTimeUTC = member.RegisAppr2DTimeUTC,
                RegisAppr2By = member.RegisAppr2By,
                RegisFinishDTimeUTC = regisFinishDTimeUTC,
                RegisFinishBy = member.RegisFinishBy,
                RegisCancelDTimeUTC = member.RegisCancelDTimeUTC,
                RegisCancelBy = member.RegisCancelBy,
                ApproveDTimeUTC = member.ApproveDTimeUTC,
                ApproveBy = member.ApproveBy,
                CancelDTimeUTC = member.CancelDTimeUTC,
                CancelBy = member.CancelBy,
                MemberStatus = member.MemberStatus,
                RegisStatus = member.RegisStatus,
                Remark = member.Remark,
                InvoiceDate = member.InvoiceDate,
                LogLUDTimeUTC = member.LogLUDTimeUTC,
                LogLUBy = member.LogLUBy,
                cc_CardNo = card?.CardNo,
                mcs_CardSourceName = null, // GAP: Mst_CardSource chưa có entity
                mpv_ProvinceCode = mpv?.ProvinceCode ?? member.ProvinceCode,
                mpv_ProvinceName = mpv?.ProvinceName,
                mdt_DistrictCode = mdt?.DistrictCode ?? member.DistrictCode,
                mdt_DistrictName = mdt?.DistrictName,
                cm_FlagShow = cm_FlagShow,
                mdcp_DLCPCode = mdcp?.DLCPCode,
                mdcp_DLCPName = mdcp?.DLCPName,
                mdcpe_DLCPCodeExcep = mdcpe?.DLCPCode,
                mdcpe_DLCPNameExcep = mdcpe?.DLCPName,
                MemberFilePath = member.MemberFilePath,
                MemberFileName = member.MemberFileName,
                MemberFileSpec = null,
                FlagFileUpload = null,
                AttFileId = null,
                SourceRegisType = member.SourceRegisType,
                ReqMemberRegisterCodeSys = member.ReqMemberRegisterCodeSys,
                ReqMemberRegisterCodeOS = null, // GAP: Req_MemberRegister chưa có entity
                PurchaseType = member.PurchaseType,
                PurchaseTypeName = mpt?.PurchaseTypeName,
                WarrantyExpiryDate = member.WarrantyExpiryDate,
                CardSourceCode = member.CardSourceCode,
                CardStatus = member.CardStatus,
                FlagExceptionCard = member.FlagExceptionCard,
                AmountCardRank = member.AmountCardRank
            };

            dataList.Add(item);
        }

        return Ok(new { Success = true, Data = new { Lst_Crd_Member = dataList } });
    }

    /// <summary>
    /// Truy vấn chi tiết hội viên cho DMS — port 1:1 từ Crd_MemberController.GetDetailForDMS (#043).
    /// Nguồn Gate: Crd_MemberController.cs:369-569 (GetDetailForDMS)
    /// Nguồn hàm lọc Crd_Member: Crd_MemberController.cs:1607-1624 (strWhereClause_Crd_MemberForDMS)
    /// Nguồn hàm lọc Map_QueryDealer_Member: Crd_MemberController.cs:1588-1605 (strWhereClause_Map_QueryDealer_Member)
    /// Nguồn API: CrdMemberController.cs:245-314 (WA_Crd_Member_Get)
    /// Nguồn Biz: Card.cs:11097-11195 (WAS_Crd_Member_Get) -> Card.cs:11197-11349 (Crd_Member_Get) -> Card.cs:11351-11573 (Crd_Member_GetX)
    /// </summary>
    [HttpPost("getdetailfordms")]
    public async Task<IActionResult> GetDetailForDMS([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Crd_MemberGetDetailForDMSReq? req)
    {
        // Gate :394: if ((CarNo != null && !TUtils.CUtils.IsNullOrEmpty(CarNo)) || (VIN != null && !TUtils.CUtils.IsNullOrEmpty(VIN)))
        // Nếu cả CarNo và VIN đều rỗng/null: không gọi backend, trả về Data = null (Gate :464-468)
        var carNo = req?.CarNo?.Trim();
        var vin = req?.VIN?.Trim();
        var dlcpCode = req?.DLCPCode?.Trim();

        var hasCarNo = !string.IsNullOrEmpty(carNo);
        var hasVIN = !string.IsNullOrEmpty(vin);

        if (!hasCarNo && !hasVIN)
        {
            return Ok(new { Success = true, Data = (object?)null });
        }

        // Mệnh đề lọc Crd_Member (Gate :1607-1624):
        // CarNo = req.CarNo, VIN = req.VIN; kết hợp "or" nếu cả hai cùng có giá trị.
        // Biz :11421: cmb.MemberStatus = 'APPROVE'
        // Biz :11423: order by cmb.MemberNo asc
        // Gate :413-414, Biz :11436-11438: Ft_RecordStart = 0, Ft_RecordCount = 1 -> lấy top 1 theo MemberNo asc
        var patApprove = PortQuery.Equal("APPROVE");
        var query = _db.Set<CrdMember>().AsNoTracking()
            .Where(cm => EF.Functions.Like((cm.MemberStatus ?? "").ToLower(), patApprove, PortQuery.Esc));

        if (hasCarNo && hasVIN)
        {
            var patCarNo = PortQuery.Equal(carNo!);
            var patVIN = PortQuery.Equal(vin!);
            query = query.Where(cm =>
                EF.Functions.Like((cm.CarNo ?? "").ToLower(), patCarNo, PortQuery.Esc) ||
                EF.Functions.Like((cm.VIN ?? "").ToLower(), patVIN, PortQuery.Esc));
        }
        else if (hasCarNo)
        {
            var patCarNo = PortQuery.Equal(carNo!);
            query = query.Where(cm => EF.Functions.Like((cm.CarNo ?? "").ToLower(), patCarNo, PortQuery.Esc));
        }
        else
        {
            var patVIN = PortQuery.Equal(vin!);
            query = query.Where(cm => EF.Functions.Like((cm.VIN ?? "").ToLower(), patVIN, PortQuery.Esc));
        }

        var member = await query.OrderBy(cm => cm.MemberNo).FirstOrDefaultAsync();
        if (member == null)
        {
            // Gate :464-468: khi không tìm thấy hội viên -> Data = null
            return Ok(new { Success = true, Data = (object?)null });
        }

        // Gate :392: FlagIsDLQuery mặc định = 1
        var flagIsDLQuery = 1;
        // Gate :428: if (DLCPCode != null && !TUtils.CUtils.IsNullOrEmpty(DLCPCode) && MemberNo != null && !TUtils.CUtils.IsNullOrEmpty(MemberNo))
        if (!string.IsNullOrEmpty(dlcpCode) && !string.IsNullOrEmpty(member.MemberNo))
        {
            // Gate :1588-1605: strWhereClause_Map_QueryDealer_Member: DLCPCode = DLCPCode and MemberNo = MemberNo
            var patDLCP = PortQuery.Equal(dlcpCode);
            var patMemberNo = PortQuery.Equal(member.MemberNo);
            var exists = await _db.Set<Map_QueryDealer_Member>().AsNoTracking()
                .AnyAsync(m => EF.Functions.Like((m.DLCPCode ?? "").ToLower(), patDLCP, PortQuery.Esc) &&
                               EF.Functions.Like((m.MemberNo ?? "").ToLower(), patMemberNo, PortQuery.Esc));

            // Gate :454-461: có bản ghi -> 1, không có -> 0
            flagIsDLQuery = exists ? 1 : 0;
        }

        // Lấy thẻ gần nhất theo cc.MemberNo = cmb.MemberNo order by cc.CreateDTimeUTC desc (Biz :11459-11467)
        var card = await _db.Set<CrdCard>().AsNoTracking()
            .Where(c => c.MemberNo == member.MemberNo)
            .OrderByDescending(c => c.CreateDTimeUTC)
            .FirstOrDefaultAsync();

        // Lookup danh mục: Mst_Province, Mst_District, Mst_DealerCP (Biz :11498-11506)
        Mst_Province? mpv = null;
        if (!string.IsNullOrEmpty(member.ProvinceCode))
        {
            mpv = await _db.Set<Mst_Province>().AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProvinceCode == member.ProvinceCode);
        }

        Mst_District? mdt = null;
        if (!string.IsNullOrEmpty(member.ProvinceCode) && !string.IsNullOrEmpty(member.DistrictCode))
        {
            mdt = await _db.Set<Mst_District>().AsNoTracking()
                .FirstOrDefaultAsync(d => d.ProvinceCode == member.ProvinceCode && d.DistrictCode == member.DistrictCode);
        }

        Mst_DealerCP? mdcp = null;
        if (!string.IsNullOrEmpty(member.DLCodeRegis))
        {
            mdcp = await _db.Set<Mst_DealerCP>().AsNoTracking()
                .FirstOrDefaultAsync(d => d.DLCPCode == member.DLCodeRegis);
        }

        Mst_DealerCP? mdcpe = null;
        if (!string.IsNullOrEmpty(member.DLCodeExceptionally))
        {
            mdcpe = await _db.Set<Mst_DealerCP>().AsNoTracking()
                .FirstOrDefaultAsync(d => d.DLCPCode == member.DLCodeExceptionally);
        }

        // cm_FlagShow: case when MemberStatus = 'APPROVE' then '1' (Biz :11483-11488)
        string cm_FlagShow = "1";

        // Gate :472-555: Data = new { Crd_Member = new { ... 78 thuộc tính gồm FlagIsDLQuery } }
        var item = new Crd_MemberGetDetailForDMSItem
        {
            MemberNo = member.MemberNo,
            NetworkID = member.NetworkID,
            OrgID = member.OrgID,
            DLCodeRegis = member.DLCodeRegis,
            DLCodeExceptionally = member.DLCodeExceptionally,
            RegistrationDate = member.RegistrationDate,
            CustomerCode = member.CustomerCode,
            ProvinceCode = member.ProvinceCode,
            DistrictCode = member.DistrictCode,
            UserCode_Member = member.UserCode_Member,
            MemberName = member.MemberName,
            MemberGender = member.MemberGender,
            IDCardNo = member.IDCardNo,
            PhoneNo = member.PhoneNo,
            DateOfBirth = member.DateOfBirth,
            MemAddress = member.MemAddress,
            CarNo = member.CarNo,
            VIN = member.VIN,
            ModelCode = member.ModelCode,
            ModelName = member.ModelName,
            TradeMarkCode = member.TradeMarkCode,
            EffDateStart = member.EffDateStart,
            EffDateEnd = member.EffDateEnd,
            PointTotal = member.PointTotal,
            PointBlock = member.PointBlock,
            PointAvail = member.PointAvail,
            AmountTotal = member.AmountTotal,
            AmountBlock = member.AmountBlock,
            AmountAvail = member.AmountAvail,
            QtyVisitTotal = member.QtyVisitTotal,
            QtyVisitBlock = member.QtyVisitBlock,
            QtyVisitAvail = member.QtyVisitAvail,
            PointBegin = member.PointBegin,
            PointEnd = member.PointEnd,
            AmountBegin = member.AmountBegin,
            AmountEnd = member.AmountEnd,
            QtyVisitBegin = member.QtyVisitBegin,
            QtyVisitEnd = member.QtyVisitEnd,
            PointCardRank = member.PointCardRank,
            CardTypeUse = member.CardTypeUse,
            CardTypeInit = member.CardTypeInit,
            MemberActiveDate = member.MemberActiveDate,
            FlagExceptionally = member.FlagExceptionally,
            MemberNoIntro = member.MemberNoIntro,
            CarNoIntro = member.CarNoIntro,
            PointIntro = member.PointIntro,
            PointBuyCar = member.PointBuyCar,
            DealNo = member.DealNo,
            TVBHName = member.TVBHName,
            CreateDTimeUTC = member.CreateDTimeUTC,
            CreateBy = member.CreateBy,
            LUDTimeUTC = member.LUDTimeUTC,
            LUBy = member.LUBy,
            InactiveDTimeUTC = member.InactiveDTimeUTC,
            InactiveBy = member.InactiveBy,
            RegisAppr1DTimeUTC = member.RegisAppr1DTimeUTC,
            RegisAppr1By = member.RegisAppr1By,
            RegisAppr2DTimeUTC = member.RegisAppr2DTimeUTC,
            RegisAppr2By = member.RegisAppr2By,
            RegisFinishDTimeUTC = member.RegisFinishDTimeUTC,
            RegisFinishBy = member.RegisFinishBy,
            RegisCancelDTimeUTC = member.RegisCancelDTimeUTC,
            RegisCancelBy = member.RegisCancelBy,
            MemberStatus = member.MemberStatus,
            RegisStatus = member.RegisStatus,
            Remark = member.Remark,
            LogLUDTimeUTC = member.LogLUDTimeUTC,
            LogLUBy = member.LogLUBy,
            cc_CardNo = card?.CardNo,
            mcs_CardSourceName = null, // GAP: Mst_CardSource chưa có entity
            mpv_ProvinceName = mpv?.ProvinceName,
            mdt_DistrictName = mdt?.DistrictName,
            cm_FlagShow = cm_FlagShow,
            mdcp_DLCPCode = mdcp?.DLCPCode,
            mdcp_DLCPName = mdcp?.DLCPName,
            mdcpe_DLCPCodeExcep = mdcpe?.DLCPCode,
            mdcpe_DLCPNameExcep = mdcpe?.DLCPName,
            FlagIsDLQuery = flagIsDLQuery
        };

        return Ok(new { Success = true, Data = new { Crd_Member = item } });
    }

    /// <summary>
    /// Tính điểm khuyến mại mua xe Creta — port 1:1 từ Crd_MemberController/GetCalcPointBuyCreta (#045).
    /// Nguồn Gate: Crd_MemberController.cs:659-737 (GetCalcPointBuyCreta)
    /// Nguồn API: CrdMemberController.cs:171-240 (WA_Crd_MemberRegis_CalcPointBuyCreta)
    /// Nguồn Biz: Card.cs:20602-20696 (WAS_Crd_MemberRegis_CalcPointBuyCreta) -> Card.cs:20701-20889 (Crd_MemberRegis_CalcPointBuyCreta) -> Card.cs:20899-21225 (Crd_MemberRegis_CalcPointBuyCretaX - bản LIVE)
    /// </summary>
    [HttpPost("getcalcpointbuycreta")]
    public IActionResult GetCalcPointBuyCreta([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Crd_MemberGetCalcPointBuyCretaReq? req)
    {
        // 1. Chuẩn hoá tham số đầu vào theo nguồn Biz Card.cs:20956-20967:
        // - StdParam: Trim + ToUpper, rỗng -> null (MemberNo :20956, CardTypeUse :20957, CarNo :20958, VIN :20959, IDCardNo :20961)
        // - StdStr: Trim + Normalize, giữ nguyên hoa/thường, rỗng -> null (PhoneNo :20960)
        // - DLCodeRegis: Trong nguồn lấy từ quyền người dùng drAbilityOfUser["DLCPCode"] (:20967). Ở MiniLoyalty, phân quyền người dùng là GAP, nhận trực tiếp từ req.DLCodeRegis qua StdParam.
        var strMemberNo = StdParam(req?.MemberNo);
        var strCardTypeUse = StdParam(req?.CardTypeUse);
        var strDLCodeRegis = StdParam(req?.DLCodeRegis);
        var strCarNo = StdParam(req?.CarNo);
        var strVIN = StdParam(req?.VIN);
        var strPhoneNo = StdStr(req?.PhoneNo);
        var strIDCardNo = StdParam(req?.IDCardNo);

        // 2. Nghiệp vụ tính điểm Creta:
        // Trong nguồn Biz Card.cs:20962 khởi tạo dblPointBuyCreta = 3000000, sau đó gọi dịch vụ ngoài WSDMS (DMSSales_DLS_DealerCustomer_Get :21031).
        // - GAP 1: Hệ thống MiniLoyalty không có WSDMS và không có bảng DLS_* (bỏ qua theo Bài học LESSONS_AGENTS.md A.16).
        // - GAP 2: Mốc thời gian chiến dịch Creta "2025-07-31" (:21076, :21092) đã qua trong thực tế -> dblPointBuyCretaUpd = 0.
        // Do đó, PointBuyCreta cuối cùng = 0.0 (Card.cs:21182-21220).
        double dblPointBuyCreta = 0.0;

        // 3. Khởi tạo đối tượng kết quả đúng 8 thuộc tính (Crd_MemberRegisCalcPoint.cs:9-19, Card.cs:20973-20998):
        var item = new Crd_MemberRegisCalcPointItem
        {
            MemberNo = strMemberNo,
            CardTypeUse = strCardTypeUse,
            DLCodeRegis = strDLCodeRegis,
            CarNo = strCarNo,
            VIN = strVIN,
            PhoneNo = strPhoneNo,
            IDCardNo = strIDCardNo,
            PointBuyCreta = dblPointBuyCreta
        };

        // 4. Trả kết quả theo cấu trúc Gate Crd_MemberController.cs:720-724:
        // Success = true, Data = { Crd_MemberRegisCalcPoint = item }
        return Ok(new
        {
            Success = true,
            Data = new Crd_MemberGetCalcPointBuyCretaRes
            {
                Crd_MemberRegisCalcPoint = item
            }
        });
    }

    /// <summary>
    /// Chuẩn hoá chuỗi mã theo CUtils.StdParam (Card.cs:20956-20961): Trim, ToUpperInvariant, rỗng -> null.
    /// </summary>
    private static string? StdParam(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim().ToUpperInvariant();

    /// <summary>
    /// Chuẩn hoá chuỗi văn bản theo CUtils.StdStr (Card.cs:20960): Trim, Normalize, giữ nguyên hoa/thường, rỗng -> null.
    /// </summary>
    private static string? StdStr(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim().Normalize();
}

public class Crd_MemberGetByCarNoReq
{
    public string? CarNo { get; set; }
}

/// <summary>
/// Tham số request truy vấn chi tiết hội viên cho DMS — port 1:1 từ Crd_MemberController.GetDetailForDMS (Gate :369).
/// </summary>
public class Crd_MemberGetDetailForDMSReq
{
    public string? CarNo { get; set; }
    public string? VIN { get; set; }
    public string? DLCPCode { get; set; }
}

/// <summary>
/// DTO thông tin hội viên trả về cho DMS — port 1:1 từ Crd_MemberController.cs:476-553 (đúng 78 thuộc tính gồm FlagIsDLQuery).
/// </summary>
public class Crd_MemberGetDetailForDMSItem
{
    public string? MemberNo { get; set; }
    public string? NetworkID { get; set; }
    public string? OrgID { get; set; }
    public string? DLCodeRegis { get; set; }
    public string? DLCodeExceptionally { get; set; }
    public DateTime? RegistrationDate { get; set; }
    public string? CustomerCode { get; set; }
    public string? ProvinceCode { get; set; }
    public string? DistrictCode { get; set; }
    public string? UserCode_Member { get; set; }
    public string? MemberName { get; set; }
    public string? MemberGender { get; set; }
    public string? IDCardNo { get; set; }
    public string? PhoneNo { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? MemAddress { get; set; }
    public string? CarNo { get; set; }
    public string? VIN { get; set; }
    public string? ModelCode { get; set; }
    public string? ModelName { get; set; }
    public string? TradeMarkCode { get; set; }
    public DateTime? EffDateStart { get; set; }
    public DateTime? EffDateEnd { get; set; }
    public double? PointTotal { get; set; }
    public double? PointBlock { get; set; }
    public double? PointAvail { get; set; }
    public double? AmountTotal { get; set; }
    public double? AmountBlock { get; set; }
    public double? AmountAvail { get; set; }
    public double? QtyVisitTotal { get; set; }
    public double? QtyVisitBlock { get; set; }
    public double? QtyVisitAvail { get; set; }
    public double? PointBegin { get; set; }
    public double? PointEnd { get; set; }
    public double? AmountBegin { get; set; }
    public double? AmountEnd { get; set; }
    public double? QtyVisitBegin { get; set; }
    public double? QtyVisitEnd { get; set; }
    public double? PointCardRank { get; set; }
    public string? CardTypeUse { get; set; }
    public string? CardTypeInit { get; set; }
    public DateTime? MemberActiveDate { get; set; }
    public string? FlagExceptionally { get; set; }
    public string? MemberNoIntro { get; set; }
    public string? CarNoIntro { get; set; }
    public double? PointIntro { get; set; }
    public double? PointBuyCar { get; set; }
    public string? DealNo { get; set; }
    public string? TVBHName { get; set; }
    public DateTime? CreateDTimeUTC { get; set; }
    public string? CreateBy { get; set; }
    public DateTime? LUDTimeUTC { get; set; }
    public string? LUBy { get; set; }
    public DateTime? InactiveDTimeUTC { get; set; }
    public string? InactiveBy { get; set; }
    public DateTime? RegisAppr1DTimeUTC { get; set; }
    public string? RegisAppr1By { get; set; }
    public DateTime? RegisAppr2DTimeUTC { get; set; }
    public string? RegisAppr2By { get; set; }
    public DateTime? RegisFinishDTimeUTC { get; set; }
    public string? RegisFinishBy { get; set; }
    public DateTime? RegisCancelDTimeUTC { get; set; }
    public string? RegisCancelBy { get; set; }
    public string? MemberStatus { get; set; }
    public string? RegisStatus { get; set; }
    public string? Remark { get; set; }
    public DateTime? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
    public string? cc_CardNo { get; set; }
    public string? mcs_CardSourceName { get; set; }
    public string? mpv_ProvinceName { get; set; }
    public string? mdt_DistrictName { get; set; }
    public string? cm_FlagShow { get; set; }
    public string? mdcp_DLCPCode { get; set; }
    public string? mdcp_DLCPName { get; set; }
    public string? mdcpe_DLCPCodeExcep { get; set; }
    public string? mdcpe_DLCPNameExcep { get; set; }
    public int FlagIsDLQuery { get; set; }
}

/// <summary>
/// Tham số request tính điểm khuyến mại mua xe Creta — port 1:1 từ Crd_MemberController.GetCalcPointBuyCreta (Gate Crd_MemberController.cs:659).
/// </summary>
public class Crd_MemberGetCalcPointBuyCretaReq
{
    public string? MemberNo { get; set; }
    public string? CardTypeUse { get; set; }
    public string? DLCodeRegis { get; set; }
    public string? CarNo { get; set; }
    public string? VIN { get; set; }
    public string? PhoneNo { get; set; }
    public string? IDCardNo { get; set; }
}

/// <summary>
/// DTO chứa thông tin kết quả tính điểm khuyến mại mua xe Creta — port 1:1 từ model Crd_MemberRegisCalcPoint.cs:9-19
/// và bảng tạm #input_Crd_MemberRegisCalcPoint (Card.cs:20973-20998). Đúng 8 trường.
/// </summary>
public class Crd_MemberRegisCalcPointItem
{
    public string? MemberNo { get; set; }
    public string? CardTypeUse { get; set; }
    public string? DLCodeRegis { get; set; }
    public string? CarNo { get; set; }
    public string? VIN { get; set; }
    public string? PhoneNo { get; set; }
    public string? IDCardNo { get; set; }
    public double PointBuyCreta { get; set; } = 0.0;
}

/// <summary>
/// DTO cấu trúc Data trả về của GetCalcPointBuyCreta — Gate Crd_MemberController.cs:721-724: Data = new { Crd_MemberRegisCalcPoint = ... }
/// </summary>
public class Crd_MemberGetCalcPointBuyCretaRes
{
    public Crd_MemberRegisCalcPointItem? Crd_MemberRegisCalcPoint { get; set; }
}
