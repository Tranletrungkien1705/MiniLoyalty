using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniLoyalty.Data;
using MiniLoyalty.Models;
using MiniLoyalty.Models.Port;

namespace MiniLoyalty.Controllers.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — Crd_CardTransactionInCreaseController: Search (#015) và Export (#016).
//
// Chuỗi gọi:
//   Gate: Crd_CardTransactionInCreaseController.cs:33-117 (Search)
//         Crd_CardTransactionInCreaseController.cs:121-215 (Export)
//         + hàm phụ strWhereClause_Crd_CardTransactionInCrease (:219-258)
//         + hàm phụ DicColums (:262-287), DicColumsDetail (:289-316)
//   API:  CrdCardTransactionInCreaseController.cs:20-87 (WA_Crd_CardTransactionInCrease_Get)
//   BIZ:  Report.cs:11903-12036 (WAS_Crd_CardTransactionInCrease_Get)
//         Report.cs:12037-12183 (Crd_CardTransactionInCrease_Get)
//         Report.cs:12184-12523 (Crd_CardTransactionInCrease_GetX — bản LIVE)
//
// ĐẶC ĐIỂM NGHIỆP VỤ NGUỒN:
//   1. Gate nhận 6 tham số: CreateDTimeUTCFrom, CreateDTimeUTCTo, MemberNo, DLCode, AutoId, PeriodMonth (:33).
//      Đề bài P1 chỉ liệt kê 5 tham số lọc (không kể PeriodMonth) — PeriodMonth vẫn phải nhận vì biz dùng
//      cho truy vấn Crd_CardTransactionInCreaseInst (Report.cs:12364).
//   2. strWhereClause_Crd_CardTransactionInCrease (:219-258):
//      - CreateDTimeUTCFrom: StdDTimeBeginDay -> StdDTimeUTC -> ">=" trên Crd_CardTransaction.CreateDTimeUTC (:225-231)
//      - CreateDTimeUTCTo:   StdDTimeEndDay   -> StdDTimeUTC -> "<=" trên Crd_CardTransaction.CreateDTimeUTC (:233-239)
//      - MemberNo: "=" trên Crd_CardTransaction.MemberNo (:241-244)
//      - DLCode:   "=" trên Crd_CardTransaction.DLCode (:246-249)
//      - AutoId:   "=" trên Crd_CardTransaction.AutoId (:251-254)
//   3. Crd_CardTransactionInCrease_GetX (:12184-12523):
//      - #tbl_Crd_CardTransaction_Filter_Draft (:12256-12276):
//          select distinct identity(bigint,0,1) MyIdxSeq, cast(cct.AutoId as bigint) AutoId, cct.CreateDTimeUTC
//          from Crd_CardTransaction cct
//            inner join Crd_Member cm on cct.MemberNo = cm.MemberNo
//            inner join Mst_DealerCP md on cct.DLCode = md.DLCPCode
//          where (1=1) + strFt_WhereClause_All + strFt_WhereClause
//            and cct.AmountChTotal > 0
//            and cct.CreateDTimeUTC >= '2023-06-30 17:00:00'  (hardcode nguồn :12272)
//          order by cct.CreateDTimeUTC asc, cast(cct.AutoId as bigint) asc
//      - Summary: select Count(0) MyCount (:12278-12280).
//      - #tbl_Crd_CardTransaction_Filter (:12282-12290): lọc MyIdxSeq >= nFilterRecordStart và <= nFilterRecordEnd.
//      - Trả 2 danh sách: Crd_CardTransactionInCrease (:12411-12463) và Crd_CardTransactionInCreaseInst (:12311-12375).
//      - Định dạng ngày: StdDTimeVN -> StdDate cho CreateDate, CreateDate_IC, CreateDate_DC (:11974-12005).
//   4. Gate trả: Data = { Lst_Crd_CardTransactionInCrease, Lst_Crd_CardTransactionInCreaseInst } (rỗng -> null) (:100-104).
//
// GAP đã khai báo:
//   - Bảng tạm #tbl_... của nguồn chuyển thành truy vấn LINQ + phân trang trong bộ nhớ (không có bảng tạm SQLite).
//   - Sys_User_ForCP_GetAbilityViewOfUser (lọc theo quyền xem của người dùng đăng nhập) bỏ qua vì đích không có
//     phiên đăng nhập; strDLCPCode = null -> nhánh CASE DLCode_DC/RefNo_DC luôn rơi vào 'ĐL khác'/'N/A'.
// ===========================================================================================

/// <summary>
/// Port 1:1 tham số request của Gate Search (Crd_CardTransactionInCreaseController.cs:33).
/// </summary>
public class Crd_CardTransactionInCreaseSearchReq
{
    public string? CreateDTimeUTCFrom { get; set; }  // Thời gian giao dịch từ — :33
    public string? CreateDTimeUTCTo { get; set; }    // Thời gian giao dịch đến — :33
    public string? MemberNo { get; set; }            // Mã hội viên — :33
    public string? DLCode { get; set; }              // Mã đại lý — :33
    public string? AutoId { get; set; }              // Mã giao dịch — :33
    public string? PeriodMonth { get; set; }         // Kỳ xét hạng (yyyy-MM) — :33 (biz dùng cho Inst, Report.cs:12364)
}

/// <summary>
/// Dòng kết quả danh sách Crd_CardTransactionInCrease — port 1:1 model nguồn
/// (12.Dev.Common/idn.Loyalty.Common/Models/Crd_CardTransactionInCrease.cs:9-31).
/// </summary>
public class Crd_CardTransactionInCreaseItem
{
    public long STT { get; set; }                    // STT — model :11
    public string? DLCode { get; set; }              // Mã Đại lý — :12
    public string? DLCPName { get; set; }            // Tên Đại lý — :13
    public long AutoId { get; set; }                 // Mã giao dịch — :14
    public string? CreateDate { get; set; }          // Thời gian giao dịch (StdDate) — :15
    public string? RefNo { get; set; }               // Số tham chiếu — :16
    public string? MemberNo { get; set; }            // Mã hội viên — :17
    public string? CarNo { get; set; }               // Biển số — :18
    public string? MemberName { get; set; }          // Họ và tên — :19
    public string? CardTypeUse { get; set; }         // Hạng sử dụng — :20
    public string? CardTypeInit { get; set; }        // Hạng thực — :21
    public string? DealPointType { get; set; }       // Loại điểm — :22
    public string? DLCodeExceptionally { get; set; } // Đại lý yêu cầu đặc cách — :23
    public double? AmountChTotal { get; set; }       // Số điểm hạng sử dụng — :24
    public double? AmountChTotalInit { get; set; }   // Số điểm hạng thực — :25
    public double? AmountChTotalDiffer { get; set; } // Số điểm chênh hạng — :26
    public string? PointExpiryDTime { get; set; }    // Ngày hết hạn sử dụng điểm — :27
    public double? PointUsed { get; set; }           // Số điểm đã tiêu — :28
    public double? PointExpiry { get; set; }         // Điểm hết hạn — :29
    public double? PointRemain { get; set; }         // Còn lại có thể tiêu — :30
}

/// <summary>
/// Dòng kết quả danh sách Crd_CardTransactionInCreaseInst — port 1:1 model nguồn
/// (12.Dev.Common/idn.Loyalty.Common/Models/Crd_CardTransactionInCreaseInst.cs:9-34).
/// </summary>
public class Crd_CardTransactionInCreaseInstItem
{
    public long STT { get; set; }                    // STT — model :11
    public string? DLCode { get; set; }              // Mã đại lý — :12
    public string? DLCPName { get; set; }            // Tên đại lý — :13
    public long AutoId_IC { get; set; }              // Mã giao dịch — :14
    public string? CreateDate_IC { get; set; }       // Thời gian giao dịch (StdDate) — :15
    public string? RefNo_IC { get; set; }            // Số tham chiếu — :16
    public string? MemberNo { get; set; }            // Mã Hội viên — :17
    public string? CarNo { get; set; }               // Biển số — :18
    public string? MemberName { get; set; }          // Họ và tên — :19
    public string? CardTypeUse { get; set; }         // Hạng sử dụng — :20
    public string? CardTypeInit { get; set; }        // Hạng thực — :21
    public string? DealPointType { get; set; }       // Loại điểm — :22
    public string? DLCodeExceptionally { get; set; } // Đại lý yêu cầu đặc cách — :23
    public double? AmountChTotal { get; set; }       // Số điểm hạng sử dụng — :24
    public double? AmountChTotalInit { get; set; }   // Số điểm hạng thực — :25
    public double? AmountChTotalDiffer { get; set; } // Số điểm chênh hạng — :26
    public string? PointExpiryDTime { get; set; }    // Ngày hết hạn sử dụng điểm — :27
    public long? AutoId_DC { get; set; }             // Mã giao dịch tiêu điểm — :28
    public string? CreateDate_DC { get; set; }       // Ngày giao dịch tiêu điểm (StdDate) — :29
    public string? CardNo_DC { get; set; }           // Mã kỳ xét hạng tiêu điểm — :30
    public double? PointDeCrease { get; set; }       // Số điểm tiêu — :31
    public string? DLCode_DC { get; set; }           // Nơi tiêu điểm — :32
    public string? RefNo_DC { get; set; }            // Số tham chiếu — :33
}

[ApiController]
[Route("api/crd_cardtransactionincrease")]
[Produces("application/json")]
public class Crd_CardTransactionInCreaseControllerPort : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment? _env;

    public Crd_CardTransactionInCreaseControllerPort(AppDbContext db, IWebHostEnvironment? env = null)
    {
        _db = db;
        _env = env;
    }

    /// <summary>
    /// Từ điển 20 cột của Sheet 1 "Điểm tích" — port 1:1 DicColums() (gate Crd_CardTransactionInCreaseController.cs:262-287).
    /// </summary>
    private static readonly Dictionary<string, string> DicColNames = new()
    {
        { "STT", "STT" },
        { "DLCode", "Mã Đại lý" },
        { "DLCPName", "Tên Đại lý" },
        { "AutoId", "Mã giao dịch" },
        { "CreateDate", "Thời gian giao dịch" },
        { "RefNo", "Số tham chiếu" },
        { "MemberNo", "Mã hội viên" },
        { "CarNo", "Biển số" },
        { "MemberName", "Họ và tên" },
        { "CardTypeUse", "Hạng sử dụng" },
        { "CardTypeInit", "Hạng thực" },
        { "DealPointType", "Loại điểm" },
        { "DLCodeExceptionally", "Đại lý yêu cầu đặc cách" },
        { "AmountChTotal", "Số điểm hạng sử dụng" },
        { "AmountChTotalInit", "Số điểm hạng thực" },
        { "AmountChTotalDiffer", "Số điểm chênh hạng" },
        { "PointExpiryDTime", "Ngày hết hạn sử dụng điểm" },
        { "PointUsed", "Số điểm đã tiêu" },
        { "PointExpiry", "Điểm hết hạn" },
        { "PointRemain", "Còn lại có thể tiêu" }
    };

    /// <summary>
    /// Từ điển 23 cột của Sheet 2 "Chi tiết tiêu điểm" — port 1:1 DicColumsDetail() (gate Crd_CardTransactionInCreaseController.cs:289-316).
    /// </summary>
    private static readonly Dictionary<string, string> DicColNamesDetail = new()
    {
        { "STT", "STT" },
        { "DLCode", "Mã đại lý" },
        { "DLCPName", "Tên đại lý" },
        { "AutoId_IC", "Mã giao dịch" },
        { "CreateDate_IC", "Thời gian giao dịch" },
        { "RefNo_IC", "Số tham chiếu tích" },
        { "MemberNo", "Mã Hội viên" },
        { "CarNo", "Biển số" },
        { "MemberName", "Họ và tên" },
        { "CardTypeUse", "Hạng sử dụng" },
        { "CardTypeInit", "Hạng thực" },
        { "DealPointType", "Loại điểm" },
        { "DLCodeExceptionally", "Đại lý yêu cầu đặc cách" },
        { "AmountChTotal", "Số điểm hạng sử dụng" },
        { "AmountChTotalInit", "Số điểm hạng thực" },
        { "AmountChTotalDiffer", "Số điểm chênh hạng" },
        { "PointExpiryDTime", "Ngày hết hạn sử dụng điểm" },
        { "AutoId_DC", "Mã giao dịch tiêu điểm" },
        { "CreateDate_DC", "Ngày giao dịch tiêu điểm" },
        { "CardNo_DC", "Mã kỳ xét hạng tiêu điểm" },
        { "PointDeCrease", "Số điểm tiêu" },
        { "DLCode_DC", "Nơi tiêu điểm" },
        { "RefNo_DC", "Số tham chiếu tiêu" }
    };

    /// <summary>
    /// Ghi danh sách đối tượng ra một worksheet Excel với các tiêu đề cột theo từ điển.
    /// Nếu danh sách rỗng hoặc null, chỉ ghi dòng 1 (tiêu đề cột).
    /// </summary>
    private static void PopulateSheet<T>(IXLWorksheet ws, IEnumerable<T>? rows, IReadOnlyDictionary<string, string> dicColNames)
    {
        var cols = dicColNames.ToList();
        for (var c = 0; c < cols.Count; c++)
        {
            ws.Cell(1, c + 1).Value = cols[c].Value;
        }

        if (rows != null)
        {
            var r = 2;
            foreach (var row in rows)
            {
                if (row is null) continue;
                var t = row.GetType();
                for (var c = 0; c < cols.Count; c++)
                {
                    var p = t.GetProperty(cols[c].Key, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                    var v = p?.GetValue(row);
                    ws.Cell(r, c + 1).Value = v switch
                    {
                        null => Blank.Value,
                        DateTime d => d,
                        double d => d,
                        int i => i,
                        long l => l,
                        decimal m => m,
                        _ => Convert.ToString(v) ?? "",
                    };
                }
                r++;
            }
        }

        ws.Columns().AdjustToContents();
    }

    /// <summary>
    /// Port 1:1 Crd_CardTransactionInCreaseController.Search (gate :33-117 -> biz Report.cs:12184-12523).
    /// Lọc giao dịch tăng điểm theo thời gian (UTC), hội viên, đại lý, mã giao dịch; trả về hai danh sách
    /// Lst_Crd_CardTransactionInCrease và Lst_Crd_CardTransactionInCreaseInst.
    /// </summary>
    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Crd_CardTransactionInCreaseSearchReq? req)
    {
        var (lst_Crd_CardTransactionInCrease, lst_Crd_CardTransactionInCreaseInst) = await QueryDataAsync(req);

        // ---- 6. Trả về đúng cấu trúc Gate :100-104 (rỗng -> null) ----
        return Ok(new
        {
            Success = true,
            Data = new
            {
                Lst_Crd_CardTransactionInCrease = lst_Crd_CardTransactionInCrease,
                Lst_Crd_CardTransactionInCreaseInst = lst_Crd_CardTransactionInCreaseInst
            }
        });
    }

    /// <summary>
    /// Port 1:1 Crd_CardTransactionInCreaseController.Export (gate :121-215 -> biz Report.cs:12184-12523).
    /// Xuất file Excel chứa 2 sheet trong CÙNG 1 file:
    ///   - Sheet 1 "Điểm tích": danh sách Lst_Crd_CardTransactionInCrease với 20 cột từ DicColums() (:178, :187, :262-287).
    ///   - Sheet 2 "Chi tiết tiêu điểm": danh sách Lst_Crd_CardTransactionInCreaseInst với 23 cột từ DicColumsDetail() (:179, :195, :289-316).
    /// Nếu danh sách tăng điểm rỗng hoặc null -> trả lỗi "Không có dữ liệu" (gate :191 ném ServiceException).
    /// Trả về Data = { Url = GetRequestBaseAddress() + url } (:198-201).
    /// </summary>
    [HttpPost("export")]
    public async Task<IActionResult> Export([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Crd_CardTransactionInCreaseSearchReq? req)
    {
        var (lst_Crd_CardTransactionInCrease, lst_Crd_CardTransactionInCreaseInst) = await QueryDataAsync(req);

        // Gate :184-192: nếu danh sách tăng điểm rỗng hoặc null -> ném ServiceException("Không có dữ liệu")
        if (lst_Crd_CardTransactionInCrease == null || lst_Crd_CardTransactionInCrease.Count == 0)
        {
            return PortResult.Fail("Crd_CardTransactionInCrease_Export", "Message", "Không có dữ liệu");
        }

        // Tạo thư mục và đường dẫn file Excel xuất: GenExcelExportFilePath (gate :182)
        var webRoot = _env?.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
        {
            webRoot = _env != null ? Path.Combine(_env.ContentRootPath, "wwwroot") : Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        }

        try
        {
            var (physicalPath, virtualPath) = PortExcel.GenExcelExportFilePath(webRoot, "Crd_CardTransactionInCrease");

            using var wb = new XLWorkbook();

            // Sheet 1: "Điểm tích" với 20 cột từ DicColums() (gate :178, :187, :262-287)
            var ws1 = wb.Worksheets.Add("Điểm tích");
            PopulateSheet(ws1, lst_Crd_CardTransactionInCrease, DicColNames);

            // Sheet 2: "Chi tiết tiêu điểm" với 23 cột từ DicColumsDetail() (gate :179, :195, :289-316)
            var ws2 = wb.Worksheets.Add("Chi tiết tiêu điểm");
            PopulateSheet(ws2, lst_Crd_CardTransactionInCreaseInst, DicColNamesDetail);

            wb.SaveAs(physicalPath);

            // Trả về { Success = true, Data = { Url = GetRequestBaseAddress() + url } } (gate :198-201)
            return Ok(new
            {
                Success = true,
                Data = new
                {
                    Url = PortExcel.GetRequestBaseAddress(Request) + virtualPath
                }
            });
        }
        catch (Exception ex)
        {
            // Bọc try/catch riêng cho phần xuất Excel, trả lỗi Fail đúng tên action (bài học A.2, gate :204-208)
            return PortResult.Fail("Crd_CardTransactionInCrease_Export", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Hàm dùng chung truy vấn dữ liệu cho Search và Export theo chuỗi logic nghiệp vụ của nguồn
    /// (strWhereClause_Crd_CardTransactionInCrease :219-258 và Report.cs:12184-12523).
    /// </summary>
    private async Task<(List<Crd_CardTransactionInCreaseItem>? increaseList, List<Crd_CardTransactionInCreaseInstItem>? instList)> QueryDataAsync(Crd_CardTransactionInCreaseSearchReq? req)
    {
        var r = req ?? new Crd_CardTransactionInCreaseSearchReq();

        // ---- 1. Chuẩn hoá tham số lọc (strWhereClause_Crd_CardTransactionInCrease :219-258) ----
        // CreateDTimeUTCFrom: StdDTimeBeginDay -> StdDTimeUTC (:227-228) => đầu ngày VN (UTC+7) chuyển sang UTC.
        DateTime? utcFrom = null;
        if (!string.IsNullOrWhiteSpace(r.CreateDTimeUTCFrom)
            && DateTime.TryParse(r.CreateDTimeUTCFrom, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dtFrom))
        {
            utcFrom = dtFrom.Date.AddHours(-7);
        }

        // CreateDTimeUTCTo: StdDTimeEndDay -> StdDTimeUTC (:235-236) => cuối ngày VN (23:59:59, UTC+7) chuyển sang UTC.
        DateTime? utcTo = null;
        if (!string.IsNullOrWhiteSpace(r.CreateDTimeUTCTo)
            && DateTime.TryParse(r.CreateDTimeUTCTo, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dtTo))
        {
            utcTo = dtTo.Date.AddDays(1).AddSeconds(-1).AddHours(-7);
        }

        // MemberNo / DLCode / AutoId: "=" nguyên chuỗi (:241-254).
        var memberNo = string.IsNullOrWhiteSpace(r.MemberNo) ? null : r.MemberNo.Trim();
        var dlCode = string.IsNullOrWhiteSpace(r.DLCode) ? null : r.DLCode.Trim();
        long? autoId = null;
        if (!string.IsNullOrWhiteSpace(r.AutoId) && long.TryParse(r.AutoId.Trim(), out var parsedAutoId))
        {
            autoId = parsedAutoId;
        }

        // ---- 2. #tbl_Crd_CardTransaction_Filter_Draft (:12256-12276) ----
        // from Crd_CardTransaction cct
        //   inner join Crd_Member cm on cct.MemberNo = cm.MemberNo
        //   inner join Mst_DealerCP md on cct.DLCode = md.DLCPCode
        // where cct.AmountChTotal > 0 and cct.CreateDTimeUTC >= '2023-06-30 17:00:00'
        var draftQuery = from cct in _db.Set<CrdCardTransaction>()
                         join cm in _db.Set<CrdMember>() on cct.MemberNo equals cm.MemberNo
                         join md in _db.Set<Mst_DealerCP>() on cct.DLCode equals md.DLCPCode
                         where (cct.AmountChTotal ?? 0) > 0
                               && cct.CreateDTimeUTC >= new DateTime(2023, 6, 30, 17, 0, 0, DateTimeKind.Utc)
                         select cct;

        // strFt_WhereClause (điều kiện lọc động từ strWhereClause_Crd_CardTransactionInCrease):
        if (utcFrom.HasValue)
        {
            var v = utcFrom.Value;
            draftQuery = draftQuery.Where(cct => cct.CreateDTimeUTC >= v);
        }
        if (utcTo.HasValue)
        {
            var v = utcTo.Value;
            draftQuery = draftQuery.Where(cct => cct.CreateDTimeUTC <= v);
        }
        if (memberNo != null)
        {
            var pattern = PortQuery.Equal(memberNo);
            draftQuery = draftQuery.Where(cct => EF.Functions.Like((cct.MemberNo ?? "").ToLower(), pattern, PortQuery.Esc));
        }
        if (dlCode != null)
        {
            var pattern = PortQuery.Equal(dlCode);
            draftQuery = draftQuery.Where(cct => EF.Functions.Like((cct.DLCode ?? "").ToLower(), pattern, PortQuery.Esc));
        }
        if (autoId.HasValue)
        {
            var v = autoId.Value;
            draftQuery = draftQuery.Where(cct => cct.AutoId == v);
        }

        // order by cct.CreateDTimeUTC asc, cast(cct.AutoId as bigint) asc (:12273-12275)
        var draft = await draftQuery
            .OrderBy(cct => cct.CreateDTimeUTC)
            .ThenBy(cct => cct.AutoId)
            .Select(cct => new { cct.AutoId, cct.CreateDTimeUTC })
            .ToListAsync();

        // ---- 3. Summary: select Count(0) MyCount (:12278-12280) ----
        var itemCount = draft.Count;

        // ---- 4. #tbl_Crd_CardTransaction_Filter: lọc MyIdxSeq >= nFilterRecordStart và <= nFilterRecordEnd (:12282-12290) ----
        // nFilterRecordStart = Convert.ToInt64(strFt_RecordStart); nFilterRecordEnd = nFilterRecordStart + nFilterRecordCount - 1 (:12218-12219).
        // Gate cố định Ft_RecordStart = "0", Ft_RecordCount = Ft_RecordCount (:80-81).
        // MyIdxSeq là identity(bigint,0,1) nên MyIdxSeq = vị trí 0-based trong danh sách đã sắp xếp.
        var filtered = draft
            .Select((x, idx) => new { x.AutoId, MyIdxSeq = (long)idx })
            .ToList();

        // ---- 5. Truy vấn chi tiết 2 danh sách kết quả (:12292-12466) ----
        var autoIds = filtered.Select(x => x.AutoId).ToList();

        List<Crd_CardTransactionInCreaseItem>? lst_Crd_CardTransactionInCrease = null;
        List<Crd_CardTransactionInCreaseInstItem>? lst_Crd_CardTransactionInCreaseInst = null;

        if (autoIds.Count > 0)
        {
            // ---- 5a. Crd_CardTransactionInCrease (:12411-12463) ----
            // PointUsed = sum(PointDeCrease) group by AutoId_IC từ #tbl_Crd_CardTransactionInCreaseInst (:12389-12398).
            // PointExpiry = isnull(rpectich.PointExpiry,0) từ RptPE_CrdTransationInCreaseHist (:12400-12409).
            var pointUsedMap = await _db.Set<RptPE_CrdTransationDeCreaseInst>()
                .Where(x => autoIds.Contains(x.RefNoIC))
                .GroupBy(x => x.RefNoIC)
                .Select(g => new { RefNoIC = g.Key, PointUsed = g.Sum(x => x.PointDeCrease ?? 0) })
                .ToListAsync();

            var pointExpiryMap = await _db.Set<RptPE_CrdTransationInCreaseHist>()
                .Where(x => autoIds.Contains(x.RefNo))
                .Select(x => new { x.RefNo, x.PointExpiry })
                .ToListAsync();

            var increaseRows = await (from cct in _db.Set<CrdCardTransaction>()
                                      join cm in _db.Set<CrdMember>() on cct.MemberNo equals cm.MemberNo
                                      join md in _db.Set<Mst_DealerCP>() on cct.DLCode equals md.DLCPCode
                                      where autoIds.Contains(cct.AutoId)
                                      orderby cct.CreateDTimeUTC
                                      select new
                                      {
                                          cct,
                                          cm.CarNo,
                                          cm.MemberName,
                                          cm.DLCodeExceptionally,
                                          md.DLCPName
                                      }).ToListAsync();

            var increaseList = new List<Crd_CardTransactionInCreaseItem>();
            long stt = 1;
            foreach (var row in increaseRows)
            {
                var cct = row.cct;
                var pointUsed = pointUsedMap.FirstOrDefault(x => x.RefNoIC == cct.AutoId)?.PointUsed;
                var pointExpiry = pointExpiryMap.FirstOrDefault(x => x.RefNo == cct.AutoId)?.PointExpiry;

                // PointRemain (:12432-12438):
                //   when PointExpiry có dòng (AutoId not null) then 0
                //   when PointUsed có dòng (AutoId not null) then (AmountChTotal - PointUsed)
                //   else AmountChTotal
                var hasExpiry = pointExpiryMap.Any(x => x.RefNo == cct.AutoId);
                var hasUsed = pointUsedMap.Any(x => x.RefNoIC == cct.AutoId);
                double? pointRemain = hasExpiry ? 0d
                    : hasUsed ? ((cct.AmountChTotal ?? 0) - (pointUsed ?? 0))
                    : (cct.AmountChTotal ?? 0);

                increaseList.Add(new Crd_CardTransactionInCreaseItem
                {
                    STT = stt++,
                    DLCode = cct.DLCode,
                    DLCPName = row.DLCPName,
                    AutoId = cct.AutoId,
                    // CreateDate = StdDate(StdDTimeVN(CreateDTimeUTC)) (:11974-11981)
                    CreateDate = FormatStdDate(cct.CreateDTimeUTC),
                    RefNo = cct.RefNo,
                    MemberNo = cct.MemberNo,
                    CarNo = row.CarNo,
                    MemberName = row.MemberName,
                    CardTypeUse = cct.CardTypeUse,
                    CardTypeInit = cct.CardTypeInit,
                    DealPointType = cct.DealPointType,
                    DLCodeExceptionally = row.DLCodeExceptionally,
                    AmountChTotal = cct.AmountChTotal,
                    AmountChTotalInit = cct.AmountChTotalInit,
                    AmountChTotalDiffer = cct.AmountChTotalDiffer,
                    PointExpiryDTime = FormatStdDate(cct.PointExpiryDTime),
                    PointUsed = pointUsed ?? 0d,
                    PointExpiry = pointExpiry ?? 0d,
                    PointRemain = pointRemain
                });
            }

            if (increaseList.Count > 0)
            {
                lst_Crd_CardTransactionInCrease = increaseList;
            }

            // ---- 5b. Crd_CardTransactionInCreaseInst (:12311-12375) ----
            // from Crd_CardTransaction cct_ic
            //   inner join #tbl_Crd_CardTransaction_Filter t on cct_ic.AutoId = t.AutoId
            //   inner join RptPE_CrdTransationDeCreaseInst rpectdci on cct_ic.AutoId = rpectdci.RefNoIC
            //   inner join Crd_CardTransaction cct_dc on rpectdci.RefNoDC = cct_dc.AutoId
            //   inner join Mst_DealerCP md on cct_ic.DLCode = md.DLCPCode
            //   inner join Crd_Member cm on cct_ic.MemberNo = cm.MemberNo
            // where rpectdci.PeriodMonth <= '@strPeriodMonth'
            var periodMonth = NormalizePeriodMonth(r.PeriodMonth);

            var instRows = await (from cct_ic in _db.Set<CrdCardTransaction>()
                                  join rpectdci in _db.Set<RptPE_CrdTransationDeCreaseInst>() on cct_ic.AutoId equals rpectdci.RefNoIC
                                  join cct_dc in _db.Set<CrdCardTransaction>() on rpectdci.RefNoDC equals cct_dc.AutoId
                                  join md in _db.Set<Mst_DealerCP>() on cct_ic.DLCode equals md.DLCPCode
                                  join cm in _db.Set<CrdMember>() on cct_ic.MemberNo equals cm.MemberNo
                                  where autoIds.Contains(cct_ic.AutoId)
                                        && string.Compare(rpectdci.PeriodMonth, periodMonth) <= 0
                                  orderby cct_ic.CreateDTimeUTC
                                  select new
                                  {
                                      cct_ic,
                                      cct_dc,
                                      rpectdci,
                                      md.DLCPName,
                                      cm.CarNo,
                                      cm.MemberName,
                                      cm.DLCodeExceptionally
                                  }).ToListAsync();

            var instList = new List<Crd_CardTransactionInCreaseInstItem>();
            long sttIc = 1;
            foreach (var row in instRows)
            {
                var cct_ic = row.cct_ic;
                var cct_dc = row.cct_dc;

                // DLCode_DC / RefNo_DC (:12335-12348): CASE theo strDLCPCode (quyền xem người dùng).
                // Đích không có phiên đăng nhập -> strDLCPCode = null -> luôn rơi vào 'ĐL khác' / 'N/A'.
                var dlCodeDc = "ĐL khác";
                var refNoDc = "N/A";

                instList.Add(new Crd_CardTransactionInCreaseInstItem
                {
                    STT = sttIc++,
                    DLCode = cct_ic.DLCode,
                    DLCPName = row.DLCPName,
                    AutoId_IC = cct_ic.AutoId,
                    // CreateDate_IC = StdDate(StdDTimeVN(CreateDTimeUTC_IC)) (:11992-12005)
                    CreateDate_IC = FormatStdDate(cct_ic.CreateDTimeUTC),
                    RefNo_IC = cct_ic.RefNo,
                    MemberNo = cct_ic.MemberNo,
                    CarNo = row.CarNo,
                    MemberName = row.MemberName,
                    CardTypeUse = cct_ic.CardTypeUse,
                    CardTypeInit = cct_ic.CardTypeInit,
                    DealPointType = cct_ic.DealPointType,
                    DLCodeExceptionally = row.DLCodeExceptionally,
                    AmountChTotal = cct_ic.AmountChTotal,
                    AmountChTotalInit = cct_ic.AmountChTotalInit,
                    AmountChTotalDiffer = cct_ic.AmountChTotalDiffer,
                    PointExpiryDTime = FormatStdDate(cct_ic.PointExpiryDTime),
                    AutoId_DC = row.rpectdci.RefNoDC,
                    // CreateDate_DC = StdDate(StdDTimeVN(CreateDTimeUTC_DC)) (:11992-12005)
                    CreateDate_DC = FormatStdDate(cct_dc.CreateDTimeUTC),
                    CardNo_DC = cct_dc.CardNo,
                    PointDeCrease = row.rpectdci.PointDeCrease,
                    DLCode_DC = dlCodeDc,
                    RefNo_DC = refNoDc
                });
            }

            if (instList.Count > 0)
            {
                lst_Crd_CardTransactionInCreaseInst = instList;
            }
        }

        return (lst_Crd_CardTransactionInCrease, lst_Crd_CardTransactionInCreaseInst);
    }

    /// <summary>
    /// Định dạng ngày theo nguồn: StdDTimeVN (UTC+7, "yyyy-MM-dd HH:mm:ss") rồi StdDate lấy 10 ký tự đầu (:11974-11981).
    /// Trả null khi giá trị null (nguồn StdDTimeVN(null) -> rỗng).
    /// </summary>
    private static string? FormatStdDate(DateTime? dtimeUtc)
    {
        if (!dtimeUtc.HasValue)
        {
            return null;
        }
        var vn = dtimeUtc.Value.AddHours(7);
        return vn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Chuẩn hoá PeriodMonth về "yyyy-MM-01" như nguồn (:12223-12225):
    /// dtimePeriodMonth = new DateTime(year, month, 1); strPeriodMonth = "yyyy-MM-01".
    /// </summary>
    private static string NormalizePeriodMonth(string? periodMonth)
    {
        if (!string.IsNullOrWhiteSpace(periodMonth)
            && DateTime.TryParse(periodMonth, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
        {
            return new DateTime(dt.Year, dt.Month, 1).ToString("yyyy-MM-01", CultureInfo.InvariantCulture);
        }
        // Nguồn Convert.ToDateTime(strPeriodMonth) sẽ ném lỗi nếu rỗng/không hợp lệ; ở đích dùng giá trị
        // lớn nhất để điều kiện "rpectdci.PeriodMonth <= @strPeriodMonth" không lọc bớt dòng nào.
        return "9999-12-01";
    }
}
