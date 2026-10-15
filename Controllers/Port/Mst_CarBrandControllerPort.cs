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
// Nguồn API:
//   MstCarBrandController.cs:238-307   WA_Mst_CarBrand_Get
//   MstCarBrandController.cs:20-90     WA_Mst_CarBrand_Create
// Nguồn biz:
//   Master.Loyalty.cs:93-193   WAS_Mst_CarBrand_Get
//   Master.Loyalty.cs:194-346  Mst_CarBrand_Get
//   Master.Loyalty.cs:347-501  Mst_CarBrand_GetX (SQL :400-447)
//   Master.Loyalty.cs:502-584  WAS_Mst_CarBrand_Create
//   Master.Loyalty.cs:585-731  Mst_CarBrand_Create
//   Master.Loyalty.cs:732-828  Mst_CarBrand_CreateX
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
//
// Bỏ qua có chủ đích (loại Đ):
//   - CheckGatewayAuthentication: gate :34, :352; API MstCarBrandController.cs:259-264, :42
//   - CheckAuthorize: gate :35, :353 (hệ thống đích chưa có hệ phân quyền)
//   - Nguồn gọi bằng tài khoản hệ thống viết cứng (tài khoản + mật khẩu viết cứng — KHÔNG chép giá trị),
//     đích chưa có phân quyền; KHÔNG chép thông tin đăng nhập vào mã
//   - Ghi RequestLog/ReturnLog: Master.Loyalty.cs:238-245, 336-343, 628-635, 721-728
//   - Sys_User_CheckAuthorize: Master.Loyalty.cs:251-264, 647-660
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
    #endregion
}
