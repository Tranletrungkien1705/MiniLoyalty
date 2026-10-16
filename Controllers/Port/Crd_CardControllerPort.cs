using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniLoyalty.Data;
using MiniLoyalty.Models;
using MiniLoyalty.Models.Port;

namespace MiniLoyalty.Controllers.Port;

// ============================================================================================
// PORT 1:1 — Crd_CardController/Search (#001).
//
// Nguồn tầng Gate:
//   Crd_CardController.cs:29-131    Search -> Crd_CardService.WA_Crd_Card_Get
//   Crd_CardController.cs:586-697   strWhereClause_Crd_Card (dựng mệnh đề lọc)
// Nguồn tầng API:
//   CrdCardController.cs:163-233    WA_Crd_Card_Get -> _biz.WAS_Crd_Card_Get
// Nguồn tầng Biz:
//   Master.Loyalty.cs:18824-18931   WAS_Crd_Card_Get
//   Master.Loyalty.cs:18932-19084   Crd_Card_Get
//   Master.Loyalty.cs:19452-19864   Crd_Card_GetX_New20251229 (bản LIVE đang được gọi)
//
// GIỮ ĐÚNG NGUỒN:
//   * Endpoint POST: /api/crd_card/search
//   * Toán tử lọc:
//       - MemberNo, CarNo, VIN, PhoneNo, DLCPCode, CardSourceCode, FlagExceptionally, CardTypeUse: = (không wildcard)
//       - DLCodeRegis, CardStatus, MemberStatus: in (tách dấu phẩy qua CUtils.StrSplit)
//       - MemberActiveDateFrom / To: >= và <= trên Crd_Member.MemberActiveDate
//       - EffDateStartFrom / To: >= và <= trên Crd_Card.ApproveDTimeUTC (chuẩn hóa đầu/cuối ngày giờ UTC)
//       - InactiveDTimeFrom / To: >= và <= trên Crd_Member.InactiveDTimeUTC
//   * Phân trang: Bài học A.12, A.7 (Ft_PageSize = 0 -> DataList rỗng, PageCount = 0, ItemCount giữ tổng số).
//   * Order by: Draft subquery lấy thẻ mới nhất theo cc.CreateDTimeUTC desc; câu select cuối sắp cc.CardNo desc.
//   * Tính toán cm_FlagShow & cc_FlagShow: PENDING -> '-1', APPROVE -> '1', CANCEL -> '0'.
//   * Tính toán PointExpiry, AmountRef, PointUse, PointAccumulated, QtyVisitMiss, RevenueMiss theo đúng SQL nguồn.
//   * Đặt tên class không trùng theo yêu cầu Reviewer Claude:
//       - Request: Crd_CardControllerSearchReq
//       - Item trả về: Crd_CardSearchItem
// ============================================================================================

/// <summary>
/// Tham số tìm kiếm thẻ hội viên — port 1:1 từ tầng Gate Crd_CardController.Search (Crd_CardController.cs:29-30).
/// </summary>
public class Crd_CardControllerSearchReq
{
    public string? MemberNo { get; set; }                    // Mã hội viên — Crd_CardController.cs:29
    public string? DLCodeRegis { get; set; }                 // Mã đại lý đăng ký — :29
    public string? DLCPCode { get; set; }                    // Mã đại lý truy vấn — :29
    public string? CardSourceCode { get; set; }              // Loại thẻ — :29
    public string? VIN { get; set; }                         // Số khung — :29
    public string? CardStatus { get; set; }                  // Trạng thái thẻ — :29
    public string? MemberActiveDateFrom { get; set; }        // Ngày kích hoạt từ — :29
    public string? MemberActiveDateTo { get; set; }          // Ngày kích hoạt đến — :29
    public string? CardNo { get; set; }                      // Số thẻ — :29 (nguồn comment out ở strWhereClause_Crd_Card)
    public string? FlagExceptionally { get; set; }           // Cờ đặc cách — :30
    public string? EffDateStartFrom { get; set; }            // Ngày hiệu lực từ — :30
    public string? EffDateStartTo { get; set; }              // Ngày hiệu lực đến — :30
    public string? InactiveDTimeFrom { get; set; }           // Thời gian dừng từ — :30
    public string? InactiveDTimeTo { get; set; }             // Thời gian dừng đến — :30
    public string? CardTypeUse { get; set; }                 // Hạng thẻ sử dụng — :30
    public string? CarNo { get; set; }                       // Biển số xe — :30
    public string? MemberStatus { get; set; }                // Trạng thái hội viên — :30
    public string? PhoneNo { get; set; }                     // Số điện thoại — :30
    public string? cm_FlagShow { get; set; }                 // Cờ hiển thị hội viên — :30
    public string? cc_FlagShow { get; set; }                 // Cờ hiển thị thẻ — :30
    public int Ft_PageIndex { get; set; } = 0;               // Trang hiện tại — :30
    public int Ft_PageSize { get; set; } = 10;               // Kích thước trang — :30
}

