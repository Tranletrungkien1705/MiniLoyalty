using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniLoyalty.Data;
using MiniLoyalty.Models;

namespace MiniLoyalty.Controllers;

// ============================================================================================
// PORT 1:1 — 4 màn lõi của dự án nguồn 2022.7.Loyalty.
//
// Nguồn controller (tầng WA):
//   CrdMemberController.cs:245                    WA_Crd_Member_Get
//   CrdCardController.cs:163                      WA_Crd_Card_Get
//   CrdCardTransactionController.cs:24            WA_Crd_CardTransaction_Get
//   CrdMemberDiscountTransactionController.cs:22  WA_Crd_MemberDiscountTransaction_Get
// Nguồn SQL (tầng biz, đã chọn ĐÚNG bản LIVE — xem từng hàm bên dưới):
//   Card.cs:11352 · Master.Loyalty.cs:19484 · Master.Loyalty.cs:21441 · Transaction.cs:5424
//
// GIỮ ĐÚNG NGUỒN:
//   * TẤT CẢ endpoint là POST — hệ Loyalty gốc không dùng GET/query-string
//     (mọi action nguồn đều [AcceptVerbs("POST")], vd CrdCardTransactionController.cs:23).
//     CỐ Ý KHÁC: body là JSON thay vì form-urlencoded, theo mẫu nhà của app đích (ApiV1Controller).
//   * Phân trang RecordStart/RecordCount + trả kèm MyCount, y như khối "---- Summary:" của nguồn.
//   * Thứ tự sắp xếp giữ nguyên từng màn (member: MemberNo asc; card: CardNo desc; 2 màn
//     giao dịch: LogLUDTimeUTC desc).
// ============================================================================================

/// <summary>Đầu vào tìm kiếm dùng chung — port 1:1 lớp cha <c>Cm_Search</c> (phần phân trang).</summary>
public class CrdSearchBase
{
    public int RecordStart { get; set; }             // = nFilterRecordStart (0-based, như nguồn)
    public int RecordCount { get; set; } = 50;       // = nFilterRecordCount
}

/// <summary>Port 1:1 <c>Crd_MemberSearch</c> — ModelsUI/Crd_MemberUI.cs:9-37.</summary>
public class CrdMemberSearchReq : CrdSearchBase
{
    public string? MemberNo { get; set; }            // :11
    public string? CarNo { get; set; }               // :12
    public DateTime? RegistrationDateFrom { get; set; }   // :13
    public DateTime? RegistrationDateTo { get; set; }     // :15
    public DateTime? MemberActiveDateFrom { get; set; }   // :17
    public DateTime? MemberActiveDateTo { get; set; }     // :19
    public string? DLCodeRegis { get; set; }         // :21
    public string? PhoneNo { get; set; }             // :23
    public string? RegisStatus { get; set; }         // :25
    public string? MemberName { get; set; }          // :27
    public string? VIN { get; set; }                 // :29
    public string? cm_FlagShow { get; set; }         // :31
    public string? MemberStatus { get; set; }        // :33
    public string? DLCPCode { get; set; }            // :35 — đại lý truy vấn (Map_QueryDealer_Member)
    public string? SourceRegisType { get; set; }     // :36 — nguồn mở thẻ
}

/// <summary>Port 1:1 <c>Crd_CardSearch</c> — ModelsUI/Crd_CardUI.cs:10-33.</summary>
public class CrdCardSearchReq : CrdSearchBase
{
    public string? CardNo { get; set; }              // :11
    public string? MemberNo { get; set; }            // :12
    public string? VIN { get; set; }                 // :13
    public string? CarNo { get; set; }               // :14
    public string? PhoneNo { get; set; }             // :15
    public string? DLCodeRegis { get; set; }         // :16 — đại lý phát hành
    public string? CardSourceCode { get; set; }      // :18 — loại kỳ thẻ
    public string? CardStatus { get; set; }          // :19
    public DateTime? MemberActiveDateFrom { get; set; } // :20
    public DateTime? MemberActiveDateTo { get; set; }   // :21
    public string? FlagExceptionally { get; set; }   // :22
    public DateTime? EffDateStartFrom { get; set; }  // :23
    public DateTime? EffDateStartTo { get; set; }    // :24
    public DateTime? InactiveDTimeFrom { get; set; } // :25
    public DateTime? InactiveDTimeTo { get; set; }   // :26
    public string? CardTypeUse { get; set; }         // :27
    public string? MemberStatus { get; set; }        // :28
    public string? IDCardNo { get; set; }            // :29
}

/// <summary>Port 1:1 <c>Crd_CardTransactionSearch</c> — ModelsUI/Crd_CardTransactionUI.cs:9-52.</summary>
public class CrdCardTransactionSearchReq : CrdSearchBase
{
    public DateTime? EffDateStartFrom { get; set; }      // :11
    public DateTime? EffDateStartTo { get; set; }        // :13
    public DateTime? UsePrmDTimeFrom { get; set; }       // :15
    public DateTime? UsePrmDTimeTo { get; set; }         // :17
    public DateTime? CreateDTimeUTCFrom { get; set; }    // :19
    public DateTime? CreateDTimeUTCTo { get; set; }      // :21
    public string? CardNo { get; set; }                  // :24
    public string? IDCardNo { get; set; }                // :26
    public string? MemberNo { get; set; }                // :27
    public double? PointRefFrom { get; set; }            // :29
    public double? PointRefTo { get; set; }              // :31
    public string? FlagPoint { get; set; }               // :33
    /// <summary>Loại điểm — danh sách, phân cách dấu phẩy. Chứa DISCOUNTRO thì union bảng chiết khấu.</summary>
    public string? DealPointType { get; set; }           // :35
    public string? DLCode { get; set; }                  // :37
    public string? CardTypeUse { get; set; }             // :39
    public string? PhoneNo { get; set; }                 // :41
    public string? VIN { get; set; }                     // :43
    public double? PointChRankTotalFrom { get; set; }     // :46
    public double? PointChRankTotalTo { get; set; }       // :45
    public double? AmountChTotalFrom { get; set; }        // :48
    public double? AmountChTotalTo { get; set; }          // :47
    public string? PrProgramCode { get; set; }            // :50
    /// <summary>Cờ chặn theo đại lý của user (Master.Loyalty.cs:21480-21497). '1' = bật.</summary>
    public string? FlagCheckAuthor { get; set; }
    /// <summary>Đại lý của user đăng nhập (nguồn lấy từ Sys_User_ForCP_GetAbilityViewOfUser).</summary>
    public string? DLCPCode { get; set; }
}

