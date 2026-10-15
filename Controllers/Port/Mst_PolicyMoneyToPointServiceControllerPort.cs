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
// PORT 1:1 — màn danh mục chính sách quy đổi tiền sang điểm (Mst_PolicyMoneyToPointService) của dự án nguồn 2022.7.Loyalty (#194, #195).
//
// Nguồn controller (tầng gate):
//   Mst_PolicyMoneyToPointServiceController.cs:24-133     GetAllActive -> WA_Mst_PolicyMoneyToPointService_Get (#194)
//   Mst_PolicyMoneyToPointServiceController.cs:134-210    GetByPolicyCode -> WA_Mst_PolicyMoneyToPointService_Get (#195)
//   Mst_PolicyMoneyToPointServiceController.cs:214-239    strWhereClause_Mst_PolicyMoneyToPointService (mệnh đề lọc GetAllActive)
//   Mst_PolicyMoneyToPointServiceController.cs:240-255    strWhereClause_Mst_PolicyMoneyToPointService_By_PolicyCode (mệnh đề lọc GetByPolicyCode)
// Nguồn API:
//   MstPolicyMoneyToPointServiceController.cs:94-164     WA_Mst_PolicyMoneyToPointService_Get
// Nguồn biz:
//   Master.Loyalty.cs:18347-18460                        WAS_Mst_PolicyMoneyToPointService_Get
//   Master.Loyalty.cs:18461-18617                        Mst_PolicyMoneyToPointService_Get
//   Master.Loyalty.cs:18618-18820                        Mst_PolicyMoneyToPointService_GetX (bản LIVE)
//
// ĐẶC ĐIỂM VÀ QUY TẮC NGHIỆP VỤ NGUỒN:
//   * Endpoint là POST (nguồn gate Mst_PolicyMoneyToPointServiceController.cs:24, :134).
//   * GetAllActive (#194): không nhận body (:24), Ft_RecordStart = "0", Ft_RecordCount = "123456000" (:66-67).
//     Dòng //FlagActive = "1" bị comment tại gate (:53), hàm lọc strWhereClause_Mst_PolicyMoneyToPointService
//     không lọc OrgID nên không lọc gì => trả toàn bộ chính sách (kể cả inactive).
//     Trả Data là mảng trần (gate :113-120), rỗng thì null.
//   * GetByPolicyCode (#195): [HttpPost("getbypolicycode")], body { PolicyCode }, EmptyBodyBehavior.Allow (gate :134-210).
//     - PolicyCode rỗng -> KHÔNG truy vấn, Data = { Mst_PolicyMoneyToPointService: null, Lst_Mst_PolicyMoneyToPointService: null } (:153-154, :193-197).
//     - Ngược lại lọc strWhereClause_Mst_PolicyMoneyToPointService_By_PolicyCode CHỈ với PolicyCode -> PolicyCode = (:247;
//       nhánh FlagActive :249-252 không truyền -> không lọc FlagActive -> chính sách inactive như P02 VẪN lấy được).
//       Ft_RecordStart = "0", Ft_RecordCount = "1" (:173-174).
//       Xin CẢ header và Dtl: Rt_Cols_Mst_PolicyMoneyToPointService = "*", Rt_Cols_Mst_PolicyMoneyToPointServiceDtl = "*" (:177-178).
//     - Kết quả (:182-197):
//       Mst_PolicyMoneyToPointService = header[0] hoặc null.
//       Lst_Mst_PolicyMoneyToPointService = danh sách DTL (nguồn đặt nhầm tên biến, GIỮ NGUYÊN tên trường JSON) hoặc null nếu rỗng.
//     - Biz GetX (:18742-18751): Dtl = t.MyIdxSeq, mpmtpsl.* từ draft inner join Dtl on PolicyCode, order by mpmtpsl.LogLUDTimeUTC desc
//       (hòa trùng: sắp phụ CardType asc, ghi comment).
//   * Bỏ qua có chủ đích (loại Đ):
//     - drAbilityOfUser (Master.Loyalty.cs:18661) nguồn gọi nhưng không dùng (:18673 bị comment) -> bỏ qua, ghi comment.
//     - FK CardType -> Mst_CardType, FK PolicyCode -> Mst_PolicyMoneyToPointService (20221026.CreateDatabase.sql:656-670)
//       chỉ ghi chú comment, chưa cấu hình FK navigation EF.
//     - CheckGatewayAuthentication, CheckAuthorize (gate :37-38, :143-144), Sys_User_CheckAuthorize (:18527-18540):
//       hệ thống đích chưa có hệ phân quyền.
//     - Ghi RequestLog/ReturnLog (:18506-18513, :18607-18614).
// ============================================================================================

