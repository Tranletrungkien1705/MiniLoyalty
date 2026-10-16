using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniLoyalty.Data;
using MiniLoyalty.Models.Port;

namespace MiniLoyalty.Controllers.Port;

// ============================================================================================
// PORT 1:1 — màn danh mục loại giấy tờ tuỳ thân (Mst_GovIDType) của dự án nguồn 2022.7.Loyalty (#156).
//
// Nguồn controller (tầng gate):
//   Mst_GovIDTypeController.cs:26-106   Search -> WA_Mst_GovIDType_Get
//   Mst_GovIDTypeController.cs:435-459  strWhereClause_Mst_GovIDType (mệnh đề lọc Search)
// Nguồn API:
//   MstGovIDTypeController.cs:21-90     WA_Mst_GovIDType_Get
// Nguồn biz:
//   Master.cs:32184-32282               WAS_Mst_GovIDType_Get
//   Master.cs:32284-32454               Mst_GovIDType_Get
//   Master.cs:32456-32609               Mst_GovIDType_GetX (SQL :32508-32556)
// Nguồn model (từ điển cột):
//   Mst_GovIDType.cs:9-24               7 thuộc tính (GovIDType, NetworkID, GovIDTypeName, Remark, FlagActive, LogLUDTimeUTC, LogLUBy)
//
// GIỮ ĐÚNG NGUỒN:
//   * Endpoint là POST (nguồn [AcceptVerbs("POST")]).
//   * Lọc KeyWord: like '%kw%' trên GovIDTypeName (Mst_GovIDTypeController.cs:440-443). KeyWord CHI lọc cột GovIDTypeName, KHÔNG lọc mã GovIDType.
//   * Lọc FlagActive: danh sách IN ('a','b') (Mst_GovIDTypeController.cs:448-456); nhánh `=` (:444-447) BỊ COMMENT nên KHÔNG dùng.
//   * OrgID: gate truyền vào search DTO (:56) nhưng hàm lọc KHÔNG đọc -> OrgID KHÔNG lọc. Biz cũng không lọc NetworkID/OrgID trong SQL (Master.cs:32508-32534).
//   * Draft: select distinct identity(bigint,0,1) MyIdxSeq, mgi.GovIDType từ Mst_GovIDType sắp xếp mgi.GovIDType asc (Master.cs:32509-32517). MyIdxSeq bắt đầu từ 0.
//   * MyCount: Count(0) của draft trước khi cắt trang (Master.cs:32520).
//   * Cắt trang: MyIdxSeq >= start và MyIdxSeq <= end với end = start + count - 1 (Master.cs:32489, :32529-32530). RecordCount = 0 => trang rỗng.
//   * Select cuối: inner join giữa bảng draft đã cắt trang và Mst_GovIDType theo GovIDType, sắp xếp theo t.MyIdxSeq asc (Master.cs:32549-32556).
//   * PageCount: CHỈ tính khi DataList KHÔNG rỗng (Mst_GovIDTypeController.cs:81-89). Khi DataList rỗng -> PageCount = 0.
//   * DataList rỗng là mảng rỗng `[]`, KHÔNG null (Mst_GovIDTypeController.cs:51 new List<TModels.Mst_GovIDType>()).
//   * Kết quả DataList chứa đúng 7 trường theo model nguồn, KHÔNG chứa MyIdxSeq (Mst_GovIDType.cs:9-24).
//
// Bỏ qua có chủ đích (loại Đ):
//   * CheckGatewayAuthentication / CheckAuthorize (gate Mst_GovIDTypeController.cs:35-36), Sys_User_CheckAuthorize (Master.cs:32358):
//     hệ thống đích chưa có hệ phân quyền.
//   * Ghi RequestLog / ReturnLog (Master.cs:32339-32346, :32443-32451).
//   * Transaction rollback: Master.cs:32336 mở BeginTransaction và luôn Rollback (:32409, :32417, :32432) vì là action đọc.
// ============================================================================================

