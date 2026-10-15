using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniLoyalty.Data;
using MiniLoyalty.Models;
using MiniLoyalty.Models.Port;

namespace MiniLoyalty.Controllers.Port;

// ============================================================================================
// PORT 1:1 — màn truy vấn hội viên theo đại lý (Map_QueryDealer_Member) của dự án nguồn 2022.7.Loyalty.
//   - Hạng mục #66: Search -> WA_Map_QueryDealer_Member_Get
//   - Hạng mục #67: SearchForRePort -> WA_Rpt_Map_QueryDealer_Member
//   - Hạng mục #69: ExportExcel -> WA_Rpt_Map_QueryDealer_Member (xuất Excel báo cáo)
//
// Nguồn Search (#66):
//   Gate: Map_QueryDealer_MemberController.cs:31-104 Search -> WA_Map_QueryDealer_Member_Get
//   API:  MapQueryDealerMemberController.cs:95-165 WA_Map_QueryDealer_Member_Get
//   Biz:  Master.Loyalty.cs:20933-21140 Map_QueryDealer_Member_GetX (Draft :21002-21018, Final :21052-21067)
//
// Nguồn SearchForRePort (#67):
//   Gate: Map_QueryDealer_MemberController.cs:106-195 SearchForRePort -> WA_Rpt_Map_QueryDealer_Member
//   Hàm lọc: Map_QueryDealer_MemberController.cs:199-259 strWhereClause_Map_QueryDealer_Member
//   API:  ReportController.cs:865 WA_Rpt_Map_QueryDealer_Member
//   Biz:  Report.cs:11398-11900 -> Rpt_Map_QueryDealer_MemberX (:11645-11900)
//
// Nguồn ExportExcel (#69):
//   Gate: Map_QueryDealer_MemberController.cs:333-422 ExportExcel -> WA_Rpt_Map_QueryDealer_Member
//   Từ điển cột: Map_QueryDealer_MemberController.cs:426-442 GetImportDicColums (11 cột)
//   API:  ReportController.cs:865 WA_Rpt_Map_QueryDealer_Member
//   Biz:  Report.cs:11398-11900 -> Rpt_Map_QueryDealer_MemberX (:11645-11900)
//
// GIỮ ĐÚNG NGUỒN VÀ ĐIỂM KHÁC BIỆT #67 / #69 VS #66:
//   * Endpoint #67: POST /api/map_querydealer_member/searchforreport (gate Map_QueryDealer_MemberController.cs:106).
//   * Endpoint #69: POST /api/map_querydealer_member/exportexcel (gate Map_QueryDealer_MemberController.cs:333).
//   * Tham số gate #67 và #69: 7 tham số QueryDateFrom, QueryDateTo, MemberNo, PhoneNo, DLCPCode, CarNo, VIN (:106-107, :333-334).
//     - Khác #66: KHÔNG có CardNo, IDCardNo, EffDateStartFrom/To, PointRefFrom/To, FlagPoint.
//     - QueryDateFrom / QueryDateTo: Ở #66 là nhánh chết; ở #67 và #69 ĐƯỢC TRUYỀN VÀO hàm lọc (:149-157)
//       tạo điều kiện Map_QueryDealer_Member.QueryDate >= và QueryDate <=.
//     - Parse ngày theo InvariantCulture; sai định dạng ngày -> SQL lỗi chuyển kiểu -> trả HTTP 400 ErridnLoyalty.Rpt_Map_QueryDealer_Member (Report.cs:11526).
//   * Bước 1: Draft (#tbl_Map_QueryDealer_Member_Filter_Draft Report.cs:11708-11726) tương tự #66, dùng chung logic.
//   * Bước 2: Final (#tbl_Map_QueryDealer_Member_Final Report.cs:11759-11788):
//     - Tách phần tính dòng Final thành hàm private GetReportRowsAsync dùng chung cho SearchForRePort (#67) và ExportExcel (#69).
//     - Join: t, Map_QueryDealer_Member mqdm, Crd_Member cm, left join Mst_DealerCP mdcp, left join Mst_Province mp (:11775-11783).
//     - ĐIỀU KIỆN QUAN TRỌNG (:11785): where mqdm.DLCPCode <> cm.DLCodeRegis (CHỈ giữ lượt tra cứu của đại lý KHÁC đại lý mở thẻ).
//     - Cột trả về: t.MyIdxSeq, mqdm.*, mdcp.DLCPName, cm.VIN, cm.CarNo, cm.PhoneNo, cm.CardTypeUse, cm.MemberName,
//       cm.ModelCode, cm.DLCodeRegis, mp.ProvinceName, cm.ModelName, cm.ModelCode + ':' + cm.ModelName ModelCodeName (:11760-11772).
//     - ModelCodeName (:11772): Toán tử `+` trong SQL trả về null nếu ModelCode HOẶC ModelName null.
//     - Sắp xếp: order by mqdm.LogLUDTimeUTC desc (:11787).
//   * Output SearchForRePort (#67) (gate :177-182): Data = new { Lst_Map_QueryDealer_Member (null nếu rỗng), Lst_Rpt_Map_QueryDealer_Member, CountRow = MyCount }.
//   * Output ExportExcel (#69) (gate :391-409):
//     - Nếu danh sách rỗng -> throw ServiceException("Không có dữ liệu") (:406-409) -> 400 PortResult.Fail("Map_QueryDealer_Member_ExportExcel", "Message", "Không có dữ liệu").
//     - Từ điển 11 cột GetImportDicColums() (:426-442) đúng thứ tự: MemberNo, QueryDate, DLCPCode, DLCPName, MemberName, PhoneNo, CardTypeUse, ProvinceName, CarNo, ModelCodeName, DLCodeRegis.
//     - File prefix & tên sheet "Map_QueryDealer_Member" (:397-398).
//     - Trả Data = { Url = PortExcel.GetRequestBaseAddress(Request) + virtualPath } (:399-403).
//   * Ability (Report.cs:11682-11694): Lấy DLCPCode của user đăng nhập; nếu khác rỗng và khác "HTV" thì lọc mqdm.DLCPCode = <đại lý user>.
//     Đích CHƯA có Sys_User / session user -> coi như HTV (không lọc đại lý).
//     GAP: khi có Sys_User / session user phải giới hạn theo đại lý của user.
// ============================================================================================

