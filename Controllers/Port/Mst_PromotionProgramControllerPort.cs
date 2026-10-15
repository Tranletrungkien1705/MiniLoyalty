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
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — Mst_PromotionProgramController/Search (#205).
//
// Nguồn tra cứu (chỉ đọc):
//   - Gate controller: 13.ClientGate/V10/idn.Loyalty.WebAPI/Controllers/Mst_PromotionProgramController.cs
//     * Search: :32-113 (KHÔNG có giá trị mặc định cho Ft_PageIndex và Ft_PageSize -> mặc định kiểu int là 0)
//     * strWhereClause_Mst_PromotionProgram: :718-730 (chỉ lọc KeyWord -> PrProgramCode like '%kw%', OrgID KHÔNG dùng)
//   - Service: WA_Mst_PromotionProgram_Get (11.BackEnd/V10/idn.Loyalty.Biz.Web/Controllers/MstPromotionProgramController.cs:22-90)
//   - Biz LIVE:
//     * WAS_Mst_PromotionProgram_Get: Master.Loyalty.cs:13997-14119
//     * Mst_PromotionProgram_Get: Master.Loyalty.cs:14120-14279
//     * Mst_PromotionProgram_GetX: Master.Loyalty.cs:14280-14543 (SQL draft :14340-14350, summary :14352-14354, filter :14356-14364, header :14390-14412)
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
//   2. Lọc: Chỉ lọc KeyWord trên PrProgramCode (like '%kw%'). OrgID gate có nhận nhưng hàm strWhereClause không dùng.
//   3. Tính cột ListDealer (Master.Loyalty.cs:14393-14406):
//      - FlagAllDL == "0": nối chuỗi các DLCode từ Mst_PromotionProgramSpec theo PrProgramCode phân cách dấu phẩy.
//        Nguồn dùng STUFF + FOR XML PATH không ORDER BY -> đích sắp DLCode asc để thứ tự xác định; nếu không có Spec -> null.
//      - FlagAllDL == "1": trả về 'Tất cả'.
//      - Khác ("0", "1"): trả về chuỗi rỗng ''.
//   4. Ability (Master.Loyalty.cs:14337, 14408-14409):
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
    /// Thực hiện 2 bước truy vấn của Mst_PromotionProgram_GetX (Master.Loyalty.cs:14280-14543):
    ///   Bước 1: Draft (#tbl_Mst_PromotionProgram_Filter_Draft) — lọc KeyWord like PrProgramCode, order by LogLUDTimeUTC desc, cắt trang.
    ///   Bước 2: Header + ListDealer (3 trường hợp: FlagAllDL='0' -> chuỗi DLCode từ Spec hoặc null nếu rỗng; '1' -> 'Tất cả'; khác -> '').
    /// GAP: Phân quyền đại lý user (myCache_Mst_PromotionProgram_ViewAbility_Get) tạm thời coi như Root (thấy toàn bộ).
    /// </summary>
    private async Task<(int itemCount, List<object> rows)> GetXAsync(string? keyWord, long recordStart, long recordCount)
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
            return (itemCount, new List<object>());
        }

        // Lấy danh sách Spec cho các PrProgramCode trong trang hiện tại
        var codes = draftPage.Select(x => x.Program.PrProgramCode).ToList();
        var specs = await _db.Set<Mst_PromotionProgramSpec>()
            .AsNoTracking()
            .Where(s => codes.Contains(s.PrProgramCode))
            .ToListAsync();

        // Gom nhóm theo PrProgramCode, sắp xếp DLCode asc (nguồn STUFF không ORDER BY -> đích sắp asc ghi comment)
        var specGroup = specs
            .GroupBy(s => s.PrProgramCode)
            .ToDictionary(
                g => g.Key,
                g => g.Select(s => s.DLCode)
                      .Where(c => !string.IsNullOrEmpty(c))
                      .OrderBy(c => c, StringComparer.Ordinal)
                      .ToList()
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

        return (itemCount, rows);
    }
}