/// <summary>
/// Model từng dòng dữ liệu thẻ hội viên trả về ở DataList — port 1:1 từ model Crd_Card.cs (1-120) và select cuối (Master.Loyalty.cs:19654-19794).
/// </summary>
public class Crd_CardSearchItem
{
    public string? CardNo { get; set; }                     // Mã thẻ hội viên — Crd_Card.cs:11
    public string? NetworkID { get; set; }                  // NetworkID — :12
    public string? MemberNo { get; set; }                   // Mã hội viên — :13
    public string? DLCodeExceptionally { get; set; }        // Đại lý đặc cách — :14
    public string? CardSourceCode { get; set; }             // Mã loại thẻ: Xuống hạng/Duy trì/Làm mới/Làm lại/Tăng hạng — :15
    public string? RankPolicyCode { get; set; }             // Mã chính sách xếp hạng — :16
    public double? PointBonus { get; set; }                 // Điểm thưởng — :17
    public double? PointPurchaseAddCar { get; set; }        // Điểm thưởng mua thêm xe — :18
    public string? EffDateStart { get; set; }               // Ngày bắt đầu kỳ xét hạng (hoặc ApproveDTime VN) — :19
    public DateTime? EffDateEnd { get; set; }               // Ngày hết hạn kỳ xét hạng — :20
    public double? PointTotal { get; set; }                 // Điểm tiêu dùng trong kỳ — :21
    public double? PointBlock { get; set; }                 // :22
    public double? PointAvail { get; set; }                 // Điểm tiêu dùng còn lại — :23
    public double? AmountTotal { get; set; }                // Giá trị tích lũy trong kỳ — :24
    public double? AmountBlock { get; set; }                // :25
    public double? AmountAvail { get; set; }                // Doanh thu xét hạng trong kỳ — :26
    public double? QtyVisitTotal { get; set; }              // Số lượt dịch vụ — :27
    public double? QtyVisitBlock { get; set; }              // :28
    public double? QtyVisitAvail { get; set; }              // :29
    public double? PointCardRank { get; set; }              // :30
    public double? TotalAmountPeriod { get; set; }          // :31
    public string? CardNoPrev { get; set; }                 // Mã thẻ kỳ trước — :32
    public string? CardTypeUse { get; set; }                // Hạng sử dụng — :33
    public string? CardTypeInit { get; set; }               // Hạng thực — :34
    public string? CardTypeUsePrev { get; set; }            // Hạng sử dụng kỳ trước — :35
    public string? CardTypeInitPrev { get; set; }           // Hạng thực kỳ trước — :36
    public DateTime? CardActiveDate { get; set; }           // Ngày kích hoạt thẻ — :37
    public string? FlagExceptionally { get; set; }          // Cờ đặc cách — :38
    public DateTime? CreateDTimeUTC { get; set; }           // Thời điểm tạo thẻ — :39
    public string? CreateBy { get; set; }                   // :40
    public DateTime? LUDTimeUTC { get; set; }               // :41
    public string? LUBy { get; set; }                       // :42
    public string? CardStatus { get; set; }                 // Trạng thái thẻ — :43
    public string? Remark { get; set; }                     // :47
    public DateTime? LogLUDTimeUTC { get; set; }            // :48
    public string? LogLUBy { get; set; }                    // :49
    public DateTime? ApproveDTimeUTC { get; set; }          // Thời điểm duyệt thẻ UTC
    public string? ApproveBy { get; set; }                  // Người duyệt thẻ
    public DateTime? CancelDTimeUTC { get; set; }           // Thời điểm huỷ thẻ UTC
    public string? CancelBy { get; set; }                   // Người huỷ thẻ

    public string? RankActionType { get; set; }             // :52
    public string? CardTypeUseExpect { get; set; }          // :53
    public string? CardTypeInitExpect { get; set; }         // :54

    public string? MemberName { get; set; }                 // :56
    public string? MemberStatus { get; set; }               // :58
    public string? IDCardNo { get; set; }                   // :59
    public string? MemberGender { get; set; }               // :60
    public string? MemAddress { get; set; }                 // :61
    public DateTime? DateOfBirth { get; set; }              // :62
    public string? PhoneNo { get; set; }                    // :63
    public string? CarNo { get; set; }                      // :64
    public string? VIN { get; set; }                        // :65