/// <summary>Port 1:1 <c>GetByPolicyCode</c> — body nhận PolicyCode (Mst_PolicyMoneyToPointServiceController.cs:134).</summary>
public class MstPolicyMoneyToPointServiceGetByPolicyCodeReq
{
    public string? PolicyCode { get; set; }
}

/// <summary>
/// DTO header chính sách trong request Save (nguồn Mst_PolicyMoneyToPointServiceController.cs:259-352, Master.Loyalty.cs:17579-17601).
/// </summary>
public class MstPolicyMoneyToPointServiceSaveHeaderReq
{
    public string? PolicyCode { get; set; }
    public string? EffDateStart { get; set; }
    public string? EffDateEnd { get; set; }
    public string? Remark { get; set; }
}

/// <summary>
/// DTO chi tiết chính sách trong request Save (nguồn Master.Loyalty.cs:18103-18161).
/// </summary>
public class MstPolicyMoneyToPointServiceSaveDtlReq
{
    public string? CardType { get; set; }
    public double? ConvertValue { get; set; }
    public double? ConvertPoint { get; set; }
    public double? ValueRankCardType { get; set; }
    public object? DiscountRate { get; set; }
    public string? Remark { get; set; }
}

/// <summary>
/// Body của action Save: { Mst_PolicyMoneyToPointService: { ... }, Lst_Mst_PolicyMoneyToPointServiceDtl: [ ... ] }
/// (nguồn gate Mst_PolicyMoneyToPointServiceController.cs:259-352).
/// </summary>
public class MstPolicyMoneyToPointServiceSaveReq
{
    public MstPolicyMoneyToPointServiceSaveHeaderReq? Mst_PolicyMoneyToPointService { get; set; }
    public List<MstPolicyMoneyToPointServiceSaveDtlReq>? Lst_Mst_PolicyMoneyToPointServiceDtl { get; set; }
}

