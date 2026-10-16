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
/// DTO request cho action GetByUserCode (Sys_UserController.cs:116).
/// </summary>
public class Sys_UserGetByUserCodeReq
{
    public string? UserCode { get; set; }
}

/// <summary>
/// DTO response cho action GetByUserCode (Gate Sys_UserController.cs:167-206).
/// </summary>
public class Sys_UserGetByUserCodeRes
{
    public string? UserCode { get; set; }
    public string? NetworkID { get; set; }
    public string? UserName { get; set; }
    public string? UserPassword { get; set; }
    public string? UserPasswordNew { get; set; }
    public string? PhoneNo { get; set; }
    public string? EMail { get; set; }
    public string? MST { get; set; }
    public string? OrganCode { get; set; }
    public string? DepartmentCode { get; set; }
    public string? Position { get; set; }
    public string? VerificationCode { get; set; }
    public string? Avatar { get; set; }
    public string? UUID { get; set; }
    public string? FlagDLAdmin { get; set; }
    public string? FlagSysAdmin { get; set; }
    public string? FlagNNTAdmin { get; set; }
    public string? OrgID { get; set; }
    public string? CustomerCodeSys { get; set; }
    public string? CustomerCode { get; set; }
    public string? CustomerName { get; set; }
    public string? FlagActive { get; set; }
    public string? ACId { get; set; }
    public string? ACAvatar { get; set; }
    public string? ACEmail { get; set; }
    public string? ACLanguage { get; set; }
    public string? ACName { get; set; }
    public string? ACPhone { get; set; }
    public string? ACTimeZone { get; set; }
    public string? mo_OrganCode { get; set; }
    public string? mo_OrganName { get; set; }
    public string? mdept_DepartmentCode { get; set; }
    public string? mdept_DepartmentName { get; set; }
    public string? mnnt_DealerType { get; set; }
    public string? ctitctg_CustomerGrpCode { get; set; }
    public string? ACAvatarUrl { get; set; }
    public string? mdcpo_DLCPCode { get; set; }
    public string? mdcp_DLCPName { get; set; }
}

/// <summary>
/// DTO request cho action GetByMST (Gate Sys_UserController.cs:220).
/// </summary>
public class Sys_UserGetByMSTReq
{
    public string? MST { get; set; }
}

/// <summary>
/// DTO response cho từng phần tử trong danh sách kết quả GetByMST (Gate Sys_UserController.cs:270-307).
/// </summary>
public class Sys_UserGetByMSTItemRes
{
    public string? UserCode { get; set; }
    public string? NetworkID { get; set; }
    public string? UserName { get; set; }
    public string? UserPassword { get; set; }
    public string? UserPasswordNew { get; set; }
    public string? PhoneNo { get; set; }
    public string? EMail { get; set; }
    public string? MST { get; set; }
    public string? OrganCode { get; set; }
    public string? DepartmentCode { get; set; }
    public string? Position { get; set; }
    public string? VerificationCode { get; set; }
    public string? Avatar { get; set; }
    public string? UUID { get; set; }
    public string? FlagDLAdmin { get; set; }
    public string? FlagSysAdmin { get; set; }
    public string? FlagNNTAdmin { get; set; }
    public string? OrgID { get; set; }
    public string? CustomerCodeSys { get; set; }
    public string? CustomerCode { get; set; }
    public string? CustomerName { get; set; }
    public string? FlagActive { get; set; }
    public string? ACId { get; set; }
    public string? ACAvatar { get; set; }
    public string? ACEmail { get; set; }
    public string? ACLanguage { get; set; }
    public string? ACName { get; set; }
    public string? ACPhone { get; set; }
    public string? ACTimeZone { get; set; }
    public string? mo_OrganCode { get; set; }
    public string? mo_OrganName { get; set; }
    public string? mdept_DepartmentCode { get; set; }
    public string? mdept_DepartmentName { get; set; }
    public string? mnnt_DealerType { get; set; }
    public string? ctitctg_CustomerGrpCode { get; set; }
    public string? ACAvatarUrl { get; set; }
}

/// <summary>
/// DTO request cho action GetForCurrentUser (Gate Sys_UserController.cs:390).
/// </summary>
public class Sys_UserGetForCurrentUserReq
{
    public string? UserCode { get; set; }
}

