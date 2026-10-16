using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Configuration;
using MiniLoyalty.Data;
using MiniLoyalty.Models.Port;

namespace MiniLoyalty.Controllers.Port
{

// ============================================================================================
// PORT 1:1 — port 1:1 Mst_PositionController.cs — Mst_Position (#197, #198, #199, #200, #201, #202, #203, #204).
//
// Nguồn controller (tầng gate):
//   - Mst_PositionController.cs:26-106   Search -> WA_Mst_Position_Get (#197)
//   - Mst_PositionController.cs:108-171  GetByPositionCode -> WA_Mst_Position_Get (#198)
//   - Mst_PositionController.cs:173-234  GetAllActive -> WA_Mst_Position_Get (#199)
//   - Mst_PositionController.cs:238-321  Create -> WA_Mst_Position_Create (#200)
//   - Mst_PositionController.cs:325-391  Update -> WA_Mst_Position_Update (#201)
//   - Mst_PositionController.cs:395-456  Delete -> WA_Mst_Position_Delete (#202)
//   - Mst_PositionController.cs:458-522  DeleteMultiple -> WA_Mst_Position_Delete (#203)
//   - Mst_PositionController.cs:527-605  Export -> WA_Mst_Position_Get (#204)
//   - Mst_PositionController.cs:624-652  strWhereClause_Mst_Position (mệnh đề lọc Search & GetAllActive)
//   - Mst_PositionController.cs:654-673  strWhereClause_Mst_Position_By_PositionCode (mệnh đề lọc GetByPositionCode)
// Nguồn API:
//   - MstPositionController.cs:17-88     WA_Mst_Position_Get -> _biz.WAS_Mst_Position_Get
//   - MstPositionController.cs:91-162    WA_Mst_Position_Create -> _biz.WAS_Mst_Position_Create
//   - MstPositionController.cs:165-236   WA_Mst_Position_Update -> _biz.WAS_Mst_Position_Update
//   - MstPositionController.cs:239-310   WA_Mst_Position_Delete -> _biz.WAS_Mst_Position_Delete
// Nguồn biz:
//   - MasterData.cs:24386-24460          WAS_Mst_Position_Delete -> Mst_Position_Delete
//   - MasterData.cs:24268-24384          Mst_Position_Delete -> Mst_Position_DeleteX
//   - MasterData.cs:24151-24266          Mst_Position_DeleteX (StdParam, Mst_Org_CheckDB, Mst_Position_CheckDB, Check Staff_MapDepartment, Delete)
//   - MasterData.cs:24069-24149          WAS_Mst_Position_Update -> Mst_Position_Update
//   - MasterData.cs:23933-24067          Mst_Position_Update -> Mst_Position_UpdateX
//   - MasterData.cs:23798-23931          Mst_Position_UpdateX (Mst_Position_CheckDB, Mst_Org_CheckDB, InvalidPositionName, Mst_Position_CheckPositionName, SaveDB)
//   - MasterData.cs:23719-23796          WAS_Mst_Position_Create -> Mst_Position_Create
//   - MasterData.cs:23592-23717          Mst_Position_Create -> Mst_Position_CreateX
//   - MasterData.cs:23452-23590          Mst_Position_CreateX (Kiểm tra OrgID, sinh SEQ PositionCode, CheckDB, Mst_Org_CheckDB, CheckPositionName, SaveDB)
//   - MasterData.cs:23357-23450          WAS_Mst_Position_Get -> Mst_Position_Get
//   - MasterData.cs:23226-23355          Mst_Position_Get -> Mst_Position_GetX
//   - MasterData.cs:23056-23224          Mst_Position_GetX:
//       1. Draft query (#tbl_Mst_Position_Filter_Draft, :23115-23131):
//          select distinct identity(bigint, 0, 1) MyIdxSeq, mp.OrgID, mp.PositionCode
//          into #tbl_Mst_Position_Filter_Draft from Mst_Position mp where (1=1) ...
//          order by mp.OrgID asc, mp.PositionCode asc;
//          select Count(0) MyCount from #tbl_Mst_Position_Filter_Draft t;
//       2. Cắt trang (#tbl_Mst_Position_Filter, :23133-23141):
//          where (t.MyIdxSeq >= @nFilterRecordStart) and (t.MyIdxSeq <= @nFilterRecordEnd)
//       3. Select cuối (:23157-23173):
//          select t.MyIdxSeq, mp.*, mnnt.NNTFullName mnnt_NNTFullName
//          from #tbl_Mst_Position_Filter t
//          inner join Mst_Position mp on t.OrgID = mp.OrgID and t.PositionCode = mp.PositionCode
//          left join Mst_NNT mnnt on t.OrgID = mnnt.OrgID
//          order by t.MyIdxSeq asc;
//
// GIỮ ĐÚNG NGUỒN:
//   * Endpoint là POST (nguồn Mst_PositionController.cs:26, :108).
//   * GetByPositionCode: PositionCode rỗng/null -> không query DB, trả Success = true, Data = null (gate :127-128, :157-158).
//   * Lọc PositionCode: toán tử '=' (gate :130-133, :659-662), KHÔNG lọc OrgID, KHÔNG lọc FlagActive (inactive vẫn lấy được).
//   * Lấy 1 bản ghi đầu tiên (Ft_RecordStart="0", Ft_RecordCount="1", gate :145-146) -> trả 1 object hoặc null.
//   * Lọc KeyWord: like '%KeyWord%' trên PositionName (Mst_PositionController.cs:629-632). KHÔNG lọc trên PositionCode.
//   * Lọc OrgID: tham số client, so sánh bằng '=' (Mst_PositionController.cs:633-636).
//   * Lọc FlagActive: tách dấu phẩy -> toán tử IN ('a','b') (Mst_PositionController.cs:641-649).
//   * Sắp xếp draft: mp.OrgID asc, mp.PositionCode asc (MasterData.cs:23124-23126).
//   * MyIdxSeq 0-based identity (MasterData.cs:23117).
//   * PageSize = 0 -> DataList rỗng `[]`, PageCount = 0, ItemCount giữ nguyên tổng (gate :46, :51, :81-91).
//   * DataList rỗng -> PageCount = 0 (gate :81-91).
//   * PageCount = (itemCount % pageSize == 0) ? itemCount / pageSize : itemCount / pageSize + 1 khi DataList có dòng (gate :88).
//   * DDL suy luận: không có DDL trong repo nguồn — dùng idn.Loyalty.Common\Models\HRM\Mst_Position.cs:1-22
//     và hàm lưu MasterData.cs:23570-23580 làm từ điển 9 cột. PK phức hợp (OrgID, PositionCode).
//
// Bỏ qua có chủ đích (loại Đ):
//   - CheckGatewayAuthentication: gate :35, :117, API :40-45
//   - CheckAuthorize: gate :36, :118 (hệ thống đích chưa có hệ phân quyền)
//   - Ghi RequestLog/ReturnLog: MasterData.cs:23267-23274, 23345-23352
//   - Sys_Access_CheckDenyV30: MasterData.cs:23284-23288
// ============================================================================================

/// <summary>
/// Port 1:1 <c>Mst_PositionSearch</c> — tham số lọc và phân trang của màn (Mst_PositionController.cs:26, :53-58).
/// </summary>
public class MstPositionSearchReq
{
    public string? OrgID { get; set; }               // Tổ chức — toán tử '=' (Mst_PositionController.cs:26, :633-636)
    public string? KeyWord { get; set; }             // Từ khóa tìm theo tên chức danh (PositionName), like '%KeyWord%' (Mst_PositionController.cs:26, :629-632). KHÔNG lọc PositionCode.
    public string? FlagActive { get; set; }          // Cờ hoạt động, tách dấu phẩy -> toán tử IN (Mst_PositionController.cs:26, :641-649)
    public int Ft_PageIndex { get; set; } = 0;       // Trang (0-based) (Mst_PositionController.cs:26, :47)
    public int Ft_PageSize { get; set; } = 10;       // Số dòng/trang (Mst_PositionController.cs:26, :48)
}

/// <summary>
/// Port 1:1 tham số đầu vào của GetByPositionCode (Mst_PositionController.cs:108).
/// </summary>
public class MstPositionGetByPositionCodeReq
{
    public string? PositionCode { get; set; }
    public string? OrgID { get; set; }   // nguồn lấy từ session (orgUserUI.OrgId, gate :143) -> nhận tuỳ chọn trong body (LESSONS A.14); không truyền = không lọc
}

/// <summary>
/// Port 1:1 tham số đầu vào của GetAllActive (Mst_PositionController.cs:173).
/// Nguồn lấy OrgID từ session/header (orgUserUI.OrgId, gate :185, :195, :207) -> nhận tuỳ chọn trong body (LESSONS A.14); không truyền = không lọc.
/// </summary>
public class MstPositionGetAllActiveReq
{
    public string? OrgID { get; set; }
}

/// <summary>
/// Port 1:1 tham số đầu vào của Create (Mst_PositionController.cs:238-321).
/// </summary>
public class MstPositionCreateReq
{
    public string? OrgID { get; set; }
    public string? PositionCode { get; set; }
    public string? PositionName { get; set; }
    public string? PositionDesc { get; set; }
    public string? Remark { get; set; }
}

/// <summary>
/// Port 1:1 tham số đầu vào của Update (Mst_PositionController.cs:325-391).
/// Gate nhận strJson bọc trong TModels.Mst_Position.
/// </summary>
public class MstPositionUpdateReq
{
    public string? OrgID { get; set; }
    public string? PositionCode { get; set; }
    public string? PositionName { get; set; }
    public string? PositionDesc { get; set; }
    public string? FlagActive { get; set; }
    public string? Remark { get; set; }
}

/// <summary>
/// Port 1:1 tham số đầu vào của Delete (Mst_PositionController.cs:395-456).
/// </summary>
public class MstPositionDeleteReq
{
    public string? OrgID { get; set; }
    public string? PositionCode { get; set; }
}

/// <summary>
/// Port 1:1 tham số đầu vào của Export (gate Mst_PositionController.cs:527).
/// Kế thừa MstPositionSearchReq (OrgID, KeyWord, FlagActive, Ft_PageIndex, Ft_PageSize).
/// </summary>
public class MstPositionExportReq : MstPositionSearchReq
{
}

/// <summary>
/// DTO dòng kết quả tìm kiếm chức danh — 9 cột từ bảng Mst_Position + MyIdxSeq (0-based) + mnnt_NNTFullName (left join Mst_NNT).
/// Nguồn: MasterData.cs:23157-23173, idn.Loyalty.Common\Models\HRM\Mst_Position.cs:1-22.
/// </summary>
public class Mst_PositionSearchResultItem
{
    public long MyIdxSeq { get; set; }               // Chỉ số 0-based từ bảng draft (MasterData.cs:23117, :23160)
    public string OrgID { get; set; } = "";          // Mã tổ chức — PK (Mst_Position.cs:12, MasterData.cs:23118, :23165)
    public string PositionCode { get; set; } = "";   // Mã chức danh — PK (Mst_Position.cs:11, MasterData.cs:23119, :23166)
    public string? NetworkID { get; set; }           // Mã mạng lưới (Mst_Position.cs:13)
    public string? PositionName { get; set; }        // Tên chức danh (Mst_Position.cs:14)
    public string? PositionDesc { get; set; }        // Mô tả chức danh (Mst_Position.cs:15)
    public string? FlagActive { get; set; }          // Trạng thái ('1'/'0') (Mst_Position.cs:16)
    public string? Remark { get; set; }              // Ghi chú (Mst_Position.cs:17)
    public DateTime? LogLUDTimeUTC { get; set; }     // Thời điểm cập nhật UTC (Mst_Position.cs:18)
    public string? LogLUBy { get; set; }             // Người cập nhật (Mst_Position.cs:19)
    public string? mnnt_NNTFullName { get; set; }   // Tên tổ chức từ left join Mst_NNT (Mst_Position.cs:20, MasterData.cs:23162, :23167-23168)
}

[ApiController]
[Route("api/mst_position")]
[Produces("application/json")]
public class Mst_PositionControllerPort : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration? _config;
    private readonly IWebHostEnvironment? _env;

