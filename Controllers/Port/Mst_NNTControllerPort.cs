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

namespace MiniLoyalty.Controllers.Port;

/// <summary>
/// Port 1:1 Mst_NNTController.cs — Mst_NNT (#172 Search, #173 GetAllActive, #174 GetByMST, #175 Create)
/// Chuỗi gọi nguồn:
///   gate: 13.ClientGate\V10\idn.Loyalty.WebAPI\Controllers\Mst_NNTController.cs:26-187 (Search), :189-289 (GetAllActive), :291-434 (GetByMST), :438-499 (Create), :666-698 (strWhereClause_Mst_NNT), :700-719 (strWhereClause_Mst_NNT_By_MST)
///   API:  11.BackEnd\V10\idn.Loyalty.Biz.Web\Controllers\MstNNTController.cs:20-89 (WA_Mst_NNT_Get), :1254-1324 (WA_Mst_NNT_CreateForNetworkCP)
///   biz:  11.BackEnd\V10\idn.Loyalty.Biz\Master.cs:7933-8030 (WAS_Mst_NNT_Get) -> :8126-8295 (Mst_NNT_Get) -> :21344-21524 (Mst_NNT_GetX),
///         11.BackEnd\V10\idn.Loyalty.Biz\Master.1.cs:3699-3812 (WAS_Mst_NNT_CreateForNetworkCP) -> :3813-4545 (Mst_NNT_CreateForNetworkCP) -> Master.cs:11937-12356 (Mst_NNT_CreateForNetworkCPX)
///
/// Bỏ qua có chủ đích (loại Đ):
/// - CheckGatewayAuthentication, CheckAuthorize (Mst_NNTController.cs:37-38, :198-199, :302-303, :457-458)
/// - WA_Sys_User_GetForCurrentUser (Mst_NNTController.cs:227)
/// - Sys_User_CheckAuthorize (Master.cs:8199-8212, Master.1.cs:3927-3940)
/// - Sys_User_GetAbilityViewOfUser / #tbl_Mst_NNT_ViewAbility (Master.cs:21382-21386, :12072-12073)
/// - RequestLog/ReturnLog (Master.cs:8180-8187, :8284-8292, Master.1.cs:3915-3922, :4535-4542)
/// - Tạo Org ngoại vi Inos_OrgService_*, OS_MstSv_*, Mst_Org_Create_X (Master.1.cs:3955-4052)
/// - Insert cấu hình mặc định bảng phụ Mst_Area, Mst_Brand, Mst_ProductGroup... (Master.1.cs:4108-4501)
/// </summary>
[ApiController]
[Route("api/mst_nnt")]
public class Mst_NNTControllerPort : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public Mst_NNTControllerPort(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    /// <summary>
    /// Port 1:1 Search action từ gate Mst_NNTController.cs:26-187.
    /// Request JSON: { OrgID, KeyWord, FlagActive, Ft_PageIndex = 0, Ft_PageSize = 10 }
    /// Trả về: Ok(new { Success = true, Data = new PageInfo&lt;...&gt;(...) })
    /// </summary>
    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_NNTSearchRequest? req)
    {
        req ??= new Mst_NNTSearchRequest();

        var pageIndex = req.Ft_PageIndex;
        var pageSize = req.Ft_PageSize;

        // Phân trang tại gate: Mst_NNTController.cs:74-75
        // Ft_RecordStart = (pageIndex * pageSize).ToString(), Ft_RecordCount = pageSize.ToString()
        long recordStart = (long)pageIndex * pageSize;
        long recordCount = pageSize;

        // Gọi GetXAsync tương ứng Mst_NNT_GetX (Master.cs:21344-21524)
        var (itemCount, dataList) = await GetXAsync(req.KeyWord, req.OrgID, req.FlagActive, recordStart, recordCount);

        // Tính PageCount theo logic tầng gate Mst_NNTController.cs:85, :165-172:
        // if (objRT_Mst_NNT.Lst_Mst_NNT != null && objRT_Mst_NNT.Lst_Mst_NNT.Count > 0)
        // {
        //     if (pageSize == 0) pageSize = PageSizeConfig; // PageSizeConfig = 10
        //     pageCount = (itemCount % pageSize == 0) ? itemCount / pageSize : itemCount / pageSize + 1;
        // }
        // Khi DataList rỗng (hoặc pageSize == 0), pageCount giữ nguyên 0 (dòng :50, :172).
        var pageCount = 0;
        if (dataList.Count > 0)
        {
            var ps = pageSize == 0 ? 10 : pageSize;
            pageCount = (itemCount % ps == 0) ? (itemCount / ps) : (itemCount / ps + 1);
        }

        var pageInfo = new PageInfo<Mst_NNTSearchResultItem>(dataList, itemCount, pageCount, pageIndex, pageSize);

        return Ok(new
        {
            Success = true,
            Data = pageInfo
        });
    }

    /// <summary>
    /// Port 1:1 GetAllActive action từ gate Mst_NNTController.cs:189-289.
    /// Chuỗi gọi nguồn:
    ///   gate: Mst_NNTController.cs:189-289 (GetAllActive), :666-698 (strWhereClause_Mst_NNT)
    ///   API:  MstNNTController.cs:20-89 (WA_Mst_NNT_Get)
    ///   biz:  Master.cs:7933-8030 (WAS_Mst_NNT_Get) -> :8126-8295 (Mst_NNT_Get) -> :21344-21524 (Mst_NNT_GetX)
    ///
    /// Nghiệp vụ:
    /// - Không nhận body/tham số (Mst_NNTController.cs:189).
    /// - Ft_RecordStart = "0", Ft_RecordCount = "123456000" (BaseController.cs:30-31, Mst_NNTController.cs:264-265).
    /// - Lọc FlagActive = "1" (Mst_NNTController.cs:235-238, :687-695 toán tử IN).
    /// - Bỏ qua xác thực session/user hiện tại (loại Đ), chạy theo nhánh SysAdmin (Mst_NNTController.cs:241-250): không lọc OrgID, không lọc KeyWord.
    /// - Sắp xếp theo mnnt.MST asc (Master.cs:21411).
    /// - Trả về mảng tràn List&lt;Mst_NNTSearchResultItem&gt;, nếu rỗng trả null (Mst_NNTController.cs:208, :271-276).
    /// </summary>
    [HttpPost("getallactive")]
    public async Task<IActionResult> GetAllActive()
    {
        // BaseController.cs:30-31: Ft_RecordStart = "0", Ft_RecordCount = "123456000"
        // Mst_NNTController.cs:235-238: strWhereClause_Mst_NNT(new Mst_NNTSearch { FlagActive = "1" })
        var (_, rows) = await GetXAsync(null, null, "1", 0, 123456000);

        // Mst_NNTController.cs:208, :271-276: List<Mst_NNT> listMst_NNT = null;
        // if (objRT_Mst_NNT.Lst_Mst_NNT != null && objRT_Mst_NNT.Lst_Mst_NNT.Count > 0) listMst_NNT = objRT_Mst_NNT.Lst_Mst_NNT;
        // objJsonResultEntry.Data = listMst_NNT;
        return Ok(new
        {
            Success = true,
            Data = rows.Count > 0 ? rows : null
        });
    }

    /// <summary>
    /// Port 1:1 GetByMST action từ gate Mst_NNTController.cs:291-434.
    /// Chuỗi gọi nguồn:
    ///   gate: Mst_NNTController.cs:291-434 (GetByMST), :700-719 (strWhereClause_Mst_NNT_By_MST)
    ///   API:  MstNNTController.cs:20-89 (WA_Mst_NNT_Get)
    ///   biz:  Master.cs:7933-8030 (WAS_Mst_NNT_Get) -> :8126-8295 (Mst_NNT_Get) -> :21344-21524 (Mst_NNT_GetX)
    ///
    /// Nghiệp vụ:
    /// - Nhận body JSON { MST } (Mst_NNTController.cs:291).
    /// - Kiểm tra tham số if (MST != null && !TUtils.CUtils.IsNullOrEmpty(MST)) (Mst_NNTController.cs:315).
    ///   Nếu MST rỗng hoặc null: không truy vấn backend, trả về Success = true, Data = null (Mst_NNTController.cs:314, :420-421).
    /// - Toán tử lọc MST: '=' (exact match) (Mst_NNTController.cs:707).
    /// - KHÔNG lọc OrgID và KHÔNG lọc FlagActive (Mst_NNTController.cs:317-320).
    /// - Lấy 1 bản ghi: Ft_RecordStart = "0", Ft_RecordCount = "1" (Mst_NNTController.cs:332-333).
    /// - Map dòng đầu tiên Lst_Mst_NNT[0] sang Data; nếu rỗng trả null (Mst_NNTController.cs:339-342, :421).
    /// </summary>
    [HttpPost("getbymst")]
    public async Task<IActionResult> GetByMST([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_NNTGetByMSTReq? req)
    {
        // Mst_NNTController.cs:314-315: if (MST != null && !TUtils.CUtils.IsNullOrEmpty(MST))
        if (string.IsNullOrWhiteSpace(req?.MST))
        {
            return Ok(new
            {
                Success = true,
                Data = (object?)null
            });
        }

        // Mst_NNTController.cs:317-336: Ft_RecordStart = "0", Ft_RecordCount = "1", chỉ lọc MST
        var (_, rows) = await GetXAsync(null, null, null, 0, 1, mst: req.MST);

        // Mst_NNTController.cs:339-342, :421: Lst_Mst_NNT[0] -> Data = objMst_NNT; nếu không có -> Data = null
        return Ok(new
        {
            Success = true,
            Data = rows.Count > 0 ? rows[0] : null
        });
    }

    /// <summary>
    /// Port 1:1 Create action từ gate Mst_NNTController.cs:438-499.
    /// Chuỗi gọi nguồn:
    ///   gate: 13.ClientGate\V10\idn.Loyalty.WebAPI\Controllers\Mst_NNTController.cs:438-499 (Create)
    ///   API:  11.BackEnd\V10\idn.Loyalty.Biz.Web\Controllers\MstNNTController.cs:1254-1324 (WA_Mst_NNT_CreateForNetworkCP)
    ///   biz:  11.BackEnd\V10\idn.Loyalty.Biz\Master.1.cs:3699-3812 (WAS_Mst_NNT_CreateForNetworkCP)
    ///         -> Master.1.cs:3813-4545 (Mst_NNT_CreateForNetworkCP)
    ///         -> Master.cs:11937-12356 (Mst_NNT_CreateForNetworkCPX)
    ///
    /// Nghiệp vụ:
    /// - Nhận body JSON ánh xạ sang Mst_NNTCreateRequest (Mst_NNTController.cs:446-449). Body rỗng -> ném lỗi hoặc trả 400 Check.strJson (dòng :453).
    /// - Mở 1 transaction cho toàn bộ nghiệp vụ (Master.1.cs:3912 _cf.db.BeginTransaction()). Lỗi -> rollback.
    /// - Chuẩn hoá đầu vào: MST dùng StdParam (Trim + ToUpper), NNTFullName dùng Trim() (Master.cs:12034, :12036).
    /// - Guard kiểm tra tính hợp lệ:
    ///   + MST rỗng/null -> 400 ErridnLoyalty.Mst_NNT_Create_InvalidMST (Master.cs:12078-12087).
    ///   + Kiểm tra trùng MST (Mst_NNT_CheckDB strFlagExistToCheck="0", Master.cs:12090-12097) -> 400 ErridnLoyalty.Mst_NNT_CheckDB_MSTExist.
    ///   + Kiểm tra MSTParent nếu có (Mst_NNT_CheckDB strFlagExistToCheck="1", FlagActive="1", Master.cs:12101-12112) -> 400 ErridnLoyalty.Mst_NNT_CheckDB_MSTParentNotExist.
    ///   + NNTFullName rỗng -> 400 ErridnLoyalty.Mst_NNT_Create_InvalidNNTFullName (Master.cs:12166-12175).
    /// - Gán giá trị mặc định cho Mst_NNT (Master.cs:12258-12306):
    ///   + MSTBUCode = "X", MSTBUPattern = "X", MSTLevel = "1"
    ///   + RegisterStatus = "FINISH", FlagActive = "1", TCTStatus = "1"
    ///   + NetworkID = _config["Biz_NetworkID"] ?? "1001", CreateBy = LogLUBy = "port"
    ///   + CreateDTimeUTC, LogLUDTimeUTC = DateTime.UtcNow
    /// - Post-check (Master.cs:12325-12351): đếm số lượng bản ghi thỏa mãn MSTLevel == '1' && MST != 'ALL' && (MSTParent == null || MSTParent == "").
    ///   Nếu TotalQtyNetWork > 1 -> 400 ErridnLoyalty.Mst_NNT_Create_InvalidTotalNetworkID.
    /// - Bỏ qua: tạo Org ngoại vi (Inos_OrgService_*, OS_MstSv_*) và insert bảng phụ (Mst_Area, Mst_Brand...) do chưa có ở đích.
    /// - Thành công: commit transaction, trả về Ok(new { Success = true, Data = (object?)null }) (Mst_NNTController.cs:485-486).
    /// </summary>
    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_NNTCreateRequest? req)
    {
        // 1. Kiểm tra body rỗng (gate Mst_NNTController.cs:447-453: if (string.IsNullOrEmpty(strJson)) throw new Exception("strJson null"))
        if (req == null)
        {
            return PortResult.Fail("Mst_NNT_Create", "Check.strJson", null);
        }

        var input = req.Mst_NNT ?? req;

        // 2. Mở transaction cho toàn bộ nghiệp vụ ghi (Master.1.cs:3912 _cf.db.BeginTransaction())
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // 3. Chuẩn hóa đầu vào (Master.cs:12034-12070)
            var strMST = StdParam(input.MST);
            var strOrgID = StdParam(input.OrgID);
            var strNNTFullName = input.NNTFullName?.Trim() ?? "";
            var strNNTShortName = input.NNTShortName?.Trim() ?? "";
            var strMSTParent = StdParam(input.MSTParent);
            var strProvinceCode = StdParam(input.ProvinceCode);
            var strDistrictCode = StdParam(input.DistrictCode);
            var strDLCode = StdParam(input.DLCode);
            var strNNTAddress = input.NNTAddress?.Trim() ?? "";
            var strNNTMobile = input.NNTMobile?.Trim() ?? "";
            var strNNTPhone = input.NNTPhone?.Trim() ?? "";
            var strNNTFax = input.NNTFax?.Trim() ?? "";
            var strPresentBy = input.PresentBy?.Trim() ?? "";
            var strBusinessRegNo = input.BusinessRegNo?.Trim() ?? "";
            var strNNTPosition = input.NNTPosition?.Trim() ?? "";
            var strPresentIDNo = input.PresentIDNo?.Trim() ?? "";
            var strPresentIDType = input.PresentIDType?.Trim() ?? "";
            var strGovTaxID = StdParam(input.GovTaxID);
            var strContactName = input.ContactName?.Trim() ?? "";
            var strContactPhone = input.ContactPhone?.Trim() ?? "";
            var strContactEmail = input.ContactEmail?.Trim() ?? "";
            var strWebsite = input.Website?.Trim() ?? "";
            var strCANumber = input.CANumber?.Trim() ?? "";
            var strCAOrg = input.CAOrg?.Trim() ?? "";
            var strCAEffDTimeUTCStart = StdStr(input.CAEffDTimeUTCStart);
            var strCAEffDTimeUTCEnd = StdStr(input.CAEffDTimeUTCEnd);
            var strPackageCode = input.PackageCode?.Trim() ?? "";
            var strCreatedDate = StdStr(input.CreatedDate);
            var strAccNo = input.AccNo?.Trim() ?? "";
            var strAccHolder = input.AccHolder?.Trim() ?? "";
            var strBankName = input.BankName?.Trim() ?? "";
            var strBizType = StdParam(input.BizType);
            var strBizFieldCode = StdParam(input.BizFieldCode);
            var strBizSizeCode = StdParam(input.BizSizeCode);
            var strDealerType = StdParam(input.DealerType);
            var strAreaCode = StdParam(input.AreaCode);

            // 4. Guard kiểm tra MST (Master.cs:12078-12087)
            if (string.IsNullOrEmpty(strMST))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_NNT_Create_InvalidMST", "Check.strMST", strMST);
            }

            // 5. Kiểm tra trùng MST trong DB (Mst_NNT_CheckDB với strFlagExistToCheck = "0", Master.cs:12090-12097)
            var existsMST = await _db.Set<Mst_NNT>()
                .AnyAsync(x => EF.Functions.Like((x.MST ?? "").ToLower(), PortQuery.Equal(strMST), PortQuery.Esc));
            if (existsMST)
            {
                await tx.RollbackAsync();
                // Mst_NNT_CheckDB(MST, "0") -> Mst_NNT_CheckDB_NNTExist (Master.cs:7813+, TError Mst_NNT_CheckDB_NNTExist)
                return PortResult.Fail("Mst_NNT_CheckDB_NNTExist", "Check.MST", strMST);
            }

            // 6. Kiểm tra MSTParent nếu có truyền (Mst_NNT_CheckDB với strFlagExistToCheck = "1", FlagActive = "1", Master.cs:12101-12112)
            if (!string.IsNullOrEmpty(strMSTParent))
            {
                var parent = await _db.Set<Mst_NNT>()
                    .FirstOrDefaultAsync(x => EF.Functions.Like((x.MST ?? "").ToLower(), PortQuery.Equal(strMSTParent), PortQuery.Esc));
                // Mst_NNT_CheckDB(MSTParent, "1", "1") (MasterData: :12101-12110): không có -> NNTNotFound; FlagActive không thuộc "1" -> FlagActiveNotMatched
                if (parent == null)
                {
                    await tx.RollbackAsync();
                    return PortResult.Fail("Mst_NNT_CheckDB_NNTNotFound", "Check.MST", strMSTParent);
                }
                if (!"1".Contains(parent.FlagActive ?? ""))
                {
                    await tx.RollbackAsync();
                    return PortResult.Fail("Mst_NNT_CheckDB_FlagActiveNotMatched", "Check.MST", strMSTParent,
                        "Check.strFlagActiveListToCheck", "1", "DB.FlagActive", parent.FlagActive);
                }
            }

            // 7. Guard kiểm tra NNTFullName (Master.cs:12166-12175)
            if (string.IsNullOrEmpty(strNNTFullName))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_NNT_Create_InvalidNNTFullName", "Check.strNNTFullName", strNNTFullName);
            }

            // 8. Tạo đối tượng Mst_NNT và gán các giá trị mặc định (Master.cs:12258-12306)
            var nowUtc = DateTime.UtcNow;
            var networkId = _config["Biz_NetworkID"] ?? "1001";
            var nnt = new Mst_NNT
            {
                MST = strMST,
                OrgID = strOrgID,
                NNTFullName = strNNTFullName,
                NNTShortName = strNNTShortName,
                NetworkID = networkId,
                MSTParent = strMSTParent,
                MSTBUCode = "X",
                MSTBUPattern = "X",
                MSTLevel = "1",
                ProvinceCode = strProvinceCode,
                DistrictCode = strDistrictCode,
                DLCode = strDLCode,
                NNTAddress = strNNTAddress,
                NNTMobile = strNNTMobile,
                NNTPhone = strNNTPhone,
                NNTFax = strNNTFax,
                PresentBy = strPresentBy,
                BusinessRegNo = strBusinessRegNo,
                NNTPosition = strNNTPosition,
                PresentIDNo = strPresentIDNo,
                PresentIDType = strPresentIDType,
                GovTaxID = strGovTaxID,
                ContactName = strContactName,
                ContactPhone = strContactPhone,
                ContactEmail = strContactEmail,
                Website = strWebsite,
                CANumber = strCANumber,
                CAOrg = strCAOrg,
                CAEffDTimeUTCStart = strCAEffDTimeUTCStart,
                CAEffDTimeUTCEnd = strCAEffDTimeUTCEnd,
                PackageCode = strPackageCode,
                CreatedDate = strCreatedDate,
                AccNo = strAccNo,
                AccHolder = strAccHolder,
                BankName = strBankName,
                BizType = strBizType,
                BizFieldCode = strBizFieldCode,
                BizSizeCode = strBizSizeCode,
                AreaCode = strAreaCode,
                DealerType = strDealerType,
                RegisterStatus = "FINISH", // TConst.RegisterStatus.Finish
                FlagActive = "1", // TConst.Flag.Active
                TCTStatus = "1", // TConst.Flag.Active
                CreateDTimeUTC = nowUtc.ToString("yyyy-MM-dd HH:mm:ss"),
                CreateBy = "port",
                Remark = input.Remark?.Trim(),
                LogLUDTimeUTC = nowUtc,
                LogLUBy = "port"
            };

            await _db.Set<Mst_NNT>().AddAsync(nnt);
            await _db.SaveChangesAsync();

            // 9. Post-check TotalQtyNetWork (Master.cs:12325-12351):
            // select count(t.MST) TotalQtyNetWork from Mst_NNT t where t.MSTLevel = '1' and (t.MST != 'ALL' and t.MSTParent is null)
            var totalQtyNetWork = await _db.Set<Mst_NNT>()
                .CountAsync(t => (t.MSTLevel == "1")
                              && !EF.Functions.Like((t.MST ?? "").ToLower(), "all")
                              && (t.MSTParent == null || t.MSTParent == ""));

            if (totalQtyNetWork > 1)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_NNT_Create_InvalidTotalNetworkID", "Check.TotalQtyNetWork", totalQtyNetWork);
            }

            // 10. Hoàn tất transaction và trả về thành công (gate Mst_NNTController.cs:485-486)
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
            return PortResult.Fail("Mst_NNT_Create", "Exception", ex.Message);
        }
    }

    private static string? StdParam(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim().ToUpperInvariant();
    private static string? StdStr(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    /// <summary>
    /// Hàm truy vấn tương ứng Mst_NNT_GetX (Master.cs:21344-21524):
    /// 1. Draft query (#tbl_Mst_NNT_Filter_Draft, :21401-21412) sắp xếp mnnt.MST asc.
    /// 2. Đếm tổng MyCount (:21415) trên toàn bộ draft.
    /// 3. Cắt trang (#tbl_Mst_NNT_Filter, :21418-21426).
    /// 4. Select cuối (:21442-21473) inner join Mst_NNT, left join Mst_Province, Mst_District, Mst_GovTaxID, Mst_Area.
    /// </summary>
    private async Task<(int itemCount, List<Mst_NNTSearchResultItem> rows)> GetXAsync(
        string? keyWord,
        string? orgId,
        string? flagActive,
        long recordStart,
        long recordCount,
        string? mst = null)
    {
        // 1. Khởi tạo truy vấn bảng Mst_NNT (#tbl_Mst_NNT_Filter_Draft, Master.cs:21401-21412)
        var draftQuery = _db.Set<Mst_NNT>().AsNoTracking();

        // 1b. Lọc MST: Mst_NNTController.cs:705-708
        // sbSql.AddWhereClause(Tbl_Mst_NNT + TblMst_NNT.MST, CUtils.StrValue(data.MST), "=")
        // Lọc cột MST với toán tử '=' (exact match, không wildcard).
        if (!string.IsNullOrWhiteSpace(mst))
        {
            draftQuery = draftQuery.Where(x => EF.Functions.Like(
                (x.MST ?? "").ToLower(),
                PortQuery.Equal(mst),
                PortQuery.Esc));
        }

        // 2. Lọc KeyWord: Mst_NNTController.cs:671-674
        // sbSql.AddWhereClause(Tbl_Mst_NNT + TblMst_NNT.NNTFullName, "%" + CUtils.StrValue(data.KeyWord) + "%", "like")
        // Lọc cột NNTFullName bằng like '%KeyWord%'. KHÔNG lọc trên cột MST hay mã.
        if (!string.IsNullOrWhiteSpace(keyWord))
        {
            draftQuery = draftQuery.Where(x => EF.Functions.Like(
                (x.NNTFullName ?? "").ToLower(),
                PortQuery.Contains(keyWord),
                PortQuery.Esc));
        }

        // 3. Lọc OrgID: Mst_NNTController.cs:675-678
        // sbSql.AddWhereClause(Tbl_Mst_NNT + TblMst_NNT.OrgID, CUtils.StrValue(data.OrgID), "=")
        // Lọc cột OrgID với toán tử '=' (exact match, không wildcard).
        if (!string.IsNullOrWhiteSpace(orgId))
        {
            draftQuery = draftQuery.Where(x => EF.Functions.Like(
                (x.OrgID ?? "").ToLower(),
                PortQuery.Equal(orgId),
                PortQuery.Esc));
        }

        // 4. Lọc FlagActive: Mst_NNTController.cs:687-695
        // Tách dấu phẩy CUtils.StrSplit(data.FlagActive, new[] { "," }) -> toán tử IN ('val1', 'val2')
        if (!string.IsNullOrWhiteSpace(flagActive))
        {
            var ds = flagActive.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => s.ToLowerInvariant())
                .ToList();
            if (ds.Count > 0)
            {
                draftQuery = draftQuery.Where(x => ds.Contains((x.FlagActive ?? "").ToLower()));
            }
        }

        // 5. Đếm MyCount trên bảng draft: Master.cs:21415
        // select Count(0) MyCount from #tbl_Mst_NNT_Filter_Draft t
        var itemCount = await draftQuery.CountAsync();

        // 6. Phân trang: Master.cs:21418-21426
        // t.MyIdxSeq >= @nFilterRecordStart and t.MyIdxSeq <= @nFilterRecordEnd
        // Nếu recordCount <= 0 hoặc recordStart < 0 -> trả về mảng rỗng [] (nguồn Ft_PageSize = 0)
        var rows = new List<Mst_NNTSearchResultItem>();

        if (recordCount > 0 && recordStart >= 0)
        {
            var take = (int)Math.Min(recordCount, int.MaxValue);
            var skip = (int)Math.Min(recordStart, int.MaxValue);

            // Sắp xếp theo mnnt.MST asc (Master.cs:21411: order by mnnt.MST asc)
            var pagedDraft = await draftQuery
                .OrderBy(x => x.MST)
                .Skip(skip)
                .Take(take)
                .ToListAsync();

            // Đánh số thứ tự 0-based MyIdxSeq (identity(bigint, 0, 1) MyIdxSeq tại Master.cs:21403)
            var pagedWithSeq = pagedDraft
                .Select((nnt, idx) => new { Nnt = nnt, MyIdxSeq = recordStart + idx })
                .ToList();

            // 7. Select cuối: Master.cs:21442-21473
            // left join Mst_Province mp on mnnt.ProvinceCode = mp.ProvinceCode
            // left join Mst_District md on mnnt.ProvinceCode = md.ProvinceCode and mnnt.DistrictCode = md.DistrictCode
            // left join Mst_GovTaxID mgt (chưa có trong repo đích -> null)
            // left join Mst_Area ma (chưa có trong repo đích -> null)
            var provinceCodes = pagedWithSeq
                .Select(x => x.Nnt.ProvinceCode)
                .Where(c => !string.IsNullOrEmpty(c))
                .Distinct()
                .ToList();

            var districtCodes = pagedWithSeq
                .Select(x => x.Nnt.DistrictCode)
                .Where(c => !string.IsNullOrEmpty(c))
                .Distinct()
                .ToList();

            var provinces = provinceCodes.Count > 0
                ? await _db.Set<Mst_Province>().AsNoTracking()
                    .Where(p => provinceCodes.Contains(p.ProvinceCode))
                    .ToListAsync()
                : new List<Mst_Province>();

            var districts = (provinceCodes.Count > 0 && districtCodes.Count > 0)
                ? await _db.Set<Mst_District>().AsNoTracking()
                    .Where(d => provinceCodes.Contains(d.ProvinceCode) && districtCodes.Contains(d.DistrictCode))
                    .ToListAsync()
                : new List<Mst_District>();

            rows = pagedWithSeq.Select(p =>
            {
                var it = p.Nnt;
                var mp = provinces.FirstOrDefault(x => string.Equals(x.ProvinceCode, it.ProvinceCode, StringComparison.OrdinalIgnoreCase));
                var md = districts.FirstOrDefault(x => string.Equals(x.ProvinceCode, it.ProvinceCode, StringComparison.OrdinalIgnoreCase)
                                                    && string.Equals(x.DistrictCode, it.DistrictCode, StringComparison.OrdinalIgnoreCase));

                return new Mst_NNTSearchResultItem
                {
                    MyIdxSeq = p.MyIdxSeq,
                    MST = it.MST,
                    OrgID = it.OrgID,
                    NNTFullName = it.NNTFullName,
                    NNTShortName = it.NNTShortName,
                    NetworkID = it.NetworkID,
                    MSTParent = it.MSTParent,
                    MSTBUCode = it.MSTBUCode,
                    MSTBUPattern = it.MSTBUPattern,
                    MSTLevel = it.MSTLevel,
                    ProvinceCode = it.ProvinceCode,
                    DistrictCode = it.DistrictCode,
                    DLCode = it.DLCode,
                    NNTAddress = it.NNTAddress,
                    NNTMobile = it.NNTMobile,
                    NNTPhone = it.NNTPhone,
                    NNTFax = it.NNTFax,
                    PresentBy = it.PresentBy,
                    BusinessRegNo = it.BusinessRegNo,
                    NNTPosition = it.NNTPosition,
                    PresentIDNo = it.PresentIDNo,
                    PresentIDType = it.PresentIDType,
                    GovTaxID = it.GovTaxID,
                    ContactName = it.ContactName,
                    ContactPhone = it.ContactPhone,
                    ContactEmail = it.ContactEmail,
                    Website = it.Website,
                    CANumber = it.CANumber,
                    CAOrg = it.CAOrg,
                    CAEffDTimeUTCStart = it.CAEffDTimeUTCStart,
                    CAEffDTimeUTCEnd = it.CAEffDTimeUTCEnd,
                    PackageCode = it.PackageCode,
                    CreatedDate = it.CreatedDate,
                    CreateDTime = it.CreateDTimeUTC,
                    CreateDTimeUTC = it.CreateDTimeUTC,
                    CreateBy = it.CreateBy,
                    AccNo = it.AccNo,
                    AccHolder = it.AccHolder,
                    BankName = it.BankName,
                    BizType = it.BizType,
                    BizFieldCode = it.BizFieldCode,
                    BizSizeCode = it.BizSizeCode,
                    DealerType = it.DealerType,
                    AreaCode = it.AreaCode,
                    RegisterStatus = it.RegisterStatus,
                    TCTStatus = it.TCTStatus,
                    FlagActive = it.FlagActive,
                    Remark = it.Remark,
                    LogLUDTimeUTC = it.LogLUDTimeUTC,
                    LogLUBy = it.LogLUBy,

                    // Cột mở rộng/join (Master.cs:21447-21456, Mst_NNTController.cs:145-162):
                    mgt_GovTaxID = null, // Mst_GovTaxID chưa có trong repo đích
                    mgt_GovTaxName = null, // Mst_GovTaxID chưa có trong repo đích
                    DepartmentCode = null,
                    DepartmentName = null,
                    UserName = null,
                    UserPassword = null,
                    UserPasswordRepeat = null,
                    mp_ProvinceCode = mp?.ProvinceCode,
                    mp_ProvinceName = mp?.ProvinceName,
                    md_DistrictCode = md?.DistrictCode,
                    md_DistrictName = md?.DistrictName,
                    QtyLicense = null,
                    CTSPath = null,
                    CTSPwd = null,
                    msio_OrderId = null,
                    ipmio_Status = null,
                    OrgIDSln = null,
                    ma_AreaName = null // Mst_Area chưa có trong repo đích
                };
            }).ToList();
        }

        return (itemCount, rows);
    }
}

