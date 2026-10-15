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
// PORT 1:1 — màn danh mục chính sách sinh nhật (Mst_BirthPolicy) của dự án nguồn 2022.7.Loyalty (#70, #71, #72).
//
// Nguồn controller (tầng gate):
//   Mst_BirthPolicyController.cs:23-103    Search -> WA_Mst_BirthPolicy_Get (#70)
//   Mst_BirthPolicyController.cs:104-213   GetAllActive -> WA_Mst_BirthPolicy_Get (#71)
//   Mst_BirthPolicyController.cs:214-290   GetByBirthPolicyNo -> WA_Mst_BirthPolicy_Get (#72)
//   Mst_BirthPolicyController.cs:294-319   strWhereClause_Mst_BirthPolicy (mệnh đề lọc Search / GetAllActive)
//   Mst_BirthPolicyController.cs:320-335   strWhereClause_Mst_BirthPolicy_By_BirthPolicyNo (mệnh đề lọc GetByBirthPolicyNo)
// Nguồn API:
//   MstBirthPolicyController.cs:168-238    WA_Mst_BirthPolicy_Get
// Nguồn biz:
//   Master.Loyalty.cs:10606-10719          WAS_Mst_BirthPolicy_Get
//   Master.Loyalty.cs:10720-10876          Mst_BirthPolicy_Get
//   Master.Loyalty.cs:10877-11085          Mst_BirthPolicy_GetX (bản LIVE)
//
// ĐẶC ĐIỂM VÀ QUY TẮC NGHIỆP VỤ NGUỒN:
//   * Endpoint là POST (nguồn gate Mst_BirthPolicyController.cs:23, :104, :214).
//   * Lọc KeyWord: `BirthPolicyNo like '%kw%'` (gate :299-302).
//   * Lọc BirthPolicyNo: `BirthPolicyNo = 'x'` (gate :327).
//   * Lọc FlagActive: danh sách tách dấu phẩy `FlagActive IN ('1','0')` (gate :308-316);
//     dòng `=` (:304-307) BỊ COMMENT ở nguồn nên KHÔNG chạy.
//   * Phân trang: Ft_RecordStart = pageIndex * pageSize, Ft_RecordCount = pageSize (gate :66-67).
//     MyIdxSeq 0-based identity (SQL Master.Loyalty.cs:10937).
//     Thứ tự draft: `order by mrp.LogLUDTimeUTC desc` (:10944). Hòa trùng sắp phụ theo `BirthPolicyNo asc`.
//   * PageSize = 0 -> DataList rỗng (nFilterRecordEnd = -1), PageCount = 0 (gate :78-86).
//   * FlagShow tính lúc select (:10984-10996):
//     - '1' khi `EffDateStart <= @dateSys and @dateSys <= EffDateEnd`
//     - '0' khi ngoài khoảng
//     - NULL nếu ngày null
//     với `@dateSys = DateTime.UtcNow.AddHours(7).ToString("yyyy-MM-dd")` (ngày Việt Nam UTC+7, lúc 00:00:00).
//     Trong C# tính với `var today = DateTime.UtcNow.AddHours(7).Date`.
//   * GetByBirthPolicyNo (#72): [HttpPost("getbybirthpolicyno")], body { BirthPolicyNo }, EmptyBodyBehavior.Allow (gate :214-290).
//     - BirthPolicyNo rỗng -> KHÔNG truy vấn, Data = { Mst_BirthPolicy: null, Lst_Mst_BirthPolicyDtl: null } (:233-234, :273-277).
//     - Ngược lại lọc strWhereClause_Mst_BirthPolicy_By_BirthPolicyNo CHỈ với BirthPolicyNo -> BirthPolicyNo = (:327;
//       nhánh FlagActive :329-332 không truyền -> không lọc FlagActive -> chính sách inactive như BP02 VẪN lấy được).
//       Ft_RecordStart = "0", Ft_RecordCount = "1" (:253-254).
//       Xin CẢ header và Dtl: Rt_Cols_Mst_BirthPolicy = "*", Rt_Cols_Mst_BirthPolicyDtl = "*" (:257-258).
//     - Kết quả (:262-277):
//       Mst_BirthPolicy = header[0] (MyIdxSeq + 12 cột + FlagShow) hoặc null.
//       Lst_Mst_BirthPolicyDtl = danh sách DTL (MyIdxSeq + 10 cột Dtl) hoặc null nếu rỗng.
//     - Biz GetX (:11008-11016): Dtl = t.MyIdxSeq, mrpl.* từ draft inner join Dtl on BirthPolicyNo, order by mrpl.LogLUDTimeUTC desc
//       (hòa trùng: sắp phụ theo CardType asc, ghi comment theo đề bài).
//   * DDL chọn: 20221026.CreateDatabase.sql:399-418 (12 cột, có EffDateEnd, UpdateDTime, UpdateBy)
//     thay vì bản cũ Mst_BirthPolicy.sql:96 thiếu EffDateEnd; khớp với 20260731.z11.Mst_BirthPolicy.Disable.sql.
//   * Mst_BirthPolicyDtl: 10 cột (7 cột gốc DDL :378-394 + 3 cột voucher từ 20260518.z11.RankRN_BirthVoucher.sql:114-130).
//     FK CardType -> Mst_CardType, FK BirthPolicyNo -> Mst_BirthPolicy ghi comment, chưa cấu hình FK navigation EF.
//   * Bỏ qua có chủ đích:
//     - drAbilityOfUser (Master.Loyalty.cs:10920) nguồn gọi nhưng không dùng (:10932 bị comment) -> bỏ qua, ghi comment.
//     - Phân quyền CheckGatewayAuthentication, CheckAuthorize (gate :32-33), Sys_User_CheckAuthorize (:10786-10799):
//       hệ thống đích chưa có hệ phân quyền.
//     - Ghi RequestLog/ReturnLog (:10765-10772, :10866-10873).
// ============================================================================================

