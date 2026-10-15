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
// PORT 1:1 — màn truy vấn hội viên theo đại lý (Map_QueryDealer_Member) của dự án nguồn 2022.7.Loyalty.
//   - Hạng mục #66: Search -> WA_Map_QueryDealer_Member_Get
//   - Hạng mục #67: SearchForRePort -> WA_Rpt_Map_QueryDealer_Member
//
// Nguồn Search (#66):
//   Gate: Map_QueryDealer_MemberController.cs:31-104 Search -> WA_Map_QueryDealer_Member_Get
//   API:  MapQueryDealerMemberController.cs:95-165 WA_Map_QueryDealer_Member_Get
//   Biz:  Master.Loyalty.cs:20933-21140 Map_QueryDealer_Member_GetX (Draft :21002-21018, Final :21052-21067)
//
// Nguồn SearchForRePort (#67):
//   Gate: Map_QueryDealer_MemberController.cs:106-195 SearchForRePort -> WA_Rpt_Map_QueryDealer_Member
//   Hàm lọc: Map_QueryDealer_MemberController.cs:199-259 strWhereClause_Map_QueryDealer_Member
//   API:  ReportController.cs:865 WA_Rpt_Map_QueryDealer_Member
//   Biz:  Report.cs:11398-11900 -> Rpt_Map_QueryDealer_MemberX (:11645-11900)
//
// GIỮ ĐÚNG NGUỒN VÀ ĐIỂM KHÁC BIỆT #67 VS #66:
//   * Endpoint: POST /api/map_querydealer_member/searchforreport (gate Map_QueryDealer_MemberController.cs:106).
//   * Tham số gate #67: 7 tham số QueryDateFrom, QueryDateTo, MemberNo, PhoneNo, DLCPCode, CarNo, VIN (:106-107).
//     - Khác #66: KHÔNG có CardNo, IDCardNo, EffDateStartFrom/To, PointRefFrom/To, FlagPoint.
//     - QueryDateFrom / QueryDateTo: Ở #66 là nhánh chết; ở #67 ĐƯỢC TRUYỀN VÀO hàm lọc (:149-157)
//       tạo điều kiện Map_QueryDealer_Member.QueryDate >= và QueryDate <=.
//     - Parse ngày theo InvariantCulture; sai định dạng ngày -> SQL lỗi chuyển kiểu -> trả HTTP 400 ErridnLoyalty.Rpt_Map_QueryDealer_Member (Report.cs:11526).
//   * Bước 1: Draft (#tbl_Map_QueryDealer_Member_Filter_Draft Report.cs:11708-11726) tương tự #66, dùng chung logic.
//   * Bước 2: Final (#tbl_Map_QueryDealer_Member_Final Report.cs:11759-11788):
//     - Join: t, Map_QueryDealer_Member mqdm, Crd_Member cm, left join Mst_DealerCP mdcp, left join Mst_Province mp (:11775-11783).
//     - ĐIỀU KIỆN QUAN TRỌNG (:11785): where mqdm.DLCPCode <> cm.DLCodeRegis (CHỈ giữ lượt tra cứu của đại lý KHÁC đại lý mở thẻ).
//     - Cột trả về: t.MyIdxSeq, mqdm.*, mdcp.DLCPName, cm.VIN, cm.CarNo, cm.PhoneNo, cm.CardTypeUse, cm.MemberName,
//       cm.ModelCode, cm.DLCodeRegis, mp.ProvinceName, cm.ModelName, cm.ModelCode + ':' + cm.ModelName ModelCodeName (:11760-11772).
//     - ModelCodeName (:11772): Toán tử `+` trong SQL trả về null nếu ModelCode HOẶC ModelName null.
//     - Sắp xếp: order by mqdm.LogLUDTimeUTC desc (:11787).
//   * CountRow (:11791): select Count(0) MyCount from #tbl_Map_QueryDealer_Member_Final -> đếm SAU KHI lọc <>.
//   * Lst_Rpt_Map_QueryDealer_Member (:11809-11823): count(distinct MemberNo) trong bảng Final
//     -> trả về mảng [{ CountMember = n }] LUÔN CÓ 1 phần tử kể cả khi n = 0.
//   * Output gate (:177-182): Data = new { Lst_Map_QueryDealer_Member (null nếu rỗng), Lst_Rpt_Map_QueryDealer_Member, CountRow = MyCount }.
//   * Ability (Report.cs:11682-11694): Lấy DLCPCode của user đăng nhập; nếu khác rỗng và khác "HTV" thì lọc mqdm.DLCPCode = <đại lý user>.
//     Đích CHƯA có Sys_User / session user -> coi như HTV (không lọc đại lý).
//     GAP: khi có Sys_User / session user phải giới hạn theo đại lý của user.
// ============================================================================================

