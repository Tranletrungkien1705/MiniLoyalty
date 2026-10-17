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

[ApiController]
[Route("api/prm_carnew")]
public class Prm_CarNewControllerPort : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration? _config;

    public Prm_CarNewControllerPort(AppDbContext db, IConfiguration? config = null)
    {
        _db = db;
        _config = config;
    }

    public class Prm_CarNewSearchReq
    {
        public string? OrgID { get; set; }
        public string? KeyWord { get; set; }
        public string? DLCPCode { get; set; }
        public string? PRMCNCodeSys { get; set; }
        public string? CreateDTimeUTCFrom { get; set; }
        public string? CreateDTimeUTCTo { get; set; }
        public string? PRMCNCode { get; set; }
        public string? PRMCNStatus { get; set; }
        public string? FlagShow { get; set; }
        public int Ft_PageIndex { get; set; } = 0;
        public int Ft_PageSize { get; set; } = 10;
    }

    public class Prm_CarNewGetByCodeSysReq
    {
        public string? PRMCNCodeSys { get; set; }
    }

    public class Prm_CarNewSaveReq
    {
        public Prm_CarNew? Prm_CarNew { get; set; }
        public List<Prm_CarNewDtl>? Lst_Prm_CarNewDtl { get; set; }
        public List<Prm_CarNewSpec>? Lst_Prm_CarNewSpec { get; set; }
    }

    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Prm_CarNewSearchReq? req)
    {
        req ??= new Prm_CarNewSearchReq();
        int pageIndex = req.Ft_PageIndex;
        int pageSize = req.Ft_PageSize;

        var query = _db.Set<Prm_CarNew>().AsQueryable();

        if (!string.IsNullOrEmpty(req.OrgID))
        {
            { var p_OrgID = PortQuery.Equal(req.OrgID); query = query.Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), p_OrgID, PortQuery.Esc)); }
        }
        if (!string.IsNullOrEmpty(req.DLCPCode))
        {
            { var p_DLCPCode = PortQuery.Equal(req.DLCPCode); query = query.Where(x => EF.Functions.Like((x.DLCPCode ?? "").ToLower(), p_DLCPCode, PortQuery.Esc)); }
        }
        if (!string.IsNullOrEmpty(req.PRMCNCodeSys))
        {
            { var p_PRMCNCodeSys = PortQuery.Equal(req.PRMCNCodeSys); query = query.Where(x => EF.Functions.Like((x.PRMCNCodeSys ?? "").ToLower(), p_PRMCNCodeSys, PortQuery.Esc)); }
        }
        if (!string.IsNullOrEmpty(req.PRMCNCode))
        {
            { var p_PRMCNCode = PortQuery.Equal(req.PRMCNCode); query = query.Where(x => EF.Functions.Like((x.PRMCNCode ?? "").ToLower(), p_PRMCNCode, PortQuery.Esc)); }
        }
        if (!string.IsNullOrEmpty(req.PRMCNStatus))
        {
            { var p_PRMCNStatus = PortQuery.Equal(req.PRMCNStatus); query = query.Where(x => EF.Functions.Like((x.PRMCNStatus ?? "").ToLower(), p_PRMCNStatus, PortQuery.Esc)); }
        }
        // KeyWord: gate strWhereClause_Prm_CarNew (Prm_CarNewController.cs:712-786) KHÔNG dùng KeyWord => bỏ qua (LESSONS A.18)
        if (!string.IsNullOrEmpty(req.CreateDTimeUTCFrom))
        {
            if (DateTime.TryParse(req.CreateDTimeUTCFrom + " 00:00:00", out var dtFrom))
            {
                query = query.Where(x => x.CreateDTimeUTC >= dtFrom);
            }
        }
        if (!string.IsNullOrEmpty(req.CreateDTimeUTCTo))
        {
            if (DateTime.TryParse(req.CreateDTimeUTCTo + " 23:59:59", out var dtTo))
            {
                query = query.Where(x => x.CreateDTimeUTC <= dtTo);
            }
        }

        // Bộ lọc FlagShow ở gate dùng DateTime.UtcNow (Prm_CarNewController.cs:812-813); cột FlagShow trả về dùng UtcNow+7 (biz :301)
        var todayFilterStr = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var todayStr = DateTime.UtcNow.AddHours(7).ToString("yyyy-MM-dd");
        if (!string.IsNullOrEmpty(req.FlagShow))
        {
            if (req.FlagShow == "1")
            {
                query = query.Where(x => x.PRMCNStatus == "FINISH" && string.Compare(x.EffDateStart, todayFilterStr) <= 0 && string.Compare(todayFilterStr, x.EffDateEnd) <= 0);
            }
            else if (req.FlagShow == "0")
            {
                query = query.Where(x => x.PRMCNStatus != "FINISH" || (x.PRMCNStatus == "FINISH" && (string.Compare(x.EffDateStart, todayFilterStr) > 0 || string.Compare(todayFilterStr, x.EffDateEnd) > 0)));
            }
        }

        var listAll = await query.OrderByDescending(x => x.CreateDTimeUTC).ToListAsync();
        var itemCount = listAll.Count;

        var pagedList = new List<Prm_CarNew>();
        if (itemCount > 0 && pageSize > 0 && pageIndex >= 0)
        {
            pagedList = listAll.Skip(pageIndex * pageSize).Take(pageSize).ToList();
        }
        else if (pageSize == 0)
        {
            pagedList = new List<Prm_CarNew>();
        }

        long idxSeq = pageIndex * pageSize;
        foreach (var item in pagedList)
        {
            idxSeq++;
            item.MyIdxSeq = idxSeq;
            if (item.PRMCNStatus == "FINISH" && string.Compare(item.EffDateStart, todayStr) <= 0 && string.Compare(todayStr, item.EffDateEnd) <= 0)
            {
                item.FlagShow = "1";
            }
            else
            {
                item.FlagShow = "0";
            }
            if (item.EffDateEnd == "2100-01-01")
            {
                item.EffDateEnd = "";
            }
        }

        int pageCount = 0;
        if (pageSize > 0)
        {
            pageCount = (itemCount % pageSize == 0) ? itemCount / pageSize : itemCount / pageSize + 1;
        }

        var pageInfo = new PageInfo<Prm_CarNew>(pagedList, itemCount, pageCount, pageIndex, pageSize);

        return Ok(new { Success = true, Data = pageInfo });
    }

    [HttpPost("getbyprmcncodesys")]
    public async Task<IActionResult> GetByPRMCNCodeSys([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Prm_CarNewGetByCodeSysReq? req)
    {
        string? prmCodeSys = req?.PRMCNCodeSys;
        if (string.IsNullOrEmpty(prmCodeSys))
        {
            // gate Prm_CarNewController.cs:170: PRMCNCodeSys rỗng => không truy vấn, vẫn SetSuccess với Data = { Prm_CarNew = null, Lst_Prm_CarNewDtl = null, Lst_Prm_CarNewSpec = null }
            return Ok(new { Success = true, Data = new { Prm_CarNew = (object?)null, Lst_Prm_CarNewDtl = (object?)null, Lst_Prm_CarNewSpec = (object?)null } });
        }

        var pat = PortQuery.Equal(prmCodeSys);
        var objPrm_CarNew = await _db.Set<Prm_CarNew>()
            .FirstOrDefaultAsync(x => EF.Functions.Like((x.PRMCNCodeSys ?? "").ToLower(), pat, PortQuery.Esc));

        object? formattedPrmCarNew = null;
        if (objPrm_CarNew != null)
        {
            string appDTime = objPrm_CarNew.ApprDTimeUTC.HasValue ? objPrm_CarNew.ApprDTimeUTC.Value.AddHours(7).ToString("yyyy-MM-dd HH:mm:ss") : "";
            string createDTime = objPrm_CarNew.CreateDTimeUTC.AddHours(7).ToString("yyyy-MM-dd HH:mm:ss");

            string effDateEnd = objPrm_CarNew.EffDateEnd;
            if (effDateEnd == "2100-01-01")
            {
                effDateEnd = "";
            }

            // FlagShow do biz tính (source_excerpt: case when FINISH và EffDateStart <= @dateSys <= EffDateEnd then '1' else '0'; @dateSys = UtcNow+7)
            var dateSys = DateTime.UtcNow.AddHours(7).ToString("yyyy-MM-dd");
            var flagShow = (objPrm_CarNew.PRMCNStatus == "FINISH" && string.Compare(objPrm_CarNew.EffDateStart, dateSys) <= 0 && string.Compare(dateSys, objPrm_CarNew.EffDateEnd) <= 0) ? "1" : "0";
            formattedPrmCarNew = new
            {
                objPrm_CarNew.PRMCNCodeSys,
                objPrm_CarNew.NetworkID,
                objPrm_CarNew.OrgID,
                objPrm_CarNew.DLCPCode,
                objPrm_CarNew.PRMCNCode,
                objPrm_CarNew.PRMCNName,
                EffDateStart = objPrm_CarNew.EffDateStart,
                EffDateEnd = effDateEnd,
                objPrm_CarNew.FlagAllModel,
                objPrm_CarNew.PointValAllModel,
                CreateDTimeUTC = createDTime,
                objPrm_CarNew.CreateBy,
                LUDTimeUTC = objPrm_CarNew.LUDTimeUTC,
                objPrm_CarNew.LUBy,
                ApprDTimeUTC = appDTime,
                objPrm_CarNew.ApprBy,
                FinishDTimeUTC = objPrm_CarNew.FinishDTimeUTC,
                objPrm_CarNew.FinishBy,
                CancelDTimeUTC = objPrm_CarNew.CancelDTimeUTC,
                objPrm_CarNew.CancelBy,
                objPrm_CarNew.PRMCNStatus,
                objPrm_CarNew.Remark,
                objPrm_CarNew.LogLUDTimeUTC,
                objPrm_CarNew.LogLUBy,
                MyIdxSeq = objPrm_CarNew.MyIdxSeq,
                FlagShow = flagShow
            };
        }

        var lstDtl = await _db.Set<Prm_CarNewDtl>()
            .Where(x => EF.Functions.Like((x.PRMCNCodeSys ?? "").ToLower(), pat, PortQuery.Esc))
            .OrderBy(x => x.Idx)
            .ToListAsync();

        var lstSpec = await _db.Set<Prm_CarNewSpec>()
            .Where(x => EF.Functions.Like((x.PRMCNCodeSys ?? "").ToLower(), pat, PortQuery.Esc))
            .OrderBy(x => x.Idx)
            .ToListAsync();

        return Ok(new
        {
            Success = true,
            Data = new
            {
                Prm_CarNew = formattedPrmCarNew,
                Lst_Prm_CarNewDtl = lstDtl.Count > 0 ? lstDtl : null,
                Lst_Prm_CarNewSpec = lstSpec.Count > 0 ? lstSpec : null
            }
        });
    }

    private static string? StdParam(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim().ToUpperInvariant();
    private static string? StdStr(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim().Normalize();
    private static string StdFlag(string? s) => s == "1" ? "1" : "0";
    private static string? StdDate(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        if (DateTime.TryParse(s, out var dt))
        {
            if (dt < DateTime.Parse("1900-01-01 00:00:00") || dt > DateTime.Parse("2100-01-01 23:59:59"))
                throw new Exception("MyBiz.DateTimeOutOfRange");
            return dt.ToString("yyyy-MM-dd");
        }
        return s.Trim();
    }

    /// <summary>
    /// Lưu thông tin chương trình khuyến mãi xe mới — port 1:1 từ Prm_CarNewController.Save (Gate :245-335, API :92-162, BIZ Promotion.cs:2419-2528, :1318-1482, :570-1316).
    /// </summary>
    [HttpPost("save")]
    public async Task<IActionResult> Save([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Prm_CarNewSaveReq? req)
    {
        // Gate Prm_CarNewController.cs:254-261: kiểm tra strJson rỗng hoặc null -> mã mặc định Prm_CarNew_Save
        if (req?.Prm_CarNew == null)
        {
            return PortResult.Fail("Prm_CarNew_Save", "Check.strJson", null);
        }

        var dtimeSys = DateTime.UtcNow;
        var todayStr = dtimeSys.ToString("yyyy-MM-dd");

        // Chuẩn hóa cờ xóa
        // Gate :263-319 không gán FlagIsDelete => luôn false.
        bool bIsDelete = false;

        // Sequence: Gate :298-299 sinh sequence và ghi đè PRMCNCodeSys.
        // Tuy nhiên Biz :648-675 lại hỗ trợ sửa bản ghi nếu đã tồn tại với status PENDING.
        // Quy tắc parity (đề bài #250): nếu client chưa truyền PRMCNCodeSys (hoặc rỗng) -> sinh mới qua PortSeq;
        // nếu client đã truyền mã cụ thể -> giữ nguyên mã để kiểm tra nghiệp vụ sửa/xóa.
        // Gate :279-299: luôn lấy mã mới từ Seq (SeqType.PRMCNCodeSys) và ghi đè Prm_CarNew.PRMCNCodeSys, mã client gửi bị bỏ qua.
        var seq = await PortSeq.NextAsync<Seq_PRMCNCodeSys>(_db);
        string? strPRMCNCodeSys = PortSeq.Build("{0}PRMCNCODE.{3}.{1:00000}{2}", "", seq, 100000, "", dtimeSys);

        // LỖI NGUỒN GIỮ NGUYÊN dòng 625: strPRMCNCode = StdParam(objPRMCNCodeSys) (gán nhầm objPRMCNCodeSys thay vì objPRMCNCode)
        string? strPRMCNCode = StdParam(strPRMCNCodeSys);
        string? strDLCPCode = StdParam(req.Prm_CarNew.DLCPCode);
        string? strPRMCNName = StdStr(req.Prm_CarNew.PRMCNName);
        string? strEffDateStart = StdDate(req.Prm_CarNew.EffDateStart);
        // LỖI NGUỒN GIỮ NGUYÊN dòng 630: strEffDateEnd = TConst.DateTimeSpecial.DateMax ("2100-01-01", bỏ qua client gửi)
        string strEffDateEnd = "2100-01-01";
        string strFlagAllModel = StdFlag(req.Prm_CarNew.FlagAllModel);
        double? dblPointValAllModel = req.Prm_CarNew.PointValAllModel;
        string strRemark = (req.Prm_CarNew.Remark ?? "").Trim();

        var networkIdHeader = Request.Headers["NetworkID"].FirstOrDefault();
        var orgIdHeader = Request.Headers["OrgID"].FirstOrDefault();
        var waUserCodeHeader = Request.Headers["WAUserCode"].FirstOrDefault();

        var strNetworkID = !string.IsNullOrWhiteSpace(networkIdHeader) ? networkIdHeader : (!string.IsNullOrWhiteSpace(req.Prm_CarNew.NetworkID) ? req.Prm_CarNew.NetworkID : (_config?["Biz_NetworkID"] ?? "1001"));
        var strOrgID = !string.IsNullOrWhiteSpace(orgIdHeader) ? orgIdHeader : (!string.IsNullOrWhiteSpace(req.Prm_CarNew.OrgID) ? req.Prm_CarNew.OrgID : (_config?["Biz_OrgID"] ?? "1001"));
        var strWAUserCode = !string.IsNullOrWhiteSpace(waUserCodeHeader) ? waUserCodeHeader : "port";

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // Kiểm tra bản ghi tồn tại trong DB theo PRMCNCodeSys (Promotion.cs:648-686)
            var prmPat = PortQuery.Equal(strPRMCNCodeSys ?? "");
            var existing = await _db.Set<Prm_CarNew>()
                .FirstOrDefaultAsync(x => EF.Functions.Like((x.PRMCNCodeSys ?? "").ToLower(), prmPat, PortQuery.Esc));

            DateTime createDTimeUtc;
            string strCreateBy;

            if (existing == null)
            {
                if (bIsDelete)
                {
                    // Promotion.cs:659-661: chưa tồn tại mà bIsDelete -> nhảy tới Done thành công
                    await tx.CommitAsync();
                    return Ok(new { Success = true, Data = (object?)null });
                }
                createDTimeUtc = dtimeSys;
                strCreateBy = strWAUserCode;
            }
            else
            {
                // Promotion.cs:665-681: nếu đã tồn tại, kiểm tra status phải là PENDING
                if (!string.Equals(existing.PRMCNStatus, "PENDING", StringComparison.OrdinalIgnoreCase))
                {
                    await tx.RollbackAsync();
                    return PortResult.Fail("Prm_CarNew_Save_InvalidPRMCNStatus",
                        "Check.DB.PRMCNStatus", existing.PRMCNStatus,
                        "Check.PRMCNStatus.Expected", "PENDING");
                }
                createDTimeUtc = existing.CreateDTimeUTC;
                strCreateBy = !string.IsNullOrEmpty(existing.CreateBy) ? existing.CreateBy : strWAUserCode;
                strNetworkID = !string.IsNullOrWhiteSpace(existing.NetworkID) ? existing.NetworkID : strNetworkID;
                strOrgID = !string.IsNullOrWhiteSpace(existing.OrgID) ? existing.OrgID : strOrgID;
            }

            // Kiểm tra chương trình khuyến mãi trước đó dt_Prm_CarNew_Previous (Promotion.cs:703-772)
            if (!bIsDelete)
            {
                var patDlcp = PortQuery.Equal(strDLCPCode ?? "");
                var patStatusFinish = PortQuery.Equal("FINISH");

                var prev = await _db.Set<Prm_CarNew>()
                    .Where(t => !EF.Functions.Like((t.PRMCNCodeSys ?? "").ToLower(), prmPat, PortQuery.Esc)
                             && EF.Functions.Like((t.DLCPCode ?? "").ToLower(), patDlcp, PortQuery.Esc)
                             && EF.Functions.Like((t.PRMCNStatus ?? "").ToLower(), patStatusFinish, PortQuery.Esc)
                             && string.Compare(t.EffDateStart, todayStr) <= 0
                             && string.Compare(todayStr, t.EffDateEnd) <= 0)
                    .OrderBy(t => t.EffDateStart)
                    .FirstOrDefaultAsync();

                if (prev != null)
                {
                    // strEffDateStart >= SysDate (Promotion.cs:748-759)
                    if (string.Compare(strEffDateStart, todayStr) < 0)
                    {
                        await tx.RollbackAsync();
                        return PortResult.Fail("Prm_CarNew_Save_InvalidEffDateStartBeforeEffSysDate",
                            "Check.strEffDateStart", strEffDateStart,
                            "Check.SysDate", todayStr);
                    }

                    // strEffDateStart > strEffDateStart_Previous (Promotion.cs:758-771)
                    string strEffDateStart_Previous = StdDate(prev.EffDateStart) ?? prev.EffDateStart;
                    if (string.Compare(strEffDateStart, strEffDateStart_Previous) <= 0)
                    {
                        await tx.RollbackAsync();
                        return PortResult.Fail("Prm_CarNew_Save_InvalidEffDateStartBeforeEffDateStartPrevious",
                            "Check.strEffDateStart", strEffDateStart,
                            "Check.PRMCNCodeSys_Previous", prev.PRMCNCodeSys,
                            "Check.strEffDateStart_Previous", strEffDateStart_Previous);
                    }
                }
            }

            // Kiểm tra bảng chi tiết Prm_CarNewDtl (Promotion.cs:920-997)
            if (!bIsDelete)
            {
                if (req.Lst_Prm_CarNewDtl == null)
                {
                    await tx.RollbackAsync();
                    return PortResult.Fail("Prm_CarNew_Save_Input_Prm_CarNewDtlTblNotFound", "Check.TableName", "Prm_CarNewDtl");
                }

                for (int nScan = 0; nScan < req.Lst_Prm_CarNewDtl.Count; nScan++)
                {
                    var dtl = req.Lst_Prm_CarNewDtl[nScan];
                    double dblPointVal = dtl.PointVal ?? 0.0;
                    if (dblPointVal <= 0.0)
                    {
                        await tx.RollbackAsync();
                        return PortResult.Fail("Prm_CarNew_Save_Input_InvalidValue", "Check.objPointVal", dtl.PointVal ?? (object)dblPointVal);
                    }
                }

                // Kiểm tra bảng chi tiết Prm_CarNewSpec (Promotion.cs:1022-1082)
                if (req.Lst_Prm_CarNewSpec == null)
                {
                    await tx.RollbackAsync();
                    return PortResult.Fail("Prm_CarNew_Save_Input_Prm_CarNewSpecTblNotFound", "Check.TableName", "Prm_CarNewSpec");
                }
            }

            // Xóa dữ liệu cũ (Clear All theo PRMCNCodeSys) (Promotion.cs:1118-1144)
            var oldSpecs = await _db.Set<Prm_CarNewSpec>()
                .Where(x => EF.Functions.Like((x.PRMCNCodeSys ?? "").ToLower(), prmPat, PortQuery.Esc))
                .ToListAsync();
            _db.Set<Prm_CarNewSpec>().RemoveRange(oldSpecs);

            var oldDtls = await _db.Set<Prm_CarNewDtl>()
                .Where(x => EF.Functions.Like((x.PRMCNCodeSys ?? "").ToLower(), prmPat, PortQuery.Esc))
                .ToListAsync();
            _db.Set<Prm_CarNewDtl>().RemoveRange(oldDtls);

            var oldHeaders = await _db.Set<Prm_CarNew>()
                .Where(x => EF.Functions.Like((x.PRMCNCodeSys ?? "").ToLower(), prmPat, PortQuery.Esc))
                .ToListAsync();
            _db.Set<Prm_CarNew>().RemoveRange(oldHeaders);

            await _db.SaveChangesAsync();

            // Nếu không phải xóa -> chèn lại toàn bộ dữ liệu mới (Insert All) (Promotion.cs:1151-1290)
            if (!bIsDelete)
            {
                var newHeader = new Prm_CarNew
                {
                    PRMCNCodeSys = strPRMCNCodeSys!,
                    NetworkID = strNetworkID,
                    OrgID = strOrgID,
                    DLCPCode = strDLCPCode ?? "",
                    PRMCNCode = strPRMCNCode ?? strPRMCNCodeSys!,
                    PRMCNName = strPRMCNName,
                    EffDateStart = strEffDateStart ?? "",
                    EffDateEnd = strEffDateEnd,
                    FlagAllModel = strFlagAllModel,
                    PointValAllModel = dblPointValAllModel,
                    CreateDTimeUTC = createDTimeUtc,
                    CreateBy = strCreateBy,
                    LUDTimeUTC = createDTimeUtc, // LỖI NGUỒN GIỮ NGUYÊN dòng 896: gán bằng strCreateDTimeUTC
                    LUBy = strCreateBy,
                    ApprDTimeUTC = null,
                    ApprBy = null,
                    FinishDTimeUTC = null,
                    FinishBy = null,
                    CancelDTimeUTC = null,
                    CancelBy = null,
                    PRMCNStatus = "PENDING",
                    Remark = strRemark,
                    LogLUDTimeUTC = dtimeSys,
                    LogLUBy = strWAUserCode
                };
                _db.Set<Prm_CarNew>().Add(newHeader);

                for (int i = 0; i < req.Lst_Prm_CarNewDtl!.Count; i++)
                {
                    var dtl = req.Lst_Prm_CarNewDtl[i];
                    _db.Set<Prm_CarNewDtl>().Add(new Prm_CarNewDtl
                    {
                        PRMCNCodeSys = strPRMCNCodeSys!,
                        Idx = dtl.Idx > 0 ? dtl.Idx : (i + 1),
                        NetworkID = strNetworkID,
                        OrgID = strOrgID,
                        PointVal = dtl.PointVal,
                        PRMCNStatusDtl = "PENDING",
                        Remark = dtl.Remark,
                        LogLUDTimeUTC = dtimeSys,
                        LogLUBy = strWAUserCode
                    });
                }

                for (int i = 0; i < req.Lst_Prm_CarNewSpec!.Count; i++)
                {
                    var spec = req.Lst_Prm_CarNewSpec[i];
                    _db.Set<Prm_CarNewSpec>().Add(new Prm_CarNewSpec
                    {
                        PRMCNCodeSys = strPRMCNCodeSys!,
                        Idx = spec.Idx > 0 ? spec.Idx : (i + 1),
                        ModelCode = StdParam(spec.ModelCode) ?? "",
                        NetworkID = strNetworkID,
                        OrgID = strOrgID,
                        PRMCNStatusSpec = "PENDING",
                        LogLUDTimeUTC = dtimeSys,
                        LogLUBy = strWAUserCode
                    });
                }

                await _db.SaveChangesAsync();
            }

            await tx.CommitAsync();
            return Ok(new { Success = true, Data = (object?)null });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return PortResult.Fail("Prm_CarNew_Save", "Exception", ex.Message);
        }
    }
}
