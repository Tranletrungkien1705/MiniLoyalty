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
// PORT 1:1 — Prm_VoucherNewCarController/Search (#264).
//
// Nguồn gate:
//   Prm_VoucherNewCarController.cs:32-146        Search -> WA_Prm_VoucherNewCar_Get
//   Prm_VoucherNewCarController.cs:723-760       strWhereClause_Prm_VoucherNewCar
//   Prm_VoucherNewCarController.cs:775-811       strWhereClause_Prm_VoucherNewCarFlagShow
// Nguồn API:
//   PrmVoucherNewCarController.cs:21-89          WA_Prm_VoucherNewCar_Get
// Nguồn biz:
//   Promotion.cs:5746-5867                       WAS_Prm_VoucherNewCar_Get
//   Promotion.cs:5869-6026                       Prm_VoucherNewCar_Get
//   Promotion.cs:6028-6273                       Prm_VoucherNewCar_GetX
//
// ĐỐI CHIẾU THAM SỐ VÀ NGHIỆP VỤ:
//   - Endpoint: POST /api/prm_vouchernewcar/search
//   - Tham số: OrgID, KeyWord, PrmVoucherCode, PrmVoucherName, QtyDayLimitFDlvDate, EffDateStart,
//     EffDateEnd, ValidityPeriod, PrmVoucherStatus, CreateDTimeUTCFrom, CreateDTimeUTCTo, FlagShow,
//     Ft_PageIndex (default 0), Ft_PageSize (default 10).
//   - strWhereClause_Prm_VoucherNewCar:
//     * PrmVoucherCode: '='
//     * PrmVoucherName: '='
//     * EffDateStart: '='
//     * EffDateEnd: '='
//     * CreateDTimeUTC >= CreateDTimeUTCFrom + " 00:00:00"
//     * CreateDTimeUTC <= CreateDTimeUTCTo + " 23:59:59"
//     (Lưu ý: OrgID, KeyWord, QtyDayLimitFDlvDate, ValidityPeriod, PrmVoucherStatus tuy gate có nhận
//      nhưng strWhereClause_Prm_VoucherNewCar ở gate :723-760 KHÔNG sinh mệnh đề where cho các trường này!
//      Nhưng để an toàn và nhất quán, giữ nguyên các trường này trong DTO request).
//   - strWhereClause_Prm_VoucherNewCarFlagShow:
//     * FlagShow == "1": PrmVoucherStatus = 'FINISH' and EffDateStart <= today and EffDateEnd >= today
//     * FlagShow == "0": PrmVoucherStatus != 'FINISH' or (PrmVoucherStatus = 'FINISH' and (EffDateStart > today or EffDateEnd < today))
//     (trong đó today = DateTime.UtcNow.ToString("yyyy-MM-dd"))
//   - Sắp xếp: order by pcn.CreateDTimeUTC desc
//   - Cột tính FlagShow trả về:
//     * PrmVoucherCodeForFlagShow: top 1 Prm_VoucherNewCar where PrmVoucherStatus = 'FINISH' and EffDateStart <= dateSys and dateSys <= EffDateEnd order by EffDateStart desc
//     * FlagShow = 1 nếu PrmVoucherCode == PrmVoucherCodeForFlagShow, ngược lại 0.
//     (trong đó dateSys = DateTime.UtcNow.AddHours(7).ToString("yyyy-MM-dd"))
//   - Format ngày giờ hiển thị ở gate:
//     * ApprDTimeUTC, CreateDTimeUTC: chuyển sang giờ local (utcOffset, mặc định +7) định dạng yyyy-MM-dd HH:mm:ss
//     * EffDateEnd == "2100-01-01" => gán thành ""
//   - Phân trang: PageInfo<Prm_VoucherNewCar> (DataList, ItemCount, PageCount, PageIndex, PageSize)
//     * PageSize == 0 => DataList rỗng, PageCount = 0 (gate :98-103)
// ============================================================================================

public class Prm_VoucherNewCarSearchReq
{
    public string? OrgID { get; set; }
    public string? KeyWord { get; set; }
    public string? PrmVoucherCode { get; set; }
    public string? PrmVoucherName { get; set; }
    public string? QtyDayLimitFDlvDate { get; set; }
    public string? EffDateStart { get; set; }
    public string? EffDateEnd { get; set; }
    public string? ValidityPeriod { get; set; }
    public string? PrmVoucherStatus { get; set; }
    public string? CreateDTimeUTCFrom { get; set; }
    public string? CreateDTimeUTCTo { get; set; }
    public string? FlagShow { get; set; }
    public int Ft_PageIndex { get; set; } = 0;
    public int Ft_PageSize { get; set; } = 10;
}

[ApiController]
[Route("api/prm_vouchernewcar")]
public class Prm_VoucherNewCarControllerPort : ControllerBase
{
    private readonly AppDbContext _db;

