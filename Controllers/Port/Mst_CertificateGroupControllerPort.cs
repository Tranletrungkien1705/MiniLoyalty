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
// PORT 1:1 — màn danh mục nhóm chứng chỉ (Mst_CertificateGroup) của dự án nguồn 2022.7.Loyalty (#107, #108).
//
// Nguồn controller (tầng gate):
//   Mst_CertificateGroupController.cs:25-105   Search -> WA_Mst_CertificateGroup_Get
//   Mst_CertificateGroupController.cs:107-167  GetAllActive -> WA_Mst_CertificateGroup_Get (#108)
//   Mst_CertificateGroupController.cs:171-186  strWhereClause_Mst_CertificateGroup (mệnh đề lọc Search)
//   Mst_CertificateGroupController.cs:188-199  strWhereClause_Mst_CertificateGroup_Active (mệnh đề lọc GetAllActive)
// Nguồn API:
//   MstCertificateGroupController.cs:17-88    WA_Mst_CertificateGroup_Get
// Nguồn biz:
//   MasterData.cs:44765-44855                 WAS_Mst_CertificateGroup_Get
//   MasterData.cs:44635-44763                 Mst_CertificateGroup_Get
//   MasterData.cs:44478-44633                 Mst_CertificateGroup_GetX (SQL :44531-44582)
//
// GIỮ ĐÚNG NGUỒN:
//   * Endpoint là POST (nguồn [AcceptVerbs("POST")]).
//   * Lọc KeyWord: like '%kw%' trên CertificateGroupName (Mst_CertificateGroupController.cs:178).
//   * Lọc FlagActive: toán tử '=' (Mst_CertificateGroupController.cs:182, :195) — KHÔNG IN.
//     Không tách dấu phẩy; "0,1" -> so sánh bằng chuỗi "0,1" -> 0 dòng.
//   * OrgID truyền vào (:25, :55) nhưng strWhereClause_Mst_CertificateGroup (:171-186) KHÔNG dùng OrgID -> không lọc.
//   * Chia trang theo mcg.CertificateGroupCode asc (MasterData.cs:44540), select cuối sắp theo t.MyIdxSeq asc (MasterData.cs:44579).
//   * MyIdxSeq 0-based identity (MasterData.cs:44534).
//   * PageSize = 0 -> DataList rỗng, PageCount = 0 (gate :83-90).
//   * GetAllActive: không body, Ft_RecordStart = "0", Ft_RecordCount = "123456000" (gate :142-143).
//     FlagActive = "1" (toán tử '=', gate :129, :195). Trả Data là mảng trần list Mst_CertificateGroup (null nếu rỗng) (gate :126, :149-154).
//   * DDL: khong co DDL trong repo — PK = CertificateGroupCode (khoa draft), kieu cot can xac nhan.
//
// Bỏ qua có chủ đích (loại Đ):
//   - CheckGatewayAuthentication: gate :34, :116, API :40-45
//   - CheckAuthorize: gate :35, :117 (hệ thống đích chưa có hệ phân quyền)
//   - Ghi RequestLog/ReturnLog: MasterData.cs:44676-44683, 44753-44760
// ============================================================================================

/// <summary>Port 1:1 <c>Mst_CertificateGroupSearch</c> — tham số lọc + phân trang của màn (Mst_CertificateGroupController.cs:25, 52-56).</summary>
public class MstCertificateGroupSearchReq
{
    public string? OrgID { get; set; }               // Tổ chức — gate nhận (:25, :55) nhưng strWhereClause_Mst_CertificateGroup (:171-186) KHÔNG dùng OrgID -> không lọc
    public string? KeyWord { get; set; }             // Từ khoá tìm theo tên nhóm chứng chỉ (CertificateGroupName) — :25, :54, :178
    public string? FlagActive { get; set; }          // Cờ hoạt động, toán tử '=' — :25, :56, :182
    public int Ft_PageIndex { get; set; } = 0;       // Trang (0-based) — :25, :46
    public int Ft_PageSize { get; set; } = 10;       // Số dòng/trang — :25, :47
}

[ApiController]
[Route("api/mst_certificategroup")]
[Produces("application/json")]
public class Mst_CertificateGroupControllerPort : ControllerBase
{
    private readonly AppDbContext _db;

