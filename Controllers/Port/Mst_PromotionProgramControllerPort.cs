using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MiniLoyalty.Data;
using MiniLoyalty.Models.Port;

namespace MiniLoyalty.Controllers.Port;

// ============================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — Mst_PromotionProgramController:
//   - Search (#205)
//   - GetAllActive (#206)
//   - GetByPrProgramCode (#207)
//   - Save (#208) — action GHI upsert xóa-ghi lại trong 1 transaction (ép FlagActive = '1')
//   - Update (#209) — action GHI cập nhật (dùng chung SaveCoreAsync, FlagActive theo client, nếu FlagActive='0' ghi đè EffDateEnd bằng DateTime.Now)
//   - Delete (#210) — action GHI xóa (1 transaction, kiểm tra EffDateStart <= now, xóa Spec, Dtl, Header)
//   - Export (#211) — action ĐỌC xuất Excel 7 cột từ điển GetImportDicColums(), phân trang toàn bộ 123456000, FlagActive bị bỏ qua
//   - ExportTemplate (#212) — action ĐỌC xuất file Excel mẫu 4 cột từ điển GetImportDicColumsTemplate(), danh sách rỗng 0 dòng, không đọc DB
//
// Nguồn tra cứu (chỉ đọc):
//   - Gate controller: 13.ClientGate/V10/idn.Loyalty.WebAPI/Controllers/Mst_PromotionProgramController.cs
//     * Search: :32-113 (KHÔNG có giá trị mặc định cho Ft_PageIndex và Ft_PageSize -> mặc định kiểu int là 0)
//     * GetAllActive: :114-223 (gọi strWhereClause với FlagActive="1" nhưng hàm lọc chỉ xử lý KeyWord => FlagActive BỊ BỎ QUA, Ft_RecordStart="0", Ft_RecordCount="123456000")
//     * GetByPrProgramCode: :224-307 (lọc PrProgramCode =, không lọc FlagActive, Ft_RecordStart="0", Ft_RecordCount="1", xin cả 3 bảng)
//     * Save: :311-402 (body rỗng -> "strJson null" -> Mst_PromotionProgram_Save. Khối Get Seq :345-366 BỊ COMMENT -> PrProgramCode do client gửi.
//       Gate ép FlagActive = '1' tại :384. Header null -> NullReference -> Mst_PromotionProgram_Save. Thành công Data = null).
//     * Update: :406-502 (y hệt Save, gọi cùng WA_Mst_PromotionProgram_Save -> SaveX. Khác Save ở 2 điểm:
//       1. KHÔNG ép FlagActive = '1' mà dùng FlagActive client gửi (StdFlag: chỉ '1' là 1, thiếu/khác -> '0').
//       2. :480-484 nếu StdFlag(FlagActive) == "0" (kể cả khi không gửi) -> EffDateEnd = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") - giờ máy chủ, không UTC).
//     * Delete: :506-569 (PrProgramCode rỗng -> ném ServiceException("invalid_BirthPolicyNo", "invalid_BirthPolicyNo") :555;
//       ngược lại gọi WA_Mst_PromotionProgram_Save với FlagIsDelete = "1", Data = null).
//     * Export: :573-656 (body { OrgID, KeyWord, FlagActive }, strWhereClause chỉ lọc KeyWord -> FlagActive và OrgID BỊ BỎ QUA.
//       Ft_RecordStart="0", Ft_RecordCount="123456000". Nếu rỗng ném ServiceException "Không có dữ liệu" -> 400 ErridnLoyalty.Mst_PromotionProgram_Export.
//       Từ điển GetImportDicColums() :691-702 gồm 7 cột. Tên file prefix và sheet "Mst_PromotionProgram" :632-633).
//     * ExportTemplate: :658-690 (danh sách rỗng List<Mst_PromotionProgram>, từ điển GetImportDicColumsTemplate() :704-713 gồm 4 cột,
//       prefix và sheet "Mst_PromotionProgram" :670-671, Data { Url } :673-676, không kiểm quyền, không đọc DB).
//     * strWhereClause_Mst_PromotionProgram: :718-730 (chỉ lọc KeyWord -> PrProgramCode like '%kw%', OrgID KHÔNG dùng)
//     * strWhereClause_Mst_PromotionProgram_By_PrProgramCode: :732-743 (lọc PrProgramCode =)
//   - Service: WA_Mst_PromotionProgram_Get (11.BackEnd/V10/idn.Loyalty.Biz.Web/Controllers/MstPromotionProgramController.cs:22-90)
//              WA_Mst_PromotionProgram_Save (11.BackEnd/V10/idn.Loyalty.Biz.Web/Controllers/MstPromotionProgramController.cs:93-163)
//   - Biz LIVE:
//     * WAS_Mst_PromotionProgram_Get: Master.Loyalty.cs:13997-14119
//     * Mst_PromotionProgram_Get: Master.Loyalty.cs:14120-14279
//     * Mst_PromotionProgram_GetX: Master.Loyalty.cs:14280-14543
//     * WAS_Mst_PromotionProgram_Save: Master.Loyalty.cs:13893-13995 (Lst Dtl/Spec null -> rỗng; FlagIsDelete không gửi -> bIsDelete = false)
//     * Mst_PromotionProgram_Save: Master.Loyalty.cs:13736-13891 (1 transaction duy nhất, commit :13854, rollback :13862)
//     * Mst_PromotionProgram_SaveX: Master.Loyalty.cs:13179-13734
//   - DDL: 11.BackEnd/V10/05.Refs.Biz/Migrate/20221006.QUYNHNTN/20221026.CreateDatabase.sql
//     * Mst_PromotionProgram: :490-509 (11 cột), PK PrProgramCode tại :506-509.
//     * Mst_PromotionProgramDtl: :466-484 (10 cột), PK composite (PrProgramCode, CardType) tại :481-484.
//       FK PrProgramCode -> Mst_PromotionProgram (:540-547) — chỉ ghi comment.
//     * Mst_PromotionProgramSpec: :444-460 (8 cột), PK composite (DLCode, PrProgramCode) tại :457-460.
//       FK PrProgramCode -> Mst_PromotionProgram (:512-519) — chỉ ghi comment.
//       FK DLCode -> Mst_DealerCP.DLCPCode (:522-528) — chỉ ghi comment.
//
// ĐẶC TẢ NGHIỆP VỤ & QUY TẮC PORT SAVE (#208) & UPDATE (#209):
//   1. Không sinh seq: Khối Get Seq (gate :345-366, :441-461) BỊ COMMENT ở nguồn -> PrProgramCode do client gửi lên.
//      Chuẩn hoá bằng StdParam (Trim().ToUpperInvariant(), rỗng -> null). Thiếu hoặc rỗng -> Mst_PromotionProgram_Save.
//   2. Phân biệt Save và Update:
//      - Save (#208): Gate ép FlagActive = '1' (gate :384). Giữ nguyên EffDateEnd do client gửi.
//      - Update (#209): Không ép FlagActive = '1', dùng FlagActive client gửi (StdFlag: chỉ '1' là 1, thiếu/khác -> '0').
//        Nếu StdFlag(FlagActive) == "0" (kể cả khi không gửi) -> EffDateEnd bị ghi đè bằng DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") (giờ máy chủ, không UTC).
//      - Cả Save và Update: Header null gây NullReferenceException tại gate -> Mst_PromotionProgram_Save.
//      - CheckDB (:13245-13276) so sánh PrProgramCode với "1" là mã vô nghĩa / dead code, bỏ qua.
//      - PrProgramName rỗng -> Mst_PromotionProgram_SaveX_InvalidPrProgramName (:13285-13295).
//      - Nhánh kiểm EffDateStart (:13300-13330) chỉ chạy khi xóa (bIsDelete = true) -> không chạy ở Save/Update.
//      - EffDateStart / EffDateEnd null hoặc sai định dạng date -> Mst_PromotionProgram_Save.
//   3. Dtl: Qty, Remark, Unit giữ nguyên chế độ ""; CardType chuẩn hoá bằng StdParam; FlagActive StdFlag (thiếu -> '0').
//      DDL Mst_PromotionProgramDtl khai FK CardType sai bảng (REFERENCES Mst_CardTypeDtl) — GAP, không kiểm tra CardType tồn tại.
//      Trùng CardType trong danh sách Dtl -> vi phạm PK composite -> Mst_PromotionProgram_Save.
//   4. Spec: DLCode chuẩn hoá bằng StdParam; Remark giữ nguyên chế độ ""; FlagActive gán cứng TConst.PRMCRStatus.Pending = "PENDING" (:13516).
//      FK DLCode -> Mst_DealerCP: giả lập kiểm tra tồn tại DLCode trong Mst_DealerCP. Không có -> Mst_PromotionProgram_Save.
//      Trùng DLCode trong danh sách Spec -> vi phạm PK composite -> Mst_PromotionProgram_Save.
//   5. Upsert xóa - ghi lại trong 1 transaction (:13562-13706): Xóa Spec cũ, Dtl cũ, Header cũ của cùng PrProgramCode rồi insert mới.
//      Nếu mã chưa tồn tại: lệnh DELETE xóa 0 dòng, lệnh INSERT thêm mới bản ghi (tạo mới giống Save).
//      Lỗi bất kỳ -> rollback + PortResult.Fail("Mst_PromotionProgram_Save", ...). Thành công Data = null (:389, :489).
//   6. GAP: OrgID/NetworkID/LogLUBy từ session user UI (đích: NetworkID lấy từ IConfiguration["Biz_NetworkID"] ?? "1001",
//      OrgID lấy từ IConfiguration["Biz_OrgID"], LogLUBy = "port"); FK CardType Dtl; phân quyền ability/gate; RequestLog/ReturnLog.
// ============================================================================================