/// <summary>
/// Port 1:1 tham số request của Gate Search (Map_QueryDealer_MemberController.cs:31-32).
/// Đúng 12 tham số nhận từ client.
/// </summary>
public class MapQueryDealerMemberSearchReq
{
    public string? EffDateStartFrom { get; set; }   // :31 — Nhận nhưng hàm lọc không dùng
    public string? EffDateStartTo { get; set; }     // :31 — Nhận nhưng hàm lọc không dùng
    public string? CardNo { get; set; }             // :31 — Lọc Crd_Card.CardNo =
    public string? IDCardNo { get; set; }           // :31 — Lọc Crd_Member.IDCardNo =
    public string? MemberNo { get; set; }           // :31 — Lọc Map_QueryDealer_Member.MemberNo =
    public string? PhoneNo { get; set; }            // :31 — Lọc Crd_Member.PhoneNo =
    public string? DLCPCode { get; set; }           // :32 — Tách dấu phẩy -> Map_QueryDealer_Member.DLCPCode IN
    public string? PointRefFrom { get; set; }       // :32 — Gate không truyền vào hàm lọc
    public string? PointRefTo { get; set; }         // :32 — Gate không truyền vào hàm lọc
    public string? FlagPoint { get; set; }          // :32 — Gate không truyền vào hàm lọc
    public string? CarNo { get; set; }              // :32 — Lọc Crd_Member.CarNo =
    public string? VIN { get; set; }                // :32 — Lọc Crd_Member.VIN =
}

/// <summary>
/// Port 1:1 tham số request của Gate SearchForRePort (Map_QueryDealer_MemberController.cs:106-107).
/// Đúng 7 tham số nhận từ client.
/// </summary>
public class MapQueryDealerMemberSearchForReportReq
{
    public string? QueryDateFrom { get; set; }      // :106 — Map_QueryDealer_Member.QueryDate >=
    public string? QueryDateTo { get; set; }        // :106 — Map_QueryDealer_Member.QueryDate <=
    public string? MemberNo { get; set; }           // :106 — Map_QueryDealer_Member.MemberNo =
    public string? PhoneNo { get; set; }            // :106 — Crd_Member.PhoneNo =
    public string? DLCPCode { get; set; }           // :107 — Tách dấu phẩy -> Map_QueryDealer_Member.DLCPCode IN
    public string? CarNo { get; set; }              // :107 — Crd_Member.CarNo =
    public string? VIN { get; set; }                // :107 — Crd_Member.VIN =
}

[ApiController]
[Route("api/map_querydealer_member")]
[Produces("application/json")]
public class Map_QueryDealer_MemberControllerPort : ControllerBase
{
    private readonly AppDbContext _db;