    public Mst_CertificateGroupControllerPort(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] MstCertificateGroupSearchReq? req)
    {
        var r = req ?? new MstCertificateGroupSearchReq();
        var pageIndex = Math.Max(0, r.Ft_PageIndex);
        var pageSize = r.Ft_PageSize;

        // Search gọi GetXAsync với start = PageIndex * PageSize, count = PageSize (giữ nguyên hành vi cũ nguồn :50-51, :75).
        var start = (long)pageIndex * pageSize;
        var (itemCount, rows) = await GetXAsync(r.KeyWord, r.FlagActive, start, pageSize);

        // PageSize=0 -> DataList rỗng (nguồn: nFilterRecordEnd = start-1, SQL biz + gate :83-90).
        // PageCount = 0 khi DataList rỗng, ngược lại ceil(ItemCount / ps) với ps = PageSize (PageSize=0 thì 10) — gate :83-90.
        var ps = pageSize == 0 ? 10 : pageSize;
        var pageCount = rows.Count == 0 ? 0 : (itemCount % ps == 0 ? itemCount / ps : itemCount / ps + 1);

        return Ok(new { Success = true, Data = new PageInfo<object>(rows, itemCount, pageCount, pageIndex, pageSize) });
    }

    /// <summary>
    /// Port 1:1 <c>Mst_CertificateGroupController.GetAllActive</c> (nguồn gate Mst_CertificateGroupController.cs:107-167).
    /// Nguồn gọi <c>strWhereClause_Mst_CertificateGroup_Active</c> với <c>FlagActive = "1"</c> (:127-130) ⇒ lọc
    /// <c>FlagActive = '1'</c> (:195, toán tử '=').
    /// Không body: RecordStart = "0", RecordCount = "123456000" (nguồn Ft_RecordStart/Ft_RecordCount :142-143).
    /// Trả <c>Ok(new { Success = true, Data = ... })</c> — Data là MẢNG TRẦN, rỗng thì <c>null</c>
    /// (nguồn :126 khởi tạo null, :149-152 chỉ gán khi có dòng, :154 trả thẳng lst_Mst_CertificateGroup).
    /// </summary>
    [HttpPost("getallactive")]
    public async Task<IActionResult> GetAllActive()
    {
        // Nguồn: Ft_RecordStart = "0", Ft_RecordCount = "123456000" (gate :142-143).
        // FlagActive = "1" (gate :129) ⇒ lọc toán tử '=' (:195); không body.
        var (_, rows) = await GetXAsync(null, "1", 0, 123456000);

        // Nguồn :154: Data = lst_Mst_CertificateGroup (mảng trần); rỗng ⇒ null (:126, :149-152).
        return Ok(new { Success = true, Data = rows.Count > 0 ? rows : null });
    }

    /// <summary>
    /// Lọc + đếm + chia trang theo CertificateGroupCode asc — dùng cho Search.
    /// Trả (itemCount, rows): itemCount = tổng số dòng sau lọc (MyCount, SQL :44544); rows = trang đã cắt kèm MyIdxSeq 0-based (:44574).
    /// </summary>
    private async Task<(int itemCount, List<object> rows)> GetXAsync(string? keyWord, string? flagActive, long recordStart, long recordCount)
    {
        var q = _db.Set<Mst_CertificateGroup>().AsNoTracking().AsQueryable();

        // KeyWord -> like '%kw%' trên CertificateGroupName (Mst_CertificateGroupController.cs:178).
        // Nguồn chạy SQL Server collation KHÔNG phân biệt hoa/thường ⇒ hạ chữ cả 2 phía rồi LIKE (bẫy #18/#31).
        if (!string.IsNullOrWhiteSpace(keyWord))
        {
            var kw = PortQuery.Contains(keyWord);
            q = q.Where(x => EF.Functions.Like((x.CertificateGroupName ?? "").ToLower(), kw, PortQuery.Esc));
        }

        // FlagActive -> toán tử '=' trên FlagActive (Mst_CertificateGroupController.cs:182) — KHÔNG IN.
        // Nguồn dùng '=' ⇒ so sánh bằng chuỗi không phân biệt hoa/thường bằng Like + PortQuery.Equal (không tách dấu phẩy).
        if (!string.IsNullOrWhiteSpace(flagActive))
        {
            var fa = PortQuery.Equal(flagActive);
            q = q.Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), fa, PortQuery.Esc));
        }

        // Chia trang theo CertificateGroupCode asc (Mst_CertificateGroup_GetX SQL: MasterData.cs:44540 order by mcg.CertificateGroupCode asc).
        var ordered = q.OrderBy(x => x.CertificateGroupCode);
        var itemCount = await ordered.CountAsync();

        // MyIdxSeq = số thứ tự 0-based trong thứ tự CertificateGroupCode (identity(bigint, 0, 1) ở SQL nguồn :44534).
        // Lấy dòng [recordStart, recordStart+recordCount) theo thứ tự CertificateGroupCode.
        var page = recordCount <= 0
            ? new List<Mst_CertificateGroup>()
            : await ordered.Skip((int)recordStart).Take((int)recordCount).ToListAsync();

        // Select cuối theo t.MyIdxSeq asc (MasterData.cs:44579 order by t.MyIdxSeq asc).
        var rows = page
            .Select((x, i) => (object)new
            {
                MyIdxSeq = recordStart + i,
                x.CertificateGroupCode,
                x.NetworkID,
                x.CertificateGroupName,
                x.FlagActive,
                x.LogLUDTimeUTC,
                x.LogLUBy,
            })
            .ToList();

        return (itemCount, rows);
    }
}
