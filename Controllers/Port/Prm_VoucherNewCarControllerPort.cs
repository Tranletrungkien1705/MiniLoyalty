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

    // ===================== #266-#270: Save / Approve / Finish / Delete / Cancel =====================
    // Gate: Prm_VoucherNewCarController.cs:246 (Save), :340 (Approve), :406 (Finish), :471 (Delete), :541 (Cancel).
    // Biz: Promotion.cs:6356-7319 (SaveX), :7320 (ApprX), :7592 (FinishX), :7967 (CancelX).

    public class Prm_VoucherNewCarSaveReq
    {
        public Prm_VoucherNewCar? Prm_VoucherNewCar { get; set; }
        public List<Prm_VoucherNewCarDtl>? Lst_Prm_VoucherNewCarDtl { get; set; }
        public List<Prm_VoucherNewCarSpec>? Lst_Prm_VoucherNewCarSpec { get; set; }
        public string? DLCPCode { get; set; }
    }

    public class Prm_VoucherNewCarStatusReq
    {
        public string? PrmVoucherCode { get; set; }
        public string? Remark { get; set; }
        public string? DLCPCode { get; set; }
    }

    private static string? VcStdParam(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim().ToUpperInvariant();
    private static string? VcStdStr(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim().Normalize();
    private static string? VcStdDate(string? s)
        => !string.IsNullOrWhiteSpace(s) && DateTime.TryParse(s, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var d) ? d.ToString("yyyy-MM-dd") : null;

    private string VcUser() => Request.Headers.TryGetValue("WAUserCode", out var h) && !string.IsNullOrWhiteSpace(h) ? h.ToString().Trim() : "port";

    // Sys_User_ForCP_GetAbilityViewOfUser(...)["FlagRoot"]: đại lý của user (port chưa có xác thực => DLCPCode từ body/header, mặc định HTV).
    private async Task<(string? FlagRoot, string? DlcpCode)> VcAbilityAsync(string? bodyDlcp)
    {
        var code = !string.IsNullOrWhiteSpace(bodyDlcp) ? bodyDlcp.Trim()
            : (Request.Headers.TryGetValue("DLCPCode", out var h) && !string.IsNullOrWhiteSpace(h) ? h.ToString().Trim() : "HTV");
        var p = PortQuery.Equal(code);
        var dl = await _db.Set<Mst_DealerCP>().AsNoTracking().FirstOrDefaultAsync(d => EF.Functions.Like((d.DLCPCode ?? "").ToLower(), p, PortQuery.Esc));
        return (dl?.FlagRoot, dl?.DLCPCode);
    }

    [HttpPost("save")]
    public async Task<IActionResult> Save([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Prm_VoucherNewCarSaveReq? req)
    {
        if (req?.Prm_VoucherNewCar == null)
        {
            return PortResult.Fail("Prm_VoucherNewCar_Save", "Check.strJson", null);
        }
        var h = req.Prm_VoucherNewCar;
        var dtimeSys = DateTime.UtcNow;
        var todayStr = dtimeSys.ToString("yyyy-MM-dd");
        var strWAUserCode = VcUser();
        var nwHeader = Request.Headers["NetworkID"].FirstOrDefault();
        var ogHeader = Request.Headers["OrgID"].FirstOrDefault();
        var strNetworkID = !string.IsNullOrWhiteSpace(nwHeader) ? nwHeader : "1001";
        var strOrgID = !string.IsNullOrWhiteSpace(ogHeader) ? ogHeader : "1001";

        // Gate :35-55: luôn lấy mã mới từ Seq (SeqType.PrmVoucherCode), ghi đè PrmVoucherCode.
        var seq = await PortSeq.NextAsync<Seq_PrmVoucherCode>(_db);
        string strPrmVoucherCode = PortSeq.Build("{0}PRMVC.{3}.{1:00000}{2}", "", seq, 100000, "", dtimeSys).ToUpperInvariant();

        string? strName = VcStdStr(h.PrmVoucherName);
        double dQtyDay = h.QtyDayLimitFDlvDate ?? 0;
        string? strEffStart = VcStdDate(h.EffDateStart);
        string? strEffEnd = VcStdDate(h.EffDateEnd);
        double dValidity = h.ValidityPeriod ?? 0;
        string strFlagAllModel = h.FlagAllModel == "1" ? "1" : "0";
        double dPointVoucherAll = h.PointVoucherAllModel ?? 0;
        double dPointUseLimitAll = h.PointUseLimitAllModel ?? 0;
        string? strRemark = VcStdStr(h.Remark);

        var ability = await VcAbilityAsync(req.DLCPCode);

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            if (!string.Equals(ability.FlagRoot, "1", StringComparison.OrdinalIgnoreCase))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Prm_VoucherNewCar_Save_InvalidPermission", "Check.DB.FlagRoot", ability.FlagRoot, "Check.FlagRoot.Expected", "1");
            }
            if (string.IsNullOrEmpty(strName))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Prm_VoucherNewCar_Save_InvalidPrmVoucherName", "Check.PrmVoucherName", strName);
            }
            if (string.IsNullOrEmpty(strEffStart))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Prm_VoucherNewCar_Save_InvalidEffDateStart", "Check.EffDateStart", strEffStart);
            }
            if (string.IsNullOrEmpty(strEffEnd))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Prm_VoucherNewCar_Save_InvalidEffDateEnd", "Check.EffDateEnd", strEffEnd);
            }
            if (string.Compare(strEffStart, todayStr, StringComparison.Ordinal) < 0)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Prm_VoucherNewCar_Save_InvalidEffDateStartBeforeEffSysDate", "Check.EffDateStart", strEffStart, "Check.SysDate", todayStr);
            }
            if (string.Compare(strEffStart, strEffEnd, StringComparison.Ordinal) > 0)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Prm_VoucherNewCar_Save_InvalidEffDateStartBeforeEffDateEnd", "Check.EffDateStart", strEffStart, "Check.EffDateEnd", strEffEnd);
            }
            if (dPointUseLimitAll > dPointVoucherAll)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("PointUseLimitAllModelLimitMustLessThanPointVoucherAllModel", "Check.PointUseLimitAllModel", dPointUseLimitAll, "Check.PointVoucherAllModel", dPointVoucherAll);
            }

            var prev = await _db.Set<Prm_VoucherNewCar>().AsNoTracking()
                .Where(t => t.PrmVoucherCode != strPrmVoucherCode && t.PrmVoucherStatus == "FINISH"
                         && string.Compare(t.EffDateStart, todayStr) <= 0 && string.Compare(t.EffDateEnd, todayStr) >= 0)
                .OrderBy(t => t.EffDateStart).FirstOrDefaultAsync();
            if (prev != null && string.Compare(strEffStart, prev.EffDateStart, StringComparison.Ordinal) <= 0)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Prm_VoucherNewCar_Save_InvalidEffDateStartBeforeEffDateStartPrevious",
                    "Check.strEffDateStart", strEffStart, "Check.PrmVoucherCode_Previous", prev.PrmVoucherCode, "Check.strEffDateStart_Previous", prev.EffDateStart);
            }
            var dup = await _db.Set<Prm_VoucherNewCar>().AsNoTracking()
                .Where(t => t.PrmVoucherCode != strPrmVoucherCode && t.PrmVoucherStatus == "FINISH" && t.EffDateStart == strEffStart)
                .OrderBy(t => t.EffDateStart).FirstOrDefaultAsync();
            if (dup != null)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Prm_VoucherNewCar_Save_InvalidDuplicateEffDateStart", "Check.strEffDateStart", strEffStart, "Check.PrmVoucherCode_Previous", dup.PrmVoucherCode);
            }

            if (req.Lst_Prm_VoucherNewCarDtl == null)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Prm_VoucherNewCar_Save_Input_Prm_VoucherNewCarDtlTblNotFound", "Check.TableName", "Prm_VoucherNewCarDtl");
            }
            foreach (var d in req.Lst_Prm_VoucherNewCarDtl)
            {
                double pv = d.PointVoucher ?? 0, pu = d.PointUseLimit ?? 0;
                if (pv <= 0.0)
                {
                    await tx.RollbackAsync();
                    return PortResult.Fail("Prm_VoucherNewCar_Save_Input_InvalidPointVoucher", "Check.objPointVoucher", pv);
                }
                if (pu <= 0.0)
                {
                    await tx.RollbackAsync();
                    return PortResult.Fail("Prm_VoucherNewCar_Save_Input_InvalidPointUseLimit", "Check.objPointUseLimit", pu);
                }
                if (pu > pv)
                {
                    await tx.RollbackAsync();
                    return PortResult.Fail("Prm_VoucherNewCar_Save_PointUseLimitMustLessThanPointVoucher", "Check.PointVoucher", pv, "Check.PointUseLimit", pu);
                }
            }
            if (req.Lst_Prm_VoucherNewCarSpec == null)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Prm_VoucherNewCar_Save_Input_Prm_VoucherNewCarSpecTblNotFound", "Check.TableName", "Prm_VoucherNewCarSpec");
            }

            // Insert Prm_VoucherNewCar + Dtl + Spec (Promotion.cs:~6990-7240); PointActual* = Point*, QtyUseLimit* = 1.
            _db.Set<Prm_VoucherNewCar>().Add(new Prm_VoucherNewCar
            {
                PrmVoucherCode = strPrmVoucherCode, NetworkID = strNetworkID, OrgID = strOrgID, PrmVoucherName = strName,
                EffDateStart = strEffStart, EffDateEnd = strEffEnd, QtyDayLimitFDlvDate = dQtyDay, ValidityPeriod = dValidity,
                FlagAllModel = strFlagAllModel, PointVoucherAllModel = dPointVoucherAll, PointUseLimitAllModel = dPointUseLimitAll,
                PointActualVCAllModel = dPointVoucherAll, QtyUseLimitAllModel = 1,
                CreateDTimeUTC = dtimeSys, CreateBy = strWAUserCode, LUDTimeUTC = dtimeSys, LUBy = strWAUserCode,
                PrmVoucherStatus = "PENDING", Remark = strRemark, LogLUDTimeUTC = dtimeSys, LogLUBy = strWAUserCode
            });
            foreach (var d in req.Lst_Prm_VoucherNewCarDtl)
            {
                _db.Set<Prm_VoucherNewCarDtl>().Add(new Prm_VoucherNewCarDtl
                {
                    PrmVoucherCode = strPrmVoucherCode, Idx = d.Idx, NetworkID = strNetworkID,
                    PointVoucher = d.PointVoucher, PointUseLimit = d.PointUseLimit, PointActualVoucher = d.PointVoucher, QtyUseLimit = 1,
                    Remark = d.Remark, LogLUDTimeUTC = dtimeSys, LogLUBy = strWAUserCode
                });
            }
            foreach (var s in req.Lst_Prm_VoucherNewCarSpec)
            {
                _db.Set<Prm_VoucherNewCarSpec>().Add(new Prm_VoucherNewCarSpec
                {
                    PrmVoucherCode = strPrmVoucherCode, Idx = s.Idx, ModelCode = VcStdParam(s.ModelCode) ?? "", NetworkID = strNetworkID,
                    LogLUDTimeUTC = dtimeSys, LogLUBy = strWAUserCode
                });
            }
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return Ok(new { Success = true, Data = (object?)null });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return PortResult.Fail("Prm_VoucherNewCar_Save", "Exception", ex.Message);
        }
    }

    // Dùng chung cho Approve/Finish/Cancel (Promotion.cs:7320, 7592, 7967): CheckDB -> FlagRoot -> (Finish: trùng EffDateStart) -> cập nhật.
    private async Task<IActionResult> VcChangeStatusAsync(Prm_VoucherNewCarStatusReq? req, string action, string statusListToCheck, string newStatus)
    {
        if (string.IsNullOrWhiteSpace(req?.PrmVoucherCode))
        {
            return PortResult.Fail("invalid_PrmVoucherCode", "Check.PrmVoucherCode", req?.PrmVoucherCode);
        }
        string? code = VcStdParam(req.PrmVoucherCode);
        string strRemark = (req.Remark ?? "").Trim();
        var strWAUserCode = VcUser();
        var ability = await VcAbilityAsync(req.DLCPCode);

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var p = PortQuery.Equal(code ?? "");
            var existing = await _db.Set<Prm_VoucherNewCar>().FirstOrDefaultAsync(x => EF.Functions.Like((x.PrmVoucherCode ?? "").ToLower(), p, PortQuery.Esc));
            if (existing == null)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Prm_VoucherNewCar_CheckDB_VoucherNewCarNotFound", "Check.PrmVoucherCode", code);
            }
            if (!statusListToCheck.Contains(existing.PrmVoucherStatus ?? ""))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Prm_VoucherNewCar_CheckDB_StatusNotMatched",
                    "Check.PrmVoucherCode", code, "Check.strStatusListToCheck", statusListToCheck, "DB.Status", existing.PrmVoucherStatus);
            }
            if (!string.Equals(ability.FlagRoot, "1", StringComparison.OrdinalIgnoreCase))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Prm_VoucherNewCar_" + action + "_InvalidPermission", "Check.DB.FlagRoot", ability.FlagRoot, "Check.FlagRoot.Expected", "1");
            }
            var dtimeSys = DateTime.UtcNow;
            var todayStr = dtimeSys.ToString("yyyy-MM-dd");
            if (newStatus == "FINISH")
            {
                var dup = await _db.Set<Prm_VoucherNewCar>().AsNoTracking()
                    .Where(t => t.PrmVoucherCode != existing.PrmVoucherCode && t.PrmVoucherStatus == "FINISH" && t.EffDateStart == existing.EffDateStart)
                    .OrderBy(t => t.EffDateStart).FirstOrDefaultAsync();
                if (dup != null)
                {
                    await tx.RollbackAsync();
                    return PortResult.Fail("Prm_VoucherNewCar_Finish_InvalidDuplicateEffDateStart", "Check.strEffDateStart", existing.EffDateStart, "Check.PrmVoucherCode_Previous", dup.PrmVoucherCode);
                }
            }

            // LỖI NGUỒN GIỮ NGUYÊN: LUDTimeUTC/LUBy không nằm trong mệnh đề SET => không đổi.
            existing.LogLUDTimeUTC = dtimeSys;
            existing.LogLUBy = strWAUserCode;
            existing.Remark = strRemark;
            existing.PrmVoucherStatus = newStatus;
            if (newStatus == "APPROVE") { existing.ApprDTimeUTC = dtimeSys; existing.ApprBy = strWAUserCode; }
            else if (newStatus == "FINISH") { existing.FinishDTimeUTC = dtimeSys; existing.FinishBy = strWAUserCode; }
            else { existing.CancelDTimeUTC = dtimeSys; existing.CancelBy = strWAUserCode; }
            await _db.SaveChangesAsync();

            if (newStatus == "FINISH")
            {
                // Promotion.cs:7790-7870 (Update EffDateEnd): chương trình FINISH đang hiệu lực hôm nay (EffDateStart giảm dần).
                var prev = await _db.Set<Prm_VoucherNewCar>()
                    .Where(t => t.PrmVoucherCode != existing.PrmVoucherCode && t.PrmVoucherStatus == "FINISH"
                             && string.Compare(t.EffDateStart, todayStr) <= 0 && string.Compare(t.EffDateEnd, todayStr) >= 0)
                    .OrderByDescending(t => t.EffDateStart).FirstOrDefaultAsync();
                if (prev != null)
                {
                    var dateStart = DateTime.Parse(existing.EffDateStart, System.Globalization.CultureInfo.InvariantCulture);
                    var dateToday = DateTime.Parse(todayStr, System.Globalization.CultureInfo.InvariantCulture);
                    if (dateStart <= dateToday)
                    {
                        prev.EffDateEnd = dateToday.AddDays(-1).ToString("yyyy-MM-dd");
                        await _db.SaveChangesAsync();
                    }
                }
            }
            await tx.CommitAsync();
            return Ok(new { Success = true, Data = (object?)null });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return PortResult.Fail("Prm_VoucherNewCar_" + action, "Exception", ex.Message);
        }
    }

    [HttpPost("approve")]
    public Task<IActionResult> Approve([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Prm_VoucherNewCarStatusReq? req)
        => VcChangeStatusAsync(req, "Appr", "PENDING", "APPROVE");

    [HttpPost("finish")]
    public Task<IActionResult> Finish([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Prm_VoucherNewCarStatusReq? req)
        => VcChangeStatusAsync(req, "Finish", "APPROVE", "FINISH");

    [HttpPost("cancel")]
    public Task<IActionResult> Cancel([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Prm_VoucherNewCarStatusReq? req)
        => VcChangeStatusAsync(req, "Cancel", "PENDING, APPROVE", "CANCEL");

    // Gate Delete(strJson) :471 => WA_Prm_VoucherNewCar_Save với FlagIsDelete = "1" (mã do client gửi).
    [HttpPost("delete")]
    public async Task<IActionResult> Delete([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Prm_VoucherNewCarSaveReq? req)
    {
        if (req?.Prm_VoucherNewCar == null)
        {
            return PortResult.Fail("Prm_VoucherNewCar_Save", "Check.strJson", null);
        }
        string? code = VcStdParam(req.Prm_VoucherNewCar.PrmVoucherCode);
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var p = PortQuery.Equal(code ?? "");
            var existing = await _db.Set<Prm_VoucherNewCar>().FirstOrDefaultAsync(x => EF.Functions.Like((x.PrmVoucherCode ?? "").ToLower(), p, PortQuery.Esc));
            if (existing == null)
            {
                await tx.CommitAsync();
                return Ok(new { Success = true, Data = (object?)null });
            }
            if (!string.Equals(existing.PrmVoucherStatus, "PENDING", StringComparison.OrdinalIgnoreCase))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Prm_VoucherNewCar_Save_InvalidPrmVoucherStatus",
                    "Check.DB.PrmVoucherStatus", existing.PrmVoucherStatus, "Check.PrmVoucherStatus.Expected", "PENDING");
            }
            _db.Set<Prm_VoucherNewCarSpec>().RemoveRange(await _db.Set<Prm_VoucherNewCarSpec>().Where(x => EF.Functions.Like((x.PrmVoucherCode ?? "").ToLower(), p, PortQuery.Esc)).ToListAsync());
            _db.Set<Prm_VoucherNewCarDtl>().RemoveRange(await _db.Set<Prm_VoucherNewCarDtl>().Where(x => EF.Functions.Like((x.PrmVoucherCode ?? "").ToLower(), p, PortQuery.Esc)).ToListAsync());
            _db.Set<Prm_VoucherNewCar>().Remove(existing);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return Ok(new { Success = true, Data = (object?)null });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return PortResult.Fail("Prm_VoucherNewCar_Save", "Exception", ex.Message);
        }
    }

    // ===================== #265: GetByPrmVoucherCode =====================
    // Gate Prm_VoucherNewCarController.cs:148-244 -> WA_Prm_VoucherNewCar_Get (Promotion.cs:6028-6273, Ft_RecordCount = "1").
    public class Prm_VoucherNewCarGetByCodeReq
    {
        public string? PrmVoucherCode { get; set; }
    }

    [HttpPost("getbyprmvouchercode")]
    public async Task<IActionResult> GetByPrmVoucherCode([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Prm_VoucherNewCarGetByCodeReq? req)
    {
        var code = req?.PrmVoucherCode;
        if (string.IsNullOrEmpty(code))
        {
            return Ok(new { Success = true, Data = new { Prm_VoucherNewCar = (object?)null, Lst_Prm_VoucherNewCarDtl = (object?)null, Lst_Prm_VoucherNewCarSpec = (object?)null } });
        }

        var pat = PortQuery.Equal(code);
        var pcn = await _db.Set<Prm_VoucherNewCar>().AsNoTracking()
            .Where(x => EF.Functions.Like((x.PrmVoucherCode ?? "").ToLower(), pat, PortQuery.Esc))
            .OrderByDescending(x => x.CreateDTimeUTC)
            .FirstOrDefaultAsync();

        object? header = null;
        if (pcn != null)
        {
            var dateSys = DateTime.UtcNow.AddHours(7).ToString("yyyy-MM-dd");
            var activeProgram = await _db.Set<Prm_VoucherNewCar>().AsNoTracking()
                .Where(t => t.PrmVoucherStatus == "FINISH" && string.Compare(t.EffDateStart, dateSys) <= 0 && string.Compare(dateSys, t.EffDateEnd) <= 0)
                .OrderByDescending(t => t.EffDateStart)
                .Select(t => t.PrmVoucherCode)
                .FirstOrDefaultAsync();
            header = new
            {
                MyIdxSeq = 0,
                pcn.PrmVoucherCode, pcn.NetworkID, pcn.OrgID, pcn.PrmVoucherName, pcn.EffDateStart,
                EffDateEnd = pcn.EffDateEnd == "2100-01-01" ? "" : pcn.EffDateEnd,
                pcn.QtyDayLimitFDlvDate, pcn.ValidityPeriod, pcn.FlagAllModel, pcn.PointVoucherAllModel, pcn.PointUseLimitAllModel,
                pcn.PointActualVCAllModel, pcn.QtyUseLimitAllModel,
                // Gate :203-208: ApprDTimeUTC/CreateDTimeUTC đổi sang giờ địa phương (utcOffset mặc định 7).
                CreateDTimeUTC = pcn.CreateDTimeUTC.AddHours(7).ToString("yyyy-MM-dd HH:mm:ss"),
                pcn.CreateBy, pcn.LUDTimeUTC, pcn.LUBy,
                ApprDTimeUTC = pcn.ApprDTimeUTC?.AddHours(7).ToString("yyyy-MM-dd HH:mm:ss"),
                pcn.ApprBy, pcn.FinishDTimeUTC, pcn.FinishBy, pcn.CancelDTimeUTC, pcn.CancelBy, pcn.PrmVoucherStatus, pcn.Remark, pcn.LogLUDTimeUTC, pcn.LogLUBy,
                FlagShow = (!string.IsNullOrEmpty(activeProgram) && pcn.PrmVoucherCode == activeProgram) ? "1" : "0"
            };
        }

        var dtl = pcn == null ? new List<Prm_VoucherNewCarDtl>() : await _db.Set<Prm_VoucherNewCarDtl>().AsNoTracking()
            .Where(x => x.PrmVoucherCode == pcn.PrmVoucherCode).OrderBy(x => x.Idx).ToListAsync();
        var spec = pcn == null ? new List<Prm_VoucherNewCarSpec>() : await _db.Set<Prm_VoucherNewCarSpec>().AsNoTracking()
            .Where(x => x.PrmVoucherCode == pcn.PrmVoucherCode).OrderBy(x => x.Idx).ToListAsync();

        return Ok(new
        {
            Success = true,
            Data = new
            {
                Prm_VoucherNewCar = header,
                Lst_Prm_VoucherNewCarDtl = dtl.Count > 0 ? dtl : null,
                Lst_Prm_VoucherNewCarSpec = spec.Count > 0 ? spec : null
            }
        });
    }
}
