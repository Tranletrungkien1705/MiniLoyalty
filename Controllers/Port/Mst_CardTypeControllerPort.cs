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
using MiniLoyalty.Models;
using MiniLoyalty.Models.Port;

namespace MiniLoyalty.Controllers.Port;

// ============================================================================================
// PORT 1:1 — màn danh mục loại thẻ của dự án nguồn 2022.7.Loyalty.
//
// Nguồn controller (tầng gate):
//   Mst_CardTypeController.cs:25-105   Search       ->  WA_Mst_CardType_Get
//   Mst_CardTypeController.cs:106-215  GetAllActive ->  WA_Mst_CardType_Get
//   Mst_CardTypeController.cs:216-279  GetByCardType -> WA_Mst_CardType_Get
//   Mst_CardTypeController.cs:329-414  Create       ->  WA_Mst_CardType_Create
//   Mst_CardTypeController.cs:418-485  Update       ->  WA_Mst_CardType_Update
//   Mst_CardTypeController.cs:489-552  Delete       ->  WA_Mst_CardType_Delete
//   Mst_CardTypeController.cs:554-618  DeleteMultiple -> WA_Mst_CardType_Delete
//   Mst_CardTypeController.cs:622-768  Import       ->  WA_Mst_CardType_Create
//   Mst_CardTypeController.cs:773-851  Export       ->  WA_Mst_CardType_Get
//   Mst_CardTypeController.cs:854-886  ExportTemplate
//   Mst_CardTypeController.cs:283-308  strWhereClause_Mst_CardType (dựng mệnh đề lọc)
// Nguồn API:  MstCardTypeController.cs:239-308  WA_Mst_CardType_Get
//             MstCardTypeController.cs:21-91    WA_Mst_CardType_Create
//             MstCardTypeController.cs:93-163   WA_Mst_CardType_Update
//             MstCardTypeController.cs:166-236  WA_Mst_CardType_Delete
// Nguồn biz:  Master.Loyalty.cs:5329 (WAS_Mst_CardType_Get) -> :5430 (Mst_CardType_Get) -> :5583-5737 (Mst_CardType_GetX)
//             Master.Loyalty.cs:5738 (WAS_Mst_CardType_Create) -> :5821 (Mst_CardType_Create) -> :5968 (Mst_CardType_CreateX)
//             Master.Loyalty.cs:6100 (WAS_Mst_CardType_Update) -> :6185 (Mst_CardType_Update) -> :6339 (Mst_CardType_UpdateX)
//             Master.Loyalty.cs:6428 (WAS_Mst_CardType_Delete) -> :6507 (Mst_CardType_Delete) -> :6645 (Mst_CardType_DeleteX)
//
// GIỮ ĐÚNG NGUỒN:
//   * Endpoint là POST (nguồn [AcceptVerbs("POST")]).
//   * Lọc KeyWord: like '%x%' trên CardTypeName (:290).
//   * Lọc FlagActive: danh sách `in ('a','b')` (:299-304).
//   * Chia trang theo CardType asc (:5643) rồi sắp trang theo Value asc (:5683).
//   * Delete: kiểm tra rỗng -> invalid_CartType; CheckDB -> Mst_CardType_CheckDB_CardTypeNotFound;
//     giả lập 4 FK đích -> Mst_CardType_Delete; xoá dòng -> Success = true, Data = null.
//   * DeleteMultiple: body là mảng [{ CardType }], lặp tuần tự từng phần tử trong 1 transaction riêng biệt.
//     Lỗi phần tử thứ k -> thoát vòng lặp ngay, "xóa một phần" là hành vi nguồn.
//   * Import: nhận file .xlsx ([Consumes("multipart/form-data")]); file thiếu hoặc đuôi khác .xlsx -> Fail("Mst_CardType_Import", "Message", "File excel import không hợp lệ");
//     Đọc qua PortExcel.ReadSheet: không có dòng -> "File excel import không có dữ liệu"; columnCount != 3 -> "File excel import không hợp lệ";
//     Kiểm tra TẤT CẢ dòng trước khi tạo: CardType rỗng -> "Mã hạng thẻ không được trống", CardTypeName rỗng -> "Tên hạng thẻ không được trống", Value rỗng -> "Giá trị không được trống";
//     Duyệt từng dòng: gọi CreateXAsync độc lập (mỗi dòng 1 transaction riêng); lỗi dòng k dừng ngay và trả lỗi dòng đó (import một phần, các dòng trước k đã lưu);
//     Thành công Data = null (:748-750).
//   * ExportTemplate: gate Mst_CardTypeController.cs:854-886; không body, không kiểm quyền, không truy vấn DB;
//     khởi tạo danh sách rỗng List<Mst_CardType> (:862); từ điển cột GetImportDicColumsTemplate() (:864, :1003-1011) gồm 3 cột:
//     CardType ("Mã hạng thẻ (*)"), CardTypeName ("Tên hạng thẻ"), Value ("Thứ tự");
//     sinh đường dẫn file qua PortExcel.GenExcelExportFilePath(webRoot, "Mst_CardType") (:866), sheet "Mst_CardType" (:867);
//     ghi file qua PortExcel.ExportToExcelFromList; trả về Success = true, Data = { Url = baseAddress + virtualPath } (:868-872).
// ============================================================================================

/// <summary>Port 1:1 <c>Mst_CardTypeSearch</c> — tham số lọc + phân trang của màn.</summary>
public class MstCardTypeSearchReq
{
    public string? KeyWord { get; set; }             // Từ khoá tìm theo tên loại thẻ — Mst_CardTypeController.cs:54
    public string? FlagActive { get; set; }          // Cờ hoạt động, danh sách phân cách dấu phẩy — :55
    public int Ft_PageIndex { get; set; } = 0;       // Trang (0-based) — :46
    public int Ft_PageSize { get; set; } = 10;       // Số dòng/trang — :47
}

/// <summary>Port 1:1 <c>GetByCardType</c> — body chỉ có CardType (Mst_CardTypeController.cs:216-279).</summary>
public class MstCardTypeGetByCardTypeReq
{
    public string? CardType { get; set; }             // Mã loại thẻ cần lấy — :238-241
}

/// <summary>
/// Port 1:1 <c>Create</c> — body JSON 4 trường của model nguồn (Mst_CardTypeController.cs:329-414).
/// Nguồn deserialize thẳng vào TModelLoyaltys.Mst_CardType (mọi cột là object) rồi truyền
/// CardType/NetworkID/CardTypeName/Value xuống biz (Master.Loyalty.cs:5787-5790).
/// Value để <c>JsonElement?</c> để chuyển đổi như <c>Convert.ToDouble(objValue)</c> của nguồn
/// (chuỗi không phải số -> lỗi; thiếu -> null -> 0).
/// </summary>
public class MstCardTypeCreateReq
{
    public string? CardType { get; set; }             // Mã loại thẻ — :5787
    public string? NetworkID { get; set; }            // NetworkID lấy từ JSON, KHÔNG từ cấu hình — :5788
    public string? CardTypeName { get; set; }         // Tên loại thẻ — :5789
    public System.Text.Json.JsonElement? Value { get; set; }  // Giá trị/ngưỡng — :5790 (Convert.ToDouble)
}

/// <summary>
/// Port 1:1 <c>Update</c> — body JSON của model nguồn (Mst_CardTypeController.cs:418-485).
/// Nguồn deserialize thẳng vào TModelLoyaltys.Mst_CardType (mọi cột là object) rồi truyền
/// CardType/NetworkID/CardTypeName/Value/FlagActive xuống biz (Master.Loyalty.cs:6148-6152).
/// Value để <c>JsonElement?</c> để chuyển đổi như <c>Convert.ToDouble(objValue)</c> của nguồn
/// (chuỗi không phải số -> lỗi; thiếu -> null -> 0).
/// </summary>
public class MstCardTypeUpdateReq
{
    public string? CardType { get; set; }             // Mã loại thẻ — :6148
    public string? NetworkID { get; set; }            // NetworkID — :6149
    public string? CardTypeName { get; set; }         // Tên loại thẻ — :6150
    public System.Text.Json.JsonElement? Value { get; set; }  // Giá trị/ngưỡng — :6151 (Convert.ToDouble)
    public string? FlagActive { get; set; }           // Cờ hoạt động — :6152 (StdFlag)
}