/// <summary>
/// Tham số request Search — port 1:1 từ tham số gate Mst_PromotionProgramController.Search (gate :32).
/// Chú ý: int mặc định 0, không có giá trị mặc định trong signature nguồn.
/// </summary>
public class MstPromotionProgramSearchReq
{
    public string? OrgID { get; set; }               // Tổ chức — gate nhận (:32) nhưng strWhereClause (:718-730) KHÔNG dùng để lọc
    public string? KeyWord { get; set; }             // Từ khoá tìm theo mã chương trình (PrProgramCode like '%kw%') — :724-727
    public int Ft_PageIndex { get; set; } = 0;       // Trang (0-based) — nguồn :32, :55
    public int Ft_PageSize { get; set; } = 0;        // Số dòng/trang — nguồn :32, :56 (mặc định 0, thiếu -> DataList rỗng)
}

/// <summary>Port 1:1 <c>GetByPrProgramCode</c> — body nhận PrProgramCode (Mst_PromotionProgramController.cs:224).</summary>
public class MstPromotionProgramGetByPrProgramCodeReq
{
    public string? PrProgramCode { get; set; }
}

/// <summary>Port 1:1 <c>Delete</c> — body nhận PrProgramCode (Mst_PromotionProgramController.cs:506, #210).</summary>
public class MstPromotionProgramDeleteReq
{
    public string? PrProgramCode { get; set; }
}

/// <summary>Port 1:1 <c>Export</c> — body nhận OrgID, KeyWord, FlagActive (Mst_PromotionProgramController.cs:573, 594-599, #211).</summary>
public class MstPromotionProgramExportReq
{
    public string? OrgID { get; set; }               // Tổ chức — gate nhận (:573, :597) nhưng strWhereClause_Mst_PromotionProgram (:718-730) KHÔNG dùng OrgID -> không lọc
    public string? KeyWord { get; set; }             // Từ khoá tìm theo mã chương trình (PrProgramCode like '%kw%') — :573, :596, :724-727
    public string? FlagActive { get; set; }          // Cờ hoạt động — gate nhận (:573, :598) nhưng strWhereClause_Mst_PromotionProgram (:718-730) KHÔNG lọc FlagActive -> BỊ BỎ QUA
}

/// <summary>
/// Port 1:1 <c>Save</c> request — body nhận Mst_PromotionProgram, Lst_Mst_PromotionProgramDtl, Lst_Mst_PromotionProgramSpec (gate :319-322, #208).
/// </summary>
public class MstPromotionProgramSaveReq
{
    public MstPromotionProgramSaveHeaderReq? Mst_PromotionProgram { get; set; }
    public List<MstPromotionProgramSaveDtlReq>? Lst_Mst_PromotionProgramDtl { get; set; }
    public List<MstPromotionProgramSaveSpecReq>? Lst_Mst_PromotionProgramSpec { get; set; }
}

public class MstPromotionProgramSaveHeaderReq
{
    public string? PrProgramCode { get; set; }
    public string? PrProgramName { get; set; }
    public string? EffDateStart { get; set; }
    public string? EffDateEnd { get; set; }
    public string? Remark { get; set; }
    public string? FlagAllDL { get; set; }
    public string? FlagActive { get; set; }
}

public class MstPromotionProgramSaveDtlReq
{
    public string? CardType { get; set; }
    [System.Text.Json.Serialization.JsonNumberHandling(System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString)]
    public double? Qty { get; set; }
    public string? Unit { get; set; }
    public string? FlagActive { get; set; }
    public string? Remark { get; set; }
}

public class MstPromotionProgramSaveSpecReq
{
    public string? DLCode { get; set; }
    public string? Remark { get; set; }
    public string? FlagActive { get; set; }
}

