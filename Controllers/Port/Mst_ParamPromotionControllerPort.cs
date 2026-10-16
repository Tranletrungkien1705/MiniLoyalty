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
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — Mst_ParamPromotionController:
//   - Search (#183)
//   - GetAllActive (#184)
//
// Nguồn tra cứu (chỉ đọc):
//   - Gate controller: 13.ClientGate/V10/idn.Loyalty.WebAPI/Controllers/Mst_ParamPromotionController.cs
//     * Search: :23-103 (tham số KeyWord, FlagActive, Ft_PageIndex = 0, Ft_PageSize = 10).
//       Ft_RecordStart = (pageIndex * pageSize).ToString() (:66), Ft_RecordCount = pageSize.ToString() (:67).
//       OrgID lấy từ header (:35, :64) nhưng KHÔNG đưa vào where clause.
//       itemCount = Convert.ToInt32(objRT_Mst_ParamPromotion.MySummaryTable.MyCount) (:74-77).
//       PageCount chỉ tính khi Lst_Mst_ParamPromotion KHÔNG rỗng (:78-86, pageSize == 0 thì tạm dùng 10).
//     * strWhereClause_Mst_ParamPromotion: :281-298 — CHỈ xử lý FlagActive (tách dấu phẩy -> IN).
//       KeyWord KHÔNG lọc (không có nhánh if xử lý KeyWord trong hàm lọc).
//     * GetAllActive: :104-213 — KHÔNG tham số; gọi strWhereClause_Mst_ParamPromotion với FlagActive="1" (:130-134)
//       -> lọc FlagActive bằng toán tử IN (tách dấu phẩy). Ft_RecordStart="0", Ft_RecordCount="123456000"
//       (BaseController.cs:30-31). Data = listMst_ParamPromotion là MẢNG TRẦN; rỗng thì Data = null
//       (:127, :153-155, :191-199). Khối Ext/tree đã bị comment -> KHÔNG port.
//   - Service: WA_Mst_ParamPromotion_Get (11.BackEnd/V10/idn.Loyalty.Biz.Web/Controllers/MstParamPromotionController.cs:97-167)
//   - Biz LIVE:
//     * WAS_Mst_ParamPromotion_Get: Master.Loyalty.cs:12170-12272
//     * Mst_ParamPromotion_Get: Master.Loyalty.cs:12273-12426
//     * Mst_ParamPromotion_GetX: Master.Loyalty.cs:12427-12598
//   - DDL: 11.BackEnd/V10/05.Refs.Biz/Migrate/20221006.QUYNHNTN/20221026.CreateDatabase.sql
//     * Mst_ParamPromotion: :322-333 (10 cột), PK composite (PrProgramCode, ParamPrType) tại :337-340.
//       FK ParamPrType -> Mst_ParamPromotionType(ParamPrType) tại :364-371 — chỉ ghi comment.
//
// ĐẶC TẢ NGHIỆP VỤ SEARCH (#183):
//   1. Lọc FlagActive theo kiểu IN: tách dấu phẩy, trim, không phân biệt hoa/thường (gate :287-295).
//      KeyWord và OrgID KHÔNG lọc (gate :281-298 không có nhánh xử lý).
//   2. Draft: select distinct identity(bigint, 0, 1) MyIdxSeq, mpp.PrProgramCode, mpp.ParamPrType, mpp.LogLUDTimeUTC
//      from Mst_ParamPromotion mpp where (1=1) and (mpp.FlagActive in (...)) order by mpp.LogLUDTimeUTC desc
//      (Master.Loyalty.cs:12484-12494). MyIdxSeq 0-based.
//   3. Summary: select Count(0) MyCount from #tbl_Mst_ParamPromotion_Filter_Draft t (Master.Loyalty.cs:12497).
//   4. Filter phân trang: t.MyIdxSeq >= @nFilterRecordStart and t.MyIdxSeq <= @nFilterRecordEnd
//      với recordStart = pageIndex * pageSize, recordEnd = recordStart + pageSize - 1 (Master.Loyalty.cs:12501-12508).
//      Ft_PageSize = 0 -> recordCount = 0 -> nFilterRecordEnd = -1 -> 0 dòng khớp -> DataList rỗng [].
//   5. Select cuối: join Mst_ParamPromotion theo (PrProgramCode, ParamPrType),
//      LEFT JOIN Mst_PromotionProgram mppt on mpp.PrProgramCode = mppt.PrProgramCode để lấy PrProgramName
//      TỪ mppt.PrProgramName (KHÔNG phải cột PrProgramName của Mst_ParamPromotion) (Master.Loyalty.cs:12526-12544).
//      order by mpp.LogLUDTimeUTC desc.
//   6. drAbilityOfUser (:12468) lấy nhưng dòng gọi cache bị comment (:12480) -> KHÔNG port, ghi comment.
// ============================================================================================

