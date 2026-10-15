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
// PORT 1:1 — màn danh mục dòng xe (Mst_CarModel) của dự án nguồn 2022.7.Loyalty (#97, #98, #99, #100, #101).
//
// Nguồn controller (tầng gate):
//   Mst_CarModelController.cs:25-105   Search              -> WA_Mst_CarModel_Get
//   Mst_CarModelController.cs:106-216  GetAllActive        -> WA_Mst_CarModel_Get
//   Mst_CarModelController.cs:217-280  GetByModelCode      -> WA_Mst_CarModel_Get
//   Mst_CarModelController.cs:329-414  Create              -> WA_Mst_CarModel_Create
//   Mst_CarModelController.cs:418-485  Update              -> WA_Mst_CarModel_Update
//   Mst_CarModelController.cs:284-305  strWhereClause_Mst_CarModel (mệnh đề lọc Search)
//   Mst_CarModelController.cs:306-325  strWhereClause_Mst_CarModel_By_ModelCode (mệnh đề lọc GetByModelCode)
// Nguồn API:
//   MstCarModelController.cs:20-90     WA_Mst_CarModel_Create
//   MstCarModelController.cs:92-162    WA_Mst_CarModel_Update
//   MstCarModelController.cs:238-307   WA_Mst_CarModel_Get
// Nguồn biz:
//   Master.Loyalty.cs:1434-1534        WAS_Mst_CarModel_Get
//   Master.Loyalty.cs:1535-1687        Mst_CarModel_Get
//   Master.Loyalty.cs:1688-1842        Mst_CarModel_GetX (SQL :1739-1791)
//   Master.Loyalty.cs:1908-1991        WAS_Mst_CarModel_Create
//   Master.Loyalty.cs:1992-2141        Mst_CarModel_Create
//   Master.Loyalty.cs:2142-2250        Mst_CarModel_CreateX
//   Master.Loyalty.cs:2251-2336        WAS_Mst_CarModel_Update
//   Master.Loyalty.cs:2337-2492        Mst_CarModel_Update
//   Master.Loyalty.cs:2494-2592        Mst_CarModel_UpdateX
//   Master.Loyalty.cs:1843-1906        Mst_CarModel_CheckDB
//   Master.Loyalty.cs:28-91            Mst_CarBrand_CheckDB
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

/// <summary>
/// Port 1:1 <c>Create</c> — body JSON 5 trường của model nguồn (Mst_CarModelController.cs:329-414).
/// Nguồn deserialize vào TModelLoyaltys.Mst_CarModel rồi truyền ModelCode/NetworkID/BrandCode/ModelName/Remark
/// xuống biz (Master.Loyalty.cs:1957-1961, 2151-2155).
/// </summary>
public class MstCarModelCreateReq
{
    public string? ModelCode { get; set; }            // Mã dòng xe — :1957, :2179 (StdParam)
    public string? NetworkID { get; set; }            // NetworkID lấy từ JSON, không từ cấu hình — :1958, :2177 (StdParam)
    public string? BrandCode { get; set; }            // Mã hãng xe — :1959, :2175 (StdParam)
    public string? ModelName { get; set; }            // Tên dòng xe — :1960, :2176 (StdStr — KHÔNG viết hoa)
    public string? Remark { get; set; }               // Ghi chú — :1961, :2178 (StdStr)
}

/// <summary>
/// Port 1:1 <c>Update</c> — body JSON của model nguồn (Mst_CarModelController.cs:418-485).
/// Nguồn deserialize vào TModelLoyaltys.Mst_CarModel rồi truyền
/// ModelCode/NetworkID/BrandCode/ModelName/Remark/FlagActive xuống biz (Master.Loyalty.cs:2299-2304, 2536-2541).
/// </summary>
public class MstCarModelUpdateReq
{
    public string? ModelCode { get; set; }            // Mã dòng xe — :2541 (StdParam)
    public string? NetworkID { get; set; }            // NetworkID nhận từ JSON nhưng không lưu — :2537 (StdParam)
    public string? BrandCode { get; set; }            // Mã hãng xe — :2536 (StdParam)
    public string? ModelName { get; set; }            // Tên dòng xe — :2538 (StdStr — KHÔNG viết hoa)
    public string? Remark { get; set; }               // Ghi chú — :2539 (StdStr)
    public string? FlagActive { get; set; }           // Cờ hoạt động — :2540 (StdFlag)
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

