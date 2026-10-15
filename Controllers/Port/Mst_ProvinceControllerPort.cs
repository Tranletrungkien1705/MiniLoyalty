using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MiniLoyalty.Data;
using MiniLoyalty.Models.Port;

namespace MiniLoyalty.Controllers.Port;

// ============================================================================================
// PORT 1:1 — màn danh mục tỉnh/thành (Mst_Province) của dự án nguồn 2022.7.Loyalty (#213, #214, #215, #216).
//
// Nguồn controller (tầng gate):
//   Mst_ProvinceController.cs:31-111   Search              -> WA_Mst_Province_Get
//   Mst_ProvinceController.cs:113-176  GetByProvinceCode   -> WA_Mst_Province_Get
//   Mst_ProvinceController.cs:178-241  GetAllActive        -> WA_Mst_Province_Get
//   Mst_ProvinceController.cs:245-305  Create              -> WA_Mst_Province_Create
//   Mst_ProvinceController.cs:735-759  strWhereClause_Mst_Province (mệnh đề lọc Search, GetAllActive)
//   Mst_ProvinceController.cs:761-776  strWhereClause_Mst_Province_By_ProvinceCode (mệnh đề lọc By ProvinceCode)
// Nguồn API:
//   MstProvinceController.cs:20-91     WA_Mst_Province_Get
//   MstProvinceController.cs:94-165    WA_Mst_Province_Create
// Nguồn biz:
//   Master.Loyalty.cs (WAS_Mst_Province_Get -> Mst_Province_Get -> Mst_Province_Get SQL: :442-488)
//   Master.cs:1215-1297 (WAS_Mst_Province_Create) -> Master.cs:591-743 (Mst_Province_Create) -> Master.cs:27-91 (Mst_Province_CheckDB)
//
// GIỮ ĐÚNG NGUỒN:
//   * Endpoint là POST (nguồn [AcceptVerbs("POST")]).
//   * Lọc KeyWord: like '%x%' trên ProvinceName (:740-743).
//   * Lọc FlagActive: danh sách in ('a','b') (:748-756).
//   * Lọc ProvinceCode: toán tử = trên ProvinceCode (:768).
//   * OrgID: gate nhận (:31, :61) nhưng strWhereClause_Mst_Province (:735-759) KHÔNG lọc OrgID.
//   * GetByProvinceCode: ProvinceCode rỗng -> không truy vấn, trả Data = null (:132-133, :163).
//   * GetByProvinceCode: không truyền FlagActive (:135-138) -> cờ FlagActive '0' vẫn lấy được.
//   * Chia trang theo mp.ProvinceCode asc (:449), select cuối sắp theo t.MyIdxSeq asc (:488).
//   * MyIdxSeq 0-based identity (:443).
//   * PageSize = 0 -> DataList rỗng, PageCount = 0 (gate :86-94).
//   * Create: kiểm tra thứ tự: (1) mã rỗng -> InvalidProvinceCode, (2) CheckDB trùng -> ProvinceExist,
//     (3) tên rỗng -> InvalidProvinceName; mã lưu chữ hoa qua StdParam, tên chỉ Trim (không Normalize);
//     NetworkID cấu hình Biz_NetworkID, mặc định 1001; FlagActive "1", LogLUBy "port", LogLUDTimeUTC = UtcNow;
//     body rỗng -> ServiceException ("strJson null") -> Fail("Mst_Province_Create").
//
// Bỏ qua có chủ đích (loại Đ):
//   - CheckGatewayAuthentication: gate Mst_ProvinceController.cs:264; API MstProvinceController.cs:117-122
//   - CheckAuthorize: gate Mst_ProvinceController.cs:265 (hệ thống đích chưa có hệ phân quyền)
//   - Nguồn gọi bằng tài khoản hệ thống viết cứng (tài khoản + mật khẩu viết cứng tại :279-282 — KHÔNG chép giá trị),
//     đích chưa có phân quyền; KHÔNG chép thông tin đăng nhập vào mã
//   - Ghi RequestLog/ReturnLog: Master.cs:628-635, 733-740
//   - LogLUBy gán cố định "port" do hệ thống đích chưa có session người dùng (Master.cs:693)
//   - Biz_NetworkID lấy từ cấu hình IConfiguration, mặc định "1001" nếu thiếu (Common.cs:39, 124; Master.cs:689)
// ============================================================================================