/// <summary>Port 1:1 <c>Mst_GovIDTypeSearch</c> — tham số lọc + phân trang của màn (Mst_GovIDTypeController.cs:26, :53-58).</summary>
public class MstGovIDTypeSearchReq
{
    public string? OrgID { get; set; }               // Mã tổ chức — gate nhận (:26, :56) nhưng strWhereClause_Mst_GovIDType (:435-459) KHÔNG lọc OrgID
    public string? KeyWord { get; set; }             // Từ khoá tìm kiếm theo tên loại giấy tờ (GovIDTypeName) — :440-443 (KHÔNG tìm mã GovIDType)
    public string? FlagActive { get; set; }          // Cờ hoạt động, danh sách phân cách dấu phẩy (toán tử IN) — :448-456
    public int Ft_PageIndex { get; set; } = 0;       // Trang (0-based) — :26, :47
    public int Ft_PageSize { get; set; } = 10;       // Số dòng/trang — :26, :48
}

[ApiController]
[Route("api/mst_govidtype")]
[Produces("application/json")]
public class Mst_GovIDTypeControllerPort : ControllerBase
{
    private readonly AppDbContext _db;

    public Mst_GovIDTypeControllerPort(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Port 1:1 <c>Mst_GovIDTypeController.Search</c> (nguồn gate Mst_GovIDTypeController.cs:26-106).
    /// </summary>
    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody] MstGovIDTypeSearchReq? req)
    {
        var r = req ?? new MstGovIDTypeSearchReq();
        var pageIndex = Math.Max(0, r.Ft_PageIndex);
        var pageSize = Math.Max(0, r.Ft_PageSize);

        // Search gọi GetXAsync với start = PageIndex * PageSize, count = PageSize (hành vi nguồn Mst_GovIDTypeController.cs:70-71).
        var start = (long)pageIndex * pageSize;
        var (itemCount, rows) = await GetXAsync(r.KeyWord, r.FlagActive, start, pageSize);

        // PageSize = 0 -> DataList rỗng (nguồn: nFilterRecordEnd = start - 1, SQL biz + gate :81-89).
        // PageCount = 0 khi DataList rỗng; ngược lại ceil(ItemCount / ps) với ps = PageSize (nếu PageSize=0 thì lấy PageSizeConfig=10) — gate :81-89.
        var ps = pageSize == 0 ? 10 : pageSize;
        var pageCount = rows.Count == 0 ? 0 : (itemCount % ps == 0 ? itemCount / ps : itemCount / ps + 1);

        // Trả kết quả theo cấu trúc JsonResultEntry và PageInfo (gate :49-51, :92-93). DataList rỗng là mảng rỗng `[]`.
        return Ok(new
        {
            Success = true,
            Data = new PageInfo<object>(rows, itemCount, pageCount, pageIndex, pageSize)
        });
    }

    /// <summary>
    /// Lọc + đếm + chia trang theo GovIDType asc — dùng cho Search.
    /// Triển khai đúng 2 bước nghiệp vụ nguồn (Master.cs:32456-32609 Mst_GovIDType_GetX):
    /// 1. Draft distinct GovIDType sắp xếp theo GovIDType asc + MyIdxSeq 0-based. MyCount đếm số dòng draft.
    /// 2. Cắt trang theo MyIdxSeq [start, end], inner join lại với Mst_GovIDType và sắp xếp theo MyIdxSeq asc.
    /// Trả về đúng 7 trường theo model nguồn TModels.Mst_GovIDType (không chứa MyIdxSeq).
    /// </summary>
    private async Task<(int itemCount, List<object> rows)> GetXAsync(string? keyWord, string? flagActiveCsv, long recordStart, long recordCount)
    {
        var q = _db.Set<Mst_GovIDType>().AsNoTracking().AsQueryable();

        // 1. Điều kiện lọc:
        // KeyWord -> like '%kw%' trên GovIDTypeName (Mst_GovIDTypeController.cs:440-443).
        // Nguồn chạy SQL Server collation không phân biệt hoa/thường -> hạ chữ cả 2 phía rồi LIKE (bẫy #18/#31).
        // CẢNH BÁO: KeyWord CHỈ lọc trên GovIDTypeName, KHÔNG lọc trên mã GovIDType.
        if (!string.IsNullOrWhiteSpace(keyWord))
        {
            var kw = PortQuery.Contains(keyWord);
            q = q.Where(x => EF.Functions.Like((x.GovIDTypeName ?? "").ToLower(), kw, PortQuery.Esc));
        }

        // FlagActive -> toán tử IN trên FlagActive (Mst_GovIDTypeController.cs:448-456).
        // Tách chuỗi theo dấu phẩy, loại bỏ khoảng trắng thừa, hạ chữ để so sánh không phân biệt hoa thường.
        if (!string.IsNullOrWhiteSpace(flagActiveCsv))
        {
            var ds = flagActiveCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => s.ToLowerInvariant())
                .ToList();
            if (ds.Count > 0)
            {
                q = q.Where(x => ds.Contains((x.FlagActive ?? "").ToLower()));
            }
        }

        // OrgID: KHÔNG lọc (gate nhận tham số OrgID nhưng hàm lọc strWhereClause_Mst_GovIDType không đọc).

        // Bước 1: Draft (Master.cs:32509-32517):
        // select distinct identity(bigint, 0, 1) MyIdxSeq, mgi.GovIDType
        // into #tbl_Mst_GovIDType_Filter_Draft from Mst_GovIDType mgi where (1=1) <where> order by mgi.GovIDType asc
        var draftKeys = await q
            .Select(x => x.GovIDType)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();

        // Bước 2: MyCount đếm số dòng draft trước khi cắt trang (Master.cs:32520)
        var itemCount = draftKeys.Count;

        // Cắt trang: MyIdxSeq >= recordStart và MyIdxSeq <= recordEnd với recordEnd = recordStart + recordCount - 1 (:32489, :32529-32530).
        // Nếu recordCount <= 0 thì end = recordStart - 1 -> trang rỗng.
        var pagedDraft = recordCount <= 0
            ? new List<(long MyIdxSeq, string GovIDType)>()
            : draftKeys
                .Select((k, idx) => (MyIdxSeq: (long)idx, GovIDType: k))
                .Where(t => t.MyIdxSeq >= recordStart && t.MyIdxSeq <= recordStart + recordCount - 1)
                .ToList();

        if (pagedDraft.Count == 0)
        {
            return (itemCount, new List<object>());
        }

        var pagedKeys = pagedDraft.Select(t => t.GovIDType).ToList();

        // Bước 3: Select cuối INNER JOIN (Master.cs:32549-32556):
        // select t.MyIdxSeq, mgi.* from #tbl_Mst_GovIDType_Filter t
        // inner join Mst_GovIDType mgi on t.GovIDType = mgi.GovIDType order by t.MyIdxSeq asc
        var entities = await _db.Set<Mst_GovIDType>()
            .AsNoTracking()
            .Where(x => pagedKeys.Contains(x.GovIDType))
            .ToListAsync();

        var entityDict = entities.ToDictionary(x => x.GovIDType, StringComparer.OrdinalIgnoreCase);

        var rows = new List<object>();
        foreach (var item in pagedDraft)
        {
            if (entityDict.TryGetValue(item.GovIDType, out var entity))
            {
                // Gate map sang TModels.Mst_GovIDType gồm đúng 7 thuộc tính (Mst_GovIDType.cs:9-24), không chứa MyIdxSeq
                rows.Add(new
                {
                    entity.GovIDType,
                    entity.NetworkID,
                    entity.GovIDTypeName,
                    entity.Remark,
                    entity.FlagActive,
                    entity.LogLUDTimeUTC,
                    entity.LogLUBy
                });
            }
        }

        return (itemCount, rows);
    }
}