    // ============================================================================================
    // Create — nguồn Mst_CarModelController.cs:329-414 (gate) -> MstCarModelController.cs:20-90 (API)
    //   -> Master.Loyalty.cs:1908-1991 (WAS_Mst_CarModel_Create) -> :1992-2141 (Mst_CarModel_Create)
    //   -> :2142-2250 (Mst_CarModel_CreateX) -> :1843-1906 (Mst_CarModel_CheckDB)
    //   -> :28-91 (Mst_CarBrand_CheckDB).
    //
    // GIỮ ĐÚNG NGUỒN:
    //   * Body JSON nhận ModelCode, NetworkID, BrandCode, ModelName, Remark (:1957-1961).
    //     ModelCode do người dùng nhập, khối sinh Seq (:360-381) bị comment trong nguồn (KHÔNG sinh tự động).
    //   * Body rỗng/null: gate :339-346 ném ServiceException("strJson null") -> mã mặc định Mst_CarModel_Create (:2016).
    //   * Chuẩn hoá (:2175-2179):
    //     - BrandCode = StdParam (Trim+ToUpper, rỗng->null)
    //     - ModelName = StdStr (Trim+Normalize, rỗng->null — KHÔNG viết hoa, khác CarBrand)
    //     - NetworkID = StdParam (Trim+ToUpper, rỗng->null)
    //     - Remark    = StdStr (Trim+Normalize, rỗng->null)
    //     - ModelCode = StdParam (Trim+ToUpper, rỗng->null)
    //   * Kiểm tra ĐÚNG THỨ TỰ (:2188-2214):
    //     1. ModelCode null/rỗng -> Mst_CarModel_Create_InvalidModelCode (:2188-2198).
    //     2. Mst_CarModel_CheckDB(ModelCode, FlagExist "0", ""): đã tồn tại -> Mst_CarModel_CheckDB_CarModelExist (:2200-2206, :1886).
    //     3. Mst_CarBrand_CheckDB(BrandCode, FlagExist "1", FlagActive "1"):
    //        - Không có dòng (hoặc BrandCode rỗng/null -> t.BrandCode = NULL -> 0 dòng) -> Mst_CarBrand_CheckDB_BrandNotFound (:2208-2214, :55-63).
    //        - Có dòng nhưng FlagActive khác "1" -> Mst_CarBrand_CheckDB_FlagActiveNotMatched (:79-90).
    //   * Lưu (:2225-2241): ModelCode, NetworkID, BrandCode, ModelName, Remark, FlagActive = "1",
    //     LogLUDTimeUTC = UtcNow, LogLUBy = user. Mã mặc định Mst_CarModel_Create (:2016).
    //   * Thành công: gate :400-401 SetSuccess + Data = null.
    //
    // Bỏ qua có chủ đích (loại Đ):
    //   * CheckGatewayAuthentication/CheckAuthorize (gate :349-350), Sys_User_CheckAuthorize (:2056-2069).
    //   * Ghi RequestLog/ReturnLog (:2037-2044, :2131-2138).
    //   * LogLUBy gán cố định "port" do đích chưa có session người dùng (:2233).
    // ============================================================================================
    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] MstCarModelCreateReq? req)
    {
        // 1. Body rỗng/null: gate :339-346 ném ServiceException("strJson null") -> mã mặc định Mst_CarModel_Create (:2016).
        if (req == null)
        {
            return PortResult.Fail("Mst_CarModel_Create", "Check.strJson", null);
        }

        // Gọi CreateXAsync thực hiện nghiệp vụ tạo (1 transaction / lần)
        var err = await CreateXAsync(req.ModelCode, req.NetworkID, req.BrandCode, req.ModelName, req.Remark);
        if (err != null)
        {
            return err;
        }

        // Thành công: gate :400-401 SetSuccess + Data = null.
        return Ok(new { Success = true, Data = (object?)null });
    }

    /// <summary>
    /// Nghiệp vụ tạo 1 dòng xe — 1 transaction riêng biệt.
    /// Nguồn: WA_Mst_CarModel_Create (MstCarModelController.cs:20-90) -> WAS_Mst_CarModel_Create (Master.Loyalty.cs:1908-1991)
    /// -> Mst_CarModel_Create (Master.Loyalty.cs:1992-2141) -> Mst_CarModel_CreateX (Master.Loyalty.cs:2142-2250)
    /// -> Mst_CarModel_CheckDB (Master.Loyalty.cs:1843-1906) -> Mst_CarBrand_CheckDB (Master.Loyalty.cs:28-91).
    /// </summary>
    private async Task<IActionResult?> CreateXAsync(string? modelCode, string? networkId, string? brandCode, string? modelName, string? remark)
    {
        // 1 transaction cho toàn bộ nghiệp vụ tạo (Master.Loyalty.cs:2034 _cf.db.BeginTransaction()).
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // Chuẩn hoá input (Master.Loyalty.cs:2175-2179):
            // BrandCode = StdParam, ModelName = StdStr (KHÔNG viết hoa), NetworkID = StdParam, Remark = StdStr, ModelCode = StdParam.
            var strBrandCode = StdParam(brandCode);
            var strModelName = StdStr(modelName);
            var strNetworkID = StdParam(networkId);
            var strRemark = StdStr(remark);
            var strModelCode = StdParam(modelCode);

            // Kiểm tra ĐÚNG THỨ TỰ:
            // 1. ModelCode null/rỗng -> ErridnLoyalty.Mst_CarModel_Create_InvalidModelCode (:2188-2198).
            if (string.IsNullOrEmpty(strModelCode))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_CarModel_Create_InvalidModelCode", "Check.strModelCode", strModelCode);
            }

            // 2. Mst_CarModel_CheckDB(ModelCode, FlagExist "0", "") (:2200-2206): đã tồn tại -> Mst_CarModel_CheckDB_CarModelExist (:1886).
            var modelExists = await _db.Set<Mst_CarModel>().AnyAsync(x =>
                EF.Functions.Like((x.ModelCode ?? "").ToLower(), PortQuery.Equal(strModelCode), PortQuery.Esc));
            if (modelExists)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_CarModel_CheckDB_CarModelExist", "Check.ModelCode", strModelCode);
            }

            // 3. Mst_CarBrand_CheckDB(BrandCode, FlagExist "1", FlagActive "1") (:2208-2214):
            // Nguồn so t.BrandCode = @objBrandCode. Nếu BrandCode null -> 0 dòng -> Mst_CarBrand_CheckDB_BrandNotFound (:55-63).
            var brand = string.IsNullOrEmpty(strBrandCode)
                ? null
                : await _db.Set<Mst_CarBrand>().FirstOrDefaultAsync(x =>
                    EF.Functions.Like((x.BrandCode ?? "").ToLower(), PortQuery.Equal(strBrandCode), PortQuery.Esc));

            if (brand == null)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_CarBrand_CheckDB_BrandNotFound", "Check.BrandCode", strBrandCode);
            }

            // Có dòng nhưng FlagActive không thuộc "1" -> Mst_CarBrand_CheckDB_FlagActiveNotMatched (:79-90).
            // Nguồn :79 `!"1".Contains(Convert.ToString(FlagActive))` — FlagActive NULL/rỗng ⇒ "" ⇒ Contains("") = true ⇒ VẪN qua.
            if (!"1".Contains(brand.FlagActive ?? ""))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_CarBrand_CheckDB_FlagActiveNotMatched",
                    "Check.BrandCode", strBrandCode,
                    "Check.strFlagActiveListToCheck", "1",
                    "DB.FlagActive", brand.FlagActive);
            }

            // Lưu dòng mới (Master.Loyalty.cs:2225-2241).
            var dtimeSys = DateTime.UtcNow; // :2014 dtimeSys = DateTime.UtcNow
            var entity = new Mst_CarModel
            {
                ModelCode = strModelCode,
                NetworkID = strNetworkID,
                BrandCode = strBrandCode,
                ModelName = strModelName,
                Remark = strRemark,
                FlagActive = "1", // TConst.Flag.Active (:2231)
                LogLUDTimeUTC = dtimeSys,
                LogLUBy = "port" // strWAUserCode (:2233) — loại Đ do đích chưa có session user
            };

            _db.Set<Mst_CarModel>().Add(entity);
            await _db.SaveChangesAsync();

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
    // Update — nguồn Mst_CarModelController.cs:418-485 (gate) -> MstCarModelController.cs:92-162 (API)
    //   -> Master.Loyalty.cs:2251-2336 (WAS_Mst_CarModel_Update) -> :2337-2492 (Mst_CarModel_Update)
    //   -> :2494-2592 (Mst_CarModel_UpdateX) -> :2553-2559 (Mst_CarModel_CheckDB)
    //   -> :2561-2567 (Mst_CarBrand_CheckDB).
    //
    // GIỮ ĐÚNG NGUỒN:
    //   * Body JSON nhận ModelCode, NetworkID, BrandCode, ModelName, Remark, FlagActive (:2299-2304).
    //   * Body rỗng/null: gate :428-435 ném Exception("strJson null") -> bắt lại thành mã mặc định Mst_CarModel_Update (:2364).
    //   * Ft_Cols_Upd cố định ở gate (:448-453): BrandCode, ModelName, Remark, FlagActive -> LUÔN cập nhật 4 cột.
    //   * Chuẩn hoá input (:2536-2541):
    //     - BrandCode = StdParam (Trim+ToUpper, rỗng->null)
    //     - NetworkID = StdParam (nhận vào nhưng KHÔNG lưu, NetworkID giữ nguyên :2537)
    //     - ModelName = StdStr (Trim+Normalize, rỗng->null — KHÔNG viết hoa, khác CarBrand)
    //     - Remark    = StdStr (Trim+Normalize, rỗng->null)
    //     - FlagActive = StdFlag ("1" chỉ khi đúng "1"; thiếu FlagActive -> '0')
    //     - ModelCode = StdParam (Trim+ToUpper, rỗng->null)
    //   * Kiểm tra ĐÚNG THỨ TỰ (:2553-2567):
    //     1. Mst_CarModel_CheckDB(ModelCode, FlagExist "1", "") (:2553-2559):
    //        Không tìm thấy dòng (hoặc ModelCode rỗng/null -> = NULL -> 0 dòng) -> Mst_CarModel_CheckDB_CarModelNotFound (:2558).
    //     2. Mst_CarBrand_CheckDB(BrandCode, FlagExist "1", "") (:2561-2567):
    //        Không tìm thấy dòng (hoặc BrandCode rỗng/null -> = NULL -> 0 dòng) -> Mst_CarBrand_CheckDB_BrandNotFound (:2566).
    //        LƯU Ý: strFlagActiveListToCheck = "" -> KHÔNG kiểm FlagActive của hãng (khác Create #100): hãng inactive vẫn hợp lệ.
    //   * Lưu (:2576-2590): ModelName, BrandCode, Remark, FlagActive + LogLUDTimeUTC = UtcNow + LogLUBy = "port".
    //     NetworkID và ModelCode KHÔNG đổi. Mã mặc định Mst_CarModel_Update (:2364).
    //   * Thành công: gate :471-472 SetSuccess + Data = null.
    //
    // Bỏ qua có chủ đích (loại Đ):
    //   * CheckGatewayAuthentication/CheckAuthorize (gate :438-439), Sys_User_CheckAuthorize (:2404-2417).
    //   * Ghi RequestLog/ReturnLog (:2384-2392, :2481-2489).
    //   * LogLUBy gán cố định "port" do đích chưa có session người dùng (:2582).
    // ============================================================================================
    [HttpPost("update")]
    public async Task<IActionResult> Update([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] MstCarModelUpdateReq? req)
    {
        // 1. Body rỗng/null: gate :428-435 ném Exception("strJson null") -> mã mặc định Mst_CarModel_Update (:2364).
        if (req == null)
        {
            return PortResult.Fail("Mst_CarModel_Update", "Check.strJson", null);
        }

        // 2. Mở 1 transaction cho toàn bộ nghiệp vụ cập nhật (Master.Loyalty.cs:2382 _cf.db.BeginTransaction()).
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // 3. Chuẩn hoá input (Master.Loyalty.cs:2536-2541):
            // Ft_Cols_Upd cố định ở gate; hãng inactive vẫn hợp lệ; thiếu FlagActive -> '0'.
            var strBrandCode = StdParam(req.BrandCode);
            var strModelName = StdStr(req.ModelName);
            var strRemark = StdStr(req.Remark);
            var strFlagActive = StdFlag(req.FlagActive);
            var strModelCode = StdParam(req.ModelCode);

            // 4. Kiểm tra ĐÚNG THỨ TỰ:
            // Bước 1: Mst_CarModel_CheckDB(ModelCode, FlagExist "1", "") (:2553-2559).
            // Nếu ModelCode rỗng hoặc không tìm thấy dòng -> Mst_CarModel_CheckDB_CarModelNotFound (:2558).
            var entity = string.IsNullOrEmpty(strModelCode)
                ? null
                : await _db.Set<Mst_CarModel>().FirstOrDefaultAsync(x =>
                    EF.Functions.Like((x.ModelCode ?? "").ToLower(), PortQuery.Equal(strModelCode), PortQuery.Esc));

            if (entity == null)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_CarModel_CheckDB_CarModelNotFound", "Check.ModelCode", strModelCode);
            }

            // Bước 2: Mst_CarBrand_CheckDB(BrandCode, FlagExist "1", "") (:2561-2567).
            // Nếu BrandCode rỗng hoặc không tìm thấy dòng -> Mst_CarBrand_CheckDB_BrandNotFound (:2566).
            // LƯU Ý: strFlagActiveListToCheck = "" -> KHÔNG kiểm FlagActive của hãng (khác Create #100): hãng inactive vẫn hợp lệ.
            var brandExists = !string.IsNullOrEmpty(strBrandCode) && await _db.Set<Mst_CarBrand>().AnyAsync(x =>
                EF.Functions.Like((x.BrandCode ?? "").ToLower(), PortQuery.Equal(strBrandCode), PortQuery.Esc));

            if (!brandExists)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_CarBrand_CheckDB_BrandNotFound", "Check.BrandCode", strBrandCode);
            }

            // 5. Cập nhật 4 cột cố định từ Ft_Cols_Upd ở gate + log (Master.Loyalty.cs:2576-2582):
            // ModelCode và NetworkID KHÔNG đổi.
            entity.ModelName = strModelName;
            entity.BrandCode = strBrandCode;
            entity.Remark = strRemark;
            entity.FlagActive = strFlagActive;
            entity.LogLUDTimeUTC = DateTime.UtcNow;
            entity.LogLUBy = "port";

            await _db.SaveChangesAsync();

            // 6. Commit transaction (Master.Loyalty.cs:2455 CommitSafety).
            await tx.CommitAsync();

            // 7. Thành công: gate :471-472 SetSuccess + Data = null.
            return Ok(new { Success = true, Data = (object?)null });
        }
        catch (Exception)
        {
            await tx.RollbackAsync();
            throw;
        }
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

    #region Helper chuẩn hoá input (05.Refs.Biz\Biz\Loyalty.TVO\Loyalty.TVO.Utils\Utils.cs)
    /// <summary>
    /// Chuẩn hoá tham số mã: Trim, ToUpper, rỗng -> null.
    /// Dùng cho mã (ModelCode, BrandCode, NetworkID...).
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
    /// Dùng cho tên, ghi chú (ModelName, Remark...). KHÔNG viết hoa (khác CarBrand).
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
