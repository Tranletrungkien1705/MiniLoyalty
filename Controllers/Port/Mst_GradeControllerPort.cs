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
// PORT 1:1 — màn danh mục trình độ chuyên môn (Mst_Grade) của dự án nguồn 2022.7.Loyalty (#162, #163 GetAllActive).
//
// Nguồn controller (tầng gate):
//   Mst_GradeController.cs:25-105   Search -> WA_Mst_Grade_Get
//   Mst_GradeController.cs:107-167  GetAllActive -> WA_Mst_Grade_Get (#163 GetAllActive)
//   Mst_GradeController.cs:171-186  strWhereClause_Mst_Grade (mệnh đề lọc Search)
//   Mst_GradeController.cs:188-199  strWhereClause_Mst_Grade_Active (mệnh đề lọc GetAllActive, toán tử '=' FlagActive "1")
// Nguồn API:
//   MstGradeController.cs:17-88     WA_Mst_Grade_Get
// Nguồn biz:
//   MasterData.cs:35544-35634       WAS_Mst_Grade_Get
//   MasterData.cs:35414-35542       Mst_Grade_Get (transaction luôn Rollback :35505, :35513, :35528 - hàm đọc, không ghi DB)
//   MasterData.cs:35257-35412       Mst_Grade_GetX (SQL :35311-35358)
// Nguồn model (từ điển cột):
//   Mst_Grade.cs:9-18               7 thuộc tính (GradeCode, NetworkID, GradeName, GradeDesc, FlagActive, LogLUDTimeUTC, LogLUBy)
//
// GIỮ ĐÚNG NGUỒN:
//   * Endpoint là POST (nguồn [AcceptVerbs("POST")]).
//   * Search lọc KeyWord: like '%kw%' trên GradeName (Mst_GradeController.cs:176-179). CHỈ tìm GradeName, KHÔNG tìm GradeCode, GradeDesc.
//   * Lọc FlagActive: toán tử '=' nguyên giá trị StrValue (Search :180-183; GetAllActive :127-130, :195 cố định "1"), KHÔNG tách dấu phẩy, KHÔNG phải IN.
//   * OrgID: gate nhận OrgID (:25, :55, :119) nhưng hàm lọc strWhereClause_Mst_Grade (:171-186) và strWhereClause_Mst_Grade_Active (:188-199) KHÔNG dùng -> OrgID KHÔNG lọc gì.
//   * SQL không lọc NetworkID/OrgID.
//   * Chuẩn hoá: gate KHÔNG gọi StdParam/StdStr/StdFlag cho KeyWord/FlagActive (:176-183, :127-130) -> không Trim/ToUpper.
//   * SQL Server không phân biệt hoa/thường -> dùng Like trên ToLower() kèm escape '\'.
//   * Draft: select distinct identity(bigint,0,1) MyIdxSeq, mg.GradeCode từ Mst_Grade sắp xếp mg.GradeCode asc (MasterData.cs:35311-35320, :35319).
//   * MyCount: Count(0) của draft trước khi cắt trang (MasterData.cs:35323).
//   * Cắt trang: MyIdxSeq >= start và MyIdxSeq <= end với end = start + count - 1 (MasterData.cs:35292, :35332-35333). RecordCount = 0 => trang rỗng.
//   * Select cuối: inner join draft đã cắt trang với Mst_Grade theo GradeCode, sắp xếp theo t.MyIdxSeq asc (MasterData.cs:35352-35358).
//   * Mỗi dòng trả đúng 8 cột: MyIdxSeq + 7 cột model. MyIdxSeq tính lại từ 0 sau khi lọc.
//   * PageCount (Search): CHỈ tính khi DataList KHÔNG rỗng (Mst_GradeController.cs:80-88). Khi DataList rỗng -> PageCount = 0.
//   * DataList rỗng ở Search là mảng rỗng `[]`, KHÔNG null (Mst_GradeController.cs:50 new List<TModels.Mst_Grade>()).
//   * Data ở GetAllActive (#163): danh sách trần (không bọc PageInfo), rỗng thì Data = null (Mst_GradeController.cs:126, :149-154).
//   * PK = GradeCode (suy từ join, DDL chưa xác minh).
//   * Tài khoản viết cứng tại Mst_GradeController.cs:135-136 — không chép giá trị.
//
// Bỏ qua có chủ đích (loại Đ):
//   * CheckGatewayAuthentication / CheckAuthorize (gate Mst_GradeController.cs:34-35, :116-117),
//     Sys_User_CheckAuthentication (:35465-35469), Sys_Access_CheckDenyV30 (:35472-35476), drAbilityOfUser (:35296)
//     đều bị COMMENT ở nguồn -> KHÔNG port (hệ thống đích chưa có hệ phân quyền).
//   * Ghi RequestLog / ReturnLog (MasterData.cs:35455-35462, :35532-35539).
//   * Transaction: Mst_Grade_Get mở BeginTransaction (:35452) nhưng luôn Rollback (:35505, :35513, :35528) vì là hàm đọc.
// ============================================================================================

