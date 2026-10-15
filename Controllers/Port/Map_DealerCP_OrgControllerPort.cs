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
// PORT 1:1 — Map_DealerCP_OrgController/GetByDLCPCode (hạng mục #65).
//
// Nguồn controller (tầng gate):
//   Map_DealerCP_OrgController.cs:26-91   GetByDLCPCode                              -> WA_Map_DealerCP_Org_Get
//   Map_DealerCP_OrgController.cs:95-111  strWhereClause_Map_DealerCP_Org_By_DLCPCode
// Nguồn API:
//   MapDealerCPOrgController.cs:92-161    WA_Map_DealerCP_Org_Get
// Nguồn biz:
//   Master.Loyalty.cs:4832-4931          WAS_Map_DealerCP_Org_Get
//   Master.Loyalty.cs:4932-5085          Map_DealerCP_Org_Get
//   Master.Loyalty.cs:5086-5260          Map_DealerCP_Org_GetX (SQL :5141-5201)
// Model:
//   12.Dev.Common\idn.Loyalty.Common\Models\Loyalty\Map_DealerCP_Org.cs:9-18
//   Models/Port/Mst_DealerCP.cs (class Map_DealerCP_Org, khoá ghép DLCPCode + OrgID)
//   Models/Port/Mst_Org.cs (class Mst_Org, Mst_NNT)
//
// GIỮ ĐÚNG NGUỒN:
//   * Endpoint là POST: [HttpPost("getbydlcpcode")], Route: [Route("api/map_dealercp_org")].
//   * Body nhận JSON { DLCPCode }, EmptyBodyBehavior = EmptyBodyBehavior.Allow.
//   * Gate Map_DealerCP_OrgController.cs:26-91:
//     - DLCPCode rỗng/null/whitespace: không truy vấn DB (:47), trả Success = true, Data = null (:78).
//     - Ngược lại: gọi strWhereClause_Map_DealerCP_Org_By_DLCPCode (:95-111).
//       Chỉ lọc Map_DealerCP_Org.DLCPCode = x (:102); dòng FlagActive (:105-108) bị comment trong nguồn
//       nên KHÔNG lọc FlagActive (dealer hoặc map inactive vẫn được lấy).
//     - Ft_RecordStart = "0", Ft_RecordCount = "1" (:65-66).
//     - Data = Lst_Map_DealerCP_Org[0] nếu có dòng, ngược lại null (:72-78).
//   * Biz Map_DealerCP_Org_GetX (Master.Loyalty.cs:5086-5260, SQL :5141-5201) thực hiện đúng 2 bước:
//     1. Draft (:5143-5153):
//        select distinct identity(bigint, 0, 1) MyIdxSeq, mdcpo.DLCPCode, mdcpo.LogLUDTimeUTC
//        into #tbl_Map_DealerCP_Org_Filter_Draft
//        from Map_DealerCP_Org mdcpo inner join Mst_DealerCP mdcp on mdcpo.DLCPCode = mdcp.DLCPCode
//        where (1=1) and (mdcpo.DLCPCode = @p_0)
//        order by mdcpo.LogLUDTimeUTC desc;
//        -> Distinct theo cặp (DLCPCode, LogLUDTimeUTC), MyIdxSeq 0-based theo LogLUDTimeUTC giảm dần.
//        MyCount = số cặp distinct. Cắt trang [start, start + count - 1].
//     2. Select cuối (:5186-5197):
//        select t.MyIdxSeq, mdcpo.*, mnnt.MST mnnt_MST
//        from #tbl_Map_DealerCP_Org_Filter t
//        inner join Map_DealerCP_Org mdcpo on t.DLCPCode = mdcpo.DLCPCode (chỉ theo DLCPCode)
//        inner join Mst_Org mo on mdcpo.OrgID = mo.OrgID
//        inner join Mst_NNT mnnt on mo.OrgID = mnnt.OrgID
//        order by mdcpo.LogLUDTimeUTC desc;
//        -> 1 DLCPCode có nhiều org -> trả nhiều dòng.
//        -> Map tới org không có Mst_NNT -> BỊ LOẠI do inner join Mst_NNT mnnt on mo.OrgID = mnnt.OrgID (:5195-5196).
//        -> Hòa trùng khi LogLUDTimeUTC bằng nhau: sắp phụ theo OrgID asc (nguồn không xác định thứ tự khi trùng).
//
// Bỏ qua có chủ đích (loại Đ):
//   - CheckGatewayAuthentication: gate :35, API MapDealerCPOrgController.cs:113-118 (hệ thống đích chưa có gateway auth).
//   - CheckAuthorize: gate :36 bị comment trong nguồn, API không check; đích chưa có hệ phân quyền.
//   - drAbilityOfUser: Master.Loyalty.cs:5127 lấy từ Sys_User_ForCP_GetAbilityViewOfUser nhưng dòng gọi cache :5139
//     bị comment trong nguồn -> biến này không được sử dụng -> không port.
//   - Nguồn gọi bằng tài khoản hệ thống viết cứng: gate Map_DealerCP_OrgController.cs:59-60
//     WAUserCode_BG / WAUserPassword_BG (tài khoản + mật khẩu viết cứng — KHÔNG chép giá trị theo quy định).
//   - Ghi RequestLog / ReturnLog: Master.Loyalty.cs:4975-4982, 5075-5082.
//   - Transaction rollback an toàn: Master.Loyalty.cs:4972, 5048.
// ============================================================================================

