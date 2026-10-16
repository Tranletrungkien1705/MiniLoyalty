using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MiniLoyalty.Data;
using MiniLoyalty.Models.Port;

namespace MiniLoyalty.Controllers.Port;

// ============================================================================================
// PORT 1:1 — màn danh mục phòng ban (Mst_Department) của dự án nguồn 2022.7.Loyalty (#122, #123, #124, #125).
//
// Nguồn controller (tầng gate):
//   Mst_DepartmentController.cs:30-109   Search -> WA_Mst_Department_Get (#122)
//   Mst_DepartmentController.cs:110-218  GetAllActive -> WA_Mst_Department_Get (#123)
//   Mst_DepartmentController.cs:219-282  GetByDepartmentCode -> WA_Mst_Department_Get (#124)
//   Mst_DepartmentController.cs:286-370  Create -> WA_Mst_Department_Create (#125)
//   Mst_DepartmentController.cs:877-905  strWhereClause_Mst_Department (mệnh đề lọc Search, GetAllActive)
//   Mst_DepartmentController.cs:907-926  strWhereClause_Mst_Department_By_DepartmentCode (mệnh đề lọc GetByDepartmentCode)
// Nguồn API:
//   MstDepartmentController.cs:20-89     WA_Mst_Department_Get
//   MstDepartmentController.cs:91-160    WA_Mst_Department_Create (#125)
// Nguồn biz:
//   Master.cs:29450-29548                WAS_Mst_Department_Get
//   Master.cs:29549-29701                Mst_Department_Get
//   Master.cs:30396-30568                Mst_Department_GetX (SQL :30449-30515)
//   Master.cs:29702-29785                WAS_Mst_Department_Create (#125)
//   Master.cs:29786-29935                Mst_Department_Create (#125)
//   Master.cs:30569-30726                Mst_Department_CreateX (#125)
//   Master.cs:29413-29448                Mst_Department_UpdBU (#125, cập nhật BUCode, BUPattern, Level toàn bảng)
//   Master.cs:7813-7890                  Mst_NNT_CheckDB (#125)
//   Master.cs:29275-29338                Mst_Department_CheckDB (#125)
//   Master.cs:29341-29411                Mst_Department_CheckDepartmentName (#125)
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
//   * Cột tính md_DepartmentNameParent: left join Mst_Department f on t.DepartmentCodeParent = f.DepartmentCode (:30512-30513),
//     lấy tên phòng ban cấp trên f.DepartmentName (null nếu không có cấp trên).
//   * Cột tính ss_QtyStaff: nguồn left join count StaffCode từ Staff_MapDepartment group by DepartmentCode (:30490-30499, :30506).
//     Bảng Staff_MapDepartment KHÔNG có ở đích, không có DDL, không có trong ledger -> ss_QtyStaff = 0 cho mọi dòng.
//     GHI GAP: khi port Staff_MapDepartment phải đếm thật.
//   * DDL: không có DDL trong repo nguồn — PK = DepartmentCode (khoá draft), kiểu cột cần xác nhận.
//
// Bỏ qua có chủ đích (loại Đ):
//   - CheckGatewayAuthentication: gate :39, :123, :228, :306, API :40-46, :112-117
//   - CheckAuthorize: gate :40, :124, :229, :307 (hệ thống đích chưa có hệ phân quyền)
//   - Ghi RequestLog/ReturnLog: Master.cs:29593-29600, 29691-29698, 29830-29837, 29925-29932
//   - Sys_User_CheckAuthorize / Sys_Access_CheckDenyV30: Master.cs:29606-29620, 29630-29634, 29849-29870
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

[ApiController]
[Route("api/mst_department")]
[Produces("application/json")]
public class Mst_DepartmentControllerPort : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public Mst_DepartmentControllerPort(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
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
