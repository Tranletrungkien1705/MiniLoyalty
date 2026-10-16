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
// PORT 1:1 — màn danh mục chính sách xếp hạng (Mst_RankPolicy) của dự án nguồn 2022.7.Loyalty (#227, #228, #229, #230).
//
// Nguồn controller (tầng gate):
//   Mst_RankPolicyController.cs:23-103   Search -> WA_Mst_RankPolicy_Get (#227)
//   Mst_RankPolicyController.cs:104-213  GetAllActive -> WA_Mst_RankPolicy_Get (#228)
//   Mst_RankPolicyController.cs:214-277  GetByRankPolicyCode -> WA_Mst_RankPolicy_Get (#229)
//   Mst_RankPolicyController.cs:319-411  Save -> WA_Mst_RankPolicy_Save (#230)
//   Mst_RankPolicyController.cs:281-298  strWhereClause_Mst_RankPolicy (mệnh đề lọc Search / GetAllActive)
//   Mst_RankPolicyController.cs:299-315  strWhereClause_Mst_RankPolicy_By_RankPolicyCode (mệnh đề lọc GetByRankPolicyCode)
// Nguồn API:
//   MstRankPolicyController.cs:95-163   WA_Mst_RankPolicy_Get
// Nguồn biz:
//   Master.Loyalty.cs:7442-7542          WAS_Mst_RankPolicy_Get
//   Master.Loyalty.cs:7543-7696          Mst_RankPolicy_Get
//   Master.Loyalty.cs:7697-7872          Mst_RankPolicy_GetX (SQL draft :7753-7764, SQL select cuối :7795-7818)
//
// ĐẶC ĐIỂM VÀ QUY TẮC NGHIỆP VỤ NGUỒN:
//   * Endpoint là POST (nguồn gate Mst_RankPolicyController.cs:23, :104, :214).
//   * Tham số lọc Search: KeyWord, FlagActive, Ft_PageIndex = 0, Ft_PageSize = 10 (gate :23).
//   * KeyWord KHÔNG lọc: hàm lọc strWhereClause_Mst_RankPolicy (:281-298) CHỈ xử lý FlagActive
//     (tách dấu phẩy -> IN), hoàn toàn không có nhánh nào lọc KeyWord.
//   * Phân trang ở gate: RecordStart = pageIndex * pageSize, RecordCount = pageSize (:66-67).
//   * Nghiệp vụ 2 bước trong Mst_RankPolicy_GetX:
//     1. Bước 1 DRAFT (#tbl_Mst_RankPolicy_Filter_Draft, :7753-7764):
//        select distinct identity(bigint, 0, 1) MyIdxSeq, mrp.RankPolicyCode, mrp.CardType, mrp.LogLUDTimeUTC
//        từ Mst_RankPolicy với điều kiện lọc, sắp xếp mrp.LogLUDTimeUTC desc.
//        MyCount = đếm tổng số dòng draft (:7767).
//        Cắt trang draft (#tbl_Mst_RankPolicy_Filter, :7771-7778): [recordStart, recordStart + recordCount - 1].
//     2. Bước 2 SELECT CUỐI (:7795-7818) — KHÁC MỌI MÀN TRƯỚC:
//        from Mst_CardType mct
//          left join #tbl_Mst_RankPolicy_Filter tmrpf on mct.CardType = tmrpf.CardType
//          left join Mst_RankPolicy mrp on tmrpf.RankPolicyCode = mrp.RankPolicyCode and mct.CardType = mrp.CardType
//        where mct.FlagActive = '1'
//        order by mct.Value asc.
//        Cột trả về ĐÚNG 12 cột:
//          MyIdxSeq (từ draft, null nếu hạng thẻ không có trong trang draft),
//          CardType (của mct),
//          Value (của Mst_CardType, KHÔNG PHẢI của chính sách Mst_RankPolicy),
//          PointUpBegin, PointUpEnd, QtyVisitUpBegin, QtyVisitUpEnd,
//          PointKeepBegin, PointKeepEnd, QtyVisitKeepBegin, QtyVisitKeepEnd, QtyMonth (từ mrp, null nếu không khớp).
//        => DataList LUÔN chứa MỌI hạng thẻ đang hoạt động (kể cả khi trang draft rỗng);
//           1 hạng thẻ có 2 chính sách trong trang draft -> sinh 2 dòng.
//        Hoà trùng Value: sắp phụ MyIdxSeq asc (null cuối), ghi chú: nguồn không xác định.
//   * GetByRankPolicyCode (gate :214-277):
//     - RankPolicyCode rỗng -> Data null, không truy vấn DB (:234, :264).
//     - Ngược lại lọc strWhereClause_Mst_RankPolicy_By_RankPolicyCode (:306) theo RankPolicyCode = 'x'.
//     - RecordStart = "0", RecordCount = "1" (:251-252).
//     - Data = Lst_Mst_RankPolicy[0] hoặc null (:258-264).
//     - CẢNH BÁO: Vì select cuối luôn chạy từ Mst_CardType (FlagActive = '1') order by mct.Value asc,
//       phần tử [0] là hạng thẻ active có Value nhỏ nhất (dòng 'R'), KHÔNG PHẢI chính sách vừa tìm.
//       Kể cả mã không tồn tại, [0] vẫn trả về dòng 'R' (chỉ null khi DB không có hạng thẻ active nào).
//   * Tính PageCount tại gate (:78-86):
//     Chỉ tính khi DataList KHÔNG rỗng (Lst_Mst_RankPolicy.Count > 0).
//     Nếu PageSize == 0 thì pageSize gán về 10 (PageSizeConfig) rồi tính (itemCount % ps == 0) ? itemCount / ps : itemCount / ps + 1.
//   * Bỏ qua có chủ đích (loại Đ):
//     - drAbilityOfUser (Master.Loyalty.cs:7738) nguồn lấy nhưng bị comment không dùng (:7750) -> không port, ghi comment.
//     - FK CardType -> Mst_CardType (20221026.CreateDatabase.sql:273) — chỉ ghi comment.
//     - CheckGatewayAuthentication, CheckAuthorize (gate :32-33, :124-125, :223-224), Sys_User_CheckAuthorize (:7607-7620):
//       hệ thống đích chưa có hệ phân quyền.
//     - Ghi RequestLog/ReturnLog (:7586-7593, :7686-7693).
// ============================================================================================

