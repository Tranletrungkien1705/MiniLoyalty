using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniLoyalty.Data;
using MiniLoyalty.Models.Port;

namespace MiniLoyalty.Controllers.Port;

// ============================================================================================
// PORT 1:1 — màn danh mục loại chi phí (Mst_ExpenseType) của dự án nguồn 2022.7.Loyalty (#151).
//
// Nguồn controller (tầng gate):
//   Mst_ExpenseTypeController.cs:23-132   GetAllActive -> WA_Mst_ExpenseType_Get
//   Mst_ExpenseTypeController.cs:138-155  strWhereClause_Mst_ExpenseType (hàm tạo điều kiện lọc)
// Nguồn API:
//   MstExpenseTypeController.cs:22-90     WA_Mst_ExpenseType_Get
// Nguồn biz:
//   Master.Loyalty.cs:8933-9035           WAS_Mst_ExpenseType_Get
//   Master.Loyalty.cs:9036-9189           Mst_ExpenseType_Get
//   Master.Loyalty.cs:9190-9349           Mst_ExpenseType_GetX (SQL excerpt :9245-9298 — bản LIVE)
//
// GIỮ ĐÚNG NGUỒN:
//   * Endpoint là POST (nguồn [AcceptVerbs("POST")]).
//   * GetAllActive: không nhận body (:23), Ft_RecordStart = "0", Ft_RecordCount = "123456000" (:65-66).
//   * Điều kiện lọc FlagActive:
//     - Gate gọi strWhereClause_Mst_ExpenseType với FlagActive = "1" (:49-53).
//     - Trong strWhereClause_Mst_ExpenseType (:144-152), FlagActive được lọc với toán tử 'in' ("in ('1')").
//     - Không wildcard, so khớp tuyệt đối theo PORT_RULES mục 3: EF.Functions.Like(cot.ToLower(), "1", "\\").
//   * Không lọc OrgID:
//     - Gate nhận OrgID (:39, :51, :63) nhưng strWhereClause_Mst_ExpenseType (:138-155) KHÔNG tạo điều kiện lọc OrgID/NetworkID.
//     - Do đó GetAllActive trả về mọi bản ghi ExpenseType đang hoạt động (FlagActive='1') của hệ thống.
//   * Sắp xếp: LogLUDTimeUTC giảm dần (Master.Loyalty.cs:9255, :9294: order by mrp.LogLUDTimeUTC desc).
//     Để kết quả ổn định khi trùng LogLUDTimeUTC, sắp phụ thêm ExpenseType asc.
//   * Select cuối: t.MyIdxSeq, mrp.* (Master.Loyalty.cs:9288-9295), trả về MyIdxSeq + 7 cột của Mst_ExpenseType.
//   * Trả Data GetAllActive: listMst_ExpenseType (mảng trần, null nếu rỗng — gate :72-74, :112-119).
//     Khoá gộp (ExpenseType, NetworkID) cho EF. Khối Ext/tree (:75-107) bị comment ở nguồn nên không port.
//
// Bỏ qua có chủ đích (loại Đ):
//   - CheckGatewayAuthentication: gate Mst_ExpenseTypeController.cs:36; API MstExpenseTypeController.cs:42-47
//   - CheckAuthorize / Sys_User_CheckAuthorize: gate Mst_ExpenseTypeController.cs:37; biz Master.Loyalty.cs:9096-9113
//   - Ghi RequestLog/ReturnLog: Master.Loyalty.cs:9079-9086, 9179-9186
//   - Ext tree (Mst_ExpenseTypeExtension): bị comment ở nguồn Mst_ExpenseTypeController.cs:75-107
// ============================================================================================

