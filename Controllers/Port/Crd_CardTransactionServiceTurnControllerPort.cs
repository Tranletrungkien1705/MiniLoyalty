namespace MiniLoyalty.Controllers.Port;

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

/// <summary>
/// DTO nhận tham số tìm kiếm giao dịch lượt dịch vụ thẻ cho action Search.
/// Tương ứng tầng Gate (Crd_CardTransactionServiceTurnController.cs:33-34):
/// DLCode, CreateDTimeUTCFrom, CreateDTimeUTCTo, AmountAvailPreviousFrom, AmountAvailPreviousTo,
/// IsAbleToUsePoint, IsAbleToUsePointNotUse, IsNotAbleToUsePoint (+ Ft_RecordCount).
/// </summary>
public class Crd_CardTransactionServiceTurnSearchReq
{
    public string? DLCode { get; set; }
    public string? CreateDTimeUTCFrom { get; set; }
    public string? CreateDTimeUTCTo { get; set; }
    public string? AmountAvailPreviousFrom { get; set; }
    public string? AmountAvailPreviousTo { get; set; }
    public string? IsAbleToUsePoint { get; set; }
    public string? IsAbleToUsePointNotUse { get; set; }
    public string? IsNotAbleToUsePoint { get; set; }

    public string? Ft_RecordCount { get; set; }
    public string? DLCPCode { get; set; } // Phân quyền đại lý của user (hoặc truyền qua header DLCPCode, mặc định HTV)
}

[ApiController]
[Route("api/crd_cardtransactionserviceturn")]
public class Crd_CardTransactionServiceTurnControllerPort : ControllerBase
{
    private readonly AppDbContext _db;

