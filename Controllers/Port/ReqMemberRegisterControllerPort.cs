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
// PORT 1:1 — ReqMemberRegisterController/GetByReqMemberRegisterCodeSys (#274).
//
// Nguồn tầng Gate:
//   ReqMemberRegisterController.cs:152-253 GetByReqMemberRegisterCodeSys
//   ReqMemberRegisterController.cs:468-479 strWhereClause_Req_MemberRegister_By_ReqMemberRegisterCodeSys
// Nguồn tầng API:
//   ReqMemberRegisterController.cs:21-91  WA_Req_MemberRegister_Get
// Nguồn tầng Biz:
//   Member.cs:222-351                     WAS_Req_MemberRegister_Get
//   Member.cs:354-513                     Req_MemberRegister_Get
//   Member.cs:515-760                     Req_MemberRegister_GetX
//
// GIỮ ĐÚNG NGUỒN:
//   - Endpoint POST: /api/reqmemberregister/getbyreqmemberregistercodesys
//   - Nhận ReqMemberRegisterCodeSys qua body JSON
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

public class Req_MemberRegisterGetResult
{
    public Req_MemberRegister? Req_MemberRegister { get; set; }
    public List<Req_MemberRegisterIDAttachFile>? Lst_Req_MemberRegisterIDAttachFile { get; set; }
    public List<Req_MemberRegisterVehicleAttachFile>? Lst_Req_MemberRegisterVehicleAttachFile { get; set; }
    public List<Req_MemberRegisterAttachFile>? Lst_Req_MemberRegisterAttachFile { get; set; }
}

[ApiController]
[Route("api/reqmemberregister")]
public class ReqMemberRegisterControllerPort : ControllerBase
{
    private readonly AppDbContext _db;

    public ReqMemberRegisterControllerPort(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost("getbyreqmemberregistercodesys")]
    public async Task<IActionResult> GetByReqMemberRegisterCodeSys(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Req_MemberRegister_GetByReqMemberRegisterCodeSysReq? req)
    {
        double utcOffset = 7;
        var tzHeader = Request.Headers["TimeZone"].FirstOrDefault();
        if (!string.IsNullOrEmpty(tzHeader) && double.TryParse(tzHeader, NumberStyles.Any, CultureInfo.InvariantCulture, out var tzVal))
        {
            utcOffset = tzVal;
        }

        var codeSys = req?.ReqMemberRegisterCodeSys?.Trim();
        if (string.IsNullOrEmpty(codeSys))
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

        var reg = await _db.Set<Req_MemberRegister>()
            .Where(x => EF.Functions.Like(x.ReqMemberRegisterCodeSys.ToLower(), PortQuery.Equal(codeSys), PortQuery.Esc))
            .FirstOrDefaultAsync();

        if (reg == null)
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

        // Chuyển đổi thời gian UTC sang Local Time theo utcOffset theo nguồn Gate ReqMemberRegisterController.cs:211-218
        if (!string.IsNullOrEmpty(reg.ApproveDTimeUTC) && DateTime.TryParse(reg.ApproveDTimeUTC, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var approveDt))
        {
            reg.ApproveDTimeUTC = approveDt.AddHours(utcOffset).ToString("yyyy-MM-dd HH:mm:ss");
        }
        if (!string.IsNullOrEmpty(reg.CreateDTimeUTC) && DateTime.TryParse(reg.CreateDTimeUTC, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var createDt))
        {
            reg.CreateDTimeUTC = createDt.AddHours(utcOffset).ToString("yyyy-MM-dd HH:mm:ss");
        }

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
}