/// <summary>Port 1:1 <c>Crd_MemberDiscountTransactionSearch</c> — ModelsUI/Crd_MemberDiscountTransactionSearch.cs:9-21.</summary>
public class CrdDiscountSearchReq : CrdSearchBase
{
    public string? DLCode { get; set; }                 // :11
    public string? MemberNo { get; set; }               // :12
    public string? RefNo { get; set; }                  // :13
    public DateTime? CreateDateFrom { get; set; }       // :14
    public DateTime? CreateDateTo { get; set; }         // :15
    public DateTime? CreateDTimeUTCFrom { get; set; }   // :16
    public DateTime? CreateDTimeUTCTo { get; set; }     // :17
    public double? PointChTotalFrom { get; set; }       // :18
    public double? PointChTotalTo { get; set; }         // :19
    public string? DealPointType { get; set; }          // :20
}

[ApiController]
[Route("api/crd")]
[Produces("application/json")]
public class CrdPortController(AppDbContext db) : ControllerBase
{
    /// <summary>Chuẩn hoá phân trang: nguồn tính nFilterRecordEnd = Start + Count - 1 (Card.cs:11383).</summary>
    private static (int skip, int take) Page(CrdSearchBase r)
        => (Math.Max(0, r.RecordStart), r.RecordCount <= 0 ? 50 : Math.Min(r.RecordCount, 500));

    /// <summary>
    /// Mẫu LIKE cho ô tìm "chứa" — port ngữ nghĩa `like '%x%'` của nguồn.
    /// BẪY ĐÃ SẬP Ở ĐÍCH KHÁC: `string.Contains` bị EF dịch thành `instr()` (SQLite) ⇒ PHÂN BIỆT
    /// hoa/thường, build `0 Errors` mà truy vấn trả 0 dòng; Postgres thì `LIKE` cũng phân biệt.
    /// Nguồn chạy SQL Server với collation KHÔNG phân biệt hoa/thường (CmUtils.SqlUtils.BuildWhere
    /// sinh `like`), nên ở đây hạ chữ CẢ HAI phía rồi mới LIKE — đúng cho SQLite lẫn Postgres.
    /// Ký tự đại diện `%` `_` `\` trong từ khoá được escape để người dùng gõ `%` không quét cả bảng.
    /// </summary>
    private static string Kw(string s) => "%" + s.Trim().ToLowerInvariant()
        .Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";

    /// <summary>Ký tự escape truyền cho `LIKE … ESCAPE '\'` (một dấu gạch chéo ngược).</summary>
    private const string Esc = "\\";

    // ---------------------------------------------------------------------------------------
    // MÀN 1 — HỘI VIÊN.  WA_Crd_Member_Get (CrdMemberController.cs:245) -> Crd_Member_GetX (Card.cs:11352)
    // ---------------------------------------------------------------------------------------
    [HttpPost("member/get")]
    public async Task<IActionResult> MemberGet([FromBody] CrdMemberSearchReq? req)
    {
        var r = req ?? new CrdMemberSearchReq();
        var q = db.CrdMembers.AsNoTracking().AsQueryable();

        // GUARD CỨNG của nguồn (Card.cs:11422): `and cmb.MemberStatus = 'APPROVE'`.
        // Nằm NGOÀI where do người dùng nhập ⇒ hội viên PENDING/CANCEL KHÔNG bao giờ ra ở màn này.
        q = q.Where(x => x.MemberStatus == LoyaltyConst.MemberStatus.Approve);

        if (!string.IsNullOrWhiteSpace(r.MemberNo)) { var k = Kw(r.MemberNo); q = q.Where(x => EF.Functions.Like(x.MemberNo.ToLower(), k, Esc)); }
        if (!string.IsNullOrWhiteSpace(r.CarNo)) { var k = Kw(r.CarNo); q = q.Where(x => EF.Functions.Like((x.CarNo ?? "").ToLower(), k, Esc)); }
        if (!string.IsNullOrWhiteSpace(r.VIN)) { var k = Kw(r.VIN); q = q.Where(x => EF.Functions.Like((x.VIN ?? "").ToLower(), k, Esc)); }
        if (!string.IsNullOrWhiteSpace(r.PhoneNo)) { var k = Kw(r.PhoneNo); q = q.Where(x => EF.Functions.Like((x.PhoneNo ?? "").ToLower(), k, Esc)); }
        if (!string.IsNullOrWhiteSpace(r.MemberName)) { var k = Kw(r.MemberName); q = q.Where(x => EF.Functions.Like((x.MemberName ?? "").ToLower(), k, Esc)); }
        if (!string.IsNullOrWhiteSpace(r.DLCodeRegis)) q = q.Where(x => x.DLCodeRegis == r.DLCodeRegis);
        if (!string.IsNullOrWhiteSpace(r.RegisStatus)) q = q.Where(x => x.RegisStatus == r.RegisStatus);
        if (!string.IsNullOrWhiteSpace(r.MemberStatus)) q = q.Where(x => x.MemberStatus == r.MemberStatus);
        if (!string.IsNullOrWhiteSpace(r.SourceRegisType)) q = q.Where(x => x.SourceRegisType == r.SourceRegisType);
        if (r.RegistrationDateFrom is { } rdf) q = q.Where(x => x.RegistrationDate >= rdf);
        if (r.RegistrationDateTo is { } rdt) q = q.Where(x => x.RegistrationDate <= rdt);
        if (r.MemberActiveDateFrom is { } maf) q = q.Where(x => x.MemberActiveDate >= maf);
        if (r.MemberActiveDateTo is { } mat) q = q.Where(x => x.MemberActiveDate <= mat);
        // cm_FlagShow là cột DẪN XUẤT (CASE trên MemberStatus, Card.cs:11484) — lọc bằng cách quy ngược về status.
        if (!string.IsNullOrWhiteSpace(r.cm_FlagShow))
        {
            var st = r.cm_FlagShow switch
            {
                "-1" => LoyaltyConst.MemberStatus.Pending,
                "1" => LoyaltyConst.MemberStatus.Approve,
                "0" => LoyaltyConst.MemberStatus.Cancel,
                _ => null,
            };
            q = q.Where(x => st != null && x.MemberStatus == st);
        }
        // r.DLCPCode: nguồn lọc qua left join Map_QueryDealer_Member (Card.cs:11415).
        // KHÔNG port được — bảng Map_QueryDealer_Member chưa có ở đích (khai GAP trong báo cáo).

        var myCount = await q.CountAsync();                          // = khối "---- Summary:" (Card.cs:11427)
        var (skip, take) = Page(r);
        var rows = await q.OrderBy(x => x.MemberNo)                  // Card.cs:11424 `order by cmb.MemberNo asc`
                          .Skip(skip).Take(take).ToListAsync();

        // Cột enrich tính được ở đích: cc_CardNo = top 1 thẻ mới nhất (Card.cs:11460-11468).
        var nos = rows.Select(x => x.MemberNo).ToList();
        var lastCard = await db.CrdCards.AsNoTracking().Where(c => nos.Contains(c.MemberNo))
            .Select(c => new { c.MemberNo, c.CardNo, c.CreateDTimeUTC, c.CardSourceCode }).ToListAsync();
        var ccByMember = lastCard.GroupBy(c => c.MemberNo)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(c => c.CreateDTimeUTC).First());

