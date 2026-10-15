using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniLoyalty.Data;
using MiniLoyalty.Models.Port;

namespace MiniLoyalty.Controllers.Port;

// ============================================================================================
// PORT 1:1 — màn danh mục loại thẻ của dự án nguồn 2022.7.Loyalty.
//
// Nguồn controller (tầng gate):
//   Mst_CardTypeController.cs:25-105   Search       ->  WA_Mst_CardType_Get
//   Mst_CardTypeController.cs:106-215  GetAllActive ->  WA_Mst_CardType_Get
//   Mst_CardTypeController.cs:283-308  strWhereClause_Mst_CardType (dựng mệnh đề lọc)
// Nguồn API:  MstCardTypeController.cs:239-308  WA_Mst_CardType_Get
// Nguồn biz:  Master.Loyalty.cs:5329 (WAS_Mst_CardType_Get) -> :5430 (Mst_CardType_Get)
//             -> :5583-5737 (Mst_CardType_GetX — SQL thật)
//
// GIỮ ĐÚNG NGUỒN:
//   * Endpoint là POST (nguồn [AcceptVerbs("POST")]).
//   * Lọc KeyWord: like '%x%' trên CardTypeName (:290).
//   * Lọc FlagActive: danh sách `in ('a','b')` (:299-304).
//   * Chia trang theo CardType asc (:5643) rồi sắp trang theo Value asc (:5683).
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

[ApiController]
[Route("api/mst_cardtype")]
[Produces("application/json")]
public class Mst_CardTypeControllerPort(AppDbContext db) : ControllerBase
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

        // 2. Mở 1 transaction cho toàn bộ nghiệp vụ (Master.Loyalty.cs:5861 _cf.db.BeginTransaction()).
        await using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            // 3. Chuẩn hoá input (Master.Loyalty.cs:5999-6002).
            //    StdParam = Trim().ToUpper(), rỗng -> null (05.Refs.Biz\Biz\Loyalty.TVO\Loyalty.TVO.Utils\Utils.cs:29).
            //    StdStr   = Trim().Normalize(), rỗng -> null (cùng tệp Utils.cs:29).
            string? strCardType = StdParam(req.CardType);
            string? strCardTypeName = StdStr(req.CardTypeName);
            string? strNetworkID = StdParam(req.NetworkID);

            // Value = Convert.ToDouble(objValue) (:6002): null -> 0; chuỗi không phải số -> lỗi -> mã mặc định.
            double dblValue;
            try
            {
                dblValue = ConvertToDouble(req.Value);
            }
            catch (Exception)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_CardType_Create", "Check.objValue", req.Value?.ToString());
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

            // 8. Thành công: gate :400-401 SetSuccess + Data = null.
            return Ok(new { Success = true, Data = (object?)null });
        }
        catch (Exception)
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Chuyển Value như <c>Convert.ToDouble(objValue)</c> của nguồn (Master.Loyalty.cs:6002):
    /// null/thiếu -> 0; số -> giá trị; chuỗi số -> parse; chuỗi không phải số -> ném lỗi.
    /// </summary>
    private static double ConvertToDouble(System.Text.Json.JsonElement? value)
    {
        if (value == null) return 0d;
        var el = value.Value;
        switch (el.ValueKind)
        {
            case System.Text.Json.JsonValueKind.Number:
                return el.GetDouble();
            case System.Text.Json.JsonValueKind.String:
                return Convert.ToDouble(el.GetString());
            case System.Text.Json.JsonValueKind.Null:
            case System.Text.Json.JsonValueKind.Undefined:
                return 0d;
            case System.Text.Json.JsonValueKind.True:
                return 1d;
            case System.Text.Json.JsonValueKind.False:
                return 0d;
            default:
                return Convert.ToDouble(el.ToString());
        }
    }

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
    #endregion
}
