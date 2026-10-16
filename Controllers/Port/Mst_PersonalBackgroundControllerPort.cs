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
// PORT 1:1 — màn danh mục thành phần bản thân (Mst_PersonalBackground) của dự án nguồn 2022.7.Loyalty (#188).
//
// Nguồn controller (tầng gate):
//   Mst_PersonalBackgroundController.cs:25-105   Search              -> WA_Mst_PersonalBackground_Get (#188)
//   Mst_PersonalBackgroundController.cs:107-167  GetAllActive        -> WA_Mst_PersonalBackground_Get (#189)
//   Mst_PersonalBackgroundController.cs:171-186  strWhereClause_Mst_PersonalBackground (mệnh đề lọc Search)
//   Mst_PersonalBackgroundController.cs:188-199  strWhereClause_Mst_PersonalBackground_Active (mệnh đề lọc GetAllActive)
// Nguồn API:
//   MstPersonalBackgroundController.cs:17-88     WA_Mst_PersonalBackground_Get
// Nguồn biz:
//   MasterData.cs:45212-45302                    WAS_Mst_PersonalBackground_Get
//   MasterData.cs:45082-45210                    Mst_PersonalBackground_Get
//   MasterData.cs:44925-45080                    Mst_PersonalBackground_GetX (SQL :44978-45030)
// Nguồn model (từ điển cột):
//   12.Dev.Common\idn.Loyalty.Common\Models\HRM\Mst_PersonalBackground.cs:9-18 (đúng 7 cột)
//
// GIỮ ĐÚNG NGUỒN:
//   * Endpoint là POST (nguồn [AcceptVerbs("POST")]).
//   * Lọc KeyWord: like '%kw%' trên PersonalBGName (Mst_PersonalBackgroundController.cs:176-179),
//     KHÔNG lọc trên PersonalBGCode.
//   * Lọc FlagActive: toán tử '=' nguyên chuỗi (Mst_PersonalBackgroundController.cs:180-183),
//     KHÔNG IN, KHÔNG tách dấu phẩy ("0,1" -> 0 dòng).
//   * OrgID: gate nhận (Mst_PersonalBackgroundController.cs:25, :37, :55) nhưng strWhereClause_Mst_PersonalBackground
//     và SQL biz KHÔNG dùng OrgID, bảng không có cột OrgID -> KHÔNG lọc.
//   * Phân trang gate: Ft_RecordStart = (pageIndex * pageSize).ToString(), Ft_RecordCount = pageSize.ToString() (:69-70).
//   * Draft sắp xếp theo mpbg.PersonalBGCode asc (MasterData.cs:45002).
//   * Select cuối sắp xếp theo t.MyIdxSeq asc (MasterData.cs:45026).
//   * MyIdxSeq 0-based identity (MasterData.cs:44981).
//   * PageSize = 0 -> DataList rỗng [], PageCount = 0 (gate :48-50, :80-90).
//   * Chuẩn hoá input: gate chỉ gọi CUtils.StrValue, biz KHÔNG gọi StdParam/StdStr/StdFlag.
//   * Trả về JsonResultEntry với Data là PageInfo<Mst_PersonalBackground> (gate :48-51, :91-93).
//
// Bỏ qua có chủ đích (loại Đ):
//   * CheckGatewayAuthentication: gate :34, API :40 (hệ thống đích chưa có hệ gateway)
//   * CheckAuthorize: gate :35 (hệ thống đích chưa có hệ phân quyền)
//   * Tài khoản viết cứng tại Mst_PersonalBackgroundController.cs:62-63 — không chép giá trị (cấm chép mật khẩu)
//   * Ghi RequestLog/ReturnLog: MasterData.cs:45123-45130, :45200-45207
//   * Sys_Access_CheckDenyV30: MasterData.cs:45140-45144 (đã bị comment ở nguồn)
//   * Sys_User_CheckAuthentication: MasterData.cs:45133-45137 (đã bị comment ở nguồn)
// ============================================================================================

/// <summary>Port 1:1 <c>Mst_PersonalBackgroundSearch</c> — tham số lọc + phân trang (Mst_PersonalBackgroundController.cs:25, 52-57).</summary>
public class MstPersonalBackgroundSearchReq
{
    public string? OrgID { get; set; }               // Tổ chức — gate nhận (:25, :55) nhưng strWhereClause_Mst_PersonalBackground (:171-186) và SQL biz KHÔNG lọc OrgID
    public string? KeyWord { get; set; }             // Từ khoá tìm theo tên thành phần bản thân (PersonalBGName) — :25, :54, :178
    public string? FlagActive { get; set; }          // Cờ hoạt động, toán tử '=' — :25, :56, :182
    public int Ft_PageIndex { get; set; } = 0;       // Trang (0-based) — :25, :46
    public int Ft_PageSize { get; set; } = 10;       // Số dòng/trang — :25, :47
}

[ApiController]
[Route("api/mst_personalbackground")]
[Produces("application/json")]
public class Mst_PersonalBackgroundControllerPort : ControllerBase
{
    private readonly AppDbContext _db;

