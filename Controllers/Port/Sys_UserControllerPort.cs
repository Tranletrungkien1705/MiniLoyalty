namespace MiniLoyalty.Controllers.Port;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MiniLoyalty.Data;
using MiniLoyalty.Models.Port;

/// <summary>
/// DTO nhận tham số tìm kiếm người dùng cho action Search.
/// Tham số tương ứng tầng Gate (Sys_UserController.cs:33): OrgID, KeyWord, FlagActive, Ft_PageIndex, Ft_PageSize.
/// Tuân thủ Bài học A.14: nhận qua body JSON với EmptyBodyBehavior = EmptyBodyBehavior.Allow.
/// </summary>
public class Sys_UserSearchReq
{
    /// <summary>Mã tổ chức OrgID - lọc chính xác bằng toán tử = (Sys_UserController.cs:1409-1412)</summary>
    public string? OrgID { get; set; }

    /// <summary>Từ khoá tìm kiếm - lọc like %...% (or) cho 4 cột: UserName, PhoneNo, EMail, UserCode (Sys_UserController.cs:1399-1407)</summary>
    public string? KeyWord { get; set; }

    /// <summary>Trạng thái kích hoạt - lọc chính xác bằng toán tử = (Sys_UserController.cs:1413-1416)</summary>
    public string? FlagActive { get; set; }

    /// <summary>Chỉ số trang (0-based) - mặc định 0</summary>
    public int Ft_PageIndex { get; set; } = 0;

    /// <summary>Kích thước trang - mặc định 10</summary>
    public int Ft_PageSize { get; set; } = 10;
}