    public string? ProvinceName { get; set; }               // :67
    public string? ModelName { get; set; }                  // :68
    public string? ModelCode { get; set; }                  // :69
    public string? BrandName { get; set; }                  // :70
    public string? DistrictName { get; set; }               // :72
    public string? ProvinceCode { get; set; }               // Mã Tỉnh/TP — :73
    public string? DistrictCode { get; set; }               // Mã Quận/huyện — :74
    public DateTime? WarrantyExpiryDate { get; set; }       // Ngày hết hạn bảo hành — :75
    public DateTime? InvoiceDate { get; set; }              // Ngày hóa đơn — :76

    public DateTime? MemberActiveDate { get; set; }         // Ngày kích hoạt hội viên — :78
    public DateTime? InactiveDTimeUTC { get; set; }         // Thời điểm Inactive hội viên — :80

    public double? PointBegin { get; set; }                 // Điểm đầu kỳ — :82
    public double? PointEnd { get; set; }                   // Điểm cuối kỳ - Điểm tích lũy tiêu dùng — :83

    public double? RevenueMiss { get; set; }                // Doanh thu còn thiếu tăng hạng — :85
    public double? QtyVisitMiss { get; set; }               // Lượt dịch vụ còn thiếu tăng hạng — :86
    public string? cc_FlagShow { get; set; }                // Trạng thái thẻ hội viên — :87
    public string? cm_FlagShow { get; set; }                // Trạng thái hội viên — :91
    public double? AmountEnd { get; set; }                  // Giá trị sử dụng còn lại — :95
    public string? mdcp_DLCPCode { get; set; }              // Mã đại lý đăng ký hội viên — :96
    public string? mdcp_DLCPName { get; set; }              // Tên đại lý đăng ký hội viên — :97

    public string? mcm_ModelName { get; set; }              // Model name lấy từ master — :99

    public double? PointExpiry { get; set; }                // Điểm tiêu dùng hết hạn — :101
    public double? AmountRef { get; set; }                  // Doanh thu xét hạng trong kỳ — :102
    public double? ConvertValue { get; set; }               // Giá trị đổi — :103
    public double? ConvertPoint { get; set; }               // Điểm tương ứng — :104
    public double? PolicyAmountUnit { get; set; }           // :105
    public double? PolicyPointUnit { get; set; }            // :106
    public string? PolicyCode { get; set; }                 // :107
    public string? ParamCode { get; set; }                  // :108
    public string? ParamPointUnit { get; set; }             // :109
    public string? ParamAmountUnit { get; set; }            // :110

    public double? PointAccumulated { get; set; }           // Điểm tích — :112
    public double? PointUse { get; set; }                   // Điểm tiêu — :113

    public string? TradeMarkCode { get; set; }              // Hiệu xe — :115
    public string? DLCodeRequest { get; set; }              // Đại lý yêu cầu — :116
    public double? PointVoucher { get; set; }               // Điểm voucher hiện tại của hội viên — :117
    public string? PurchaseType { get; set; }               // Loại thẻ (Mua mới/Mua thêm xe/Mua lại xe/Đổi xe) — :118
    public string? PurchaseTypeName { get; set; }           // Tên loại thẻ — :119
}

[ApiController]
[Route("api/crd_card")]
public class Crd_CardControllerPort : ControllerBase
{
    private readonly AppDbContext _db;

