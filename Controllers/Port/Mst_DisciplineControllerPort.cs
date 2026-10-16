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
// PORT 1:1 — màn danh mục hình thức kỷ luật (Mst_Discipline) của dự án nguồn 2022.7.Loyalty (#132, #133, #134, #135, #136).
//
// Nguồn controller (tầng gate):
//   Mst_DisciplineController.cs:26-106         Search -> WA_Mst_Discipline_Get (#132)
//   Mst_DisciplineController.cs:108-171        GetByDisciplineCode -> WA_Mst_Discipline_Get (#133)
//   Mst_DisciplineController.cs:173-233        GetAllActive -> WA_Mst_Discipline_Get (#134)
//   Mst_DisciplineController.cs:237-320        Create -> WA_Mst_Discipline_Create (#135)
//   Mst_DisciplineController.cs:324-389        Update -> WA_Mst_Discipline_Update (#136)
//   Mst_DisciplineController.cs:523-538        strWhereClause_Mst_Discipline (mệnh đề lọc Search)
//   Mst_DisciplineController.cs:540-555        strWhereClause_Mst_Discipline_By_DisciplineCode (mệnh đề lọc GetByDisciplineCode, GetAllActive)
// Nguồn API:
//   MstDisciplineController.cs:17-88          WA_Mst_Discipline_Get
//   MstDisciplineController.cs:91-162         WA_Mst_Discipline_Create (#135)
//   MstDisciplineController.cs:165-236        WA_Mst_Discipline_Update (#136)
// Nguồn biz:
//   MasterData.cs:34241-34331                 WAS_Mst_Discipline_Get
//   MasterData.cs:34111-34239                 Mst_Discipline_Get
//   MasterData.cs:33954-34109                 Mst_Discipline_GetX (SQL :34007-34059)
//   MasterData.cs:34555-34630                 WAS_Mst_Discipline_Create (#135)
//   MasterData.cs:34434-34553                 Mst_Discipline_Create (#135)
//   MasterData.cs:34333-34432                 Mst_Discipline_CreateX (#135)
//   MasterData.cs:34861-34939                 WAS_Mst_Discipline_Update (#136)
//   MasterData.cs:34731-34859                 Mst_Discipline_Update (#136)
//   MasterData.cs:34632-34729                 Mst_Discipline_UpdateX (#136)
//
// GIỮ ĐÚNG NGUỒN:
//   * Endpoint là POST (nguồn [AcceptVerbs("POST")]).
//   * Lọc KeyWord: like '%kw%' trên DisciplineName (Mst_DisciplineController.cs:530).
//   * Lọc FlagActive: toán tử '=' (Mst_DisciplineController.cs:534, :551) — KHÔNG IN.
//     Không tách dấu phẩy; "0,1" -> so sánh bằng chuỗi "0,1" -> 0 dòng.
//   * OrgID truyền vào (:26, :56) nhưng strWhereClause_Mst_Discipline (:523-538) KHÔNG dùng OrgID -> không lọc.
//   * Chia trang theo md.DisciplineCode asc (MasterData.cs:34016), select cuối sắp theo t.MyIdxSeq asc (MasterData.cs:34055).
//   * MyIdxSeq 0-based identity (MasterData.cs:34010).
//   * PageSize = 0 -> DataList rỗng, PageCount = 0 (gate :81-89).
//   * DDL: khong co DDL trong repo — PK = DisciplineCode (khoá draft), kieu cot can xac nhan.
//   * GetByDisciplineCode: [HttpPost("getbydisciplinecode")], body { DisciplineCode } (gate :108-171).
//     DisciplineCode rỗng -> Data null, không truy vấn (:128, :158). Ngược lại lọc DisciplineCode = (Like + PortQuery.Equal, :547).
//     FlagActive không truyền (:130-133) nên hàng FlagActive '0' vẫn lấy.
//     Ft_RecordStart = "0", Ft_RecordCount = "1" (:145-146). Trả Lst_Mst_Discipline[0] hoặc null (:152-155).
//   * GetAllActive: [HttpPost("getallactive")], không body, Ft_RecordStart = "0", Ft_RecordCount = "123456000" (gate :208-209).
//     FlagActive = "1" (toán tử '=', gate :195, :551). Trả Data là mảng trần list Mst_Discipline (null nếu rỗng) (gate :192, :215-220).
//   * Create: [HttpPost("create")], body { DisciplineCode, DisciplineName, DisciplineDesc } (gate :237-320, #135).
//     - Body rỗng hoàn toàn -> ServiceException("strJson null") (:246-253) -> mã mặc định Mst_Discipline_Create (KHÔNG tăng seq).
//     - Lấy mã sequence: WA_Seq_Common_Get với SeqType.DisciplineCode (:267-288, Seq.cs:28-41, format "{0}DISCIPLINECODE.{3}.{1:00000}{2}", max 100000).
//     - Mã từ seq GHI ĐÈ mã client gửi (:287 objMst_Discipline.DisciplineCode = strDisciplineCode).
//     - Seq tăng cả khi lỗi: seq lấy trong transaction riêng đã commit trước khi vào transaction lưu.
//     - Tên rỗng sau StdStr -> ErridnLoyalty.Mst_Discipline_CreateX_InvalidDisciplineName (:34394-34404).
//     - Lưu 7 cột: DisciplineCode, NetworkID = config["Biz_NetworkID"] ?? "1001", DisciplineName, DisciplineDesc,
//       FlagActive = '1', LogLUDTimeUTC = DateTime.UtcNow, LogLUBy = "port" (:34414-34429).
//     - 1 transaction lưu DB (:34467), lỗi -> rollback + PortResult.Fail. Thành công -> Data = null (:306-307).
//   * Update: [HttpPost("update")], body { DisciplineCode, DisciplineName, DisciplineDesc, FlagActive } (gate :324-389, #136).
//     - Body rỗng hoàn toàn -> ServiceException("strJson null") (:333-340) -> mã mặc định Mst_Discipline_Update (400).
//     - Ft_Cols_Upd cố định ở gate (:353-357): DisciplineName, DisciplineDesc, FlagActive -> luôn cập nhật 3 cột; thiếu FlagActive -> '0' (StdFlag).
//     - Chuẩn hoá dữ liệu input (:34668-34674):
//       DisciplineCode = StdParam (Trim+ToUpper, rỗng -> null);
//       DisciplineName = StdStr (Trim+Normalize, rỗng -> null);
//       DisciplineDesc = StdStr (Trim+Normalize, rỗng -> null);
//       FlagActive = StdFlag ("1" -> "1", còn lại kể cả null/thiếu/rỗng -> "0").
//     - Kiểm tra THEO THỨ TỰ:
//       (1) Mst_Discipline_CheckDB(ma, "1") (:34684-34690): mã rỗng hoặc không tìm thấy trong DB -> ErridnLoyalty.Mst_Discipline_CheckDB_DisciplineCodeNotFound.
//       (2) DisciplineName rỗng sau StdStr (:34694-34704) -> ErridnLoyalty.Mst_Discipline_UpdateX_InvalidDisciplineName.
//     - Lưu (:34713-34726): DisciplineName, DisciplineDesc, FlagActive (3 cột trong Ft_Cols_Upd) + LogLUDTimeUTC (UtcNow) + LogLUBy ("port").
//       NetworkID và DisciplineCode KHÔNG đổi.
//     - 1 transaction lưu DB (:34770 _cf.db.BeginTransaction()), lỗi -> rollback + PortResult.Fail.
//     - Thành công: gate :375-376 SetSuccess + Data = null.
//
// Bỏ qua có chủ đích (loại Đ):
//   - CheckGatewayAuthentication: gate :35, :117, :182, :256, :343, API :40-45, :114-119, :188-193
//   - CheckAuthorize: gate :36, :118, :183, :257, :344 (hệ thống đích chưa có hệ phân quyền)
//   - Ghi RequestLog/ReturnLog: MasterData.cs:34152-34159, 34229-34236, 34470-34477, 34543-34550, 34773-34780, 34849-34856
//   - Sys_Access_CheckDenyV30, Sys_User_CheckAuthentication
// ============================================================================================

