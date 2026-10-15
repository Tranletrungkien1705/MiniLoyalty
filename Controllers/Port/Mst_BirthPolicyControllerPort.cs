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
// PORT 1:1 — màn danh mục chính sách sinh nhật (Mst_BirthPolicy) của dự án nguồn 2022.7.Loyalty (#70).
//
// Nguồn controller (tầng gate):
//   Mst_BirthPolicyController.cs:23-103    Search -> WA_Mst_BirthPolicy_Get (#70)
//   Mst_BirthPolicyController.cs:294-319   strWhereClause_Mst_BirthPolicy (mệnh đề lọc Search)
// Nguồn API:
//   MstBirthPolicyController.cs:168-238    WA_Mst_BirthPolicy_Get
// Nguồn biz:
//   Master.Loyalty.cs:10606-10719          WAS_Mst_BirthPolicy_Get
//   Master.Loyalty.cs:10720-10876          Mst_BirthPolicy_Get
//   Master.Loyalty.cs:10877-11085          Mst_BirthPolicy_GetX (bản LIVE)
//
// ĐẶC ĐIỂM VÀ QUY TẮC NGHIỆP VỤ NGUỒN:
//   * Endpoint là POST (nguồn gate Mst_BirthPolicyController.cs:23).
//   * Lọc KeyWord: `BirthPolicyNo like '%kw%'` (gate :299-302).
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
    /// Lọc + đếm + chia trang theo LogLUDTimeUTC desc — dùng cho Search (Master.Loyalty.cs:10877-11085).
    /// Trả (itemCount, rows): itemCount = tổng số dòng sau lọc (MyCount, SQL :10948);
    /// rows = trang đã cắt kèm MyIdxSeq 0-based (:10937) và cột tính FlagShow (:10984-10996).
    /// </summary>
    private async Task<(int itemCount, List<object> rows)> GetXAsync(
        string? keyWord,
        string? flagActiveCsv,
        long recordStart,
        long recordCount)
    {
        var q = _db.Set<Mst_BirthPolicy>().AsNoTracking().AsQueryable();

        // 1. Lọc KeyWord: like '%kw%' trên BirthPolicyNo (nguồn gate Mst_BirthPolicyController.cs:299-302).
        // Nguồn chạy SQL Server collation không phân biệt hoa/thường -> hạ chữ cả 2 phía rồi LIKE qua PortQuery.
        if (!string.IsNullOrWhiteSpace(keyWord))
        {
            var kw = PortQuery.Contains(keyWord);
            q = q.Where(x => EF.Functions.Like((x.BirthPolicyNo ?? "").ToLower(), kw, PortQuery.Esc));
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

        return (itemCount, rows);
    }
}
