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

/// <summary>
/// Port 1:1 Mst_DealerCPController.cs — Mst_DealerCP
/// Chuỗi gọi nguồn:
///   gate: Mst_DealerCPController.cs:26-106 (Search) + :107-230 (GetAllActive) + :231-296 (GetByDLCPCode) + :297-360 (GetByOrgID) + :364-389 (strWhereClause_Mst_DealerCP) + :390-406 (strWhereClause_Mst_DealerCP_By_DLCPCode) + :407-419 (strWhereClause_Mst_DealerCP_By_OrgID) + :436-521 (CreateOld)
///   API:  MstDealerCPController.cs:21-90 (WA_Mst_DealerCP_Get) + :93-162 (WA_Mst_DealerCP_Create)
///   biz:  Master.Loyalty.cs:3937-4050 (WAS_Mst_DealerCP_Get) -> :4051-4206 (Mst_DealerCP_Get) -> :4209-4433 (Mst_DealerCP_GetX)
///         Master.Loyalty.cs:2973-3055 (WAS_Mst_DealerCP_Create) -> :3056-3202 (Mst_DealerCP_Create) -> :3205-3322 (Mst_DealerCP_CreateX) -> :2870-2934 (Mst_DealerCP_CheckDB) -> :2935-2971 (Mst_DealerCP_UpdBU)
///
/// Bỏ qua có chủ đích (loại Đ):
/// - CheckGatewayAuthentication: gate Mst_DealerCPController.cs:35, 120, 306, 456; API MstDealerCPController.cs:42, 114
/// - CheckAuthorize: gate Mst_DealerCPController.cs:36, 121, 307, 457; dòng :241 đã bị comment out ở nguồn
/// - Nguồn gọi bằng tài khoản hệ thống viết cứng (tài khoản + mật khẩu viết cứng tại :264-265 — KHÔNG chép giá trị), đích chưa có phân quyền; KHÔNG chép thông tin đăng nhập vào mã
/// - Sys_User_CheckAuthorize: Master.Loyalty.cs:4107-4124, 3117-3130 (hệ thống đích chưa có hệ phân quyền)
/// - Ghi RequestLog/ReturnLog: Master.Loyalty.cs:4097-4104, 4196-4203, 3098-3105, 3192-3199
/// - LogLUBy gán cố định "port" do hệ thống đích chưa có session người dùng (Master.Loyalty.cs:3305)
/// - Biz_NetworkID lấy từ cấu hình IConfiguration, mặc định "1001" nếu thiếu (Common.cs:39, 124; Master.Loyalty.cs:3293)
/// - Bỏ qua Sequence WA_Seq_Common_Get (gate dòng 468-488 nguồn đã comment out, DLCPCode lấy từ client)
/// </summary>
[ApiController]
[Route("api/mst_dealercp")]
public class Mst_DealerCPControllerPort : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public Mst_DealerCPControllerPort(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    /// <summary>
    /// Port 1:1 GetAllActive action từ gate Mst_DealerCPController.cs:107-230.
    /// Tầng gate truyền OrgID = orgUserUI.OrgId (:135), FlagActive = "1" (:136),
    /// nhưng hàm strWhereClause_Mst_DealerCP (:364-389) KHÔNG sử dụng OrgID trong mệnh đề WHERE,
    /// do đó chỉ lọc FlagActive = "1" và KHÔNG lọc OrgID.
    /// Rt_Cols_Mst_DealerCP = "*" (:153) và Rt_Cols_Map_DealerCP_Org = "*" (:154).
    /// Ft_RecordStart = "0", Ft_RecordCount = "123456000" (BaseController.cs:30-31).
    /// </summary>
    [HttpPost("getallactive")]
    public async Task<IActionResult> GetAllActive()
    {
        var (_, rows, maps) = await GetXAsync(
            keyWord: null,
            flagActiveCsv: "1",
            recordStart: 0,
            recordCount: 123456000,
            getDealer: true,
            getMap: true,
            dlcpCodeEqual: null,
            mapOrgIdEqual: null);

        return Ok(new
        {
            Success = true,
            Data = new
            {
                Lst_Mst_DealerCP = rows.Count > 0 ? rows : null,
                Lst_Map_DealerCP_Org = maps.Count > 0 ? maps : null
            }
        });
    }

    /// <summary>
    /// Port 1:1 GetByDLCPCode action từ gate Mst_DealerCPController.cs:231-296.
    /// Request JSON: { DLCPCode }.
    /// Khi DLCPCode rỗng (null hoặc whitespace): nguồn kiểm tra !IsNullOrEmpty (:252),
    /// bỏ qua truy vấn và trả về Success = true, Data = null (:282-283).
    /// Khi DLCPCode có giá trị: lọc theo DLCPCode với toán tử '=' (strWhereClause_Mst_DealerCP_By_DLCPCode :390-406).
    /// Không truyền FlagActive (:254-257) nên đại lý có FlagActive = '0' vẫn lấy được.
    /// Ft_RecordStart = "0", Ft_RecordCount = "1" (:270-271); trả về phần tử đầu tiên rows[0] (:277-280).
    ///
    /// Bỏ qua có chủ đích (loại Đ):
    /// - CheckAuthorize: gate Mst_DealerCPController.cs:241 đã bị comment out ở nguồn.
    /// - Nguồn gọi bằng tài khoản hệ thống viết cứng (WAUserCode/WAUserPassword viết cứng tại :264-265 — KHÔNG chép giá trị),
    ///   đích chưa có phân quyền; KHÔNG chép thông tin đăng nhập vào mã.
    /// </summary>
    [HttpPost("getbydlcpcode")]
    public async Task<IActionResult> GetByDLCPCode([FromBody] Mst_DealerCPGetByDLCPCodeRequest? req)
    {
        // Khi DLCPCode rỗng: Success = true, Data = null và KHÔNG truy vấn (gate Mst_DealerCPController.cs:251-283)
        if (string.IsNullOrWhiteSpace(req?.DLCPCode))
        {
            return Ok(new
            {
                Success = true,
                Data = (object?)null
            });
        }

        // Lấy đúng 1 đại lý (recordStart = 0, recordCount = 1), toán tử '=', getDealer = true, getMap = false
        var (_, rows, _) = await GetXAsync(
            keyWord: null,
            flagActiveCsv: null,
            recordStart: 0,
            recordCount: 1,
            getDealer: true,
            getMap: false,
            dlcpCodeEqual: req.DLCPCode,
            mapOrgIdEqual: null);

        // Nguồn: lấy phần tử đầu tiên Lst_Mst_DealerCP[0] nếu có (gate Mst_DealerCPController.cs:277-280)
        return Ok(new
        {
            Success = true,
            Data = rows.Count > 0 ? rows[0] : null
        });
    }

    /// <summary>
    /// Port 1:1 GetByOrgID action từ gate Mst_DealerCPController.cs:297-360.
    /// Request JSON: { OrgID }.
    /// Khi OrgID rỗng (null hoặc whitespace): nguồn kiểm tra !IsNullOrEmpty (:317),
    /// bỏ qua truy vấn và trả về Success = true, Data = null (:346-347).
    /// Khi OrgID có giá trị: lọc qua strWhereClause_Mst_DealerCP_By_OrgID (:407-419)
    /// với điều kiện trên Map_DealerCP_Org.OrgID = OrgID (toán tử '=').
    /// Điều kiện này nằm ở bảng nháp (#tbl_Mst_DealerCP_Filter_Draft trong Master.Loyalty.cs:4272-4283),
    /// lọc các đại lý có liên kết với OrgID chỉ định.
    /// Select cuối (:4328-4348) KHÔNG lọc lại theo OrgID, do đó với đại lý liên kết nhiều Org
    /// (ví dụ DL02 map với cả 4020320001 và 4020320003),
    /// dòng đầu tiên Lst_Mst_DealerCP[0] mang mo_OrgID là OrgID đầu tiên theo thứ tự kết quả (:341-344, ở đây là 4020320001),
    /// CÓ THỂ KHÁC OrgID được hỏi (4020320003) — ĐÂY LÀ HÀNH VI CỦA NGUỒN.
    /// Ft_RecordStart = "0", Ft_RecordCount = "1" (:334-335); getDealer = true, getMap = false (:338-339).
    /// Trả về phần tử đầu tiên rows[0] hoặc null nếu không có dòng nào (:341-344).
    ///
    /// Bỏ qua có chủ đích (loại Đ):
    /// - CheckGatewayAuthentication: gate Mst_DealerCPController.cs:306
    /// - CheckAuthorize: gate Mst_DealerCPController.cs:307 (hệ thống đích chưa có hệ phân quyền)
    /// - Sys_User_CheckAuthorize: Master.Loyalty.cs:4107-4124
    /// - Ghi RequestLog/ReturnLog: Master.Loyalty.cs:4097-4104, 4196-4203
    /// </summary>
    [HttpPost("getbyorgid")]
    public async Task<IActionResult> GetByOrgID([FromBody] Mst_DealerCPGetByOrgIDRequest? req)
    {
        // Khi OrgID rỗng: Success = true, Data = null và KHÔNG truy vấn (gate Mst_DealerCPController.cs:317, 346-347)
        if (string.IsNullOrWhiteSpace(req?.OrgID))
        {
            return Ok(new
            {
                Success = true,
                Data = (object?)null
            });
        }

        // Lấy đúng 1 đại lý (recordStart = 0, recordCount = 1), toán tử '=', getDealer = true, getMap = false
        var (_, rows, _) = await GetXAsync(
            keyWord: null,
            flagActiveCsv: null,
            recordStart: 0,
            recordCount: 1,
            getDealer: true,
            getMap: false,
            dlcpCodeEqual: null,
            mapOrgIdEqual: req.OrgID);

        // Nguồn: lấy phần tử đầu tiên Lst_Mst_DealerCP[0] nếu có (gate Mst_DealerCPController.cs:341-344).
        // mo_OrgID có thể khác OrgID được hỏi do select cuối không lọc lại theo OrgID (hành vi nguồn).
        return Ok(new
        {
            Success = true,
            Data = rows.Count > 0 ? rows[0] : null
        });
    }

    /// <summary>
    /// Port 1:1 Search action từ gate Mst_DealerCPController.cs:26-106.
    /// Request JSON: { KeyWord, FlagActive, Ft_PageIndex = 0, Ft_PageSize = 10 }
    /// Trả về: Ok(new { Success = true, Data = new PageInfo&lt;...&gt;(...) })
    /// </summary>
    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody] Mst_DealerCPSearchRequest? req)
    {
        req ??= new Mst_DealerCPSearchRequest();

        var pageIndex = Math.Max(0, req.Ft_PageIndex);
        var pageSize = req.Ft_PageSize;
        long recordStart = (long)pageIndex * pageSize;
        long recordCount = pageSize;

        var (itemCount, rows, _) = await GetXAsync(
            keyWord: req.KeyWord,
            flagActiveCsv: req.FlagActive,
            recordStart: recordStart,
            recordCount: recordCount,
            getDealer: true,
            getMap: false,
            dlcpCodeEqual: null,
            mapOrgIdEqual: null);

        // Tính PageCount theo logic tầng gate Mst_DealerCPController.cs:81-89
        var pageCount = 0;
        if (rows.Count > 0)
        {
            var ps = pageSize == 0 ? 10 : pageSize;
            pageCount = (itemCount % ps == 0) ? (itemCount / ps) : (itemCount / ps + 1);
        }

        var pageInfo = new PageInfo<Mst_DealerCPSearchResultItem>(rows, itemCount, pageCount, pageIndex, pageSize);

        return Ok(new
        {
            Success = true,
            Data = pageInfo
        });
    }

    /// <summary>
    /// Port 1:1 CreateOld action từ gate Mst_DealerCPController.cs:436-521.
    /// Chuỗi gọi: gate :436-521 -> API WA_Mst_DealerCP_Create (:93-162) -> biz WAS_Mst_DealerCP_Create (Master.Loyalty.cs:2973-3055)
    /// -> Mst_DealerCP_Create (:3056-3202) -> Mst_DealerCP_CreateX (:3205-3322) -> Mst_DealerCP_CheckDB (:2870-2934) -> Mst_DealerCP_UpdBU (:2935-2971).
    /// </summary>
    [HttpPost("createold")]
    public async Task<IActionResult> CreateOld([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] Mst_DealerCPCreateOldRequest? req)
    {
        // 1. Kiểm tra body rỗng / null: Gate Mst_DealerCPController.cs:446-453
        // if (!string.IsNullOrEmpty(strJson)) { ... } else { throw new ServiceException("strJson null"); }
        if (req == null)
        {
            return PortResult.Fail("Mst_DealerCP_Create", "Check.strJson", null);
        }

        // Bắt đầu Transaction toàn bộ quá trình tạo và tính lại BU (Master.Loyalty.cs:3095 _cf.db.BeginTransaction())
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // 2. Chuẩn hoá dữ liệu input (Master.Loyalty.cs:3238-3242):
            // StdParam = trim; StdStr = trim (rỗng -> null)
            string? strDLCPCode = req.DLCPCode?.Trim();
            string? strDLCPCodeParent = string.IsNullOrWhiteSpace(req.DLCPCodeParent) ? null : req.DLCPCodeParent.Trim();
            string? strDLCPName = string.IsNullOrWhiteSpace(req.DLCPName) ? null : req.DLCPName.Trim();
            string? strDLCPPhoneNo = string.IsNullOrWhiteSpace(req.DLCPPhoneNo) ? null : req.DLCPPhoneNo.Trim();
            string? strRemark = string.IsNullOrWhiteSpace(req.Remark) ? null : req.Remark.Trim();

            // 3. Kiểm tra mã đại lý: Master.Loyalty.cs:3248-3258
            // if (strDLCPCode == null || strDLCPCode.Length < 1) -> ErridnLoyalty.Mst_DealerCP_Create_InvalidDLCPCode
            if (string.IsNullOrEmpty(strDLCPCode))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_DealerCP_Create_InvalidDLCPCode", "Check.strDLCPCode", strDLCPCode, "Check.DLCPCode", strDLCPCode);
            }

            // 4. Kiểm tra tồn tại mã đại lý mới: Master.Loyalty.cs:3260-3266 -> Mst_DealerCP_CheckDB (:2870-2934)
            // strFlagExistToCheck = "0" (TConst.Flag.No). Nếu đã tồn tại dòng -> ErridnLoyalty.Mst_DealerCP_CheckDB_DLExist (:2907-2917)
            var exists = await _db.Set<Mst_DealerCP>().AnyAsync(x =>
                EF.Functions.Like((x.DLCPCode ?? "").ToLower(), PortQuery.Equal(strDLCPCode), PortQuery.Esc));
            if (exists)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_DealerCP_CheckDB_DLExist", "Check.DLCPCode", strDLCPCode);
            }

            // 5. Kiểm tra đại lý cha (nếu có DLCPCodeParent): Master.Loyalty.cs:3270-3281
            // strFlagExistToCheck = "1" (TConst.Flag.Yes), strFlagActiveListToCheck = "1" (TConst.Flag.Active)
            if (!string.IsNullOrEmpty(strDLCPCodeParent))
            {
                var parent = await _db.Set<Mst_DealerCP>().FirstOrDefaultAsync(x =>
                    EF.Functions.Like((x.DLCPCode ?? "").ToLower(), PortQuery.Equal(strDLCPCodeParent), PortQuery.Esc));

                if (parent == null)
                {
                    // Không tìm thấy đại lý cha (:2896-2906) -> ErridnLoyalty.Mst_DealerCP_CheckDB_DLNotFound
                    await tx.RollbackAsync();
                    return PortResult.Fail("Mst_DealerCP_CheckDB_DLNotFound", "Check.DLCPCode", strDLCPCodeParent);
                }

                // Cờ hoạt động của đại lý cha không khớp (:2920-2933) -> ErridnLoyalty.Mst_DealerCP_CheckDB_FlagActiveNotMatched
                if (!string.Equals(parent.FlagActive, "1", StringComparison.OrdinalIgnoreCase))
                {
                    await tx.RollbackAsync();
                    return PortResult.Fail("Mst_DealerCP_CheckDB_FlagActiveNotMatched",
                        "Check.DLCPCode", strDLCPCodeParent,
                        "Check.strFlagActiveListToCheck", "1",
                        "DB.FlagActive", parent.FlagActive);
                }
            }

            // 6. Lưu 14 cột bảng Mst_DealerCP: Master.Loyalty.cs:3292-3305
            var networkId = _config["Biz_NetworkID"] ?? "1001"; // Cần cấu hình Biz_NetworkID, mặc định 1001
            var dtimeSys = DateTime.UtcNow;

            var newDealer = new Mst_DealerCP
            {
                DLCPCode = strDLCPCode,                            // :3292 DLCPCode
                NetworkID = networkId,                             // :3293 NetworkID
                DLCPCodeParent = strDLCPCodeParent,                // :3294 DLCPCodeParent
                DLCPBUCode = "X",                                  // :3295 DLCPBUCode tạm thời
                DLCPBUPattern = "X",                               // :3296 DLCPBUPattern tạm thời
                DLCPLevel = "1",                                   // :3297 DLCPLevel tạm thời
                DLCPName = strDLCPName,                            // :3298 DLCPName
                DLCPPhoneNo = strDLCPPhoneNo,                      // :3299 DLCPPhoneNo
                FlagActive = "1",                                  // :3300 FlagActive = TConst.Flag.Active ("1")
                FlagRoot = "0",                                    // :3301 FlagRoot = TConst.Flag.Inactive ("0")
                FlagBirthPoints = "0",                             // :3302 FlagBirthPoints = TConst.Flag.Inactive ("0")
                Remark = strRemark,                                // :3303 Remark
                LogLUDTimeUTC = dtimeSys,                          // :3304 LogLUDTimeUTC
                LogLUBy = "port"                                   // :3305 LogLUBy ("port" - đích chưa có session user, comment loại Đ)
            };

            await _db.Set<Mst_DealerCP>().AddAsync(newDealer);
            await _db.SaveChangesAsync();

            // 7. Post Save: Tính toán lại toàn bộ cây BU trên toàn bảng: Master.Loyalty.cs:3319 -> Mst_DealerCP_UpdBU (:2935-2971)
            await UpdBUAsync();

            // Commit Transaction (Master.Loyalty.cs:3165 TDALUtils.DBUtils.CommitSafety)
            await tx.CommitAsync();

            // Thành công trả về Data = null: gate Mst_DealerCPController.cs:507-508
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
    /// Tách phần lọc + đếm + phân trang + select của Search, GetAllActive, GetByDLCPCode và GetByOrgID
    /// tương ứng Mst_DealerCP_GetX (Master.Loyalty.cs:4209-4433).
    /// Tham số getDealer và getMap tương ứng bGet_Mst_DealerCP (:4244) và bGet_Map_DealerCP_Org (:4245).
    /// Tham số dlcpCodeEqual lọc theo DLCPCode với toán tử '=' (strWhereClause_Mst_DealerCP_By_DLCPCode :390-406).
    /// Tham số mapOrgIdEqual lọc theo OrgID trên Map_DealerCP_Org với toán tử '=' (strWhereClause_Mst_DealerCP_By_OrgID :407-419).
    /// </summary>
    private async Task<(int itemCount, List<Mst_DealerCPSearchResultItem> rows, List<Map_DealerCP_OrgResultItem> maps)> GetXAsync(
        string? keyWord,
        string? flagActiveCsv,
        long recordStart,
        long recordCount,
        bool getDealer,
        bool getMap,
        string? dlcpCodeEqual = null,
        string? mapOrgIdEqual = null)
    {
        // 1. Khởi tạo truy vấn bảng Mst_DealerCP (#tbl_Mst_DealerCP_Filter_Draft trong Master.Loyalty.cs:4271-4283)
        // Lưu ý: nguồn `select distinct identity(...)` + left join Map_DealerCP_Org; port coi là distinct theo DLCPCode.
        var query = _db.Set<Mst_DealerCP>().AsNoTracking();

        // 2. Lọc theo OrgID trên Map_DealerCP_Org: Mst_DealerCPController.cs:407-419
        // sbSql.AddWhereClause(Tbl_Map_DealerCP_Org + TblMap_DealerCP_Org.OrgID, CUtils.StrValue(data.OrgID), "=");
        // Nguồn áp dụng điều kiện này vào bảng nháp #tbl_Mst_DealerCP_Filter_Draft (Master.Loyalty.cs:4272-4283),
        // nơi Mst_DealerCP left join Map_DealerCP_Org mdcpo on mdcp.DLCPCode = mdcpo.DLCPCode.
        // Điều kiện mdcpo.OrgID = OrgID giữ lại các đại lý có ít nhất 1 dòng Map_DealerCP_Org khớp OrgID.
        // KHÔNG lọc lại ở select cuối.
        if (!string.IsNullOrWhiteSpace(mapOrgIdEqual))
        {
            query = query.Where(x => _db.Set<Map_DealerCP_Org>().Any(m =>
                m.DLCPCode == x.DLCPCode &&
                EF.Functions.Like(
                    (m.OrgID ?? "").ToLower(),
                    PortQuery.Equal(mapOrgIdEqual),
                    PortQuery.Esc)));
        }

        // 3. Lọc theo DLCPCode (toán tử '='): Mst_DealerCPController.cs:395-398
        // sbSql.AddWhereClause(Tbl_Mst_DealerCP + TblMst_DealerCP.DLCPCode, CUtils.StrValue(data.DLCPCode), "=");
        // Nguồn dùng toán tử '=' không wildcard. Collation không phân biệt hoa/thường: dùng PortQuery.Equal và Esc.
        if (!string.IsNullOrWhiteSpace(dlcpCodeEqual))
        {
            query = query.Where(x => EF.Functions.Like(
                (x.DLCPCode ?? "").ToLower(),
                PortQuery.Equal(dlcpCodeEqual),
                PortQuery.Esc));
        }

        // 4. Lọc theo KeyWord: Mst_DealerCPController.cs:369-372
        // sbSql.AddWhereClause(Tbl_Mst_DealerCP + TblMst_DealerCP.DLCPName, "%" + CUtils.StrValue(data.KeyWord) + "%", "like");
        // Nguồn lọc cột DLCPName với toán tử like '%KeyWord%'. Collation không phân biệt hoa/thường: dùng PortQuery.Contains và Esc.
        if (!string.IsNullOrWhiteSpace(keyWord))
        {
            query = query.Where(x => EF.Functions.Like(
                (x.DLCPName ?? "").ToLower(),
                PortQuery.Contains(keyWord),
                PortQuery.Esc));
        }

        // 5. Lọc theo FlagActive: Mst_DealerCPController.cs:378-386
        // Khối '=' (:374-377) đã bị comment out trong mã nguồn. Nguồn tách mảng dấu phẩy và dùng toán tử 'in'.
        // CẤM ==/.Contains() trên chuỗi trực tiếp mà hạ chữ hai phía và dùng danh sách ds.Contains.
        if (!string.IsNullOrWhiteSpace(flagActiveCsv))
        {
            var ds = flagActiveCsv.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim().ToLowerInvariant())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
            if (ds.Count > 0)
            {
                query = query.Where(x => ds.Contains((x.FlagActive ?? "").ToLower()));
            }
        }

        // 6. Đếm MyCount: Master.Loyalty.cs:4286
        // "select Count(0) MyCount from #tbl_Mst_DealerCP_Filter_Draft t"
        // ItemCount = số DLCPCode distinct sau lọc (:4286).
        var itemCount = await query.CountAsync();

        var rows = new List<Mst_DealerCPSearchResultItem>();
        var maps = new List<Map_DealerCP_OrgResultItem>();

        // 5. Phân trang trên tập draft: Master.Loyalty.cs:4289-4297
        // (t.MyIdxSeq >= @nFilterRecordStart) and (t.MyIdxSeq <= @nFilterRecordEnd)
        // Khi recordCount <= 0 hoặc recordStart < 0 -> trả về danh sách rỗng.
        if (recordCount > 0 && recordStart >= 0 && (getDealer || getMap))
        {
            var take = recordCount > int.MaxValue ? int.MaxValue : (int)recordCount;
            var skip = recordStart > int.MaxValue ? int.MaxValue : (int)recordStart;

            // Sắp xếp theo mdcp.DLCPCode asc (Master.Loyalty.cs:4282) và phân trang
            var pagedDealers = await query
                .OrderBy(x => x.DLCPCode)
                .Skip(skip)
                .Take(take)
                .ToListAsync();

            // Đánh số thứ tự 0-based MyIdxSeq trên tập draft đã sắp xếp (Master.Loyalty.cs:4273: identity(bigint, 0, 1) MyIdxSeq)
            var startSeq = recordStart;
            var pagedWithSeq = pagedDealers
                .Select((d, idx) => new { Dealer = d, MyIdxSeq = startSeq + idx })
                .ToList();

            var pagedCodes = pagedWithSeq.Select(x => x.Dealer.DLCPCode).ToList();

            if (pagedCodes.Count > 0)
            {
                // Tra cứu Map_DealerCP_Org (mdcpo) dùng chung cho cả khối Mst_DealerCP và khối Map_DealerCP_Org
                var dealerOrgMaps = await _db.Set<Map_DealerCP_Org>()
                    .AsNoTracking()
                    .Where(m => pagedCodes.Contains(m.DLCPCode))
                    .ToListAsync();

                // 6. Câu select Mst_DealerCP (:4328-4348) khi bGet_Mst_DealerCP = true:
                // inner join Mst_DealerCP mdcp on t.DLCPCode = mdcp.DLCPCode
                // left join Mst_DealerCP f on t.DLCPCodeParent = f.DLCPCode -> mdcp_DLCPNameParent
                // left join Map_DealerCP_Org mdcpo on t.DLCPCode = mdcpo.DLCPCode
                // left join Mst_Org mo on mdcpo.OrgID = mo.OrgID -> mo_OrgID
                // left join Mst_NNT mnnt on mo.OrgID = mnnt.OrgID -> mnnt_MST, mnnt_NNTFullName, mnnt_ContactEmail
                // Mỗi dòng kết quả = 1 cặp (đại lý, map) — đại lý có 2 map ra 2 dòng; đại lý không có map ra 1 dòng với enrich null.
                // Bỏ dòng trùng hoàn toàn (select distinct). Sắp theo MyIdxSeq asc (:4347).
                if (getDealer)
                {
                    // Tra cứu đại lý cha (f)
                    var parentCodes = pagedWithSeq
                        .Select(x => x.Dealer.DLCPCodeParent)
                        .Where(c => !string.IsNullOrEmpty(c))
                        .Distinct()
                        .ToList();
                    var parentDict = parentCodes.Count > 0
                        ? await _db.Set<Mst_DealerCP>()
                            .AsNoTracking()
                            .Where(p => parentCodes.Contains(p.DLCPCode))
                            .ToDictionaryAsync(p => p.DLCPCode)
                        : new Dictionary<string, Mst_DealerCP>();

                    // Tra cứu Mst_Org (mo)
                    var orgIds = dealerOrgMaps
                        .Select(m => m.OrgID)
                        .Where(o => !string.IsNullOrEmpty(o))
                        .Distinct()
                        .ToList();
                    var orgDict = orgIds.Count > 0
                        ? await _db.Set<Mst_Org>()
                            .AsNoTracking()
                            .Where(o => orgIds.Contains(o.OrgID))
                            .ToDictionaryAsync(o => o.OrgID)
                        : new Dictionary<string, Mst_Org>();

                    // Tra cứu Mst_NNT (mnnt)
                    var nnts = orgIds.Count > 0
                        ? await _db.Set<Mst_NNT>()
                            .AsNoTracking()
                            .Where(n => n.OrgID != null && orgIds.Contains(n.OrgID))
                            .ToListAsync()
                        : new List<Mst_NNT>();
                    var nntLookup = nnts.ToLookup(n => n.OrgID!);

                    foreach (var item in pagedWithSeq)
                    {
                        var dealer = item.Dealer;
                        var parentName = (dealer.DLCPCodeParent != null && parentDict.TryGetValue(dealer.DLCPCodeParent, out var parent))
                            ? parent.DLCPName
                            : null;

                        var itemMaps = dealerOrgMaps.Where(m => m.DLCPCode == dealer.DLCPCode).OrderBy(m => m.OrgID).ToList();
                        if (itemMaps.Count == 0)
                        {
                            // Đại lý không gắn với Org nào -> 1 dòng, các cột liên kết Org/NNT để null
                            rows.Add(new Mst_DealerCPSearchResultItem(
                                item.MyIdxSeq,
                                dealer.DLCPCode,
                                dealer.NetworkID,
                                dealer.DLCPCodeParent,
                                dealer.DLCPBUCode,
                                dealer.DLCPBUPattern,
                                dealer.DLCPLevel,
                                dealer.DLCPName,
                                dealer.DLCPPhoneNo,
                                dealer.FlagRoot,
                                dealer.FlagActive,
                                dealer.FlagBirthPoints,
                                dealer.Remark,
                                dealer.LogLUDTimeUTC,
                                dealer.LogLUBy,
                                null,
                                parentName,
                                null,
                                null,
                                null
                            ));
                        }
                        else
                        {
                            // Đại lý có map Org -> mỗi cặp đại lý - map sinh 1 dòng
                            foreach (var map in itemMaps)
                            {
                                var mo = (map.OrgID != null && orgDict.TryGetValue(map.OrgID, out var o)) ? o : null;
                                var moOrgId = mo?.OrgID;
                                var matchingNnts = (moOrgId != null && nntLookup.Contains(moOrgId))
                                    ? nntLookup[moOrgId].ToList()
                                    : new List<Mst_NNT>();

                                if (matchingNnts.Count == 0)
                                {
                                    rows.Add(new Mst_DealerCPSearchResultItem(
                                        item.MyIdxSeq,
                                        dealer.DLCPCode,
                                        dealer.NetworkID,
                                        dealer.DLCPCodeParent,
                                        dealer.DLCPBUCode,
                                        dealer.DLCPBUPattern,
                                        dealer.DLCPLevel,
                                        dealer.DLCPName,
                                        dealer.DLCPPhoneNo,
                                        dealer.FlagRoot,
                                        dealer.FlagActive,
                                        dealer.FlagBirthPoints,
                                        dealer.Remark,
                                        dealer.LogLUDTimeUTC,
                                        dealer.LogLUBy,
                                        moOrgId,
                                        parentName,
                                        null,
                                        null,
                                        null
                                    ));
                                }
                                else
                                {
                                    foreach (var nnt in matchingNnts)
                                    {
                                        rows.Add(new Mst_DealerCPSearchResultItem(
                                            item.MyIdxSeq,
                                            dealer.DLCPCode,
                                            dealer.NetworkID,
                                            dealer.DLCPCodeParent,
                                            dealer.DLCPBUCode,
                                            dealer.DLCPBUPattern,
                                            dealer.DLCPLevel,
                                            dealer.DLCPName,
                                            dealer.DLCPPhoneNo,
                                            dealer.FlagRoot,
                                            dealer.FlagActive,
                                            dealer.FlagBirthPoints,
                                            dealer.Remark,
                                            dealer.LogLUDTimeUTC,
                                            dealer.LogLUBy,
                                            moOrgId,
                                            parentName,
                                            nnt.MST,
                                            nnt.NNTFullName,
                                            nnt.ContactEmail
                                        ));
                                    }
                                }
                            }
                        }
                    }

                    // Bỏ dòng trùng hoàn toàn (select distinct nguồn) và sắp theo MyIdxSeq asc (:4347)
                    rows = rows.Distinct().OrderBy(r => r.MyIdxSeq).ToList();
                }

                // 7. Câu select Map_DealerCP_Org (:4354-4370) khi bGet_Map_DealerCP_Org = true:
                // inner join giữa #tbl_Mst_DealerCP_Filter t và Map_DealerCP_Org mdcpo on t.DLCPCode = mdcpo.DLCPCode
                // select distinct t.MyIdxSeq, mdcpo.* order by t.MyIdxSeq asc (:4360-4366)
                if (getMap)
                {
                    var seqDict = pagedWithSeq.ToDictionary(x => x.Dealer.DLCPCode, x => x.MyIdxSeq);

                    maps = dealerOrgMaps
                        .Where(m => seqDict.ContainsKey(m.DLCPCode))
                        .Select(m => new Map_DealerCP_OrgResultItem(
                            seqDict[m.DLCPCode],
                            m.DLCPCode,
                            m.OrgID,
                            m.NetworkID,
                            m.FlagActive,
                            m.LogLUDTimeUTC,
                            m.LogLUBy
                        ))
                        .Distinct()
                        .OrderBy(m => m.MyIdxSeq)
                        .ThenBy(m => m.OrgID)
                        .ToList();
                }
            }
        }

        return (itemCount, rows, maps);
    }

    /// <summary>
    /// Port 1:1 Mst_DealerCP_UpdBU (Master.Loyalty.cs:2935-2971).
    /// Cập nhật lại cây BU (DLCPBUCode, DLCPBUPattern, DLCPLevel) trên toàn bảng Mst_DealerCP.
    /// Thuật toán nguồn:
    /// (1) Gốc 'HO' viết cứng (:2938-2950): DLCPBUCode='HO', DLCPBUPattern='HO%', DLCPLevel=1.
    /// (2) Lặp 7 vòng (@nDeepDealer 0..6 :2952-2968): mỗi dòng DLCPCode != 'HO':
    ///     DLCPBUCode = IsNull(cha.DLCPBUCode + '.', '') + DLCPCode;
    ///     DLCPBUPattern = IsNull(cha.DLCPBUCode + '.', '') + DLCPCode + '%';
    ///     DLCPLevel = IsNull(cha.DLCPLevel, 0) + 1 (cha = left join theo DLCPCodeParent).
    /// Thực hiện trong bộ nhớ trên danh sách entity đã tải rồi SaveChangesAsync.
    /// </summary>
    private async Task UpdBUAsync()
    {
        var allDealers = await _db.Set<Mst_DealerCP>().ToListAsync();
        if (allDealers.Count == 0) return;

        // Bước 1: Dòng có DLCPCode = 'HO' (gốc viết cứng nguồn :2938-2950)
        foreach (var d in allDealers)
        {
            if (string.Equals(d.DLCPCode, "HO", StringComparison.OrdinalIgnoreCase))
            {
                d.DLCPBUCode = "HO";
                d.DLCPBUPattern = "HO%";
                d.DLCPLevel = "1";
            }
        }

        // Bước 2: Lặp 7 vòng (nguồn @nDeepDealer <= 6 lặp 7 lần :2952-2968)
        for (int round = 0; round < 7; round++)
        {
            // Lấy snapshot giá trị BUCode và Level của các đại lý tại đầu mỗi vòng lặp
            var parentMap = allDealers.ToDictionary(
                d => d.DLCPCode,
                d => (d.DLCPBUCode, d.DLCPLevel),
                StringComparer.OrdinalIgnoreCase);

            foreach (var t in allDealers)
            {
                if (string.Equals(t.DLCPCode, "HO", StringComparison.OrdinalIgnoreCase))
                    continue;

                string? parentBU = null;
                int parentLevel = 0;

                if (!string.IsNullOrEmpty(t.DLCPCodeParent) && parentMap.TryGetValue(t.DLCPCodeParent, out var pInfo))
                {
                    parentBU = pInfo.DLCPBUCode;
                    if (int.TryParse(pInfo.DLCPLevel, out var pl))
                        parentLevel = pl;
                }

                var prefix = !string.IsNullOrEmpty(parentBU) ? parentBU + "." : "";
                t.DLCPBUCode = prefix + t.DLCPCode;
                t.DLCPBUPattern = prefix + t.DLCPCode + "%";
                t.DLCPLevel = (parentLevel + 1).ToString();
            }
        }

        await _db.SaveChangesAsync();
    }
}

