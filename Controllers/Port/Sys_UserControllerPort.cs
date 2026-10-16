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
/// DTO request cho action Create (Gate Sys_UserController.cs:565-683, #329).
/// </summary>
public class Sys_UserCreateReq
{
    /// <summary>Body JSON lồng nếu client gửi qua biến strJson (Gate Sys_UserController.cs:565)</summary>
    
    /// <summary>Cờ tồn tại tài khoản: nếu "1", gán mật khẩu mặc định (Gate Sys_UserController.cs:595-603)</summary>
    public string? flagExist { get; set; }

    /// <summary>Cờ đổi avatar inos (Gate Sys_UserController.cs:658-663)</summary>
    public string? flagChangeAvatar { get; set; }

    /// <summary>Mã người dùng (PK) - Sys_User.cs:12</summary>
    public string? UserCode { get; set; }

    /// <summary>Tên người dùng - Sys_User.cs:16</summary>
    public string? UserName { get; set; }

    /// <summary>Mật khẩu - Sys_User.cs:18</summary>
    public string? UserPassword { get; set; }

    /// <summary>Số điện thoại - Sys_User.cs:22</summary>
    public string? PhoneNo { get; set; }

    /// <summary>Email - Sys_User.cs:24. Ở gate dòng 594: UserCode = EMail</summary>
    public string? EMail { get; set; }

    /// <summary>Mã số thuế - Sys_User.cs:26</summary>
    public string? MST { get; set; }

    /// <summary>Mã phòng ban - Sys_User.cs:30</summary>
    public string? DepartmentCode { get; set; }

    /// <summary>Vị trí - Sys_User.cs:32</summary>
    public string? Position { get; set; }

    /// <summary>Cờ quản trị đại lý - Sys_User.cs:40</summary>
    public string? FlagDLAdmin { get; set; }

    /// <summary>Cờ quản trị hệ thống - Sys_User.cs:42</summary>
    public string? FlagSysAdmin { get; set; }

    /// <summary>Cờ quản trị NNT - Sys_User.cs:44</summary>
    public string? FlagNNTAdmin { get; set; }

    /// <summary>Múi giờ - Sys_User.cs:67</summary>
    public string? ACTimeZone { get; set; }

    /// <summary>Ngôn ngữ - Sys_User.cs:61</summary>
    public string? ACLanguage { get; set; }

    /// <summary>Mã hệ thống khách hàng - Sys_User.cs:46</summary>
    public string? CustomerCodeSys { get; set; }

    /// <summary>User thực hiện thao tác (nếu không truyền qua Header WAUserCode)</summary>
    public string? WAUserCode { get; set; }

    /// <summary>Mã đại lý - Sys_User.cs:80, Gate Sys_UserController.cs:714</summary>
    public string? mdcpo_DLCPCode { get; set; }
}

/// <summary>
/// DTO request cho action Update (Gate Sys_UserController.cs:841-912, #331).
/// </summary>
public class Sys_UserUpdateReq
{
    public string? UserCode { get; set; }
    public string? UserName { get; set; }
    public string? PhoneNo { get; set; }
    public string? EMail { get; set; }
    public string? Position { get; set; }
    public string? Avatar { get; set; }
    public string? FlagDLAdmin { get; set; }
    public string? FlagSysAdmin { get; set; }
    public string? FlagNNTAdmin { get; set; }
    public string? FlagActive { get; set; }
    public string? WAUserCode { get; set; }
}

/// <summary>
/// DTO request cho action Delete (Gate Sys_UserController.cs:916-976, #332).
/// </summary>
public class Sys_UserDeleteReq
{
    public string? UserCode { get; set; }
}

