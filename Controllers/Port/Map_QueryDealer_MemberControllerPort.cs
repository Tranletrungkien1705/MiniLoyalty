using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniLoyalty.Data;
using MiniLoyalty.Models;
using MiniLoyalty.Models.Port;

namespace MiniLoyalty.Controllers.Port;

// ============================================================================================
// PORT 1:1 — màn truy vấn hội viên theo đại lý (Map_QueryDealer_Member) của dự án nguồn 2022.7.Loyalty (#66).
//
// Nguồn controller (tầng gate):
//   Map_QueryDealer_MemberController.cs:31-104   Search -> WA_Map_QueryDealer_Member_Get
//   Map_QueryDealer_MemberController.cs:199-259  strWhereClause_Map_QueryDealer_Member (hàm tạo điều kiện lọc)
// Nguồn API:
//   MapQueryDealerMemberController.cs:95-165     WA_Map_QueryDealer_Member_Get
// Nguồn biz:
//   Master.Loyalty.cs:20675-20778                WAS_Map_QueryDealer_Member_Get
//   Master.Loyalty.cs:20779-20932                Map_QueryDealer_Member_Get
//   Master.Loyalty.cs:20933-21140                Map_QueryDealer_Member_GetX (SQL excerpt :21000-21072)
//
// GIỮ ĐÚNG NGUỒN:
//   * Endpoint là POST (nguồn gate Map_QueryDealer_MemberController.cs:31).
//   * Gate nhận 12 tham số: EffDateStartFrom, EffDateStartTo, CardNo, IDCardNo, MemberNo, PhoneNo,
//     DLCPCode, PointRefFrom, PointRefTo, FlagPoint, CarNo, VIN (:31-32).
//   * Truyền vào hàm lọc strWhereClause_Map_QueryDealer_Member (:52-64):
//     - CardNo: Crd_Card.CardNo = (:207-210)
//     - IDCardNo: Crd_Member.IDCardNo = (:212-215)
//     - DLCPCode: tách dấu phẩy -> Map_QueryDealer_Member.DLCPCode IN ('x','y') (:217-225)
//     - MemberNo: Map_QueryDealer_Member.MemberNo = (:227-230)
//     - CarNo: Crd_Member.CarNo = (:232-235)
//     - PhoneNo: Crd_Member.PhoneNo = (:237-240)
//     - VIN: Crd_Member.VIN = (:242-245)
//   * Tham số không dùng:
//     - EffDateStartFrom, EffDateStartTo truyền vào Crd_CardSearch nhưng hàm lọc KHÔNG dùng (:199-259).
//     - PointRefFrom, PointRefTo, FlagPoint không được gate truyền vào hàm lọc (:52-64).
//     - QueryDateFrom, QueryDateTo có trong hàm lọc (:247-255) nhưng gate KHÔNG truyền vào -> nhánh chết trong nguồn, KHÔNG port.
//   * Phân trang cố định ở gate: Ft_RecordStart = "0", Ft_RecordCount = "123456000" (:77-78).
//   * Kết quả trả về: Data = new { DataList = list } (:88-91) — CHỈ có DataList (null nếu danh sách rỗng), KHÔNG có ItemCount/PageCount.
//   * Nghiệp vụ GetX gồm 2 bước SQL (Master.Loyalty.cs:21000-21072):
//     - Bước 1 (Draft :21002-21018): select distinct identity MyIdxSeq, mqdm.DLCPCode, mqdm.MemberNo, mqdm.LogLUDTimeUTC
//       into #tbl_Map_QueryDealer_Member_Filter_Draft từ Map_QueryDealer_Member mqdm
//       inner join Crd_Member cm on mqdm.MemberNo = cm.MemberNo
//       inner join Crd_Card cc on cm.MemberNo = cc.MemberNo
//       + where clause, order by mqdm.LogLUDTimeUTC desc.
//     - Bước 2 (Select cuối :21052-21067): select t.MyIdxSeq, mqdm.*, mdcp.DLCPName, cm.VIN, cm.CarNo, cm.ModelName
//       from #tbl_Map_QueryDealer_Member_Filter t
//       inner join Map_QueryDealer_Member mqdm on t.DLCPCode = mqdm.DLCPCode and t.MemberNo = mqdm.MemberNo
//       left join Mst_DealerCP mdcp on mqdm.DLCPCode = mdcp.DLCPCode
//       inner join Crd_Member cm on mqdm.MemberNo = cm.MemberNo
//       order by mqdm.LogLUDTimeUTC desc.
//   * Khóa suy luận: DDL 20221108.z11.Crd_Member.Crd_Card.sql:12-21 không có PK -> khóa ghép (DLCPCode, MemberNo) suy từ cột join select cuối (cần xác nhận).
//   * Ability (Master.Loyalty.cs:20974-20986): lấy DLCPCode của người dùng đăng nhập; nếu khác rỗng và khác "HTV"
//     thì thêm điều kiện mqdm.DLCPCode = <đại lý user>. Đích CHƯA có Sys_User / session user -> coi như "HTV" (toàn quyền, không lọc đại lý).
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
    /// Thực hiện nghiệp vụ GetX mô phỏng 2 bước SQL tại Master.Loyalty.cs:20933-21140.
    /// Bước 1: Draft (#tbl_Map_QueryDealer_Member_Filter_Draft :21002-21018).
    /// Bước 2: Select cuối (:21052-21067).
    /// </summary>
    private async Task<List<object>> GetXAsync(MapQueryDealerMemberSearchReq filter, long recordStart, long recordCount)
    {
        if (recordCount <= 0)
        {
            return new List<object>();
        }

        // -----------------------------------------------------------------------------------------
        // BƯỚC 1: DRAFT (Master.Loyalty.cs:21002-21018)
        // select distinct identity(bigint, 0, 1) MyIdxSeq, mqdm.DLCPCode, mqdm.MemberNo, mqdm.LogLUDTimeUTC
        // into #tbl_Map_QueryDealer_Member_Filter_Draft
        // from Map_QueryDealer_Member mqdm
        //   inner join Crd_Member cm on mqdm.MemberNo = cm.MemberNo
        //   inner join Crd_Card cc on cm.MemberNo = cc.MemberNo
        // -----------------------------------------------------------------------------------------
        var draftBase = from mqdm in _db.Set<Map_QueryDealer_Member>()
                        join cm in _db.Set<CrdMember>() on mqdm.MemberNo equals cm.MemberNo
                        join cc in _db.Set<CrdCard>() on cm.MemberNo equals cc.MemberNo
                        select new { mqdm, cm, cc };

        // Ability (Master.Loyalty.cs:20974-20986):
        // Nguồn: DataRow drAbilityOfUser = Sys_User_ForCP_GetAbilityViewOfUser(strWAUserCode);
        //        string strDLCPCode = TUtils.CUtils.StdStr(drAbilityOfUser["DLCPCode"]);
        //        if (!string.IsNullOrEmpty(strDLCPCode) && !CmUtils.StringUtils.StringEqual(strDLCPCode, "HTV"))
        //            strFt_WhereClause_All += "and mqdm.DLCPCode = '" + strDLCPCode + "'";
        // Đích CHƯA có Sys_User / session user -> coi như HTV (không lọc theo đại lý của user).
        // GAP: khi có Sys_User phải giới hạn theo đại lý của user.

        // Lọc theo hàm strWhereClause_Map_QueryDealer_Member (gate Map_QueryDealer_MemberController.cs:199-259):
        // Chuỗi so sánh bằng toán tử '=' trong nguồn được ánh xạ sang Like + PortQuery.Equal (hạ chữ 2 phía, không phân biệt hoa thường).

        // CardNo: Crd_Card.CardNo = (:207-210)
        if (!string.IsNullOrWhiteSpace(filter.CardNo))
        {
            var pattern = PortQuery.Equal(filter.CardNo);
            draftBase = draftBase.Where(x => EF.Functions.Like((x.cc.CardNo ?? "").ToLower(), pattern, PortQuery.Esc));
        }

        // IDCardNo: Crd_Member.IDCardNo = (:212-215)
        if (!string.IsNullOrWhiteSpace(filter.IDCardNo))
        {
            var pattern = PortQuery.Equal(filter.IDCardNo);
            draftBase = draftBase.Where(x => EF.Functions.Like((x.cm.IDCardNo ?? "").ToLower(), pattern, PortQuery.Esc));
        }

        // DLCPCode: tách dấu phẩy -> Map_QueryDealer_Member.DLCPCode IN (:217-225)
        if (!string.IsNullOrWhiteSpace(filter.DLCPCode))
        {
            var dlcpList = filter.DLCPCode.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                          .Select(p => p.ToLowerInvariant())
                                          .ToList();
            if (dlcpList.Count > 0)
            {
                draftBase = draftBase.Where(x => dlcpList.Contains((x.mqdm.DLCPCode ?? "").ToLower()));
            }
        }

        // MemberNo: Map_QueryDealer_Member.MemberNo = (:227-230)
        if (!string.IsNullOrWhiteSpace(filter.MemberNo))
        {
            var pattern = PortQuery.Equal(filter.MemberNo);
            draftBase = draftBase.Where(x => EF.Functions.Like((x.mqdm.MemberNo ?? "").ToLower(), pattern, PortQuery.Esc));
        }

        // CarNo: Crd_Member.CarNo = (:232-235)
        if (!string.IsNullOrWhiteSpace(filter.CarNo))
        {
            var pattern = PortQuery.Equal(filter.CarNo);
            draftBase = draftBase.Where(x => EF.Functions.Like((x.cm.CarNo ?? "").ToLower(), pattern, PortQuery.Esc));
        }

        // PhoneNo: Crd_Member.PhoneNo = (:237-240)
        if (!string.IsNullOrWhiteSpace(filter.PhoneNo))
        {
            var pattern = PortQuery.Equal(filter.PhoneNo);
            draftBase = draftBase.Where(x => EF.Functions.Like((x.cm.PhoneNo ?? "").ToLower(), pattern, PortQuery.Esc));
        }

        // VIN: Crd_Member.VIN = (:242-245)
        if (!string.IsNullOrWhiteSpace(filter.VIN))
        {
            var pattern = PortQuery.Equal(filter.VIN);
            draftBase = draftBase.Where(x => EF.Functions.Like((x.cm.VIN ?? "").ToLower(), pattern, PortQuery.Esc));
        }

        // LƯU Ý về các tham số khác:
        // - EffDateStartFrom, EffDateStartTo: Gate truyền vào Crd_CardSearch (:54-55) nhưng hàm strWhereClause_Map_QueryDealer_Member (:199-259) KHÔNG dùng.
        // - PointRefFrom, PointRefTo, FlagPoint: Gate nhận vào (:31-32) nhưng KHÔNG truyền vào hàm tạo where clause (:52-64).
        // - QueryDateFrom, QueryDateTo: Hàm strWhereClause_Map_QueryDealer_Member có hỗ trợ lọc QueryDate >= và <= (:247-255),
        //   nhưng Gate Map_QueryDealer_MemberController.Search (:52-64) KHÔNG truyền 2 tham số này vào -> Nhánh chết trong nguồn, KHÔNG port.

        // Draft distinct và sắp xếp LogLUDTimeUTC desc (:21018 order by mqdm.LogLUDTimeUTC desc)
        var draftQuery = draftBase
            .Select(x => new
            {
                x.mqdm.DLCPCode,
                x.mqdm.MemberNo,
                x.mqdm.LogLUDTimeUTC
            })
            .Distinct()
            .OrderByDescending(x => x.LogLUDTimeUTC);

        // Phân trang trên draft table (#tbl_Map_QueryDealer_Member_Filter :21025-21033)
        var skip = (int)Math.Max(0, Math.Min(recordStart, int.MaxValue));
        var take = (int)Math.Max(0, Math.Min(recordCount, int.MaxValue));

        var draftItems = await draftQuery
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        if (draftItems.Count == 0)
        {
            return new List<object>();
        }

        // Gán số thứ tự MyIdxSeq 0-based tuần tự (identity(bigint, 0, 1) trong SQL nguồn :21003)
        var draftWithSeq = draftItems
            .Select((x, i) => new
            {
                MyIdxSeq = recordStart + i,
                x.DLCPCode,
                x.MemberNo,
                x.LogLUDTimeUTC
            })
            .ToList();

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
}