/// <summary>
/// Port 1:1 tham số request của Gate Search (Map_QueryDealer_MemberController.cs:31-32).
/// Đúng 12 tham số nhận từ client.
/// </summary>
public class MapQueryDealerMemberSearchReq
{
    public string? EffDateStartFrom { get; set; }   // :31 — Nhận nhưng hàm lọc không dùng
    public string? EffDateStartTo { get; set; }     // :31 — Nhận nhưng hàm lọc không dùng
    public string? CardNo { get; set; }             // :31 — Lọc Crd_Card.CardNo =
    public string? IDCardNo { get; set; }           // :31 — Lọc Crd_Member.IDCardNo =
    public string? MemberNo { get; set; }           // :31 — Lọc Map_QueryDealer_Member.MemberNo =
    public string? PhoneNo { get; set; }            // :31 — Lọc Crd_Member.PhoneNo =
    public string? DLCPCode { get; set; }           // :32 — Tách dấu phẩy -> Map_QueryDealer_Member.DLCPCode IN
    public string? PointRefFrom { get; set; }       // :32 — Gate không truyền vào hàm lọc
    public string? PointRefTo { get; set; }         // :32 — Gate không truyền vào hàm lọc
    public string? FlagPoint { get; set; }          // :32 — Gate không truyền vào hàm lọc
    public string? CarNo { get; set; }              // :32 — Lọc Crd_Member.CarNo =
    public string? VIN { get; set; }                // :32 — Lọc Crd_Member.VIN =
}

/// <summary>
/// Port 1:1 tham số request của Gate SearchForRePort (Map_QueryDealer_MemberController.cs:106-107).
/// Đúng 7 tham số nhận từ client.
/// </summary>
public class MapQueryDealerMemberSearchForReportReq
{
    public string? QueryDateFrom { get; set; }      // :106 — Map_QueryDealer_Member.QueryDate >=
    public string? QueryDateTo { get; set; }        // :106 — Map_QueryDealer_Member.QueryDate <=
    public string? MemberNo { get; set; }           // :106 — Map_QueryDealer_Member.MemberNo =
    public string? PhoneNo { get; set; }            // :106 — Crd_Member.PhoneNo =
    public string? DLCPCode { get; set; }           // :107 — Tách dấu phẩy -> Map_QueryDealer_Member.DLCPCode IN
    public string? CarNo { get; set; }              // :107 — Crd_Member.CarNo =
    public string? VIN { get; set; }                // :107 — Crd_Member.VIN =
}

/// <summary>
/// Port 1:1 tham số request của Gate ExportExcel (Map_QueryDealer_MemberController.cs:333-334).
/// Đúng 7 tham số nhận từ client: QueryDateFrom, QueryDateTo, MemberNo, PhoneNo, DLCPCode, CarNo, VIN.
/// </summary>
public class MapQueryDealerMemberExportExcelReq
{
    public string? QueryDateFrom { get; set; }      // :333 — Map_QueryDealer_Member.QueryDate >=
    public string? QueryDateTo { get; set; }        // :333 — Map_QueryDealer_Member.QueryDate <=
    public string? MemberNo { get; set; }           // :333 — Map_QueryDealer_Member.MemberNo =
    public string? PhoneNo { get; set; }            // :333 — Crd_Member.PhoneNo =
    public string? DLCPCode { get; set; }           // :334 — Tách dấu phẩy -> Map_QueryDealer_Member.DLCPCode IN
    public string? CarNo { get; set; }              // :334 — Crd_Member.CarNo =
    public string? VIN { get; set; }                // :334 — Crd_Member.VIN =
}

