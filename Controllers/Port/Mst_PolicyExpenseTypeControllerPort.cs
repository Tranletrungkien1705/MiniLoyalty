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
// PORT 1:1 — màn danh mục chính sách loại chi phí (Mst_PolicyExpenseType) của dự án nguồn 2022.7.Loyalty (#190, #191, #192).
//
// Nguồn controller (tầng gate):
//   Mst_PolicyExpenseTypeController.cs:23-103  Search -> WA_Mst_PolicyExpenseType_Get (#190)
//   Mst_PolicyExpenseTypeController.cs:104-213 GetAllActive -> WA_Mst_PolicyExpenseType_Get (#191)
//   Mst_PolicyExpenseTypeController.cs:214-277 GetByPolicyExpenseTypeNo -> WA_Mst_PolicyExpenseType_Get (#192)
//   Mst_PolicyExpenseTypeController.cs:281-298 strWhereClause_Mst_PolicyExpenseType (mệnh đề lọc Search / GetAllActive)
//   Mst_PolicyExpenseTypeController.cs:299-315 strWhereClause_Mst_PolicyExpenseType_By_PolicyExpenseTypeNo (mệnh đề lọc GetByPolicyExpenseTypeNo)
// Nguồn API:
//   MstPolicyExpenseTypeController.cs:96-164   WA_Mst_PolicyExpenseType_Get
// Nguồn biz:
//   Master.Loyalty.cs:8499-8602                WAS_Mst_PolicyExpenseType_Get
//   Master.Loyalty.cs:8603-8756                Mst_PolicyExpenseType_Get
//   Master.Loyalty.cs:8757-8929                Mst_PolicyExpenseType_GetX (SQL draft :8814-8824, SQL select cuối :8856-8867)
//
// ĐẶC ĐIỂM VÀ QUY TẮC NGHIỆP VỤ NGUỒN:
//   * Endpoint là POST (nguồn gate Mst_PolicyExpenseTypeController.cs:23, :104, :214).
//   * Tham số lọc Search: KeyWord, FlagActive, Ft_PageIndex = 0, Ft_PageSize = 10 (gate :23).
//   * KeyWord KHÔNG lọc: hàm lọc strWhereClause_Mst_PolicyExpenseType (:281-298) CHỈ xử lý FlagActive
//     (tách dấu phẩy -> toan tử IN :289-294), hoàn toàn không có nhánh nào lọc KeyWord.
//   * Action GetByPolicyExpenseTypeNo (#192):
//     - Nhận PolicyExpenseTypeNo (gate :214). Nếu PolicyExpenseTypeNo null hoặc rỗng -> không truy vấn (:234), trả Data = null (:264), Success = true (:263).
//     - Hàm lọc strWhereClause_Mst_PolicyExpenseType_By_PolicyExpenseTypeNo (:299-315): lọc PolicyExpenseTypeNo bằng toán tử '=' (:306).
//     - Nhánh FlagActive (:309-312) không truyền tại gate (:236-239) -> KHÔNG lọc FlagActive (bản ghi inactive FlagActive = '0' như PET06 vẫn lấy được).
//     - OrgID và NetworkID nhận ở Request.Headers (:225-226) nhưng không lọc vào Ft_WhereClause (:304-312).
//     - Phân trang Ft_RecordStart = "0", Ft_RecordCount = "1" (:251-252), Rt_Cols_Mst_PolicyExpenseType = "*" (:255).
//     - Kết quả: nếu Lst_Mst_PolicyExpenseType không rỗng -> Data = Lst_Mst_PolicyExpenseType[0] (object đơn lẻ, không phải mảng, không phải PageInfo); rỗng -> Data = null.
//   * Phân trang ở gate cho Search: Ft_RecordStart = (pageIndex * pageSize).ToString(), Ft_RecordCount = pageSize.ToString() (:49-50).
//   * Nghiệp vụ 2 bước trong Mst_PolicyExpenseType_GetX:
//     1. Bước 1 DRAFT (#tbl_Mst_PolicyExpenseType_Filter_Draft, :8814-8824):
//        select distinct identity(bigint, 0, 1) MyIdxSeq, mpet.PolicyExpenseTypeNo, mpet.ExpenseType, mpet.LogLUDTimeUTC
//        into #tbl_Mst_PolicyExpenseType_Filter_Draft
//        from Mst_PolicyExpenseType mpet where (1=1) zzB_Where_strFilter_zzE
//        order by mpet.LogLUDTimeUTC desc.
//        MyCount = Count(0) của bảng draft (:8827).
//        Cắt trang draft (#tbl_Mst_PolicyExpenseType_Filter, :8831-8838):
//        t.MyIdxSeq >= @nFilterRecordStart and t.MyIdxSeq <= @nFilterRecordEnd
//        với nFilterRecordStart = recordStart, nFilterRecordEnd = recordStart + recordCount - 1 (:8793-8794).
//     2. Bước 2 SELECT CUỐI (:8856-8867):
//        select t.MyIdxSeq, mpet.*, met.ExpenseTypeName
//        from #tbl_Mst_PolicyExpenseType_Filter t
//          inner join Mst_PolicyExpenseType mpet on t.PolicyExpenseTypeNo = mpet.PolicyExpenseTypeNo and t.ExpenseType = mpet.ExpenseType
//          left join Mst_ExpenseType met on t.ExpenseType = met.ExpenseType
//        order by mpet.LogLUDTimeUTC desc.
//        Cột trả về gồm: MyIdxSeq (0-based từ draft), toàn bộ 16 cột của mpet, và cột ExpenseTypeName (từ Mst_ExpenseType qua left join).
//   * Tính PageCount tại gate (:61-69, :78-86):
//     Chỉ tính khi Lst_Mst_PolicyExpenseType không rỗng và count > 0.
//     Nếu pageSize == 0 thì pageSize gán về 10 (PageSizeConfig = 10) rồi tính:
//     pageCount = (itemCount % pageSize == 0) ? itemCount / pageSize : itemCount / pageSize + 1.
//     Khi DataList rỗng hoặc pageSize = 0: DataList = [], PageCount = 0, ItemCount = itemCount.
//   * Bỏ qua có chủ đích (loại Đ):
//     - drAbilityOfUser (Master.Loyalty.cs:8798) nguồn lấy nhưng bị comment không dùng (:8810) -> không port, ghi comment.
//     - FK ExpenseType -> Mst_ExpenseType (20221026.CreateDatabase.sql:264-270) — chỉ ghi comment.
//     - CheckGatewayAuthentication, CheckAuthorize (gate :32-33, API :116-121), Sys_User_CheckAuthorize (:8667-8680):
//       hệ thống đích chưa có hệ phân quyền.
//     - OrgID, NetworkID gửi ở Request.Headers / WARQBase (gate :34-35, :46-47) nhưng hàm lọc :281-298 không lọc vào Ft_WhereClause.
// ============================================================================================

