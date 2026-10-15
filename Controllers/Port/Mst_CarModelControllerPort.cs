using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MiniLoyalty.Data;
using MiniLoyalty.Models.Port;

namespace MiniLoyalty.Controllers.Port;

// ============================================================================================
// PORT 1:1 — màn danh mục dòng xe (Mst_CarModel) của dự án nguồn 2022.7.Loyalty (#97, #98, #99, #100, #101, #102, #103, #104).
//
// Nguồn controller (tầng gate):
//   Mst_CarModelController.cs:25-105   Search              -> WA_Mst_CarModel_Get
//   Mst_CarModelController.cs:106-216  GetAllActive        -> WA_Mst_CarModel_Get
//   Mst_CarModelController.cs:217-280  GetByModelCode      -> WA_Mst_CarModel_Get
//   Mst_CarModelController.cs:329-414  Create              -> WA_Mst_CarModel_Create
//   Mst_CarModelController.cs:418-485  Update              -> WA_Mst_CarModel_Update
//   Mst_CarModelController.cs:489-552  Delete              -> WA_Mst_CarModel_Delete (#102)
//   Mst_CarModelController.cs:554-618  DeleteMultiple      -> WA_Mst_CarModel_Delete (#103)
//   Mst_CarModelController.cs:622-766  Import              -> WA_Mst_CarModel_Create (#104)
//   Mst_CarModelController.cs:284-305  strWhereClause_Mst_CarModel (mệnh đề lọc Search)
//   Mst_CarModelController.cs:306-325  strWhereClause_Mst_CarModel_By_ModelCode (mệnh đề lọc GetByModelCode)
// Nguồn API:
//   MstCarModelController.cs:20-90     WA_Mst_CarModel_Create
//   MstCarModelController.cs:92-162    WA_Mst_CarModel_Update
//   MstCarModelController.cs:165-235   WA_Mst_CarModel_Delete
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
//   Master.Loyalty.cs:2593-2671        WAS_Mst_CarModel_Delete
//   Master.Loyalty.cs:2672-2808        Mst_CarModel_Delete
//   Master.Loyalty.cs:2810-2866        Mst_CarModel_DeleteX
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
//   * Delete: kiểm tra ModelCode rỗng tại gate -> invalid_ModelCode;
//     CheckDB(ModelCode, FlagExist "1") -> Mst_CarModel_CheckDB_CarModelNotFound;
//     GAP FK: Prm_CarRecommendSpec/Prm_CarNewSpec chua port;
//     xoá dòng -> Success = true, Data = null.
//   * DeleteMultiple: body là mảng [{ ModelCode }], lặp tuần tự từng phần tử trong 1 transaction riêng biệt.
//     Lỗi phần tử thứ k -> thoát vòng lặp ngay, "xóa một phần" là hành vi nguồn. Body rỗng -> Mst_CarModel_Delete.
//   * Import: nhận file .xlsx ([Consumes("multipart/form-data")]); file thiếu hoặc đuôi khác .xlsx -> Fail("Mst_CarModel_Import", "Message", "File excel import không hợp lệ");
//     Đọc qua PortExcel.ReadSheet: không có dòng -> "File excel import không có dữ liệu"; columnCount != 3 -> "File excel import không hợp lệ";
//     Kiểm tra TẤT CẢ dòng trước khi tạo: ModelCode rỗng -> "Mã Model không được trống", ModelName rỗng -> "Tên Model không được trống", BrandCode rỗng -> "Mã hiệu xe không được trống";
//     Khối kiểm tra trùng trong file bị comment trong nguồn — không port;
//     Duyệt từng dòng: gọi CreateXAsync độc lập (mỗi dòng 1 transaction riêng); lỗi dòng k dừng ngay và trả lỗi dòng đó (import một phần, các dòng trước k đã lưu);
//     GAP NetworkID: nguồn lấy orgUserUI.NetworkId từ user đăng nhập, đích chưa có user -> IConfiguration["Biz_NetworkID"] ?? "1001";
//     Thành công Data = null (:746-747).
//
// Bỏ qua có chủ đích (loại Đ):
//   - CheckGatewayAuthentication: gate :34, :226, :498, :574; API MstCarModelController.cs:259-264, :187-192
//   - CheckAuthorize: gate :35, :227, :499, :575 (hệ thống đích chưa có hệ phân quyền)
//   - Nguồn gọi bằng tài khoản hệ thống viết cứng (tài khoản + mật khẩu viết cứng — KHÔNG chép giá trị),
//     đích chưa có phân quyền; KHÔNG chép thông tin đăng nhập vào mã
//   - Ghi RequestLog/ReturnLog: Master.Loyalty.cs:1578-1586, 1676-1684, 2708-2715, 2798-2805
//   - Sys_User_CheckAuthorize: Master.Loyalty.cs:1592-1606, 2727-2741
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

