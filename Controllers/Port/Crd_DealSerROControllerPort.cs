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

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — Crd_DealSerROController.Search (#019).
//
// Nguồn tra cứu (chỉ đọc):
//   - Gate:     13.ClientGate/V10/idn.Loyalty.WebAPI/Controllers/Crd_DealSerROController.cs:29-109
//   - Filter:   Crd_DealSerROController.cs:255-270 (strWhereClause_Crd_DealSerRO)
//   - API:      11.BackEnd/V10/idn.Loyalty.Biz.Web/Controllers/CrdDealSerROController.cs:23-93
//   - Biz LIVE: 11.BackEnd/V10/idn.Loyalty.Biz/Card.Deal.cs:99-581 (WAS_Crd_DealSerRO_Get, Crd_DealSerRO_Get, Crd_DealSerRO_GetX)
//
// ĐẶC ĐIỂM NGHIỆP VỤ:
//   - Gate Search nhận: MemberNo, CardNo, DLCPCode, Ft_PageIndex = 0, Ft_PageSize = 10.
//   - strWhereClause_Crd_DealSerRO chỉ lọc MemberNo và CardNo với toán tử '=' (nguyên chuỗi, không phân biệt hoa/thường).
//   - DLCPCode và OrgID KHÔNG lọc trong SQL WHERE (khớp hành vi nguồn gate:56-61 và biz Card.Deal.cs:166-168).
//   - Sắp xếp: order by cdsro.LogLUDTimeUTC asc (Card.Deal.cs:168).
//   - Phân trang: Ft_PageSize = 0 -> DataList rỗng [], PageCount = 0, ItemCount giữ nguyên tổng số (Bài học A.7).
//   - Chống tràn số phân trang (Bài học A.12).
//   - DataList luôn là mảng [] khi rỗng, không bao giờ null (gate :54, :96).
// ===========================================================================================

/// <summary>
/// DTO yêu cầu tìm kiếm báo giá dịch vụ Crd_DealSerRO.
/// </summary>
public class Crd_DealSerROSearchReq
{
    public string? MemberNo { get; set; }             // Mã hội viên — gate :29, :58, :260-263
    public string? CardNo { get; set; }               // Số thẻ — gate :29, :59, :264-267
    public string? DLCPCode { get; set; }             // Mã đại lý — gate :29, :60 (không lọc trong strWhereClause)
    public string? OrgID { get; set; }                // Mã tổ chức — gate :41, :71 (không lọc trong SQL WHERE)
    public string? NetworkID { get; set; }            // NetworkID — gate :40, :70 (không lọc trong SQL WHERE)
    public int Ft_PageIndex { get; set; } = 0;        // Vị trí trang 0-based — gate :29, mặc định 0
    public int Ft_PageSize { get; set; } = 10;        // Số bản ghi mỗi trang — gate :29, mặc định 10
}

[ApiController]
[Route("api/crd_dealserro")]
public class Crd_DealSerROControllerPort : ControllerBase
{
    private readonly AppDbContext _db;

    public Crd_DealSerROControllerPort(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Port 1:1 action Search — tìm kiếm danh sách báo giá dịch vụ Crd_DealSerRO.
    /// Nguồn: Crd_DealSerROController.cs:29-109 (gate), Card.Deal.cs:99-581 (biz).
    /// </summary>
    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Crd_DealSerROSearchReq? req)
    {
        req ??= new Crd_DealSerROSearchReq();
        int pageIndex = req.Ft_PageIndex;
        int pageSize = req.Ft_PageSize;

        var query = _db.Set<Crd_DealSerRO>().AsNoTracking().AsQueryable();

        // 1. Lọc MemberNo với toán tử '=' (nguyên chuỗi, không phân biệt hoa/thường) — gate :260-263
        if (!string.IsNullOrEmpty(req.MemberNo))
        {
            var p_MemberNo = PortQuery.Equal(req.MemberNo);
            query = query.Where(x => EF.Functions.Like((x.MemberNo ?? "").ToLower(), p_MemberNo, PortQuery.Esc));
        }

        // 2. Lọc CardNo với toán tử '=' (nguyên chuỗi, không phân biệt hoa/thường) — gate :264-267
        if (!string.IsNullOrEmpty(req.CardNo))
        {
            var p_CardNo = PortQuery.Equal(req.CardNo);
            query = query.Where(x => EF.Functions.Like((x.CardNo ?? "").ToLower(), p_CardNo, PortQuery.Esc));
        }

        // 3. DLCPCode, OrgID, NetworkID: nguồn gate truyền vào RQ nhưng strWhereClause_Crd_DealSerRO (gate :255-270)
        // và Crd_DealSerRO_GetX (Card.Deal.cs:166-168) HOÀN TOÀN KHÔNG lọc trong mệnh đề WHERE -> giữ nguyên hành vi nguồn (Bài học A.18).

        // 4. Sắp xếp: order by cdsro.LogLUDTimeUTC asc (Card.Deal.cs:168)
        var ordered = query.OrderBy(x => x.LogLUDTimeUTC).ThenBy(x => x.DealSerRONo);
        var itemCount = await ordered.CountAsync();

        // 5. Phân trang: chống tràn số (Bài học A.12) và xử lý pageSize = 0 (Bài học A.7)
        long recordStart = (long)pageIndex * pageSize;
        long recordCount = pageSize;

        var dataList = (recordCount <= 0 || recordStart < 0)
            ? new List<Crd_DealSerRO>()
            : await ordered
                .Skip((int)Math.Min(recordStart, int.MaxValue))
                .Take((int)Math.Min(recordCount, int.MaxValue))
                .ToListAsync();

        // 6. Tính PageCount: khi dataList rỗng (bao gồm trường hợp pageSize = 0) thì PageCount = 0 (gate :62-70, :84-92)
        int ps = pageSize == 0 ? 10 : pageSize;
        int pageCount = dataList.Count == 0 ? 0 : (itemCount % ps == 0 ? itemCount / ps : itemCount / ps + 1);

        var pageInfo = new PageInfo<Crd_DealSerRO>(dataList, itemCount, pageCount, pageIndex, pageSize);

        return Ok(new { Success = true, Data = pageInfo });
    }
}
