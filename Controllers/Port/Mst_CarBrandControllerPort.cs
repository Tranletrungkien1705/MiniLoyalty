using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MiniLoyalty.Data;
using MiniLoyalty.Models.Port;

namespace MiniLoyalty.Controllers.Port;

// ============================================================================================
// PORT 1:1 — màn danh mục hãng xe (Mst_CarBrand) của dự án nguồn 2022.7.Loyalty (#77).
//
// Nguồn controller (tầng gate):
//   Mst_CarBrandController.cs:25-105   Search              -> WA_Mst_CarBrand_Get
//   Mst_CarBrandController.cs:283-308  strWhereClause_Mst_CarBrand (mệnh đề lọc Search)
//   Mst_CarBrandController.cs:332-419  Create              -> WA_Mst_CarBrand_Create (#80)
//   Mst_CarBrandController.cs:421-488  Update              -> WA_Mst_CarBrand_Update (#81)
//   Mst_CarBrandController.cs:492-555  Delete              -> WA_Mst_CarBrand_Delete (#82)
//   Mst_CarBrandController.cs:557-621  DeleteMultiple      -> WA_Mst_CarBrand_Delete (#83)
//   Mst_CarBrandController.cs:625-769  Import              -> WA_Mst_CarBrand_Create (#84)
//   Mst_CarBrandController.cs:774-852  Export              -> WA_Mst_CarBrand_Get (#85)
//   Mst_CarBrandController.cs:855-887  ExportTemplate      -> (không qua service/biz, không đọc DB) (#86)
// Nguồn API:
//   MstCarBrandController.cs:238-307   WA_Mst_CarBrand_Get
//   MstCarBrandController.cs:20-90     WA_Mst_CarBrand_Create
//   MstCarBrandController.cs:92-162    WA_Mst_CarBrand_Update
//   MstCarBrandController.cs:165-235   WA_Mst_CarBrand_Delete
// Nguồn biz:
//   Master.Loyalty.cs:93-193   WAS_Mst_CarBrand_Get
//   Master.Loyalty.cs:194-346  Mst_CarBrand_Get
//   Master.Loyalty.cs:347-501  Mst_CarBrand_GetX (SQL :400-447)
//   Master.Loyalty.cs:502-584  WAS_Mst_CarBrand_Create
//   Master.Loyalty.cs:585-731  Mst_CarBrand_Create
//   Master.Loyalty.cs:732-828  Mst_CarBrand_CreateX
//   Master.Loyalty.cs:829-913  WAS_Mst_CarBrand_Update
//   Master.Loyalty.cs:914-1066 Mst_CarBrand_Update
//   Master.Loyalty.cs:1068-1156 Mst_CarBrand_UpdateX
//   Master.Loyalty.cs:1157-1235 WAS_Mst_CarBrand_Delete
//   Master.Loyalty.cs:1236-1372 Mst_CarBrand_Delete
//   Master.Loyalty.cs:1374-1430 Mst_CarBrand_DeleteX
//   Master.Loyalty.cs:28-91    Mst_CarBrand_CheckDB
//
// GIỮ ĐÚNG NGUỒN:
//   * Endpoint là POST (nguồn [AcceptVerbs("POST")]).
//   * Lọc KeyWord: like '%x%' trên BrandName (:290).
//   * Lọc FlagActive: danh sách in ('a','b') (:297-305); dòng `=` (:293-296) BỊ COMMENT nên KHÔNG chạy.
//   * Chia trang theo mcb.BrandCode asc (:407), select cuối sắp theo t.MyIdxSeq asc (:441-447).
//   * MyIdxSeq 0-based identity (:401).
//   * PageSize = 0 -> DataList rỗng, PageCount = 0 (gate :83-94).
//   * Create: BrandCode, NetworkID, BrandName qua StdParam (BrandName viết hoa ToUpper :764);
//     Remark qua StdStr. Kiểm tra BrandCode rỗng/trùng; KHÔNG kiểm tra BrandName/NetworkID.
//   * Update: Ft_Cols_Upd cố định ở gate -> luôn cập nhật 3 cột; thiếu FlagActive -> '0' (StdFlag).
//     BrandName qua StdStr (không viết hoa, khác Create #80 dùng StdParam).
//     NetworkID và BrandCode không đổi.
//   * Delete: kiểm tra rỗng tại gate -> invalid_Brandcode (chữ 'c' viết thường, nguyên văn nguồn);
//     CheckDB(BrandCode, FlagExist "1") -> Mst_CarBrand_CheckDB_BrandNotFound;
//     khong co FK toi Mst_CarBrand trong DDL — dong xe co the mo coi (xóa hãng đang được dòng xe dùng vẫn thành công);
//     xoá dòng -> Success = true, Data = null.
//   * DeleteMultiple: body là mảng [{ BrandCode }], lặp tuần tự từng phần tử trong 1 transaction riêng biệt.
//     Lỗi phần tử thứ k -> thoát vòng lặp ngay, "xóa một phần" là hành vi nguồn.
//   * Import: nhận file .xlsx ([Consumes("multipart/form-data")]); file thiếu hoặc đuôi khác .xlsx -> Fail("Mst_CarBrand_Import", "Message", "File excel import không hợp lệ");
//     Đọc qua PortExcel.ReadSheet: không có dòng -> "File excel import không có dữ liệu"; columnCount != 2 -> "File excel import không hợp lệ";
//     Kiểm tra TẤT CẢ dòng trước khi tạo: BrandCode rỗng -> "Mã hiệu xe không được trống", BrandName rỗng -> "Tên hiệu xe không được trống";
//     Duyệt từng dòng: gọi CreateXAsync độc lập (mỗi dòng 1 transaction riêng, "import một phần");
//     Lỗi dòng k dừng ngay và trả lỗi dòng đó (các dòng trước k đã lưu);
//     Thành công Data = null (:749-750).
//   * Export: nhận body { OrgID, KeyWord, FlagActive } (:774, :795-800); strWhereClause_Mst_CarBrand (:283-308)
//     lọc KeyWord (like trên BrandName) và FlagActive (in), không lọc OrgID;
//     Ft_RecordStart = "0", Ft_RecordCount = "123456000" (:813-814); rỗng -> ServiceException("Không có dữ liệu") (:838)
//     -> 400 ErridnLoyalty.Mst_CarBrand_Export; từ điển 3 cột GetImportDicColums() (BrandCode, BrandName, FlagActive);
//     file + sheet "Mst_CarBrand" (:827-828); trả Url tải file (:830-833).
//   * ExportTemplate: danh sách rỗng kiểu Mst_CarBrand (:863); từ điển GetImportDicColumsTemplate()
//     2 cột (BrandCode -> "Mã hiệu xe (*)", BrandName -> "Tên hiệu xe (*)"); file + sheet "Mst_CarBrand" (:867-868);
//     trả Url tải file (:869-873). Khởi tạo danh sách rỗng, không qua service/biz, không đọc DB.
//
// GAP:
//   - NetworkID: nguồn lấy NetworkID = orgUserUI.NetworkId từ user đăng nhập (Mst_CarBrandController.cs:731);
//     hệ thống đích chưa có session user -> lấy từ cấu hình IConfiguration["Biz_NetworkID"] ?? "1001" (như #94).
//   - Định dạng ô Excel (DLL nguồn) cần xác nhận khi có file mẫu thật.
//
// Bỏ qua có chủ đích (loại Đ):
//   - CheckGatewayAuthentication: gate :34, :352, :441, :501, :577, :636; API MstCarBrandController.cs:259-264, :42, :114-119, :187-192
//   - CheckAuthorize: gate :35, :353, :442, :502, :578, :637 (hệ thống đích chưa có hệ phân quyền)
//   - Nguồn gọi bằng tài khoản hệ thống viết cứng (tài khoản + mật khẩu viết cứng — KHÔNG chép giá trị),
//     đích chưa có phân quyền; KHÔNG chép thông tin đăng nhập vào mã
//   - Ghi RequestLog/ReturnLog: Master.Loyalty.cs:238-245, 336-343, 628-635, 721-728, 960-967, 1056-1063, 1272-1279, 1362-1369
//   - Sys_User_CheckAuthorize: Master.Loyalty.cs:251-264, 647-660, 979-992, 1291-1305
// ============================================================================================