    public Mst_PersonalBackgroundControllerPort(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] MstPersonalBackgroundSearchReq? req)
    {
        var r = req ?? new MstPersonalBackgroundSearchReq();
        var pageIndex = r.Ft_PageIndex;
        var pageSize = r.Ft_PageSize;

        // Search gọi GetXAsync với start = PageIndex * PageSize, count = PageSize (giữ nguyên hành vi cũ nguồn Mst_PersonalBackgroundController.cs:69-70).
        var start = (long)pageIndex * pageSize;
        var (itemCount, rows) = await GetXAsync(r.KeyWord, r.FlagActive, start, pageSize);

        // PageSize=0 -> DataList rỗng (nguồn: nFilterRecordEnd = start-1, SQL biz + gate Mst_PersonalBackgroundController.cs:80-88).
        // PageCount = 0 khi DataList rỗng, ngược lại ceil(ItemCount / ps) với ps = PageSize (PageSize=0 thì 10) — gate :83-87.
        var ps = pageSize == 0 ? 10 : pageSize;
        var pageCount = rows.Count == 0 ? 0 : (itemCount % ps == 0 ? itemCount / ps : itemCount / ps + 1);

        return Ok(new { Success = true, Data = new PageInfo<object>(rows, itemCount, pageCount, pageIndex, pageSize) });
    }

    /// <summary>
    /// Port 1:1 <c>Mst_PersonalBackgroundController.GetAllActive</c> (nguồn gate Mst_PersonalBackgroundController.cs:107-167, #189).
    /// Nguồn gọi <c>strWhereClause_Mst_PersonalBackground_Active</c> với <c>FlagActive = "1"</c> (:127-130).
    /// Hàm lọc dùng toán tử '=' nguyên chuỗi (:195), không lọc OrgID hay KeyWord.
    /// Ft_RecordStart = "0", Ft_RecordCount = "123456000" (BaseController.cs:30-31, gate :142-143).
    /// Trả về mảng trần lst_Mst_PersonalBackground, null khi rỗng (gate :126, :149-154).
    /// </summary>
    [HttpPost("getallactive")]
    public async Task<IActionResult> GetAllActive()
    {
        // Gọi GetXAsync với keyWord = null, flagActive = "1", recordStart = 0, recordCount = 123456000
        // (nguồn: gate Mst_PersonalBackgroundController.cs:127-130, :142-143, BaseController.cs:30-31).
        var (_, rows) = await GetXAsync(null, "1", 0, 123456000);

        // Nguồn: lst_Mst_PersonalBackground = null khi rỗng (gate Mst_PersonalBackgroundController.cs:126, :149-154),
        // Data = lst_Mst_PersonalBackground (mảng trần List<object>, null khi rỗng).
        return Ok(new { Success = true, Data = rows.Count > 0 ? rows : null });
    }

    /// <summary>
    /// Lọc + đếm + chia trang theo PersonalBGCode asc — dùng cho Search và GetAllActive.
    /// Trả (itemCount, rows): itemCount = tổng số dòng sau lọc (MyCount, SQL :44991); rows = trang đã cắt kèm MyIdxSeq 0-based (:45021).
    /// </summary>
    private async Task<(int itemCount, List<object> rows)> GetXAsync(string? keyWord, string? flagActive, long recordStart, long recordCount)
    {
        var q = _db.Set<Mst_PersonalBackground>().AsNoTracking().AsQueryable();

        // KeyWord -> like '%kw%' trên PersonalBGName (Mst_PersonalBackgroundController.cs:178).
        // Nguồn chạy SQL Server collation KHÔNG phân biệt hoa/thường ⇒ hạ chữ cả 2 phía rồi LIKE (bài học A.1, bẫy #18/#31).
        if (!string.IsNullOrWhiteSpace(keyWord))
        {
            var kw = PortQuery.Contains(keyWord);
            q = q.Where(x => EF.Functions.Like((x.PersonalBGName ?? "").ToLower(), kw, PortQuery.Esc));
        }

        // FlagActive -> toán tử '=' trên FlagActive (Mst_PersonalBackgroundController.cs:182) — KHÔNG IN, KHÔNG tách dấu phẩy.
        // Nguồn dùng '=' ⇒ so sánh bằng chuỗi không phân biệt hoa/thường bằng Like + PortQuery.Equal.
        if (!string.IsNullOrWhiteSpace(flagActive))
        {
            var fa = PortQuery.Equal(flagActive);
            q = q.Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), fa, PortQuery.Esc));
        }

        // Chia trang theo PersonalBGCode asc (Mst_PersonalBackground_GetX SQL: MasterData.cs:44987 order by mpbg.PersonalBGCode asc).
        var ordered = q.OrderBy(x => x.PersonalBGCode);
        var itemCount = await ordered.CountAsync();

        // MyIdxSeq = số thứ tự 0-based trong thứ tự PersonalBGCode (identity(bigint, 0, 1) ở SQL nguồn MasterData.cs:44981).
        // Lấy dòng [recordStart, recordStart+recordCount) theo thứ tự PersonalBGCode (bài học A.12 chống tràn int và không clamp âm).
        var page = (recordCount <= 0 || recordStart < 0)
            ? new List<Mst_PersonalBackground>()
            : await ordered.Skip((int)Math.Min(recordStart, int.MaxValue)).Take((int)Math.Min(recordCount, int.MaxValue)).ToListAsync();

        // Select cuối theo t.MyIdxSeq asc (MasterData.cs:45026 order by t.MyIdxSeq asc), gồm MyIdxSeq + 7 cột model.
        var rows = page
            .Select((x, i) => (object)new
            {
                MyIdxSeq = recordStart + i,
                x.PersonalBGCode,
                x.NetworkID,
                x.PersonalBGName,
                x.PersonalBGDesc,
                x.FlagActive,
                x.LogLUDTimeUTC,
                x.LogLUBy,
            })
            .ToList();

        return (itemCount, rows);
    }
}