/// <summary>
/// Controller Port 1:1 cho Sys_UserController — hạng mục #323 (Search), #324 (GetByUserCode), #325 (GetByMST), #327 (GetForCurrentUser), #328 (GetAllActive), #329 (Create), #330 (CreateCP), #331 (Update), #332 (Delete).
/// Nguồn Gate: D:\idocNet\2022.7.Loyalty\13.ClientGate\V10\idn.Loyalty.WebAPI\Controllers\Sys_UserController.cs:498-561, 565-683, 685-837, 841-912, 916-976
/// Nguồn API: D:\idocNet\2022.7.Loyalty\11.BackEnd\V10\idn.Loyalty.Biz.Web\Controllers\SysUserController.cs:199-270, 421-492, 569-640, 643-714
/// Nguồn Biz LIVE: D:\idocNet\2022.7.Loyalty\11.BackEnd\V10\idn.Loyalty.Biz\System.cs:8365-8478, 7987-8363 (Sys_User_Get_New20191104), 11222-11323 (WAS_Sys_User_Create), 9218-9391, 10160-10828 (Sys_User_CreateForNetworkX_New20200110), 12524-12617 (WAS_Sys_User_Update), 12243-12523 (Sys_User_Update_New20191226), 13092-13171 (WAS_Sys_User_Delete), 12619-12732 (Sys_User_Delete)
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

    /// <summary>
    /// Port 1:1 Create action từ gate Sys_UserController.cs:565-683 (#329).
    /// Chuỗi gọi: Gate Sys_UserController.cs:565 -> WA_Sys_User_Create (SysUserController.cs:421)
    ///            -> WAS_Sys_User_Create (System.cs:11222) -> Sys_User_Create_New20200110 (System.cs:9218)
    ///            -> Sys_User_CreateForNetworkX_New20200110 (System.cs:10160-10828).
    /// </summary>
    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Sys_UserCreateReq? req)
    {
        // 0. Body rỗng / null: gate Sys_UserController.cs:578-581 ném ServiceException("strJson null") -> Fail("Sys_User_Create", "Check.strJson", null)
        if (req == null)
        {
            return PortResult.Fail("Sys_User_Create", "Check.strJson", null);
        }

        req.UserCode = req.EMail; // gate Sys_UserController.cs:594: objSys_User.UserCode = objSys_User.EMail

        return await CreateXAsync(req);
    }

    /// <summary>
    /// Port 1:1 CreateCP action từ gate Sys_UserController.cs:685-837 (#330).
    /// Chuỗi gọi: Gate Sys_UserController.cs:685 -> WA_Sys_User_Create (SysUserController.cs:421)
    ///            -> WAS_Sys_User_Create (System.cs:11222) -> Sys_User_Create_New20200110 (System.cs:9218)
    ///            -> Sys_User_CreateForNetworkX_New20200110 (System.cs:10160-10828).
    /// Khác với Create (#329):
    /// 1. Gán EMail = UserCode (gate dòng 716), trong khi Create gán UserCode = EMail (gate dòng 594).
    /// 2. Hỗ trợ tra cứu MST qua mdcpo_DLCPCode trên Map_DealerCP_Org join Mst_DealerCP (FlagActive="1") và Mst_NNT (gate dòng 714, 726-756).
    /// 3. flagExist = "1" -> gán mật khẩu mặc định từ PassDefault hoặc "idocNet@123".
    /// 4. Dịch vụ ngoài Inos bỏ qua có chủ đích (GAP).
    /// </summary>
    [HttpPost("createcp")]
    public async Task<IActionResult> CreateCP([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Sys_UserCreateReq? req)
    {
        // 0. Body rỗng / null: gate Sys_UserController.cs:698-701 ném ServiceException("strJson null") -> Fail("Sys_User_Create", "Check.strJson", null)
        if (req == null)
        {
            return PortResult.Fail("Sys_User_Create", "Check.strJson", null);
        }

        // 1. Gate Sys_UserController.cs:716: objSys_User.EMail = objSys_User.UserCode
        req.EMail = req.UserCode;

        // 2. Tra cứu MST từ DLCPCode nếu được truyền (Gate Sys_UserController.cs:714, 726-756)
        var dlcpCode = req.mdcpo_DLCPCode;   // gate :714 chỉ đọc objSys_User.mdcpo_DLCPCode
        if (!string.IsNullOrWhiteSpace(dlcpCode))
        {
            var dlcpPattern = PortQuery.Equal(dlcpCode.Trim());
            var faPattern = PortQuery.Equal("1");

            var foundMst = await (
                from mdcpo in _db.Set<Map_DealerCP_Org>().AsNoTracking()
                join mdcp in _db.Set<Mst_DealerCP>().AsNoTracking()
                    on mdcpo.DLCPCode equals mdcp.DLCPCode
                join mo in _db.Set<Mst_Org>().AsNoTracking()
                    on mdcpo.OrgID equals mo.OrgID
                join mnnt in _db.Set<Mst_NNT>().AsNoTracking()
                    on mo.OrgID equals mnnt.OrgID
                where EF.Functions.Like((mdcpo.DLCPCode ?? "").ToLower(), dlcpPattern, PortQuery.Esc)
                   && EF.Functions.Like((mdcp.FlagActive ?? "").ToLower(), faPattern, PortQuery.Esc)
                orderby mdcpo.LogLUDTimeUTC descending, mnnt.MST ascending
                select mnnt.MST
            ).FirstOrDefaultAsync();

            if (!string.IsNullOrEmpty(foundMst))
            {
                req.MST = foundMst;
            }
        }

        return await CreateXAsync(req);
    }

    /// <summary>
    /// Hàm nghiệp vụ tạo người dùng Sys_User (Sys_User_CreateForNetworkX_New20200110, System.cs:10160-10828).
    /// </summary>
    public async Task<IActionResult> CreateXAsync(Sys_UserCreateReq req)
    {
        // 1. Chuẩn hoá đầu vào:
        // Gate dòng 594: objSys_User.UserCode = objSys_User.EMail (Create)
        // Gate dòng 716: objSys_User.EMail = objSys_User.UserCode (CreateCP)
        var rawUserCode = !string.IsNullOrEmpty(req.UserCode) ? req.UserCode : req.EMail;
        var strUserCode = StdParam(rawUserCode);

        // Guard 1: strUserCode rỗng hoặc null -> ném ngoại lệ Sys_User_Create_InvalidUserCode (System.cs:10235-10245)
        if (string.IsNullOrEmpty(strUserCode))
        {
            return PortResult.Fail("Sys_User_Create_InvalidUserCode", "Check.strUserCode", strUserCode);
        }

        // Bắt đầu transaction (System.cs:9275: _cf.db.BeginTransaction())
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // Guard 2: Sys_User_CheckDB (UserCode, FlagExistToCheck = No, FlagActiveListToCheck = "") (System.cs:10246-10252, 3840-3850)
            var uCodePattern = PortQuery.Equal(strUserCode);
            var existingUser = await _db.Set<Sys_User>().AsNoTracking()
                .FirstOrDefaultAsync(x => EF.Functions.Like((x.UserCode ?? "").ToLower(), uCodePattern, PortQuery.Esc));
            if (existingUser != null)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Sys_User_CheckDB_UserCodeExist", "Check.UserCodeExist", strUserCode);
            }

            // Guard 3: strUserName độ dài < 1 -> ném ngoại lệ Sys_User_Create_InvalidUserName (System.cs:10254-10264)
            var strUserName = string.Format("{0}", req.UserName).Trim();
            if (strUserName.Length < 1)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Sys_User_Create_InvalidUserName", "Check.strUserName", strUserName);
            }

            // Guard 4: strUserPassword độ dài < 1 -> ném ngoại lệ Sys_User_Create_InvalidUserPassword (System.cs:10266-10276)
            var strUserPassword = string.Format("{0}", req.UserPassword);
            var flagExist = req.flagExist ?? Request.Query["flagExist"].FirstOrDefault();
            if (!string.IsNullOrEmpty(flagExist) && flagExist.Equals("1", StringComparison.Ordinal))
            {
                var passwordDefault = _config?["PassDefault"];
                if (string.IsNullOrEmpty(passwordDefault))
                {
                    passwordDefault = "idocNet@123";
                }
                strUserPassword = passwordDefault;
            }
            if (strUserPassword.Length < 1)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Sys_User_Create_InvalidUserPassword", "Check.strUserPassword", strUserPassword);
            }

            // Guard 5: Mst_NNT_CheckDB (MST, FlagExistToCheck = Yes, FlagActiveList = Active) (System.cs:10280-10289, Master.cs:7813-7878)
            var strMST = StdParam(req.MST);
            if (string.IsNullOrEmpty(strMST))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_NNT_CheckDB_NNTNotFound", "Check.MST", strMST);
            }
            var mstPattern = PortQuery.Equal(strMST);
            var nnt = await _db.Set<Mst_NNT>().AsNoTracking()
                .FirstOrDefaultAsync(x => EF.Functions.Like((x.MST ?? "").ToLower(), mstPattern, PortQuery.Esc));
            if (nnt == null)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_NNT_CheckDB_NNTNotFound", "Check.MST", strMST);
            }
            if (!string.Equals(nnt.FlagActive?.Trim(), "1", StringComparison.Ordinal))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_NNT_CheckDB_FlagActiveNotMatched",
                    "Check.MST", strMST,
                    "Check.strFlagActiveListToCheck", "1",
                    "DB.FlagActive", nnt.FlagActive);
            }
            var strOrgID = StdParam(nnt.OrgID);

            // Guard 6: strDepartmentCode nếu không rỗng phải qua Mst_Department_CheckDB (Yes, Active) (System.cs:10293-10302, Master.cs:29275-29339)
            var strDepartmentCode = StdParam(req.DepartmentCode);
            if (!string.IsNullOrEmpty(strDepartmentCode))
            {
                var deptPattern = PortQuery.Equal(strDepartmentCode);
                var dept = await _db.Set<Mst_Department>().AsNoTracking()
                    .FirstOrDefaultAsync(x => EF.Functions.Like((x.DepartmentCode ?? "").ToLower(), deptPattern, PortQuery.Esc));
                if (dept == null)
                {
                    await tx.RollbackAsync();
                    return PortResult.Fail("Mst_Department_CheckDB_DepartmentNotFound", "Check.DepartmentCode", strDepartmentCode);
                }
                if (!string.Equals(dept.FlagActive?.Trim(), "1", StringComparison.Ordinal))
                {
                    await tx.RollbackAsync();
                    return PortResult.Fail("Mst_Department_CheckDB_FlagActiveNotMatched",
                        "Check.DepartmentCode", strDepartmentCode,
                        "Check.strFlagActiveListToCheck", "1",
                        "DB.FlagActive", dept.FlagActive);
                }
            }

            // Guard 7: strFlagSysAdmin độ dài < 1 -> ném Sys_User_Create_InvalidFlagSysAdmin (System.cs:10316-10326)
            var strFlagSysAdmin = StdFlag(req.FlagSysAdmin);
            if (strFlagSysAdmin.Length < 1)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Sys_User_Create_InvalidFlagSysAdmin", "Check.strFlagSysAdmin", strFlagSysAdmin);
            }

            // Sys_User_CheckDB(strWAUserCode, "1", "1") (System.cs:10330-10355): người dùng ĐANG ĐĂNG NHẬP (nguồn: orgUserUI.Email). Đích chưa có phiên đăng nhập
            // -> nhận qua header "WAUserCode" (tuỳ chọn). Có header: kiểm tra đúng như nguồn. KHÔNG có header (GAP): coi người tạo KHÔNG phải quản trị
            // (nFlagSysAdminRoot = 0) để không ai tự cấp được FlagSysAdmin = 1 khi chưa xác thực.
            var strWAUserCode = StdParam(Request.Headers["WAUserCode"].FirstOrDefault());
            Sys_User? userRoot = null;
            int nFlagSysAdminRoot = 0;
            if (!string.IsNullOrEmpty(strWAUserCode))
            {
                var waPattern = PortQuery.Equal(strWAUserCode);
                userRoot = await _db.Set<Sys_User>().AsNoTracking()
                    .FirstOrDefaultAsync(x => EF.Functions.Like((x.UserCode ?? "").ToLower(), waPattern, PortQuery.Esc));
                if (userRoot == null)
                {
                    await tx.RollbackAsync();
                    return PortResult.Fail("Sys_User_CheckDB_UserCodeNotFound", "Check.UserCodeNotFound", strWAUserCode);
                }
                if (!"1".Contains(userRoot.FlagActive ?? ""))
                {
                    await tx.RollbackAsync();
                    return PortResult.Fail("Sys_User_CheckDB_FlagActiveNotMatched",
                        "Check.UserCodeError", strWAUserCode,
                        "Check.FlagActiveListToCheck", "1",
                        "DB.FlagActive", userRoot.FlagActive);
                }
                nFlagSysAdminRoot = Convert.ToInt32(userRoot.FlagSysAdmin);
            }

            // Người tạo không phải quản trị -> không cho bật cờ FlagSysAdmin (:10339-10354)
            if (Convert.ToInt32(strFlagSysAdmin) == 1 && nFlagSysAdminRoot != 1)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Sys_User_Create_NNTNotAdmin",
                    "Check.strWAUserCode", strWAUserCode,
                    "Check.nFlagSysAdmin", nFlagSysAdminRoot);
            }

            // Lưu DB và tích hợp (System.cs:1204-1236):
            var networkId = _config?["Biz_NetworkID"] ?? "1001";
            var now = DateTime.UtcNow;
            var logLUBy = userRoot?.UserCode ?? "port";

            var newUser = new Sys_User
            {
                UserCode = strUserCode,
                NetworkID = networkId,
                UserName = strUserName,
                UserPassword = GetEncodedHash(strUserPassword),
                PhoneNo = !string.IsNullOrWhiteSpace(req.PhoneNo) ? req.PhoneNo.Trim() : null,
                EMail = string.Format("{0}", req.EMail).Trim(),
                MST = strMST,
                DepartmentCode = strDepartmentCode,
                Position = !string.IsNullOrWhiteSpace(req.Position) ? req.Position.Trim() : null,
                UUID = "-1", // TConst.InosMix.Default_Anonymous = -1
                FlagDLAdmin = StdFlag(req.FlagDLAdmin),
                FlagSysAdmin = strFlagSysAdmin,
                FlagNNTAdmin = StdFlag(req.FlagNNTAdmin),
                OrgID = strOrgID,
                CustomerCodeSys = StdParam(req.CustomerCodeSys),
                ACLanguage = StdStr(req.ACLanguage),
                ACTimeZone = StdStr(req.ACTimeZone),
                FlagActive = "1",
                LogLUDTimeUTC = now,
                LogLUBy = logLUBy
            };

            await _db.Set<Sys_User>().AddAsync(newUser);
            await _db.SaveChangesAsync();

            // GAP: Các dịch vụ Inos bên ngoài (Mst_ManageNotify_CreateX, Inos_AccountService_*, Inos_OrgService_*, Inos_LicService_*)
            // được bỏ qua có chủ đích do không có sẵn môi trường backend Inos tại đích (Bài học B.7 / A.16).

            await tx.CommitAsync();

            return Ok(new
            {
                Success = true,
                Data = (object?)null
            });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return PortResult.Fail("Sys_User_Create", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Port 1:1 Update action từ gate Sys_UserController.cs:841-912 (#331).
    /// Chuỗi gọi: gate Sys_UserController.Update -> WA_Sys_User_Update (SysUserController.cs:569-640)
    ///            -> WAS_Sys_User_Update (System.cs:12524-12617) -> Sys_User_Update_New20191226 (System.cs:12243-12523).
    ///
    /// Quy trình nghiệp vụ:
    ///   0. Body rỗng / null: gate Sys_UserController.cs:854-857 throw ServiceException("strJson null") -> 400 ErridnLoyalty.Sys_User_Update (Check.strJson).
    ///   1. Mở transaction: System.cs:12307 _cf.db.BeginTransaction(). Lỗi -> rollback + trả PortResult.Fail("Sys_User_Update", "Exception", ex.Message).
    ///   2. Chuỗi Ft_Cols_Upd do gate gửi lên: gate Sys_UserController.cs:870-879:
    ///      strFt_Cols_Upd gồm 5 trường: Sys_User.UserName, Sys_User.PhoneNo, Sys_User.FlagSysAdmin, Sys_User.FlagNNTAdmin, Sys_User.FlagActive,
    ///      Biz System.cs:12355: strFt_Cols_Upd = StdParam(objFt_Cols_Upd) (ToUpper).
    ///      Kiểm tra các cột cập nhật (System.cs:12374-12383):
    ///      - bUpd_UserName = strFt_Cols_Upd.Contains("SYS_USER.USERNAME")
    ///      - bUpd_PhoneNo = strFt_Cols_Upd.Contains("SYS_USER.PHONENO")
    ///      - bUpd_EMail = strFt_Cols_Upd.Contains("SYS_USER.EMAIL")
    ///      - bUpd_Position = strFt_Cols_Upd.Contains("SYS_USER.POSITION")
    ///      - bUpd_Avatar = strFt_Cols_Upd.Contains("SYS_USER.AVATAR")
    ///      - bUpd_FlagDLAdmin = strFt_Cols_Upd.Contains("SYS_USER.FLAGDLADMIN")
    ///      - bUpd_FlagNNTAdmin = strFt_Cols_Upd.Contains("SYS_USER.FLAGNNTADMIN")
    ///      - bUpd_FlagSysAdmin = strFt_Cols_Upd.Contains("SYS_USER.FLAGSYSADMIN")
    ///      - bUpd_FlagActive = strFt_Cols_Upd.Contains("SYS_USER.FLAGACTIVE")
    ///   3. Chuẩn hoá dữ liệu input (System.cs:12358-12370):
    ///      - strUserCode = StdParam(objUserCode)
    ///      - strUserName = string.Format("{0}", objUserName).Trim()
    ///      - strPhoneNo = string.Format("{0}", objPhoneNo).Trim()
    ///      - strEMail = string.Format("{0}", objEMail).Trim()
    ///      - strPosition = string.Format("{0}", objPosition)
    ///      - strAvatar = string.Format("{0}", objAvatar)
    ///      - strFlagDLAdmin = StdFlag(objFlagDLAdmin)
    ///      - strFlagNNTAdmin = StdFlag(objFlagNNTAdmin)
    ///      - strFlagSysAdmin = StdFlag(objFlagSysAdmin)
    ///      - strFlagActive = StdFlag(objFlagActive)
    ///   4. Thứ tự kiểm tra điều kiện (guards theo System.cs:12389-12407):
    ///      - Sys_User_CheckDB(strUserCode, TConst.Flag.Yes, "", out dtDB_Sys_User) (System.cs:12389-12395, 3809-3868):
    ///        Kiểm tra tồn tại UserCode trong DB. Nếu không tìm thấy -> ném ErridnLoyalty.Sys_User_CheckDB_UserCodeNotFound (Check.UserCodeNotFound).
    ///      - Nếu bUpd_UserName và strUserName.Length < 1 (System.cs:12397-12407):
    ///        -> ném ErridnLoyalty.Sys_User_Update_InvalidUserName (Check.strUserName).
    ///   5. Lưu DB (System.cs:12431-12454):
    ///      - Lưu các cột có trong Ft_Cols_Upd
    ///      - Cập nhật LogLUDTimeUTC = DateTime.UtcNow, LogLUBy = strWAUserCode / "port"
    ///   6. Commit transaction (System.cs:12486 TDALUtils.DBUtils.CommitSafety), trả Ok(new { Success = true, Data = (object?)null }) (gate :898-899).
    /// </summary>
    [HttpPost("update")]
    public async Task<IActionResult> Update([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Sys_UserUpdateReq? req)
    {
        // 0. Body rỗng / null: gate Sys_UserController.cs:854-857 throw ServiceException("strJson null") -> mã mặc định Sys_User_Update
        if (req == null)
        {
            return PortResult.Fail("Sys_User_Update", "Check.strJson", null);
        }

        // 1. Mở transaction: System.cs:12307 _cf.db.BeginTransaction()
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // 2. Chuỗi Ft_Cols_Upd do gate gửi lên: gate Sys_UserController.cs:870-879
            var strFt_Cols_Upd = "";
            var tblSysUser = "Sys_User.";
            strFt_Cols_Upd += string.Format("{0}{1},", tblSysUser, "UserName");
            //strFt_Cols_Upd += string.Format("{0}{1},", tblSysUser, "EMail");
            //strFt_Cols_Upd += string.Format("{0}{1},", tblSysUser, "Position");
            strFt_Cols_Upd += string.Format("{0}{1},", tblSysUser, "PhoneNo");
            //strFt_Cols_Upd += string.Format("{0}{1},", tblSysUser, "DepartmentCode");
            strFt_Cols_Upd += string.Format("{0}{1},", tblSysUser, "FlagSysAdmin");
            strFt_Cols_Upd += string.Format("{0}{1},", tblSysUser, "FlagNNTAdmin");
            strFt_Cols_Upd += string.Format("{0}{1},", tblSysUser, "FlagActive");

            // Chuẩn hoá Ft_Cols_Upd theo biz System.cs:12355: strFt_Cols_Upd = StdParam(objFt_Cols_Upd)
            strFt_Cols_Upd = StdParam(strFt_Cols_Upd) ?? "";

            // So khớp các cột được phép cập nhật (System.cs:12374-12383):
            bool bUpd_UserName = strFt_Cols_Upd.Contains("Sys_User.UserName".ToUpperInvariant());
            bool bUpd_PhoneNo = strFt_Cols_Upd.Contains("Sys_User.PhoneNo".ToUpperInvariant());
            bool bUpd_EMail = strFt_Cols_Upd.Contains("Sys_User.EMail".ToUpperInvariant());
            bool bUpd_Position = strFt_Cols_Upd.Contains("Sys_User.Position".ToUpperInvariant());
            bool bUpd_Avatar = strFt_Cols_Upd.Contains("Sys_User.Avatar".ToUpperInvariant());
            bool bUpd_FlagDLAdmin = strFt_Cols_Upd.Contains("Sys_User.FlagDLAdmin".ToUpperInvariant());
            bool bUpd_FlagNNTAdmin = strFt_Cols_Upd.Contains("Sys_User.FlagNNTAdmin".ToUpperInvariant());
            bool bUpd_FlagSysAdmin = strFt_Cols_Upd.Contains("Sys_User.FlagSysAdmin".ToUpperInvariant());
            bool bUpd_FlagActive = strFt_Cols_Upd.Contains("Sys_User.FlagActive".ToUpperInvariant());

            // 3. Chuẩn hoá dữ liệu input (System.cs:12358-12370):
            var strUserCode = StdParam(req.UserCode);
            var strUserName = string.Format("{0}", req.UserName).Trim();
            var strPhoneNo = string.Format("{0}", req.PhoneNo).Trim();
            var strEMail = string.Format("{0}", req.EMail).Trim();
            var strPosition = string.Format("{0}", req.Position);
            var strAvatar = string.Format("{0}", req.Avatar);
            var strFlagDLAdmin = StdFlag(req.FlagDLAdmin);
            var strFlagNNTAdmin = StdFlag(req.FlagNNTAdmin);
            var strFlagSysAdmin = StdFlag(req.FlagSysAdmin);
            var strFlagActive = StdFlag(req.FlagActive);

            // 4. Thứ tự kiểm tra điều kiện (guards theo System.cs:12389-12407):
            // (a) Sys_User_CheckDB: kiểm tra tồn tại UserCode (System.cs:12389-12395, 3809-3868)
            Sys_User? matchedUser = null;
            if (!string.IsNullOrEmpty(strUserCode))
            {
                var uCodePattern = PortQuery.Equal(strUserCode);
                matchedUser = await _db.Set<Sys_User>()
                    .FirstOrDefaultAsync(x => EF.Functions.Like((x.UserCode ?? "").ToLower(), uCodePattern, PortQuery.Esc));
            }

            if (matchedUser == null)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Sys_User_CheckDB_UserCodeNotFound", "Check.UserCodeNotFound", strUserCode);
            }

            // (b) Nếu bUpd_UserName và strUserName.Length < 1: ném Sys_User_Update_InvalidUserName (System.cs:12397-12407)
            if (bUpd_UserName && strUserName.Length < 1)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Sys_User_Update_InvalidUserName", "Check.strUserName", strUserName);
            }

            // 5. Lưu DB (System.cs:12431-12454):
            if (bUpd_UserName)
            {
                matchedUser.UserName = strUserName;
            }
            if (bUpd_PhoneNo)
            {
                matchedUser.PhoneNo = strPhoneNo;
            }
            if (bUpd_EMail)
            {
                matchedUser.EMail = strEMail;
            }
            if (bUpd_Position)
            {
                matchedUser.Position = strPosition;
            }
            if (bUpd_Avatar)
            {
                matchedUser.Avatar = strAvatar;
            }
            if (bUpd_FlagDLAdmin)
            {
                matchedUser.FlagDLAdmin = strFlagDLAdmin;
            }
            if (bUpd_FlagNNTAdmin)
            {
                matchedUser.FlagNNTAdmin = strFlagNNTAdmin;
            }
            if (bUpd_FlagSysAdmin)
            {
                matchedUser.FlagSysAdmin = strFlagSysAdmin;
            }
            if (bUpd_FlagActive)
            {
                matchedUser.FlagActive = strFlagActive;
            }

            // Cập nhật LogLUDTimeUTC (UtcNow) + LogLUBy (strWAUserCode / "port") (System.cs:12445-12446)
            var waUserCode = Request.Headers["WAUserCode"].FirstOrDefault() ?? "port";
            matchedUser.LogLUDTimeUTC = DateTime.UtcNow;
            matchedUser.LogLUBy = waUserCode;

            await _db.SaveChangesAsync();

            // 6. Commit transaction (System.cs:12486 TDALUtils.DBUtils.CommitSafety)
            await tx.CommitAsync();

            return Ok(new
            {
                Success = true,
                Data = (object?)null
            });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return PortResult.Fail("Sys_User_Update", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Port 1:1 Delete action từ gate Sys_UserController.cs:916-976 (#332).
    /// Chuỗi gọi: gate Sys_UserController.Delete -> WA_Sys_User_Delete (SysUserController.cs:643-714)
    ///            -> WAS_Sys_User_Delete (System.cs:13092-13171) -> Sys_User_Delete (System.cs:12619-12732).
    ///
    /// Quy trình nghiệp vụ:
    ///   0. Kiểm tra UserCode rỗng: Gate Sys_UserController.cs:935, 962:
    ///      Nếu UserCode rỗng/khoảng trắng hoặc body null -> ném ServiceException("invalid_usercode", "invalid_usercode") (chữ thường) -> trả về HTTP 400 ErridnLoyalty.invalid_usercode.
    ///   1. Mở transaction: System.cs:12646 _cf.db.BeginTransaction(). Rẽ nhánh lỗi bắt buộc await tx.RollbackAsync(). Lỗi Exception bắt trả mã Sys_User_Delete.
    ///   2. Chuẩn hoá: strUserCode = StdParam(req.UserCode) (Trim, ToUpper).
    ///   3. Kiểm tra DB: Sys_User_CheckDB(strUserCode, TConst.Flag.Yes, "") (System.cs:12669-12675, 3809-3868):
    ///      Yêu cầu tồn tại trong DB. Nếu không tìm thấy -> ném ErridnLoyalty.Sys_User_CheckDB_UserCodeNotFound (Check.UserCodeNotFound).
    ///   4. Xóa bảng phụ Sys_UserInGroup_Delete_ByUser(strUserCode) (System.cs:12677-12679, 14110-14125):
    ///      delete from Sys_UserInGroup where UserCode = @strUserCode.
    ///   5. Xóa bản ghi Sys_User (System.cs:12687-12693):
    ///      dtDB_Sys_User.Rows[0].Delete(); _cf.db.SaveData("Sys_User", dtDB_Sys_User).
    ///   6. Commit transaction (System.cs:12698 TDALUtils.DBUtils.CommitSafety), trả về Success = true, Data = null (Gate :957-958).
    /// </summary>
    [HttpPost("delete")]
    public async Task<IActionResult> Delete([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Sys_UserDeleteReq? req)
    {
        // 0. Kiểm tra UserCode rỗng: gate Sys_UserController.cs:935, 962
        // if (!TUtils.CUtils.IsNullOrEmpty(UserCode)) ... else throw new ServiceException("invalid_usercode", "invalid_usercode")
        if (req == null || string.IsNullOrEmpty(req.UserCode))
        {
            return PortResult.Fail("invalid_usercode", "Check.UserCode", req?.UserCode);
        }

        var err = await DeleteXAsync(req.UserCode);
        if (err != null)
        {
            return err;
        }

        // Gate Sys_UserController.cs:957-958: SetSuccess(), Data = null
        return Ok(new
        {
            Success = true,
            Data = (object?)null
        });
    }

    /// <summary>
    /// Thực hiện nghiệp vụ xóa 1 bản ghi Sys_User trong 1 transaction riêng biệt.
    /// Nguồn: biz Sys_User_Delete (System.cs:12619-12732).
    /// </summary>
    private async Task<IActionResult?> DeleteXAsync(string? rawUserCode)
    {
        // 1. Mở transaction (System.cs:12646 _cf.db.BeginTransaction())
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // 2. Chuẩn hoá UserCode theo CUtils.StdParam (System.cs:12664): Trim, ToUpper, rỗng -> null
            var strUserCode = StdParam(rawUserCode);

            // 3. Sys_User_CheckDB (System.cs:12669-12675, 3809-3868) với FlagExist = Yes
            // Kiểm tra tồn tại trong DB bằng EF.Functions.Like((x.UserCode ?? "").ToLower(), PortQuery.Equal(strUserCode), PortQuery.Esc)
            Sys_User? matchedUser = null;
            if (!string.IsNullOrEmpty(strUserCode))
            {
                var userCodePattern = PortQuery.Equal(strUserCode);
                matchedUser = await _db.Set<Sys_User>()
                    .FirstOrDefaultAsync(x => EF.Functions.Like((x.UserCode ?? "").ToLower(), userCodePattern, PortQuery.Esc));
            }

            if (matchedUser == null)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Sys_User_CheckDB_UserCodeNotFound", "Check.UserCodeNotFound", strUserCode ?? rawUserCode);
            }

            // 4. Xóa bảng phụ Sys_UserInGroup_Delete_ByUser(strUserCode) (System.cs:12677-12679, 14110-14125)
            var userCodeMatchPattern = PortQuery.Equal(strUserCode!);
            var usersInGroup = await _db.Set<Sys_UserInGroup>()
                .Where(x => EF.Functions.Like((x.UserCode ?? "").ToLower(), userCodeMatchPattern, PortQuery.Esc))
                .ToListAsync();
            if (usersInGroup.Count > 0)
            {
                _db.Set<Sys_UserInGroup>().RemoveRange(usersInGroup);
            }

            // 5. Xóa bản ghi Sys_User (System.cs:12687-12693)
            _db.Set<Sys_User>().Remove(matchedUser);
            await _db.SaveChangesAsync();

            // 6. Commit transaction (System.cs:12698)
            await tx.CommitAsync();

            return null;
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return PortResult.Fail("Sys_User_Delete", "Exception", ex.Message);
        }
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

    /// <summary>
    /// Chuẩn hoá cờ theo CUtils.StdFlag: "1" nếu là "1", mọi giá trị khác -> "0".
    /// </summary>
    private static string StdFlag(object? s)
    {
        return string.Equals(s?.ToString()?.Trim(), "1", StringComparison.Ordinal) ? "1" : "0";
    }

    /// <summary>
    /// Chuẩn hoá chuỗi văn bản theo CUtils.StdStr: Trim, Normalize, rỗng -> null.
    /// </summary>
    private static string? StdStr(object? s)
    {
        if (s == null) return null;
        var str = s.ToString()?.Trim().Normalize();
        return string.IsNullOrEmpty(str) ? null : str;
    }

    /// <summary>
    /// Hash mật khẩu bằng MD5 + muối 'idocNet' theo TUtils.CUtils.GetEncodedHash nguồn (12.Dev.Common/idn.Loyalty.Utils/Utils.cs:1823-1829).
    /// </summary>
    private static string GetEncodedHash(string password)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        var digest = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password + "idocNet"));
        var base64 = Convert.ToBase64String(digest);
        return base64.Substring(0, base64.Length - 2);
    }
}
