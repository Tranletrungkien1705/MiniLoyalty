using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniLoyalty.Data;
using MiniLoyalty.Models.Port;

namespace MiniLoyalty.Controllers.Port;

// ============================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — Mst_PromotionProgramController/Search (#205), GetAllActive (#206), GetByPrProgramCode (#207).
//
// Nguồn tra cứu (chỉ đọc):
//   - Gate controller: 13.ClientGate/V10/idn.Loyalty.WebAPI/Controllers/Mst_PromotionProgramController.cs
//     * Search: :32-113 (KHÔNG có giá trị mặc định cho Ft_PageIndex và Ft_PageSize -> mặc định kiểu int là 0)
//     * GetAllActive: :114-223 (gọi strWhereClause với FlagActive="1" nhưng hàm lọc chỉ xử lý KeyWord => FlagActive BỊ BỎ QUA, Ft_RecordStart="0", Ft_RecordCount="123456000")
//     * GetByPrProgramCode: :224-307 (lọc PrProgramCode =, không lọc FlagActive, Ft_RecordStart="0", Ft_RecordCount="1", xin cả 3 bảng)
//     * strWhereClause_Mst_PromotionProgram: :718-730 (chỉ lọc KeyWord -> PrProgramCode like '%kw%', OrgID KHÔNG dùng)
//     * strWhereClause_Mst_PromotionProgram_By_PrProgramCode: :732-743 (lọc PrProgramCode =)
//   - Service: WA_Mst_PromotionProgram_Get (11.BackEnd/V10/idn.Loyalty.Biz.Web/Controllers/MstPromotionProgramController.cs:22-90)
//   - Biz LIVE:
//     * WAS_Mst_PromotionProgram_Get: Master.Loyalty.cs:13997-14119
//     * Mst_PromotionProgram_Get: Master.Loyalty.cs:14120-14279
//     * Mst_PromotionProgram_GetX: Master.Loyalty.cs:14280-14543 (SQL draft :14340-14350, summary :14352-14354, filter :14356-14364, header :14390-14412, Dtl :14427-14440, Spec :14451-14464)
//   - DDL: 11.BackEnd/V10/05.Refs.Biz/Migrate/20221006.QUYNHNTN/20221026.CreateDatabase.sql
//     * Mst_PromotionProgram: :490-509 (11 cột), PK PrProgramCode tại :506-509.
//     * Mst_PromotionProgramDtl: :466-484 (10 cột), PK composite (PrProgramCode, CardType) tại :481-484.
//       FK PrProgramCode -> Mst_PromotionProgram (:540-547) — chỉ ghi comment.
//     * Mst_PromotionProgramSpec: :444-460 (8 cột), PK composite (DLCode, PrProgramCode) tại :457-460.
//       FK PrProgramCode -> Mst_PromotionProgram (:512-519) — chỉ ghi comment.
//       FK DLCode -> Mst_DealerCP.DLCPCode (:522-528) — chỉ ghi comment.
//
// ĐẶC TẢ NGHIỆP VỤ & QUY TẮC PORT:
//   1. Phân trang: Gate Search(OrgID, KeyWord, Ft_PageIndex, Ft_PageSize) không có giá trị mặc định trong signature.
//      Khi client gửi {} hoặc thiếu Ft_PageSize, int mặc định 0 -> Ft_PageSize = 0 -> RecordCount = 0 -> DataList rỗng.
//      PageCount chỉ tính khi DataList không rỗng (gate :88-96); nếu DataList rỗng thì PageCount = 0.
//      Khi tính PageCount, nếu pageSize == 0 thì fallback về PageSizeConfig (10).
//   2. Lọc:
//      - Search: Chỉ lọc KeyWord trên PrProgramCode (like '%kw%'). OrgID gate có nhận nhưng hàm strWhereClause không dùng.
//      - GetByPrProgramCode (#207): Lọc PrProgramCode = (so khớp bằng, không phân biệt hoa/thường qua PortQuery.Equal).
//        Không lọc FlagActive (PR03 inactive vẫn lấy). PrProgramCode rỗng -> không truy vấn, Data trả 3 trường null.
//   3. Tính cột ListDealer (Master.Loyalty.cs:14393-14406):
//      - FlagAllDL == "0": nối chuỗi các DLCode từ Mst_PromotionProgramSpec theo PrProgramCode phân cách dấu phẩy.
//        Nguồn dùng STUFF + FOR XML PATH không ORDER BY -> đích sắp DLCode asc để thứ tự xác định; nếu không có Spec -> null.
//      - FlagAllDL == "1": trả về 'Tất cả'.
//      - Khác ("0", "1"): trả về chuỗi rỗng ''.
//   4. Dtl và Spec (#207):
//      - Dtl: MyIdxSeq + 10 cột DDL, order by LogLUDTimeUTC desc (hòa trùng: sắp phụ theo CardType asc, ghi comment).
//      - Spec: MyIdxSeq + 8 cột DDL, order by LogLUDTimeUTC desc (hòa trùng: sắp phụ theo DLCode asc, ghi comment).
//   5. Ability (Master.Loyalty.cs:14337, 14408-14409, 14432-14433, 14456-14457):
//      Nguồn phân quyền myCache_Mst_PromotionProgram_ViewAbility_Get (System.Mix.cs:1655-1790) dựa trên user đăng nhập:
//      FlagRoot '1' -> thấy mọi chương trình; user đại lý FlagRoot '0' -> chỉ thấy chương trình có Spec.DLCode = đại lý hoặc FlagAllDL='1'.
//      Hệ thống đích chưa có session user / phân quyền -> coi như Root (thấy toàn bộ), ghi GAP.
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

[ApiController]
[Route("api/mst_promotionprogram")]
[Produces("application/json")]
public class Mst_PromotionProgramControllerPort : ControllerBase
{
    private readonly AppDbContext _db;

    public Mst_PromotionProgramControllerPort(AppDbContext db)
    {
        _db = db;
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
