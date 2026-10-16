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
// PORT 1:1 — màn danh mục quận/huyện (Mst_District) của dự án nguồn 2022.7.Loyalty (#139, #140, #141, #142, #143, #144, #145, #146, #147, #148).
//
// Nguồn controller (tầng gate):
//   Mst_DistrictController.cs:31-111   Search              -> WA_Mst_District_Get
//   Mst_DistrictController.cs:113-177  GetDetail           -> WA_Mst_District_Get
//   Mst_DistrictController.cs:179-250  GetByProvinceCode   -> WA_Mst_District_Get
//   Mst_DistrictController.cs:252-315  GetAllActive        -> WA_Mst_District_Get
//   Mst_DistrictController.cs:316-376  Create              -> WA_Mst_District_Create
//   Mst_DistrictController.cs:380-447  Update              -> WA_Mst_District_Update
//   Mst_DistrictController.cs:449-513  Delete              -> WA_Mst_District_Delete (#145)
//   Mst_DistrictController.cs:517-673  Import              -> WA_Mst_District_Create (#146)
//   Mst_DistrictController.cs:678-756  Export              -> WA_Mst_District_Get (#147)
//   Mst_DistrictController.cs:759-791  ExportTemplate      -> không gọi service (#148)
//   Mst_DistrictController.cs:818-833  strWhereClause_Mst_District (mệnh đề lọc Search, GetAllActive, Export)
//   Mst_DistrictController.cs:835-863  strWhereClause_Mst_District_Detail (mệnh đề lọc GetDetail, GetByProvinceCode)
// Nguồn API:
//   MstDistrictController.cs:20-91     WA_Mst_District_Get
//   MstDistrictController.cs:94-165    WA_Mst_District_Create
//   MstDistrictController.cs:168-239   WA_Mst_District_Update
//   MstDistrictController.cs:242-313   WA_Mst_District_Delete
// Nguồn biz:
//   Master.cs:2584-2708                WAS_Mst_District_Get
//   Master.cs:1801-2023                Mst_District_Get (SQL excerpt :1883-1929)
//   Master.cs:2710-2794                WAS_Mst_District_Create
//   Master.cs:2024-2193                Mst_District_Create -> Mst_District_CheckDB (Master.cs:1729-1799) -> Mst_Province_CheckDB (Master.cs:27-91)
//   Master.cs:2796-2883                WAS_Mst_District_Update
//   Master.cs:2194-2359                Mst_District_Update -> Mst_District_CheckDB (Master.cs:1729-1799) -> Mst_Province_CheckDB (Master.cs:27-91)
//   Master.cs:2885-2968                WAS_Mst_District_Delete
//   Master.cs:2360-2475                Mst_District_Delete -> Mst_District_CheckDB (Master.cs:1729-1799)
//
// GIỮ ĐÚNG NGUỒN:
//   * Endpoint là POST (nguồn [AcceptVerbs("POST")]).
//   * GetDetail: chỉ truy vấn khi CẢ HAI ProvinceCode và DistrictCode khác rỗng (:133-134); thiếu 1 trong 2 -> Data = null (:132, :164).
//   * GetDetail: lọc ProvinceCode = và DistrictCode = (:840-847); dòng FlagActive bị comment ở nguồn (:848-851) nên KHÔNG lọc cờ hoạt động (huyện '0' vẫn lấy được).
//   * GetDetail: Ft_RecordStart = "0", Ft_RecordCount = "1" (:151-152) -> trả Lst_Mst_District[0] hoặc null (:158-161).
//   * GetByProvinceCode: nếu ProvinceCode rỗng -> Data = { Lst_Mst_District = null } (:198, :234-237).
//   * GetByProvinceCode: nguồn gán FlagActive = "1" nếu rỗng (:201-203), nhưng strWhereClause_Mst_District_Detail (:848-851) bị comment nên KHÔNG lọc cờ hoạt động (huyện '0' vẫn lấy được).
//   * GetByProvinceCode: Ft_RecordStart = "0", Ft_RecordCount = "123456000" (:221-222) -> trả Lst_Mst_District = rows.Count > 0 ? rows : null (:228-237).
//   * GetAllActive: không body, Ft_RecordStart = "0", Ft_RecordCount = "123456000" (:287-288); gọi strWhereClause_Mst_District với FlagActive = "1" (:272-275) -> lọc FlagActive = '1' CÓ TÁC DỤNG (khác GetByProvinceCode); trả Data = lst_Mst_District (danh sách trần, null nếu rỗng — :298-299, KHÁC Mst_Province bọc { Lst_Mst_Province }).
//   * Create: kiểm tra thứ tự: (1) mã rỗng -> InvalidDistrictCode, (2) CheckDB trùng theo CẶP (ProvinceCode, DistrictCode) -> DistrictExist,
//     (3) Province_CheckDB: không thấy -> ProvinceNotFound, cờ không '1' -> FlagActiveNotMatched,
//     (4) tên rỗng -> InvalidDistrictName; mã lưu chữ hoa qua StdParam, tên chỉ Trim (không Normalize);
//     NetworkID cấu hình Biz_NetworkID, mặc định 1001; FlagActive "1", LogLUBy "port", LogLUDTimeUTC = UtcNow;
//     body rỗng -> ServiceException ("strJson null") -> Fail("Mst_District_Create").
//   * Update: Ft_Cols_Upd gồm DistrictName và FlagActive (:409-413, dòng ProvinceCode bị comment -> KHÔNG đổi tỉnh);
//     kiểm tra thứ tự: (1) body rỗng -> Fail("Mst_District_Update"), (2) CheckDB(Flag.Yes) không thấy theo cặp (ProvinceCode, DistrictCode) -> DistrictNotFound,
//     (3) Province_CheckDB: không thấy -> ProvinceNotFound, cờ không '1' -> FlagActiveNotMatched, (4) bUpd_DistrictName && tên rỗng -> InvalidDistrictName;
//     mã qua StdParam (Trim + ToUpper), tên chỉ Trim; StdFlag: thiếu/không phải "1" -> "0" (cảnh báo: thiếu FlagActive cập nhật về "0");
//     LogLUBy "port", LogLUDTimeUTC = UtcNow; 1 transaction, lỗi -> rollback; thành công Data = null (:431-432).
//   * Delete (#145): Gate kiểm tra if (!IsNullOrEmpty(DistrictCode)) lồng if (!IsNullOrEmpty(ProvinceCode)) (:468-496):
//     (1) Thiếu DistrictCode -> ném ServiceException("invalid_districtcode_provincecode", "invalid_districtcode_provincecode") (:497-500) -> 400 ErridnLoyalty.invalid_districtcode_provincecode.
//     (2) Có DistrictCode nhưng thiếu ProvinceCode -> KHÔNG có nhánh else -> không gọi xóa, không ném lỗi, KHÔNG SetSuccess -> trả về Success = false (giá trị khởi tạo), Data = null (HTTP 200).
//     (3) Đủ DistrictCode và ProvinceCode: 1 transaction (_cf.db.BeginTransaction :2391, CommitSafety :2438, RollbackSafety :2446, :2461);
//         chuẩn hoá mã qua StdParam (Trim + ToUpper); Mst_District_CheckDB(strProvinceCode, objDistrictCode, Flag.Yes, "") (:2412-2421, :1729-1799)
//         kiểm tra tồn tại theo cặp (ProvinceCode, DistrictCode), không tìm thấy -> ném Mst_District_CheckDB_DistrictNotFound -> 400 ErridnLoyalty.Mst_District_CheckDB_DistrictNotFound;
//         FK duy nhất tới Mst_District là Mst_Agent (20191203.z11.CreateTable.Mst_Agent.sql:27) CHƯA port -> GAP, không giả lập FK;
//         xoá dòng Mst_District (:2427-2434); thành công trả về Success = true, Data = null (:493-494).
//   * Export (#147): nhận { OrgID, KeyWord, FlagActive } (:678); OrgID gate nhận (:702) nhưng strWhereClause_Mst_District (:818-833) KHÔNG sử dụng để lọc;
//     Gọi GetXAsync(KeyWord, FlagActive, 0, 123456000) (:717-718);
//     Nếu danh sách rỗng -> throw ServiceException("Không có dữ liệu") (:742) -> trả 400 ErridnLoyalty.Mst_District_Export, Message = "Không có dữ liệu";
//     Nếu có dữ liệu -> từ điển cột GetImportDicColums() (:729, :795-804: dùng từ điển Import cho Export là của nguồn) gồm 4 cột:
//     DistrictCode ("Mã quận/huyện (*)"), ProvinceCode ("Mã tỉnh/thành (*)"), DistrictName ("Tên quận/huyện"), FlagActive ("Trạng thái");
//     Sinh đường dẫn file qua PortExcel.GenExcelExportFilePath(webRoot, "Mst_District") (:731), sheet "Mst_District" (:732);
//     Ghi file qua PortExcel.ExportToExcelFromList; trả về Success = true, Data = { Url = baseAddress + virtualPath } (:733-737).
//   * ExportTemplate (#148): nguồn gate Mst_DistrictController.cs:759-791: không kiểm quyền, không đọc DB,
//     danh sách rỗng List<Mst_District>, từ điển cột GetImportDicColumsTemplate() gồm 3 cột (DistrictCode, ProvinceCode, DistrictName),
//     xuất ra sheet "Mst_District", trả về Success = true, Data = { Url = PortExcel.GetRequestBaseAddress(Request) + virtualPath } (HTTP 200).
//   * Lọc KeyWord: like '%x%' trên DistrictName (:823-826).
//   * Lọc FlagActive: toán tử = (:827-830) — KHÁC Province dùng IN ('a','b').
//   * OrgID: gate nhận (:31, :61, :125, :149, :264, :285) nhưng strWhereClause_Mst_District (:818-833) và strWhereClause_Mst_District_Detail (:835-863) KHÔNG lọc OrgID.
//   * Chia trang theo md.DistrictCode asc (:1890), select cuối sắp theo t.MyIdxSeq asc (:1929).
//   * Select cuối: t.MyIdxSeq, md.* (:1923-1929) — KHÔNG join Mst_Province.
//   * MyIdxSeq 0-based identity (:1884).
//   * PageSize = 0 -> DataList rỗng, PageCount = 0 (gate :86-94).
//
// Bỏ qua có chủ đích (loại Đ):
//   - CheckGatewayAuthentication: gate Mst_DistrictController.cs:40, 122, 188, 335, 458, 688; API MstDistrictController.cs:42-49, 117-122, 265-270
//   - CheckAuthorize: gate Mst_DistrictController.cs:41, 123, 189, 336, 459, 689 (hệ thống đích chưa có hệ phân quyền)
//   - Nguồn gọi bằng tài khoản hệ thống viết cứng tại MstDistrictController.cs:268-269 — không chép giá trị
//   - Ghi RequestLog/ReturnLog: Master.cs:1841-1849, 2063-2070, 2183-2190, 2394-2401, 2465-2472, 2700-2705, 2960-2966
//   - Sys_User_CheckAuthorize: Master.cs:2623-2636
//   - LogLUBy gán cố định "port" do hệ thống đích chưa có session người dùng (Master.cs:2143)
//   - Biz_NetworkID lấy từ cấu hình IConfiguration, mặc định "1001" nếu thiếu (Common.cs:39, 124; Master.cs:2138)
// ============================================================================================