    public Mst_PositionControllerPort(AppDbContext db, IWebHostEnvironment? env = null, IConfiguration? config = null)
    {
        _db = db;
        _env = env;
        _config = config;
    }

    /// <summary>
    /// Port 1:1 action Search từ gate Mst_PositionController.cs:26-106.
    /// Nhận POST JSON body, trả JsonResultEntry bọc PageInfo&lt;Mst_PositionSearchResultItem&gt;.
    /// </summary>
    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] MstPositionSearchReq? req)
    {
        var r = req ?? new MstPositionSearchReq();
        var pageIndex = r.Ft_PageIndex;
        var pageSize = r.Ft_PageSize;

        // Search gọi GetXAsync với start = PageIndex * PageSize, count = PageSize (Mst_PositionController.cs:70-71).
        var start = (long)pageIndex * pageSize;
        var (itemCount, rows) = await GetXAsync(r.KeyWord, r.OrgID, r.FlagActive, start, pageSize);

        // PageSize=0 -> DataList rỗng (nguồn: nFilterRecordEnd = start-1, SQL biz + gate :81-91).
        // PageCount = 0 khi DataList rỗng, ngược lại ceil(ItemCount / ps) với ps = PageSize (PageSize=0 thì 10) — gate :81-89.
        var ps = pageSize == 0 ? 10 : pageSize;
        var pageCount = rows.Count == 0 ? 0 : (itemCount % ps == 0 ? itemCount / ps : itemCount / ps + 1);

        var pageInfo = new PageInfo<Mst_PositionSearchResultItem>(rows, itemCount, pageCount, pageIndex, pageSize);
        return Ok(new { Success = true, Data = pageInfo });
    }

    /// <summary>
    /// Port 1:1 action GetByPositionCode từ gate Mst_PositionController.cs:108-171.
    /// Nhận POST JSON body, trả 1 đối tượng Mst_PositionSearchResultItem hoặc null trong Data.
    /// Nếu PositionCode rỗng/null: không truy vấn DB, trả ngay Success = true, Data = null (gate :127-128, :157-158).
    /// </summary>
    [HttpPost("getbypositioncode")]
    public async Task<IActionResult> GetByPositionCode([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] MstPositionGetByPositionCodeReq? req)
    {
        // Kiểm tra tham số: nếu PositionCode null hoặc rỗng -> không query DB, trả Success = true, Data = null (gate Mst_PositionController.cs:127-128, :157-158)
        if (string.IsNullOrWhiteSpace(req?.PositionCode))
        {
            return Ok(new { Success = true, Data = (object?)null });
        }

        // Truy vấn 1 bản ghi đầu tiên với PositionCode khớp bằng '=' (gate :130-133, :145-146, :654-673)
        // Order by OrgID asc, PositionCode asc theo biz MasterData.cs:23124-23126
        var (_, rows) = await GetXAsync(null, req.OrgID, null, 0, 1, positionCodeEqual: req.PositionCode);

        // Trả về 1 object duy nhất nếu tìm thấy, ngược lại null (gate Mst_PositionController.cs:152-158)
        return Ok(new { Success = true, Data = rows.Count > 0 ? rows[0] : null });
    }

    /// <summary>
    /// Port 1:1 action GetAllActive từ gate Mst_PositionController.cs:173-234.
    /// Lọc FlagActive = "1" (toán tử IN tách dấu phẩy, :641-649), OrgID tuỳ chọn từ body (nguồn lấy từ session/header :185, :195).
    /// Ft_RecordStart = 0, Ft_RecordCount = int.MaxValue. Sắp xếp OrgID asc, PositionCode asc (MasterData.cs:23124-23126).
    /// Trả danh sách trần (List&lt;Mst_PositionSearchResultItem&gt;), nếu không có dữ liệu trả Data = null (gate :192, :216-221).
    /// </summary>
    [HttpPost("getallactive")]
    public async Task<IActionResult> GetAllActive([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] MstPositionGetAllActiveReq? req)
    {
        // Gọi GetXAsync với KeyWord = null, OrgID từ body (hoặc null nếu không truyền), FlagActive = "1", RecordStart = 0, RecordCount = int.MaxValue (gate Mst_PositionController.cs:192-215, MasterData.cs:23056-23224)
        var (_, rows) = await GetXAsync(null, req?.OrgID, "1", 0, int.MaxValue, null);

        // Trả về danh sách trần trong Data, rỗng trả về null (gate Mst_PositionController.cs:192, :216-221)
        return Ok(new { Success = true, Data = rows.Count > 0 ? rows : null });
    }

    /// <summary>
    /// Port 1:1 action Create từ gate Mst_PositionController.cs:238-321 (#200).
    /// Chuỗi gọi: gate Mst_PositionController.Create -> WA_Mst_Position_Create -> WAS_Mst_Position_Create -> Mst_Position_Create -> Mst_Position_CreateX (MasterData.cs:23452-23590).
    ///
    /// Quy trình nghiệp vụ:
    ///   0. Body rỗng / null: gate :247-254 ném ServiceException("strJson null") -> mã mặc định Mst_Position_Create (400).
    ///   1. Sinh mã tự động (Seq): gate :268-288 gọi WA_Seq_Common_Get với SeqType.PositionCode,
    ///      lấy sequence tăng dần qua PortSeq.NextAsync&lt;Seq_PositionCode&gt; trước khi vào transaction lưu (LESSONS A.15),
    ///      format tạm "POS{0:00000}" (GAP: chưa rõ format Seq nguồn, dùng tạm POS{0:00000}). Mã sinh ra ghi đè lên PositionCode của client.
    ///   2. Mở transaction lưu DB (MasterData.cs:23629 _cf.db.BeginTransaction()).
    ///   3. Chuẩn hoá dữ liệu (MasterData.cs:23486-23490):
    ///      - strOrgID = StdParam(req.OrgID) (Trim + ToUpper, rỗng -> null)
    ///      - strPositionCode = StdParam(generatedPositionCode)
    ///      - strPositionName = StdStr(req.PositionName) (Trim + Normalize, rỗng -> null)
    ///      - strPositionDesc = StdStr(req.PositionDesc)
    ///      - strRemark = StdStr(req.Remark)
    ///   4. Kiểm tra chuỗi rỗng:
    ///      - strOrgID rỗng -> ErridnLoyalty.Mst_Position_CreateX_InvalidOrgID (:23500-23510)
    ///      - strPositionCode rỗng -> ErridnLoyalty.Mst_Position_CreateX_InvalidPositionCode (:23512-23522)
    ///   5. Kiểm tra DB (theo đúng thứ tự nguồn :23524-23560):
    ///      - Mst_Position_CheckDB(strOrgID, strPositionCode, FlagExistToCheck: No, FlagActiveListToCheck: ""):
    ///        PositionCode trong OrgID phải CHƯA tồn tại -> nếu đã tồn tại ném ErridnLoyalty.Mst_Position_CheckDB_PositionCodeExist (:22960)
    ///      - Mst_Org_CheckDB(strOrgID, FlagExistToCheck: Yes, FlagActiveListToCheck: Active):
    ///        OrgID phải TỒN TẠI trong bảng Mst_Org -> nếu không thấy ném ErridnLoyalty.Mst_Org_CheckDB_OrgNotFound (:61);
    ///        FlagActive phải thuộc "1" -> nếu không thuộc ném ErridnLoyalty.Mst_Org_CheckDB_FlagActiveNotMatched (:88)
    ///      - strPositionName rỗng -> ErridnLoyalty.Mst_Position_CreateX_InvalidPositionName (:23541-23551)
    ///      - Mst_Position_CheckPositionName(strOrgID, strPositionName, FlagExistToCheck: No, FlagActiveListToCheck: ""):
    ///        PositionName trong OrgID phải CHƯA tồn tại -> nếu đã tồn tại ném ErridnLoyalty.Mst_Position_CheckDB_PositionNameExist (:23032)
    ///   6. Lưu DB Mst_Position (:23565-23588):
    ///      OrgID, PositionCode, NetworkID (cấu hình Biz_NetworkID, mặc định "1001"), PositionName, PositionDesc,
    ///      FlagActive = "1" (TConst.Flag.Active), Remark, LogLUDTimeUTC = DateTime.UtcNow, LogLUBy = "port".
    ///   7. Commit transaction. Thành công trả JsonResultEntry với Success = true, Data = null (gate :307-308).
    ///   8. Ngoại lệ: rollback transaction và trả lỗi mặc định PortResult.Fail("Mst_Position_Create", "Exception", ex.Message).
    /// </summary>
    [HttpPost("create")]
    public async Task<IActionResult> Create(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] MstPositionCreateReq? req,
        [FromServices] IConfiguration? config)
    {
        // 0. Body rỗng / null: gate Mst_PositionController.cs:247-254 ném ServiceException("strJson null")
        // Trả mã mặc định của Create (LESSONS A.15)
        if (req == null)
        {
            return PortResult.Fail("Mst_Position_Create", "Check.strJson", null);
        }

        // 1. Sinh mã tự động (Seq) TRƯỚC transaction lưu (gate :268-288, LESSONS A.15).
        // Format: "POS{0:00000}" theo yêu cầu đề bài; mã sinh ra ghi đè lên PositionCode của client.
        var seq = await PortSeq.NextAsync<Seq_PositionCode>(_db);
        var generatedPositionCode = string.Format("POS{0:00000}", seq);

        // 2. Mở transaction chính cho nghiệp vụ lưu (MasterData.cs:23629 _cf.db.BeginTransaction())
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // 3. Chuẩn hoá đầu vào (MasterData.cs:23486-23490)
            var strOrgID = StdParam(req.OrgID);
            var strPositionCode = StdParam(generatedPositionCode);
            var strPositionName = StdStr(req.PositionName);
            var strPositionDesc = StdStr(req.PositionDesc);
            var strRemark = StdStr(req.Remark);

            // 4. Kiểm tra chuỗi rỗng OrgID (MasterData.cs:23500-23510)
            if (string.IsNullOrEmpty(strOrgID))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_Position_CreateX_InvalidOrgID", "Check.OrgID", strOrgID);
            }

            // Kiểm tra chuỗi rỗng PositionCode (MasterData.cs:23512-23522)
            if (string.IsNullOrEmpty(strPositionCode))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_Position_CreateX_InvalidPositionCode", "Check.strPositionCode", strPositionCode);
            }

            // 5. Kiểm tra DB theo đúng thứ tự nguồn:
            // (a) Mst_Position_CheckDB(strOrgID, strPositionCode, FlagExistToCheck: No, "") (:23524-23531)
            //     PositionCode trong OrgID phải CHƯA tồn tại
            var existPositionCode = await _db.Set<Mst_Position>().AsNoTracking()
                .AnyAsync(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(strOrgID), PortQuery.Esc)
                            && EF.Functions.Like((x.PositionCode ?? "").ToLower(), PortQuery.Equal(strPositionCode), PortQuery.Esc));
            if (existPositionCode)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_Position_CheckDB_PositionCodeExist", "Check.OrgID", strOrgID, "Check.PositionCode", strPositionCode);
            }

            // (b) Mst_Org_CheckDB(strOrgID, FlagExistToCheck: Yes, FlagActiveListToCheck: Active) (:23533-23539, MasterData.Org.cs:29-93)
            //     OrgID phải TỒN TẠI trong bảng Mst_Org và FlagActive thuộc "1"
            var org = await _db.Set<Mst_Org>().AsNoTracking()
                .FirstOrDefaultAsync(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(strOrgID), PortQuery.Esc));
            if (org == null)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_Org_CheckDB_OrgNotFound", "Check.OrgID", strOrgID);
            }
            if (!"1".Contains(org.FlagActive ?? ""))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_Org_CheckDB_FlagActiveNotMatched", "Check.OrgID", strOrgID,
                    "Check.strFlagActiveListToCheck", "1", "DB.FlagActive", org.FlagActive);
            }

            // (c) Kiểm tra chuỗi rỗng PositionName (MasterData.cs:23541-23551)
            if (string.IsNullOrEmpty(strPositionName))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_Position_CreateX_InvalidPositionName", "Check.strPositionName", strPositionName);
            }

            // (d) Mst_Position_CheckPositionName(strOrgID, strPositionName, FlagExistToCheck: No, "") (:23553-23560, :22984-23054)
            //     PositionName trong OrgID phải CHƯA tồn tại
            var existPositionName = await _db.Set<Mst_Position>().AsNoTracking()
                .AnyAsync(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(strOrgID), PortQuery.Esc)
                            && EF.Functions.Like((x.PositionName ?? "").ToLower(), PortQuery.Equal(strPositionName), PortQuery.Esc));
            if (existPositionName)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_Position_CheckDB_PositionNameExist", "Check.OrgID", strOrgID, "Check.PositionName", strPositionName);
            }

            // 6. Lưu DB Mst_Position (MasterData.cs:23565-23588)
            var cfg = config ?? _config;
            var networkId = cfg?["Biz_NetworkID"] ?? "1001";
            var newPos = new Mst_Position
            {
                OrgID = strOrgID,
                PositionCode = strPositionCode,
                NetworkID = networkId,
                PositionName = strPositionName,
                PositionDesc = strPositionDesc,
                FlagActive = "1",
                Remark = strRemark,
                LogLUDTimeUTC = DateTime.UtcNow,
                LogLUBy = "port"
            };

            _db.Set<Mst_Position>().Add(newPos);
            await _db.SaveChangesAsync();

            // 7. Commit transaction
            await tx.CommitAsync();

            // Thành công trả Data = null (gate Mst_PositionController.cs:307-308)
            return Ok(new { Success = true, Data = (object?)null });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return PortResult.Fail("Mst_Position_Create", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Port 1:1 action Update từ gate Mst_PositionController.cs:325-391 (#201).
    /// Chuỗi gọi: gate Mst_PositionController.Update -> WA_Mst_Position_Update -> WAS_Mst_Position_Update -> Mst_Position_Update -> Mst_Position_UpdateX (MasterData.cs:23798-23931).
    ///
    /// Quy trình nghiệp vụ:
    ///   0. Body rỗng / null: gate :340 ném ServiceException("strJson null") -> mã mặc định Mst_Position_Update (400).
    ///   1. Chuỗi Ft_Cols_Upd cứng từ Gate: "Mst_Position.PositionName,Mst_Position.PositionDesc,Mst_Position.FlagActive," (:356-359).
    ///      Biz MasterData.cs:23838-23851:
    ///      strFt_Cols_Upd = StdParam(objFt_Cols_Upd) ?? "";
    ///      bUpd_PositionName = strFt_Cols_Upd.Contains("Mst_Position.PositionName".ToUpperInvariant());
    ///      bUpd_PositionDesc = strFt_Cols_Upd.Contains("Mst_Position.PositionDesc".ToUpperInvariant());
    ///      bUpd_FlagActive = strFt_Cols_Upd.Contains("Mst_Position.FlagActive".ToUpperInvariant());
    ///      bUpd_Remark = strFt_Cols_Upd.Contains("Mst_Position.Remark".ToUpperInvariant());
    ///   2. Mở transaction: _cf.db.BeginTransaction() (:23976).
    ///   3. Chuẩn hoá dữ liệu (MasterData.cs:23841-23846):
    ///      - strOrgID = StdParam(req.OrgID) (Trim + ToUpper, rỗng -> null)
    ///      - strPositionCode = StdParam(req.PositionCode)
    ///      - strPositionName = StdStr(req.PositionName) (Trim + Normalize, rỗng -> null)
    ///      - strPositionDesc = StdStr(req.PositionDesc)
    ///      - strFlagActive = StdFlag(req.FlagActive) ("1" -> "1", khác -> "0")
    ///      - strRemark = StdStr(req.Remark)
    ///   4. Kiểm tra DB theo đúng thứ tự (MasterData.cs:23859-23905):
    ///      (a) Mst_Position_CheckDB(OrgID, PositionCode, "1", "") (:23859-23867, :22920-22970):
    ///          Kiểm tra PositionCode tồn tại trong OrgID. Lấy thực thể được tracking ra.
    ///          Nếu không tồn tại -> Mst_Position_CheckDB_PositionCodeNotFound (nguồn: MasterData.cs:22948, Error.HRM.cs:167; đề bài ghi Mst_Position_CheckDB_PositionNotFound -> xung đột đã ghi nhận).
    ///          Lấy strPositionName_DB = StdStr(position.PositionName) (:23867).
    ///      (b) Mst_Org_CheckDB(OrgID, "1", "1") (:23870-23876, MasterData.Org.cs:29-93):
    ///          OrgID phải tồn tại trong Mst_Org và FlagActive thuộc "1"
    ///          -> nếu không thấy ném Mst_Org_CheckDB_OrgNotFound (:61);
    ///          -> nếu FlagActive không thuộc "1" ném Mst_Org_CheckDB_FlagActiveNotMatched (:88).
    ///      (c) strPositionName rỗng -> ErridnLoyalty.Mst_Position_UpdateX_InvalidPositionName (:23878-23888).
    ///      (d) bUpd_PositionName và đổi tên (!string.Equals(strPositionName, strPositionName_DB, StringComparison.Ordinal)):
    ///          Mst_Position_CheckPositionName(OrgID, PositionName, "0", "") (:23890-23903, :22984-23054):
    ///          Nếu trùng tên trong cùng OrgID -> ném ErridnLoyalty.Mst_Position_CheckDB_PositionNameExist (:23032).
    ///   5. Cập nhật các cột có cờ trong Ft_Cols_Upd (MasterData.cs:23910-23928):
    ///      - if (bUpd_PositionName) position.PositionName = strPositionName;
    ///      - if (bUpd_PositionDesc) position.PositionDesc = strPositionDesc;
    ///      - if (bUpd_FlagActive) position.FlagActive = strFlagActive;
    ///      - if (bUpd_Remark) position.Remark = strRemark;
    ///      - position.LogLUDTimeUTC = DateTime.UtcNow;
    ///      - position.LogLUBy = "port";
    ///   6. Commit transaction. Thành công trả Data = null (gate :377-378).
    ///   7. Gặp ngoại lệ -> rollback và trả lỗi mặc định PortResult.Fail("Mst_Position_Update", "Exception", ex.Message).
    /// </summary>
    [HttpPost("update")]
    public async Task<IActionResult> Update(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] MstPositionUpdateReq? req)
    {
        // 0. Body rỗng / null: gate Mst_PositionController.cs:340 ném ServiceException("strJson null") -> lỗi mặc định Mst_Position_Update
        if (req == null)
        {
            return PortResult.Fail("Mst_Position_Update", "Check.strJson", null);
        }

        // 1. Chuỗi Ft_Cols_Upd cứng từ Gate: "Mst_Position.PositionName,Mst_Position.PositionDesc,Mst_Position.FlagActive," (gate :356-359)
        var strFt_Cols_Upd = StdParam("Mst_Position.PositionName,Mst_Position.PositionDesc,Mst_Position.FlagActive,") ?? "";
        var bUpd_PositionName = strFt_Cols_Upd.Contains("Mst_Position.PositionName".ToUpperInvariant());
        var bUpd_PositionDesc = strFt_Cols_Upd.Contains("Mst_Position.PositionDesc".ToUpperInvariant());
        var bUpd_FlagActive = strFt_Cols_Upd.Contains("Mst_Position.FlagActive".ToUpperInvariant());
        var bUpd_Remark = strFt_Cols_Upd.Contains("Mst_Position.Remark".ToUpperInvariant());

        // 2. Mở transaction chính cho nghiệp vụ cập nhật (MasterData.cs:23976 _cf.db.BeginTransaction())
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // 3. Chuẩn hoá đầu vào (MasterData.cs:23841-23846)
            var strOrgID = StdParam(req.OrgID);
            var strPositionCode = StdParam(req.PositionCode);
            var strPositionName = StdStr(req.PositionName);
            var strPositionDesc = StdStr(req.PositionDesc);
            var strFlagActive = StdFlag(req.FlagActive);
            var strRemark = StdStr(req.Remark);

            // 4. Kiểm tra DB theo đúng thứ tự nguồn:
            // (a) Mst_Position_CheckDB(OrgID, PositionCode, "1", "") (:23859-23867, :22920-22970)
            //     Kiểm tra PositionCode tồn tại trong OrgID. Lấy thực thể được tracking ra.
            var position = await _db.Set<Mst_Position>()
                .FirstOrDefaultAsync(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(strOrgID ?? ""), PortQuery.Esc)
                                       && EF.Functions.Like((x.PositionCode ?? "").ToLower(), PortQuery.Equal(strPositionCode ?? ""), PortQuery.Esc));
            if (position == null)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_Position_CheckDB_PositionCodeNotFound", "Check.OrgID", strOrgID, "Check.PositionCode", strPositionCode);
            }

            var strPositionName_DB = StdStr(position.PositionName);

            // (b) Mst_Org_CheckDB(OrgID, "1", "1") (:23870-23876, MasterData.Org.cs:29-93)
            //     OrgID phải tồn tại trong Mst_Org và FlagActive thuộc "1"
            var org = await _db.Set<Mst_Org>().AsNoTracking()
                .FirstOrDefaultAsync(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(strOrgID ?? ""), PortQuery.Esc));
            if (org == null)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_Org_CheckDB_OrgNotFound", "Check.OrgID", strOrgID);
            }
            if (!"1".Contains(org.FlagActive ?? ""))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_Org_CheckDB_FlagActiveNotMatched", "Check.OrgID", strOrgID,
                    "Check.strFlagActiveListToCheck", "1", "DB.FlagActive", org.FlagActive);
            }

            // (c) Kiểm tra chuỗi rỗng PositionName (MasterData.cs:23878-23888)
            if (string.IsNullOrEmpty(strPositionName))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_Position_UpdateX_InvalidPositionName", "Check.strPositionName", strPositionName);
            }

            // (d) Mst_Position_CheckPositionName(OrgID, PositionName, "0", "") (:23890-23903, :22984-23054)
            //     Nếu đổi tên: PositionName trong OrgID phải CHƯA tồn tại
            if (bUpd_PositionName && !string.Equals(strPositionName, strPositionName_DB, StringComparison.Ordinal))
            {
                var existPositionName = await _db.Set<Mst_Position>().AsNoTracking()
                    .AnyAsync(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(strOrgID ?? ""), PortQuery.Esc)
                                && EF.Functions.Like((x.PositionName ?? "").ToLower(), PortQuery.Equal(strPositionName ?? ""), PortQuery.Esc));
                if (existPositionName)
                {
                    await tx.RollbackAsync();
                    return PortResult.Fail("Mst_Position_CheckDB_PositionNameExist", "Check.OrgID", strOrgID, "Check.PositionName", strPositionName);
                }
            }

            // 5. Cập nhật các cột có cờ trong Ft_Cols_Upd (MasterData.cs:23910-23928)
            if (bUpd_PositionName) position.PositionName = strPositionName;
            if (bUpd_PositionDesc) position.PositionDesc = strPositionDesc;
            if (bUpd_FlagActive) position.FlagActive = strFlagActive;
            if (bUpd_Remark) position.Remark = strRemark;
            position.LogLUDTimeUTC = DateTime.UtcNow;
            position.LogLUBy = "port";

            await _db.SaveChangesAsync();

            // 6. Commit transaction
            await tx.CommitAsync();

            // Thành công trả Data = null (gate Mst_PositionController.cs:377-378)
            return Ok(new { Success = true, Data = (object?)null });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return PortResult.Fail("Mst_Position_Update", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Port 1:1 action Delete từ gate Mst_PositionController.cs:395-456 (#202).
    /// Chuỗi gọi: gate Mst_PositionController.Delete -> WA_Mst_Position_Delete (MstPositionController.cs:239-310) -> WAS_Mst_Position_Delete (MasterData.cs:24386-24460) -> Mst_Position_Delete (MasterData.cs:24268-24384) -> Mst_Position_DeleteX (MasterData.cs:24151-24266).
    ///
    /// Quy trình nghiệp vụ:
    ///   1. Kiểm tra PositionCode rỗng/null tại gate (:414, :442) -> throw ServiceException("invalid_positioncode", "invalid_positioncode") -> 400 ErridnLoyalty.invalid_positioncode.
    ///   2. Gọi DeleteXAsync mở transaction lưu DB: _cf.db.BeginTransaction() (MasterData.cs:24299).
    ///   3. Thành công trả JsonResultEntry với Success = true, Data = null (gate :437-438).
    /// </summary>
    [HttpPost("delete")]
    public async Task<IActionResult> Delete(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] MstPositionDeleteReq? req)
    {
        // 1. Kiểm tra PositionCode rỗng tại gate (:414, :442): rỗng -> ném ServiceException("invalid_positioncode", "invalid_positioncode")
        if (string.IsNullOrWhiteSpace(req?.PositionCode))
        {
            return PortResult.Fail("invalid_positioncode", "Check.PositionCode", req?.PositionCode);
        }

        var err = await DeleteXAsync(req?.OrgID, req?.PositionCode);
        if (err != null)
        {
            return err;
        }

        return Ok(new { Success = true, Data = (object?)null });
    }

    /// <summary>
    /// Port 1:1 action DeleteMultiple từ gate Mst_PositionController.cs:458-522 (#203).
    /// Chuỗi gọi: gate Mst_PositionController.DeleteMultiple -> WA_Mst_Position_Delete (MstPositionController.cs:239-310)
    /// -> WAS_Mst_Position_Delete (MasterData.cs:24386-24460) -> Mst_Position_Delete (MasterData.cs:24268-24384)
    /// -> Mst_Position_DeleteX (MasterData.cs:24151-24266).
    ///
    /// Quy trình nghiệp vụ:
    ///   1. Body rỗng / null: gate :467-474 ném ServiceException("strJson null") -> bắt lỗi trả về lỗi mặc định (400 ErridnLoyalty.Mst_Position_DeleteMultiple).
    ///   2. Lặp tuần tự qua từng phần tử (gate :489-506), mỗi phần tử gọi WA_Mst_Position_Delete riêng biệt
    ///      (mỗi phần tử mở 1 transaction tại MasterData.cs:24299 qua DeleteXAsync).
    ///      Nếu lỗi ở phần tử k -> dừng lặp, return lỗi đó ngay. Các phần tử TRƯỚC k ĐÃ XÓA thành công vào DB
    ///      và không bị rollback ("xóa một phần" là hành vi nguồn).
    ///   3. KHÔNG kiểm tra rỗng thủ công ở gate (khác với Delete đơn): mã rỗng xuống biz sẽ bị CheckDB bắt và trả về
    ///      Mst_Org_CheckDB_OrgNotFound (nếu OrgID rỗng) hoặc Mst_Position_CheckDB_PositionCodeNotFound (nếu PositionCode rỗng).
    ///   4. Thành công Data = null, Success = true (gate :508-509).
    /// </summary>
    [HttpPost("deletemultiple")]
    public async Task<IActionResult> DeleteMultiple(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] List<MstPositionDeleteReq>? req)
    {
        // 1. Kiểm tra body rỗng / null (gate Mst_PositionController.cs:467-474):
        // if (!string.IsNullOrEmpty(strJson)) { ... } else { throw new ServiceException("strJson null"); }
        if (req == null)
        {
            return PortResult.Fail("Mst_Position_Delete", "Check.strJson", null);
        }

        // 2. Lặp tuần tự qua từng phần tử (gate Mst_PositionController.cs:489-506).
        // Mỗi phần tử chạy trong 1 transaction riêng biệt (MasterData.cs:24299 qua DeleteXAsync).
        // "xóa một phần" là hành vi nguồn: nếu phần tử thứ k gặp lỗi -> dừng lặp và trả lỗi ngay,
        // các phần tử trước k đã commit thành công vào DB và KHÔNG bị rollback,
        // các phần tử sau k không được xử lý.
        foreach (var item in req)
        {
            var err = await DeleteXAsync(item?.OrgID, item?.PositionCode);
            if (err != null)
            {
                return err;
            }
        }

        // 3. Thành công trả Data = null (gate Mst_PositionController.cs:508-509)
        return Ok(new { Success = true, Data = (object?)null });
    }

    // ============================================================================================
    // Export — nguồn gate Mst_PositionController.cs:527-605 (#204) -> API WA_Mst_Position_Get
    // (MstPositionController.cs:17-88) -> biz WAS_Mst_Position_Get (MasterData.cs:23357-23450)
    // -> biz Mst_Position_Get (MasterData.cs:23226-23355, luôn Rollback :23318, KHÔNG ghi DB)
    // -> biz Mst_Position_GetX (MasterData.cs:23056-23224).
    //
    // Quy trình và thứ tự xử lý:
    //   1. Body JSON: { OrgID, KeyWord, FlagActive } (gate :527, :548-553).
    //   2. Gọi GetXAsync(req.KeyWord, req.OrgID, req.FlagActive, 0, int.MaxValue, null).
    //      Nguồn: strWhereClause_Mst_Position (:624-652):
    //        - KeyWord: like '%KeyWord%' trên PositionName (:629-632). KHÔNG lọc PositionCode.
    //        - OrgID: CÓ lọc bằng toán tử '=' nếu OrgID được truyền (:633-636).
    //        - FlagActive: tách dấu phẩy -> toán tử IN ('a','b') (:641-649).
    //      Ft_RecordStart = "0" (:566), Ft_RecordCount = Ft_RecordCount (nguồn gán từ cấu hình/hằng số, ở đây int.MaxValue) (:567).
    //   3. Kiểm tra danh sách kết quả: gate :574, :589-592
    //      Nếu danh sách rỗng (rows == null || rows.Count == 0) -> throw ServiceException("Không có dữ liệu") (:591)
    //      -> trả về 400 ErridnLoyalty.Mst_Position_Export kèm Params Message = "Không có dữ liệu".
    //   4. Từ điển cột xuất Excel: gate :578 -> GetImportDicColums() (:609-619).
    //      Dùng chung từ điển Import cho Export theo chuẩn nguồn tại dòng 578.
    //      Gồm 3 cột theo thứ tự:
    //      - PositionName: "Tên chức danh (*)" (:614)
    //      - PositionDesc: "Mô tả" (:615)
    //      - FlagActive: "Trạng thái" (:617)
    //      (Lưu ý: PositionCode và Remark bị comment ở nguồn :613, :616 nên không đưa vào file export).
    //   5. Sinh đường dẫn file xuất: BaseController.GenExcelExportFilePath (:580) -> PortExcel.GenExcelExportFilePath(webRoot, "Mst_Position").
    //      Tên sheet: "Mst_Position" (:581).
    //      Ghi file qua PortExcel.ExportToExcelFromList(rows, dicColNames, physicalPath, "Mst_Position") (:581).
    //   6. Trả URL tải file với Success = true, Data = { Url = PortExcel.GetRequestBaseAddress(Request) + virtualPath } (:582-586).
    //
    // Bỏ qua có chủ đích (loại Đ):
    //   - CheckGatewayAuthentication: gate Mst_PositionController.cs:537, API MstPositionController.cs:40-45
    //   - CheckAuthorize: gate Mst_PositionController.cs:538 (hệ thống đích chưa có hệ phân quyền)
    //   - Sys_Access_CheckDenyV30: MasterData.cs:23284-23288
    //   - Ghi RequestLog/ReturnLog: MasterData.cs:23267-23274, 23345-23352
    //   - Cột MyIdxSeq, OrgID, PositionCode, mnnt_NNTFullName có trong list kết quả nhưng không nằm trong từ điển -> không xuất ra file
    // ============================================================================================
    [HttpPost("export")]
    public async Task<IActionResult> Export([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] MstPositionSearchReq? req)
    {
        var r = req ?? new MstPositionSearchReq();

        // Gọi GetXAsync(keyWord, orgIdEqual, flagActiveCsv, recordStart, recordCount):
        // KeyWord lọc trên PositionName (like %kw%), OrgID lọc theo tham số client (=), FlagActive IN tách dấu phẩy.
        // RecordStart = 0, RecordCount = int.MaxValue (gate Mst_PositionController.cs:566-567).
        var (_, rows) = await GetXAsync(
            keyWord: r.KeyWord,
            orgIdEqual: r.OrgID,
            flagActiveCsv: r.FlagActive,
            recordStart: 0,
            recordCount: int.MaxValue);

        // Nguồn Mst_PositionController.cs:574, 590-592: nếu danh sách null hoặc Count == 0 -> throw ServiceException("Không có dữ liệu")
        if (rows == null || rows.Count == 0)
        {
            return PortResult.Fail("Mst_Position_Export", "Message", "Không có dữ liệu");
        }

        // Tạo thư mục và đường dẫn file Excel xuất: GenExcelExportFilePath(prefix) (gate :580)
        var webRoot = _env?.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
        {
            webRoot = _env != null
                ? Path.Combine(_env.ContentRootPath, "wwwroot")
                : Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        }

        // Dùng từ điển GetImportDicColums() (gate :578, :609-619) — dùng từ điển Import cho Export theo chuẩn nguồn
        var dicColNames = GetImportDicColums();
        string virtualPath;
        try
        {
            string physicalPath;
            (physicalPath, virtualPath) = PortExcel.GenExcelExportFilePath(webRoot, "Mst_Position");

            // Ghi danh sách ra file Excel với tên sheet "Mst_Position" (gate :581)
            PortExcel.ExportToExcelFromList(rows, dicColNames, physicalPath, "Mst_Position");
        }
        catch (Exception ex)
        {
            // Nguồn (gate :594-598) bắt mọi ngoại lệ (kể cả lỗi ghi file) và trả Success=false, không để thành HTTP 500.
            return PortResult.Fail("Mst_Position_Export", "Exception", ex.Message);
        }

        // Trả URL tải file: Data = { Url = GetRequestBaseAddress() + url } (gate :583-586)
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
    /// Từ điển cột xuất Excel: dùng chung từ điển Import cho Export theo chuẩn nguồn tại gate Mst_PositionController.cs:578, :609-619.
    /// Gồm 3 cột:
    ///   - PositionName: "Tên chức danh (*)" (:614)
    ///   - PositionDesc: "Mô tả" (:615)
    ///   - FlagActive: "Trạng thái" (:617)
    /// (PositionCode và Remark bị comment ở nguồn :613, :616).
    /// </summary>
    private static Dictionary<string, string> GetImportDicColums()
    {
        return new Dictionary<string, string>
        {
            { "PositionName", "Tên chức danh (*)" },
            { "PositionDesc", "Mô tả" },
            { "FlagActive", "Trạng thái" }
        };
    }

    /// <summary>
    /// Xóa 1 chức danh — tương ứng chuỗi gọi:
    /// WA_Mst_Position_Delete (MstPositionController.cs:239-310) -> WAS_Mst_Position_Delete (MasterData.cs:24386-24460)
    /// -> Mst_Position_Delete (MasterData.cs:24268-24384) -> Mst_Position_DeleteX (MasterData.cs:24151-24266).
    /// Mỗi lần gọi mở 1 transaction riêng (_cf.db.BeginTransaction :24299),
    /// kiểm tra Mst_Org_CheckDB, Mst_Position_CheckDB, xóa dòng khỏi Mst_Position, và commit.
    /// Nếu lỗi: rollback transaction và trả IActionResult tương ứng; thành công trả null.
    /// </summary>
    private async Task<IActionResult?> DeleteXAsync(string? rawOrgId, string? rawPositionCode)
    {
        var strOrgID = StdParam(rawOrgId);
        var strPositionCode = StdParam(rawPositionCode);

        // Mở transaction riêng cho từng phần tử (MasterData.cs:24299)
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // Mst_Org_CheckDB(OrgID, "1", "1") (MasterData.cs:24187-24193, MasterData.Org.cs:29-93)
            Mst_Org? org = null;
            if (!string.IsNullOrEmpty(strOrgID))
            {
                org = await _db.Set<Mst_Org>().AsNoTracking()
                    .FirstOrDefaultAsync(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(strOrgID), PortQuery.Esc));
            }
            if (org == null)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_Org_CheckDB_OrgNotFound", "Check.OrgID", strOrgID);
            }
            if (!"1".Contains(org.FlagActive ?? ""))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_Org_CheckDB_FlagActiveNotMatched", "Check.OrgID", strOrgID,
                    "Check.strFlagActiveListToCheck", "1", "DB.FlagActive", org.FlagActive);
            }

            // Mst_Position_CheckDB(OrgID, PositionCode, "1", "") (MasterData.cs:24195-24202, :22920-22970)
            Mst_Position? position = null;
            if (!string.IsNullOrEmpty(strPositionCode))
            {
                position = await _db.Set<Mst_Position>()
                    .FirstOrDefaultAsync(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(strOrgID!), PortQuery.Esc)
                                           && EF.Functions.Like((x.PositionCode ?? "").ToLower(), PortQuery.Equal(strPositionCode), PortQuery.Esc));
            }
            if (position == null)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_Position_CheckDB_PositionCodeNotFound", "Check.OrgID", strOrgID, "Check.PositionCode", strPositionCode);
            }

            // Kiểm tra Staff_MapDepartment (MasterData.cs:24207-24251):
            // GAP: Chưa có entity Staff_MapDepartment trong DbContext -> Bỏ qua theo đề bài

            // Thực hiện xóa (MasterData.cs:24257)
            _db.Set<Mst_Position>().Remove(position);
            await _db.SaveChangesAsync();

            // Commit transaction (MasterData.cs:24347)
            await tx.CommitAsync();

            return null;
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return PortResult.Fail("Mst_Position_Delete", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Truy vấn 2 bước tương ứng Mst_Position_GetX (MasterData.cs:23056-23224):
    /// 1. Draft query (#tbl_Mst_Position_Filter_Draft, :23115-23131):
    ///    Lọc KeyWord (PositionName like '%kw%'), OrgID (=), FlagActive (in (...)), PositionCode (=),
    ///    đếm MyCount, sắp xếp mp.OrgID asc, mp.PositionCode asc.
    /// 2. Cắt trang (#tbl_Mst_Position_Filter, :23133-23141).
    /// 3. Select cuối (:23157-23173) left join Mst_NNT theo OrgID lấy mnnt_NNTFullName, sắp theo MyIdxSeq asc.
    /// </summary>
    private async Task<(int itemCount, List<Mst_PositionSearchResultItem> rows)> GetXAsync(
        string? keyWord,
        string? orgIdEqual,
        string? flagActiveCsv,
        long recordStart,
        long recordCount,
        string? positionCodeEqual = null)
    {
        // 1. Khởi tạo truy vấn AsNoTracking từ bảng Mst_Position (#tbl_Mst_Position_Filter_Draft, MasterData.cs:23120-23121)
        var q = _db.Set<Mst_Position>().AsNoTracking().AsQueryable();

        // 2. Lọc KeyWord: like '%KeyWord%' trên PositionName (Mst_PositionController.cs:629-632).
        // CHỈ lọc cột PositionName, KHÔNG lọc PositionCode.
        // Hạ chữ cả 2 phía và escape % _ \ để phòng injection/lỗi chuỗi trên SQLite/EF Core.
        if (!string.IsNullOrWhiteSpace(keyWord))
        {
            var kw = PortQuery.Contains(keyWord);
            q = q.Where(p => EF.Functions.Like((p.PositionName ?? "").ToLower(), kw, PortQuery.Esc));
        }

        // 3. Lọc OrgID: toán tử '=' (Mst_PositionController.cs:633-636).
        // Nguồn dùng '=' ⇒ so sánh bằng chuỗi không phân biệt hoa/thường qua PortQuery.Equal (không wildcard).
        if (!string.IsNullOrWhiteSpace(orgIdEqual))
        {
            var org = PortQuery.Equal(orgIdEqual);
            q = q.Where(p => EF.Functions.Like((p.OrgID ?? "").ToLower(), org, PortQuery.Esc));
        }

        // 4. Lọc FlagActive: toán tử 'IN' tách dấu phẩy (Mst_PositionController.cs:641-649).
        // CUtils.StrSplit(data.FlagActive, new[] { "," }) -> in ('1','0').
        if (!string.IsNullOrWhiteSpace(flagActiveCsv))
        {
            var ds = flagActiveCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => s.ToLowerInvariant())
                .ToList();
            if (ds.Count > 0)
            {
                q = q.Where(p => ds.Contains((p.FlagActive ?? "").ToLower()));
            }
        }

        // 5. Lọc PositionCode: toán tử '=' (gate Mst_PositionController.cs:130-133, :659-662).
        // Nguồn dùng '=' ⇒ so sánh bằng chuỗi không phân biệt hoa/thường qua PortQuery.Equal (không wildcard).
        if (!string.IsNullOrWhiteSpace(positionCodeEqual))
        {
            var pos = PortQuery.Equal(positionCodeEqual);
            q = q.Where(p => EF.Functions.Like((p.PositionCode ?? "").ToLower(), pos, PortQuery.Esc));
        }

        // 6. Sắp xếp draft: mp.OrgID asc, mp.PositionCode asc (MasterData.cs:23124-23126)
        var draftOrdered = q.OrderBy(p => p.OrgID).ThenBy(p => p.PositionCode);

        // 7. Đếm tổng MyCount trên bảng draft (#tbl_Mst_Position_Filter_Draft, MasterData.cs:23130)
        var itemCount = await draftOrdered.CountAsync();

        // 8. Cắt trang draft (#tbl_Mst_Position_Filter, MasterData.cs:23133-23141).
        // Chống tràn int và PageIndex âm/PageSize=0 theo LESSONS A.12, A.7.
        var page = (recordCount <= 0 || recordStart < 0)
            ? new List<Mst_Position>()
            : await draftOrdered.Skip((int)Math.Min(recordStart, int.MaxValue)).Take((int)Math.Min(recordCount, int.MaxValue)).ToListAsync();

        // 9. Select cuối: left join Mst_NNT theo OrgID để lấy mnnt_NNTFullName (MasterData.cs:23162, :23167-23168).
        var orgIds = page.Select(x => x.OrgID).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();
        var nntMap = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        if (orgIds.Count > 0)
        {
            var nnts = await _db.Set<Mst_NNT>()
                .AsNoTracking()
                .Where(n => n.OrgID != null && orgIds.Contains(n.OrgID))
                .OrderBy(n => n.MST)
                .ToListAsync();

            foreach (var n in nnts)
            {
                if (n.OrgID != null && !nntMap.ContainsKey(n.OrgID))
                {
                    nntMap[n.OrgID] = n.NNTFullName;
                }
            }
        }

        // 10. Sắp xếp cuối theo MyIdxSeq asc (MasterData.cs:23170) và gán MyIdxSeq 0-based
        var rows = page.Select((p, idx) => new Mst_PositionSearchResultItem
        {
            MyIdxSeq = recordStart + idx,
            OrgID = p.OrgID,
            PositionCode = p.PositionCode,
            NetworkID = p.NetworkID,
            PositionName = p.PositionName,
            PositionDesc = p.PositionDesc,
            FlagActive = p.FlagActive,
            Remark = p.Remark,
            LogLUDTimeUTC = p.LogLUDTimeUTC,
            LogLUBy = p.LogLUBy,
            mnnt_NNTFullName = (!string.IsNullOrEmpty(p.OrgID) && nntMap.TryGetValue(p.OrgID, out var name)) ? name : null
        }).ToList();

        return (itemCount, rows);
    }

    private static string? StdParam(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim().ToUpperInvariant();
    private static string? StdStr(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim().Normalize();
    private static string StdFlag(string? s) => s == "1" ? "1" : "0";
}
}

namespace MiniLoyalty.Models.Port
{
    /// <summary>
    /// Entity sinh sequence cho mã chức danh — phục vụ SeqType.PositionCode (gate Mst_PositionController.cs:268-288, Seq.cs:117).
    /// PK Id tự tăng.
    /// </summary>
    public class Seq_PositionCode
    {
        public long Id { get; set; }
        public string? Remark { get; set; }
    }

    /// <summary>
    /// Cấu hình EF cho <see cref="Seq_PositionCode"/> — bảng Seq_PositionCode, PK Id.
    /// </summary>
    public class Seq_PositionCodeConfig : IEntityTypeConfiguration<Seq_PositionCode>
    {
        public void Configure(EntityTypeBuilder<Seq_PositionCode> b)
        {
            b.ToTable("Seq_PositionCode");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedOnAdd();
        }
    }
}