/// <summary>
/// Request model cho action CreateOld — khớp tham số tại gate Mst_DealerCPController.cs:436,
/// API MstDealerCPController.cs:93 và biz Master.Loyalty.cs:3214-3218.
/// Chỉ nhận 5 trường (DLCPCode, DLCPCodeParent, DLCPName, DLCPPhoneNo, Remark); các trường khác bỏ qua như nguồn.
/// </summary>
public class Mst_DealerCPCreateOldRequest
{
    public string? DLCPCode { get; set; }
    public string? DLCPCodeParent { get; set; }
    public string? DLCPName { get; set; }
    public string? DLCPPhoneNo { get; set; }
    public string? Remark { get; set; }
}

/// <summary>
/// Request model cho action Search — khớp tham số tại Mst_DealerCPController.cs:26
/// </summary>
public class Mst_DealerCPSearchRequest
{
    public string? KeyWord { get; set; }
    public string? FlagActive { get; set; }
    public int Ft_PageIndex { get; set; } = 0;
    public int Ft_PageSize { get; set; } = 10;
}

/// <summary>
/// Request model cho action GetByDLCPCode — khớp tham số tại Mst_DealerCPController.cs:231
/// </summary>
public class Mst_DealerCPGetByDLCPCodeRequest
{
    public string? DLCPCode { get; set; }
}