/// <summary>Port 1:1 <c>Mst_ProvinceSearch</c> — tham số lọc + phân trang của màn (Mst_ProvinceController.cs:31, 58-63).</summary>
public class MstProvinceSearchReq
{
    public string? OrgID { get; set; }               // Tổ chức — gate nhận (:31, :61) nhưng strWhereClause_Mst_Province (:735-759) KHÔNG lọc OrgID
    public string? KeyWord { get; set; }             // Từ khoá tìm theo tên tỉnh/thành (ProvinceName) — :60, :740-743
    public string? FlagActive { get; set; }          // Cờ hoạt động, danh sách phân cách dấu phẩy — :62, :748-756
    public int Ft_PageIndex { get; set; } = 0;       // Trang (0-based) — :31, :52
    public int Ft_PageSize { get; set; } = 10;       // Số dòng/trang — :31, :53
}

/// <summary>Port 1:1 <c>GetByProvinceCode</c> — body chỉ có ProvinceCode (Mst_ProvinceController.cs:113-176).</summary>
public class MstProvinceGetByProvinceCodeReq
{
    public string? ProvinceCode { get; set; }         // Mã tỉnh/thành cần lấy — :135-138
}

/// <summary>Port 1:1 <c>Create</c> — body nhận ProvinceCode, ProvinceName (Mst_ProvinceController.cs:245-305).</summary>
public class MstProvinceCreateReq
{
    public string? ProvinceCode { get; set; }         // Mã tỉnh/thành — Mst_Province.cs:11, Master.cs:640
    public string? ProvinceName { get; set; }         // Tên tỉnh/thành — Mst_Province.cs:15, Master.cs:641
}