/// <summary>Port 1:1 <c>Mst_RankPolicySearch</c> — tham số lọc + phân trang (Mst_RankPolicyController.cs:23, 50-54).</summary>
public class MstRankPolicySearchReq
{
    public string? KeyWord { get; set; }             // Từ khoá (gate nhận nhưng strWhereClause_Mst_RankPolicy :281-298 KHÔNG lọc KeyWord)
    public string? FlagActive { get; set; }          // Cờ hoạt động, danh sách phân cách dấu phẩy (lọc IN) — :288-295
    public int Ft_PageIndex { get; set; } = 0;       // Trang (0-based) — :23, :44
    public int Ft_PageSize { get; set; } = 10;       // Số dòng/trang — :23, :45
}

/// <summary>Port 1:1 <c>GetByRankPolicyCode</c> — body chỉ có RankPolicyCode (Mst_RankPolicyController.cs:214-277).</summary>
public class MstRankPolicyGetByRankPolicyCodeReq
{
    public string? RankPolicyCode { get; set; }       // Mã chính sách xếp hạng — :214, :234-238
}

/// <summary>
/// Dòng cấu hình chính sách cho 1 hạng thẻ trong payload Save (Master.Loyalty.cs:7166-7180).
/// </summary>
public class MstRankPolicySaveRowReq
{
    public string? CardType { get; set; }
    public double? Value { get; set; }
    public double? PointUpBegin { get; set; }
    public double? PointUpEnd { get; set; }
    public double? QtyVisitUpBegin { get; set; }
    public double? QtyVisitUpEnd { get; set; }
    public double? PointKeepBegin { get; set; }
    public double? PointKeepEnd { get; set; }
    public double? QtyVisitKeepBegin { get; set; }
    public double? QtyVisitKeepEnd { get; set; }
    public double? QtyMonth { get; set; }
    public string? Remark { get; set; }
}

/// <summary>
/// Body của Save: { Lst_Mst_RankPolicy: [ ... ] } (gate Mst_RankPolicyController.cs:328-336).
/// </summary>
public class MstRankPolicySaveReq
{
    public List<MstRankPolicySaveRowReq>? Lst_Mst_RankPolicy { get; set; }
}