/// <summary>Port 1:1 <c>Mst_GradeSearch</c> — tham số lọc + phân trang của màn (Mst_GradeController.cs:25, :52-57).</summary>
public class MstGradeSearchReq
{
    public string? OrgID { get; set; }               // Mã tổ chức — gate nhận (:25, :55) nhưng strWhereClause_Mst_Grade (:171-186) KHÔNG lọc OrgID
    public string? KeyWord { get; set; }             // Từ khoá tìm kiếm theo tên trình độ (GradeName) — :176-179 (KHÔNG tìm GradeCode, GradeDesc)
    public string? FlagActive { get; set; }          // Cờ hoạt động — toán tử '=' nguyên giá trị, KHÔNG tách dấu phẩy — :180-183
    public int Ft_PageIndex { get; set; } = 0;       // Trang (0-based) — :25, :46
    public int Ft_PageSize { get; set; } = 10;       // Số dòng/trang — :25, :47
}

[ApiController]
[Route("api/mst_grade")]
[Produces("application/json")]
public class Mst_GradeControllerPort : ControllerBase
{
    private readonly AppDbContext _db;

    public Mst_GradeControllerPort(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Port 1:1 <c>Mst_GradeController.Search</c> (nguồn gate Mst_GradeController.cs:25-105).
    /// </summary>
    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody] MstGradeSearchReq? req)
    {
        var r = req ?? new MstGradeSearchReq();
        var pageIndex = r.Ft_PageIndex;
        var pageSize = r.Ft_PageSize;

        // Search gọi GetXAsync với start = PageIndex * PageSize, count = PageSize (hành vi nguồn Mst_GradeController.cs:69-70).
        var start = (long)pageIndex * pageSize;
        var (itemCount, rows) = await GetXAsync(r.KeyWord, r.FlagActive, start, pageSize);

        // PageSize = 0 -> DataList rỗng (nguồn: nFilterRecordEnd = start - 1, SQL biz MasterData.cs:35292, gate Mst_GradeController.cs:80-88).
        // PageCount chỉ tính khi DataList KHÔNG rỗng (gate :80-88): ps = PageSize (nếu PageSize=0 thì lấy PageSizeConfig=10) -> ceil(itemCount / ps).
        // Khi DataList rỗng -> PageCount = 0.
        var ps = pageSize == 0 ? 10 : pageSize;
        var pageCount = rows.Count == 0 ? 0 : (itemCount % ps == 0 ? itemCount / ps : itemCount / ps + 1);

        // Trả kết quả theo cấu trúc JsonResultEntry và PageInfo (gate :48-50, :91-92). DataList rỗng là mảng rỗng `[]`, KHÔNG null.
        return Ok(new
        {
            Success = true,
            Data = new PageInfo<object>(rows, itemCount, pageCount, pageIndex, pageSize)
        });
    }

    // Hằng số tạm cho Ft_RecordStart / Ft_RecordCount (nguồn Mst_GradeController.cs:142-143, lớp cơ sở ngoài excerpt — giá trị tạm, cần Claude xác nhận).
    private const long GetAllActiveRecordStart = 0;
    private const long GetAllActiveRecordCount = 123456000;

    /// <summary>
    /// Port 1:1 <c>Mst_GradeController.GetAllActive</c> (nguồn gate Mst_GradeController.cs:107-167, #163 GetAllActive).
    /// Lấy toàn bộ danh mục trình độ có FlagActive = "1" (danh sách trần, không nhận tham số lọc/phân trang từ request).
    /// </summary>
    [HttpPost("getallactive")]
    public async Task<IActionResult> GetAllActive()
    {
        // 1. Lọc: FlagActive = "1" cố định với toán tử '=' (nguồn gate Mst_GradeController.cs:127-130, hàm lọc :195).
        //    Không nhận từ request; KHÔNG có KeyWord; không lọc OrgID/NetworkID (gate :118-120 đọc header nhưng strWhereClause_Mst_Grade_Active :188-199 và SQL :35316-35318 không dùng).
        //    Tài khoản viết cứng tại Mst_GradeController.cs:135-136 — không chép giá trị.
        // 2. Tái sử dụng GetXAsync với recordStart = 0, recordCount = 123456000 (giá trị tạm cho Ft_RecordStart/Ft_RecordCount :142-143, cần Claude xác nhận).
        // 3. Draft sắp xếp theo GradeCode asc, MyIdxSeq bắt đầu từ 0 (MasterData.cs:35313, :35319).
        // 4. Data trả về danh sách trần (không bọc PageInfo, không ItemCount), mỗi dòng gồm đúng 8 trường (MyIdxSeq + 7 cột model).
        // 5. Khi không có dòng nào (hoặc rỗng) -> Data = null (nguồn lst_Mst_Grade khởi tạo null :126, chỉ gán khi Count > 0 :149-152, :154).
        var (_, rows) = await GetXAsync(null, "1", GetAllActiveRecordStart, GetAllActiveRecordCount);

        return Ok(new
        {
            Success = true,
            Data = rows.Count > 0 ? rows : null
        });
    }

    private static string EscapeLike(string s) => s.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    /// <summary>
    /// Lọc + đếm + chia trang theo GradeCode asc — dùng cho Search.
    /// Triển khai ĐÚNG 2 bước nghiệp vụ nguồn (MasterData.cs:35257-35412 Mst_Grade_GetX):
    /// 1. Draft distinct GradeCode sắp xếp theo GradeCode asc + MyIdxSeq 0-based. MyCount đếm số dòng draft.
    /// 2. Cắt trang theo MyIdxSeq [start, end], inner join lại với Mst_Grade theo GradeCode và sắp xếp theo t.MyIdxSeq asc.
    /// Trả về đúng 8 cột: MyIdxSeq + 7 cột model nguồn.
    /// </summary>
    private async Task<(int itemCount, List<object> rows)> GetXAsync(string? keyWord, string? flagActive, long recordStart, long recordCount)
    {
        var q = _db.Set<Mst_Grade>().AsNoTracking().AsQueryable();

        // 1. Điều kiện lọc:
        // KeyWord -> like '%kw%' trên GradeName (Mst_GradeController.cs:176-179).
        // Nguồn chạy SQL Server collation không phân biệt hoa/thường -> hạ chữ ToLower() cả hai phía và LIKE escape '\'.
        // CẢNH BÁO: KeyWord CHỈ lọc trên GradeName, KHÔNG lọc trên GradeCode hay GradeDesc.
        // Nguồn không gọi StdParam/StdStr -> KHÔNG Trim/ToUpper.
        if (!string.IsNullOrEmpty(keyWord))
        {
            var kwLower = "%" + EscapeLike(keyWord.ToLower()) + "%";
            q = q.Where(x => EF.Functions.Like((x.GradeName ?? "").ToLower(), kwLower, "\\"));
        }

        // FlagActive -> toán tử '=' (Mst_GradeController.cs:180-183).
        // Giữ nguyên giá trị StrValue, KHÔNG tách dấu phẩy, KHÔNG phải IN, KHÔNG Trim/ToUpper/StdFlag.
        // LIKE không wildcard biểu diễn '=' không phân biệt hoa/thường.
        if (!string.IsNullOrEmpty(flagActive))
        {
            var faLower = EscapeLike(flagActive.ToLower());
            q = q.Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), faLower, "\\"));
        }

        // OrgID: gate nhận OrgID (Mst_GradeController.cs:25, :55) nhưng strWhereClause_Mst_Grade (:171-186)
        // KHÔNG dùng -> OrgID KHÔNG lọc gì. Header OrgID/NetworkID cũng không vào WHERE (SQL không có NetworkID).

        // Bước 1: Draft (MasterData.cs:35311-35320):
        // select distinct identity(bigint, 0, 1) MyIdxSeq, mg.GradeCode
        // into #tbl_Mst_Grade_Filter_Draft from Mst_Grade mg where (1=1) <loc> order by mg.GradeCode asc
        var draftKeys = await q
            .Select(x => x.GradeCode)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();

        // MyCount: Count(0) của draft trước khi cắt trang (MasterData.cs:35323)
        var itemCount = draftKeys.Count;

        // Cắt trang: MyIdxSeq >= start và MyIdxSeq <= end với end = start + count - 1 (MasterData.cs:35292, :35332-35333).
        // Khi recordCount <= 0 thì end = start - 1 -> trang rỗng.
        var pagedDraft = recordCount <= 0
            ? new List<(long MyIdxSeq, string GradeCode)>()
            : draftKeys
                .Select((k, idx) => (MyIdxSeq: (long)idx, GradeCode: k))
                .Where(t => t.MyIdxSeq >= recordStart && t.MyIdxSeq <= recordStart + recordCount - 1)
                .ToList();

        if (pagedDraft.Count == 0)
        {
            return (itemCount, new List<object>());
        }

        var pagedKeys = pagedDraft.Select(t => t.GradeCode).ToList();

        // Bước 2: Select cuối INNER JOIN (MasterData.cs:35352-35358):
        // select t.MyIdxSeq, mg.* from #tbl_Mst_Grade_Filter t inner join Mst_Grade mg on t.GradeCode = mg.GradeCode order by t.MyIdxSeq asc
        // Mỗi dòng trả đúng 8 cột: MyIdxSeq + 7 cột model.
        var entities = await _db.Set<Mst_Grade>().AsNoTracking()
            .Where(x => pagedKeys.Contains(x.GradeCode))
            .ToListAsync();

        var entityDict = entities.ToDictionary(x => x.GradeCode, StringComparer.OrdinalIgnoreCase);

        var rows = pagedDraft
            .Where(t => entityDict.ContainsKey(t.GradeCode))
            .OrderBy(t => t.MyIdxSeq)
            .Select(t =>
            {
                var mg = entityDict[t.GradeCode];
                return (object)new
                {
                    t.MyIdxSeq,
                    mg.GradeCode,
                    mg.NetworkID,
                    mg.GradeName,
                    mg.GradeDesc,
                    mg.FlagActive,
                    mg.LogLUDTimeUTC,
                    mg.LogLUBy,
                };
            })
            .ToList();

        return (itemCount, rows);
    }
}