/// <summary>Port 1:1 <c>Mst_CarBrandSearch</c> — tham số lọc + phân trang của màn (Mst_CarBrandController.cs:25, 52-56).</summary>
public class MstCarBrandSearchReq
{
    public string? KeyWord { get; set; }             // Từ khoá tìm theo tên hãng xe (BrandName) — :54, :290
    public string? FlagActive { get; set; }          // Cờ hoạt động, danh sách phân cách dấu phẩy — :55, :297-305
    public int Ft_PageIndex { get; set; } = 0;       // Trang (0-based) — :25, :46
    public int Ft_PageSize { get; set; } = 10;       // Số dòng/trang — :25, :47
}

/// <summary>Port 1:1 <c>GetByBrandCode</c> — body chỉ có BrandCode (Mst_CarBrandController.cs:216-279).</summary>
public class MstCarBrandGetByBrandCodeReq
{
    public string? BrandCode { get; set; }            // Mã hãng xe cần lấy — :238-241
}

/// <summary>
/// Port 1:1 <c>Create</c> — body JSON 4 trường của model nguồn (Mst_CarBrandController.cs:332-419).
/// Nguồn deserialize thẳng vào TModelLoyaltys.Mst_CarBrand (mọi cột là object) rồi truyền
/// BrandCode, NetworkID, BrandName, Remark xuống biz (Master.Loyalty.cs:551-554).
/// </summary>
public class MstCarBrandCreateReq
{
    public string? BrandCode { get; set; }            // Mã hãng xe — :551
    public string? NetworkID { get; set; }            // NetworkID lấy từ JSON, KHÔNG từ cấu hình — :552
    public string? BrandName { get; set; }            // Tên hãng xe (bị viết hoa do dùng StdParam) — :553, :764
    public string? Remark { get; set; }               // Ghi chú — :554
}

/// <summary>
/// Port 1:1 <c>Update</c> — body JSON của model nguồn (Mst_CarBrandController.cs:421-488).
/// Nguồn deserialize thẳng vào TModelLoyaltys.Mst_CarBrand (mọi cột là object) rồi truyền
/// BrandCode, NetworkID, BrandName, Remark, FlagActive xuống biz (Master.Loyalty.cs:877-881).
/// </summary>
public class MstCarBrandUpdateReq
{
    public string? BrandCode { get; set; }            // Mã hãng xe — :877
    public string? NetworkID { get; set; }            // NetworkID — :878 (không lưu khi update)
    public string? BrandName { get; set; }            // Tên hãng xe (StdStr, không viết hoa) — :879, :1111
    public string? Remark { get; set; }               // Ghi chú (StdStr) — :880, :1112
    public string? FlagActive { get; set; }           // Cờ hoạt động (StdFlag, "1" -> "1", khác -> "0") — :881, :1113
}

/// <summary>
/// Port 1:1 <c>Delete</c> — body chỉ có BrandCode (Mst_CarBrandController.cs:492-555).
/// </summary>
public class MstCarBrandDeleteReq
{
    public string? BrandCode { get; set; }            // Mã hãng xe cần xoá — :492, :516
}

/// <summary>Port 1:1 <c>Export</c> — body nhận OrgID, KeyWord, FlagActive (Mst_CarBrandController.cs:774, 795-800).</summary>
public class MstCarBrandExportReq
{
    public string? OrgID { get; set; }               // Tổ chức — gate nhận (:774, :798) nhưng strWhereClause_Mst_CarBrand (:283-308) KHÔNG lọc OrgID
    public string? KeyWord { get; set; }             // Từ khoá tìm theo tên hiệu xe (BrandName) — :774, :797, :290
    public string? FlagActive { get; set; }          // Cờ hoạt động, danh sách phân cách dấu phẩy — :774, :799, :297-305
}

