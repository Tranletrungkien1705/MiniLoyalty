using System;
using System.Collections.Generic;
using System.Globalization;
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
// PORT 1:1 — Crd_CardRankController/Search của dự án nguồn 2022.7.Loyalty (#007).
//
// Chuỗi gọi:
//   Gate: Crd_CardRankController.cs:30-95 Search -> WA_Crd_Card_CalcRank
//   API:  CrdCardRankController.cs:93-163 WA_Crd_Card_CalcRank -> _biz.WAS_Crd_Card_CalcRank
//   Biz:  CardRank.cs:7999-8108 WAS_Crd_Card_CalcRank -> Crd_Card_CalcRank (:7065) -> Crd_Card_CalcRankX (:7206-7570)
//   SQL:  CardRank.cs:7288-7517 + zSqlTemplate.cs:28-42 (zzB_Return_Full_zzE)
//
// GIỮ ĐÚNG NGHIỆP VỤ NGUỒN:
//   1. Gate: CardStatus LUÔN = 'APPROVE' (CardRankController.cs:70), không nhận từ client.
//      Data = { DataList = list }, rỗng -> DataList null, không PageInfo, không ItemCount (:78-82).
//   2. Lọc Crd_Member (cm) theo EffDateEnd (cm.EffDateEnd = @d, so sánh ngày), MemberNo, CarNo, VIN,
//      PhoneNo, FlagExceptionally (CardRank.cs:7290-7305).
//   3. Lọc Crd_Card (cc) theo CardNo, CardTypeUse, CardStatus = 'APPROVE' (CardRank.cs:7311-7324).
//   4. Phép tính xếp hạng (CardRank.cs:7330-7455):
//      - Join Mst_RankPolicy (mrp) theo cc.CardTypeUse = mrp.CardType.
//      - CardTypeUseExpect = (cm.PointCardRank >= mrp.PointKeepBegin && cm.QtyVisitAvail >= mrp.QtyVisitKeepBegin)
//          ? cc.CardTypeUse
//          : (hạng kế dưới cùng RankPolicyCode có Value < mrp.Value lớn nhất, mặc định cm.CardTypeUse).
//      - Tương tự CardTypeInitExpect theo CardTypeInit.
//      - RankActionType = (CardTypeUse != CardTypeUseExpect) ? 'DOWN' : 'KEEP'.
//      - CardTypeInitExpect cuối cùng = (RankActionType == 'KEEP') ? CardTypeUseExpect : CardTypeInitExpect(từ Init).
//      - Lọc RankActionType theo tham số đầu vào (zSqlTemplate.cs:37).
//   5. GAP đã khai báo: bảng #tbl_Crd_Rank_ViewAbility theo người dùng đăng nhập -> không có session: bỏ qua.
// ============================================================================================

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

[ApiController]
[Route("api/crd_cardrank")]
public class Crd_CardRankControllerPort : ControllerBase
{
    private readonly AppDbContext _db;

    public Crd_CardRankControllerPort(AppDbContext db)
    {
        _db = db;
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
}