    public Crd_CardControllerPort(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Tìm kiếm thẻ hội viên phân trang — port 1:1 từ Crd_CardController.Search (Crd_CardController.cs:29-131).
    /// </summary>
    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Crd_CardControllerSearchReq? req)
    {
        req ??= new Crd_CardControllerSearchReq();

        // 1. Xây dựng câu truy vấn lọc draft bám sát Crd_Card_GetX_New20251229 (Master.Loyalty.cs:19502-19525):
        //    cm (Crd_Member) inner join cc (Crd_Card) với cc.CardNo là thẻ mới nhất của hội viên đó
        //    (select top 1 cc.CardNo from Crd_Card cc where cc.MemberNo = cm.MemberNo order by cc.CreateDTimeUTC desc).
        var query = from cm in _db.CrdMembers.AsNoTracking()
                    join cc in _db.CrdCards.AsNoTracking() on cm.MemberNo equals cc.MemberNo
                    where cc.CardNo == _db.CrdCards
                        .Where(c => c.MemberNo == cm.MemberNo)
                        .OrderByDescending(c => c.CreateDTimeUTC)
                        .Select(c => c.CardNo)
                        .FirstOrDefault()
                    select new { cm, cc };

        // 2. Áp dụng các điều kiện lọc từ strWhereClause_Crd_Card (Crd_CardController.cs:586-697):

        // MemberNo: Crd_Member.MemberNo = (:593-596)
        if (!string.IsNullOrWhiteSpace(req.MemberNo))
        {
            var pat = PortQuery.Equal(req.MemberNo);
            query = query.Where(x => EF.Functions.Like((x.cm.MemberNo ?? "").ToLower(), pat, PortQuery.Esc));
        }

        // CarNo: Crd_Member.CarNo = (:601-604)
        if (!string.IsNullOrWhiteSpace(req.CarNo))
        {
            var pat = PortQuery.Equal(req.CarNo);
            query = query.Where(x => EF.Functions.Like((x.cm.CarNo ?? "").ToLower(), pat, PortQuery.Esc));
        }

        // VIN: Crd_Member.VIN = (:605-608)
        if (!string.IsNullOrWhiteSpace(req.VIN))
        {
            var pat = PortQuery.Equal(req.VIN);
            query = query.Where(x => EF.Functions.Like((x.cm.VIN ?? "").ToLower(), pat, PortQuery.Esc));
        }

        // PhoneNo: Crd_Member.PhoneNo = (:609-612)
        if (!string.IsNullOrWhiteSpace(req.PhoneNo))
        {
            var pat = PortQuery.Equal(req.PhoneNo);
            query = query.Where(x => EF.Functions.Like((x.cm.PhoneNo ?? "").ToLower(), pat, PortQuery.Esc));
        }

        // DLCodeRegis: Crd_Member.DLCodeRegis in ('x', 'y') (:617-625)
        if (!string.IsNullOrWhiteSpace(req.DLCodeRegis))
        {
            var codes = req.DLCodeRegis.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                       .Select(s => s.ToLowerInvariant()).ToList();
            if (codes.Count > 0)
            {
                query = query.Where(x => codes.Contains((x.cm.DLCodeRegis ?? "").ToLower()));
            }
        }

        // DLCPCode: Map_QueryDealer_Member.DLCPCode = (:626-629)
        if (!string.IsNullOrWhiteSpace(req.DLCPCode))
        {
            var patDLCP = PortQuery.Equal(req.DLCPCode);
            query = query.Where(x => _db.Set<Map_QueryDealer_Member>().Any(m => m.MemberNo == x.cm.MemberNo && EF.Functions.Like((m.DLCPCode ?? "").ToLower(), patDLCP, PortQuery.Esc)));
        }

        // CardStatus: Crd_Member.CardStatus in ('x', 'y') (:634-642)
        // NGUỒN chỉ lọc trên Crd_Member.CardStatus (Tbl_Crd_Member + TblCrd_Member.CardStatus) — KHÔNG lọc cc.CardStatus.
        if (!string.IsNullOrWhiteSpace(req.CardStatus))
        {
            var statuses = req.CardStatus.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                         .Select(s => s.ToLowerInvariant()).ToList();
            if (statuses.Count > 0)
            {
                query = query.Where(x => statuses.Contains((x.cm.CardStatus ?? "").ToLower()));
            }
        }

        // MemberActiveDateFrom / To: Crd_Member.MemberActiveDate >= và <= (:643-650)
        // Gate chuẩn hoá input bằng StdDate (Crd_CardController.cs:59-60) => 'yyyy-MM-dd' (nửa đêm), KHÔNG mở rộng cuối ngày.
        if (!string.IsNullOrWhiteSpace(req.MemberActiveDateFrom) && DateTime.TryParse(req.MemberActiveDateFrom, out var dFrom))
        {
            query = query.Where(x => x.cm.MemberActiveDate >= dFrom.Date);
        }
        if (!string.IsNullOrWhiteSpace(req.MemberActiveDateTo) && DateTime.TryParse(req.MemberActiveDateTo, out var dTo))
        {
            query = query.Where(x => x.cm.MemberActiveDate <= dTo.Date);
        }

        // EffDateStartFrom / To: Crd_Card.ApproveDTimeUTC >= và <= (:651-664)
        // StdDTimeBeginDay/StdDTimeEndDay -> StdDTimeUTC: đầu ngày VN (UTC+7) và cuối ngày VN đổi sang UTC.
        if (!string.IsNullOrWhiteSpace(req.EffDateStartFrom) && DateTime.TryParse(req.EffDateStartFrom, out var effFrom))
        {
            var effFromUtc = effFrom.Date.AddHours(-7); // Đầu ngày VN (UTC+7) chuyển sang UTC
            query = query.Where(x => x.cc.ApproveDTimeUTC >= effFromUtc);
        }
        if (!string.IsNullOrWhiteSpace(req.EffDateStartTo) && DateTime.TryParse(req.EffDateStartTo, out var effTo))
        {
            var effToUtc = effTo.Date.AddDays(1).AddSeconds(-1).AddHours(-7); // Cuối ngày VN chuyển sang UTC
            query = query.Where(x => x.cc.ApproveDTimeUTC <= effToUtc);
        }

        // CardSourceCode: Crd_Member.CardSourceCode = (:665-668)
        if (!string.IsNullOrWhiteSpace(req.CardSourceCode))
        {
            var pat = PortQuery.Equal(req.CardSourceCode);
            query = query.Where(x => EF.Functions.Like((x.cm.CardSourceCode ?? "").ToLower(), pat, PortQuery.Esc));
        }

        // FlagExceptionally: Crd_Member.FlagExceptionCard = (:669-672)
        // NGUỒN chỉ dùng cột Crd_Member.FlagExceptionCard — KHÔNG fallback sang FlagExceptionally.
        if (!string.IsNullOrWhiteSpace(req.FlagExceptionally))
        {
            var pat = PortQuery.Equal(req.FlagExceptionally);
            query = query.Where(x => EF.Functions.Like((x.cm.FlagExceptionCard ?? "").ToLower(), pat, PortQuery.Esc));
        }

        // InactiveDTimeFrom / To: Crd_Member.InactiveDTimeUTC >= và <= (:673-680)
        // NGUỒN dùng thẳng CUtils.StrValue(input) với >= / <= — KHÔNG chuẩn hoá đầu/cuối ngày.
        if (!string.IsNullOrWhiteSpace(req.InactiveDTimeFrom) && DateTime.TryParse(req.InactiveDTimeFrom, out var inactFrom))
        {
            query = query.Where(x => x.cm.InactiveDTimeUTC >= inactFrom);
        }
        if (!string.IsNullOrWhiteSpace(req.InactiveDTimeTo) && DateTime.TryParse(req.InactiveDTimeTo, out var inactTo))
        {
            query = query.Where(x => x.cm.InactiveDTimeUTC <= inactTo);
        }

        // CardTypeUse: Crd_Member.CardTypeUse = (:681-684)
        if (!string.IsNullOrWhiteSpace(req.CardTypeUse))
        {
            var pat = PortQuery.Equal(req.CardTypeUse);
            query = query.Where(x => EF.Functions.Like((x.cm.CardTypeUse ?? "").ToLower(), pat, PortQuery.Esc));
        }

        // MemberStatus: Crd_Member.MemberStatus in ('x', 'y') (:685-693)
        if (!string.IsNullOrWhiteSpace(req.MemberStatus))
        {
            var mStatuses = req.MemberStatus.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                            .Select(s => s.ToLowerInvariant()).ToList();
            if (mStatuses.Count > 0)
            {
                query = query.Where(x => mStatuses.Contains((x.cm.MemberStatus ?? "").ToLower()));
            }
        }

        // 3. Đếm tổng số bản ghi (MyCount — Master.Loyalty.cs:19528)
        var itemCount = await query.CountAsync();

        // 4. Xử lý phân trang theo Bài học A.12 và A.7:
        var pageSize = req.Ft_PageSize;
        var pageIndex = req.Ft_PageIndex;
        var ps = pageSize <= 0 ? 10 : pageSize;
        var pageCount = pageSize <= 0 ? 0 : (itemCount % ps == 0 ? itemCount / ps : itemCount / ps + 1);

        // Bài học A.7: Ft_PageSize = 0 -> DataList rỗng, PageCount = 0, ItemCount giữ nguyên
        if (pageSize <= 0 || pageIndex < 0 || itemCount == 0)
        {
            var emptyResult = new PageInfo<Crd_CardSearchItem>(
                new List<Crd_CardSearchItem>(),
                itemCount,
                pageSize == 0 ? 0 : pageCount,
                pageIndex,
                pageSize
            );
            return Ok(new { Success = true, Data = emptyResult });
        }

        // Phân trang và sắp xếp theo cc.CardNo desc (Master.Loyalty.cs:19793)
        long recordStart = (long)pageIndex * pageSize;
        var pagedPairs = await query
            .OrderByDescending(x => x.cc.CardNo)
            .Skip((int)Math.Min(recordStart, int.MaxValue))
            .Take((int)Math.Min(pageSize, int.MaxValue))
            .Select(x => new { Member = x.cm, Card = x.cc })
            .ToListAsync();

        if (pagedPairs.Count == 0)
        {
            var emptyResult = new PageInfo<Crd_CardSearchItem>(
                new List<Crd_CardSearchItem>(),
                itemCount,
                pageCount,
                pageIndex,
                pageSize
            );
            return Ok(new { Success = true, Data = emptyResult });
        }

        // 5. Nạp dữ liệu các bảng tra cứu liên quan cho các bản ghi trong trang hiện tại (Master.Loyalty.cs:19556-19794):
        var memberNos = pagedPairs.Select(p => p.Member.MemberNo).Distinct().ToList();
        var cardNos = pagedPairs.Select(p => p.Card.CardNo).Distinct().ToList();

        // 5.1 RptPE_CrdMemberPointExpiryMember -> Sum(PointExpiry) group by MemberNo (:19556-19566)
        var expiryMap = await _db.Set<RptPE_CrdMemberPointExpiryMember>()
            .Where(x => memberNos.Contains(x.MemberNo))
            .GroupBy(x => x.MemberNo)
            .Select(g => new { MemberNo = g.Key, PointExpiry = g.Sum(x => x.PointExpiry) })
            .ToDictionaryAsync(x => x.MemberNo, x => x.PointExpiry);

        // 5.2 Crd_CardTransaction -> Sum(AmountRef) theo POINTINCREASE (:19625-19635)
        var amountRefMap = await _db.CrdCardTransactions
            .Where(x => cardNos.Contains(x.CardNo) && x.DealPointType == "POINTINCREASE")
            .GroupBy(x => x.CardNo)
            .Select(g => new { CardNo = g.Key, AmountRef = g.Sum(x => x.AmountRef) })
            .ToDictionaryAsync(x => x.CardNo, x => x.AmountRef);

        // 5.3 Crd_CardTransaction -> Sum(AmountChTotal) PointUse theo POINTUSE (:19639-19649)
        var pointUseMap = await _db.CrdCardTransactions
            .Where(x => cardNos.Contains(x.CardNo) && x.DealPointType == "POINTUSE")
            .GroupBy(x => x.CardNo)
            .Select(g => new { CardNo = g.Key, PointUse = g.Sum(x => x.AmountChTotal) })
            .ToDictionaryAsync(x => x.CardNo, x => x.PointUse);

        // 5.4 Mst_Province (:19773-19774)
        var provinceCodes = pagedPairs.Select(p => p.Member.ProvinceCode).Where(c => !string.IsNullOrEmpty(c)).Distinct().ToList();
        var provinceMap = await _db.Set<Mst_Province>()
            .Where(x => provinceCodes.Contains(x.ProvinceCode))
            .ToDictionaryAsync(x => x.ProvinceCode, x => x.ProvinceName);

        // 5.5 Mst_District (:19775-19777)
        var districtMap = await _db.Set<Mst_District>()
            .Where(x => provinceCodes.Contains(x.ProvinceCode))
            .ToDictionaryAsync(x => $"{x.ProvinceCode}_{x.DistrictCode}", x => x.DistrictName);

        // 5.6 Mst_CarModel & Mst_CarBrand (:19778-19781)
        var modelCodes = pagedPairs.Select(p => p.Member.ModelCode).Where(c => !string.IsNullOrEmpty(c)).Distinct().ToList();
        var carModels = await _db.Set<Mst_CarModel>()
            .Where(x => modelCodes.Contains(x.ModelCode))
            .ToListAsync();
        var modelMap = carModels.ToDictionary(x => x.ModelCode, x => x);
        var brandCodes = carModels.Select(m => m.BrandCode).Where(c => !string.IsNullOrEmpty(c)).Distinct().ToList();
        var brandMap = await _db.Set<Mst_CarBrand>()
            .Where(x => brandCodes.Contains(x.BrandCode))
            .ToDictionaryAsync(x => x.BrandCode, x => x.BrandName);

        // 5.7 Mst_DealerCP (:19782-19783)
        var dlCodes = pagedPairs.Select(p => p.Member.DLCodeRegis).Where(c => !string.IsNullOrEmpty(c)).Distinct().ToList();
        var dealerMap = await _db.Set<Mst_DealerCP>()
            .Where(x => dlCodes.Contains(x.DLCPCode))
            .ToDictionaryAsync(x => x.DLCPCode, x => x);

        // 5.8 Mst_PurchaseType (:19763-19764)
        var purchaseTypes = pagedPairs.Select(p => p.Member.PurchaseType).Where(c => !string.IsNullOrEmpty(c)).Distinct().ToList();
        var purchaseTypeMap = await _db.Set<Mst_PurchaseType>()
            .Where(x => purchaseTypes.Contains(x.PurchaseType))
            .ToDictionaryAsync(x => x.PurchaseType, x => x.PurchaseTypeName);

        // 5.9 Mst_RankPolicy (:19784-19785)
        var cardTypeUses = pagedPairs.Select(p => p.Member.CardTypeUse).Where(c => !string.IsNullOrEmpty(c)).Distinct().ToList();
        var rankPolicyMap = await _db.Set<Mst_RankPolicy>()
            .Where(x => cardTypeUses.Contains(x.CardType))
            .GroupBy(x => x.CardType)
            .Select(g => g.First())
            .ToDictionaryAsync(x => x.CardType, x => x);

        // 5.10 Mst_PolicyMoneyToPointServiceDtl (:19786-19788)
        var policyDtlMap = await _db.Set<Mst_PolicyMoneyToPointServiceDtl>()
            .Where(x => cardTypeUses.Contains(x.CardType) && x.FlagActive == "1")
            .GroupBy(x => x.CardType)
            .Select(g => g.First())
            .ToDictionaryAsync(x => x.CardType, x => x);

        // 5.11 Mst_ParamSys UNITPOINTTOMONEY (:19789-19792)
        var paramSys = await _db.Set<Mst_ParamSys>()
            .FirstOrDefaultAsync(x => x.ParamCode == "UNITPOINTTOMONEY");

        // 6. Ánh xạ dữ liệu sang model Crd_CardSearchItem kết hợp các phép tính toán:
        var dataList = new List<Crd_CardSearchItem>();
        foreach (var pair in pagedPairs)
        {
            var cm = pair.Member;
            var cc = pair.Card;

            // EffDateStart: nếu có ApproveDTimeUTC thì chuyển sang giờ VN (UTC+7) định dạng yyyy-MM-dd (Master.Loyalty.cs:18890-18900)
            string? effDateStart = null;
            if (cc.ApproveDTimeUTC.HasValue)
            {
                effDateStart = cc.ApproveDTimeUTC.Value.AddHours(7).ToString("yyyy-MM-dd");
            }
            else if (cc.EffDateStart.HasValue)
            {
                effDateStart = cc.EffDateStart.Value.ToString("yyyy-MM-dd");
            }

            // cm_FlagShow: PENDING -> '-1', APPROVE -> '1', CANCEL -> '0' (:19704-19710)
            string? cm_FlagShow = cm.MemberStatus?.ToUpperInvariant() switch
            {
                "PENDING" => "-1",
                "APPROVE" => "1",
                "CANCEL" => "0",
                _ => null
            };

            // cc_FlagShow: PENDING -> '-1', APPROVE -> '1', CANCEL -> '0' (:19711-19717)
            string? cc_FlagShow = cc.CardStatus?.ToUpperInvariant() switch
            {
                "PENDING" => "-1",
                "APPROVE" => "1",
                "CANCEL" => "0",
                _ => null
            };

            // QtyVisitMiss: (:19721-19729)
            // (mrp.QtyVisitUpBegin - cc.QtyVisitAvail) >= 0 ? diff : 0
            double? qtyVisitMiss = 0;
            if (rankPolicyMap.TryGetValue(cm.CardTypeUse ?? "", out var mrp))
            {
                var diff = (mrp.QtyVisitUpBegin ?? 0) - (cc.QtyVisitAvail ?? 0);
                qtyVisitMiss = diff >= 0 ? diff : 0;
            }

            // RevenueMiss: (:19736-19744)
            // if ConvertPoint = 0 or ConvertValue = 0 -> 0; if (PointUpBegin - PointCardRank) >= 0 -> Round(diff / (cp/cv), 2) else 0
            double? revenueMiss = 0;
            policyDtlMap.TryGetValue(cm.CardTypeUse ?? "", out var mpmtpsd);
            if (mpmtpsd != null && mpmtpsd.ConvertPoint.GetValueOrDefault() != 0 && mpmtpsd.ConvertValue.GetValueOrDefault() != 0 && mrp != null)
            {
                var diffPoint = (mrp.PointUpBegin ?? 0) - (cc.PointCardRank ?? 0);
                if (diffPoint >= 0)
                {
                    var rate = mpmtpsd.ConvertPoint!.Value / mpmtpsd.ConvertValue!.Value;
                    revenueMiss = Math.Round(diffPoint / rate, 2);
                }
            }

            // Master lookup values:
            provinceMap.TryGetValue(cm.ProvinceCode ?? "", out var provinceName);
            districtMap.TryGetValue($"{cm.ProvinceCode}_{cm.DistrictCode}", out var districtName);
            modelMap.TryGetValue(cm.ModelCode ?? "", out var carModel);
            string? brandName = null;
            if (carModel != null && !string.IsNullOrEmpty(carModel.BrandCode))
            {
                brandMap.TryGetValue(carModel.BrandCode, out brandName);
            }
            dealerMap.TryGetValue(cm.DLCodeRegis ?? "", out var dealer);
            purchaseTypeMap.TryGetValue(cm.PurchaseType ?? "", out var purchaseTypeName);
            expiryMap.TryGetValue(cm.MemberNo, out var pointExpiry);
            amountRefMap.TryGetValue(cc.CardNo, out var amountRef);
            pointUseMap.TryGetValue(cc.CardNo, out var pointUse);

            var item = new Crd_CardSearchItem
            {
                CardNo = cc.CardNo,
                CardNoPrev = cc.CardNoPrev,
                NetworkID = cc.NetworkID,
                MemberNo = cc.MemberNo,
                CardSourceCode = cc.CardSourceCode,
                RankPolicyCode = cc.RankPolicyCode,
                DLCodeExceptionally = cc.DLCodeExceptionally,
                PointBonus = cc.PointBonus,
                PointPurchaseAddCar = cc.PointPurchaseAddCar,
                EffDateStart = effDateStart,
                EffDateEnd = cc.EffDateEnd,
                PointTotal = cc.PointTotal,
                PointBlock = cc.PointBlock,
                PointAvail = cc.PointAvail,
                AmountTotal = cc.AmountTotal,
                AmountBlock = cc.AmountBlock,
                AmountAvail = cc.AmountAvail,
                QtyVisitTotal = cc.QtyVisitTotal,
                QtyVisitBlock = cc.QtyVisitBlock,
                QtyVisitAvail = cc.QtyVisitAvail,
                PointCardRank = cc.PointCardRank,
                TotalAmountPeriod = cc.TotalAmountPeriod,
                CardTypeUse = cc.CardTypeUse,
                CardTypeInit = cc.CardTypeInit,
                CardTypeUsePrev = cc.CardTypeUsePrev,
                CardTypeInitPrev = cc.CardTypeInitPrev,
                CardActiveDate = cc.CardActiveDate,
                FlagExceptionally = cc.FlagExceptionally,
                CardTypeUseExpect = cc.CardTypeUseExpect,
                CardTypeInitExpect = cc.CardTypeInitExpect,
                RankActionType = cc.RankActionType,
                CreateDTimeUTC = cc.CreateDTimeUTC,
                CreateBy = cc.CreateBy,
                LUDTimeUTC = cc.LUDTimeUTC,
                LUBy = cc.LUBy,
                ApproveDTimeUTC = cc.ApproveDTimeUTC,
                ApproveBy = cc.ApproveBy,
                CancelDTimeUTC = cc.CancelDTimeUTC,
                CancelBy = cc.CancelBy,
                CardStatus = cc.CardStatus,
                Remark = cc.Remark,
                LogLUDTimeUTC = cc.LogLUDTimeUTC,
                LogLUBy = cc.LogLUBy,

                MemberName = cm.MemberName,
                MemberStatus = cm.MemberStatus,
                IDCardNo = cm.IDCardNo,
                MemberGender = cm.MemberGender,
                MemAddress = cm.MemAddress,
                DateOfBirth = cm.DateOfBirth,
                PhoneNo = cm.PhoneNo,
                CarNo = cm.CarNo,
                VIN = cm.VIN,
                ModelCode = cm.ModelCode,
                ProvinceCode = cm.ProvinceCode,
                DistrictCode = cm.DistrictCode,
                WarrantyExpiryDate = cm.WarrantyExpiryDate,
                InvoiceDate = cm.InvoiceDate,
                MemberActiveDate = cm.MemberActiveDate,
                InactiveDTimeUTC = cm.InactiveDTimeUTC,
                PointBegin = cm.PointBegin,
                PointEnd = cm.PointEnd,
                AmountEnd = cm.AmountEnd,
                PointVoucher = cm.PointVoucher,
                PurchaseType = cm.PurchaseType,
                TradeMarkCode = cm.TradeMarkCode,
                DLCodeRequest = cm.DLCodeRegis,

                ProvinceName = provinceName,
                DistrictName = districtName,
                ModelName = carModel?.ModelName,
                mcm_ModelName = carModel?.ModelName,
                BrandName = brandName,
                mdcp_DLCPCode = dealer?.DLCPCode,
                mdcp_DLCPName = dealer?.DLCPName,
                PurchaseTypeName = purchaseTypeName,

                cm_FlagShow = cm_FlagShow,
                cc_FlagShow = cc_FlagShow,
                PointExpiry = pointExpiry ?? 0.0,
                QtyVisitMiss = qtyVisitMiss,
                RevenueMiss = revenueMiss,
                AmountRef = amountRef ?? 0.0,
                ConvertValue = mpmtpsd?.ConvertValue,
                ConvertPoint = mpmtpsd?.ConvertPoint,
                PolicyAmountUnit = mpmtpsd?.ConvertValue ?? 0.0,
                PolicyPointUnit = mpmtpsd?.ConvertPoint ?? 0.0,
                PolicyCode = mpmtpsd?.PolicyCode,
                ParamCode = paramSys?.ParamCode ?? "UNITPOINTTOMONEY",
                ParamPointUnit = "1",
                ParamAmountUnit = "1",
                PointAccumulated = cc.PointTotal, // Điểm tích tiêu dùng trong kỳ hiện tại (:19756)
                PointUse = pointUse ?? 0.0
            };

            dataList.Add(item);
        }

        var result = new PageInfo<Crd_CardSearchItem>(
            dataList,
            itemCount,
            pageCount,
            pageIndex,
            pageSize
        );

        return Ok(new { Success = true, Data = result });
    }
}