/// <summary>
/// Port 1:1 <c>Delete</c> — body chỉ có CardType (Mst_CardTypeController.cs:489-552).
/// </summary>
public class MstCardTypeDeleteReq
{
    public string? CardType { get; set; }             // Mã loại thẻ cần xoá — :489, :513
}

/// <summary>Port 1:1 <c>Export</c> — body nhận OrgID, KeyWord, FlagActive (Mst_CardTypeController.cs:773, 794-799).</summary>
public class MstCardTypeExportReq
{
    public string? OrgID { get; set; }               // Tổ chức — gate nhận (:773, :797) nhưng strWhereClause_Mst_CardType (:283-308) KHÔNG lọc OrgID
    public string? KeyWord { get; set; }             // Từ khoá tìm theo tên loại thẻ (CardTypeName) — :773, :796, :290
    public string? FlagActive { get; set; }          // Cờ hoạt động, danh sách phân cách dấu phẩy — :773, :798, :298-305
}

[ApiController]
[Route("api/mst_cardtype")]
[Produces("application/json")]
public class Mst_CardTypeControllerPort(AppDbContext db, IConfiguration config, IWebHostEnvironment env) : ControllerBase
{
    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody] MstCardTypeSearchReq? req)
    {
        var r = req ?? new MstCardTypeSearchReq();
        var pageIndex = Math.Max(0, r.Ft_PageIndex);
        var pageSize = r.Ft_PageSize;

        // Search gọi GetXAsync với start = PageIndex*PageSize, count = PageSize (giữ nguyên hành vi cũ).
        var start = (long)pageIndex * pageSize;
        var (itemCount, dataList) = await GetXAsync(r.KeyWord, r.FlagActive, start, pageSize, null);

        // PageSize=0 -> DataList rỗng (nguồn: nFilterRecordEnd = start-1, :5594 + gate).
        // PageCount = 0 khi DataList rỗng, ngược lại ceil(ItemCount / ps) với ps = PageSize (PageSize=0 thì 10) — gate :80-88.
        var ps = pageSize == 0 ? 10 : pageSize;
        var pageCount = dataList.Count == 0 ? 0 : (itemCount % ps == 0 ? itemCount / ps : itemCount / ps + 1);

        return Ok(new { Success = true, Data = new PageInfo<object>(dataList, itemCount, pageCount, pageIndex, pageSize) });
    }

    // ============================================================================================
    // GetAllActive — nguồn Mst_CardTypeController.cs:106-215.
    // Gate gán OrgID (:134) và FlagActive = "1" (:135) rồi gọi strWhereClause_Mst_CardType (:283-308).
    // LƯU Ý: hàm lọc :283-308 CHỈ dùng KeyWord (like) và FlagActive (in) — KHÔNG có nhánh nào lọc OrgID,
    //        nên OrgID gán ở :134 không đi vào mệnh đề WHERE ⇒ port KHÔNG lọc OrgID.
    // Ft_RecordStart = "0", Ft_RecordCount = "123456000" (BaseController.cs:30-31).
    // Trả Data = null khi rỗng (:129, :155-157, :193-202).
    // ============================================================================================
    [HttpPost("getallactive")]
    public async Task<IActionResult> GetAllActive()
    {
        // Không body: lọc FlagActive = "1", lấy từ dòng 0 với RecordCount = 123456000 (BaseController.cs:30-31).
        var (_, rows) = await GetXAsync(null, "1", 0, 123456000, null);

        // Nguồn: listMst_CardType = null khi rỗng (:129), chỉ gán khi Count > 0 (:155-157), Data = listMst_CardType (:193-202).
        return Ok(new { Success = true, Data = rows.Count > 0 ? rows : null });
    }

    // ============================================================================================
    // GetByCardType — nguồn Mst_CardTypeController.cs:216-279.
    // Gate: nếu CardType rỗng thì KHÔNG truy vấn (:236), objMst_CardType giữ null (:235) ⇒ Data = null (:266).
    // Ngược lại gán CardType (:238-241) rồi gọi strWhereClause_Mst_CardType_By_CardType (:309-325)
    // (lọc `CardType = 'x'` — KHÔNG truyền FlagActive nên hàng FlagActive '0' VẪN lấy được).
    // Ft_RecordStart = "0", Ft_RecordCount = "1" (:253-254) ⇒ lấy đúng 1 dòng, trả Lst_Mst_CardType[0] (:260-263).
    // ============================================================================================
    [HttpPost("getbycardtype")]
    public async Task<IActionResult> GetByCardType([FromBody] MstCardTypeGetByCardTypeReq? req)
    {
        var cardType = req?.CardType;

        // CardType rỗng ⇒ nguồn không truy vấn (:236) và trả Data = null (:266).
        if (string.IsNullOrWhiteSpace(cardType))
        {
            return Ok(new { Success = true, Data = (object?)null });
        }

        // Lấy 1 dòng theo CardType (Ft_RecordStart = "0", Ft_RecordCount = "1" — :253-254).
        var (_, rows) = await GetXAsync(null, null, 0, 1, cardTypeEqual: cardType);

        // Nguồn trả Lst_Mst_CardType[0] khi có dòng, ngược lại null (:260-263).
        return Ok(new { Success = true, Data = rows.Count > 0 ? rows[0] : null });
    }

    /// <summary>
    /// Lọc + đếm + chia trang + sắp Value — tách từ Search để dùng chung cho Search và GetAllActive.
    /// Trả (itemCount, rows): itemCount = tổng số dòng sau lọc (MyCount, :5647); rows = trang đã sắp Value asc (:5683).
    /// recordCount &lt;= 0 ⇒ 0 dòng (nguồn: nFilterRecordEnd = start-1, :5616).
    /// </summary>
    private async Task<(int itemCount, List<object> rows)> GetXAsync(string? keyWord, string? flagActiveCsv, long recordStart, long recordCount, string? cardTypeEqual)
    {
        var q = db.Set<Mst_CardType>().AsNoTracking().AsQueryable();

        // KeyWord -> like '%x%' trên CardTypeName (Mst_CardTypeController.cs:290).
        // Nguồn chạy SQL Server collation KHÔNG phân biệt hoa/thường ⇒ hạ chữ cả 2 phía rồi LIKE (bẫy #18/#31).
        if (!string.IsNullOrWhiteSpace(keyWord))
        {
            var kw = PortQuery.Contains(keyWord);
            q = q.Where(x => EF.Functions.Like((x.CardTypeName ?? "").ToLower(), kw, PortQuery.Esc));
        }

        // CardType -> `CardType = 'x'` (Mst_CardTypeController.cs:316 — strWhereClause_Mst_CardType_By_CardType).
        // Nguồn dùng `=` trên SQL Server collation KHÔNG phân biệt hoa/thường ⇒ Like KHÔNG wildcard, hạ chữ 2 phía.
        if (!string.IsNullOrWhiteSpace(cardTypeEqual))
        {
            var ct = PortQuery.Equal(cardTypeEqual);
            q = q.Where(x => EF.Functions.Like((x.CardType ?? "").ToLower(), ct, PortQuery.Esc));
        }

        // FlagActive -> `in ('a','b')` (Mst_CardTypeController.cs:299-304): tách dấu phẩy, bỏ khoảng trắng, hạ chữ.
        if (!string.IsNullOrWhiteSpace(flagActiveCsv))
        {
            var ds = flagActiveCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => s.ToLowerInvariant()).ToList();
            q = q.Where(x => ds.Contains((x.FlagActive ?? "").ToLower()));
        }

        // Chia trang theo CardType asc (Master.Loyalty.cs:5643 — `order by mcb.CardType asc`).
        var ordered = q.OrderBy(x => x.CardType);
        var itemCount = await ordered.CountAsync();

        // MyIdxSeq = số thứ tự 0-based trong thứ tự CardType (identity(bigint,0,1) ở :5637).
        // Lấy dòng [recordStart, recordStart+recordCount) theo thứ tự CardType.
        // (EF không dịch được Select kèm chỉ số dòng ⇒ cắt trang bằng Skip/Take rồi tự gán MyIdxSeq trong bộ nhớ.)
        var page = recordCount <= 0
            ? new List<Mst_CardType>()
            : await ordered.Skip((int)recordStart).Take((int)recordCount).ToListAsync();

        // Rồi sắp trang đó theo Value asc (Master.Loyalty.cs:5683 — `order by mcb.Value asc`) trước khi trả về.
        var rows = page
            .Select((x, i) => new { x, idx = recordStart + i })
            .OrderBy(t => t.x.Value)
            .Select(t => (object)new
            {
                MyIdxSeq = t.idx,
                t.x.CardType,
                t.x.NetworkID,
                t.x.CardTypeName,
                t.x.Value,
                t.x.FlagActive,
                t.x.LogLUDTimeUTC,
                t.x.LogLUBy,
            })
            .ToList();

        return (itemCount, rows);
    }

    // ============================================================================================
    // Create — nguồn Mst_CardTypeController.cs:329-414 (gate) -> MstCardTypeController.cs:21-91 (API)
    //   -> Master.Loyalty.cs:5738 (WAS_Mst_CardType_Create) -> :5821-5967 (Mst_CardType_Create)
    //   -> :5968-6099 (Mst_CardType_CreateX) -> :5264-5325 (Mst_CardType_CheckDB).
    //
    // GIỮ ĐÚNG NGUỒN:
    //   * Body JSON 4 trường: CardType, NetworkID, CardTypeName, Value (:5787-5790).
    //     NetworkID lấy TỪ JSON, KHÔNG từ cấu hình.
    //   * Chuẩn hoá (:5999-6002): CardType = StdParam (Trim+ToUpper, rỗng->null);
    //     CardTypeName = StdStr (Trim+Normalize, rỗng->null); NetworkID = StdParam;
    //     Value = Convert.ToDouble(objValue) (null -> 0; chuỗi không phải số -> lỗi).
    //   * Kiểm tra THEO THỨ TỰ (:6011-6066):
    //       1) CardType null/rỗng -> Mst_CardType_Create_InvalidCardType
    //       2) CheckDB(CardType, FlagExist "0") -> Mst_CardType_CheckDB_CardTypeExist nếu đã có
    //       3) Value <= 0 -> Mst_CardType_Create_InvalidValue
    //       4) Đã có dòng khác CÙNG Value -> Mst_CardType_Create_InvalidValue
    //   * Lưu (:6076-6082): CardType, CardTypeName, NetworkID, Value, FlagActive = "1",
    //     LogLUDTimeUTC = UtcNow, LogLUBy = user.
    //   * Cột NOT NULL trong DDL: CardType, NetworkID, CardTypeName. Nguồn thiếu NetworkID/CardTypeName
    //     -> SQL Server báo lỗi khi SaveData, bắt lại thành mã mặc định Mst_CardType_Create (:5830).
    //
    // Bỏ qua có chủ đích (loại Đ):
    //   * CheckGatewayAuthentication/CheckAuthorize (gate :349-350), Sys_User_CheckAuthorize (:5883-5896):
    //     hệ thống đích chưa có hệ phân quyền.
    //   * Ghi RequestLog/ReturnLog (:5864-5871, :5957-5964).
    //   * LogLUBy gán cố định "port" do đích chưa có session người dùng (:6082).
    // ============================================================================================
    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] MstCardTypeCreateReq? req)
    {
        // 1. Body rỗng/null: gate :339-346 ném ServiceException("strJson null") -> mã mặc định Mst_CardType_Create.
        if (req == null)
        {
            return PortResult.Fail("Mst_CardType_Create", "Check.strJson", null);
        }

        // Gọi CreateXAsync thực hiện nghiệp vụ tạo (1 transaction / lần)
        var err = await CreateXAsync(req.CardType, req.NetworkID, req.CardTypeName, req.Value);
        if (err != null)
        {
            return err;
        }

        // 8. Thành công: gate :400-401 SetSuccess + Data = null.
        return Ok(new { Success = true, Data = (object?)null });
    }

    /// <summary>
    /// Nghiệp vụ tạo 1 loại thẻ — 1 transaction riêng biệt. Dùng chung cho Create (#90) và Import (#94, gọi từng dòng).
    /// Nguồn: WA_Mst_CardType_Create (MstCardTypeController.cs:21-91) -> WAS_Mst_CardType_Create (Master.Loyalty.cs:5738-5820)
    /// -> Mst_CardType_Create (Master.Loyalty.cs:5821-5967) -> Mst_CardType_CreateX (Master.Loyalty.cs:5968-6099)
    /// -> Mst_CardType_CheckDB (Master.Loyalty.cs:5264-5325).
    ///
    /// Thứ tự kiểm tra:
    ///   1. Chuẩn hoá: strCardType = StdParam(cardType), strCardTypeName = StdStr(name), strNetworkID = StdParam(networkId)
    ///   2. Parse Value = ConvertToDouble(valueRaw) -> nếu lỗi ném mã ErridnLoyalty.Mst_CardType_Create (:6002)
    ///   3. CardType rỗng -> ErridnLoyalty.Mst_CardType_Create_InvalidCardType (:6011-6021)
    ///   4. CheckDB trùng CardType -> ErridnLoyalty.Mst_CardType_CheckDB_CardTypeExist (:6023-6029)
    ///   5. Value <= 0 -> ErridnLoyalty.Mst_CardType_Create_InvalidValue (:6031-6041)
    ///   6. Trùng Value trong DB -> ErridnLoyalty.Mst_CardType_Create_InvalidValue (:6043-6066)
    ///   7. Giả lập NOT NULL của DDL: NetworkID rỗng / CardTypeName rỗng -> ErridnLoyalty.Mst_CardType_Create (:6086-6090)
    ///   8. Lưu 7 cột Mst_CardType, commit transaction (:6076-6090).
    /// Trả null nếu thành công; trả IActionResult (PortResult.Fail) nếu thất bại.
    /// </summary>
    private async Task<IActionResult?> CreateXAsync(string? cardType, string? networkId, string? name, object? valueRaw)
    {
        // 2. Mở 1 transaction cho toàn bộ nghiệp vụ (Master.Loyalty.cs:5861 _cf.db.BeginTransaction()).
        await using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            // 3. Chuẩn hoá input (Master.Loyalty.cs:5999-6002).
            //    StdParam = Trim().ToUpper(), rỗng -> null (05.Refs.Biz\Biz\Loyalty.TVO\Loyalty.TVO.Utils\Utils.cs:29).
            //    StdStr   = Trim().Normalize(), rỗng -> null (cùng tệp Utils.cs:29).
            string? strCardType = StdParam(cardType);
            string? strCardTypeName = StdStr(name);
            string? strNetworkID = StdParam(networkId);

            // Value = Convert.ToDouble(objValue) (:6002): null -> 0; chuỗi không phải số -> lỗi -> mã mặc định.
            double dblValue;
            try
            {
                dblValue = ConvertToDouble(valueRaw);
            }
            catch (Exception)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_CardType_Create", "Check.objValue", valueRaw?.ToString());
            }

            // 4.1. CardType null/rỗng -> Mst_CardType_Create_InvalidCardType (:6011-6021).
            if (string.IsNullOrEmpty(strCardType))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_CardType_Create_InvalidCardType", "Check.strCardType", strCardType);
            }

            // 4.2. CheckDB(CardType, FlagExist "0") (:6023-6029 -> Mst_CardType_CheckDB :5264-5325):
            //      FlagExist "0" + có dòng -> Mst_CardType_CheckDB_CardTypeExist.
            var exists = await db.Set<Mst_CardType>().AnyAsync(x =>
                EF.Functions.Like((x.CardType ?? "").ToLower(), PortQuery.Equal(strCardType), PortQuery.Esc));
            if (exists)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_CardType_CheckDB_CardTypeExist", "Check.CardType", strCardType);
            }

            // 4.3. Value <= 0 -> Mst_CardType_Create_InvalidValue (:6031-6041).
            if (dblValue <= 0)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_CardType_Create_InvalidValue", "Check.Value", dblValue);
            }

            // 4.4. Đã có dòng khác CÙNG Value -> Mst_CardType_Create_InvalidValue (:6043-6066).
            //      Nguồn so `t.Value = @objValue` (so SỐ, không phải chuỗi).
            var valueExists = await db.Set<Mst_CardType>().AnyAsync(x => x.Value == dblValue);
            if (valueExists)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_CardType_Create_InvalidValue", "Check.Value", dblValue);
            }

            // 5. Giả lập ràng buộc NOT NULL của DDL (CardType, NetworkID, CardTypeName).
            //    Nguồn thiếu NetworkID/CardTypeName -> SQL Server báo lỗi khi SaveData (:6086-6090),
            //    bắt lại thành mã mặc định Mst_CardType_Create (:5830 strErrorCodeDefault).
            if (string.IsNullOrEmpty(strNetworkID))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_CardType_Create", "Check.NotNull", "NetworkID");
            }
            if (string.IsNullOrEmpty(strCardTypeName))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_CardType_Create", "Check.NotNull", "CardTypeName");
            }

            // 6. Lưu dòng mới (Master.Loyalty.cs:6076-6082).
            var dtimeSys = DateTime.UtcNow;   // :5842 dtimeSys = DateTime.UtcNow
            var entity = new Mst_CardType
            {
                CardType = strCardType,           // :6076 CardType
                CardTypeName = strCardTypeName,   // :6077 CardTypeName
                NetworkID = strNetworkID,         // :6078 NetworkID
                Value = dblValue,                 // :6079 Value
                FlagActive = "1",                 // :6080 FlagActive = TConst.Flag.Active ("1")
                LogLUDTimeUTC = dtimeSys,         // :6081 LogLUDTimeUTC
                LogLUBy = "port"                  // :6082 LogLUBy (đích chưa có session user — loại Đ)
            };
            db.Set<Mst_CardType>().Add(entity);
            await db.SaveChangesAsync();

            // 7. Commit (Master.Loyalty.cs:5930 CommitSafety).
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
    // Update — nguồn Mst_CardTypeController.cs:418-485 (gate) -> MstCardTypeController.cs:93-163 (API)
    //   -> Master.Loyalty.cs:6100 (WAS_Mst_CardType_Update) -> :6185-6337 (Mst_CardType_Update)
    //   -> :6339-6427 (Mst_CardType_UpdateX) -> :5264-5325 (Mst_CardType_CheckDB).
    //
    // GIỮ ĐÚNG NGUỒN:
    //   * Gate dựng Ft_Cols_Upd NGUYÊN VĂN (:450-453):
    //       "Mst_CardType.CardType,Mst_CardType.CardTypeName,Mst_CardType.Value,Mst_CardType.FlagActive,"
    //     Biz chuẩn hoá bằng StdParam (Trim+ToUpper) rồi Contains("MST_CARDTYPE.<CỘT>") (:6377-6388).
    //   * Chuẩn hoá (:6380-6384): CardType = StdParam (Trim+ToUpper, rỗng->null);
    //     NetworkID = StdParam; CardTypeName = StdStr (Trim+Normalize, rỗng->null);
    //     Value = Convert.ToDouble(objValue) (thiếu -> 0; chuỗi không phải số -> lỗi);
    //     FlagActive = StdFlag ("1" -> "1", MỌI giá trị khác kể cả thiếu -> "0").
    //   * CheckDB(CardType, FlagExist "1") (:6395-6401): không có dòng -> Mst_CardType_CheckDB_CardTypeNotFound.
    //   * KHÔNG kiểm Value <= 0, KHÔNG kiểm trùng Value (khác Create #90) — nguồn :6377-6427 không có.
    //   * Lưu (:6413-6417): chỉ ghi cột có cờ bUpd_* + LogLUDTimeUTC + LogLUBy.
    //   * CardTypeName là NOT NULL trong DDL (:85) -> tên rỗng -> SQL lỗi -> mã mặc định Mst_CardType_Update (:6211).
    //
    // Bỏ qua có chủ đích (loại Đ):
    //   * CheckGatewayAuthentication/CheckAuthorize (gate :438-439), Sys_User_CheckAuthorize (:6250-6263):
    //     hệ thống đích chưa có hệ phân quyền.
    //   * Ghi RequestLog/ReturnLog (:6231-6238, :6327-6334).
    //   * LogLUBy gán cố định "port" do đích chưa có session người dùng (:6417).
    // ============================================================================================
    [HttpPost("update")]
    public async Task<IActionResult> Update([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] MstCardTypeUpdateReq? req)
    {
        // 1. Body rỗng/null: gate :428-435 ném Exception("strJson null") -> mã mặc định Mst_CardType_Update.
        if (req == null)
        {
            return PortResult.Fail("Mst_CardType_Update", "Check.strJson", null);
        }

        // 2. Mở 1 transaction cho toàn bộ nghiệp vụ (Master.Loyalty.cs:6228 _cf.db.BeginTransaction()).
        await using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            // 3. Ft_Cols_Upd: gate dựng nguyên văn (:450-453) rồi biz StdParam (ToUpper) + Contains (:6377-6388).
            //    Viết đúng chuỗi gate, KHÔNG rút gọn.
            var strFt_Cols_Upd = "Mst_CardType.CardType,Mst_CardType.CardTypeName,Mst_CardType.Value,Mst_CardType.FlagActive,";
            strFt_Cols_Upd = StdParam(strFt_Cols_Upd) ?? "";
            bool bUpd_CardTypeName = strFt_Cols_Upd.Contains("Mst_CardType.CardTypeName".ToUpperInvariant());
            bool bUpd_Value = strFt_Cols_Upd.Contains("Mst_CardType.Value".ToUpperInvariant());
            bool bUpd_FlagActive = strFt_Cols_Upd.Contains("Mst_CardType.FlagActive".ToUpperInvariant());

            // 4. Chuẩn hoá input (Master.Loyalty.cs:6380-6384).
            //    StdParam = Trim().ToUpper(), rỗng -> null (05.Refs.Biz\Biz\Loyalty.TVO\Loyalty.TVO.Utils\Utils.cs:29).
            //    StdStr   = Trim().Normalize(), rỗng -> null (cùng tệp Utils.cs:29).
            string? strCardType = StdParam(req.CardType);
            string? strCardTypeName = StdStr(req.CardTypeName);
            string strFlagActive = StdFlag(req.FlagActive);   // :6384 StdFlag

            // Value = Convert.ToDouble(objValue) (:6383): thiếu -> 0; chuỗi không phải số -> lỗi -> mã mặc định.
            double dblValue;
            try
            {
                dblValue = ConvertToDouble(req.Value);
            }
            catch (Exception)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_CardType_Update", "Check.objValue", req.Value?.ToString());
            }

            // 5. CheckDB(CardType, FlagExist "1") (:6395-6401 -> Mst_CardType_CheckDB :5264-5325):
            //    FlagExist "1" + KHÔNG có dòng -> Mst_CardType_CheckDB_CardTypeNotFound.
            var entity = await db.Set<Mst_CardType>().FirstOrDefaultAsync(x =>
                EF.Functions.Like((x.CardType ?? "").ToLower(), PortQuery.Equal(strCardType ?? ""), PortQuery.Esc));
            if (entity == null)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_CardType_CheckDB_CardTypeNotFound", "Check.CardType", strCardType);
            }

            // 6. CÓ Ý KHÔNG kiểm Value <= 0 và KHÔNG kiểm trùng Value — nguồn :6377-6427 không có (khác Create #90).

            // 7. Ghi cột có cờ bUpd_* + LogLUDTimeUTC + LogLUBy (:6413-6417).
            //    CardTypeName NOT NULL trong DDL (:85): tên rỗng (sau StdStr) mà bUpd_CardTypeName
            //    -> SQL Server báo lỗi khi SaveData -> mã mặc định Mst_CardType_Update (:6211).
            if (bUpd_CardTypeName)
            {
                if (string.IsNullOrEmpty(strCardTypeName))
                {
                    await tx.RollbackAsync();
                    return PortResult.Fail("Mst_CardType_Update", "Check.NotNull", "CardTypeName");
                }
                entity.CardTypeName = strCardTypeName;   // :6413 CardTypeName
            }
            if (bUpd_Value)
            {
                entity.Value = dblValue;                 // :6414 Value
            }
            if (bUpd_FlagActive)
            {
                entity.FlagActive = strFlagActive;       // :6415 FlagActive
            }
            entity.LogLUDTimeUTC = DateTime.UtcNow;      // :6416 LogLUDTimeUTC = dtimeSys (UtcNow)
            entity.LogLUBy = "port";                     // :6417 LogLUBy (đích chưa có session user — loại Đ)
            await db.SaveChangesAsync();

            // 8. Commit (Master.Loyalty.cs:6300 CommitSafety).
            await tx.CommitAsync();

            // 9. Thành công: gate :471-472 SetSuccess + Data = null.
            return Ok(new { Success = true, Data = (object?)null });
        }
        catch (Exception)
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    // ============================================================================================
    // Delete — nguồn Mst_CardTypeController.cs:489-552 (gate) -> MstCardTypeController.cs:166-236 (API)
    //   -> Master.Loyalty.cs:6428 (WAS_Mst_CardType_Delete) -> :6507-6643 (Mst_CardType_Delete)
    //   -> :6645-6701 (Mst_CardType_DeleteX) -> :5264-5325 (Mst_CardType_CheckDB).
    //
    // GIỮ ĐÚNG NGUỒN:
    //   * Kiểm tra mã rỗng tại gate (:508, :538): CardType rỗng -> ném ServiceException("invalid_CartType", "invalid_CartType")
    //     (GIỮ NGUYÊN lỗi chính tả chữ "CartType" từ nguồn).
    //   * Mở 1 transaction cho toàn bộ nghiệp vụ (Master.Loyalty.cs:6540 _cf.db.BeginTransaction()).
    //   * Chuẩn hoá mã qua StdParam (:6671): Trim + ToUpper, rỗng -> null.
    //   * CheckDB(CardType, FlagExist "1") (:6677-6682 -> Mst_CardType_CheckDB :5264-5325):
    //     FlagExist "1" + KHÔNG có dòng -> Mst_CardType_CheckDB_CardTypeNotFound.
    //   * Giả lập 4 ràng buộc khoá ngoại (FK) đã có ở đích (SQL Server chặn xoá nếu còn bản ghi con):
    //       1. Crd_Card.CardTypeInit (20221108.z11.Crd_Member.Crd_Card.sql:437)
    //       2. Crd_Card.CardTypeUse (20221108.z11.Crd_Member.Crd_Card.sql:446)
    //       3. Crd_Member.CardTypeInit (20221108.z11.Crd_Member.Crd_Card.sql:463)
    //       4. Crd_Member.CardTypeUse (20221108.z11.Crd_Member.Crd_Card.sql:472)
    //       5. Mst_RankPolicy.CardType (20221026.CreateDatabase.sql:273 — bổ sung sau khi port bảng ở #227)
    //     Nếu có bản ghi con -> rollback + PortResult.Fail("Mst_CardType_Delete", "Check.CardType", strCardType, "FK", "<Bang.Cot>").
    //   * Xoá dòng Mst_CardType (:6692-6695) và CommitSafety (:6606).
    //   * Thành công: gate :532-533 SetSuccess + Data = null.
    //
    // GAP:
    //   * Các bảng FK còn lại trong DDL nguồn chưa có ở đích -> thêm kiểm tra khi port các bảng đó:
    //     - Mst_BirthPolicyDtl.CardType (chưa có ở đích -> GAP: thêm kiểm tra khi port bảng đó)
    //     - Mst_PromotionProgramSpec.CardType (chưa có ở đích -> GAP: thêm kiểm tra khi port bảng đó)
    //     - Mst_PolicyMoneyToPointServiceDtl.CardType (chưa có ở đích -> GAP: thêm kiểm tra khi port bảng đó)
    //
    // Bỏ qua có chủ đích (loại Đ):
    //   * CheckGatewayAuthentication/CheckAuthorize (gate :498-499), Sys_User_CheckAuthorize (:6562-6576):
    //     hệ thống đích chưa có hệ phân quyền.
    //   * Ghi RequestLog/ReturnLog (:6543-6550, :6633-6640).
    // ============================================================================================
    [HttpPost("delete")]
    public async Task<IActionResult> Delete([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] MstCardTypeDeleteReq? req)
    {
        // 1. Kiểm tra mã rỗng tại gate (Mst_CardTypeController.cs:508, 538):
        //    Giữ nguyên lỗi chính tả nguồn: ServiceException("invalid_CartType", "invalid_CartType")
        if (string.IsNullOrWhiteSpace(req?.CardType))
        {
            return PortResult.Fail("invalid_CartType", "Check.CardType", req?.CardType);
        }

        // 2. Thực hiện nghiệp vụ xóa qua DeleteXAsync (Master.Loyalty.cs:6507-6643)
        var err = await DeleteXAsync(req?.CardType);
        if (err != null)
        {
            return err;
        }

        // 3. Thành công: gate Mst_CardTypeController.cs:532-533 SetSuccess + Data = null
        return Ok(new { Success = true, Data = (object?)null });
    }

    /// <summary>
    /// Port 1:1 DeleteMultiple action từ gate Mst_CardTypeController.cs:554-618.
    /// Chuỗi gọi: gate :554-618 -> lặp qua danh sách Lst_Mst_CardType, mỗi phần tử gọi
    /// WA_Mst_CardType_Delete (:585-602) -> biz WAS_Mst_CardType_Delete -> Mst_CardType_Delete -> Mst_CardType_DeleteX.
    ///
    /// Request JSON: mảng [{ CardType }].
    /// 1. Kiểm tra body rỗng / null: Gate Mst_CardTypeController.cs:564-571
    ///    if (!string.IsNullOrEmpty(strJson)) { ... } else { throw new ServiceException("strJson null"); }
    ///    Khi body null hoặc rỗng -> trả về 400 ErridnLoyalty.Mst_CardType_Delete với Check.strJson = null.
    /// 2. Danh sách rỗng `[]`: vòng lặp foreach không chạy, trả về Ok(Success = true, Data = null).
    /// 3. Lặp tuần tự qua từng phần tử trong danh sách:
    ///    Mỗi lần lặp là một lời gọi độc lập tới chuỗi nghiệp vụ Delete, sử dụng TRANSACTION RIÊNG
    ///    (Master.Loyalty.cs:6540 _cf.db.BeginTransaction() và :6606 CommitSafety).
    ///    - KHÔNG có bước kiểm tra CardType rỗng ở tầng gate như Delete đơn (:508, 538);
    ///      khi CardType rỗng, lỗi phát sinh ở tầng nghiệp vụ Mst_CardType_CheckDB
    ///      -> trả về 400 ErridnLoyalty.Mst_CardType_CheckDB_CardTypeNotFound.
    ///    - Hành vi "xóa một phần" (nguồn Mst_CardTypeController.cs:585-611):
    ///      Nếu phần tử thứ k gặp lỗi, exception được ném ra và thoát khỏi vòng lặp ngay lập tức
    ///      nhảy vào khối catch (ex) (:607-611).
    ///      Do mỗi phần tử chạy trên một transaction riêng đã được commit, các phần tử TRƯỚC k
    ///      ĐÃ BỊ XÓA VÀ KHÔNG BỊ ROLLBACK; các phần tử SAU k hoàn toàn không được xử lý.
    ///      "xóa một phần" là hành vi nguồn.
    /// 4. Thành công: gate :604-605 trả objJsonResultEntry.Data = null -> Ok(new { Success = true, Data = (object?)null }).
    ///
    /// Bỏ qua có chủ đích (loại Đ):
    /// - CheckGatewayAuthentication: gate Mst_CardTypeController.cs:574
    /// - CheckAuthorize: gate Mst_CardTypeController.cs:575 (hệ thống đích chưa có hệ phân quyền)
    /// - Sys_User_CheckAuthorize: Master.Loyalty.cs:6562-6576
    /// - Ghi RequestLog/ReturnLog: Master.Loyalty.cs:6543-6550, 6633-6640
    /// </summary>
    [HttpPost("deletemultiple")]
    public async Task<IActionResult> DeleteMultiple([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] List<MstCardTypeDeleteReq>? req)
    {
        // 1. Kiểm tra body rỗng / null: Gate Mst_CardTypeController.cs:564-571
        // if (!string.IsNullOrEmpty(strJson)) { ... } else { throw new ServiceException("strJson null"); }
        if (req == null)
        {
            return PortResult.Fail("Mst_CardType_Delete", "Check.strJson", null);
        }

        // 2. Lặp tuần tự qua từng phần tử (gate Mst_CardTypeController.cs:585-602).
        // Mỗi phần tử chạy trong 1 transaction riêng biệt (hàm DeleteXAsync).
        // Nếu phần tử thứ k gặp lỗi -> return ngay lỗi đó (hành vi nguồn :585-611:
        // "xóa một phần" là hành vi nguồn: các phần tử trước k đã commit thành công
        // vào DB và KHÔNG bị rollback; các phần tử sau k không được xử lý).
        foreach (var item in req)
        {
            var err = await DeleteXAsync(item?.CardType);
            if (err != null)
            {
                return err;
            }
        }

        // 3. Thành công: gate Mst_CardTypeController.cs:604-605
        return Ok(new
        {
            Success = true,
            Data = (object?)null
        });
    }

    /// <summary>
    /// Nghiệp vụ xóa 1 loại thẻ, tương ứng chuỗi gọi:
    /// API WA_Mst_CardType_Delete (MstCardTypeController.cs:166-236) -> biz WAS_Mst_CardType_Delete (Master.Loyalty.cs:6428-6506)
    /// -> Mst_CardType_Delete (:6507-6643) -> Mst_CardType_DeleteX (:6645-6701) -> Mst_CardType_CheckDB (:5264-5325).
    ///
    /// Mỗi lần gọi mở 1 Transaction riêng (Master.Loyalty.cs:6540 _cf.db.BeginTransaction() và :6606 CommitSafety).
    /// Chuẩn hoá mã qua StdParam: Trim, ToUpper, rỗng -> null (Master.Loyalty.cs:6671).
    /// - Khi mã rỗng hoặc không tìm thấy loại thẻ trong DB: trả về 400 ErridnLoyalty.Mst_CardType_CheckDB_CardTypeNotFound (Mst_CardType_CheckDB :5264-5325).
    /// - Khi vi phạm khoá ngoại Crd_Card/Crd_Member: trả về 400 ErridnLoyalty.Mst_CardType_Delete (Master.Loyalty.cs:6527).
    /// - Xóa dòng loại thẻ và Commit.
    /// Trả về null nếu thành công; trả về IActionResult (PortResult.Fail) nếu có lỗi.
    /// </summary>
    private async Task<IActionResult?> DeleteXAsync(string? code)
    {
        // 1. Mở 1 transaction cho mỗi phần tử (Master.Loyalty.cs:6540 _cf.db.BeginTransaction()).
        await using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            // 2. Chuẩn hoá mã qua StdParam: Trim, ToUpper, rỗng -> null (Master.Loyalty.cs:6671)
            string? strCardType = StdParam(code);

            // 3. CheckDB(CardType, FlagExist "1") (Master.Loyalty.cs:6677-6682 -> Mst_CardType_CheckDB :5264-5325):
            //    FlagExist "1" + KHÔNG có dòng (hoặc strCardType rỗng) -> Mst_CardType_CheckDB_CardTypeNotFound.
            if (string.IsNullOrEmpty(strCardType))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_CardType_CheckDB_CardTypeNotFound", "Check.CardType", strCardType);
            }

            var entity = await db.Set<Mst_CardType>().FirstOrDefaultAsync(x =>
                EF.Functions.Like((x.CardType ?? "").ToLower(), PortQuery.Equal(strCardType), PortQuery.Esc));
            if (entity == null)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_CardType_CheckDB_CardTypeNotFound", "Check.CardType", strCardType);
            }

            // 4. Giả lập 4 ràng buộc khoá ngoại (FK) đã có ở đích (SQL Server chặn xoá nếu còn bản ghi con):
            //    Dùng Like + PortQuery.Equal trên CardTypeInit/CardTypeUse của CrdCards và CrdMembers.
            // 4.1. Crd_Card.CardTypeInit (20221108.z11.Crd_Member.Crd_Card.sql:437)
            var hasCardInit = await db.CrdCards.AnyAsync(x =>
                EF.Functions.Like((x.CardTypeInit ?? "").ToLower(), PortQuery.Equal(strCardType), PortQuery.Esc));
            if (hasCardInit)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_CardType_Delete", "Check.CardType", strCardType, "FK", "Crd_Card.CardTypeInit");
            }

            // 4.2. Crd_Card.CardTypeUse (20221108.z11.Crd_Member.Crd_Card.sql:446)
            var hasCardUse = await db.CrdCards.AnyAsync(x =>
                EF.Functions.Like((x.CardTypeUse ?? "").ToLower(), PortQuery.Equal(strCardType), PortQuery.Esc));
            if (hasCardUse)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_CardType_Delete", "Check.CardType", strCardType, "FK", "Crd_Card.CardTypeUse");
            }

            // 4.3. Crd_Member.CardTypeInit (20221108.z11.Crd_Member.Crd_Card.sql:463)
            var hasMemberInit = await db.CrdMembers.AnyAsync(x =>
                EF.Functions.Like((x.CardTypeInit ?? "").ToLower(), PortQuery.Equal(strCardType), PortQuery.Esc));
            if (hasMemberInit)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_CardType_Delete", "Check.CardType", strCardType, "FK", "Crd_Member.CardTypeInit");
            }

            // 4.4. Crd_Member.CardTypeUse (20221108.z11.Crd_Member.Crd_Card.sql:472)
            var hasMemberUse = await db.CrdMembers.AnyAsync(x =>
                EF.Functions.Like((x.CardTypeUse ?? "").ToLower(), PortQuery.Equal(strCardType), PortQuery.Esc));
            if (hasMemberUse)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_CardType_Delete", "Check.CardType", strCardType, "FK", "Crd_Member.CardTypeUse");
            }

            // 4.5. Mst_RankPolicy.CardType (FK_MstCardTp_MstRankPolicy — 20221026.CreateDatabase.sql:273; bảng port ở #227)
            var hasRankPolicy = await db.Set<Mst_RankPolicy>().AnyAsync(x =>
                EF.Functions.Like((x.CardType ?? "").ToLower(), PortQuery.Equal(strCardType), PortQuery.Esc));
            if (hasRankPolicy)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_CardType_Delete", "Check.CardType", strCardType, "FK", "Mst_RankPolicy.CardType");
            }

            // GAP: Các bảng FK còn lại trong DDL nguồn chưa có ở đích -> thêm kiểm tra khi port các bảng đó:
            // - Mst_BirthPolicyDtl.CardType (chưa có ở đích -> GAP: thêm kiểm tra khi port bảng đó)
            // - Mst_PromotionProgramSpec.CardType (chưa có ở đích -> GAP: thêm kiểm tra khi port bảng đó)
            // - Mst_PolicyMoneyToPointServiceDtl.CardType (chưa có ở đích -> GAP: thêm kiểm tra khi port bảng đó)

            // 5. Xoá dòng Mst_CardType (Master.Loyalty.cs:6692-6695 DeleteX)
            db.Set<Mst_CardType>().Remove(entity);
            await db.SaveChangesAsync();

            // 6. Commit (Master.Loyalty.cs:6606 TDALUtils.DBUtils.CommitSafety)
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
    // Import — nguồn gate Mst_CardTypeController.cs:622-768 -> API WA_Mst_CardType_Create
    // (MstCardTypeController.cs:21-91) -> biz WAS_Mst_CardType_Create (Master.Loyalty.cs:5738-5820)
    // -> Mst_CardType_Create (Master.Loyalty.cs:5821-5967) -> Mst_CardType_CreateX (Master.Loyalty.cs:5968-6099).
    //
    // GIẢ ĐỊNH (kế thừa từ Data/PortExcel.cs):
    //   - Định dạng file Excel: dòng 1 = tiêu đề (đúng file ExportTemplate sinh ra), dữ liệu bắt đầu từ ô A2 (firstDataRow = 2).
    //   - Cột ánh xạ theo vị trí: cột 1 = CardType, cột 2 = CardTypeName, cột 3 = Value (GetImportDicColumsTemplate có 3 cột:
    //     CardType -> "Mã hạng thẻ (*)", CardTypeName -> "Tên hạng thẻ", Value -> "Thứ tự").
    //
    // Quy trình và thứ tự kiểm tra:
    //   1. Kiểm tra file upload: gate Mst_CardTypeController.cs:646-651
    //      - Thiếu file hoặc file rỗng hoặc đuôi file != ".xlsx" -> Fail("Mst_CardType_Import", "Message", "File excel import không hợp lệ")
    //   2. Đọc file bằng PortExcel.ReadSheet(stream, new[] { "CardType", "CardTypeName", "Value" }):
    //      - Bảng không có dòng dữ liệu (table == null || table.Rows.Count == 0) -> Fail("Mst_CardType_Import", "Message", "File excel import không có dữ liệu") (:663, :752-756)
    //      - Số cột tiêu đề != GetImportDicColumsTemplate().Count (3 cột) -> Fail("Mst_CardType_Import", "Message", "File excel import không hợp lệ") (:665-669)
    //   3. Kiểm tra TẤT CẢ các dòng TRƯỚC khi tạo (#region["Check null"] :672-699):
    //      - CardType rỗng / khoảng trắng -> Fail("Mst_CardType_Import", "Message", "Mã hạng thẻ không được trống") (:675-679)
    //      - CardTypeName rỗng / khoảng trắng -> Fail("Mst_CardType_Import", "Message", "Tên hạng thẻ không được trống") (:680-684)
    //      - Value rỗng / khoảng trắng -> Fail("Mst_CardType_Import", "Message", "Giá trị không được trống") (:685-689)
    //      Nếu có bất kỳ dòng nào vi phạm, dừng ngay và KHÔNG có dòng nào được tạo vào CSDL.
    //   4. Lưu từng dòng (hành vi "import một phần"):
    //      - NetworkID lấy từ cấu hình: IConfiguration["Biz_NetworkID"] ?? "1001" (GAP: nguồn lấy NetworkID = orgUserUI.NetworkId từ user đăng nhập :728).
    //      - Duyệt từng dòng theo thứ tự, gọi CreateXAsync(cardType, networkId, name, value) riêng cho mỗi dòng (mỗi dòng 1 transaction biz riêng biệt: gate :725-746).
    //      - Nếu dòng thứ k gặp lỗi (vd trùng mã Mst_CardType_CheckDB_CardTypeExist, sai Value Mst_CardType_Create_InvalidValue),
    //        vòng lặp dừng ngay lập tức, trả về đúng lỗi của CreateXAsync cho dòng k.
    //      - CÁC DÒNG TRƯỚC k ĐÃ ĐƯỢC COMMIT VÀO DB (không rollback các dòng đã tạo trước đó).
    //      - Các dòng sau k không được duyệt.
    //   5. Thành công: trả Ok({ Success = true, Data = null }) (:748-750).
    //
    // Bỏ qua có chủ đích (loại Đ):
    //   - CheckGatewayAuthentication, CheckAuthorize (:633-634)
    //   - Nguồn lưu file tạm xuống đĩa (FileUtils.SaveTempFile :654) -> ở đây đọc trực tiếp từ stream qua ClosedXML
    //   - Session người dùng orgUserUI (:632, :728, :736-740).
    // ============================================================================================
    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Import(IFormFile? file)
    {
        // 1. Kiểm tra file upload: gate Mst_CardTypeController.cs:646-651
        if (file == null || file.Length == 0)
        {
            return PortResult.Fail("Mst_CardType_Import", "Message", "File excel import không hợp lệ");
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".xlsx")
        {
            return PortResult.Fail("Mst_CardType_Import", "Message", "File excel import không hợp lệ");
        }

        // 2. Đọc bảng tính bằng PortExcel.ReadSheet (giả định định dạng file Excel từ PortExcel)
        int columnCount;
        List<Dictionary<string, string?>> rows;
        try
        {
            using var stream = file.OpenReadStream();
            (columnCount, rows) = PortExcel.ReadSheet(stream, GetImportDicColumsTemplate().Keys.ToArray());
        }
        catch
        {
            return PortResult.Fail("Mst_CardType_Import", "Message", "File excel import không hợp lệ");
        }

        // Kiểm tra có dữ liệu hay không: gate :663, :752-756 (table == null || table.Rows.Count == 0)
        if (rows.Count == 0)
        {
            return PortResult.Fail("Mst_CardType_Import", "Message", "File excel import không có dữ liệu");
        }

        // Kiểm tra số cột dòng tiêu đề so với GetImportDicColumsTemplate().Count (3 cột): gate :665-669
        if (columnCount != GetImportDicColumsTemplate().Count)
        {
            return PortResult.Fail("Mst_CardType_Import", "Message", "File excel import không hợp lệ");
        }

        // 3. Kiểm tra TẤT CẢ các dòng TRƯỚC khi tạo (Check null: gate Mst_CardTypeController.cs:672-699)
        foreach (var r in rows)
        {
            var cardType = r.GetValueOrDefault("CardType");
            if (string.IsNullOrWhiteSpace(cardType))
            {
                return PortResult.Fail("Mst_CardType_Import", "Message", "Mã hạng thẻ không được trống");
            }

            var cardTypeName = r.GetValueOrDefault("CardTypeName");
            if (string.IsNullOrWhiteSpace(cardTypeName))
            {
                return PortResult.Fail("Mst_CardType_Import", "Message", "Tên hạng thẻ không được trống");
            }

            var value = r.GetValueOrDefault("Value");
            if (string.IsNullOrWhiteSpace(value))
            {
                return PortResult.Fail("Mst_CardType_Import", "Message", "Giá trị không được trống");
            }
        }

        // 4. Lưu từng dòng tuần tự: mỗi dòng 1 transaction riêng (hành vi "import một phần": gate :725-746)
        // GAP: nguồn lấy NetworkID = orgUserUI.NetworkId (:728) từ user đăng nhập; đích chưa có user -> dùng IConfiguration["Biz_NetworkID"] ?? "1001"
        var networkId = config["Biz_NetworkID"] ?? "1001";
        foreach (var r in rows)
        {
            var cardType = r.GetValueOrDefault("CardType");
            var cardTypeName = r.GetValueOrDefault("CardTypeName");
            var value = r.GetValueOrDefault("Value");

            var err = await CreateXAsync(cardType, networkId, cardTypeName, value);
            if (err != null)
            {
                // Lỗi ở dòng k -> thoát ngay, trả đúng lỗi của Create, các dòng trước k đã tạo thành công
                return err;
            }
        }

        // Thành công: gate :748-750
        return Ok(new
        {
            Success = true,
            Data = (object?)null
        });
    }

    // ============================================================================================
    // Export — nguồn gate Mst_CardTypeController.cs:773-851 -> API WA_Mst_CardType_Get
    // (MstCardTypeController.cs:239-308) -> biz WAS_Mst_CardType_Get (Master.Loyalty.cs:5329-5429)
    // -> Mst_CardType_Get (Master.Loyalty.cs:5430-5582) -> Mst_CardType_GetX (Master.Loyalty.cs:5583-5737).
    //
    // GIỮ ĐÚNG NGUỒN:
    //   * Tham số nhận qua body: OrgID, KeyWord, FlagActive (:773, :794-799).
    //   * strWhereClause_Mst_CardType (:283-308):
    //       - KeyWord: like '%x%' trên CardTypeName (:290).
    //       - FlagActive: danh sách IN (:298-305).
    //       - OrgID: không lọc (:283-308 không có nhánh lọc OrgID).
    //   * Ft_RecordStart = "0", Ft_RecordCount = "123456000" (:812-813, BaseController.cs:30-31),
    //     Rt_Cols_Mst_CardType = "*" (:816).
    //   * Thứ tự: Mst_CardType_GetX chia trang theo CardType asc (:5643) rồi sắp theo Value asc (:5683).
    //   * Nếu danh sách rỗng (rows == null || rows.Count == 0):
    //     ném ServiceException("Không có dữ liệu") (:820, :836-838) -> trả 400 ErridnLoyalty.Mst_CardType_Export.
    //   * Từ điển cột: GetImportDicColums() (:824, :890-901) gồm đúng 4 cột:
    //       1. CardType -> "Mã hạng thẻ (*)"
    //       2. CardTypeName -> "Tên hạng thẻ (*)"
    //       3. Value -> "Thứ tự (*)"
    //       4. FlagActive -> "Trạng thái"
    //   * Sinh đường dẫn file xuất: BaseController.GenExcelExportFilePath (:826)
    //     -> PortExcel.GenExcelExportFilePath(webRoot, "Mst_CardType").
    //   * Tên sheet: "Mst_CardType" (:827).
    //   * Ghi file qua PortExcel.ExportToExcelFromList(rows, dicColNames, physicalPath, "Mst_CardType") (:827).
    //   * Trả URL tải file với Success = true, Data = { Url = PortExcel.GetRequestBaseAddress(Request) + virtualPath } (:828-832).
    //
    // Bỏ qua có chủ đích (loại Đ):
    //   * CheckGatewayAuthentication: gate Mst_CardTypeController.cs:783; API MstCardTypeController.cs:259-266
    //   * CheckAuthorize: gate Mst_CardTypeController.cs:784 (hệ thống đích chưa có hệ phân quyền)
    //   * Sys_User_CheckAuthorize: Master.Loyalty.cs:5487-5501
    //   * Ghi RequestLog/ReturnLog: Master.Loyalty.cs:5474-5481, 5572-5579
    // ============================================================================================
    [HttpPost("export")]
    public async Task<IActionResult> Export([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] MstCardTypeExportReq? req)
    {
        req ??= new MstCardTypeExportReq();

        // Nguồn: strWhereClause_Mst_CardType (:283-308) chỉ lọc theo KeyWord (like trên CardTypeName) và FlagActive (in),
        // OrgID truyền vào (:797) nhưng KHÔNG được sử dụng trong hàm tạo WHERE clause -> không lọc OrgID.
        // Ft_RecordStart = "0", Ft_RecordCount = "123456000", Rt_Cols_Mst_CardType = "*" (:812-816).
        var (_, rows) = await GetXAsync(req.KeyWord, req.FlagActive, 0, 123456000, null);

        // Nguồn :820, :836-838: nếu danh sách rỗng -> throw ServiceException("Không có dữ liệu")
        if (rows == null || rows.Count == 0)
        {
            return PortResult.Fail("Mst_CardType_Export", "Message", "Không có dữ liệu");
        }

        // Tạo thư mục và đường dẫn file Excel xuất: BaseController.GenExcelExportFilePath (:826)
        var webRoot = env.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
        {
            webRoot = Path.Combine(env.ContentRootPath, "wwwroot");
        }

        // Lấy từ điển cột: Mst_CardTypeController.cs:824 gọi GetImportDicColums() (4 cột)
        var dicColNames = GetImportDicColums();
        var (physicalPath, virtualPath) = PortExcel.GenExcelExportFilePath(webRoot, "Mst_CardType");

        // Ghi danh sách ra file Excel: ExcelExport.ExportToExcelFromList (:827) với sheet "Mst_CardType"
        PortExcel.ExportToExcelFromList(rows, dicColNames, physicalPath, "Mst_CardType");

        // Trả URL tải file: Data = { Url = GetRequestBaseAddress() + url } (:828-832)
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
    // ExportTemplate — nguồn gate Mst_CardTypeController.cs:854-886.
    // Action này KHÔNG kiểm quyền ở nguồn (không có CheckGatewayAuthentication và không có CheckAuthorize).
    // Không gọi tầng service, không truy vấn cơ sở dữ liệu.
    // Khởi tạo danh sách rỗng kiểu Mst_CardType (:862).
    // Lấy từ điển cột mẫu từ GetImportDicColumsTemplate (:864, :1003-1011) gồm 3 cột:
    //   1. CardType -> "Mã hạng thẻ (*)"
    //   2. CardTypeName -> "Tên hạng thẻ"
    //   3. Value -> "Thứ tự"
    // Sinh đường dẫn file mẫu qua PortExcel.GenExcelExportFilePath(webRoot, "Mst_CardType") (:866).
    // Xuất file Excel qua PortExcel.ExportToExcelFromList(list, dicColNames, physicalPath, "Mst_CardType") (:867).
    // Trả về Success = true, Data = { Url = PortExcel.GetRequestBaseAddress(Request) + virtualPath } (:868-872).
    // Không có nhánh báo lỗi khi danh sách rỗng (khác với Export).
    // ============================================================================================
    [HttpPost("exporttemplate")]
    public IActionResult ExportTemplate()
    {
        // Khởi tạo danh sách rỗng kiểu Mst_CardType (gate Mst_CardTypeController.cs:862)
        var list = new List<Mst_CardType>();

        // Lấy từ điển 3 cột mẫu import (gate Mst_CardTypeController.cs:864)
        var dicColNames = GetImportDicColumsTemplate();

        // Xác định thư mục webRoot và sinh đường dẫn file (gate Mst_CardTypeController.cs:866)
        var webRoot = env.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
        {
            webRoot = Path.Combine(env.ContentRootPath, "wwwroot");
        }

        var (physicalPath, virtualPath) = PortExcel.GenExcelExportFilePath(webRoot, "Mst_CardType");

        // Ghi danh sách rỗng ra file Excel với tên sheet Mst_CardType (gate Mst_CardTypeController.cs:867)
        PortExcel.ExportToExcelFromList(list, dicColNames, physicalPath, "Mst_CardType");

        // Trả URL tải file với Success = true (gate Mst_CardTypeController.cs:868-872)
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
    /// Chuyển Value như <c>Convert.ToDouble(objValue)</c> của nguồn (Master.Loyalty.cs:6002):
    /// null/thiếu -> 0; số -> giá trị; chuỗi số -> parse; chuỗi không phải số -> ném lỗi.
    /// </summary>
    private static double ConvertToDouble(object? value)
    {
        if (value == null) return 0d;
        if (value is System.Text.Json.JsonElement el)
        {
            switch (el.ValueKind)
            {
                case System.Text.Json.JsonValueKind.Number:
                    return el.GetDouble();
                case System.Text.Json.JsonValueKind.String:
                    var str = el.GetString();
                    if (string.IsNullOrWhiteSpace(str)) return 0d;
                    if (double.TryParse(str, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var d1)) return d1;
                    return Convert.ToDouble(str);
                case System.Text.Json.JsonValueKind.Null:
                case System.Text.Json.JsonValueKind.Undefined:
                    return 0d;
                case System.Text.Json.JsonValueKind.True:
                    return 1d;
                case System.Text.Json.JsonValueKind.False:
                    return 0d;
                default:
                    var sEl = el.ToString();
                    if (double.TryParse(sEl, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var d2)) return d2;
                    return Convert.ToDouble(sEl);
            }
        }
        if (value is double d) return d;
        if (value is int i) return i;
        if (value is long l) return l;
        if (value is decimal m) return (double)m;
        if (value is float f) return f;
        if (value is string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0d;
            if (double.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var dVal))
                return dVal;
            return Convert.ToDouble(s);
        }
        return Convert.ToDouble(value);
    }

    #region DicColums
    /// <summary>
    /// Từ điển cột xuất Excel: gate Mst_CardTypeController.cs:889-901 (GetImportDicColums).
    /// ĐẶC BIỆT LƯU Ý THEO NGUỒN: Nguồn tại Mst_CardTypeController.cs:824 sử dụng GetImportDicColums()
    /// (dùng từ điển Import cho Export là thiết kế của nguồn).
    /// Giữ đúng thứ tự 4 cột từ nguồn:
    /// 1. CardType -> "Mã hạng thẻ (*)"
    /// 2. CardTypeName -> "Tên hạng thẻ (*)"
    /// 3. Value -> "Thứ tự (*)"
    /// 4. FlagActive -> "Trạng thái"
    /// </summary>
    private static Dictionary<string, string> GetImportDicColums()
    {
        return new Dictionary<string, string>
        {
            { "CardType", "Mã hạng thẻ (*)" },
            { "CardTypeName", "Tên hạng thẻ (*)" },
            { "Value", "Thứ tự (*)" },
            { "FlagActive", "Trạng thái" }
        };
    }

    private static Dictionary<string, string> GetImportDicColumsTemplate()
    {
        return new Dictionary<string, string>
        {
            { "CardType", "Mã hạng thẻ (*)" },
            { "CardTypeName", "Tên hạng thẻ" },
            { "Value", "Thứ tự" }
        };
    }
    #endregion

    #region Helper chuẩn hoá input (05.Refs.Biz\Biz\Loyalty.TVO\Loyalty.TVO.Utils\Utils.cs)
    /// <summary>
    /// Chuẩn hoá tham số mã: Trim, ToUpper, rỗng -> null.
    /// Dùng cho mã (CardType, NetworkID...).
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
    /// Dùng cho tên (CardTypeName...).
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
