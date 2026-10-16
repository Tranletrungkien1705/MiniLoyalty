using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MiniLoyalty.Data;
using MiniLoyalty.Models.Port;

namespace MiniLoyalty.Controllers.Port;

// ============================================================================================
// PORT 1:1 — màn danh mục phòng ban (Mst_Department) của dự án nguồn 2022.7.Loyalty (#122, #123, #124, #125, #126, #127, #128, #129, #130).
//
// Nguồn controller (tầng gate):
//   Mst_DepartmentController.cs:30-109   Search -> WA_Mst_Department_Get (#122)
//   Mst_DepartmentController.cs:110-218  GetAllActive -> WA_Mst_Department_Get (#123)
//   Mst_DepartmentController.cs:219-282  GetByDepartmentCode -> WA_Mst_Department_Get (#124)
//   Mst_DepartmentController.cs:286-370  Create -> WA_Mst_Department_Create (#125)
//   Mst_DepartmentController.cs:374-443  Update -> WA_Mst_Department_Update (#126)
//   Mst_DepartmentController.cs:446-515  Delete -> WA_Mst_Department_Delete (#127)
//   Mst_DepartmentController.cs:516-580  DeleteMultiple -> WA_Mst_Department_Delete (#128)
//   Mst_DepartmentController.cs:584-730  Import -> WA_Mst_Department_Create (#129)
//   Mst_DepartmentController.cs:735-812  Export -> WA_Mst_Department_Get (#130)
//   Mst_DepartmentController.cs:851-862  GetImportDicColums (dùng cho Export)
//   Mst_DepartmentController.cs:864-873  GetImportDicColumsTemplate
//   Mst_DepartmentController.cs:877-905  strWhereClause_Mst_Department (mệnh đề lọc Search, GetAllActive, Export)
//   Mst_DepartmentController.cs:907-926  strWhereClause_Mst_Department_By_DepartmentCode (mệnh đề lọc GetByDepartmentCode)
// Nguồn API:
//   MstDepartmentController.cs:20-89     WA_Mst_Department_Get
//   MstDepartmentController.cs:91-160    WA_Mst_Department_Create (#125)
//   MstDepartmentController.cs:162-231   WA_Mst_Department_Update (#126)
//   MstDepartmentController.cs:233-302   WA_Mst_Department_Delete (#127, #128)
// Nguồn biz:
//   Master.cs:29450-29548                WAS_Mst_Department_Get
//   Master.cs:29549-29701                Mst_Department_Get
//   Master.cs:30396-30568                Mst_Department_GetX (SQL :30449-30515)
//   Master.cs:29702-29785                WAS_Mst_Department_Create (#125)
//   Master.cs:29786-29935                Mst_Department_Create (#125)
//   Master.cs:30569-30726                Mst_Department_CreateX (#125)
//   Master.cs:29936-30021                WAS_Mst_Department_Update (#126)
//   Master.cs:30022-30180                Mst_Department_Update (#126)
//   Master.cs:30727-30864                Mst_Department_UpdateX (#126)
//   Master.cs:30181-30258                WAS_Mst_Department_Delete (#127, #128)
//   Master.cs:30259-30395                Mst_Department_Delete (#127, #128)
//   Master.cs:30865-30968                Mst_Department_DeleteX (#127, #128)
//   Master.cs:29413-29448                Mst_Department_UpdBU (#125, cập nhật BUCode, BUPattern, Level toàn bảng)
//   Master.cs:7813-7890                  Mst_NNT_CheckDB (#125)
//   Master.cs:29275-29338                Mst_Department_CheckDB (#125, #126, #127)
//   Master.cs:29341-29411                Mst_Department_CheckDepartmentName (#125, #126)
//
// GIỮ ĐÚNG NGUỒN:
//   * Endpoint là POST (nguồn Mst_DepartmentController.cs:30, :110, :286).
//   * Lọc KeyWord: like '%kw%' trên DepartmentName (Mst_DepartmentController.cs:884).
//   * Lọc OrgID: tham số CLIENT ở Search (:30, :888), hoặc OrgID từ user đăng nhập ở GetAllActive (:138).
//     Toán tử '=' trên OrgID.
//   * Lọc FlagActive: tách dấu phẩy -> danh sách IN ('a','b') (Mst_DepartmentController.cs:894-902).
//     Dòng '=' (:890-893) BỊ COMMENT trong nguồn nên KHÔNG chạy.
//   * Chia trang theo md.DepartmentCode asc (Master.cs:30457), select cuối sắp theo t.MyIdxSeq asc (Master.cs:30514).
//   * MyIdxSeq 0-based identity (Master.cs:30450).
//   * PageSize = 0 -> DataList rỗng, PageCount = 0 (gate :84-94).
//   * GetAllActive: không body, Ft_RecordStart = "0", Ft_RecordCount = "123456000" (gate :152-153).
//     FlagActive = "1" (:139). OrgID lấy từ user đăng nhập orgUserUI.OrgId (:138).
//     Đích chưa có user đăng nhập: lấy từ IConfiguration["Biz_OrgID"]; nếu không cấu hình (null/rỗng)
//     thì GetXAsync bỏ nhánh lọc OrgID -> lấy mọi tổ chức (GAP: nguồn lọc theo tổ chức của user đăng nhập).
//     Khối Ext/tree (:162-193) bị comment ở nguồn -> trả Data là mảng trần list Mst_Department (null nếu rỗng) (:199-204).
//   * GetByDepartmentCode: [HttpPost("getbydepartmentcode")], body { DepartmentCode } (gate :219-282).
//     DepartmentCode rỗng -> Data null, không truy vấn (:239, :269). Ngược lại lọc DepartmentCode = (Like + PortQuery.Equal, :914).
//     Nhánh OrgID (:916-919) và FlagActive (:920-923) không truyền nên không chạy (OLD inactive VẪN lấy; OrgID user chỉ gán vào request, không lọc).
//     Ft_RecordStart = "0", Ft_RecordCount = "1" (:256-257).
//     Trả Lst_Mst_Department[0] hoặc null (:263-269).
//   * Create (#125): [HttpPost("create")], body { DepartmentCode, OrgID, DepartmentCodeParent, MST, DepartmentName, DepartmentDesc }.
//     EmptyBodyBehavior.Allow; body rỗng -> throw ServiceException("strJson null") -> lỗi mặc định Mst_Department_Create (KHÔNG tăng seq).
//     Lấy sequence qua PortSeq.NextAsync<Seq_DepartmentCode>(_db) (SeqType.DepartmentCode format "{0}DPMCODE.{3}.{1:00000}{2}", max 100000),
//     GHI ĐÈ DepartmentCode client gửi.
//     OrgID và MST lấy từ user đăng nhập (Sys_User_GetAbilityViewOfUser) -> OrgID/MST client gửi BỊ BỎ QUA.
//     Đích chưa có user: OrgID_UC = IConfiguration["Biz_OrgID"]; MST_UC = Mst_Org.MST của OrgID đó; không cấu hình / không có org -> null (GHI GAP).
//     Thứ tự kiểm tra:
//       a. DepartmentCode rỗng -> Mst_Department_Create_InvalidDepartmentCode (seq đảm bảo không xảy ra).
//       b. Mst_Department_CheckDB(code, "0") -> Mst_Department_CheckDB_DepartmentExist (không xảy ra).
//       c. Mst_NNT_CheckDB(MST_UC, "1", "1", "") -> thiếu: Mst_NNT_CheckDB_NNTNotFound; cờ khác '1': Mst_NNT_CheckDB_FlagActiveNotMatched.
//       d. DepartmentCodeParent khác rỗng -> Mst_Department_CheckDB(parent, "1", "1") -> Mst_Department_CheckDB_DepartmentNotFound / FlagActiveNotMatched.
//       e. DepartmentName rỗng -> Mst_Department_Create_InvalidDepartmentName.
//       f. Mst_Department_CheckDepartmentName(OrgID_UC, Name, "0") -> Mst_Department_CheckDB_DepartmentNameExist
//          (LIKE không tự thêm wildcard, ký tự %/_ trong tên người dùng vẫn là wildcard, không phân biệt hoa thường).
//     Lưu 13 cột: DepartmentCode, NetworkID = Biz_NetworkID ?? "1001", OrgID = OrgID_UC, DepartmentCodeParent, BUCode = "X", BUPattern = "X",
//     Level = 1, MST = MST_UC, DepartmentName, DepartmentDesc, FlagActive = "1", LogLUDTimeUTC = now, LogLUBy = "port".
//     Mst_Department_UpdBU: gốc 'HO' (mã viết cứng tại Master.cs:29416) -> BUCode 'HO', Pattern 'HO%', Level 1; lặp 7 vòng snapshot cập nhật toàn bảng.
//     1 transaction duy nhất, lỗi -> rollback + PortResult.Fail. Thành công Data = null.
//   * Update (#126): [HttpPost("update")], body { DepartmentCode, DepartmentCodeParent, OrgID, MST, DepartmentName, DepartmentDesc, FlagActive }.
//     EmptyBodyBehavior.Allow; body rỗng -> throw Exception("strJson null") -> mã mặc định Mst_Department_Update.
//     Ft_Cols_Upd CỐ ĐỊNH từ gate (:404-410): DepartmentCodeParent, DepartmentName, DepartmentDesc, FlagActive (MST bị comment).
//     Biz Mst_Department_UpdateX (Master.cs:30727-30864):
//       - StdParam: DepartmentCode, DepartmentCodeParent, OrgID, MST; StdStr: DepartmentName, DepartmentDesc; FlagActive StdFlag (thiếu -> '0').
//       - Cờ cập nhật: bUpd_MST (false do gate comment MST), bUpd_DepartmentName, bUpd_DepartmentDesc, bUpd_FlagActive.
//       - ĐẶC BIỆT: KHÔNG có cờ cập nhật DepartmentCodeParent -> cha KHÔNG BAO GIỜ đổi dù gate có gửi (ghi comment).
//       - Kiểm tra đúng thứ tự:
//         a. Mst_Department_CheckDB(ma, "1") -> Mst_Department_CheckDB_DepartmentNotFound (Mst_NNT check :30801-30810 bị comment).
//         b. Tên rỗng -> Mst_Department_UpdateX_InvalidDepartmentName.
//         c. CHỈ KHI tên mới KHÁC tên DB (so sánh chính xác string.Equals(..., StringComparison.Ordinal)):
//            -> Mst_Department_CheckDepartmentName(OrgID CỦA DÒNG DB, tên, "0") (LIKE không phân biệt hoa thường, %/_ là wildcard)
//            -> Mst_Department_CheckDB_DepartmentNameExist.
//            HỆ QUẢ: đổi KD thành "kinh doanh" (chỉ khác hoa thường) sẽ vấp chính nó -> lỗi (giữ nguyên).
//       - Lưu DB: DepartmentName, DepartmentDesc, FlagActive, LogLUDTimeUTC = now, LogLUBy = "port".
//         KHÔNG gọi UpdBU. Mã, cha, OrgID, MST, BU giữ nguyên.
//     1 transaction duy nhất, lỗi -> rollback + PortResult.Fail. Thành công Data = null (:428-429).
//   * Delete (#127): [HttpPost("delete")], body { DepartmentCode, OrgID } (gate Mst_DepartmentController.cs:446-515).
//     EmptyBodyBehavior.Allow. Kiểm tra rỗng tại gate:
//       - DepartmentCode rỗng / body rỗng -> throw ServiceException("invalid_departmentcode", "invalid_departmentcode") (:500)
//         -> 400 ErridnLoyalty.invalid_departmentcode.
//       - OrgID rỗng -> throw ServiceException("invalid_orgid", "invalid_orgid") (:495, kiểm SAU DepartmentCode)
//         -> 400 ErridnLoyalty.invalid_orgid. OrgID KHÔNG dùng trong nghiệp vụ biz sau đó.
//     Biz Mst_Department_DeleteX (Master.cs:30865-30968), tách thành DeleteXAsync(code):
//       - 1 transaction duy nhất (Master.cs:30292 _cf.db.BeginTransaction(), :30358 CommitSafety).
//       - StdParam mã phòng ban (Trim + ToUpper, rỗng -> null) (:30891).
//       - Mst_Department_CheckDB(ma, "1"): mã rỗng hoặc không tìm thấy trong DB -> ErridnLoyalty.Mst_Department_CheckDB_DepartmentNotFound (:30897-30903).
//       - Đếm nhân viên trong Staff_MapDepartment (:30909-30953): join DepartmentCode + OrgID > 0
//         -> Mst_Department_DeleteX_ExistStaff_MapDepartment — bảng Staff_MapDepartment CHƯA có ở đích -> KHÔNG chặn, GHI GAP.
//       - FK Train_CourseDepartment.DepartmentCode (20220219.z11.UpdDB.60.Course.sql:177) chưa port -> GHI GAP.
//       - KHÔNG kiểm tra phòng ban con: các phòng ban con vẫn giữ DepartmentCodeParent trỏ tới mã đã xoá (hành vi nguồn).
//       - KHÔNG gọi UpdBU: nguồn không gọi Mst_Department_UpdBU khi xoá dòng (Master.cs:30865-30968).
//       - Xoá dòng Mst_Department. Thành công Data = null (:490-491).
//   * DeleteMultiple (#128): [HttpPost("deletemultiple")], body mảng [{ DepartmentCode }] (gate Mst_DepartmentController.cs:516-580).
//     - Body rỗng hoàn toàn / null -> ServiceException("strJson null") (:526-533) -> 400 ErridnLoyalty.Mst_Department_Delete.
//     - Mảng rỗng [] -> Success = true, Data = null (:566-567).
//     - Lặp tuần tự (:547-564), mỗi phần tử gọi WA_Mst_Department_Delete trong 1 transaction riêng (DeleteXAsync).
//     - KHÔNG kiểm tra DepartmentCode rỗng và KHÔNG kiểm OrgID ở gate (khác Delete đơn #127): mã rỗng -> CheckDB -> ErridnLoyalty.Mst_Department_CheckDB_DepartmentNotFound.
//     - Lỗi ở phần tử thứ k dừng ngay, các phần tử TRƯỚC k ĐÃ XÓA ("xóa một phần" là hành vi nguồn).
//   * Import (#129): [HttpPost("import")], [Consumes("multipart/form-data")], IFormFile? file (gate Mst_DepartmentController.cs:584-730).
//     GIẢ ĐỊNH (kế thừa Data/PortExcel.cs): ExcelImportNew là DLL không có mã ⇒ giả định dòng 1 = tiêu đề, dữ liệu từ A2;
//     ánh xạ theo thứ tự 3 cột của GetImportDicColumsTemplate (:864-873): DepartmentName, DepartmentCodeParent, DepartmentDesc (DepartmentCode bị comment ở nguồn :868).
//     Kiểm tra file upload: file thiếu/rỗng hoặc đuôi != ".xlsx" -> Fail("Mst_Department_Import", "Message", "File excel import không hợp lệ") (:606-621).
//     Bảng không có dòng -> Fail("Mst_Department_Import", "Message", "File excel import không có dữ liệu") (:625, :713-716).
//     Số cột dòng tiêu đề != 3 -> Fail("Mst_Department_Import", "Message", "File excel import không hợp lệ") (:627-631).
//     Kiểm tra TẤT CẢ các dòng trước khi tạo (:635-660): DepartmentName rỗng -> Fail("Mst_Department_Import", "Message", "Tên bộ phận không được trống") (:642-646).
//     Các kiểm tra Code, Parent, MST, trùng lặp bị comment ở nguồn (:637-651, :664-679) -> không port.
//     Lưu từng dòng: mỗi dòng 1 transaction riêng biệt (hành vi "import một phần": lỗi dòng k dừng ngay, các dòng trước k đã lưu);
//     DepartmentCode = Guid.NewGuid().ToString() (:689) (khác Create đơn dùng seq DPMCODE..., Import không qua seq; qua StdParam thành GUID IN HOA 36 ký tự);
//     OrgID/MST client gửi ("ALL") bị bỏ qua, thực tế lấy theo cấu hình Biz_OrgID giống #125;
//     gọi CreateXAsync lưu dữ liệu và gọi UpdBUAsync cập nhật cấu trúc BU toàn bảng.
//     Thành công Data = null (:710-711).
//   * Cột tính md_DepartmentNameParent: left join Mst_Department f on t.DepartmentCodeParent = f.DepartmentCode (:30512-30513),
//     lấy tên phòng ban cấp trên f.DepartmentName (null nếu không có cấp trên).
//   * Cột tính ss_QtyStaff: nguồn left join count StaffCode từ Staff_MapDepartment group by DepartmentCode (:30490-30499, :30506).
//     Bảng Staff_MapDepartment KHÔNG có ở đích, không có DDL, không có trong ledger -> ss_QtyStaff = 0 cho mọi dòng.
//     GHI GAP: khi port Staff_MapDepartment phải đếm thật.
//   * DDL: không có DDL trong repo nguồn — PK = DepartmentCode (khoá draft), kiểu cột cần xác nhận.
//
// Bỏ qua có chủ đích (loại Đ):
//   - CheckGatewayAuthentication: gate :39, :123, :228, :306, :394, :455, :536, API :40-46, :112-117, :147-152, :183-188, :254-259
//   - CheckAuthorize: gate :40, :124, :229, :307, :395, :456, :537 (hệ thống đích chưa có hệ phân quyền)
//   - Ghi RequestLog/ReturnLog: Master.cs:29593-29600, 29691-29698, 29830-29837, 29925-29932, 30072-30079, 30170-30177, 30295-30302, 30385-30392
//   - Sys_User_CheckAuthorize / Sys_Access_CheckDenyV30: Master.cs:29606-29620, 29630-29634, 29849-29870, 30091-30112, 30314-30335
// ============================================================================================