        return Ok(new
        {
            myCount,
            items = rows.Select(x => new
            {
                x.MemberNo, x.MemberName, x.PhoneNo, x.IDCardNo, x.MemberGender, x.DateOfBirth, x.MemAddress,
                x.CarNo, x.VIN, x.ModelCode, x.ModelName, x.TradeMarkCode,
                x.ProvinceCode, x.DistrictCode, x.CustomerCode,
                x.DLCodeRegis, x.DLCodeExceptionally, x.FlagExceptionally,
                x.RegistrationDate, x.MemberActiveDate, x.EffDateStart, x.EffDateEnd,
                x.CardTypeUse, x.CardTypeInit, x.MemberStatus, x.RegisStatus,
                x.PointTotal, x.PointAvail, x.PointEnd, x.PointCardRank,
                x.AmountTotal, x.AmountAvail, x.AmountEnd, x.AmountCardRank,
                x.QtyVisitTotal, x.QtyVisitAvail, x.QtyVisitEnd,
                x.PointVoucher, x.PointActualVoucher, x.PointIntro, x.PointBuyCar, x.PointBuyCreta,
                x.SourceRegisType, x.PurchaseType, x.WarrantyExpiryDate, x.InvoiceDate,
                x.DealNo, x.TVBHName, x.Remark,
                x.CreateDTimeUTC, x.CreateBy, x.LUDTimeUTC, x.LUBy, x.LogLUDTimeUTC, x.LogLUBy,
                //// Cột enrich (alias y như nguồn để đối chiếu được):
                cc_CardNo = ccByMember.TryGetValue(x.MemberNo, out var cc) ? cc.CardNo : null,
                cm_FlagShow = LoyaltyConst.FlagShow(x.MemberStatus),
            }),
            // Khai báo GAP ngay trong payload để người đọc UI không tưởng là "đã đủ cột".
            gapCols = new[] { "mcs_CardSourceName", "mpv_ProvinceName", "mdt_DistrictName",
                              "mdcp_DLCPCode", "mdcp_DLCPName", "mdcpe_DLCPCodeExcep", "mdcpe_DLCPNameExcep",
                              "PurchaseTypeName" },
            gapReason = "Cần master Mst_CardSource/Mst_Province/Mst_District/Mst_DealerCP/Mst_PurchaseType — chưa port sang MiniLoyalty.",
        });
    }

    // ---------------------------------------------------------------------------------------
    // MÀN 2 — THẺ / KỲ XÉT HẠNG.
    // WA_Crd_Card_Get (CrdCardController.cs:163) -> Crd_Card_GetX_New20251229 (Master.Loyalty.cs:19484)
    // CHỌN BẢN: hàm bao gọi ĐÚNG `Crd_Card_GetX_New20251229` (Master.Loyalty.cs:19054);
    //           bản cũ `Crd_Card_GetX` (:19117) là mã chết ⇒ KHÔNG port theo bản cũ.
    // ---------------------------------------------------------------------------------------
    [HttpPost("card/get")]
    public async Task<IActionResult> CardGet([FromBody] CrdCardSearchReq? req)
    {
        var r = req ?? new CrdCardSearchReq();

        // GUARD CỨNG của nguồn (Master.Loyalty.cs:19544-19552): chỉ lấy THẺ MỚI NHẤT của mỗi hội viên
        //   `and cc.CardNo = (select top 1 cc.CardNo ... order by cc.CreateDTimeUTC desc)`.
        // Bỏ guard này là màn nở ra mọi kỳ xét hạng cũ — sai số dòng mà build vẫn xanh.
        var latestCardNos = db.CrdCards.AsNoTracking()
            .GroupBy(c => c.MemberNo)
            .Select(g => g.OrderByDescending(c => c.CreateDTimeUTC).Select(c => c.CardNo).First());

        var q = from c in db.CrdCards.AsNoTracking()
                join m in db.CrdMembers.AsNoTracking() on c.MemberNo equals m.MemberNo   // inner join (:19541)
                where latestCardNos.Contains(c.CardNo)
                select new { c, m };

        if (!string.IsNullOrWhiteSpace(r.CardNo)) { var k = Kw(r.CardNo); q = q.Where(x => EF.Functions.Like(x.c.CardNo.ToLower(), k, Esc)); }
        if (!string.IsNullOrWhiteSpace(r.MemberNo)) { var k = Kw(r.MemberNo); q = q.Where(x => EF.Functions.Like(x.c.MemberNo.ToLower(), k, Esc)); }
        if (!string.IsNullOrWhiteSpace(r.CardSourceCode)) q = q.Where(x => x.c.CardSourceCode == r.CardSourceCode);
        if (!string.IsNullOrWhiteSpace(r.CardStatus)) q = q.Where(x => x.c.CardStatus == r.CardStatus);
        if (!string.IsNullOrWhiteSpace(r.CardTypeUse)) q = q.Where(x => x.c.CardTypeUse == r.CardTypeUse);
        if (!string.IsNullOrWhiteSpace(r.FlagExceptionally)) q = q.Where(x => x.c.FlagExceptionally == r.FlagExceptionally);
        if (r.EffDateStartFrom is { } ef) q = q.Where(x => x.c.EffDateStart >= ef);
        if (r.EffDateStartTo is { } et) q = q.Where(x => x.c.EffDateStart <= et);
        // Ô tìm thuộc bảng Crd_Member (nguồn nạp htSpCols cả 2 bảng — Master.Loyalty.cs:19845-19856):
        if (!string.IsNullOrWhiteSpace(r.VIN)) { var k = Kw(r.VIN); q = q.Where(x => EF.Functions.Like((x.m.VIN ?? "").ToLower(), k, Esc)); }
        if (!string.IsNullOrWhiteSpace(r.CarNo)) { var k = Kw(r.CarNo); q = q.Where(x => EF.Functions.Like((x.m.CarNo ?? "").ToLower(), k, Esc)); }
        if (!string.IsNullOrWhiteSpace(r.PhoneNo)) { var k = Kw(r.PhoneNo); q = q.Where(x => EF.Functions.Like((x.m.PhoneNo ?? "").ToLower(), k, Esc)); }
        if (!string.IsNullOrWhiteSpace(r.IDCardNo)) { var k = Kw(r.IDCardNo); q = q.Where(x => EF.Functions.Like((x.m.IDCardNo ?? "").ToLower(), k, Esc)); }
        if (!string.IsNullOrWhiteSpace(r.DLCodeRegis)) q = q.Where(x => x.m.DLCodeRegis == r.DLCodeRegis);
        if (!string.IsNullOrWhiteSpace(r.MemberStatus)) q = q.Where(x => x.m.MemberStatus == r.MemberStatus);
        if (r.MemberActiveDateFrom is { } maf) q = q.Where(x => x.m.MemberActiveDate >= maf);
        if (r.MemberActiveDateTo is { } mat) q = q.Where(x => x.m.MemberActiveDate <= mat);
        if (r.InactiveDTimeFrom is { } idf) q = q.Where(x => x.m.InactiveDTimeUTC >= idf);
        if (r.InactiveDTimeTo is { } idt) q = q.Where(x => x.m.InactiveDTimeUTC <= idt);

        var myCount = await q.CountAsync();
        var (skip, take) = Page(r);
        var rows = await q.OrderByDescending(x => x.c.CardNo)        // Master.Loyalty.cs:19825 `order by cc.CardNo desc`
                          .Skip(skip).Take(take).ToListAsync();

        // Cột tổng hợp tính được ở đích — port 1:1 2 bảng tạm của nguồn:
        //   #tbl_Crd_CardTransaction_SumAmountRef  : Sum(AmountRef)   where DealPointType='POINTINCREASE' (:19657-19666)
        //   #tbl_Crd_CardTransaction_SumPointUse   : Sum(AmountChTotal) where DealPointType='POINTUSE'    (:19671-19680)
        var cardNos = rows.Select(x => x.c.CardNo).ToList();
        var txs = await db.CrdCardTransactions.AsNoTracking()
            .Where(t => cardNos.Contains(t.CardNo))
            .Select(t => new { t.CardNo, t.DealPointType, t.AmountRef, t.AmountChTotal }).ToListAsync();
        var sumAmountRef = txs.Where(t => t.DealPointType == LoyaltyConst.DealPointType.PointIncrease)
            .GroupBy(t => t.CardNo).ToDictionary(g => g.Key, g => g.Sum(t => t.AmountRef ?? 0));
        var sumPointUse = txs.Where(t => t.DealPointType == LoyaltyConst.DealPointType.PointUse)
            .GroupBy(t => t.CardNo).ToDictionary(g => g.Key, g => g.Sum(t => t.AmountChTotal ?? 0));

        return Ok(new
        {
            myCount,
            items = rows.Select(x => new
            {
                x.c.CardNo, x.c.CardNoPrev, x.c.MemberNo, x.c.CardSourceCode, x.c.RankPolicyCode,
                x.c.DLCodeExceptionally, x.c.PointBonus, x.c.PointPurchaseAddCar,
                x.c.EffDateStart, x.c.EffDateEnd,
                x.c.PointTotal, x.c.PointBlock, x.c.PointAvail,
                x.c.AmountTotal, x.c.AmountBlock, x.c.AmountAvail,
                x.c.QtyVisitTotal, x.c.QtyVisitBlock, x.c.QtyVisitAvail,
                x.c.PointCardRank, x.c.TotalAmountPeriod,
                x.c.CardTypeUse, x.c.CardTypeInit, x.c.CardTypeUsePrev, x.c.CardTypeInitPrev,
                x.c.CardTypeUseExpect, x.c.CardTypeInitExpect, x.c.RankActionType,
                x.c.CardActiveDate, x.c.FlagExceptionally, x.c.CardStatus, x.c.Remark,
                x.c.ApproveDTimeUTC, x.c.ApproveBy, x.c.CancelDTimeUTC, x.c.CancelBy,
                x.c.CreateDTimeUTC, x.c.CreateBy, x.c.LUDTimeUTC, x.c.LUBy,
                //// Cột của Crd_Member đi kèm (nguồn select `cm.*` — Master.Loyalty.cs:19735):
                x.m.MemberName, x.m.PhoneNo, x.m.IDCardNo, x.m.MemberGender, x.m.MemAddress,
                x.m.DateOfBirth, x.m.CarNo, x.m.VIN, x.m.ModelCode, x.m.ProvinceCode, x.m.DistrictCode,
                x.m.TradeMarkCode, x.m.MemberStatus, x.m.MemberActiveDate, x.m.InactiveDTimeUTC,
                x.m.PointBegin, x.m.PointEnd, x.m.AmountEnd, x.m.PointVoucher,
                x.m.PurchaseType, x.m.WarrantyExpiryDate, x.m.InvoiceDate,
                //// Cột dẫn xuất (alias y nguồn):
                cm_FlagShow = LoyaltyConst.FlagShow(x.m.MemberStatus),   // :19742
                cc_FlagShow = LoyaltyConst.FlagShow(x.c.CardStatus),     // :19749
                AmountRef = sumAmountRef.TryGetValue(x.c.CardNo, out var ar) ? ar : 0d,  // :19777
                PointUse = sumPointUse.TryGetValue(x.c.CardNo, out var pu) ? pu : 0d,    // :19789
                PointAccumulated = x.c.PointTotal ?? 0d,                 // :19788 `cc.PointTotal PointAccumulated`
            }),
            gapCols = new[] { "ProvinceName", "DistrictName", "mcm_ModelName", "BrandName",
                              "mdcp_DLCPCode", "mdcp_DLCPName", "PurchaseTypeName",
                              "PointExpiry", "QtyVisitMiss", "RevenueMiss",
                              "ConvertValue", "ConvertPoint", "PolicyAmountUnit", "PolicyPointUnit",
                              "PolicyCode", "ParamCode", "ParamPointUnit", "ParamAmountUnit" },
            gapReason = "Cần Mst_Province/Mst_District/Mst_CarModel/Mst_CarBrand/Mst_DealerCP/Mst_PurchaseType "
                      + "và Mst_RankPolicy + Mst_PolicyMoneyToPointServiceDtl + Mst_ParamSys + RptPE_CrdMemberPointExpiryMember "
                      + "— chưa port sang MiniLoyalty. RevenueMiss/QtyVisitMiss là công thức phụ thuộc Mst_RankPolicy (Master.Loyalty.cs:19762-19775).",
        });
    }

    // ---------------------------------------------------------------------------------------
    // MÀN 3 — GIAO DỊCH ĐIỂM (kèm union chiết khấu).
    // WA_Crd_CardTransaction_Get (CrdCardTransactionController.cs:24) -> Crd_CardTransaction_GetX
    //   (Master.Loyalty.cs:21441). Phần union DISCOUNTRO: Master.Loyalty.cs:21697-21820.
    // ---------------------------------------------------------------------------------------
    [HttpPost("cardtransaction/get")]
    public async Task<IActionResult> CardTransactionGet([FromBody] CrdCardTransactionSearchReq? req)
    {
        var r = req ?? new CrdCardTransactionSearchReq();
        var types = (r.DealPointType ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                          .Select(s => s.ToUpperInvariant()).ToList();
        // Nguồn bật union khi CHUỖI FILTER có chứa DISCOUNTRO (Master.Loyalty.cs:21701 `.ToUpper().Contains(...)`).
        var wantDiscount = types.Contains(LoyaltyConst.DealPointType.DiscountRO);

        // inner join Crd_Member + inner join Crd_Card (Master.Loyalty.cs:21638-21644):
        //   giao dịch không có hội viên/thẻ khớp sẽ BỊ LOẠI — giữ nguyên để số dòng khớp nguồn.
        var q = from t in db.CrdCardTransactions.AsNoTracking()
                join m in db.CrdMembers.AsNoTracking() on t.MemberNo equals m.MemberNo
                join c in db.CrdCards.AsNoTracking() on new { t.MemberNo, t.CardNo } equals new { c.MemberNo, c.CardNo }
                select new { t, m, c };

        if (types.Count > 0) q = q.Where(x => types.Contains((x.t.DealPointType ?? "").ToUpper()));   // IN-list; hạ/nâng chữ vì SQL Server nguồn không phân biệt hoa/thường
        if (!string.IsNullOrWhiteSpace(r.CardNo)) { var k = Kw(r.CardNo); q = q.Where(x => EF.Functions.Like(x.t.CardNo.ToLower(), k, Esc)); }
        if (!string.IsNullOrWhiteSpace(r.MemberNo)) { var k = Kw(r.MemberNo); q = q.Where(x => EF.Functions.Like(x.t.MemberNo.ToLower(), k, Esc)); }
        if (!string.IsNullOrWhiteSpace(r.DLCode)) q = q.Where(x => x.t.DLCode == r.DLCode);
        if (!string.IsNullOrWhiteSpace(r.CardTypeUse)) q = q.Where(x => x.t.CardTypeUse == r.CardTypeUse);
        if (!string.IsNullOrWhiteSpace(r.FlagPoint)) q = q.Where(x => x.t.FlagPoint == r.FlagPoint);
        if (!string.IsNullOrWhiteSpace(r.PrProgramCode)) q = q.Where(x => x.t.PrProgramCode == r.PrProgramCode);
        if (!string.IsNullOrWhiteSpace(r.PhoneNo)) { var k = Kw(r.PhoneNo); q = q.Where(x => EF.Functions.Like((x.m.PhoneNo ?? "").ToLower(), k, Esc)); }
        if (!string.IsNullOrWhiteSpace(r.VIN)) { var k = Kw(r.VIN); q = q.Where(x => EF.Functions.Like((x.m.VIN ?? "").ToLower(), k, Esc)); }
        if (!string.IsNullOrWhiteSpace(r.IDCardNo)) { var k = Kw(r.IDCardNo); q = q.Where(x => EF.Functions.Like((x.m.IDCardNo ?? "").ToLower(), k, Esc)); }
        if (r.EffDateStartFrom is { } ef) q = q.Where(x => x.t.EffDateStart >= ef);
        if (r.EffDateStartTo is { } et) q = q.Where(x => x.t.EffDateStart <= et);
        if (r.UsePrmDTimeFrom is { } uf) q = q.Where(x => x.t.UsePrmDTime >= uf);
        if (r.UsePrmDTimeTo is { } ut) q = q.Where(x => x.t.UsePrmDTime <= ut);
        if (r.CreateDTimeUTCFrom is { } cf) q = q.Where(x => x.t.CreateDTimeUTC >= cf);
        if (r.CreateDTimeUTCTo is { } ct) q = q.Where(x => x.t.CreateDTimeUTC <= ct);
        if (r.PointRefFrom is { } prf) q = q.Where(x => x.t.PointRef >= prf);
        if (r.PointRefTo is { } prt) q = q.Where(x => x.t.PointRef <= prt);
        if (r.PointChRankTotalFrom is { } pcf) q = q.Where(x => x.t.PointChRankTotal >= pcf);
        if (r.PointChRankTotalTo is { } pct) q = q.Where(x => x.t.PointChRankTotal <= pct);
        if (r.AmountChTotalFrom is { } acf) q = q.Where(x => x.t.AmountChTotal >= acf);
        if (r.AmountChTotalTo is { } act) q = q.Where(x => x.t.AmountChTotal <= act);

        // RBAC port 1:1 (Master.Loyalty.cs:21480-21497): cờ bật + DLCPCode có giá trị + KHÁC 'HTV'
        //   -> chặn `and cct.DLCode = <DLCPCode>`. 'HTV' = hãng, không bị chặn.
        if (r.FlagCheckAuthor == "1" && !string.IsNullOrWhiteSpace(r.DLCPCode) && r.DLCPCode != "HTV")
            q = q.Where(x => x.t.DLCode == r.DLCPCode);

        var normal = await q.Select(x => new TxRow
        {
            AutoId = x.t.AutoId, RefNo = x.t.RefNo, ProjectCode = x.t.ProjectCode,
            NetworkID = x.t.NetworkID, OrgID = x.t.OrgID, DLCode = x.t.DLCode,
            MemberNo = x.t.MemberNo, CardNo = x.t.CardNo, CardSourceCode = x.t.CardSourceCode,
            CardTypeUse = x.t.CardTypeUse, CardTypeInit = x.t.CardTypeInit, DealPointType = x.t.DealPointType,
            PrProgramCode = x.t.PrProgramCode, EffDateStart = x.t.EffDateStart, EffDateEnd = x.t.EffDateEnd,
            UsePrmDTime = x.t.UsePrmDTime,
            PointChTotal = x.t.PointChTotal, PointChBlock = x.t.PointChBlock,
            AmountChTotal = x.t.AmountChTotal, AmountChBlock = x.t.AmountChBlock,
            QtyVisitChTotal = x.t.QtyVisitChTotal, QtyVisitChBlock = x.t.QtyVisitChBlock,
            QtyPrChTotal = x.t.QtyPrChTotal, QtyPrChBlock = x.t.QtyPrChBlock, QtyPrUsed = x.t.QtyPrUsed,
            PointRef = x.t.PointRef, AmountRef = x.t.AmountRef,
            PolicyPointUnit = x.t.PolicyPointUnit, PolicyAmountUnit = x.t.PolicyAmountUnit, PolicyCode = x.t.PolicyCode,
            ParamPointUnit = x.t.ParamPointUnit, ParamAmountUnit = x.t.ParamAmountUnit, ParamCode = x.t.ParamCode,
            FlagViewUI = x.t.FlagViewUI, FlagPoint = x.t.FlagPoint, FlagAmount = x.t.FlagAmount, FlagPr = x.t.FlagPr,
            FunctionName = x.t.FunctionName, FunctionActionType = x.t.FunctionActionType, FunctionRemark = x.t.FunctionRemark,
            RefType = x.t.RefType, PointChRankTotal = x.t.PointChRankTotal,
            RefCode00 = x.t.RefCode00, RefCode01 = x.t.RefCode01, RefCode02 = x.t.RefCode02,
            RefCode03 = x.t.RefCode03, RefCode04 = x.t.RefCode04, RefCode05 = x.t.RefCode05,
            RefCode06 = x.t.RefCode06, RefCode07 = x.t.RefCode07, RefCode08 = x.t.RefCode08, RefCode09 = x.t.RefCode09,
            CreateDTimeUTC = x.t.CreateDTimeUTC, CreateBy = x.t.CreateBy,
            LUDTimeUTC = x.t.LUDTimeUTC, LUBy = x.t.LUBy,
            LogLUDTimeUTC = x.t.LogLUDTimeUTC, LogLUBy = x.t.LogLUBy,
            PointExpiryDTime = x.t.PointExpiryDTime, PointExpiryDTimeSync = x.t.PointExpiryDTimeSync,
            PointChTotalInit = x.t.PointChTotalInit, AmountChTotalInit = x.t.AmountChTotalInit,
            PointChTotalDiffer = x.t.PointChTotalDiffer, AmountChTotalDiffer = x.t.AmountChTotalDiffer,
            AmountAvailPrevious = x.t.AmountAvailPrevious,
            CarNo = x.m.CarNo, PhoneNo = x.m.PhoneNo, MemberName = x.m.MemberName,
            DLCodeExceptionally = x.c.DLCodeExceptionally,      // nguồn lấy từ Crd_Card (:21631)
            // Dòng giao dịch điểm thường: 3 cột chiết khấu = 0 (Master.Loyalty.cs:21632-21634).
            DiscountRate = 0, DiscountAmount = 0, DiscountRevenue = 0,
        }).ToListAsync();

        // ---- union all bảng chiết khấu (Master.Loyalty.cs:21736-21820) ----
        // Dòng chiết khấu: MỌI cột điểm-hạng = 0 / null; chỉ 3 cột Discount* mang giá trị.
        var discount = new List<TxRow>();
        if (wantDiscount)
        {
            var dq = from v in db.CrdMemberDiscountTransactions.AsNoTracking()
                     join m in db.CrdMembers.AsNoTracking() on v.MemberNo equals m.MemberNo   // inner join (:21812)
                     select new { v, m };
            if (!string.IsNullOrWhiteSpace(r.CardNo)) { var k = Kw(r.CardNo); dq = dq.Where(x => EF.Functions.Like(x.v.CardNo.ToLower(), k, Esc)); }
            if (!string.IsNullOrWhiteSpace(r.MemberNo)) { var k = Kw(r.MemberNo); dq = dq.Where(x => EF.Functions.Like(x.v.MemberNo.ToLower(), k, Esc)); }
            if (!string.IsNullOrWhiteSpace(r.DLCode)) dq = dq.Where(x => x.v.DLCode == r.DLCode);
            if (!string.IsNullOrWhiteSpace(r.CardTypeUse)) dq = dq.Where(x => x.v.CardTypeUse == r.CardTypeUse);
            if (!string.IsNullOrWhiteSpace(r.PhoneNo)) { var k = Kw(r.PhoneNo); dq = dq.Where(x => EF.Functions.Like((x.m.PhoneNo ?? "").ToLower(), k, Esc)); }
            if (!string.IsNullOrWhiteSpace(r.VIN)) { var k = Kw(r.VIN); dq = dq.Where(x => EF.Functions.Like((x.m.VIN ?? "").ToLower(), k, Esc)); }
            if (r.EffDateStartFrom is { } def) dq = dq.Where(x => x.v.EffDateStart >= def);
            if (r.EffDateStartTo is { } det) dq = dq.Where(x => x.v.EffDateStart <= det);
            if (r.CreateDTimeUTCFrom is { } dcf) dq = dq.Where(x => x.v.CreateDTimeUTC >= dcf);
            if (r.CreateDTimeUTCTo is { } dct) dq = dq.Where(x => x.v.CreateDTimeUTC <= dct);
            if (r.FlagCheckAuthor == "1" && !string.IsNullOrWhiteSpace(r.DLCPCode) && r.DLCPCode != "HTV")
                dq = dq.Where(x => x.v.DLCode == r.DLCPCode);

            discount = await dq.Select(x => new TxRow
            {
                AutoId = x.v.AutoId, RefNo = x.v.RefNo, ProjectCode = null,
                NetworkID = x.v.NetworkID, OrgID = x.v.OrgID, DLCode = x.v.DLCode,
                MemberNo = x.v.MemberNo, CardNo = x.v.CardNo, CardSourceCode = null,
                CardTypeUse = x.v.CardTypeUse, CardTypeInit = x.v.CardTypeInit,
                DealPointType = LoyaltyConst.DealPointType.DiscountRO,   // hằng 'DISCOUNTRO' (:21748)
                PrProgramCode = null, EffDateStart = x.v.EffDateStart, EffDateEnd = x.v.EffDateEnd,
                UsePrmDTime = null,
                PointChTotal = 0, PointChBlock = 0, AmountChTotal = 0, AmountChBlock = 0,
                QtyVisitChTotal = 0, QtyVisitChBlock = 0, QtyPrChTotal = 0, QtyPrChBlock = 0, QtyPrUsed = 0,
                PointRef = 0, AmountRef = 0,
                PolicyPointUnit = null, PolicyAmountUnit = null, PolicyCode = x.v.PolicyCode,
                ParamPointUnit = null, ParamAmountUnit = null, ParamCode = null,
                FlagViewUI = null, FlagPoint = null, FlagAmount = null, FlagPr = null,
                FunctionName = null, FunctionActionType = null, FunctionRemark = null,
                RefType = null, PointChRankTotal = 0,
                RefCode00 = x.v.RefCode00, RefCode01 = x.v.RefCode01, RefCode02 = x.v.RefCode02,
                RefCode03 = x.v.RefCode03, RefCode04 = x.v.RefCode04, RefCode05 = x.v.RefCode05,
                RefCode06 = null, RefCode07 = null, RefCode08 = null, RefCode09 = null,
                CreateDTimeUTC = x.v.CreateDTimeUTC, CreateBy = x.v.CreateBy,
                LUDTimeUTC = x.v.LUDTimeUTC, LUBy = x.v.LUBy,
                LogLUDTimeUTC = x.v.LogLUDTimeUTC, LogLUBy = x.v.LogLUBy,
                PointExpiryDTime = null, PointExpiryDTimeSync = null,
                PointChTotalInit = 0, AmountChTotalInit = 0, PointChTotalDiffer = 0, AmountChTotalDiffer = 0,
                AmountAvailPrevious = 0,
                CarNo = x.m.CarNo, PhoneNo = x.m.PhoneNo, MemberName = x.m.MemberName,
                DLCodeExceptionally = x.v.DLCodeExceptionally,
                DiscountRate = x.v.PolicyDiscountRate ?? 0,   // % chiết khấu (:21812)
                DiscountAmount = x.v.PointChTotal ?? 0,       // Tổng tiền chiết khấu (:21813)
                DiscountRevenue = x.v.AmountForDC ?? 0,       // Doanh thu chiết khấu (:21814)
            }).ToListAsync();
        }

        // `order by LogLUDTimeUTC desc` áp cho TOÀN BỘ tập sau union (Master.Loyalty.cs:21650).
        var all = normal.Concat(discount).OrderByDescending(x => x.LogLUDTimeUTC).ToList();
        var (skip, take) = Page(r);
        return Ok(new { myCount = all.Count, items = all.Skip(skip).Take(take) });
    }

    // ---------------------------------------------------------------------------------------
    // MÀN 4 — CHIẾT KHẤU DỊCH VỤ.
    // WA_Crd_MemberDiscountTransaction_Get (CrdMemberDiscountTransactionController.cs:22)
    //   -> Crd_MemberDiscountTransaction_GetX (Transaction.cs:5424)
    // ---------------------------------------------------------------------------------------
    [HttpPost("discounttransaction/get")]
    public async Task<IActionResult> DiscountGet([FromBody] CrdDiscountSearchReq? req)
    {
        var r = req ?? new CrdDiscountSearchReq();

        // inner join Crd_Member (Transaction.cs:5480) — dòng không có hội viên khớp bị loại.
        var q = from v in db.CrdMemberDiscountTransactions.AsNoTracking()
                join m in db.CrdMembers.AsNoTracking() on v.MemberNo equals m.MemberNo
                select new { v, m };

        // NGUỒN chỉ nạp htSpCols cho Crd_MemberDiscountTransaction; phần Crd_Member BỊ COMMENT
        // (Transaction.cs:5581-5588) ⇒ ô tìm chỉ chạy trên bảng chiết khấu. Giữ đúng, không thêm.
        if (!string.IsNullOrWhiteSpace(r.DLCode)) q = q.Where(x => x.v.DLCode == r.DLCode);
        if (!string.IsNullOrWhiteSpace(r.MemberNo)) { var k = Kw(r.MemberNo); q = q.Where(x => EF.Functions.Like(x.v.MemberNo.ToLower(), k, Esc)); }
        if (!string.IsNullOrWhiteSpace(r.RefNo)) { var k = Kw(r.RefNo); q = q.Where(x => EF.Functions.Like((x.v.RefNo ?? "").ToLower(), k, Esc)); }
        if (!string.IsNullOrWhiteSpace(r.DealPointType)) q = q.Where(x => x.v.DealPointType == r.DealPointType);
        if (r.CreateDateFrom is { } cdf) q = q.Where(x => x.v.CreateDate >= cdf);
        if (r.CreateDateTo is { } cdt) q = q.Where(x => x.v.CreateDate <= cdt);
        if (r.CreateDTimeUTCFrom is { } cuf) q = q.Where(x => x.v.CreateDTimeUTC >= cuf);
        if (r.CreateDTimeUTCTo is { } cut) q = q.Where(x => x.v.CreateDTimeUTC <= cut);
        if (r.PointChTotalFrom is { } pf) q = q.Where(x => x.v.PointChTotal >= pf);
        if (r.PointChTotalTo is { } pt) q = q.Where(x => x.v.PointChTotal <= pt);

        var myCount = await q.CountAsync();
        var (skip, take) = Page(r);
        var rows = await q.OrderByDescending(x => x.v.LogLUDTimeUTC)   // Transaction.cs:5485
                          .Skip(skip).Take(take).ToListAsync();

        return Ok(new
        {
            myCount,
            items = rows.Select(x => new
            {
                x.v.AutoId, x.v.RefNo, x.v.NetworkID, x.v.OrgID, x.v.DLCode,
                x.v.MemberNo, x.v.CardNo,
                x.v.CardTypeUse, x.v.CardTypeInit, x.v.CardTypeApply,
                x.v.DealPointType, x.v.PolicyCode, x.v.PolicyDiscountRate,
                x.v.DLCodeExceptionally, x.v.FlagExceptionally,
                x.v.EffDateStart, x.v.EffDateEnd,
                x.v.PointChTotal, x.v.AmountForDC, x.v.CreateDate,
                x.v.RefCode00, x.v.RefCode01, x.v.RefCode02, x.v.RefCode03, x.v.RefCode04, x.v.RefCode05,
                x.v.CreateDTimeUTC, x.v.CreateBy, x.v.LUDTimeUTC, x.v.LUBy, x.v.LogLUDTimeUTC, x.v.LogLUBy,
                //// Không có ở select nguồn (nguồn comment join) — thêm để UI đọc được, đánh dấu rõ:
                _memberName_khongCoONguon = x.m.MemberName,
            }),
        });
    }

    /// <summary>
    /// Một dòng của màn giao dịch — 72 cột, đúng thứ tự của <c>zzB_Select_Crd_CardTransaction_zzE</c>
    /// (Master.Loyalty.cs:21558-21634) để nhánh union chiết khấu khớp vị trí cột 1:1.
    /// </summary>
    public class TxRow
    {
        public long AutoId { get; set; }
        public string? RefNo { get; set; }
        public string? ProjectCode { get; set; }
        public string? NetworkID { get; set; }
        public string? OrgID { get; set; }
        public string? DLCode { get; set; }
        public string? MemberNo { get; set; }
        public string? CardNo { get; set; }
        public string? CardSourceCode { get; set; }
        public string? CardTypeUse { get; set; }
        public string? CardTypeInit { get; set; }
        public string? DealPointType { get; set; }
        public string? PrProgramCode { get; set; }
        public DateTime? EffDateStart { get; set; }
        public DateTime? EffDateEnd { get; set; }
        public DateTime? UsePrmDTime { get; set; }
        public double? PointChTotal { get; set; }
        public double? PointChBlock { get; set; }
        public double? AmountChTotal { get; set; }
        public double? AmountChBlock { get; set; }
        public double? QtyVisitChTotal { get; set; }
        public double? QtyVisitChBlock { get; set; }
        public double? QtyPrChTotal { get; set; }
        public double? QtyPrChBlock { get; set; }
        public double? QtyPrUsed { get; set; }
        public double? PointRef { get; set; }
        public double? AmountRef { get; set; }
        public double? PolicyPointUnit { get; set; }
        public double? PolicyAmountUnit { get; set; }
        public string? PolicyCode { get; set; }
        public double? ParamPointUnit { get; set; }
        public double? ParamAmountUnit { get; set; }
        public string? ParamCode { get; set; }
        public string? FlagViewUI { get; set; }
        public string? FlagPoint { get; set; }
        public string? FlagAmount { get; set; }
        public string? FlagPr { get; set; }
        public string? FunctionName { get; set; }
        public string? FunctionActionType { get; set; }
        public string? FunctionRemark { get; set; }
        public string? RefType { get; set; }
        public double? PointChRankTotal { get; set; }
        public string? RefCode00 { get; set; }
        public string? RefCode01 { get; set; }
        public string? RefCode02 { get; set; }
        public string? RefCode03 { get; set; }
        public string? RefCode04 { get; set; }
        public string? RefCode05 { get; set; }
        public string? RefCode06 { get; set; }
        public string? RefCode07 { get; set; }
        public string? RefCode08 { get; set; }
        public string? RefCode09 { get; set; }
        public DateTime? CreateDTimeUTC { get; set; }
        public string? CreateBy { get; set; }
        public DateTime? LUDTimeUTC { get; set; }
        public string? LUBy { get; set; }
        public DateTime? LogLUDTimeUTC { get; set; }
        public string? LogLUBy { get; set; }
        public DateTime? PointExpiryDTime { get; set; }
        public DateTime? PointExpiryDTimeSync { get; set; }
        public double? PointChTotalInit { get; set; }
        public double? AmountChTotalInit { get; set; }
        public double? PointChTotalDiffer { get; set; }
        public double? AmountChTotalDiffer { get; set; }
        public double? AmountAvailPrevious { get; set; }
        /// <summary>Từ Crd_Member (nguồn: cm.CarNo — Master.Loyalty.cs:21628).</summary>
        public string? CarNo { get; set; }
        public string? PhoneNo { get; set; }
        public string? MemberName { get; set; }
        /// <summary>Từ Crd_Card ở nhánh thường, từ Crd_MemberDiscountTransaction ở nhánh chiết khấu.</summary>
        public string? DLCodeExceptionally { get; set; }
        public double DiscountRate { get; set; }
        public double DiscountAmount { get; set; }
        public double DiscountRevenue { get; set; }
        //// GAP: PrProgramName / Remark / Unit cần Mst_PromotionProgram + Mst_PromotionProgramDtl
        ////      (left join ở Master.Loyalty.cs:21652-21656) — 2 bảng này chưa port sang MiniLoyalty.
    }
}