/// <summary>
/// Request model cho action GetByOrgID — khớp tham số tại Mst_DealerCPController.cs:297
/// </summary>
public class Mst_DealerCPGetByOrgIDRequest
{
    public string? OrgID { get; set; }
}

/// <summary>
/// Model từng dòng dữ liệu DataList trả về: MyIdxSeq (0-based) + 14 cột Mst_DealerCP + 5 cột enrich
/// (Master.Loyalty.cs:4328-4335, Mst_DealerCP.cs:11-31).
/// Dùng record để tự động hỗ trợ so sánh giá trị và khử trùng lặp (select distinct).
/// </summary>
public record Mst_DealerCPSearchResultItem(
    /// <summary>Số thứ tự 0-based trên toàn tập đã sắp xếp (Master.Loyalty.cs:4273, 4329)</summary>
    long MyIdxSeq,

    /// <summary>Mã đại lý - Mst_DealerCP.cs:11</summary>
    string DLCPCode,

    /// <summary>Mã mạng lưới - Mst_DealerCP.cs:12</summary>
    string? NetworkID,

    /// <summary>Mã đại lý cha - Mst_DealerCP.cs:13</summary>
    string? DLCPCodeParent,

    /// <summary>Mã đơn vị kinh doanh BU - Mst_DealerCP.cs:14</summary>
    string? DLCPBUCode,

    /// <summary>Mẫu đơn vị kinh doanh BU - Mst_DealerCP.cs:15</summary>
    string? DLCPBUPattern,

    /// <summary>Cấp bậc đại lý - Mst_DealerCP.cs:16</summary>
    string? DLCPLevel,

    /// <summary>Tên đại lý - Mst_DealerCP.cs:17</summary>
    string? DLCPName,

    /// <summary>Số điện thoại đại lý - Mst_DealerCP.cs:18</summary>
    string? DLCPPhoneNo,

    /// <summary>Cờ đại lý gốc - Mst_DealerCP.cs:19</summary>
    string? FlagRoot,

    /// <summary>Trạng thái kích hoạt - Mst_DealerCP.cs:20</summary>
    string? FlagActive,

    /// <summary>Cờ tặng điểm sinh nhật - Mst_DealerCP.cs:21</summary>
    string? FlagBirthPoints,

    /// <summary>Ghi chú đại lý - Mst_DealerCP.cs:22</summary>
    string? Remark,

    /// <summary>Thời gian cập nhật UTC - Mst_DealerCP.cs:23</summary>
    DateTime? LogLUDTimeUTC,

    /// <summary>Người dùng thực hiện cập nhật - Mst_DealerCP.cs:24</summary>
    string? LogLUBy,

    /// <summary>Mã tổ chức liên kết từ bảng Mst_Org qua Map_DealerCP_Org (Master.Loyalty.cs:4331 mo.OrgID mo_OrgID)</summary>
    string? mo_OrgID,

    /// <summary>Tên đại lý cha từ bảng Mst_DealerCP f (Master.Loyalty.cs:4332 f.DLCPName mdcp_DLCPNameParent)</summary>
    string? mdcp_DLCPNameParent,

    /// <summary>Mã số thuế từ bảng Mst_NNT qua Mst_Org (Master.Loyalty.cs:4333 mnnt.MST mnnt_MST)</summary>
    string? mnnt_MST,

    /// <summary>Tên người nộp thuế từ bảng Mst_NNT (Master.Loyalty.cs:4334 mnnt.NNTFullName mnnt_NNTFullName)</summary>
    string? mnnt_NNTFullName,

    /// <summary>Email liên hệ người nộp thuế từ bảng Mst_NNT (Master.Loyalty.cs:4335 mnnt.ContactEmail mnnt_ContactEmail)</summary>
    string? mnnt_ContactEmail
);