    public Crd_CardTransactionServiceTurnControllerPort(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Port 1:1 Crd_CardTransactionServiceTurnController.Search (#017).
    /// Chuỗi gọi nguồn:
    ///   - Gate:     13.ClientGate/V10/idn.Loyalty.WebAPI/Controllers/Crd_CardTransactionServiceTurnController.cs:33-118 (Search)
    ///   - Filter:   Crd_CardTransactionServiceTurnController.cs:234-261 (strWhereClause_Crd_CardTransactionServiceTurn),
    ///               :265-348 (strFilterClause_Crd_CardTransactionServiceTurn)
    ///   - Service:  13.ClientGate/V10/idn.Loyalty.ClientService/Services/Loyalty/Crd_CardTransactionServiceTurnService.cs
    ///               (WA_Crd_CardTransactionServiceTurn_Get)
    ///   - API:      11.BackEnd/V10/idn.Loyalty.Biz.Web/Controllers/CrdCardTransactionServiceTurnController.cs:20-87
    ///               (WA_Crd_CardTransactionServiceTurn_Get)
    ///   - Biz LIVE: 11.BackEnd/V10/idn.Loyalty.Biz/Report.cs:12527-12632 (WAS_Crd_CardTransactionServiceTurn_Get),
    ///               :12633-12776 (Crd_CardTransactionServiceTurn_Get),
    ///               :12777-13010 (Crd_CardTransactionServiceTurn_GetX)
    ///
    /// ĐẶC ĐIỂM NGHIỆP VỤ 1:1:
    ///   - Gate nhận các tham số: DLCode, CreateDTimeUTCFrom, CreateDTimeUTCTo, AmountAvailPreviousFrom,
    ///     AmountAvailPreviousTo, IsAbleToUsePoint, IsAbleToUsePointNotUse, IsNotAbleToUsePoint (+ Ft_RecordCount).
    ///   - Chuẩn hoá ngày tháng qua StdDTimeBeginDay/StdDTimeEndDay kết hợp StdDTimeUTC (AddHours(-7)).
    ///   - Phân quyền đại lý: lấy DLCPCode của user từ header hoặc body; nếu != "HTV" thì lọc cct.DLCode = DLCPCode (Report.cs:12820-12826).
    ///   - Biz lọc cố định CreateDTimeUTC >= '2023-06-30 17:00:00' (Report.cs:12853).
    ///   - Truy vấn 3 bước nguồn:
    ///     (a) Filter_Draft: select distinct cct.RefNo từ Crd_DealSerRO cdsro INNER JOIN Crd_CardTransaction cct (SERVICETURN)
    ///         kèm điều kiện Where, phân trang theo RecordStart="0" và Ft_RecordCount (mặc định 123456000 từ BaseController).
    ///     (b) SERVICETURN: INNER JOIN Crd_DealSerRODtl (ExpenseType='ROREPAIR') và INNER JOIN Mst_DealerCP (ĐL không có trong Mst_DealerCP bị loại).
    ///     (c) POINTUSE: LEFT JOIN theo dòng (không GroupBy/Sum: nhiều dòng POINTUSE cùng RefNo => nhân dòng).
    ///   - Lọc theo Ft_FilterClause (AmountAvailPreviousFrom/To, IsAbleToUsePoint, IsAbleToUsePointNotUse, IsNotAbleToUsePoint).
    ///   - Sắp xếp CreateDTimeUTC asc, đánh số STT = 1..N, định dạng CreateDate = yyyy-MM-dd giờ VN (+7h).
    ///   - Phản hồi: khi danh sách rỗng, Lst_Crd_CardTransactionServiceTurn trả về null (gate :96-105).
    /// </summary>
    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Crd_CardTransactionServiceTurnSearchReq? req)
    {
        var r = req ?? new Crd_CardTransactionServiceTurnSearchReq();

        // 1. Phân quyền đại lý người dùng (drAbilityOfUser, Report.cs:12814-12826)
        // Nguồn lấy DLCPCode của user: nếu có và != "HTV" thì cct.DLCode = DLCPCode. Mặc định "HTV" (không lọc).
        string userDLCPCode = "HTV";
        if (Request.Headers.TryGetValue("DLCPCode", out var hdrVal) && !string.IsNullOrWhiteSpace(hdrVal))
        {
            userDLCPCode = hdrVal.ToString().Trim();
        }
        else if (!string.IsNullOrWhiteSpace(r.DLCPCode))
        {
            userDLCPCode = r.DLCPCode.Trim();
        }

        // 2. Chuẩn hoá tham số bộ lọc từ gate
        string? dlCode = !string.IsNullOrWhiteSpace(r.DLCode) ? r.DLCode.Trim() : null;
        DateTime? dFromUtc = ParseCreateDTimeFrom(r.CreateDTimeUTCFrom);
        DateTime? dToUtc = ParseCreateDTimeTo(r.CreateDTimeUTCTo);

        // 3. Mốc thời gian tối thiểu theo nghiệp vụ nguồn (Report.cs:12853):
        // cct.CreateDTimeUTC >= '2023-06-30 17:00:00'
        var minDTimeUtc = new DateTime(2023, 6, 30, 17, 0, 0, DateTimeKind.Utc);

        // 4. Bước 1: Filter_Draft & Filter (Report.cs:12840-12868)
        // select distinct cct.RefNo into #tbl_Crd_CardTransaction_Filter_Draft
        // from Crd_DealSerRO cdsro inner join Crd_CardTransaction cct on cdsro.DealSerRONo = cct.RefNo and cct.DealPointType = 'SERVICETURN'
        var draftQuery = from cdsro in _db.Set<Crd_DealSerRO>().AsNoTracking()
                         join cct in _db.Set<CrdCardTransaction>().AsNoTracking()
                             on cdsro.DealSerRONo equals cct.RefNo
                         where cct.DealPointType == "SERVICETURN"
                            && cct.CreateDTimeUTC >= minDTimeUtc
                         select new
                         {
                             cct.RefNo,
                             cct.DLCode,
                             cct.CreateDTimeUTC
                         };

        // Phân quyền đại lý (drAbilityOfUser): nếu != "HTV" thì lọc cct.DLCode = DLCPCode
        if (!string.Equals(userDLCPCode, "HTV", StringComparison.OrdinalIgnoreCase))
        {
            var p_userDl = PortQuery.Equal(userDLCPCode.ToLower());
            draftQuery = draftQuery.Where(x => x.DLCode != null && EF.Functions.Like(x.DLCode.ToLower(), p_userDl, PortQuery.Esc));
        }

        // Bộ lọc gate: DLCode (toán tử '=', strWhereClause_Crd_CardTransactionServiceTurn :240-243)
        if (!string.IsNullOrWhiteSpace(dlCode))
        {
            var p_dl = PortQuery.Equal(dlCode.ToLower());
            draftQuery = draftQuery.Where(x => x.DLCode != null && EF.Functions.Like(x.DLCode.ToLower(), p_dl, PortQuery.Esc));
        }

        // Bộ lọc gate: CreateDTimeUTCFrom (toán tử '>=', :244-250)
        if (dFromUtc.HasValue)
        {
            draftQuery = draftQuery.Where(x => x.CreateDTimeUTC >= dFromUtc.Value);
        }

        // Bộ lọc gate: CreateDTimeUTCTo (toán tử '<=', :251-257)
        if (dToUtc.HasValue)
        {
            draftQuery = draftQuery.Where(x => x.CreateDTimeUTC <= dToUtc.Value);
        }

        // Phân trang theo RecordStart="0" và Ft_RecordCount (Report.cs:12810-12811, :12860-12868)
        long recordStart = 0;
        long recordCount = 123456000; // BaseController.Ft_RecordCount
        if (!string.IsNullOrWhiteSpace(r.Ft_RecordCount) && long.TryParse(r.Ft_RecordCount.Trim(), out var parsedCount) && parsedCount > 0)
        {
            recordCount = parsedCount;
        }

        var distinctRefNos = await draftQuery
            .Where(x => !string.IsNullOrEmpty(x.RefNo))
            .Select(x => x.RefNo!)
            .Distinct()
            .Skip((int)Math.Min(recordStart, int.MaxValue))
            .Take((int)Math.Min(recordCount, int.MaxValue))
            .ToListAsync();

        if (distinctRefNos.Count == 0)
        {
            return Ok(new
            {
                Success = true,
                Data = new
                {
                    Lst_Crd_CardTransactionServiceTurn = (List<Crd_CardTransactionServiceTurn>?)null
                }
            });
        }

        // 5. Bước 2: SERVICETURN (Report.cs:12886-12902)
        // INNER JOIN Crd_DealSerRODtl cdsrod (ExpenseType = 'ROREPAIR')
        // và INNER JOIN Mst_DealerCP mdcp (cct.DLCode = mdcp.DLCPCode) -> ĐL không có trong Mst_DealerCP bị loại
        var serviceTurns = await (from cct in _db.Set<CrdCardTransaction>().AsNoTracking()
                                  join cdsrod in _db.Set<Crd_DealSerRODtl>().AsNoTracking()
                                      on cct.RefNo equals cdsrod.DealSerRONo
                                  join mdcp in _db.Set<Mst_DealerCP>().AsNoTracking()
                                      on cct.DLCode equals mdcp.DLCPCode
                                  where distinctRefNos.Contains(cct.RefNo!)
                                     && cct.DealPointType == "SERVICETURN"
                                     && cdsrod.ExpenseType == "ROREPAIR"
                                  select new
                                  {
                                      cct.AutoId,
                                      cct.DLCode,
                                      mdcp.DLCPName,
                                      cct.MemberNo,
                                      cct.CardNo,
                                      cct.CardTypeUse,
                                      cct.RefNo,
                                      cct.AmountAvailPrevious,
                                      cct.QtyVisitChTotal,
                                      cct.CreateDTimeUTC,
                                      Amount = cdsrod.Amount
                                  }).ToListAsync();

        if (serviceTurns.Count == 0)
        {
            return Ok(new
            {
                Success = true,
                Data = new
                {
                    Lst_Crd_CardTransactionServiceTurn = (List<Crd_CardTransactionServiceTurn>?)null
                }
            });
        }

        // 6. Bước 3: POINTUSE & JOIN (Report.cs:12905-12935)
        // LEFT JOIN theo dòng (không GroupBy/Sum): nhiều dòng POINTUSE cùng RefNo => nhân dòng; AmountChTotal = isnull(abs(pointuse.AmountChTotal), 0)
        var pointUses = await _db.Set<CrdCardTransaction>().AsNoTracking()
            .Where(x => x.DealPointType == "POINTUSE" && x.RefNo != null && distinctRefNos.Contains(x.RefNo))
            .ToListAsync();

        var joinedList = (from st in serviceTurns
                          join pu in pointUses
                              on st.RefNo equals pu.RefNo into puGroup
                          from pu in puGroup.DefaultIfEmpty()
                          select new Crd_CardTransactionServiceTurn
                          {
                              AutoId = st.AutoId,
                              DLCode = st.DLCode,
                              DLCPName = st.DLCPName,
                              MemberNo = st.MemberNo,
                              CardNo = st.CardNo,
                              CardTypeUse = st.CardTypeUse,
                              RefNo = st.RefNo,
                              AmountAvailPrevious = st.AmountAvailPrevious,
                              AmountChTotal = pu != null ? Math.Abs(pu.AmountChTotal ?? 0) : 0,
                              Amount = st.Amount,
                              QtyVisitChTotal = st.QtyVisitChTotal,
                              CreateDTimeUTC = st.CreateDTimeUTC
                          }).ToList();

        // 7. Bước 4: Lọc Ft_FilterClause (strFilterClause_Crd_CardTransactionServiceTurn, gate :265-348)
        IEnumerable<Crd_CardTransactionServiceTurn> filtered = joinedList;

        if (!string.IsNullOrWhiteSpace(r.AmountAvailPreviousFrom) &&
            double.TryParse(r.AmountAvailPreviousFrom.Trim().Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var prevFrom))
        {
            filtered = filtered.Where(x => (x.AmountAvailPrevious ?? 0) >= prevFrom);
        }

        if (!string.IsNullOrWhiteSpace(r.AmountAvailPreviousTo) &&
            double.TryParse(r.AmountAvailPreviousTo.Trim().Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var prevTo))
        {
            filtered = filtered.Where(x => (x.AmountAvailPrevious ?? 0) <= prevTo);
        }

        bool hasAble = !string.IsNullOrWhiteSpace(r.IsAbleToUsePoint);
        bool hasAbleNotUse = !string.IsNullOrWhiteSpace(r.IsAbleToUsePointNotUse);
        bool hasNotAble = !string.IsNullOrWhiteSpace(r.IsNotAbleToUsePoint);

        if (hasAble || hasAbleNotUse || hasNotAble)
        {
            filtered = filtered.Where(t =>
                (hasAble && ((t.AmountAvailPrevious ?? 0) > 0 && (t.Amount ?? 0) > 0)) ||
                (hasAbleNotUse && ((t.AmountAvailPrevious ?? 0) > 0 && (t.Amount ?? 0) > 0 && (t.AmountChTotal ?? 0) == 0)) ||
                (hasNotAble && ((t.AmountAvailPrevious ?? 0) == 0 || (t.Amount ?? 0) == 0))
            );
        }

        // 8. Bước 5: Sắp xếp CreateDTimeUTC asc (Report.cs:12946), STT = 1..N (Report.cs:12939), CreateDate giờ VN (Report.cs:12599-12602)
        var ordered = filtered.OrderBy(x => x.CreateDTimeUTC).ToList();

        long stt = 1;
        foreach (var item in ordered)
        {
            item.STT = stt++;
            if (item.CreateDTimeUTC.HasValue)
            {
                // StdDTimeVN(CreateDTimeUTC) -> StdDate (Report.cs:12599-12602)
                item.CreateDate = item.CreateDTimeUTC.Value.AddHours(7).ToString("yyyy-MM-dd");
            }
        }

        // 9. Phản hồi: khi danh sách rỗng, Lst_Crd_CardTransactionServiceTurn trả về null (gate :96-105)
        return Ok(new
        {
            Success = true,
            Data = new
            {
                Lst_Crd_CardTransactionServiceTurn = ordered.Count > 0 ? ordered : null
            }
        });
    }

    /// <summary>
    /// Chuẩn hoá CreateDTimeUTCFrom theo TUtils.CUtils.StdDTimeBeginDay kết hợp StdDTimeUTC:
    /// Lấy mốc 00:00:00 ngày đầu theo giờ VN, trừ 7 giờ để quy đổi sang chuẩn UTC.
    /// </summary>
    private static DateTime? ParseCreateDTimeFrom(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        if (DateTime.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt) ||
            DateTime.TryParse(input, out dt))
        {
            var beginDayLocal = dt.Date;
            return DateTime.SpecifyKind(beginDayLocal.AddHours(-7), DateTimeKind.Utc);
        }
        return null;
    }

    /// <summary>
    /// Chuẩn hoá CreateDTimeUTCTo theo TUtils.CUtils.StdDTimeEndDay kết hợp StdDTimeUTC:
    /// Lấy mốc 23:59:59 cuối ngày theo giờ VN, trừ 7 giờ để quy đổi sang chuẩn UTC.
    /// </summary>
    private static DateTime? ParseCreateDTimeTo(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        if (DateTime.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt) ||
            DateTime.TryParse(input, out dt))
        {
            var endDayLocal = dt.Date.AddDays(1).AddSeconds(-1);
            return DateTime.SpecifyKind(endDayLocal.AddHours(-7), DateTimeKind.Utc);
        }
        return null;
    }
}