/// <summary>Port 1:1 <c>Mst_DisciplineSearch</c> — tham số lọc + phân trang của màn (Mst_DisciplineController.cs:26, 53-58).</summary>
public class MstDisciplineSearchReq
{
    public string? OrgID { get; set; }               // Tổ chức — gate nhận (:26, :56) nhưng strWhereClause_Mst_Discipline (:523-538) KHÔNG dùng OrgID -> không lọc
    public string? KeyWord { get; set; }             // Từ khoá tìm theo tên hình thức kỷ luật (DisciplineName) — :26, :55, :530
    public string? FlagActive { get; set; }          // Cờ hoạt động, toán tử '=' — :26, :57, :534
    public int Ft_PageIndex { get; set; } = 0;       // Trang (0-based) — :26, :47
    public int Ft_PageSize { get; set; } = 10;       // Số dòng/trang — :26, :48
}

/// <summary>Port 1:1 <c>GetByDisciplineCode</c> — body chỉ có DisciplineCode (Mst_DisciplineController.cs:108-171).</summary>
public class MstDisciplineGetByDisciplineCodeReq
{
    public string? DisciplineCode { get; set; }       // Mã hình thức kỷ luật cần lấy — :108, :128-133
}

/// <summary>Port 1:1 <c>Create</c> — body { DisciplineCode, DisciplineName, DisciplineDesc } (gate Mst_DisciplineController.cs:237-253, #135).</summary>
public class MstDisciplineCreateReq
{
    public string? DisciplineCode { get; set; }       // Mã hình thức kỷ luật (client gửi nhưng bị ghi đè bởi seq)
    public string? DisciplineName { get; set; }       // Tên hình thức kỷ luật
    public string? DisciplineDesc { get; set; }       // Mô tả hình thức kỷ luật
}