/// <summary>
/// Request model cho action Search — Mst_NNTController.cs:26
/// </summary>
public class Mst_NNTSearchRequest
{
    public string? OrgID { get; set; }
    public string? KeyWord { get; set; }
    public string? FlagActive { get; set; }
    public int Ft_PageIndex { get; set; } = 0;
    public int Ft_PageSize { get; set; } = 10;
}

/// <summary>
/// Request model cho action GetByMST — Mst_NNTController.cs:291
/// </summary>
public class Mst_NNTGetByMSTReq
{
    public string? MST { get; set; }
}

/// <summary>
/// Request DTO cho action Create — Mst_NNTController.cs:438, :446-449
/// </summary>
public class Mst_NNTCreateRequest
{
    public Mst_NNTCreateRequest? Mst_NNT { get; set; }

    public string? MST { get; set; }
    public string? OrgID { get; set; }
    public string? NNTFullName { get; set; }
    public string? NNTShortName { get; set; }
    public string? NetworkID { get; set; }
    public string? MSTParent { get; set; }
    public string? ProvinceCode { get; set; }
    public string? DistrictCode { get; set; }
    public string? DLCode { get; set; }
    public string? NNTAddress { get; set; }
    public string? NNTMobile { get; set; }
    public string? NNTPhone { get; set; }
    public string? NNTFax { get; set; }
    public string? PresentBy { get; set; }
    public string? BusinessRegNo { get; set; }
    public string? NNTPosition { get; set; }
    public string? PresentIDNo { get; set; }
    public string? PresentIDType { get; set; }
    public string? GovTaxID { get; set; }
    public string? ContactName { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }
    public string? Website { get; set; }
    public string? CANumber { get; set; }
    public string? CAOrg { get; set; }
    public string? CAEffDTimeUTCStart { get; set; }
    public string? CAEffDTimeUTCEnd { get; set; }
    public string? PackageCode { get; set; }
    public string? CreatedDate { get; set; }
    public string? AccNo { get; set; }
    public string? AccHolder { get; set; }
    public string? BankName { get; set; }
    public string? BizType { get; set; }
    public string? BizFieldCode { get; set; }
    public string? BizSizeCode { get; set; }
    public string? DealerType { get; set; }
    public string? AreaCode { get; set; }
    public string? Remark { get; set; }
}