/// <summary>Port 1:1 <c>Mst_DistrictSearch</c> — tham số lọc + phân trang của màn (Mst_DistrictController.cs:31, 58-63).</summary>
public class MstDistrictSearchReq
{
    public string? OrgID { get; set; }               // Tổ chức — gate nhận (:31, :61) nhưng strWhereClause_Mst_District (:818-833) KHÔNG lọc OrgID
    public string? KeyWord { get; set; }             // Từ khoá tìm theo tên quận/huyện (DistrictName) — :60, :823-826
    public string? FlagActive { get; set; }          // Cờ hoạt động — :62, :827-830 (toán tử =)
    public int Ft_PageIndex { get; set; } = 0;       // Trang (0-based) — :31, :52
    public int Ft_PageSize { get; set; } = 10;       // Số dòng/trang — :31, :53
}

/// <summary>Port 1:1 <c>GetDetail</c> — body nhận ProvinceCode, DistrictCode (Mst_DistrictController.cs:113-177).</summary>
public class MstDistrictGetDetailReq
{
    public string? ProvinceCode { get; set; }        // Mã tỉnh/thành — :113, :137
    public string? DistrictCode { get; set; }        // Mã quận/huyện — :113, :138
}

/// <summary>Port 1:1 <c>GetByProvinceCode</c> — body nhận ProvinceCode, FlagActive (Mst_DistrictController.cs:179-250).</summary>
public class MstDistrictGetByProvinceCodeReq
{
    public string? ProvinceCode { get; set; }        // Mã tỉnh/thành — :179, :199, :207
    public string? FlagActive { get; set; }          // Cờ hoạt động — :179, :201-208 (chú ý: nguồn gán "1" nếu rỗng nhưng strWhereClause_Mst_District_Detail không lọc)
}

/// <summary>Port 1:1 <c>Create</c> — body nhận DistrictCode, ProvinceCode, DistrictName (Mst_DistrictController.cs:316-376).</summary>
public class MstDistrictCreateReq
{
    public string? DistrictCode { get; set; }        // Mã quận/huyện — Master.cs:2076
    public string? ProvinceCode { get; set; }        // Mã tỉnh/thành — Master.cs:2077
    public string? DistrictName { get; set; }        // Tên quận/huyện — Master.cs:2078
}

/// <summary>Port 1:1 <c>Update</c> — body nhận ProvinceCode, DistrictCode, DistrictName, FlagActive (Mst_DistrictController.cs:380-447).</summary>
public class MstDistrictUpdateReq
{
    public string? ProvinceCode { get; set; }        // Mã tỉnh/thành — PK ghép, Master.cs:2203
    public string? DistrictCode { get; set; }        // Mã quận/huyện — PK ghép, Master.cs:2202
    public string? DistrictName { get; set; }        // Tên quận/huyện — Master.cs:2204
    public object? FlagActive { get; set; }          // Cờ hoạt động — Master.cs:2205
}

/// <summary>Port 1:1 <c>Delete</c> — body nhận DistrictCode, ProvinceCode (Mst_DistrictController.cs:449-513).</summary>
public class MstDistrictDeleteReq
{
    public string? DistrictCode { get; set; }        // Mã quận/huyện — Mst_DistrictController.cs:449
    public string? ProvinceCode { get; set; }        // Mã tỉnh/thành — Mst_DistrictController.cs:449
}

/// <summary>Port 1:1 <c>Export</c> — body nhận OrgID, KeyWord, FlagActive (Mst_DistrictController.cs:678, 699-704).</summary>
public class MstDistrictExportReq
{
    public string? OrgID { get; set; }               // Tổ chức — gate nhận (:678, :702) nhưng strWhereClause_Mst_District (:818-833) KHÔNG lọc OrgID
    public string? KeyWord { get; set; }             // Từ khoá tìm theo tên quận/huyện (DistrictName) — :678, :701, :823-826
    public string? FlagActive { get; set; }          // Cờ hoạt động — :678, :703, :827-830 (toán tử =)
}