/// <summary>Port 1:1 <c>Mst_DepartmentSearch</c> — tham số lọc + phân trang của màn (Mst_DepartmentController.cs:30, 57-61).</summary>
public class MstDepartmentSearchReq
{
    public string? OrgID { get; set; }               // Tổ chức — tham số client, toán tử '=' (:30, :59, :888)
    public string? KeyWord { get; set; }             // Từ khoá tìm theo tên phòng ban (DepartmentName), like '%kw%' (:30, :58, :884)
    public string? FlagActive { get; set; }          // Cờ hoạt động, danh sách phân cách dấu phẩy -> IN (:30, :60, :894-902)
    public int Ft_PageIndex { get; set; } = 0;       // Trang (0-based) (:30, :51)
    public int Ft_PageSize { get; set; } = 10;       // Số dòng/trang (:30, :52)
}

/// <summary>Port 1:1 <c>GetByDepartmentCode</c> — body chỉ có DepartmentCode (Mst_DepartmentController.cs:219-282).</summary>
public class MstDepartmentGetByDepartmentCodeReq
{
    public string? DepartmentCode { get; set; }       // Mã phòng ban cần lấy — :239-243
}

/// <summary>
/// Body của Create: { DepartmentCode, OrgID, DepartmentCodeParent, MST, DepartmentName, DepartmentDesc }
/// Nguồn gate Mst_DepartmentController.cs:286-370 (#125).
/// </summary>
public class MstDepartmentCreateReq
{
    public string? DepartmentCode { get; set; }
    public string? OrgID { get; set; }
    public string? DepartmentCodeParent { get; set; }
    public string? MST { get; set; }
    public string? DepartmentName { get; set; }
    public string? DepartmentDesc { get; set; }
}

/// <summary>
/// Body của Update: { DepartmentCode, DepartmentCodeParent, OrgID, MST, DepartmentName, DepartmentDesc, FlagActive }
/// Nguồn gate Mst_DepartmentController.cs:374-443 (#126).
/// </summary>
public class MstDepartmentUpdateReq
{
    public string? DepartmentCode { get; set; }
    public string? DepartmentCodeParent { get; set; }
    public string? OrgID { get; set; }
    public string? MST { get; set; }
    public string? DepartmentName { get; set; }
    public string? DepartmentDesc { get; set; }
    public string? FlagActive { get; set; }
}

/// <summary>
/// Body của Delete: { DepartmentCode, OrgID }
/// Nguồn gate Mst_DepartmentController.cs:446-515 (#127).
/// </summary>
public class MstDepartmentDeleteReq
{
    public string? DepartmentCode { get; set; }
    public string? OrgID { get; set; }
}

/// <summary>
/// Body của Export: { OrgID, KeyWord, FlagActive }
/// Nguồn gate Mst_DepartmentController.cs:735 (#130).
/// </summary>
public class MstDepartmentExportReq
{
    public string? OrgID { get; set; }
    public string? KeyWord { get; set; }
    public string? FlagActive { get; set; }
}