/// <summary>
/// DTO response cho action GetForCurrentUser (Gate Sys_UserController.cs:442-483).
/// Gồm 39 trường tương ứng gate.
/// </summary>
public class Sys_UserGetForCurrentUserRes
{
    public string? UserCode { get; set; }
    public string? NetworkID { get; set; }
    public string? UserName { get; set; }
    public string? UserPassword { get; set; }
    public string? UserPasswordNew { get; set; }
    public string? PhoneNo { get; set; }
    public string? EMail { get; set; }
    public string? MST { get; set; }
    public string? OrganCode { get; set; }
    public string? DepartmentCode { get; set; }
    public string? Position { get; set; }
    public string? VerificationCode { get; set; }
    public string? Avatar { get; set; }
    public string? UUID { get; set; }
    public string? FlagDLAdmin { get; set; }
    public string? FlagSysAdmin { get; set; }
    public string? FlagNNTAdmin { get; set; }
    public string? OrgID { get; set; }
    public string? CustomerCodeSys { get; set; }
    public string? CustomerCode { get; set; }
    public string? CustomerName { get; set; }
    public string? FlagActive { get; set; }
    public string? ACId { get; set; }
    public string? ACAvatar { get; set; }
    public string? ACEmail { get; set; }
    public string? ACLanguage { get; set; }
    public string? ACName { get; set; }
    public string? ACPhone { get; set; }
    public string? ACTimeZone { get; set; }
    public string? mo_OrganCode { get; set; }
    public string? mo_OrganName { get; set; }
    public string? mdept_DepartmentCode { get; set; }
    public string? mdept_DepartmentName { get; set; }
    public string? mnnt_DealerType { get; set; }
    public string? ctitctg_CustomerGrpCode { get; set; }
    public string? ACAvatarUrl { get; set; }
    public string? mdcpo_DLCPCode { get; set; }
    public string? mdcp_DLCPName { get; set; }
    public string? mdcp_FlagRoot { get; set; }
}

/// <summary>
/// Controller Port 1:1 cho Sys_UserController — hạng mục #323 (Search), #324 (GetByUserCode), #325 (GetByMST), #327 (GetForCurrentUser), #328 (GetAllActive).
/// Nguồn Gate: D:\idocNet\2022.7.Loyalty\13.ClientGate\V10\idn.Loyalty.WebAPI\Controllers\Sys_UserController.cs:498-561
/// Nguồn API: D:\idocNet\2022.7.Loyalty\11.BackEnd\V10\idn.Loyalty.Biz.Web\Controllers\SysUserController.cs:199-270
/// Nguồn Biz LIVE: D:\idocNet\2022.7.Loyalty\11.BackEnd\V10\idn.Loyalty.Biz\System.cs:8365-8478, 7987-8363 (Sys_User_Get_New20191104)
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
                ACAvatar = !string.IsNullOrEmpty(u.ACAvatar) ? string.Format("{0}{1}", inosBaseUrl, UrlContent(u.ACAvatar)) : "",   // x.ACAvatar = ... Url.Content(x.ACAvatar) (gate Sys_UserController.cs:90)
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

    /// <summary>
    /// Port 1:1 GetByUserCode action từ gate Sys_UserController.cs:116-219 (#324).
    /// Hỗ trợ GET query parameter: /api/sys_user/getbyusercode?userCode={userCode} (DEBAI.md P3).
    /// </summary>