/// <summary>
/// Model từng dòng dữ liệu trả về trong DataList của Search:
/// 48 cột từ Mst_NNT + MyIdxSeq (0-based) + các cột join mở rộng (mgt_*, mp_*, md_*, ma_*...).
/// Mst_NNTController.cs:89-163, Master.cs:21442-21473.
/// </summary>
public class Mst_NNTSearchResultItem
{
    public long MyIdxSeq { get; set; }
    public string MST { get; set; } = "";
    public string? OrgID { get; set; }
    public string? NNTFullName { get; set; }
    public string? NNTShortName { get; set; }
    public string? NetworkID { get; set; }
    public string? MSTParent { get; set; }
    public string? MSTBUCode { get; set; }
    public string? MSTBUPattern { get; set; }
    public string? MSTLevel { get; set; }
    public string? ProvinceCode { get; set; }
    public string? DistrictCode { get; set; }
    public string? DLCode { get; set; }
    public string? NNTAddress { get; set; }
    public string? NNTMobile { get; set; }
    public string? NNTPhone { get; set; }
    public string? NNTFax { get; set; }
    public string? PresentBy { get; set; }
    public string? BusinessRegNo { get; set; }
    public string? NNTPosition { get; set; }
    public string? PresentIDNo { get; set; }
    public string? PresentIDType { get; set; }
    public string? GovTaxID { get; set; }
    public string? ContactName { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }
    public string? Website { get; set; }
    public string? CANumber { get; set; }
    public string? CAOrg { get; set; }
    public string? CAEffDTimeUTCStart { get; set; }
    public string? CAEffDTimeUTCEnd { get; set; }
    public string? PackageCode { get; set; }
    public string? CreatedDate { get; set; }
    public string? CreateDTime { get; set; }
    public string? CreateDTimeUTC { get; set; }
    public string? CreateBy { get; set; }
    public string? AccNo { get; set; }
    public string? AccHolder { get; set; }
    public string? BankName { get; set; }
    public string? BizType { get; set; }
    public string? BizFieldCode { get; set; }
    public string? BizSizeCode { get; set; }
    public string? DealerType { get; set; }
    public string? AreaCode { get; set; }
    public string? RegisterStatus { get; set; }
    public string? TCTStatus { get; set; }
    public string? FlagActive { get; set; }
    public string? Remark { get; set; }
    public DateTime? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }

    // Các cột mở rộng / join:
    public string? mgt_GovTaxID { get; set; }
    public string? mgt_GovTaxName { get; set; }
    public string? DepartmentCode { get; set; }
    public string? DepartmentName { get; set; }
    public string? UserName { get; set; }
    public string? UserPassword { get; set; }
    public string? UserPasswordRepeat { get; set; }
    public string? mp_ProvinceCode { get; set; }
    public string? mp_ProvinceName { get; set; }
    public string? md_DistrictCode { get; set; }
    public string? md_DistrictName { get; set; }
    public string? QtyLicense { get; set; }
    public string? CTSPath { get; set; }
    public string? CTSPwd { get; set; }
    public string? msio_OrderId { get; set; }
    public string? ipmio_Status { get; set; }
    public string? OrgIDSln { get; set; }
    public string? ma_AreaName { get; set; }
}