[ApiController]
[Route("api/mst_department")]
[Produces("application/json")]
public class Mst_DepartmentControllerPort : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;

    public Mst_DepartmentControllerPort(AppDbContext db, IConfiguration config, IWebHostEnvironment env)
    {
        _db = db;
        _config = config;
        _env = env;
    }

    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] MstDepartmentSearchReq? req)
    {
        var r = req ?? new MstDepartmentSearchReq();
        var pageIndex = Math.Max(0, r.Ft_PageIndex);
        var pageSize = r.Ft_PageSize;

        // Search gọi GetXAsync với start = PageIndex * PageSize, count = PageSize (giữ nguyên hành vi nguồn Mst_DepartmentController.cs:73-74).
        var start = (long)pageIndex * pageSize;
        var (itemCount, rows) = await GetXAsync(r.KeyWord, r.OrgID, r.FlagActive, start, pageSize);

        // PageSize=0 -> DataList rỗng (nguồn: nFilterRecordEnd = start-1, SQL biz + gate :84-92).
        // PageCount = 0 khi DataList rỗng, ngược lại ceil(ItemCount / ps) với ps = PageSize (PageSize=0 thì 10) — gate :87-92.
        var ps = pageSize == 0 ? 10 : pageSize;
        var pageCount = rows.Count == 0 ? 0 : (itemCount % ps == 0 ? itemCount / ps : itemCount / ps + 1);

        return Ok(new { Success = true, Data = new PageInfo<object>(rows, itemCount, pageCount, pageIndex, pageSize) });
    }

    /// <summary>
    /// Port 1:1 <c>Mst_DepartmentController.GetAllActive</c> (nguồn gate Mst_DepartmentController.cs:110-218).
    /// Nguồn gọi <c>strWhereClause_Mst_Department</c> với <c>OrgID = orgUserUI.OrgId</c>, <c>FlagActive = "1"</c> (:136-140) ⇒ lọc
    /// phòng ban đang hoạt động thuộc tổ chức của user đang đăng nhập.
    /// Không body: RecordStart = "0", RecordCount = "123456000" (nguồn Ft_RecordStart/Ft_RecordCount gate :152-153).
    /// Khối Ext/tree (:162-193) bị comment ở nguồn. Trả <c>Ok(new { Success = true, Data = ... })</c> — Data là MẢNG TRẦN,
    /// null nếu rỗng (nguồn :133 khởi tạo null, :159-161 gán list khi có dòng, :199-204 trả Json).
    /// Đích chưa có user: lấy OrgID từ cấu hình <c>_config["Biz_OrgID"]</c>; nếu không cấu hình (null/rỗng)
    /// thì GetXAsync bỏ nhánh lọc OrgID -> lấy mọi tổ chức (GAP: nguồn lọc theo tổ chức của user đang đăng nhập).
    /// Trả <c>Ok(new { Success = true, Data = rows.Count > 0 ? rows : null })</c>.
    /// </summary>
    [HttpPost("getallactive")]
    public async Task<IActionResult> GetAllActive()
    {
        // Nguồn: Ft_RecordStart = "0", Ft_RecordCount = "123456000" (gate :152-153).
        // Nguồn lọc: OrgID = orgUserUI.OrgId (:138), FlagActive = "1" (:139).
        // Đích chưa có user: lấy OrgID từ cấu hình Biz_OrgID (GAP: nguồn lọc theo user đăng nhập); null/rỗng -> mọi tổ chức.
        var (_, rows) = await GetXAsync(null, _config["Biz_OrgID"], "1", 0, 123456000);

        // Nguồn :199-204: Data = listMst_Department (mảng trần); rỗng ⇒ null (:133, :159-161).
        return Ok(new { Success = true, Data = rows.Count > 0 ? rows : null });
    }

    // ============================================================================================
    // GetByDepartmentCode — nguồn Mst_DepartmentController.cs:219-282 (#124).
    // Gate: nếu DepartmentCode rỗng thì KHÔNG truy vấn (:239), objMst_Department giữ null (:238) ⇒ Data = null (:269).
    // Ngược lại gán DepartmentCode (:241-244) rồi gọi strWhereClause_Mst_Department_By_DepartmentCode (:907-926)
    // (lọc `DepartmentCode = 'x'` — KHÔNG truyền OrgID/FlagActive nên hàng FlagActive '0' VẪN lấy được, OrgID không lọc).
    // Ft_RecordStart = "0", Ft_RecordCount = "1" (:256-257) ⇒ lấy đúng 1 dòng, trả Lst_Mst_Department[0] (:263-266).
    // ============================================================================================
    [HttpPost("getbydepartmentcode")]
    public async Task<IActionResult> GetByDepartmentCode([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] MstDepartmentGetByDepartmentCodeReq? req)
    {
        var departmentCode = req?.DepartmentCode;

        // DepartmentCode rỗng ⇒ nguồn không truy vấn (:239) và trả Data = null (:269).
        if (string.IsNullOrWhiteSpace(departmentCode))
        {
            return Ok(new { Success = true, Data = (object?)null });
        }

        // Lấy 1 dòng theo DepartmentCode (Ft_RecordStart = "0", Ft_RecordCount = "1" — :256-257).
        var (_, rows) = await GetXAsync(null, null, null, 0, 1, departmentCodeEqual: departmentCode);

        // Nguồn trả Lst_Mst_Department[0] khi có dòng, ngược lại null (:263-266).
        return Ok(new { Success = true, Data = rows.Count > 0 ? rows[0] : null });
    }

    // ============================================================================================
    // Create — nguồn Mst_DepartmentController.cs:286-370 (#125).
    // Gate: body rỗng -> throw ServiceException("strJson null") -> ném lỗi mặc định Mst_Department_Create.
    // Lấy sequence SeqType.DepartmentCode -> PortSeq.NextAsync<Seq_DepartmentCode>(_db),
    // GHI ĐÈ DepartmentCode mà client gửi lên (format "{0}DPMCODE.{3}.{1:00000}{2}", max 100000).
    // 1 transaction duy nhất cho CreateXAsync + UpdBUAsync, lỗi bất kỳ rollback + PortResult.Fail.
    // Thành công Data = null (:356-357).
    // ============================================================================================
    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] MstDepartmentCreateReq? req)
    {
        // 0. Body rỗng / null: gate :296-303 ném ServiceException("strJson null") -> mã mặc định Mst_Department_Create.
        // Bước này xảy ra TRƯỚC khi gọi sequence nên KHÔNG tăng seq.
        if (req == null)
        {
            return PortResult.Fail("Mst_Department_Create", "Check.strJson", null);
        }

        // Lấy seq trong transaction riêng đã commit độc lập (gate :318-337).
        var seq = await PortSeq.NextAsync<Seq_DepartmentCode>(_db);
        var departmentCode = PortSeq.Build("{0}DPMCODE.{3}.{1:00000}{2}", "", seq, 100000, "", DateTime.UtcNow);

        // 1 transaction duy nhất cho CreateX (Master.cs:29827 _cf.db.BeginTransaction(), :29898 CommitSafety)
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var err = await CreateXAsync(
                departmentCode,
                req.OrgID,
                req.DepartmentCodeParent,
                req.MST,
                req.DepartmentName,
                req.DepartmentDesc,
                DateTime.UtcNow,
                "port"
            );

            if (err != null)
            {
                await tx.RollbackAsync();
                return err;
            }

            await tx.CommitAsync();
            return Ok(new { Success = true, Data = (object?)null });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return PortResult.Fail("Mst_Department_Create", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Nghiệp vụ tạo mới phòng ban — port 1:1 từ Mst_Department_CreateX (Master.cs:30569-30726).
    /// Tách riêng để tái sử dụng cho Import sau này.
    /// </summary>
    private async Task<IActionResult?> CreateXAsync(
        string? departmentCode,
        string? orgId,
        string? departmentCodeParent,
        string? mst,
        string? departmentName,
        string? departmentDesc,
        DateTime dtimeSys,
        string logLUBy)
    {
        // 1. Chuẩn hoá tham số: mã dùng StdParam (Trim + ToUpper), tên/mô tả dùng StdStr (Trim + Normalize)
        // Master.cs:30605-30610
        var strDepartmentCode = StdParam(departmentCode);
        var strOrgID = StdParam(orgId);
        var strDepartmentCodeParent = StdParam(departmentCodeParent);
        var strMST = StdParam(mst);
        var strDepartmentName = StdStr(departmentName);
        var strDepartmentDesc = StdStr(departmentDesc);

        // 2. OrgID và MST lấy từ user đăng nhập (Master.cs:30613-30615 drAbilityOfUser = Sys_User_GetAbilityViewOfUser).
        // OrgID và MST do client gửi lên BỊ BỎ QUA.
        // Đích chưa có user đăng nhập: OrgID_UC lấy từ cấu hình IConfiguration["Biz_OrgID"].
        // MST_UC = Mst_Org.MST của OrgID đó (Models/Port/Mst_Org.cs).
        // Không cấu hình hoặc không tìm thấy org -> null (GAP: nguồn lấy OrgID/MST từ user đăng nhập).
        var strOrgID_UC = StdParam(_config["Biz_OrgID"]);
        string? strMST_UC = null;
        if (!string.IsNullOrEmpty(strOrgID_UC))
        {
            var org = await _db.Set<Mst_Org>().AsNoTracking()
                .FirstOrDefaultAsync(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(strOrgID_UC), PortQuery.Esc));
            if (org != null)
            {
                strMST_UC = StdParam(org.MST);
            }
        }

        // 3. Kiểm tra ĐÚNG THỨ TỰ (Master.cs:30620-30689):
        // 3.a. Mã phòng ban rỗng (Master.cs:30621-30630) -> Mst_Department_Create_InvalidDepartmentCode
        if (string.IsNullOrEmpty(strDepartmentCode))
        {
            return PortResult.Fail("Mst_Department_Create_InvalidDepartmentCode", "Check.strDepartmentCode", strDepartmentCode);
        }

        // 3.b. Kiểm tra mã đã tồn tại (Master.cs:30633-30640: Mst_Department_CheckDB(code, "0"))
        var depExist = await _db.Set<Mst_Department>().AsNoTracking()
            .FirstOrDefaultAsync(x => EF.Functions.Like((x.DepartmentCode ?? "").ToLower(), PortQuery.Equal(strDepartmentCode), PortQuery.Esc));
        if (depExist != null)
        {
            return PortResult.Fail("Mst_Department_CheckDB_DepartmentExist", "Check.DepartmentCode", strDepartmentCode);
        }

        // 3.c. Mst_NNT_CheckDB(MST_UC, "1", "1", "") (Master.cs:30643-30650, :7813-7890)
        // Tra cứu theo t.MST = @objMST
        Mst_NNT? nntDb = null;
        if (!string.IsNullOrEmpty(strMST_UC))
        {
            nntDb = await _db.Set<Mst_NNT>().AsNoTracking()
                .FirstOrDefaultAsync(x => EF.Functions.Like((x.MST ?? "").ToLower(), PortQuery.Equal(strMST_UC), PortQuery.Esc));
        }

        if (nntDb == null)
        {
            return PortResult.Fail("Mst_NNT_CheckDB_NNTNotFound", "Check.MST", strMST_UC);
        }

        if (!"1".Contains(nntDb.FlagActive ?? ""))
        {
            return PortResult.Fail("Mst_NNT_CheckDB_FlagActiveNotMatched",
                "Check.MST", strMST_UC,
                "Check.strFlagActiveListToCheck", "1",
                "DB.FlagActive", nntDb.FlagActive);
        }

        // 3.d. DepartmentCodeParent khác rỗng -> Mst_Department_CheckDB(parent, "1", "1") (Master.cs:30654-30665, :29275-29338)
        if (!string.IsNullOrEmpty(strDepartmentCodeParent))
        {
            var parentDb = await _db.Set<Mst_Department>().AsNoTracking()
                .FirstOrDefaultAsync(x => EF.Functions.Like((x.DepartmentCode ?? "").ToLower(), PortQuery.Equal(strDepartmentCodeParent), PortQuery.Esc));

            if (parentDb == null)
            {
                return PortResult.Fail("Mst_Department_CheckDB_DepartmentNotFound", "Check.DepartmentCode", strDepartmentCodeParent);
            }

            if (!"1".Contains(parentDb.FlagActive ?? ""))
            {
                return PortResult.Fail("Mst_Department_CheckDB_FlagActiveNotMatched",
                    "Check.DepartmentCode", strDepartmentCodeParent,
                    "Check.strFlagActiveListToCheck", "1",
                    "DB.FlagActive", parentDb.FlagActive);
            }
        }

        // 3.e. Tên phòng ban rỗng (Master.cs:30667-30676)
        if (string.IsNullOrEmpty(strDepartmentName))
        {
            return PortResult.Fail("Mst_Department_Create_InvalidDepartmentName", "Check.strDepartmentName", strDepartmentName);
        }

        // 3.f. Mst_Department_CheckDepartmentName(OrgID_UC, DepartmentName, "0") (Master.cs:30679-30686, :29341-29411)
        // SQL nguồn: OrgID = @objOrgID AND DepartmentName LIKE @objDepartmentName
        // LIKE không tự thêm wildcard, ký tự %/_ trong tên người dùng vẫn là wildcard, không phân biệt hoa thường.
        // Để giữ nguyên %/_ của người dùng trên SQLite: chỉ escape ký tự '\', không escape '%' và '_' (khác PortQuery.Equal).
        var namePattern = (strDepartmentName ?? "").ToLowerInvariant().Replace("\\", "\\\\");
        var orgPattern = PortQuery.Equal(strOrgID_UC ?? "");

        var nameExist = await _db.Set<Mst_Department>().AsNoTracking()
            .FirstOrDefaultAsync(x =>
                EF.Functions.Like((x.OrgID ?? "").ToLower(), orgPattern, PortQuery.Esc) &&
                EF.Functions.Like((x.DepartmentName ?? "").ToLower(), namePattern, PortQuery.Esc));

        if (nameExist != null)
        {
            return PortResult.Fail("Mst_Department_CheckDB_DepartmentNameExist",
                "Check.OrgID", strOrgID_UC,
                "Check.DepartmentName", strDepartmentName);
        }

        // 4. Lưu DB Mst_Department (Master.cs:30695-30717)
        var networkId = _config["Biz_NetworkID"] ?? "1001";
        var newDept = new Mst_Department
        {
            DepartmentCode = strDepartmentCode,
            NetworkID = networkId,
            OrgID = strOrgID_UC,
            DepartmentCodeParent = strDepartmentCodeParent,
            DepartmentBUCode = "X",
            DepartmentBUPattern = "X",
            DepartmentLevel = 1,
            MST = strMST_UC,
            DepartmentName = strDepartmentName,
            DepartmentDesc = strDepartmentDesc,
            FlagActive = "1",
            LogLUDTimeUTC = dtimeSys,
            LogLUBy = logLUBy
        };

        _db.Set<Mst_Department>().Add(newDept);
        await _db.SaveChangesAsync();

        // 5. Post Save: UpdBUAsync (Master.cs:30723, :29413-29448)
        await UpdBUAsync();

        return null;
    }

    // ============================================================================================
    // Update — nguồn Mst_DepartmentController.cs:374-443 (#126).
    // Gate: body rỗng -> throw Exception("strJson null") -> lỗi mặc định Mst_Department_Update.
    // Ft_Cols_Upd CỐ ĐỊNH từ gate (:404-410): DepartmentCodeParent, DepartmentName, DepartmentDesc, FlagActive (MST bị comment).
    // Nghiệp vụ Mst_Department_UpdateX (Master.cs:30727-30864):
    // 1. StdParam: DepartmentCode, DepartmentCodeParent, OrgID, MST; StdStr: DepartmentName, DepartmentDesc; FlagActive: StdFlag (thiếu -> '0').
    //    Cờ cập nhật: bUpd_MST (false do MST bị comment ở gate), bUpd_DepartmentName, bUpd_DepartmentDesc, bUpd_FlagActive.
    //    ĐẶC BIỆT: KHÔNG có cờ hay lệnh gán cho DepartmentCodeParent -> phòng ban cha KHÔNG BAO GIỜ đổi dù gate có gửi.
    // 2. Kiểm tra ĐÚNG THỨ TỰ:
    //    (a) Mst_Department_CheckDB(ma, "1") -> Mst_Department_CheckDB_DepartmentNotFound.
    //        (Mst_NNT check :30801-30810 bị comment ở nguồn nên không chạy).
    //    (b) Tên rỗng sau StdStr -> Mst_Department_UpdateX_InvalidDepartmentName.
    //    (c) CHỈ KHI tên mới KHÁC tên DB (so sánh chính xác string.Equals(..., StringComparison.Ordinal)):
    //        -> Mst_Department_CheckDepartmentName(OrgID CỦA DÒNG DB, tên, "0") (LIKE không phân biệt hoa thường, %/_ là wildcard)
    //        -> trùng tên trong cùng OrgID -> Mst_Department_CheckDB_DepartmentNameExist.
    //        HỆ QUẢ: đổi tên chỉ khác chữ hoa/thường (vd KD "Kinh doanh 2" -> "KINH DOANH 2") sẽ vấp phải chính nó -> lỗi.
    // 3. Lưu DB (:30848-30861): DepartmentName, DepartmentDesc, FlagActive, LogLUDTimeUTC = now, LogLUBy = "port".
    //    KHÔNG gọi UpdBU. Mã, cha, OrgID, MST, BUCode, BUPattern, Level KHÔNG đổi.
    // 1 transaction duy nhất, lỗi -> rollback + PortResult.Fail. Thành công Data = null (:428-429).
    // ============================================================================================
    [HttpPost("update")]
    public async Task<IActionResult> Update([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] MstDepartmentUpdateReq? req)
    {
        // 0. Body rỗng / null: gate :388-391 ném Exception("strJson null") -> mã mặc định Mst_Department_Update.
        if (req == null)
        {
            return PortResult.Fail("Mst_Department_Update", "Check.strJson", null);
        }

        // 1 transaction duy nhất cho Update (Master.cs:30069 _cf.db.BeginTransaction(), :30143 CommitSafety, :30151 RollbackSafety)
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var err = await UpdateXAsync(req);
            if (err != null)
            {
                await tx.RollbackAsync();
                return err;
            }

            await tx.CommitAsync();
            return Ok(new { Success = true, Data = (object?)null });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return PortResult.Fail("Mst_Department_Update", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Nghiệp vụ cập nhật phòng ban — port 1:1 từ Mst_Department_UpdateX (Master.cs:30727-30864).
    /// </summary>
    private async Task<IActionResult?> UpdateXAsync(MstDepartmentUpdateReq req)
    {
        // 1. Chuỗi Ft_Cols_Upd cố định từ gate Mst_DepartmentController.cs:404-410
        // Gate: DepartmentCodeParent, DepartmentName, DepartmentDesc, FlagActive (MST bị comment)
        var strFt_Cols_Upd = "Mst_Department.DepartmentCodeParent,Mst_Department.DepartmentName,Mst_Department.DepartmentDesc,Mst_Department.FlagActive,";
        strFt_Cols_Upd = StdParam(strFt_Cols_Upd) ?? "";

        // So khớp các cột được phép cập nhật: Master.cs:30780-30784
        bool bUpd_MST = strFt_Cols_Upd.Contains("Mst_Department.MST".ToUpperInvariant());
        bool bUpd_DepartmentName = strFt_Cols_Upd.Contains("Mst_Department.DepartmentName".ToUpperInvariant());
        bool bUpd_DepartmentDesc = strFt_Cols_Upd.Contains("Mst_Department.DepartmentDesc".ToUpperInvariant());
        bool bUpd_FlagActive = strFt_Cols_Upd.Contains("Mst_Department.FlagActive".ToUpperInvariant());

        // 2. Chuẩn hoá tham số input (Master.cs:30772-30778)
        var strDepartmentCode = StdParam(req.DepartmentCode);
        var strDepartmentCodeParent = StdParam(req.DepartmentCodeParent);
        var strOrgID = StdParam(req.OrgID);
        var strMST = StdParam(req.MST);
        var strDepartmentName = StdStr(req.DepartmentName);
        var strDepartmentDesc = StdStr(req.DepartmentDesc);
        var strFlagActive = StdFlag(req.FlagActive);

        // 3. Kiểm tra ĐÚNG THỨ TỰ (Master.cs:30786-30839):
        // 3.a. Mst_Department_CheckDB(ma, "1") (Master.cs:30790-30799, :29275-29338)
        if (string.IsNullOrEmpty(strDepartmentCode))
        {
            return PortResult.Fail("Mst_Department_CheckDB_DepartmentNotFound", "Check.DepartmentCode", strDepartmentCode);
        }

        var dep = await _db.Set<Mst_Department>().FirstOrDefaultAsync(x =>
            EF.Functions.Like((x.DepartmentCode ?? "").ToLower(), PortQuery.Equal(strDepartmentCode), PortQuery.Esc));

        if (dep == null)
        {
            return PortResult.Fail("Mst_Department_CheckDB_DepartmentNotFound", "Check.DepartmentCode", strDepartmentCode);
        }

        var strDepartmentName_DB = StdStr(dep.DepartmentName);
        var strOrgID_DB = StdParam(dep.OrgID);

        // (Mst_NNT check :30801-30810 bị comment trong nguồn)

        // 3.b. Tên phòng ban rỗng (Master.cs:30813-30823) -> Mst_Department_UpdateX_InvalidDepartmentName
        if (bUpd_DepartmentName && string.IsNullOrEmpty(strDepartmentName))
        {
            return PortResult.Fail("Mst_Department_UpdateX_InvalidDepartmentName", "Check.strDepartmentName", strDepartmentName);
        }

        // 3.c. Kiểm tra trùng tên: CHỈ KHI tên mới KHÁC tên DB (so sánh chính xác, phân biệt hoa thường)
        // Master.cs:30825-30838 (!CmUtils.StringUtils.StringEqual(strDepartmentName, strDepartmentName_DB))
        // So sánh phân biệt hoa thường bằng StringComparison.Ordinal.
        // Mst_Department_CheckDepartmentName(OrgID CỦA DÒNG DB, tên, "0")
        // LIKE không phân biệt hoa thường, %/_ là wildcard.
        // HỆ QUẢ: đổi tên chỉ khác chữ hoa/thường (vd KD "Kinh doanh 2" -> "KINH DOANH 2") sẽ vấp phải chính nó -> báo lỗi tồn tại.
        if (bUpd_DepartmentName)
        {
            if (!string.Equals(strDepartmentName, strDepartmentName_DB, StringComparison.Ordinal))
            {
                var namePattern = (strDepartmentName ?? "").ToLowerInvariant().Replace("\\", "\\\\");
                var orgPattern = PortQuery.Equal(strOrgID_DB ?? "");

                var nameExist = await _db.Set<Mst_Department>().AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        EF.Functions.Like((x.OrgID ?? "").ToLower(), orgPattern, PortQuery.Esc) &&
                        EF.Functions.Like((x.DepartmentName ?? "").ToLower(), namePattern, PortQuery.Esc));

                if (nameExist != null)
                {
                    return PortResult.Fail("Mst_Department_CheckDB_DepartmentNameExist",
                        "Check.OrgID", strOrgID_DB,
                        "Check.DepartmentName", strDepartmentName);
                }
            }
        }

        // 4. Lưu DB Mst_Department (Master.cs:30843-30861)
        // LƯU Ý: KHÔNG có cờ cập nhật DepartmentCodeParent -> cha KHÔNG BAO GIỜ đổi dù gate có gửi DepartmentCodeParent.
        // KHÔNG gọi UpdBU. Mã, cha, OrgID, MST, BUCode, BUPattern, Level giữ nguyên.
        if (bUpd_MST) dep.MST = strMST;
        if (bUpd_DepartmentName) dep.DepartmentName = strDepartmentName;
        if (bUpd_DepartmentDesc) dep.DepartmentDesc = strDepartmentDesc;
        if (bUpd_FlagActive) dep.FlagActive = strFlagActive;
        dep.LogLUDTimeUTC = DateTime.UtcNow;
        dep.LogLUBy = "port"; // GAP: nguồn lấy từ strWAUserCode

        await _db.SaveChangesAsync();

        return null; // Thành công
    }

    // ============================================================================================
    // Delete — nguồn Mst_DepartmentController.cs:446-515 (#127).
    // Gate:
    //   - DepartmentCode rỗng / body rỗng -> throw ServiceException("invalid_departmentcode", "invalid_departmentcode") (:500)
    //     -> 400 ErridnLoyalty.invalid_departmentcode.
    //   - OrgID rỗng -> throw ServiceException("invalid_orgid", "invalid_orgid") (:495, kiểm SAU DepartmentCode)
    //     -> 400 ErridnLoyalty.invalid_orgid.
    //   - Cả hai hợp lệ -> gọi WA_Mst_Department_Delete (:488), thành công SetSuccess, Data = null (:490-491).
    //   - OrgID KHÔNG dùng trong nghiệp vụ biz sau đó.
    // Biz Mst_Department_DeleteX (Master.cs:30865-30968), tách thành DeleteXAsync(code) (1 transaction):
    //   - 1 transaction duy nhất (Master.cs:30292 _cf.db.BeginTransaction(), :30358 CommitSafety).
    //   - StdParam mã phòng ban (Trim + ToUpper, rỗng -> null) (:30891).
    //   - Mst_Department_CheckDB(ma, "1"): mã rỗng hoặc không tìm thấy -> ErridnLoyalty.Mst_Department_CheckDB_DepartmentNotFound (:30897-30903).
    //   - Đếm nhân viên trong Staff_MapDepartment (:30909-30953): join DepartmentCode + OrgID > 0 ->
    //     Mst_Department_DeleteX_ExistStaff_MapDepartment — bảng CHƯA có ở đích -> KHÔNG chặn, GHI GAP.
    //   - FK Train_CourseDepartment.DepartmentCode (20220219.z11.UpdDB.60.Course.sql:177) chưa port -> GHI GAP.
    //   - KHÔNG kiểm tra phòng ban con: các phòng ban cấp dưới vẫn giữ DepartmentCodeParent trỏ tới mã đã xoá (hành vi nguồn).
    //   - KHÔNG gọi UpdBU: nguồn không gọi Mst_Department_UpdBU khi xoá dòng (Master.cs:30865-30968).
    //   - Xoá dòng Mst_Department và commit transaction.
    // ============================================================================================
    [HttpPost("delete")]
    public async Task<IActionResult> Delete([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] MstDepartmentDeleteReq? req)
    {
        // 1. Kiểm tra DepartmentCode rỗng tại gate (Mst_DepartmentController.cs:465, :500):
        //    DepartmentCode rỗng / body rỗng -> ServiceException("invalid_departmentcode", "invalid_departmentcode")
        if (string.IsNullOrWhiteSpace(req?.DepartmentCode))
        {
            return PortResult.Fail("invalid_departmentcode", "Check.DepartmentCode", req?.DepartmentCode);
        }

        // 2. Kiểm tra OrgID rỗng tại gate (Mst_DepartmentController.cs:467, :495):
        //    OrgID rỗng -> ServiceException("invalid_orgid", "invalid_orgid") (kiểm SAU DepartmentCode).
        //    LƯU Ý: OrgID không dùng trong nghiệp vụ biz sau đó.
        if (string.IsNullOrWhiteSpace(req?.OrgID))
        {
            return PortResult.Fail("invalid_orgid", "Check.OrgID", req?.OrgID);
        }

        // 3. Thực hiện nghiệp vụ xoá qua DeleteXAsync (Master.cs:30259-30395, :30865-30968)
        var err = await DeleteXAsync(req?.DepartmentCode);
        if (err != null)
        {
            return err;
        }

        // 4. Thành công: gate Mst_DepartmentController.cs:490-491 SetSuccess + Data = null
        return Ok(new
        {
            Success = true,
            Data = (object?)null
        });
    }

    /// <summary>
    /// Port 1:1 DeleteMultiple action từ gate Mst_DepartmentController.cs:516-580 (#128).
    /// Chuỗi gọi: gate :516-580 -> lặp qua danh sách Lst_Mst_Department, mỗi phần tử gọi
    /// WA_Mst_Department_Delete (:547-564) -> biz WAS_Mst_Department_Delete -> Mst_Department_Delete -> Mst_Department_DeleteX.
    ///
    /// Request JSON: mảng [{ DepartmentCode }].
    /// 1. Kiểm tra body rỗng / null: Gate Mst_DepartmentController.cs:526-533
    ///    if (!string.IsNullOrEmpty(strJson)) { ... } else { throw new ServiceException("strJson null"); }
    ///    Khi body null hoặc rỗng -> trả về 400 ErridnLoyalty.Mst_Department_Delete với Check.strJson = null.
    /// 2. Danh sách rỗng `[]`: vòng lặp foreach không chạy, trả về Ok(Success = true, Data = null).
    /// 3. Lặp tuần tự qua từng phần tử trong danh sách (:547-564):
    ///    Mỗi lần lặp là một lời gọi độc lập tới chuỗi nghiệp vụ Delete, sử dụng TRANSACTION RIÊNG
    ///    (Master.cs:30292 _cf.db.BeginTransaction() và :30358 CommitSafety trong DeleteXAsync).
    ///    - KHÔNG có bước kiểm tra DepartmentCode rỗng và KHÔNG kiểm OrgID ở tầng gate như Delete đơn (:465, :467);
    ///      OrgID request nguồn lấy từ org của user (orgUserUI.OrgId), không lấy từ phần tử.
    ///      Khi DepartmentCode rỗng, lỗi phát sinh ở tầng nghiệp vụ Mst_Department_CheckDB
    ///      -> trả về 400 ErridnLoyalty.Mst_Department_CheckDB_DepartmentNotFound.
    ///    - Hành vi "xóa một phần" (nguồn Mst_DepartmentController.cs:547-573):
    ///      Nếu phần tử thứ k gặp lỗi, exception được ném ra và thoát khỏi vòng lặp ngay lập tức
    ///      nhảy vào khối catch (ex) (:569-573).
    ///      Do mỗi phần tử chạy trên một transaction riêng đã được commit, các phần tử TRƯỚC k
    ///      ĐÃ BỊ XÓA VÀ KHÔNG BỊ ROLLBACK; các phần tử SAU k hoàn toàn không được xử lý.
    ///      "xóa một phần" là hành vi nguồn.
    /// 4. Thành công: gate :566-567 trả objJsonResultEntry.Data = null -> Ok(new { Success = true, Data = (object?)null }).
    ///
    /// Bỏ qua có chủ đích (loại Đ):
    /// - CheckGatewayAuthentication: gate Mst_DepartmentController.cs:536
    /// - CheckAuthorize: gate Mst_DepartmentController.cs:537 (hệ thống đích chưa có hệ phân quyền)
    /// - Sys_User_CheckAuthentication, Sys_Access_CheckDenyV30
    /// - Ghi RequestLog/ReturnLog: Master.cs:30295-30302, 30385-30392
    /// </summary>
    [HttpPost("deletemultiple")]
    public async Task<IActionResult> DeleteMultiple([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] List<MstDepartmentDeleteReq>? req)
    {
        // 1. Kiểm tra body rỗng / null: Gate Mst_DepartmentController.cs:526-533
        // if (!string.IsNullOrEmpty(strJson)) { ... } else { throw new ServiceException("strJson null"); }
        if (req == null)
        {
            return PortResult.Fail("Mst_Department_Delete", "Check.strJson", null);
        }

        // 2. Lặp tuần tự qua từng phần tử (gate Mst_DepartmentController.cs:547-564).
        // Mỗi phần tử chạy trong 1 transaction riêng biệt (hàm DeleteXAsync).
        // Nếu phần tử thứ k gặp lỗi -> return ngay lỗi đó (hành vi nguồn :547-573:
        // "xóa một phần" là hành vi nguồn: các phần tử trước k đã commit thành công
        // vào DB và KHÔNG bị rollback; các phần tử sau k không được xử lý).
        foreach (var item in req)
        {
            var err = await DeleteXAsync(item?.DepartmentCode);
            if (err != null)
            {
                return err;
            }
        }

        // 3. Thành công: gate Mst_DepartmentController.cs:566-567
        return Ok(new
        {
            Success = true,
            Data = (object?)null
        });
    }

    // ============================================================================================
    // Import — nguồn gate Mst_DepartmentController.cs:584-730 (#129) -> API WA_Mst_Department_Create
    // (MstDepartmentController.cs:91-160) -> biz WAS_Mst_Department_Create (Master.cs:29702-29785)
    // -> Mst_Department_Create (Master.cs:29786-29935) -> Mst_Department_CreateX (Master.cs:30569-30726).
    //
    // GIẢ ĐỊNH (kế thừa từ Data/PortExcel.cs):
    //   - DLL ExcelImportNew của nguồn không có mã ⇒ giả định định dạng file Excel:
    //     dòng 1 = tiêu đề (đúng theo GetImportDicColumsTemplate sinh ra), dữ liệu bắt đầu từ ô A2 (firstDataRow = 2).
    //   - Cột ánh xạ theo vị trí: cột 1 = DepartmentName, cột 2 = DepartmentCodeParent, cột 3 = DepartmentDesc
    //     (GetImportDicColumsTemplate có 3 cột: DepartmentName -> "Tên phòng ban (*)", DepartmentCodeParent -> "Phòng ban cấp trên",
    //      DepartmentDesc -> "Mô tả"; cột DepartmentCode bị comment ở nguồn :868 nên không có trong file).
    //
    // Quy trình và thứ tự kiểm tra:
    //   1. Kiểm tra file upload: gate Mst_DepartmentController.cs:606-621
    //      - Thiếu file hoặc file rỗng hoặc đuôi file != ".xlsx" -> Fail("Mst_Department_Import", "Message", "File excel import không hợp lệ")
    //   2. Đọc file bằng PortExcel.ReadSheet(stream, new[] { "DepartmentName", "DepartmentCodeParent", "DepartmentDesc" }):
    //      - Bảng không có dòng dữ liệu (rows == null || rows.Count == 0) -> Fail("Mst_Department_Import", "Message", "File excel import không có dữ liệu") (:625, :713-716)
    //      - Số cột tiêu đề != GetImportDicColumsTemplate().Count (3 cột) -> Fail("Mst_Department_Import", "Message", "File excel import không hợp lệ") (:627-631)
    //   3. Kiểm tra TẤT CẢ các dòng TRƯỚC khi tạo (#region["Check null"] :635-660):
    //      - DepartmentName rỗng / khoảng trắng -> Fail("Mst_Department_Import", "Message", "Tên bộ phận không được trống") (:642-646)
    //        (nguồn ném ServiceException("Tên bộ phận không được trống") — GIỮ NGUYÊN VĂN).
    //      - Các kiểm tra Code, Parent, MST, trùng lặp bị comment ở nguồn (:637-651, :664-679) -> không port.
    //      Nếu có bất kỳ dòng nào vi phạm, dừng ngay và KHÔNG có dòng nào được tạo vào CSDL.
    //   4. Lưu từng dòng tuần tự (hành vi "import một phần"):
    //      - Mỗi dòng gán item.DepartmentCode = Guid.NewGuid().ToString() (:689)
    //        (KHÁC Create đơn: Create đơn lấy seq DPMCODE..., Import KHÔNG qua seq mà sinh Guid mới;
    //         qua StdParam trong CreateXAsync mã trở thành GUID IN HOA 36 ký tự).
    //      - Gọi CreateXAsync trong 1 transaction riêng cho mỗi dòng (gate :687-708).
    //      - OrgID và MST client gửi ("ALL") bị bỏ qua, thực tế lấy từ cấu hình Biz_OrgID giống Create đơn #125.
    //      - CreateXAsync thực hiện kiểm tra DB, lưu dòng Mst_Department và gọi UpdBUAsync cập nhật cấu trúc BU toàn bảng.
    //      - Nếu dòng thứ k gặp lỗi, transaction dòng k bị rollback, vòng lặp dừng ngay lập tức, trả về đúng lỗi của CreateXAsync cho dòng k.
    //      - CÁC DÒNG TRƯỚC k ĐÃ ĐƯỢC COMMIT VÀO DB (không rollback các dòng đã tạo trước đó — "import một phần").
    //      - Các dòng sau k không được duyệt.
    //   5. Thành công: trả Ok({ Success = true, Data = null }) (:710-711).
    //
    // Bỏ qua có chủ đích (loại Đ):
    //   - CheckGatewayAuthentication, CheckAuthorize (:595-596)
    //   - Nguồn lưu file tạm xuống đĩa (FileUtils.SaveTempFile :616) -> đọc trực tiếp từ stream qua ClosedXML
    //   - Session người dùng orgUserUI (:594, :690-692, :698-702).
    // ============================================================================================
    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Import(IFormFile? file)
    {
        // 1. Kiểm tra file upload: gate Mst_DepartmentController.cs:606-621
        if (file == null || file.Length == 0)
        {
            return PortResult.Fail("Mst_Department_Import", "Message", "File excel import không hợp lệ");
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".xlsx")
        {
            return PortResult.Fail("Mst_Department_Import", "Message", "File excel import không hợp lệ");
        }

        // 2. Đọc bảng tính bằng PortExcel.ReadSheet (giả định định dạng file Excel từ PortExcel)
        int columnCount;
        List<Dictionary<string, string?>> rows;
        try
        {
            using var stream = file.OpenReadStream();
            (columnCount, rows) = PortExcel.ReadSheet(stream, new[] { "DepartmentName", "DepartmentCodeParent", "DepartmentDesc" });
        }
        catch
        {
            return PortResult.Fail("Mst_Department_Import", "Message", "File excel import không hợp lệ");
        }

        // Kiểm tra có dữ liệu hay không: gate Mst_DepartmentController.cs:625, :713-716 (table == null || table.Rows.Count == 0)
        if (rows.Count == 0)
        {
            return PortResult.Fail("Mst_Department_Import", "Message", "File excel import không có dữ liệu");
        }

        // Kiểm tra số cột dòng tiêu đề so với GetImportDicColumsTemplate().Count (3 cột): gate Mst_DepartmentController.cs:627-631
        if (columnCount != GetImportDicColumsTemplate().Count)
        {
            return PortResult.Fail("Mst_Department_Import", "Message", "File excel import không hợp lệ");
        }

        // 3. Kiểm tra TẤT CẢ các dòng TRƯỚC khi tạo (Check null: gate Mst_DepartmentController.cs:635-660)
        foreach (var r in rows)
        {
            var departmentName = r.GetValueOrDefault("DepartmentName");
            if (string.IsNullOrWhiteSpace(departmentName))
            {
                return PortResult.Fail("Mst_Department_Import", "Message", "Tên bộ phận không được trống");
            }
        }

        // 4. Lưu từng dòng tuần tự: mỗi dòng 1 transaction riêng (hành vi "import một phần": gate Mst_DepartmentController.cs:687-708)
        foreach (var r in rows)
        {
            var name = r.GetValueOrDefault("DepartmentName");
            var parent = r.GetValueOrDefault("DepartmentCodeParent");
            var desc = r.GetValueOrDefault("DepartmentDesc");

            // Nguồn :689 item.DepartmentCode = Guid.NewGuid().ToString() (khác Create đơn dùng seq DPMCODE...)
            // Mã Guid đi qua StdParam trong CreateXAsync -> IN HOA 36 ký tự.
            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var err = await CreateXAsync(
                    Guid.NewGuid().ToString(),
                    null, // OrgID client gửi bị bỏ qua, lấy từ Biz_OrgID giống Create đơn #125
                    parent,
                    "ALL", // MST truyền "ALL", nhưng CreateXAsync lấy theo Biz_OrgID
                    name,
                    desc,
                    DateTime.UtcNow,
                    "port"
                );

                if (err != null)
                {
                    // Lỗi ở dòng k -> thoát ngay, rollback dòng k, trả đúng lỗi của CreateXAsync, các dòng trước k đã tạo thành công
                    await tx.RollbackAsync();
                    return err;
                }

                await tx.CommitAsync();
            }
            catch (Exception ex)
            {
                // Nguồn (gate :719-723) bắt MỌI ngoại lệ và trả Success=false; không để văng ra thành HTTP 500.
                // Các dòng trước dòng này đã commit (import một phần) — giữ đúng hành vi nguồn. Cùng mẫu với Create.
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_Department_Import", "Exception", ex.Message);
            }
        }

        // Thành công: gate Mst_DepartmentController.cs:710-711
        return Ok(new
        {
            Success = true,
            Data = (object?)null
        });
    }

    /// <summary>
    /// Port 1:1 Export action từ gate Mst_DepartmentController.cs:735-812 (#130).
    /// Chuỗi gọi: gate Mst_DepartmentController.cs:735-812 (Export)
    ///   -> Mst_DepartmentService.Instance.WA_Mst_Department_Get (:781)
    ///   -> API MstDepartmentController.cs:20-89 (WA_Mst_Department_Get)
    ///   -> biz WAS_Mst_Department_Get (Master.cs:29450-29548)
    ///   -> biz Mst_Department_Get (Master.cs:29549-29701, luôn Rollback :29664, KHÔNG ghi DB)
    ///   -> biz Mst_Department_GetX (Master.cs:30396-30568): sắp xếp order by md.DepartmentCode asc (:30457), select cuối order by t.MyIdxSeq asc (:30514).
    ///
    /// Tham số từ body { OrgID, KeyWord, FlagActive } (EmptyBodyBehavior.Allow, gate :735).
    /// strWhereClause_Mst_Department (:756-761, :877-905):
    ///   - KeyWord -> like '%kw%' trên DepartmentName (:882-885).
    ///   - OrgID -> OrgID = 'x' (:886-889) — CÓ lọc, OrgID lọc theo tham số client.
    ///   - FlagActive -> IN ('a','b') (:894-902): tách dấu phẩy (FlagActive IN).
    /// Ft_RecordStart = "0" (:774), Ft_RecordCount = "123456000" (:775), Rt_Cols_Mst_Department = "*" (:778).
    ///
    /// Từ điển cột xuất Excel: dùng từ điển Import cho Export là của nguồn (gate :786, :851-862, GetImportDicColums) — đúng 4 cột theo thứ tự:
    ///   1. DepartmentName: "Tên phòng ban (*)"
    ///   2. DepartmentCodeParent: "Phòng ban cấp trên"
    ///   3. DepartmentDesc: "Mô tả"
    ///   4. FlagActive: "Trạng thái"
    /// File: GenExcelExportFilePath("Mst_Department") (:788), sheet "Mst_Department" (:789).
    /// Danh sách rỗng (rows null hoặc Count == 0, :782) -> throw ServiceException("Không có dữ liệu") (:798)
    ///   -> catch trả về 400 ErridnLoyalty.Mst_Department_Export với Params Message = "Không có dữ liệu" (:801-805).
    /// Thành công: trả Ok({ Success = true, Data = { Url = PortExcel.GetRequestBaseAddress(Request) + virtualPath } }) (:790-793).
    /// KHÔNG transaction, KHÔNG ghi DB (chỉ đọc).
    ///
    /// Bỏ qua có chủ đích (loại Đ):
    /// - CheckGatewayAuthentication / CheckAuthorize: gate :745-746, biz :29606-29634 (đích chưa có phân quyền).
    /// - Cột md_DepartmentNameParent, ss_QtyStaff có trong list nguồn nhưng không nằm trong từ điển -> không lên file.
    /// - Cột "Phòng ban cấp trên" ghi giá trị property DepartmentCodeParent (mã, không phải tên) — suy từ key của từ điển (DLL nguồn không có mã).
    /// </summary>
    [HttpPost("export")]
    public async Task<IActionResult> Export([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] MstDepartmentExportReq? req)
    {
        req ??= new MstDepartmentExportReq();

        // Gọi GetXAsync(keyWord, orgIdEqual, flagActiveCsv, recordStart, recordCount):
        // KeyWord lọc trên DepartmentName (like %kw%), OrgID lọc theo tham số client (=), FlagActive IN tách dấu phẩy.
        // RecordStart = 0, RecordCount = 123456000 (gate Mst_DepartmentController.cs:774-775).
        var (_, rows) = await GetXAsync(
            keyWord: req.KeyWord,
            orgIdEqual: req.OrgID,
            flagActiveCsv: req.FlagActive,
            recordStart: 0,
            recordCount: 123456000);

        // Nguồn Mst_DepartmentController.cs:782, 796-798: nếu danh sách null hoặc Count == 0 -> throw ServiceException("Không có dữ liệu")
        if (rows == null || rows.Count == 0)
        {
            return PortResult.Fail("Mst_Department_Export", "Message", "Không có dữ liệu");
        }

        // Tạo thư mục và đường dẫn file Excel xuất: GenExcelExportFilePath(prefix) (gate :788)
        var webRoot = _env.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
        {
            webRoot = Path.Combine(_env.ContentRootPath, "wwwroot");
        }

        // Dùng từ điển GetImportDicColums() (gate :786, :851-862) — dùng từ điển Import cho Export là của nguồn
        var dicColNames = GetImportDicColums();
        string virtualPath;
        try
        {
            string physicalPath;
            (physicalPath, virtualPath) = PortExcel.GenExcelExportFilePath(webRoot, "Mst_Department");

            // Ghi danh sách ra file Excel với tên sheet "Mst_Department" (gate :789)
            PortExcel.ExportToExcelFromList(rows, dicColNames, physicalPath, "Mst_Department");
        }
        catch (Exception ex)
        {
            // Nguồn (gate :801-805) bắt mọi ngoại lệ (kể cả lỗi ghi file) và trả Success=false, không để thành HTTP 500.
            return PortResult.Fail("Mst_Department_Export", "Exception", ex.Message);
        }

        // Trả URL tải file: Data = { Url = GetRequestBaseAddress() + url } (gate :791-793)
        return Ok(new
        {
            Success = true,
            Data = new
            {
                Url = PortExcel.GetRequestBaseAddress(Request) + virtualPath
            }
        });
    }

    /// <summary>
    /// Từ điển cột xuất Excel: dùng GetImportDicColums() từ gate Mst_DepartmentController.cs:851-862.
    /// Chú ý: dùng từ điển Import cho Export là của nguồn (gate :786).
    /// Đúng 4 cột theo đúng thứ tự:
    ///   1. DepartmentName: "Tên phòng ban (*)"
    ///   2. DepartmentCodeParent: "Phòng ban cấp trên"
    ///   3. DepartmentDesc: "Mô tả"
    ///   4. FlagActive: "Trạng thái"
    /// (DepartmentCode :855 và MST :859 bị comment ở nguồn nên không xuất).
    /// </summary>
    private static Dictionary<string, string> GetImportDicColums()
    {
        return new Dictionary<string, string>
        {
            { "DepartmentName", "Tên phòng ban (*)" },
            { "DepartmentCodeParent", "Phòng ban cấp trên" },
            { "DepartmentDesc", "Mô tả" },
            { "FlagActive", "Trạng thái" }
        };
    }

    /// <summary>
    /// Từ điển cột mẫu Import Excel: gate Mst_DepartmentController.cs:864-873 (GetImportDicColumsTemplate).
    /// Giữ đúng thứ tự 3 cột từ nguồn:
    /// 1. DepartmentName ("Tên phòng ban (*)")
    /// 2. DepartmentCodeParent ("Phòng ban cấp trên")
    /// 3. DepartmentDesc ("Mô tả")
    /// Cột DepartmentCode bị comment ở nguồn (:868) nên không có trong file mẫu.
    /// </summary>
    private static Dictionary<string, string> GetImportDicColumsTemplate()
    {
        return new Dictionary<string, string>
        {
            { "DepartmentName", "Tên phòng ban (*)" },
            { "DepartmentCodeParent", "Phòng ban cấp trên" },
            { "DepartmentDesc", "Mô tả" }
        };
    }

    /// <summary>
    /// Nghiệp vụ xoá 1 phòng ban — port 1:1 từ Mst_Department_DeleteX (Master.cs:30865-30968).
    /// Chuỗi gọi: API WA_Mst_Department_Delete (MstDepartmentController.cs:233-302)
    /// -> biz WAS_Mst_Department_Delete (Master.cs:30181-30258)
    /// -> Mst_Department_Delete (Master.cs:30259-30395)
    /// -> Mst_Department_DeleteX (Master.cs:30865-30968).
    ///
    /// Tách riêng DeleteXAsync(code) trong 1 transaction để DeleteMultiple sau này tái sử dụng.
    /// Thứ tự xử lý:
    ///   1. Mở 1 transaction cho mỗi lần xoá (Master.cs:30292 _cf.db.BeginTransaction()).
    ///   2. Chuẩn hoá mã qua StdParam: Trim + ToUpper, rỗng -> null (Master.cs:30891).
    ///   3. Mst_Department_CheckDB(ma, "1") (Master.cs:30897-30903, :29275-29338):
    ///      Mã rỗng hoặc không tìm thấy phòng ban trong DB -> ErridnLoyalty.Mst_Department_CheckDB_DepartmentNotFound.
    ///   4. Ràng buộc:
    ///      - GAP Staff_MapDepartment: nguồn kiểm tra nhân viên thuộc phòng ban (Master.cs:30909-30953).
    ///        Bảng Staff_MapDepartment chưa có ở đích -> không chặn, ghi GAP.
    ///      - GAP Train_CourseDepartment: FK Train_CourseDepartment.DepartmentCode (20220219.z11.UpdDB.60.Course.sql:177) chưa port -> ghi GAP.
    ///      - KHÔNG kiểm tra phòng ban con: các phòng ban cấp dưới vẫn giữ DepartmentCodeParent trỏ tới mã đã xoá (hành vi nguồn).
    ///      - KHÔNG gọi UpdBU: nguồn không gọi Mst_Department_UpdBU khi xoá dòng (Master.cs:30865-30968).
    ///   5. Xoá dòng Mst_Department (Master.cs:30959 dtDB_Mst_Department.Rows[0].Delete()).
    ///   6. Commit transaction (Master.cs:30358 TDALUtils.DBUtils.CommitSafety).
    /// Trả null nếu thành công; trả IActionResult (PortResult.Fail) nếu thất bại.
    /// </summary>
    private async Task<IActionResult?> DeleteXAsync(string? code)
    {
        // 1. Mở 1 transaction cho mỗi lần xoá (Master.cs:30292 _cf.db.BeginTransaction()).
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // 2. Chuẩn hoá mã qua StdParam: Trim + ToUpper, rỗng -> null (Master.cs:30891)
            var strDepartmentCode = StdParam(code);

            // 3. Mst_Department_CheckDB(ma, "1") (Master.cs:30897-30903, :29275-29338):
            //    Mã rỗng hoặc không tìm thấy -> ErridnLoyalty.Mst_Department_CheckDB_DepartmentNotFound.
            if (string.IsNullOrEmpty(strDepartmentCode))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_Department_CheckDB_DepartmentNotFound", "Check.DepartmentCode", strDepartmentCode);
            }

            var dep = await _db.Set<Mst_Department>().FirstOrDefaultAsync(x =>
                EF.Functions.Like((x.DepartmentCode ?? "").ToLower(), PortQuery.Equal(strDepartmentCode), PortQuery.Esc));

            if (dep == null)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_Department_CheckDB_DepartmentNotFound", "Check.DepartmentCode", strDepartmentCode);
            }

            // 4. Ràng buộc & nghiệp vụ nguồn:
            //    - GAP Staff_MapDepartment: nguồn kiểm tra nhân viên thuộc phòng ban (Master.cs:30909-30953).
            //      Bảng Staff_MapDepartment chưa có ở đích -> không chặn, ghi GAP.
            //    - GAP Train_CourseDepartment: FK Train_CourseDepartment.DepartmentCode (20220219.z11.UpdDB.60.Course.sql:177) chưa port -> ghi GAP.
            //    - KHÔNG kiểm tra phòng ban con: các phòng ban cấp dưới vẫn giữ DepartmentCodeParent trỏ tới mã đã xoá (hành vi nguồn).
            //    - KHÔNG gọi UpdBU: nguồn không gọi Mst_Department_UpdBU khi xoá dòng (Master.cs:30865-30968).

            // 5. Xoá dòng Mst_Department (Master.cs:30959 dtDB_Mst_Department.Rows[0].Delete())
            _db.Set<Mst_Department>().Remove(dep);
            await _db.SaveChangesAsync();

            // 6. Commit transaction (Master.cs:30358 TDALUtils.DBUtils.CommitSafety)
            await tx.CommitAsync();

            return null;
        }
        catch (Exception ex)
        {
            // Nguồn (gate :503-507, :569-573) bắt MỌI ngoại lệ và trả Success=false; không để văng ra thành HTTP 500.
            // Dùng chung cho Delete và DeleteMultiple. Cùng mẫu với Create/Update.
            await tx.RollbackAsync();
            return PortResult.Fail("Mst_Department_Delete", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Cập nhật cấu trúc BUCode, BUPattern, Level cho toàn bảng Mst_Department sau khi ghi.
    /// Nguồn: Master.cs:29413-29448 (Mst_Department_UpdBU).
    /// Tách riêng để tái sử dụng cho các hạng mục Update/Delete sau này.
    /// - Bước 1: Gốc 'HO' viết cứng (@strDepartmentCode_Root = 'HO' tại Master.cs:29416) -> BUCode 'HO', Pattern 'HO%', Level 1.
    /// - Bước 2: Lặp 7 vòng (deep dealer) cho mọi phòng ban khác 'HO':
    ///   Mỗi vòng đọc giá trị BUCode và Level của phòng ban cha từ snapshot của vòng trước.
    ///   BUCode = IsNull(parent.BUCode + '.', '') + Code
    ///   Pattern = BUCode + '%'
    ///   Level = IsNull(parent.Level, 0) + 1
    /// </summary>
    private async Task UpdBUAsync()
    {
        var all = await _db.Set<Mst_Department>().ToListAsync();
        if (all.Count == 0) return;

        // Bước 1: gốc 'HO' (mã viết cứng tại Master.cs:29416)
        foreach (var d in all)
        {
            if (string.Equals(d.DepartmentCode, "HO", StringComparison.OrdinalIgnoreCase))
            {
                d.DepartmentBUCode = "HO";
                d.DepartmentBUPattern = "HO%";
                d.DepartmentLevel = 1;
            }
        }

        // Bước 2: 7 vòng lặp snapshot (Master.cs:29430-29446)
        for (int round = 0; round < 7; round++)
        {
            var snapshot = all.ToDictionary(
                x => x.DepartmentCode,
                x => (BUCode: x.DepartmentBUCode, Level: x.DepartmentLevel),
                StringComparer.OrdinalIgnoreCase
            );

            foreach (var d in all)
            {
                if (string.Equals(d.DepartmentCode, "HO", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string? parentBU = null;
                int? parentLevel = null;

                if (!string.IsNullOrEmpty(d.DepartmentCodeParent) &&
                    snapshot.TryGetValue(d.DepartmentCodeParent, out var pInfo))
                {
                    parentBU = pInfo.BUCode;
                    parentLevel = pInfo.Level;
                }

                // IsNull(t_Parent.DepartmentBUCode + '.', '') + t.DepartmentCode (Master.cs:29437)
                var prefix = string.IsNullOrEmpty(parentBU) ? "" : parentBU + ".";
                d.DepartmentBUCode = prefix + d.DepartmentCode;
                // IsNull(t_Parent.DepartmentBUCode + '.', '') + t.DepartmentCode + '%' (Master.cs:29438)
                d.DepartmentBUPattern = d.DepartmentBUCode + "%";
                // IsNull(t_Parent.DepartmentLevel, 0) + 1 (Master.cs:29439)
                d.DepartmentLevel = (parentLevel ?? 0) + 1;
            }
        }

        await _db.SaveChangesAsync();
    }

    private static string? StdParam(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var t = s.Trim();
        return t.Length == 0 ? null : t.ToUpperInvariant();
    }

    private static string? StdStr(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var t = s.Trim().Normalize();
        return t.Length == 0 ? null : t;
    }

    private static string StdFlag(string? s) => s?.Trim() == "1" ? "1" : "0";

    /// <summary>
    /// Lọc + đếm + chia trang theo DepartmentCode asc — dùng cho Search, GetAllActive, GetByDepartmentCode.
    /// Trả (itemCount, rows): itemCount = tổng số dòng sau lọc (MyCount, SQL :30461); rows = trang đã cắt kèm MyIdxSeq 0-based (:30503).
    /// </summary>
    private async Task<(int itemCount, List<object> rows)> GetXAsync(
        string? keyWord,
        string? orgIdEqual,
        string? flagActiveCsv,
        long recordStart,
        long recordCount,
        string? departmentCodeEqual = null)
    {
        var q = _db.Set<Mst_Department>().AsNoTracking().AsQueryable();

        // KeyWord -> like '%kw%' trên DepartmentName (Mst_DepartmentController.cs:884).
        // Nguồn chạy SQL Server collation KHÔNG phân biệt hoa/thường ⇒ hạ chữ cả 2 phía rồi LIKE (bẫy #18/#31).
        if (!string.IsNullOrWhiteSpace(keyWord))
        {
            var kw = PortQuery.Contains(keyWord);
            q = q.Where(x => EF.Functions.Like((x.DepartmentName ?? "").ToLower(), kw, PortQuery.Esc));
        }

        // DepartmentCode -> `DepartmentCode = 'x'` (Mst_DepartmentController.cs:914 — strWhereClause_Mst_Department_By_DepartmentCode).
        // Nguồn dùng `=` trên SQL Server collation KHÔNG phân biệt hoa/thường ⇒ Like KHÔNG wildcard, hạ chữ 2 phía (PortQuery.Equal).
        // LƯU Ý: nhánh OrgID (:916-919) và FlagActive (:920-923) KHÔNG được truyền ở GetByDepartmentCode (:241-244) nên KHÔNG chạy.
        if (!string.IsNullOrWhiteSpace(departmentCodeEqual))
        {
            var dc = PortQuery.Equal(departmentCodeEqual);
            q = q.Where(x => EF.Functions.Like((x.DepartmentCode ?? "").ToLower(), dc, PortQuery.Esc));
        }

        // OrgID -> `OrgID = 'x'` (Mst_DepartmentController.cs:888).
        // Nguồn dùng '=' ⇒ so sánh bằng chuỗi không phân biệt hoa/thường bằng Like + PortQuery.Equal (không wildcard).
        if (!string.IsNullOrWhiteSpace(orgIdEqual))
        {
            var orgId = PortQuery.Equal(orgIdEqual);
            q = q.Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), orgId, PortQuery.Esc));
        }

        // FlagActive -> in ('a','b') (Mst_DepartmentController.cs:894-902): tách dấu phẩy, bỏ khoảng trắng, hạ chữ.
        // Dòng '=' (:890-893) BỊ COMMENT ở nguồn nên KHÔNG dùng.
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

        // Chia trang theo DepartmentCode asc (Mst_Department_GetX SQL: Master.cs:30457 order by md.DepartmentCode asc).
        var ordered = q.OrderBy(x => x.DepartmentCode);
        var itemCount = await ordered.CountAsync();

        // MyIdxSeq = số thứ tự 0-based trong thứ tự DepartmentCode (identity(bigint, 0, 1) ở SQL nguồn :30450).
        // Lấy dòng [recordStart, recordStart+recordCount) theo thứ tự DepartmentCode.
        var page = recordCount <= 0
            ? new List<Mst_Department>()
            : await ordered.Skip((int)recordStart).Take((int)recordCount).ToListAsync();

        // Tra cứu tên phòng ban cấp trên md_DepartmentNameParent (left join Mst_Department f on t.DepartmentCodeParent = f.DepartmentCode :30512-30513).
        var parentCodes = page
            .Select(x => x.DepartmentCodeParent)
            .Where(c => !string.IsNullOrEmpty(c))
            .Distinct()
            .ToList();

        var parentMap = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        if (parentCodes.Count > 0)
        {
            var parents = await _db.Set<Mst_Department>()
                .AsNoTracking()
                .Where(x => parentCodes.Contains(x.DepartmentCode))
                .Select(x => new { x.DepartmentCode, x.DepartmentName })
                .ToListAsync();

            foreach (var p in parents)
            {
                parentMap[p.DepartmentCode] = p.DepartmentName;
            }
        }

        // Select cuối theo t.MyIdxSeq asc (Master.cs:30514 order by t.MyIdxSeq asc).
        // ss_QtyStaff: nguồn left join count từ Staff_MapDepartment (:30490-30499) -> chưa có ở đích -> gán 0 cho mọi dòng (GAP).
        var rows = page
            .Select((x, i) =>
            {
                string? parentName = !string.IsNullOrEmpty(x.DepartmentCodeParent) && parentMap.TryGetValue(x.DepartmentCodeParent, out var pn)
                    ? pn
                    : null;

                return (object)new
                {
                    MyIdxSeq = recordStart + i,
                    x.DepartmentCode,
                    x.NetworkID,
                    x.DepartmentCodeParent,
                    x.DepartmentBUCode,
                    x.DepartmentBUPattern,
                    x.DepartmentLevel,
                    x.MST,
                    x.OrgID,
                    x.DepartmentName,
                    x.DepartmentDesc,
                    x.FlagActive,
                    x.LogLUDTimeUTC,
                    x.LogLUBy,
                    md_DepartmentNameParent = parentName,
                    ss_QtyStaff = 0
                };
            })
            .ToList();

        return (itemCount, rows);
    }
}
