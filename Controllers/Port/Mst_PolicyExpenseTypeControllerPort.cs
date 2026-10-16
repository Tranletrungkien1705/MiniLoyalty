using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniLoyalty.Data;
using MiniLoyalty.Models.Port;

namespace MiniLoyalty.Controllers.Port;

// ============================================================================================
// PORT 1:1 — màn danh mục chính sách loại chi phí (Mst_PolicyExpenseType) của dự án nguồn 2022.7.Loyalty (#190, #191).
//
// Nguồn controller (tầng gate):
//   Mst_PolicyExpenseTypeController.cs:23-103  Search -> WA_Mst_PolicyExpenseType_Get (#190)
//   Mst_PolicyExpenseTypeController.cs:104-213 GetAllActive -> WA_Mst_PolicyExpenseType_Get (#191)
//   Mst_PolicyExpenseTypeController.cs:281-298 strWhereClause_Mst_PolicyExpenseType (mệnh đề lọc Search / GetAllActive)
// Nguồn API:
//   MstPolicyExpenseTypeController.cs:96-164   WA_Mst_PolicyExpenseType_Get
// Nguồn biz:
//   Master.Loyalty.cs:8499-8602                WAS_Mst_PolicyExpenseType_Get
//   Master.Loyalty.cs:8603-8756                Mst_PolicyExpenseType_Get
//   Master.Loyalty.cs:8757-8929                Mst_PolicyExpenseType_GetX (SQL draft :8814-8824, SQL select cuối :8856-8867)
//
// ĐẶC ĐIỂM VÀ QUY TẮC NGHIỆP VỤ NGUỒN:
//   * Endpoint là POST (nguồn gate Mst_PolicyExpenseTypeController.cs:23).
//   * Tham số lọc Search: KeyWord, FlagActive, Ft_PageIndex = 0, Ft_PageSize = 10 (gate :23).
//   * KeyWord KHÔNG lọc: hàm lọc strWhereClause_Mst_PolicyExpenseType (:281-298) CHỈ xử lý FlagActive
//     (tách dấu phẩy -> toan tử IN :289-294), hoàn toàn không có nhánh nào lọc KeyWord.
//   * Phân trang ở gate: Ft_RecordStart = (pageIndex * pageSize).ToString(), Ft_RecordCount = pageSize.ToString() (:49-50).
//   * Nghiệp vụ 2 bước trong Mst_PolicyExpenseType_GetX:
//     1. Bước 1 DRAFT (#tbl_Mst_PolicyExpenseType_Filter_Draft, :8814-8824):
//        select distinct identity(bigint, 0, 1) MyIdxSeq, mpet.PolicyExpenseTypeNo, mpet.ExpenseType, mpet.LogLUDTimeUTC
//        into #tbl_Mst_PolicyExpenseType_Filter_Draft
//        from Mst_PolicyExpenseType mpet where (1=1) zzB_Where_strFilter_zzE
//        order by mpet.LogLUDTimeUTC desc.
//        MyCount = Count(0) của bảng draft (:8827).
//        Cắt trang draft (#tbl_Mst_PolicyExpenseType_Filter, :8831-8838):
//        t.MyIdxSeq >= @nFilterRecordStart and t.MyIdxSeq <= @nFilterRecordEnd
//        với nFilterRecordStart = recordStart, nFilterRecordEnd = recordStart + recordCount - 1 (:8793-8794).
//     2. Bước 2 SELECT CUỐI (:8856-8867):
//        select t.MyIdxSeq, mpet.*, met.ExpenseTypeName
//        from #tbl_Mst_PolicyExpenseType_Filter t
//          inner join Mst_PolicyExpenseType mpet on t.PolicyExpenseTypeNo = mpet.PolicyExpenseTypeNo and t.ExpenseType = mpet.ExpenseType
//          left join Mst_ExpenseType met on t.ExpenseType = met.ExpenseType
//        order by mpet.LogLUDTimeUTC desc.
//        Cột trả về gồm: MyIdxSeq (0-based từ draft), toàn bộ 16 cột của mpet, và cột ExpenseTypeName (từ Mst_ExpenseType qua left join).
//   * Tính PageCount tại gate (:61-69, :78-86):
//     Chỉ tính khi Lst_Mst_PolicyExpenseType không rỗng và count > 0.
//     Nếu pageSize == 0 thì pageSize gán về 10 (PageSizeConfig = 10) rồi tính:
//     pageCount = (itemCount % pageSize == 0) ? itemCount / pageSize : itemCount / pageSize + 1.
//     Khi DataList rỗng hoặc pageSize = 0: DataList = [], PageCount = 0, ItemCount = itemCount.
//   * Bỏ qua có chủ đích (loại Đ):
//     - drAbilityOfUser (Master.Loyalty.cs:8798) nguồn lấy nhưng bị comment không dùng (:8810) -> không port, ghi comment.
//     - FK ExpenseType -> Mst_ExpenseType (20221026.CreateDatabase.sql:264-270) — chỉ ghi comment.
//     - CheckGatewayAuthentication, CheckAuthorize (gate :32-33, API :116-121), Sys_User_CheckAuthorize (:8667-8680):
//       hệ thống đích chưa có hệ phân quyền.
//     - OrgID, NetworkID gửi ở Request.Headers / WARQBase (gate :34-35, :46-47) nhưng hàm lọc :281-298 không lọc vào Ft_WhereClause.
// ============================================================================================

