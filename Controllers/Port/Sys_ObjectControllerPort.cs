using System.IO;
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
// PORT 1:1 — màn Sys_ObjectController.GetAllMenuActive (#317), GetAllMenuButtonActive (#318), GetFunctionActive (#319), GetFunctionByObjectCode (#320), GetAllFunctionBy_ObjectCode (#321) & Get (#322)
// Nguồn:
//   Sys_ObjectController.cs:29-98 (GetAllMenuActive)
//   Sys_ObjectController.cs:101-172 (GetAllMenuButtonActive)
//   Sys_ObjectController.cs:175-243 (GetFunctionActive)
//   Sys_ObjectController.cs:246-302 (GetFunctionByObjectCode)
//   Sys_ObjectController.cs:305-406 (GetAllFunctionBy_ObjectCode)
//   Sys_ObjectController.cs:408-515 (Get)
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
    private readonly Microsoft.AspNetCore.Hosting.IWebHostEnvironment _env;

    public Sys_ObjectControllerPort(AppDbContext db, Microsoft.AspNetCore.Hosting.IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
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

    /// <summary>
    /// Port 1:1 <c>Sys_ObjectController.GetFunctionActive</c>.
    /// Lọc tĩnh <c>ObjectType == "FUNC"</c> và <c>FlagActive == "1"</c>.
    /// Sắp xếp theo <c>ObjectCode asc</c>.
    /// Không nhận body phức tạp. Trả về mảng dữ liệu hoặc <c>null</c> nếu rỗng.
    /// </summary>
    [HttpPost("getfunctionactive")]
    public async Task<IActionResult> GetFunctionActive([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] object? req)
    {
        // GATE Sys_ObjectController.cs:197-201: ObjectType = "FUNC", FlagActive = Client_Flag.Active ("1")
        // strWhereClause_SysObject dùng toán tử "=" cho cả hai
        var q = _db.Set<Sys_Object>().AsNoTracking()
            .Where(x => x.ObjectType == "FUNC" && x.FlagActive == "1")
            .OrderBy(x => x.ObjectCode); // System.cs:14964: order by so.ObjectCode asc

        var rows = await q.ToListAsync();

        // Nguồn Sys_ObjectController.cs:202, 225-228: listSys_Object khởi tạo null, chỉ gán khi Lst_Sys_Object != null && Count > 0
        return Ok(new { Success = true, Data = rows.Count > 0 ? rows : null });
    }

    /// <summary>
    /// Port 1:1 <c>Sys_ObjectController.GetFunctionByObjectCode</c> (#320, gate :246-302).
    /// Danh sách FUNC gán vào Menu lấy từ tệp cấu hình <c>App_Data/SysObjectSetting.config</c> (BaseController.cs:654-669 GetSysObjectSetting):
    /// tệp không tồn tại -> setting null -> danh sách rỗng. So sánh ObjectCode không phân biệt hoa/thường (:274), mỗi FunctionCode được Trim (:280).
    /// Data = List&lt;string&gt; (rỗng -> mảng rỗng [] chứ không phải null, gate :288-289). Lỗi bất ngờ do PortExceptionFilter trả 400.
    /// </summary>
    [HttpPost("getfunctionbyobjectcode")]
    public IActionResult GetFunctionByObjectCode([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] Sys_Object_GetFunctionByObjectCodeReq? req)
    {
        var objectCode = req?.ObjectCode;
        var listObjectCode = new List<string>();

        var setting = GetSysObjectSetting();
        if (setting != null && setting.List != null)
        {
            foreach (var o in setting.List)
            {
                if (o.ObjectCode!.Equals(objectCode, StringComparison.InvariantCultureIgnoreCase))   // nguồn :274 (ObjectCode null trong tệp -> NullReference như nguồn)
                {
                    if (o.FunctionCodes != null && o.FunctionCodes.Count > 0)
                    {
                        foreach (var it in o.FunctionCodes)
                        {
                            listObjectCode.Add(it.Trim());
                        }
                    }
                }
            }
        }

        return Ok(new { Success = true, Data = listObjectCode });
    }

    /// <summary>
    /// Port 1:1 <c>Sys_ObjectController.GetAllFunctionBy_ObjectCode</c> (#321, gate :305-406).
    /// Lấy danh sách hàm trong hệ thống với ObjectType = "FUNC" và FlagActive = "1" (Sys_ObjectController.cs:331-332),
    /// đồng thời đọc cấu hình <c>SysObjectSetting.config</c> để lấy danh sách FunctionCodes gán cho <c>ObjectCode</c> truyền vào.
    /// Gán trường <c>FlagSelected</c> = "1" nếu ObjectCode nằm trong danh sách FunctionCodes của cấu hình, ngược lại "0" (so sánh không phân biệt hoa/thường).
    /// Trả về đối tượng <c>JsonResultEntry&lt;object&gt;</c> tiêu chuẩn với <c>Success = true</c> và <c>Data</c> chứa các thuộc tính:
    /// ObjectCode, ObjectName, ServiceCode, ObjectType, FlagExecModal, FlagActive, FlagSelected.
    /// </summary>
    [HttpPost("getallfunctionby_objectcode")]
    public async Task<IActionResult> GetAllFunctionBy_ObjectCode([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] Sys_Object_GetAllFunctionBy_ObjectCodeReq? req)
    {
        var objectCode = req?.ObjectCode;
        var objJsonResultEntry = new {
            Success = true,
            Data = (object?)null
        };

        // Nguồn Sys_ObjectController.cs:329-333: ObjectType = "FUNC", FlagActive = Client_Flag.Active ("1")
        var q = _db.Set<Sys_Object>().AsNoTracking()
            .Where(x => x.ObjectType == "FUNC" && x.FlagActive == "1")
            .OrderBy(x => x.ObjectCode);

        var listSys_Object = await q.ToListAsync();

        // Nguồn Sys_ObjectController.cs:362-378: Đọc danh sách FunctionCodes từ SysObjectSetting.config
        var listObjectCode = new List<string>();
        var setting = GetSysObjectSetting();
        if (setting != null && setting.List != null)
        {
            foreach (var o in setting.List)
            {
                if (o.ObjectCode != null && o.ObjectCode.Equals(objectCode, StringComparison.InvariantCultureIgnoreCase))
                {
                    if (o.FunctionCodes != null && o.FunctionCodes.Count > 0)
                    {
                        foreach (var it in o.FunctionCodes)
                        {
                            listObjectCode.Add(it.Trim());
                        }
                    }
                }
            }
        }

        // Nguồn Sys_ObjectController.cs:380-391: Gán FlagSelected = "1" nếu có trong listObjectCode, ngược lại "0"
        if (listSys_Object != null && listSys_Object.Count > 0)
        {
            var dataList = listSys_Object.Select(item => new {
                ObjectCode = item.ObjectCode,
                ObjectName = item.ObjectName,
                ServiceCode = item.ServiceCode,
                ObjectType = item.ObjectType,
                FlagExecModal = item.FlagExecModal,
                FlagActive = item.FlagActive,
                FlagSelected = listObjectCode.Where(it => it.Equals(item.ObjectCode)).FirstOrDefault() != null ? "1" : "0"   // nguồn :385-ish: it.Equals(item.ObjectCode) PHÂN BIỆT hoa/thường (khác so sánh ObjectCode của setting),
            }).ToList();

            // Gán Data cho JsonResultEntry
            return Ok(new {
                Success = true,
                Data = dataList
            });
        }

        return Ok(new {
            Success = true,
            Data = (object?)null
        });
    }

    /// <summary>
    /// Port 1:1 <c>Sys_ObjectController.Get</c> (#322, gate :408-515).
    /// Lấy danh sách Sys_Object với điều kiện lọc mặc định <c>FlagActive = "1"</c> (Sys_ObjectController.cs:435)
    /// và hỗ trợ nhận tham số lọc <c>ObjectCode</c> qua body nếu được truyền vào (strWhereClause_Sys_Object, gate :582-593).
    /// Trả về <c>JsonResultEntry&lt;object&gt;</c> tiêu chuẩn với <c>Success = true</c> và <c>Data</c> chứa danh sách hoặc <c>null</c> nếu rỗng.
    /// </summary>
        // Port 1:1 Sys_ObjectController.Get (#322, gate :408-504): KHÔNG nhận tham số. Gate gọi strWhereClause_Sys_Object({OrgID = session, FlagActive = "1"}) nhưng hàm lọc (:582-593)
    // CHỈ xét ObjectCode (không được gán) -> where rỗng: trả TẤT CẢ Sys_Object (kể cả FlagActive = "0"), sắp theo ObjectCode asc (System.cs:14964). Rỗng -> null (:455-457).
    [HttpPost("get")]
    public async Task<IActionResult> Get()
    {
        var rows = await _db.Set<Sys_Object>().AsNoTracking().OrderBy(x => x.ObjectCode).ToListAsync();
        return Ok(new { Success = true, Data = rows.Count > 0 ? rows : null });
    }

    /// <summary>
    /// BaseController.cs:654-669 GetSysObjectSetting: đọc <c>~/App_Data/SysObjectSetting.config</c> (DataContract XML: SysObjectSetting/List/SysObjectFunction{ObjectCode, FunctionCodes/string});
    /// không có tệp -> null. Tệp mẫu của nguồn được đặt tại App_Data/SysObjectSetting.config của dự án.
    /// </summary>
    private SysObjectSettingModel? GetSysObjectSetting()
    {
        var filePath = Path.Combine(_env.ContentRootPath, "App_Data", "SysObjectSetting.config");
        if (!System.IO.File.Exists(filePath)) return null;

        var doc = System.Xml.Linq.XDocument.Load(filePath);
        var items = new List<SysObjectSettingItem>();
        foreach (var e in doc.Descendants().Where(x => x.Name.LocalName == "SysObjectFunction"))
        {
            var code = e.Elements().FirstOrDefault(x => x.Name.LocalName == "ObjectCode")?.Value;
            var funcs = e.Elements().FirstOrDefault(x => x.Name.LocalName == "FunctionCodes")?.Elements().Select(x => x.Value).ToList();
            items.Add(new SysObjectSettingItem { ObjectCode = code, FunctionCodes = funcs });
        }
        return new SysObjectSettingModel { List = items };
    }
}

public class Sys_Object_GetAllMenuButtonActiveReq
{
    public string? ObjectType { get; set; }
    public string? ObjectName { get; set; }
}

public class Sys_Object_GetFunctionByObjectCodeReq
{
    public string? ObjectCode { get; set; }
}

public class Sys_Object_GetAllFunctionBy_ObjectCodeReq
{
    public string? ObjectCode { get; set; }
}

public class Sys_Object_GetReq
{
    public string? ObjectCode { get; set; }
}

public class SysObjectSettingModel
{
    public List<SysObjectSettingItem>? List { get; set; }
}

public class SysObjectSettingItem
{
    public string? ObjectCode { get; set; }
    public List<string>? FunctionCodes { get; set; }
}