[ApiController]
[Route("api/mst_promotionprogram")]
[Produces("application/json")]
public class Mst_PromotionProgramControllerPort : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;

    public Mst_PromotionProgramControllerPort(AppDbContext db, IConfiguration config, IWebHostEnvironment env)
    {
        _db = db;
        _config = config;
        _env = env;
    }

    /// <summary>
    /// Port 1:1 action Search của Mst_PromotionProgramController (gate :32-113).
    /// </summary>
    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] MstPromotionProgramSearchReq? req)
    {
        var r = req ?? new MstPromotionProgramSearchReq();
        var pageIndex = Math.Max(0, r.Ft_PageIndex);
        var pageSize = r.Ft_PageSize;

        // RecordStart = pageIndex * pageSize, RecordCount = pageSize (gate :76-77).
        var start = (long)pageIndex * pageSize;
        var (itemCount, rows) = await GetXAsync(r.KeyWord, start, pageSize);

        // PageSize=0 -> DataList rỗng (nguồn: nFilterRecordEnd = start - 1 = -1 < MyIdxSeq 0, gate :88-96).
        // PageCount chỉ tính khi DataList không rỗng: ceil(itemCount / ps) với ps = pageSize (nếu ps == 0 thì dùng 10).
        var ps = pageSize == 0 ? 10 : pageSize;
        var pageCount = rows.Count == 0 ? 0 : (itemCount % ps == 0 ? itemCount / ps : itemCount / ps + 1);

        return Ok(new { Success = true, Data = new PageInfo<object>(rows, itemCount, pageCount, pageIndex, pageSize) });
    }

    /// <summary>
    /// Port 1:1 <c>Mst_PromotionProgramController.GetAllActive</c> (nguồn gate Mst_PromotionProgramController.cs:114-223).
    ///
    /// CẢNH BÁO BẪY NGUỒN (FlagActive bị bỏ qua):
    /// Nguồn gate (:140-144) gọi <c>strWhereClause_Mst_PromotionProgram</c> với <c>FlagActive = "1"</c> và <c>OrgID</c>.
    /// Tuy nhiên hàm <c>strWhereClause_Mst_PromotionProgram</c> (:718-730) CHỈ xử lý <c>data.KeyWord</c> (tìm theo PrProgramCode).
    /// Hoàn toàn không có nhánh nào lọc FlagActive hoặc OrgID => <c>FlagActive = "1"</c> BỊ BỎ QUA, KHÔNG LỌC.
    /// Do đó GetAllActive trả về TẤT CẢ chương trình khuyến mại trong hệ thống, kể cả inactive (FlagActive = '0').
    ///
    /// Không nhận body: Ft_RecordStart = "0", Ft_RecordCount = "123456000" (gate :156-157).
    /// Khối Ext/tree (:166-198) bị comment ở nguồn => không port.
    /// Dùng lại GetXAsync của Search (#205): MyIdxSeq + 11 cột + ListDealer (cùng GetX, cùng ability coi như Root).
    /// Trả về mảng trần: <c>Ok(new { Success = true, Data = rows.Count > 0 ? rows : null })</c> (gate :201, :207).
    /// </summary>
    [HttpPost("getallactive")]
    public async Task<IActionResult> GetAllActive()
    {
        // Nguồn: Ft_RecordStart = "0", Ft_RecordCount = "123456000" (gate Mst_PromotionProgramController.cs:156-157).
        // FlagActive = "1" bị bỏ qua do strWhereClause (:718-730) chỉ lọc KeyWord -> truyền keyWord = null để không lọc.
        var (_, rows) = await GetXAsync(null, 0, 123456000);

        // Nguồn :203-210: Data = listMst_PromotionProgram (mảng trần, null nếu rỗng theo :137, :163, :201).
        return Ok(new { Success = true, Data = rows.Count > 0 ? rows : null });
    }

    /// <summary>
    /// Port 1:1 <c>Mst_PromotionProgramController.GetByPrProgramCode</c> (nguồn gate Mst_PromotionProgramController.cs:224-307, #207).
    /// Body JSON nhận { PrProgramCode }, EmptyBodyBehavior.Allow.
    /// PrProgramCode rỗng -> KHÔNG truy vấn, Data = { Mst_PromotionProgram: null, Lst_Mst_PromotionProgramDtl: null, Lst_Mst_PromotionProgramSpec: null } (gate :248, :289-294).
    /// Ngược lại lọc PrProgramCode = (Like + PortQuery.Equal, :738); không lọc FlagActive -> chương trình inactive như PR03 vẫn lấy được.
    /// Ft_RecordStart = "0", Ft_RecordCount = "1" (gate :265-266).
    /// Xin CẢ 3 bảng (Rt_Cols_Mst_PromotionProgram = "*", Rt_Cols_Mst_PromotionProgramDtl = "*", Rt_Cols_Mst_PromotionProgramSpec = "*", gate :269-271).
    /// Kết quả:
    ///   - Mst_PromotionProgram: header[0] (MyIdxSeq + 11 cột + ListDealer) hoặc null nếu không tìm thấy (:275-278).
    ///   - Lst_Mst_PromotionProgramDtl: danh sách Dtl (MyIdxSeq + 10 cột Dtl) hoặc null nếu rỗng (:279-282, :292).
    ///   - Lst_Mst_PromotionProgramSpec: danh sách Spec (MyIdxSeq + 8 cột Spec) hoặc null nếu rỗng (:283-286, :293).
    /// </summary>
    [HttpPost("getbyprprogramcode")]
    public async Task<IActionResult> GetByPrProgramCode(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] MstPromotionProgramGetByPrProgramCodeReq? req)
    {
        var code = req?.PrProgramCode;

        // PrProgramCode rỗng -> nguồn không truy vấn (:248) và trả cả 3 trường null (:245-247, :289-294).
        if (string.IsNullOrWhiteSpace(code))
        {
            return Ok(new
            {
                Success = true,
                Data = new
                {
                    Mst_PromotionProgram = (object?)null,
                    Lst_Mst_PromotionProgramDtl = (object?)null,
                    Lst_Mst_PromotionProgramSpec = (object?)null
                }
            });
        }

        // Lấy 1 dòng theo PrProgramCode (Ft_RecordStart = "0", Ft_RecordCount = "1" — gate :265-266).
        // getDtl = true, getSpec = true -> xin thêm cả Dtl và Spec (gate :269-271).
        // Không lọc FlagActive (gate :738 chỉ lọc PrProgramCode) -> chương trình inactive vẫn lấy được.
        var (_, rows, dtlRows, specRows) = await GetXAsync(null, 0, 1, prProgramCodeEqual: code, getDtl: true, getSpec: true);

        // Nguồn gate :275-286, :289-294:
        var objMst_PromotionProgram = rows.Count > 0 ? rows[0] : null;
        var lst_Mst_PromotionProgramDtl = dtlRows.Count > 0 ? dtlRows : null;
        var lst_Mst_PromotionProgramSpec = specRows.Count > 0 ? specRows : null;

        return Ok(new
        {
            Success = true,
            Data = new
            {
                Mst_PromotionProgram = objMst_PromotionProgram,
                Lst_Mst_PromotionProgramDtl = lst_Mst_PromotionProgramDtl,
                Lst_Mst_PromotionProgramSpec = lst_Mst_PromotionProgramSpec
            }
        });
    }

    /// <summary>
    /// Port 1:1 <c>Mst_PromotionProgramController.Save</c> (nguồn gate Mst_PromotionProgramController.cs:311-402, #208).
    /// Chuỗi gọi: gate Save -> API WA_Mst_PromotionProgram_Save (MstPromotionProgramController.cs:93-163)
    /// -> BIZ WAS_Mst_PromotionProgram_Save (Master.Loyalty.cs:13893-13995)
    /// -> BIZ Mst_PromotionProgram_Save (Master.Loyalty.cs:13736-13891)
    /// -> BIZ Mst_PromotionProgram_SaveX (Master.Loyalty.cs:13179-13734).
    ///
    /// Hành vi Save: ép FlagActive = '1' (gate Mst_PromotionProgramController.cs:384).
    /// Giữ nguyên EffDateEnd do client gửi lên.
    /// Gọi hàm dùng chung <see cref="SaveCoreAsync(MstPromotionProgramSaveReq?, bool)"/> với forceActive = true.
    /// </summary>
    [HttpPost("save")]
    public async Task<IActionResult> Save(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] MstPromotionProgramSaveReq? req)
    {
        return await SaveCoreAsync(req, forceActive: true);
    }

    /// <summary>
    /// Port 1:1 <c>Mst_PromotionProgramController.Update</c> (nguồn gate Mst_PromotionProgramController.cs:406-502, #209).
    /// Chuỗi gọi: gate Update -> API WA_Mst_PromotionProgram_Save (MstPromotionProgramController.cs:93-163)
    /// -> BIZ WAS_Mst_PromotionProgram_Save (Master.Loyalty.cs:13893-13995)
    /// -> BIZ Mst_PromotionProgram_Save (Master.Loyalty.cs:13736-13891)
    /// -> BIZ Mst_PromotionProgram_SaveX (Master.Loyalty.cs:13179-13734).
    ///
    /// KHÁC Save ở 2 điểm:
    /// 1. KHÔNG ép FlagActive = '1': dùng FlagActive client gửi (SaveX StdFlag: chỉ "1" là 1, thiếu/khác -> "0").
    /// 2. Gate :480-484: nếu StdFlag(FlagActive) == "0" (KỂ CẢ khi client không gửi FlagActive)
    ///    thì EffDateEnd = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") (giờ máy chủ, không UTC) — ghi đè EffDateEnd client gửi.
    ///
    /// Gọi hàm dùng chung <see cref="SaveCoreAsync(MstPromotionProgramSaveReq?, bool)"/> với forceActive = false.
    /// </summary>
    [HttpPost("update")]
    public async Task<IActionResult> Update(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] MstPromotionProgramSaveReq? req)
    {
        return await SaveCoreAsync(req, forceActive: false);
    }

    /// <summary>
    /// Port 1:1 action Delete của Mst_PromotionProgramController (gate Mst_PromotionProgramController.cs:506-569, #210).
    /// Chuỗi gọi: gate Delete -> API WA_Mst_PromotionProgram_Save (MstPromotionProgramController.cs:93-163)
    /// -> BIZ WAS_Mst_PromotionProgram_Save (Master.Loyalty.cs:13893-13995)
    /// -> BIZ Mst_PromotionProgram_Save (Master.Loyalty.cs:13736-13891)
    /// -> BIZ Mst_PromotionProgram_SaveX (Master.Loyalty.cs:13179-13734, bIsDelete = true).
    ///
    /// Hành vi tầng gate (Mst_PromotionProgramController.cs:525-556):
    ///   - PrProgramCode rỗng / null / whitespace (hoặc body rỗng, `{}`) -> ném ServiceException("invalid_BirthPolicyNo", "invalid_BirthPolicyNo") (:555,
    ///     chép nhầm tên từ BirthPolicy — GIỮ NGUYÊN mã nguồn) -> HTTP 400 ErridnLoyalty.invalid_BirthPolicyNo.
    ///   - Ngược lại gọi DeleteXAsync(code) trong 1 transaction duy nhất.
    ///   - Thành công: gate :550-551 SetSuccess + Data = null -> HTTP 200 { Success: true, Data: null }.
    /// </summary>
    [HttpPost("delete")]
    public async Task<IActionResult> Delete(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] MstPromotionProgramDeleteReq? req)
    {
        // 1. Kiểm tra mã rỗng tại gate (Mst_PromotionProgramController.cs:525, :555):
        //    PrProgramCode rỗng / body rỗng -> throw new ServiceException("invalid_BirthPolicyNo", "invalid_BirthPolicyNo")
        if (string.IsNullOrWhiteSpace(req?.PrProgramCode))
        {
            return PortResult.Fail("invalid_BirthPolicyNo", "Check.PrProgramCode", req?.PrProgramCode);
        }

        // 2. Thực hiện nghiệp vụ xóa qua DeleteXAsync (1 transaction) — tách riêng để DeleteMultiple sau dùng lại.
        var err = await DeleteXAsync(req.PrProgramCode);
        if (err != null)
        {
            return err;
        }

        // 3. Thành công: gate Mst_PromotionProgramController.cs:550-551 SetSuccess + Data = null
        return Ok(new
        {
            Success = true,
            Data = (object?)null
        });
    }

    /// <summary>
    /// Port 1:1 <c>Mst_PromotionProgramController.Export</c> (nguồn gate Mst_PromotionProgramController.cs:573-656, #211).
    /// Chuỗi gọi: gate Export -> API WA_Mst_PromotionProgram_Get (MstPromotionProgramController.cs:22-90)
    /// -> BIZ WAS_Mst_PromotionProgram_Get (Master.Loyalty.cs:13997-14119)
    /// -> BIZ Mst_PromotionProgram_Get (Master.Loyalty.cs:14120-14279)
    /// -> BIZ Mst_PromotionProgram_GetX (Master.Loyalty.cs:14280-14543).
    ///
    /// GIỮ ĐÚNG NGUỒN:
    ///   * Tham số nhận qua body: OrgID, KeyWord, FlagActive (Mst_PromotionProgramController.cs:573, 594-599).
    ///   * CẢNH BÁO BẪY NGUỒN (FlagActive bị bỏ qua):
    ///     Hàm strWhereClause_Mst_PromotionProgram (:718-730) CHỈ xử lý data.KeyWord (PrProgramCode like '%kw%').
    ///     Hoàn toàn không có nhánh nào lọc FlagActive hoặc OrgID => FlagActive và OrgID BỊ BỎ QUA, KHÔNG LỌC.
    ///   * Phân trang xuất toàn bộ: Ft_RecordStart = "0", Ft_RecordCount = "123456000" (:612-613), Rt_Cols_Mst_PromotionProgram = "*" (:616).
    ///   * Thứ tự: GetXAsync giống Search #205, sắp theo LogLUDTimeUTC desc (Master.Loyalty.cs:14349).
    ///   * Nếu danh sách rỗng (rows == null || rows.Count == 0):
    ///     ném ServiceException("Không có dữ liệu") (:643) -> trả HTTP 400 ErridnLoyalty.Mst_PromotionProgram_Export.
    ///   * Từ điển cột: GetImportDicColums() (gate Mst_PromotionProgramController.cs:630, 691-702) gồm đúng 7 cột:
    ///       1. MyIdxSeq -> "Số thứ tự"
    ///       2. PrProgramCode -> "Mã chương trình"
    ///       3. PrProgramName -> "Tên chương trình"
    ///       4. EffDateStart -> "Ngày hiệu lực"
    ///       5. EffDateEnd -> "Ngày hết hạn"
    ///       6. ListDealer -> "Đại lý áp dụng"
    ///       7. FlagActive -> "Trạng thái"
    ///   * Sinh đường dẫn file xuất: BaseController.GenExcelExportFilePath (:632)
    ///     -> PortExcel.GenExcelExportFilePath(webRoot, "Mst_PromotionProgram").
    ///   * Tên sheet: "Mst_PromotionProgram" (:633).
    ///   * Ghi file qua PortExcel.ExportToExcelFromList(rows, dicColNames, physicalPath, "Mst_PromotionProgram") (:633).
    ///   * Trả URL tải file với Success = true, Data = { Url = PortExcel.GetRequestBaseAddress(Request) + virtualPath } (:634-638).
    ///
    /// Bỏ qua có chủ đích (loại Đ):
    ///   * CheckGatewayAuthentication, CheckAuthorize (:583-584)
    ///   * Sys_User_CheckAuthorize: Master.Loyalty.cs:1592-1606
    ///   * Ghi RequestLog/ReturnLog
    /// </summary>
    [HttpPost("export")]
    public async Task<IActionResult> Export(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] MstPromotionProgramExportReq? req)
    {
        req ??= new MstPromotionProgramExportReq();

        // Nguồn gate Mst_PromotionProgramController.cs:594-599:
        // strWhereClause_Mst_PromotionProgram (:718-730) CHỈ xử lý KeyWord (PrProgramCode like '%kw%').
        // OrgID và FlagActive truyền vào nhưng hàm lọc KHÔNG sử dụng -> FlagActive và OrgID BỊ BỎ QUA.
        // Ft_RecordStart = "0", Ft_RecordCount = "123456000", Rt_Cols_Mst_PromotionProgram = "*" (:612-616).
        var (_, rows) = await GetXAsync(req.KeyWord, 0, 123456000);

        // Nguồn :626, :643: nếu danh sách rỗng -> throw ServiceException("Không có dữ liệu") -> 400 ErridnLoyalty.Mst_PromotionProgram_Export
        if (rows == null || rows.Count == 0)
        {
            return PortResult.Fail("Mst_PromotionProgram_Export", "Message", "Không có dữ liệu");
        }

        // Tạo thư mục và đường dẫn file Excel xuất: BaseController.GenExcelExportFilePath (:632)
        var webRoot = _env.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
        {
            webRoot = Path.Combine(_env.ContentRootPath, "wwwroot");
        }

        // Lấy từ điển cột: Mst_PromotionProgramController.cs:630 gọi GetImportDicColums() (7 cột)
        var dicColNames = GetImportDicColums();
        var (physicalPath, virtualPath) = PortExcel.GenExcelExportFilePath(webRoot, "Mst_PromotionProgram");

        // Ghi danh sách ra file Excel: ExcelExport.ExportToExcelFromList (:633) với sheet "Mst_PromotionProgram"
        PortExcel.ExportToExcelFromList(rows, dicColNames, physicalPath, "Mst_PromotionProgram");

        // Trả URL tải file: Data = { Url = GetRequestBaseAddress() + url } (:635-638)
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
    // ExportTemplate — nguồn gate Mst_PromotionProgramController.cs:658-689 (#212).
    // Action này KHÔNG kiểm quyền ở nguồn (không có CheckGatewayAuthentication và không có CheckAuthorize).
    // Không gọi tầng service, không truy vấn cơ sở dữ liệu.
    // Khởi tạo danh sách rỗng kiểu Mst_PromotionProgram (:666).
    // Lấy từ điển 4 cột mẫu từ GetImportDicColumsTemplate (:668, :704-713) gồm 4 cột:
    //   1. PrProgramCode -> "Mã chương trình (*)"
    //   2. PrProgramName -> "Tên chương trình (*)"
    //   3. EffDateStart -> "Ngày hiệu lực (*)"
    //   4. EffDateEnd -> "Ngày hết hạn (*)"
    // Sinh đường dẫn file mẫu qua PortExcel.GenExcelExportFilePath(_env.WebRootPath, "Mst_PromotionProgram") (:670).
    // Xuất file Excel qua PortExcel.ExportToExcelFromList(list, dicColNames, physicalPath, "Mst_PromotionProgram") (:671).
    // Trả về Success = true, Data = { Url = PortExcel.GetRequestBaseAddress(Request) + virtualPath } (:672-676).
    // Không có nhánh báo lỗi khi danh sách rỗng (khác với Export).
    // (Controller nguồn KHÔNG có action Import -> không kiểm upload lại).
    // ============================================================================================
    [HttpPost("exporttemplate")]
    public IActionResult ExportTemplate()
    {
        // Khởi tạo danh sách rỗng kiểu Mst_PromotionProgram (gate Mst_PromotionProgramController.cs:666)
        var list = new List<Mst_PromotionProgram>();

        // Lấy từ điển 4 cột mẫu import (gate Mst_PromotionProgramController.cs:668)
        var dicColNames = GetImportDicColumsTemplate();

        // Xác định thư mục webRoot và sinh đường dẫn file (gate Mst_PromotionProgramController.cs:670)
        var webRoot = _env.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
        {
            webRoot = Path.Combine(_env.ContentRootPath, "wwwroot");
        }

        var (physicalPath, virtualPath) = PortExcel.GenExcelExportFilePath(webRoot, "Mst_PromotionProgram");

        // Ghi danh sách rỗng ra file Excel với tên sheet Mst_PromotionProgram (gate Mst_PromotionProgramController.cs:671)
        PortExcel.ExportToExcelFromList(list, dicColNames, physicalPath, "Mst_PromotionProgram");

        // Trả URL tải file với Success = true (gate Mst_PromotionProgramController.cs:672-676)
        return Ok(new
        {
            Success = true,
            Data = new
            {
                Url = PortExcel.GetRequestBaseAddress(Request) + virtualPath
            }
        });
    }

    #region DicColums
    /// <summary>
    /// Từ điển 7 cột xuất Excel: gate Mst_PromotionProgramController.cs:630, 691-702 (GetImportDicColums).
    /// Giữ đúng thứ tự 7 cột từ nguồn:
    /// 1. MyIdxSeq -> "Số thứ tự"
    /// 2. PrProgramCode -> "Mã chương trình"
    /// 3. PrProgramName -> "Tên chương trình"
    /// 4. EffDateStart -> "Ngày hiệu lực"
    /// 5. EffDateEnd -> "Ngày hết hạn"
    /// 6. ListDealer -> "Đại lý áp dụng"
    /// 7. FlagActive -> "Trạng thái"
    /// </summary>
    private static Dictionary<string, string> GetImportDicColums()
    {
        return new Dictionary<string, string>
        {
            { "MyIdxSeq", "Số thứ tự" },
            { "PrProgramCode", "Mã chương trình" },
            { "PrProgramName", "Tên chương trình" },
            { "EffDateStart", "Ngày hiệu lực" },
            { "EffDateEnd", "Ngày hết hạn" },
            { "ListDealer", "Đại lý áp dụng" },
            { "FlagActive", "Trạng thái" }
        };
    }

    /// <summary>
    /// Từ điển 4 cột mẫu Excel: gate Mst_PromotionProgramController.cs:668, 704-713 (GetImportDicColumsTemplate).
    /// Giữ đúng thứ tự 4 cột từ nguồn:
    /// 1. PrProgramCode -> "Mã chương trình (*)"
    /// 2. PrProgramName -> "Tên chương trình (*)"
    /// 3. EffDateStart -> "Ngày hiệu lực (*)"
    /// 4. EffDateEnd -> "Ngày hết hạn (*)"
    /// </summary>
    private static Dictionary<string, string> GetImportDicColumsTemplate()
    {
        return new Dictionary<string, string>
        {
            { "PrProgramCode", "Mã chương trình (*)" },
            { "PrProgramName", "Tên chương trình (*)" },
            { "EffDateStart", "Ngày hiệu lực (*)" },
            { "EffDateEnd", "Ngày hết hạn (*)" }
        };
    }
    #endregion

    /// <summary>
    /// Nghiệp vụ xóa 1 chương trình khuyến mại — 1 transaction duy nhất.
    /// Nguồn:
    ///   - WA_Mst_PromotionProgram_Save (MstPromotionProgramController.cs:93-163)
    ///   - WAS_Mst_PromotionProgram_Save (Master.Loyalty.cs:13893-13995)
    ///   - Mst_PromotionProgram_Save (Master.Loyalty.cs:13736-13891)
    ///   - Mst_PromotionProgram_SaveX (Master.Loyalty.cs:13179-13734, bIsDelete = true).
    ///
    /// Quy trình nghiệp vụ:
    ///   1. Chuẩn hoá mã qua StdParam (Trim + ToUpper, rỗng -> null).
    ///   2. CheckDB (:13245-13253): So sánh chuỗi DB bằng Like + PortQuery.Equal.
    ///      Nếu KHÔNG tồn tại trong DB -> goto MyCodeLabel_Done = THÀNH CÔNG, không làm gì (:13255-13258).
    ///   3. CẢNH BÁO HÀNH VI NGUỒN (:13298-13331):
    ///      Nếu chương trình có EffDateStart > now (DateTime.UtcNow) -> ném lỗi ErridnLoyalty.Mst_PromotionProgram_SaveX_InvalidEffDateStart.
    ///      Hành vi nguồn: CHỈ cho phép xóa chương trình ĐÃ BẮT ĐẦU (EffDateStart <= now). Chương trình có ngày bắt đầu ở tương lai KHÔNG xóa được.
    ///   4. Xóa Spec, Dtl, Header cùng PrProgramCode trong 1 transaction (:13562-13588). Không insert mới (:13591 chỉ chạy khi !bIsDelete).
    ///
    /// Dùng chung cho Delete (#210) và DeleteMultiple sau này.
    /// </summary>
    private async Task<IActionResult?> DeleteXAsync(string? code)
    {
        // 1. Mở 1 transaction cho nghiệp vụ xóa (Master.Loyalty.cs:13776 _cf.db.BeginTransaction()).
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // 2. Chuẩn hoá mã qua StdParam: Trim, ToUpper, rỗng -> null (Master.Loyalty.cs:13230)
            var strPrProgramCode = StdParam(code);
            if (string.IsNullOrEmpty(strPrProgramCode))
            {
                // Nếu mã sau chuẩn hoá rỗng -> không tồn tại -> thành công không làm gì (Master.Loyalty.cs:13255-13258)
                return null;
            }

            var codeParam = PortQuery.Equal(strPrProgramCode);

            // 3. CheckDB (Master.Loyalty.cs:13245-13253):
            //    Tìm chương trình bằng Like + PortQuery.Equal (hạ chữ 2 phía, collation không phân biệt hoa thường)
            var program = await _db.Set<Mst_PromotionProgram>()
                .FirstOrDefaultAsync(h => EF.Functions.Like((h.PrProgramCode ?? "").ToLower(), codeParam, PortQuery.Esc));

            // Bước 1 nguồn: KHÔNG tồn tại -> goto MyCodeLabel_Done = THÀNH CÔNG, không làm gì (Master.Loyalty.cs:13255-13258).
            if (program == null)
            {
                return null;
            }

            // Bước 2 nguồn (Master.Loyalty.cs:13298-13331):
            // CẢNH BÁO HÀNH VI NGUỒN:
            // Nguồn kiểm tra nếu chương trình có EffDateStart > now (DateTime.UtcNow) -> ném Mst_PromotionProgram_SaveX_InvalidEffDateStart.
            // Nghĩa là: CHỈ xóa được chương trình ĐÃ BẮT ĐẦU (EffDateStart <= now). Chương trình ở tương lai KHÔNG xóa được.
            var nowUtc = DateTime.UtcNow;
            if (program.EffDateStart > nowUtc)
            {
                await tx.RollbackAsync();
                return PortResult.Fail(
                    "Mst_PromotionProgram_SaveX_InvalidEffDateStart",
                    "Check.PrProgramCode", strPrProgramCode,
                    "Check.EffDateStart", program.EffDateStart.ToString("yyyy-MM-dd HH:mm:ss"),
                    "Check.strToday", nowUtc.ToString("yyyy-MM-dd HH:mm:ss"));
            }

            // Bước 3 nguồn: Xóa Spec, Dtl, Header cùng PrProgramCode (Master.Loyalty.cs:13562-13588)
            // 3.1. Xóa Spec cũ (Master.Loyalty.cs:13563-13569)
            var specs = await _db.Set<Mst_PromotionProgramSpec>()
                .Where(s => EF.Functions.Like((s.PrProgramCode ?? "").ToLower(), codeParam, PortQuery.Esc))
                .ToListAsync();
            if (specs.Count > 0)
            {
                _db.Set<Mst_PromotionProgramSpec>().RemoveRange(specs);
            }

            // 3.2. Xóa Dtl cũ (Master.Loyalty.cs:13570-13576)
            var dtls = await _db.Set<Mst_PromotionProgramDtl>()
                .Where(d => EF.Functions.Like((d.PrProgramCode ?? "").ToLower(), codeParam, PortQuery.Esc))
                .ToListAsync();
            if (dtls.Count > 0)
            {
                _db.Set<Mst_PromotionProgramDtl>().RemoveRange(dtls);
            }

            // 3.3. Xóa Header cũ (Master.Loyalty.cs:13578-13584)
            _db.Set<Mst_PromotionProgram>().Remove(program);

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return null; // Thành công
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return PortResult.Fail("Mst_PromotionProgram_Save", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Hàm xử lý cốt lõi dùng chung cho Save (#208) và Update (#209).
    /// Nguồn:
    ///   - Gate Save: Mst_PromotionProgramController.cs:311-402 (ép FlagActive = '1' tại :384).
    ///   - Gate Update: Mst_PromotionProgramController.cs:406-502 (:480-484 không ép FlagActive; nếu StdFlag(FlagActive) == "0"
    ///     thì ghi đè EffDateEnd = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") — giờ máy chủ, không UTC).
    ///   - Service: WA_Mst_PromotionProgram_Save (MstPromotionProgramController.cs:93-163).
    ///   - Biz: WAS_Mst_PromotionProgram_Save (Master.Loyalty.cs:13893-13995)
    ///          -> Mst_PromotionProgram_Save (Master.Loyalty.cs:13736-13891)
    ///          -> Mst_PromotionProgram_SaveX (Master.Loyalty.cs:13179-13734).
    /// </summary>
    private async Task<IActionResult> SaveCoreAsync(MstPromotionProgramSaveReq? req, bool forceActive)
    {
        // 0. Body rỗng / null: gate :320-327 (Save) / :415-422 (Update) ném ServiceException("strJson null") -> mã mặc định Mst_PromotionProgram_Save.
        if (req == null)
        {
            return PortResult.Fail("Mst_PromotionProgram_Save", "Check.strJson", null);
        }

        // Header null (ví dụ `{}`): gate :384 (Save) / :480 (Update) truy cập objRQ_Mst_PromotionProgram.Mst_PromotionProgram.FlagActive
        // ném NullReferenceException -> mã mặc định Mst_PromotionProgram_Save.
        if (req.Mst_PromotionProgram == null)
        {
            return PortResult.Fail("Mst_PromotionProgram_Save", "Check.Mst_PromotionProgram", null);
        }

        var header = req.Mst_PromotionProgram;

        // Chuẩn hoá input header (Master.Loyalty.cs:13230-13236):
        var strPrProgramCode = StdParam(header.PrProgramCode);
        var strPrProgramName = StdStr(header.PrProgramName);
        var strRemark = StdStr(header.Remark);
        var strFlagAllDL = StdFlag(header.FlagAllDL);

        string strFlagActive;
        string? effDateEndStr;

        if (forceActive)
        {
            // Gate Save (Mst_PromotionProgramController.cs:384): ép FlagActive = '1'
            strFlagActive = "1";
            effDateEndStr = header.EffDateEnd;
        }
        else
        {
            // Gate Update (Mst_PromotionProgramController.cs:480-484):
            // Dùng FlagActive client gửi (StdFlag: chỉ "1" là 1, thiếu/khác -> "0").
            strFlagActive = StdFlag(header.FlagActive);
            if (strFlagActive == "0")
            {
                // Nguồn gate Mst_PromotionProgramController.cs:480-484: nếu StdFlag(FlagActive) == "0" (kể cả khi không gửi)
                // thì EffDateEnd = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") (giờ máy chủ, không UTC).
                // Ghi đè EffDateEnd client gửi.
                effDateEndStr = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            }
            else
            {
                effDateEndStr = header.EffDateEnd;
            }
        }

        // CheckDB (:13245-13276): Đoạn kiểm dtDB_Mst_PromotionProgram so sánh PrProgramCode với "1" là mã vô nghĩa / dead code, bỏ qua.

        // Kiểm tra PrProgramName (Master.Loyalty.cs:13285-13295): rỗng -> Mst_PromotionProgram_SaveX_InvalidPrProgramName
        if (string.IsNullOrEmpty(strPrProgramName))
        {
            return PortResult.Fail("Mst_PromotionProgram_SaveX_InvalidPrProgramName", "Check.PrProgramName", strPrProgramName);
        }

        // Nhánh kiểm EffDateStart (:13300-13330) chỉ chạy khi xóa (bIsDelete = true) -> KHÔNG chạy ở Save/Update.

        // Chuẩn hoá và kiểm tra Dtl (Master.Loyalty.cs:13407-13431):
        var dtlInputList = req.Lst_Mst_PromotionProgramDtl ?? new List<MstPromotionProgramSaveDtlReq>();
        var newDtls = new List<Mst_PromotionProgramDtl>();
        var seenCardTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var networkId = _config["Biz_NetworkID"] ?? "1001";
        var orgId = _config["Biz_OrgID"];
        var now = DateTime.UtcNow;

        foreach (var item in dtlInputList)
        {
            var cardType = StdParam(item.CardType);
            if (string.IsNullOrEmpty(cardType))
            {
                // CardType là PK composite NOT NULL -> lỗi Mst_PromotionProgram_Save
                return PortResult.Fail("Mst_PromotionProgram_Save", "Check.CardType", item.CardType);
            }

            // Kiểm tra trùng CardType trong danh sách Dtl -> vi phạm PK composite -> Mst_PromotionProgram_Save
            if (!seenCardTypes.Add(cardType))
            {
                return PortResult.Fail("Mst_PromotionProgram_Save", "Check.CardType", cardType);
            }

            // GAP: KHÔNG kiểm CardType trong Mst_CardType vì DDL khai sai bảng (REFERENCES Mst_CardTypeDtl)
            // Qty, Remark, Unit giữ nguyên chế độ "" (Remark KHÔNG trim)
            // FlagActive chuẩn hoá bằng StdFlag (thiếu hoặc khác '1' -> '0')
            newDtls.Add(new Mst_PromotionProgramDtl
            {
                PrProgramCode = strPrProgramCode ?? "",
                CardType = cardType,
                NetworkID = networkId,
                OrgID = orgId,
                Qty = item.Qty,
                Unit = item.Unit, // Giữ nguyên không trim
                FlagActive = StdFlag(item.FlagActive),
                Remark = item.Remark, // Giữ nguyên không trim
                LogLUDTimeUTC = now,
                LogLUBy = "port"
            });
        }

        // Chuẩn hoá và kiểm tra Spec (Master.Loyalty.cs:13493-13518):
        var specInputList = req.Lst_Mst_PromotionProgramSpec ?? new List<MstPromotionProgramSaveSpecReq>();
        var newSpecs = new List<Mst_PromotionProgramSpec>();
        var seenDLCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in specInputList)
        {
            var dlCode = StdParam(item.DLCode);
            if (string.IsNullOrEmpty(dlCode))
            {
                // DLCode là PK composite NOT NULL -> lỗi Mst_PromotionProgram_Save
                return PortResult.Fail("Mst_PromotionProgram_Save", "Check.DLCode", item.DLCode);
            }

            // Kiểm tra trùng DLCode trong danh sách Spec -> vi phạm PK composite -> Mst_PromotionProgram_Save
            if (!seenDLCodes.Add(dlCode))
            {
                return PortResult.Fail("Mst_PromotionProgram_Save", "Check.DLCode", dlCode);
            }

            // FK DLCode -> Mst_DealerCP (DDL FK_MstDealerCp_Mst_ProPrSpec):
            // Giả lập kiểm tra tồn tại DLCode trong Mst_DealerCP. Không tồn tại -> Mst_PromotionProgram_Save
            var dlCodeParam = PortQuery.Equal(dlCode);
            var dealerExists = await _db.Set<Mst_DealerCP>().AsNoTracking()
                .AnyAsync(x => EF.Functions.Like((x.DLCPCode ?? "").ToLower(), dlCodeParam, PortQuery.Esc));
            if (!dealerExists)
            {
                return PortResult.Fail("Mst_PromotionProgram_Save", "Check.DLCode", dlCode);
            }

            // Remark giữ nguyên chế độ "" (Remark KHÔNG trim)
            // Spec PENDING: FlagActive gán cứng TConst.PRMCRStatus.Pending = "PENDING" (Master.Loyalty.cs:13516, Const.Main.BE.cs:653)
            newSpecs.Add(new Mst_PromotionProgramSpec
            {
                DLCode = dlCode,
                PrProgramCode = strPrProgramCode ?? "",
                NetworkID = networkId,
                OrgID = orgId,
                FlagActive = "PENDING",
                Remark = item.Remark, // Giữ nguyên không trim
                LogLUDTimeUTC = now,
                LogLUBy = "port"
            });
        }

        // Kiểm tra PrProgramCode (cột NOT NULL / PK):
        if (string.IsNullOrEmpty(strPrProgramCode))
        {
            return PortResult.Fail("Mst_PromotionProgram_Save", "Check.PrProgramCode", header.PrProgramCode);
        }

        // Kiểm tra EffDateStart và EffDateEnd (cột MyDateTime NOT NULL trong DB):
        if (!TryParseEffDate(header.EffDateStart, out var effDateStartUtc))
        {
            return PortResult.Fail("Mst_PromotionProgram_Save", "Check.EffDateStart", header.EffDateStart);
        }
        if (!TryParseEffDate(effDateEndStr, out var effDateEndUtc))
        {
            return PortResult.Fail("Mst_PromotionProgram_Save", "Check.EffDateEnd", header.EffDateEnd);
        }

        // Cập nhật lại PrProgramCode cho các dòng Dtl và Spec sau khi đã validate PrProgramCode không null
        foreach (var d in newDtls) d.PrProgramCode = strPrProgramCode;
        foreach (var s in newSpecs) s.PrProgramCode = strPrProgramCode;

        // Upsert xoá - ghi lại trong 1 transaction (Master.Loyalty.cs:13562-13706):
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var codeParam = PortQuery.Equal(strPrProgramCode);

            // 1. Xoá Spec cũ của cùng PrProgramCode (Master.Loyalty.cs:13563-13569)
            var oldSpecs = await _db.Set<Mst_PromotionProgramSpec>()
                .Where(s => EF.Functions.Like((s.PrProgramCode ?? "").ToLower(), codeParam, PortQuery.Esc))
                .ToListAsync();
            if (oldSpecs.Count > 0)
            {
                _db.Set<Mst_PromotionProgramSpec>().RemoveRange(oldSpecs);
            }

            // 2. Xoá Dtl cũ của cùng PrProgramCode (Master.Loyalty.cs:13570-13576)
            var oldDtls = await _db.Set<Mst_PromotionProgramDtl>()
                .Where(d => EF.Functions.Like((d.PrProgramCode ?? "").ToLower(), codeParam, PortQuery.Esc))
                .ToListAsync();
            if (oldDtls.Count > 0)
            {
                _db.Set<Mst_PromotionProgramDtl>().RemoveRange(oldDtls);
            }

            // 3. Xoá Header cũ của cùng PrProgramCode (Master.Loyalty.cs:13578-13584)
            var oldHeader = await _db.Set<Mst_PromotionProgram>()
                .Where(h => EF.Functions.Like((h.PrProgramCode ?? "").ToLower(), codeParam, PortQuery.Esc))
                .ToListAsync();
            if (oldHeader.Count > 0)
            {
                _db.Set<Mst_PromotionProgram>().RemoveRange(oldHeader);
            }

            await _db.SaveChangesAsync();

            // 4. Insert Header mới (Master.Loyalty.cs:13596-13623)
            var newHeader = new Mst_PromotionProgram
            {
                PrProgramCode = strPrProgramCode,
                NetworkID = networkId,
                OrgID = orgId,
                PrProgramName = strPrProgramName,
                EffDateStart = effDateStartUtc,
                EffDateEnd = effDateEndUtc,
                FlagAllDL = strFlagAllDL,
                FlagActive = strFlagActive,
                Remark = strRemark,
                LogLUDTimeUTC = now,
                LogLUBy = "port"
            };
            _db.Set<Mst_PromotionProgram>().Add(newHeader);

            // 5. Insert Dtl mới (Master.Loyalty.cs:13629-13654)
            if (newDtls.Count > 0)
            {
                _db.Set<Mst_PromotionProgramDtl>().AddRange(newDtls);
            }

            // 6. Insert Spec mới (Master.Loyalty.cs:13659-13680)
            if (newSpecs.Count > 0)
            {
                _db.Set<Mst_PromotionProgramSpec>().AddRange(newSpecs);
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return Ok(new { Success = true, Data = (object?)null });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return PortResult.Fail("Mst_PromotionProgram_Save", "Exception", ex.Message);
        }
    }

    private static string? StdParam(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim().ToUpperInvariant();
    private static string? StdStr(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    private static string StdFlag(string? s) => s?.Trim() == "1" ? "1" : "0";

    private static bool TryParseEffDate(string? s, out DateTime dt)
    {
        dt = default;
        if (string.IsNullOrWhiteSpace(s)) return false;
        if (DateTime.TryParse(s, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out dt)
            || DateTime.TryParse(s, out dt))
        {
            if (dt >= new DateTime(1900, 1, 1) && dt <= new DateTime(2100, 1, 1, 23, 59, 59))
            {
                dt = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Overload giữ nguyên chữ ký cũ cho Search (#205) và GetAllActive (#206).
    /// </summary>
    private async Task<(int itemCount, List<object> rows)> GetXAsync(string? keyWord, long recordStart, long recordCount)
    {
        var (itemCount, rows, _, _) = await GetXAsync(keyWord, recordStart, recordCount, null, false, false);
        return (itemCount, rows);
    }

    /// <summary>
    /// Thực hiện các bước truy vấn của Mst_PromotionProgram_GetX (Master.Loyalty.cs:14280-14543):
    ///   Bước 1: Draft (#tbl_Mst_PromotionProgram_Filter_Draft) — lọc KeyWord like PrProgramCode, PrProgramCode = prProgramCodeEqual, order by LogLUDTimeUTC desc, cắt trang.
    ///   Bước 2: Header + ListDealer (3 trường hợp: FlagAllDL='0' -> chuỗi DLCode từ Spec hoặc null nếu rỗng; '1' -> 'Tất cả'; khác -> '').
    ///   Bước 3: Nếu getDtl = true, select chi tiết Dtl từ #tbl_Mst_PromotionProgram_Filter join Mst_PromotionProgramDtl theo PrProgramCode,
    ///           order by LogLUDTimeUTC desc (hòa trùng: sắp phụ theo CardType asc, ghi comment theo đề bài) (Master.Loyalty.cs:14427-14440).
    ///   Bước 4: Nếu getSpec = true, select đại lý chỉ định Spec từ #tbl_Mst_PromotionProgram_Filter join Mst_PromotionProgramSpec theo PrProgramCode,
    ///           order by LogLUDTimeUTC desc (hòa trùng: sắp phụ theo DLCode asc, ghi comment theo đề bài) (Master.Loyalty.cs:14451-14464).
    /// GAP: Phân quyền đại lý user (myCache_Mst_PromotionProgram_ViewAbility_Get) tạm thời coi như Root (thấy toàn bộ).
    /// </summary>
    private async Task<(int itemCount, List<object> rows, List<object> dtlRows, List<object> specRows)> GetXAsync(
        string? keyWord,
        long recordStart,
        long recordCount,
        string? prProgramCodeEqual = null,
        bool getDtl = false,
        bool getSpec = false)
    {
        // -----------------------------------------------------------------------------------------
        // BƯỚC 1: DRAFT (#tbl_Mst_PromotionProgram_Filter_Draft, Master.Loyalty.cs:14340-14350)
        // select distinct identity(bigint, 0, 1) MyIdxSeq, mppr.PrProgramCode, mppr.LogLUDTimeUTC
        // from Mst_PromotionProgram mppr where (1=1) zzB_Where_strFilter_zzE order by mppr.LogLUDTimeUTC desc
        // -----------------------------------------------------------------------------------------
        var q = _db.Set<Mst_PromotionProgram>().AsNoTracking().AsQueryable();

        // Lọc KeyWord: `PrProgramCode LIKE '%kw%'` (gate :724-727, strWhereClause_Mst_PromotionProgram).
        // OrgID không được đưa vào strWhereClause nên KHÔNG lọc theo OrgID.
        if (!string.IsNullOrWhiteSpace(keyWord))
        {
            var kw = PortQuery.Contains(keyWord);
            q = q.Where(x => EF.Functions.Like((x.PrProgramCode ?? "").ToLower(), kw, PortQuery.Esc));
        }

        // Lọc PrProgramCode: `PrProgramCode = 'code'` (gate :738, strWhereClause_Mst_PromotionProgram_By_PrProgramCode).
        // Nguồn dùng `=` trên SQL Server collation KHÔNG phân biệt hoa/thường ⇒ Like KHÔNG wildcard, hạ chữ 2 phía (PortQuery.Equal).
        if (!string.IsNullOrWhiteSpace(prProgramCodeEqual))
        {
            var pc = PortQuery.Equal(prProgramCodeEqual);
            q = q.Where(x => EF.Functions.Like((x.PrProgramCode ?? "").ToLower(), pc, PortQuery.Esc));
        }

        // Sắp xếp mppr.LogLUDTimeUTC desc (Master.Loyalty.cs:14349).
        // Hòa trùng LogLUDTimeUTC: sắp phụ theo PrProgramCode asc để thứ tự xác định.
        var draftList = await q
            .OrderByDescending(x => x.LogLUDTimeUTC)
            .ThenBy(x => x.PrProgramCode)
            .ToListAsync();

        // MyCount = Count(0) của bảng draft (#tbl_Mst_PromotionProgram_Filter_Draft, :14353).
        var itemCount = draftList.Count;

        // Cắt trang draft (#tbl_Mst_PromotionProgram_Filter, :14356-14364):
        // t.MyIdxSeq >= @nFilterRecordStart and t.MyIdxSeq <= @nFilterRecordEnd.
        // Khi recordCount <= 0 (pageSize=0) hoặc recordStart >= draftList.Count -> draftPage rỗng.
        var draftPage = (recordCount <= 0 || recordStart >= draftList.Count)
            ? new List<(long MyIdxSeq, Mst_PromotionProgram Program)>()
            : draftList
                .Select((p, idx) => (MyIdxSeq: (long)idx, Program: p))
                .Where(x => x.MyIdxSeq >= recordStart && x.MyIdxSeq < recordStart + recordCount)
                .ToList();

        // -----------------------------------------------------------------------------------------
        // BƯỚC 2: SELECT HEADER VÀ TÍNH ListDealer (Master.Loyalty.cs:14390-14412)
        // select t.MyIdxSeq, mppr.*, (case ... end) ListDealer
        // order by mppr.LogLUDTimeUTC desc
        // -----------------------------------------------------------------------------------------
        if (draftPage.Count == 0)
        {
            return (itemCount, new List<object>(), new List<object>(), new List<object>());
        }

        // Lấy danh sách Spec cho các PrProgramCode trong trang hiện tại
        var codes = draftPage.Select(x => x.Program.PrProgramCode).ToList();
        var codeMap = draftPage.ToDictionary(
            x => x.Program.PrProgramCode,
            x => x.MyIdxSeq,
            StringComparer.OrdinalIgnoreCase);

        List<Mst_PromotionProgramSpec> specs;
        if (codes.Count == 1)
        {
            var targetCode = codes[0];
            specs = await _db.Set<Mst_PromotionProgramSpec>()
                .AsNoTracking()
                .Where(s => EF.Functions.Like((s.PrProgramCode ?? "").ToLower(), PortQuery.Equal(targetCode), PortQuery.Esc))
                .ToListAsync();
        }
        else
        {
            specs = await _db.Set<Mst_PromotionProgramSpec>()
                .AsNoTracking()
                .Where(s => codes.Contains(s.PrProgramCode))
                .ToListAsync();
        }

        // Gom nhóm theo PrProgramCode, sắp xếp DLCode asc (nguồn STUFF không ORDER BY -> đích sắp asc ghi comment)
        var specGroup = specs
            .GroupBy(s => s.PrProgramCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.Select(s => s.DLCode)
                      .Where(c => !string.IsNullOrEmpty(c))
                      .OrderBy(c => c, StringComparer.Ordinal)
                      .ToList(),
                StringComparer.OrdinalIgnoreCase
            );

        var rows = draftPage
            .OrderByDescending(x => x.Program.LogLUDTimeUTC)
            .ThenBy(x => x.Program.PrProgramCode)
            .Select(x =>
            {
                var prog = x.Program;
                var flagAllDL = prog.FlagAllDL?.Trim();

                // Tính ListDealer (Master.Loyalty.cs:14393-14406):
                // 1) FlagAllDL == "0": chuỗi DLCode nối bằng ',', nếu không có Spec nào -> null (STUFF trả NULL).
                // 2) FlagAllDL == "1": 'Tất cả'.
                // 3) Khác: '' (chuỗi rỗng).
                string? listDealer = flagAllDL switch
                {
                    "0" => (specGroup.TryGetValue(prog.PrProgramCode, out var dlList) && dlList.Count > 0)
                        ? string.Join(",", dlList)
                        : null,
                    "1" => "Tất cả",
                    _ => ""
                };

                return (object)new
                {
                    x.MyIdxSeq,
                    prog.PrProgramCode,
                    prog.NetworkID,
                    prog.OrgID,
                    prog.PrProgramName,
                    prog.EffDateStart,
                    prog.EffDateEnd,
                    prog.FlagAllDL,
                    prog.FlagActive,
                    prog.Remark,
                    prog.LogLUDTimeUTC,
                    prog.LogLUBy,
                    ListDealer = listDealer,
                };
            })
            .ToList();

        // -----------------------------------------------------------------------------------------
        // BƯỚC 3: SELECT DTL (Master.Loyalty.cs:14427-14436)
        // select t.MyIdxSeq, mpprl.*
        // from #tbl_Mst_PromotionProgram_Filter t
        //   inner join #tbl_Mst_PromotionProgram_ViewAbility_Read tmppvr on t.PrProgramCode = tmppvr.PrProgramCode
        //   inner join Mst_PromotionProgramDtl mpprl on t.PrProgramCode = mpprl.PrProgramCode
        // order by mpprl.LogLUDTimeUTC desc
        // GAP: Bỏ qua tmppvr (phân quyền đại lý user), đích coi như Root (thấy toàn bộ).
        // -----------------------------------------------------------------------------------------
        var dtlRows = new List<object>();
        if (getDtl)
        {
            List<Mst_PromotionProgramDtl> dtlList;
            if (codes.Count == 1)
            {
                var targetCode = codes[0];
                dtlList = await _db.Set<Mst_PromotionProgramDtl>()
                    .AsNoTracking()
                    .Where(d => EF.Functions.Like((d.PrProgramCode ?? "").ToLower(), PortQuery.Equal(targetCode), PortQuery.Esc))
                    .ToListAsync();
            }
            else
            {
                dtlList = await _db.Set<Mst_PromotionProgramDtl>()
                    .AsNoTracking()
                    .Where(d => codes.Contains(d.PrProgramCode))
                    .ToListAsync();
            }

            // Sắp xếp Dtl theo mpprl.LogLUDTimeUTC desc (Master.Loyalty.cs:14436).
            // Hòa trùng LogLUDTimeUTC: sắp phụ theo CardType asc, ghi comment theo đề bài (Master.Loyalty.cs:14436).
            dtlRows = dtlList
                .OrderByDescending(d => d.LogLUDTimeUTC)
                .ThenBy(d => d.CardType)
                .Select(d => (object)new
                {
                    MyIdxSeq = codeMap.TryGetValue(d.PrProgramCode, out var seq) ? seq : 0L,
                    d.PrProgramCode,
                    d.CardType,
                    d.NetworkID,
                    d.OrgID,
                    d.Qty,
                    d.Unit,
                    d.FlagActive,
                    d.Remark,
                    d.LogLUDTimeUTC,
                    d.LogLUBy
                })
                .ToList();
        }

        // -----------------------------------------------------------------------------------------
        // BƯỚC 4: SELECT SPEC (Master.Loyalty.cs:14451-14460)
        // select t.MyIdxSeq, mpprs.*
        // from #tbl_Mst_PromotionProgram_Filter t
        //   inner join #tbl_Mst_PromotionProgram_ViewAbility_Read tmppvr on t.PrProgramCode = tmppvr.PrProgramCode
        //   inner join Mst_PromotionProgramSpec mpprs on t.PrProgramCode = mpprs.PrProgramCode
        // order by mpprs.LogLUDTimeUTC desc
        // GAP: Bỏ qua tmppvr (phân quyền đại lý user), đích coi như Root (thấy toàn bộ).
        // -----------------------------------------------------------------------------------------
        var specRows = new List<object>();
        if (getSpec)
        {
            // Sắp xếp Spec theo mpprs.LogLUDTimeUTC desc (Master.Loyalty.cs:14460).
            // Hòa trùng LogLUDTimeUTC: sắp phụ theo DLCode asc, ghi comment theo đề bài (Master.Loyalty.cs:14460).
            specRows = specs
                .OrderByDescending(s => s.LogLUDTimeUTC)
                .ThenBy(s => s.DLCode)
                .Select(s => (object)new
                {
                    MyIdxSeq = codeMap.TryGetValue(s.PrProgramCode, out var seq) ? seq : 0L,
                    s.DLCode,
                    s.PrProgramCode,
                    s.NetworkID,
                    s.OrgID,
                    s.FlagActive,
                    s.Remark,
                    s.LogLUDTimeUTC,
                    s.LogLUBy
                })
                .ToList();
        }

        return (itemCount, rows, dtlRows, specRows);
    }
}