[ApiController]
[Route("api/mst_policymoneytopointservice")]
[Produces("application/json")]
public class Mst_PolicyMoneyToPointServiceControllerPort : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public Mst_PolicyMoneyToPointServiceControllerPort(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    /// <summary>
    /// Port 1:1 <c>Mst_PolicyMoneyToPointServiceController.GetAllActive</c> (nguồn gate Mst_PolicyMoneyToPointServiceController.cs:24-133, #194).
    /// Không nhận body. Dòng FlagActive = "1" bị comment tại gate (:53), hàm lọc strWhereClause_Mst_PolicyMoneyToPointService
    /// (:214-239) không lọc OrgID nên không lọc gì => trả toàn bộ chính sách (kể cả inactive).
    /// Ft_RecordStart = "0", Ft_RecordCount = "123456000" (:66-67).
    /// Trả Data là mảng trần, null nếu rỗng (gate :113-120).
    /// </summary>
    [HttpPost("getallactive")]
    public async Task<IActionResult> GetAllActive()
    {
        // Nguồn: Ft_RecordStart = "0", Ft_RecordCount = "123456000" (gate Mst_PolicyMoneyToPointServiceController.cs:66-67).
        // Dòng //FlagActive = "1" bị comment (:53), KeyWord null -> gọi GetXAsync(null, null, 0, 123456000).
        var (_, rows) = await GetXAsync(null, null, 0, 123456000);

        // Nguồn :113-120: Data = listMst_PolicyMoneyToPointService (mảng trần, null nếu rỗng).
        return Ok(new { Success = true, Data = rows.Count > 0 ? rows : null });
    }

    /// <summary>
    /// Port 1:1 <c>Mst_PolicyMoneyToPointServiceController.GetByPolicyCode</c> (nguồn gate Mst_PolicyMoneyToPointServiceController.cs:134-210, #195).
    /// Body JSON nhận PolicyCode, EmptyBodyBehavior.Allow.
    /// PolicyCode rỗng -> KHÔNG truy vấn, Data = { Mst_PolicyMoneyToPointService: null, Lst_Mst_PolicyMoneyToPointService: null } (gate :153-154, :193-197).
    /// Ngược lại lọc PolicyCode = (Like + PortQuery.Equal, :247); không lọc FlagActive -> chính sách inactive vẫn lấy được.
    /// Ft_RecordStart = "0", Ft_RecordCount = "1" (:173-174).
    /// Xin cả header và Dtl (Rt_Cols_* = "*", gate :177-178).
    /// Kết quả:
    ///   - Data.Mst_PolicyMoneyToPointService: header[0] hoặc null (:182-185).
    ///   - Data.Lst_Mst_PolicyMoneyToPointService: danh sách DTL (nguồn đặt nhầm tên biến, GIỮ NGUYÊN tên trường JSON) hoặc null nếu rỗng (:186-189, :196).
    /// </summary>
    [HttpPost("getbypolicycode")]
    public async Task<IActionResult> GetByPolicyCode(
        [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] MstPolicyMoneyToPointServiceGetByPolicyCodeReq? req)
    {
        var policyCode = req?.PolicyCode;

        // PolicyCode rỗng -> nguồn không truy vấn (:156) và trả cả 2 trường null (:153-154, :193-197).
        if (string.IsNullOrWhiteSpace(policyCode))
        {
            return Ok(new
            {
                Success = true,
                Data = new
                {
                    Mst_PolicyMoneyToPointService = (object?)null,
                    Lst_Mst_PolicyMoneyToPointService = (object?)null
                }
            });
        }

        // Lấy 1 dòng theo PolicyCode (Ft_RecordStart = "0", Ft_RecordCount = "1" — :173-174).
        // getDtl = true -> xin thêm danh sách Dtl (Rt_Cols_Mst_PolicyMoneyToPointServiceDtl = "*" — :178).
        // Nhánh FlagActive không truyền ở gate (:158-161) nên không lọc FlagActive -> chính sách inactive vẫn lấy được.
        var (_, rows, dtlRows) = await GetXAsync(null, null, 0, 1, policyCodeEqual: policyCode, getDtl: true);

        // Nguồn gate :182-189, :193-197:
        // Mst_PolicyMoneyToPointService = header[0] hoặc null.
        // Lst_Mst_PolicyMoneyToPointService = danh sách DTL (nguồn đặt nhầm tên biến Lst_Mst_PolicyMoneyToPointService
        // nhưng kiểu List<Mst_PolicyMoneyToPointServiceDtl>, GIỮ NGUYÊN tên trường JSON này) hoặc null nếu rỗng.
        var objMst_PolicyMoneyToPointService = rows.Count > 0 ? rows[0] : null;
        var lst_Mst_PolicyMoneyToPointService = dtlRows.Count > 0 ? dtlRows : null;

        return Ok(new
        {
            Success = true,
            Data = new
            {
                Mst_PolicyMoneyToPointService = objMst_PolicyMoneyToPointService,
                Lst_Mst_PolicyMoneyToPointService = lst_Mst_PolicyMoneyToPointService
            }
        });
    }

    // ============================================================================================
    // Save — nguồn Mst_PolicyMoneyToPointServiceController.cs:259-352 (gate)
    //   -> MstPolicyMoneyToPointServiceController.cs:22-92 (API WA_Mst_PolicyMoneyToPointService_Save)
    //   -> Master.Loyalty.cs:17542-17632 (WAS_Mst_PolicyMoneyToPointService_Save)
    //   -> :17633-17776 (Mst_PolicyMoneyToPointService_Save)
    //   -> :17963-18345 (Mst_PolicyMoneyToPointService_SaveX).
    //
    // GIỮ ĐÚNG NGUỒN:
    //   * Body JSON: { Mst_PolicyMoneyToPointService: { EffDateStart, ... }, Lst_Mst_PolicyMoneyToPointServiceDtl: [ ... ] }
    //   * seq tăng cả khi lỗi: Lấy sequence trong transaction RIÊNG đã commit độc lập (gate Mst_PolicyMoneyToPointServiceController.cs:293-313,
    //     SeqType.PolicyCode — Seq.cs:146: bảng Seq_PolicyCode, format "{0}POLICYCODE.{3}.{1:00000}{2}", max 100000)
    //     qua bảng Seq_PolicyCode rồi gán vào header (:315-317). Do đó sequence tăng MỖI LẦN GỌI vượt qua bước body rỗng,
    //     KỂ CẢ KHI LƯU LỖI SAU ĐÓ.
    //     Body rỗng hoàn toàn -> ServiceException("strJson null") -> mã mặc định Mst_PolicyMoneyToPointService_Save (KHÔNG tăng seq).
    //     Body {} (thiếu Mst_PolicyMoneyToPointService) -> NullReference SAU khi lấy seq -> Mst_PolicyMoneyToPointService_Save (seq ĐÃ tăng).
    //   * EffDateStart: nguồn Master.Loyalty.cs:18008 gọi StdDate(header.EffDateStart). EffDateStart null/rỗng -> cột NOT NULL trong DB
    //     gây lỗi -> mã mặc định Mst_PolicyMoneyToPointService_Save.
    //   * EffDateEnd cố định: nguồn Master.Loyalty.cs:18009 gán cứng StdDate(Convert.ToDateTime("2100-01-01 00:00:00")).
    //   * BirthPolicy check bỏ: nguồn Master.Loyalty.cs:18017-18034 gọi Mst_BirthPolicy_CheckDB(PolicyCode, "", ...) do chép nhầm
    //     từ module BirthPolicy, kết quả kiểm tra không được sử dụng và bảng này không có ở hệ thống đích -> bỏ qua theo đề bài.
    //   * Remark header bỏ: câu lệnh insert nguồn Master.Loyalty.cs:18214-18234 không có cột Remark nên giá trị Remark của header
    //     bị bỏ qua (không lưu vào DB, Remark lưu null).
    //   * Dtl: Lst null -> danh sách rỗng (Master.Loyalty.cs:17579-17580, không lỗi).
    //     CardType chuẩn hoá bằng StdParam (Trim().ToUpper(), rỗng -> null) — Master.Loyalty.cs:18105.
    //     ConvertValue, ConvertPoint, ValueRankCardType, DiscountRate, Remark chế độ "" = GIỮ NGUYÊN (Remark KHÔNG trim) — :18106-18110.
    //     Kiểm tra Mst_CardType_CheckDB(CardType, "1", "1") (:18126-18132): không thấy -> Mst_CardType_CheckDB_CardTypeNotFound;
    //     FlagActive != '1' -> Mst_CardType_CheckDB_FlagActiveNotMatched (NULL/rỗng FlagActive vẫn qua).
    //     DiscountRate khác rỗng: Convert.ToDouble (sai định dạng -> ..._Save); < 0 hoặc > 100 -> Mst_PolicyMoneyToPointService_Save_InvalidValue (:18134-18153).
    //     Trùng CardType -> vi phạm PK (PolicyCode, CardType) -> mã mặc định Mst_PolicyMoneyToPointService_Save (kiểm tra sau vòng lặp).
    //   * Vô hiệu hoá chính sách khác: Master.Loyalty.cs:18294-18320 cập nhật FlagActive = '0' cho mọi header VÀ mọi Dtl có PolicyCode <> mã mới.
    //     Chính sách mới là chính sách duy nhất hoạt động ('1').
    //   * Lưu: 1 transaction duy nhất (:17667, commit :17739), lỗi -> rollback + PortResult.Fail. Thành công -> Data = null (:338-339).
    //   * NetworkID lấy từ cấu hình: IConfiguration["Biz_NetworkID"] ?? "1001" (GAP nguồn lấy từ session user).
    //   * LogLUBy = "port", LogLUDTimeUTC = DateTime.UtcNow.
    // ============================================================================================
    [HttpPost("save")]
    public async Task<IActionResult> Save(
        [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] MstPolicyMoneyToPointServiceSaveReq? req)
    {
        // 0. Body rỗng / null: gate :269-275 ném ServiceException("strJson null") -> mã mặc định Mst_PolicyMoneyToPointService_Save.
        // Bước này xảy ra TRƯỚC khi gọi sequence nên KHÔNG tăng seq.
        if (req == null)
        {
            return PortResult.Fail("Mst_PolicyMoneyToPointService_Save", "Check.strJson", null);
        }

        // Seq tăng cả khi lỗi: Lấy sequence trong transaction RIÊNG đã commit độc lập (gate Mst_PolicyMoneyToPointServiceController.cs:293-313,
        // SeqType.PolicyCode — Seq.cs:146: bảng Seq_PolicyCode, format "{0}POLICYCODE.{3}.{1:00000}{2}", max 100000).
        // Nhờ transaction riêng, mã sequence đã tăng sẽ không bị rollback kể cả khi bước lưu sau bị lỗi.
        var seq = await PortSeq.NextAsync<Seq_PolicyCode>(_db);
        var policyCode = PortSeq.Build("{0}POLICYCODE.{3}.{1:00000}{2}", "", seq, 100000, "", DateTime.UtcNow);

        // Header null (client gửi `{}` thiếu Mst_PolicyMoneyToPointService):
        // Nguồn gate :315 gán objMst_PolicyMoneyToPointService.PolicyCode ném NullReferenceException SAU khi lấy seq -> mã mặc định Mst_PolicyMoneyToPointService_Save (seq ĐÃ tăng).
        if (req.Mst_PolicyMoneyToPointService == null)
        {
            return PortResult.Fail("Mst_PolicyMoneyToPointService_Save", "Check.Mst_PolicyMoneyToPointService", null);
        }

        // Kiểm tra EffDateStart của header (Master.Loyalty.cs:18008 StdDate(header.EffDateStart)):
        // Cột EffDateStart là DateTime NOT NULL trong DB. Nếu thiếu hoặc sai định dạng date -> mã mặc định Mst_PolicyMoneyToPointService_Save.
        // Thứ tự lỗi như nguồn: SAI định dạng -> StdDate ném ngay (:18008, TRƯỚC kiểm Dtl) ; THIẾU -> StdDate = null,
        // chỉ lỗi khi INSERT cột NOT NULL (SAU kiểm Dtl) -> kiểm "thiếu" ở sau vòng lặp Dtl.
        var effDateStartRaw = req.Mst_PolicyMoneyToPointService.EffDateStart;
        var effDateStartMissing = string.IsNullOrWhiteSpace(effDateStartRaw);
        DateTime parsedEffDateStart = default;
        if (!effDateStartMissing && !DateTime.TryParse(effDateStartRaw, out parsedEffDateStart))
        {
            return PortResult.Fail("Mst_PolicyMoneyToPointService_Save", "Check.EffDateStart", effDateStartRaw);
        }
        var effDateStartUtc = DateTime.SpecifyKind(parsedEffDateStart.Date, DateTimeKind.Utc);

        // EffDateEnd cố định: nguồn Master.Loyalty.cs:18009 gán cứng StdDate(Convert.ToDateTime("2100-01-01 00:00:00")).
        var effDateEndUtc = new DateTime(2100, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // BirthPolicy check bỏ: nguồn Master.Loyalty.cs:18017-18034 gọi Mst_BirthPolicy_CheckDB(PolicyCode, "", ...) do chép nhầm
        // từ module BirthPolicy, kết quả kiểm tra không được sử dụng và bảng này không có ở hệ thống đích -> bỏ qua theo đề bài.

        // Remark header bỏ: câu lệnh insert nguồn Master.Loyalty.cs:18214-18234 không có cột Remark nên giá trị Remark của header bị bỏ qua (lưu null).

        // Lst Dtl null -> danh sách rỗng (Master.Loyalty.cs:17579-17580, không lỗi).
        var dtlInputList = req.Lst_Mst_PolicyMoneyToPointServiceDtl ?? new List<MstPolicyMoneyToPointServiceSaveDtlReq>();

        var networkId = _config["Biz_NetworkID"] ?? "1001";
        var now = DateTime.UtcNow;
        var newDtls = new List<Mst_PolicyMoneyToPointServiceDtl>();

        // Duyệt từng dòng Dtl theo thứ tự gửi lên (Master.Loyalty.cs:18119-18163)
        for (int nScan = 0; nScan < dtlInputList.Count; nScan++)
        {
            var drScan = dtlInputList[nScan];
            // CardType chuẩn hoá bằng StdParam (Trim().ToUpper(), rỗng -> null) — Master.Loyalty.cs:18105
            var strCardType = StdParam(drScan.CardType);

            // Mst_CardType_CheckDB(CardType, "1", "1") — Master.Loyalty.cs:18126-18132
            // Tra cứu Mst_CardType trong DB (so sánh Like + PortQuery.Equal không phân biệt hoa/thường)
            var cardTypeDb = await _db.Set<Mst_CardType>().AsNoTracking()
                .FirstOrDefaultAsync(x => EF.Functions.Like((x.CardType ?? "").ToLower(), PortQuery.Equal(strCardType ?? ""), PortQuery.Esc));

            if (cardTypeDb == null)
            {
                return PortResult.Fail("Mst_CardType_CheckDB_CardTypeNotFound", "Check.CardType", strCardType);
            }

            // Nguồn Mst_CardType_CheckDB: !"1".Contains(Convert.ToString(FlagActive)) — NULL/rỗng FlagActive vẫn qua (như #230)
            if (!"1".Contains(cardTypeDb.FlagActive ?? ""))
            {
                return PortResult.Fail("Mst_CardType_CheckDB_FlagActiveNotMatched",
                    "Check.CardType", strCardType,
                    "Check.strFlagActiveListToCheck", "1",
                    "DB.FlagActive", cardTypeDb.FlagActive);
            }

            // Kiểm tra DiscountRate: khác rỗng -> Convert.ToDouble (sai định dạng -> ..._Save) ; < 0 hoặc > 100 -> Mst_PolicyMoneyToPointService_Save_InvalidValue (Master.Loyalty.cs:18134-18153)
            double? discountRateVal = null;
            if (drScan.DiscountRate != null)
            {
                try
                {
                    if (drScan.DiscountRate is System.Text.Json.JsonElement je)
                    {
                        if (je.ValueKind == System.Text.Json.JsonValueKind.Number)
                        {
                            discountRateVal = je.GetDouble();
                        }
                        else if (je.ValueKind == System.Text.Json.JsonValueKind.String)
                        {
                            var s = je.GetString();
                            if (!string.IsNullOrWhiteSpace(s))
                            {
                                discountRateVal = Convert.ToDouble(s);
                            }
                        }
                        else if (je.ValueKind != System.Text.Json.JsonValueKind.Null && je.ValueKind != System.Text.Json.JsonValueKind.Undefined)
                        {
                            discountRateVal = Convert.ToDouble(je.ToString());
                        }
                    }
                    else
                    {
                        var s = drScan.DiscountRate.ToString();
                        if (!string.IsNullOrWhiteSpace(s))
                        {
                            discountRateVal = Convert.ToDouble(drScan.DiscountRate);
                        }
                    }
                }
                catch
                {
                    return PortResult.Fail("Mst_PolicyMoneyToPointService_Save", "Check.DiscountRate", drScan.DiscountRate);
                }
            }

            if (discountRateVal.HasValue)
            {
                if (discountRateVal.Value < 0 || discountRateVal.Value > 100)
                {
                    return PortResult.Fail("Mst_PolicyMoneyToPointService_Save_InvalidValue",
                        "Check.CardType", drScan.CardType,
                        "Check.DiscountRate", drScan.DiscountRate,
                        "Check.ErrCoditional", "DiscountRate < 0 or DiscountRate > 100");
                }
            }

            // Dtl: ConvertValue, ConvertPoint, ValueRankCardType, DiscountRate, Remark chế độ "" = GIỮ NGUYÊN (Remark KHÔNG trim) — Master.Loyalty.cs:18106-18110
            newDtls.Add(new Mst_PolicyMoneyToPointServiceDtl
            {
                PolicyCode = policyCode,
                CardType = strCardType!,
                NetworkID = networkId,
                ConvertValue = drScan.ConvertValue,
                ConvertPoint = drScan.ConvertPoint,
                ValueRankCardType = drScan.ValueRankCardType,
                DiscountRate = discountRateVal,
                FlagActive = "1",
                Remark = drScan.Remark, // GIỮ NGUYÊN không trim
                LogLUBy = "port",
                LogLUDTimeUTC = now
            });
        }

        // Kiểm tra trùng CardType sau vòng lặp: nếu trùng CardType -> vi phạm PK (PolicyCode, CardType) -> mã lỗi Mst_PolicyMoneyToPointService_Save
        var distinctCardTypes = newDtls.Select(x => x.CardType).Distinct(StringComparer.OrdinalIgnoreCase).Count();
        if (distinctCardTypes < newDtls.Count)
        {
            return PortResult.Fail("Mst_PolicyMoneyToPointService_Save", "Check.DuplicateCardType", null);
        }

        // EffDateStart thiếu -> insert header vào cột NOT NULL lỗi (:18214-18234) -> mã mặc định Mst_PolicyMoneyToPointService_Save.
        if (effDateStartMissing)
        {
            return PortResult.Fail("Mst_PolicyMoneyToPointService_Save", "Check.EffDateStart", effDateStartRaw);
        }

        // Lưu: 1 transaction duy nhất (Master.Loyalty.cs:17667, commit :17739)
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // 1. Insert header mới: Remark không lưu (null) — Master.Loyalty.cs:18214-18234
            var newHeader = new Mst_PolicyMoneyToPointService
            {
                PolicyCode = policyCode,
                NetworkID = networkId,
                EffDateStart = effDateStartUtc,
                EffDateEnd = effDateEndUtc,
                FlagActive = "1",
                Remark = null, // Remark header KHÔNG lưu
                LogLUBy = "port",
                LogLUDTimeUTC = now
            };
            _db.Set<Mst_PolicyMoneyToPointService>().Add(newHeader);

            // 2. Insert các dòng Dtl mới — Master.Loyalty.cs:18238-18269
            if (newDtls.Count > 0)
            {
                _db.Set<Mst_PolicyMoneyToPointServiceDtl>().AddRange(newDtls);
            }
            await _db.SaveChangesAsync();

            // 3. Vô hiệu hoá chính sách khác: Master.Loyalty.cs:18294-18320 cập nhật FlagActive = '0' cho mọi header VÀ mọi Dtl có PolicyCode <> mã mới
            await _db.Database.ExecuteSqlRawAsync(
                "UPDATE \"Mst_PolicyMoneyToPointService\" SET \"FlagActive\" = '0' WHERE LOWER(\"PolicyCode\") <> LOWER({0})", policyCode);
            await _db.Database.ExecuteSqlRawAsync(
                "UPDATE \"Mst_PolicyMoneyToPointServiceDtl\" SET \"FlagActive\" = '0' WHERE LOWER(\"PolicyCode\") <> LOWER({0})", policyCode);

            await tx.CommitAsync();
            return Ok(new { Success = true, Data = (object?)null });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return PortResult.Fail("Mst_PolicyMoneyToPointService_Save", "Exception", ex.Message);
        }
    }

    private static string? StdParam(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim().ToUpperInvariant();

    /// <summary>
    /// Overload giữ nguyên tương thích cho GetAllActive (#194).
    /// </summary>
    private async Task<(int itemCount, List<object> rows)> GetXAsync(string? keyWord, string? flagActiveCsv, long recordStart, long recordCount)
    {
        var (itemCount, rows, _) = await GetXAsync(keyWord, flagActiveCsv, recordStart, recordCount, null, false);
        return (itemCount, rows);
    }

    /// <summary>
    /// Thực hiện các bước truy vấn nghiệp vụ của Mst_PolicyMoneyToPointService_GetX (Master.Loyalty.cs:18618-18820).
    /// Bước 1: Draft từ Mst_PolicyMoneyToPointService (lọc KeyWord like PolicyCode, FlagActive IN, PolicyCode = policyCodeEqual, order by LogLUDTimeUTC desc, cắt trang).
    /// Bước 2: Select header từ #tbl_Mst_PolicyMoneyToPointService_Filter join Mst_PolicyMoneyToPointService theo PolicyCode, order by LogLUDTimeUTC desc.
    /// Bước 3: Nếu getDtl = true, select chi tiết Dtl từ #tbl_Mst_PolicyMoneyToPointService_Filter join Mst_PolicyMoneyToPointServiceDtl theo PolicyCode,
    ///         order by LogLUDTimeUTC desc (hòa trùng: sắp phụ theo CardType asc, ghi comment theo đề bài) (Master.Loyalty.cs:18742-18751).
    /// drAbilityOfUser (Master.Loyalty.cs:18661) nguồn gọi nhưng không dùng (:18673 bị comment) -> bỏ qua, không port.
    /// </summary>
    private async Task<(int itemCount, List<object> rows, List<object> dtlRows)> GetXAsync(
        string? keyWord,
        string? flagActiveCsv,
        long recordStart,
        long recordCount,
        string? policyCodeEqual = null,
        bool getDtl = false)
    {
        // drAbilityOfUser (Master.Loyalty.cs:18661) nguồn gọi nhưng bị comment không dùng (:18673) -> không port, ghi comment.

        // -----------------------------------------------------------------------------------------
        // BƯỚC 1: DRAFT (#tbl_Mst_PolicyMoneyToPointService_Filter_Draft) — Master.Loyalty.cs:18677-18686
        // select distinct identity(bigint, 0, 1) MyIdxSeq, mpmtps.PolicyCode, mpmtps.LogLUDTimeUTC
        // from Mst_PolicyMoneyToPointService mpmtps where (1=1) zzB_Where_strFilter_zzE order by mpmtps.LogLUDTimeUTC desc
        // -----------------------------------------------------------------------------------------
        var q = _db.Set<Mst_PolicyMoneyToPointService>().AsNoTracking().AsQueryable();

        // Lọc KeyWord: `PolicyCode LIKE '%x%'` (gate Mst_PolicyMoneyToPointServiceController.cs:219-222).
        if (!string.IsNullOrWhiteSpace(keyWord))
        {
            var kw = PortQuery.Contains(keyWord);
            q = q.Where(x => EF.Functions.Like((x.PolicyCode ?? "").ToLower(), kw, PortQuery.Esc));
        }

        // Lọc PolicyCode: `PolicyCode = 'x'` (gate Mst_PolicyMoneyToPointServiceController.cs:247 — strWhereClause_Mst_PolicyMoneyToPointService_By_PolicyCode).
        // Nguồn dùng `=` trên SQL Server collation KHÔNG phân biệt hoa/thường ⇒ Like KHÔNG wildcard, hạ chữ 2 phía (PortQuery.Equal).
        if (!string.IsNullOrWhiteSpace(policyCodeEqual))
        {
            var pc = PortQuery.Equal(policyCodeEqual);
            q = q.Where(x => EF.Functions.Like((x.PolicyCode ?? "").ToLower(), pc, PortQuery.Esc));
        }

        // Lọc FlagActive: danh sách phân cách dấu phẩy -> IN ('a','b') (gate Mst_PolicyMoneyToPointServiceController.cs:228-236).
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

        // Sắp xếp mpmtps.LogLUDTimeUTC desc (Master.Loyalty.cs:18685)
        // Hòa trùng LogLUDTimeUTC: sắp phụ theo PolicyCode asc để thứ tự xác định
        var draftList = await q
            .OrderByDescending(x => x.LogLUDTimeUTC)
            .ThenBy(x => x.PolicyCode)
            .ToListAsync();

        // MyCount = Count(0) của bảng draft (#tbl_Mst_PolicyMoneyToPointService_Filter_Draft) — :18689
        var itemCount = draftList.Count;

        // Cắt trang draft (#tbl_Mst_PolicyMoneyToPointService_Filter, :18693-18700):
        // t.MyIdxSeq >= @nFilterRecordStart and t.MyIdxSeq <= @nFilterRecordEnd
        // với nFilterRecordStart = recordStart, nFilterRecordEnd = recordStart + recordCount - 1 (:18655-18656).
        var draftPage = (recordCount <= 0 || recordStart >= draftList.Count)
            ? new List<(long MyIdxSeq, Mst_PolicyMoneyToPointService Policy)>()
            : draftList
                .Select((p, idx) => (MyIdxSeq: (long)idx, Policy: p))
                .Where(x => x.MyIdxSeq >= recordStart && x.MyIdxSeq < recordStart + recordCount)
                .ToList();

        // -----------------------------------------------------------------------------------------
        // BƯỚC 2: SELECT HEADER (Master.Loyalty.cs:18722-18729)
        // select t.MyIdxSeq, mpmtps.*
        // from #tbl_Mst_PolicyMoneyToPointService_Filter t
        //   inner join Mst_PolicyMoneyToPointService mpmtps on t.PolicyCode = mpmtps.PolicyCode
        // order by mpmtps.LogLUDTimeUTC desc
        // -----------------------------------------------------------------------------------------
        var rows = draftPage
            .OrderByDescending(x => x.Policy.LogLUDTimeUTC)
            .ThenBy(x => x.Policy.PolicyCode)
            .Select(x => (object)new
            {
                x.MyIdxSeq,
                x.Policy.PolicyCode,
                x.Policy.NetworkID,
                x.Policy.EffDateStart,
                x.Policy.EffDateEnd,
                x.Policy.FlagActive,
                x.Policy.Remark,
                x.Policy.LogLUBy,
                x.Policy.LogLUDTimeUTC
            })
            .ToList();

        // -----------------------------------------------------------------------------------------
        // BƯỚC 3: SELECT DTL (Master.Loyalty.cs:18742-18751)
        // select t.MyIdxSeq, mpmtpsl.*
        // from #tbl_Mst_PolicyMoneyToPointService_Filter t
        //   inner join Mst_PolicyMoneyToPointServiceDtl mpmtpsl on t.PolicyCode = mpmtpsl.PolicyCode
        // order by mpmtpsl.LogLUDTimeUTC desc
        // -----------------------------------------------------------------------------------------
        var dtlRows = new List<object>();
        if (getDtl && draftPage.Count > 0)
        {
            var policyCodes = draftPage.Select(x => x.Policy.PolicyCode).ToList();
            var policyMap = draftPage.ToDictionary(
                x => x.Policy.PolicyCode,
                x => x.MyIdxSeq,
                StringComparer.OrdinalIgnoreCase);

            List<Mst_PolicyMoneyToPointServiceDtl> dtlList;
            if (policyCodes.Count == 1)
            {
                var targetCode = policyCodes[0];
                dtlList = await _db.Set<Mst_PolicyMoneyToPointServiceDtl>()
                    .AsNoTracking()
                    .Where(d => EF.Functions.Like((d.PolicyCode ?? "").ToLower(), PortQuery.Equal(targetCode), PortQuery.Esc))
                    .ToListAsync();
            }
            else
            {
                dtlList = await _db.Set<Mst_PolicyMoneyToPointServiceDtl>()
                    .AsNoTracking()
                    .Where(d => policyCodes.Contains(d.PolicyCode))
                    .ToListAsync();
            }

            // Sắp xếp Dtl theo mpmtpsl.LogLUDTimeUTC desc (Master.Loyalty.cs:18750).
            // Hòa trùng LogLUDTimeUTC: sắp phụ theo CardType asc, ghi comment theo đề bài (Master.Loyalty.cs:18750).
            dtlRows = dtlList
                .OrderByDescending(d => d.LogLUDTimeUTC)
                .ThenBy(d => d.CardType)
                .Select(d => (object)new
                {
                    MyIdxSeq = policyMap.TryGetValue(d.PolicyCode, out var seq) ? seq : 0L,
                    d.PolicyCode,
                    d.CardType,
                    d.NetworkID,
                    d.ConvertValue,
                    d.ConvertPoint,
                    d.ValueRankCardType,
                    d.DiscountRate,
                    d.FlagActive,
                    d.Remark,
                    d.LogLUBy,
                    d.LogLUDTimeUTC
                })
                .ToList();
        }

        return (itemCount, rows, dtlRows);
    }
}
