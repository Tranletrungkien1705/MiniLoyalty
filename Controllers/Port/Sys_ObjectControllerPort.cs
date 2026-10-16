using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniLoyalty.Data;
using MiniLoyalty.Models.Port;

namespace MiniLoyalty.Controllers.Port;

// ============================================================================================
// PORT 1:1 — màn Sys_ObjectController.GetAllMenuActive (#317) & GetAllMenuButtonActive (#318)
// Nguồn:
//   Sys_ObjectController.cs:29-98 (GetAllMenuActive)
//   Sys_ObjectController.cs:101-172 (GetAllMenuButtonActive)
//   SysObjectController.cs:20-91 (WA_Sys_Object_Get)
//   System.cs:15325-15424 (WAS_Sys_Object_Get)
//   System.cs:14892-15098 (Sys_Object_Get)
// ============================================================================================

[ApiController]
[Route("api/sys_object")]
[Produces("application/json")]
public class Sys_ObjectControllerPort : ControllerBase
{
    private readonly AppDbContext _db;

    public Sys_ObjectControllerPort(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Port 1:1 <c>Sys_ObjectController.GetAllMenuActive</c>.
    /// Lọc tĩnh <c>ObjectType == "MENU"</c> và <c>FlagActive == "1"</c>.
    /// Sắp xếp theo <c>ObjectCode asc</c>.
    /// Không nhận body. Trả về mảng dữ liệu hoặc <c>null</c> nếu rỗng.
    /// </summary>
    [HttpPost("getallmenuactive")]
    public async Task<IActionResult> GetAllMenuActive([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] object? req)
    {
        // GATE: sysObject.ObjectType = "MENU", FlagActive = "1"
        // strWhereClause_SysObject dùng toán tử "=" cho cả hai
        var q = _db.Set<Sys_Object>().AsNoTracking()
            .Where(x => x.ObjectType == "MENU" && x.FlagActive == "1")
            .OrderBy(x => x.ObjectCode);

        var rows = await q.ToListAsync();

        // Nguồn Sys_ObjectController.cs:56, 80-85: listSys_Object khởi tạo = null, nếu query có dữ liệu mới gán
        return Ok(new { Success = true, Data = rows.Count > 0 ? rows : null });
    }

    /// <summary>
    /// Port 1:1 <c>Sys_ObjectController.GetAllMenuButtonActive</c>.
    /// Lọc tĩnh <c>ObjectType IN ('MENU', 'BUTTON')</c> và <c>FlagActive == "1"</c>.
    /// Có lọc theo ObjectName (Like).
    /// </summary>
    [HttpPost("getallmenubuttonactive")]
    public async Task<IActionResult> GetAllMenuButtonActive([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] Sys_Object_GetAllMenuButtonActiveReq? req)
    {
        // Nguồn Sys_ObjectController.cs:125, 127: Ghi đè cứng ObjectType = "'MENU','BUTTON'", FlagActive = "1"
        var allowedTypes = new[] { "MENU", "BUTTON" };
        var q = _db.Set<Sys_Object>().AsNoTracking()
            .Where(x => allowedTypes.Contains(x.ObjectType))
            .Where(x => x.FlagActive == "1");

        // Nguồn Sys_ObjectController.cs:554-557: Lọc theo ObjectName (like %...%)
        if (req != null && !string.IsNullOrWhiteSpace(req.ObjectName))
        {
            var keyword = req.ObjectName.Trim();
            q = q.Where(x => EF.Functions.Like((x.ObjectName ?? "").ToLower(), PortQuery.Contains(keyword), PortQuery.Esc));
        }

        // Nguồn System.cs:14964: order by so.ObjectCode asc
        q = q.OrderBy(x => x.ObjectCode);

        var rows = await q.ToListAsync();

        // Nguồn Sys_ObjectController.cs:129, 154-159: Khởi tạo null, chỉ gán nếu > 0 dòng
        return Ok(new { Success = true, Data = rows.Count > 0 ? rows : null });
    }
}

public class Sys_Object_GetAllMenuButtonActiveReq
{
    public string? ObjectType { get; set; }
    public string? ObjectName { get; set; }
}
