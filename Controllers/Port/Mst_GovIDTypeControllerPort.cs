using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniLoyalty.Data;
using MiniLoyalty.Models.Port;

namespace MiniLoyalty.Controllers.Port;

// ============================================================================================
// PORT 1:1 — màn danh mục loại giấy tờ tuỳ thân (Mst_GovIDType) của dự án nguồn 2022.7.Loyalty (#156, #157, #158).
//
// Nguồn controller (tầng gate):
//   Mst_GovIDTypeController.cs:26-106   Search -> WA_Mst_GovIDType_Get (#156)
//   Mst_GovIDTypeController.cs:108-171  GetByGovIDType -> WA_Mst_GovIDType_Get (#157)
//   Mst_GovIDTypeController.cs:173-233  GetAllActive -> WA_Mst_GovIDType_Get (#158)
//   Mst_GovIDTypeController.cs:435-459  strWhereClause_Mst_GovIDType (mệnh đề lọc Search, GetAllActive)
//   Mst_GovIDTypeController.cs:461-476  strWhereClause_Mst_GovIDType_By_GovIDType (mệnh đề lọc GetByGovIDType)
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
//   * GetAllActive (gate :173-233, #158):
//     - Gate gọi strWhereClause_Mst_GovIDType với duy nhất FlagActive = "1" (:193-196); không truyền KeyWord.
//     - Hàm lọc dùng toán tử IN (:448-456) => IN ('1'); sắp xếp GovIDType asc theo SQL biz (Master.cs:32516).
//     - Ft_RecordStart = 0, Ft_RecordCount = 123456000 theo quy ước lấy hết (gate :208-209 chưa xác minh giá trị thật).
//     - KHÔNG lọc OrgID/NetworkID (gate :205-206 chỉ đưa vào request; biz SQL Master.cs:32508-32534 không lọc).
//     - Data là MẢNG TRẦN (không PageInfo, không MyIdxSeq), null nếu rỗng hoặc Count == 0 (gate :215-220).
//   * GetByGovIDType (gate :108-171, #157):
//     - GovIDType rỗng hoặc null (string.IsNullOrEmpty, gate :128) -> không truy vấn DB, trả Success = true, Data = null (gate :157-158).
//     - Lọc GovIDType bằng toán tử '=' (gate :466-469) không wildcard, collation SQL Server không phân biệt hoa thường (Like + PortQuery.Equal).
//     - KHÔNG lọc FlagActive (gate :130-133, nhánh :470-473 không được kích; bản ghi FlagActive='0' vẫn trả).
//     - KHÔNG lọc OrgID/NetworkID (gate :119-120 chỉ đọc header; biz Master.cs:32508-32534 không lọc).
//     - Ft_RecordStart="0", Ft_RecordCount="1" (gate :145-146), sắp GovIDType asc (Master.cs:32516), lấy phần tử [0] nếu có (gate :152-155).
//     - Data là MỘT ĐỐI TƯỢNG đúng 7 trường (không phải mảng, không PageInfo, không MyIdxSeq, gate :158, model Mst_GovIDType.cs:9-24).
//   * Search (gate :26-106, #156):
//     - Lọc KeyWord: like '%kw%' trên GovIDTypeName (Mst_GovIDTypeController.cs:440-443). KeyWord CHI lọc cột GovIDTypeName, KHÔNG lọc mã GovIDType.
//     - Lọc FlagActive: danh sách IN ('a','b') (Mst_GovIDTypeController.cs:448-456); nhánh `=` (:444-447) BỊ COMMENT nên KHÔNG dùng.
//     - OrgID: gate truyền vào search DTO (:56) nhưng hàm lọc KHÔNG đọc -> OrgID KHÔNG lọc. Biz cũng không lọc NetworkID/OrgID trong SQL (Master.cs:32508-32534).
//     - Draft: select distinct identity(bigint,0,1) MyIdxSeq, mgi.GovIDType từ Mst_GovIDType sắp xếp mgi.GovIDType asc (Master.cs:32509-32517). MyIdxSeq bắt đầu từ 0.
//     - MyCount: Count(0) của draft trước khi cắt trang (Master.cs:32520).
//     - Cắt trang: MyIdxSeq >= start và MyIdxSeq <= end với end = start + count - 1 (Master.cs:32489, :32529-32530). RecordCount = 0 => trang rỗng.
//     - Select cuối: inner join giữa bảng draft đã cắt trang và Mst_GovIDType theo GovIDType, sắp xếp theo t.MyIdxSeq asc (Master.cs:32549-32556).
//     - PageCount: CHỈ tính khi DataList KHÔNG rỗng (Mst_GovIDTypeController.cs:81-89). Khi DataList rỗng -> PageCount = 0.
//     - DataList rỗng là mảng rỗng `[]`, KHÔNG null (Mst_GovIDTypeController.cs:51 new List<TModels.Mst_GovIDType>()).
//     - Kết quả DataList chứa đúng 7 trường theo model nguồn, KHÔNG chứa MyIdxSeq (Mst_GovIDType.cs:9-24).
//
// Bỏ qua có chủ đích (loại Đ):
//   * CheckGatewayAuthentication / CheckAuthorize (gate Mst_GovIDTypeController.cs:35-36, :117-118), Sys_User_CheckAuthorize (Master.cs:32358):
//     hệ thống đích chưa có hệ phân quyền.
//   * Ghi RequestLog / ReturnLog (Master.cs:32339-32346, :32443-32451).
//   * Không chép thông tin đăng nhập nguồn (như GwUserCode, GwPassword tại gate :138-139, biz Master.cs:32341-32342 — chỉ nhắc tên).
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

