using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniLoyalty.Data;
using MiniLoyalty.Models.Port;

namespace MiniLoyalty.Controllers.Port;

/// <summary>
/// Port 1:1 Mst_OrgController.cs — Mst_Org
/// Chuỗi gọi nguồn:
///   gate: Mst_OrgController.cs:25-105 (Search) + :570-585 (strWhereClause_Mst_Org)
///   API:  MstOrgController.cs:17-88 (WA_Mst_Org_Get)
///   biz:  MasterData.Org.cs:835-932 (WAS_Mst_Org_Get) -> :681-833 (Mst_Org_Get) -> :514-679 (Mst_Org_GetX)
///
/// Bỏ qua có chủ đích (loại Đ):
/// - CheckGatewayAuthentication: Mst_OrgController.cs:34, MstOrgController.cs:40
/// - CheckAuthorize: Mst_OrgController.cs:35
/// - Sys_User_CheckAuthorize: MasterData.Org.cs:745-759 (hệ thống đích chưa có hệ phân quyền)
/// - Sys_Access_CheckDenyV30: MasterData.Org.cs:762-766
/// - Ghi RequestLog/ReturnLog: MasterData.Org.cs:726-733, 823-830
/// </summary>
[ApiController]
[Route("api/mst_org")]
public class Mst_OrgControllerPort : ControllerBase
{
    private readonly AppDbContext _db;

    public Mst_OrgControllerPort(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Port 1:1 Search action từ gate Mst_OrgController.cs:25-105.
    /// Request JSON: { OrgID, KeyWord, FlagActive, Ft_PageIndex = 0, Ft_PageSize = 10 }
    /// Trả về: Ok(new { Success = true, Data = new PageInfo<...>(...) })
    /// </summary>
    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody] Mst_OrgSearchRequest? req)
    {
        req ??= new Mst_OrgSearchRequest();

        // 1. Khởi tạo truy vấn bảng Mst_Org (#tbl_Mst_Org_Filter_Draft trong MasterData.Org.cs:574-583)
        var query = _db.Set<Mst_Org>().AsNoTracking();

        // 2. Lọc theo KeyWord: Mst_OrgController.cs:575-578
        // sbSql.AddWhereClause(Tbl_Mst_Org + TblMst_Org.OrgID, "%" + CUtils.StrValue(data.KeyWord) + "%", "like")
        // Nguồn lọc cột OrgID với toán tử like '%KeyWord%'. Collation không phân biệt hoa/thường: dùng PortQuery.Contains và Esc.
        if (!string.IsNullOrWhiteSpace(req.KeyWord))
        {
            query = query.Where(x => EF.Functions.Like(
                (x.OrgID ?? "").ToLower(),
                PortQuery.Contains(req.KeyWord),
                PortQuery.Esc));
        }

        // 3. Lọc theo FlagActive: Mst_OrgController.cs:579-582
        // sbSql.AddWhereClause(Tbl_Mst_Org + TblMst_Org.FlagActive, CUtils.StrValue(data.FlagActive), "=")
        // Nguồn dùng toán tử '=' -> dùng PortQuery.Equal không thêm wildcard, escape ký tự đặc biệt.
        if (!string.IsNullOrWhiteSpace(req.FlagActive))
        {
            query = query.Where(x => EF.Functions.Like(
                (x.FlagActive ?? "").ToLower(),
                PortQuery.Equal(req.FlagActive),
                PortQuery.Esc));
        }

        // 4. Tham số OrgID của request:
        // Gate Mst_OrgController.cs:25 nhận tham số OrgID và đưa vào Mst_OrgSearch (dòng 52-56),
        // nhưng hàm strWhereClause_Mst_Org (dòng 570-585) hoàn toàn KHÔNG dùng data.OrgID để tạo điều kiện lọc.
        // Tại tầng biz MasterData.Org.cs:756, strOrgID chỉ được dùng trong Sys_User_CheckAuthorize,
        // hoàn toàn KHÔNG truyền vào SQL Mst_Org_GetX (dòng 579-580).
        // Giữ đúng nguồn: KHÔNG lọc theo req.OrgID.

        // 5. Đếm MyCount: MasterData.Org.cs:554, 586
        // "select Count(0) MyCount from #tbl_Mst_Org_Filter_Draft t"
        // Nguồn đếm MyCount trên bảng draft TRƯỚC KHI inner join với Mst_NNT và phân trang
        // => ItemCount VẪN ĐẾM cả Org không có bản ghi trong Mst_NNT.
        var itemCount = await query.CountAsync();

        // 6. Phân trang: MasterData.Org.cs:553-554, 589-597
        // start = PageIndex * PageSize, count = PageSize
        // Khi Ft_PageSize = 0: nFilterRecordStart = 0, nFilterRecordEnd = 0 + 0 - 1 = -1
        // Câu điều kiện (t.MyIdxSeq >= 0 and t.MyIdxSeq <= -1) trả về 0 dòng (DataList = 0 dòng).
        var pageIndex = Math.Max(0, req.Ft_PageIndex);
        var pageSize = req.Ft_PageSize;

        var dataList = new List<Mst_OrgSearchResultItem>();

        if (pageSize > 0)
        {
            // Sắp xếp theo mo.OrgID asc (MasterData.Org.cs:581-582) và phân trang trên tập draft
            var pagedDraft = await query
                .OrderBy(x => x.OrgID)
                .Skip(pageIndex * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Đánh số thứ tự 0-based MyIdxSeq trên toàn tập draft đã sắp xếp (MasterData.Org.cs:575: identity(bigint, 0, 1) MyIdxSeq)
            var startSeq = (long)pageIndex * pageSize;
            var pagedWithSeq = pagedDraft
                .Select((org, idx) => new { Org = org, MyIdxSeq = startSeq + idx })
                .ToList();

            // 7. Inner join Mst_NNT theo OrgID: MasterData.Org.cs:620-623
            // "inner join Mst_NNT mnnt on t.OrgID = mnnt.OrgID"
            // Inner join CHỈ áp dụng cho DataList, do đó Org không có dòng trong Mst_NNT sẽ bị loại khỏi DataList.
            // Khoá Mst_NNT = MST là suy luận (không có DDL) — cần xác nhận.
            var orgIds = pagedWithSeq.Select(x => x.Org.OrgID).ToList();
            var nntList = await _db.Set<Mst_NNT>()
                .AsNoTracking()
                .Where(n => n.OrgID != null && orgIds.Contains(n.OrgID))
                .ToListAsync();

            dataList = (from p in pagedWithSeq
                        join nnt in nntList on p.Org.OrgID equals nnt.OrgID
                        orderby p.MyIdxSeq ascending
                        select new Mst_OrgSearchResultItem
                        {
                            MyIdxSeq = p.MyIdxSeq,
                            OrgID = p.Org.OrgID,
                            NetworkID = p.Org.NetworkID,
                            OrgParent = p.Org.OrgParent,
                            OrgBUCode = p.Org.OrgBUCode,
                            OrgBUPattern = p.Org.OrgBUPattern,
                            OrgLevel = p.Org.OrgLevel,
                            Remark = p.Org.Remark,
                            FlagActive = p.Org.FlagActive,
                            LogLUDTimeUTC = p.Org.LogLUDTimeUTC,
                            LogLUBy = p.Org.LogLUBy,
                            MST = p.Org.MST,
                            mnnt_NNTFullName = nnt.NNTFullName
                        }).ToList();
        }

        // 8. Tính PageCount theo logic tầng gate Mst_OrgController.cs:80-88:
        // if (objRT_Mst_Org.Lst_Mst_Org != null && objRT_Mst_Org.Lst_Mst_Org.Count > 0)
        // {
        //     if (pageSize == 0) pageSize = PageSizeConfig; // PageSizeConfig = 10
        //     pageCount = (itemCount % pageSize == 0) ? itemCount / pageSize : itemCount / pageSize + 1;
        // }
        // Chỉ tính PageCount khi DataList có dòng (Lst_Mst_Org.Count > 0), ngược lại PageCount = 0.
        var pageCount = 0;
        if (dataList.Count > 0)
        {
            var ps = pageSize == 0 ? 10 : pageSize;
            pageCount = (itemCount % ps == 0) ? (itemCount / ps) : (itemCount / ps + 1);
        }

        var pageInfo = new PageInfo<Mst_OrgSearchResultItem>(dataList, itemCount, pageCount, pageIndex, pageSize);

        return Ok(new
        {
            Success = true,
            Data = pageInfo
        });
    }
}

