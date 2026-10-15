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
// PORT 1:1 — màn danh mục dòng xe (Mst_CarModel) của dự án nguồn 2022.7.Loyalty (#97, #98, #99).
//
// Nguồn controller (tầng gate):
//   Mst_CarModelController.cs:25-105   Search              -> WA_Mst_CarModel_Get
//   Mst_CarModelController.cs:106-216  GetAllActive        -> WA_Mst_CarModel_Get
//   Mst_CarModelController.cs:217-280  GetByModelCode      -> WA_Mst_CarModel_Get
//   Mst_CarModelController.cs:284-305  strWhereClause_Mst_CarModel (mệnh đề lọc Search)
//   Mst_CarModelController.cs:306-325  strWhereClause_Mst_CarModel_By_ModelCode (mệnh đề lọc GetByModelCode)
// Nguồn API:
//   MstCarModelController.cs:238-307   WA_Mst_CarModel_Get
// Nguồn biz:
//   Master.Loyalty.cs:1434-1534        WAS_Mst_CarModel_Get
//   Master.Loyalty.cs:1535-1687        Mst_CarModel_Get
//   Master.Loyalty.cs:1688-1842        Mst_CarModel_GetX (SQL :1739-1791)
//
// GIỮ ĐÚNG NGUỒN:
//   * Endpoint là POST (nguồn [AcceptVerbs("POST")]).
//   * Lọc KeyWord: like '%kw%' trên ModelName (Mst_CarModelController.cs:291).
//   * Lọc FlagActive: toán tử '=' (Mst_CarModelController.cs:299-302) — KHÁC Mst_CarBrand (IN).
//     Dòng :295-298 bị comment nhưng giống hệt. Không tách dấu phẩy; "0,1" -> so sánh bằng chuỗi "0,1" -> 0 dòng.
//   * Chia trang theo md.ModelCode asc (Master.Loyalty.cs:1748), select cuối sắp theo t.MyIdxSeq asc (Master.Loyalty.cs:1788).
//   * MyIdxSeq 0-based identity (Master.Loyalty.cs:1742).
//   * PageSize = 0 -> DataList rỗng, PageCount = 0 (gate :83-94).
//   * DDL: khong co DDL — PK suy tu FK REFERENCES Mst_CarModel (ModelCode) (20221026.CreateDatabase.sql:91).
//   * GetAllActive: không body, Ft_RecordStart = "0", Ft_RecordCount = "123456000" (gate :148-149).
//     FlagActive = "1" (toán tử '=', gate :135, :301), OrgID không lọc (:134). Khối Ext/tree (:159-191) bị comment — không port.
//     Trả Data là mảng trần listMst_CarModel (null nếu rỗng) (gate :129, :156-158, :196-203).
//   * GetByModelCode: [HttpPost("getbymodelcode")], body { ModelCode } (gate :217-280).
//     ModelCode rỗng -> Data null, không truy vấn (:237, :267). Ngược lại lọc ModelCode = (Like + PortQuery.Equal, :313).
//     Nhánh OrgID (:315-318) trỏ tới cột không tồn tại — mã chết, không port; FlagActive (:319-322) không truyền nên không chạy.
//     Hàng FlagActive '0' vẫn lấy. Ft_RecordStart = "0", Ft_RecordCount = "1" (:254-255).
//     Trả Lst_Mst_CarModel[0] hoặc null (:261-264).
//
// Bỏ qua có chủ đích (loại Đ):
//   - CheckGatewayAuthentication: gate :34, :226; API MstCarModelController.cs:259-264
//   - CheckAuthorize: gate :35, :227 (hệ thống đích chưa có hệ phân quyền)
//   - Nguồn gọi bằng tài khoản hệ thống viết cứng (tài khoản + mật khẩu viết cứng — KHÔNG chép giá trị),
//     đích chưa có phân quyền; KHÔNG chép thông tin đăng nhập vào mã
//   - Ghi RequestLog/ReturnLog: Master.Loyalty.cs:1578-1586, 1676-1684
//   - Sys_User_CheckAuthorize: Master.Loyalty.cs:1592-1606
// ============================================================================================

/// <summary>Port 1:1 <c>Mst_CarModelSearch</c> — tham số lọc + phân trang của màn (Mst_CarModelController.cs:25, 52-56).</summary>
public class MstCarModelSearchReq
{
    public string? KeyWord { get; set; }             // Từ khoá tìm theo tên dòng xe (ModelName) — :54, :291
    public string? FlagActive { get; set; }          // Cờ hoạt động, toán tử '=' — :55, :299-302
    public int Ft_PageIndex { get; set; } = 0;       // Trang (0-based) — :25, :46
    public int Ft_PageSize { get; set; } = 10;       // Số dòng/trang — :25, :47
}