[ApiController]
[Route("api/mst_carbrand")]
[Produces("application/json")]
public class Mst_CarBrandControllerPort : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;

    public Mst_CarBrandControllerPort(AppDbContext db, IConfiguration config, IWebHostEnvironment env)
    {
        _db = db;
        _config = config;
        _env = env;
    }

    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody] MstCarBrandSearchReq? req)
    {
        var r = req ?? new MstCarBrandSearchReq();
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
    /// Port 1:1 <c>Mst_CarBrandController.GetAllActive</c> (nguồn gate :106-215).
    /// Nguồn gọi <c>strWhereClause_Mst_CarBrand</c> với <c>FlagActive = "1"</c> (:132-136) ⇒ lọc
    /// <c>FlagActive IN ('1')</c> (:297-305). <c>OrgID</c> chỉ được GÁN vào request (:134, :146),
    /// hàm lọc KHÔNG dùng OrgID ⇒ KHÔNG lọc theo OrgID.
    /// Không body: RecordStart = "0", RecordCount = "123456000" (nguồn Ft_RecordStart/Ft_RecordCount).
    /// Trả <c>Ok(new { Success = true, Data = ... })</c> — Data là MẢNG TRẦN, rỗng thì <c>null</c>
    /// (nguồn :129 khởi tạo null, :155-157 chỉ gán khi có dòng, :195-199 trả thẳng listMst_CarBrand).
    /// </summary>
    [HttpPost("getallactive")]
    public async Task<IActionResult> GetAllActive()
    {
        // Nguồn: Ft_RecordStart = "0", Ft_RecordCount = "123456000" (gate :148-149).
        // FlagActive = "1" (gate :135) ⇒ lọc IN ('1'); OrgID không lọc.
        var (_, rows) = await GetXAsync(null, "1", 0, 123456000);

        // Nguồn :195-199: Data = listMst_CarBrand (mảng trần); rỗng ⇒ null (:129, :155-157).
        return Ok(new { Success = true, Data = rows.Count > 0 ? rows : null });
    }

    // ============================================================================================
    // GetByBrandCode — nguồn Mst_CarBrandController.cs:216-279.
    // Gate: nếu BrandCode rỗng thì KHÔNG truy vấn (:236), objMst_CarBrand giữ null (:235) ⇒ Data = null (:266).
    // Ngược lại gán BrandCode (:238-241) rồi gọi strWhereClause_Mst_CarBrand_By_BrandCode (:309-328)
    // (lọc `BrandCode = 'x'` — KHÔNG truyền FlagActive nên hàng FlagActive '0' VẪN lấy được).
    // Ft_RecordStart = "0", Ft_RecordCount = "1" (:253-254) ⇒ lấy đúng 1 dòng, trả Lst_Mst_CarBrand[0] (:260-263).
    // ============================================================================================
    [HttpPost("getbybrandcode")]
    public async Task<IActionResult> GetByBrandCode([FromBody] MstCarBrandGetByBrandCodeReq? req)
    {
        var brandCode = req?.BrandCode;

        // BrandCode rỗng ⇒ nguồn không truy vấn (:236) và trả Data = null (:266).
        if (string.IsNullOrWhiteSpace(brandCode))
        {
            return Ok(new { Success = true, Data = (object?)null });
        }

        // Lấy 1 dòng theo BrandCode (Ft_RecordStart = "0", Ft_RecordCount = "1" — :253-254).
        var (_, rows) = await GetXAsync(null, null, 0, 1, brandCodeEqual: brandCode);

        // Nguồn trả Lst_Mst_CarBrand[0] khi có dòng, ngược lại null (:260-263).
        return Ok(new { Success = true, Data = rows.Count > 0 ? rows[0] : null });
    }

    /// <summary>
    /// Lọc + đếm + chia trang theo BrandCode asc — dùng cho Search.
    /// Trả (itemCount, rows): itemCount = tổng số dòng sau lọc (MyCount, SQL :411); rows = trang đã cắt kèm MyIdxSeq 0-based (:443).
    /// </summary>
    private async Task<(int itemCount, List<object> rows)> GetXAsync(string? keyWord, string? flagActiveCsv, long recordStart, long recordCount, string? brandCodeEqual = null)
    {
        var q = _db.Set<Mst_CarBrand>().AsNoTracking().AsQueryable();

        // KeyWord -> like '%x%' trên BrandName (Mst_CarBrandController.cs:290).
        // Nguồn chạy SQL Server collation KHÔNG phân biệt hoa/thường ⇒ hạ chữ cả 2 phía rồi LIKE (bẫy #18/#31).
        if (!string.IsNullOrWhiteSpace(keyWord))
        {
            var kw = PortQuery.Contains(keyWord);
            q = q.Where(x => EF.Functions.Like((x.BrandName ?? "").ToLower(), kw, PortQuery.Esc));
        }

        // BrandCode -> `BrandCode = 'x'` (Mst_CarBrandController.cs:316 — strWhereClause_Mst_CarBrand_By_BrandCode).
        // Nguồn dùng `=` trên SQL Server collation KHÔNG phân biệt hoa/thường ⇒ Like KHÔNG wildcard, hạ chữ 2 phía.
        // LƯU Ý: nhánh OrgID trong hàm lọc nguồn (:318-321) trỏ tới cột OrgID KHÔNG tồn tại ở Mst_CarBrand — mã chết,
        //         KHÔNG port. Nhánh FlagActive (:322-...) KHÔNG được truyền ở GetByBrandCode nên KHÔNG chạy.
        if (!string.IsNullOrWhiteSpace(brandCodeEqual))
        {
            var bc = PortQuery.Equal(brandCodeEqual);
            q = q.Where(x => EF.Functions.Like((x.BrandCode ?? "").ToLower(), bc, PortQuery.Esc));
        }

        // FlagActive -> in ('a','b') (Mst_CarBrandController.cs:297-305): tách dấu phẩy, bỏ khoảng trắng, hạ chữ.
        // Dòng `=` (:293-296) BỊ COMMENT ở nguồn nên KHÔNG dùng.
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

        // Chia trang theo BrandCode asc (Mst_CarBrand_GetX SQL: :407 order by mcb.BrandCode asc).
        var ordered = q.OrderBy(x => x.BrandCode);
        var itemCount = await ordered.CountAsync();

        // MyIdxSeq = số thứ tự 0-based trong thứ tự BrandCode (identity(bigint, 0, 1) ở SQL nguồn :401).
        // Lấy dòng [recordStart, recordStart+recordCount) theo thứ tự BrandCode.
        var page = recordCount <= 0
            ? new List<Mst_CarBrand>()
            : await ordered.Skip((int)recordStart).Take((int)recordCount).ToListAsync();

        // Select cuối theo t.MyIdxSeq asc (:441-447 order by t.MyIdxSeq asc).
        var rows = page
            .Select((x, i) => (object)new
            {
                MyIdxSeq = recordStart + i,
                x.BrandCode,
                x.NetworkID,
                x.BrandName,
                x.FlagActive,
                x.Remark,
                x.LogLUDTimeUTC,
                x.LogLUBy,
            })
            .ToList();

        return (itemCount, rows);
    }

    // ============================================================================================
    // Create — nguồn Mst_CarBrandController.cs:332-419 (gate) -> MstCarBrandController.cs:20-90 (API)
    //   -> Master.Loyalty.cs:502-584 (WAS_Mst_CarBrand_Create) -> :585-731 (Mst_CarBrand_Create)
    //   -> :732-828 (Mst_CarBrand_CreateX) -> :28-91 (Mst_CarBrand_CheckDB).
    //
    // GIỮ ĐÚNG NGUỒN:
    //   * Body JSON 4 trường: BrandCode, NetworkID, BrandName, Remark (:551-554).
    //     NetworkID lấy TỪ JSON, KHÔNG từ cấu hình.
    //   * Chuẩn hoá (:763-766): BrandCode = StdParam (Trim+ToUpper, rỗng->null);
    //     BrandName = StdParam (Trim+ToUpper, rỗng->null — tên hãng xe bị VIẾT HOA, hành vi nguồn :764);
    //     NetworkID = StdParam (Trim+ToUpper, rỗng->null);
    //     Remark = StdStr (Trim+Normalize, rỗng->null).
    //   * Kiểm tra THEO THỨ TỰ (:775-794):
    //       1) BrandCode null/rỗng -> ErridnLoyalty.Mst_CarBrand_Create_InvalidBrandCode (:775-785)
    //       2) CheckDB(BrandCode, FlagExist "0") -> ErridnLoyalty.Mst_CarBrand_CheckDB_BrandExist nếu đã tồn tại (:787-793 -> :65-75).
    //       KHÔNG kiểm tra tên hãng (BrandName), KHÔNG kiểm tra NetworkID (:773-796).
    //   * Lưu (:804-820): BrandCode, BrandName, NetworkID, Remark, FlagActive = "1",
    //     LogLUDTimeUTC = UtcNow, LogLUBy = "port".
    //   * Thành công: gate :403-404 SetSuccess + Data = null.
    //
    // Bỏ qua có chủ đích (loại Đ):
    //   * CheckGatewayAuthentication/CheckAuthorize (gate :352-353), Sys_User_CheckAuthorize (:647-660):
    //     hệ thống đích chưa có hệ phân quyền.
    //   * Ghi RequestLog/ReturnLog (:628-635, :721-728).
    //   * LogLUBy gán cố định "port" do đích chưa có session người dùng (:811).
    // ============================================================================================
    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] MstCarBrandCreateReq? req)
    {
        // 1. Body rỗng/null: gate Mst_CarBrandController.cs:342-348 ném ServiceException("strJson null") -> mã mặc định Mst_CarBrand_Create.
        if (req == null)
        {
            return PortResult.Fail("Mst_CarBrand_Create", "Check.strJson", null);
        }

        // 2. Thực hiện nghiệp vụ tạo qua CreateXAsync (1 transaction / lần)
        var err = await CreateXAsync(req.BrandCode, req.NetworkID, req.BrandName, req.Remark);
        if (err != null)
        {
            return err;
        }

        // 7. Thành công: gate Mst_CarBrandController.cs:403-404 SetSuccess + Data = null.
        return Ok(new
        {
            Success = true,
            Data = (object?)null
        });
    }

    /// <summary>
    /// Nghiệp vụ tạo 1 hãng xe — 1 transaction riêng biệt. Dùng chung cho Create (#80) và Import (#84, gọi từng dòng).
    /// Nguồn: WA_Mst_CarBrand_Create (MstCarBrandController.cs:20-90) -> WAS_Mst_CarBrand_Create (Master.Loyalty.cs:502-584)
    /// -> Mst_CarBrand_Create (Master.Loyalty.cs:585-731) -> Mst_CarBrand_CreateX (Master.Loyalty.cs:732-828)
    /// -> Mst_CarBrand_CheckDB (Master.Loyalty.cs:28-91).
    ///
    /// Thứ tự xử lý:
    ///   1. Chuẩn hoá dữ liệu input (Master.Loyalty.cs:763-766):
    ///      StdParam = Trim().ToUpper(), rỗng -> null (05.Refs.Biz\Biz\Loyalty.TVO\Loyalty.TVO.Utils\Utils.cs:29).
    ///      StdStr   = Trim().Normalize(), rỗng -> null (cùng tệp Utils.cs:29).
    ///      CẢNH BÁO: BrandName dùng StdParam (:764) nên tên hãng xe bị VIẾT HOA (ToUpper) theo đúng hành vi nguồn!
    ///   2. Kiểm tra dữ liệu:
    ///      2.1. BrandCode null/rỗng -> ErridnLoyalty.Mst_CarBrand_Create_InvalidBrandCode (:775-785).
    ///      2.2. Mst_CarBrand_CheckDB(BrandCode, Flag "0") (Master.Loyalty.cs:787-793 -> :28-90):
    ///           strFlagExistToCheck = "0" (TConst.Flag.No). Nếu đã tồn tại dòng -> ErridnLoyalty.Mst_CarBrand_CheckDB_BrandExist (:65-75).
    ///      2.3. KHÔNG kiểm tra tên hãng (BrandName), KHÔNG kiểm tra NetworkID (hành vi nguồn Master.Loyalty.cs:773-796).
    ///   3. Lưu dòng mới (Master.Loyalty.cs:804-820):
    ///      BrandCode, BrandName, NetworkID, Remark, FlagActive = "1", LogLUDTimeUTC = UtcNow, LogLUBy = "port".
    ///      Commit transaction (Master.Loyalty.cs:694 TDALUtils.DBUtils.CommitSafety).
    /// Trả null nếu thành công; trả IActionResult (PortResult.Fail) nếu thất bại.
    /// </summary>
    private async Task<IActionResult?> CreateXAsync(string? brandCode, string? networkId, string? brandName, string? remark)
    {
        // Mở 1 transaction cho mỗi lần tạo (Master.Loyalty.cs:625 _cf.db.BeginTransaction()).
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // Chuẩn hoá dữ liệu input (Master.Loyalty.cs:763-766):
            string? strBrandCode = StdParam(brandCode);
            string? strBrandName = StdParam(brandName);
            string? strNetworkID = StdParam(networkId);
            string? strRemark = StdStr(remark);

            // Kiểm tra dữ liệu:
            // BrandCode null/rỗng -> ErridnLoyalty.Mst_CarBrand_Create_InvalidBrandCode (:775-785).
            if (string.IsNullOrEmpty(strBrandCode))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_CarBrand_Create_InvalidBrandCode", "Check.strBrandCode", strBrandCode);
            }

            // Mst_CarBrand_CheckDB(BrandCode, Flag "0") (Master.Loyalty.cs:787-793 -> :28-90):
            var exists = await _db.Set<Mst_CarBrand>().AnyAsync(x =>
                EF.Functions.Like((x.BrandCode ?? "").ToLower(), PortQuery.Equal(strBrandCode), PortQuery.Esc));
            if (exists)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_CarBrand_CheckDB_BrandExist", "Check.BrandCode", strBrandCode);
            }

            // Lưu dòng mới (Master.Loyalty.cs:804-820):
            var dtimeSys = DateTime.UtcNow;
            var entity = new Mst_CarBrand
            {
                BrandCode = strBrandCode,
                BrandName = strBrandName,
                NetworkID = strNetworkID,
                Remark = strRemark,
                FlagActive = "1",
                LogLUDTimeUTC = dtimeSys,
                LogLUBy = "port"
            };

            await _db.Set<Mst_CarBrand>().AddAsync(entity);
            await _db.SaveChangesAsync();

            // Commit transaction (Master.Loyalty.cs:694 TDALUtils.DBUtils.CommitSafety).
            await tx.CommitAsync();
            return null;
        }
        catch (Exception)
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    // ============================================================================================
    // Update — nguồn Mst_CarBrandController.cs:421-488 (gate) -> MstCarBrandController.cs:92-162 (API)
    //   -> Master.Loyalty.cs:829-913 (WAS_Mst_CarBrand_Update) -> :914-1066 (Mst_CarBrand_Update)
    //   -> :1068-1156 (Mst_CarBrand_UpdateX) -> :28-91 (Mst_CarBrand_CheckDB).
    //
    // GIỮ ĐÚNG NGUỒN:
    //   * Body JSON: BrandCode, NetworkID, BrandName, Remark, FlagActive (:877-881).
    //   * Gate dựng Ft_Cols_Upd CỐ ĐỊNH (:451-456):
    //       "Mst_CarBrand.BrandCode,Mst_CarBrand.BrandName,Mst_CarBrand.Remark,Mst_CarBrand.FlagActive,"
    //     -> Ft_Cols_Upd co dinh o gate -> luon cap nhat 3 cot; thieu FlagActive -> '0' (StdFlag).
    //   * Chuẩn hoá (:1109-1113):
    //       BrandCode = StdParam (Trim+ToUpper, rỗng -> null);
    //       NetworkID = StdParam (không lưu);
    //       BrandName = StdStr (Trim+Normalize, rỗng -> null — KHÔNG viết hoa, KHÁC Create #80 dùng StdParam);
    //       Remark = StdStr (Trim+Normalize, rỗng -> null);
    //       FlagActive = StdFlag ("1" chỉ khi đúng "1", còn lại — kể cả thiếu/null/rỗng — là "0").
    //   * Kiểm tra: Mst_CarBrand_CheckDB(BrandCode, Flag "1", "") (:1124-1130 -> :28-91).
    //       Nếu không tìm thấy dòng -> ErridnLoyalty.Mst_CarBrand_CheckDB_BrandNotFound (:65-75).
    //       BrandCode null/rỗng -> không tìm thấy -> ErridnLoyalty.Mst_CarBrand_CheckDB_BrandNotFound.
    //       KHÔNG kiểm tra gì khác.
    //   * Lưu (:1141-1153): BrandName, Remark, FlagActive (3 cột trong Ft_Cols_Upd) + LogLUDTimeUTC (UtcNow) + LogLUBy ("port").
    //       NetworkID và BrandCode KHÔNG đổi.
    //   * Thành công: gate :474-475 SetSuccess + Data = null.
    //
    // Bỏ qua có chủ đích (loại Đ):
    //   * CheckGatewayAuthentication/CheckAuthorize (gate :441-442), Sys_User_CheckAuthorize (:979-992):
    //     hệ thống đích chưa có hệ phân quyền.
    //   * Ghi RequestLog/ReturnLog (:960-967, :1056-1063).
    //   * LogLUBy gán cố định "port" do đích chưa có session người dùng (:1146).
    // ============================================================================================
    [HttpPost("update")]
    public async Task<IActionResult> Update([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] MstCarBrandUpdateReq? req)
    {
        // 1. Body rỗng/null: gate Mst_CarBrandController.cs:431-438 ném Exception("strJson null") -> mã mặc định Mst_CarBrand_Update (:940).
        if (req == null)
        {
            return PortResult.Fail("Mst_CarBrand_Update", "Check.strJson", null);
        }

        // 2. Mở 1 transaction cho toàn bộ nghiệp vụ (Master.Loyalty.cs:957 _cf.db.BeginTransaction()).
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // 3. Ft_Cols_Upd cố định ở gate (:451-456) rồi biz StdParam (ToUpper) + Contains (:1106-1117).
            //    Ft_Cols_Upd co dinh o gate -> luon cap nhat 3 cot; thieu FlagActive -> '0' (StdFlag).
            var strFt_Cols_Upd = "Mst_CarBrand.BrandCode,Mst_CarBrand.BrandName,Mst_CarBrand.Remark,Mst_CarBrand.FlagActive,";
            strFt_Cols_Upd = StdParam(strFt_Cols_Upd) ?? "";
            bool bUpd_BrandName = strFt_Cols_Upd.Contains("Mst_CarBrand.BrandName".ToUpperInvariant());
            bool bUpd_Remark = strFt_Cols_Upd.Contains("Mst_CarBrand.Remark".ToUpperInvariant());
            bool bUpd_FlagActive = strFt_Cols_Upd.Contains("Mst_CarBrand.FlagActive".ToUpperInvariant());

            // 4. Chuẩn hoá dữ liệu input (Master.Loyalty.cs:1109-1113):
            //    StdParam = Trim().ToUpper(), rỗng -> null (05.Refs.Biz\Biz\Loyalty.TVO\Loyalty.TVO.Utils\Utils.cs:29).
            //    StdStr   = Trim().Normalize(), rỗng -> null (cùng tệp Utils.cs:29).
            //    StdFlag  = "1" nếu đúng "1", mọi giá trị khác -> "0".
            //    LƯU Ý: BrandName dùng StdStr (:1111) nên KHÔNG VIẾT HOA (khác Create #80 dùng StdParam).
            string? strBrandCode = StdParam(req.BrandCode);
            string? strBrandName = StdStr(req.BrandName);
            string? strRemark = StdStr(req.Remark);
            string strFlagActive = StdFlag(req.FlagActive);

            // 5. Kiểm tra dòng tồn tại: Mst_CarBrand_CheckDB(BrandCode, Flag "1", "") (Master.Loyalty.cs:1124-1130 -> :28-91).
            //    Tìm dòng bằng Like + PortQuery.Equal (tracking). BrandCode null/rỗng -> không tìm thấy -> BrandNotFound.
            var entity = string.IsNullOrEmpty(strBrandCode)
                ? null
                : await _db.Set<Mst_CarBrand>().FirstOrDefaultAsync(x =>
                    EF.Functions.Like((x.BrandCode ?? "").ToLower(), PortQuery.Equal(strBrandCode), PortQuery.Esc));

            if (entity == null)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_CarBrand_CheckDB_BrandNotFound", "Check.BrandCode", strBrandCode);
            }

            // 6. Gán 3 cột + log (Master.Loyalty.cs:1141-1147):
            //    Ft_Cols_Upd co dinh o gate -> luon cap nhat 3 cot; thieu FlagActive -> '0' (StdFlag).
            if (bUpd_BrandName)
            {
                entity.BrandName = strBrandName;               // :1142 BrandName (StdStr, không viết hoa)
            }
            if (bUpd_Remark)
            {
                entity.Remark = strRemark;                     // :1143 Remark (StdStr)
            }
            if (bUpd_FlagActive)
            {
                entity.FlagActive = strFlagActive;             // :1144 FlagActive (StdFlag)
            }
            entity.LogLUDTimeUTC = DateTime.UtcNow;            // :1145 LogLUDTimeUTC = dtimeSys (UtcNow)
            entity.LogLUBy = "port";                           // :1146 LogLUBy ("port" - đích chưa có session user, loại Đ)

            await _db.SaveChangesAsync();

            // 7. Commit transaction (Master.Loyalty.cs:1029 TDALUtils.DBUtils.CommitSafety).
            await tx.CommitAsync();

            // 8. Thành công: gate Mst_CarBrandController.cs:474-475 SetSuccess + Data = null.
            return Ok(new
            {
                Success = true,
                Data = (object?)null
            });
        }
        catch (Exception)
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    // ============================================================================================
    // Delete — nguồn Mst_CarBrandController.cs:492-555 (gate) -> MstCarBrandController.cs:165-235 (API)
    //   -> Master.Loyalty.cs:1157-1235 (WAS_Mst_CarBrand_Delete) -> :1236-1372 (Mst_CarBrand_Delete)
    //   -> :1374-1430 (Mst_CarBrand_DeleteX) -> :28-91 (Mst_CarBrand_CheckDB).
    //
    // GIỮ ĐÚNG NGUỒN:
    //   * Kiểm tra mã rỗng tại gate (Mst_CarBrandController.cs:511, 541): BrandCode rỗng -> ném
    //     ServiceException("invalid_Brandcode", "invalid_Brandcode") (GIỮ NGUYÊN chữ "Brandcode" viết thường 'c' từ nguồn).
    //   * Mở 1 transaction cho toàn bộ nghiệp vụ (Master.Loyalty.cs:1269 _cf.db.BeginTransaction()).
    //   * Chuẩn hoá mã qua StdParam (:1400): Trim + ToUpper, rỗng -> null.
    //   * CheckDB(BrandCode, FlagExist "1", "") (:1406-1412 -> Mst_CarBrand_CheckDB :28-91):
    //     FlagExist "1" + KHÔNG có dòng (hoặc strBrandCode rỗng) -> ErridnLoyalty.Mst_CarBrand_CheckDB_BrandNotFound.
    //   * Ràng buộc khoá ngoại: không có FK tới Mst_CarBrand trong DDL — dòng xe có thể mồ côi
    //     (khong co FK toi Mst_CarBrand trong DDL — dong xe co the mo coi).
    //     Mst_CarModel.BrandCode trỏ tên nhưng KHÔNG có ràng buộc -> xóa hãng đang được dòng xe dùng VẪN THÀNH CÔNG.
    //   * Xoá dòng Mst_CarBrand (:1421-1427 dtDB_Mst_CarBrand.Rows[0].Delete()) và CommitSafety (:1335).
    //   * Thành công: gate :535-536 SetSuccess + Data = null.
    //
    // Bỏ qua có chủ đích (loại Đ):
    //   * CheckGatewayAuthentication/CheckAuthorize (gate :501-502), Sys_User_CheckAuthorize (:1291-1305):
    //     hệ thống đích chưa có hệ phân quyền.
    //   * Ghi RequestLog/ReturnLog (:1272-1279, :1362-1369).
    // ============================================================================================
    [HttpPost("delete")]
    public async Task<IActionResult> Delete([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] MstCarBrandDeleteReq? req)
    {
        // 1. Kiểm tra mã rỗng tại gate (Mst_CarBrandController.cs:511, 541):
        //    Giữ nguyên chữ "Brandcode" viết thường 'c': ServiceException("invalid_Brandcode", "invalid_Brandcode")
        if (string.IsNullOrWhiteSpace(req?.BrandCode))
        {
            return PortResult.Fail("invalid_Brandcode", "Check.BrandCode", req?.BrandCode);
        }

        // 2. Thực hiện nghiệp vụ xóa qua DeleteXAsync (Master.Loyalty.cs:1236-1372)
        var err = await DeleteXAsync(req?.BrandCode);
        if (err != null)
        {
            return err;
        }

        // 3. Thành công: gate Mst_CarBrandController.cs:535-536 SetSuccess + Data = null
        return Ok(new
        {
            Success = true,
            Data = (object?)null
        });
    }

    /// <summary>
    /// Port 1:1 DeleteMultiple action từ gate Mst_CarBrandController.cs:557-621.
    /// Chuỗi gọi: gate :557-621 -> lặp qua danh sách Lst_Mst_CarBrand, mỗi phần tử gọi
    /// WA_Mst_CarBrand_Delete (:588-605) -> biz WAS_Mst_CarBrand_Delete -> Mst_CarBrand_Delete -> Mst_CarBrand_DeleteX.
    ///
    /// Request JSON: mảng [{ BrandCode }].
    /// 1. Kiểm tra body rỗng / null: Gate Mst_CarBrandController.cs:567-574
    ///    if (!string.IsNullOrEmpty(strJson)) { ... } else { throw new ServiceException("strJson null"); }
    ///    Khi body null hoặc rỗng -> trả về 400 ErridnLoyalty.Mst_CarBrand_Delete với Check.strJson = null.
    /// 2. Danh sách rỗng `[]`: vòng lặp foreach không chạy, trả về Ok(Success = true, Data = null).
    /// 3. Lặp tuần tự qua từng phần tử trong danh sách:
    ///    Mỗi lần lặp là một lời gọi độc lập tới chuỗi nghiệp vụ Delete, sử dụng TRANSACTION RIÊNG
    ///    (Master.Loyalty.cs:1269 _cf.db.BeginTransaction() và :1335 CommitSafety).
    ///    - KHÔNG có bước kiểm tra BrandCode rỗng ở tầng gate như Delete đơn (:511, 541);
    ///      khi BrandCode rỗng, lỗi phát sinh ở tầng nghiệp vụ Mst_CarBrand_CheckDB
    ///      -> trả về 400 ErridnLoyalty.Mst_CarBrand_CheckDB_BrandNotFound.
    ///    - Hành vi "xóa một phần" (nguồn Mst_CarBrandController.cs:588-614):
    ///      Nếu phần tử thứ k gặp lỗi, exception được ném ra và thoát khỏi vòng lặp ngay lập tức
    ///      nhảy vào khối catch (ex) (:610-614).
    ///      Do mỗi phần tử chạy trên một transaction riêng đã được commit, các phần tử TRƯỚC k
    ///      ĐÃ BỊ XÓA VÀ KHÔNG BỊ ROLLBACK; các phần tử SAU k hoàn toàn không được xử lý.
    ///      "xóa một phần" là hành vi nguồn.
    /// 4. Thành công: gate :607-608 trả objJsonResultEntry.Data = null -> Ok(new { Success = true, Data = (object?)null }).
    ///
    /// Bỏ qua có chủ đích (loại Đ):
    /// - CheckGatewayAuthentication: gate Mst_CarBrandController.cs:577
    /// - CheckAuthorize: gate Mst_CarBrandController.cs:578 (hệ thống đích chưa có hệ phân quyền)
    /// - Sys_User_CheckAuthorize: Master.Loyalty.cs:1291-1305
    /// - Ghi RequestLog/ReturnLog: Master.Loyalty.cs:1272-1279, 1362-1369
    /// </summary>
    [HttpPost("deletemultiple")]
    public async Task<IActionResult> DeleteMultiple([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] List<MstCarBrandDeleteReq>? req)
    {
        // 1. Kiểm tra body rỗng / null: Gate Mst_CarBrandController.cs:567-574
        // if (!string.IsNullOrEmpty(strJson)) { ... } else { throw new ServiceException("strJson null"); }
        if (req == null)
        {
            return PortResult.Fail("Mst_CarBrand_Delete", "Check.strJson", null);
        }

        // 2. Lặp tuần tự qua từng phần tử (gate Mst_CarBrandController.cs:588-605).
        // Mỗi phần tử chạy trong 1 transaction riêng biệt (hàm DeleteXAsync).
        // Nếu phần tử thứ k gặp lỗi -> return ngay lỗi đó (hành vi nguồn :588-614:
        // "xóa một phần" là hành vi nguồn: các phần tử trước k đã commit thành công
        // vào DB và KHÔNG bị rollback; các phần tử sau k không được xử lý).
        foreach (var item in req)
        {
            var err = await DeleteXAsync(item?.BrandCode);
            if (err != null)
            {
                return err;
            }
        }

        // 3. Thành công: gate Mst_CarBrandController.cs:607-608
        return Ok(new
        {
            Success = true,
            Data = (object?)null
        });
    }

    /// <summary>
    /// Nghiệp vụ xóa 1 hãng xe, tương ứng chuỗi gọi:
    /// API WA_Mst_CarBrand_Delete (MstCarBrandController.cs:165-235) -> biz WAS_Mst_CarBrand_Delete (Master.Loyalty.cs:1157-1235)
    /// -> Mst_CarBrand_Delete (:1236-1372) -> Mst_CarBrand_DeleteX (:1374-1430) -> Mst_CarBrand_CheckDB (:28-91).
    ///
    /// Mỗi lần gọi mở 1 Transaction riêng (Master.Loyalty.cs:1269 _cf.db.BeginTransaction() và :1335 CommitSafety).
    /// Chuẩn hoá mã qua StdParam: Trim, ToUpper, rỗng -> null (Master.Loyalty.cs:1400).
    /// - Khi mã rỗng hoặc không tìm thấy hãng xe trong DB: trả về 400 ErridnLoyalty.Mst_CarBrand_CheckDB_BrandNotFound (Mst_CarBrand_CheckDB :28-91).
    /// - Xóa dòng hãng xe và Commit.
    /// Trả về null nếu thành công; trả về IActionResult (PortResult.Fail) nếu có lỗi.
    /// </summary>
    private async Task<IActionResult?> DeleteXAsync(string? code)
    {
        // 1. Mở 1 transaction cho mỗi phần tử (Master.Loyalty.cs:1269 _cf.db.BeginTransaction()).
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // 2. Chuẩn hoá mã qua StdParam: Trim, ToUpper, rỗng -> null (Master.Loyalty.cs:1400)
            string? strBrandCode = StdParam(code);

            // 3. Mst_CarBrand_CheckDB(BrandCode, Flag "1", "") (Master.Loyalty.cs:1406-1412 -> :28-91):
            //    FlagExist "1" + KHÔNG có dòng (hoặc strBrandCode rỗng) -> ErridnLoyalty.Mst_CarBrand_CheckDB_BrandNotFound.
            if (string.IsNullOrEmpty(strBrandCode))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_CarBrand_CheckDB_BrandNotFound", "Check.BrandCode", strBrandCode);
            }

            // Tìm dòng bằng Like + PortQuery.Equal (hạ chữ 2 phía, SQL Server collation không phân biệt hoa thường)
            var entity = await _db.Set<Mst_CarBrand>().FirstOrDefaultAsync(x =>
                EF.Functions.Like((x.BrandCode ?? "").ToLower(), PortQuery.Equal(strBrandCode), PortQuery.Esc));

            if (entity == null)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_CarBrand_CheckDB_BrandNotFound", "Check.BrandCode", strBrandCode);
            }

            // 4. Ràng buộc khoá ngoại: không có FK tới Mst_CarBrand trong DDL — dòng xe có thể mồ côi
            //    (khong co FK toi Mst_CarBrand trong DDL — dong xe co the mo coi).
            //    Không có bảng nào trong DDL có REFERENCES Mst_CarBrand; Mst_CarModel.BrandCode chỉ trỏ theo tên
            //    nhưng không có FK nên xóa hãng xe đang được dòng xe sử dụng vẫn thành công.

            // 5. Xoá dòng Mst_CarBrand (Master.Loyalty.cs:1421-1427 dtDB_Mst_CarBrand.Rows[0].Delete())
            _db.Set<Mst_CarBrand>().Remove(entity);
            await _db.SaveChangesAsync();

            // 6. Commit transaction (Master.Loyalty.cs:1335 TDALUtils.DBUtils.CommitSafety)
            await tx.CommitAsync();

            return null;
        }
        catch (Exception)
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    // ============================================================================================
    // Import — nguồn gate Mst_CarBrandController.cs:625-769 -> API WA_Mst_CarBrand_Create
    // (MstCarBrandController.cs:20-90) -> biz WAS_Mst_CarBrand_Create (Master.Loyalty.cs:502-584)
    // -> Mst_CarBrand_Create (Master.Loyalty.cs:585-731) -> Mst_CarBrand_CreateX (Master.Loyalty.cs:732-828).
    //
    // GIẢ ĐỊNH (kế thừa từ Data/PortExcel.cs):
    //   - Định dạng file Excel: dòng 1 = tiêu đề (đúng mẫu GetImportDicColumsTemplate), dữ liệu bắt đầu từ ô A2 (firstDataRow = 2).
    //   - Cột ánh xạ theo vị trí: cột 1 = BrandCode, cột 2 = BrandName (GetImportDicColumsTemplate có 2 cột:
    //     BrandCode -> "Mã hiệu xe (*)", BrandName -> "Tên hiệu xe (*)").
    //
    // Quy trình và thứ tự kiểm tra:
    //   1. Kiểm tra file upload: gate Mst_CarBrandController.cs:648-654
    //      - Thiếu file hoặc file rỗng hoặc đuôi file != ".xlsx" -> Fail("Mst_CarBrand_Import", "Message", "File excel import không hợp lệ")
    //   2. Đọc file bằng PortExcel.ReadSheet(stream, new[] { "BrandCode", "BrandName" }):
    //      - Bảng không có dòng dữ liệu (table == null || table.Rows.Count == 0) -> Fail("Mst_CarBrand_Import", "Message", "File excel import không có dữ liệu") (:665, :754-755)
    //      - Số cột tiêu đề != GetImportDicColumsTemplate().Count (2 cột) -> Fail("Mst_CarBrand_Import", "Message", "File excel import không hợp lệ") (:668-672)
    //   3. Kiểm tra TẤT CẢ các dòng TRƯỚC khi tạo (#region["Check null"] :676-701):
    //      - BrandCode rỗng / khoảng trắng -> Fail("Mst_CarBrand_Import", "Message", "Mã hiệu xe không được trống") (:678-682)
    //      - BrandName rỗng / khoảng trắng -> Fail("Mst_CarBrand_Import", "Message", "Tên hiệu xe không được trống") (:683-687)
    //      Khối kiểm trùng trong file (:704-721) BỊ COMMENT ở nguồn -> KHÔNG port.
    //      Nếu có bất kỳ dòng nào vi phạm ở bước này, dừng ngay và KHÔNG có dòng nào được tạo vào CSDL.
    //   4. Lưu từng dòng (hành vi "import một phần"):
    //      - NetworkID lấy từ cấu hình: IConfiguration["Biz_NetworkID"] ?? "1001"
    //        (GAP: nguồn lấy NetworkID = orgUserUI.NetworkId từ user đăng nhập :731).
    //      - Duyệt từng dòng theo thứ tự, gọi CreateXAsync(brandCode, networkId, brandName, null) riêng cho mỗi dòng
    //        (mỗi dòng 1 transaction biz riêng biệt: gate :728-746).
    //      - Nếu dòng thứ k gặp lỗi (vd trùng mã Mst_CarBrand_CheckDB_BrandExist),
    //        vòng lặp dừng ngay lập tức, trả về đúng lỗi của CreateXAsync cho dòng k.
    //      - CÁC DÒNG TRƯỚC k ĐÃ ĐƯỢC COMMIT VÀO DB (không rollback các dòng đã tạo trước đó — "import một phần" là hành vi nguồn).
    //      - Các dòng sau k không được duyệt.
    //   5. Thành công: trả Ok({ Success = true, Data = null }) (:749-750).
    //
    // Bỏ qua có chủ đích (loại Đ):
    //   - CheckGatewayAuthentication, CheckAuthorize (:636-637)
    //   - Nguồn lưu file tạm xuống đĩa (FileUtils.SaveTempFile :657) -> ở đây đọc trực tiếp từ stream qua ClosedXML
    //   - Session người dùng orgUserUI (:635, :731, :737-741).
    // ============================================================================================
    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Import(IFormFile? file)
    {
        // 1. Kiểm tra file upload: gate Mst_CarBrandController.cs:648-654
        if (file == null || file.Length == 0)
        {
            return PortResult.Fail("Mst_CarBrand_Import", "Message", "File excel import không hợp lệ");
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".xlsx")
        {
            return PortResult.Fail("Mst_CarBrand_Import", "Message", "File excel import không hợp lệ");
        }

        // 2. Đọc bảng tính bằng PortExcel.ReadSheet (giả định định dạng file Excel từ PortExcel)
        int columnCount;
        List<Dictionary<string, string?>> rows;
        try
        {
            using var stream = file.OpenReadStream();
            (columnCount, rows) = PortExcel.ReadSheet(stream, new[] { "BrandCode", "BrandName" });
        }
        catch
        {
            return PortResult.Fail("Mst_CarBrand_Import", "Message", "File excel import không hợp lệ");
        }

        // Kiểm tra có dữ liệu hay không: gate :666, :754-755 (table == null || table.Rows.Count == 0)
        if (rows.Count == 0)
        {
            return PortResult.Fail("Mst_CarBrand_Import", "Message", "File excel import không có dữ liệu");
        }

        // Kiểm tra số cột dòng tiêu đề so với GetImportDicColumsTemplate().Count (2 cột): gate :668-672
        if (columnCount != GetImportDicColumsTemplate().Count)
        {
            return PortResult.Fail("Mst_CarBrand_Import", "Message", "File excel import không hợp lệ");
        }

        // 3. Kiểm tra TẤT CẢ các dòng TRƯỚC khi tạo (Check null: gate Mst_CarBrandController.cs:676-701)
        foreach (var r in rows)
        {
            var brandCode = r.GetValueOrDefault("BrandCode");
            if (string.IsNullOrWhiteSpace(brandCode))
            {
                return PortResult.Fail("Mst_CarBrand_Import", "Message", "Mã hiệu xe không được trống");
            }

            var brandName = r.GetValueOrDefault("BrandName");
            if (string.IsNullOrWhiteSpace(brandName))
            {
                return PortResult.Fail("Mst_CarBrand_Import", "Message", "Tên hiệu xe không được trống");
            }
        }

        // 4. Lưu từng dòng tuần tự: mỗi dòng 1 transaction riêng (hành vi "import một phần": gate :728-746)
        // GAP: nguồn lấy NetworkID = orgUserUI.NetworkId (:731) từ user đăng nhập; đích chưa có user -> dùng IConfiguration["Biz_NetworkID"] ?? "1001"
        var networkId = _config["Biz_NetworkID"] ?? "1001";
        foreach (var r in rows)
        {
            var brandCode = r.GetValueOrDefault("BrandCode");
            var brandName = r.GetValueOrDefault("BrandName");

            var err = await CreateXAsync(brandCode, networkId, brandName, null);
            if (err != null)
            {
                // Lỗi ở dòng k -> thoát ngay, trả đúng lỗi của Create, các dòng trước k đã tạo thành công
                return err;
            }
        }

        // Thành công: gate :749-750
        return Ok(new
        {
            Success = true,
            Data = (object?)null
        });
    }

    private static Dictionary<string, string> GetImportDicColumsTemplate()
    {
        return new Dictionary<string, string>
        {
            { "BrandCode", "Mã hiệu xe (*)" },
            { "BrandName", "Tên hiệu xe (*)" }
        };
    }

    // ============================================================================================
    // Export — nguồn gate Mst_CarBrandController.cs:774-852 -> API WA_Mst_CarBrand_Get
    // (MstCarBrandController.cs:238-307) -> biz WAS_Mst_CarBrand_Get (Master.Loyalty.cs:93-193)
    // -> Mst_CarBrand_Get (Master.Loyalty.cs:194-346) -> Mst_CarBrand_GetX (Master.Loyalty.cs:347-501).
    //
    // GIỮ ĐÚNG NGUỒN:
    //   * Tham số nhận qua body: OrgID, KeyWord, FlagActive (:774, :795-800).
    //   * strWhereClause_Mst_CarBrand (:283-308):
    //       - KeyWord: like '%x%' trên BrandName (:290).
    //       - FlagActive: danh sách IN (:297-305).
    //       - OrgID: không lọc (:283-308 không có nhánh lọc OrgID).
    //   * Ft_RecordStart = "0", Ft_RecordCount = "123456000" (:813-814, BaseController.cs:30-31),
    //     Rt_Cols_Mst_CarBrand = "*" (:817).
    //   * Thứ tự: Mst_CarBrand_GetX chia trang theo BrandCode asc (:407) rồi sắp theo MyIdxSeq asc (:441-447).
    //   * Nếu danh sách rỗng (rows == null || rows.Count == 0):
    //     ném ServiceException("Không có dữ liệu") (:821, :838) -> trả 400 ErridnLoyalty.Mst_CarBrand_Export.
    //   * Từ điển cột: GetImportDicColums() (:825, :891-901) gồm đúng 3 cột:
    //       1. BrandCode -> "Mã hiệu xe (*)"
    //       2. BrandName -> "Tên hiệu xe (*)"
    //       3. FlagActive -> "Trạng thái"
    //     (dòng DepartmentCode và MST bị comment ở nguồn — KHÔNG port).
    //   * Sinh đường dẫn file xuất: BaseController.GenExcelExportFilePath (:827)
    //     -> PortExcel.GenExcelExportFilePath(webRoot, "Mst_CarBrand").
    //   * Tên sheet: "Mst_CarBrand" (:828).
    //   * Ghi file qua PortExcel.ExportToExcelFromList(rows, dicColNames, physicalPath, "Mst_CarBrand") (:828).
    //   * Trả URL tải file với Success = true, Data = { Url = PortExcel.GetRequestBaseAddress(Request) + virtualPath } (:829-833).
    //
    // Bỏ qua có chủ đích (loại Đ):
    //   * CheckGatewayAuthentication: gate Mst_CarBrandController.cs:784; API MstCarBrandController.cs:259-264
    //   * CheckAuthorize: gate Mst_CarBrandController.cs:785 (hệ thống đích chưa có hệ phân quyền)
    //   * Sys_User_CheckAuthorize: Master.Loyalty.cs:251-264
    //   * Ghi RequestLog/ReturnLog: Master.Loyalty.cs:238-245, 336-343
    // ============================================================================================
    [HttpPost("export")]
    public async Task<IActionResult> Export([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] MstCarBrandExportReq? req)
    {
        req ??= new MstCarBrandExportReq();

        // Nguồn: strWhereClause_Mst_CarBrand (:283-308) chỉ lọc theo KeyWord (like trên BrandName) và FlagActive (in),
        // OrgID truyền vào (:798) nhưng KHÔNG được sử dụng trong hàm tạo WHERE clause -> không lọc OrgID.
        // Ft_RecordStart = "0", Ft_RecordCount = "123456000", Rt_Cols_Mst_CarBrand = "*" (:813-817).
        var (_, rows) = await GetXAsync(req.KeyWord, req.FlagActive, 0, 123456000);

        // Nguồn Mst_CarBrandController.cs:821, 836-838: nếu danh sách rỗng -> throw ServiceException("Không có dữ liệu")
        if (rows == null || rows.Count == 0)
        {
            return PortResult.Fail("Mst_CarBrand_Export", "Message", "Không có dữ liệu");
        }

        // Tạo thư mục và đường dẫn file Excel xuất: BaseController.GenExcelExportFilePath (:827)
        var webRoot = _env.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
        {
            webRoot = Path.Combine(_env.ContentRootPath, "wwwroot");
        }

        // Lấy từ điển cột: Mst_CarBrandController.cs:825 gọi GetImportDicColums() (3 cột)
        var dicColNames = GetImportDicColums();
        var (physicalPath, virtualPath) = PortExcel.GenExcelExportFilePath(webRoot, "Mst_CarBrand");

        // Ghi danh sách ra file Excel: ExcelExport.ExportToExcelFromList (:828) với sheet "Mst_CarBrand"
        PortExcel.ExportToExcelFromList(rows, dicColNames, physicalPath, "Mst_CarBrand");

        // Trả URL tải file: Data = { Url = GetRequestBaseAddress() + url } (:829-833)
        return Ok(new
        {
            Success = true,
            Data = new
            {
                Url = PortExcel.GetRequestBaseAddress(Request) + virtualPath
            }
        });
    }

    // ============================================================================================
    // ExportTemplate — nguồn gate Mst_CarBrandController.cs:855-887.
    // Action này KHÔNG kiểm quyền ở nguồn (không có CheckGatewayAuthentication và không có CheckAuthorize).
    // Không gọi tầng service, không truy vấn cơ sở dữ liệu.
    // Khởi tạo danh sách rỗng kiểu Mst_CarBrand (:863).
    // Lấy từ điển 2 cột mẫu import từ GetImportDicColumsTemplate (:865, :903-911) gồm:
    //   1. BrandCode -> "Mã hiệu xe (*)"
    //   2. BrandName -> "Tên hiệu xe (*)"
    // (Dùng lại hàm GetImportDicColumsTemplate() đã có ở action Import #84, không tạo bản thứ hai).
    // Sinh đường dẫn file mẫu qua PortExcel.GenExcelExportFilePath(webRoot, "Mst_CarBrand") (:867).
    // Xuất file Excel qua PortExcel.ExportToExcelFromList(list, dicColNames, physicalPath, "Mst_CarBrand") (:868).
    // Trả về Success = true, Data = { Url = PortExcel.GetRequestBaseAddress(Request) + virtualPath } (:869-873).
    // Không có nhánh báo lỗi khi danh sách rỗng (khác với Export).
    // ============================================================================================
    [HttpPost("exporttemplate")]
    public IActionResult ExportTemplate()
    {
        // Khởi tạo danh sách rỗng kiểu Mst_CarBrand (gate Mst_CarBrandController.cs:863)
        var list = new List<Mst_CarBrand>();

        // Lấy từ điển 2 cột mẫu import (gate Mst_CarBrandController.cs:865) — dùng lại GetImportDicColumsTemplate() của action import
        var dicColNames = GetImportDicColumsTemplate();

        // Xác định thư mục webRoot và sinh đường dẫn file (gate Mst_CarBrandController.cs:867)
        var webRoot = _env.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
        {
            webRoot = Path.Combine(_env.ContentRootPath, "wwwroot");
        }

        var (physicalPath, virtualPath) = PortExcel.GenExcelExportFilePath(webRoot, "Mst_CarBrand");

        // Ghi danh sách rỗng ra file Excel với tên sheet Mst_CarBrand (gate Mst_CarBrandController.cs:868)
        PortExcel.ExportToExcelFromList(list, dicColNames, physicalPath, "Mst_CarBrand");

        // Trả URL tải file với Success = true (gate Mst_CarBrandController.cs:869-873)
        return Ok(new
        {
            Success = true,
            Data = new
            {
                Url = PortExcel.GetRequestBaseAddress(Request) + virtualPath
            }
        });
    }

    #region DicColums
    /// <summary>
    /// Từ điển cột xuất Excel: gate Mst_CarBrandController.cs:891-901 (GetImportDicColums).
    /// ĐẶC BIỆT LƯU Ý THEO NGUỒN: Nguồn tại Mst_CarBrandController.cs:825 sử dụng GetImportDicColums().
    /// Giữ đúng thứ tự 3 cột từ nguồn:
    /// 1. BrandCode -> "Mã hiệu xe (*)"
    /// 2. BrandName -> "Tên hiệu xe (*)"
    /// 3. FlagActive -> "Trạng thái"
    /// (dòng DepartmentCode và MST bị comment ở nguồn — KHÔNG port).
    /// </summary>
    private static Dictionary<string, string> GetImportDicColums()
    {
        return new Dictionary<string, string>
        {
            { "BrandCode", "Mã hiệu xe (*)" },
            { "BrandName", "Tên hiệu xe (*)" },
            { "FlagActive", "Trạng thái" }
        };
    }
    #endregion

    #region Helper chuẩn hoá input (05.Refs.Biz\Biz\Loyalty.TVO\Loyalty.TVO.Utils\Utils.cs)
    /// <summary>
    /// Chuẩn hoá tham số mã: Trim, ToUpper, rỗng -> null.
    /// Dùng cho mã và tên hãng xe (BrandCode, NetworkID, BrandName...).
    /// Nguồn tham chiếu: 05.Refs.Biz\Biz\Loyalty.TVO\Loyalty.TVO.Utils\Utils.cs:29
    /// </summary>
    private static string? StdParam(object? val)
    {
        if (val == null) return null;
        var s = val.ToString()?.Trim();
        return string.IsNullOrEmpty(s) ? null : s.ToUpperInvariant();
    }

    /// <summary>
    /// Chuẩn hoá chuỗi: Trim, Normalize, rỗng -> null.
    /// Dùng cho ghi chú (Remark...).
    /// Nguồn tham chiếu: 05.Refs.Biz\Biz\Loyalty.TVO\Loyalty.TVO.Utils\Utils.cs:29
    /// </summary>
    private static string? StdStr(object? val)
    {
        if (val == null) return null;
        var s = val.ToString()?.Trim();
        if (string.IsNullOrEmpty(s)) return null;
        return s.Normalize();
    }

    /// <summary>
    /// Chuẩn hoá cờ: "1" nếu đúng "1", mọi giá trị khác (kể cả null/thiếu/rỗng) -> "0".
    /// Dùng cho cờ (FlagActive...).
    /// Nguồn tham chiếu: 05.Refs.Biz\Biz\Loyalty.TVO\Loyalty.TVO.Utils\Utils.cs
    /// </summary>
    private static string StdFlag(object? val)
    {
        if (val == null) return "0";
        var s = val.ToString()?.Trim();
        return s == "1" ? "1" : "0";
    }
    #endregion
}