/// <summary>Port 1:1 <c>Mst_GovIDTypeGetByReq</c> — tham số lấy 1 bản ghi loại giấy tờ tuỳ thân theo mã (Mst_GovIDTypeController.cs:108, :128-133).</summary>
public class MstGovIDTypeGetByReq
{
    public string? GovIDType { get; set; }           // Mã loại giấy tờ tuỳ thân — gate :108, :128-133; toán tử '=' (:466-469)
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
        var pageIndex = r.Ft_PageIndex;
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
    /// Port 1:1 <c>Mst_GovIDTypeController.GetByGovIDType</c> (nguồn gate Mst_GovIDTypeController.cs:108-171).
    /// </summary>
    [HttpPost("getbygovidtype")]
    public async Task<IActionResult> GetByGovIDType([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] MstGovIDTypeGetByReq? req)
    {
        // Nhánh chỉ chạy khi GovIDType != null && !IsNullOrEmpty(GovIDType) (gate Mst_GovIDTypeController.cs:128).
        // Ngược lại KHÔNG gọi service/biz, Data = null, vẫn SetSuccess() -> Success = true (gate :157-158).
        if (string.IsNullOrEmpty(req?.GovIDType))
        {
            return Ok(new
            {
                Success = true,
                Data = (object?)null
            });
        }

        // Gọi GetXAsync với start = 0, count = 1 (Ft_RecordStart="0", Ft_RecordCount="1", gate :145-146).
        // Lọc GovIDType bằng toán tử '=' (gate :466-469).
        // Không lọc FlagActive (gate :130-133, :470-473 không được kích) và không lọc OrgID/NetworkID (gate :119-120, biz Master.cs:32508-32534).
        var (_, rows) = await GetXAsync(null, null, 0, 1, req.GovIDType);

        // Lấy Lst_Mst_GovIDType[0] nếu danh sách khác null và Count > 0, ngược lại null (gate :152-155).
        // Data là MỘT ĐỐI TƯỢNG (không phải mảng, không PageInfo) gồm 7 trường (gate :158, model Mst_GovIDType.cs:9-24, biz Master.cs:32509-32556).
        var data = rows.Count > 0 ? rows[0] : null;

        return Ok(new
        {
            Success = true,
            Data = data
        });
    }

    /// <summary>
    /// Port 1:1 <c>Mst_GovIDTypeController.GetAllActive</c> (nguồn gate Mst_GovIDTypeController.cs:173-233, #158).
    /// </summary>
    [HttpPost("getallactive")]
    public async Task<IActionResult> GetAllActive([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] object? _ = null)
    {
        // Gate Mst_GovIDTypeController.cs:193-196 gọi strWhereClause_Mst_GovIDType với duy nhất FlagActive = "1", không truyền KeyWord.
        // Hàm lọc :448-456 dùng toán tử IN ('1').
        // Không lọc OrgID/NetworkID (gate :205-206 chỉ đưa vào request, SQL biz Master.cs:32508-32534 không lọc).
        // Ft_RecordStart = 0, Ft_RecordCount = 123456000 theo quy ước lấy hết (gate :208-209 chưa xác minh giá trị thật).
        // Sắp xếp GovIDType asc theo SQL biz Master.cs:32516.
        // Tái sử dụng GetXAsync có sẵn theo bài học A.3.
        var (_, rows) = await GetXAsync(null, "1", 0, 123456000);

        // Gate :215-220: Data = lst_Mst_GovIDType (mảng trần, null nếu rỗng hoặc Count == 0), SetSuccess() -> Success = true.
        // Đúng 7 trường theo model nguồn TModels.Mst_GovIDType (Mst_GovIDType.cs:9-24), không MyIdxSeq, không PageInfo.
        return Ok(new
        {
            Success = true,
            Data = rows.Count > 0 ? rows : null
        });
    }

    /// <summary>
    /// Lọc + đếm + chia trang theo GovIDType asc — dùng cho Search và GetByGovIDType.
    /// Triển khai đúng các bước nghiệp vụ nguồn (Master.cs:32456-32609 Mst_GovIDType_GetX):
    /// 1. Draft distinct GovIDType sắp xếp theo GovIDType asc + MyIdxSeq 0-based. MyCount đếm số dòng draft.
    /// 2. Cắt trang theo MyIdxSeq [start, end], inner join lại với Mst_GovIDType và sắp xếp theo MyIdxSeq asc.
    /// Trả về đúng 7 trường theo model nguồn TModels.Mst_GovIDType (không chứa MyIdxSeq).
    /// </summary>
    private async Task<(int itemCount, List<object> rows)> GetXAsync(
        string? keyWord,
        string? flagActiveCsv,
        long recordStart,
        long recordCount,
        string? govIDType = null)
    {
        var q = _db.Set<Mst_GovIDType>().AsNoTracking().AsQueryable();

        // 1. Điều kiện lọc:
        // GovIDType -> toán tử '=' trên mã GovIDType (Mst_GovIDTypeController.cs:466-469).
        // Nguồn chạy SQL Server collation không phân biệt hoa/thường -> hạ chữ cả 2 phía rồi LIKE không wildcard (PortQuery.Equal).
        if (!string.IsNullOrEmpty(govIDType))
        {
            var gid = PortQuery.Equal(govIDType);
            q = q.Where(x => EF.Functions.Like((x.GovIDType ?? "").ToLower(), gid, PortQuery.Esc));
        }

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
