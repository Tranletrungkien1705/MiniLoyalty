using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniLoyalty.Data;
using MiniLoyalty.Models.Port;

namespace MiniLoyalty.Controllers.Port;

/// <summary>
/// Body của Sys_AccessController.SaveMenuButtonInGroup = RQ_Sys_Access (gate :474-576): Sys_Group.GroupCode + Lst_Sys_Access[{ObjectCode, so_ObjectType}].
/// </summary>
public class SysAccessSaveMenuButtonInGroupReq
{
    public Sys_Group_Input? Sys_Group { get; set; }
    public List<Sys_Access_Input>? Lst_Sys_Access { get; set; }
}

public class Sys_Group_Input
{
    public string? GroupCode { get; set; }
}

public class Sys_Access_Input
{
    public string? ObjectCode { get; set; }
    public string? so_ObjectType { get; set; }
}

/// <summary>
/// DTO tham số tìm kiếm cho Sys_AccessController.Search (gate Sys_AccessController.cs:27-109).
/// Nhận OrgID, KeyWord, FlagActive, Ft_PageIndex, Ft_PageSize qua JSON body (Bài học A.14).
/// </summary>
public class SysAccessSearchReq
{
    public string? OrgID { get; set; }
    public string? KeyWord { get; set; }
    public string? FlagActive { get; set; }
    public int Ft_PageIndex { get; set; } = 0;
    public int Ft_PageSize { get; set; } = 10;
}

/// <summary>
/// DTO tham số cho Sys_AccessController.GetByGroupCode (gate Sys_AccessController.cs:220-285).
/// Nhận GroupCode qua JSON body (Bài học A.14).
/// </summary>
public class SysAccessGetByGroupCodeReq
{
    public string? GroupCode { get; set; }
}

/// <summary>
/// DTO tham số cho Sys_AccessController.GetAllFunctionBy_ObjectCode (gate Sys_AccessController.cs:288-402).
/// Nhận GroupCode qua JSON body (Bài học A.14).
/// </summary>
public class SysAccessGetAllFunctionByObjectCodeReq
{
    public string? GroupCode { get; set; }
    public string? OrgID { get; set; }
    public string? NetworkID { get; set; }
    public string? Tid { get; set; }
}

/// <summary>
/// DTO tham số cho Sys_AccessController.GetByObjectCode (gate Sys_AccessController.cs:405-472).
/// Nhận ObjectCode qua JSON body (Bài học A.14).
/// </summary>
public class SysAccessGetByObjectCodeReq
{
    public string? ObjectCode { get; set; }
}