[ApiController]
[Route("api/mst_expensetype")]
[Produces("application/json")]
public class Mst_ExpenseTypeControllerPort(AppDbContext db) : ControllerBase
{
    // ============================================================================================
    // GetAllActive — nguồn Mst_ExpenseTypeController.cs:23-132 (#151).
    // Mệnh đề lọc: strWhereClause_Mst_ExpenseType (Mst_ExpenseTypeController.cs:138-155).
    // Gate:
    //   - Không nhận body: gọi trực tiếp [HttpPost("getallactive")].
    //   - Gate truyền OrgID = orgUserUI.OrgId (:51) và FlagActive = "1" (:52).
    //   - Trong strWhereClause_Mst_ExpenseType (:144-152), chỉ có FlagActive được lọc bằng toán tử 'in' ('1'),
    //     OrgID HOÀN TOÀN KHÔNG ĐƯỢC LỌC (:138-155 không xét OrgID).
    //   - Ft_RecordStart = "0", Ft_RecordCount = "123456000" (gate :65-66) -> lấy toàn bộ loại chi phí có FlagActive = '1'.
    //   - Trả Data = listMst_ExpenseType (mảng trần, null nếu rỗng — gate :72-74, :112-119).
    // ============================================================================================
    [HttpPost("getallactive")]
    public async Task<IActionResult> GetAllActive()
    {
        // Nguồn Mst_ExpenseTypeController.cs:49-53: strWhereClause_Mst_ExpenseType với FlagActive = "1".
        // KHÔNG lọc OrgID/NetworkID (strWhereClause_Mst_ExpenseType :138-155 không xử lý OrgID).
        // Ft_RecordStart = "0", Ft_RecordCount = "123456000" (gate :65-66).
        var (_, rows) = await GetXAsync("1", 0, 123456000);

        // Nguồn Mst_ExpenseTypeController.cs:72-74, :112-119:
        // if (objRT_Mst_ExpenseType.Lst_Mst_ExpenseType != null && objRT_Mst_ExpenseType.Lst_Mst_ExpenseType.Count > 0)
        //     listMst_ExpenseType = objRT_Mst_ExpenseType.Lst_Mst_ExpenseType;
        // objJsonResultEntry.Data = listMst_ExpenseType; (mảng trần, null nếu rỗng)
        return Ok(new { Success = true, Data = rows.Count > 0 ? rows : null });
    }

    /// <summary>
    /// Hàm dùng chung lấy dữ liệu Mst_ExpenseType hỗ trợ lọc và phân trang.
    /// Nguồn SQL: Master.Loyalty.cs:9190-9349 (Mst_ExpenseType_GetX — bản LIVE).
    /// </summary>
    public async Task<(int ItemCount, List<object> Rows)> GetXAsync(
        string? flagActive,
        long recordStart,
        long recordCount)
    {
        var q = db.Set<Mst_ExpenseType>().AsNoTracking().AsQueryable();

        // Lọc FlagActive: nguồn Mst_ExpenseTypeController.cs:144-152 (FlagActive in ('1'))
        // Ràng buộc P3.2 & PORT_RULES mục 3: Dùng EF.Functions.Like((x.FlagActive ?? "").ToLower(), "1", "\\") KHÔNG wildcard (so khớp tuyệt đối).
        // KHÔNG lọc OrgID/NetworkID vì strWhereClause_Mst_ExpenseType (:138-155) không tạo điều kiện cho OrgID.
        if (!string.IsNullOrWhiteSpace(flagActive))
        {
            q = q.Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), "1", "\\"));
        }

        // Sắp xếp: Master.Loyalty.cs:9255, :9294 (order by mrp.LogLUDTimeUTC desc).
        // Nguồn sắp xếp LogLUDTimeUTC giảm dần; sắp phụ thêm ExpenseType asc để kết quả ổn định khi trùng mốc thời gian.
        var ordered = q.OrderByDescending(x => x.LogLUDTimeUTC)
                       .ThenBy(x => x.ExpenseType);

        var itemCount = await ordered.CountAsync();

        // Phân trang: nFilterRecordStart..nFilterRecordEnd (Master.Loyalty.cs:9226-9227, :9268-9269).
        var page = (recordCount <= 0 || recordStart < 0)
            ? new List<Mst_ExpenseType>()
            : await ordered.Skip((int)Math.Min(recordStart, int.MaxValue)).Take((int)Math.Min(recordCount, int.MaxValue)).ToListAsync();

        // Select cuối: t.MyIdxSeq, mrp.* (Master.Loyalty.cs:9288-9295), trả về MyIdxSeq + 7 cột của Mst_ExpenseType.
        var rows = page
            .Select((x, i) => (object)new
            {
                MyIdxSeq = recordStart + i,
                x.ExpenseType,
                x.NetworkID,
                x.ExpenseTypeName,
                x.FlagActive,
                x.Remark,
                x.LogLUDTimeUTC,
                x.LogLUBy,
            })
            .ToList();

        return (itemCount, rows);
    }
}