/// <summary>Port 1:1 body request cho action GetByDLCPCode (Map_DealerCP_OrgController.cs:26, 47, 51).</summary>
public class MapDealerCPOrgGetByDLCPCodeReq
{
    public string? DLCPCode { get; set; }
}

[ApiController]
[Route("api/map_dealercp_org")]
[Produces("application/json")]
public class Map_DealerCP_OrgControllerPort : ControllerBase
{
    private readonly AppDbContext _db;

    public Map_DealerCP_OrgControllerPort(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Port 1:1 <c>Map_DealerCP_OrgController.GetByDLCPCode</c> (Map_DealerCP_OrgController.cs:26-91).
    /// </summary>
    [HttpPost("getbydlcpcode")]
    public async Task<IActionResult> GetByDLCPCode([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] MapDealerCPOrgGetByDLCPCodeReq? req)
    {
        var dlcpCode = req?.DLCPCode;

        // Gate Map_DealerCP_OrgController.cs:47: DLCPCode rỗng/null/whitespace -> không truy vấn, trả Data = null (:78).
        if (string.IsNullOrWhiteSpace(dlcpCode))
        {
            return Ok(new { Success = true, Data = (object?)null });
        }

        // Gate :65-66: Ft_RecordStart = "0", Ft_RecordCount = "1".
        // Gọi hàm nghiệp vụ GetXAsync lấy dữ liệu.
        var (_, rows) = await GetXAsync(dlcpCode, 0, 1);

        // Gate :72-78: Data = Lst_Map_DealerCP_Org[0] nếu có kết quả, ngược lại null.
        return Ok(new { Success = true, Data = rows.Count > 0 ? rows[0] : null });
    }

    /// <summary>
    /// Port 1:1 <c>Map_DealerCP_Org_GetX</c> (Master.Loyalty.cs:5086-5260, SQL :5141-5201).
    /// Thực hiện đúng 2 bước SQL nguồn:
    ///   Bước 1 (Draft): distinct theo cặp (DLCPCode, LogLUDTimeUTC), inner join Mst_DealerCP, lọc DLCPCode, sắp xếp LogLUDTimeUTC desc, cắt trang [recordStart, recordStart + recordCount - 1].
    ///   Bước 2 (Select cuối): từ trang draft, inner join Map_DealerCP_Org theo DLCPCode, inner join Mst_Org theo OrgID, inner join Mst_NNT theo OrgID, sắp xếp LogLUDTimeUTC desc (hòa trùng: OrgID asc).
    /// </summary>
    private async Task<(long itemCount, List<object> rows)> GetXAsync(string? dlcpCodeEqual, long recordStart, long recordCount)
    {
        recordStart = Math.Max(0, recordStart);

        // ----------------------------------------------------------------------------------------
        // BƯỚC 1: DRAFT (Master.Loyalty.cs:5143-5153)
        // SQL:
        //   select distinct
        //       identity(bigint, 0, 1) MyIdxSeq,
        //       mdcpo.DLCPCode,
        //       mdcpo.LogLUDTimeUTC
        //   into #tbl_Map_DealerCP_Org_Filter_Draft
        //   from Map_DealerCP_Org mdcpo
        //       inner join Mst_DealerCP mdcp on mdcpo.DLCPCode = mdcp.DLCPCode
        //   where (1=1) and (mdcpo.DLCPCode = @p_0)
        //   order by mdcpo.LogLUDTimeUTC desc;
        // ----------------------------------------------------------------------------------------
        var draftBaseQuery = from mdcpo in _db.Set<Map_DealerCP_Org>()
                             join mdcp in _db.Set<Mst_DealerCP>() on mdcpo.DLCPCode equals mdcp.DLCPCode
                             select new
                             {
                                 mdcpo.DLCPCode,
                                 mdcpo.LogLUDTimeUTC
                             };

        // Lọc DLCPCode: nguồn dùng '=' trên SQL Server collation không phân biệt hoa/thường (PortQuery.Equal).
        // Gate :105-108: dòng FlagActive bị comment trong nguồn -> KHÔNG lọc FlagActive.
        if (!string.IsNullOrWhiteSpace(dlcpCodeEqual))
        {
            var pattern = PortQuery.Equal(dlcpCodeEqual);
            draftBaseQuery = draftBaseQuery.Where(x => EF.Functions.Like((x.DLCPCode ?? "").ToLower(), pattern, PortQuery.Esc));
        }

        // Distinct theo cặp (DLCPCode, LogLUDTimeUTC) và sắp xếp theo LogLUDTimeUTC desc (hòa trùng theo DLCPCode asc).
        var distinctDraft = draftBaseQuery
            .Distinct()
            .OrderByDescending(x => x.LogLUDTimeUTC)
            .ThenBy(x => x.DLCPCode);

        // Đếm tổng số bản ghi draft (MyCount của khối Summary :5157).
        var itemCount = await distinctDraft.CountAsync();

        if (recordCount <= 0 || itemCount == 0 || recordStart >= itemCount)
        {
            return (itemCount, new List<object>());
        }

        // Cắt trang [recordStart, recordStart + recordCount - 1] (SQL :5160-5168).
        var draftPage = await distinctDraft
            .Skip((int)recordStart)
            .Take((int)recordCount)
            .ToListAsync();

        // Gán MyIdxSeq 0-based theo thứ tự draft (SQL :5144 identity(bigint, 0, 1) MyIdxSeq).
        var draftWithIdx = draftPage
            .Select((d, idx) => new
            {
                MyIdxSeq = recordStart + idx,
                d.DLCPCode,
                d.LogLUDTimeUTC
            })
            .ToList();

        // ----------------------------------------------------------------------------------------
        // BƯỚC 2: SELECT CUỐI (Master.Loyalty.cs:5186-5197)
        // SQL:
        //   select
        //       t.MyIdxSeq,
        //       mdcpo.*,
        //       mnnt.MST mnnt_MST
        //   from #tbl_Map_DealerCP_Org_Filter t
        //       inner join Map_DealerCP_Org mdcpo on t.DLCPCode = mdcpo.DLCPCode
        //       inner join Mst_Org mo on mdcpo.OrgID = mo.OrgID
        //       inner join Mst_NNT mnnt on mo.OrgID = mnnt.OrgID
        //   order by mdcpo.LogLUDTimeUTC desc;
        //
        // Lý do loại dòng không có NNT: inner join Mst_NNT mnnt on mo.OrgID = mnnt.OrgID (:5195-5196).
        // ----------------------------------------------------------------------------------------
        var targetDlcpCodes = draftWithIdx.Select(x => x.DLCPCode).Distinct().ToList();

        var details = await (from mdcpo in _db.Set<Map_DealerCP_Org>()
                             where targetDlcpCodes.Contains(mdcpo.DLCPCode)
                             join mo in _db.Set<Mst_Org>() on mdcpo.OrgID equals mo.OrgID
                             join mnnt in _db.Set<Mst_NNT>() on mo.OrgID equals mnnt.OrgID
                             select new
                             {
                                 mdcpo.DLCPCode,
                                 mdcpo.OrgID,
                                 mdcpo.NetworkID,
                                 mdcpo.FlagActive,
                                 mdcpo.LogLUDTimeUTC,
                                 mdcpo.LogLUBy,
                                 mnnt_MST = mnnt.MST
                             }).ToListAsync();

        // Join trang draft t với mdcpo theo CHỈ DLCPCode (t.DLCPCode = mdcpo.DLCPCode) như SQL nguồn (:5192).
        // Sắp xếp theo mdcpo.LogLUDTimeUTC desc (hòa trùng: sắp phụ theo OrgID asc; nguồn không xác định thứ tự khi trùng).
        var rows = (from t in draftWithIdx
                    join d in details on t.DLCPCode equals d.DLCPCode
                    orderby d.LogLUDTimeUTC descending, d.OrgID ascending
                    select (object)new
                    {
                        t.MyIdxSeq,
                        d.DLCPCode,
                        d.OrgID,
                        d.NetworkID,
                        d.FlagActive,
                        d.LogLUDTimeUTC,
                        d.LogLUBy,
                        d.mnnt_MST
                    }).ToList();

        return (itemCount, rows);
    }
}