[ApiController]
[Route("api/mst_province")]
[Produces("application/json")]
public class Mst_ProvinceControllerPort(AppDbContext db, IConfiguration config) : ControllerBase
{
    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody] MstProvinceSearchReq? req)
    {
        var r = req ?? new MstProvinceSearchReq();
        var pageIndex = Math.Max(0, r.Ft_PageIndex);
        var pageSize = r.Ft_PageSize;

        // Search gọi GetXAsync với start = PageIndex * PageSize, count = PageSize (giữ nguyên hành vi cũ nguồn :50-51, :75-76).
        var start = (long)pageIndex * pageSize;
        // OrgID nhận ở request nhưng trong strWhereClause_Mst_Province (:735-759) KHÔNG lọc OrgID; provinceCodeEqual truyền null.
        var (itemCount, rows) = await GetXAsync(r.KeyWord, r.FlagActive, start, pageSize, null);

        // PageSize=0 -> DataList rỗng (nguồn: nFilterRecordEnd = start-1, SQL biz + gate :86-94).
        // PageCount = 0 khi DataList rỗng, ngược lại ceil(ItemCount / ps) với ps = PageSize (PageSize=0 thì 10) — gate :86-94.
        var ps = pageSize == 0 ? 10 : pageSize;
        var pageCount = rows.Count == 0 ? 0 : (itemCount % ps == 0 ? itemCount / ps : itemCount / ps + 1);

        return Ok(new { Success = true, Data = new PageInfo<object>(rows, itemCount, pageCount, pageIndex, pageSize) });
    }

    // ============================================================================================
    // GetByProvinceCode — nguồn Mst_ProvinceController.cs:113-176.
    // Gate: nếu ProvinceCode rỗng thì KHÔNG truy vấn (:133), objMst_Province giữ null (:132) ⇒ Data = null (:163).
    // Ngược lại gán ProvinceCode (:135-138) rồi gọi strWhereClause_Mst_Province_By_ProvinceCode (:761-776)
    // (lọc `ProvinceCode = 'x'` — KHÔNG truyền FlagActive nên hàng FlagActive '0' VẪN lấy được).
    // Ft_RecordStart = "0", Ft_RecordCount = "1" (:150-151) ⇒ lấy đúng 1 dòng, trả Lst_Mst_Province[0] (:157-160).
    // ============================================================================================
    [HttpPost("getbyprovincecode")]
    public async Task<IActionResult> GetByProvinceCode([FromBody] MstProvinceGetByProvinceCodeReq? req)
    {
        var provinceCode = req?.ProvinceCode;

        // ProvinceCode rỗng ⇒ nguồn không truy vấn (:133) và trả Data = null (:163).
        if (string.IsNullOrWhiteSpace(provinceCode))
        {
            return Ok(new { Success = true, Data = (object?)null });
        }

        // Lấy 1 dòng theo ProvinceCode (Ft_RecordStart = "0", Ft_RecordCount = "1" — :150-151).
        var (_, rows) = await GetXAsync(null, null, 0, 1, provinceCodeEqual: provinceCode);

        // Nguồn trả Lst_Mst_Province[0] khi có dòng, ngược lại null (:157-160).
        return Ok(new { Success = true, Data = rows.Count > 0 ? rows[0] : null });
    }

    // ============================================================================================
    // GetAllActive — nguồn Mst_ProvinceController.cs:178-241.
    // Lọc FlagActive = "1" qua strWhereClause_Mst_Province (toán tử IN :748-756).
    // OrgID chỉ gán vào request (:211), KHÔNG đưa vào where clause (:735-759).
    // Ft_RecordStart = "0", Ft_RecordCount = "123456000" (:213-214, BaseController.cs:30-31).
    // HÌNH DÁNG DATA KHÁC CÁC DANH MỤC KHÁC (như CardType):
    // Nguồn bọc trong object { Lst_Mst_Province = lst_Mst_Province } (:225-227),
    // nếu không có bản ghi nào thì lst_Mst_Province giữ null (:197, :220-222) => Data.Lst_Mst_Province = null.
    // ============================================================================================
    [HttpPost("getallactive")]
    public async Task<IActionResult> GetAllActive()
    {
        var (_, rows) = await GetXAsync(null, "1", 0, 123456000, null);
        return Ok(new { Success = true, Data = new { Lst_Mst_Province = rows.Count > 0 ? rows : null } });
    }

    // ============================================================================================
    // Create — nguồn gate Mst_ProvinceController.cs:245-305 -> API WA_Mst_Province_Create
    // (MstProvinceController.cs:94-165) -> biz WAS_Mst_Province_Create (Master.cs:1215-1297)
    // -> Mst_Province_Create (Master.cs:591-743) -> Mst_Province_CheckDB (Master.cs:27-91).
    //
    // Quy trình và thứ tự kiểm tra:
    //   1. Body rỗng / null: gate :254-261 -> ServiceException("strJson null") -> Fail("Mst_Province_Create")
    //   2. Chuẩn hoá:
    //      - strProvinceCode = StdParam(objProvinceCode) (:640: Trim + ToUpper, rỗng -> null)
    //      - strProvinceName = string.Format("{0}", objProvinceName).Trim() (:641: chỉ Trim, không Normalize)
    //   3. Kiểm tra mã: if (strProvinceCode == null || strProvinceCode.Length < 1)
    //      -> Fail("Mst_Province_Create_InvalidProvinceCode", "Check.strProvinceCode", strProvinceCode) (:649-659)
    //   4. Kiểm tra tồn tại trong DB: Mst_Province_CheckDB với Flag "0" (:660-666, :64-74)
    //      nếu đã có -> Fail("Mst_Province_CheckDB_ProvinceExist", "Check.ProvinceCode", strProvinceCode)
    //   5. Kiểm tra tên: if (strProvinceName.Length < 1)
    //      -> Fail("Mst_Province_Create_InvalidProvinceName", "Check.strProvinceName", strProvinceName) (:668-678)
    //   6. Lưu 6 cột Mst_Province (:687-701):
    //      ProvinceCode = strProvinceCode, NetworkID = config["Biz_NetworkID"] ?? "1001",
    //      ProvinceName = strProvinceName, FlagActive = "1", LogLUDTimeUTC = UtcNow, LogLUBy = "port".
    //   7. Commit và trả Ok({ Success = true, Data = null }) (:291-292).
    // ============================================================================================
    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] MstProvinceCreateReq? req)
    {
        // 1. Kiểm tra body rỗng / null: gate Mst_ProvinceController.cs:254-261
        if (req == null)
        {
            return PortResult.Fail("Mst_Province_Create", "Check.strJson", null);
        }

        // Bắt đầu Transaction toàn bộ quá trình tạo (Master.cs:625 _cf.db.BeginTransaction())
        await using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            // 2. Chuẩn hoá dữ liệu input (Master.cs:640-641):
            // strProvinceCode = StdParam(objProvinceCode) (Trim + ToUpper, rỗng -> null)
            // strProvinceName = string.Format("{0}", objProvinceName).Trim() (CHỈ Trim, KHÔNG Normalize; null -> "")
            var strProvinceCode = StdParam(req.ProvinceCode);
            var strProvinceName = string.Format("{0}", req.ProvinceName).Trim();

            // 3. Kiểm tra mã tỉnh/thành: Master.cs:649-659
            if (string.IsNullOrEmpty(strProvinceCode))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_Province_Create_InvalidProvinceCode", "Check.strProvinceCode", strProvinceCode);
            }

            // 4. Kiểm tra tồn tại mã tỉnh/thành mới: Master.cs:660-666 -> Mst_Province_CheckDB (Master.cs:27-91)
            var exists = await db.Set<Mst_Province>().AnyAsync(x =>
                EF.Functions.Like((x.ProvinceCode ?? "").ToLower(), PortQuery.Equal(strProvinceCode), PortQuery.Esc));
            if (exists)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_Province_CheckDB_ProvinceExist", "Check.ProvinceCode", strProvinceCode);
            }

            // 5. Kiểm tra tên tỉnh/thành: Master.cs:668-678
            if (strProvinceName.Length < 1)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_Province_Create_InvalidProvinceName", "Check.strProvinceName", strProvinceName);
            }

            // 6. Lưu dữ liệu bảng Mst_Province: Master.cs:687-701
            var networkId = config["Biz_NetworkID"] ?? "1001"; // Cấu hình Biz_NetworkID, mặc định 1001 (Common.cs:39, 124)
            var now = DateTime.UtcNow;

            var newProvince = new Mst_Province
            {
                ProvinceCode = strProvinceCode,                    // :688 ProvinceCode
                NetworkID = networkId,                             // :689 NetworkID = nNetworkID
                ProvinceName = strProvinceName,                    // :690 ProvinceName
                FlagActive = "1",                                  // :691 FlagActive = TConst.Flag.Active ("1")
                LogLUDTimeUTC = now,                               // :692 LogLUDTimeUTC = dtimeSys
                LogLUBy = "port"                                   // :693 LogLUBy ("port" - đích chưa có session user, comment loại Đ)
            };

            await db.Set<Mst_Province>().AddAsync(newProvince);
            await db.SaveChangesAsync();

            // Commit Transaction (Master.cs:706 TDALUtils.DBUtils.CommitSafety)
            await tx.CommitAsync();

            // Thành công trả về Success = true, Data = null: gate Mst_ProvinceController.cs:291-292
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

    /// <summary>
    /// Lọc + đếm + chia trang theo ProvinceCode asc — dùng chung cho Search, GetByProvinceCode và GetAllActive.
    /// Trả (itemCount, rows): itemCount = tổng số dòng sau lọc (MyCount, SQL :453); rows = trang đã cắt kèm MyIdxSeq 0-based (:443).
    /// </summary>
    private async Task<(int itemCount, List<object> rows)> GetXAsync(string? keyWord, string? flagActiveCsv, long recordStart, long recordCount, string? provinceCodeEqual)
    {
        var q = db.Set<Mst_Province>().AsNoTracking().AsQueryable();

        // KeyWord -> like '%x%' trên ProvinceName (Mst_ProvinceController.cs:740-743).
        // Nguồn chạy SQL Server collation KHÔNG phân biệt hoa/thường ⇒ hạ chữ cả 2 phía rồi LIKE (bẫy #18/#31).
        if (!string.IsNullOrWhiteSpace(keyWord))
        {
            var kw = PortQuery.Contains(keyWord);
            q = q.Where(x => EF.Functions.Like((x.ProvinceName ?? "").ToLower(), kw, PortQuery.Esc));
        }

        // ProvinceCode -> `ProvinceCode = 'x'` (Mst_ProvinceController.cs:768 — strWhereClause_Mst_Province_By_ProvinceCode).
        // Nguồn dùng `=` trên SQL Server collation KHÔNG phân biệt hoa/thường ⇒ Like KHÔNG wildcard, hạ chữ 2 phía.
        if (!string.IsNullOrWhiteSpace(provinceCodeEqual))
        {
            var pc = PortQuery.Equal(provinceCodeEqual);
            q = q.Where(x => EF.Functions.Like((x.ProvinceCode ?? "").ToLower(), pc, PortQuery.Esc));
        }

        // FlagActive -> in ('a','b') (Mst_ProvinceController.cs:748-756): tách dấu phẩy, bỏ khoảng trắng, hạ chữ.
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

        // Chia trang theo ProvinceCode asc (Mst_Province_Get SQL: :449 order by mp.ProvinceCode asc).
        var ordered = q.OrderBy(x => x.ProvinceCode);
        var itemCount = await ordered.CountAsync();

        // MyIdxSeq = số thứ tự 0-based trong thứ tự ProvinceCode (identity(bigint, 0, 1) ở SQL nguồn :443).
        // Lấy dòng [recordStart, recordStart+recordCount) theo thứ tự ProvinceCode.
        var page = recordCount <= 0
            ? new List<Mst_Province>()
            : await ordered.Skip((int)recordStart).Take((int)recordCount).ToListAsync();

        // Select cuối theo t.MyIdxSeq asc (:488 order by t.MyIdxSeq asc).
        var rows = page
            .Select((x, i) => (object)new
            {
                MyIdxSeq = recordStart + i,
                x.ProvinceCode,
                x.NetworkID,
                x.ProvinceName,
                x.FlagActive,
                x.LogLUDTimeUTC,
                x.LogLUBy,
            })
            .ToList();

        return (itemCount, rows);
    }

    #region Helper chuẩn hoá input (05.Refs.Biz\Biz\Loyalty.TVO\Loyalty.TVO.Utils\Utils.cs)
    /// <summary>
    /// Chuẩn hoá tham số mã: Trim, ToUpper, rỗng -> null.
    /// Dùng cho mã (ProvinceCode...).
    /// Nguồn tham chiếu: 05.Refs.Biz\Biz\Loyalty.TVO\Loyalty.TVO.Utils\Utils.cs:29
    /// </summary>
    private static string? StdParam(object? val)
    {
        if (val == null) return null;
        var s = val.ToString()?.Trim();
        return string.IsNullOrEmpty(s) ? null : s.ToUpperInvariant();
    }
    #endregion
}