[ApiController]
[Route("api/mst_rankpolicy")]
[Produces("application/json")]
public class Mst_RankPolicyControllerPort : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public Mst_RankPolicyControllerPort(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    /// <summary>
    /// Port 1:1 <c>Mst_RankPolicyController.Search</c> (nguồn gate Mst_RankPolicyController.cs:23-103).
    /// </summary>
    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] MstRankPolicySearchReq? req)
    {
        var r = req ?? new MstRankPolicySearchReq();
        var pageIndex = r.Ft_PageIndex;
        var pageSize = r.Ft_PageSize;

        // KeyWord KHÔNG lọc (gate Mst_RankPolicyController.cs:281-298 không có nhánh nào lọc KeyWord).
        // RecordStart = PageIndex * PageSize, RecordCount = PageSize (gate :66-67).
        var start = (long)pageIndex * pageSize;
        var (itemCount, rows) = await GetXAsync(r.FlagActive, start, pageSize);

        // PageCount chỉ tính khi DataList KHÔNG rỗng (gate :78-86).
        // Nếu PageSize == 0 thì pageSize gán về 10 (PageSizeConfig = 10) để tính PageCount (:81-85).
        var ps = pageSize == 0 ? 10 : pageSize;
        var pageCount = rows.Count == 0 ? 0 : (itemCount % ps == 0 ? itemCount / ps : itemCount / ps + 1);

        return Ok(new
        {
            Success = true,
            Data = new PageInfo<object>(rows, itemCount, pageCount, pageIndex, pageSize)
        });
    }

    /// <summary>
    /// Port 1:1 <c>Mst_RankPolicyController.GetAllActive</c> (nguồn gate Mst_RankPolicyController.cs:104-213).
    /// Nguồn gọi <c>strWhereClause_Mst_RankPolicy</c> với <c>FlagActive = "1"</c> (:130-134) ⇒ lọc
    /// <c>FlagActive IN ('1')</c> (:288-295). <c>OrgID</c> chỉ được gán vào request (:132, :144),
    /// hàm lọc KHÔNG dùng OrgID ⇒ KHÔNG lọc theo OrgID.
    /// Không body: RecordStart = "0", RecordCount = "123456000" (nguồn Ft_RecordStart/Ft_RecordCount :146-147).
    /// Khối Ext/tree (:156-188) bị comment ở nguồn ⇒ KHÔNG port.
    /// Dùng lại GetXAsync của Search (#227): select cuối từ Mst_CardType đang hoạt động left join chính sách.
    /// Trả <c>Ok(new { Success = true, Data = ... })</c> — Data là MẢNG TRẦN, rỗng thì <c>null</c>
    /// (nguồn :127 khởi tạo null, :153-155 chỉ gán khi có dòng, :193-200 trả thẳng listMst_RankPolicy).
    /// </summary>
    [HttpPost("getallactive")]
    public async Task<IActionResult> GetAllActive()
    {
        // Nguồn: Ft_RecordStart = "0", Ft_RecordCount = "123456000" (gate Mst_RankPolicyController.cs:146-147).
        // FlagActive = "1" (gate :133) ⇒ lọc IN ('1'); OrgID không lọc.
        var (_, rows) = await GetXAsync("1", 0, 123456000);

        // Nguồn :193-200: Data = listMst_RankPolicy (mảng trần); rỗng ⇒ null (:127, :153-155).
        return Ok(new { Success = true, Data = rows.Count > 0 ? rows : null });
    }

    /// <summary>
    /// Port 1:1 <c>Mst_RankPolicyController.GetByRankPolicyCode</c> (nguồn gate Mst_RankPolicyController.cs:214-277).
    /// Gate: nếu RankPolicyCode rỗng (!IsNullOrEmpty :234) thì KHÔNG truy vấn, objMst_RankPolicy giữ null
    ///       ⇒ Data = null (:256, :264).
    /// Ngược lại gọi strWhereClause_Mst_RankPolicy_By_RankPolicyCode (:299-315) lọc RankPolicyCode = 'x'
    /// (:306). Nhánh FlagActive (:309-312) không truyền nên không lọc cờ hoạt động.
    /// Ft_RecordStart = "0", Ft_RecordCount = "1" (:251-252).
    /// Kết quả: objRT_Mst_RankPolicy.Lst_Mst_RankPolicy[0] nếu Count > 0, ngược lại null (:258-261).
    ///
    /// CẢNH BÁO HÀNH VI NGUỒN:
    /// Mst_RankPolicy_GetX có Bước 2 select cuối chạy từ Mst_CardType (FlagActive = '1') LEFT JOIN draft
    /// ORDER BY mct.Value asc. Phần tử [0] nguồn lấy Lst_Mst_RankPolicy[0] là hạng thẻ active có Value
    /// nhỏ nhất (thường là 'R' với Value = 0), KHÔNG PHẢI chính sách vừa tìm. Kể cả mã RankPolicyCode
    /// không tồn tại trong DB, bước 2 vẫn sinh ra danh sách gồm các hạng thẻ active (MyIdxSeq null),
    /// nên [0] vẫn trả về dòng 'R' (chỉ null khi DB không có hạng thẻ active nào). Giữ nguyên 1:1 hành vi nguồn.
    /// </summary>
    [HttpPost("getbyrankpolicycode")]
    public async Task<IActionResult> GetByRankPolicyCode([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] MstRankPolicyGetByRankPolicyCodeReq? req)
    {
        var rankPolicyCode = req?.RankPolicyCode;

        // RankPolicyCode rỗng ⇒ nguồn không truy vấn DB (:234) và trả Data = null (:264).
        if (string.IsNullOrWhiteSpace(rankPolicyCode))
        {
            return Ok(new { Success = true, Data = (object?)null });
        }

        // Lấy 1 dòng draft theo RankPolicyCode (Ft_RecordStart = "0", Ft_RecordCount = "1" — :251-252).
        var (_, rows) = await GetXAsync(null, 0, 1, rankPolicyCodeEqual: rankPolicyCode);

        // Nguồn: Lst_Mst_RankPolicy[0] khi có dòng, ngược lại null (:258-261).
        return Ok(new { Success = true, Data = rows.Count > 0 ? rows[0] : null });
    }

    // ============================================================================================
    // Save — nguồn Mst_RankPolicyController.cs:319-411 (gate) -> MstRankPolicyController.cs:22-92 (API)
    //   -> Master.Loyalty.cs:6770-6855 (WAS_Mst_RankPolicy_Save) -> :6856-6995 (Mst_RankPolicy_Save)
    //   -> :7096-7441 (Mst_RankPolicy_SaveX) -> :6997-7094 (Mst_RankPolicy_Perform).
    //
    // GIỮ ĐÚNG NGUỒN:
    //   * Body JSON: { Lst_Mst_RankPolicy: [ { CardType, Value, PointUpBegin, PointUpEnd, ... } ] }
    //   * seq tăng cả khi lỗi: Lấy mã sequence trước khi lưu (gate :351-371, SeqType.RankPolicyCode)
    //     qua bảng Seq_RankPolicyCode trong transaction RIÊNG đã commit độc lập, rồi gán CÙNG 1 mã
    //     cho mọi dòng (:373-376). Do đó sequence tăng MỖI LẦN GỌI vượt qua bước body rỗng, KỂ CẢ KHI
    //     LƯU LỖI SAU ĐÓ.
    //     Body rỗng hoàn toàn -> ServiceException("strJson null") -> mã mặc định Mst_RankPolicy_Save (KHÔNG tăng seq).
    //   * xoá toàn bảng: Mst_RankPolicy_SaveX (:7284-7293) thực hiện `delete t from Mst_RankPolicy t where (1=1)`
    //     xoá toàn bộ chính sách trước khi insert các dòng input mới.
    //   * [] xoá hết: Nếu client gửi Lst_Mst_RankPolicy: [], không có lỗi xảy ra, bảng Mst_RankPolicy
    //     bị XOÁ HẾT thành 0 dòng (hành vi nguồn).
    //   * End tính từ dòng kế: Duyệt từng dòng theo thứ tự gửi lên (:7188-7248):
    //     - Dòng KHÔNG phải cuối: PointUpEnd = PointUpBegin(kế) - 1, QtyVisitUpEnd = QtyVisitUpBegin(kế) - 1,
    //       PointKeepEnd = PointKeepBegin(kế) - 1, QtyVisitKeepEnd = QtyVisitKeepBegin(kế) - 1 (:7219-7230).
    //       Nếu dòng kế tiếp có trường nào null thì Convert.ToDouble ném ngoại lệ -> mã mặc định Mst_RankPolicy_Save.
    //     - Dòng CUỐI: cả 4 cột End = 9999999999 (:7233-7236). Mọi giá trị End do client gửi lên BỊ BỎ QUA.
    //   * Hist: Mst_RankPolicy_Perform (:6997-7094) copy toàn bộ các dòng input vào bảng Mst_RankPolicyHist
    //     (17 cột + AutoId tự tăng).
    //   * Kiểm tra hợp lệ:
    //     1) Mst_CardType_CheckDB(CardType, "1", "1") -> không tồn tại: Mst_CardType_CheckDB_CardTypeNotFound (:7194-7200);
    //        FlagActive != '1': Mst_CardType_CheckDB_FlagActiveNotMatched.
    //     2) PointUpBegin < PointKeepBegin -> Mst_RankPolicy_Save_InvalidValue (:7201-7215).
    //        Nếu PointUpBegin hoặc PointKeepBegin là null -> Convert.ToDouble ném lỗi -> Mst_RankPolicy_Save.
    //     3) Trùng CardType trong input -> vi phạm PK (CardType, RankPolicyCode) -> Mst_RankPolicy_Save.
    //   * Lưu: 1 transaction duy nhất, lỗi -> rollback + PortResult.Fail. Thành công -> Data = null (:397-398).
    //   * NetworkID lấy từ cấu hình: IConfiguration["Biz_NetworkID"] ?? "1001" (GAP nguồn lấy từ session user).
    //   * LogLUBy = "port", LogLUDTimeUTC = DateTime.UtcNow.
    // ============================================================================================
    [HttpPost("save")]
    public async Task<IActionResult> Save([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] MstRankPolicySaveReq? req)
    {
        // 0. Body rỗng / null: gate :329-336 ném ServiceException("strJson null") -> mã mặc định Mst_RankPolicy_Save.
        // Bước này xảy ra TRƯỚC khi gọi sequence nên KHÔNG tăng seq.
        if (req == null)
        {
            return PortResult.Fail("Mst_RankPolicy_Save", "Check.strJson", null);
        }

        // seq tăng cả khi lỗi: Lấy sequence trong transaction RIÊNG đã commit (gate :351-371).
        // Nhờ transaction riêng, mã sequence đã tăng sẽ không bị rollback kể cả khi bước lưu sau bị lỗi.
        var seq = await PortSeq.NextAsync<Seq_RankPolicyCode>(_db);
        var rankPolicyCode = PortSeq.Build("{0}RANKPOLICYCODE.{3}.{1:00000}{2}", "", seq, 100000, "", DateTime.UtcNow);

        // 1. Kiểm tra bảng input Mst_RankPolicy có tồn tại không (Master.Loyalty.cs:7141)
        // Nếu body là {} (thiếu Lst_Mst_RankPolicy) -> Mst_RankPolicy_Save_Input_Mst_RankPolicyTblNotFound.
        // Gate :373 gọi Lst_Mst_RankPolicy.Count() khi Lst null ⇒ NullReferenceException SAU khi đã lấy seq ⇒ mã mặc định Mst_RankPolicy_Save
        // (Mst_RankPolicy_Save_Input_Mst_RankPolicyTblNotFound :7141 không bao giờ tới được vì ToDataTable luôn tạo bảng).
        if (req.Lst_Mst_RankPolicy == null)
        {
            return PortResult.Fail("Mst_RankPolicy_Save", "Check.Lst_Mst_RankPolicy", null);
        }

        var inputRows = req.Lst_Mst_RankPolicy;

        // [] xoá hết: Nếu mảng rỗng -> xoá sạch toàn bộ Mst_RankPolicy, không sinh dòng nào, không lỗi (hành vi nguồn).
        if (inputRows.Count == 0)
        {
            await using var txEmpty = await _db.Database.BeginTransactionAsync();
            try
            {
                // xoá toàn bảng Mst_RankPolicy (Master.Loyalty.cs:7284-7293)
                await _db.Database.ExecuteSqlRawAsync("DELETE FROM \"Mst_RankPolicy\"");
                await txEmpty.CommitAsync();
                return Ok(new { Success = true, Data = (object?)null });
            }
            catch (Exception ex)
            {
                await txEmpty.RollbackAsync();
                return PortResult.Fail("Mst_RankPolicy_Save", "Exception", ex.Message);
            }
        }

        // 3. Duyệt từng dòng theo thứ tự gửi lên để kiểm tra hợp lệ và tính toán các cột End (Master.Loyalty.cs:7188-7248)
        var networkId = _config["Biz_NetworkID"] ?? "1001";
        var now = DateTime.UtcNow;
        var newPolicies = new List<Mst_RankPolicy>();
        var newHists = new List<Mst_RankPolicyHist>();

        for (int nScan = 0; nScan < inputRows.Count; nScan++)
        {
            var drScan = inputRows[nScan];
            var strCardType = StdParam(drScan.CardType);

            // 3.a. Mst_CardType_CheckDB(CardType, "1", "1") — :7194-7200
            // Tra cứu Mst_CardType trong DB (so sánh Like + PortQuery.Equal không phân biệt hoa/thường)
            var cardTypeDb = await _db.Set<Mst_CardType>().AsNoTracking()
                .FirstOrDefaultAsync(x => EF.Functions.Like((x.CardType ?? "").ToLower(), PortQuery.Equal(strCardType ?? ""), PortQuery.Esc));

            if (cardTypeDb == null)
            {
                return PortResult.Fail("Mst_CardType_CheckDB_CardTypeNotFound", "Check.CardType", strCardType);
            }

            // Nguồn Mst_CardType_CheckDB: `!"1".Contains(Convert.ToString(FlagActive))` — NULL/rỗng ⇒ "" ⇒ Contains("") = true ⇒ VẪN qua.
            if (!"1".Contains(cardTypeDb.FlagActive ?? ""))
            {
                return PortResult.Fail("Mst_CardType_CheckDB_FlagActiveNotMatched",
                    "Check.CardType", strCardType,
                    "Check.strFlagActiveListToCheck", "1",
                    "DB.FlagActive", cardTypeDb.FlagActive);
            }

            // 3.b. Convert.ToDouble(PointUpBegin) < Convert.ToDouble(PointKeepBegin) — :7201-7215
            // Nếu PointUpBegin hoặc PointKeepBegin là null -> Convert.ToDouble ném lỗi -> mã mặc định Mst_RankPolicy_Save
            if (!drScan.PointUpBegin.HasValue || !drScan.PointKeepBegin.HasValue)
            {
                return PortResult.Fail("Mst_RankPolicy_Save",
                    "Check.DB.PointUpBegin", drScan.PointUpBegin,
                    "Check.DB.PointKeepBegin", drScan.PointKeepBegin);
            }

            if (drScan.PointUpBegin.Value < drScan.PointKeepBegin.Value)
            {
                return PortResult.Fail("Mst_RankPolicy_Save_InvalidValue",
                    "Check.DB.RankPolicyCode", rankPolicyCode,
                    "Check.DB.CardType", strCardType,
                    "Check.DB.PointUpBegin", drScan.PointUpBegin.Value,
                    "Check.DB.PointKeepBegin", drScan.PointKeepBegin.Value,
                    "Check.ErrCoditional", "PointUpBegin < PointKeepBegin");
            }

            // 3.c. End tính từ dòng kế: tính các cột End (:7219-7238). Giá trị End của client gửi lên BỊ BỎ QUA.
            double pointUpEnd;
            double qtyVisitUpEnd;
            double pointKeepEnd;
            double qtyVisitKeepEnd;

            if (nScan != inputRows.Count - 1)
            {
                var nextRow = inputRows[nScan + 1];
                // Dòng kế tiếp có bất kỳ giá trị nào null -> Convert.ToDouble ném lỗi -> mã mặc định Mst_RankPolicy_Save
                if (!nextRow.PointUpBegin.HasValue || !nextRow.QtyVisitUpBegin.HasValue
                    || !nextRow.PointKeepBegin.HasValue || !nextRow.QtyVisitKeepBegin.HasValue)
                {
                    return PortResult.Fail("Mst_RankPolicy_Save", "Check.NextRowValues", null);
                }

                pointUpEnd = nextRow.PointUpBegin.Value - 1;
                qtyVisitUpEnd = nextRow.QtyVisitUpBegin.Value - 1;
                pointKeepEnd = nextRow.PointKeepBegin.Value - 1;
                qtyVisitKeepEnd = nextRow.QtyVisitKeepBegin.Value - 1;
            }
            else
            {
                // Dòng cuối: cả 4 cột End = 9999999999 (:7233-7236)
                pointUpEnd = 9999999999;
                qtyVisitUpEnd = 9999999999;
                pointKeepEnd = 9999999999;
                qtyVisitKeepEnd = 9999999999;
            }

            var remark = StdStr(drScan.Remark);

            // Tạo entity Mst_RankPolicy (17 cột đúng DDL)
            newPolicies.Add(new Mst_RankPolicy
            {
                CardType = strCardType!,
                RankPolicyCode = rankPolicyCode,
                NetworkID = networkId,
                Value = drScan.Value,
                PointUpBegin = drScan.PointUpBegin,
                PointUpEnd = pointUpEnd,
                QtyVisitUpBegin = drScan.QtyVisitUpBegin,
                QtyVisitUpEnd = qtyVisitUpEnd,
                PointKeepBegin = drScan.PointKeepBegin,
                PointKeepEnd = pointKeepEnd,
                QtyVisitKeepBegin = drScan.QtyVisitKeepBegin,
                QtyVisitKeepEnd = qtyVisitKeepEnd,
                QtyMonth = drScan.QtyMonth,
                FlagActive = "1",
                Remark = remark,
                LogLUDTimeUTC = now,
                LogLUBy = "port"
            });

            // Hist: Tạo entity Mst_RankPolicyHist (17 cột + AutoId tự tăng) — Mst_RankPolicy_Perform (:6997-7094)
            newHists.Add(new Mst_RankPolicyHist
            {
                CardType = strCardType!,
                RankPolicyCode = rankPolicyCode,
                NetworkID = networkId,
                Value = drScan.Value,
                PointUpBegin = drScan.PointUpBegin,
                PointUpEnd = pointUpEnd,
                QtyVisitUpBegin = drScan.QtyVisitUpBegin,
                QtyVisitUpEnd = qtyVisitUpEnd,
                PointKeepBegin = drScan.PointKeepBegin,
                PointKeepEnd = pointKeepEnd,
                QtyVisitKeepBegin = drScan.QtyVisitKeepBegin,
                QtyVisitKeepEnd = qtyVisitKeepEnd,
                QtyMonth = drScan.QtyMonth,
                FlagActive = "1",
                Remark = remark,
                LogLUDTimeUTC = now,
                LogLUBy = "port"
            });
        }

        // Trùng CardType trong input ⇒ nguồn chỉ lỗi khi INSERT (vi phạm PK (CardType, RankPolicyCode) :7300-7358), tức SAU khi mọi dòng đã qua kiểm tra
        // ⇒ kiểm tra ở đây (sau vòng lặp) để giữ đúng thứ tự lỗi ; mã mặc định Mst_RankPolicy_Save.
        var seenCardTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var np in newPolicies)
        {
            if (!seenCardTypes.Add(np.CardType))
            {
                return PortResult.Fail("Mst_RankPolicy_Save", "Check.CardTypeDuplicate", np.CardType);
            }
        }

        // 4. Giao dịch lưu: xoá toàn bảng Mst_RankPolicy rồi insert các dòng mới và copy vào Mst_RankPolicyHist
        // 1 transaction duy nhất (Master.Loyalty.cs:6888 _cf.db.BeginTransaction(), :6958 CommitSafety)
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // xoá toàn bảng Mst_RankPolicy (:7284-7293, delete from Mst_RankPolicy where (1=1))
            await _db.Database.ExecuteSqlRawAsync("DELETE FROM \"Mst_RankPolicy\"");

            await _db.Set<Mst_RankPolicy>().AddRangeAsync(newPolicies);
            await _db.Set<Mst_RankPolicyHist>().AddRangeAsync(newHists);

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            // Thành công: Data = null (gate Mst_RankPolicyController.cs:397-398)
            return Ok(new { Success = true, Data = (object?)null });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return PortResult.Fail("Mst_RankPolicy_Save", "Exception", ex.Message);
        }
    }

    private static string? StdParam(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim().ToUpperInvariant();
    private static string? StdStr(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim().Normalize();

    /// <summary>
    /// Thực hiện 2 bước truy vấn nghiệp vụ của Mst_RankPolicy_GetX (Master.Loyalty.cs:7697-7872).
    /// Bước 1: Draft từ Mst_RankPolicy (lọc FlagActive IN, order by LogLUDTimeUTC desc, cắt trang).
    /// Bước 2: Select cuối từ Mst_CardType (FlagActive = '1') LEFT JOIN draft page LEFT JOIN Mst_RankPolicy,
    ///         lấy Value của Mst_CardType, order by mct.Value asc (hoà Value: MyIdxSeq asc, null cuối).
    /// </summary>
    private async Task<(int itemCount, List<object> rows)> GetXAsync(string? flagActiveCsv, long recordStart, long recordCount, string? rankPolicyCodeEqual = null)
    {
        // drAbilityOfUser (Master.Loyalty.cs:7738) nguồn gọi nhưng bị comment không dùng (:7750) -> không port, ghi comment.

        // -----------------------------------------------------------------------------------------
        // BƯỚC 1: DRAFT (#tbl_Mst_RankPolicy_Filter_Draft) — Master.Loyalty.cs:7753-7764
        // select distinct identity(bigint, 0, 1) MyIdxSeq, mrp.RankPolicyCode, mrp.CardType, mrp.LogLUDTimeUTC
        // from Mst_RankPolicy mrp where (1=1) zzB_Where_strFilter_zzE order by mrp.LogLUDTimeUTC desc
        // -----------------------------------------------------------------------------------------
        var qPolicy = _db.Set<Mst_RankPolicy>().AsNoTracking().AsQueryable();

        // Lọc RankPolicyCode: `RankPolicyCode = 'x'` (gate Mst_RankPolicyController.cs:306 — strWhereClause_Mst_RankPolicy_By_RankPolicyCode).
        // Nguồn dùng '=' trên SQL Server collation không phân biệt hoa/thường ⇒ Like KHÔNG wildcard, hạ chữ 2 phía (PortQuery.Equal).
        if (!string.IsNullOrWhiteSpace(rankPolicyCodeEqual))
        {
            var rpc = PortQuery.Equal(rankPolicyCodeEqual);
            qPolicy = qPolicy.Where(x => EF.Functions.Like((x.RankPolicyCode ?? "").ToLower(), rpc, PortQuery.Esc));
        }

        // Lọc FlagActive: danh sách phân cách dấu phẩy -> IN ('a','b') (gate Mst_RankPolicyController.cs:288-295).
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

        // KeyWord KHÔNG lọc (không có nhánh nào lọc KeyWord trong strWhereClause_Mst_RankPolicy :281-298).

        // Sắp xếp mrp.LogLUDTimeUTC desc (Master.Loyalty.cs:7763)
        var draftList = await qPolicy
            .OrderByDescending(x => x.LogLUDTimeUTC)
            .ToListAsync();

        // MyCount = Count(0) của bảng draft (#tbl_Mst_RankPolicy_Filter_Draft) — :7767
        var itemCount = draftList.Count;

        // Cắt trang draft (#tbl_Mst_RankPolicy_Filter, :7771-7778):
        // t.MyIdxSeq >= @nFilterRecordStart and t.MyIdxSeq <= @nFilterRecordEnd
        // với nFilterRecordStart = recordStart, nFilterRecordEnd = recordStart + recordCount - 1 (:7733-7734).
        // Khi recordCount <= 0 thì nFilterRecordEnd < nFilterRecordStart -> trang draft rỗng.
        var draftPage = (recordCount <= 0 || recordStart >= draftList.Count)
            ? new List<(long MyIdxSeq, Mst_RankPolicy Policy)>()
            : draftList
                .Select((p, idx) => (MyIdxSeq: (long)idx, Policy: p))
                .Where(x => x.MyIdxSeq >= recordStart && x.MyIdxSeq < recordStart + recordCount)
                .ToList();

        // -----------------------------------------------------------------------------------------
        // BƯỚC 2: SELECT CUỐI (Master.Loyalty.cs:7795-7818)
        // from Mst_CardType mct
        //   left join #tbl_Mst_RankPolicy_Filter tmrpf on mct.CardType = tmrpf.CardType
        //   left join Mst_RankPolicy mrp on tmrpf.RankPolicyCode = mrp.RankPolicyCode and mct.CardType = mrp.CardType
        // where mct.FlagActive = '1'
        // order by mct.Value asc
        // -----------------------------------------------------------------------------------------
        // Lấy mọi hạng thẻ đang hoạt động (mct.FlagActive = '1', :7816)
        var activeCardTypes = await _db.Set<Mst_CardType>()
            .AsNoTracking()
            .Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc))
            .ToListAsync();

        var flatRows = new List<(long? MyIdxSeq, string CardType, double? Value, double? PointUpBegin, double? PointUpEnd, double? QtyVisitUpBegin, double? QtyVisitUpEnd, double? PointKeepBegin, double? PointKeepEnd, double? QtyVisitKeepBegin, double? QtyVisitKeepEnd, double? QtyMonth)>();

        foreach (var mct in activeCardTypes)
        {
            // Khớp mct.CardType = tmrpf.CardType (Master.Loyalty.cs:7811)
            var matchedDraft = draftPage
                .Where(d => string.Equals(d.Policy.CardType, mct.CardType, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matchedDraft.Count == 0)
            {
                // Left join: hạng thẻ không có trong trang draft -> MyIdxSeq và các cột chính sách là null
                flatRows.Add((
                    MyIdxSeq: (long?)null,
                    CardType: mct.CardType,
                    Value: mct.Value, // Giá trị Value của Mst_CardType, KHÔNG PHẢI của chính sách (:7799)
                    PointUpBegin: (double?)null,
                    PointUpEnd: (double?)null,
                    QtyVisitUpBegin: (double?)null,
                    QtyVisitUpEnd: (double?)null,
                    PointKeepBegin: (double?)null,
                    PointKeepEnd: (double?)null,
                    QtyVisitKeepBegin: (double?)null,
                    QtyVisitKeepEnd: (double?)null,
                    QtyMonth: (double?)null
                ));
            }
            else
            {
                // Có chính sách trong trang draft (1 hạng thẻ có thể có nhiều chính sách trong trang -> nhiều dòng)
                foreach (var d in matchedDraft)
                {
                    flatRows.Add((
                        MyIdxSeq: (long?)d.MyIdxSeq,
                        CardType: mct.CardType,
                        Value: mct.Value, // Giá trị Value của Mst_CardType, KHÔNG PHẢI của chính sách (:7799)
                        PointUpBegin: d.Policy.PointUpBegin,
                        PointUpEnd: d.Policy.PointUpEnd,
                        QtyVisitUpBegin: d.Policy.QtyVisitUpBegin,
                        QtyVisitUpEnd: d.Policy.QtyVisitUpEnd,
                        PointKeepBegin: d.Policy.PointKeepBegin,
                        PointKeepEnd: d.Policy.PointKeepEnd,
                        QtyVisitKeepBegin: d.Policy.QtyVisitKeepBegin,
                        QtyVisitKeepEnd: d.Policy.QtyVisitKeepEnd,
                        QtyMonth: d.Policy.QtyMonth
                    ));
                }
            }
        }

        // Sắp xếp: order by mct.Value asc (Master.Loyalty.cs:7817)
        // Hoà trùng Value: sắp phụ MyIdxSeq asc (null cuối), ghi chú: nguồn không xác định.
        var orderedRows = flatRows
            .OrderBy(x => x.Value)
            .ThenBy(x => x.MyIdxSeq.HasValue ? 0 : 1)
            .ThenBy(x => x.MyIdxSeq)
            .ThenBy(x => x.CardType)
            .Select(x => (object)new
            {
                x.MyIdxSeq,
                x.CardType,
                x.Value,
                x.PointUpBegin,
                x.PointUpEnd,
                x.QtyVisitUpBegin,
                x.QtyVisitUpEnd,
                x.PointKeepBegin,
                x.PointKeepEnd,
                x.QtyVisitKeepBegin,
                x.QtyVisitKeepEnd,
                x.QtyMonth
            })
            .ToList();

        return (itemCount, orderedRows);
    }
}
