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
// PORT 1:1 — màn danh mục nhóm chứng chỉ (Mst_CertificateGroup) của dự án nguồn 2022.7.Loyalty (#107).
//
// Nguồn controller (tầng gate):
//   Mst_CertificateGroupController.cs:25-105   Search -> WA_Mst_CertificateGroup_Get
//   Mst_CertificateGroupController.cs:171-186  strWhereClause_Mst_CertificateGroup (mệnh đề lọc Search)
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
//   * Lọc FlagActive: toán tử '=' (Mst_CertificateGroupController.cs:182) — KHÔNG IN.
//     Không tách dấu phẩy; "0,1" -> so sánh bằng chuỗi "0,1" -> 0 dòng.
//   * OrgID truyền vào (:25, :55) nhưng strWhereClause_Mst_CertificateGroup (:171-186) KHÔNG dùng OrgID -> không lọc.
//   * Chia trang theo mcg.CertificateGroupCode asc (MasterData.cs:44540), select cuối sắp theo t.MyIdxSeq asc (MasterData.cs:44579).
//   * MyIdxSeq 0-based identity (MasterData.cs:44534).
//   * PageSize = 0 -> DataList rỗng, PageCount = 0 (gate :83-90).
//   * DDL: khong co DDL trong repo — PK = CertificateGroupCode (khoa draft), kieu cot can xac nhan.
//
// Bỏ qua có chủ đích (loại Đ):
//   - CheckGatewayAuthentication: gate :34, API :40-45
//   - CheckAuthorize: gate :35 (hệ thống đích chưa có hệ phân quyền)
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
