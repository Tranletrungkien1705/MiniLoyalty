using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MiniLoyalty.Data;
using MiniLoyalty.Models.Port;

namespace MiniLoyalty.Controllers.Port;

/// <summary>Body = RQ_Sys_UserInGroup (gate Sys_UserInGroupController.cs:27-112): Sys_Group.GroupCode + Lst_Sys_UserInGroup[{UserCode}].</summary>
public class SysUserInGroupSaveReq
{
    public SysUserInGroupGroupRef? Sys_Group { get; set; }
    public List<SysUserInGroupItemReq>? Lst_Sys_UserInGroup { get; set; }
}

public class SysUserInGroupGroupRef
{
    public string? GroupCode { get; set; }
}

public class SysUserInGroupItemReq
{
    public string? UserCode { get; set; }
}

[ApiController]
[Route("api/sys_useringroup")]
[Produces("application/json")]
public class Sys_UserInGroupControllerPort : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public Sys_UserInGroupControllerPort(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    /// <summary>
    /// Port 1:1 Sys_UserInGroupController.Save (#337, gate :27-112) -> WA_Sys_UserInGroup_Save -> Sys_UserInGroup_Save_New20191102 (zTemp.cs:1255-1450).
    /// Body rỗng -> ServiceException("strJson null") -> mã mặc định Sys_UserInGroup_Save. Biz: Sys_Group_CheckDB(GroupCode, "1") -> bảng Lst_Sys_UserInGroup phải có
    /// (Sys_UserInGroup_Save_InputTblDtlNotFound) -> StdParam(UserCode) -> XOÁ HẾT user của nhóm rồi chèn lại (nhóm x danh sách user, :1398-1431).
    /// KHÔNG kiểm tra từng UserCode có tồn tại. Nguồn gán t_sg.MST = MST của người dùng đang đăng nhập (Sys_User_CheckDB(strWAUserCode), :1337-1347); đích chưa có session
    /// -> dùng MST của nhóm (GAP). NetworkID = Biz_NetworkID, LogLUDTimeUTC = bây giờ, LogLUBy = "port".
    /// </summary>
    [HttpPost("save")]
    public async Task<IActionResult> Save([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] SysUserInGroupSaveReq? req)
    {
        if (req == null)
        {
            return PortResult.Fail("Sys_UserInGroup_Save", "Check.strJson", null);
        }

        var groupCodeRaw = req.Sys_Group!.GroupCode;   // Sys_Group null -> NullReference như nguồn

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // Sys_Group_CheckDB(GroupCode, "1") (:1329-1334)
            var strGroupCode = string.IsNullOrWhiteSpace(groupCodeRaw) ? null : groupCodeRaw.Trim().ToUpper();
            var dbGroup = await _db.Set<Sys_Group>().AsNoTracking()
                .FirstOrDefaultAsync(x => EF.Functions.Like((x.GroupCode ?? "").ToLower(), PortQuery.Equal(strGroupCode ?? ""), PortQuery.Esc));
            if (dbGroup == null)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Sys_Group_CheckDB_GroupCodeNotFound", "Check.GroupCodeNotFound", strGroupCode);
            }

            // bảng chi tiết phải có (:1368-1380)
            if (req.Lst_Sys_UserInGroup == null)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Sys_UserInGroup_Save_InputTblDtlNotFound", "Check.TableName", "Sys_UserInGroup");
            }

            // StdParam(UserCode) (:1382-1385)
            var userCodes = req.Lst_Sys_UserInGroup.Select(u => u.UserCode!.Trim().ToUpper()).ToList();

            // Clear All + Insert All (:1400-1431)
            var existing = await _db.Set<Sys_UserInGroup>().Where(x => x.GroupCode == dbGroup.GroupCode).ToListAsync();
            _db.Set<Sys_UserInGroup>().RemoveRange(existing);
            await _db.SaveChangesAsync();

            var now = DateTime.UtcNow;
            var networkId = _config["Biz_NetworkID"] ?? "1001";
            foreach (var userCode in userCodes)
            {
                _db.Set<Sys_UserInGroup>().Add(new Sys_UserInGroup
                {
                    GroupCode = dbGroup.GroupCode,
                    UserCode = userCode,
                    NetworkID = networkId,
                    MST = dbGroup.MST,
                    LogLUDTimeUTC = now,
                    LogLUBy = "port"
                });
            }
            await _db.SaveChangesAsync();

            await tx.CommitAsync();
            return Ok(new { Success = true, Data = (object?)null });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return PortResult.Fail("Sys_UserInGroup_Save", "Exception", ex.Message);
        }
    }
}