/// <summary>
/// Model từng dòng dữ liệu bảng Map_DealerCP_Org trả về trong Mst_DealerCP_GetX:
/// MyIdxSeq + 6 cột Map_DealerCP_Org (Master.Loyalty.cs:4359-4366, Map_DealerCP_Org.cs:11-16).
/// Dùng record để tự động hỗ trợ so sánh giá trị và khử trùng lặp (select distinct).
/// </summary>
public record Map_DealerCP_OrgResultItem(
    /// <summary>Số thứ tự 0-based của đại lý tương ứng (Master.Loyalty.cs:4273, 4361)</summary>
    long MyIdxSeq,

    /// <summary>Mã đại lý - Map_DealerCP_Org.cs:11</summary>
    string DLCPCode,

    /// <summary>Mã tổ chức liên kết - Map_DealerCP_Org.cs:12</summary>
    string OrgID,

    /// <summary>Mã mạng lưới - Map_DealerCP_Org.cs:13</summary>
    string? NetworkID,

    /// <summary>Trạng thái kích hoạt - Map_DealerCP_Org.cs:14</summary>
    string? FlagActive,

    /// <summary>Thời gian cập nhật UTC - Map_DealerCP_Org.cs:15</summary>
    DateTime? LogLUDTimeUTC,

    /// <summary>Người dùng thực hiện cập nhật - Map_DealerCP_Org.cs:16</summary>
    string? LogLUBy
);
