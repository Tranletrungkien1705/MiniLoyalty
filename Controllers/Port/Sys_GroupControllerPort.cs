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

// ============================================================================================
// PORT 1:1 — Sys_GroupController:
//   - Search (#307)
//   - GetAllActive (#308)
//   - GetByGroupCode (#309)
//   - Create (#310)
//   - Update (#311)
//   - Delete (#312)
//   - DeleteMultiple (#313)
//   - Import (#314)
//
// Nguồn gate: Sys_GroupController.cs:27-135 (Search), 136-243 (GetAllActive), 244-310 (GetByGroupCode) -> WA_Sys_Group_Get
//   - Search: Nhận OrgID, KeyWord, FlagActive, Ft_PageIndex = 0, Ft_PageSize = 10
//   - GetAllActive: Nhận FlagActive = "1" (:163), OrgID đưa vào request nhưng không lọc
//   - Hàm lọc strWhereClause_Sys_Group (:917-932):
//     * KeyWord được map vào Sys_GroupUI.KeyWord (:56), nhưng hàm lọc chỉ kiểm tra data.GroupCode (:922-925).
//       Do GroupCode không được map -> điều kiện này KHÔNG BAO GIỜ được kích hoạt -> KeyWord BỎ QUA hoàn toàn.
//     * OrgID map vào Sys_GroupUI.OrgID (:57, :162) nhưng hàm lọc KHÔNG HỀ kiểm tra -> BỎ QUA hoàn toàn.
//     * FlagActive lọc bằng toán tử = nguyên chuỗi (:928).
//   - Gate Search map kết quả (:85-110): Lấy Lst_Sys_Group, map đếm user từ Lst_Sys_UserInGroup theo GroupCode.
//     Trả ra mảng object gồm 7 trường: GroupCode, GroupName, MST, FlagActive, LogLUDTimeUTC, LogLUBy, QtyUserInGroup.
//   - Gate GetAllActive (:183-228): Trả listSys_Group trần (null nếu rỗng), không map Sys_GroupExt hay QtyUserInGroup.
//   - Gate Create (:314-398): Body rỗng -> throw ServiceException("strJson null") -> 400.
//     Deser objSys_Group -> WA_Sys_Group_Create -> WAS_Sys_Group_Create -> Sys_Group_Create_New20191102 (zTemp.cs:1034-1251).
//     Trả Success = true, Data = null.
//   - Gate Update (:402-471): Body rỗng -> throw Exception("strJson null") -> 400.
//     Deser objSys_Group -> gán mặc định FlagActive="1" nếu null/rỗng (:415-418).
//     strFt_Cols_Upd = Sys_Group.GroupName,Sys_Group.FlagActive, (:438-439).
//     WA_Sys_Group_Update -> WAS_Sys_Group_Update -> Sys_Group_Update (System.cs:3019-3163).
//     Trả Success = true, Data = null.
//   - Gate Delete (:475-542): GroupCode rỗng -> throw ServiceException("invalid_GroupCode", "invalid_GroupCode") (:528).
//     Tạo RQ_Sys_Group -> WA_Sys_Group_Delete -> WAS_Sys_Group_Delete -> Sys_Group_Delete (System.cs:3453-3566).
//     Mở transaction -> Sys_Group_CheckDB (FlagExist = Yes) -> Sys_UserInGroup_Delete_ByGroup -> Delete Sys_Group -> Commit.
//     Trả Success = true, Data = null (:518-519).
//   - Gate DeleteMultiple (:544-608): Body rỗng -> throw ServiceException("strJson null") -> 400 ErridnLoyalty.Sys_Group_Delete.
//     Lặp tuần tự danh sách nhóm (:575-592), mỗi phần tử gọi WA_Sys_Group_Delete trong 1 transaction riêng biệt.
//     "xóa một phần" là hành vi nguồn: lỗi ở phần tử thứ k -> dừng lại, trả mã lỗi của phần tử thứ k, các phần tử trước k đã commit.
//     Mảng rỗng [] -> Success = true, Data = null (:594-595).
//   - Gate Import (:612-753): Nhận file upload qua multipart/form-data.
//     Đuôi file khác .xlsx -> throw ServiceException("File excel import không hợp lệ") (:636-640).
//     Đọc Excel: table rỗng -> throw ServiceException("File excel import không có dữ liệu") (:653, :738-740).
//     Số cột table != GetImportDicColumsTemplate().Count -> throw ServiceException("File excel import không hợp lệ") (:655-659).
//     Vòng lặp 1: kiểm tra rỗng GroupCode ("Mã nhóm người dùng không được trống"), GroupName ("Tên nhóm người dùng không được trống") cho TẤT CẢ các dòng trước khi tạo (:662-684).
//     Vòng lặp 2: gọi WA_Sys_Group_Create tuần tự cho từng dòng (mỗi dòng 1 transaction riêng, :705-731). Lỗi ở dòng k -> dừng ngay, trả lỗi dòng k (import một phần).
//     Thành công: Data = null (:733-735).
//
// Nguồn API: SysGroupController.cs:20-91 (Get), 94-165 (Create), 168-239 (Update), 242-313 (Delete)
// Nguồn biz: System.cs:2510-2625 (WAS_Sys_Group_Get), 2938-3018 (WAS_Sys_Group_Create), 3368-3451 (WAS_Sys_Group_Update), 3019-3163 (Sys_Group_Update), 3726-3805 (WAS_Sys_Group_Delete), 3453-3566 (Sys_Group_Delete)
//   - Draft (:2304-2319): select distinct identity(bigint, 0, 1) MyIdxSeq, sg.GroupCode ... order by sg.GroupCode asc
//   - Summary (:2322): select Count(0) MyCount from #tbl_Sys_Group_Filter_Draft
//   - Select cuối (:2356-2363): select t.MyIdxSeq, sg.* from #tbl_Sys_Group_Filter t inner join Sys_Group sg ... order by t.MyIdxSeq asc
//   - Create_New20191102 (zTemp.cs:1034-1251):
//     * StdParam(GroupCode), GroupName.Trim()
//     * GroupCode rỗng -> Sys_Group_Create_InvalidGroupCode
//     * GroupName.Length < 1 -> Sys_Group_Create_InvalidGroupName
//     * Sys_Group_CheckDB (GroupCode, FlagExistToCheck: No) -> Sys_Group_CheckDB_GroupCodeExist
//     * Sys_User_CheckDB (WAUserCode, Yes, Active) -> GAP
//     * Save DB: GroupCode, NetworkID, MST, GroupName, FlagActive="1", LogLUDTimeUTC=UtcNow, LogLUBy="port"
//   - Sys_Group_Update (System.cs:3019-3163):
//     * strFt_Cols_Upd = StdParam(objFt_Cols_Upd) (ToUpper), strGroupCode = StdParam(objGroupCode), strGroupName = string.Format("{0}", objGroupName).Trim(), strFlagActive = StdFlag(objFlagActive)
//     * bUpd_GroupName = strFt_Cols_Upd.Contains("SYS_GROUP.GROUPNAME"), bUpd_FlagActive = strFt_Cols_Upd.Contains("SYS_GROUP.FLAGACTIVE")
//     * Sys_Group_CheckDB(objGroupCode, TConst.Flag.Yes, "") -> Sys_Group_CheckDB_GroupCodeNotFound
//     * Nếu bUpd_GroupName và strGroupName.Length < 1 -> Sys_Group_Update_InvalidGroupName
//     * Save DB: GroupName (nếu bUpd_GroupName), FlagActive (nếu bUpd_FlagActive), LogLUDTimeUTC=UtcNow, LogLUBy="port"
//
// Bỏ qua có chủ đích (loại Đ):
//   - CheckGatewayAuthentication: Sys_GroupController.cs:36, :149, :334, SysGroupController.cs:43-48, :117-122
//   - CheckAuthorize: Sys_GroupController.cs:37, :150, :335, Sys_User_CheckAuthorize (System.cs:2236-2249, zTemp.cs:1094-1108)
//   - Sys_Access_CheckDenyV30: System.cs:2268-2272, zTemp.cs:1126-1130
//   - Ghi RequestLog/ReturnLog: System.cs:2217-2224, 2498-2505, zTemp.cs:1075-1082, 1241-1248
// ============================================================================================

