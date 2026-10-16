using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MiniLoyalty.Data;
using MiniLoyalty.Models.Port;

namespace MiniLoyalty.Controllers.Port;

public class MstResignReasonGetByReasonIDReq
{
    public string? ReasonID { get; set; }
    public string? OrgID { get; set; }
}

public class MstResignReasonSearchReq
{
    public string? OrgID { get; set; }
    public string? KeyWord { get; set; }
    public string? FlagActive { get; set; }
    public int Ft_PageIndex { get; set; } = 0;
    public int Ft_PageSize { get; set; } = 10;
}

public class MstResignReasonExportReq
{
    public string? OrgID { get; set; }
    public string? KeyWord { get; set; }
    public string? FlagActive { get; set; }
}

[ApiController]
[Route("api/mst_resignreason")]
[Produces("application/json")]
public class Mst_ResignReasonControllerPort : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;

    public Mst_ResignReasonControllerPort(AppDbContext db, IConfiguration config, IWebHostEnvironment env)
    {
        _db = db;
        _config = config;
        _env = env;
    }

    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] MstResignReasonSearchReq? req)
    {
        var r = req ?? new MstResignReasonSearchReq();
        var pageIndex = r.Ft_PageIndex;
        var pageSize = r.Ft_PageSize;

        // Tinh RecordStart = PageIndex*PageSize, RecordCount = PageSize (Mst_ResignReasonController.cs:67-68)
        var start = (long)pageIndex * pageSize;
        var (itemCount, rows) = await GetXAsync(r.OrgID, null, r.KeyWord, r.FlagActive, start, pageSize);

        // PageCount chi tinh khi DataList KHONG rong; doi PageSize = 0 thanh 10 (Mst_ResignReasonController.cs:81-85)
        var ps = pageSize == 0 ? 10 : pageSize;
        var pageCount = rows.Count == 0 ? 0 : (itemCount % ps == 0 ? itemCount / ps : itemCount / ps + 1);

        return Ok(new { Success = true, Data = new PageInfo<object>(rows, itemCount, pageCount, pageIndex, pageSize) });
    }

    [HttpPost("getbyreasonid")]
    public async Task<IActionResult> GetByReasonID([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] MstResignReasonGetByReasonIDReq? req)
    {
        // gate (dòng 117): nếu ReasonID rỗng, bỏ qua gọi Biz và trả về mặc định là null.
        // OrgID nguồn lấy từ session (orgUserUI.OrgId, :132) — hệ đích không có session nên nhận OrgID tuỳ chọn trong body
        // (cùng quy ước với Search); không truyền thì không lọc OrgID. Nhận qua JSON body như mọi action GetBy* khác (#174, #185, #192).
        if (string.IsNullOrWhiteSpace(req?.ReasonID))
        {
            return Ok(new { Success = true, Data = (object?)null });
        }

        var (_, rows) = await GetXAsync(req.OrgID, req.ReasonID, null, null, 0, 1);
        return Ok(new { Success = true, Data = rows.FirstOrDefault() }); // gate (dòng 141-147): gán phần tử [0] hoặc null
    }

    [HttpPost("getallactive")]
    public async Task<IActionResult> GetAllActive()
    {
        // gate truyen: Ft_RecordStart, Ft_RecordCount mac dinh tu Controller, hieu ngam la lay tat ca vi khong phan trang (Mst_ResignReasonController.cs:197-198)
        // filter FlagActive = "1", khong truyen OrgID/KeyWord null (Mst_ResignReasonController.cs:182-185)
        var (_, rows) = await GetXAsync(null, null, null, "1", 0, int.MaxValue);

        // gate tra mang (lst_Mst_ResignReason), null neu rong (Mst_ResignReasonController.cs:204-209)
        return Ok(new { Success = true, Data = rows.Count > 0 ? rows : null });
    }

    /// <summary>
    /// Port 1:1 Mst_ResignReasonController.Create (gate :226-309) -> WA_Mst_ResignReason_Create -> Mst_ResignReason_CreateX (MasterData.cs:25005-25143).
    /// - Body rỗng -> ServiceException("strJson null") -> mã mặc định Mst_ResignReason_Create (gate :239-242).
    /// - ReasonID do SEQ sinh (gate :256-276, SeqType.ReasonID -> Seq_ReasonID, format "{0}REASONID.{3}.{1:00000}{2}", max 100000) và GHI ĐÈ ReasonID client gửi;
    ///   seq nằm trong transaction riêng đã commit nên vẫn tăng kể cả khi lưu lỗi (Seq.cs:28-41, :118).
    /// - Nghiệp vụ tạo được tách sang CreateXAsync để dùng chung cho cả Create và Import (mỗi dòng 1 transaction biz riêng).
    /// </summary>
    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] Mst_ResignReason? req)
    {
        // Body rỗng (Mst_ResignReasonController.cs:239-242) -> mã mặc định của Create
        if (req == null)
        {
            return PortResult.Fail("Mst_ResignReason_Create", "Check.strJson", null);
        }

        // Gate :256-276: lấy ReasonID từ sequence TRƯỚC khi vào transaction lưu (transaction riêng, đã commit độc lập)
        var seq = await PortSeq.NextAsync<Seq_ReasonID>(_db);
        var strReasonID = (PortSeq.Build("{0}REASONID.{3}.{1:00000}{2}", "", seq, 100000, "", DateTime.UtcNow) ?? "").Trim().ToUpper();

        var err = await CreateXAsync(req.OrgID, strReasonID, req.ResignReason, req.ReasonDesc, req.Remark);
        if (err != null)
        {
            return err;
        }

        return Ok(new { Success = true, Data = (object?)null }); // Mst_ResignReasonController.cs:295-296
    }

    /// <summary>
    /// Nghiệp vụ tạo 1 lý do nghỉ việc — tách riêng để dùng chung cho Create (#236) và Import (#240, gọi từng dòng).
    /// Nguồn: MasterData.cs:25005-25143 (Mst_ResignReason_CreateX).
    /// Nhận thẳng reasonId (Create sinh từ sequence Seq_ReasonID; Import sinh bằng Guid.NewGuid().ToString()).
    /// Mỗi lời gọi chạy trong 1 transaction độc lập (MasterData.cs:25182 _cf.db.BeginTransaction()).
    /// Trả về null nếu thành công; trả về IActionResult chứa lỗi nếu thất bại (và rollback transaction của dòng đó).
    /// </summary>
    private async Task<IActionResult?> CreateXAsync(string? orgId, string? reasonId, string? resignReason, string? reasonDesc, string? remark)
    {
        // Chuẩn hoá đầu vào (MasterData.cs:25039-25043): StdParam = Trim + ToUpper; StdStr = Trim
        var strOrgID = string.IsNullOrWhiteSpace(orgId) ? null : orgId.Trim().ToUpper();
        var strReasonID = string.IsNullOrWhiteSpace(reasonId) ? null : reasonId.Trim().ToUpper();
        var strResignReason = string.IsNullOrWhiteSpace(resignReason) ? null : resignReason.Trim();
        var strReasonDesc = string.IsNullOrWhiteSpace(reasonDesc) ? null : reasonDesc.Trim();
        var strRemark = string.IsNullOrWhiteSpace(remark) ? null : remark.Trim();

        await using var tx = await _db.Database.BeginTransactionAsync(); // MasterData.cs:25182
        try
        {
            if (string.IsNullOrEmpty(strOrgID))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_ResignReason_CreateX_InvalidOrgID", "Check.OrgID", strOrgID); // :25053-25063
            }
            if (string.IsNullOrEmpty(strReasonID))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_ResignReason_CreateX_InvalidReasonID", "Check.strReasonID", strReasonID); // :25065-25075
            }

            // Mst_ResignReason_CheckDB(OrgID, ReasonID, "0"): phải CHƯA tồn tại (:25077-25084)
            var existReasonID = await _db.Set<Mst_ResignReason>().AsNoTracking()
                .AnyAsync(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(strOrgID), PortQuery.Esc)
                            && EF.Functions.Like((x.ReasonID ?? "").ToLower(), PortQuery.Equal(strReasonID), PortQuery.Esc));
            if (existReasonID)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_ResignReason_CheckDB_ReasonIDExist", "Check.OrgID", strOrgID, "Check.ReasonID", strReasonID);
            }

            // Mst_Org_CheckDB(OrgID, "1", "1") (:25086-25092; MasterData.Org.cs:29-98): phải tồn tại và FlagActive thuộc "1"
            var org = await _db.Set<Mst_Org>().AsNoTracking()
                .FirstOrDefaultAsync(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(strOrgID), PortQuery.Esc));
            if (org == null)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_Org_CheckDB_OrgNotFound", "Check.OrgID", strOrgID);
            }
            if (!"1".Contains(org.FlagActive ?? ""))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_Org_CheckDB_FlagActiveNotMatched", "Check.OrgID", strOrgID,
                    "Check.strFlagActiveListToCheck", "1", "DB.FlagActive", org.FlagActive);
            }

            if (string.IsNullOrEmpty(strResignReason))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_ResignReason_CreateX_InvalidResignReason", "Check.strResignReason", strResignReason); // :25094-25104
            }

            // Mst_ResignReason_CheckResignReason(OrgID, ResignReason, "0"): chưa tồn tại trong cùng OrgID (:25106-25113)
            var existResignReason = await _db.Set<Mst_ResignReason>().AsNoTracking()
                .AnyAsync(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(strOrgID), PortQuery.Esc)
                            && EF.Functions.Like((x.ResignReason ?? "").ToLower(), PortQuery.Equal(strResignReason), PortQuery.Esc));
            if (existResignReason)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_ResignReason_CheckResignReason_ResignReasonExist", "Check.OrgID", strOrgID, "Check.ResignReason", strResignReason);
            }

            // SaveDB (:25118-25141)
            _db.Set<Mst_ResignReason>().Add(new Mst_ResignReason
            {
                OrgID = strOrgID,
                ReasonID = strReasonID,
                NetworkID = _config["Biz_NetworkID"] ?? "1001",
                ResignReason = strResignReason,
                ReasonDesc = strReasonDesc,
                FlagActive = "1",
                Remark = strRemark,
                LogLUDTimeUTC = DateTime.UtcNow,
                LogLUBy = "port"
            });
            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return null; // Thành công
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return PortResult.Fail("Mst_ResignReason_Create", "Exception", ex.Message); // mã mặc định (MasterData.cs:25246)
        }
    }

    /// <summary>
    /// Port 1:1 Mst_ResignReasonController.Import (gate :512-623) -> WA_Mst_ResignReason_Create -> Mst_ResignReason_CreateX (MasterData.cs:25005-25143).
    /// - Nhận file upload qua multipart/form-data.
    /// - Tham số orgId nhận từ form ([FromForm] string? orgId) hoặc header OrgID (do nguồn lấy OrgID từ session user orgUserUI.OrgId :594).
    /// - Thiếu file hoặc file rỗng hoặc đuôi khác .xlsx -> Fail("Mst_ResignReason_Import", "Message", "File excel import không hợp lệ") (gate :536-549).
    /// - Đọc sheet bằng PortExcel.ReadSheet với 3 cột: ResignReason, OrgID, ReasonDesc (gate :552-555, template :757-767).
    ///   Template (GetImportDicColumsTemplate :757-767): ResignReason ("Lý do nghỉ việc (*)"), OrgID ("Mã tổ chức"), ReasonDesc ("Mô tả"). (ghi chú cũ: GetImportDicColumsTemplate().
    /// - Không có dòng dữ liệu (rows.Count == 0) -> Fail("Mst_ResignReason_Import", "Message", "File excel import không có dữ liệu") (gate :553, :606-610).
    /// - Số cột dòng tiêu đề != 3 -> Fail("Mst_ResignReason_Import", "Message", "File excel import không hợp lệ") (gate :555-559).
    /// - Vòng lặp 1 (kiểm tra trước): nếu bất kỳ dòng nào có ResignReason rỗng -> Fail("Mst_ResignReason_Import", "Message", "Lý do nghỉ việc không được trống") (gate :562-570).
    ///   Nếu lỗi ở bước này: không có dòng nào được tạo (kiểm tra trước).
    /// - Vòng lặp 2 (lưu dữ liệu từng dòng): mỗi dòng sinh ReasonID = Guid.NewGuid().ToString() (gate :585)
    ///   rồi gọi CreateXAsync độc lập (1 transaction riêng cho mỗi dòng). Lỗi ở dòng k -> trả về lỗi của CreateXAsync ngay lập tức
    ///   (hành vi import một phần: các dòng trước k đã commit thành công, dòng k và các dòng sau không được tạo).
    /// - Thành công: trả { Success = true, Data = null } (gate :603-604).
    /// </summary>
    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Import(IFormFile? file)
    {
        // 1. Kiểm tra file upload (gate Mst_ResignReasonController.cs:534-549)
        if (file == null || file.Length == 0)
        {
            return PortResult.Fail("Mst_ResignReason_Import", "Message", "File excel import không hợp lệ");
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".xlsx")
        {
            return PortResult.Fail("Mst_ResignReason_Import", "Message", "File excel import không hợp lệ");
        }

        // 2. Đọc bảng tính bằng PortExcel.ReadSheet (gate Mst_ResignReasonController.cs:552)
        // Giả định mẫu template gồm 3 cột: ResignReason, ReasonDesc, Remark do mã nguồn thiếu hàm GetImportDicColumsTemplate()
        int columnCount;
        List<Dictionary<string, string?>> rows;
        try
        {
            using var stream = file.OpenReadStream();
            (columnCount, rows) = PortExcel.ReadSheet(stream, new[] { "ResignReason", "OrgID", "ReasonDesc" });   // GetImportDicColumsTemplate (Mst_ResignReasonController.cs:757-767): ResignReason, OrgID, ReasonDesc (ReasonID/mnnt bị comment)
        }
        catch
        {
            return PortResult.Fail("Mst_ResignReason_Import", "Message", "File excel import không hợp lệ");
        }

        // Kiểm tra có dữ liệu hay không (gate Mst_ResignReasonController.cs:553, :606-610)
        if (rows.Count == 0)
        {
            return PortResult.Fail("Mst_ResignReason_Import", "Message", "File excel import không có dữ liệu");
        }

        // Kiểm tra số cột dòng tiêu đề so với template (3 cột) (gate Mst_ResignReasonController.cs:555-559)
        if (columnCount != 3)   // GetImportDicColumsTemplate().Count = 3
        {
            return PortResult.Fail("Mst_ResignReason_Import", "Message", "File excel import không hợp lệ");
        }

        // 3. Vòng lặp 1: Kiểm tra TẤT CẢ các dòng TRƯỚC khi tạo (gate Mst_ResignReasonController.cs:562-570)
        foreach (var r in rows)
        {
            var resignReason = r.GetValueOrDefault("ResignReason");
            if (string.IsNullOrWhiteSpace(resignReason))
            {
                return PortResult.Fail("Mst_ResignReason_Import", "Message", "Lý do nghỉ việc không được trống");
            }
        }

        // OrgID lấy từ TỪNG DÒNG Excel (cột "Mã tổ chức"): gate :578 ToListof<Mst_ResignReason>(table) -> :597 Mst_ResignReason = item; CreateX dùng item.OrgID (không dùng OrgID session)
        // 4. Vòng lặp 2: Lưu từng dòng tuần tự: mỗi dòng sinh ReasonID bằng Guid.NewGuid().ToString() (gate :585)
        // và gọi CreateXAsync trong 1 transaction riêng (hành vi import một phần: gate :583-601)
        foreach (var r in rows)
        {
            var reasonId = Guid.NewGuid().ToString();
            var resignReason = r.GetValueOrDefault("ResignReason");
            var reasonDesc = r.GetValueOrDefault("ReasonDesc");
            var rowOrgID = r.GetValueOrDefault("OrgID");
            var err = await CreateXAsync(rowOrgID, reasonId, resignReason, reasonDesc, null);
            if (err != null)
            {
                // Lỗi ở dòng k -> dừng ngay và trả lỗi của dòng đó (các dòng trước k đã commit thành công)
                return err;
            }
        }

        // Thành công: Data null (gate Mst_ResignReasonController.cs:603-604)
        return Ok(new { Success = true, Data = (object?)null });
    }

    /// <summary>
    /// Port 1:1 Mst_ResignReasonController.Update (gate :313-379) -> WA_Mst_ResignReason_Update -> Mst_ResignReason_UpdateX (MasterData.cs:25351-25484).
    /// </summary>
    [HttpPost("update")]
    public async Task<IActionResult> Update([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] Mst_ResignReason? req)
    {
        // Body rỗng -> ném lỗi (Mst_ResignReasonController.cs:326-328) -> catch ném mã mặc định
        if (req == null)
        {
            return PortResult.Fail("Mst_ResignReason_Update", "Check.strJson", null);
        }

        // Cột update từ gate (Mst_ResignReasonController.cs:344-347)
        var strFt_Cols_Upd = "Mst_ResignReason.ResignReason,Mst_ResignReason.ReasonDesc,Mst_ResignReason.FlagActive,".ToUpper();

        // Chuẩn hoá input (MasterData.cs:25391-25404)
        var strOrgID = string.IsNullOrWhiteSpace(req.OrgID) ? null : req.OrgID.Trim().ToUpper();
        var strReasonID = string.IsNullOrWhiteSpace(req.ReasonID) ? null : req.ReasonID.Trim().ToUpper();
        var strResignReason = string.IsNullOrWhiteSpace(req.ResignReason) ? null : req.ResignReason.Trim();
        var strReasonDesc = string.IsNullOrWhiteSpace(req.ReasonDesc) ? null : req.ReasonDesc.Trim();
        var strFlagActive = req.FlagActive == "1" ? "1" : "0"; // StdFlag
        var strRemark = string.IsNullOrWhiteSpace(req.Remark) ? null : req.Remark.Trim();

        var bUpd_ResignReason = strFt_Cols_Upd.Contains("MST_RESIGNREASON.RESIGNREASON");
        var bUpd_ReasonDesc = strFt_Cols_Upd.Contains("MST_RESIGNREASON.REASONDESC");
        var bUpd_FlagActive = strFt_Cols_Upd.Contains("MST_RESIGNREASON.FLAGACTIVE");
        var bUpd_Remark = strFt_Cols_Upd.Contains("MST_RESIGNREASON.REMARK");

        await using var tx = await _db.Database.BeginTransactionAsync(); // MasterData.cs:25529
        try
        {
            // Mst_ResignReason_CheckDB (OrgID, ReasonID, "1", "") (:25412-25420)
            var oldRecord = await _db.Set<Mst_ResignReason>().AsNoTracking()
                .FirstOrDefaultAsync(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(strOrgID ?? ""), PortQuery.Esc)
                                       && EF.Functions.Like((x.ReasonID ?? "").ToLower(), PortQuery.Equal(strReasonID ?? ""), PortQuery.Esc));
            if (oldRecord == null)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_ResignReason_CheckDB_ReasonIDNotFound", "Check.OrgID", strOrgID, "Check.ReasonID", strReasonID);
            }
            var strResignReason_DB = (oldRecord.ResignReason ?? "").Trim();

            // Mst_Org_CheckDB (OrgID, "1", "1") (:25423-25429)
            var org = await _db.Set<Mst_Org>().AsNoTracking()
                .FirstOrDefaultAsync(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(strOrgID ?? ""), PortQuery.Esc));
            if (org == null)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_Org_CheckDB_OrgNotFound", "Check.OrgID", strOrgID);
            }
            if (!"1".Contains(org.FlagActive ?? ""))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_Org_CheckDB_FlagActiveNotMatched", "Check.OrgID", strOrgID,
                    "Check.strFlagActiveListToCheck", "1", "DB.FlagActive", org.FlagActive);
            }

            // Check ResignReason invalid (:25431-25441)
            if (string.IsNullOrEmpty(strResignReason))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_ResignReason_UpdateX_InvalidResignReason", "Check.strResignReason", strResignReason);
            }

            // Mst_ResignReason_CheckResignReason (OrgID, ResignReason, "0", "") (:25443-25456)
            // StringEqual của nguồn so sánh PHÂN BIỆT hoa/thường (quyết định chung như Mst_Department): đổi tên chỉ khác hoa/thường sẽ vấp chính nó -> báo tồn tại (như nguồn)
            if (bUpd_ResignReason && !string.Equals(strResignReason, strResignReason_DB, StringComparison.Ordinal))
            {
                var nameExist = await _db.Set<Mst_ResignReason>().AsNoTracking()
                    .AnyAsync(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(strOrgID ?? ""), PortQuery.Esc)
                                && EF.Functions.Like((x.ResignReason ?? "").ToLower(), PortQuery.Equal(strResignReason), PortQuery.Esc));
                if (nameExist)
                {
                    await tx.RollbackAsync();
                    return PortResult.Fail("Mst_ResignReason_CheckResignReason_ResignReasonExist", "Check.OrgID", strOrgID, "Check.ResignReason", strResignReason);
                }
            }

            // Save Mst_ResignReason (:25462-25481)
            // Khoá lấy từ dòng THẬT trong DB (CheckDB khớp không phân biệt hoa/thường như SQL Server; WHERE của EF trên SQLite thì có phân biệt)
            var recordToUpdate = new Mst_ResignReason { OrgID = oldRecord.OrgID, ReasonID = oldRecord.ReasonID };
            _db.Set<Mst_ResignReason>().Attach(recordToUpdate);

            if (bUpd_ResignReason) recordToUpdate.ResignReason = strResignReason;
            if (bUpd_ReasonDesc) recordToUpdate.ReasonDesc = strReasonDesc;
            if (bUpd_FlagActive) recordToUpdate.FlagActive = strFlagActive;
            if (bUpd_Remark) recordToUpdate.Remark = strRemark;

            recordToUpdate.LogLUDTimeUTC = DateTime.UtcNow;
            recordToUpdate.LogLUBy = "port";

            var entry = _db.Entry(recordToUpdate);
            if (bUpd_ResignReason) entry.Property(x => x.ResignReason).IsModified = true;
            if (bUpd_ReasonDesc) entry.Property(x => x.ReasonDesc).IsModified = true;
            if (bUpd_FlagActive) entry.Property(x => x.FlagActive).IsModified = true;
            if (bUpd_Remark) entry.Property(x => x.Remark).IsModified = true;
            entry.Property(x => x.LogLUDTimeUTC).IsModified = true;
            entry.Property(x => x.LogLUBy).IsModified = true;

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return Ok(new { Success = true, Data = (object?)null }); // Mst_ResignReasonController.cs:365-366
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return PortResult.Fail("Mst_ResignReason_Update", "Exception", ex.Message); // MasterData.cs:25595
        }
    }

    /// <summary>
    /// Port 1:1 Mst_ResignReasonController.Delete (gate :383-444) -> WA_Mst_ResignReason_Delete -> Mst_ResignReason_DeleteX (MasterData.cs:25704-25772).
    /// </summary>
    [HttpPost("delete")]
    public async Task<IActionResult> Delete([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] Mst_ResignReason? req)
    {
        // Gate :402-431: Nếu ReasonID rỗng -> throw new ServiceException("invalid_ResignReason", "invalid_ResignReason")
        if (string.IsNullOrWhiteSpace(req?.ReasonID))
        {
            return PortResult.Fail("invalid_ResignReason");
        }

        var strOrgID = string.IsNullOrWhiteSpace(req.OrgID) ? null : req.OrgID.Trim().ToUpper();
        var strReasonID = req.ReasonID.Trim().ToUpper();

        await using var tx = await _db.Database.BeginTransactionAsync(); // MasterData.cs:25805
        try
        {
            // Mst_Org_CheckDB(OrgID, "1", "1") (:25740-25746)
            var org = await _db.Set<Mst_Org>().AsNoTracking()
                .FirstOrDefaultAsync(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(strOrgID ?? ""), PortQuery.Esc));
            if (org == null)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_Org_CheckDB_OrgNotFound", "Check.OrgID", strOrgID);
            }
            if (!"1".Contains(org.FlagActive ?? ""))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_Org_CheckDB_FlagActiveNotMatched", "Check.OrgID", strOrgID,
                    "Check.strFlagActiveListToCheck", "1", "DB.FlagActive", org.FlagActive);
            }

            // Mst_ResignReason_CheckDB(OrgID, ReasonID, "1", "") (:25748-25755)
            var recordToDelete = await _db.Set<Mst_ResignReason>()
                .FirstOrDefaultAsync(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(strOrgID ?? ""), PortQuery.Esc)
                                       && EF.Functions.Like((x.ReasonID ?? "").ToLower(), PortQuery.Equal(strReasonID), PortQuery.Esc));
            if (recordToDelete == null)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_ResignReason_CheckDB_ReasonIDNotFound", "Check.OrgID", strOrgID, "Check.ReasonID", strReasonID);
            }

            // Delete (:25763-25769)
            _db.Set<Mst_ResignReason>().Remove(recordToDelete);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return Ok(new { Success = true, Data = (object?)null }); // Gate :425-426
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return PortResult.Fail("Mst_ResignReason_Delete", "Exception", ex.Message); // (:25791, 25857-25869)
        }
    }

    /// <summary>
    /// Port 1:1 Mst_ResignReasonController.DeleteMultiple (gate :446-509).
    /// Gọi WA_Mst_ResignReason_Delete cho từng phần tử (xóa một phần, lỗi ở phần tử nào dừng lại trả lỗi, các phần tử trước đã xóa thành công).
    /// </summary>
    [HttpPost("deletemultiple")]
    public async Task<IActionResult> DeleteMultiple([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] List<Mst_ResignReason>? req)
    {
        // xoa mot phan
        if (req == null)
        {
            // Body rong -> 400 ErridnLoyalty.Mst_ResignReason_Delete (Mst_ResignReasonController.cs:459-462)
            // cùng quy ước Mst_Discipline_Delete: Fail(<mã mặc định>, "Check.strJson", null)
            return PortResult.Fail("Mst_ResignReason_Delete", "Check.strJson", null);
        }

        // Mỗi phần tử gọi WA_Mst_ResignReason_Delete riêng (gate :476-493): phần tử k lỗi -> trả lỗi ngay, các phần tử trước ĐÃ xoá thật (không rollback)
        foreach (var item in req)
        {
            // Chuẩn hóa mã trước khi kiểm tra DB (phần tử null trong mảng -> coi như thiếu OrgID => Mst_Org_CheckDB_OrgNotFound)
            var strOrgID = string.IsNullOrWhiteSpace(item?.OrgID) ? null : item!.OrgID.Trim().ToUpper();
            var strReasonID = string.IsNullOrWhiteSpace(item?.ReasonID) ? null : item!.ReasonID.Trim().ToUpper();

            await using var tx = await _db.Database.BeginTransactionAsync(); // (MasterData.cs:25805)
            try
            {
                // Mst_Org_CheckDB(OrgID, "1", "1") (:25740-25746)
                var org = await _db.Set<Mst_Org>().AsNoTracking()
                    .FirstOrDefaultAsync(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(strOrgID ?? ""), PortQuery.Esc));
                if (org == null)
                {
                    await tx.RollbackAsync();
                    return PortResult.Fail("Mst_Org_CheckDB_OrgNotFound", "Check.OrgID", strOrgID);
                }
                if (!"1".Contains(org.FlagActive ?? ""))
                {
                    await tx.RollbackAsync();
                    return PortResult.Fail("Mst_Org_CheckDB_FlagActiveNotMatched", "Check.OrgID", strOrgID,
                        "Check.strFlagActiveListToCheck", "1", "DB.FlagActive", org.FlagActive);
                }

                // Mst_ResignReason_CheckDB(OrgID, ReasonID, "1", "") (:25748-25755)
                var recordToDelete = await _db.Set<Mst_ResignReason>()
                    .FirstOrDefaultAsync(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(strOrgID ?? ""), PortQuery.Esc)
                                           && EF.Functions.Like((x.ReasonID ?? "").ToLower(), PortQuery.Equal(strReasonID ?? ""), PortQuery.Esc));
                if (recordToDelete == null)
                {
                    await tx.RollbackAsync();
                    return PortResult.Fail("Mst_ResignReason_CheckDB_ReasonIDNotFound", "Check.OrgID", strOrgID, "Check.ReasonID", strReasonID);
                }

                // Delete (:25763-25769)
                _db.Set<Mst_ResignReason>().Remove(recordToDelete);
                await _db.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_ResignReason_Delete", "Exception", ex.Message);
            }
        }

        return Ok(new { Success = true, Data = (object?)null }); // Gate :495-496
    }

    /// <summary>
    /// Port 1:1 action Export từ gate Mst_ResignReasonController.cs:628-706 (#241).
    /// Chuỗi gọi: gate Mst_ResignReasonController.Export -> WA_Mst_ResignReason_Get -> WAS_Mst_ResignReason_Get -> Mst_ResignReason_Get -> Mst_ResignReason_GetX (MasterData.cs:24608-25003).
    ///
    /// Quy trình nghiệp vụ:
    ///   1. Tham số: { OrgID, KeyWord, FlagActive } qua JSON body.
    ///   2. Gọi lại GetXAsync(req.OrgID, null, req.KeyWord, req.FlagActive, 0, 123456000) (gate :649-674).
    ///      Ft_RecordStart = "0", Ft_RecordCount = 123456000 (:667-668).
    ///   3. Rỗng: nếu danh sách null hoặc rỗng -> ném ServiceException("Không có dữ liệu") (gate :690-693)
    ///      -> trả về 400 ErridnLoyalty.Mst_ResignReason_Export kèm Params Message = "Không có dữ liệu".
    ///   4. Từ điển cột: dùng GetImportDicColums() (gate :679) — dùng từ điển Import cho Export là của nguồn.
    ///      Gồm 5 cột theo đúng thứ tự nguồn: ReasonID ("Mã lý do (*)"), ResignReason ("Lý do nghỉ việc (*)"),
    ///      mnnt_NNTFullName ("Tổ chức"), ReasonDesc ("Mô tả"), FlagActive ("Trạng thái") (gate :745-755).
    ///   5. Sinh đường dẫn file qua PortExcel.GenExcelExportFilePath(webRoot, "Mst_ResignReason") (gate :681).
    ///   6. Ghi dữ liệu ra Excel bằng PortExcel.ExportToExcelFromList(rows, dicColNames, physicalPath, "Mst_ResignReason") (gate :682).
    ///   7. Bắt ngoại lệ nếu ghi file lỗi -> PortResult.Fail("Mst_ResignReason_Export", "Exception", ex.Message) (gate :695-699).
    ///   8. Thành công: trả về JsonResultEntry Data = { Url = PortExcel.GetRequestBaseAddress(Request) + virtualPath } (gate :683-687).
    /// </summary>
    [HttpPost("export")]
    public async Task<IActionResult> Export(
        [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] MstResignReasonExportReq? req)
    {
        var r = req ?? new MstResignReasonExportReq();

        // Gọi GetXAsync(OrgID, null, KeyWord, FlagActive, 0, 123456000) (gate :649-674)
        // RecordStart = 0, RecordCount = 123456000 (gate :667-668)
        var (_, rows) = await GetXAsync(r.OrgID, null, r.KeyWord, r.FlagActive, 0, 123456000);

        // Nguồn Mst_ResignReasonController.cs:675, 690-693: nếu danh sách null hoặc Count == 0 -> throw ServiceException("Không có dữ liệu")
        if (rows == null || rows.Count == 0)
        {
            return PortResult.Fail("Mst_ResignReason_Export", "Message", "Không có dữ liệu");
        }

        // Tạo thư mục và đường dẫn file Excel xuất: GenExcelExportFilePath (gate :681)
        var webRoot = _env.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
        {
            webRoot = Path.Combine(_env.ContentRootPath, "wwwroot");
        }

        // Dùng từ điển GetImportDicColums() (gate :679, :745-755) — dùng từ điển Import cho Export là của nguồn
        var dicColNames = GetImportDicColums();
        string virtualPath;
        try
        {
            string physicalPath;
            (physicalPath, virtualPath) = PortExcel.GenExcelExportFilePath(webRoot, "Mst_ResignReason");

            // Ghi danh sách ra file Excel với tên sheet "Mst_ResignReason" (gate :682)
            PortExcel.ExportToExcelFromList(rows, dicColNames, physicalPath, "Mst_ResignReason");
        }
        catch (Exception ex)
        {
            // Bắt ngoại lệ và trả Fail (gate :695-699)
            return PortResult.Fail("Mst_ResignReason_Export", "Exception", ex.Message);
        }

        // Trả URL tải file: Data = { Url = GetRequestBaseAddress() + url } (gate :684-687)
        return Ok(new
        {
            Success = true,
            Data = new
            {
                Url = PortExcel.GetRequestBaseAddress(Request) + virtualPath
            }
        });
    }

    /// <summary>
    /// Từ điển cột xuất Excel: dùng GetImportDicColums() (gate Mst_ResignReasonController.cs:679, :745-755) — dùng từ điển Import cho Export là của nguồn.
    /// Giữ đúng thứ tự 5 cột từ nguồn:
    /// ReasonID ("Mã lý do (*)"), ResignReason ("Lý do nghỉ việc (*)"), mnnt_NNTFullName ("Tổ chức"), ReasonDesc ("Mô tả"), FlagActive ("Trạng thái").
    /// </summary>
    private static Dictionary<string, string> GetImportDicColums()
    {
        return new Dictionary<string, string>
        {
            { "ReasonID", "Mã lý do (*)" },
            { "ResignReason", "Lý do nghỉ việc (*)" },
            { "mnnt_NNTFullName", "Tổ chức" },
            { "ReasonDesc", "Mô tả" },
            { "FlagActive", "Trạng thái" }
        };
    }

    private async Task<(int itemCount, List<object> rows)> GetXAsync(string? orgId, string? reasonId, string? keyWord, string? flagActive, long recordStart, long recordCount)
    {
        var q = _db.Set<Mst_ResignReason>().AsNoTracking().AsQueryable();

        // OrgID: loc OrgID = ... (Mst_ResignReasonController.cs:781, 809-812)
        if (!string.IsNullOrWhiteSpace(orgId))
        {
            // toán tử '=' (Mst_ResignReasonController.cs:779-782, 811): không phân biệt hoa/thường như SQL Server
            var oid = PortQuery.Equal(orgId.Trim());
            q = q.Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), oid, PortQuery.Esc));
        }

        // ReasonID: loc ReasonID = ... (Mst_ResignReasonController.cs:805-808)
        if (!string.IsNullOrWhiteSpace(reasonId))
        {
            var rid = PortQuery.Equal(reasonId.Trim());
            q = q.Where(x => EF.Functions.Like((x.ReasonID ?? "").ToLower(), rid, PortQuery.Esc));
        }

        // KeyWord: loc ResignReason like '%...%' (Mst_ResignReasonController.cs:777)
        if (!string.IsNullOrWhiteSpace(keyWord))
        {
            var kw = PortQuery.Contains(keyWord);
            q = q.Where(x => EF.Functions.Like((x.ResignReason ?? "").ToLower(), kw, PortQuery.Esc));
        }

        // FlagActive: cat chuoi bang dau phay, dung toan tu IN (Mst_ResignReasonController.cs:789-794) / toán tử = ở By_ReasonID (dòng 815)
        if (!string.IsNullOrWhiteSpace(flagActive))
        {
            var faArr = flagActive.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToList();
            q = q.Where(x => x.FlagActive != null && faArr.Contains(x.FlagActive));
        }

        // Draft: order by mrr.OrgID asc, mrr.ReasonID asc (MasterData.cs:24676-24678)
        var ordered = q.OrderBy(x => x.OrgID).ThenBy(x => x.ReasonID);
        var itemCount = await ordered.CountAsync(); // MyCount = so dong draft (MasterData.cs:24682)

        // Lay tu [start, start+count-1]
        var page = (recordCount <= 0 || recordStart < 0)
            ? new List<Mst_ResignReason>()
            : await ordered.Skip((int)Math.Min(recordStart, int.MaxValue)).Take((int)Math.Min(recordCount, int.MaxValue)).ToListAsync();

        // Select cuoi: left join Mst_NNT mnnt on t.OrgID = mnnt.OrgID (MasterData.cs:24719-24720)
        var orgIds = page.Select(p => p.OrgID).Where(x => !string.IsNullOrEmpty(x)).Select(x => x!).Distinct().ToList();
        // left join Mst_NNT theo OrgID (MasterData.cs:24719-24721): một OrgID có thể có nhiều dòng NNT -> lấy dòng đầu theo MST (như #197), tránh ToDictionary ném lỗi trùng khoá
        var nntRows = await _db.Set<Mst_NNT>().AsNoTracking()
            .Where(n => n.OrgID != null && orgIds.Contains(n.OrgID))
            .OrderBy(n => n.MST)
            .ToListAsync();
        var nntDict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var n in nntRows) { if (n.OrgID != null && !nntDict.ContainsKey(n.OrgID)) nntDict[n.OrgID] = n.NNTFullName; }

        var rows = page
            .Select((x, i) => (object)new
            {
                // select t.MyIdxSeq ... order by t.MyIdxSeq asc (MasterData.cs:24712, 24723)
                MyIdxSeq = recordStart + i,
                x.ReasonID,
                x.OrgID,
                x.NetworkID,
                x.ResignReason,
                x.ReasonDesc,
                x.FlagActive,
                x.Remark,
                x.LogLUDTimeUTC,
                x.LogLUBy,
                mnnt_NNTFullName = nntDict.GetValueOrDefault(x.OrgID) // mnnt.NNTFullName mnnt_NNTFullName (MasterData.cs:24714)
            })
            .ToList();

        return (itemCount, rows);
    }
}