/// <summary>
/// Tham số request Search — port 1:1 từ tham số gate Mst_ParamPromotionController.Search (gate :23).
/// </summary>
public class MstParamPromotionSearchReq
{
    public string? KeyWord { get; set; }             // Từ khoá — gate nhận (:23, :52) nhưng strWhereClause (:281-298) KHÔNG lọc KeyWord
    public string? FlagActive { get; set; }          // Cờ hoạt động — lọc theo IN, tách dấu phẩy (gate :287-295)
    public int Ft_PageIndex { get; set; } = 0;       // Trang (0-based) — nguồn :23, :44
    public int Ft_PageSize { get; set; } = 10;       // Số dòng/trang — nguồn :23, :45 (mặc định 10)
}

[ApiController]
[Route("api/mst_parampromotion")]
[Produces("application/json")]
public class Mst_ParamPromotionControllerPort : ControllerBase
{
    private readonly AppDbContext _db;

    public Mst_ParamPromotionControllerPort(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Port 1:1 action Search của Mst_ParamPromotionController (gate :23-103).
    /// </summary>
    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] MstParamPromotionSearchReq? req)
    {
        var r = req ?? new MstParamPromotionSearchReq();
        var pageIndex = r.Ft_PageIndex;
        var pageSize = r.Ft_PageSize;

        // RecordStart = pageIndex * pageSize, RecordCount = pageSize (gate :66-67).
        var start = (long)pageIndex * pageSize;
        var (itemCount, rows) = await GetXAsync(r.FlagActive, start, pageSize);

        // PageSize=0 -> DataList rỗng (nguồn: nFilterRecordEnd = start - 1 = -1 < MyIdxSeq 0, Master.Loyalty.cs:12501-12508).
        // PageCount chỉ tính khi DataList không rỗng: ceil(itemCount / ps) với ps = pageSize (nếu ps == 0 thì dùng 10) (gate :78-86).
        var ps = pageSize == 0 ? 10 : pageSize;
        var pageCount = rows.Count == 0 ? 0 : (itemCount % ps == 0 ? itemCount / ps : itemCount / ps + 1);

        return Ok(new { Success = true, Data = new PageInfo<object>(rows, itemCount, pageCount, pageIndex, pageSize) });
    }

    /// <summary>
    /// Port 1:1 action GetAllActive của Mst_ParamPromotionController (gate :104-213).
    /// KHÔNG tham số; lọc FlagActive="1" qua strWhereClause_Mst_ParamPromotion (:130-134),
    /// Ft_RecordStart="0", Ft_RecordCount="123456000" (BaseController.cs:30-31).
    /// Data là MẢNG TRẦN; rỗng thì Data = null (:127, :153-155, :191-199).
    /// </summary>
    [HttpPost("getallactive")]
    public async Task<IActionResult> GetAllActive()
    {
        var (_, rows) = await GetXAsync("1", 0, 123456000);
        return Ok(new { Success = true, Data = rows.Count > 0 ? rows : null });
    }

    /// <summary>
    /// Thực hiện các bước truy vấn của Mst_ParamPromotion_GetX (Master.Loyalty.cs:12427-12598):
    ///   Bước 1 (Draft + Count): lọc FlagActive theo IN (tách dấu phẩy, trim, không phân biệt hoa/thường),
    ///           order by LogLUDTimeUTC desc, đếm tổng dòng draft làm ItemCount.
    ///   Bước 2 (Paging + Select cuối): cắt theo [recordStart, recordStart + recordCount),
    ///           LEFT JOIN Mst_PromotionProgram qua PrProgramCode để lấy PrProgramName từ Mst_PromotionProgram,
    ///           order by LogLUDTimeUTC desc (hòa trùng: sắp phụ theo PrProgramCode asc, ParamPrType asc).
    /// GAP: drAbilityOfUser (:12468) lấy nhưng dòng gọi cache bị comment (:12480) -> không port (coi như thấy toàn bộ).
    /// </summary>
    private async Task<(int itemCount, List<object> rows)> GetXAsync(string? flagActiveCsv, long recordStart, long recordCount)
    {
        // -----------------------------------------------------------------------------------------
        // BƯỚC 1: DRAFT + COUNT (Master.Loyalty.cs:12484-12497)
        // select distinct identity(bigint, 0, 1) MyIdxSeq, mpp.PrProgramCode, mpp.ParamPrType, mpp.LogLUDTimeUTC
        // from Mst_ParamPromotion mpp where (1=1) and (mpp.FlagActive in (...)) order by mpp.LogLUDTimeUTC desc
        // -----------------------------------------------------------------------------------------
        var q = _db.Set<Mst_ParamPromotion>().AsNoTracking().AsQueryable();

        // Lọc FlagActive theo IN: tách dấu phẩy, trim, không phân biệt hoa/thường (gate :287-295).
        // KeyWord KHÔNG lọc (gate :281-298 không có nhánh xử lý KeyWord).
        // OrgID KHÔNG lọc (bảng Mst_ParamPromotion không có cột OrgID).
        var flagValues = SplitCsv(flagActiveCsv);
        if (flagValues.Count > 0)
        {
            // Nguồn dùng `FlagActive in ('0','1')` trên SQL Server collation KHÔNG phân biệt hoa/thường
            // ⇒ so sánh Like KHÔNG wildcard, hạ chữ 2 phía (PortQuery.Equal).
            var patterns = flagValues.Select(v => PortQuery.Equal(v)).ToList();
            q = q.Where(x => patterns.Contains((x.FlagActive ?? "").ToLower()));
        }

        // Sắp xếp mpp.LogLUDTimeUTC desc (Master.Loyalty.cs:12493).
        // Hòa trùng LogLUDTimeUTC: sắp phụ theo PrProgramCode asc, ParamPrType asc để thứ tự xác định.
        var draftList = await q
            .OrderByDescending(x => x.LogLUDTimeUTC)
            .ThenBy(x => x.PrProgramCode)
            .ThenBy(x => x.ParamPrType)
            .ToListAsync();

        // MyCount = Count(0) của bảng draft (#tbl_Mst_ParamPromotion_Filter_Draft, :12497).
        var itemCount = draftList.Count;

        // Cắt trang draft (#tbl_Mst_ParamPromotion_Filter, :12501-12508):
        // t.MyIdxSeq >= @nFilterRecordStart and t.MyIdxSeq <= @nFilterRecordEnd.
        // Khi recordCount <= 0 (pageSize=0) hoặc recordStart < 0 -> draftPage rỗng (nFilterRecordEnd = -1).
        var draftPage = (recordCount <= 0 || recordStart < 0)
            ? new List<(long MyIdxSeq, Mst_ParamPromotion Param)>()
            : draftList
                .Select((p, idx) => (MyIdxSeq: (long)idx, Param: p))
                .Where(x => x.MyIdxSeq >= recordStart && x.MyIdxSeq < recordStart + recordCount)
                .ToList();

        if (draftPage.Count == 0)
        {
            return (itemCount, new List<object>());
        }

        // -----------------------------------------------------------------------------------------
        // BƯỚC 2: SELECT CUỐI (Master.Loyalty.cs:12526-12544)
        // select t.MyIdxSeq, mpp.PrProgramCode, mpp.ParamPrType, mpp.QtyDateAfter, mpp.QtyDateBefore,
        //        mpp.FlagActive, mpp.Remark, mpp.LogLUBy, mpp.LogLUDTimeUTC, mppt.PrProgramName
        // from #tbl_Mst_ParamPromotion_Filter t
        //   inner join Mst_ParamPromotion mpp on t.PrProgramCode = mpp.PrProgramCode and t.ParamPrType = mpp.ParamPrType
        //   left join Mst_PromotionProgram mppt on mpp.PrProgramCode = mppt.PrProgramCode
        // order by mpp.LogLUDTimeUTC desc
        // -----------------------------------------------------------------------------------------
        // Lấy PrProgramName TỪ Mst_PromotionProgram qua LEFT JOIN on PrProgramCode (Master.Loyalty.cs:12536, :12541-12542).
        // KHÔNG lấy cột PrProgramName của Mst_ParamPromotion.
        var codes = draftPage.Select(x => x.Param.PrProgramCode).Distinct().ToList();
        var programNames = await _db.Set<Mst_PromotionProgram>().AsNoTracking()
            .Where(p => codes.Contains(p.PrProgramCode))
            .ToDictionaryAsync(p => p.PrProgramCode, p => p.PrProgramName, StringComparer.OrdinalIgnoreCase);

        var rows = draftPage
            .OrderByDescending(x => x.Param.LogLUDTimeUTC)
            .ThenBy(x => x.Param.PrProgramCode)
            .ThenBy(x => x.Param.ParamPrType)
            .Select(x =>
            {
                var p = x.Param;
                // PrProgramName lấy từ Mst_PromotionProgram (LEFT JOIN) — không tồn tại (PR_NONE) -> null.
                programNames.TryGetValue(p.PrProgramCode, out var programName);
                return (object)new
                {
                    x.MyIdxSeq,
                    p.PrProgramCode,
                    p.ParamPrType,
                    p.QtyDateAfter,
                    p.QtyDateBefore,
                    p.FlagActive,
                    p.Remark,
                    p.LogLUBy,
                    p.LogLUDTimeUTC,
                    PrProgramName = programName,
                };
            })
            .ToList();

        return (itemCount, rows);
    }

    /// <summary>
    /// Tách chuỗi CSV theo dấu phẩy, trim từng phần, bỏ phần rỗng (gate :289-292).
    /// </summary>
    private static List<string> SplitCsv(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) return new List<string>();
        return csv.Split(',')
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .ToList();
    }
}