/// <summary>
/// Port 1:1 <c>Mst_PolicyExpenseTypeSearch</c> — tham số lọc + phân trang (Mst_PolicyExpenseTypeController.cs:23, 50-54).
/// </summary>
public class MstPolicyExpenseTypeSearchReq
{
    public string? KeyWord { get; set; }             // Từ khoá (gate nhận nhưng strWhereClause_Mst_PolicyExpenseType :281-298 KHÔNG lọc KeyWord)
    public string? FlagActive { get; set; }          // Cờ hoạt động, danh sách phân cách dấu phẩy (lọc IN) — :288-295
    public int Ft_PageIndex { get; set; } = 0;       // Trang (0-based) — :23, :44
    public int Ft_PageSize { get; set; } = 10;       // Số dòng/trang — :23, :45
}

[ApiController]
[Route("api/mst_policyexpensetype")]
[Produces("application/json")]
public class Mst_PolicyExpenseTypeControllerPort : ControllerBase
{
    private readonly AppDbContext _db;

    public Mst_PolicyExpenseTypeControllerPort(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Port 1:1 <c>Mst_PolicyExpenseTypeController.Search</c> (nguồn gate Mst_PolicyExpenseTypeController.cs:23-103).
    /// </summary>
    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] MstPolicyExpenseTypeSearchReq? req)
    {
        var r = req ?? new MstPolicyExpenseTypeSearchReq();
        var pageIndex = r.Ft_PageIndex;
        var pageSize = r.Ft_PageSize;

        // KeyWord KHÔNG lọc (gate Mst_PolicyExpenseTypeController.cs:281-298 không có nhánh nào lọc KeyWord).
        // Phân trang nguồn: Ft_RecordStart = (pageIndex * pageSize).ToString(), Ft_RecordCount = pageSize.ToString() (gate :49-50).
        var start = (long)pageIndex * pageSize;
        var (itemCount, rows) = await GetXAsync(r.FlagActive, start, pageSize);

        // PageCount chỉ tính khi Lst_Mst_PolicyExpenseType không rỗng và count > 0 (gate :61-69, :78-86).
        // Nếu pageSize == 0 thì gán tạm pageSize = 10 (PageSizeConfig = 10) để tính PageCount (:64-67).
        var ps = pageSize == 0 ? 10 : pageSize;
        var pageCount = rows.Count == 0 ? 0 : (itemCount % ps == 0 ? itemCount / ps : itemCount / ps + 1);

        return Ok(new
        {
            Success = true,
            Data = new PageInfo<object>(rows, itemCount, pageCount, pageIndex, pageSize)
        });
    }

    /// <summary>
    /// Port 1:1 <c>Mst_PolicyExpenseTypeController.GetAllActive</c> (nguồn gate Mst_PolicyExpenseTypeController.cs:104-213).
    /// - Gọi hàm lọc strWhereClause_Mst_PolicyExpenseType với FlagActive = "1" (:130-134, :281-298).
    ///   Lọc FlagActive theo toán tử IN (:289-294); KeyWord và OrgID KHÔNG lọc trong where clause (:281-298).
    /// - Ft_RecordStart = "0", Ft_RecordCount = "123456000" (BaseController.cs:30-31, :146-147).
    /// - Rt_Cols_Mst_PolicyExpenseType = "*" (:150).
    /// - Kết quả trả về: mảng trần List&lt;object&gt;, khi rỗng hoặc null trả về Data = null (:127, :153-155, :190-200).
    /// </summary>
    [HttpPost("getallactive")]
    public async Task<IActionResult> GetAllActive()
    {
        var (_, rows) = await GetXAsync("1", 0, 123456000);
        return Ok(new
        {
            Success = true,
            Data = rows.Count > 0 ? rows : null
        });
    }

    /// <summary>
    /// Port 1:1 hàm nghiệp vụ <c>Mst_PolicyExpenseType_GetX</c> (Master.Loyalty.cs:8757-8929).
    /// Thực hiện ĐÚNG 2 bước:
    /// - Bước 1: Draft từ Mst_PolicyExpenseType (lọc FlagActive IN tách dấu phẩy, order by LogLUDTimeUTC desc, cắt trang).
    /// - Bước 2: Select cuối từ draft page inner join Mst_PolicyExpenseType, left join Mst_ExpenseType lấy ExpenseTypeName, order by LogLUDTimeUTC desc.
    /// </summary>
    private async Task<(int ItemCount, List<object> Rows)> GetXAsync(string? flagActiveCsv, long recordStart, long recordCount)
    {
        // -----------------------------------------------------------------------------------------
        // BƯỚC 1: DRAFT (#tbl_Mst_PolicyExpenseType_Filter_Draft, Master.Loyalty.cs:8814-8824)
        // -----------------------------------------------------------------------------------------
        var qPolicy = _db.Set<Mst_PolicyExpenseType>().AsNoTracking();

        // Lọc FlagActive: danh sách phân cách dấu phẩy -> IN ('a','b') (gate Mst_PolicyExpenseTypeController.cs:288-295).
        // So sánh không phân biệt hoa/thường (bẫy #18/#31, PORT_RULES §3).
        if (!string.IsNullOrWhiteSpace(flagActiveCsv))
        {
            var ds = flagActiveCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => s.ToLowerInvariant())
                .ToList();
            if (ds.Count > 0)
            {
                qPolicy = qPolicy.Where(x => ds.Contains((x.FlagActive ?? "").ToLower()));
            }
        }

        // KeyWord KHÔNG lọc (không có nhánh nào lọc KeyWord trong strWhereClause_Mst_PolicyExpenseType :281-298).
        // OrgID và NetworkID KHÔNG lọc trong where clause (:281-298).

        // Sắp xếp mpet.LogLUDTimeUTC desc (Master.Loyalty.cs:8823)
        var draftList = await qPolicy
            .OrderByDescending(x => x.LogLUDTimeUTC)
            .ToListAsync();

        // MyCount = Count(0) của bảng draft (#tbl_Mst_PolicyExpenseType_Filter_Draft) — Master.Loyalty.cs:8827
        var itemCount = draftList.Count;

        // Cắt trang draft (#tbl_Mst_PolicyExpenseType_Filter, Master.Loyalty.cs:8831-8838):
        // t.MyIdxSeq >= @nFilterRecordStart and t.MyIdxSeq <= @nFilterRecordEnd
        // với nFilterRecordStart = recordStart, nFilterRecordEnd = recordStart + recordCount - 1 (:8793-8794).
        // Khi recordCount <= 0 hoặc recordStart < 0 thì nFilterRecordEnd < nFilterRecordStart -> trang draft rỗng.
        var draftPage = (recordCount <= 0 || recordStart < 0 || recordStart >= draftList.Count)
            ? new List<(long MyIdxSeq, Mst_PolicyExpenseType Policy)>()
            : draftList
                .Select((p, idx) => (MyIdxSeq: (long)idx, Policy: p))
                .Where(x => x.MyIdxSeq >= recordStart && x.MyIdxSeq < recordStart + recordCount)
                .ToList();

        // -----------------------------------------------------------------------------------------
        // BƯỚC 2: SELECT CUỐI (Master.Loyalty.cs:8856-8867)
        // select t.MyIdxSeq, mpet.*, met.ExpenseTypeName
        // from #tbl_Mst_PolicyExpenseType_Filter t
        //   inner join Mst_PolicyExpenseType mpet on t.PolicyExpenseTypeNo = mpet.PolicyExpenseTypeNo and t.ExpenseType = mpet.ExpenseType
        //   left join Mst_ExpenseType met on t.ExpenseType = met.ExpenseType
        // order by mpet.LogLUDTimeUTC desc
        // -----------------------------------------------------------------------------------------
        if (draftPage.Count == 0)
        {
            return (itemCount, new List<object>());
        }

        // Lấy thông tin Mst_ExpenseType để thực hiện left join lấy ExpenseTypeName
        var distinctExpenseTypes = draftPage.Select(x => x.Policy.ExpenseType).Distinct().ToList();
        var expenseTypes = await _db.Set<Mst_ExpenseType>()
            .AsNoTracking()
            .Where(x => distinctExpenseTypes.Contains(x.ExpenseType))
            .ToListAsync();

        var rows = draftPage
            .OrderByDescending(x => x.Policy.LogLUDTimeUTC)
            .ThenBy(x => x.MyIdxSeq)
            .Select(d =>
            {
                var met = expenseTypes.FirstOrDefault(e => string.Equals(e.ExpenseType, d.Policy.ExpenseType, StringComparison.OrdinalIgnoreCase));
                return (object)new
                {
                    d.MyIdxSeq,
                    d.Policy.PolicyExpenseTypeNo,
                    d.Policy.ExpenseType,
                    d.Policy.NetworkID,
                    d.Policy.ExpenseTypeNameActual,
                    ExpenseTypeName = met?.ExpenseTypeName,
                    d.Policy.FlagPoint,
                    d.Policy.FlagPointRank,
                    d.Policy.AmountRate,
                    d.Policy.MaxRankReviewPoint,
                    d.Policy.MaxAccumulationPoint,
                    d.Policy.FlagCountService,
                    d.Policy.FlagDiscount,
                    d.Policy.DiscountRate,
                    d.Policy.FlagActive,
                    d.Policy.Remark,
                    d.Policy.LogLUDTimeUTC,
                    d.Policy.LogLUBy
                };
            })
            .ToList();

        return (itemCount, rows);
    }
}