/// <summary>Port 1:1 <c>GetByModelCode</c> — body chỉ có ModelCode (Mst_CarModelController.cs:217-280).</summary>
public class MstCarModelGetByModelCodeReq
{
    public string? ModelCode { get; set; }            // Mã dòng xe cần lấy — :237-241
}

[ApiController]
[Route("api/mst_carmodel")]
[Produces("application/json")]
public class Mst_CarModelControllerPort : ControllerBase
{
    private readonly AppDbContext _db;

    public Mst_CarModelControllerPort(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody] MstCarModelSearchReq? req)
    {
        var r = req ?? new MstCarModelSearchReq();
        var pageIndex = Math.Max(0, r.Ft_PageIndex);
        var pageSize = r.Ft_PageSize;

        // Search gọi GetXAsync với start = PageIndex * PageSize, count = PageSize (giữ nguyên hành vi cũ nguồn :68-69, :75).
        var start = (long)pageIndex * pageSize;
        var (itemCount, rows) = await GetXAsync(r.KeyWord, r.FlagActive, start, pageSize);

        // PageSize=0 -> DataList rỗng (nguồn: nFilterRecordEnd = start-1, SQL biz + gate :83-94).
        // PageCount = 0 khi DataList rỗng, ngược lại ceil(ItemCount / ps) với ps = PageSize (PageSize=0 thì 10) — gate :83-94.
        var ps = pageSize == 0 ? 10 : pageSize;
        var pageCount = rows.Count == 0 ? 0 : (itemCount % ps == 0 ? itemCount / ps : itemCount / ps + 1);

        return Ok(new { Success = true, Data = new PageInfo<object>(rows, itemCount, pageCount, pageIndex, pageSize) });
    }

    /// <summary>
    /// Port 1:1 <c>Mst_CarModelController.GetAllActive</c> (nguồn gate Mst_CarModelController.cs:106-216).
    /// Nguồn gọi <c>strWhereClause_Mst_CarModel</c> với <c>FlagActive = "1"</c> (:132-136) ⇒ lọc
    /// <c>FlagActive = '1'</c> (:299-302). <c>OrgID</c> gán vào object lọc (:134) nhưng hàm lọc KHÔNG dùng OrgID ⇒ KHÔNG lọc.
    /// Khối Ext/tree (:159-191) bị comment trong nguồn — KHÔNG port.
    /// Không body: RecordStart = "0", RecordCount = "123456000" (nguồn Ft_RecordStart/Ft_RecordCount :148-149).
    /// Trả <c>Ok(new { Success = true, Data = ... })</c> — Data là MẢNG TRẦN, rỗng thì <c>null</c>
    /// (nguồn :129 khởi tạo null, :156-158 chỉ gán khi có dòng, :196-203 trả thẳng listMst_CarModel).
    /// </summary>
    [HttpPost("getallactive")]
    public async Task<IActionResult> GetAllActive()
    {
        // Nguồn: Ft_RecordStart = "0", Ft_RecordCount = "123456000" (gate :148-149).
        // FlagActive = "1" (gate :135) ⇒ lọc toán tử '=' (:299-302); OrgID không lọc (:134).
        var (_, rows) = await GetXAsync(null, "1", 0, 123456000);

        // Nguồn :196-203: Data = listMst_CarModel (mảng trần); rỗng ⇒ null (:129, :156-158).
        return Ok(new { Success = true, Data = rows.Count > 0 ? rows : null });
    }

    // ============================================================================================
    // GetByModelCode — nguồn Mst_CarModelController.cs:217-280.
    // Gate: nếu ModelCode rỗng thì KHÔNG truy vấn (:237), objMst_CarModel giữ null (:236) ⇒ Data = null (:267).
    // Ngược lại gán ModelCode (:239-242) rồi gọi strWhereClause_Mst_CarModel_By_ModelCode (:306-325)
    // (lọc `ModelCode = 'x'` — KHÔNG truyền FlagActive nên hàng FlagActive '0' VẪN lấy được).
    // Ft_RecordStart = "0", Ft_RecordCount = "1" (:254-255) ⇒ lấy đúng 1 dòng, trả Lst_Mst_CarModel[0] (:261-264).
    // ============================================================================================
    [HttpPost("getbymodelcode")]
    public async Task<IActionResult> GetByModelCode([FromBody] MstCarModelGetByModelCodeReq? req)
    {
        var modelCode = req?.ModelCode;

        // ModelCode rỗng ⇒ nguồn không truy vấn (:237) và trả Data = null (:267).
        if (string.IsNullOrWhiteSpace(modelCode))
        {
            return Ok(new { Success = true, Data = (object?)null });
        }

        // Lấy 1 dòng theo ModelCode (Ft_RecordStart = "0", Ft_RecordCount = "1" — :254-255).
        var (_, rows) = await GetXAsync(null, null, 0, 1, modelCodeEqual: modelCode);

        // Nguồn trả Lst_Mst_CarModel[0] khi có dòng, ngược lại null (:261-264).
        return Ok(new { Success = true, Data = rows.Count > 0 ? rows[0] : null });
    }

    /// <summary>
    /// Lọc + đếm + chia trang theo ModelCode asc — dùng cho Search, GetAllActive, GetByModelCode.
    /// Trả (itemCount, rows): itemCount = tổng số dòng sau lọc (MyCount, SQL :1752); rows = trang đã cắt kèm MyIdxSeq 0-based (:1783).
    /// </summary>
    private async Task<(int itemCount, List<object> rows)> GetXAsync(string? keyWord, string? flagActive, long recordStart, long recordCount, string? modelCodeEqual = null)
    {
        var q = _db.Set<Mst_CarModel>().AsNoTracking().AsQueryable();

        // KeyWord -> like '%kw%' trên ModelName (Mst_CarModelController.cs:291).
        // Nguồn chạy SQL Server collation KHÔNG phân biệt hoa/thường ⇒ hạ chữ cả 2 phía rồi LIKE (bẫy #18/#31).
        if (!string.IsNullOrWhiteSpace(keyWord))
        {
            var kw = PortQuery.Contains(keyWord);
            q = q.Where(x => EF.Functions.Like((x.ModelName ?? "").ToLower(), kw, PortQuery.Esc));
        }

        // ModelCode -> `ModelCode = 'x'` (Mst_CarModelController.cs:313 — strWhereClause_Mst_CarModel_By_ModelCode).
        // Nguồn dùng `=` trên SQL Server collation KHÔNG phân biệt hoa/thường ⇒ Like KHÔNG wildcard, hạ chữ 2 phía (PortQuery.Equal).
        // LƯU Ý: nhánh OrgID trong hàm lọc nguồn (:315-318) trỏ tới cột OrgID KHÔNG tồn tại ở Mst_CarModel — mã chết,
        //         KHÔNG port. Nhánh FlagActive (:319-322) KHÔNG được truyền ở GetByModelCode nên KHÔNG chạy.
        if (!string.IsNullOrWhiteSpace(modelCodeEqual))
        {
            var mc = PortQuery.Equal(modelCodeEqual);
            q = q.Where(x => EF.Functions.Like((x.ModelCode ?? "").ToLower(), mc, PortQuery.Esc));
        }

        // FlagActive -> toán tử '=' trên FlagActive (Mst_CarModelController.cs:299-302) — KHÁC Mst_CarBrand (IN).
        // Nguồn dùng '=' ⇒ so sánh bằng chuỗi không phân biệt hoa/thường bằng Like + PortQuery.Equal (không tách dấu phẩy).
        if (!string.IsNullOrWhiteSpace(flagActive))
        {
            var fa = PortQuery.Equal(flagActive);
            q = q.Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), fa, PortQuery.Esc));
        }

        // Chia trang theo ModelCode asc (Mst_CarModel_GetX SQL: Master.Loyalty.cs:1748 order by md.ModelCode asc).
        var ordered = q.OrderBy(x => x.ModelCode);
        var itemCount = await ordered.CountAsync();

        // MyIdxSeq = số thứ tự 0-based trong thứ tự ModelCode (identity(bigint, 0, 1) ở SQL nguồn :1742).
        // Lấy dòng [recordStart, recordStart+recordCount) theo thứ tự ModelCode.
        var page = recordCount <= 0
            ? new List<Mst_CarModel>()
            : await ordered.Skip((int)recordStart).Take((int)recordCount).ToListAsync();

        // Select cuối theo t.MyIdxSeq asc (Master.Loyalty.cs:1788 order by t.MyIdxSeq asc).
        var rows = page
            .Select((x, i) => (object)new
            {
                MyIdxSeq = recordStart + i,
                x.ModelCode,
                x.NetworkID,
                x.BrandCode,
                x.ModelName,
                x.FlagActive,
                x.Remark,
                x.LogLUDTimeUTC,
                x.LogLUBy,
            })
            .ToList();

        return (itemCount, rows);
    }
}
