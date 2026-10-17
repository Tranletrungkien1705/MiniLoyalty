using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniLoyalty.Data;
using MiniLoyalty.Models;
using MiniLoyalty.Models.Port;

namespace MiniLoyalty.Controllers.Port;

// ============================================================================================
// PORT 1:1 — ReqMemberRegisterController (#274 & #275).
//
// 1. GetByReqMemberRegisterCodeSys (#274)
//    - Gate: ReqMemberRegisterController.cs:152-253
//    - Filter: strWhereClause_Req_MemberRegister_By_ReqMemberRegisterCodeSys (:468-479)
//
// 2. GetByReqMemberRegisterCode (#275)
//    - Gate: ReqMemberRegisterController.cs:255-356
//    - Filter: strWhereClause_Req_MemberRegister_By_ReqMemberRegisterCode (:481-492)
//
// API / BIZ chung:
//    - API: ReqMemberRegisterController.cs:21-91 WA_Req_MemberRegister_Get
//    - Biz: Member.cs:222-351 WAS_Req_MemberRegister_Get
//    - Biz: Member.cs:354-513 Req_MemberRegister_Get
//    - Biz: Member.cs:515-760 Req_MemberRegister_GetX
//
// GIỮ ĐÚNG NGUỒN:
//   - Endpoint POST: /api/reqmemberregister/getbyreqmemberregistercodesys
//   - Endpoint POST: /api/reqmemberregister/getbyreqmemberregistercode (và hỗ trợ /api/req_memberregister/getbyreqmemberregistercode)
//   - Nhận mã qua body JSON
//   - Lọc toán tử "=" không phân biệt hoa thường bằng EF.Functions.Like(..., PortQuery.Equal(...), PortQuery.Esc)
//   - Nối thông tin từ Mst_Province, Mst_District, CrdMember, Mst_DealerCP nếu có
//   - Chuyển đổi CreateDTimeUTC và ApproveDTimeUTC sang Local Time theo utcOffset (mặc định 7)
//   - Trả về đối tượng Data chứa:
//       Req_MemberRegister, Lst_Req_MemberRegisterIDAttachFile,
//       Lst_Req_MemberRegisterVehicleAttachFile, Lst_Req_MemberRegisterAttachFile
// ============================================================================================

public class Req_MemberRegister_GetByReqMemberRegisterCodeSysReq
{
    public string? ReqMemberRegisterCodeSys { get; set; }
}

public class Req_MemberRegister_GetByReqMemberRegisterCodeReq
{
    public string? ReqMemberRegisterCode { get; set; }
}

public class Req_MemberRegisterGetResult
{
    public Req_MemberRegister? Req_MemberRegister { get; set; }
    public List<Req_MemberRegisterIDAttachFile>? Lst_Req_MemberRegisterIDAttachFile { get; set; }
    public List<Req_MemberRegisterVehicleAttachFile>? Lst_Req_MemberRegisterVehicleAttachFile { get; set; }
    public List<Req_MemberRegisterAttachFile>? Lst_Req_MemberRegisterAttachFile { get; set; }
}