/// <summary>
/// Dòng kết quả báo cáo dùng chung cho SearchForRePort (#67) và ExportExcel (#69)
/// tương ứng với các cột của bảng tạm #tbl_Map_QueryDealer_Member_Final (Report.cs:11760-11772).
/// </summary>
public class MapQueryDealerMemberReportRow
{
    public long MyIdxSeq { get; set; }              // :11760 MyIdxSeq
    public string DLCPCode { get; set; } = "";      // :11761 mqdm.* (DLCPCode)
    public string MemberNo { get; set; } = "";      // :11761 MemberNo
    public string NetworkID { get; set; } = "";     // :11761 NetworkID
    public DateTime QueryDate { get; set; }         // :11761 QueryDate
    public string? Remark { get; set; }             // :11761 Remark
    public string? FlagActive { get; set; }         // :11761 FlagActive
    public DateTime? LogLUDTimeUTC { get; set; }    // :11761 LogLUDTimeUTC
    public string? LogLUBy { get; set; }            // :11761 LogLUBy
    public string? DLCPName { get; set; }           // :11762 mdcp.DLCPName
    public string? VIN { get; set; }                // :11763 cm.VIN
    public string? CarNo { get; set; }              // :11764 cm.CarNo
    public string? PhoneNo { get; set; }            // :11765 cm.PhoneNo
    public string? CardTypeUse { get; set; }        // :11766 cm.CardTypeUse
    public string? MemberName { get; set; }         // :11767 cm.MemberName
    public string? ModelCode { get; set; }          // :11768 cm.ModelCode
    public string? DLCodeRegis { get; set; }        // :11769 cm.DLCodeRegis
    public string? ProvinceName { get; set; }       // :11770 mp.ProvinceName
    public string? ModelName { get; set; }          // :11771 cm.ModelName
    public string? ModelCodeName { get; set; }      // :11772 cm.ModelCode + ':' + cm.ModelName
}

