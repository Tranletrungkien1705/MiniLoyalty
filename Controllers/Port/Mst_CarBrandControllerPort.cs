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
// PORT 1:1 — màn danh mục hãng xe (Mst_CarBrand) của dự án nguồn 2022.7.Loyalty (#77).
//
// Nguồn controller (tầng gate):
//   Mst_CarBrandController.cs:25-105   Search              -> WA_Mst_CarBrand_Get
//   Mst_CarBrandController.cs:283-308  strWhereClause_Mst_CarBrand (mệnh đề lọc Search)
//   Mst_CarBrandController.cs:332-419  Create              -> WA_Mst_CarBrand_Create (#80)
//   Mst_CarBrandController.cs:421-488  Update              -> WA_Mst_CarBrand_Update (#81)
//   Mst_CarBrandController.cs:492-555  Delete              -> WA_Mst_CarBrand_Delete (#82)
//   Mst_CarBrandController.cs:557-621  DeleteMultiple      -> WA_Mst_CarBrand_Delete (#83)
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
//
// Bỏ qua có chủ đích (loại Đ):
//   - CheckGatewayAuthentication: gate :34, :352, :441, :501, :577; API MstCarBrandController.cs:259-264, :42, :114-119, :187-192
//   - CheckAuthorize: gate :35, :353, :442, :502, :578 (hệ thống đích chưa có hệ phân quyền)
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

[ApiController]
[Route("api/mst_carbrand")]
[Produces("application/json")]
public class Mst_CarBrandControllerPort : ControllerBase
{
    private readonly AppDbContext _db;

    public Mst_CarBrandControllerPort(AppDbContext db)
    {
        _db = db;
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

        // 2. Mở 1 transaction cho toàn bộ nghiệp vụ (Master.Loyalty.cs:625 _cf.db.BeginTransaction()).
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // 3. Chuẩn hoá dữ liệu input (Master.Loyalty.cs:763-766):
            //    StdParam = Trim().ToUpper(), rỗng -> null (05.Refs.Biz\Biz\Loyalty.TVO\Loyalty.TVO.Utils\Utils.cs:29).
            //    StdStr   = Trim().Normalize(), rỗng -> null (cùng tệp Utils.cs:29).
            //    CẢNH BÁO: BrandName dùng StdParam (:764) nên tên hãng xe bị VIẾT HOA (ToUpper) theo đúng hành vi nguồn!
            string? strBrandCode = StdParam(req.BrandCode);
            string? strBrandName = StdParam(req.BrandName);
            string? strNetworkID = StdParam(req.NetworkID);
            string? strRemark = StdStr(req.Remark);

            // 4. Kiểm tra dữ liệu:
            // 4.1. BrandCode null/rỗng -> ErridnLoyalty.Mst_CarBrand_Create_InvalidBrandCode (:775-785).
            if (string.IsNullOrEmpty(strBrandCode))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_CarBrand_Create_InvalidBrandCode", "Check.strBrandCode", strBrandCode);
            }

            // 4.2. Mst_CarBrand_CheckDB(BrandCode, Flag "0") (Master.Loyalty.cs:787-793 -> :28-90):
            //      strFlagExistToCheck = "0" (TConst.Flag.No). Nếu đã tồn tại dòng -> ErridnLoyalty.Mst_CarBrand_CheckDB_BrandExist (:65-75).
            //      Nguồn dùng `=` trên SQL Server collation không phân biệt hoa/thường -> Like PortQuery.Equal, hạ chữ 2 phía.
            var exists = await _db.Set<Mst_CarBrand>().AnyAsync(x =>
                EF.Functions.Like((x.BrandCode ?? "").ToLower(), PortQuery.Equal(strBrandCode), PortQuery.Esc));
            if (exists)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_CarBrand_CheckDB_BrandExist", "Check.BrandCode", strBrandCode);
            }

            // 4.3. KHÔNG kiểm tra tên hãng (BrandName), KHÔNG kiểm tra NetworkID (hành vi nguồn Master.Loyalty.cs:773-796).

            // 5. Lưu dòng mới (Master.Loyalty.cs:804-820):
            var dtimeSys = DateTime.UtcNow; // Master.Loyalty.cs:606 dtimeSys = DateTime.UtcNow
            var entity = new Mst_CarBrand
            {
                BrandCode = strBrandCode,       // :805 BrandCode
                BrandName = strBrandName,       // :806 BrandName (viết hoa theo hành vi nguồn :764)
                NetworkID = strNetworkID,       // :807 NetworkID
                Remark = strRemark,             // :808 Remark
                FlagActive = "1",               // :809 FlagActive = TConst.Flag.Active ("1")
                LogLUDTimeUTC = dtimeSys,       // :810 LogLUDTimeUTC
                LogLUBy = "port"                // :811 LogLUBy ("port" - đích chưa có session user, loại Đ)
            };

            await _db.Set<Mst_CarBrand>().AddAsync(entity);
            await _db.SaveChangesAsync();

            // 6. Commit transaction (Master.Loyalty.cs:694 TDALUtils.DBUtils.CommitSafety).
            await tx.CommitAsync();

            // 7. Thành công: gate Mst_CarBrandController.cs:403-404 SetSuccess + Data = null.
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