/// <summary>
    /// Port 1:1 GetByUserCode action hỗ trợ POST JSON body (Bài học A.14).
    /// </summary>
    [HttpPost("getbyusercode")]
    public async Task<IActionResult> GetByUserCodePost([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Sys_UserGetByUserCodeReq? req)
    {
        var userCode = req?.UserCode;
        return await GetByUserCodeCoreAsync(userCode);
    }

    /// <summary>
    /// Logic cốt lõi của GetByUserCode:
    /// 1. Nếu userCode rỗng hoặc null: trả Success = true, Data = null (Gate Sys_UserController.cs:136).
    /// 2. Tìm Sys_User theo UserCode với toán tử '=' (không phân biệt hoa/thường, không wildcard).
    /// 3. Nếu không tìm thấy: trả Success = true, Data = null (Gate Sys_UserController.cs:167).
    /// 4. Left join các bảng liên quan theo System.cs:8196-8207:
    ///    - Mst_Department mdept on su.DepartmentCode = mdept.DepartmentCode
    ///    - Mst_Customer mc on su.CustomerCodeSys = mc.CustomerCodeSys
    ///    - Mst_Org mo on su.OrgID = mo.OrgID
    ///    - Map_DealerCP_Org mdcpo on mo.OrgID = mdcpo.OrgID
    ///    - Mst_DealerCP mdcp on mdcpo.DLCPCode = mdcp.DLCPCode
    /// 5. Mask UserPassword = "*********" (TConst.BizMix.Default_PasswordMask, System.cs:8170, 8211).
    /// 6. Xử lý ACAvatar: inosBaseUrl + ACAvatar.Split('~')[1] (Gate Sys_UserController.cs:191, 203).
    /// </summary>
    private async Task<IActionResult> GetByUserCodeCoreAsync(string? userCode)
    {
        if (string.IsNullOrWhiteSpace(userCode))
        {
            return Ok(new
            {
                Success = true,
                Data = (object?)null
            });
        }

        var uCode = PortQuery.Equal(userCode.Trim());
        var user = await _db.Set<Sys_User>().AsNoTracking()
            .FirstOrDefaultAsync(x => EF.Functions.Like((x.UserCode ?? "").ToLower(), uCode, PortQuery.Esc));

        if (user == null)
        {
            return Ok(new
            {
                Success = true,
                Data = (object?)null
            });
        }

        // 1. Left join Mst_Department mdept on su.DepartmentCode = mdept.DepartmentCode
        Mst_Department? dept = null;
        if (!string.IsNullOrEmpty(user.DepartmentCode))
        {
            var dCode = PortQuery.Equal(user.DepartmentCode.Trim());
            dept = await _db.Set<Mst_Department>().AsNoTracking()
                .FirstOrDefaultAsync(d => EF.Functions.Like((d.DepartmentCode ?? "").ToLower(), dCode, PortQuery.Esc));
        }

        // 2. Left join Mst_Customer mc on su.CustomerCodeSys = mc.CustomerCodeSys
        Mst_Customer? cust = null;
        if (!string.IsNullOrEmpty(user.CustomerCodeSys))
        {
            var cCode = PortQuery.Equal(user.CustomerCodeSys.Trim());
            cust = await _db.Set<Mst_Customer>().AsNoTracking()
                .FirstOrDefaultAsync(c => EF.Functions.Like((c.CustomerCodeSys ?? "").ToLower(), cCode, PortQuery.Esc));
        }

        // 3. Left join Mst_Org mo -> Map_DealerCP_Org mdcpo -> Mst_DealerCP mdcp (System.cs:8202-8207)
        string? dlcpCode = null;
        string? dlcpName = null;
        if (!string.IsNullOrEmpty(user.OrgID))
        {
            var orgId = PortQuery.Equal(user.OrgID.Trim());
            var dealerInfo = await (
                from mo in _db.Set<Mst_Org>().AsNoTracking()
                where EF.Functions.Like((mo.OrgID ?? "").ToLower(), orgId, PortQuery.Esc)
                join mdcpo in _db.Set<Map_DealerCP_Org>().AsNoTracking() on mo.OrgID equals mdcpo.OrgID into mdcpoGroup
                from mdcpo in mdcpoGroup.DefaultIfEmpty()
                join mdcp in _db.Set<Mst_DealerCP>().AsNoTracking() on (mdcpo != null ? mdcpo.DLCPCode : null) equals mdcp.DLCPCode into mdcpGroup
                from mdcp in mdcpGroup.DefaultIfEmpty()
                select new
                {
                    DLCPCode = mdcpo != null ? mdcpo.DLCPCode : null,
                    DLCPName = mdcp != null ? mdcp.DLCPName : null
                }
            ).FirstOrDefaultAsync();

            if (dealerInfo != null)
            {
                dlcpCode = dealerInfo.DLCPCode;
                dlcpName = dealerInfo.DLCPName;
            }
        }

        var inosBaseUrl = _config?["InosBaseUrl"] ?? "";

        var res = new Sys_UserGetByUserCodeRes
        {
            UserCode = user.UserCode,
            NetworkID = null,   // không nằm trong select của Sys_User_Get
            UserName = user.UserName,
            UserPassword = Default_PasswordMask,
            UserPasswordNew = null,
            PhoneNo = user.PhoneNo,
            EMail = user.EMail,
            MST = user.MST,
            OrganCode = null,
            DepartmentCode = user.DepartmentCode,
            Position = user.Position,
            VerificationCode = null,
            Avatar = null,
            UUID = null,
            FlagDLAdmin = user.FlagDLAdmin,
            FlagSysAdmin = user.FlagSysAdmin,
            FlagNNTAdmin = user.FlagNNTAdmin,
            OrgID = user.OrgID,
            CustomerCodeSys = user.CustomerCodeSys,
            CustomerCode = cust?.CustomerCode,
            CustomerName = cust?.CustomerName,
            FlagActive = user.FlagActive,
            ACId = user.ACId,
            ACAvatar = FormatACAvatar(user.ACAvatar, inosBaseUrl),
            ACEmail = user.ACEmail,
            ACLanguage = user.ACLanguage,
            ACName = user.ACName,
            ACPhone = user.ACPhone,
            ACTimeZone = user.ACTimeZone,
            mo_OrganCode = null,
            mo_OrganName = null,
            mdept_DepartmentCode = dept?.DepartmentCode,
            mdept_DepartmentName = dept?.DepartmentName,
            mnnt_DealerType = null,
            ctitctg_CustomerGrpCode = null,
            ACAvatarUrl = FormatACAvatar(user.ACAvatar, inosBaseUrl),
            mdcpo_DLCPCode = dlcpCode,
            mdcp_DLCPName = dlcpName
        };

        return Ok(new
        {
            Success = true,
            Data = res
        });
    }

    /// <summary>
    /// Port 1:1 GetByMST action từ gate Sys_UserController.cs:220-320 (#325).
    /// Chuỗi gọi: Gate Sys_UserController.cs:220 -> WA_Sys_User_Get (SysUserController.cs:199)
    ///            -> WAS_Sys_User_Get (System.cs:8365) -> Sys_User_Get_New20191104 (System.cs:7987-8363).
    /// Nhận body JSON { MST } qua DTO Sys_UserGetByMSTReq (Bài học A.14).
    /// </summary>
    [HttpPost("getbymst")]
    public async Task<IActionResult> GetByMST([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Sys_UserGetByMSTReq? req)
    {
        var mst = req?.MST;

        // Gate Sys_UserController.cs:240: if (!TUtils.CUtils.IsNullOrEmpty(MST))
        // Khi MST null hoặc rỗng: không gọi backend, trả về Success = true, Data = null (Sys_UserController.cs:224, 269)
        if (string.IsNullOrWhiteSpace(mst))
        {
            return Ok(new
            {
                Success = true,
                Data = (object?)null
            });
        }

        // Gate Sys_UserController.cs:242-245 & strWhereClause_Sys_User_GetDetail (Sys_UserController.cs:1430-1433):
        // sbSql.AddWhereClause(Tbl_Sys_User + TblSys_User.MST, CUtils.StrValue(data.MST), "=")
        // Chỉ truyền MST, KHÔNG truyền và KHÔNG lọc OrgID, UserCode hay FlagActive (Sys_UserController.cs:242-245).
        // Lọc chính xác bằng toán tử = (Quy ước mục 3, bài học A.14: EF.Functions.Like + PortQuery.Equal)
        var mstPattern = PortQuery.Equal(mst.Trim());
        var query = _db.Set<Sys_User>().AsNoTracking()
            .Where(x => EF.Functions.Like((x.MST ?? "").ToLower(), mstPattern, PortQuery.Esc));

        // Backend System.cs:8127: order by su.UserCode asc
        var users = await query.OrderBy(x => x.UserCode).ToListAsync();

        // Gate Sys_UserController.cs:264, 270: nếu Lst_Sys_User null hoặc rỗng -> Data = null
        if (users.Count == 0)
        {
            return Ok(new
            {
                Success = true,
                Data = (object?)null
            });
        }

        // Left join Mst_Department và Mst_Customer theo System.cs:8196-8207:
        // - Mst_Department mdept on su.DepartmentCode = mdept.DepartmentCode
        // - Mst_Customer mc on su.CustomerCodeSys = mc.CustomerCodeSys
        var deptCodes = users.Select(u => u.DepartmentCode).Where(c => !string.IsNullOrEmpty(c)).Distinct().ToList();
        var custCodeSysList = users.Select(u => u.CustomerCodeSys).Where(c => !string.IsNullOrEmpty(c)).Distinct().ToList();

        var departments = await _db.Set<Mst_Department>().AsNoTracking()
            .Where(d => deptCodes.Contains(d.DepartmentCode))
            .ToDictionaryAsync(d => d.DepartmentCode, d => d);

        var customers = await _db.Set<Mst_Customer>().AsNoTracking()
            .Where(c => custCodeSysList.Contains(c.CustomerCodeSys))
            .ToDictionaryAsync(c => c.CustomerCodeSys, c => c);

        var inosBaseUrl = _config?["InosBaseUrl"] ?? "";

        // Map danh sách kết quả theo đúng cấu trúc gate Sys_UserController.cs:270-307:
        // - UserPassword được ẩn thành Default_PasswordMask (System.cs:8170, 8211)
        // - ACAvatar và ACAvatarUrl xử lý qua FormatACAvatar (Sys_UserController.cs:294, 306)
        var dataList = users.Select(it =>
        {
            departments.TryGetValue(it.DepartmentCode ?? "", out var dept);
            customers.TryGetValue(it.CustomerCodeSys ?? "", out var cust);

            var avatar = FormatACAvatar(it.ACAvatar, inosBaseUrl);

            return new Sys_UserGetByMSTItemRes
            {
                UserCode = it.UserCode,
                NetworkID = null, // Backend Sys_User_Get không chọn NetworkID (System.cs:8167-8195)
                UserName = it.UserName,
                UserPassword = Default_PasswordMask, // TConst.BizMix.Default_PasswordMask '*********' (System.cs:8170, 8211)
                UserPasswordNew = null,
                PhoneNo = it.PhoneNo,
                EMail = it.EMail,
                MST = it.MST,
                OrganCode = null,
                DepartmentCode = it.DepartmentCode,
                Position = it.Position,
                VerificationCode = null,
                Avatar = null,
                UUID = null,
                FlagDLAdmin = it.FlagDLAdmin,
                FlagSysAdmin = it.FlagSysAdmin,
                FlagNNTAdmin = it.FlagNNTAdmin,
                OrgID = it.OrgID,
                CustomerCodeSys = it.CustomerCodeSys,
                CustomerCode = cust?.CustomerCode,
                CustomerName = cust?.CustomerName,
                FlagActive = it.FlagActive,
                ACId = it.ACId,
                ACAvatar = avatar,
                ACEmail = it.ACEmail,
                ACLanguage = it.ACLanguage,
                ACName = it.ACName,
                ACPhone = it.ACPhone,
                ACTimeZone = it.ACTimeZone,
                mo_OrganCode = null,
                mo_OrganName = null,
                mdept_DepartmentCode = dept?.DepartmentCode,
                mdept_DepartmentName = dept?.DepartmentName,
                mnnt_DealerType = null,
                ctitctg_CustomerGrpCode = null,
                ACAvatarUrl = avatar
            };
        }).ToList();

        return Ok(new
        {
            Success = true,
            Data = dataList
        });
    }

    /// <summary>
    /// Port 1:1 Sys_UserController.GetForCurrentUser (#327, gate :390-490) -> WA_Sys_User_GetForCurrentUser -> Sys_User_GetForCurrentUser (System.cs:5726-5860).
    /// Biz CHỈ chọn 6 cột của Sys_User: UserCode, UserName, MST, FlagDLAdmin, FlagSysAdmin, FlagActive (:5774-5783) — mọi cột khác trong phép chiếu của gate là null.
    /// UserCode rỗng -> không truy vấn, Data = null; không tìm thấy -> Data = null. (Các bảng quyền Sys_Access/Sys_Object do biz trả thêm không được gate dùng; Sys_Access_CheckDeny loại Đ.)
    /// </summary>
    [HttpPost("getforcurrentuser")]
    public async Task<IActionResult> GetForCurrentUser([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Sys_UserGetForCurrentUserReq? req)
    {
        var userCode = req?.UserCode;
        if (string.IsNullOrEmpty(userCode))
        {
            return Ok(new { Success = true, Data = (object?)null });
        }

        var uCode = PortQuery.Equal(userCode);
        var user = await _db.Set<Sys_User>().AsNoTracking()
            .FirstOrDefaultAsync(x => EF.Functions.Like((x.UserCode ?? "").ToLower(), uCode, PortQuery.Esc));
        if (user == null)
        {
            return Ok(new { Success = true, Data = (object?)null });
        }

        var res = new Sys_UserGetForCurrentUserRes
        {
            UserCode = user.UserCode,
            UserName = user.UserName,
            MST = user.MST,
            FlagDLAdmin = user.FlagDLAdmin,
            FlagSysAdmin = user.FlagSysAdmin,
            FlagActive = user.FlagActive
        };
        return Ok(new { Success = true, Data = res });
    }

    /// <summary>
    /// Port 1:1 GetAllActive action từ gate Sys_UserController.cs:498-561 (#328).
    /// Chuỗi gọi: Gate Sys_UserController.cs:498 -> WA_Sys_User_Get (SysUserController.cs:199)
    ///            -> WAS_Sys_User_Get (System.cs:8365) -> Sys_User_Get_New20191104 (System.cs:7987-8363).
    /// Lọc FlagActive = "1", sắp xếp theo UserCode asc, trả về đối tượng chứa Lst_Sys_User (null nếu rỗng).
    /// </summary>
    [HttpPost("getallactive")]
    public async Task<IActionResult> GetAllActive([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] object? req)
    {
        // 1. Lọc theo FlagActive = "1" (Sys_UserController.cs:518-521, 1413-1416)
        var fa = PortQuery.Equal("1");
        var q = _db.Set<Sys_User>().AsNoTracking()
            .Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), fa, PortQuery.Esc));

        // 2. Sắp xếp draft: System.cs:8127: "order by su.UserCode asc"
        var users = await q.OrderBy(x => x.UserCode).ToListAsync();

        if (users.Count == 0)
        {
            return Ok(new
            {
                Success = true,
                Data = new
                {
                    Lst_Sys_User = (List<Sys_User>?)null
                }
            });
        }

        // 3. Thực hiện Left Join sang các bảng liên quan theo System.cs:8196-8207:
        // - Mst_Department mdept on su.DepartmentCode = mdept.DepartmentCode
        // - Mst_Customer mc on su.CustomerCodeSys = mc.CustomerCodeSys
        // - Mst_Org mo on su.OrgID = mo.OrgID
        // - Map_DealerCP_Org mdcpo on mo.OrgID = mdcpo.OrgID
        // - Mst_DealerCP mdcp on mdcpo.DLCPCode = mdcp.DLCPCode
        var deptCodes = users.Select(u => u.DepartmentCode).Where(c => !string.IsNullOrEmpty(c)).Distinct().ToList();
        var custCodeSysList = users.Select(u => u.CustomerCodeSys).Where(c => !string.IsNullOrEmpty(c)).Distinct().ToList();
        var orgIds = users.Select(u => u.OrgID).Where(c => !string.IsNullOrEmpty(c)).Distinct().ToList();

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

        // 4. Map dữ liệu trả về cho Client (System.cs:8165-8209)
        var rows = users.Select((u, idx) =>
        {
            departments.TryGetValue(u.DepartmentCode ?? "", out var dept);
            customers.TryGetValue(u.CustomerCodeSys ?? "", out var cust);
            dealerDict.TryGetValue(u.OrgID ?? "", out var dcp);

            return new Sys_User
            {
                MyIdxSeq = idx,
                UserCode = u.UserCode,
                UserName = u.UserName,
                UserPassword = Default_PasswordMask,
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

                ACId = u.ACId,
                ACAvatar = u.ACAvatar,
                ACEmail = u.ACEmail,
                ACLanguage = u.ACLanguage,
                ACName = u.ACName,
                ACPhone = u.ACPhone,
                ACTimeZone = u.ACTimeZone,

                CustomerCode = cust?.CustomerCode,
                CustomerName = cust?.CustomerName,
                mdept_DepartmentCode = dept?.DepartmentCode,
                mdept_DepartmentName = dept?.DepartmentName,
                mdcpo_DLCPCode = dcp?.DLCPCode,
                mdcp_DLCPName = dcp?.DLCPName
            };
        }).ToList();

        return Ok(new
        {
            Success = true,
            Data = new
            {
                Lst_Sys_User = rows.Count > 0 ? rows : null
            }
        });
    }

    /// <summary>
    /// Xử lý đường dẫn avatar theo Gate Sys_UserController.cs:191, 203:
    /// inosBaseUrl + TUtils.CUtils.StrValue(objSys_User.ACAvatar).Split('~')[1]
    /// An toàn khi chuỗi không chứa ký tự '~'.
    /// </summary>
    /// <summary>Url.Content: "~/path" -> "/path" (đường dẫn gốc ứng dụng); đường dẫn khác giữ nguyên.</summary>
    private static string UrlContent(string p) => p.StartsWith("~/") ? "/" + p.Substring(2) : p;

    private static string? FormatACAvatar(string? acAvatar, string inosBaseUrl)
    {
        // gate Sys_UserController.cs:191,203: !IsNullOrEmpty(ACAvatar) ? inosBaseUrl + ACAvatar.Split('~')[1] : null
        // LỖI NGUỒN GIỮ NGUYÊN: ACAvatar không chứa '~' -> IndexOutOfRangeException (PortExceptionFilter trả 400)
        if (string.IsNullOrEmpty(acAvatar)) return null;
        return inosBaseUrl + acAvatar.Split('~')[1];
    }
}