/// <summary>
/// Port 1:1 Sys_AccessController — màn phân quyền truy cập hệ thống (#300 Search, #301 Get, #302 GetByGroupCode, #303 GetAllFunctionBy_ObjectCode, #304 GetByObjectCode, #305 SaveMenuButtonInGroup).
/// Nguồn Gate: D:\idocNet\2022.7.Loyalty\13.ClientGate\V10\idn.Loyalty.WebAPI\Controllers\Sys_AccessController.cs:27-109, 110-217, 220-285, 288-402, 405-472, 474-576
/// Nguồn API: D:\idocNet\2022.7.Loyalty\11.BackEnd\V10\idn.Loyalty.Biz.Web\Controllers\SysAccessController.cs:20-91 (WA_Sys_Access_Get), 94-165 (WA_Sys_Access_Save)
/// Nguồn BIZ: D:\idocNet\2022.7.Loyalty\11.BackEnd\V10\idn.Loyalty.Biz\System.cs:757-850 (WAS_Sys_Access_Get), 1284-1366 (WAS_Sys_Access_Save), 852-1055 (Sys_Access_Save)
///            D:\idocNet\2022.7.Loyalty\11.BackEnd\V10\idn.Loyalty.Biz\System.cs:290-512 (Sys_Access_Get)
/// </summary>
[ApiController]
[Route("api/sys_access")]
[Produces("application/json")]
public class Sys_AccessControllerPort : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly Microsoft.AspNetCore.Hosting.IWebHostEnvironment? _env;

    public Sys_AccessControllerPort(AppDbContext db, Microsoft.AspNetCore.Hosting.IWebHostEnvironment? env = null)
    {
        _db = db;
        _env = env;
    }

    /// <summary>
    /// Tìm kiếm phân quyền truy cập hệ thống (Sys_AccessController.cs:27-109).
    /// </summary>
    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] SysAccessSearchReq? req)
    {
        var r = req ?? new SysAccessSearchReq();
        var pageIndex = r.Ft_PageIndex;
        var pageSize = r.Ft_PageSize;

        // Tính recordStart = PageIndex * PageSize (gate :73)
        var start = (long)pageIndex * pageSize;
        var (itemCount, rows) = await GetXAsync(r.OrgID, r.KeyWord, r.FlagActive, groupCode: null, start, pageSize);

        // Tính pageCount theo gate :87-91. Nếu pageSize = 0 thì quy ước chia 10 hoặc trả 0 khi rỗng.
        // Theo bài học A.7: Khi Ft_PageSize = 0 thì DataList rỗng, PageCount = 0, ItemCount giữ nguyên tổng số.
        var ps = pageSize == 0 ? 10 : pageSize;
        var pageCount = rows.Count == 0 ? 0 : (itemCount % ps == 0 ? itemCount / ps : itemCount / ps + 1);

        return Ok(new { Success = true, Data = new PageInfo<object>(rows, itemCount, pageCount, pageIndex, pageSize) });
    }

    /// <summary>
    /// Port 1:1 Sys_AccessController.Get (gate :110-217) -> WA_Sys_Access_Get. Không nhận tham số từ client: Ft_RecordStart = "0", Ft_RecordCount = "123456000" (BaseController.cs:30-31),
    /// where = strWhereClause_Sys_Access(OrgID = session, FlagActive = "1") nhưng hàm lọc chỉ xét GroupCode/ObjectCode (không được gán) -> không lọc gì (xem GetXAsync).
    /// Data = danh sách (có thêm các cột so_* từ left join Sys_Object), rỗng -> null (gate :157-159, :197-204).
    /// </summary>
    [HttpPost("get")]
    public async Task<IActionResult> Get()
    {
        var (_, rows) = await GetXAsync(null, null, null, groupCode: null, 0, 123456000);
        return Ok(new { Success = true, Data = rows.Count > 0 ? rows : null });
    }

    /// <summary>
    /// Port 1:1 Sys_AccessController.GetByGroupCode (gate Sys_AccessController.cs:220-285).
    /// GroupCode rỗng/null: chặn ngay từ gate, trả Success = true, Data = null (gate :242, :271-272).
    /// </summary>
    [HttpPost("getbygroupcode")]
    public async Task<IActionResult> GetByGroupCode([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] SysAccessGetByGroupCodeReq? req)
    {
        var groupCode = req?.GroupCode;
        if (string.IsNullOrEmpty(groupCode))
        {
            return Ok(new { Success = true, Data = (object?)null });
        }

        var (_, rows) = await GetXAsync(null, null, null, groupCode, 0, 123456000);
        return Ok(new { Success = true, Data = rows.Count > 0 ? rows : null });
    }

    /// <summary>
    /// Port 1:1 Sys_AccessController.GetByObjectCode (gate Sys_AccessController.cs:405-472).
    /// ObjectCode rỗng/null: bỏ qua gọi Biz, trả Success = true, Data = null (gate :427, :458-459).
    /// </summary>
    [HttpPost("getbyobjectcode")]
    public async Task<IActionResult> GetByObjectCode([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] SysAccessGetByObjectCodeReq? req)
    {
        var objectCode = req?.ObjectCode;
        if (string.IsNullOrEmpty(objectCode))
        {
            return Ok(new { Success = true, Data = (object?)null });
        }

        var (_, rows) = await GetXAsync(null, null, null, groupCode: null, 0, 123456000, objectCode: objectCode);
        return Ok(new { Success = true, Data = rows.Count > 0 ? rows : null });
    }

    /// <summary>
    /// Port 1:1 Sys_AccessController.GetAllFunctionBy_ObjectCode (gate Sys_AccessController.cs:288-402).
    /// Lấy danh sách Menu và Button trong hệ thống (Sys_Object: ObjectType IN ('MENU','BUTTON') và FlagActive = '1'),
    /// sắp xếp theo ObjectCode asc (System.cs:14964).
    /// Nếu GroupCode được truyền vào (!IsNullOrEmpty), lấy danh sách phân quyền Sys_Access theo GroupCode.
    /// Gán FlagSelected: "1" nếu ObjectCode có trong phân quyền của nhóm, ngược lại "0".
    /// Nếu GroupCode rỗng/null, bỏ qua lấy Sys_Access, toàn bộ FlagSelected gán "0" (gate :346-374, :385).
    /// Nếu danh sách Sys_Object rỗng, Data = null (gate :376).
    /// </summary>
    [HttpPost("getallfunctionby_objectcode")]
    public async Task<IActionResult> GetAllFunctionBy_ObjectCode([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] SysAccessGetAllFunctionByObjectCodeReq? req)
    {
        // Danh sách Menu - Button trong hệ thống (gate :310-343)
        // ObjectType IN ('MENU', 'BUTTON') và FlagActive = "1", order by ObjectCode asc (System.cs:14964)
        var allowedTypes = new[] { "MENU", "BUTTON" };
        var listSys_Object = await _db.Set<Sys_Object>().AsNoTracking()
            .Where(x => allowedTypes.Contains(x.ObjectType) && x.FlagActive == "1")
            .OrderBy(x => x.ObjectCode)
            .ToListAsync();

        if (listSys_Object.Count == 0)
        {
            return Ok(new { Success = true, Data = (object?)null });
        }

        // Danh sách Menu - Button gán vào nhóm người dùng (gate :344-374)
        HashSet<string>? accessSet = null;
        var groupCode = req?.GroupCode;
        if (!string.IsNullOrEmpty(groupCode))
        {
            var accessObjs = await _db.Set<Sys_Access>().AsNoTracking()
                .Where(x => EF.Functions.Like((x.GroupCode ?? "").ToLower(), PortQuery.Equal(groupCode), PortQuery.Esc))
                .Select(x => x.ObjectCode)
                .ToListAsync();
            // gate :370-373: listSys_Access chỉ khác null khi có >= 1 dòng Sys_Access; rỗng -> vẫn null
            if (accessObjs.Count > 0) accessSet = new HashSet<string>(accessObjs, StringComparer.Ordinal);   // it.ObjectCode.Equals(item.ObjectCode) phân biệt hoa/thường
        }

        // LỖI NGUỒN GIỮ NGUYÊN (gate :385): listSys_Access.Where(...) khi listSys_Access == null (không truyền GroupCode, hoặc nhóm chưa có dòng phân quyền nào)
        // ném NullReferenceException -> catch của gate (AddException) -> Success = false; ở đích PortExceptionFilter trả 400 PortResult.Fail("Sys_Access_GetAllFunctionBy_ObjectCode", "Exception", ex.Message).
        if (accessSet == null) throw new NullReferenceException("Object reference not set to an instance of an object.");

        // Project theo gate :378-386
        var rows = listSys_Object.Select(item => new
        {
            item.ObjectCode,
            item.ObjectName,
            item.ServiceCode,
            item.ObjectType,
            item.FlagExecModal,
            item.FlagActive,
            FlagSelected = accessSet != null && accessSet.Contains(item.ObjectCode) ? "1" : "0",
        }).ToList();

        return Ok(new { Success = true, Data = rows });
    }

    /// <summary>
    /// Thực hiện 2 bước theo đúng mã nguồn gốc BIZ (System.cs:290-512):
    /// Bước 1: Draft (#tbl_Sys_Access_Filter_Draft, System.cs:359-370)
    /// select distinct identity(bigint, 0, 1) MyIdxSeq, sa.GroupCode, sa.ObjectCode
    /// into #tbl_Sys_Access_Filter_Draft from Sys_Access sa where (1=1) ...
    /// order by sa.GroupCode asc, sa.ObjectCode asc
    /// 
    /// LỖI DI SẢN MÃ NGUỒN:
    /// Gate Sys_AccessController.cs:56-61 truyền KeyWord, OrgID, FlagActive vào hàm strWhereClause_Sys_Access (:795-810),
    /// nhưng hàm này lại kiểm tra data.GroupCode và data.ObjectCode (không hề được gán từ gate).
    /// Vì vậy mệnh đề WHERE luôn là (1=1), các tham số OrgID, KeyWord, FlagActive hoàn toàn bị bỏ qua.
    /// 
    /// Phân trang: Áp dụng bài học A.12 và A.7:
    /// - Ft_PageSize = 0 hoặc recordStart &lt; 0: trả danh sách rỗng, PageCount = 0, giữ nguyên ItemCount tổng số.
    /// - Ngược lại: Skip(start).Take(count) chống tràn số int.
    /// 
    /// Bước 2: Select cuối (System.cs:403-419)
    /// select t.MyIdxSeq, sa.*, so.ObjectCode so_ObjectCode, so.ObjectName so_ObjectName,
    /// so.ServiceCode so_ServiceCode, so.ObjectType so_ObjectType, so.FlagExecModal so_FlagExecModal,
    /// so.FlagActive so_FlagActive
    /// from #tbl_Sys_Access_Filter t
    /// inner join Sys_Access sa on t.GroupCode = sa.GroupCode and t.ObjectCode = sa.ObjectCode
    /// left join Sys_Object so on t.ObjectCode = so.ObjectCode
    /// order by t.MyIdxSeq asc
    /// 
    /// - Sys_Object ĐÃ port ở #317 -> left join thật (so_* lấy từ Sys_Object).
    /// - Phân quyền Sys_Access_CheckDeny (System.cs:332) được coi là GAP trong MiniLoyalty.
    /// </summary>
    private async Task<(int itemCount, List<object> rows)> GetXAsync(string? orgId, string? keyWord, string? flagActive, string? groupCode, long recordStart, long recordCount, string? objectCode = null)
    {
        var q = _db.Set<Sys_Access>().AsNoTracking().AsQueryable();

        // Lọc theo GroupCode nếu có (gate Sys_AccessController.cs:244-247, strWhereClause_Sys_Access :795-810)
        if (!string.IsNullOrEmpty(groupCode))
        {
            q = q.Where(x => EF.Functions.Like(x.GroupCode.ToLower(), PortQuery.Equal(groupCode), PortQuery.Esc));
        }

        // Lọc theo ObjectCode nếu có (gate Sys_AccessController.cs:427-434, strWhereClause_Sys_Access :804-807)
        if (!string.IsNullOrEmpty(objectCode))
        {
            q = q.Where(x => EF.Functions.Like(x.ObjectCode.ToLower(), PortQuery.Equal(objectCode), PortQuery.Esc));
        }

        // Nguồn gốc: Where 1=1 (KeyWord, OrgID, FlagActive không được gán vào GroupCode/ObjectCode của Sys_AccessUI)
        // Bước 1: Draft sắp xếp theo sa.GroupCode asc, sa.ObjectCode asc (System.cs:368-369)
        var ordered = q.OrderBy(x => x.GroupCode).ThenBy(x => x.ObjectCode);
        var itemCount = await ordered.CountAsync(); // Summary: Count(0) MyCount (System.cs:373)

        // Phân trang chuẩn theo bài học A.12 và A.7
        var page = (recordCount <= 0 || recordStart < 0)
            ? new List<Sys_Access>()
            : await ordered.Skip((int)Math.Min(recordStart, int.MaxValue)).Take((int)Math.Min(recordCount, int.MaxValue)).ToListAsync();

        // Bước 2: Select cuối - sắp xếp theo t.MyIdxSeq asc (System.cs:418)
        // left join Sys_Object so on t.ObjectCode = so.ObjectCode (System.cs:416-417): Sys_Object đã port ở #317 (PK ObjectCode -> tối đa 1 dòng/khớp)
        var objCodes = page.Select(p => p.ObjectCode).Distinct().ToList();
        var objs = objCodes.Count == 0
            ? new List<Sys_Object>()
            : await _db.Set<Sys_Object>().AsNoTracking().Where(o => objCodes.Contains(o.ObjectCode)).ToListAsync();
        var objMap = new Dictionary<string, Sys_Object>(StringComparer.OrdinalIgnoreCase);
        foreach (var o in objs) objMap[o.ObjectCode] = o;

        var rows = page
            .Select((x, i) =>
            {
                objMap.TryGetValue(x.ObjectCode, out var so);
                return (object)new
                {
                    MyIdxSeq = recordStart + i,
                    x.GroupCode,
                    x.ObjectCode,
                    x.LogLUDTimeUTC,
                    x.LogLUBy,
                    so_ObjectCode = so?.ObjectCode,
                    so_ObjectName = so?.ObjectName,
                    so_ServiceCode = so?.ServiceCode,
                    so_ObjectType = so?.ObjectType,
                    so_FlagExecModal = so?.FlagExecModal,
                    so_FlagActive = so?.FlagActive,
                };
            })
            .ToList();

        return (itemCount, rows);
    }

    /// <summary>
    /// Port 1:1 Sys_AccessController.SaveMenuButtonInGroup (#305, gate :474-576) -> WA_Sys_Access_Save -> Sys_Access_Save (System.cs:856-1033).
    /// Body = RQ_Sys_Access: { Sys_Group: { GroupCode }, Lst_Sys_Access: [ { ObjectCode, so_ObjectType } ] }.
    /// Gate: Lst rỗng/null -> danh sách rỗng (sẽ xoá hết quyền của nhóm); mỗi phần tử so_ObjectType.Equals("MENU"/"BUTTON") (PHÂN BIỆT hoa/thường, :530-537);
    ///   listObjectCode = ResolveObjects(menus) (menu + FunctionCodes của menu trong SysObjectSetting.config, :539); NẾU có kết quả: + buttons, Distinct,
    ///   Lst = {ObjectCode.Trim(), GroupCode = Sys_Group.GroupCode.Trim()} (:540-559); NẾU rỗng (không có menu / không có cấu hình): GIỮ NGUYÊN Lst client gửi (LỖI NGUỒN giữ nguyên).
    /// Biz: Sys_Group_CheckDB(GroupCode, Yes) -> StdParam(ObjectCode) từng dòng -> Sys_Object_CheckDB(ObjectCode, Yes, "1") từng dòng -> XOÁ HẾT Sys_Access của nhóm rồi chèn lại (:990-1012).
    /// Lỗi bất ngờ (vd so_ObjectType null -> NullReference như nguồn) do PortExceptionFilter trả 400. Kiểm tra quyền Sys_Access_CheckDenyV30 loại Đ (chưa có phân quyền).
    /// </summary>
    [HttpPost("savemenubuttoningroup")]
    public async Task<IActionResult> SaveMenuButtonInGroup([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] SysAccessSaveMenuButtonInGroupReq? req)
    {
        // gate :498-505: strJson rỗng -> ServiceException("strJson null") -> mã mặc định Sys_Access_Save
        if (req == null)
        {
            return PortResult.Fail("Sys_Access_Save", "Check.strJson", null);
        }

        var lst = req.Lst_Sys_Access ?? new List<Sys_Access_Input>();
        var listMenu = new List<string>();
        var listButton = new List<string>();
        foreach (var item in lst)
        {
            if (item.so_ObjectType!.Equals("MENU")) listMenu.Add(item.ObjectCode!);
            else if (item.so_ObjectType.Equals("BUTTON")) listButton.Add(item.ObjectCode!);
        }

        var listObjectCode = ResolveObjects(listMenu);
        List<string?> finalCodes;
        if (listObjectCode.Count > 0)
        {
            if (listButton.Count > 0) listObjectCode.AddRange(listButton);
            finalCodes = listObjectCode.Distinct().Select(it => (string?)it.Trim()).ToList();
        }
        else
        {
            finalCodes = lst.Select(x => x.ObjectCode).ToList();   // giữ nguyên Lst của client (gate :540-559 chỉ thay khi listObjectCode không rỗng)
        }

        var groupCodeRaw = req.Sys_Group!.GroupCode;   // Sys_Group null -> NullReference như nguồn

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // Sys_Group_CheckDB(GroupCode, "1") (System.cs:910-915)
            var strGroupCode = StdParam(groupCodeRaw);
            var dbGroup = await _db.Set<Sys_Group>().AsNoTracking()
                .FirstOrDefaultAsync(x => EF.Functions.Like((x.GroupCode ?? "").ToLower(), PortQuery.Equal(strGroupCode ?? ""), PortQuery.Esc));
            if (dbGroup == null)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Sys_Group_CheckDB_GroupCodeNotFound", "Check.GroupCodeNotFound", strGroupCode);
            }

            // StdParam(ObjectCode) cả bảng rồi Sys_Object_CheckDB(ObjectCode, "1", "1") từng dòng theo thứ tự (:952-972)
            var objCodes = finalCodes.Select(c => StdParam(c)).ToList();
            foreach (var objCode in objCodes)
            {
                var dbObj = await _db.Set<Sys_Object>().AsNoTracking()
                    .FirstOrDefaultAsync(x => EF.Functions.Like((x.ObjectCode ?? "").ToLower(), PortQuery.Equal(objCode ?? ""), PortQuery.Esc));
                if (dbObj == null)
                {
                    await tx.RollbackAsync();
                    return PortResult.Fail("Sys_Object_CheckDB_ObjectCodeNotFound", "Check.ObjectCodeNotFound", objCode);
                }
                if (!"1".Contains(dbObj.FlagActive ?? ""))
                {
                    await tx.RollbackAsync();
                    return PortResult.Fail("Sys_Object_CheckDB_FlagActiveNotMatched", "Check.ObjectCode", objCode,
                        "Check.FlagActiveListToCheck", "1", "DB.FlagActive", dbObj.FlagActive);
                }
            }

            // Clear All + Insert All (:988-1012): GroupCode = mã nhóm trong DB, LogLU = nhóm (LogLUDTimeUTC = bây giờ, LogLUBy = người dùng)
            var existing = await _db.Set<Sys_Access>().Where(x => x.GroupCode == dbGroup.GroupCode).ToListAsync();
            _db.Set<Sys_Access>().RemoveRange(existing);
            await _db.SaveChangesAsync();

            var now = DateTime.UtcNow;
            foreach (var objCode in objCodes)
            {
                _db.Set<Sys_Access>().Add(new Sys_Access { GroupCode = dbGroup.GroupCode, ObjectCode = objCode!, LogLUDTimeUTC = now, LogLUBy = "port" });
            }
            await _db.SaveChangesAsync();

            await tx.CommitAsync();
            return Ok(new { Success = true, Data = (object?)null });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return PortResult.Fail("Sys_Access_Save", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// BaseController.cs:717-767 ResolveObjects: menu + các FunctionCodes của menu (SysObjectSetting.config); mã chức năng tách theo dấu phẩy; loại trùng theo Dictionary (phân biệt hoa/thường).
    /// Không có cấu hình (setting null) -> danh sách rỗng (kể cả menu).
    /// </summary>
    private List<string> ResolveObjects(List<string> objectCodes)
    {
        var list = new List<string>();
        var setting = GetSysObjectSetting();
        var dic = new Dictionary<string, bool>();
        if (setting != null && setting.List != null)
        {
            foreach (var code in objectCodes)
            {
                if (!dic.ContainsKey(code))
                {
                    dic.Add(code, true);
                    list.Add(code);
                }

                foreach (var scode in setting.List)
                {
                    if (scode.ObjectCode!.Equals(code, StringComparison.InvariantCultureIgnoreCase))
                    {
                        foreach (var f in scode.FunctionCodes!)
                        {
                            if (!string.IsNullOrEmpty(f))
                            {
                                if (!dic.ContainsKey(f))
                                {
                                    dic.Add(f, true);
                                    foreach (var strCode in f.Split(','))
                                    {
                                        list.Add(strCode);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        return list;
    }

    /// <summary>
    /// BaseController.cs:654-669 GetSysObjectSetting: đọc cấu hình ~/App_Data/SysObjectSetting.config.
    /// </summary>
    private SysObjectSettingModel? GetSysObjectSetting()
    {
        var root = _env?.ContentRootPath ?? Directory.GetCurrentDirectory();
        var filePath = Path.Combine(root, "App_Data", "SysObjectSetting.config");
        if (!System.IO.File.Exists(filePath))
        {
            filePath = Path.Combine(AppContext.BaseDirectory, "App_Data", "SysObjectSetting.config");
            if (!System.IO.File.Exists(filePath)) return null;
        }

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

    /// <summary>
    /// Chuẩn hoá chuỗi mã theo CUtils.StdParam: Trim, viết HOA, rỗng -> null.
    /// </summary>
    private static string? StdParam(object? s)
    {
        if (s == null) return null;
        var str = s.ToString()?.Trim().ToUpperInvariant();
        return string.IsNullOrEmpty(str) ? null : str;
    }
}