[ApiController]
[Route("api/reqmemberregister")]
[Route("api/req_memberregister")]
public class ReqMemberRegisterControllerPort : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly Microsoft.AspNetCore.Hosting.IWebHostEnvironment? _env;

    public ReqMemberRegisterControllerPort(AppDbContext db, Microsoft.AspNetCore.Hosting.IWebHostEnvironment? env = null)
    {
        _db = db;
        _env = env;
    }

    [HttpPost("getbyreqmemberregistercodesys")]
    public async Task<IActionResult> GetByReqMemberRegisterCodeSys(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Req_MemberRegister_GetByReqMemberRegisterCodeSysReq? req)
    {
        double utcOffset = GetUtcOffset();
        var codeSys = req?.ReqMemberRegisterCodeSys?.Trim();
        if (string.IsNullOrEmpty(codeSys))
        {
            return OkEmptyResult();
        }

        var reg = await _db.Set<Req_MemberRegister>()
            .Where(x => EF.Functions.Like(x.ReqMemberRegisterCodeSys.ToLower(), PortQuery.Equal(codeSys), PortQuery.Esc))
            .FirstOrDefaultAsync();

        if (reg == null)
        {
            return OkEmptyResult();
        }

        return await BuildSuccessResultAsync(reg, utcOffset);
    }

    [HttpPost("getbyreqmemberregistercode")]
    public async Task<IActionResult> GetByReqMemberRegisterCode(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Req_MemberRegister_GetByReqMemberRegisterCodeReq? req)
    {
        double utcOffset = GetUtcOffset();
        var code = req?.ReqMemberRegisterCode?.Trim();
        if (string.IsNullOrEmpty(code))
        {
            return OkEmptyResult();
        }

        // Gate ReqMemberRegisterController.cs:285-288 và strWhereClause_Req_MemberRegister_By_ReqMemberRegisterCode (:481-492)
        // lọc "=" trên cột ReqMemberRegisterCode
        var reg = await _db.Set<Req_MemberRegister>()
            .Where(x => EF.Functions.Like(x.ReqMemberRegisterCode.ToLower(), PortQuery.Equal(code), PortQuery.Esc))
            .FirstOrDefaultAsync();

        if (reg == null)
        {
            return OkEmptyResult();
        }

        return await BuildSuccessResultAsync(reg, utcOffset);
    }

    private double GetUtcOffset()
    {
        double utcOffset = 7;
        var tzHeader = Request.Headers["TimeZone"].FirstOrDefault();
        if (!string.IsNullOrEmpty(tzHeader) && double.TryParse(tzHeader, NumberStyles.Any, CultureInfo.InvariantCulture, out var tzVal))
        {
            utcOffset = tzVal;
        }
        return utcOffset;
    }

    private IActionResult OkEmptyResult()
    {
        return Ok(new
        {
            Success = true,
            Data = new Req_MemberRegisterGetResult
            {
                Req_MemberRegister = null,
                Lst_Req_MemberRegisterIDAttachFile = null,
                Lst_Req_MemberRegisterVehicleAttachFile = null,
                Lst_Req_MemberRegisterAttachFile = null
            }
        });
    }

    private async Task EnrichAsync(Req_MemberRegister reg, double utcOffset)
    {
        // Bổ sung thông tin từ các bảng liên quan như trong Member.cs:630-653
        if (!string.IsNullOrEmpty(reg.ProvinceCode))
        {
            var prov = await _db.Set<Mst_Province>()
                .Where(p => EF.Functions.Like(p.ProvinceCode.ToLower(), PortQuery.Equal(reg.ProvinceCode), PortQuery.Esc))
                .FirstOrDefaultAsync();
            if (prov != null)
            {
                reg.mpv_ProvinceCode = prov.ProvinceCode;
                reg.mpv_ProvinceName = prov.ProvinceName;
            }

            if (!string.IsNullOrEmpty(reg.DistrictCode))
            {
                var dist = await _db.Set<Mst_District>()
                    .Where(d => EF.Functions.Like(d.ProvinceCode.ToLower(), PortQuery.Equal(reg.ProvinceCode), PortQuery.Esc)
                             && EF.Functions.Like(d.DistrictCode.ToLower(), PortQuery.Equal(reg.DistrictCode), PortQuery.Esc))
                    .FirstOrDefaultAsync();
                if (dist != null)
                {
                    reg.mdt_DistrictCode = dist.DistrictCode;
                    reg.mdt_DistrictName = dist.DistrictName;
                }
            }
        }

        if (!string.IsNullOrEmpty(reg.DLCodeRegis))
        {
            var dl = await _db.Set<Mst_DealerCP>()
                .Where(d => EF.Functions.Like(d.DLCPCode.ToLower(), PortQuery.Equal(reg.DLCodeRegis), PortQuery.Esc))
                .FirstOrDefaultAsync();
            if (dl != null)
            {
                reg.mdcp_DLCPCode = dl.DLCPCode;
                reg.mdcp_DLCPName = dl.DLCPName;
            }
        }

        var cm = await _db.Set<CrdMember>()
            .Where(m => EF.Functions.Like((m.ReqMemberRegisterCodeSys ?? "").ToLower(), PortQuery.Equal(reg.ReqMemberRegisterCodeSys), PortQuery.Esc))
            .FirstOrDefaultAsync();
        if (cm != null && cm.RegisFinishDTimeUTC.HasValue)
        {
            reg.cm_RegisFinishDTimeUTC = cm.RegisFinishDTimeUTC.Value.ToString("yyyy-MM-dd HH:mm:ss");
        }

        // Chuyển đổi thời gian UTC sang Local Time theo utcOffset theo nguồn Gate ReqMemberRegisterController.cs:314-321
        if (!string.IsNullOrEmpty(reg.ApproveDTimeUTC) && DateTime.TryParse(reg.ApproveDTimeUTC, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var approveDt))
        {
            reg.ApproveDTimeUTC = approveDt.AddHours(utcOffset).ToString("yyyy-MM-dd HH:mm:ss");
        }
        if (!string.IsNullOrEmpty(reg.CreateDTimeUTC) && DateTime.TryParse(reg.CreateDTimeUTC, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var createDt))
        {
            reg.CreateDTimeUTC = createDt.AddHours(utcOffset).ToString("yyyy-MM-dd HH:mm:ss");
        }

    }

    private async Task<IActionResult> BuildSuccessResultAsync(Req_MemberRegister reg, double utcOffset)
    {
        await EnrichAsync(reg, utcOffset);
        return await BuildAttachResultAsync(reg);
    }

    private async Task<IActionResult> BuildAttachResultAsync(Req_MemberRegister reg)
    {
        // Lấy danh sách attach files (Member.cs:660-715)
        var idFiles = await _db.Set<Req_MemberRegisterIDAttachFile>()
            .Where(f => EF.Functions.Like(f.ReqMemberRegisterCodeSys.ToLower(), PortQuery.Equal(reg.ReqMemberRegisterCodeSys), PortQuery.Esc))
            .OrderBy(f => f.Idx)
            .ToListAsync();

        var vehFiles = await _db.Set<Req_MemberRegisterVehicleAttachFile>()
            .Where(f => EF.Functions.Like(f.ReqMemberRegisterCodeSys.ToLower(), PortQuery.Equal(reg.ReqMemberRegisterCodeSys), PortQuery.Esc))
            .OrderBy(f => f.Idx)
            .ToListAsync();

        var attachFiles = await _db.Set<Req_MemberRegisterAttachFile>()
            .Where(f => EF.Functions.Like(f.ReqMemberRegisterCodeSys.ToLower(), PortQuery.Equal(reg.ReqMemberRegisterCodeSys), PortQuery.Esc))
            .OrderBy(f => f.Idx)
            .ToListAsync();

        return Ok(new
        {
            Success = true,
            Data = new Req_MemberRegisterGetResult
            {
                Req_MemberRegister = reg,
                Lst_Req_MemberRegisterIDAttachFile = idFiles.Count > 0 ? idFiles : null,
                Lst_Req_MemberRegisterVehicleAttachFile = vehFiles.Count > 0 ? vehFiles : null,
                Lst_Req_MemberRegisterAttachFile = attachFiles.Count > 0 ? attachFiles : null
            }
        });
    }

    // ===================== #273 Search / #276 Export =====================
    // Gate ReqMemberRegisterController.cs:31 (Search), :497 (Export); strWhereClause_Req_MemberRegister; Biz Member.cs:515 (Req_MemberRegister_GetX).
    public class Req_MemberRegisterSearchReq
    {
        public string? OrgID { get; set; }
        public string? ReqMemberRegisterCode { get; set; }
        public string? ReqMemberRegisterCodeSys { get; set; }
        public string? RegisterDateFrom { get; set; }
        public string? RegisterDateTo { get; set; }
        public string? DLCodeRegis { get; set; }
        public string? CustomerPhoneNo { get; set; }
        public string? CarNo { get; set; }
        public string? VIN { get; set; }
        public string? CustomerName { get; set; }
        public string? ReqMemberRegisterStatus { get; set; }
        public int Ft_PageIndex { get; set; } = 0;
        public int Ft_PageSize { get; set; } = 10;
    }

    // Export (:497) không có tham số ReqMemberRegisterCode.
    public class Req_MemberRegisterExportReq
    {
        public string? OrgID { get; set; }
        public string? ReqMemberRegisterCodeSys { get; set; }
        public string? RegisterDateFrom { get; set; }
        public string? RegisterDateTo { get; set; }
        public string? DLCodeRegis { get; set; }
        public string? CustomerPhoneNo { get; set; }
        public string? CarNo { get; set; }
        public string? VIN { get; set; }
        public string? CustomerName { get; set; }
        public string? ReqMemberRegisterStatus { get; set; }
    }

    private IQueryable<Req_MemberRegister> RmrFilter(string? codeSys, string? code, string? dateFrom, string? dateTo, string? dlCodeRegis,
        string? phone, string? carNo, string? vin, string? custName, string? status)
    {
        var q = _db.Set<Req_MemberRegister>().AsNoTracking().AsQueryable();
        if (!string.IsNullOrEmpty(codeSys)) { var p = PortQuery.Equal(codeSys); q = q.Where(x => EF.Functions.Like((x.ReqMemberRegisterCodeSys ?? "").ToLower(), p, PortQuery.Esc)); }
        if (!string.IsNullOrEmpty(code)) { var p = PortQuery.Equal(code); q = q.Where(x => EF.Functions.Like((x.ReqMemberRegisterCode ?? "").ToLower(), p, PortQuery.Esc)); }
        if (!string.IsNullOrEmpty(dateFrom)) { q = q.Where(x => string.Compare(x.RegisterDate, dateFrom) >= 0); }
        if (!string.IsNullOrEmpty(dateTo)) { q = q.Where(x => string.Compare(x.RegisterDate, dateTo) <= 0); }
        if (!string.IsNullOrEmpty(dlCodeRegis)) { var p = PortQuery.Equal(dlCodeRegis); q = q.Where(x => EF.Functions.Like((x.DLCodeRegis ?? "").ToLower(), p, PortQuery.Esc)); }
        if (!string.IsNullOrEmpty(phone)) { var p = PortQuery.Contains(phone); q = q.Where(x => EF.Functions.Like((x.CustomerPhoneNo ?? "").ToLower(), p, PortQuery.Esc)); }
        if (!string.IsNullOrEmpty(carNo)) { var p = PortQuery.Equal(carNo); q = q.Where(x => EF.Functions.Like((x.CarNo ?? "").ToLower(), p, PortQuery.Esc)); }
        if (!string.IsNullOrEmpty(vin)) { var p = PortQuery.Equal(vin); q = q.Where(x => EF.Functions.Like((x.VIN ?? "").ToLower(), p, PortQuery.Esc)); }
        if (!string.IsNullOrEmpty(custName)) { var p = PortQuery.Contains(custName); q = q.Where(x => EF.Functions.Like((x.CustomerName ?? "").ToLower(), p, PortQuery.Esc)); }
        if (!string.IsNullOrEmpty(status)) { var p = PortQuery.Equal(status); q = q.Where(x => EF.Functions.Like((x.ReqMemberRegisterStatus ?? "").ToLower(), p, PortQuery.Esc)); }
        return q.OrderByDescending(x => x.CreateDTimeUTC);
    }

    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Req_MemberRegisterSearchReq? req)
    {
        req ??= new Req_MemberRegisterSearchReq();
        double utcOffset = GetUtcOffset();
        int pageIndex = req.Ft_PageIndex;
        int pageSize = req.Ft_PageSize;

        var q = RmrFilter(req.ReqMemberRegisterCodeSys, req.ReqMemberRegisterCode, req.RegisterDateFrom, req.RegisterDateTo, req.DLCodeRegis,
            req.CustomerPhoneNo, req.CarNo, req.VIN, req.CustomerName, req.ReqMemberRegisterStatus);
        int itemCount = await q.CountAsync();

        long recordStart = (long)pageIndex * pageSize;
        var list = new List<Req_MemberRegister>();
        if (pageSize > 0 && recordStart >= 0 && recordStart < itemCount)
        {
            list = await q.Skip((int)Math.Min(recordStart, int.MaxValue)).Take((int)Math.Min(pageSize, int.MaxValue)).ToListAsync();
        }

        int pageCount = 0;
        if (list.Count > 0)
        {
            foreach (var item in list) await EnrichAsync(item, utcOffset);
            int ps = pageSize == 0 ? 10 : pageSize;
            pageCount = (itemCount % ps == 0) ? itemCount / ps : itemCount / ps + 1;
        }
        return Ok(new { Success = true, Data = new PageInfo<Req_MemberRegister>(list, itemCount, pageCount, pageIndex, pageSize) });
    }

    [HttpPost("export")]
    public async Task<IActionResult> Export([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Req_MemberRegisterExportReq? req)
    {
        req ??= new Req_MemberRegisterExportReq();
        double utcOffset = GetUtcOffset();
        var list = await RmrFilter(req.ReqMemberRegisterCodeSys, null, req.RegisterDateFrom, req.RegisterDateTo, req.DLCodeRegis,
            req.CustomerPhoneNo, req.CarNo, req.VIN, req.CustomerName, req.ReqMemberRegisterStatus).ToListAsync();
        if (list.Count == 0)
        {
            return PortResult.Fail("Req_MemberRegister_Export", "Message", "Không có dữ liệu");
        }
        foreach (var item in list) await EnrichAsync(item, utcOffset);

        var webRoot = _env?.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
        {
            webRoot = _env != null ? System.IO.Path.Combine(_env.ContentRootPath, "wwwroot") : System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "wwwroot");
        }
        var dic = new Dictionary<string, string>
        {
            { "ReqMemberRegisterCodeSys", "Mã lượt đăng ký" },
            { "DLCodeRegis", "Đại lý đăng ký" },
            { "RegisterDate", "Ngày HV đăng ký" },
            { "ReqMemberRegisterStatus", "Trạng thái yêu cầu" },
            { "FlagIsSyncDMS", "Trạng thái đồng bộ DMS" },
            { "CarNo", "Biển số" },
            { "VIN", "Số khung" },
            { "ModelName", "Tên Model" },
            { "CustomerName", "Tên khách hàng" },
            { "CustomerIDNo", "MST/CCCD" },
            { "CustomerPhoneNo", "Số điện thoại" },
            { "CustomerDateOfBirth", "Ngày sinh" },
            { "GenderCode", "Mã Giới tính" },
            { "GenderName", "Tên Giới tính" },
            { "ProvinceCode", "Mã Tỉnh/TP" },
            { "ProvinceName", "Tên Tỉnh/TP" },
            { "Remark", "Ghi chú" },
        };
        try
        {
            var (physicalPath, virtualPath) = PortExcel.GenExcelExportFilePath(webRoot, "Req_MemberRegister");
            PortExcel.ExportToExcelFromList(list, dic, physicalPath, "Req_MemberRegister");
            return Ok(new { Success = true, Data = new { Url = PortExcel.GetRequestBaseAddress(Request) + virtualPath } });
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Req_MemberRegister_Export", "Exception", ex.Message);
        }
    }

    // ===================== #277-#280: Approve / ApproveOld / Cancel / Reject =====================
    // Gate ReqMemberRegisterController.cs:645 (Approve), :712 (ApproveOld), :778 (Cancel), :848 (Reject).
    // Biz Member.cs: ApprX :2214 (Approve + ApproveOld cùng gọi WA_Req_MemberRegister_Appr), CancelX :3099, RejectX :5556; Req_MemberRegister_CheckDB.
    // Nguồn còn gọi dịch vụ NGOÀI HTC TV Mobile App (ReqMemberRegisterUpdStatus) chỉ khi Mst_Param HTC_TV_Mobile_App_Url có giá trị; MiniLoyalty không có tham số này => bỏ qua.
    public class Req_MemberRegisterStatusReq
    {
        public Req_MemberRegisterStatusHeader? Req_MemberRegister { get; set; }
        public List<Req_MemberRegisterAttachFile>? Lst_Req_MemberRegisterAttachFile { get; set; }
    }

    public class Req_MemberRegisterStatusHeader
    {
        public string? ReqMemberRegisterCodeSys { get; set; }
        public string? Remark { get; set; }
    }

    public class Req_MemberRegisterApproveOldReq
    {
        public string? ReqMemberRegisterCodeSys { get; set; }
        public string? Remark { get; set; }
    }

    private static string? RmrStdParam(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim().ToUpperInvariant();

    private string RmrUser() => Request.Headers.TryGetValue("WAUserCode", out var h) && !string.IsNullOrWhiteSpace(h) ? h.ToString().Trim() : "port";

    // Req_MemberRegister_CheckDB(codeSys, FlagExist "1", statusList) — so khớp trạng thái kiểu Contains của nguồn.
    private async Task<(Req_MemberRegister? Row, IActionResult? Error)> RmrCheckDbAsync(string? codeSys, string statusList)
    {
        var p = PortQuery.Equal(codeSys ?? "");
        var r = await _db.Set<Req_MemberRegister>().FirstOrDefaultAsync(x => EF.Functions.Like((x.ReqMemberRegisterCodeSys ?? "").ToLower(), p, PortQuery.Esc));
        if (r == null)
        {
            return (null, PortResult.Fail("Req_MemberRegister_CheckDB_MemberRegisterNotFound", "Check.ReqMemberRegisterCodeSys", codeSys));
        }
        if (statusList.Length > 0 && !statusList.Contains(r.ReqMemberRegisterStatus ?? ""))
        {
            return (null, PortResult.Fail("Req_MemberRegister_CheckDB_ReqMemberRegisterStatusNotMatched",
                "Check.ReqMemberRegisterCodeSys", codeSys, "Check.strReqMemberRegisterStatusListToCheck", statusList, "DB.ReqMemberRegisterStatus", r.ReqMemberRegisterStatus));
        }
        return (r, null);
    }

    // Lưu Req_MemberRegisterAttachFile: chỉ INSERT (không xoá bản cũ), FileName giữ nguyên giá trị client gửi (nguồn tính strFileName nhưng không gán).
    private void RmrInsertAttach(string codeSys, List<Req_MemberRegisterAttachFile>? files, string now, string user)
    {
        foreach (var f in files ?? new List<Req_MemberRegisterAttachFile>())
        {
            _db.Set<Req_MemberRegisterAttachFile>().Add(new Req_MemberRegisterAttachFile
            {
                ReqMemberRegisterCodeSys = codeSys, Idx = f.Idx, NetworkID = "1001", OrgID = "1001",
                FileType = f.FileType, FileName = f.FileName, FilePath = f.FilePath, LogLUDTimeUTC = now, LogLUBy = user
            });
        }
    }

    private async Task<IActionResult> RmrChangeAsync(string? codeSysRaw, string? remarkRaw, List<Req_MemberRegisterAttachFile>? files,
        string action, string statusList, string newStatus, bool remarkRequired)
    {
        string? codeSys = RmrStdParam(codeSysRaw);
        string strRemark = (remarkRaw ?? "").Trim();
        string user = RmrUser();
        string now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var (r, err) = await RmrCheckDbAsync(codeSys, statusList);
            if (err != null) { await tx.RollbackAsync(); return err; }
            if (remarkRequired && string.IsNullOrEmpty(strRemark))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Req_MemberRegister_" + action + "_InvalidRemark", "Check.strRemark", strRemark);
            }
            if (action == "Appr")
            {
                // Req_MemberRegister_ApprX: không được có hội viên APPROVE trùng biển số/số khung.
                var pCar = PortQuery.Equal((r!.CarNo ?? "").Trim());
                var pVin = PortQuery.Equal((r.VIN ?? "").Trim());
                var dup = await _db.Set<CrdMember>().AsNoTracking()
                    .FirstOrDefaultAsync(t => (EF.Functions.Like((t.CarNo ?? "").ToLower(), pCar, PortQuery.Esc) || EF.Functions.Like((t.VIN ?? "").ToLower(), pVin, PortQuery.Esc))
                                           && t.MemberStatus == "APPROVE");
                if (dup != null)
                {
                    await tx.RollbackAsync();
                    return PortResult.Fail("Req_MemberRegister_Appr_InvalidMemberExist",
                        "Check.strReqMemberRegisterCodeSys", codeSys, "Check.MemberNo.DB", dup.MemberNo, "Check.CarNo.DB", r.CarNo?.Trim(), "Check.VIN.DB", r.VIN?.Trim());
                }
            }

            r!.LUDTimeUTC = now; r.LUBy = user; r.LogLUDTimeUTC = now; r.LogLUBy = user;
            r.Remark = strRemark;
            r.ReqMemberRegisterStatus = newStatus;
            if (newStatus == "APPROVE") { r.ApproveDTimeUTC = now; r.ApproveBy = user; }
            else if (newStatus == "CANCEL") { r.CancelDTimeUTC = now; r.CancelBy = user; }
            else { r.RejectDTimeUTC = now; r.RejectBy = user; }
            RmrInsertAttach(r.ReqMemberRegisterCodeSys, files, now, user);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return Ok(new { Success = true, Data = (object?)null });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return PortResult.Fail("Req_MemberRegister_" + action, "Exception", ex.Message);
        }
    }

    [HttpPost("approve")]
    public Task<IActionResult> Approve([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Req_MemberRegisterStatusReq? req)
        => req?.Req_MemberRegister == null
            ? Task.FromResult<IActionResult>(PortResult.Fail("Req_MemberRegister_Appr", "Check.strJson", null))
            : RmrChangeAsync(req.Req_MemberRegister.ReqMemberRegisterCodeSys, req.Req_MemberRegister.Remark, req.Lst_Req_MemberRegisterAttachFile, "Appr", "PENDING", "APPROVE", false);

    [HttpPost("approveold")]
    public Task<IActionResult> ApproveOld([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Req_MemberRegisterApproveOldReq? req)
        => string.IsNullOrEmpty(req?.ReqMemberRegisterCodeSys)
            ? Task.FromResult<IActionResult>(PortResult.Fail("invalid_ReqMemberRegisterCodeSys", "Check.ReqMemberRegisterCodeSys", req?.ReqMemberRegisterCodeSys))
            : RmrChangeAsync(req.ReqMemberRegisterCodeSys, req.Remark, null, "Appr", "PENDING", "APPROVE", false);

    [HttpPost("cancel")]
    public Task<IActionResult> Cancel([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Req_MemberRegisterStatusReq? req)
        => req?.Req_MemberRegister == null
            ? Task.FromResult<IActionResult>(PortResult.Fail("Req_MemberRegister_Cancel", "Check.strJson", null))
            : RmrChangeAsync(req.Req_MemberRegister.ReqMemberRegisterCodeSys, req.Req_MemberRegister.Remark, req.Lst_Req_MemberRegisterAttachFile, "Cancel", "PENDING", "CANCEL", true);

    [HttpPost("reject")]
    public Task<IActionResult> Reject([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Req_MemberRegisterStatusReq? req)
        => req?.Req_MemberRegister == null
            ? Task.FromResult<IActionResult>(PortResult.Fail("Req_MemberRegister_Reject", "Check.strJson", null))
            : RmrChangeAsync(req.Req_MemberRegister.ReqMemberRegisterCodeSys, req.Req_MemberRegister.Remark, req.Lst_Req_MemberRegisterAttachFile, "Reject", "APPROVE", "REJECT", true);
}
