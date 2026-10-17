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

    public class Prm_CarNewApproveReq
    {
        public string? PRMCNCodeSys { get; set; }
        public string? Remark { get; set; }
        public string? WAUserCode { get; set; }
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

    /// <summary>
    /// Phê duyệt chương trình khuyến mãi xe mới — port 1:1 từ Prm_CarNewController.Approve (Gate :339-400, API :165-235, BIZ Promotion.cs:2530-2606, :1626-1745, :1484-1624, :29-115).
    /// </summary>
    [HttpPost("approve")]
    public async Task<IActionResult> Approve([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Prm_CarNewApproveReq? req)
    {
        var codeSys = req?.PRMCNCodeSys;
        if (string.IsNullOrWhiteSpace(codeSys))
        {
            // Gate Prm_CarNewController.cs:358, :386: PRMCNCodeSys rỗng hoặc null -> ném ServiceException("invalid_PRMCNCodeSys", "invalid_PRMCNCodeSys")
            return PortResult.Fail("invalid_PRMCNCodeSys", "Check.PRMCNCodeSys", codeSys);
        }

        string? strPRMCNCodeSys = StdParam(codeSys);
        string strRemark = (req?.Remark ?? "").Trim();

        var waUserCodeHeader = Request.Headers["WAUserCode"].FirstOrDefault();
        string strWAUserCode = !string.IsNullOrWhiteSpace(waUserCodeHeader)
            ? waUserCodeHeader
            : (!string.IsNullOrWhiteSpace(req?.WAUserCode) ? req.WAUserCode : "port");

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var pat = PortQuery.Equal(strPRMCNCodeSys ?? "");
            var existing = await _db.Set<Prm_CarNew>()
                .FirstOrDefaultAsync(x => EF.Functions.Like((x.PRMCNCodeSys ?? "").ToLower(), pat, PortQuery.Esc));

            // Promotion.cs:64-74 (Prm_CarNew_CheckDB strFlagExistToCheck="1"): không tìm thấy bản ghi -> Prm_CarNew_CheckDB_CarNewNotFound
            if (existing == null)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Prm_CarNew_CheckDB_CarNewNotFound", "Check.PRMCNCodeSys", strPRMCNCodeSys);
            }

            // Promotion.cs:88-99 (Prm_CarNew_CheckDB strStatusListToCheck="PENDING"): PRMCNStatus khác PENDING -> Prm_CarNew_CheckDB_StatusNotMatched
            if (!string.Equals(existing.PRMCNStatus, "PENDING", StringComparison.OrdinalIgnoreCase))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Prm_CarNew_CheckDB_StatusNotMatched",
                    "Check.PRMCNCodeSys", strPRMCNCodeSys,
                    "Check.strStatusListToCheck", "PENDING",
                    "DB.Status", existing.PRMCNStatus);
            }

            var dtimeSys = DateTime.UtcNow;

            // Promotion.cs:1585-1606 (SaveDB):
            // Cập nhật LogLUDTimeUTC, LogLUBy, Remark, ApprDTimeUTC, ApprBy, PRMCNStatus
            existing.LogLUDTimeUTC = dtimeSys;
            existing.LogLUBy = strWAUserCode;
            existing.Remark = strRemark;
            existing.ApprDTimeUTC = dtimeSys;
            existing.ApprBy = strWAUserCode;
            existing.PRMCNStatus = "APPROVE";

            // LỖI NGUỒN GIỮ NGUYÊN dòng 1585-1592: Cột LUDTimeUTC và LUBy có trong bảng tạm #input_Prm_CarNew (:1555-1556)
            // nhưng bị BỎ SÓT trong mệnh đề SET của lệnh UPDATE. Do đó LUDTimeUTC và LUBy giữ nguyên giá trị cũ.
            // Các bảng chi tiết Prm_CarNewDtl và Prm_CarNewSpec KHÔNG bị cập nhật (giữ nguyên trạng thái cũ).

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return Ok(new { Success = true, Data = (object?)null });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return PortResult.Fail("Prm_CarNew_Appr", "Exception", ex.Message);
        }
    }

    // Dùng chung cho Finish/Cancel: Prm_CarNew_CheckDB (Promotion.cs:64-99) + cập nhật trạng thái (Promotion.cs:1785-1976, 2099-2296).
    private async Task<IActionResult> ChangeStatusAsync(Prm_CarNewApproveReq? req, string funcErrCode, string statusListToCheck, string newStatus)
    {
        var codeSys = req?.PRMCNCodeSys;
        if (string.IsNullOrWhiteSpace(codeSys))
        {
            return PortResult.Fail("invalid_PRMCNCodeSys", "Check.PRMCNCodeSys", codeSys);
        }

        string? strPRMCNCodeSys = StdParam(codeSys);
        string strRemark = (req?.Remark ?? "").Trim();

        var waUserCodeHeader = Request.Headers["WAUserCode"].FirstOrDefault();
        string strWAUserCode = !string.IsNullOrWhiteSpace(waUserCodeHeader)
            ? waUserCodeHeader
            : (!string.IsNullOrWhiteSpace(req?.WAUserCode) ? req.WAUserCode : "port");

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var pat = PortQuery.Equal(strPRMCNCodeSys ?? "");
            var existing = await _db.Set<Prm_CarNew>()
                .FirstOrDefaultAsync(x => EF.Functions.Like((x.PRMCNCodeSys ?? "").ToLower(), pat, PortQuery.Esc));

            if (existing == null)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Prm_CarNew_CheckDB_CarNewNotFound", "Check.PRMCNCodeSys", strPRMCNCodeSys);
            }

            var okStatuses = statusListToCheck.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (!okStatuses.Contains(existing.PRMCNStatus ?? "", StringComparer.OrdinalIgnoreCase))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Prm_CarNew_CheckDB_StatusNotMatched",
                    "Check.PRMCNCodeSys", strPRMCNCodeSys,
                    "Check.strStatusListToCheck", statusListToCheck,
                    "DB.Status", existing.PRMCNStatus);
            }

            var dtimeSys = DateTime.UtcNow;

            // LỖI NGUỒN GIỮ NGUYÊN: LUDTimeUTC/LUBy có trong bảng tạm nhưng KHÔNG nằm trong mệnh đề SET => không đổi.
            existing.LogLUDTimeUTC = dtimeSys;
            existing.LogLUBy = strWAUserCode;
            existing.Remark = strRemark;
            existing.PRMCNStatus = newStatus;
            if (newStatus == "FINISH")
            {
                existing.FinishDTimeUTC = dtimeSys;
                existing.FinishBy = strWAUserCode;
            }
            else
            {
                existing.CancelDTimeUTC = dtimeSys;
                existing.CancelBy = strWAUserCode;
            }
            await _db.SaveChangesAsync();

            if (newStatus == "FINISH")
            {
                // Promotion.cs:1886-1971 (Update EffDateStart): chương trình FINISH trước đó của cùng đại lý đang hiệu lực hôm nay.
                var todayStr = dtimeSys.ToString("yyyy-MM-dd");
                var patDlcp = PortQuery.Equal(existing.DLCPCode ?? "");
                var prev = await _db.Set<Prm_CarNew>()
                    .Where(t => t.PRMCNCodeSys != existing.PRMCNCodeSys
                             && EF.Functions.Like((t.DLCPCode ?? "").ToLower(), patDlcp, PortQuery.Esc)
                             && t.PRMCNStatus == "FINISH"
                             && string.Compare(t.EffDateStart, todayStr) <= 0
                             && string.Compare(t.EffDateEnd, todayStr) >= 0)
                    .OrderBy(t => t.EffDateStart)
                    .FirstOrDefaultAsync();
                if (prev != null)
                {
                    var dateEffDateStart = DateTime.Parse(existing.EffDateStart, System.Globalization.CultureInfo.InvariantCulture);
                    var dateToday = DateTime.Parse(todayStr, System.Globalization.CultureInfo.InvariantCulture);
                    prev.EffDateEnd = (dateEffDateStart <= dateToday ? dateToday : dateEffDateStart).AddDays(-1).ToString("yyyy-MM-dd");
                    await _db.SaveChangesAsync();
                }
            }

            await tx.CommitAsync();
            return Ok(new { Success = true, Data = (object?)null });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return PortResult.Fail(funcErrCode, "Exception", ex.Message);
        }
    }

    [HttpPost("finish")]
    public Task<IActionResult> Finish([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Prm_CarNewApproveReq? req)
        => ChangeStatusAsync(req, "Prm_CarNew_Finish", "APPROVE", "FINISH");

    [HttpPost("cancel")]
    public Task<IActionResult> Cancel([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Prm_CarNewApproveReq? req)
        => ChangeStatusAsync(req, "Prm_CarNew_Cancel", "PENDING, APPROVE", "CANCEL");

    // Gate Delete(strJson) :469-535 => WA_Prm_CarNew_Save với FlagIsDelete = "1"; mã client gửi (không sinh mã mới).
    [HttpPost("delete")]
    public async Task<IActionResult> Delete([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Prm_CarNewSaveReq? req)
    {
        if (req?.Prm_CarNew == null)
        {
            return PortResult.Fail("Prm_CarNew_Save", "Check.strJson", null);
        }

        string? strPRMCNCodeSys = StdParam(req.Prm_CarNew.PRMCNCodeSys);
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var prmPat = PortQuery.Equal(strPRMCNCodeSys ?? "");
            var existing = await _db.Set<Prm_CarNew>()
                .FirstOrDefaultAsync(x => EF.Functions.Like((x.PRMCNCodeSys ?? "").ToLower(), prmPat, PortQuery.Esc));
            if (existing == null)
            {
                await tx.CommitAsync();
                return Ok(new { Success = true, Data = (object?)null });
            }
            if (!string.Equals(existing.PRMCNStatus, "PENDING", StringComparison.OrdinalIgnoreCase))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Prm_CarNew_Save_InvalidPRMCNStatus",
                    "Check.DB.PRMCNStatus", existing.PRMCNStatus,
                    "Check.PRMCNStatus.Expected", "PENDING");
            }

            _db.Set<Prm_CarNewSpec>().RemoveRange(await _db.Set<Prm_CarNewSpec>()
                .Where(x => EF.Functions.Like((x.PRMCNCodeSys ?? "").ToLower(), prmPat, PortQuery.Esc)).ToListAsync());
            _db.Set<Prm_CarNewDtl>().RemoveRange(await _db.Set<Prm_CarNewDtl>()
                .Where(x => EF.Functions.Like((x.PRMCNCodeSys ?? "").ToLower(), prmPat, PortQuery.Esc)).ToListAsync());
            _db.Set<Prm_CarNew>().RemoveRange(await _db.Set<Prm_CarNew>()
                .Where(x => EF.Functions.Like((x.PRMCNCodeSys ?? "").ToLower(), prmPat, PortQuery.Esc)).ToListAsync());
            await _db.SaveChangesAsync();
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

public class Prm_CarNewApproveSeeder : IPortSeeder
{
    public int Order => 79;

    public async Task SeedAsync(AppDbContext db)
    {
        if (!await db.Set<Prm_CarNew>().AnyAsync(x => x.PRMCNCodeSys == "PCN_APP_01"))
        {
            var seedDTime1 = new DateTime(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);
            var seedDTime2 = new DateTime(2026, 10, 2, 8, 0, 0, DateTimeKind.Utc);
            var seedDTime3 = new DateTime(2026, 10, 3, 8, 0, 0, DateTimeKind.Utc);
            var seedDTime4 = new DateTime(2026, 10, 4, 8, 0, 0, DateTimeKind.Utc);

            db.Set<Prm_CarNew>().AddRange(
                new Prm_CarNew
                {
                    PRMCNCodeSys = "PCN_APP_01",
                    NetworkID = "1001",
                    OrgID = "1001",
                    DLCPCode = "DL01",
                    PRMCNCode = "PCN_CODE_01",
                    PRMCNName = "Chuong trinh khuyen mai 01",
                    EffDateStart = "2026-01-01",
                    EffDateEnd = "2026-12-31",
                    PRMCNStatus = "PENDING",
                    Remark = "Ghi chu goc PCN_APP_01",
                    CreateDTimeUTC = seedDTime1,
                    CreateBy = "seed",
                    LUDTimeUTC = seedDTime1,
                    LUBy = "seed"
                },
                new Prm_CarNew
                {
                    PRMCNCodeSys = "PCN_APP_02",
                    NetworkID = "1001",
                    OrgID = "1001",
                    DLCPCode = "DL01",
                    PRMCNCode = "PCN_CODE_02",
                    PRMCNName = "Chuong trinh khuyen mai 02",
                    EffDateStart = "2026-02-01",
                    EffDateEnd = "2026-12-31",
                    PRMCNStatus = "PENDING",
                    Remark = "Ghi chu goc PCN_APP_02",
                    CreateDTimeUTC = seedDTime2,
                    CreateBy = "seed",
                    LUDTimeUTC = seedDTime2,
                    LUBy = "seed"
                },
                new Prm_CarNew
                {
                    PRMCNCodeSys = "PCN_APP_STATUS_ERR",
                    NetworkID = "1001",
                    OrgID = "1001",
                    DLCPCode = "DL01",
                    PRMCNCode = "PCN_CODE_ERR",
                    PRMCNName = "Chuong trinh da approve",
                    EffDateStart = "2026-03-01",
                    EffDateEnd = "2026-12-31",
                    PRMCNStatus = "APPROVE",
                    Remark = "Da approve truoc do",
                    CreateDTimeUTC = seedDTime3,
                    CreateBy = "seed",
                    LUDTimeUTC = seedDTime3,
                    LUBy = "seed"
                },
                new Prm_CarNew
                {
                    PRMCNCodeSys = "PCN_APP_FINISH_ERR",
                    NetworkID = "1001",
                    OrgID = "1001",
                    DLCPCode = "DL01",
                    PRMCNCode = "PCN_CODE_FINISH",
                    PRMCNName = "Chuong trinh da finish",
                    EffDateStart = "2026-04-01",
                    EffDateEnd = "2026-12-31",
                    PRMCNStatus = "FINISH",
                    Remark = "Da finish truoc do",
                    CreateDTimeUTC = seedDTime4,
                    CreateBy = "seed",
                    LUDTimeUTC = seedDTime4,
                    LUBy = "seed"
                }
            );

            db.Set<Prm_CarNewDtl>().Add(new Prm_CarNewDtl
            {
                PRMCNCodeSys = "PCN_APP_01",
                Idx = 1,
                NetworkID = "1001",
                OrgID = "1001",
                PointVal = 100,
                PRMCNStatusDtl = "PENDING",
                Remark = "Chi tiet 01 goc"
            });

            db.Set<Prm_CarNewSpec>().Add(new Prm_CarNewSpec
            {
                PRMCNCodeSys = "PCN_APP_01",
                Idx = 1,
                ModelCode = "MODEL_01",
                NetworkID = "1001",
                OrgID = "1001",
                PRMCNStatusSpec = "PENDING"
            });

            await db.SaveChangesAsync();
        }
    }
}