/// <summary>
/// Port 1:1 Sys_GroupController.cs — Sys_Group
/// </summary>
[ApiController]
[Route("api/sys_group")]
[Produces("application/json")]
public class Sys_GroupControllerPort : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration? _config;

    public Sys_GroupControllerPort(AppDbContext db, IConfiguration? config = null)
    {
        _db = db;
        _config = config;
    }

    /// <summary>
    /// Port 1:1 Search action từ gate Sys_GroupController.cs:27-135 (#307).
    /// </summary>
    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Sys_GroupSearchReq? req)
    {
        var r = req ?? new Sys_GroupSearchReq();
        var pageIndex = r.Ft_PageIndex;
        var pageSize = r.Ft_PageSize;

        // Bắt đầu truy vấn trên bảng Sys_Group
        var q = _db.Set<Sys_Group>().AsNoTracking().AsQueryable();

        // Gate Sys_GroupController.cs:27 nhận OrgID, KeyWord, FlagActive
        // Hàm strWhereClause_Sys_Group (:917-932):
        // - KeyWord: map vào Sys_GroupUI.KeyWord nhưng hàm lọc chỉ check data.GroupCode -> BỎ QUA hoàn toàn
        // - OrgID: map vào Sys_GroupUI.OrgID nhưng hàm lọc KHÔNG HỀ check -> BỎ QUA hoàn toàn
        // - FlagActive: lọc bằng toán tử = nguyên chuỗi (nếu có giá trị)
        if (!string.IsNullOrEmpty(r.FlagActive))
        {
            var fa = PortQuery.Equal(r.FlagActive);
            q = q.Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), fa, PortQuery.Esc));
        }

        // Sắp xếp: Biz Sys_Group_Get_New20191102 (:2318) order by sg.GroupCode asc
        var ordered = q.OrderBy(x => x.GroupCode);

        // Tổng số bản ghi (select Count(0) MyCount from #tbl_Sys_Group_Filter_Draft)
        var itemCount = await ordered.CountAsync();

        // Phân trang theo bài học A.12 và A.7:
        // Ft_PageSize = 0 hoặc recordCount <= 0 hoặc recordStart < 0 -> DataList rỗng
        var recordStart = (long)pageIndex * pageSize;
        var recordCount = (long)pageSize;

        var pagedGroups = (recordCount <= 0 || recordStart < 0)
            ? new List<Sys_Group>()
            : await ordered.Skip((int)Math.Min(recordStart, int.MaxValue))
                           .Take((int)Math.Min(recordCount, int.MaxValue))
                           .ToListAsync();

        // Đếm user trong group từ bảng Sys_UserInGroup:
        // Gate (:85-110): map đếm số lượng user từ Lst_Sys_UserInGroup theo GroupCode
        var groupCodes = pagedGroups.Select(g => g.GroupCode).ToList();
        var userCounts = await _db.Set<Sys_UserInGroup>()
            .AsNoTracking()
            .Where(u => groupCodes.Contains(u.GroupCode))
            .GroupBy(u => u.GroupCode)
            .Select(g => new { GroupCode = g.Key, Qty = g.Count() })
            .ToDictionaryAsync(x => x.GroupCode, x => x.Qty);

        var dataList = pagedGroups.Select(item => new Sys_GroupSearchResultItem
        {
            GroupCode = item.GroupCode,
            GroupName = item.GroupName,
            MST = item.MST,
            FlagActive = item.FlagActive,
            LogLUDTimeUTC = item.LogLUDTimeUTC,
            LogLUBy = item.LogLUBy,
            // LỖI NGUỒN GIỮ NGUYÊN (Sys_GroupController.cs:87): Qty = x.FirstOrDefault().GroupCode.Count() = số KÝ TỰ của GroupCode (string.Count()), không phải số user;
            // chỉ có khi nhóm có >= 1 dòng Sys_UserInGroup (group by trên Lst_Sys_UserInGroup), ngược lại 0 (:90-95).
            QtyUserInGroup = userCounts.ContainsKey(item.GroupCode) ? item.GroupCode.Length : 0
        }).ToList();

        // Tính PageCount theo logic tầng gate (:113-117):
        // if (objRT_Sys_Group.Lst_Sys_Group != null && objRT_Sys_Group.Lst_Sys_Group.Count > 0)
        // {
        //     if (pageSize == 0) pageSize = PageSizeConfig; // PageSizeConfig = 10
        //     pageCount = (itemCount % pageSize == 0) ? itemCount / pageSize : itemCount / pageSize + 1;
        // }
        // Khi dataList rỗng -> pageCount = 0 (Bài học A.7).
        var pageCount = 0;
        if (dataList.Count > 0)
        {
            var ps = pageSize == 0 ? 10 : pageSize;
            pageCount = (itemCount % ps == 0) ? (itemCount / ps) : (itemCount / ps + 1);
        }

        var pageInfo = new PageInfo<Sys_GroupSearchResultItem>(dataList, itemCount, pageCount, pageIndex, pageSize);

        return Ok(new
        {
            Success = true,
            Data = pageInfo
        });
    }

    /// <summary>
    /// Port 1:1 GetAllActive action từ gate Sys_GroupController.cs:136-243 (#308).
    /// </summary>
    [HttpPost("getallactive")]
    public async Task<IActionResult> GetAllActive([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] object? req = null)
    {
        // Truy vấn Entity Sys_Group với FlagActive = "1"
        // Gate Sys_GroupController.cs:160-164 truyền FlagActive = "1"
        // Hàm lọc strWhereClause_Sys_Group (:926-929): FlagActive lọc bằng toán tử = nguyên chuỗi
        var q = _db.Set<Sys_Group>().AsNoTracking().AsQueryable();
        var fa = PortQuery.Equal("1");
        q = q.Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), fa, PortQuery.Esc));

        // Sắp xếp theo GroupCode asc (SQL System.cs:2318)
        var ordered = q.OrderBy(x => x.GroupCode);

        // Lấy từ chỉ mục 0 đến 123456000 (System.cs:2331-2332; gate Ft_RecordStart=0, Ft_RecordCount=123456000)
        var paged = await ordered.Skip(0).Take(123456000).ToListAsync();

        // Map DTO Sys_GroupGetAllActiveRes gồm 6 cột Sys_Group + MyIdxSeq (SQL System.cs:2305, 2356-2363)
        // GetAllActive gate không map Sys_GroupExt hay join Sys_UserInGroup nên không trả QtyUserInGroup
        var rows = paged.Select((x, i) => new Sys_GroupGetAllActiveRes
        {
            MyIdxSeq = i,
            GroupCode = x.GroupCode,
            GroupName = x.GroupName,
            FlagActive = x.FlagActive,
            LogLUDTimeUTC = x.LogLUDTimeUTC,
            LogLUBy = x.LogLUBy,
            MST = x.MST
        }).ToList();

        // Gate Sys_GroupController.cs:183, 220-230:
        // Data = listSys_Group (danh sách trần, null nếu rỗng)
        return Ok(new
        {
            Success = true,
            Data = rows.Count > 0 ? rows : null
        });
    }

    /// <summary>
    /// Port 1:1 GetByGroupCode action từ gate Sys_GroupController.cs:244-310 (#309).
    /// </summary>
    [HttpPost("getbygroupcode")]
    public async Task<IActionResult> GetByGroupCode([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Sys_GroupGetByGroupCodeReq? req)
    {
        // Gate Sys_GroupController.cs:265: nếu GroupCode rỗng -> trả Data = null (không gọi DB)
        if (req == null || string.IsNullOrWhiteSpace(req.GroupCode))
        {
            return Ok(new
            {
                Success = true,
                Data = (object?)null
            });
        }

        var groupCode = req.GroupCode.Trim();

        // Gate :267-289, Biz WAS_Sys_Group_Get System.cs:2550 -> Sys_Group_Get_New20191102 :2318
        // Truy vấn 1 bản ghi Sys_Group theo GroupCode bằng Like với toán tử = (không phân biệt hoa/thường)
        var matchedGroup = await _db.Set<Sys_Group>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => EF.Functions.Like((x.GroupCode ?? "").ToLower(), PortQuery.Equal(groupCode), PortQuery.Esc));

        // Gate :290-297: Nếu không tìm thấy nhóm -> Data = null
        if (matchedGroup == null)
        {
            return Ok(new
            {
                Success = true,
                Data = (object?)null
            });
        }

        // Biz System.cs:2374-2393: Truy vấn Sys_UserInGroup theo GroupCode
        // Left join Sys_Group (sg) để lấy sg_GroupCode, sg_GroupName, sg_FlagActive
        // Left join Sys_User (su) để lấy su_UserCode, su_UserName, su_FlagSysAdmin, su_FlagActive
        // (Entity Sys_User chưa có trong repo -> gán null cho các cột su_... và ghi GAP)
        var groupCodePattern = PortQuery.Equal(matchedGroup.GroupCode);
        var usersInGroup = await (from suig in _db.Set<Sys_UserInGroup>().AsNoTracking()
                                  where EF.Functions.Like((suig.GroupCode ?? "").ToLower(), groupCodePattern, PortQuery.Esc)
                                  join sg in _db.Set<Sys_Group>().AsNoTracking()
                                      on suig.GroupCode equals sg.GroupCode into sgJoin
                                  from sg in sgJoin.DefaultIfEmpty()
                                  select new Sys_UserInGroupRes
                                  {
                                      UserCode = suig.UserCode,
                                      GroupCode = suig.GroupCode,
                                      LogLUDTimeUTC = suig.LogLUDTimeUTC,
                                      LogLUBy = suig.LogLUBy,
                                      su_UserCode = null,
                                      su_UserName = null,
                                      su_FlagSysAdmin = null,
                                      su_FlagActive = null,
                                      sg_GroupCode = sg != null ? sg.GroupCode : null,
                                      sg_GroupName = sg != null ? sg.GroupName : null,
                                      sg_FlagActive = sg != null ? sg.FlagActive : null
                                  }).ToListAsync();

        var res = new Sys_GroupGetByGroupCodeRes
        {
            objSys_Group = matchedGroup,
            Lst_Sys_UserInGroup = usersInGroup
        };

        return Ok(new
        {
            Success = true,
            Data = res
        });
    }

    /// <summary>
    /// Port 1:1 Create action từ gate Sys_GroupController.cs:314-398 (#310).
    /// Chuỗi gọi: gate Sys_GroupController.Create -> WA_Sys_Group_Create -> WAS_Sys_Group_Create -> Sys_Group_Create_New20191102 (zTemp.cs:1034-1251).
    ///
    /// Quy trình nghiệp vụ:
    ///   0. Body rỗng / null: gate Sys_GroupController.cs:324-331 ném ServiceException("strJson null") -> 400 ErridnLoyalty.Sys_Group_Create (P2).
    ///   1. Mở transaction: zTemp.cs:1072 _cf.db.BeginTransaction(). Lỗi -> rollback + trả lỗi.
    ///   2. Chuẩn hoá: strGroupCode = StdParam(objGroupCode), strGroupName = string.Format("{0}", objGroupName).Trim().
    ///   3. Kiểm tra rỗng đầu vào:
    ///      - strGroupCode rỗng hoặc null -> ErridnLoyalty.Sys_Group_Create_InvalidGroupCode (zTemp.cs:1142-1151).
    ///      - strGroupName dài &lt; 1 -> ErridnLoyalty.Sys_Group_Create_InvalidGroupName (zTemp.cs:1161-1170).
    ///   4. Kiểm tra DB (thứ tự):
    ///      - Sys_Group_CheckDB (GroupCode, FlagExistToCheck: No) -> nếu đã tồn tại ném ErridnLoyalty.Sys_Group_CheckDB_GroupCodeExist (zTemp.cs:1153-1159, System.cs:1577-1587).
    ///      - Sys_User_CheckDB (WAUserCode, FlagExistToCheck: Yes, FlagActive: Active) -> kiểm tra user thực hiện (zTemp.cs:1173-1184).
    ///        (GAP: Entity Sys_User chưa có trong hệ thống đích, bỏ qua kiểm tra chi tiết và lấy MST từ req nếu có).
    ///   5. Lưu DB (zTemp.cs:1189-1210): GroupCode, NetworkID (lấy từ config _config["Biz_NetworkID"] ?? "1001"),
    ///      MST, GroupName, FlagActive = "1" (TConst.Flag.Yes), LogLUDTimeUTC = DateTime.UtcNow, LogLUBy = "port".
    ///   6. Commit transaction, trả Ok(new { Success = true, Data = (object?)null }) (gate :384-385).
    /// </summary>
    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Sys_GroupCreateReq? req)
    {
        // 0. Body rỗng: gate Sys_GroupController.cs:324-331 if (string.IsNullOrEmpty(strJson)) throw new ServiceException("strJson null")
        if (req == null)
        {
            return PortResult.Fail("Sys_Group_Create", "Check.strJson", null);   // gate :324-330 ServiceException("strJson null") -> mã mặc định Sys_Group_Create
        }

        var err = await CreateXAsync(req.GroupCode, req.GroupName, req.MST);
        if (err != null) return err;

        return Ok(new
        {
            Success = true,
            Data = (object?)null
        });
    }

    /// <summary>
    /// Port 1:1 Sys_GroupController.Import (gate Sys_GroupController.cs:612-753, #314) -> WA_Sys_Group_Create -> WAS_Sys_Group_Create -> Sys_Group_Create_New20191102 (zTemp.cs:1034-1251).
    /// - Nhận file upload qua multipart/form-data.
    /// - Tham số orgId nhận từ form ([FromForm] string? orgId) hoặc header OrgID (do nguồn lấy OrgID từ session user orgUserUI.OrgId :626).
    /// - Thiếu file hoặc file rỗng hoặc đuôi khác .xlsx -> Fail("Sys_Group_Import", "Message", "File excel import không hợp lệ") (gate :636-641).
    /// - Đọc sheet bằng PortExcel.ReadSheet với 3 cột: GroupCode, GroupName, MST (giả định theo đề bài #314 do excerpt thiếu GetImportDicColumsTemplate; thực tế nguồn gate :885-893 có 2 cột GroupCode, GroupName).
    /// - Không có dòng dữ liệu (rows.Count == 0) -> Fail("Sys_Group_Import", "Message", "File excel import không có dữ liệu") (gate :653, :738-740).
    /// - Số cột dòng tiêu đề != 3 -> Fail("Sys_Group_Import", "Message", "File excel import không hợp lệ") (gate :655-659).
    /// - Vòng lặp 1 (kiểm tra trước, gate :662-684):
    ///   * dr[TblSys_Group.GroupCode] rỗng -> Fail("Sys_Group_Import", "Message", "Mã nhóm người dùng không được trống") (:665-668).
    ///   * dr[TblSys_Group.GroupName] rỗng -> Fail("Sys_Group_Import", "Message", "Tên nhóm người dùng không được trống") (:670-673).
    ///   Nếu lỗi ở bước này: dừng ngay, KHÔNG có dòng nào được tạo (kiểm trước).
    /// - Vòng lặp 2 (lưu dữ liệu từng dòng, gate :705-731):
    ///   Gọi CreateXAsync(groupCode, groupName, null) độc lập cho từng dòng (1 transaction riêng cho mỗi dòng).
    ///   Lỗi ở dòng k -> trả về lỗi của dòng k ngay lập tức (hành vi import một phần: các dòng trước k đã commit thành công, dòng k và các dòng sau không được tạo).
    /// - Thành công: trả { Success = true, Data = null } (gate :733-735).
    /// </summary>
    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Import([FromForm] string? orgId, IFormFile? file)
    {
        // 1. Kiểm tra file upload: gate Sys_GroupController.cs:636-641
        if (file == null || file.Length == 0)
        {
            return PortResult.Fail("Sys_Group_Import", "Message", "File excel import không hợp lệ");
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".xlsx")
        {
            return PortResult.Fail("Sys_Group_Import", "Message", "File excel import không hợp lệ");
        }

        // 2. Đọc bảng tính bằng PortExcel.ReadSheet (gate Sys_GroupController.cs:652)
        // Template 2 cột theo NGUỒN (quyết định Claude 16/10, xem conflicts\314_conflict.md): GroupCode, GroupName; MST không lấy từ Excel (biz lấy MST của người dùng đăng nhập).
        // (Lưu ý: Mã nguồn thật Sys_GroupController.cs:885-893 GetImportDicColumsTemplate trả về 2 cột: GroupCode, GroupName).
        int columnCount;
        List<Dictionary<string, string?>> rows;
        try
        {
            using var stream = file.OpenReadStream();
            (columnCount, rows) = PortExcel.ReadSheet(stream, new[] { "GroupCode", "GroupName" });   // GetImportDicColumsTemplate (Sys_GroupController.cs:885-893): GroupCode, GroupName (FlagActive bị comment)
        }
        catch
        {
            return PortResult.Fail("Sys_Group_Import", "Message", "File excel import không hợp lệ");
        }

        // Kiểm tra có dữ liệu hay không (gate Sys_GroupController.cs:653, :738-740)
        if (rows.Count == 0)
        {
            return PortResult.Fail("Sys_Group_Import", "Message", "File excel import không có dữ liệu");
        }

        // Kiểm tra số cột dòng tiêu đề so với template (3 cột theo giả định đề bài #314, gate :655-659)
        if (columnCount != 2)   // GetImportDicColumsTemplate().Count = 2
        {
            return PortResult.Fail("Sys_Group_Import", "Message", "File excel import không hợp lệ");
        }

        // 3. Vòng lặp 1: Kiểm tra TẤT CẢ các dòng TRƯỚC khi tạo (#region["Check null"] gate :662-684)
        foreach (var r in rows)
        {
            var groupCode = r.GetValueOrDefault("GroupCode");
            if (string.IsNullOrWhiteSpace(groupCode))
            {
                return PortResult.Fail("Sys_Group_Import", "Message", "Mã nhóm người dùng không được trống");
            }

            var groupName = r.GetValueOrDefault("GroupName");
            if (string.IsNullOrWhiteSpace(groupName))
            {
                return PortResult.Fail("Sys_Group_Import", "Message", "Tên nhóm người dùng không được trống");
            }
        }

        // 4. Vòng lặp 2: Lưu từng dòng tuần tự (gate Sys_GroupController.cs:705-731)
        // Mỗi dòng gọi CreateXAsync trong 1 transaction riêng biệt. Gặp lỗi đầu tiên -> return lỗi đó luôn (import một phần).
        foreach (var r in rows)
        {
            var groupCode = r.GetValueOrDefault("GroupCode");
            var groupName = r.GetValueOrDefault("GroupName");
            var err = await CreateXAsync(groupCode, groupName, null);
            if (err != null)
            {
                return err;
            }
        }

        return Ok(new
        {
            Success = true,
            Data = (object?)null
        });
    }

    /// <summary>
    /// Thực hiện nghiệp vụ tạo mới 1 nhóm người dùng trong 1 transaction riêng biệt.
    /// Nguồn: biz Sys_Group_Create_New20191102 (zTemp.cs:1034-1251).
    /// Dùng chung cho action Create (#310) và Import (#314).
    /// Trả về null nếu thành công; trả về IActionResult (PortResult.Fail) nếu thất bại (transaction đã rollback).
    /// </summary>
    private async Task<IActionResult?> CreateXAsync(string? groupCode, string? groupName, string? mst)
    {
        // 1. Mở transaction (zTemp.cs:1072 _cf.db.BeginTransaction())
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // 2. Chuẩn hoá tham số (zTemp.cs:1135-1136, TUtils.CUtils):
            // GroupCode -> StdParam (Trim + ToUpper, rỗng -> null)
            // GroupName -> string.Format("{0}", objGroupName).Trim()
            var strGroupCode = StdParam(groupCode);
            var strGroupName = string.Format("{0}", groupName).Trim();

            // 3. Kiểm tra chuỗi rỗng:
            // strGroupCode rỗng hoặc null -> ErridnLoyalty.Sys_Group_Create_InvalidGroupCode (zTemp.cs:1142-1151)
            if (string.IsNullOrEmpty(strGroupCode))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Sys_Group_Create_InvalidGroupCode", "Check.strGroupCode", strGroupCode);
            }

            // 4. Kiểm tra DB (Sys_Group_CheckDB: GroupCode phải CHƯA tồn tại trong Sys_Group, zTemp.cs:1153-1159):
            var groupCodePattern = PortQuery.Equal(strGroupCode);
            var existGroup = await _db.Set<Sys_Group>()
                .AsNoTracking()
                .AnyAsync(x => EF.Functions.Like((x.GroupCode ?? "").ToLower(), groupCodePattern, PortQuery.Esc));
            if (existGroup)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Sys_Group_CheckDB_GroupCodeExist", "Check.GroupCodeExist", strGroupCode);
            }

            // strGroupName dài < 1 -> ErridnLoyalty.Sys_Group_Create_InvalidGroupName (zTemp.cs:1161-1170)
            if (strGroupName.Length < 1)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Sys_Group_Create_InvalidGroupName", "Check.strGroupName", strGroupName);
            }

            // Sys_User_CheckDB: User thực hiện phải tồn tại và active (zTemp.cs:1175-1181)
            // Lấy MST từ Sys_User (zTemp.cs:1183)
            // GAP: Entity Sys_User chưa có trong hệ thống đích (chưa port #323-336), gán MST = mst nếu có, ngược lại null.
            var strMST = !string.IsNullOrWhiteSpace(mst) ? StdParam(mst) : null;

            // NetworkID từ cấu hình theo luật A.13 / đề bài line 66 (bảng Sys_Group nguồn không có cột NetworkID)
            var networkID = _config?["Biz_NetworkID"] ?? "1001";

            // 5. Lưu DB (zTemp.cs:1189-1210):
            var now = DateTime.UtcNow;
            var newGroup = new Sys_Group
            {
                GroupCode = strGroupCode,
                GroupName = strGroupName,
                FlagActive = "1",
                MST = strMST,
                LogLUDTimeUTC = now,
                LogLUBy = "port"
            };

            await _db.Set<Sys_Group>().AddAsync(newGroup);
            await _db.SaveChangesAsync();

            // 6. Commit transaction (zTemp.cs:1214)
            await tx.CommitAsync();

            return null;
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return PortResult.Fail("Sys_Group_Create", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Port 1:1 Update action từ gate Sys_GroupController.cs:402-471 (#311).
    /// Chuỗi gọi: gate Sys_GroupController.Update -> WA_Sys_Group_Update -> WAS_Sys_Group_Update -> Sys_Group_Update (System.cs:3019-3163).
    ///
    /// Quy trình nghiệp vụ:
    ///   0. Body rỗng / null: gate Sys_GroupController.cs:420-423 ném Exception("strJson null") -> 400 ErridnLoyalty.Sys_Group_Update (Check.strJson).
    ///   1. Mở transaction: System.cs:3054 _cf.db.BeginTransaction(). Lỗi -> rollback + trả PortResult.Fail("Sys_Group_Update", "Exception", ex.Message).
    ///   2. Chuỗi Ft_Cols_Upd do gate gửi lên: gate Sys_GroupController.cs:436-439:
    ///      strFt_Cols_Upd = Sys_Group.GroupName,Sys_Group.FlagActive,
    ///      Biz System.cs:3072: strFt_Cols_Upd = StdParam(objFt_Cols_Upd) (ToUpper).
    ///      Kiểm tra các cột cập nhật (System.cs:3080-3081):
    ///      - bUpd_GroupName = strFt_Cols_Upd.Contains("SYS_GROUP.GROUPNAME")
    ///      - bUpd_FlagActive = strFt_Cols_Upd.Contains("SYS_GROUP.FLAGACTIVE")
    ///   3. Chuẩn hoá dữ liệu input (System.cs:3075-3077):
    ///      - strGroupCode = StdParam(objGroupCode)
    ///      - strGroupName = string.Format("{0}", objGroupName).Trim()
    ///      - FlagActive: Gate Sys_GroupController.cs:415-418 nếu IsNullOrEmpty(FlagActive) -> mặc định "1" (CẢNH BÁO / HÀNH VI NGUỒN).
    ///        Sau đó qua StdFlag(objFlagActive) (System.cs:3077: "1" nếu là "1", mọi giá trị khác -> "0").
    ///   4. Thứ tự kiểm tra điều kiện (guards theo System.cs:3085-3103):
    ///      - Sys_Group_CheckDB(objGroupCode, TConst.Flag.Yes, "") (System.cs:3085-3091, 1566-1576):
    ///        Kiểm tra tồn tại GroupCode trong DB. Nếu không tìm thấy -> ném ErridnLoyalty.Sys_Group_CheckDB_GroupCodeNotFound (Check.GroupCodeNotFound).
    ///      - Nếu bUpd_GroupName và strGroupName.Length &lt; 1 (System.cs:3093-3103):
    ///        -> ném ErridnLoyalty.Sys_Group_Update_InvalidGroupName (Check.strGroupName).
    ///   5. Lưu DB (System.cs:3108-3125):
    ///      - Nếu bUpd_GroupName: cập nhật GroupName = strGroupName
    ///      - Nếu bUpd_FlagActive: cập nhật FlagActive = strFlagActive
    ///      - Cập nhật LogLUDTimeUTC = DateTime.UtcNow, LogLUBy = "port"
    ///   6. Commit transaction (System.cs:3129 TDALUtils.DBUtils.CommitSafety), trả Ok(new { Success = true, Data = (object?)null }) (gate :457-458).
    /// </summary>
    [HttpPost("update")]
    public async Task<IActionResult> Update([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Sys_GroupUpdateReq? req)
    {
        // 0. Body rỗng: gate Sys_GroupController.cs:420-423 throw new Exception("strJson null") -> mã mặc định Sys_Group_Update
        if (req == null)
        {
            return PortResult.Fail("Sys_Group_Update", "Check.strJson", null);
        }

        // 1. Mở transaction (System.cs:3054 _cf.db.BeginTransaction())
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // 2. Chuỗi Ft_Cols_Upd do gate gửi lên: gate Sys_GroupController.cs:436-439
            var strFt_Cols_Upd = "";
            var tblSysGroup = "Sys_Group.";
            strFt_Cols_Upd += string.Format("{0}{1},", tblSysGroup, "GroupName");
            strFt_Cols_Upd += string.Format("{0}{1},", tblSysGroup, "FlagActive");

            // Chuẩn hoá Ft_Cols_Upd theo biz System.cs:3072: strFt_Cols_Upd = StdParam(objFt_Cols_Upd)
            strFt_Cols_Upd = StdParam(strFt_Cols_Upd) ?? "";

            // So khớp các cột được phép cập nhật: System.cs:3080-3081
            bool bUpd_GroupName = strFt_Cols_Upd.Contains("Sys_Group.GroupName".ToUpperInvariant());
            bool bUpd_FlagActive = strFt_Cols_Upd.Contains("Sys_Group.FlagActive".ToUpperInvariant());

            // 3. Chuẩn hoá dữ liệu input (System.cs:3075-3077):
            var strGroupCode = StdParam(req.GroupCode);
            var strGroupName = string.Format("{0}", req.GroupName).Trim();

            // Xử lý FlagActive theo tầng gate Sys_GroupController.cs:415-418 và biz System.cs:3077:
            // CẢNH BÁO / HÀNH VI NGUỒN: nếu req.FlagActive là null/rỗng thì gate tự gán mặc định "1" trước khi gọi Biz.
            var strFlagActive = StdFlag(req.FlagActive);   // nguồn :3077 StdFlag(objFlagActive): chỉ "1" -> "1", còn lại (kể cả thiếu/null) -> "0" (KHÔNG có mặc định "1")

            // 4. Thứ tự kiểm tra điều kiện (guards theo System.cs:3085-3103):
            // (a) Sys_Group_CheckDB: kiểm tra tồn tại GroupCode (System.cs:3085-3091, 1566-1576)
            Sys_Group? matchedGroup = null;
            if (!string.IsNullOrEmpty(strGroupCode))
            {
                var groupCodePattern = PortQuery.Equal(strGroupCode);
                matchedGroup = await _db.Set<Sys_Group>()
                    .FirstOrDefaultAsync(x => EF.Functions.Like((x.GroupCode ?? "").ToLower(), groupCodePattern, PortQuery.Esc));
            }

            if (matchedGroup == null)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Sys_Group_CheckDB_GroupCodeNotFound", "Check.GroupCodeNotFound", strGroupCode);
            }

            // (b) Nếu bUpd_GroupName và strGroupName.Length < 1: ném Sys_Group_Update_InvalidGroupName (System.cs:3093-3103)
            if (bUpd_GroupName && strGroupName.Length < 1)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Sys_Group_Update_InvalidGroupName", "Check.strGroupName", strGroupName);
            }

            // 5. Lưu DB (System.cs:3108-3125):
            if (bUpd_GroupName)
            {
                matchedGroup.GroupName = strGroupName;
            }
            if (bUpd_FlagActive)
            {
                matchedGroup.FlagActive = strFlagActive;
            }
            matchedGroup.LogLUDTimeUTC = DateTime.UtcNow;
            matchedGroup.LogLUBy = "port";

            await _db.SaveChangesAsync();

            // 6. Commit transaction (System.cs:3129 TDALUtils.DBUtils.CommitSafety)
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
            return PortResult.Fail("Sys_Group_Update", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Port 1:1 Delete action từ gate Sys_GroupController.cs:475-542 (#312).
    /// Chuỗi gọi: gate Sys_GroupController.Delete -> WA_Sys_Group_Delete -> WAS_Sys_Group_Delete -> Sys_Group_Delete (System.cs:3453-3566).
    ///
    /// Quy trình nghiệp vụ:
    ///   0. Kiểm tra GroupCode rỗng: Gate Sys_GroupController.cs:494, 528
    ///      Nếu GroupCode null hoặc rỗng -> ném ServiceException("invalid_GroupCode", "invalid_GroupCode") -> HTTP 400 ErridnLoyalty.invalid_GroupCode.
    ///   1. Mở transaction: System.cs:3480 _cf.db.BeginTransaction(). Lỗi rẽ nhánh -> rollback. Ngoại lệ -> trả mã mặc định Sys_Group_Delete.
    ///   2. Chuẩn hoá: strGroupCode = StdParam(objGroupCode).
    ///   3. Kiểm tra DB: Sys_Group_CheckDB (System.cs:3503-3509, 1546-1576) với strFlagExistToCheck = TConst.Flag.Yes ("1"):
    ///      Nếu không tìm thấy bản ghi trong Sys_Group -> ném ErridnLoyalty.Sys_Group_CheckDB_GroupCodeNotFound (Check.GroupCodeNotFound).
    ///   4. Xóa bảng phụ Sys_UserInGroup trước (System.cs:3511-3513, 14126-14141 Sys_UserInGroup_Delete_ByGroup):
    ///      Xóa tất cả bản ghi trong Sys_UserInGroup có GroupCode khớp với strGroupCode.
    ///   5. Xóa bản ghi Sys_Group (System.cs:3521-3527):
    ///      dtDB_Sys_Group.Rows[0].Delete(); _cf.db.SaveData("Sys_Group", dtDB_Sys_Group);
    ///   6. Commit transaction (System.cs:3532 TDALUtils.DBUtils.CommitSafety), trả về Success = true, Data = null (gate :518-519).
    /// </summary>
    [HttpPost("delete")]
    public async Task<IActionResult> Delete([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Sys_GroupDeleteReq? req)
    {
        // 0. Kiểm tra GroupCode rỗng tại gate Sys_GroupController.cs:494, 528:
        // Nếu req null hoặc GroupCode null/trắng -> throw ServiceException("invalid_GroupCode", "invalid_GroupCode")
        if (req == null || string.IsNullOrWhiteSpace(req.GroupCode))
        {
            return PortResult.Fail("invalid_GroupCode", "Check.GroupCode", req?.GroupCode);
        }

        var err = await DeleteXAsync(req.GroupCode);
        if (err != null)
        {
            return err;
        }

        // Gate Sys_GroupController.cs:518-519: SetSuccess(), Data = null
        return Ok(new
        {
            Success = true,
            Data = (object?)null
        });
    }

    /// <summary>
    /// Port 1:1 DeleteMultiple action từ gate Sys_GroupController.cs:544-608 (#313).
    /// Chuỗi gọi: gate Sys_GroupController.DeleteMultiple -> WA_Sys_Group_Delete (SysGroupController.cs:242-313)
    /// -> WAS_Sys_Group_Delete (System.cs:3726-3805) -> Sys_Group_Delete (System.cs:3453-3566).
    ///
    /// Quy trình nghiệp vụ:
    ///   0. Body rỗng / null: gate Sys_GroupController.cs:558-560:
    ///      if (string.IsNullOrEmpty(strJson)) throw new ServiceException("strJson null") -> bắt lỗi trả về 400 ErridnLoyalty.Sys_Group_Delete (Check.strJson).
    ///   1. Lặp tuần tự qua danh sách nhóm (Sys_GroupController.cs:575-592).
    ///      Mỗi phần tử chạy trong một transaction riêng biệt tại biz Sys_Group_Delete (System.cs:3480 _cf.db.BeginTransaction()).
    ///      "xóa một phần" là hành vi nguồn: nếu phần tử thứ k gặp lỗi -> dừng lặp và trả lỗi ngay,
    ///      các phần tử trước k đã commit thành công vào DB và KHÔNG bị rollback,
    ///      các phần tử sau k không được xử lý.
    ///   2. Nếu body mảng rỗng [] -> vòng lặp không chạy, trả về Success = true, Data = null (gate :594-595).
    ///   3. Không kiểm tra GroupCode rỗng ở gate (khác Delete đơn): phần tử có GroupCode rỗng hoặc không tồn tại sẽ bị
    ///      Sys_Group_CheckDB bắt và ném ErridnLoyalty.Sys_Group_CheckDB_GroupCodeNotFound.
    ///   4. Thành công trả Success = true, Data = null (gate :594-595).
    /// </summary>
    [HttpPost("deletemultiple")]
    public async Task<IActionResult> DeleteMultiple(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] List<Sys_GroupDeleteReq>? req)
    {
        // 0. Body rỗng / null (gate Sys_GroupController.cs:558-560):
        // if (!string.IsNullOrEmpty(strJson)) { ... } else { throw new ServiceException("strJson null"); }
        // Bắt lỗi trả về lỗi mặc định của Biz Delete (ErridnLoyalty.Sys_Group_Delete)
        if (req == null)
        {
            return PortResult.Fail("Sys_Group_Delete", "Check.strJson", null);
        }

        // 1. Lặp tuần tự qua từng phần tử (gate Sys_GroupController.cs:575-592).
        // Mỗi phần tử chạy trong 1 transaction riêng biệt (hàm DeleteXAsync).
        // "xóa một phần" là hành vi nguồn: nếu phần tử thứ k gặp lỗi -> dừng lặp và trả lỗi ngay,
        // các phần tử trước k đã commit thành công vào DB và KHÔNG bị rollback,
        // các phần tử sau k không được xử lý.
        foreach (var item in req)
        {
            var err = await DeleteXAsync(item?.GroupCode);
            if (err != null)
            {
                return err;
            }
        }

        // 2. Mảng rỗng hoặc xóa hết thành công: trả Success = true, Data = null (gate Sys_GroupController.cs:594-595)
        return Ok(new
        {
            Success = true,
            Data = (object?)null
        });
    }

    /// <summary>
    /// Thực hiện nghiệp vụ xóa 1 bản ghi Sys_Group trong 1 transaction riêng biệt.
    /// Nguồn: biz Sys_Group_Delete (System.cs:3453-3566).
    /// Dùng chung cho action Delete (#312) và DeleteMultiple (#313).
    /// </summary>
    private async Task<IActionResult?> DeleteXAsync(string? rawGroupCode)
    {
        // 1. Mở transaction cho mỗi phần tử (System.cs:3480 _cf.db.BeginTransaction())
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // 2. Chuẩn hoá GroupCode theo CUtils.StdParam (System.cs:3498): Trim, ToUpper, rỗng -> null
            var strGroupCode = StdParam(rawGroupCode);

            // 3. Sys_Group_CheckDB (System.cs:3503-3509, 1546-1576) với FlagExist = Yes
            // So sánh chuỗi trên SQLite không phân biệt hoa thường theo chuẩn: Like + PortQuery.Equal
            Sys_Group? matchedGroup = null;
            if (!string.IsNullOrEmpty(strGroupCode))
            {
                var groupCodePattern = PortQuery.Equal(strGroupCode);
                matchedGroup = await _db.Set<Sys_Group>()
                    .FirstOrDefaultAsync(x => EF.Functions.Like((x.GroupCode ?? "").ToLower(), groupCodePattern, PortQuery.Esc));
            }

            if (matchedGroup == null)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Sys_Group_CheckDB_GroupCodeNotFound", "Check.GroupCodeNotFound", strGroupCode ?? rawGroupCode);
            }

            // 4. Xóa bảng phụ Sys_UserInGroup_Delete_ByGroup(strGroupCode) (System.cs:3511, 14126-14141)
            var groupCodeMatchPattern = PortQuery.Equal(strGroupCode!);
            var usersInGroup = await _db.Set<Sys_UserInGroup>()
                .Where(x => EF.Functions.Like((x.GroupCode ?? "").ToLower(), groupCodeMatchPattern, PortQuery.Esc))
                .ToListAsync();
            if (usersInGroup.Count > 0)
            {
                _db.Set<Sys_UserInGroup>().RemoveRange(usersInGroup);
            }

            // 5. Xóa bản ghi Sys_Group (System.cs:3521-3527)
            _db.Set<Sys_Group>().Remove(matchedGroup);
            await _db.SaveChangesAsync();

            // 6. Commit transaction (System.cs:3532)
            await tx.CommitAsync();

            return null;
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return PortResult.Fail("Sys_Group_Delete", "Exception", ex.Message);
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
}

/// <summary>
/// Tham số request cho action Search — Sys_GroupController.cs:27
/// </summary>
public class Sys_GroupSearchReq
{
    public string? OrgID { get; set; }
    public string? KeyWord { get; set; }
    public string? FlagActive { get; set; }
    public int Ft_PageIndex { get; set; } = 0;
    public int Ft_PageSize { get; set; } = 10;
}

/// <summary>
/// DTO từng dòng trong DataList trả về client (Gate Sys_GroupController.cs:97-106).
/// Gồm 7 trường: GroupCode, GroupName, MST, FlagActive, LogLUDTimeUTC, LogLUBy, QtyUserInGroup.
/// </summary>
public class Sys_GroupSearchResultItem
{
    public string GroupCode { get; set; } = "";
    public string? GroupName { get; set; }
    public string? MST { get; set; }
    public string? FlagActive { get; set; }
    public DateTime? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
    public int QtyUserInGroup { get; set; }
}

/// <summary>
/// DTO trả về cho action GetAllActive (Sys_GroupController.cs:136-243, #308).
/// Gồm 6 cột của Sys_Group + MyIdxSeq (SQL System.cs:2356-2363).
/// </summary>
public class Sys_GroupGetAllActiveRes
{
    public long MyIdxSeq { get; set; }
    public string GroupCode { get; set; } = "";
    public string? GroupName { get; set; }
    public string? FlagActive { get; set; }
    public DateTime? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
    public string? MST { get; set; }
}

/// <summary>
/// DTO request cho action GetByGroupCode — Sys_GroupController.cs:244 (#309).
/// </summary>
public class Sys_GroupGetByGroupCodeReq
{
    public string? GroupCode { get; set; }
    public string? OrgID { get; set; }
}

/// <summary>
/// DTO response cho action GetByGroupCode (Gate Sys_GroupController.cs:293).
/// Gồm 2 property: objSys_Group và Lst_Sys_UserInGroup.
/// </summary>
public class Sys_GroupGetByGroupCodeRes
{
    public Sys_Group? objSys_Group { get; set; }
    public List<Sys_UserInGroupRes> Lst_Sys_UserInGroup { get; set; } = new();
}

/// <summary>
/// DTO từng dòng trong Lst_Sys_UserInGroup (SQL System.cs:2374-2393).
/// Gồm các trường cơ sở của Sys_UserInGroup và các cột mở rộng từ Sys_User và Sys_Group.
/// </summary>
public class Sys_UserInGroupRes
{
    public string UserCode { get; set; } = "";
    public string GroupCode { get; set; } = "";
    public DateTime? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
    // (suig.* không có cột MST)

    public string? su_UserCode { get; set; }
    public string? su_UserName { get; set; }
    public string? su_FlagSysAdmin { get; set; }
    public string? su_FlagActive { get; set; }

    public string? sg_GroupCode { get; set; }
    public string? sg_GroupName { get; set; }
    public string? sg_FlagActive { get; set; }
}

/// <summary>
/// DTO request cho action Create — Sys_GroupController.cs:314 (#310).
/// </summary>
public class Sys_GroupCreateReq
{
    public string? GroupCode { get; set; }
    public string? GroupName { get; set; }
    public string? MST { get; set; }
}

/// <summary>
/// DTO request cho action Update — Sys_GroupController.cs:402 (#311).
/// </summary>
public class Sys_GroupUpdateReq
{
    public string? GroupCode { get; set; }
    public string? GroupName { get; set; }
    public string? FlagActive { get; set; }
    public string? MST { get; set; }
}

/// <summary>
/// DTO request cho action Delete — Sys_GroupController.cs:475 (#312).
/// </summary>
public class Sys_GroupDeleteReq
{
    public string? GroupCode { get; set; }
}