[ApiController]
[Route("api/mst_district")]
[Produces("application/json")]
public class Mst_DistrictControllerPort(AppDbContext db, IConfiguration config, IWebHostEnvironment env) : ControllerBase
{
    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody] MstDistrictSearchReq? req)
    {
        var r = req ?? new MstDistrictSearchReq();
        var pageIndex = r.Ft_PageIndex;
        var pageSize = r.Ft_PageSize;

        // Search gọi GetXAsync với start = PageIndex * PageSize, count = PageSize (giữ nguyên hành vi cũ nguồn :50-51, :75-76).
        var start = (long)pageIndex * pageSize;
        // OrgID nhận ở request nhưng trong strWhereClause_Mst_District (:818-833) KHÔNG lọc OrgID; districtCodeEqual và provinceCodeEqual truyền null.
        var (itemCount, rows) = await GetXAsync(r.KeyWord, r.FlagActive, start, pageSize, null, null);

        // PageSize=0 -> DataList rỗng (nguồn: nFilterRecordEnd = start-1, SQL biz + gate :86-94).
        // PageCount = 0 khi DataList rỗng, ngược lại ceil(ItemCount / ps) với ps = PageSize (PageSize=0 thì 10) — gate :86-94.
        var ps = pageSize == 0 ? 10 : pageSize;
        var pageCount = rows.Count == 0 ? 0 : (itemCount % ps == 0 ? itemCount / ps : itemCount / ps + 1);

        return Ok(new { Success = true, Data = new PageInfo<object>(rows, itemCount, pageCount, pageIndex, pageSize) });
    }

    // ============================================================================================
    // GetDetail — nguồn Mst_DistrictController.cs:113-177.
    // Mệnh đề lọc: strWhereClause_Mst_District_Detail (Mst_DistrictController.cs:835-863).
    // Gate: nếu ProvinceCode rỗng HOẶC DistrictCode rỗng thì KHÔNG truy vấn (:133-134),
    //       objMst_District giữ null (:132) => Data = null (:164).
    // Ngược lại gọi strWhereClause_Mst_District_Detail (:835-863) lọc:
    //   - ProvinceCode = (toán tử = :840-843)
    //   - DistrictCode = (toán tử = :844-847)
    //   - Chú ý: dòng FlagActive bị comment ở nguồn (:848-851), và GetDetail không truyền FlagActive (:135-139)
    //     => KHÔNG lọc cờ hoạt động (bản ghi có FlagActive '0' vẫn lấy được).
    //   - Ft_RecordStart = "0", Ft_RecordCount = "1" (:151-152) => lấy tối đa 1 dòng.
    // Trả Lst_Mst_District[0] hoặc null nếu không có bản ghi (:158-161).
    // ============================================================================================
    [HttpPost("getdetail")]
    public async Task<IActionResult> GetDetail([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] MstDistrictGetDetailReq? req)
    {
        // 1 trong 2 rỗng -> Data null, không truy vấn CSDL: gate :133, :164
        if (string.IsNullOrWhiteSpace(req?.ProvinceCode) || string.IsNullOrWhiteSpace(req?.DistrictCode))
        {
            return Ok(new { Success = true, Data = (object?)null });
        }

        // Lấy 1 dòng theo DistrictCode và ProvinceCode (Ft_RecordStart = "0", Ft_RecordCount = "1" — gate :151-152).
        // Dòng FlagActive bị comment ở nguồn (:848-851) nên không truyền flagActive (truyền null).
        var (_, rows) = await GetXAsync(null, null, 0, 1, districtCodeEqual: req.DistrictCode, provinceCodeEqual: req.ProvinceCode);

        // Nguồn trả Lst_Mst_District[0] khi có dòng, ngược lại null (:158-161).
        return Ok(new { Success = true, Data = rows.Count > 0 ? rows[0] : null });
    }

    // ============================================================================================
    // GetByProvinceCode — nguồn Mst_DistrictController.cs:179-250 (#141).
    // Mệnh đề lọc: strWhereClause_Mst_District_Detail (Mst_DistrictController.cs:835-863).
    // Gate:
    //   - Nếu ProvinceCode rỗng (null hoặc "") -> bỏ qua khối if (:199), listMst_District = null (:198)
    //     => objJsonResultEntry.Data = new { Lst_Mst_District = null } (:234-237).
    //   - Nếu ProvinceCode không rỗng:
    //     * Nguồn gán: if (IsNullOrEmpty(FlagActive)) FlagActive = "1" (:201-203).
    //     * Gọi strWhereClause_Mst_District_Detail với { ProvinceCode, FlagActive } (:205-209).
    //     * Chú ý cảnh báo: Trong strWhereClause_Mst_District_Detail (:848-851), dòng lọc FlagActive bị comment
    //       nên FlagActive truyền vào KHÔNG có tác dụng lọc -> trả MỌI huyện của tỉnh, kể cả FlagActive '0'.
    //     * Ft_RecordStart = "0", Ft_RecordCount = "123456000" (:221-222) -> lấy toàn bộ.
    //     * Trả Data = new { Lst_Mst_District = rows.Count > 0 ? rows : null } (:228-237).
    // ============================================================================================
    [HttpPost("getbyprovincecode")]
    public async Task<IActionResult> GetByProvinceCode([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] MstDistrictGetByProvinceCodeReq? req)
    {
        // Nguồn Mst_DistrictController.cs:198-199: Nếu ProvinceCode rỗng thì không truy vấn CSDL,
        // listMst_District giữ null (:198), Data được gán ngoài khối if (:234-237) -> { Lst_Mst_District = null }.
        if (string.IsNullOrWhiteSpace(req?.ProvinceCode))
        {
            return Ok(new { Success = true, Data = new { Lst_Mst_District = (List<object>?)null } });
        }

        // Gọi GetXAsync với Ft_RecordStart = "0", Ft_RecordCount = "123456000" (gate :221-222),
        // provinceCodeEqual = req.ProvinceCode.
        // Chú ý cảnh báo: Nguồn gán FlagActive = "1" nếu rỗng (:201-203) nhưng strWhereClause_Mst_District_Detail (:848-851)
        // không lọc FlagActive, do đó KHÔNG truyền FlagActive vào GetXAsync (truyền null) để trả mọi huyện kể cả cờ '0'.
        var (_, rows) = await GetXAsync(null, null, 0, 123456000, districtCodeEqual: null, provinceCodeEqual: req.ProvinceCode);

        // Nguồn Mst_DistrictController.cs:228-237: Data = new { Lst_Mst_District = rows.Count > 0 ? rows : null }
        return Ok(new { Success = true, Data = new { Lst_Mst_District = rows.Count > 0 ? rows : null } });
    }

    // ============================================================================================
    // GetAllActive — nguồn Mst_DistrictController.cs:252-312 (#142).
    // Mệnh đề lọc: strWhereClause_Mst_District (Mst_DistrictController.cs:818-833).
    // Gate:
    //   - Không nhận body: gọi trực tiếp [HttpPost("getallactive")].
    //   - Gọi strWhereClause_Mst_District với { FlagActive = "1" } (:272-275).
    //   - Trong strWhereClause_Mst_District (:827-830), FlagActive được lọc với toán tử '=' -> CÓ TÁC DỤNG (khác GetByProvinceCode).
    //   - Ft_RecordStart = "0", Ft_RecordCount = "123456000" (:287-288) -> lấy toàn bộ huyện có cờ hoạt động '1'.
    //   - Trả Data = lst_Mst_District (danh sách trần, null nếu rỗng — :298-299, KHÁC Mst_Province bọc { Lst_Mst_Province }).
    // ============================================================================================
    [HttpPost("getallactive")]
    public async Task<IActionResult> GetAllActive()
    {
        // Nguồn Mst_DistrictController.cs:272-275: strWhereClause_Mst_District với FlagActive = "1".
        // Ft_RecordStart = "0", Ft_RecordCount = "123456000" (gate :287-288).
        var (_, rows) = await GetXAsync(null, "1", 0, 123456000);

        // Nguồn Mst_DistrictController.cs:294-299:
        // if (objRT_Mst_District.Lst_Mst_District != null && objRT_Mst_District.Lst_Mst_District.Count > 0)
        //     lst_Mst_District = objRT_Mst_District.Lst_Mst_District;
        // objJsonResultEntry.Data = lst_Mst_District; (danh sách trần, null nếu rỗng; KHÁC Mst_Province bọc { Lst_Mst_Province })
        return Ok(new { Success = true, Data = rows.Count > 0 ? rows : null });
    }

    // ============================================================================================
    // Create — nguồn gate Mst_DistrictController.cs:316-376 -> API WA_Mst_District_Create
    // (MstDistrictController.cs:94-165) -> biz WAS_Mst_District_Create (Master.cs:2710-2794)
    // -> Mst_District_Create (Master.cs:2024-2193) -> Mst_District_CheckDB (Master.cs:1729-1799)
    // -> Mst_Province_CheckDB (Master.cs:27-91).
    //
    // Quy trình và thứ tự kiểm tra:
    //   1. Body rỗng / null: gate :325-332 -> ServiceException("strJson null") -> Fail("Mst_District_Create")
    //   2. Chuẩn hoá dữ liệu input (Master.cs:2076-2078):
    //      - strDistrictCode = StdParam(objDistrictCode) (:2076: Trim + ToUpper, rỗng -> null)
    //      - strProvinceCode = StdParam(objProvinceCode) (:2077: Trim + ToUpper, rỗng -> null)
    //      - strDistrictName = string.Format("{0}", objDistrictName).Trim() (:2078: chỉ Trim, không Normalize; null -> "")
    //   3. Kiểm tra mã quận/huyện: if (strDistrictCode == null || strDistrictCode.Length < 1)
    //      -> Fail("Mst_District_Create_InvalidDistrictCode", "Check.strDistrictCode", strDistrictCode) (:2086-2095)
    //   4. Kiểm tra tồn tại trong DB theo CẶP (ProvinceCode, DistrictCode): Mst_District_CheckDB với Flag "0" (:2097-2104, :1744-1745, :1770-1781)
    //      nếu đã có -> Fail("Mst_District_CheckDB_DistrictExist", "Check.ProvinceCode", strProvinceCode, "Check.DistrictCode", strDistrictCode)
    //   5. Kiểm tra tỉnh/thành: Mst_Province_CheckDB với Flag "1", ActiveList "1" (:2108-2114, :53-63, :78-89)
    //      - Không tìm thấy tỉnh (hoặc strProvinceCode rỗng) -> Fail("Mst_Province_CheckDB_ProvinceNotFound", "Check.ProvinceCode", strProvinceCode)
    //      - Tỉnh có FlagActive != "1" -> Fail("Mst_Province_CheckDB_FlagActiveNotMatched", "Check.ProvinceCode", strProvinceCode, "Check.strFlagActiveListToCheck", "1", "DB.FlagActive", province.FlagActive)
    //   6. Kiểm tra tên quận/huyện: if (strDistrictName.Length < 1)
    //      -> Fail("Mst_District_Create_InvalidDistrictName", "Check.strDistrictName", strDistrictName) (:2116-2126)
    //   7. Lưu 7 cột Mst_District (:2136-2144):
    //      DistrictCode = strDistrictCode, NetworkID = config["Biz_NetworkID"] ?? "1001",
    //      ProvinceCode = strProvinceCode, DistrictName = strDistrictName, FlagActive = "1",
    //      LogLUDTimeUTC = UtcNow, LogLUBy = "port".
    //   8. Commit và trả Ok({ Success = true, Data = null }) (:362-363).
    // ============================================================================================
    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] MstDistrictCreateReq? req)
    {
        // 1. Kiểm tra body rỗng / null: gate Mst_DistrictController.cs:325-332
        if (req == null)
        {
            return PortResult.Fail("Mst_District_Create", "Check.strJson", null);
        }

        // Gọi CreateXAsync thực hiện nghiệp vụ tạo (1 transaction / lần)
        var err = await CreateXAsync(req.DistrictCode, req.ProvinceCode, req.DistrictName);
        if (err != null)
        {
            return err;
        }

        // Thành công trả về Success = true, Data = null: gate Mst_DistrictController.cs:362-363
        return Ok(new
        {
            Success = true,
            Data = (object?)null
        });
    }

    /// <summary>
    /// Nghiệp vụ tạo 1 quận/huyện — 1 transaction riêng biệt. Dùng chung cho Create (#143) và Import (#146, gọi từng dòng).
    /// Nguồn: WA_Mst_District_Create (MstDistrictController.cs:94-165) -> WAS_Mst_District_Create (Master.cs:2710-2794)
    /// -> Mst_District_Create (Master.cs:2024-2193) -> Mst_District_CheckDB (Master.cs:1729-1799)
    /// -> Mst_Province_CheckDB (Master.cs:27-91).
    /// Thứ tự kiểm tra:
    ///   1. Chuẩn hoá: strDistrictCode = StdParam(districtCode), strProvinceCode = StdParam(provinceCode), strDistrictName = string.Format("{0}", name).Trim() (:2076-2078)
    ///   2. Mã quận/huyện rỗng -> ErridnLoyalty.Mst_District_Create_InvalidDistrictCode (:2086-2095)
    ///   3. Trùng theo CẶP (ProvinceCode, DistrictCode) -> ErridnLoyalty.Mst_District_CheckDB_DistrictExist (:2097-2104)
    ///   4. Tỉnh/thành: không tìm thấy -> ErridnLoyalty.Mst_Province_CheckDB_ProvinceNotFound (:2108-2114, :53-63), cờ khác '1' -> ErridnLoyalty.Mst_Province_CheckDB_FlagActiveNotMatched (:78-89)
    ///   5. Tên quận/huyện rỗng -> ErridnLoyalty.Mst_District_Create_InvalidDistrictName (:2116-2126)
    ///   6. Lưu 7 cột Mst_District, commit transaction (:2136-2156).
    /// Trả null nếu thành công; trả IActionResult lỗi (400) nếu thất bại.
    /// </summary>
    private async Task<IActionResult?> CreateXAsync(string? districtCode, string? provinceCode, string? name)
    {
        // Bắt đầu Transaction toàn bộ quá trình tạo (Master.cs:2060 _cf.db.BeginTransaction())
        await using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            // 2. Chuẩn hoá dữ liệu input (Master.cs:2076-2078):
            // strDistrictCode = StdParam(objDistrictCode) (Trim + ToUpper, rỗng -> null)
            // strProvinceCode = StdParam(objProvinceCode) (Trim + ToUpper, rỗng -> null)
            // strDistrictName = string.Format("{0}", objDistrictName).Trim() (CHỈ Trim, KHÔNG Normalize; null -> "")
            var strDistrictCode = StdParam(districtCode);
            var strProvinceCode = StdParam(provinceCode);
            var strDistrictName = string.Format("{0}", name).Trim();

            // 3. Kiểm tra mã quận/huyện: Master.cs:2086-2095
            if (string.IsNullOrEmpty(strDistrictCode))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_District_Create_InvalidDistrictCode", "Check.strDistrictCode", strDistrictCode);
            }

            // 4. Kiểm tra tồn tại trong DB theo CẶP (ProvinceCode, DistrictCode): Master.cs:2097-2104 -> Mst_District_CheckDB (Master.cs:1729-1799)
            // SQL nguồn (:1744-1745): t.ProvinceCode = @objProvinceCode and t.DistrictCode = @objDistrictCode
            var districtExists = !string.IsNullOrEmpty(strProvinceCode) && await db.Set<Mst_District>().AnyAsync(x =>
                EF.Functions.Like((x.ProvinceCode ?? "").ToLower(), PortQuery.Equal(strProvinceCode), PortQuery.Esc) &&
                EF.Functions.Like((x.DistrictCode ?? "").ToLower(), PortQuery.Equal(strDistrictCode), PortQuery.Esc));
            if (districtExists)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_District_CheckDB_DistrictExist", "Check.ProvinceCode", strProvinceCode, "Check.DistrictCode", strDistrictCode);
            }

            // 5. Kiểm tra tỉnh/thành: Master.cs:2108-2114 -> Mst_Province_CheckDB (Master.cs:27-91)
            var province = string.IsNullOrEmpty(strProvinceCode)
                ? null
                : await db.Set<Mst_Province>().FirstOrDefaultAsync(x =>
                    EF.Functions.Like((x.ProvinceCode ?? "").ToLower(), PortQuery.Equal(strProvinceCode), PortQuery.Esc));

            if (province == null)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_Province_CheckDB_ProvinceNotFound", "Check.ProvinceCode", strProvinceCode);
            }

            if (province.FlagActive != "1")
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_Province_CheckDB_FlagActiveNotMatched",
                    "Check.ProvinceCode", strProvinceCode,
                    "Check.strFlagActiveListToCheck", "1",
                    "DB.FlagActive", province.FlagActive);
            }

            // 6. Kiểm tra tên quận/huyện: Master.cs:2116-2126
            if (strDistrictName.Length < 1)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_District_Create_InvalidDistrictName", "Check.strDistrictName", strDistrictName);
            }

            // 7. Lưu dữ liệu bảng Mst_District: Master.cs:2136-2144
            var networkId = config["Biz_NetworkID"] ?? "1001"; // Cấu hình Biz_NetworkID, mặc định 1001 (Common.cs:39, 124)
            var now = DateTime.UtcNow;

            var newDistrict = new Mst_District
            {
                DistrictCode = strDistrictCode,                    // :2137 DistrictCode
                NetworkID = networkId,                             // :2138 NetworkID = nNetworkID
                ProvinceCode = strProvinceCode!,                   // :2139 ProvinceCode
                DistrictName = strDistrictName,                    // :2140 DistrictName
                FlagActive = "1",                                  // :2141 FlagActive = TConst.Flag.Active ("1")
                LogLUDTimeUTC = now,                               // :2142 LogLUDTimeUTC = dtimeSys
                LogLUBy = "port"                                   // :2143 LogLUBy ("port" - đích chưa có session user, comment loại Đ)
            };

            await db.Set<Mst_District>().AddAsync(newDistrict);
            await db.SaveChangesAsync();

            // Commit Transaction (Master.cs:2156 TDALUtils.DBUtils.CommitSafety)
            await tx.CommitAsync();

            return null; // Thành công
        }
        catch (Exception)
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    // ============================================================================================
    // Update — nguồn gate Mst_DistrictController.cs:380-447 -> API WA_Mst_District_Update
    // (MstDistrictController.cs:168-239) -> biz WAS_Mst_District_Update (Master.cs:2796-2883)
    // -> Mst_District_Update (Master.cs:2194-2359) -> Mst_District_CheckDB (Master.cs:1729-1799)
    // -> Mst_Province_CheckDB (Master.cs:27-91).
    //
    // Quy trình và thứ tự kiểm tra:
    //   1. Body rỗng / null: gate :389-396 -> ServiceException("strJson null") -> Fail("Mst_District_Update", "Check.strJson", null)
    //   2. Chuỗi Ft_Cols_Upd do gate gửi lên: gate :409-413
    //      - strFt_Cols_Upd = "Mst_District.DistrictName,Mst_District.FlagActive," (ProvinceCode bị comment ở :411 -> KHÔNG đổi tỉnh)
    //      - strFt_Cols_Upd = StdParam(objFt_Cols_Upd) (:2252-2253)
    //      - bUpd_DistrictName = strFt_Cols_Upd.Contains("Mst_District.DistrictName".ToUpper()) (:2260)
    //      - bUpd_FlagActive = strFt_Cols_Upd.Contains("Mst_District.FlagActive".ToUpper()) (:2261)
    //   3. Chuẩn hoá dữ liệu input:
    //      - strDistrictCode = StdParam(objDistrictCode) (:2255: Trim + ToUpper, rỗng -> null)
    //      - strProvinceCode = StdParam(objProvinceCode) (:2256: Trim + ToUpper, rỗng -> null)
    //      - strDistrictName = string.Format("{0}", objDistrictName).Trim() (:2257: CHỈ Trim, null -> "")
    //      - strFlagActive = StdFlag(objFlagActive) (:2258: "1" nếu "1", mọi giá trị khác kể cả thiếu/null -> "0")
    //   4. CẢNH BÁO / HÀNH VI NGUỒN:
    //      Gate luôn gửi FlagActive trong Ft_Cols_Upd (:413), Update dùng StdFlag(objFlagActive) (:2258).
    //      StdFlag coi null / thiếu / không phải "1" đều thành "0".
    //      Do đó, nếu request JSON thiếu FlagActive, giá trị trong DB sẽ bị cập nhật về "0".
    //   5. Kiểm tra tồn tại trong DB theo CẶP (ProvinceCode, DistrictCode): Mst_District_CheckDB với Flag.Yes ("1") (:2267-2274, :1744-1745, :1758-1769)
    //      Nếu không tìm thấy -> Fail("Mst_District_CheckDB_DistrictNotFound", "Check.ProvinceCode", strProvinceCode, "Check.DistrictCode", strDistrictCode)
    //   6. Kiểm tra tỉnh/thành: Mst_Province_CheckDB với Flag.Yes ("1"), ActiveList ("1") (:2278-2284, :53-63, :78-89)
    //      - Không tìm thấy tỉnh (hoặc strProvinceCode rỗng) -> Fail("Mst_Province_CheckDB_ProvinceNotFound", "Check.ProvinceCode", strProvinceCode)
    //      - Tỉnh có FlagActive != "1" -> Fail("Mst_Province_CheckDB_FlagActiveNotMatched", "Check.ProvinceCode", strProvinceCode, "Check.strFlagActiveListToCheck", "1", "DB.FlagActive", province.FlagActive)
    //   7. Kiểm tra tên hợp lệ: if (bUpd_DistrictName && string.IsNullOrEmpty(strDistrictName)) (:2286-2296)
    //      -> Fail("Mst_District_Update_InvalidDistrictName", "Check.strDistrictName", strDistrictName)
    //   8. Cập nhật các cột có cờ hiệu lực: Master.cs:2306-2318
    //      if (bUpd_DistrictName) district.DistrictName = strDistrictName;
    //      if (bUpd_FlagActive) district.FlagActive = strFlagActive;
    //      district.LogLUDTimeUTC = DateTime.UtcNow; (:2309)
    //      district.LogLUBy = "port"; (:2310, loại Đ: đích chưa có session user)
    //   9. Commit và trả Ok({ Success = true, Data = null }) (:431-432).
    // ============================================================================================
    [HttpPost("update")]
    public async Task<IActionResult> Update([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] MstDistrictUpdateReq? req)
    {
        // 1. Kiểm tra body rỗng / null: gate Mst_DistrictController.cs:389-396
        if (req == null)
        {
            return PortResult.Fail("Mst_District_Update", "Check.strJson", null);
        }

        // Bắt đầu Transaction toàn bộ quá trình cập nhật (Master.cs:2236 _cf.db.BeginTransaction())
        await using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            // 2. Chuỗi Ft_Cols_Upd do gate gửi lên: gate Mst_DistrictController.cs:409-413
            var strFt_Cols_Upd = "";
            var tblMstDistrict = "Mst_District.";
            //strFt_Cols_Upd += string.Format("{0}{1},", tblMstDistrict, "ProvinceCode"); // Dòng ProvinceCode bị comment -> KHÔNG đổi tỉnh
            strFt_Cols_Upd += string.Format("{0}{1},", tblMstDistrict, "DistrictName");
            strFt_Cols_Upd += string.Format("{0}{1},", tblMstDistrict, "FlagActive");

            // Chuẩn hoá Ft_Cols_Upd theo Master.cs:2252-2253: StdParam(objFt_Cols_Upd)
            strFt_Cols_Upd = StdParam(strFt_Cols_Upd) ?? "";

            // So khớp các cột được phép cập nhật: Master.cs:2260-2261 (không rút gọn, giữ đúng cấu trúc ToUpper và Contains)
            bool bUpd_DistrictName = strFt_Cols_Upd.Contains("Mst_District.DistrictName".ToUpperInvariant());
            bool bUpd_FlagActive = strFt_Cols_Upd.Contains("Mst_District.FlagActive".ToUpperInvariant());

            // 3. Chuẩn hoá dữ liệu input (Master.cs:2255-2258):
            // strDistrictCode = StdParam(objDistrictCode) (Trim + ToUpper, rỗng -> null)
            // strProvinceCode = StdParam(objProvinceCode) (Trim + ToUpper, rỗng -> null)
            // strDistrictName = string.Format("{0}", objDistrictName).Trim() (CHỈ Trim, KHÔNG Normalize; null -> "")
            // strFlagActive = StdFlag(req.FlagActive) ("1" nếu "1", mọi giá trị khác kể cả thiếu/null -> "0")
            var strDistrictCode = StdParam(req.DistrictCode);
            var strProvinceCode = StdParam(req.ProvinceCode);
            var strDistrictName = string.Format("{0}", req.DistrictName).Trim();
            var strFlagActive = StdFlag(req.FlagActive);

            // CẢNH BÁO / HÀNH VI NGUỒN (Master.cs:2258, 2308):
            // Gate luôn đưa FlagActive vào Ft_Cols_Upd (:413), và Update dùng StdFlag(objFlagActive).
            // StdFlag coi null / thiếu / không phải "1" đều thành "0".
            // Vì vậy, nếu request JSON không truyền FlagActive, giá trị trong DB sẽ bị cập nhật về "0".

            // 4. Kiểm tra tồn tại trong DB theo CẶP (ProvinceCode, DistrictCode): Master.cs:2267-2274 -> Mst_District_CheckDB (Master.cs:1729-1799)
            // strFlagExistToCheck = "1" (TConst.Flag.Yes / Active). SQL nguồn (:1744-1745): t.ProvinceCode = @objProvinceCode and t.DistrictCode = @objDistrictCode
            var district = (string.IsNullOrEmpty(strProvinceCode) || string.IsNullOrEmpty(strDistrictCode))
                ? null
                : await db.Set<Mst_District>().FirstOrDefaultAsync(x =>
                    EF.Functions.Like((x.ProvinceCode ?? "").ToLower(), PortQuery.Equal(strProvinceCode), PortQuery.Esc) &&
                    EF.Functions.Like((x.DistrictCode ?? "").ToLower(), PortQuery.Equal(strDistrictCode), PortQuery.Esc));

            if (district == null)
            {
                // Không tìm thấy quận/huyện theo cặp (:1758-1769) -> ErridnLoyalty.Mst_District_CheckDB_DistrictNotFound
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_District_CheckDB_DistrictNotFound",
                    "Check.ProvinceCode", strProvinceCode,
                    "Check.DistrictCode", strDistrictCode);
            }

            // 5. Kiểm tra tỉnh/thành: Master.cs:2278-2284 -> Mst_Province_CheckDB (Master.cs:27-91)
            var province = await db.Set<Mst_Province>().FirstOrDefaultAsync(x =>
                EF.Functions.Like((x.ProvinceCode ?? "").ToLower(), PortQuery.Equal(strProvinceCode!), PortQuery.Esc));

            if (province == null)
            {
                // Không tìm thấy tỉnh/thành -> ErridnLoyalty.Mst_Province_CheckDB_ProvinceNotFound
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_Province_CheckDB_ProvinceNotFound", "Check.ProvinceCode", strProvinceCode);
            }

            if (province.FlagActive != "1")
            {
                // Tỉnh ngừng hoạt động -> ErridnLoyalty.Mst_Province_CheckDB_FlagActiveNotMatched
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_Province_CheckDB_FlagActiveNotMatched",
                    "Check.ProvinceCode", strProvinceCode,
                    "Check.strFlagActiveListToCheck", "1",
                    "DB.FlagActive", province.FlagActive);
            }

            // 6. Kiểm tra tên quận/huyện: Master.cs:2286-2296
            // if (bUpd_DistrictName && string.IsNullOrEmpty(strDistrictName)) -> ErridnLoyalty.Mst_District_Update_InvalidDistrictName
            if (bUpd_DistrictName && string.IsNullOrEmpty(strDistrictName))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_District_Update_InvalidDistrictName", "Check.strDistrictName", strDistrictName);
            }

            // 7. Cập nhật các cột có cờ hiệu lực: Master.cs:2306-2318
            if (bUpd_DistrictName) district.DistrictName = strDistrictName;
            if (bUpd_FlagActive) district.FlagActive = strFlagActive;

            // Cột nhật ký hệ thống: Master.cs:2309-2310
            district.LogLUDTimeUTC = DateTime.UtcNow;
            district.LogLUBy = "port"; // Đích chưa có session người dùng (bỏ qua loại Đ)

            await db.SaveChangesAsync();

            // Commit Transaction (Master.cs:2322 TDALUtils.DBUtils.CommitSafety)
            await tx.CommitAsync();

            // Thành công trả về Success = true, Data = null: gate Mst_DistrictController.cs:431-432
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
    // Delete — nguồn gate Mst_DistrictController.cs:449-513 -> API WA_Mst_District_Delete
    // (MstDistrictController.cs:242-313) -> biz WAS_Mst_District_Delete (Master.cs:2885-2968)
    // -> Mst_District_Delete (Master.cs:2360-2475) -> Mst_District_CheckDB (Master.cs:1729-1799).
    //
    // Quy trình và thứ tự kiểm tra:
    //   1. Kiểm tra mã quận/huyện: Gate Mst_DistrictController.cs:468, 497-500
    //      if (!TUtils.CUtils.IsNullOrEmpty(DistrictCode)) { ... }
    //      else { throw new ServiceException("invalid_districtcode_provincecode", "invalid_districtcode_provincecode"); }
    //      Khi DistrictCode null/rỗng/khoảng trắng -> trả về 400 ErridnLoyalty.invalid_districtcode_provincecode (chữ thường đúng nguồn).
    //   2. Kiểm tra mã tỉnh/thành: Gate Mst_DistrictController.cs:470-496
    //      if (!TUtils.CUtils.IsNullOrEmpty(ProvinceCode)) { ... WA_Mst_District_Delete ... ; SetSuccess(); Data = null; }
    //      CẢNH BÁO / HÀNH VI NGUỒN:
    //      KHÔNG có else: có DistrictCode nhưng THIẾU ProvinceCode -> không gọi xóa, không ném lỗi, KHÔNG SetSuccess
    //      -> trả về Success = false (giá trị khởi tạo), Data = null, KHÔNG có thông báo lỗi (HTTP 200).
    //   3. Bắt đầu 1 Transaction: Master.cs:2391 _cf.db.BeginTransaction()
    //   4. Chuẩn hoá dữ liệu input: Master.cs:2406-2407
    //      strProvinceCode = StdParam(objProvinceCode) (Trim + ToUpper, rỗng -> null)
    //      strDistrictCode = StdParam(objDistrictCode) (Trim + ToUpper, rỗng -> null)
    //   5. Kiểm tra tồn tại trong DB: Master.cs:2412-2421 -> Mst_District_CheckDB (Master.cs:1729-1799)
    //      strFlagExistToCheck = "1" (TConst.Flag.Yes / Active), strFlagActiveListToCheck = ""
    //      Nếu không tìm thấy -> Fail("Mst_District_CheckDB_DistrictNotFound", "Check.ProvinceCode", strProvinceCode, "Check.DistrictCode", strDistrictCode) (:1758-1769)
    //   6. GAP: FK duy nhất trỏ tới Mst_District là Mst_Agent (20191203.z11.CreateTable.Mst_Agent.sql:27)
    //      nhưng bảng Mst_Agent CHƯA port -> GAP, không giả lập FK.
    //   7. Xoá dòng Mst_District trong DB: Master.cs:2427-2434
    //   8. Commit và trả Ok({ Success = true, Data = null }) (:493-494).
    //
    // Bỏ qua có chủ đích (loại Đ):
    //   - CheckGatewayAuthentication: gate Mst_DistrictController.cs:458; API MstDistrictController.cs:265-270
    //   - CheckAuthorize: gate Mst_DistrictController.cs:459 (hệ thống đích chưa có hệ phân quyền)
    //   - Nguồn gọi bằng tài khoản hệ thống viết cứng tại MstDistrictController.cs:268-269 — không chép giá trị
    //   - Ghi RequestLog/ReturnLog: Master.cs:2394-2401, 2465-2472, 2960-2966
    // ============================================================================================
    [HttpPost("delete")]
    public async Task<IActionResult> Delete([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] MstDistrictDeleteReq? req)
    {
        // 1. Kiểm tra DistrictCode: Gate Mst_DistrictController.cs:468, 497-500
        if (!string.IsNullOrWhiteSpace(req?.DistrictCode))
        {
            // 2. Kiểm tra ProvinceCode: Gate Mst_DistrictController.cs:470-496
            if (!string.IsNullOrWhiteSpace(req?.ProvinceCode))
            {
                // Bắt đầu 1 Transaction toàn bộ quá trình xoá (Master.cs:2391 _cf.db.BeginTransaction())
                await using var tx = await db.Database.BeginTransactionAsync();
                try
                {
                    // Chuẩn hoá dữ liệu input: Master.cs:2406-2407
                    // strProvinceCode = StdParam(objProvinceCode) (Trim + ToUpper, rỗng -> null)
                    // strDistrictCode = StdParam(objDistrictCode) (Trim + ToUpper, rỗng -> null)
                    var strProvinceCode = StdParam(req.ProvinceCode);
                    var strDistrictCode = StdParam(req.DistrictCode);

                    // Kiểm tra tồn tại trong DB: Master.cs:2412-2421 -> Mst_District_CheckDB (Master.cs:1729-1799)
                    // strFlagExistToCheck = "1" (TConst.Flag.Yes / Active). So mã qua PortQuery.Equal + Like (không phân biệt hoa/thường)
                    var district = await db.Set<Mst_District>().FirstOrDefaultAsync(x =>
                        EF.Functions.Like((x.ProvinceCode ?? "").ToLower(), PortQuery.Equal(strProvinceCode!), PortQuery.Esc) &&
                        EF.Functions.Like((x.DistrictCode ?? "").ToLower(), PortQuery.Equal(strDistrictCode!), PortQuery.Esc));

                    if (district == null)
                    {
                        // Không tìm thấy quận/huyện theo cặp (:1758-1769) -> ErridnLoyalty.Mst_District_CheckDB_DistrictNotFound
                        await tx.RollbackAsync();
                        return PortResult.Fail("Mst_District_CheckDB_DistrictNotFound",
                            "Check.ProvinceCode", strProvinceCode,
                            "Check.DistrictCode", strDistrictCode);
                    }

                    // GAP: FK duy nhất trỏ tới Mst_District là Mst_Agent (20191203.z11.CreateTable.Mst_Agent.sql:27)
                    // nhưng bảng Mst_Agent CHƯA port -> GAP, không giả lập FK.

                    // Xoá dòng Mst_District: Master.cs:2427-2434
                    db.Set<Mst_District>().Remove(district);
                    await db.SaveChangesAsync();

                    // Commit Transaction (Master.cs:2438 TDALUtils.DBUtils.CommitSafety)
                    await tx.CommitAsync();

                    // Thành công trả về Success = true, Data = null: gate Mst_DistrictController.cs:493-494
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

            // CẢNH BÁO / HÀNH VI NGUỒN (Mst_DistrictController.cs:470-496):
            // KHÔNG có else: có DistrictCode nhưng THIẾU ProvinceCode -> không gọi xóa, không ném lỗi, KHÔNG SetSuccess
            // -> trả về Success = false (giá trị khởi tạo), Data = null, KHÔNG có thông báo lỗi (HTTP 200).
            return Ok(new
            {
                Success = false,
                Data = (object?)null
            });
        }
        else
        {
            // Gate Mst_DistrictController.cs:497-500
            // else { throw new ServiceException("invalid_districtcode_provincecode", "invalid_districtcode_provincecode"); }
            return PortResult.Fail("invalid_districtcode_provincecode",
                "Check.DistrictCode", req?.DistrictCode,
                "Check.ProvinceCode", req?.ProvinceCode);
        }
    }

    // ============================================================================================
    // Import — nguồn gate Mst_DistrictController.cs:517-673 -> API WA_Mst_District_Create
    // (MstDistrictController.cs:94-165) -> biz WAS_Mst_District_Create (Master.cs:2710-2794)
    // -> Mst_District_Create (Master.cs:2024-2193).
    //
    // GIẢ ĐỊNH (kế thừa từ Data/PortExcel.cs, commit f9de190):
    //   - Định dạng file Excel: dòng 1 = tiêu đề (đúng file ExportTemplate sinh ra), dữ liệu bắt đầu từ ô A2 (firstDataRow = 2).
    //   - Cột ánh xạ theo vị trí: cột 1 = DistrictCode, cột 2 = ProvinceCode, cột 3 = DistrictName
    //     (GetImportDicColumsTemplate có 3 cột:
    //      DistrictCode -> "Mã quận/huyện (*)", ProvinceCode -> "Mã tỉnh/thành (*)", DistrictName -> "Tên quận/huyện").
    //
    // Quy trình và thứ tự kiểm tra:
    //   1. Kiểm tra file upload: gate Mst_DistrictController.cs:541-546
    //      - Thiếu file hoặc file rỗng hoặc đuôi file != ".xlsx" -> Fail("Mst_District_Import", "Message", "File excel import không hợp lệ")
    //   2. Đọc file bằng PortExcel.ReadSheet(stream, new[] { "DistrictCode", "ProvinceCode", "DistrictName" }):
    //      - Bảng không có dòng dữ liệu (table == null || table.Rows.Count == 0) -> Fail("Mst_District_Import", "Message", "File excel import không có dữ liệu") (:558, :658)
    //      - Số cột tiêu đề != GetImportDicColumsTemplate().Count (3 cột) -> Fail("Mst_District_Import", "Message", "File excel import không hợp lệ") (:560-563)
    //   3. Kiểm tra TẤT CẢ các dòng TRƯỚC khi tạo (#region["Check null"] :567-603):
    //      - DistrictCode rỗng / khoảng trắng -> Fail("Mst_District_Import", "Message", "Mã quận/huyện không được trống") (:570-573)
    //      - ProvinceCode rỗng / khoảng trắng -> Fail("Mst_District_Import", "Message", "Mã tỉnh/thành không được trống") (:575-578)
    //      - DistrictName rỗng / khoảng trắng -> Fail("Mst_District_Import", "Message", "Tên quận/huyện không được trống") (:580-583)
    //      Nếu có bất kỳ dòng nào vi phạm, dừng ngay và KHÔNG có dòng nào được tạo vào CSDL.
    //   4. Lưu từng dòng (hành vi "import một phần"):
    //      - Duyệt từng dòng theo thứ tự, gọi CreateXAsync(districtCode, provinceCode, name) riêng cho mỗi dòng (mỗi dòng 1 transaction biz riêng biệt: gate :630-651).
    //      - Nếu dòng thứ k gặp lỗi (vd trùng mã Mst_District_CheckDB_DistrictExist hoặc tỉnh ngưng Mst_Province_CheckDB_FlagActiveNotMatched),
    //        vòng lặp dừng ngay lập tức, trả về đúng lỗi của CreateXAsync cho dòng k.
    //      - CÁC DÒNG TRƯỚC k ĐÃ ĐƯỢC COMMIT VÀO DB (không rollback các dòng đã tạo trước đó).
    //      - Các dòng sau k không được duyệt.
    //   5. Thành công: trả Ok({ Success = true, Data = null }) (:653-654).
    //
    // Bỏ qua có chủ đích (loại Đ):
    //   - CheckGatewayAuthentication, CheckAuthorize (:528-529)
    //   - Nguồn lưu file tạm xuống đĩa (FileUtils.SaveTempFile :549) -> ở đây đọc trực tiếp từ stream qua ClosedXML
    //   - Session người dùng orgUserUI (:527, :633, :641-645).
    // ============================================================================================
    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Import(IFormFile? file)
    {
        // 1. Kiểm tra file upload: gate Mst_DistrictController.cs:541-546
        if (file == null || file.Length == 0)
        {
            return PortResult.Fail("Mst_District_Import", "Message", "File excel import không hợp lệ");
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".xlsx")
        {
            return PortResult.Fail("Mst_District_Import", "Message", "File excel import không hợp lệ");
        }

        // 2. Đọc bảng tính bằng PortExcel.ReadSheet (giả định định dạng file Excel từ PortExcel)
        int columnCount;
        List<Dictionary<string, string?>> rows;
        try
        {
            using var stream = file.OpenReadStream();
            (columnCount, rows) = PortExcel.ReadSheet(stream, new[] { "DistrictCode", "ProvinceCode", "DistrictName" });
        }
        catch
        {
            return PortResult.Fail("Mst_District_Import", "Message", "File excel import không hợp lệ");
        }

        // Kiểm tra có dữ liệu hay không: gate :558, :658 (table == null || table.Rows.Count == 0)
        if (rows.Count == 0)
        {
            return PortResult.Fail("Mst_District_Import", "Message", "File excel import không có dữ liệu");
        }

        // Kiểm tra số cột dòng tiêu đề so với GetImportDicColumsTemplate().Count (3 cột): gate :560-563
        if (columnCount != GetImportDicColumsTemplate().Count)
        {
            return PortResult.Fail("Mst_District_Import", "Message", "File excel import không hợp lệ");
        }

        // 3. Kiểm tra TẤT CẢ các dòng TRƯỚC khi tạo (Check null: gate Mst_DistrictController.cs:567-603)
        foreach (var r in rows)
        {
            var districtCode = r.GetValueOrDefault("DistrictCode");
            if (string.IsNullOrWhiteSpace(districtCode))
            {
                return PortResult.Fail("Mst_District_Import", "Message", "Mã quận/huyện không được trống");
            }

            var provinceCode = r.GetValueOrDefault("ProvinceCode");
            if (string.IsNullOrWhiteSpace(provinceCode))
            {
                return PortResult.Fail("Mst_District_Import", "Message", "Mã tỉnh/thành không được trống");
            }

            var districtName = r.GetValueOrDefault("DistrictName");
            if (string.IsNullOrWhiteSpace(districtName))
            {
                return PortResult.Fail("Mst_District_Import", "Message", "Tên quận/huyện không được trống");
            }
        }

        // 4. Lưu từng dòng tuần tự: mỗi dòng 1 transaction riêng (hành vi "import một phần": gate :630-651)
        foreach (var r in rows)
        {
            var districtCode = r.GetValueOrDefault("DistrictCode");
            var provinceCode = r.GetValueOrDefault("ProvinceCode");
            var districtName = r.GetValueOrDefault("DistrictName");

            var err = await CreateXAsync(districtCode, provinceCode, districtName);
            if (err != null)
            {
                // Lỗi ở dòng k -> thoát ngay, trả đúng lỗi của Create, các dòng trước k đã tạo thành công
                return err;
            }
        }

        // Thành công: gate :653-654
        return Ok(new
        {
            Success = true,
            Data = (object?)null
        });
    }

    // ============================================================================================
    // Export (#147) — nguồn gate Mst_DistrictController.cs:678-756 -> API WA_Mst_District_Get
    // (MstDistrictController.cs:20-91) -> biz WAS_Mst_District_Get (Master.cs:2584-2708)
    // -> Mst_District_Get (Master.cs:1801-2023).
    //
    // Quy trình và thứ tự xử lý:
    //   1. Body JSON: { OrgID, KeyWord, FlagActive } (gate :678, :699-704).
    //   2. Gọi GetXAsync(req.KeyWord, req.FlagActive, 0, 123456000).
    //      Nguồn: OrgID nhận từ client (:702) nhưng hàm strWhereClause_Mst_District (:818-833) KHÔNG sử dụng OrgID để lọc,
    //      giữ đúng hành vi nguồn (không lọc OrgID).
    //      Toán tử lọc: KeyWord like '%DistrictName%', FlagActive '=' (KHÁC Province dùng IN).
    //      Ft_RecordStart = "0", Ft_RecordCount = "123456000" (:717-718).
    //   3. Kiểm tra danh sách kết quả: gate :725, :740-743
    //      Nếu danh sách rỗng (rows == null || rows.Count == 0) -> ném ServiceException("Không có dữ liệu") (:742)
    //      -> trả về 400 ErridnLoyalty.Mst_District_Export kèm Params Message = "Không có dữ liệu".
    //   4. Từ điển cột xuất Excel: gate :729 -> GetImportDicColums() (:795-804).
    //      ĐẶC BIỆT LƯU Ý: dùng từ điển Import cho Export là của nguồn (nguồn gọi GetImportDicColums(), KHÔNG phải GetExportDicColums).
    //      Gồm đúng 4 cột theo thứ tự:
    //      - DistrictCode: "Mã quận/huyện (*)"
    //      - ProvinceCode: "Mã tỉnh/thành (*)"
    //      - DistrictName: "Tên quận/huyện"
    //      - FlagActive: "Trạng thái"
    //   5. Sinh đường dẫn file xuất: BaseController.GenExcelExportFilePath (:731) -> PortExcel.GenExcelExportFilePath(webRoot, "Mst_District").
    //      Tên sheet: "Mst_District" (:732).
    //      Ghi file qua PortExcel.ExportToExcelFromList(rows, dicColNames, physicalPath, "Mst_District") (:732).
    //   6. Trả URL tải file với Success = true, Data = { Url = PortExcel.GetRequestBaseAddress(Request) + virtualPath } (:733-737).
    //
    // Bỏ qua có chủ đích (loại Đ):
    //   - CheckGatewayAuthentication: gate Mst_DistrictController.cs:688; API MstDistrictController.cs:42-49
    //   - CheckAuthorize: gate Mst_DistrictController.cs:689 (hệ thống đích chưa có hệ phân quyền)
    //   - Sys_User_CheckAuthorize: Master.cs:2623-2636
    //   - Ghi RequestLog/ReturnLog: Master.cs:1841-1849, 2700-2705
    // ============================================================================================
    [HttpPost("export")]
    public async Task<IActionResult> Export([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] MstDistrictExportReq? req)
    {
        req ??= new MstDistrictExportReq();

        // Nguồn: strWhereClause_Mst_District (:818-833) chỉ lọc theo KeyWord và FlagActive (toán tử =),
        // OrgID truyền vào (:702) nhưng KHÔNG được sử dụng trong hàm tạo WHERE clause -> không lọc OrgID.
        // Ft_RecordStart = "0", Ft_RecordCount = 123456000, Rt_Cols_Mst_District = "*" (:717-721).
        var (_, rows) = await GetXAsync(req.KeyWord, req.FlagActive, 0, 123456000);

        // Nguồn Mst_DistrictController.cs:725, :740-743: nếu danh sách rỗng -> throw ServiceException("Không có dữ liệu")
        if (rows == null || rows.Count == 0)
        {
            return PortResult.Fail("Mst_District_Export", "Message", "Không có dữ liệu");
        }

        // Tạo thư mục và đường dẫn file Excel xuất: BaseController.GenExcelExportFilePath (:731)
        var webRoot = env.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
        {
            webRoot = Path.Combine(env.ContentRootPath, "wwwroot");
        }

        // Lấy từ điển cột: Mst_DistrictController.cs:729 gọi GetImportDicColums()
        // Ghi chú: dùng từ điển Import cho Export là của nguồn (4 cột)
        var dicColNames = GetImportDicColums();
        var (physicalPath, virtualPath) = PortExcel.GenExcelExportFilePath(webRoot, "Mst_District");

        // Ghi danh sách ra file Excel: ExcelExport.ExportToExcelFromList (:732) với sheet "Mst_District"
        PortExcel.ExportToExcelFromList(rows, dicColNames, physicalPath, "Mst_District");

        // Trả URL tải file: Data = { Url = GetRequestBaseAddress() + url } (:733-737)
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
    // ExportTemplate — nguồn gate Mst_DistrictController.cs:759-791 (#148).
    // Action này KHÔNG kiểm quyền ở nguồn (không có CheckGatewayAuthentication và không có CheckAuthorize).
    // Không gọi tầng service, không truy vấn cơ sở dữ liệu.
    // Khởi tạo danh sách rỗng kiểu Mst_District (:767).
    // Lấy từ điển 3 cột mẫu import từ GetImportDicColumsTemplate (:769):
    //   1. DistrictCode ("Mã quận/huyện (*)")
    //   2. ProvinceCode ("Mã tỉnh/thành (*)")
    //   3. DistrictName ("Tên quận/huyện")
    // Sinh đường dẫn file mẫu qua PortExcel.GenExcelExportFilePath(webRoot, "Mst_District") (:771).
    // Xuất file Excel qua PortExcel.ExportToExcelFromList(list, dicColNames, physicalPath, "Mst_District") (:772).
    // Trả về Success = true, Data = { Url = PortExcel.GetRequestBaseAddress(Request) + virtualPath } (:773-777).
    // Không có nhánh báo lỗi khi danh sách rỗng (khác với Export).
    // ============================================================================================
    [HttpPost("exporttemplate")]
    public IActionResult ExportTemplate()
    {
        // Khởi tạo danh sách rỗng kiểu Mst_District (gate Mst_DistrictController.cs:767)
        var list = new List<Mst_District>();

        // Lấy từ điển 3 cột mẫu import (gate Mst_DistrictController.cs:769, GetImportDicColumsTemplate)
        var dicColNames = GetImportDicColumsTemplate();

        // Xác định thư mục webRoot và sinh đường dẫn file (gate Mst_DistrictController.cs:771)
        var webRoot = env.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
        {
            webRoot = Path.Combine(env.ContentRootPath, "wwwroot");
        }

        var (physicalPath, virtualPath) = PortExcel.GenExcelExportFilePath(webRoot, "Mst_District");

        // Ghi danh sách rỗng ra file Excel với tên sheet Mst_District (gate Mst_DistrictController.cs:772)
        PortExcel.ExportToExcelFromList(list, dicColNames, physicalPath, "Mst_District");

        // Trả URL tải file với Success = true (gate Mst_DistrictController.cs:773-777)
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
    /// Từ điển cột xuất Excel: gate Mst_DistrictController.cs:795-804 (GetImportDicColums).
    /// ĐẶC BIỆT LƯU Ý THEO NGUỒN: Nguồn tại Mst_DistrictController.cs:729 sử dụng GetImportDicColums()
    /// (dùng từ điển Import cho Export là của nguồn, KHÔNG phải GetExportDicColums).
    /// Giữ đúng thứ tự 4 cột từ nguồn:
    /// 1. DistrictCode ("Mã quận/huyện (*)")
    /// 2. ProvinceCode ("Mã tỉnh/thành (*)")
    /// 3. DistrictName ("Tên quận/huyện")
    /// 4. FlagActive ("Trạng thái")
    /// </summary>
    private static Dictionary<string, string> GetImportDicColums()
    {
        return new Dictionary<string, string>
        {
            { "DistrictCode", "Mã quận/huyện (*)" },
            { "ProvinceCode", "Mã tỉnh/thành (*)" },
            { "DistrictName", "Tên quận/huyện" },
            { "FlagActive", "Trạng thái" }
        };
    }

    /// <summary>
    /// Từ điển cột mẫu Import Excel: gate Mst_DistrictController.cs (GetImportDicColumsTemplate).
    /// Giữ đúng thứ tự 3 cột từ nguồn:
    /// 1. DistrictCode ("Mã quận/huyện (*)")
    /// 2. ProvinceCode ("Mã tỉnh/thành (*)")
    /// 3. DistrictName ("Tên quận/huyện")
    /// </summary>
    private static Dictionary<string, string> GetImportDicColumsTemplate()
    {
        return new Dictionary<string, string>
        {
            { "DistrictCode", "Mã quận/huyện (*)" },
            { "ProvinceCode", "Mã tỉnh/thành (*)" },
            { "DistrictName", "Tên quận/huyện" }
        };
    }

    /// <summary>
    /// Hàm dùng chung lấy dữ liệu Mst_District hỗ trợ phân trang và tìm kiếm.
    /// Nguồn SQL: Master.cs:1881-1934 (Mst_District_Get).
    /// </summary>
    public async Task<(int ItemCount, List<object> Rows)> GetXAsync(
        string? keyWord,
        string? flagActive,
        long recordStart,
        long recordCount,
        string? districtCodeEqual = null,
        string? provinceCodeEqual = null)
    {
        var q = db.Set<Mst_District>().AsNoTracking().AsQueryable();

        // KeyWord -> like '%x%' trên DistrictName (Mst_DistrictController.cs:823-826).
        // Nguồn chạy SQL Server collation KHÔNG phân biệt hoa/thường ⇒ hạ chữ cả 2 phía rồi LIKE (bẫy #18/#31).
        if (!string.IsNullOrWhiteSpace(keyWord))
        {
            var kw = PortQuery.Contains(keyWord);
            q = q.Where(x => EF.Functions.Like((x.DistrictName ?? "").ToLower(), kw, PortQuery.Esc));
        }

        // DistrictCode -> `DistrictCode = 'x'`
        // Nguồn dùng `=` trên SQL Server collation KHÔNG phân biệt hoa/thường ⇒ Like KHÔNG wildcard, hạ chữ 2 phía (Mst_DistrictController.cs:844-847).
        if (!string.IsNullOrWhiteSpace(districtCodeEqual))
        {
            var dc = PortQuery.Equal(districtCodeEqual);
            q = q.Where(x => EF.Functions.Like((x.DistrictCode ?? "").ToLower(), dc, PortQuery.Esc));
        }

        // ProvinceCode -> `ProvinceCode = 'x'`
        // Nguồn dùng `=` trên SQL Server collation KHÔNG phân biệt hoa/thường ⇒ Like KHÔNG wildcard, hạ chữ 2 phía (Mst_DistrictController.cs:840-843).
        if (!string.IsNullOrWhiteSpace(provinceCodeEqual))
        {
            var pc = PortQuery.Equal(provinceCodeEqual);
            q = q.Where(x => EF.Functions.Like((x.ProvinceCode ?? "").ToLower(), pc, PortQuery.Esc));
        }

        // FlagActive -> toán tử '=' (Mst_DistrictController.cs:827-830) — KHÁC Province dùng IN:
        // EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal(flagActive), PortQuery.Esc)
        // Chú ý: dòng FlagActive bị comment ở nguồn trong strWhereClause_Mst_District_Detail (:848-851).
        if (!string.IsNullOrWhiteSpace(flagActive))
        {
            var fa = PortQuery.Equal(flagActive);
            q = q.Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), fa, PortQuery.Esc));
        }

        // OrgID: nhận ở gate (Mst_DistrictController.cs:31, :61) nhưng strWhereClause_Mst_District (:818-833) KHÔNG lọc OrgID.

        // Chia trang theo DistrictCode asc (Mst_District_Get SQL: :1890 order by md.DistrictCode asc).
        var ordered = q.OrderBy(x => x.DistrictCode);
        var itemCount = await ordered.CountAsync();

        // MyIdxSeq = số thứ tự 0-based trong thứ tự DistrictCode (identity(bigint, 0, 1) ở SQL nguồn :1884).
        // Lấy dòng [recordStart, recordStart+recordCount) theo thứ tự DistrictCode.
        var page = (recordCount <= 0 || recordStart < 0)
            ? new List<Mst_District>()
            : await ordered.Skip((int)Math.Min(recordStart, int.MaxValue)).Take((int)Math.Min(recordCount, int.MaxValue)).ToListAsync();

        // Select cuối theo t.MyIdxSeq asc (:1929 order by t.MyIdxSeq asc) — KHÔNG join Mst_Province.
        var rows = page
            .Select((x, i) => (object)new
            {
                MyIdxSeq = recordStart + i,
                x.DistrictCode,
                x.ProvinceCode,
                x.NetworkID,
                x.DistrictName,
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
    /// Dùng cho mã (DistrictCode, ProvinceCode...).
    /// Nguồn tham chiếu: 05.Refs.Biz\Biz\Loyalty.TVO\Loyalty.TVO.Utils\Utils.cs:29
    /// </summary>
    private static string? StdParam(object? val)
    {
        if (val == null) return null;
        var s = val.ToString()?.Trim();
        return string.IsNullOrEmpty(s) ? null : s.ToUpperInvariant();
    }

    /// <summary>
    /// Chuẩn hoá cờ: "1" nếu đúng "1", mọi giá trị khác (kể cả null/thiếu/rỗng) -> "0".
    /// Dùng cho cờ (FlagActive...).
    /// Nguồn tham chiếu: 05.Refs.Biz\Biz\Loyalty.TVO\Loyalty.TVO.Utils\Utils.cs:89
    /// </summary>
    private static string StdFlag(object? val)
    {
        if (val == null) return "0";
        var s = val.ToString()?.Trim();
        return s == "1" ? "1" : "0";
    }
    #endregion
}