    public Prm_VoucherNewCarControllerPort(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Prm_VoucherNewCarSearchReq? req)
    {
        req ??= new Prm_VoucherNewCarSearchReq();
        int pageIndex = req.Ft_PageIndex;
        int pageSize = req.Ft_PageSize;

        var query = _db.Set<Prm_VoucherNewCar>().AsQueryable();

        // 1. Lọc theo PrmVoucherCode (=)
        if (!string.IsNullOrEmpty(req.PrmVoucherCode))
        {
            var p_Code = PortQuery.Equal(req.PrmVoucherCode);
            query = query.Where(x => EF.Functions.Like((x.PrmVoucherCode ?? "").ToLower(), p_Code, PortQuery.Esc));
        }

        // 2. Lọc theo PrmVoucherName (=)
        if (!string.IsNullOrEmpty(req.PrmVoucherName))
        {
            var p_Name = PortQuery.Equal(req.PrmVoucherName);
            query = query.Where(x => EF.Functions.Like((x.PrmVoucherName ?? "").ToLower(), p_Name, PortQuery.Esc));
        }

        // 3. Lọc theo EffDateStart (=)
        if (!string.IsNullOrEmpty(req.EffDateStart))
        {
            var p_EffStart = PortQuery.Equal(req.EffDateStart);
            query = query.Where(x => EF.Functions.Like((x.EffDateStart ?? "").ToLower(), p_EffStart, PortQuery.Esc));
        }

        // 4. Lọc theo EffDateEnd (=)
        if (!string.IsNullOrEmpty(req.EffDateEnd))
        {
            var p_EffEnd = PortQuery.Equal(req.EffDateEnd);
            query = query.Where(x => EF.Functions.Like((x.EffDateEnd ?? "").ToLower(), p_EffEnd, PortQuery.Esc));
        }

        // 5. Lọc theo CreateDTimeUTCFrom / To (gate :54-55, :749-756)
        if (!string.IsNullOrEmpty(req.CreateDTimeUTCFrom))
        {
            if (DateTime.TryParse(req.CreateDTimeUTCFrom + " 00:00:00", out var dtFrom))
            {
                query = query.Where(x => x.CreateDTimeUTC >= dtFrom);
            }
        }
        if (!string.IsNullOrEmpty(req.CreateDTimeUTCTo))
        {
            if (DateTime.TryParse(req.CreateDTimeUTCTo + " 23:59:59", out var dtTo))
            {
                query = query.Where(x => x.CreateDTimeUTC <= dtTo);
            }
        }

        // 6. Lọc theo FlagShow (gate :784-806, dùng UtcNow.ToString("yyyy-MM-dd"))
        var todayFilterStr = DateTime.UtcNow.ToString("yyyy-MM-dd");
        if (!string.IsNullOrEmpty(req.FlagShow))
        {
            if (req.FlagShow == "1")
            {
                query = query.Where(x => x.PrmVoucherStatus == "FINISH" && string.Compare(x.EffDateStart, todayFilterStr) <= 0 && string.Compare(todayFilterStr, x.EffDateEnd) <= 0);
            }
            else if (req.FlagShow == "0")
            {
                query = query.Where(x => x.PrmVoucherStatus != "FINISH" || (x.PrmVoucherStatus == "FINISH" && (string.Compare(x.EffDateStart, todayFilterStr) > 0 || string.Compare(todayFilterStr, x.EffDateEnd) > 0)));
            }
        }

        // Đếm tổng số dòng khớp bộ lọc (Summary: count(0))
        var listAll = await query.OrderByDescending(x => x.CreateDTimeUTC).ToListAsync();
        var itemCount = listAll.Count;

        var pagedList = new List<Prm_VoucherNewCar>();
        long recordStart = (long)pageIndex * pageSize;
        if (itemCount > 0 && pageSize > 0 && pageIndex >= 0 && recordStart < itemCount)
        {
            pagedList = listAll.Skip((int)Math.Min(recordStart, int.MaxValue)).Take((int)Math.Min(pageSize, int.MaxValue)).ToList();
        }
        else if (pageSize == 0)
        {
            pagedList = new List<Prm_VoucherNewCar>();
        }

        // Biz tính PrmVoucherCodeForFlagShow (Promotion.cs:6093-6100, 770-773, dateSys = UtcNow + 7)
        var dateSys = DateTime.UtcNow.AddHours(7).ToString("yyyy-MM-dd");
        var activeProgram = await _db.Set<Prm_VoucherNewCar>()
            .Where(t => t.PrmVoucherStatus == "FINISH" && string.Compare(t.EffDateStart, dateSys) <= 0 && string.Compare(dateSys, t.EffDateEnd) <= 0)
            .OrderByDescending(t => t.EffDateStart)
            .Select(t => t.PrmVoucherCode)
            .FirstOrDefaultAsync();

        long idxSeq = Math.Max(0, recordStart);
        foreach (var item in pagedList)
        {
            item.MyIdxSeq = idxSeq++;
            item.FlagShow = (!string.IsNullOrEmpty(activeProgram) && item.PrmVoucherCode == activeProgram) ? "1" : "0";

            // Gate format (gate :91-94)
            if (item.EffDateEnd == "2100-01-01")
            {
                item.EffDateEnd = "";
            }
        }

        int pageCount = 0;
        if (pageSize > 0 && pagedList.Count > 0)
        {
            pageCount = (itemCount % pageSize == 0) ? itemCount / pageSize : itemCount / pageSize + 1;
        }

        var pageInfo = new PageInfo<Prm_VoucherNewCar>(pagedList, itemCount, pageCount, pageIndex, pageSize);

        return Ok(new { Success = true, Data = pageInfo });
    }
}