/// <summary>
/// Port 1:1 <c>Delete</c> — body chỉ có ModelCode (Mst_CarModelController.cs:489-552).
/// </summary>
public class MstCarModelDeleteReq
{
    public string? ModelCode { get; set; }            // Mã dòng xe cần xoá — :489, :513
}

[ApiController]
[Route("api/mst_carmodel")]
[Produces("application/json")]
public class Mst_CarModelControllerPort : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public Mst_CarModelControllerPort(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
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

    // ============================================================================================
    // Delete — nguồn Mst_CarModelController.cs:489-552 (gate) -> MstCarModelController.cs:165-235 (API)
    //   -> Master.Loyalty.cs:2593-2671 (WAS_Mst_CarModel_Delete) -> :2672-2808 (Mst_CarModel_Delete)
    //   -> :2810-2866 (Mst_CarModel_DeleteX) -> :1843-1906 (Mst_CarModel_CheckDB).
    //
    // GIỮ ĐÚNG NGUỒN:
    //   * Kiểm tra ModelCode rỗng tại gate (Mst_CarModelController.cs:508, 538): ModelCode rỗng -> ném
    //     ServiceException("invalid_ModelCode", "invalid_ModelCode").
    //   * Tách phần biz sau bước kiểm rỗng thành DeleteXAsync(modelCode) (1 transaction/lần)
    //     để hạng mục DeleteMultiple sau này tái sử dụng.
    //   * Mở 1 transaction cho mỗi lần xóa (Master.Loyalty.cs:2705 _cf.db.BeginTransaction()).
    //   * Chuẩn hoá mã qua StdParam (:2836): Trim + ToUpper, rỗng -> null.
    //   * Mst_CarModel_CheckDB(ModelCode, FlagExist "1", "") (:2842-2847 -> :1843-1906):
    //     FlagExist "1" + không có dòng (hoặc strModelCode rỗng) -> ErridnLoyalty.Mst_CarModel_CheckDB_CarModelNotFound.
    //   * Ràng buộc khoá ngoại (GAP FK): Trong DDL nguồn, Prm_CarRecommendSpec.ModelCode
    //     (20221026.CreateDatabase.sql:91, FK_MstCarModel_PrmCRSpec) và Prm_CarNewSpec.ModelCode
    //     (20221017.z11.UpdDB.30.Promotion.sql:104, FK_MstCarModel_PrmCNSpec) có FK trỏ tới Mst_CarModel.
    //     CẢ HAI CHƯA CÓ ở đích -> KHÔNG giả lập.
    //     GAP: khi port Prm_CarRecommendSpec/Prm_CarNewSpec phai chan xoa -> Mst_CarModel_Delete.
    //   * Xoá dòng Mst_CarModel (Master.Loyalty.cs:2857 dtDB_Mst_CarModel.Rows[0].Delete()) và CommitSafety (:2771).
    //   * Thành công: gate :532-533 SetSuccess + Data = null.
    //
    // Bỏ qua có chủ đích (loại Đ):
    //   * CheckGatewayAuthentication/CheckAuthorize (gate :498-499), Sys_User_CheckAuthorize (:2727-2741):
    //     hệ thống đích chưa có hệ phân quyền.
    //   * Ghi RequestLog/ReturnLog (:2708-2715, :2798-2805).
    // ============================================================================================
    [HttpPost("delete")]
    public async Task<IActionResult> Delete([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] MstCarModelDeleteReq? req)
    {
        // 1. Kiểm tra mã rỗng tại gate (Mst_CarModelController.cs:508, 538):
        //    ModelCode rỗng -> ném ServiceException("invalid_ModelCode", "invalid_ModelCode")
        if (string.IsNullOrWhiteSpace(req?.ModelCode))
        {
            return PortResult.Fail("invalid_ModelCode", "Check.ModelCode", req?.ModelCode);
        }

        // 2. Thực hiện nghiệp vụ xóa qua DeleteXAsync (1 transaction)
        var err = await DeleteXAsync(req.ModelCode);
        if (err != null)
        {
            return err;
        }

        // 3. Thành công: gate Mst_CarModelController.cs:532-533 SetSuccess + Data = null
        return Ok(new
        {
            Success = true,
            Data = (object?)null
        });
    }

    /// <summary>
    /// Port 1:1 DeleteMultiple action từ gate Mst_CarModelController.cs:554-618 (#103).
    /// Chuỗi gọi: gate :554-618 -> lặp qua danh sách Lst_Mst_CarModel, mỗi phần tử gọi
    /// WA_Mst_CarModel_Delete (:585-602) -> biz WAS_Mst_CarModel_Delete -> Mst_CarModel_Delete -> Mst_CarModel_DeleteX.
    ///
    /// Request JSON: mảng [{ ModelCode }].
    /// 1. Kiểm tra body rỗng / null: Gate Mst_CarModelController.cs:564-571
    ///    if (!string.IsNullOrEmpty(strJson)) { ... } else { throw new ServiceException("strJson null"); }
    ///    Khi body null hoặc rỗng -> trả về 400 ErridnLoyalty.Mst_CarModel_Delete với Check.strJson = null.
    /// 2. Danh sách rỗng `[]`: vòng lặp foreach không chạy, trả về Ok(Success = true, Data = null).
    /// 3. Lặp tuần tự qua từng phần tử trong danh sách:
    ///    Mỗi lần lặp là một lời gọi độc lập tới chuỗi nghiệp vụ Delete, sử dụng TRANSACTION RIÊNG
    ///    (Master.Loyalty.cs:2705 _cf.db.BeginTransaction() và :2771 TDALUtils.DBUtils.CommitSafety).
    ///    - KHÔNG có bước kiểm tra ModelCode rỗng ở tầng gate như Delete đơn (:508, 538);
    ///      khi ModelCode rỗng, lỗi phát sinh ở tầng nghiệp vụ Mst_CarModel_CheckDB
    ///      -> trả về 400 ErridnLoyalty.Mst_CarModel_CheckDB_CarModelNotFound.
    ///    - Hành vi "xóa một phần" (nguồn Mst_CarModelController.cs:585-611):
    ///      Nếu phần tử thứ k gặp lỗi, exception được ném ra và thoát khỏi vòng lặp ngay lập tức
    ///      nhảy vào khối catch (ex) (:607-611).
    ///      Do mỗi phần tử chạy trên một transaction riêng đã được commit, các phần tử TRƯỚC k
    ///      ĐÃ BỊ XÓA VÀ KHÔNG BỊ ROLLBACK; các phần tử SAU k hoàn toàn không được xử lý.
    ///      "xóa một phần" là hành vi nguồn.
    /// 4. Thành công: gate :604-605 trả objJsonResultEntry.Data = null -> Ok(new { Success = true, Data = (object?)null }).
    ///
    /// Bỏ qua có chủ đích (loại Đ):
    /// - CheckGatewayAuthentication: gate Mst_CarModelController.cs:574
    /// - CheckAuthorize: gate Mst_CarModelController.cs:575 (hệ thống đích chưa có hệ phân quyền)
    /// - Sys_User_CheckAuthorize: Master.Loyalty.cs:2727-2741
    /// - Ghi RequestLog/ReturnLog: Master.Loyalty.cs:2708-2715, 2798-2805
    /// </summary>
    [HttpPost("deletemultiple")]
    public async Task<IActionResult> DeleteMultiple([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] List<MstCarModelDeleteReq>? req)
    {
        // 1. Kiểm tra body rỗng / null: Gate Mst_CarModelController.cs:564-571
        // if (!string.IsNullOrEmpty(strJson)) { ... } else { throw new ServiceException("strJson null"); }
        if (req == null)
        {
            return PortResult.Fail("Mst_CarModel_Delete", "Check.strJson", null);
        }

        // 2. Lặp tuần tự qua từng phần tử (gate Mst_CarModelController.cs:585-602).
        // Mỗi phần tử chạy trong 1 transaction riêng biệt (hàm DeleteXAsync).
        // Nếu phần tử thứ k gặp lỗi -> return ngay lỗi đó (hành vi nguồn :585-611:
        // "xóa một phần" là hành vi nguồn: các phần tử trước k đã commit thành công
        // vào DB và KHÔNG bị rollback; các phần tử sau k không được xử lý).
        foreach (var item in req)
        {
            var err = await DeleteXAsync(item?.ModelCode);
            if (err != null)
            {
                return err;
            }
        }

        // 3. Thành công: gate Mst_CarModelController.cs:604-605
        return Ok(new
        {
            Success = true,
            Data = (object?)null
        });
    }

    /// <summary>
    /// Nghiệp vụ xóa 1 dòng xe — 1 transaction riêng biệt.
    /// Nguồn: WA_Mst_CarModel_Delete (MstCarModelController.cs:165-235) -> WAS_Mst_CarModel_Delete (Master.Loyalty.cs:2593-2671)
    /// -> Mst_CarModel_Delete (Master.Loyalty.cs:2672-2808) -> Mst_CarModel_DeleteX (Master.Loyalty.cs:2810-2866)
    /// -> Mst_CarModel_CheckDB (Master.Loyalty.cs:1843-1906).
    /// Dùng chung cho Delete (#102) và DeleteMultiple (#103, gọi từng phần tử).
    /// </summary>
    private async Task<IActionResult?> DeleteXAsync(string? modelCode)
    {
        // 1. Mở 1 transaction cho nghiệp vụ xóa (Master.Loyalty.cs:2705 _cf.db.BeginTransaction()).
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // 2. Chuẩn hoá mã qua StdParam: Trim, ToUpper, rỗng -> null (Master.Loyalty.cs:2836)
            string? strModelCode = StdParam(modelCode);

            // 3. Mst_CarModel_CheckDB(ModelCode, Flag "1", "") (Master.Loyalty.cs:2842-2847 -> :1843-1906):
            //    FlagExist "1" + KHÔNG có dòng (hoặc strModelCode rỗng) -> ErridnLoyalty.Mst_CarModel_CheckDB_CarModelNotFound.
            if (string.IsNullOrEmpty(strModelCode))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_CarModel_CheckDB_CarModelNotFound", "Check.ModelCode", strModelCode);
            }

            // Tìm dòng bằng Like + PortQuery.Equal (hạ chữ 2 phía, SQL Server collation không phân biệt hoa thường)
            var entity = await _db.Set<Mst_CarModel>().FirstOrDefaultAsync(x =>
                EF.Functions.Like((x.ModelCode ?? "").ToLower(), PortQuery.Equal(strModelCode), PortQuery.Esc));

            if (entity == null)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_CarModel_CheckDB_CarModelNotFound", "Check.ModelCode", strModelCode);
            }

            // 4. Ràng buộc khoá ngoại (GAP FK):
            //    Trong DDL nguồn, Prm_CarRecommendSpec.ModelCode (20221026.CreateDatabase.sql:91, FK_MstCarModel_PrmCRSpec)
            //    và Prm_CarNewSpec.ModelCode (20221017.z11.UpdDB.30.Promotion.sql:104, FK_MstCarModel_PrmCNSpec) có FK trỏ tới Mst_CarModel.
            //    Cả hai bảng này CHƯA CÓ ở đích -> KHÔNG giả lập.
            //    GAP: khi port Prm_CarRecommendSpec/Prm_CarNewSpec phai chan xoa -> Mst_CarModel_Delete.

            // 5. Xoá dòng Mst_CarModel (Master.Loyalty.cs:2857 dtDB_Mst_CarModel.Rows[0].Delete())
            _db.Set<Mst_CarModel>().Remove(entity);
            await _db.SaveChangesAsync();

            // 6. Commit transaction (Master.Loyalty.cs:2771 TDALUtils.DBUtils.CommitSafety)
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
    // Import — nguồn gate Mst_CarModelController.cs:622-766 -> API WA_Mst_CarModel_Create
    // (MstCarModelController.cs:20-90) -> biz WAS_Mst_CarModel_Create (Master.Loyalty.cs:1908-1991)
    // -> Mst_CarModel_Create (Master.Loyalty.cs:1992-2141) -> Mst_CarModel_CreateX (Master.Loyalty.cs:2142-2250).
    //
    // GIẢ ĐỊNH (kế thừa từ Data/PortExcel.cs):
    //   - Định dạng file Excel: dòng 1 = tiêu đề (đúng file template), dữ liệu bắt đầu từ ô A2 (firstDataRow = 2).
    //   - Cột ánh xạ theo vị trí: cột 1 = ModelCode, cột 2 = ModelName, cột 3 = BrandCode (GetImportDicColumsTemplate có 3 cột:
    //     ModelCode -> "Mã Model", ModelName -> "Tên Model(*)", BrandCode -> "Mã hiệu xe" :899-906).
    //
    // Quy trình và thứ tự kiểm tra:
    //   1. Kiểm tra file upload: gate Mst_CarModelController.cs:646-651
    //      - Thiếu file hoặc file rỗng hoặc đuôi file != ".xlsx" -> Fail("Mst_CarModel_Import", "Message", "File excel import không hợp lệ")
    //   2. Đọc file bằng PortExcel.ReadSheet(stream, new[] { "ModelCode", "ModelName", "BrandCode" }):
    //      - Bảng không có dòng dữ liệu (table == null || table.Rows.Count == 0) -> Fail("Mst_CarModel_Import", "Message", "File excel import không có dữ liệu") (:663, :751)
    //      - Số cột tiêu đề != GetImportDicColumsTemplate().Count (3 cột) -> Fail("Mst_CarModel_Import", "Message", "File excel import không hợp lệ") (:665-667)
    //   3. Kiểm tra TẤT CẢ các dòng TRƯỚC khi tạo (#region["Check null"] :672-699):
    //      - ModelCode rỗng / khoảng trắng -> Fail("Mst_CarModel_Import", "Message", "Mã Model không được trống") (:675-678)
    //      - ModelName rỗng / khoảng trắng -> Fail("Mst_CarModel_Import", "Message", "Tên Model không được trống") (:680-683)
    //      - BrandCode rỗng / khoảng trắng -> Fail("Mst_CarModel_Import", "Message", "Mã hiệu xe không được trống") (:685-688)
    //      Khối kiểm tra trùng trong file (#region["Check duplicate"] :701-718) bị COMMENT trong nguồn — KHÔNG port.
    //      Nếu có bất kỳ dòng nào vi phạm, dừng ngay và KHÔNG có dòng nào được tạo vào CSDL.
    //   4. Lưu từng dòng (hành vi "import một phần"):
    //      - NetworkID: GAP nguồn lấy NetworkID = orgUserUI.NetworkId (:728) từ user đăng nhập;
    //        đích chưa có user -> dùng IConfiguration["Biz_NetworkID"] ?? "1001".
    //      - Duyệt từng dòng theo thứ tự, gọi CreateXAsync(modelCode, networkId, brandCode, modelName, remark: null) riêng cho mỗi dòng
    //        (mỗi dòng 1 transaction biz riêng biệt: gate :725-745).
    //      - Nếu dòng thứ k gặp lỗi (vd trùng mã Mst_CarModel_CheckDB_CarModelExist, hãng xe không tồn tại Mst_CarBrand_CheckDB_BrandNotFound,
    //        hãng xe không hoạt động Mst_CarBrand_CheckDB_FlagActiveNotMatched), vòng lặp dừng ngay lập tức, trả về đúng lỗi của CreateXAsync cho dòng k.
    //      - CÁC DÒNG TRƯỚC k ĐÃ ĐƯỢC COMMIT VÀO DB (không rollback các dòng đã tạo trước đó).
    //      - Các dòng sau k không được duyệt.
    //   5. Thành công: gate :746-747 trả SetSuccess + Data = null -> Ok(new { Success = true, Data = (object?)null }).
    //
    // Bỏ qua có chủ đích (loại Đ):
    //   - CheckGatewayAuthentication, CheckAuthorize (:633-634)
    //   - FileUtils.SaveTempFile (:654) -> đọc trực tiếp từ stream
    //   - Session người dùng orgUserUI (:632, :728, :734-738).
    // ============================================================================================
    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Import(IFormFile? file)
    {
        // 1. Kiểm tra file upload: gate Mst_CarModelController.cs:646-651
        if (file == null || file.Length == 0)
        {
            return PortResult.Fail("Mst_CarModel_Import", "Message", "File excel import không hợp lệ");
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".xlsx")
        {
            return PortResult.Fail("Mst_CarModel_Import", "Message", "File excel import không hợp lệ");
        }

        // 2. Đọc bảng tính bằng PortExcel.ReadSheet (giả định định dạng file Excel từ PortExcel)
        int columnCount;
        List<Dictionary<string, string?>> rows;
        try
        {
            using var stream = file.OpenReadStream();
            (columnCount, rows) = PortExcel.ReadSheet(stream, new[] { "ModelCode", "ModelName", "BrandCode" });
        }
        catch
        {
            return PortResult.Fail("Mst_CarModel_Import", "Message", "File excel import không hợp lệ");
        }

        // Kiểm tra có dữ liệu hay không: gate :663, :751 (table == null || table.Rows.Count == 0)
        if (rows.Count == 0)
        {
            return PortResult.Fail("Mst_CarModel_Import", "Message", "File excel import không có dữ liệu");
        }

        // Kiểm tra số cột dòng tiêu đề so với GetImportDicColumsTemplate().Count (3 cột): gate :665-667
        if (columnCount != GetImportDicColumsTemplate().Count)
        {
            return PortResult.Fail("Mst_CarModel_Import", "Message", "File excel import không hợp lệ");
        }

        // 3. Kiểm tra TẤT CẢ các dòng TRƯỚC khi tạo (Check null: gate Mst_CarModelController.cs:672-699)
        foreach (var r in rows)
        {
            var modelCode = r.GetValueOrDefault("ModelCode");
            if (string.IsNullOrWhiteSpace(modelCode))
            {
                return PortResult.Fail("Mst_CarModel_Import", "Message", "Mã Model không được trống");
            }

            var modelName = r.GetValueOrDefault("ModelName");
            if (string.IsNullOrWhiteSpace(modelName))
            {
                return PortResult.Fail("Mst_CarModel_Import", "Message", "Tên Model không được trống");
            }

            var brandCode = r.GetValueOrDefault("BrandCode");
            if (string.IsNullOrWhiteSpace(brandCode))
            {
                return PortResult.Fail("Mst_CarModel_Import", "Message", "Mã hiệu xe không được trống");
            }
        }

        // 4. Lưu từng dòng tuần tự: mỗi dòng 1 transaction riêng (hành vi "import một phần": gate :725-745)
        // GAP: nguồn lấy NetworkID = orgUserUI.NetworkId (:728) từ user đăng nhập; đích chưa có user -> dùng IConfiguration["Biz_NetworkID"] ?? "1001"
        var networkId = _config["Biz_NetworkID"] ?? "1001";
        foreach (var r in rows)
        {
            var modelCode = r.GetValueOrDefault("ModelCode");
            var modelName = r.GetValueOrDefault("ModelName");
            var brandCode = r.GetValueOrDefault("BrandCode");

            var err = await CreateXAsync(modelCode, networkId, brandCode, modelName, remark: null);
            if (err != null)
            {
                // Lỗi ở dòng k -> thoát ngay, trả đúng lỗi của Create, các dòng trước k đã tạo thành công
                return err;
            }
        }

        // 5. Thành công: gate :746-747 SetSuccess + Data = null
        return Ok(new
        {
            Success = true,
            Data = (object?)null
        });
    }

    /// <summary>
    /// Từ điển 3 cột mẫu Import (Mst_CarModelController.cs:899-906 GetImportDicColumsTemplate):
    /// ModelCode -> "Mã Model", ModelName -> "Tên Model(*)", BrandCode -> "Mã hiệu xe".
    /// </summary>
    private static Dictionary<string, string> GetImportDicColumsTemplate()
    {
        return new Dictionary<string, string>
        {
            { "ModelCode", "Mã Model" },
            { "ModelName", "Tên Model(*)" },
            { "BrandCode", "Mã hiệu xe" }
        };
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