/// <summary>
/// Port 1:1 <c>Mst_PolicyExpenseTypeSearch</c> — tham số lọc + phân trang (Mst_PolicyExpenseTypeController.cs:23, 50-54).
/// </summary>
public class MstPolicyExpenseTypeSearchReq
{
    public string? KeyWord { get; set; }             // Từ khoá (gate nhận nhưng strWhereClause_Mst_PolicyExpenseType :281-298 KHÔNG lọc KeyWord)
    public string? FlagActive { get; set; }          // Cờ hoạt động, danh sách phân cách dấu phẩy (lọc IN) — :288-295
    public int Ft_PageIndex { get; set; } = 0;       // Trang (0-based) — :23, :44
    public int Ft_PageSize { get; set; } = 10;       // Số dòng/trang — :23, :45
}

/// <summary>
/// Port 1:1 body request cho <c>Mst_PolicyExpenseTypeController.GetByPolicyExpenseTypeNo</c> (Mst_PolicyExpenseTypeController.cs:214-277, #192).
/// </summary>
public class MstPolicyExpenseTypeGetByPolicyExpenseTypeNoReq
{
    public string? PolicyExpenseTypeNo { get; set; }
}

/// <summary>
/// DTO đại diện 1 dòng chi phí trong danh sách gửi lên của Save (Master.Loyalty.cs:8273-8287).
/// </summary>
public class MstPolicyExpenseTypeSaveRowReq
{
    public string? ExpenseType { get; set; }
    public string? ExpenseTypeNameActual { get; set; }
    public string? FlagPoint { get; set; }
    public string? FlagPointRank { get; set; }
    public double? AmountRate { get; set; }
    public double? MaxRankReviewPoint { get; set; }
    public double? MaxAccumulationPoint { get; set; }
    public string? FlagCountService { get; set; }
    public string? FlagDiscount { get; set; }
    public double? DiscountRate { get; set; }
    public string? Remark { get; set; }
}