/// <summary>Port 1:1 <c>Update</c> — body { DisciplineCode, DisciplineName, DisciplineDesc, FlagActive } (gate Mst_DisciplineController.cs:324-389, #136).</summary>
public class MstDisciplineUpdateReq
{
    public string? DisciplineCode { get; set; }       // Mã hình thức kỷ luật — :34671
    public string? DisciplineName { get; set; }       // Tên hình thức kỷ luật (StdStr) — :34672
    public string? DisciplineDesc { get; set; }       // Mô tả (StdStr) — :34673
    public string? FlagActive { get; set; }           // Cờ hoạt động (StdFlag, "1" -> "1", khác -> "0") — :34674
}

[ApiController]
[Route("api/mst_discipline")]
[Produces("application/json")]
public class Mst_DisciplineControllerPort : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public Mst_DisciplineControllerPort(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] MstDisciplineSearchReq? req)
    {
        var r = req ?? new MstDisciplineSearchReq();
        var pageIndex = Math.Max(0, r.Ft_PageIndex);
        var pageSize = r.Ft_PageSize;

        // Search gọi GetXAsync với start = PageIndex * PageSize, count = PageSize (giữ nguyên hành vi cũ nguồn :50-51, :70-71).
        var start = (long)pageIndex * pageSize;
        var (itemCount, rows) = await GetXAsync(r.KeyWord, r.FlagActive, start, pageSize);

        // PageSize=0 -> DataList rỗng (nguồn: nFilterRecordEnd = start-1, SQL biz + gate :81-89).
        // PageCount = 0 khi DataList rỗng, ngược lại ceil(ItemCount / ps) với ps = PageSize (PageSize=0 thì 10) — gate :81-89.
        var ps = pageSize == 0 ? 10 : pageSize;
        var pageCount = rows.Count == 0 ? 0 : (itemCount % ps == 0 ? itemCount / ps : itemCount / ps + 1);
        return Ok(new { Success = true, Data = new PageInfo<object>(rows, itemCount, pageCount, pageIndex, pageSize) });
    }

    /// <summary>
    /// Port 1:1 <c>Mst_DisciplineController.GetAllActive</c> (nguồn gate Mst_DisciplineController.cs:173-233, #134).
    /// Nguồn gọi <c>strWhereClause_Mst_Discipline_By_DisciplineCode</c> với <c>FlagActive = "1"</c> (:193-196) ⇒ lọc
    /// <c>FlagActive = '1'</c> (:195, toán tử '=' tại :551).
    /// Không body: RecordStart = "0", RecordCount = "123456000" (nguồn Ft_RecordStart/Ft_RecordCount :208-209).
    /// Trả <c>Ok(new { Success = true, Data = ... })</c> — Data là MẢNG TRẦN, rỗng thì <c>null</c>
    /// (nguồn :192 khởi tạo null, :215-218 chỉ gán khi có dòng, :220 trả thẳng lst_Mst_Discipline).
    /// </summary>
    [HttpPost("getallactive")]
    public async Task<IActionResult> GetAllActive()
    {
        // Nguồn: Ft_RecordStart = "0", Ft_RecordCount = "123456000" (gate :208-209).
        // FlagActive = "1" (gate :195) ⇒ lọc toán tử '=' (:551); không body.
        var (_, rows) = await GetXAsync(null, "1", 0, 123456000);

        // Nguồn :220: Data = lst_Mst_Discipline (mảng trần); rỗng ⇒ null (:192, :215-218).
        return Ok(new { Success = true, Data = rows.Count > 0 ? rows : null });
    }

    // ============================================================================================
    // GetByDisciplineCode — nguồn Mst_DisciplineController.cs:108-171.
    // Gate: nếu DisciplineCode rỗng thì KHÔNG truy vấn (:128), objMst_Discipline giữ null (:127) ⇒ Data = null (:158).
    // Ngược lại gán DisciplineCode (:130-133) rồi gọi strWhereClause_Mst_Discipline_By_DisciplineCode (:540-555)
    // (lọc `DisciplineCode = 'x'` — KHÔNG truyền FlagActive nên hàng FlagActive '0' VẪN lấy được).
    // Ft_RecordStart = "0", Ft_RecordCount = "1" (:145-146) ⇒ lấy đúng 1 dòng, trả Lst_Mst_Discipline[0] (:152-155).
    // ============================================================================================
    [HttpPost("getbydisciplinecode")]
    public async Task<IActionResult> GetByDisciplineCode([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] MstDisciplineGetByDisciplineCodeReq? req)
    {
        var disciplineCode = req?.DisciplineCode;

        // DisciplineCode rỗng ⇒ nguồn không truy vấn (:128) và trả Data = null (:158).
        if (string.IsNullOrWhiteSpace(disciplineCode))
        {
            return Ok(new { Success = true, Data = (object?)null });
        }

        // Lấy 1 dòng theo DisciplineCode (Ft_RecordStart = "0", Ft_RecordCount = "1" — :145-146).
        var (_, rows) = await GetXAsync(null, null, 0, 1, disciplineCodeEqual: disciplineCode);

        // Nguồn trả Lst_Mst_Discipline[0] khi có dòng, ngược lại null (:152-155).
        return Ok(new { Success = true, Data = rows.Count > 0 ? rows[0] : null });
    }

    // ============================================================================================
    // Create — nguồn Mst_DisciplineController.cs:237-320 (gate) -> MstDisciplineController.cs:91-162 (API)
    //   -> MasterData.cs:34555-34630 (WAS_Mst_Discipline_Create) -> :34434-34553 (Mst_Discipline_Create)
    //   -> :34333-34432 (Mst_Discipline_CreateX).
    //
    // GIỮ ĐÚNG NGUỒN:
    //   * Body JSON: { DisciplineCode, DisciplineName, DisciplineDesc } (gate :246-248).
    //   * Body rỗng hoàn toàn -> ServiceException("strJson null") (:246-253) -> mã mặc định Mst_Discipline_Create (KHÔNG tăng seq).
    //   * seq tăng cả khi lỗi: Lấy mã sequence trước khi lưu (gate :267-288, SeqType.DisciplineCode — Seq.cs: bang Seq_DisciplineCode,
    //     format "{0}DISCIPLINECODE.{3}.{1:00000}{2}", max 100000) qua bảng Seq_DisciplineCode trong transaction RIÊNG đã commit độc lập.
    //     Do đó sequence tăng MỖI LẦN GỌI vượt qua bước body rỗng, KỂ CẢ KHI LƯU LỖI SAU ĐÓ.
    //   * Mã từ seq ghi đè mã client gửi: objMst_Discipline.DisciplineCode = strDisciplineCode (:287) — client gửi gì cũng bị bỏ qua.
    //   * Kiểm tra hợp lệ:
    //     1) Mã rỗng -> Mst_Discipline_CreateX_InvalidDisciplineCode (:34374-34384, không xảy ra vì lấy từ seq).
    //     2) Mst_Discipline_CheckDB(ma, "0") -> Mst_Discipline_CheckDB_DisciplineCodeExist (:34386-34392, không xảy ra vì seq mới).
    //     3) DisciplineName rỗng sau StdStr -> Mst_Discipline_CreateX_InvalidDisciplineName (:34394-34404).
    //   * Lưu: 1 transaction duy nhất (:34467 _cf.db.BeginTransaction()), lỗi -> rollback + PortResult.Fail. Thành công -> Data = null (:306-307).
    //   * NetworkID lấy từ cấu hình: IConfiguration["Biz_NetworkID"] ?? "1001" (Common.cs:124 nNetworkID).
    //   * LogLUBy = "port" (GAP nguồn lấy từ session user), LogLUDTimeUTC = DateTime.UtcNow.
    // ============================================================================================
    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] MstDisciplineCreateReq? req)
    {
        // 0. Body rỗng / null: gate :246-253 ném Exception "strJson null" -> mã mặc định Mst_Discipline_Create.
        // Bước này xảy ra TRƯỚC khi gọi sequence nên KHÔNG tăng seq.
        if (req == null)
        {
            return PortResult.Fail("Mst_Discipline_Create", "Check.strJson", null);
        }

        // Tách nghiệp vụ thành CreateXAsync(name, desc) để dùng lại nếu có Import sau.
        // Mã từ seq ghi đè mã client; seq tăng cả khi lỗi nghiệp vụ phía sau (seq tiêu).
        var err = await CreateXAsync(req.DisciplineName, req.DisciplineDesc);
        if (err != null)
        {
            return err;
        }

        // Thành công: trả Data = null (gate Mst_DisciplineController.cs:306-307)
        return Ok(new { Success = true, Data = (object?)null });
    }

    /// <summary>
    /// Nghiệp vụ tạo 1 hình thức kỷ luật — tách riêng để dùng lại cho Create và Import (nếu có sau).
    /// 1 transaction duy nhất cho bước lưu DB (MasterData.cs:34467 _cf.db.BeginTransaction()).
    /// Trình tự:
    ///   1. Lấy mã sequence mới từ Seq_DisciplineCode trong transaction riêng đã commit độc lập (PortSeq.NextAsync).
    ///      Mã từ seq ghi đè mã client; seq tăng cả khi lỗi (nếu bước kiểm tra hoặc lưu sau đó thất bại thì seq vẫn bị tiêu).
    ///   2. Mở transaction lưu DB.
    ///   3. Chuẩn hoá DisciplineName (StdStr), DisciplineDesc (StdStr), strDisciplineCode (StdParam từ seq).
    ///   4. Kiểm tra tên rỗng -> ErridnLoyalty.Mst_Discipline_CreateX_InvalidDisciplineName (rollback, seq đã tiêu).
    ///   5. Lưu bản ghi Mst_Discipline với FlagActive = '1', NetworkID từ cấu hình, commit transaction.
    /// </summary>
    private async Task<IActionResult?> CreateXAsync(string? name, string? desc)
    {
        // 1. Lấy mã sequence trong transaction RIÊNG đã commit độc lập (Seq.cs:28-41, gate Mst_DisciplineController.cs:267-288).
        // Format: "{0}DISCIPLINECODE.{3}.{1:00000}{2}", maxSeq: 100000.
        // Mã từ seq ghi đè mã client gửi; seq tăng cả khi lỗi vì transaction của seq đã commit trước khi vào transaction lưu.
        var seq = await PortSeq.NextAsync<Seq_DisciplineCode>(_db);
        var disciplineCode = PortSeq.Build("{0}DISCIPLINECODE.{3}.{1:00000}{2}", "", seq, 100000, "", DateTime.UtcNow);

        // 2. Mở transaction chính cho nghiệp vụ lưu (MasterData.cs:34467 _cf.db.BeginTransaction())
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // 3. Chuẩn hoá input (MasterData.cs:34363-34365)
            // DisciplineCode: StdParam (Trim + ToUpper)
            // DisciplineName: StdStr (Trim + Normalize, rỗng -> null)
            // DisciplineDesc: StdStr (Trim + Normalize, rỗng -> null)
            var strDisciplineCode = StdParam(disciplineCode);
            var strDisciplineName = StdStr(name);
            var strDisciplineDesc = StdStr(desc);

            // Kiểm tra mã rỗng (MasterData.cs:34374-34384) — phòng ngừa, không xảy ra với mã từ seq
            if (string.IsNullOrEmpty(strDisciplineCode))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_Discipline_CreateX_InvalidDisciplineCode", "Check.strDisciplineCode", strDisciplineCode);
            }

            // Kiểm tra mã đã tồn tại (MasterData.cs:34386-34392) — Mst_Discipline_CheckDB(ma, "0")
            var exists = await _db.Set<Mst_Discipline>().AnyAsync(x =>
                EF.Functions.Like((x.DisciplineCode ?? "").ToLower(), PortQuery.Equal(strDisciplineCode), PortQuery.Esc));
            if (exists)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_Discipline_CheckDB_DisciplineCodeExist", "Check.DisciplineCode", strDisciplineCode);
            }

            // 4. Kiểm tra tên rỗng (MasterData.cs:34394-34404) -> Mst_Discipline_CreateX_InvalidDisciplineName
            if (string.IsNullOrEmpty(strDisciplineName))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_Discipline_CreateX_InvalidDisciplineName", "Check.DisciplineName", strDisciplineName);
            }

            // 5. Lưu DB Mst_Discipline (MasterData.cs:34414-34429)
            // NetworkID = cấu hình Biz_NetworkID (Common.cs:124 -> IConfiguration["Biz_NetworkID"] ?? "1001")
            var networkId = _config["Biz_NetworkID"] ?? "1001";
            var now = DateTime.UtcNow;

            var entity = new Mst_Discipline
            {
                DisciplineCode = strDisciplineCode,
                NetworkID = networkId,
                DisciplineName = strDisciplineName,
                DisciplineDesc = strDisciplineDesc,
                FlagActive = "1", // TConst.Flag.Active
                LogLUDTimeUTC = now,
                LogLUBy = "port"  // GAP: nguồn lấy từ strWAUserCode
            };

            _db.Set<Mst_Discipline>().Add(entity);
            await _db.SaveChangesAsync();

            // Commit transaction an toàn (MasterData.cs:34516 DBUtils.CommitSafety)
            await tx.CommitAsync();
            return null; // Thành công
        }
        catch (Exception ex)
        {
            // Lỗi -> rollback transaction và trả mã lỗi mặc định Mst_Discipline_Create (MasterData.cs:34524, 34452)
            await tx.RollbackAsync();
            return PortResult.Fail("Mst_Discipline_Create", "Exception", ex.Message);
        }
    }

    // ============================================================================================
    // Update — nguồn Mst_DisciplineController.cs:324-389 (gate) -> MstDisciplineController.cs:165-236 (API)
    //   -> MasterData.cs:34861-34939 (WAS_Mst_Discipline_Update) -> :34731-34859 (Mst_Discipline_Update)
    //   -> :34632-34729 (Mst_Discipline_UpdateX) -> Mst_Discipline_CheckDB.
    //
    // GIỮ ĐÚNG NGUỒN:
    //   * Body JSON: { DisciplineCode, DisciplineName, DisciplineDesc, FlagActive } (gate :332-335, biz :34671-34674).
    //   * Body rỗng / null: gate :333-340 ném Exception("strJson null") -> mã mặc định Mst_Discipline_Update (:34752).
    //   * Gate dựng Ft_Cols_Upd CỐ ĐỊNH (:353-357):
    //       "Mst_Discipline.DisciplineName,Mst_Discipline.DisciplineDesc,Mst_Discipline.FlagActive,"
    //     -> Ft_Cols_Upd cố định ở gate -> luôn cập nhật 3 cột; thiếu FlagActive -> '0' (StdFlag).
    //   * Chuẩn hoá dữ liệu input (:34668-34674):
    //       DisciplineCode = StdParam (Trim + ToUpper, rỗng -> null);
    //       DisciplineName = StdStr (Trim + Normalize, rỗng -> null);
    //       DisciplineDesc = StdStr (Trim + Normalize, rỗng -> null);
    //       FlagActive = StdFlag ("1" chỉ khi đúng "1", còn lại — kể cả thiếu/null/rỗng — là "0").
    //   * Kiểm tra THEO THỨ TỰ:
    //       (1) Mst_Discipline_CheckDB(ma, "1") (:34684-34690): mã rỗng hoặc không tìm thấy trong DB -> ErridnLoyalty.Mst_Discipline_CheckDB_DisciplineCodeNotFound.
    //       (2) DisciplineName rỗng sau StdStr (:34694-34704) -> ErridnLoyalty.Mst_Discipline_UpdateX_InvalidDisciplineName.
    //   * Lưu (:34713-34726): DisciplineName, DisciplineDesc, FlagActive (3 cột trong Ft_Cols_Upd) + LogLUDTimeUTC (UtcNow) + LogLUBy ("port").
    //       NetworkID và DisciplineCode KHÔNG đổi.
    //   * 1 transaction duy nhất cho toàn bộ nghiệp vụ (MasterData.cs:34770 _cf.db.BeginTransaction()), lỗi -> rollback + PortResult.Fail.
    //   * Thành công: gate :375-376 SetSuccess + Data = null.
    // ============================================================================================
    [HttpPost("update")]
    public async Task<IActionResult> Update([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] MstDisciplineUpdateReq? req)
    {
        // 1. Body rỗng/null: gate Mst_DisciplineController.cs:333-340 ném Exception("strJson null") -> mã mặc định Mst_Discipline_Update (:34752).
        if (req == null)
        {
            return PortResult.Fail("Mst_Discipline_Update", "Check.strJson", null);
        }

        // 2. Mở 1 transaction cho toàn bộ nghiệp vụ cập nhật (MasterData.cs:34770 _cf.db.BeginTransaction()).
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // 3. Ft_Cols_Upd cố định ở gate (:353-357) rồi biz StdParam (ToUpper) + Contains (:34668-34679).
            //    Ft_Cols_Upd cố định ở gate -> luôn cập nhật 3 cột; thiếu FlagActive -> '0' (StdFlag).
            var strFt_Cols_Upd = "Mst_Discipline.DisciplineName,Mst_Discipline.DisciplineDesc,Mst_Discipline.FlagActive,";
            strFt_Cols_Upd = StdParam(strFt_Cols_Upd) ?? "";
            bool bUpd_DisciplineName = strFt_Cols_Upd.Contains("Mst_Discipline.DisciplineName".ToUpperInvariant());
            bool bUpd_DisciplineDesc = strFt_Cols_Upd.Contains("Mst_Discipline.DisciplineDesc".ToUpperInvariant());
            bool bUpd_FlagActive = strFt_Cols_Upd.Contains("Mst_Discipline.FlagActive".ToUpperInvariant());

            // 4. Chuẩn hoá dữ liệu input (MasterData.cs:34671-34674):
            //    StdParam: Trim + ToUpper, rỗng -> null
            //    StdStr: Trim + Normalize, rỗng -> null
            //    StdFlag: "1" nếu đúng "1", mọi giá trị khác (kể cả null/thiếu/rỗng) -> "0"
            string? strDisciplineCode = StdParam(req.DisciplineCode);
            string? strDisciplineName = StdStr(req.DisciplineName);
            string? strDisciplineDesc = StdStr(req.DisciplineDesc);
            string strFlagActive = StdFlag(req.FlagActive);

            // 5. Kiểm tra theo thứ tự:
            // Bước 1: Mst_Discipline_CheckDB(ma, "1") (MasterData.cs:34684-34690 -> Mst_Discipline_CheckDB)
            // Nếu strDisciplineCode rỗng hoặc không tìm thấy dòng -> ErridnLoyalty.Mst_Discipline_CheckDB_DisciplineCodeNotFound
            if (string.IsNullOrEmpty(strDisciplineCode))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_Discipline_CheckDB_DisciplineCodeNotFound", "Check.DisciplineCode", strDisciplineCode);
            }

            var entity = await _db.Set<Mst_Discipline>().FirstOrDefaultAsync(x =>
                EF.Functions.Like((x.DisciplineCode ?? "").ToLower(), PortQuery.Equal(strDisciplineCode), PortQuery.Esc));

            if (entity == null)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_Discipline_CheckDB_DisciplineCodeNotFound", "Check.DisciplineCode", strDisciplineCode);
            }

            // Bước 2: DisciplineName rỗng sau StdStr (MasterData.cs:34694-34704)
            // -> ErridnLoyalty.Mst_Discipline_UpdateX_InvalidDisciplineName
            if (string.IsNullOrEmpty(strDisciplineName))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_Discipline_UpdateX_InvalidDisciplineName", "Check.DisciplineName", strDisciplineName);
            }

            // 6. Gán 3 cột + log (MasterData.cs:34713-34720):
            //    Ft_Cols_Upd cố định ở gate -> luôn cập nhật 3 cột; thiếu FlagActive -> '0' (StdFlag).
            if (bUpd_DisciplineName)
            {
                entity.DisciplineName = strDisciplineName;             // :34715 DisciplineName (StdStr)
            }
            if (bUpd_DisciplineDesc)
            {
                entity.DisciplineDesc = strDisciplineDesc;             // :34716 DisciplineDesc (StdStr)
            }
            if (bUpd_FlagActive)
            {
                entity.FlagActive = strFlagActive;                     // :34717 FlagActive (StdFlag)
            }
            entity.LogLUDTimeUTC = DateTime.UtcNow;                    // :34718 LogLUDTimeUTC = dtimeSys (UtcNow)
            entity.LogLUBy = "port";                                   // :34719 LogLUBy ("port" - đích chưa có session user, loại Đ)

            await _db.SaveChangesAsync();

            // 7. Commit transaction (MasterData.cs:34822 TDALUtils.DBUtils.CommitSafety).
            await tx.CommitAsync();

            // 8. Thành công: gate Mst_DisciplineController.cs:375-376 SetSuccess + Data = null.
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

    private static string? StdParam(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim().ToUpperInvariant();
    private static string? StdStr(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim().Normalize();
    private static string StdFlag(string? s)
    {
        if (s == null) return "0";
        var t = s.Trim();
        return t == "1" ? "1" : "0";
    }

    /// <summary>
    /// Lọc + đếm + chia trang theo DisciplineCode asc — dùng cho Search, GetByDisciplineCode, GetAllActive.
    /// Trả (itemCount, rows): itemCount = tổng số dòng sau lọc (MyCount, SQL :34020); rows = trang đã cắt kèm MyIdxSeq 0-based (:34050).
    /// </summary>
    private async Task<(int itemCount, List<object> rows)> GetXAsync(string? keyWord, string? flagActive, long recordStart, long recordCount, string? disciplineCodeEqual = null)
    {
        var q = _db.Set<Mst_Discipline>().AsNoTracking().AsQueryable();

        // KeyWord -> like '%kw%' trên DisciplineName (Mst_DisciplineController.cs:530).
        // Nguồn chạy SQL Server collation KHÔNG phân biệt hoa/thường ⇒ hạ chữ cả 2 phía rồi LIKE (bẫy #18/#31).
        if (!string.IsNullOrWhiteSpace(keyWord))
        {
            var kw = PortQuery.Contains(keyWord);
            q = q.Where(x => EF.Functions.Like((x.DisciplineName ?? "").ToLower(), kw, PortQuery.Esc));
        }

        // DisciplineCode -> `DisciplineCode = 'x'` (Mst_DisciplineController.cs:547 — strWhereClause_Mst_Discipline_By_DisciplineCode).
        // Nguồn dùng `=` trên SQL Server collation KHÔNG phân biệt hoa/thường ⇒ Like KHÔNG wildcard, hạ chữ 2 phía (PortQuery.Equal).
        // FlagActive không được truyền ở GetByDisciplineCode (:130-133) nên hàng FlagActive '0' vẫn lấy được.
        if (!string.IsNullOrWhiteSpace(disciplineCodeEqual))
        {
            var dc = PortQuery.Equal(disciplineCodeEqual);
            q = q.Where(x => EF.Functions.Like((x.DisciplineCode ?? "").ToLower(), dc, PortQuery.Esc));
        }

        // FlagActive -> toán tử '=' trên FlagActive (Mst_DisciplineController.cs:534) — KHÔNG IN.
        // Nguồn dùng '=' ⇒ so sánh bằng chuỗi không phân biệt hoa/thường bằng Like + PortQuery.Equal (không tách dấu phẩy).
        if (!string.IsNullOrWhiteSpace(flagActive))
        {
            var fa = PortQuery.Equal(flagActive);
            q = q.Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), fa, PortQuery.Esc));
        }

        // Chia trang theo DisciplineCode asc (Mst_Discipline_GetX SQL: MasterData.cs:34016 order by md.DisciplineCode asc).
        var ordered = q.OrderBy(x => x.DisciplineCode);
        var itemCount = await ordered.CountAsync();

        // MyIdxSeq = số thứ tự 0-based trong thứ tự DisciplineCode (identity(bigint, 0, 1) ở SQL nguồn :34010).
        // Lấy dòng [recordStart, recordStart+recordCount) theo thứ tự DisciplineCode.
        var page = recordCount <= 0
            ? new List<Mst_Discipline>()
            : await ordered.Skip((int)recordStart).Take((int)recordCount).ToListAsync();

        // Select cuối theo t.MyIdxSeq asc (MasterData.cs:34055 order by t.MyIdxSeq asc).
        var rows = page
            .Select((x, i) => (object)new
            {
                MyIdxSeq = recordStart + i,
                x.DisciplineCode,
                x.NetworkID,
                x.DisciplineName,
                x.DisciplineDesc,
                x.FlagActive,
                x.LogLUDTimeUTC,
                x.LogLUBy,
            })
            .ToList();

        return (itemCount, rows);
    }
}
