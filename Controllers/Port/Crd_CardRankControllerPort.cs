using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MiniLoyalty.Data;
using MiniLoyalty.Models;
using MiniLoyalty.Models.Port;

namespace MiniLoyalty.Controllers.Port;

// ============================================================================================
// PORT 1:1 — Crd_CardRankController/Search (#007) và SearchForMember (#008) từ dự án nguồn 2022.7.Loyalty.
//
// Chuỗi gọi SearchForMember (#008):
//   Gate: Crd_CardRankController.cs:97-183 SearchForMember -> Crd_CardRankService.WA_Crd_CardRank_Get
//   API:  CrdCardRankController.cs:23-90 WA_Crd_CardRank_Get -> _biz.WAS_Crd_CardRank_Get
//   Biz:  CardRank.cs:658-842 WAS_Crd_CardRank_Get -> Crd_CardRank_Get (:494-656) -> Crd_CardRank_GetX (:156-492)
//   SQL:  CardRank.cs:214-383 (#tbl_Crd_CardRank_Filter_Draft, #tbl_Crd_CardRank_Filter)
//
// ĐẶC ĐIỂM NGHIỆP VỤ NGUỒN:
//   1. Gate nhận: IDCardNo, CardNo, EffDateStartTo, EffDateStartFrom, MemberNo, DLCode (:97).
//   2. strWhereClause_Crd_CardRank (:424-469):
//      - CardNo: Crd_Card.CardNo = CardNo (=)
//      - IDCardNo: Crd_Member.IDCardNo = IDCardNo (=)
//      - EffDateStartFrom: Crd_CardAfter.CreateDTimeUTC >= StdDTimeUTC(StdDTimeBeginDay(EffDateStartFrom)) (>=)
//      - EffDateStartTo: Crd_CardAfter.CreateDTimeUTC <= StdDTimeUTC(StdDTimeEndDay(EffDateStartTo)) (<=)
//      - MemberNo: Crd_CardAfter.MemberNo = MemberNo (=) (chú ý: lọc trên Crd_CardAfter, không phải Crd_Member)
//      - DLCode: Crd_Member.DLCodeRegis = DLCode (=)
//   3. Gate cố định: Ft_RecordStart = '0', Ft_RecordCount = '6400',
//      Rt_Cols_Crd_CardRank = '*', Rt_Cols_Crd_CardBefore = '*', Rt_Cols_Crd_CardAfter = '*' (:140-145).
//      Rt_Cols_Crd_MemberBefore và Rt_Cols_Crd_MemberAfter không truyền (null).
//   4. Trả về: Data = { Lst_Crd_CardRank, Lst_Crd_CardBefore, Lst_Crd_CardAfter } (rỗng -> null) (:165-170).
//      Crd_CardAfter được join với Crd_CardBefore để lấy PointCardRankPrev và QtyVisitTotalPrev (:1076-1077).
//   5. Transaction: Nguồn mở transaction và luôn RollbackSafety (:538, :619, :627, :642).
//   6. GAP đã khai báo: bảng tạm #tbl_Crd_CardRank_Filter_Draft chuyển đổi sang LINQ;
//      bỏ qua Sys_User_ForCP_GetAbilityViewOfUser do không có session người dùng.
// ============================================================================================

/// <summary>
/// Port 1:1 tham số request của Gate SearchForMember (Crd_CardRankController.cs:97).
/// </summary>
public class Crd_CardRankSearchForMemberReq
{
    public string? CardNo { get; set; }              // Mã thẻ hội viên — :97
    public string? IDCardNo { get; set; }            // Số CMND/CCCD — :97
    public string? EffDateStartFrom { get; set; }    // Ngày xét hạng từ — :97
    public string? EffDateStartTo { get; set; }      // Ngày xét hạng đến — :97
    public string? MemberNo { get; set; }            // Mã hội viên (lọc Crd_CardAfter.MemberNo) — :97, :460
    public string? DLCode { get; set; }              // Mã đại lý (lọc Crd_Member.DLCodeRegis) — :97, :464
    public string? NetworkID { get; set; }           // Phân vùng mạng (tùy chọn)
    public string? OrgID { get; set; }               // Tổ chức (tùy chọn)
}

/// <summary>
/// Port 1:1 tham số request của Gate Search (Crd_CardRankController.cs:30).
/// </summary>
public class Crd_CardSearchReq
{
    public string? EffDateEnd { get; set; }          // Ngày hết hạn kỳ xét hạng — :30
    public string? MemberNo { get; set; }            // Mã hội viên — :30
    public string? CardTypeUse { get; set; }         // Hạng sử dụng — :30
    public string? CardNo { get; set; }              // Mã thẻ — :30
    public string? CarNo { get; set; }               // Biển số xe — :30
    public string? PhoneNo { get; set; }             // Số điện thoại — :30
    public string? VIN { get; set; }                 // Số khung — :30
    public string? RankActionType { get; set; }      // Loại hành động hạng: KEEP/DOWN — :30
    public string? FlagExceptionally { get; set; }   // Cờ đặc cách — :30
    public string? CardStatus { get; set; }          // Trạng thái thẻ: Gate nguồn cố định APPROVE (:70), nhận để khớp chữ ký
    public string? NetworkID { get; set; }           // Phân vùng mạng (tùy chọn)
    public string? OrgID { get; set; }               // Tổ chức (tùy chọn)
}

/// <summary>
/// Port 1:1 tham số request của Gate ApproveRankDown (Crd_CardRankController.cs:187) (#009).
/// </summary>
public class Crd_CardRankApproveRankDownReq
{
    public string? strJson { get; set; }
    public List<Crd_CardRankApproveRankDownItem>? Lst_Crd_Card { get; set; }
}

public class Crd_CardRankApproveRankDownItem
{
    public string? CardNo { get; set; }
    public string? CardTypeUseExpect { get; set; }
    public string? CardTypeInitExpect { get; set; }
}

/// <summary>
/// Port 1:1 tham số request của Gate ApproveRankKeep (Crd_CardRankController.cs:251) (#010).
/// </summary>
public class Crd_CardRankApproveRankKeepReq
{
    public string? strJson { get; set; }
    public List<Crd_CardRankApproveRankKeepItem>? Lst_Crd_Card { get; set; }
}

public class Crd_CardRankApproveRankKeepItem
{
    public string? CardNo { get; set; }
    public string? CardTypeUseExpect { get; set; }
    public string? CardTypeInitExpect { get; set; }
    public string? RankActionType { get; set; }
}