[ApiController]
[Route("api/map_querydealer_member")]
[Produces("application/json")]
public class Map_QueryDealer_MemberControllerPort : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public Map_QueryDealer_MemberControllerPort(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    /// <summary>
    /// Port 1:1 Map_QueryDealer_MemberController.Search (gate Map_QueryDealer_MemberController.cs:31-104).
    /// </summary>
    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody] MapQueryDealerMemberSearchReq? req)
    {
        var r = req ?? new MapQueryDealerMemberSearchReq();

        // Gate: RecordStart = "0", RecordCount = "123456000" (gate Map_QueryDealer_MemberController.cs:77-78).
        var rows = await GetXAsync(r, 0, 123456000);

        // Gate :88-91: Data = new { DataList = lst_Map_QueryDealer_Member } (null nếu danh sách rỗng, không có ItemCount/PageCount).
        return Ok(new
        {
            Success = true,
            Data = new
            {
                DataList = rows.Count > 0 ? rows : null
            }
        });
    }

    /// <summary>
    /// Port 1:1 Map_QueryDealer_MemberController.SearchForRePort (gate Map_QueryDealer_MemberController.cs:106-195).
    /// Báo cáo truy vấn hội viên theo đại lý (Report.cs:11645-11900 Rpt_Map_QueryDealer_MemberX).
    /// </summary>
    [HttpPost("searchforreport")]
    public async Task<IActionResult> SearchForRePort([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] MapQueryDealerMemberSearchForReportReq? req)
    {
        var r = req ?? new MapQueryDealerMemberSearchForReportReq();

        var (error, rows, countMember) = await GetReportRowsAsync(
            r.QueryDateFrom,
            r.QueryDateTo,
            r.MemberNo,
            r.PhoneNo,
            r.DLCPCode,
            r.CarNo,
            r.VIN);

        if (error != null)
        {
            return error;
        }

        // Summary CountRow (Report.cs:11791): select Count(0) MyCount from #tbl_Map_QueryDealer_Member_Final
        // Đếm số dòng của bảng Final SAU KHI lọc <> (CountRow sau lọc)
        int countRow = rows.Count;

        // Rpt CountMember (Report.cs:11809-11823): count(distinct MemberNo) trong bảng Final
        // Luôn trả về mảng 1 phần tử [{ CountMember = n }] kể cả khi n = 0
        var lstRpt = new List<object> { new { CountMember = countMember } };

        // Gate Map_QueryDealer_MemberController.cs:166-182:
        // Lst_Map_QueryDealer_Member = null nếu rỗng (:166-169)
        return Ok(new
        {
            Success = true,
            Data = new
            {
                Lst_Map_QueryDealer_Member = rows.Count > 0 ? rows : null,
                Lst_Rpt_Map_QueryDealer_Member = lstRpt,
                CountRow = countRow
            }
        });
    }

    /// <summary>
    /// Port 1:1 Map_QueryDealer_MemberController.ExportExcel (gate Map_QueryDealer_MemberController.cs:333-422).
    /// Xuất báo cáo truy vấn hội viên theo đại lý ra file Excel.
    /// Nguồn gate :333-422, từ điển cột :426-442, biz Report.cs:11645-11900.
    /// </summary>
    [HttpPost("exportexcel")]
    public async Task<IActionResult> ExportExcel([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] MapQueryDealerMemberExportExcelReq? req)
    {
        var r = req ?? new MapQueryDealerMemberExportExcelReq();

        // 1. Lấy dữ liệu báo cáo: dùng chung logic với SearchForRePort (#67)
        // Tham số + hàm lọc + service Y HỆT SearchForRePort (gate :333-334): QueryDateFrom, QueryDateTo, MemberNo, PhoneNo, DLCPCode, CarNo, VIN
        var (error, rows, _) = await GetReportRowsAsync(
            r.QueryDateFrom,
            r.QueryDateTo,
            r.MemberNo,
            r.PhoneNo,
            r.DLCPCode,
            r.CarNo,
            r.VIN);

        if (error != null)
        {
            return error;
        }

        // 2. Kiểm tra danh sách rỗng (gate :391, :406-409):
        // if (objRT_Map_QueryDealer_Member.Lst_Map_QueryDealer_Member != null && objRT_Map_QueryDealer_Member.Lst_Map_QueryDealer_Member.Count > 0)
        // else throw new mbiz.core.Exceptions.ServiceException("Không có dữ liệu");
        // -> PortResult.Fail("Map_QueryDealer_Member_ExportExcel", "Message", "Không có dữ liệu")
        if (rows == null || rows.Count == 0)
        {
            return PortResult.Fail("Map_QueryDealer_Member_ExportExcel", "Message", "Không có dữ liệu");
        }

        // 3. Tạo thư mục và đường dẫn file Excel xuất: GenExcelExportFilePath (gate :397)
        var webRoot = _env.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
        {
            webRoot = Path.Combine(_env.ContentRootPath, "wwwroot");
        }

        // 4. Từ điển 11 cột GetImportDicColums() (gate :395, :426-442) đúng thứ tự:
        //    MemberNo -> "Mã hội viên", QueryDate -> "Ngày truy vấn", DLCPCode -> "Mã đại lý truy vấn",
        //    DLCPName -> "Tên đại lý", MemberName -> "Tên hội viên", PhoneNo -> "Điện thoại",
        //    CardTypeUse -> "Hạng thẻ", ProvinceName -> "Tỉnh/TP", CarNo -> "Biển số xe",
        //    ModelCodeName -> "Model", DLCodeRegis -> "Đại lý mở thẻ"
        var dicColNames = GetImportDicColums();
        var (physicalPath, virtualPath) = PortExcel.GenExcelExportFilePath(webRoot, "Map_QueryDealer_Member");

        // 5. Ghi file Excel: ExcelExport.ExportToExcelFromList(list, dicColNames, filePath, "Map_QueryDealer_Member") (gate :398)
        PortExcel.ExportToExcelFromList(rows, dicColNames, physicalPath, "Map_QueryDealer_Member");

        // 6. Trả URL tải file (gate :399-403):
        // objJsonResultEntry.SetSuccess();
        // objJsonResultEntry.Data = new { Url = string.Format("{0}{1}", GetRequestBaseAddress(), url) };
        return Ok(new
        {
            Success = true,
            Data = new
            {
                Url = $"{PortExcel.GetRequestBaseAddress(Request)}{virtualPath}"
            }
        });
    }

    /// <summary>
    /// Từ điển 11 cột xuất Excel theo đúng thứ tự của gate Map_QueryDealer_MemberController.cs:426-442.
    /// </summary>
    private static Dictionary<string, string> GetImportDicColums()
    {
        return new Dictionary<string, string>
        {
            { "MemberNo", "Mã hội viên" },          // :430
            { "QueryDate", "Ngày truy vấn" },       // :431
            { "DLCPCode", "Mã đại lý truy vấn" },   // :432
            { "DLCPName", "Tên đại lý" },           // :433
            { "MemberName", "Tên hội viên" },       // :434
            { "PhoneNo", "Điện thoại" },            // :435
            { "CardTypeUse", "Hạng thẻ" },          // :436
            { "ProvinceName", "Tỉnh/TP" },          // :437
            { "CarNo", "Biển số xe" },              // :438
            { "ModelCodeName", "Model" },           // :439
            { "DLCodeRegis", "Đại lý mở thẻ" }      // :440
        };
    }

    /// <summary>
    /// Thực hiện nghiệp vụ báo cáo dùng chung cho SearchForRePort (#67) và ExportExcel (#69).
    /// Nguồn SQL: Report.cs:11645-11900 (Rpt_Map_QueryDealer_MemberX).
    /// Trả về: (IActionResult? Error, List&lt;MapQueryDealerMemberReportRow&gt; Rows, int DistinctMemberCount).
    /// </summary>
    private async Task<(IActionResult? Error, List<MapQueryDealerMemberReportRow> Rows, int DistinctMemberCount)> GetReportRowsAsync(
        string? queryDateFromStr,
        string? queryDateToStr,
        string? memberNo,
        string? phoneNo,
        string? dlcpCode,
        string? carNo,
        string? vin)
    {
        // Parse QueryDateFrom và QueryDateTo theo InvariantCulture.
        // Nếu sai định dạng ngày -> SQL lỗi chuyển kiểu kẹp trong try/catch -> 400 ErridnLoyalty.Rpt_Map_QueryDealer_Member (Report.cs:11526).
        DateTime? queryDateFrom = null;
        if (!string.IsNullOrWhiteSpace(queryDateFromStr))
        {
            if (DateTime.TryParse(queryDateFromStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dtFrom))
            {
                queryDateFrom = DateTime.SpecifyKind(dtFrom, DateTimeKind.Utc);
            }
            else
            {
                return (PortResult.Fail("Rpt_Map_QueryDealer_Member", "Check.QueryDate", queryDateFromStr), new List<MapQueryDealerMemberReportRow>(), 0);
            }
        }

        DateTime? queryDateTo = null;
        if (!string.IsNullOrWhiteSpace(queryDateToStr))
        {
            if (DateTime.TryParse(queryDateToStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dtTo))
            {
                queryDateTo = DateTime.SpecifyKind(dtTo, DateTimeKind.Utc);
            }
            else
            {
                return (PortResult.Fail("Rpt_Map_QueryDealer_Member", "Check.QueryDate", queryDateToStr), new List<MapQueryDealerMemberReportRow>(), 0);
            }
        }

        // Gate: RecordStart = "0", RecordCount = "123456000" (gate Map_QueryDealer_MemberController.cs:156-157 và :381-382).
        var draftWithSeq = await GetDraftItemsAsync(
            cardNo: null,
            idCardNo: null,
            dlcpCode: dlcpCode,
            memberNo: memberNo,
            carNo: carNo,
            phoneNo: phoneNo,
            vin: vin,
            queryDateFrom: queryDateFrom,
            queryDateTo: queryDateTo,
            recordStart: 0,
            recordCount: 123456000
        );

        if (draftWithSeq.Count == 0)
        {
            return (null, new List<MapQueryDealerMemberReportRow>(), 0);
        }

        // -----------------------------------------------------------------------------------------
        // BƯỚC 2: SELECT CUỐI cho #67 và #69 (Report.cs:11759-11788)
        // select
        //     t.MyIdxSeq
        //     , mqdm.*
        //     , mdcp.DLCPName
        //     , cm.VIN
        //     , cm.CarNo
        //     , cm.PhoneNo
        //     , cm.CardTypeUse
        //     , cm.MemberName
        //     , cm.ModelCode
        //     , cm.DLCodeRegis
        //     , mp.ProvinceName
        //     , cm.ModelName
        //     , cm.ModelCode + ':' + cm.ModelName ModelCodeName
        // into #tbl_Map_QueryDealer_Member_Final
        // from #tbl_Map_QueryDealer_Member_Filter t
        //     inner join Map_QueryDealer_Member mqdm on t.DLCPCode = mqdm.DLCPCode and t.MemberNo = mqdm.MemberNo
        //     inner join Crd_Member cm on mqdm.MemberNo = cm.MemberNo
        //     left join Mst_DealerCP mdcp on mqdm.DLCPCode = mdcp.DLCPCode
        //     left join Mst_Province mp on cm.ProvinceCode = mp.ProvinceCode
        // where (1=1)
        //     and mqdm.DLCPCode <> cm.DLCodeRegis  -- ĐIỀU KIỆN QUAN TRỌNG: chỉ giữ lượt tra cứu của đại lý KHÁC đại lý mở thẻ (:11785)
        // order by mqdm.LogLUDTimeUTC desc
        // -----------------------------------------------------------------------------------------
        var memberNos = draftWithSeq.Select(d => d.MemberNo).Distinct().ToList();
        var dlcpCodes = draftWithSeq.Select(d => d.DLCPCode).Distinct().ToList();

        var mqdmList = await _db.Set<Map_QueryDealer_Member>()
            .Where(m => memberNos.Contains(m.MemberNo) && dlcpCodes.Contains(m.DLCPCode))
            .ToListAsync();

        var cmList = await _db.Set<CrdMember>()
            .Where(c => memberNos.Contains(c.MemberNo))
            .ToListAsync();

        var mdcpList = await _db.Set<Mst_DealerCP>()
            .Where(d => dlcpCodes.Contains(d.DLCPCode))
            .ToListAsync();

        var provinceCodes = cmList.Select(c => c.ProvinceCode).Where(p => !string.IsNullOrEmpty(p)).Distinct().ToList();
        var mpList = await _db.Set<Mst_Province>()
            .Where(p => provinceCodes.Contains(p.ProvinceCode))
            .ToListAsync();

        var finalRows = new List<MapQueryDealerMemberReportRow>();
        var finalMemberNos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var t in draftWithSeq)
        {
            var mqdm = mqdmList.FirstOrDefault(m =>
                string.Equals(m.DLCPCode, t.DLCPCode, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(m.MemberNo, t.MemberNo, StringComparison.OrdinalIgnoreCase));
            if (mqdm == null) continue;

            var cm = cmList.FirstOrDefault(c =>
                string.Equals(c.MemberNo, t.MemberNo, StringComparison.OrdinalIgnoreCase));
            if (cm == null) continue;

            // ĐIỀU KIỆN QUAN TRỌNG (Report.cs:11785): where mqdm.DLCPCode <> cm.DLCodeRegis
            // CHỈ giữ lượt tra cứu của đại lý KHÁC đại lý mở thẻ
            // SQL `mqdm.DLCPCode <> cm.DLCodeRegis`: DLCodeRegis NULL ⇒ so sánh UNKNOWN ⇒ dòng BỊ LOẠI (không coi NULL là "").
            if (cm.DLCodeRegis == null
                || string.Equals(mqdm.DLCPCode, cm.DLCodeRegis, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var mdcp = mdcpList.FirstOrDefault(d =>
                string.Equals(d.DLCPCode, t.DLCPCode, StringComparison.OrdinalIgnoreCase));

            var mp = !string.IsNullOrEmpty(cm.ProvinceCode)
                ? mpList.FirstOrDefault(p => string.Equals(p.ProvinceCode, cm.ProvinceCode, StringComparison.OrdinalIgnoreCase))
                : null;

            // ModelCodeName (Report.cs:11772): cm.ModelCode + ':' + cm.ModelName
            // Toán tử `+` trong SQL trả về null nếu ModelCode HOẶC ModelName null.
            string? modelCodeName = (cm.ModelCode != null && cm.ModelName != null)
                ? $"{cm.ModelCode}:{cm.ModelName}"
                : null;

            finalMemberNos.Add(t.MemberNo);

            finalRows.Add(new MapQueryDealerMemberReportRow
            {
                MyIdxSeq = t.MyIdxSeq,
                DLCPCode = mqdm.DLCPCode,
                MemberNo = mqdm.MemberNo,
                NetworkID = mqdm.NetworkID,
                QueryDate = mqdm.QueryDate,
                Remark = mqdm.Remark,
                FlagActive = mqdm.FlagActive,
                LogLUDTimeUTC = mqdm.LogLUDTimeUTC,
                LogLUBy = mqdm.LogLUBy,
                DLCPName = mdcp?.DLCPName,
                VIN = cm.VIN,
                CarNo = cm.CarNo,
                PhoneNo = cm.PhoneNo,
                CardTypeUse = cm.CardTypeUse,
                MemberName = cm.MemberName,
                ModelCode = cm.ModelCode,
                DLCodeRegis = cm.DLCodeRegis,
                ProvinceName = mp?.ProvinceName,
                ModelName = cm.ModelName,
                ModelCodeName = modelCodeName
            });
        }

        return (null, finalRows, finalMemberNos.Count);
    }

    /// <summary>
    /// Thực hiện nghiệp vụ GetX cho Search #66 (Master.Loyalty.cs:20933-21140).
    /// Bước 1: Draft (#tbl_Map_QueryDealer_Member_Filter_Draft :21002-21018).
    /// Bước 2: Select cuối (:21052-21067).
    /// </summary>
    private async Task<List<object>> GetXAsync(MapQueryDealerMemberSearchReq filter, long recordStart, long recordCount)
    {
        var draftWithSeq = await GetDraftItemsAsync(
            cardNo: filter.CardNo,
            idCardNo: filter.IDCardNo,
            dlcpCode: filter.DLCPCode,
            memberNo: filter.MemberNo,
            carNo: filter.CarNo,
            phoneNo: filter.PhoneNo,
            vin: filter.VIN,
            queryDateFrom: null, // Gate Search KHÔNG truyền QueryDate -> không lọc QueryDate
            queryDateTo: null,
            recordStart: recordStart,
            recordCount: recordCount
        );

        if (draftWithSeq.Count == 0)
        {
            return new List<object>();
        }

        // -----------------------------------------------------------------------------------------
        // BƯỚC 2: SELECT CUỐI (Master.Loyalty.cs:21052-21067)
        // select t.MyIdxSeq, mqdm.*, mdcp.DLCPName, cm.VIN, cm.CarNo, cm.ModelName
        // from #tbl_Map_QueryDealer_Member_Filter t
        //   inner join Map_QueryDealer_Member mqdm on t.DLCPCode = mqdm.DLCPCode and t.MemberNo = mqdm.MemberNo
        //   left join Mst_DealerCP mdcp on mqdm.DLCPCode = mdcp.DLCPCode
        //   inner join Crd_Member cm on mqdm.MemberNo = cm.MemberNo
        // order by mqdm.LogLUDTimeUTC desc
        // -----------------------------------------------------------------------------------------
        var memberNos = draftWithSeq.Select(d => d.MemberNo).Distinct().ToList();
        var dlcpCodes = draftWithSeq.Select(d => d.DLCPCode).Distinct().ToList();

        var mqdmList = await _db.Set<Map_QueryDealer_Member>()
            .Where(m => memberNos.Contains(m.MemberNo) && dlcpCodes.Contains(m.DLCPCode))
            .ToListAsync();

        var cmList = await _db.Set<CrdMember>()
            .Where(c => memberNos.Contains(c.MemberNo))
            .ToListAsync();

        var mdcpList = await _db.Set<Mst_DealerCP>()
            .Where(d => dlcpCodes.Contains(d.DLCPCode))
            .ToListAsync();

        var rows = draftWithSeq
            .Select(t =>
            {
                var mqdm = mqdmList.FirstOrDefault(m =>
                    string.Equals(m.DLCPCode, t.DLCPCode, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(m.MemberNo, t.MemberNo, StringComparison.OrdinalIgnoreCase));
                if (mqdm == null) return null;

                var cm = cmList.FirstOrDefault(c =>
                    string.Equals(c.MemberNo, t.MemberNo, StringComparison.OrdinalIgnoreCase));
                if (cm == null) return null;

                var mdcp = mdcpList.FirstOrDefault(d =>
                    string.Equals(d.DLCPCode, t.DLCPCode, StringComparison.OrdinalIgnoreCase));

                return new
                {
                    t.MyIdxSeq,
                    mqdm.DLCPCode,
                    mqdm.MemberNo,
                    mqdm.NetworkID,
                    mqdm.QueryDate,
                    mqdm.Remark,
                    mqdm.FlagActive,
                    mqdm.LogLUDTimeUTC,
                    mqdm.LogLUBy,
                    DLCPName = mdcp?.DLCPName,
                    cm.VIN,
                    cm.CarNo,
                    cm.ModelName
                };
            })
            .Where(x => x != null)
            .OrderByDescending(x => x!.LogLUDTimeUTC)
            .Cast<object>()
            .ToList();

        return rows;
    }

    /// <summary>
    /// Thực hiện BƯỚC 1: DRAFT (#tbl_Map_QueryDealer_Member_Filter_Draft) dùng chung giữa Search #66 và SearchForRePort #67.
    /// Nguồn SQL: Master.Loyalty.cs:21002-21018 (#66) và Report.cs:11708-11726 (#67).
    /// </summary>
    private async Task<List<DraftItem>> GetDraftItemsAsync(
        string? cardNo,
        string? idCardNo,
        string? dlcpCode,
        string? memberNo,
        string? carNo,
        string? phoneNo,
        string? vin,
        DateTime? queryDateFrom,
        DateTime? queryDateTo,
        long recordStart,
        long recordCount)
    {
        if (recordCount <= 0)
        {
            return new List<DraftItem>();
        }

        var draftBase = from mqdm in _db.Set<Map_QueryDealer_Member>()
                        join cm in _db.Set<CrdMember>() on mqdm.MemberNo equals cm.MemberNo
                        join cc in _db.Set<CrdCard>() on cm.MemberNo equals cc.MemberNo
                        select new { mqdm, cm, cc };

        // Ability (Master.Loyalty.cs:20974-20986 / Report.cs:11682-11694):
        // Nguồn: lấy DLCPCode của người dùng đăng nhập; nếu khác rỗng và khác "HTV" thì thêm mqdm.DLCPCode = <đại lý user>.
        // Đích CHƯA có Sys_User / session user -> coi như HTV (không lọc theo đại lý của user).
        // GAP: khi có Sys_User phải giới hạn theo đại lý của user.

        // CardNo: Crd_Card.CardNo = (:207-210)
        if (!string.IsNullOrWhiteSpace(cardNo))
        {
            var pattern = PortQuery.Equal(cardNo);
            draftBase = draftBase.Where(x => EF.Functions.Like((x.cc.CardNo ?? "").ToLower(), pattern, PortQuery.Esc));
        }

        // IDCardNo: Crd_Member.IDCardNo = (:212-215)
        if (!string.IsNullOrWhiteSpace(idCardNo))
        {
            var pattern = PortQuery.Equal(idCardNo);
            draftBase = draftBase.Where(x => EF.Functions.Like((x.cm.IDCardNo ?? "").ToLower(), pattern, PortQuery.Esc));
        }

        // DLCPCode: tách dấu phẩy -> Map_QueryDealer_Member.DLCPCode IN (:217-225)
        if (!string.IsNullOrWhiteSpace(dlcpCode))
        {
            var dlcpList = dlcpCode.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                          .Select(p => p.ToLowerInvariant())
                                          .ToList();
            if (dlcpList.Count > 0)
            {
                draftBase = draftBase.Where(x => dlcpList.Contains((x.mqdm.DLCPCode ?? "").ToLower()));
            }
        }

        // MemberNo: Map_QueryDealer_Member.MemberNo = (:227-230)
        if (!string.IsNullOrWhiteSpace(memberNo))
        {
            var pattern = PortQuery.Equal(memberNo);
            draftBase = draftBase.Where(x => EF.Functions.Like((x.mqdm.MemberNo ?? "").ToLower(), pattern, PortQuery.Esc));
        }

        // CarNo: Crd_Member.CarNo = (:232-235)
        if (!string.IsNullOrWhiteSpace(carNo))
        {
            var pattern = PortQuery.Equal(carNo);
            draftBase = draftBase.Where(x => EF.Functions.Like((x.cm.CarNo ?? "").ToLower(), pattern, PortQuery.Esc));
        }

        // PhoneNo: Crd_Member.PhoneNo = (:237-240)
        if (!string.IsNullOrWhiteSpace(phoneNo))
        {
            var pattern = PortQuery.Equal(phoneNo);
            draftBase = draftBase.Where(x => EF.Functions.Like((x.cm.PhoneNo ?? "").ToLower(), pattern, PortQuery.Esc));
        }

        // VIN: Crd_Member.VIN = (:242-245)
        if (!string.IsNullOrWhiteSpace(vin))
        {
            var pattern = PortQuery.Equal(vin);
            draftBase = draftBase.Where(x => EF.Functions.Like((x.cm.VIN ?? "").ToLower(), pattern, PortQuery.Esc));
        }

        // QueryDateFrom: Map_QueryDealer_Member.QueryDate >= (:247-250)
        if (queryDateFrom.HasValue)
        {
            draftBase = draftBase.Where(x => x.mqdm.QueryDate >= queryDateFrom.Value);
        }

        // QueryDateTo: Map_QueryDealer_Member.QueryDate <= (:252-255)
        if (queryDateTo.HasValue)
        {
            draftBase = draftBase.Where(x => x.mqdm.QueryDate <= queryDateTo.Value);
        }

        // Draft distinct và sắp xếp LogLUDTimeUTC desc (:21018 và :11725 order by mqdm.LogLUDTimeUTC desc)
        var draftQuery = draftBase
            .Select(x => new
            {
                x.mqdm.DLCPCode,
                x.mqdm.MemberNo,
                x.mqdm.LogLUDTimeUTC
            })
            .Distinct()
            .OrderByDescending(x => x.LogLUDTimeUTC);

        // Phân trang trên draft table (#tbl_Map_QueryDealer_Member_Filter :21025-21033 và :11734-11735)
        var skip = (int)Math.Max(0, Math.Min(recordStart, int.MaxValue));
        var take = (int)Math.Max(0, Math.Min(recordCount, int.MaxValue));

        var draftItems = await draftQuery
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        if (draftItems.Count == 0)
        {
            return new List<DraftItem>();
        }

        // Gán số thứ tự MyIdxSeq 0-based tuần tự (identity(bigint, 0, 1) trong SQL nguồn :21003 và :11709)
        return draftItems
            .Select((x, i) => new DraftItem
            {
                MyIdxSeq = recordStart + i,
                DLCPCode = x.DLCPCode,
                MemberNo = x.MemberNo,
                LogLUDTimeUTC = x.LogLUDTimeUTC
            })
            .ToList();
    }

    private sealed class DraftItem
    {
        public long MyIdxSeq { get; set; }
        public string DLCPCode { get; set; } = "";
        public string MemberNo { get; set; } = "";
        public DateTime? LogLUDTimeUTC { get; set; }
    }
}