/// <summary>
/// Request model cho action Search — khớp tham số tại Mst_OrgController.cs:25
/// </summary>
public class Mst_OrgSearchRequest
{
    public string? OrgID { get; set; }
    public string? KeyWord { get; set; }
    public string? FlagActive { get; set; }
    public int Ft_PageIndex { get; set; } = 0;
    public int Ft_PageSize { get; set; } = 10;
}

/// <summary>
/// Model từng dòng dữ liệu DataList trả về: 11 cột Mst_Org + MyIdxSeq (0-based) + mnnt_NNTFullName
/// (MasterData.Org.cs:584-586, 615-618).
/// </summary>
public class Mst_OrgSearchResultItem
{
    /// <summary>Số thứ tự 0-based trên toàn tập đã sắp xếp (MasterData.Org.cs:575, 584)</summary>
    public long MyIdxSeq { get; set; }

    /// <summary>Mã tổ chức - Mst_Org.cs:11</summary>
    public string OrgID { get; set; } = "";

    /// <summary>Mã mạng lưới - Mst_Org.cs:13</summary>
    public string? NetworkID { get; set; }

    /// <summary>Mã tổ chức cha - Mst_Org.cs:15</summary>
    public string? OrgParent { get; set; }

    /// <summary>Mã đơn vị kinh doanh BU - Mst_Org.cs:17</summary>
    public string? OrgBUCode { get; set; }

    /// <summary>Mẫu đơn vị kinh doanh BU - Mst_Org.cs:19</summary>
    public string? OrgBUPattern { get; set; }

    /// <summary>Cấp bậc tổ chức - Mst_Org.cs:21</summary>
    public string? OrgLevel { get; set; }

    /// <summary>Ghi chú tổ chức - Mst_Org.cs:23</summary>
    public string? Remark { get; set; }

    /// <summary>Trạng thái kích hoạt - Mst_Org.cs:25</summary>
    public string? FlagActive { get; set; }

    /// <summary>Thời gian cập nhật cuối cùng theo chuẩn UTC - Mst_Org.cs:27</summary>
    public DateTime? LogLUDTimeUTC { get; set; }

    /// <summary>Người dùng thực hiện cập nhật cuối cùng - Mst_Org.cs:29</summary>
    public string? LogLUBy { get; set; }

    /// <summary>Mã số thuế liên kết - Mst_Org.cs:31</summary>
    public string? MST { get; set; }

    /// <summary>Tên người nộp thuế từ bảng Mst_NNT (MasterData.Org.cs:618 mnnt.NNTFullName mnnt_NNTFullName)</summary>
    public string? mnnt_NNTFullName { get; set; }
}