[ApiController]
[Route("api/crd_cardrank")]
public class Crd_CardRankControllerPort : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public Crd_CardRankControllerPort(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    /// <summary>
    /// Port 1:1 Crd_CardRankController.Search (gate Crd_CardRankController.cs:30-95 -> Biz CardRank.cs:7065-7570).
    /// Thực hiện lọc và tính toán xếp hạng thẻ hội viên.
    /// </summary>
    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Crd_CardSearchReq? req)
    {
        var r = req ?? new Crd_CardSearchReq();

        // Chuẩn hóa tham số theo nguồn (TUtils.CUtils — CardRank.cs:7259-7269)
        DateTime? effDateEnd = null;
        if (!string.IsNullOrWhiteSpace(r.EffDateEnd) && DateTime.TryParse(r.EffDateEnd, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dtParsed))
        {
            effDateEnd = dtParsed.Date;
        }

        var memberNo = string.IsNullOrWhiteSpace(r.MemberNo) ? null : r.MemberNo.Trim().ToUpperInvariant();
        var cardTypeUse = string.IsNullOrWhiteSpace(r.CardTypeUse) ? null : r.CardTypeUse.Trim().ToUpperInvariant();
        var cardNo = string.IsNullOrWhiteSpace(r.CardNo) ? null : r.CardNo.Trim().ToUpperInvariant();
        var carNo = string.IsNullOrWhiteSpace(r.CarNo) ? null : r.CarNo.Trim().ToUpperInvariant();
        var vin = string.IsNullOrWhiteSpace(r.VIN) ? null : r.VIN.Trim().ToUpperInvariant();
        var phoneNo = string.IsNullOrWhiteSpace(r.PhoneNo) ? null : r.PhoneNo.Trim();
        var flagExceptionally = string.IsNullOrWhiteSpace(r.FlagExceptionally) ? null : r.FlagExceptionally.Trim().ToUpperInvariant();
        var rankActionType = string.IsNullOrWhiteSpace(r.RankActionType) ? null : r.RankActionType.Trim().ToUpperInvariant();
        var networkId = string.IsNullOrWhiteSpace(r.NetworkID) ? null : r.NetworkID.Trim().ToUpperInvariant();

        // 1. Lọc Crd_Member: CardRank.cs:7290-7305 (#tbl_Crd_Member)
        // Lưu ý: bảng #tbl_Crd_Rank_ViewAbility theo người dùng đăng nhập bị bỏ qua vì không có session (ghi GAP).
        var memberQuery = _db.CrdMembers.AsQueryable();

        // cm.EffDateEnd = @strEffDateEnd (CardRank.cs:7299, so sánh ngày)
        if (effDateEnd.HasValue)
        {
            memberQuery = memberQuery.Where(cm => cm.EffDateEnd.HasValue && cm.EffDateEnd.Value.Date == effDateEnd.Value);
        }

        // cm.MemberNo = @strMemberNo (CardRank.cs:7300)
        if (!string.IsNullOrWhiteSpace(memberNo))
        {
            var pattern = PortQuery.Equal(memberNo);
            memberQuery = memberQuery.Where(cm => EF.Functions.Like((cm.MemberNo ?? "").ToLower(), pattern, PortQuery.Esc));
        }

        // cm.CarNo = @strCarNo (CardRank.cs:7301)
        if (!string.IsNullOrWhiteSpace(carNo))
        {
            var pattern = PortQuery.Equal(carNo);
            memberQuery = memberQuery.Where(cm => EF.Functions.Like((cm.CarNo ?? "").ToLower(), pattern, PortQuery.Esc));
        }

        // cm.VIN = @strVIN (CardRank.cs:7302)
        if (!string.IsNullOrWhiteSpace(vin))
        {
            var pattern = PortQuery.Equal(vin);
            memberQuery = memberQuery.Where(cm => EF.Functions.Like((cm.VIN ?? "").ToLower(), pattern, PortQuery.Esc));
        }

        // cm.PhoneNo = @strPhoneNo (CardRank.cs:7303)
        if (!string.IsNullOrWhiteSpace(phoneNo))
        {
            var pattern = PortQuery.Equal(phoneNo);
            memberQuery = memberQuery.Where(cm => EF.Functions.Like((cm.PhoneNo ?? "").ToLower(), pattern, PortQuery.Esc));
        }

        // cm.FlagExceptionally = @strFlagExceptionally (CardRank.cs:7304)
        if (!string.IsNullOrWhiteSpace(flagExceptionally))
        {
            var pattern = PortQuery.Equal(flagExceptionally);
            memberQuery = memberQuery.Where(cm => EF.Functions.Like((cm.FlagExceptionally ?? "").ToLower(), pattern, PortQuery.Esc));
        }

        // 2. Lọc Crd_Card: CardRank.cs:7310-7325 (#tbl_Crd_Card_Approve)
        // Gate Crd_CardRankController.cs:70: CardStatus LUÔN = 'APPROVE', không nhận từ client
        var cardQuery = _db.CrdCards.AsQueryable();
        {
            var approvePattern = PortQuery.Equal("APPROVE");
            cardQuery = cardQuery.Where(cc => EF.Functions.Like((cc.CardStatus ?? "").ToLower(), approvePattern, PortQuery.Esc));
        }

        // cc.CardNo = @strCardNo (CardRank.cs:7318)
        if (!string.IsNullOrWhiteSpace(cardNo))
        {
            var pattern = PortQuery.Equal(cardNo);
            cardQuery = cardQuery.Where(cc => EF.Functions.Like((cc.CardNo ?? "").ToLower(), pattern, PortQuery.Esc));
        }

        // cc.CardTypeUse = @strCardTypeUse (CardRank.cs:7319)
        if (!string.IsNullOrWhiteSpace(cardTypeUse))
        {
            var pattern = PortQuery.Equal(cardTypeUse);
            cardQuery = cardQuery.Where(cc => EF.Functions.Like((cc.CardTypeUse ?? "").ToLower(), pattern, PortQuery.Esc));
        }

        // Lọc NetworkID (nếu có truyền)
        if (!string.IsNullOrWhiteSpace(networkId))
        {
            var pattern = PortQuery.Equal(networkId);
            cardQuery = cardQuery.Where(cc => EF.Functions.Like((cc.NetworkID ?? "").ToLower(), pattern, PortQuery.Esc));
        }

        // 3. Kết nối Crd_Card và Crd_Member: CardRank.cs:7315-7316 (cc.MemberNo = cm.MemberNo)
        var joinedQuery = from cc in cardQuery
                          join cm in memberQuery on cc.MemberNo equals cm.MemberNo
                          select new { cc, cm };

        var rawList = await joinedQuery.ToListAsync();

        // 4. Lấy danh sách Mst_RankPolicy phục vụ tính toán xếp hạng (CardRank.cs:7329-7455)
        var policies = await _db.Set<Mst_RankPolicy>().ToListAsync();

        var resultList = new List<Crd_Card>();

        foreach (var item in rawList.OrderBy(x => x.cc.CardNo))
        {
            var cc = item.cc;
            var cm = item.cm;

            // mrpUse: CardRank.cs:7363-7364
            // inner join Mst_RankPolicy mrp on cc.CardTypeUse = mrp.CardType
            var mrpUse = policies.FirstOrDefault(p =>
                string.Equals(p.CardType, cc.CardTypeUse, StringComparison.OrdinalIgnoreCase)
                && (string.IsNullOrEmpty(cc.RankPolicyCode) || string.Equals(p.RankPolicyCode, cc.RankPolicyCode, StringComparison.OrdinalIgnoreCase)));

            // Nguồn inner join: nếu không tìm thấy chính sách phù hợp cho CardTypeUse thì bỏ qua (CardRank.cs:7363)
            if (mrpUse == null) continue;

            // CardTypeUseExpect: CardRank.cs:7334-7348
            // case when ((cm.PointCardRank >= mrp.PointKeepBegin) and (cm.QtyVisitAvail >= mrp.QtyVisitKeepBegin)) then cc.CardTypeUse
            // else IsNull((select top 1 mrp_t.CardType from Mst_RankPolicy mrp_t where mrp_t.RankPolicyCode = mrp.RankPolicyCode and mrp_t.Value < mrp.Value order by mrp_t.Value desc), cm.CardTypeUse) end
            string? cardTypeUseExpect;
            var ptRank = cm.PointCardRank ?? 0;
            var qtyVisit = cm.QtyVisitAvail ?? 0;
            if (ptRank >= (mrpUse.PointKeepBegin ?? 0) && qtyVisit >= (mrpUse.QtyVisitKeepBegin ?? 0))
            {
                cardTypeUseExpect = cc.CardTypeUse;
            }
            else
            {
                var lower = policies
                    .Where(p => string.Equals(p.RankPolicyCode, mrpUse.RankPolicyCode, StringComparison.OrdinalIgnoreCase)
                             && p.Value < mrpUse.Value)
                    .OrderByDescending(p => p.Value)
                    .FirstOrDefault();
                cardTypeUseExpect = lower?.CardType ?? cm.CardTypeUse ?? cc.CardTypeUse;
            }

            // mrpInit: CardRank.cs:7404-7405
            // inner join Mst_RankPolicy mrp on cc.CardTypeInit = mrp.CardType
            var mrpInit = policies.FirstOrDefault(p =>
                string.Equals(p.CardType, cc.CardTypeInit, StringComparison.OrdinalIgnoreCase)
                && (string.IsNullOrEmpty(cc.RankPolicyCode) || string.Equals(p.RankPolicyCode, cc.RankPolicyCode, StringComparison.OrdinalIgnoreCase)));

            // CardTypeInitExpect: CardRank.cs:7376-7390
            string? initExpect;
            if (mrpInit != null)
            {
                if (ptRank >= (mrpInit.PointKeepBegin ?? 0) && qtyVisit >= (mrpInit.QtyVisitKeepBegin ?? 0))
                {
                    initExpect = cc.CardTypeInit;
                }
                else
                {
                    var lower = policies
                        .Where(p => string.Equals(p.RankPolicyCode, mrpInit.RankPolicyCode, StringComparison.OrdinalIgnoreCase)
                                 && p.Value < mrpInit.Value)
                        .OrderByDescending(p => p.Value)
                        .FirstOrDefault();
                    initExpect = lower?.CardType ?? cm.CardTypeInit ?? cc.CardTypeInit;
                }
            }
            else
            {
                initExpect = cm.CardTypeInit ?? cc.CardTypeInit;
            }

            // RankActionType: CardRank.cs:7416-7419
            // when t.CardTypeUse != t.CardTypeUseExpect then 'DOWN' else 'KEEP'
            string calcRankActionType = !string.Equals(cc.CardTypeUse, cardTypeUseExpect, StringComparison.OrdinalIgnoreCase)
                ? "DOWN"
                : "KEEP";

            // CardTypeInitExpect cuối cùng: CardRank.cs:7437-7440
            // when t.RankActionType = 'KEEP' then t.CardTypeUseExpect
            // when t.RankActionType = 'DOWN' then f.CardTypeInitExpect
            string? finalCardTypeInitExpect = (calcRankActionType == "KEEP")
                ? cardTypeUseExpect
                : initExpect;

            // Lọc RankActionType nếu có truyền: zSqlTemplate.cs:37 (SqlTemplate_Crd_Card_CalcRank.zzB_Return_Full_zzE)
            // and (@strRankActionType is null or t.RankActionType = @strRankActionType)
            if (!string.IsNullOrWhiteSpace(rankActionType) && !string.Equals(calcRankActionType, rankActionType, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Map đối tượng kết quả theo đúng #tbl_Crd_Card_CalcRank (CardRank.cs:7460-7503)
            resultList.Add(new Crd_Card
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
                EffDateStart = cc.EffDateStart,
                EffDateEnd = cc.EffDateEnd,
                PointTotal = cc.PointTotal,
                PointBlock = cc.PointBlock,
                PointAvail = cm.PointAvail,           // :7468: t.PointAvail (từ cm.PointAvail :7351)
                AmountTotal = cc.AmountTotal,
                AmountBlock = cc.AmountBlock,
                AmountAvail = cc.AmountAvail,         // :7478: cc.AmountAvail
                QtyVisitTotal = cc.QtyVisitTotal,
                QtyVisitBlock = cc.QtyVisitBlock,
                QtyVisitAvail = cm.QtyVisitAvail,     // :7469: t.QtyVisitAvail (từ cm.QtyVisitAvail :7352)
                PointCardRank = cc.PointCardRank,     // :7477: cc.PointCardRank
                TotalAmountPeriod = cc.TotalAmountPeriod,
                CardTypeUse = cc.CardTypeUse,
                CardTypeInit = cc.CardTypeInit,
                CardTypeUsePrev = cc.CardTypeUsePrev,
                CardTypeInitPrev = cc.CardTypeInitPrev,
                CardActiveDate = cc.CardActiveDate,
                FlagExceptionally = cc.FlagExceptionally,
                CardTypeUseExpect = cardTypeUseExpect,
                CardTypeInitExpect = finalCardTypeInitExpect,
                RankActionType = calcRankActionType,
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

                // Các cột từ Mst_RankPolicy (CardRank.cs:7470-7473)
                QtyVisitUpBegin = mrpUse.QtyVisitKeepBegin,
                PointKeepBegin = mrpUse.PointKeepBegin,
                MRP_CardType = mrpUse.CardType,
                MRP_RankPolicyCode = mrpUse.RankPolicyCode,

                // Các cột từ Crd_Member (CardRank.cs:7480-7495)
                MemberName = cm.MemberName,
                PhoneNo = cm.PhoneNo,
                mdcp_DLCPCode = cm.DLCodeRegis,
                CarNo = cm.CarNo,
                VIN = cm.VIN,
                PointEnd = cm.PointEnd,
                AmountEnd = cm.AmountEnd,
                QtyVisitEnd = cm.QtyVisitEnd,

                // Các trường mở rộng từ cm
                MemberStatus = cm.MemberStatus,
                IDCardNo = cm.IDCardNo,
                MemberGender = cm.MemberGender,
                MemAddress = cm.MemAddress,
                DateOfBirth = cm.DateOfBirth,
                ProvinceCode = cm.ProvinceCode,
                DistrictCode = cm.DistrictCode,
                ModelCode = cm.ModelCode,
                ModelName = cm.ModelName,
                TradeMarkCode = cm.TradeMarkCode,
                PointBegin = cm.PointBegin,
                PointVoucher = cm.PointVoucher,
                PurchaseType = cm.PurchaseType,
                WarrantyExpiryDate = cm.WarrantyExpiryDate,
                InvoiceDate = cm.InvoiceDate,
                MemberActiveDate = cm.MemberActiveDate,
                InactiveDTimeUTC = cm.InactiveDTimeUTC
            });
        }

        // Khớp gate Crd_CardRankController.cs:74-82:
        // if (objRT_Crd_Card_CalcRank.Lst_Crd_Card != null && objRT_Crd_Card_CalcRank.Lst_Crd_Card.Count > 0)
        //     lst_Crd_Card = objRT_Crd_Card_CalcRank.Lst_Crd_Card;
        // objJsonResultEntry.Data = new { DataList = lst_Crd_Card }; (rỗng -> DataList null)
        List<Crd_Card>? dataList = resultList.Count > 0 ? resultList : null;

        return Ok(new
        {
            Success = true,
            Data = new
            {
                DataList = dataList
            }
        });
    }

    /// <summary>
    /// Port 1:1 Crd_CardRankController.SearchForMember (gate Crd_CardRankController.cs:97-183 -> API CrdCardRankController.cs:23-90 -> Biz CardRank.cs:658-842, 156-492) (#008).
    /// </summary>
    [HttpPost("searchformember")]
    public async Task<IActionResult> SearchForMember([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Crd_CardRankSearchForMemberReq? req)
    {
        var r = req ?? new Crd_CardRankSearchForMemberReq();


        // 2. Transaction & Rollback an toàn theo nguồn (CardRank.cs:538, :619, :627, :642)
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // Phân trang: Gate nguồn cố định Ft_RecordStart = '0', Ft_RecordCount = '6400' (:140-141)
            const long recordStart = 0;   // gate :140 cố định "0" (không nhận từ client)
            const long recordCount = 6400;   // gate :141 cố định "6400"

            // Truy vấn cơ sở kết nối các bảng theo Crd_CardRank_GetX (CardRank.cs:214-239)
            // ccr: Crd_CardRank
            // ccbf: Crd_CardBefore on ccr.CardRankNo = ccbf.CardRankNo
            // ccat: Crd_CardAfter on ccr.CardRankNo = ccat.CardRankNo
            // cc: Crd_Card on ccbf.CardNo = cc.CardNo
            // cm: Crd_Member on cc.MemberNo = cm.MemberNo
            // Lưu ý: bảng tạm #tbl_Crd_CardRank_ViewAbility theo người dùng đăng nhập bị bỏ qua vì không có session (ghi GAP).
            var baseQuery = from ccr in _db.Set<Crd_CardRank>()
                            join ccbf in _db.Set<Crd_CardBefore>() on ccr.CardRankNo equals ccbf.CardRankNo
                            join ccat in _db.Set<Crd_CardAfter>() on ccr.CardRankNo equals ccat.CardRankNo into ccatGroup
                            from ccat in ccatGroup.DefaultIfEmpty()
                            join cc in _db.CrdCards on ccbf.CardNo equals cc.CardNo
                            join cm in _db.CrdMembers on cc.MemberNo equals cm.MemberNo
                            select new { ccr, ccbf, ccat, cc, cm };

            // strWhereClause_Crd_CardRank (Gate Crd_CardRankController.cs:424-469):
            // CardNo: Crd_Card.CardNo = CardNo (:434)
            if (!string.IsNullOrWhiteSpace(r.CardNo))
            {
                var pattern = PortQuery.Equal(r.CardNo.Trim());
                baseQuery = baseQuery.Where(x => EF.Functions.Like((x.cc.CardNo ?? "").ToLower(), pattern, PortQuery.Esc));
            }

            // IDCardNo: Crd_Member.IDCardNo = IDCardNo (:442)
            if (!string.IsNullOrWhiteSpace(r.IDCardNo))
            {
                var pattern = PortQuery.Equal(r.IDCardNo.Trim());
                baseQuery = baseQuery.Where(x => EF.Functions.Like((x.cm.IDCardNo ?? "").ToLower(), pattern, PortQuery.Esc));
            }

            // EffDateStartFrom: Crd_CardAfter.CreateDTimeUTC >= StdDTimeUTC(StdDTimeBeginDay(EffDateStartFrom)) (:444-449)
            if (!string.IsNullOrWhiteSpace(r.EffDateStartFrom) && DateTime.TryParse(r.EffDateStartFrom, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dtFrom))
            {
                var utcFrom = dtFrom.Date.AddHours(-7); // Đầu ngày Việt Nam (UTC+7) chuyển sang UTC
                baseQuery = baseQuery.Where(x => x.ccat != null && x.ccat.CreateDTimeUTC >= utcFrom);
            }

            // EffDateStartTo: Crd_CardAfter.CreateDTimeUTC <= StdDTimeUTC(StdDTimeEndDay(EffDateStartTo)) (:451-456)
            if (!string.IsNullOrWhiteSpace(r.EffDateStartTo) && DateTime.TryParse(r.EffDateStartTo, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dtTo))
            {
                var utcTo = dtTo.Date.AddDays(1).AddSeconds(-1).AddHours(-7); // Cuối ngày 23:59:59 Việt Nam (UTC+7) chuyển sang UTC
                baseQuery = baseQuery.Where(x => x.ccat != null && x.ccat.CreateDTimeUTC <= utcTo);
            }

            // MemberNo: Crd_CardAfter.MemberNo = MemberNo (:458-460)
            if (!string.IsNullOrWhiteSpace(r.MemberNo))
            {
                var pattern = PortQuery.Equal(r.MemberNo.Trim());
                baseQuery = baseQuery.Where(x => x.ccat != null && EF.Functions.Like((x.ccat.MemberNo ?? "").ToLower(), pattern, PortQuery.Esc));
            }

            // DLCode: Crd_Member.DLCodeRegis = DLCode (:462-464)
            if (!string.IsNullOrWhiteSpace(r.DLCode))
            {
                var pattern = PortQuery.Equal(r.DLCode.Trim());
                baseQuery = baseQuery.Where(x => EF.Functions.Like((x.cm.DLCodeRegis ?? "").ToLower(), pattern, PortQuery.Esc));
            }

            // Lọc NetworkID (nếu có truyền)
            if (!string.IsNullOrWhiteSpace(r.NetworkID))
            {
                var pattern = PortQuery.Equal(r.NetworkID.Trim());
                baseQuery = baseQuery.Where(x => EF.Functions.Like((x.ccr.NetworkID ?? "").ToLower(), pattern, PortQuery.Esc));
            }

            // Lọc OrgID (nếu có truyền)
            if (!string.IsNullOrWhiteSpace(r.OrgID))
            {
                var pattern = PortQuery.Equal(r.OrgID.Trim());
                baseQuery = baseQuery.Where(x => EF.Functions.Like((x.ccr.OrgID ?? "").ToLower(), pattern, PortQuery.Esc));
            }

            // #tbl_Crd_CardRank_Filter_Draft: select distinct ccr.CardRankNo, ccr.CreateDTimeUTC order by ccr.CreateDTimeUTC desc (CardRank.cs:216-239)
            var filteredDraft = await baseQuery
                .Select(x => new { x.ccr.CardRankNo, x.ccr.CreateDTimeUTC })
                .Distinct()
                .OrderByDescending(x => x.CreateDTimeUTC)
                .ToListAsync();

            // Áp dụng phân trang theo bài học A.12 (chống tràn int và không clamp âm)
            var pagedRankNos = (recordCount <= 0 || recordStart < 0)
                ? new List<string>()
                : filteredDraft
                    .Skip((int)Math.Min(recordStart, int.MaxValue))
                    .Take((int)Math.Min(recordCount, int.MaxValue))
                    .Select(x => x.CardRankNo)
                    .ToList();

            List<Crd_CardRank>? lst_Crd_CardRank = null;
            List<Crd_CardBefore>? lst_Crd_CardBefore = null;
            List<Crd_CardAfter>? lst_Crd_CardAfter = null;

            if (pagedRankNos.Count > 0)
            {
                // Crd_CardRank: order by ccr.CreateDTimeUTC desc (CardRank.cs:993-1001)
                var rankList = await _db.Set<Crd_CardRank>()
                    .Where(x => pagedRankNos.Contains(x.CardRankNo))
                    .OrderByDescending(x => x.CreateDTimeUTC)
                    .ToListAsync();

                if (rankList.Count > 0)
                {
                    lst_Crd_CardRank = rankList;
                }

                // Crd_CardBefore: order by t.MyIdxSeq asc (CardRank.cs:1033-1041)
                var beforeList = await _db.Set<Crd_CardBefore>()
                    .Where(x => pagedRankNos.Contains(x.CardRankNo))
                    .ToListAsync();

                if (beforeList.Count > 0)
                {
                    lst_Crd_CardBefore = beforeList
                        .OrderBy(x => pagedRankNos.IndexOf(x.CardRankNo))
                        .ThenBy(x => x.CardNo)
                        .ToList();
                }

                // Crd_CardAfter: join Crd_CardBefore để lấy PointCardRankPrev, QtyVisitTotalPrev, order by t.MyIdxSeq asc (CardRank.cs:1072-1085)
                var afterList = await _db.Set<Crd_CardAfter>()
                    .Where(x => pagedRankNos.Contains(x.CardRankNo))
                    .ToListAsync();

                if (afterList.Count > 0)
                {
                    foreach (var item in afterList)
                    {
                        var matchBefore = beforeList.FirstOrDefault(b => b.CardRankNo == item.CardRankNo && b.CardNo == item.CardNo)
                            ?? beforeList.FirstOrDefault(b => b.CardRankNo == item.CardRankNo);
                        item.PointCardRankPrev = matchBefore?.PointCardRank;
                        item.QtyVisitTotalPrev = matchBefore?.QtyVisitTotal;
                    }

                    lst_Crd_CardAfter = afterList
                        .OrderBy(x => pagedRankNos.IndexOf(x.CardRankNo))
                        .ThenBy(x => x.CardNo)
                        .ToList();
                }
            }

            // Nguồn luôn rollback an toàn (CardRank.cs:619 TDALUtils.DBUtils.RollbackSafety)
            await tx.RollbackAsync();

            // Khớp cấu trúc trả về tại Gate Crd_CardRankController.cs:165-170:
            // Data = new { Lst_Crd_CardRank, Lst_Crd_CardBefore, Lst_Crd_CardAfter } (rỗng -> null)
            return Ok(new
            {
                Success = true,
                Data = new
                {
                    Lst_Crd_CardRank = lst_Crd_CardRank,
                    Lst_Crd_CardBefore = lst_Crd_CardBefore,
                    Lst_Crd_CardAfter = lst_Crd_CardAfter
                }
            });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return PortResult.Fail("Crd_CardRank_Get", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Port 1:1 Crd_CardRankController.ApproveRankDown (gate Crd_CardRankController.cs:187-249 -> API CrdCardRankController.cs:385-455 -> Biz CardRank.cs:10106-10206, 9757-10104) (#009).
    /// </summary>
    [HttpPost("approverankdown")]
    public async Task<IActionResult> ApproveRankDown([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Crd_CardRankApproveRankDownReq? req)
    {
        // 0. Kiểm tra strJson đầu vào: Gate Crd_CardRankController.cs:196-203
        // if (!string.IsNullOrEmpty(strJson)) { ... } else { throw new mbiz.core.Exceptions.ServiceException("strJson null"); }
        if (!ModelState.IsValid || req == null)
        {
            return PortResult.Fail("Crd_CardRank_ApproveRankDown", "Check.strJson", null);
        }

        List<Crd_CardRankApproveRankDownItem>? cardList = null;

        if (!string.IsNullOrEmpty(req.strJson))
        {
            try
            {
                var parsed = System.Text.Json.JsonSerializer.Deserialize<Crd_CardRankApproveRankDownReq>(
                    req.strJson,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                cardList = parsed?.Lst_Crd_Card;
                if (cardList == null)
                {
                    return PortResult.Fail("Crd_CardRank_ApproveRankDown", "Check.strJson", null);
                }
            }
            catch (Exception)
            {
                return PortResult.Fail("Crd_CardRank_ApproveRankDown", "Check.strJson", null);
            }
        }
        else if (req.Lst_Crd_Card != null)
        {
            cardList = req.Lst_Crd_Card;
        }
        else
        {
            // strJson bị null hoặc rỗng -> trả lỗi ServiceException theo Gate Crd_CardRankController.cs:202
            return PortResult.Fail("Crd_CardRank_ApproveRankDown", "Check.strJson", null);
        }

        if (cardList.Count == 0)
        {
            // Khi danh sách rỗng, vòng lặp nguồn chạy 0 lần và trả về Success = true, Data = null
            return Ok(new
            {
                Success = true,
                Data = (object?)null
            });
        }

        // Sequence CardRankNo lấy TRƯỚC khi mở transaction chính (Seq.cs:28-41: transaction riêng, commit độc lập)
        var cardRankSeqs = new List<long>();
        for (var k = 0; k < cardList.Count; k++) cardRankSeqs.Add(await PortSeq.NextAsync<Seq_CardRankNo>(_db));

        // 1. Mở transaction chính cho nghiệp vụ duyệt xuống hạng thẻ (CardRank.cs:9787 _cf.db.BeginTransaction())
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var dtimeSys = DateTime.UtcNow;
            var policies = await _db.Set<Mst_RankPolicy>().ToListAsync();

            // Lấy chính sách hoạt động từ Mst_RankPolicy (CardRank.cs:956-968, 1939)
            var activePolicy = policies.FirstOrDefault(p => p.FlagActive == "1");

            for (int i = 0; i < cardList.Count; i++)
            {
                var item = cardList[i];
                var cardNo = item.CardNo?.Trim().ToUpperInvariant();
                if (string.IsNullOrEmpty(cardNo)) continue;

                // Kiểm tra thẻ trong DB: Crd_Card_CheckDB (CardRank.cs:9997-10003)
                var card = await _db.CrdCards.FirstOrDefaultAsync(c =>
                    EF.Functions.Like((c.CardNo ?? "").ToLower(), PortQuery.Equal(cardNo), PortQuery.Esc));
                if (card == null)
                {
                    await tx.RollbackAsync();
                    return PortResult.Fail("Crd_Card_CheckDB_CardNotFound", "Check.strCardNo", cardNo);
                }

                // Kiểm tra hội viên trong DB: Crd_Member_CheckDB (CardRank.cs:10006-10012)
                var member = await _db.CrdMembers.FirstOrDefaultAsync(m => m.MemberNo == card.MemberNo);
                if (member == null)
                {
                    await tx.RollbackAsync();
                    return PortResult.Fail("Crd_Member_CheckDB_MemberNotFound", "Check.strMemberNo", card.MemberNo);
                }

                // Kiểm tra ngày hết hạn kỳ xét hạng: CardRank.cs:10025-10039
                if (card.EffDateEnd.HasValue)
                {
                    var strEffDateEnd = card.EffDateEnd.Value.ToString("yyyy-MM-dd");
                    if (string.CompareOrdinal(dtimeSys.ToString("yyyy-MM-dd"), strEffDateEnd) < 0)
                    {
                        await tx.RollbackAsync();
                        return PortResult.Fail("Crd_Card_RankDown_Input_InvalidIEffDateEndBeforeSysDate",
                            "Check.strCardNo", card.CardNo,
                            "Check.dtimeSys", dtimeSys.ToString("yyyy-MM-dd"),
                            "Check.strEffDateEnd", strEffDateEnd);
                    }
                }

                // Xác định hạng dự kiến hạ xuống theo nguồn:
                // 1) Lấy từ input client gửi nếu có (CardRank.cs:9981-9982, 10022-10023)
                // 2) Nếu không có trong input, lấy từ cột CardTypeUseExpect/CardTypeInitExpect lưu sẵn trên thẻ trong DB
                // 3) Nếu vẫn chưa có, tính theo công thức Mst_RankPolicy của Crd_Card_CalcRank (CardRank.cs:7334-7348)
                var cardTypeUseExpect = !string.IsNullOrWhiteSpace(item.CardTypeUseExpect)
                    ? item.CardTypeUseExpect.Trim().ToUpperInvariant()
                    : (!string.IsNullOrWhiteSpace(card.CardTypeUseExpect) ? card.CardTypeUseExpect.Trim().ToUpperInvariant() : null);

                var cardTypeInitExpect = !string.IsNullOrWhiteSpace(item.CardTypeInitExpect)
                    ? item.CardTypeInitExpect.Trim().ToUpperInvariant()
                    : (!string.IsNullOrWhiteSpace(card.CardTypeInitExpect) ? card.CardTypeInitExpect.Trim().ToUpperInvariant() : cardTypeUseExpect);

                if (string.IsNullOrWhiteSpace(cardTypeUseExpect))
                {
                    // Tính hạng đề xuất theo đúng Crd_Card_CalcRank (CardRank.cs:7334-7348)
                    var mrpUse = policies.FirstOrDefault(p =>
                        string.Equals(p.CardType, card.CardTypeUse, StringComparison.OrdinalIgnoreCase)
                        && (string.IsNullOrEmpty(card.RankPolicyCode) || string.Equals(p.RankPolicyCode, card.RankPolicyCode, StringComparison.OrdinalIgnoreCase))
                        && p.FlagActive == "1");

                    if (mrpUse != null)
                    {
                        var ptRank = member.PointCardRank ?? 0;
                        var qtyVisit = member.QtyVisitAvail ?? 0;
                        if (ptRank >= (mrpUse.PointKeepBegin ?? 0) && qtyVisit >= (mrpUse.QtyVisitKeepBegin ?? 0))
                        {
                            cardTypeUseExpect = card.CardTypeUse;
                        }
                        else
                        {
                            var lower = policies
                                .Where(p => string.Equals(p.RankPolicyCode, mrpUse.RankPolicyCode, StringComparison.OrdinalIgnoreCase)
                                         && p.Value < mrpUse.Value)
                                .OrderByDescending(p => p.Value)
                                .FirstOrDefault();
                            cardTypeUseExpect = lower?.CardType ?? member.CardTypeUse ?? card.CardTypeUse;
                        }
                    }
                    cardTypeInitExpect = cardTypeUseExpect;
                }

                // Kiểm tra CardTypeUse != CardTypeUseExpect (CardRank.cs:1748-1761)
                // Nếu nguồn không cho phép suy ra hạng hạ xuống hoặc hạng đề xuất trùng hạng hiện tại -> báo lỗi đúng mã nguồn
                if (string.IsNullOrWhiteSpace(cardTypeUseExpect) || string.Equals(card.CardTypeUse, cardTypeUseExpect, StringComparison.OrdinalIgnoreCase))
                {
                    await tx.RollbackAsync();
                    return PortResult.Fail("Crd_CardRankPolicy_Perform_InvalidRankCardTypeUse",
                        "Check.strCardNo", card.CardNo,
                        "Check.strFunctionActionType", "DOWN",
                        "Check.strCardTypeUse", card.CardTypeUse,
                        "Check.strCardTypeUseExpect", cardTypeUseExpect ?? "");
                }

                // Sinh mã thẻ mới theo GenCardNo nguồn (Card.cs:27641-27744) — dùng hàm chung của Crd_CardControllerPort
                var memberCardNos = await _db.CrdCards.AsNoTracking().Where(c => c.MemberNo == member.MemberNo).Select(c => c.CardNo).ToListAsync();
                var (newCardNo, genErr) = Crd_CardControllerPort.GenCardNo(member.MemberNo, member.DLCodeRegis, memberCardNos);
                if (genErr != null)
                {
                    await tx.RollbackAsync();
                    return genErr;
                }

                // Sinh mã đợt xét hạng CardRankNo qua PortSeq.Build theo chuẩn Seq_Common_Get (Seq.cs:138, CardRank.cs:1768-1790)
                var rankCount = cardRankSeqs[i];
                var cardRankNo = PortSeq.Build("{0}CARDRANKNO.{3}.{1:00000}{2}", "", rankCount, 100000, "", dtimeSys);

                // Chính sách xếp hạng cho thẻ mới (CardRank.cs:1939)
                var rankPolicyCode = card.RankPolicyCode ?? activePolicy?.RankPolicyCode;   // nguồn CardRank.cs:1939 lấy từ chính sách; không đặt mặc định bịa

                // Cờ đặc cách FlagExceptionally theo nguồn (CardRank.cs:1906-1913)
                string flagExceptionally = "0";
                string? dlCodeExceptionally = null;
                if (!string.Equals(cardTypeUseExpect, cardTypeInitExpect, StringComparison.OrdinalIgnoreCase))
                {
                    flagExceptionally = "1";
                    dlCodeExceptionally = member.DLCodeExceptionally;
                }

                // Tính hiệu lực thẻ theo Crd_Card_ApproveX (Card.cs:23532-23538: 12 tháng)
                var effDateStart = dtimeSys.Date;
                var startMonth = new DateTime(effDateStart.Year, effDateStart.Month, 1);
                var effDateEnd = startMonth.AddMonths(13).AddDays(-1);

                // 1. Crd_Card_CancelX: Hủy thẻ cũ (CardRank.cs:1856-1874, Card.cs:22736-22758)
                card.CardStatus = "CANCEL";
                card.CancelDTimeUTC = dtimeSys;
                card.CancelBy = "port";
                card.Remark = "";
                card.LUDTimeUTC = dtimeSys;
                card.LUBy = "port";
                card.LogLUDTimeUTC = dtimeSys;
                card.LogLUBy = "port";

                // 2. Crd_Card_AddX + Crd_Card_ApproveX: Tạo thẻ mới đã duyệt (CardRank.cs:1918-1949, 2051, Card.cs:21627-21668, 23532-23578)
                var newCard = new CrdCard
                {
                    CardNo = newCardNo,
                    CardNoPrev = card.CardNo,
                    MemberNo = card.MemberNo,
                    NetworkID = card.NetworkID ?? (_config["Biz_NetworkID"] ?? "1001"),
                    CardSourceCode = "DOWN",
                    RankPolicyCode = rankPolicyCode,
                    DLCodeExceptionally = dlCodeExceptionally,
                    PointBonus = 0,
                    PointPurchaseAddCar = 0,
                    EffDateStart = effDateStart,
                    EffDateEnd = effDateEnd,
                    PointTotal = 0,
                    PointBlock = 0,
                    PointAvail = 0,
                    AmountTotal = 0,
                    AmountBlock = 0,
                    AmountAvail = 0,
                    QtyVisitTotal = 0,
                    QtyVisitBlock = 0,
                    QtyVisitAvail = 0,
                    PointCardRank = 0,
                    TotalAmountPeriod = 0,
                    CardTypeUse = cardTypeUseExpect,
                    CardTypeInit = cardTypeInitExpect,
                    CardTypeUsePrev = card.CardTypeUse,
                    CardTypeInitPrev = card.CardTypeInit,
                    CardActiveDate = effDateStart,
                    FlagExceptionally = flagExceptionally,
                    CardStatus = "APPROVE",
                    Remark = "",
                    CreateDTimeUTC = dtimeSys,
                    CreateBy = "port",
                    LUDTimeUTC = dtimeSys,
                    LUBy = "port",
                    ApproveDTimeUTC = dtimeSys,
                    ApproveBy = "port",
                    CancelDTimeUTC = null,
                    CancelBy = null,
                    LogLUDTimeUTC = dtimeSys,
                    LogLUBy = "port"
                };
                _db.CrdCards.Add(newCard);

                // 3. Crd_Member_UpdateForRankX: Cập nhật hội viên (CardRank.cs:1951-2039, 11825-11880)
                var ptBegin = (member.PointBegin ?? 0) + (member.PointAvail ?? 0);
                var amtBegin = (member.AmountBegin ?? 0) + (member.AmountAvail ?? 0);
                var qtyBegin = (member.QtyVisitBegin ?? 0) + (member.QtyVisitAvail ?? 0);

                member.PointBegin = ptBegin;
                member.PointEnd = ptBegin;
                member.PointTotal = 0;
                member.PointBlock = 0;
                member.PointAvail = 0;

                member.AmountBegin = amtBegin;
                member.AmountEnd = amtBegin;
                member.AmountTotal = 0;
                member.AmountBlock = 0;
                member.AmountAvail = 0;

                member.QtyVisitBegin = qtyBegin;
                member.QtyVisitEnd = qtyBegin;
                member.QtyVisitTotal = 0;
                member.QtyVisitBlock = 0;
                member.QtyVisitAvail = 0;

                member.CardTypeUse = cardTypeUseExpect;
                member.CardTypeInit = cardTypeInitExpect;
                member.PointCardRank = 0;
                member.DLCodeExceptionally = null;
                member.FlagExceptionally = "0";
                member.LUDTimeUTC = dtimeSys;
                member.LUBy = "port";
                member.LogLUDTimeUTC = dtimeSys;
                member.LogLUBy = "port";

                // 4. Crd_CardRank_AddX: Ghi nhận lịch sử xét hạng (CardRank.cs:2102-2140, 4246-4270)
                var rankRec = new Crd_CardRank
                {
                    CardRankNo = cardRankNo,
                    NetworkID = card.NetworkID ?? (_config["Biz_NetworkID"] ?? "1001"),
                    OrgID = member.OrgID ?? "1001",
                    RankPolicyCode = rankPolicyCode,
                    CardSourceCode = "DOWN",
                    DealPointType = "LOYALTY",
                    FunctionName = "Crd_Card_RankDown",
                    FunctionActionType = "NONE",
                    FunctionRemark = "0",
                    RefType = "ADD",
                    RefCode00 = null,
                    RefCode01 = null,
                    RefCode02 = null,
                    RefCode03 = null,
                    RefCode04 = null,
                    RefCode05 = null,
                    CreateDTimeUTC = dtimeSys,
                    CreateBy = "port",
                    LUDTimeUTC = dtimeSys,
                    LUBy = "port",
                    LogLUDTimeUTC = dtimeSys,
                    LogLUBy = "port"
                };
                _db.Set<Crd_CardRank>().Add(rankRec);

                // Crd_CardBefore: Lưu lại ảnh thẻ cũ (CardRank.cs:1824-1836, 4506-4545)
                var cardBefore = new Crd_CardBefore
                {
                    CardRankNo = cardRankNo,
                    CardNo = card.CardNo,
                    NetworkID = card.NetworkID ?? (_config["Biz_NetworkID"] ?? "1001"),
                    MemberNo = card.MemberNo,
                    CardSourceCode = card.CardSourceCode ?? "NEW",
                    CardTypeUse = card.CardTypeUse ?? "",
                    CardTypeInit = card.CardTypeInit ?? "",
                    PointCardRank = card.PointCardRank,
                    QtyVisitTotal = card.QtyVisitTotal,
                    CreateDTimeUTC = dtimeSys,
                    CreateBy = "port",
                    LUDTimeUTC = dtimeSys,
                    LUBy = "port"
                };
                _db.Set<Crd_CardBefore>().Add(cardBefore);

                // Crd_CardAfter: Lưu lại ảnh thẻ mới sau duyệt xuống hạng (CardRank.cs:2088-2100, 4506-4545)
                var cardAfter = new Crd_CardAfter
                {
                    CardRankNo = cardRankNo,
                    CardNo = newCardNo,
                    NetworkID = newCard.NetworkID,
                    MemberNo = newCard.MemberNo,
                    CardSourceCode = "DOWN",
                    CardTypeUse = cardTypeUseExpect,
                    CardTypeInit = cardTypeInitExpect ?? cardTypeUseExpect,
                    PointCardRank = 0,
                    QtyVisitTotal = 0,
                    CreateDTimeUTC = dtimeSys,
                    CreateBy = "port",
                    LUDTimeUTC = dtimeSys,
                    LUBy = "port"
                };
                _db.Set<Crd_CardAfter>().Add(cardAfter);
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            // Kết quả trả về object JsonResultEntry với Data = null khi thành công (Gate Crd_CardRankController.cs:235-236)
            return Ok(new
            {
                Success = true,
                Data = (object?)null
            });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return PortResult.Fail("Crd_CardRank_ApproveRankDown", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Port 1:1 Crd_CardRankController.ApproveRankKeep (gate Crd_CardRankController.cs:251-313 -> API CrdCardRankController.cs:312-382 -> Biz CardRank.cs:9655-9755, 8772-9120, 1341-1740) (#010).
    /// </summary>
    [HttpPost("approverankkeep")]
    public async Task<IActionResult> ApproveRankKeep([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Crd_CardRankApproveRankKeepReq? req)
    {
        // 0. Kiểm tra strJson đầu vào: Gate Crd_CardRankController.cs:260-267
        // if (!string.IsNullOrEmpty(strJson)) { ... } else { throw new mbiz.core.Exceptions.ServiceException("strJson null"); }
        if (!ModelState.IsValid || req == null)
        {
            return PortResult.Fail("Crd_CardRank_ApproveRankKeep", "Check.strJson", null);
        }

        List<Crd_CardRankApproveRankKeepItem>? cardList = null;

        if (!string.IsNullOrEmpty(req.strJson))
        {
            try
            {
                var parsed = System.Text.Json.JsonSerializer.Deserialize<Crd_CardRankApproveRankKeepReq>(
                    req.strJson,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                cardList = parsed?.Lst_Crd_Card;
                if (cardList == null)
                {
                    return PortResult.Fail("Crd_CardRank_ApproveRankKeep", "Check.strJson", null);
                }
            }
            catch (Exception)
            {
                return PortResult.Fail("Crd_CardRank_ApproveRankKeep", "Check.strJson", null);
            }
        }
        else if (req.Lst_Crd_Card != null)
        {
            cardList = req.Lst_Crd_Card;
        }
        else
        {
            // strJson bị null hoặc rỗng -> trả lỗi ServiceException theo Gate Crd_CardRankController.cs:266
            return PortResult.Fail("Crd_CardRank_ApproveRankKeep", "Check.strJson", null);
        }

        if (cardList.Count == 0)
        {
            // Khi danh sách rỗng, vòng lặp nguồn chạy 0 lần và trả về Success = true, Data = null (Gate Crd_CardRankController.cs:299-300)
            return Ok(new
            {
                Success = true,
                Data = (object?)null
            });
        }

        // Sequence CardRankNo lấy TRƯỚC khi mở transaction chính (Seq.cs:28-41: transaction riêng, commit độc lập)
        var cardRankSeqs = new List<long>();
        for (var k = 0; k < cardList.Count; k++) cardRankSeqs.Add(await PortSeq.NextAsync<Seq_CardRankNo>(_db));

        // 1. Mở transaction chính cho cả lô duyệt duy trì hạng thẻ (CardRank.cs:8802 _cf.db.BeginTransaction())
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var dtimeSys = DateTime.UtcNow;
            var policies = await _db.Set<Mst_RankPolicy>().ToListAsync();

            // Lấy chính sách hoạt động từ Mst_RankPolicy (CardRank.cs:956-968, 1537)
            var activePolicy = policies.FirstOrDefault(p => p.FlagActive == "1");

            for (int i = 0; i < cardList.Count; i++)
            {
                var item = cardList[i];
                var cardNo = item.CardNo?.Trim().ToUpperInvariant();
                if (string.IsNullOrEmpty(cardNo)) continue;

                // Kiểm tra thẻ trong DB: Crd_Card_CheckDB (CardRank.cs:9012-9018, Card.cs:21229-21276)
                var card = await _db.CrdCards.FirstOrDefaultAsync(c =>
                    EF.Functions.Like((c.CardNo ?? "").ToLower(), PortQuery.Equal(cardNo), PortQuery.Esc));
                if (card == null)
                {
                    await tx.RollbackAsync();
                    return PortResult.Fail("Crd_Card_CheckDB_CardNotFound", "Check.strCardNo", cardNo);
                }

                // Kiểm tra trạng thái thẻ: Crd_Card_CheckDB với strCardStatusListToCheck = APPROVE (CardRank.cs:9016, Card.cs:21294-21305)
                if (!string.Equals(card.CardStatus, "APPROVE", StringComparison.OrdinalIgnoreCase))
                {
                    await tx.RollbackAsync();
                    return PortResult.Fail("Crd_Card_CheckDB_CardStatusNotMatched", "Check.strCardNo", card.CardNo);
                }

                // Kiểm tra hội viên trong DB: Crd_Member_CheckDB (CardRank.cs:9021-9028, Member.cs:3379-3420)
                var member = await _db.CrdMembers.FirstOrDefaultAsync(m => m.MemberNo == card.MemberNo);
                if (member == null)
                {
                    await tx.RollbackAsync();
                    return PortResult.Fail("Crd_Member_CheckDB_MemberNotFound", "Check.strMemberNo", card.MemberNo);
                }

                // Kiểm tra ngày hết hạn kỳ xét hạng: CardRank.cs:9041-9055
                if (card.EffDateEnd.HasValue)
                {
                    var strEffDateEnd = card.EffDateEnd.Value.ToString("yyyy-MM-dd");
                    if (string.CompareOrdinal(dtimeSys.ToString("yyyy-MM-dd"), strEffDateEnd) < 0)
                    {
                        await tx.RollbackAsync();
                        return PortResult.Fail("Crd_Card_RankKeep_Input_InvalidIEffDateEndBeforeSysDate",
                            "Check.strCardNo", card.CardNo,
                            "Check.dtimeSys", dtimeSys.ToString("yyyy-MM-dd"),
                            "Check.strEffDateEnd", strEffDateEnd);
                    }
                }

                // Xác định hạng dự kiến duy trì theo nguồn:
                // 1) Lấy từ input client gửi nếu có (CardRank.cs:8996-8997, 9037-9038)
                // 2) Nếu không có trong input, lấy từ cột CardTypeUseExpect/CardTypeInitExpect lưu sẵn trên thẻ trong DB
                // 3) Nếu vẫn chưa có, lấy CardTypeUse hiện tại của thẻ
                var cardTypeUseExpect = !string.IsNullOrWhiteSpace(item.CardTypeUseExpect)
                    ? item.CardTypeUseExpect.Trim().ToUpperInvariant()
                    : (!string.IsNullOrWhiteSpace(card.CardTypeUseExpect) ? card.CardTypeUseExpect.Trim().ToUpperInvariant() : card.CardTypeUse);

                var cardTypeInitExpect = !string.IsNullOrWhiteSpace(item.CardTypeInitExpect)
                    ? item.CardTypeInitExpect.Trim().ToUpperInvariant()
                    : (!string.IsNullOrWhiteSpace(card.CardTypeInitExpect) ? card.CardTypeInitExpect.Trim().ToUpperInvariant() : cardTypeUseExpect);

                // Kiểm tra CardTypeUse == CardTypeUseExpect (CardRank.cs:1346-1360)
                // Đối với duy trì hạng (KEEP), hạng dự kiến giữ phải trùng với hạng hiện tại
                if (!string.Equals(card.CardTypeUse, cardTypeUseExpect, StringComparison.OrdinalIgnoreCase))
                {
                    await tx.RollbackAsync();
                    return PortResult.Fail("Crd_CardRankPolicy_Perform_InvalidRankCardTypeUse",
                        "Check.strCardNo", card.CardNo,
                        "Check.strFunctionActionType", "KEEP",
                        "Check.strCardTypeUse", card.CardTypeUse,
                        "Check.strCardTypeUseExpect", cardTypeUseExpect ?? "");
                }

                // Sinh mã thẻ mới theo GenCardNo nguồn (Card.cs:27641-27744) — dùng hàm chung của Crd_CardControllerPort
                var memberCardNos = await _db.CrdCards.AsNoTracking().Where(c => c.MemberNo == member.MemberNo).Select(c => c.CardNo).ToListAsync();
                var (newCardNo, genErr) = Crd_CardControllerPort.GenCardNo(member.MemberNo, member.DLCodeRegis, memberCardNos);
                if (genErr != null)
                {
                    await tx.RollbackAsync();
                    return genErr;
                }

                // Sinh mã đợt xét hạng CardRankNo qua PortSeq.Build theo chuẩn Seq_Common_Get (Seq.cs:138, CardRank.cs:1364-1389)
                var rankCount = cardRankSeqs[i];
                var cardRankNo = PortSeq.Build("{0}CARDRANKNO.{3}.{1:00000}{2}", "", rankCount, 100000, "", dtimeSys);

                // Chính sách xếp hạng cho thẻ mới (CardRank.cs:1537)
                var rankPolicyCode = card.RankPolicyCode ?? activePolicy?.RankPolicyCode;

                // Cờ đặc cách FlagExceptionally theo nguồn (CardRank.cs:1505-1512)
                string flagExceptionally = "0";
                string? dlCodeExceptionally = null;
                if (!string.Equals(cardTypeUseExpect, cardTypeInitExpect, StringComparison.OrdinalIgnoreCase))
                {
                    flagExceptionally = "1";
                    dlCodeExceptionally = member.DLCodeExceptionally;
                }

                // Tính hiệu lực thẻ theo Crd_Card_ApproveX (Card.cs:23532-23538: 12 tháng)
                var effDateStart = dtimeSys.Date;
                var startMonth = new DateTime(effDateStart.Year, effDateStart.Month, 1);
                var effDateEnd = startMonth.AddMonths(13).AddDays(-1);

                // 1. Crd_Card_CancelX: Hủy thẻ cũ (CardRank.cs:1454-1472, Card.cs:22736-22758)
                card.CardStatus = "CANCEL";
                card.CancelDTimeUTC = dtimeSys;
                card.CancelBy = "port";
                card.Remark = "";
                card.LUDTimeUTC = dtimeSys;
                card.LUBy = "port";
                card.LogLUDTimeUTC = dtimeSys;
                card.LogLUBy = "port";

                // 2. Crd_Card_AddX + Crd_Card_ApproveX: Tạo thẻ mới đã duyệt với CardSourceCode = KEEP (CardRank.cs:1474-1547, 1648, Card.cs:21627-21668, 23532-23578)
                var newCard = new CrdCard
                {
                    CardNo = newCardNo,
                    CardNoPrev = card.CardNo,
                    MemberNo = card.MemberNo,
                    NetworkID = card.NetworkID ?? (_config["Biz_NetworkID"] ?? "1001"),
                    CardSourceCode = "KEEP",
                    RankPolicyCode = rankPolicyCode,
                    DLCodeExceptionally = dlCodeExceptionally,
                    PointBonus = 0,
                    PointPurchaseAddCar = 0,
                    EffDateStart = effDateStart,
                    EffDateEnd = effDateEnd,
                    PointTotal = 0,
                    PointBlock = 0,
                    PointAvail = 0,
                    AmountTotal = 0,
                    AmountBlock = 0,
                    AmountAvail = 0,
                    QtyVisitTotal = 0,
                    QtyVisitBlock = 0,
                    QtyVisitAvail = 0,
                    PointCardRank = 0,
                    TotalAmountPeriod = 0,
                    CardTypeUse = cardTypeUseExpect,
                    CardTypeInit = cardTypeInitExpect,
                    CardTypeUsePrev = card.CardTypeUse,
                    CardTypeInitPrev = card.CardTypeInit,
                    CardActiveDate = effDateStart,
                    FlagExceptionally = flagExceptionally,
                    CardStatus = "APPROVE",
                    Remark = "",
                    CreateDTimeUTC = dtimeSys,
                    CreateBy = "port",
                    LUDTimeUTC = dtimeSys,
                    LUBy = "port",
                    ApproveDTimeUTC = dtimeSys,
                    ApproveBy = "port",
                    CancelDTimeUTC = null,
                    CancelBy = null,
                    LogLUDTimeUTC = dtimeSys,
                    LogLUBy = "port"
                };
                _db.CrdCards.Add(newCard);

                // 3. Crd_Member_UpdateForRankX: Cập nhật hội viên (CardRank.cs:1550-1638)
                var ptBegin = (member.PointBegin ?? 0) + (member.PointAvail ?? 0);
                var amtBegin = (member.AmountBegin ?? 0) + (member.AmountAvail ?? 0);
                var qtyBegin = (member.QtyVisitBegin ?? 0) + (member.QtyVisitAvail ?? 0);

                member.PointBegin = ptBegin;
                member.PointEnd = ptBegin;
                member.PointTotal = 0;
                member.PointBlock = 0;
                member.PointAvail = 0;

                member.AmountBegin = amtBegin;
                member.AmountEnd = amtBegin;
                member.AmountTotal = 0;
                member.AmountBlock = 0;
                member.AmountAvail = 0;

                member.QtyVisitBegin = qtyBegin;
                member.QtyVisitEnd = qtyBegin;
                member.QtyVisitTotal = 0;
                member.QtyVisitBlock = 0;
                member.QtyVisitAvail = 0;

                member.CardTypeUse = cardTypeUseExpect;
                member.CardTypeInit = cardTypeInitExpect;
                member.PointCardRank = 0;
                member.DLCodeExceptionally = null;
                member.FlagExceptionally = "0";
                member.LUDTimeUTC = dtimeSys;
                member.LUBy = "port";
                member.LogLUDTimeUTC = dtimeSys;
                member.LogLUBy = "port";

                // 4. Crd_CardRank_AddX: Ghi nhận lịch sử xét hạng (CardRank.cs:1700-1738)
                var rankRec = new Crd_CardRank
                {
                    CardRankNo = cardRankNo,
                    NetworkID = card.NetworkID ?? (_config["Biz_NetworkID"] ?? "1001"),
                    OrgID = member.OrgID ?? "1001",
                    RankPolicyCode = rankPolicyCode,
                    CardSourceCode = "KEEP",
                    DealPointType = "LOYALTY",
                    FunctionName = "Crd_Card_RankKeep",
                    FunctionActionType = "NONE",
                    FunctionRemark = "0",
                    RefType = "ADD",
                    RefCode00 = null,
                    RefCode01 = null,
                    RefCode02 = null,
                    RefCode03 = null,
                    RefCode04 = null,
                    RefCode05 = null,
                    CreateDTimeUTC = dtimeSys,
                    CreateBy = "port",
                    LUDTimeUTC = dtimeSys,
                    LUBy = "port",
                    LogLUDTimeUTC = dtimeSys,
                    LogLUBy = "port"
                };
                _db.Set<Crd_CardRank>().Add(rankRec);

                // Crd_CardBefore: Lưu lại ảnh thẻ cũ (CardRank.cs:1422-1434, 1705)
                var cardBefore = new Crd_CardBefore
                {
                    CardRankNo = cardRankNo,
                    CardNo = card.CardNo,
                    NetworkID = card.NetworkID ?? (_config["Biz_NetworkID"] ?? "1001"),
                    MemberNo = card.MemberNo,
                    CardSourceCode = card.CardSourceCode ?? "NEW",
                    CardTypeUse = card.CardTypeUse ?? "",
                    CardTypeInit = card.CardTypeInit ?? "",
                    PointCardRank = card.PointCardRank,
                    QtyVisitTotal = card.QtyVisitTotal,
                    CreateDTimeUTC = dtimeSys,
                    CreateBy = "port",
                    LUDTimeUTC = dtimeSys,
                    LUBy = "port"
                };
                _db.Set<Crd_CardBefore>().Add(cardBefore);

                // Crd_CardAfter: Lưu lại ảnh thẻ mới sau duyệt duy trì hạng (CardRank.cs:1686-1698, 1707)
                var cardAfter = new Crd_CardAfter
                {
                    CardRankNo = cardRankNo,
                    CardNo = newCardNo,
                    NetworkID = newCard.NetworkID,
                    MemberNo = newCard.MemberNo,
                    CardSourceCode = "KEEP",
                    CardTypeUse = cardTypeUseExpect ?? "",
                    CardTypeInit = cardTypeInitExpect ?? cardTypeUseExpect ?? "",
                    PointCardRank = 0,
                    QtyVisitTotal = 0,
                    CreateDTimeUTC = dtimeSys,
                    CreateBy = "port",
                    LUDTimeUTC = dtimeSys,
                    LUBy = "port"
                };
                _db.Set<Crd_CardAfter>().Add(cardAfter);
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            // Kết quả trả về object JsonResultEntry với Data = null khi thành công (Gate Crd_CardRankController.cs:299-300)
            return Ok(new
            {
                Success = true,
                Data = (object?)null
            });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return PortResult.Fail("Crd_CardRank_ApproveRankKeep", "Exception", ex.Message);
        }
    }
}