/// <summary>
/// Body request của Search (Mst_BirthPolicyController.cs:23).
/// KeyWord, FlagActive, Ft_PageIndex (mặc định 0), Ft_PageSize (mặc định 10).
/// </summary>
public class MstBirthPolicySearchReq
{
    public string? KeyWord { get; set; }
    public string? FlagActive { get; set; }
    public int Ft_PageIndex { get; set; } = 0;
    public int Ft_PageSize { get; set; } = 10;
}

/// <summary>Port 1:1 <c>GetByBirthPolicyNo</c> — body nhận BirthPolicyNo (Mst_BirthPolicyController.cs:214).</summary>
public class MstBirthPolicyGetByBirthPolicyNoReq
{
    public string? BirthPolicyNo { get; set; }
}

[ApiController]
[Route("api/mst_birthpolicy")]
[Produces("application/json")]
public class Mst_BirthPolicyControllerPort : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public Mst_BirthPolicyControllerPort(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    /// <summary>
    /// Port 1:1 <c>Mst_BirthPolicyController.Search</c> (nguồn gate Mst_BirthPolicyController.cs:23-103).
    /// Body JSON nhận KeyWord, FlagActive, Ft_PageIndex, Ft_PageSize, EmptyBodyBehavior.Allow.
    /// </summary>
    [HttpPost("search")]
    public async Task<IActionResult> Search(
        [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] MstBirthPolicySearchReq? req)
    {
        var r = req ?? new MstBirthPolicySearchReq();
        var pageIndex = Math.Max(0, r.Ft_PageIndex);
        var pageSize = r.Ft_PageSize;

        // Search gọi GetXAsync với start = PageIndex * PageSize, count = PageSize (gate :66-67).
        var start = (long)pageIndex * pageSize;
        var (itemCount, rows) = await GetXAsync(r.KeyWord, r.FlagActive, start, pageSize);

        // PageSize=0 -> DataList rỗng (nguồn: nFilterRecordEnd = start-1, SQL biz + gate :78-86).
        // PageCount = 0 khi DataList rỗng, ngược lại ceil(ItemCount / ps) với ps = PageSize (PageSize=0 thì 10) — gate :78-86.
        var ps = pageSize == 0 ? 10 : pageSize;
        var pageCount = rows.Count == 0 ? 0 : (itemCount % ps == 0 ? itemCount / ps : itemCount / ps + 1);

        return Ok(new
        {
            Success = true,
            Data = new PageInfo<object>(rows, itemCount, pageCount, pageIndex, pageSize)
        });
    }

    /// <summary>
    /// Port 1:1 <c>Mst_BirthPolicyController.GetAllActive</c> (nguồn gate Mst_BirthPolicyController.cs:104-213).
    /// Nguồn gọi <c>strWhereClause_Mst_BirthPolicy</c> với <c>FlagActive = "1"</c> (:130-134) ⇒ lọc
    /// <c>FlagActive IN ('1')</c> (:308-316). <c>OrgID</c> chỉ được gán vào request (:132, :144),
    /// hàm lọc KHÔNG dùng OrgID ⇒ KHÔNG lọc theo OrgID.
    /// Không body: RecordStart = "0", RecordCount = "123456000" (nguồn Ft_RecordStart/Ft_RecordCount :146-147).
    /// Khối Ext/tree (:156-180) bị comment ở nguồn ⇒ KHÔNG port.
    /// Dùng lại GetXAsync của Search (#70): MyIdxSeq + 12 cột + FlagShow.
    /// Trả <c>Ok(new { Success = true, Data = ... })</c> — Data là MẢNG TRẦN, rỗng thì <c>null</c>
    /// (nguồn :127 khởi tạo null, :153-155 chỉ gán khi có dòng, :193-198 trả thẳng listMst_BirthPolicy).
    /// </summary>
    [HttpPost("getallactive")]
    public async Task<IActionResult> GetAllActive()
    {
        // Nguồn: Ft_RecordStart = "0", Ft_RecordCount = "123456000" (gate Mst_BirthPolicyController.cs:146-147).
        // FlagActive = "1" (gate :133) ⇒ lọc IN ('1'); OrgID không lọc.
        var (_, rows) = await GetXAsync(null, "1", 0, 123456000);

        // Nguồn :193-198: Data = listMst_BirthPolicy (mảng trần); rỗng ⇒ null (:127, :153-155).
        return Ok(new { Success = true, Data = rows.Count > 0 ? rows : null });
    }


    /// <summary>
    /// Port 1:1 <c>Mst_BirthPolicyController.GetByBirthPolicyNo</c> (nguồn gate Mst_BirthPolicyController.cs:214-290, #72).
    /// Body JSON nhận BirthPolicyNo, EmptyBodyBehavior.Allow.
    /// BirthPolicyNo rỗng -> KHÔNG truy vấn, Data = { Mst_BirthPolicy: null, Lst_Mst_BirthPolicyDtl: null } (gate :233-234, :273-277).
    /// Ngược lại lọc BirthPolicyNo = (Like + PortQuery.Equal, gate :327 — strWhereClause_Mst_BirthPolicy_By_BirthPolicyNo);
    /// không lọc FlagActive (nhánh FlagActive không truyền ở gate :238-241) -> chính sách inactive (như BP02) VẪN lấy được.
    /// Ft_RecordStart = "0", Ft_RecordCount = "1" (gate :253-254).
    /// Xin cả header và Dtl (Rt_Cols_Mst_BirthPolicy = "*", Rt_Cols_Mst_BirthPolicyDtl = "*", gate :257-258).
    /// Kết quả:
    ///   - Data.Mst_BirthPolicy: header[0] (MyIdxSeq + 12 cột + FlagShow) hoặc null (gate :262-265, :275).
    ///   - Data.Lst_Mst_BirthPolicyDtl: danh sách DTL (MyIdxSeq + 10 cột Dtl gồm VoucherValue, VoucherPointVCLimit, VoucherExpireDays)
    ///     hoặc null nếu rỗng (gate :266-269, :276).
    /// </summary>
    [HttpPost("getbybirthpolicyno")]
    public async Task<IActionResult> GetByBirthPolicyNo(
        [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] MstBirthPolicyGetByBirthPolicyNoReq? req)
    {
        var birthPolicyNo = req?.BirthPolicyNo;

        // BirthPolicyNo rỗng -> nguồn không truy vấn (:236) và trả cả 2 trường null (:233-234, :273-277).
        if (string.IsNullOrWhiteSpace(birthPolicyNo))
        {
            return Ok(new
            {
                Success = true,
                Data = new
                {
                    Mst_BirthPolicy = (object?)null,
                    Lst_Mst_BirthPolicyDtl = (object?)null
                }
            });
        }

        // Lấy 1 dòng theo BirthPolicyNo (Ft_RecordStart = "0", Ft_RecordCount = "1" — gate :253-254).
        // getDtl = true -> xin thêm danh sách Dtl (Rt_Cols_Mst_BirthPolicyDtl = "*" — gate :258).
        // Nhánh FlagActive không truyền ở gate (:238-241) nên không lọc FlagActive -> chính sách inactive vẫn lấy được.
        var (_, rows, dtlRows) = await GetXAsync(null, null, 0, 1, birthPolicyNoEqual: birthPolicyNo, getDtl: true);

        // Nguồn gate :262-269, :273-277:
        // Mst_BirthPolicy = header[0] hoặc null.
        // Lst_Mst_BirthPolicyDtl = danh sách DTL hoặc null nếu rỗng.
        var objMst_BirthPolicy = rows.Count > 0 ? rows[0] : null;
        var lst_Mst_BirthPolicyDtl = dtlRows.Count > 0 ? dtlRows : null;

        return Ok(new
        {
            Success = true,
            Data = new
            {
                Mst_BirthPolicy = objMst_BirthPolicy,
                Lst_Mst_BirthPolicyDtl = lst_Mst_BirthPolicyDtl
            }
        });
    }

    /// <summary>
    /// Overload giữ nguyên tương thích cho Search (#70) và GetAllActive (#71).
    /// </summary>
    private async Task<(int itemCount, List<object> rows)> GetXAsync(
        string? keyWord,
        string? flagActiveCsv,
        long recordStart,
        long recordCount)
    {
        var (itemCount, rows, _) = await GetXAsync(keyWord, flagActiveCsv, recordStart, recordCount, null, false);
        return (itemCount, rows);
    }

    /// <summary>
    /// Lọc + đếm + chia trang theo LogLUDTimeUTC desc (Master.Loyalty.cs:10877-11085).
    /// Hỗ trợ lọc KeyWord (like '%kw%'), FlagActive (IN), BirthPolicyNo (=), và lấy danh sách chi tiết Dtl.
    /// drAbilityOfUser (Master.Loyalty.cs:10920) nguồn gọi nhưng không dùng (:10932 bị comment) -> bỏ qua, ghi comment.
    /// </summary>
    private async Task<(int itemCount, List<object> rows, List<object> dtlRows)> GetXAsync(
        string? keyWord,
        string? flagActiveCsv,
        long recordStart,
        long recordCount,
        string? birthPolicyNoEqual = null,
        bool getDtl = false)
    {
        // drAbilityOfUser (Master.Loyalty.cs:10920) nguồn gọi nhưng bị comment không dùng (:10932) -> bỏ qua, ghi comment.
        var q = _db.Set<Mst_BirthPolicy>().AsNoTracking().AsQueryable();

        // 1. Lọc KeyWord: like '%kw%' trên BirthPolicyNo (nguồn gate Mst_BirthPolicyController.cs:299-302).
        // Nguồn chạy SQL Server collation không phân biệt hoa/thường -> hạ chữ cả 2 phía rồi LIKE qua PortQuery.
        if (!string.IsNullOrWhiteSpace(keyWord))
        {
            var kw = PortQuery.Contains(keyWord);
            q = q.Where(x => EF.Functions.Like((x.BirthPolicyNo ?? "").ToLower(), kw, PortQuery.Esc));
        }

        // Lọc BirthPolicyNo: `BirthPolicyNo = 'x'` (gate Mst_BirthPolicyController.cs:327 — strWhereClause_Mst_BirthPolicy_By_BirthPolicyNo).
        // Nguồn dùng `=` trên SQL Server collation KHÔNG phân biệt hoa/thường ⇒ Like KHÔNG wildcard, hạ chữ 2 phía (PortQuery.Equal).
        if (!string.IsNullOrWhiteSpace(birthPolicyNoEqual))
        {
            var bp = PortQuery.Equal(birthPolicyNoEqual);
            q = q.Where(x => EF.Functions.Like((x.BirthPolicyNo ?? "").ToLower(), bp, PortQuery.Esc));
        }

        // 2. Lọc FlagActive: danh sách IN ('val1','val2',...) (nguồn gate Mst_BirthPolicyController.cs:308-316).
        // Tách dấu phẩy, bỏ khoảng trắng thừa, hạ chữ để so sánh không phân biệt hoa/thường.
        // Lưu ý: nhánh '=' tại :304-307 bị comment ở nguồn nên không dùng.
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

        // 3. Draft filter (#tbl_Mst_BirthPolicy_Filter_Draft — Master.Loyalty.cs:10936-10945):
        // order by mrp.LogLUDTimeUTC desc. Hòa trùng LogLUDTimeUTC: sắp phụ theo BirthPolicyNo asc để kết quả ổn định.
        var ordered = q
            .OrderByDescending(x => x.LogLUDTimeUTC)
            .ThenBy(x => x.BirthPolicyNo);

        // Summary count: select Count(0) MyCount from #tbl_Mst_BirthPolicy_Filter_Draft (:10948)
        var itemCount = await ordered.CountAsync();

        // 4. Phân trang (#tbl_Mst_BirthPolicy_Filter — Master.Loyalty.cs:10952-10959):
        // t.MyIdxSeq >= @nFilterRecordStart and t.MyIdxSeq <= @nFilterRecordEnd
        // với nFilterRecordEnd = recordStart + recordCount - 1. Nếu recordCount <= 0 thì trả danh sách rỗng.
        var page = recordCount <= 0
            ? new List<Mst_BirthPolicy>()
            : await ordered.Skip((int)recordStart).Take((int)recordCount).ToListAsync();

        // 5. Select header + FlagShow (Master.Loyalty.cs:10981-10996):
        // FlagShow = '1' khi EffDateStart <= @dateSys and @dateSys <= EffDateEnd, '0' khi ngoài khoảng
        // với @dateSys = DateTime.UtcNow.AddHours(7).ToString("yyyy-MM-dd") (ngày Việt Nam UTC+7 lúc 00:00:00).
        var today = DateTime.UtcNow.AddHours(7).Date;

        var rows = page
            .OrderByDescending(x => x.LogLUDTimeUTC)
            .ThenBy(x => x.BirthPolicyNo)
            .Select((x, i) =>
            {
                var myIdxSeq = recordStart + i;
                // SQL so sánh cột datetime với chuỗi 'yyyy-MM-dd' = 00:00 hôm nay ⇒ so NGUYÊN giá trị datetime (KHÔNG cắt .Date):
                // EffDateStart hôm nay 10:00 ⇒ 10:00 <= 00:00 sai ⇒ '0' như nguồn.
                var flagShow = (x.EffDateStart <= today && today <= x.EffDateEnd) ? "1" : "0";
                return (object)new
                {
                    MyIdxSeq = myIdxSeq,
                    x.BirthPolicyNo,
                    x.NetworkID,
                    x.ParamPrType,
                    x.BirthPolicyName,
                    x.EffDateStart,
                    x.EffDateEnd,
                    x.FlagActive,
                    x.Remark,
                    x.UpdateDTime,
                    x.UpdateBy,
                    x.LogLUDTimeUTC,
                    x.LogLUBy,
                    FlagShow = flagShow,
                };
            })
            .ToList();

        // 6. Select Dtl (Master.Loyalty.cs:11008-11016) khi getDtl = true
        // select t.MyIdxSeq, mrpl.*
        // from #tbl_Mst_BirthPolicy_Filter t
        //   inner join Mst_BirthPolicyDtl mrpl on t.BirthPolicyNo = mrpl.BirthPolicyNo
        // order by mrpl.LogLUDTimeUTC desc
        var dtlRows = new List<object>();
        if (getDtl && page.Count > 0)
        {
            var birthPolicyNos = page.Select(x => x.BirthPolicyNo).ToList();
            var policyMap = page
                .Select((x, i) => new { x.BirthPolicyNo, MyIdxSeq = recordStart + i })
                .ToDictionary(x => x.BirthPolicyNo, x => x.MyIdxSeq, StringComparer.OrdinalIgnoreCase);

            List<Mst_BirthPolicyDtl> dtlList;
            if (birthPolicyNos.Count == 1)
            {
                var targetNo = birthPolicyNos[0];
                dtlList = await _db.Set<Mst_BirthPolicyDtl>()
                    .AsNoTracking()
                    .Where(d => EF.Functions.Like((d.BirthPolicyNo ?? "").ToLower(), PortQuery.Equal(targetNo), PortQuery.Esc))
                    .ToListAsync();
            }
            else
            {
                dtlList = await _db.Set<Mst_BirthPolicyDtl>()
                    .AsNoTracking()
                    .Where(d => birthPolicyNos.Contains(d.BirthPolicyNo))
                    .ToListAsync();
            }

            // Sắp xếp Dtl theo mrpl.LogLUDTimeUTC desc (Master.Loyalty.cs:11015).
            // Hòa trùng LogLUDTimeUTC: sắp phụ theo CardType asc, ghi comment theo đề bài (Master.Loyalty.cs:11015).
            dtlRows = dtlList
                .OrderByDescending(d => d.LogLUDTimeUTC)
                .ThenBy(d => d.CardType)
                .Select(d => (object)new
                {
                    MyIdxSeq = policyMap.TryGetValue(d.BirthPolicyNo, out var seq) ? seq : 0L,
                    d.BirthPolicyNo,
                    d.CardType,
                    d.NetworkID,
                    d.Point,
                    d.Remark,
                    d.LogLUDTimeUTC,
                    d.LogLUBy,
                    d.VoucherValue,
                    d.VoucherPointVCLimit,
                    d.VoucherExpireDays,
                })
                .ToList();
        }

        return (itemCount, rows, dtlRows);
    }
}
