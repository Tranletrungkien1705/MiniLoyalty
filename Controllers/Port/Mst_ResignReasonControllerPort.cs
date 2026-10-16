using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

[ApiController]
[Route("api/mst_resignreason")]
[Produces("application/json")]
public class Mst_ResignReasonControllerPort : ControllerBase
{
    private readonly AppDbContext _db;

    public Mst_ResignReasonControllerPort(AppDbContext db)
    {
        _db = db;
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
    /// - Trình tự CreateX: OrgID rỗng -> ReasonID rỗng -> Mst_ResignReason_CheckDB(No) -> Mst_Org_CheckDB(Yes, Active) -> ResignReason rỗng -> CheckResignReason(No) -> lưu.
    /// </summary>
    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] Mst_ResignReason? req, [FromServices] Microsoft.Extensions.Configuration.IConfiguration _config)
    {
        // Body rỗng (Mst_ResignReasonController.cs:239-242) -> mã mặc định của Create
        if (req == null)
        {
            return PortResult.Fail("Mst_ResignReason_Create", "Check.strJson", null);
        }

        // Gate :256-276: lấy ReasonID từ sequence TRƯỚC khi vào transaction lưu (transaction riêng, đã commit độc lập)
        var seq = await PortSeq.NextAsync<Seq_ReasonID>(_db);
        var strReasonID = (PortSeq.Build("{0}REASONID.{3}.{1:00000}{2}", "", seq, 100000, "", DateTime.UtcNow) ?? "").Trim().ToUpper();

        // Chuẩn hoá đầu vào (MasterData.cs:25039-25043): StdParam = Trim + ToUpper; StdStr = Trim
        var strOrgID = string.IsNullOrWhiteSpace(req.OrgID) ? null : req.OrgID.Trim().ToUpper();
        var strResignReason = string.IsNullOrWhiteSpace(req.ResignReason) ? null : req.ResignReason.Trim();
        var strReasonDesc = string.IsNullOrWhiteSpace(req.ReasonDesc) ? null : req.ReasonDesc.Trim();
        var strRemark = string.IsNullOrWhiteSpace(req.Remark) ? null : req.Remark.Trim();

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

            return Ok(new { Success = true, Data = (object?)null }); // Mst_ResignReasonController.cs:295-296
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return PortResult.Fail("Mst_ResignReason_Create", "Exception", ex.Message); // mã mặc định (MasterData.cs:25246)
        }
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
            if (bUpd_ResignReason && !string.Equals(strResignReason, strResignReason_DB, StringComparison.OrdinalIgnoreCase))
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
        var orgIds = page.Select(p => p.OrgID).Distinct().ToList();
        // left join Mst_NNT theo OrgID (MasterData.cs:24719-24721): một OrgID có thể có nhiều dòng NNT -> lấy dòng đầu theo MST (như #197), tránh ToDictionary ném lỗi trùng khoá
        var nntRows = await _db.Set<Mst_NNT>().AsNoTracking()
            .Where(n => orgIds.Contains(n.OrgID))
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