    public Map_QueryDealer_MemberControllerPort(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Port 1:1 Map_QueryDealer_MemberController.Search (gate Map_QueryDealer_MemberController.cs:31-104).
    /// </summary>
    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody] MapQueryDealerMemberSearchReq? req)
    {
        var r = req ?? new MapQueryDealerMemberSearchReq();

        // Gate: RecordStart = "0", RecordCount = "123456000" (gate Map_QueryDealer_MemberController.cs:77-78).
        var rows = await GetXAsync(r, 0, 123456000);

        // Gate :88-91: Data = new { DataList = lst_Map_QueryDealer_Member } (null nếu danh sách rỗng, không có ItemCount/PageCount).
        return Ok(new
        {
            Success = true,
            Data = new
            {
                DataList = rows.Count > 0 ? rows : null
            }
        });
    }

    /// <summary>
    /// Port 1:1 Map_QueryDealer_MemberController.SearchForRePort (gate Map_QueryDealer_MemberController.cs:106-195).
    /// Báo cáo truy vấn hội viên theo đại lý (Report.cs:11645-11900 Rpt_Map_QueryDealer_MemberX).
    /// </summary>
    [HttpPost("searchforreport")]
    public async Task<IActionResult> SearchForRePort([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] MapQueryDealerMemberSearchForReportReq? req)
    {
        var r = req ?? new MapQueryDealerMemberSearchForReportReq();

        // Parse QueryDateFrom và QueryDateTo theo InvariantCulture.
        // Nếu sai định dạng ngày -> SQL lỗi chuyển kiểu kẹp trong try/catch -> 400 ErridnLoyalty.Rpt_Map_QueryDealer_Member (Report.cs:11526).
        DateTime? queryDateFrom = null;
        if (!string.IsNullOrWhiteSpace(r.QueryDateFrom))
        {
            if (DateTime.TryParse(r.QueryDateFrom, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dtFrom))
            {
                queryDateFrom = DateTime.SpecifyKind(dtFrom, DateTimeKind.Utc);
            }
            else
            {
                return PortResult.Fail("Rpt_Map_QueryDealer_Member", "Check.QueryDate", r.QueryDateFrom);
            }
        }

        DateTime? queryDateTo = null;
        if (!string.IsNullOrWhiteSpace(r.QueryDateTo))
        {
            if (DateTime.TryParse(r.QueryDateTo, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dtTo))
            {
                queryDateTo = DateTime.SpecifyKind(dtTo, DateTimeKind.Utc);
            }
            else
            {
                return PortResult.Fail("Rpt_Map_QueryDealer_Member", "Check.QueryDate", r.QueryDateTo);
            }
        }

        // Gate: RecordStart = "0", RecordCount = "123456000" (gate Map_QueryDealer_MemberController.cs:156-157).
        var draftWithSeq = await GetDraftItemsAsync(
            cardNo: null,
            idCardNo: null,
            dlcpCode: r.DLCPCode,
            memberNo: r.MemberNo,
            carNo: r.CarNo,
            phoneNo: r.PhoneNo,
            vin: r.VIN,
            queryDateFrom: queryDateFrom,
            queryDateTo: queryDateTo,
            recordStart: 0,
            recordCount: 123456000
        );

        if (draftWithSeq.Count == 0)
        {
            // Khi không có bản ghi nào:
            // Lst_Map_QueryDealer_Member = null (:166)
            // CountRow = 0 (:179-181)
            // Lst_Rpt_Map_QueryDealer_Member = [{ CountMember = 0 }] (:11820 count(t.MemberNo) trên bảng rỗng trả về 1 dòng 0)
            return Ok(new
            {
                Success = true,
                Data = new
                {
                    Lst_Map_QueryDealer_Member = (List<object>?)null,
                    Lst_Rpt_Map_QueryDealer_Member = new List<object> { new { CountMember = 0 } },
                    CountRow = 0
                }
            });
        }

        // -----------------------------------------------------------------------------------------
        // BƯỚC 2: SELECT CUỐI cho #67 (Report.cs:11759-11788)
        // select
        //     t.MyIdxSeq
        //     , mqdm.*
        //     , mdcp.DLCPName
        //     , cm.VIN
        //     , cm.CarNo
        //     , cm.PhoneNo
        //     , cm.CardTypeUse
        //     , cm.MemberName
        //     , cm.ModelCode
        //     , cm.DLCodeRegis
        //     , mp.ProvinceName
        //     , cm.ModelName
        //     , cm.ModelCode + ':' + cm.ModelName ModelCodeName
        // into #tbl_Map_QueryDealer_Member_Final
        // from #tbl_Map_QueryDealer_Member_Filter t
        //     inner join Map_QueryDealer_Member mqdm on t.DLCPCode = mqdm.DLCPCode and t.MemberNo = mqdm.MemberNo
        //     inner join Crd_Member cm on mqdm.MemberNo = cm.MemberNo
        //     left join Mst_DealerCP mdcp on mqdm.DLCPCode = mdcp.DLCPCode
        //     left join Mst_Province mp on cm.ProvinceCode = mp.ProvinceCode
        // where (1=1)
        //     and mqdm.DLCPCode <> cm.DLCodeRegis  -- ĐIỀU KIỆN QUAN TRỌNG: chỉ giữ lượt tra cứu của đại lý KHÁC đại lý mở thẻ (:11785)
        // order by mqdm.LogLUDTimeUTC desc
        // -----------------------------------------------------------------------------------------
        var memberNos = draftWithSeq.Select(d => d.MemberNo).Distinct().ToList();
        var dlcpCodes = draftWithSeq.Select(d => d.DLCPCode).Distinct().ToList();

        var mqdmList = await _db.Set<Map_QueryDealer_Member>()
            .Where(m => memberNos.Contains(m.MemberNo) && dlcpCodes.Contains(m.DLCPCode))
            .ToListAsync();

        var cmList = await _db.Set<CrdMember>()
            .Where(c => memberNos.Contains(c.MemberNo))
            .ToListAsync();

        var mdcpList = await _db.Set<Mst_DealerCP>()
            .Where(d => dlcpCodes.Contains(d.DLCPCode))
            .ToListAsync();

        var provinceCodes = cmList.Select(c => c.ProvinceCode).Where(p => !string.IsNullOrEmpty(p)).Distinct().ToList();
        var mpList = await _db.Set<Mst_Province>()
            .Where(p => provinceCodes.Contains(p.ProvinceCode))
            .ToListAsync();

        var finalRows = new List<object>();
        var finalMemberNos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var t in draftWithSeq)
        {
            var mqdm = mqdmList.FirstOrDefault(m =>
                string.Equals(m.DLCPCode, t.DLCPCode, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(m.MemberNo, t.MemberNo, StringComparison.OrdinalIgnoreCase));
            if (mqdm == null) continue;

            var cm = cmList.FirstOrDefault(c =>
                string.Equals(c.MemberNo, t.MemberNo, StringComparison.OrdinalIgnoreCase));
            if (cm == null) continue;

            // ĐIỀU KIỆN QUAN TRỌNG (Report.cs:11785): where mqdm.DLCPCode <> cm.DLCodeRegis
            // CHỈ giữ lượt tra cứu của đại lý KHÁC đại lý mở thẻ
            // SQL `mqdm.DLCPCode <> cm.DLCodeRegis`: DLCodeRegis NULL ⇒ so sánh UNKNOWN ⇒ dòng BỊ LOẠI (không coi NULL là "").
            if (cm.DLCodeRegis == null
                || string.Equals(mqdm.DLCPCode, cm.DLCodeRegis, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var mdcp = mdcpList.FirstOrDefault(d =>
                string.Equals(d.DLCPCode, t.DLCPCode, StringComparison.OrdinalIgnoreCase));

            var mp = !string.IsNullOrEmpty(cm.ProvinceCode)
                ? mpList.FirstOrDefault(p => string.Equals(p.ProvinceCode, cm.ProvinceCode, StringComparison.OrdinalIgnoreCase))
                : null;

            // ModelCodeName (Report.cs:11772): cm.ModelCode + ':' + cm.ModelName
            // Toán tử `+` trong SQL trả về null nếu ModelCode HOẶC ModelName null.
            string? modelCodeName = (cm.ModelCode != null && cm.ModelName != null)
                ? $"{cm.ModelCode}:{cm.ModelName}"
                : null;

            finalMemberNos.Add(t.MemberNo);

            finalRows.Add(new
            {
                t.MyIdxSeq,
                mqdm.DLCPCode,
                mqdm.MemberNo,
                mqdm.NetworkID,
                mqdm.QueryDate,
                mqdm.Remark,
                mqdm.FlagActive,
                mqdm.LogLUDTimeUTC,
                mqdm.LogLUBy,
                DLCPName = mdcp?.DLCPName,
                cm.VIN,
                cm.CarNo,
                cm.PhoneNo,
                cm.CardTypeUse,
                cm.MemberName,
                cm.ModelCode,
                cm.DLCodeRegis,
                ProvinceName = mp?.ProvinceName,
                cm.ModelName,
                ModelCodeName = modelCodeName
            });
        }

        // Summary CountRow (Report.cs:11791): select Count(0) MyCount from #tbl_Map_QueryDealer_Member_Final
        // Đếm số dòng của bảng Final SAU KHI lọc <> (CountRow sau lọc)
        int countRow = finalRows.Count;

        // Rpt CountMember (Report.cs:11809-11823): count(distinct MemberNo) trong bảng Final
        // Luôn trả về mảng 1 phần tử [{ CountMember = n }] kể cả khi n = 0
        int countMember = finalMemberNos.Count;
        var lstRpt = new List<object> { new { CountMember = countMember } };

        // Gate Map_QueryDealer_MemberController.cs:166-182:
        // Lst_Map_QueryDealer_Member = null nếu rỗng (:166-169)
        return Ok(new
        {
            Success = true,
            Data = new
            {
                Lst_Map_QueryDealer_Member = finalRows.Count > 0 ? finalRows : null,
                Lst_Rpt_Map_QueryDealer_Member = lstRpt,
                CountRow = countRow
            }
        });
    }

    /// <summary>
    /// Thực hiện nghiệp vụ GetX cho Search #66 (Master.Loyalty.cs:20933-21140).
    /// Bước 1: Draft (#tbl_Map_QueryDealer_Member_Filter_Draft :21002-21018).
    /// Bước 2: Select cuối (:21052-21067).
    /// </summary>
    private async Task<List<object>> GetXAsync(MapQueryDealerMemberSearchReq filter, long recordStart, long recordCount)
    {
        var draftWithSeq = await GetDraftItemsAsync(
            cardNo: filter.CardNo,
            idCardNo: filter.IDCardNo,
            dlcpCode: filter.DLCPCode,
            memberNo: filter.MemberNo,
            carNo: filter.CarNo,
            phoneNo: filter.PhoneNo,
            vin: filter.VIN,
            queryDateFrom: null, // Gate Search KHÔNG truyền QueryDate -> không lọc QueryDate
            queryDateTo: null,
            recordStart: recordStart,
            recordCount: recordCount
        );

        if (draftWithSeq.Count == 0)
        {
            return new List<object>();
        }

        // -----------------------------------------------------------------------------------------
        // BƯỚC 2: SELECT CUỐI (Master.Loyalty.cs:21052-21067)
        // select t.MyIdxSeq, mqdm.*, mdcp.DLCPName, cm.VIN, cm.CarNo, cm.ModelName
        // from #tbl_Map_QueryDealer_Member_Filter t
        //   inner join Map_QueryDealer_Member mqdm on t.DLCPCode = mqdm.DLCPCode and t.MemberNo = mqdm.MemberNo
        //   left join Mst_DealerCP mdcp on mqdm.DLCPCode = mdcp.DLCPCode
        //   inner join Crd_Member cm on mqdm.MemberNo = cm.MemberNo
        // order by mqdm.LogLUDTimeUTC desc
        // -----------------------------------------------------------------------------------------
        var memberNos = draftWithSeq.Select(d => d.MemberNo).Distinct().ToList();
        var dlcpCodes = draftWithSeq.Select(d => d.DLCPCode).Distinct().ToList();

        var mqdmList = await _db.Set<Map_QueryDealer_Member>()
            .Where(m => memberNos.Contains(m.MemberNo) && dlcpCodes.Contains(m.DLCPCode))
            .ToListAsync();

        var cmList = await _db.Set<CrdMember>()
            .Where(c => memberNos.Contains(c.MemberNo))
            .ToListAsync();

        var mdcpList = await _db.Set<Mst_DealerCP>()
            .Where(d => dlcpCodes.Contains(d.DLCPCode))
            .ToListAsync();

        var rows = draftWithSeq
            .Select(t =>
            {
                var mqdm = mqdmList.FirstOrDefault(m =>
                    string.Equals(m.DLCPCode, t.DLCPCode, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(m.MemberNo, t.MemberNo, StringComparison.OrdinalIgnoreCase));
                if (mqdm == null) return null;

                var cm = cmList.FirstOrDefault(c =>
                    string.Equals(c.MemberNo, t.MemberNo, StringComparison.OrdinalIgnoreCase));
                if (cm == null) return null;

                var mdcp = mdcpList.FirstOrDefault(d =>
                    string.Equals(d.DLCPCode, t.DLCPCode, StringComparison.OrdinalIgnoreCase));

                return new
                {
                    t.MyIdxSeq,
                    mqdm.DLCPCode,
                    mqdm.MemberNo,
                    mqdm.NetworkID,
                    mqdm.QueryDate,
                    mqdm.Remark,
                    mqdm.FlagActive,
                    mqdm.LogLUDTimeUTC,
                    mqdm.LogLUBy,
                    DLCPName = mdcp?.DLCPName,
                    cm.VIN,
                    cm.CarNo,
                    cm.ModelName
                };
            })
            .Where(x => x != null)
            .OrderByDescending(x => x!.LogLUDTimeUTC)
            .Cast<object>()
            .ToList();

        return rows;
    }

    /// <summary>
    /// Thực hiện BƯỚC 1: DRAFT (#tbl_Map_QueryDealer_Member_Filter_Draft) dùng chung giữa Search #66 và SearchForRePort #67.
    /// Nguồn SQL: Master.Loyalty.cs:21002-21018 (#66) và Report.cs:11708-11726 (#67).
    /// </summary>
    private async Task<List<DraftItem>> GetDraftItemsAsync(
        string? cardNo,
        string? idCardNo,
        string? dlcpCode,
        string? memberNo,
        string? carNo,
        string? phoneNo,
        string? vin,
        DateTime? queryDateFrom,
        DateTime? queryDateTo,
        long recordStart,
        long recordCount)
    {
        if (recordCount <= 0)
        {
            return new List<DraftItem>();
        }

        var draftBase = from mqdm in _db.Set<Map_QueryDealer_Member>()
                        join cm in _db.Set<CrdMember>() on mqdm.MemberNo equals cm.MemberNo
                        join cc in _db.Set<CrdCard>() on cm.MemberNo equals cc.MemberNo
                        select new { mqdm, cm, cc };

        // Ability (Master.Loyalty.cs:20974-20986 / Report.cs:11682-11694):
        // Nguồn: lấy DLCPCode của người dùng đăng nhập; nếu khác rỗng và khác "HTV" thì thêm mqdm.DLCPCode = <đại lý user>.
        // Đích CHƯA có Sys_User / session user -> coi như HTV (không lọc theo đại lý của user).
        // GAP: khi có Sys_User phải giới hạn theo đại lý của user.

        // CardNo: Crd_Card.CardNo = (:207-210)
        if (!string.IsNullOrWhiteSpace(cardNo))
        {
            var pattern = PortQuery.Equal(cardNo);
            draftBase = draftBase.Where(x => EF.Functions.Like((x.cc.CardNo ?? "").ToLower(), pattern, PortQuery.Esc));
        }

        // IDCardNo: Crd_Member.IDCardNo = (:212-215)
        if (!string.IsNullOrWhiteSpace(idCardNo))
        {
            var pattern = PortQuery.Equal(idCardNo);
            draftBase = draftBase.Where(x => EF.Functions.Like((x.cm.IDCardNo ?? "").ToLower(), pattern, PortQuery.Esc));
        }

        // DLCPCode: tách dấu phẩy -> Map_QueryDealer_Member.DLCPCode IN (:217-225)
        if (!string.IsNullOrWhiteSpace(dlcpCode))
        {
            var dlcpList = dlcpCode.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                          .Select(p => p.ToLowerInvariant())
                                          .ToList();
            if (dlcpList.Count > 0)
            {
                draftBase = draftBase.Where(x => dlcpList.Contains((x.mqdm.DLCPCode ?? "").ToLower()));
            }
        }

        // MemberNo: Map_QueryDealer_Member.MemberNo = (:227-230)
        if (!string.IsNullOrWhiteSpace(memberNo))
        {
            var pattern = PortQuery.Equal(memberNo);
            draftBase = draftBase.Where(x => EF.Functions.Like((x.mqdm.MemberNo ?? "").ToLower(), pattern, PortQuery.Esc));
        }

        // CarNo: Crd_Member.CarNo = (:232-235)
        if (!string.IsNullOrWhiteSpace(carNo))
        {
            var pattern = PortQuery.Equal(carNo);
            draftBase = draftBase.Where(x => EF.Functions.Like((x.cm.CarNo ?? "").ToLower(), pattern, PortQuery.Esc));
        }

        // PhoneNo: Crd_Member.PhoneNo = (:237-240)
        if (!string.IsNullOrWhiteSpace(phoneNo))
        {
            var pattern = PortQuery.Equal(phoneNo);
            draftBase = draftBase.Where(x => EF.Functions.Like((x.cm.PhoneNo ?? "").ToLower(), pattern, PortQuery.Esc));
        }

        // VIN: Crd_Member.VIN = (:242-245)
        if (!string.IsNullOrWhiteSpace(vin))
        {
            var pattern = PortQuery.Equal(vin);
            draftBase = draftBase.Where(x => EF.Functions.Like((x.cm.VIN ?? "").ToLower(), pattern, PortQuery.Esc));
        }

        // QueryDateFrom: Map_QueryDealer_Member.QueryDate >= (:247-250)
        if (queryDateFrom.HasValue)
        {
            draftBase = draftBase.Where(x => x.mqdm.QueryDate >= queryDateFrom.Value);
        }

        // QueryDateTo: Map_QueryDealer_Member.QueryDate <= (:252-255)
        if (queryDateTo.HasValue)
        {
            draftBase = draftBase.Where(x => x.mqdm.QueryDate <= queryDateTo.Value);
        }

        // Draft distinct và sắp xếp LogLUDTimeUTC desc (:21018 và :11725 order by mqdm.LogLUDTimeUTC desc)
        var draftQuery = draftBase
            .Select(x => new
            {
                x.mqdm.DLCPCode,
                x.mqdm.MemberNo,
                x.mqdm.LogLUDTimeUTC
            })
            .Distinct()
            .OrderByDescending(x => x.LogLUDTimeUTC);

        // Phân trang trên draft table (#tbl_Map_QueryDealer_Member_Filter :21025-21033 và :11734-11735)
        var skip = (int)Math.Max(0, Math.Min(recordStart, int.MaxValue));
        var take = (int)Math.Max(0, Math.Min(recordCount, int.MaxValue));

        var draftItems = await draftQuery
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        if (draftItems.Count == 0)
        {
            return new List<DraftItem>();
        }

        // Gán số thứ tự MyIdxSeq 0-based tuần tự (identity(bigint, 0, 1) trong SQL nguồn :21003 và :11709)
        return draftItems
            .Select((x, i) => new DraftItem
            {
                MyIdxSeq = recordStart + i,
                DLCPCode = x.DLCPCode,
                MemberNo = x.MemberNo,
                LogLUDTimeUTC = x.LogLUDTimeUTC
            })
            .ToList();
    }

    private sealed class DraftItem
    {
        public long MyIdxSeq { get; set; }
        public string DLCPCode { get; set; } = "";
        public string MemberNo { get; set; } = "";
        public DateTime? LogLUDTimeUTC { get; set; }
    }
}