/// <summary>
/// Controller Port 1:1 cho Sys_UserController — hạng mục #323 Sys_UserController/Search.
/// Nguồn Gate: D:\idocNet\2022.7.Loyalty\13.ClientGate\V10\idn.Loyalty.WebAPI\Controllers\Sys_UserController.cs:33-114
/// Nguồn Hàm lọc: Sys_UserController.cs:1394-1419 (strWhereClause_Sys_User)
/// Nguồn Biz LIVE: D:\idocNet\2022.7.Loyalty\11.BackEnd\V10\idn.Loyalty.Biz\System.cs:7987-8363 (Sys_User_Get_New20191104)
/// Route: api/sys_user
/// </summary>
[ApiController]
[Route("api/sys_user")]
[Produces("application/json")]
public class Sys_UserControllerPort : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration? _config;

    // Hằng số che mật khẩu từ nguồn: TConst.BizMix.Default_PasswordMask = "*********" (Const.Main.BE.cs:13, System.cs:8170, 8211)
    private const string Default_PasswordMask = "*********";

    public Sys_UserControllerPort(AppDbContext db, IConfiguration? config = null)
    {
        _db = db;
        _config = config;
    }

    /// <summary>
    /// Port 1:1 Search action từ gate Sys_UserController.cs:33-114 (#323).
    /// </summary>
    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Sys_UserSearchReq? req)
    {
        var r = req ?? new Sys_UserSearchReq();
        var pageIndex = r.Ft_PageIndex;
        var pageSize = r.Ft_PageSize;

        // 1. Khởi tạo truy vấn bảng Sys_User (#tbl_Sys_User_Filter_Draft trong System.cs:8110-8128)
        var q = _db.Set<Sys_User>().AsNoTracking().AsQueryable();

        // 2. Lọc theo KeyWord: Sys_UserController.cs:1399-1407
        // sbSql.AddWhereClause(Tbl_Sys_User + TblSys_User.UserName, "%" + CUtils.StrValue(data.KeyWord) + "%", "like", "or")
        // sbSql.AddWhereClause(Tbl_Sys_User + TblSys_User.PhoneNo, "%" + CUtils.StrValue(data.KeyWord) + "%", "like", "or")
        // sbSql.AddWhereClause(Tbl_Sys_User + TblSys_User.EMail, "%" + CUtils.StrValue(data.KeyWord) + "%", "like", "or")
        // sbSql.AddWhereClause(Tbl_Sys_User + TblSys_User.UserCode, "%" + CUtils.StrValue(data.KeyWord) + "%", "like", "or")
        if (!string.IsNullOrWhiteSpace(r.KeyWord))
        {
            var kw = PortQuery.Contains(r.KeyWord);
            q = q.Where(x =>
                EF.Functions.Like((x.UserName ?? "").ToLower(), kw, PortQuery.Esc) ||
                EF.Functions.Like((x.PhoneNo ?? "").ToLower(), kw, PortQuery.Esc) ||
                EF.Functions.Like((x.EMail ?? "").ToLower(), kw, PortQuery.Esc) ||
                EF.Functions.Like((x.UserCode ?? "").ToLower(), kw, PortQuery.Esc));
        }

        // 3. Lọc theo OrgID: Sys_UserController.cs:1409-1412
        // sbSql.AddWhereClause(Tbl_Sys_User + TblSys_User.OrgID, CUtils.StrValue(data.OrgID), "=")
        if (!string.IsNullOrWhiteSpace(r.OrgID))
        {
            var org = PortQuery.Equal(r.OrgID);
            q = q.Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), org, PortQuery.Esc));
        }

        // 4. Lọc theo FlagActive: Sys_UserController.cs:1413-1416
        // sbSql.AddWhereClause(Tbl_Sys_User + TblSys_User.FlagActive, CUtils.StrValue(data.FlagActive), "=")
        if (!string.IsNullOrWhiteSpace(r.FlagActive))
        {
            var fa = PortQuery.Equal(r.FlagActive);
            q = q.Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), fa, PortQuery.Esc));
        }

        // 5. Sắp xếp draft: System.cs:8127: "order by su.UserCode asc"
        var ordered = q.OrderBy(x => x.UserCode);

        // 6. Tổng số bản ghi (select Count(0) MyCount from #tbl_Sys_User_Filter_Draft - System.cs:8131)
        var itemCount = await ordered.CountAsync();

        // 7. Phân trang theo bài học A.12 và A.7:
        // nFilterRecordStart = pageIndex * pageSize; nFilterRecordEnd = nFilterRecordStart + pageSize - 1;
        // Ft_PageSize = 0 hoặc recordCount <= 0 hoặc recordStart < 0 -> DataList rỗng
        var recordStart = (long)pageIndex * pageSize;
        var recordCount = (long)pageSize;

        var pagedUsers = (recordCount <= 0 || recordStart < 0)
            ? new List<Sys_User>()
            : await ordered.Skip((int)Math.Min(recordStart, int.MaxValue))
                           .Take((int)Math.Min(recordCount, int.MaxValue))
                           .ToListAsync();

        // 8. Thực hiện Left Join sang các bảng liên quan theo System.cs:8196-8207:
        // - Mst_Department mdept on su.DepartmentCode = mdept.DepartmentCode
        // - Mst_Customer mc on su.CustomerCodeSys = mc.CustomerCodeSys
        // - Mst_Org mo on su.OrgID = mo.OrgID
        // - Map_DealerCP_Org mdcpo on mo.OrgID = mdcpo.OrgID
        // - Mst_DealerCP mdcp on mdcpo.DLCPCode = mdcp.DLCPCode
        var deptCodes = pagedUsers.Select(u => u.DepartmentCode).Where(c => !string.IsNullOrEmpty(c)).Distinct().ToList();
        var custCodeSysList = pagedUsers.Select(u => u.CustomerCodeSys).Where(c => !string.IsNullOrEmpty(c)).Distinct().ToList();
        var orgIds = pagedUsers.Select(u => u.OrgID).Where(c => !string.IsNullOrEmpty(c)).Distinct().ToList();

        var departments = await _db.Set<Mst_Department>().AsNoTracking()
            .Where(d => deptCodes.Contains(d.DepartmentCode))
            .ToDictionaryAsync(d => d.DepartmentCode, d => d);

        var customers = await _db.Set<Mst_Customer>().AsNoTracking()
            .Where(c => custCodeSysList.Contains(c.CustomerCodeSys))
            .ToDictionaryAsync(c => c.CustomerCodeSys, c => c);

        var dealerMappings = await (
            from mo in _db.Set<Mst_Org>().AsNoTracking()
            where orgIds.Contains(mo.OrgID)
            join mdcpo in _db.Set<Map_DealerCP_Org>().AsNoTracking() on mo.OrgID equals mdcpo.OrgID into mdcpoGroup
            from mdcpo in mdcpoGroup.DefaultIfEmpty()
            join mdcp in _db.Set<Mst_DealerCP>().AsNoTracking() on (mdcpo != null ? mdcpo.DLCPCode : null) equals mdcp.DLCPCode into mdcpGroup
            from mdcp in mdcpGroup.DefaultIfEmpty()
            select new
            {
                OrgID = mo.OrgID,
                DLCPCode = mdcpo != null ? mdcpo.DLCPCode : null,
                DLCPName = mdcp != null ? mdcp.DLCPName : null
            }
        ).ToListAsync();

        var dealerDict = dealerMappings
            .GroupBy(x => x.OrgID)
            .ToDictionary(g => g.Key, g => g.FirstOrDefault());

        // 9. Map dữ liệu trả về cho Client, áp dụng gán PasswordMask và xử lý ACAvatar ở tầng Gate (Sys_UserController.cs:90)
        var inosBaseUrl = _config?["InosBaseUrl"] ?? "";

        var dataList = pagedUsers.Select((u, idx) =>
        {
            departments.TryGetValue(u.DepartmentCode ?? "", out var dept);
            customers.TryGetValue(u.CustomerCodeSys ?? "", out var cust);
            dealerDict.TryGetValue(u.OrgID ?? "", out var dcp);

            return new Sys_User
            {
                MyIdxSeq = recordStart + idx,
                UserCode = u.UserCode,
                UserName = u.UserName,
                // Nguồn (System.cs:8165-8200) CHỈ chọn: MyIdxSeq, UserCode, UserName, MST, UserPassword(mask), PhoneNo, EMail, DepartmentCode, Position, Flag*, CustomerCodeSys, AC*, mc.*, mdept_*, mdcpo_*, mdcp_*  -> các cột khác (UserPasswordNew, VerificationCode, UUID, Avatar, NetworkID, OrganCode, LogLU*) KHÔNG được trả (null)
                UserPassword = Default_PasswordMask, // 'zzzzClausePVal_Default_PasswordMask' -> TConst.BizMix.Default_PasswordMask (System.cs:8170, 8211)
                PhoneNo = u.PhoneNo,
                EMail = u.EMail,
                MST = u.MST,
                DepartmentCode = u.DepartmentCode,
                Position = u.Position,
                FlagDLAdmin = u.FlagDLAdmin,
                FlagSysAdmin = u.FlagSysAdmin,
                FlagNNTAdmin = u.FlagNNTAdmin,
                OrgID = u.OrgID,
                CustomerCodeSys = u.CustomerCodeSys,
                FlagActive = u.FlagActive,

                // Xử lý ACAvatar ở tầng Gate (Sys_UserController.cs:90):
                // x.ACAvatar = !string.IsNullOrEmpty(x.ACAvatar) ? string.Format("{0}{1}", inosBaseUrl, Url.Content(x.ACAvatar)) : ""
                ACId = u.ACId,
                ACAvatar = !string.IsNullOrEmpty(u.ACAvatar) ? $"{inosBaseUrl}{u.ACAvatar}" : "",
                ACEmail = u.ACEmail,
                ACLanguage = u.ACLanguage,
                ACName = u.ACName,
                ACPhone = u.ACPhone,
                ACTimeZone = u.ACTimeZone,

                // Các trường mở rộng từ bảng join (System.cs:8189-8194)
                CustomerCode = cust?.CustomerCode,
                CustomerName = cust?.CustomerName,
                mdept_DepartmentCode = dept?.DepartmentCode,
                mdept_DepartmentName = dept?.DepartmentName,
                mdcpo_DLCPCode = dcp?.DLCPCode,
                mdcp_DLCPName = dcp?.DLCPName
            };
        }).ToList();

        // 10. Tính PageCount theo logic tầng gate (Sys_UserController.cs:92-96):
        // if (objRT_Sys_User.Lst_Sys_User != null && objRT_Sys_User.Lst_Sys_User.Count > 0)
        // {
        //     if (pageSize == 0) pageSize = PageSizeConfig;
        //     pageCount = (itemCount % pageSize == 0) ? itemCount / pageSize : itemCount / pageSize + 1;
        // }
        // Khi dataList rỗng -> pageCount = 0 (Bài học A.7).
        var pageCount = 0;
        if (dataList.Count > 0)
        {
            var ps = pageSize == 0 ? 10 : pageSize;
            pageCount = (itemCount % ps == 0) ? (itemCount / ps) : (itemCount / ps + 1);
        }

        var pageInfo = new PageInfo<Sys_User>(dataList, itemCount, pageCount, pageIndex, pageSize);

        return Ok(new
        {
            Success = true,
            Data = pageInfo
        });
    }
}