/// <summary>
/// Body của Save: { Lst_Mst_PolicyExpenseType: [ ... ] } (gate Mst_PolicyExpenseTypeController.cs:328-336).
/// </summary>
public class MstPolicyExpenseTypeSaveReq
{
    public List<MstPolicyExpenseTypeSaveRowReq>? Lst_Mst_PolicyExpenseType { get; set; }
}

[ApiController]
[Route("api/mst_policyexpensetype")]
[Produces("application/json")]
public class Mst_PolicyExpenseTypeControllerPort : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public Mst_PolicyExpenseTypeControllerPort(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    /// <summary>
    /// Port 1:1 <c>Mst_PolicyExpenseTypeController.Search</c> (nguồn gate Mst_PolicyExpenseTypeController.cs:23-103).
    /// </summary>
    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] MstPolicyExpenseTypeSearchReq? req)
    {
        var r = req ?? new MstPolicyExpenseTypeSearchReq();
        var pageIndex = r.Ft_PageIndex;
        var pageSize = r.Ft_PageSize;

        // KeyWord KHÔNG lọc (gate Mst_PolicyExpenseTypeController.cs:281-298 không có nhánh nào lọc KeyWord).
        // Phân trang nguồn: Ft_RecordStart = (pageIndex * pageSize).ToString(), Ft_RecordCount = pageSize.ToString() (gate :49-50).
        var start = (long)pageIndex * pageSize;
        var (itemCount, rows) = await GetXAsync(r.FlagActive, start, pageSize);

        // PageCount chỉ tính khi Lst_Mst_PolicyExpenseType không rỗng và count > 0 (gate :61-69, :78-86).
        // Nếu pageSize == 0 thì gán tạm pageSize = 10 (PageSizeConfig = 10) để tính PageCount (:64-67).
        var ps = pageSize == 0 ? 10 : pageSize;
        var pageCount = rows.Count == 0 ? 0 : (itemCount % ps == 0 ? itemCount / ps : itemCount / ps + 1);

        return Ok(new
        {
            Success = true,
            Data = new PageInfo<object>(rows, itemCount, pageCount, pageIndex, pageSize)
        });
    }

    /// <summary>
    /// Port 1:1 <c>Mst_PolicyExpenseTypeController.GetAllActive</c> (nguồn gate Mst_PolicyExpenseTypeController.cs:104-213).
    /// - Gọi hàm lọc strWhereClause_Mst_PolicyExpenseType với FlagActive = "1" (:130-134, :281-298).
    ///   Lọc FlagActive theo toán tử IN (:289-294); KeyWord và OrgID KHÔNG lọc trong where clause (:281-298).
    /// - Ft_RecordStart = "0", Ft_RecordCount = "123456000" (BaseController.cs:30-31, :146-147).
    /// - Rt_Cols_Mst_PolicyExpenseType = "*" (:150).
    /// - Kết quả trả về: mảng trần List&lt;object&gt;, khi rỗng hoặc null trả về Data = null (:127, :153-155, :190-200).
    /// </summary>
    [HttpPost("getallactive")]
    public async Task<IActionResult> GetAllActive()
    {
        var (_, rows) = await GetXAsync("1", 0, 123456000);
        return Ok(new
        {
            Success = true,
            Data = rows.Count > 0 ? rows : null
        });
    }

    /// <summary>
    /// Port 1:1 <c>Mst_PolicyExpenseTypeController.GetByPolicyExpenseTypeNo</c> (nguồn gate Mst_PolicyExpenseTypeController.cs:214-277, #192).
    /// - Body JSON nhận PolicyExpenseTypeNo (EmptyBodyBehavior.Allow).
    /// - Khi PolicyExpenseTypeNo null hoặc whitespace -> trả về ngay Data = null, Success = true (không truy vấn DB, khớp gate :234, :263-264).
    /// - Khi có PolicyExpenseTypeNo: gọi strWhereClause_Mst_PolicyExpenseType_By_PolicyExpenseTypeNo (:299-315) lọc PolicyExpenseTypeNo bằng '=' (:306).
    ///   Không phân biệt hoa/thường trên SQLite (dùng Like không wildcard, escape chuẩn qua PortQuery.Equal).
    /// - KHÔNG lọc FlagActive (gate :236-239 không truyền FlagActive) -> bản ghi FlagActive = '0' (như PET06) vẫn lấy được.
    /// - OrgID và NetworkID không lọc vào where clause (:304-312).
    /// - Phân trang: Ft_RecordStart = "0", Ft_RecordCount = "1" (:251-252) -> lấy tối đa 1 dòng.
    /// - Kết quả: nếu có dòng trả về Data = rows[0] (object đơn lẻ), nếu không có dòng trả về Data = null (:258-264).
    /// </summary>
    [HttpPost("getbypolicyexpensetypeno")]
    public async Task<IActionResult> GetByPolicyExpenseTypeNo(
        [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] MstPolicyExpenseTypeGetByPolicyExpenseTypeNoReq? req)
    {
        var policyExpenseTypeNo = req?.PolicyExpenseTypeNo;

        // PolicyExpenseTypeNo rỗng -> nguồn không truy vấn (:234), Data = null (:264), Success = true (:263).
        if (string.IsNullOrWhiteSpace(policyExpenseTypeNo))
        {
            return Ok(new { Success = true, Data = (object?)null });
        }

        // Lấy 1 dòng theo PolicyExpenseTypeNo (Ft_RecordStart = "0", Ft_RecordCount = "1" — gate :251-252).
        var (_, rows) = await GetXAsync(null, 0, 1, policyExpenseTypeNo: policyExpenseTypeNo);

        // Nguồn: Data = objMst_PolicyExpenseType = Lst_Mst_PolicyExpenseType[0] khi có dòng, ngược lại null (:258-264).
        return Ok(new
        {
            Success = true,
            Data = rows.Count > 0 ? rows[0] : null
        });
    }

    // ============================================================================================
    // Save — nguồn Mst_PolicyExpenseTypeController.cs:319-411 (gate) -> MstPolicyExpenseTypeController.cs:23-93 (API)
    //   -> Master.Loyalty.cs:7876-7961 (WAS_Mst_PolicyExpenseType_Save) -> :7962-8101 (Mst_PolicyExpenseType_Save)
    //   -> :8203-8498 (Mst_PolicyExpenseType_SaveX) -> :8103-8195 (Mst_PolicyExpenseType_Perform).
    //
    // GIỮ ĐÚNG NGUỒN:
    //   * Body JSON: { Lst_Mst_PolicyExpenseType: [ { ExpenseType, ExpenseTypeNameActual, FlagPoint, ... } ] }
    //   * seq tăng cả khi lỗi: Lấy mã sequence trước khi lưu (gate :351-371, SeqType.PolicyExpenseTypeNo)
    //     qua bảng Seq_PolicyExpenseTypeNo trong transaction RIÊNG đã commit độc lập (Seq.cs:172-180),
    //     rồi gán CÙNG 1 mã cho mọi dòng (:373-376). Do đó sequence tăng MỖI LẦN GỌI vượt qua bước body rỗng,
    //     KỂ CẢ KHI LƯU LỖI SAU ĐÓ hoặc khi Lst_Mst_PolicyExpenseType là null (gate :373 gọi .Count() ném NullReferenceException SAU khi lấy seq).
    //     Body rỗng hoàn toàn -> ServiceException("strJson null") -> mã mặc định Mst_PolicyExpenseType_Save (KHÔNG tăng seq).
    //   * xoá toàn bảng: Mst_PolicyExpenseType_SaveX (:8391-8401) thực hiện `delete t from Mst_PolicyExpenseType t where (1=1)`
    //     xoá toàn bộ chính sách trước khi insert các dòng input mới.
    //   * [] xoá hết: Nếu client gửi Lst_Mst_PolicyExpenseType: [], không có lỗi xảy ra, bảng Mst_PolicyExpenseType
    //     bị XOÁ HẾT thành 0 dòng (hành vi nguồn :8261-8271 bị comment). CẢNH BÁO: xoá sạch dữ liệu chính sách.
    //   * Hist lưu lịch sử: Mst_PolicyExpenseType_Perform (:8103-8195, :8469) copy toàn bộ các dòng input vào bảng
    //     Mst_PolicyExpenseTypeHist (14 cột DB + AutoId PK tự tăng). Bảng này không bao giờ bị xoá khi Save.
    //   * validate DiscountRate:
    //     - 0 <= DiscountRate <= 100 (:8309-8328), vi phạm ném Mst_PolicyExpenseType_Save_InvalidValue.
    //     - Kiểm tra chéo: FlagDiscount != "1" mà DiscountRate != 0 (:8330-8346), vi phạm ném Mst_PolicyExpenseType_Save_InvalidValue.
    //   * Kiểm tra hợp lệ từng dòng:
    //     1) Mst_ExpenseType_CheckDB(ExpenseType, "1", "1") (:8301-8307, :6705-6769):
    //        Không tồn tại: ErridnLoyalty.Mst_ExpenseType_CheckDB_ExpenseTypeNotFound (:6737).
    //        FlagActive != '1': ErridnLoyalty.Mst_ExpenseType_CheckDB_FlagActiveNotMatched (:6764).
    //     2) validate DiscountRate như trên.
    //     3) Trùng ExpenseType trong input -> vi phạm composite PK (PolicyExpenseTypeNo, ExpenseType) -> rollback -> Mst_PolicyExpenseType_Save.
    //   * Lưu: 1 transaction duy nhất, lỗi -> rollback + PortResult.Fail. Thành công -> Data = null, Success = true (:397-398).
    //   * NetworkID lấy từ cấu hình: IConfiguration["Biz_NetworkID"] ?? "1001" (GAP nguồn lấy từ session user).
    //   * LogLUBy = "port", LogLUDTimeUTC = DateTime.UtcNow.
    // ============================================================================================
    [HttpPost("save")]
    public async Task<IActionResult> Save([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] MstPolicyExpenseTypeSaveReq? req)
    {
        // 0. Body rỗng / null: gate :329-335 ném ServiceException("strJson null") -> mã mặc định Mst_PolicyExpenseType_Save.
        // Bước này xảy ra TRƯỚC khi gọi sequence nên seq KHÔNG tăng.
        if (req == null)
        {
            return PortResult.Fail("Mst_PolicyExpenseType_Save", "Check.strJson", null);
        }

        // seq tăng cả khi lỗi: Lấy sequence trong transaction RIÊNG đã commit (gate :351-371, Seq.cs:172-180).
        // Nhờ transaction riêng commit độc lập, sequence vẫn tăng kể cả khi bước lưu sau bị lỗi.
        var seq = await PortSeq.NextAsync<Seq_PolicyExpenseTypeNo>(_db);
        var policyExpenseTypeNo = PortSeq.Build("{0}POLICYEXPENSETYPENO.{3}.{1:00000}{2}", "", seq, 100000, "", DateTime.UtcNow);

        // 1. Kiểm tra Lst_Mst_PolicyExpenseType:
        // Gate :373 gọi objRQ_Mst_PolicyExpenseTypeInput.Lst_Mst_PolicyExpenseType.Count() ném NullReferenceException SAU khi đã lấy seq
        // -> mã mặc định Mst_PolicyExpenseType_Save.
        if (req.Lst_Mst_PolicyExpenseType == null)
        {
            return PortResult.Fail("Mst_PolicyExpenseType_Save", "Check.Lst_Mst_PolicyExpenseType", null);
        }

        var inputRows = req.Lst_Mst_PolicyExpenseType;

        // [] xoá hết: Nếu mảng rỗng -> xoá sạch toàn bộ Mst_PolicyExpenseType, Data = null, Success = true (hành vi nguồn :8391-8401).
        // CẢNH BÁO: hành vi nguồn xoá sạch toàn bộ bảng chính sách khi danh sách rỗng.
        if (inputRows.Count == 0)
        {
            await using var txEmpty = await _db.Database.BeginTransactionAsync();
            try
            {
                // xoá toàn bảng Mst_PolicyExpenseType (Master.Loyalty.cs:8391-8401)
                await _db.Database.ExecuteSqlRawAsync("DELETE FROM \"Mst_PolicyExpenseType\"");
                await txEmpty.CommitAsync();
                return Ok(new { Success = true, Data = (object?)null });
            }
            catch (Exception ex)
            {
                await txEmpty.RollbackAsync();
                return PortResult.Fail("Mst_PolicyExpenseType_Save", "Exception", ex.Message);
            }
        }

        // 2. Duyệt từng dòng theo thứ tự gửi lên để kiểm tra hợp lệ (Master.Loyalty.cs:8294-8356)
        var networkId = _config["Biz_NetworkID"] ?? "1001";
        var now = DateTime.UtcNow;
        var newPolicies = new List<Mst_PolicyExpenseType>();
        var newHists = new List<Mst_PolicyExpenseTypeHist>();

        for (int nScan = 0; nScan < inputRows.Count; nScan++)
        {
            var drScan = inputRows[nScan];

            // Chuẩn hoá đầu vào (Master.Loyalty.cs:8273-8287)
            // PolicyExpenseTypeNo, ExpenseType: StdParam (Trim + ToUpper)
            var expenseType = string.IsNullOrWhiteSpace(drScan.ExpenseType) ? "" : drScan.ExpenseType.Trim().ToUpperInvariant();
            var expenseTypeNameActual = drScan.ExpenseTypeNameActual ?? "";

            // FlagPoint, FlagPointRank, FlagCountService, FlagDiscount: StdFlag ("1" nếu "1", ngược lại "0")
            var flagPoint = string.Equals(drScan.FlagPoint?.Trim(), "1", StringComparison.OrdinalIgnoreCase) ? "1" : "0";
            var flagPointRank = string.Equals(drScan.FlagPointRank?.Trim(), "1", StringComparison.OrdinalIgnoreCase) ? "1" : "0";
            var flagCountService = string.Equals(drScan.FlagCountService?.Trim(), "1", StringComparison.OrdinalIgnoreCase) ? "1" : "0";
            var flagDiscount = string.Equals(drScan.FlagDiscount?.Trim(), "1", StringComparison.OrdinalIgnoreCase) ? "1" : "0";

            var amountRate = drScan.AmountRate;
            var maxRankReviewPoint = drScan.MaxRankReviewPoint;
            var maxAccumulationPoint = drScan.MaxAccumulationPoint;
            var discountRate = drScan.DiscountRate;
            var remark = string.IsNullOrWhiteSpace(drScan.Remark) ? null : drScan.Remark.Trim();

            // 2.a: Mst_ExpenseType_CheckDB(ExpenseType, "1", "1") (Master.Loyalty.cs:8301-8307, :6705-6769)
            // So sánh chuỗi trong DB qua PortQuery.Equal và PortQuery.Esc (bẫy #18/#31, PORT_RULES §3)
            var matchEt = PortQuery.Equal(expenseType);
            var dbExpenseType = await _db.Set<Mst_ExpenseType>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => EF.Functions.Like(x.ExpenseType.ToLower(), matchEt, PortQuery.Esc));

            if (dbExpenseType == null)
            {
                // Không tìm thấy ExpenseType trong Mst_ExpenseType (:6737)
                return PortResult.Fail("Mst_ExpenseType_CheckDB_ExpenseTypeNotFound", "Check.ExpenseType", drScan.ExpenseType);
            }

            if (!string.Equals(dbExpenseType.FlagActive, "1", StringComparison.OrdinalIgnoreCase))
            {
                // Tìm thấy nhưng FlagActive != "1" (:6764)
                return PortResult.Fail(
                    "Mst_ExpenseType_CheckDB_FlagActiveNotMatched",
                    "Check.ExpenseType", drScan.ExpenseType,
                    "Check.strFlagActiveListToCheck", "1",
                    "DB.FlagActive", dbExpenseType.FlagActive);
            }

            // 2.b: validate DiscountRate: 0 <= DiscountRate <= 100 (Master.Loyalty.cs:8309-8328)
            var dDiscountRate = discountRate ?? 0.0;
            if (dDiscountRate < 0 || dDiscountRate > 100)
            {
                return PortResult.Fail(
                    "Mst_PolicyExpenseType_Save_InvalidValue",
                    "Check.ExpenseType", drScan.ExpenseType,
                    "Check.DiscountRate", drScan.DiscountRate,
                    "Check.ErrCoditional", "DiscountRate < 0 or DiscountRate > 100");
            }

            // 2.c: validate DiscountRate: kiểm tra chéo FlagDiscount != "1" mà DiscountRate != 0 (Master.Loyalty.cs:8330-8346)
            if (!string.Equals(flagDiscount, "1", StringComparison.OrdinalIgnoreCase) && dDiscountRate != 0)
            {
                return PortResult.Fail(
                    "Mst_PolicyExpenseType_Save_InvalidValue",
                    "Check.ExpenseType", drScan.ExpenseType,
                    "Check.FlagDiscount", drScan.FlagDiscount,
                    "Check.DiscountRate", drScan.DiscountRate,
                    "Check.ErrCoditional", "FlagDiscount not '1' but DiscountRate <> 0");
            }

            // Thêm vào danh sách entity Mst_PolicyExpenseType (16 cột DB)
            newPolicies.Add(new Mst_PolicyExpenseType
            {
                PolicyExpenseTypeNo = policyExpenseTypeNo,
                ExpenseType = expenseType,
                NetworkID = networkId,
                ExpenseTypeNameActual = expenseTypeNameActual,
                FlagPoint = flagPoint,
                FlagPointRank = flagPointRank,
                AmountRate = amountRate,
                MaxRankReviewPoint = maxRankReviewPoint,
                MaxAccumulationPoint = maxAccumulationPoint,
                FlagCountService = flagCountService,
                FlagDiscount = flagDiscount,
                DiscountRate = discountRate,
                FlagActive = "1",
                Remark = remark,
                LogLUDTimeUTC = now,
                LogLUBy = "port"
            });

            // Hist lưu lịch sử: Mst_PolicyExpenseTypeHist (14 cột DB + AutoId tự tăng) — Master.Loyalty.cs:8129-8145
            newHists.Add(new Mst_PolicyExpenseTypeHist
            {
                PolicyExpenseTypeNo = policyExpenseTypeNo,
                ExpenseType = expenseType,
                NetworkID = networkId,
                ExpenseTypeNameActual = expenseTypeNameActual,
                FlagPoint = flagPoint,
                AmountRate = amountRate,
                MaxRankReviewPoint = maxRankReviewPoint,
                MaxAccumulationPoint = maxAccumulationPoint,
                FlagDiscount = flagDiscount,
                DiscountRate = discountRate,
                FlagActive = "1",
                Remark = remark,
                LogLUDTimeUTC = now,
                LogLUBy = "port"
            });
        }

        // 3. Xoá toàn bộ Mst_PolicyExpenseType (where 1=1) rồi ghi lại và ghi lịch sử (Master.Loyalty.cs:8391-8465, :8469)
        // 1 transaction duy nhất; trùng ExpenseType trong input -> vi phạm PK composite -> rollback -> lỗi mặc định Mst_PolicyExpenseType_Save
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // xoá toàn bảng Mst_PolicyExpenseType (Master.Loyalty.cs:8391-8401)
            await _db.Database.ExecuteSqlRawAsync("DELETE FROM \"Mst_PolicyExpenseType\"");

            // Thêm các dòng Mst_PolicyExpenseType
            _db.Set<Mst_PolicyExpenseType>().AddRange(newPolicies);

            // Hist lưu lịch sử: Mst_PolicyExpenseType_Perform: copy các dòng input vào Mst_PolicyExpenseTypeHist (Master.Loyalty.cs:8103-8195)
            _db.Set<Mst_PolicyExpenseTypeHist>().AddRange(newHists);

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return Ok(new { Success = true, Data = (object?)null });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return PortResult.Fail("Mst_PolicyExpenseType_Save", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Port 1:1 hàm nghiệp vụ <c>Mst_PolicyExpenseType_GetX</c> (Master.Loyalty.cs:8757-8929).
    /// Thực hiện ĐÚNG 2 bước:
    /// - Bước 1: Draft từ Mst_PolicyExpenseType (lọc PolicyExpenseTypeNo '=' không phân biệt hoa/thường nếu có, lọc FlagActive IN tách dấu phẩy nếu có, order by LogLUDTimeUTC desc, cắt trang).
    /// - Bước 2: Select cuối từ draft page inner join Mst_PolicyExpenseType, left join Mst_ExpenseType lấy ExpenseTypeName, order by LogLUDTimeUTC desc.
    /// </summary>
    private async Task<(int ItemCount, List<object> Rows)> GetXAsync(string? flagActiveCsv, long recordStart, long recordCount, string? policyExpenseTypeNo = null)
    {
        // -----------------------------------------------------------------------------------------
        // BƯỚC 1: DRAFT (#tbl_Mst_PolicyExpenseType_Filter_Draft, Master.Loyalty.cs:8814-8824)
        // -----------------------------------------------------------------------------------------
        var qPolicy = _db.Set<Mst_PolicyExpenseType>().AsNoTracking();

        // Lọc PolicyExpenseTypeNo: toán tử '=' không phân biệt hoa/thường (gate Mst_PolicyExpenseTypeController.cs:306).
        // Dùng PortQuery.Equal (không wildcard, escape chuẩn) và PortQuery.Esc (bẫy #18/#31, PORT_RULES §3).
        if (!string.IsNullOrWhiteSpace(policyExpenseTypeNo))
        {
            var petNo = PortQuery.Equal(policyExpenseTypeNo);
            qPolicy = qPolicy.Where(x => EF.Functions.Like(x.PolicyExpenseTypeNo.ToLower(), petNo, PortQuery.Esc));
        }

        // Lọc FlagActive: danh sách phân cách dấu phẩy -> IN ('a','b') (gate Mst_PolicyExpenseTypeController.cs:288-295).
        // So sánh không phân biệt hoa/thường (bẫy #18/#31, PORT_RULES §3).
        if (!string.IsNullOrWhiteSpace(flagActiveCsv))
        {
            var ds = flagActiveCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => s.ToLowerInvariant())
                .ToList();
            if (ds.Count > 0)
            {
                qPolicy = qPolicy.Where(x => ds.Contains((x.FlagActive ?? "").ToLower()));
            }
        }

        // KeyWord KHÔNG lọc (không có nhánh nào lọc KeyWord trong strWhereClause_Mst_PolicyExpenseType :281-298).
        // OrgID và NetworkID KHÔNG lọc trong where clause (:281-298).

        // Sắp xếp mpet.LogLUDTimeUTC desc (Master.Loyalty.cs:8823)
        var draftList = await qPolicy
            .OrderByDescending(x => x.LogLUDTimeUTC)
            .ToListAsync();

        // MyCount = Count(0) của bảng draft (#tbl_Mst_PolicyExpenseType_Filter_Draft) — Master.Loyalty.cs:8827
        var itemCount = draftList.Count;

        // Cắt trang draft (#tbl_Mst_PolicyExpenseType_Filter, Master.Loyalty.cs:8831-8838):
        // t.MyIdxSeq >= @nFilterRecordStart and t.MyIdxSeq <= @nFilterRecordEnd
        // với nFilterRecordStart = recordStart, nFilterRecordEnd = recordStart + recordCount - 1 (:8793-8794).
        // Khi recordCount <= 0 hoặc recordStart < 0 thì nFilterRecordEnd < nFilterRecordStart -> trang draft rỗng.
        var draftPage = (recordCount <= 0 || recordStart < 0 || recordStart >= draftList.Count)
            ? new List<(long MyIdxSeq, Mst_PolicyExpenseType Policy)>()
            : draftList
                .Select((p, idx) => (MyIdxSeq: (long)idx, Policy: p))
                .Where(x => x.MyIdxSeq >= recordStart && x.MyIdxSeq < recordStart + recordCount)
                .ToList();

        // -----------------------------------------------------------------------------------------
        // BƯỚC 2: SELECT CUỐI (Master.Loyalty.cs:8856-8867)
        // select t.MyIdxSeq, mpet.*, met.ExpenseTypeName
        // from #tbl_Mst_PolicyExpenseType_Filter t
        //   inner join Mst_PolicyExpenseType mpet on t.PolicyExpenseTypeNo = mpet.PolicyExpenseTypeNo and t.ExpenseType = mpet.ExpenseType
        //   left join Mst_ExpenseType met on t.ExpenseType = met.ExpenseType
        // order by mpet.LogLUDTimeUTC desc
        // -----------------------------------------------------------------------------------------
        if (draftPage.Count == 0)
        {
            return (itemCount, new List<object>());
        }

        // Lấy thông tin Mst_ExpenseType để thực hiện left join lấy ExpenseTypeName
        var distinctExpenseTypes = draftPage.Select(x => x.Policy.ExpenseType).Distinct().ToList();
        var expenseTypes = await _db.Set<Mst_ExpenseType>()
            .AsNoTracking()
            .Where(x => distinctExpenseTypes.Contains(x.ExpenseType))
            .ToListAsync();

        var rows = draftPage
            .OrderByDescending(x => x.Policy.LogLUDTimeUTC)
            .ThenBy(x => x.MyIdxSeq)
            .Select(d =>
            {
                var met = expenseTypes.FirstOrDefault(e => string.Equals(e.ExpenseType, d.Policy.ExpenseType, StringComparison.OrdinalIgnoreCase));
                return (object)new
                {
                    d.MyIdxSeq,
                    d.Policy.PolicyExpenseTypeNo,
                    d.Policy.ExpenseType,
                    d.Policy.NetworkID,
                    d.Policy.ExpenseTypeNameActual,
                    ExpenseTypeName = met?.ExpenseTypeName,
                    d.Policy.FlagPoint,
                    d.Policy.FlagPointRank,
                    d.Policy.AmountRate,
                    d.Policy.MaxRankReviewPoint,
                    d.Policy.MaxAccumulationPoint,
                    d.Policy.FlagCountService,
                    d.Policy.FlagDiscount,
                    d.Policy.DiscountRate,
                    d.Policy.FlagActive,
                    d.Policy.Remark,
                    d.Policy.LogLUDTimeUTC,
                    d.Policy.LogLUBy
                };
            })
            .ToList();

        return (itemCount, rows);
    }
}
