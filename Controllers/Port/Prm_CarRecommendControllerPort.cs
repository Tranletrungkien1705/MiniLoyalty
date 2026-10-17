using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniLoyalty.Data;
using MiniLoyalty.Models.Port;

namespace MiniLoyalty.Controllers.Port;

[ApiController]
[Route("api/prm_carrecommend")]
public class Prm_CarRecommendControllerPort : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly Microsoft.Extensions.Configuration.IConfiguration? _config;

    public Prm_CarRecommendControllerPort(AppDbContext context, Microsoft.Extensions.Configuration.IConfiguration? config = null)
    {
        _context = context;
        _config = config;
    }

    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Prm_CarRecommendSearchReq? req)
    {
        req ??= new Prm_CarRecommendSearchReq();

        var query = _context.Set<Prm_CarRecommend>().AsQueryable();

        if (!string.IsNullOrEmpty(req.OrgID))
        {
            var p_OrgID = PortQuery.Equal(req.OrgID);
            query = query.Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), p_OrgID, PortQuery.Esc));
        }

        if (!string.IsNullOrEmpty(req.PRMCRCodeSys))
        {
            var p_PRMCRCodeSys = PortQuery.Equal(req.PRMCRCodeSys);
            query = query.Where(x => EF.Functions.Like((x.PRMCRCodeSys ?? "").ToLower(), p_PRMCRCodeSys, PortQuery.Esc));
        }

        // PRMCRName: nguon Prm_CarRecommendController.cs:763-766 kiem tra data.PRMCRName nhung LIKE dung "%"+data.KeyWord+"%" (bug nguon, giu 1-1)
        if (!string.IsNullOrEmpty(req.PRMCRName))
        {
            var p_Name = PortQuery.Contains(req.KeyWord ?? "");
            query = query.Where(x => EF.Functions.Like((x.PRMCRName ?? "").ToLower(), p_Name, PortQuery.Esc));
        }

        if (!string.IsNullOrEmpty(req.PRMCRCode))
        {
            var p_PRMCRCode = PortQuery.Equal(req.PRMCRCode);
            query = query.Where(x => EF.Functions.Like((x.PRMCRCode ?? "").ToLower(), p_PRMCRCode, PortQuery.Esc));
        }

        if (!string.IsNullOrEmpty(req.DLCPCode))
        {
            var p_DLCPCode = PortQuery.Equal(req.DLCPCode);
            query = query.Where(x => EF.Functions.Like((x.DLCPCode ?? "").ToLower(), p_DLCPCode, PortQuery.Esc));
        }

        if (!string.IsNullOrEmpty(req.EffDateStart))
        {
            var p_EffDateStart = PortQuery.Equal(req.EffDateStart);
            query = query.Where(x => EF.Functions.Like((x.EffDateStart ?? "").ToLower(), p_EffDateStart, PortQuery.Esc));
        }

        if (!string.IsNullOrEmpty(req.CreateDTimeUTCFrom))
        {
            if (DateTime.TryParse(req.CreateDTimeUTCFrom.Contains(' ') ? req.CreateDTimeUTCFrom : req.CreateDTimeUTCFrom + " 00:00:00", out var dtFrom))
            {
                query = query.Where(x => x.CreateDTimeUTC >= dtFrom);
            }
        }

        if (!string.IsNullOrEmpty(req.CreateDTimeUTCTo))
        {
            if (DateTime.TryParse(req.CreateDTimeUTCTo.Contains(' ') ? req.CreateDTimeUTCTo : req.CreateDTimeUTCTo + " 23:59:59", out var dtTo))
            {
                query = query.Where(x => x.CreateDTimeUTC <= dtTo);
            }
        }

        var todayFilterStr = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var todayStr = DateTime.UtcNow.AddHours(7).ToString("yyyy-MM-dd");
        if (!string.IsNullOrEmpty(req.FlagShow))
        {
            if (req.FlagShow == "1")
            {
                query = query.Where(x => x.PRMCRStatus == "FINISH" && string.Compare(x.EffDateStart, todayFilterStr) <= 0 && string.Compare(todayFilterStr, x.EffDateEnd) <= 0);
            }
            else if (req.FlagShow == "0")
            {
                query = query.Where(x => x.PRMCRStatus != "FINISH" || (x.PRMCRStatus == "FINISH" && (string.Compare(x.EffDateStart, todayFilterStr) > 0 || string.Compare(todayFilterStr, x.EffDateEnd) > 0)));
            }
        }

        var listAll = await query.OrderByDescending(x => x.CreateDTimeUTC).ToListAsync();
        var itemCount = listAll.Count;
        int pageIndex = req.Ft_PageIndex < 0 ? 0 : req.Ft_PageIndex;
        int pageSize = req.Ft_PageSize < 0 ? 10 : req.Ft_PageSize;

        var pagedList = new List<Prm_CarRecommend>();
        if (itemCount > 0 && pageSize > 0 && pageIndex >= 0)
        {
            pagedList = listAll.Skip(pageIndex * pageSize).Take(pageSize).ToList();
        }
        else if (pageSize == 0)
        {
            pagedList = new List<Prm_CarRecommend>();
        }

        long idxSeq = pageIndex * pageSize;
        foreach (var item in pagedList)
        {
            idxSeq++;
            item.MyIdxSeq = idxSeq;
            if (item.PRMCRStatus == "FINISH" && string.Compare(item.EffDateStart, todayStr) <= 0 && string.Compare(todayStr, item.EffDateEnd) <= 0)
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

        var pageInfo = new PageInfo<Prm_CarRecommend>(pagedList, itemCount, pageCount, pageIndex, pageSize);

        return Ok(new { Success = true, Data = pageInfo });
    }

    /// <summary>
    /// Port 1:1 Prm_CarRecommendController.GetByPRMCRCodeSys (gate Prm_CarRecommendController.cs:~149-235, #257).
    /// PRMCRCodeSys rỗng => không truy vấn, Data = { Prm_CarRecommend = null, Lst_Prm_CarRecommendDtl = null, Lst_Prm_CarRecommendSpec = null }.
    /// Khi có: lấy bản ghi đầu (Ft_RecordCount = "1"); ApprDTimeUTC/CreateDTimeUTC đổi sang giờ địa phương (utcOffset mặc định 7, gate :201-208) thành chuỗi;
    /// EffDateEnd = "2100-01-01" => "" (gate :209-212); FlagShow do biz tính theo ngày UTC+7 (biz :3286-3298); Dtl/Spec rỗng => null.
    /// Lưu ý DDL nguồn: Prm_CarRecommendDtl/Spec dùng cột PRMCNCodeSys làm khoá liên kết với Prm_CarRecommend.PRMCRCodeSys.
    /// </summary>
    [HttpPost("getbyprmcrcodesys")]
    public async Task<IActionResult> GetByPRMCRCodeSys([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Prm_CarRecommendGetByCodeSysReq? req)
    {
        var codeSys = req?.PRMCRCodeSys;
        if (string.IsNullOrEmpty(codeSys))
        {
            return Ok(new { Success = true, Data = new { Prm_CarRecommend = (object?)null, Lst_Prm_CarRecommendDtl = (object?)null, Lst_Prm_CarRecommendSpec = (object?)null } });
        }

        var pat = PortQuery.Equal(codeSys);
        var cr = await _context.Set<Prm_CarRecommend>().AsNoTracking()
            .Where(x => EF.Functions.Like((x.PRMCRCodeSys ?? "").ToLower(), pat, PortQuery.Esc))
            .OrderByDescending(x => x.CreateDTimeUTC)
            .FirstOrDefaultAsync();

        object? header = null;
        if (cr != null)
        {
            var dateSys = DateTime.UtcNow.AddHours(7).ToString("yyyy-MM-dd");
            var flagShow = (cr.PRMCRStatus == "FINISH" && string.Compare(cr.EffDateStart, dateSys) <= 0 && string.Compare(dateSys, cr.EffDateEnd) <= 0) ? "1" : "0";
            header = new
            {
                cr.PRMCRCodeSys, cr.NetworkID, cr.OrgID, cr.DLCPCode, cr.PRMCRCode, cr.PRMCRName, cr.EffDateStart,
                EffDateEnd = cr.EffDateEnd == "2100-01-01" ? "" : cr.EffDateEnd,
                cr.FlagAllModel, cr.PointValAllModel,
                CreateDTimeUTC = cr.CreateDTimeUTC?.AddHours(7).ToString("yyyy-MM-dd HH:mm:ss"),
                cr.CreateBy, cr.LUDTimeUTC, cr.LUBy,
                ApprDTimeUTC = cr.ApprDTimeUTC?.AddHours(7).ToString("yyyy-MM-dd HH:mm:ss"),
                cr.ApprBy, cr.FinishDTimeUTC, cr.FinishBy, cr.CancelDTimeUTC, cr.CancelBy, cr.PRMCRStatus, cr.Remark, cr.LogLUDTimeUTC, cr.LogLUBy,
                FlagShow = flagShow
            };
        }

        var dtl = await _context.Set<Prm_CarRecommendDtl>().AsNoTracking()
            .Where(x => EF.Functions.Like((x.PRMCNCodeSys ?? "").ToLower(), pat, PortQuery.Esc)).OrderBy(x => x.Idx).ToListAsync();
        var spec = await _context.Set<Prm_CarRecommendSpec>().AsNoTracking()
            .Where(x => EF.Functions.Like((x.PRMCNCodeSys ?? "").ToLower(), pat, PortQuery.Esc)).OrderBy(x => x.Idx).ToListAsync();

        return Ok(new
        {
            Success = true,
            Data = new
            {
                Prm_CarRecommend = header,
                Lst_Prm_CarRecommendDtl = dtl.Count > 0 ? dtl : null,
                Lst_Prm_CarRecommendSpec = spec.Count > 0 ? spec : null
            }
        });
    }

    public class Prm_CarRecommendSaveReq
    {
        public Prm_CarRecommend? Prm_CarRecommend { get; set; }
        public List<Prm_CarRecommendDtl>? Lst_Prm_CarRecommendDtl { get; set; }
        public List<Prm_CarRecommendSpec>? Lst_Prm_CarRecommendSpec { get; set; }
    }

    public class Prm_CarRecommendApproveReq
    {
        public string? PRMCRCodeSys { get; set; }
        public string? Remark { get; set; }
        public string? WAUserCode { get; set; }
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
    public async Task<IActionResult> Save([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Prm_CarRecommendSaveReq? req)
    {
        // Gate Prm_CarNewController.cs:254-261: kiểm tra strJson rỗng hoặc null -> mã mặc định Prm_CarRecommend_Save
        if (req?.Prm_CarRecommend == null)
        {
            return PortResult.Fail("Prm_CarRecommend_Save", "Check.strJson", null);
        }

        var dtimeSys = DateTime.UtcNow;
        var todayStr = dtimeSys.ToString("yyyy-MM-dd");

        // Chuẩn hóa cờ xóa
        // Gate :263-319 không gán FlagIsDelete => luôn false.
        bool bIsDelete = false;

        // Sequence: Gate :298-299 sinh sequence và ghi đè PRMCRCodeSys.
        // Tuy nhiên Biz :648-675 lại hỗ trợ sửa bản ghi nếu đã tồn tại với status PENDING.
        // Quy tắc parity (đề bài #250): nếu client chưa truyền PRMCRCodeSys (hoặc rỗng) -> sinh mới qua PortSeq;
        // nếu client đã truyền mã cụ thể -> giữ nguyên mã để kiểm tra nghiệp vụ sửa/xóa.
        // Gate :279-299: luôn lấy mã mới từ Seq (SeqType.PRMCRCodeSys) và ghi đè Prm_CarRecommend.PRMCRCodeSys, mã client gửi bị bỏ qua.
        var seq = await PortSeq.NextAsync<Seq_PRMCRCodeSys>(_context);
        string? strPRMCRCodeSys = PortSeq.Build("{0}PRMCRCODE.{3}.{1:00000}{2}", "", seq, 100000, "", dtimeSys);

        // LỖI NGUỒN GIỮ NGUYÊN dòng 625: strPRMCRCode = StdParam(objPRMCRCodeSys) (gán nhầm objPRMCRCodeSys thay vì objPRMCRCode)
        string? strPRMCRCode = StdParam(strPRMCRCodeSys);
        string? strDLCPCode = null;
        // Gate Prm_CarRecommendController.cs:303-340: DLCPCode bị ghi đè bằng đại lý đầu tiên có FlagActive = '1' và FlagRoot = '1'.
        var rootDealer = await _context.Set<Mst_DealerCP>().AsNoTracking()
            .Where(d => d.FlagActive == "1" && d.FlagRoot == "1")
            .OrderBy(d => d.DLCPCode)
            .FirstOrDefaultAsync();
        if (rootDealer == null)
        {
            return PortResult.Fail("Prm_CarRecommend_Save", "Exception", "Object reference not set to an instance of an object.");
        }
        strDLCPCode = StdParam(rootDealer.DLCPCode);
        string? strPRMCRName = StdStr(req.Prm_CarRecommend.PRMCRName);
        string? strEffDateStart = StdDate(req.Prm_CarRecommend.EffDateStart);
        // LỖI NGUỒN GIỮ NGUYÊN dòng 630: strEffDateEnd = TConst.DateTimeSpecial.DateMax ("2100-01-01", bỏ qua client gửi)
        string strEffDateEnd = "2100-01-01";
        string strFlagAllModel = StdFlag(req.Prm_CarRecommend.FlagAllModel);
        double? dblPointValAllModel = req.Prm_CarRecommend.PointValAllModel;
        string? strRemark = StdStr(req.Prm_CarRecommend.Remark);

        var networkIdHeader = Request.Headers["NetworkID"].FirstOrDefault();
        var orgIdHeader = Request.Headers["OrgID"].FirstOrDefault();
        var waUserCodeHeader = Request.Headers["WAUserCode"].FirstOrDefault();

        var strNetworkID = !string.IsNullOrWhiteSpace(networkIdHeader) ? networkIdHeader : (!string.IsNullOrWhiteSpace(req.Prm_CarRecommend.NetworkID) ? req.Prm_CarRecommend.NetworkID : (_config?["Biz_NetworkID"] ?? "1001"));
        var strOrgID = !string.IsNullOrWhiteSpace(orgIdHeader) ? orgIdHeader : (!string.IsNullOrWhiteSpace(req.Prm_CarRecommend.OrgID) ? req.Prm_CarRecommend.OrgID : (_config?["Biz_OrgID"] ?? "1001"));
        var strWAUserCode = !string.IsNullOrWhiteSpace(waUserCodeHeader) ? waUserCodeHeader : "port";

        await using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            // Kiểm tra bản ghi tồn tại trong DB theo PRMCRCodeSys (Promotion.cs:648-686)
            var prmPat = PortQuery.Equal(strPRMCRCodeSys ?? "");
            var existing = await _context.Set<Prm_CarRecommend>()
                .FirstOrDefaultAsync(x => EF.Functions.Like((x.PRMCRCodeSys ?? "").ToLower(), prmPat, PortQuery.Esc));

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
                if (!string.Equals(existing.PRMCRStatus, "PENDING", StringComparison.OrdinalIgnoreCase))
                {
                    await tx.RollbackAsync();
                    return PortResult.Fail("Prm_CarRecommend_Save_InvalidPRMCNStatus",
                        "Check.DB.PRMCRStatus", existing.PRMCRStatus,
                        "Check.PRMCRStatus.Expected", "PENDING");
                }
                createDTimeUtc = existing.CreateDTimeUTC ?? dtimeSys;
                strCreateBy = !string.IsNullOrEmpty(existing.CreateBy) ? existing.CreateBy : strWAUserCode;
                strNetworkID = !string.IsNullOrWhiteSpace(existing.NetworkID) ? existing.NetworkID : strNetworkID;
                strOrgID = !string.IsNullOrWhiteSpace(existing.OrgID) ? existing.OrgID : strOrgID;
            }

            // Kiểm tra chương trình khuyến mãi trước đó dt_Prm_CarRecommend_Previous (Promotion.cs:703-772)
            if (!bIsDelete)
            {
                var patDlcp = PortQuery.Equal(strDLCPCode ?? "");
                var patStatusFinish = PortQuery.Equal("FINISH");

                var prev = await _context.Set<Prm_CarRecommend>()
                    .Where(t => !EF.Functions.Like((t.PRMCRCodeSys ?? "").ToLower(), prmPat, PortQuery.Esc)
                             && EF.Functions.Like((t.DLCPCode ?? "").ToLower(), patDlcp, PortQuery.Esc)
                             && EF.Functions.Like((t.PRMCRStatus ?? "").ToLower(), patStatusFinish, PortQuery.Esc)
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
                        return PortResult.Fail("Prm_CarRecommend_Save_InvalidEffDateStartBeforeEffSysDate",
                            "Check.strEffDateStart", strEffDateStart,
                            "Check.SysDate", todayStr);
                    }

                    // strEffDateStart > strEffDateStart_Previous (Promotion.cs:758-771)
                    string strEffDateStart_Previous = StdDate(prev.EffDateStart) ?? prev.EffDateStart;
                    if (string.Compare(strEffDateStart, strEffDateStart_Previous) <= 0)
                    {
                        await tx.RollbackAsync();
                        return PortResult.Fail("Prm_CarRecommend_Save_InvalidEffDateStartBeforeEffDateStartPrevious",
                            "Check.strEffDateStart", strEffDateStart,
                            "Check.PRMCRCodeSys_Previous", prev.PRMCRCodeSys,
                            "Check.strEffDateStart_Previous", strEffDateStart_Previous);
                    }
                }
            }

            // Kiểm tra bảng chi tiết Prm_CarRecommendDtl (Promotion.cs:920-997)
            if (!bIsDelete)
            {
                if (req.Lst_Prm_CarRecommendDtl == null)
                {
                    await tx.RollbackAsync();
                    return PortResult.Fail("Prm_CarRecommend_Save_Input_Prm_CarRecommendDtlTblNotFound", "Check.TableName", "Prm_CarRecommendDtl");
                }

                for (int nScan = 0; nScan < req.Lst_Prm_CarRecommendDtl.Count; nScan++)
                {
                    var dtl = req.Lst_Prm_CarRecommendDtl[nScan];
                    double dblPointVal = dtl.PointVal ?? 0.0;
                    if (dblPointVal <= 0.0)
                    {
                        await tx.RollbackAsync();
                        return PortResult.Fail("Prm_CarRecommend_Save_Input_InvalidValue", "Check.objPointVal", dtl.PointVal ?? (object)dblPointVal);
                    }
                }

                // Kiểm tra bảng chi tiết Prm_CarRecommendSpec (Promotion.cs:1022-1082)
                if (req.Lst_Prm_CarRecommendSpec == null)
                {
                    await tx.RollbackAsync();
                    return PortResult.Fail("Prm_CarRecommend_Save_Input_Prm_CarRecommendSpecTblNotFound", "Check.TableName", "Prm_CarRecommendSpec");
                }
            }

            // Xóa dữ liệu cũ (Clear All theo PRMCRCodeSys) (Promotion.cs:1118-1144)
            var oldSpecs = await _context.Set<Prm_CarRecommendSpec>()
                .Where(x => EF.Functions.Like((x.PRMCNCodeSys ?? "").ToLower(), prmPat, PortQuery.Esc))
                .ToListAsync();
            _context.Set<Prm_CarRecommendSpec>().RemoveRange(oldSpecs);

            var oldDtls = await _context.Set<Prm_CarRecommendDtl>()
                .Where(x => EF.Functions.Like((x.PRMCNCodeSys ?? "").ToLower(), prmPat, PortQuery.Esc))
                .ToListAsync();
            _context.Set<Prm_CarRecommendDtl>().RemoveRange(oldDtls);

            var oldHeaders = await _context.Set<Prm_CarRecommend>()
                .Where(x => EF.Functions.Like((x.PRMCRCodeSys ?? "").ToLower(), prmPat, PortQuery.Esc))
                .ToListAsync();
            _context.Set<Prm_CarRecommend>().RemoveRange(oldHeaders);

            await _context.SaveChangesAsync();

            // Nếu không phải xóa -> chèn lại toàn bộ dữ liệu mới (Insert All) (Promotion.cs:1151-1290)
            if (!bIsDelete)
            {
                var newHeader = new Prm_CarRecommend
                {
                    PRMCRCodeSys = strPRMCRCodeSys!,
                    NetworkID = strNetworkID,
                    OrgID = strOrgID,
                    DLCPCode = strDLCPCode ?? "",
                    PRMCRCode = strPRMCRCode ?? strPRMCRCodeSys!,
                    PRMCRName = strPRMCRName,
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
                    PRMCRStatus = "PENDING",
                    Remark = strRemark,
                    LogLUDTimeUTC = dtimeSys,
                    LogLUBy = strWAUserCode
                };
                _context.Set<Prm_CarRecommend>().Add(newHeader);

                for (int i = 0; i < req.Lst_Prm_CarRecommendDtl!.Count; i++)
                {
                    var dtl = req.Lst_Prm_CarRecommendDtl[i];
                    _context.Set<Prm_CarRecommendDtl>().Add(new Prm_CarRecommendDtl
                    {
                        PRMCNCodeSys = strPRMCRCodeSys!,
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

                for (int i = 0; i < req.Lst_Prm_CarRecommendSpec!.Count; i++)
                {
                    var spec = req.Lst_Prm_CarRecommendSpec[i];
                    _context.Set<Prm_CarRecommendSpec>().Add(new Prm_CarRecommendSpec
                    {
                        PRMCNCodeSys = strPRMCRCodeSys!,
                        Idx = spec.Idx > 0 ? spec.Idx : (i + 1),
                        ModelCode = StdParam(spec.ModelCode) ?? "",
                        NetworkID = strNetworkID,
                        OrgID = strOrgID,
                        PRMCNStatusSpec = "PENDING",
                        LogLUDTimeUTC = dtimeSys,
                        LogLUBy = strWAUserCode
                    });
                }

                await _context.SaveChangesAsync();
            }

            await tx.CommitAsync();
            return Ok(new { Success = true, Data = (object?)null });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return PortResult.Fail("Prm_CarRecommend_Save", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Phê duyệt chương trình khuyến mãi xe mới — port 1:1 từ Prm_CarNewController.Approve (Gate :339-400, API :165-235, BIZ Promotion.cs:2530-2606, :1626-1745, :1484-1624, :29-115).
    /// </summary>
    [HttpPost("approve")]
    public async Task<IActionResult> Approve([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Prm_CarRecommendApproveReq? req)
    {
        var codeSys = req?.PRMCRCodeSys;
        if (string.IsNullOrWhiteSpace(codeSys))
        {
            // Gate Prm_CarNewController.cs:358, :386: PRMCRCodeSys rỗng hoặc null -> ném ServiceException("invalid_PRMCRCodeSys", "invalid_PRMCRCodeSys")
            return PortResult.Fail("invalid_PRMCRCodeSys", "Check.PRMCRCodeSys", codeSys);
        }

        string? strPRMCRCodeSys = StdParam(codeSys);
        string strRemark = (req?.Remark ?? "").Trim();

        var waUserCodeHeader = Request.Headers["WAUserCode"].FirstOrDefault();
        string strWAUserCode = !string.IsNullOrWhiteSpace(waUserCodeHeader)
            ? waUserCodeHeader
            : (!string.IsNullOrWhiteSpace(req?.WAUserCode) ? req.WAUserCode : "port");

        await using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            var pat = PortQuery.Equal(strPRMCRCodeSys ?? "");
            var existing = await _context.Set<Prm_CarRecommend>()
                .FirstOrDefaultAsync(x => EF.Functions.Like((x.PRMCRCodeSys ?? "").ToLower(), pat, PortQuery.Esc));

            // Promotion.cs:64-74 (Prm_CarRecommend_CheckDB strFlagExistToCheck="1"): không tìm thấy bản ghi -> Prm_CarRecommend_CheckDB_CarNewNotFound
            if (existing == null)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Prm_CarRecommend_CheckDB_CarNewNotFound", "Check.PRMCRCodeSys", strPRMCRCodeSys);
            }

            // Promotion.cs:88-99 (Prm_CarRecommend_CheckDB strStatusListToCheck="PENDING"): PRMCRStatus khác PENDING -> Prm_CarRecommend_CheckDB_StatusNotMatched
            if (!string.Equals(existing.PRMCRStatus, "PENDING", StringComparison.OrdinalIgnoreCase))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Prm_CarRecommend_CheckDB_StatusNotMatched",
                    "Check.PRMCRCodeSys", strPRMCRCodeSys,
                    "Check.strStatusListToCheck", "PENDING",
                    "DB.Status", existing.PRMCRStatus);
            }

            var dtimeSys = DateTime.UtcNow;

            // Promotion.cs:1585-1606 (SaveDB):
            // Cập nhật LogLUDTimeUTC, LogLUBy, Remark, ApprDTimeUTC, ApprBy, PRMCRStatus
            existing.LogLUDTimeUTC = dtimeSys;
            existing.LogLUBy = strWAUserCode;
            existing.Remark = strRemark;
            existing.ApprDTimeUTC = dtimeSys;
            existing.ApprBy = strWAUserCode;
            existing.PRMCRStatus = "APPROVE";

            // LỖI NGUỒN GIỮ NGUYÊN dòng 1585-1592: Cột LUDTimeUTC và LUBy có trong bảng tạm #input_Prm_CarRecommend (:1555-1556)
            // nhưng bị BỎ SÓT trong mệnh đề SET của lệnh UPDATE. Do đó LUDTimeUTC và LUBy giữ nguyên giá trị cũ.
            // Các bảng chi tiết Prm_CarRecommendDtl và Prm_CarRecommendSpec KHÔNG bị cập nhật (giữ nguyên trạng thái cũ).

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            return Ok(new { Success = true, Data = (object?)null });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return PortResult.Fail("Prm_CarRecommend_Appr", "Exception", ex.Message);
        }
    }

    // Dùng chung cho Finish/Cancel: Prm_CarRecommend_CheckDB (Promotion.cs:64-99) + cập nhật trạng thái (Promotion.cs:1785-1976, 2099-2296).
    private async Task<IActionResult> ChangeStatusAsync(Prm_CarRecommendApproveReq? req, string funcErrCode, string statusListToCheck, string newStatus)
    {
        var codeSys = req?.PRMCRCodeSys;
        if (string.IsNullOrWhiteSpace(codeSys))
        {
            return PortResult.Fail("invalid_PRMCRCodeSys", "Check.PRMCRCodeSys", codeSys);
        }

        string? strPRMCRCodeSys = StdParam(codeSys);
        string strRemark = (req?.Remark ?? "").Trim();

        var waUserCodeHeader = Request.Headers["WAUserCode"].FirstOrDefault();
        string strWAUserCode = !string.IsNullOrWhiteSpace(waUserCodeHeader)
            ? waUserCodeHeader
            : (!string.IsNullOrWhiteSpace(req?.WAUserCode) ? req.WAUserCode : "port");

        await using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            var pat = PortQuery.Equal(strPRMCRCodeSys ?? "");
            var existing = await _context.Set<Prm_CarRecommend>()
                .FirstOrDefaultAsync(x => EF.Functions.Like((x.PRMCRCodeSys ?? "").ToLower(), pat, PortQuery.Esc));

            if (existing == null)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Prm_CarRecommend_CheckDB_CarNewNotFound", "Check.PRMCRCodeSys", strPRMCRCodeSys);
            }

            var okStatuses = statusListToCheck.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (!okStatuses.Contains(existing.PRMCRStatus ?? "", StringComparer.OrdinalIgnoreCase))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Prm_CarRecommend_CheckDB_StatusNotMatched",
                    "Check.PRMCRCodeSys", strPRMCRCodeSys,
                    "Check.strStatusListToCheck", statusListToCheck,
                    "DB.Status", existing.PRMCRStatus);
            }

            var dtimeSys = DateTime.UtcNow;

            // LỖI NGUỒN GIỮ NGUYÊN: LUDTimeUTC/LUBy có trong bảng tạm nhưng KHÔNG nằm trong mệnh đề SET => không đổi.
            existing.LogLUDTimeUTC = dtimeSys;
            existing.LogLUBy = strWAUserCode;
            existing.Remark = strRemark;
            existing.PRMCRStatus = newStatus;
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
            await _context.SaveChangesAsync();

            if (newStatus == "FINISH")
            {
                // Promotion.cs:1886-1971 (Update EffDateStart): chương trình FINISH trước đó của cùng đại lý đang hiệu lực hôm nay.
                var todayStr = dtimeSys.ToString("yyyy-MM-dd");
                var patDlcp = PortQuery.Equal(existing.DLCPCode ?? "");
                var prev = await _context.Set<Prm_CarRecommend>()
                    .Where(t => t.PRMCRCodeSys != existing.PRMCRCodeSys
                             && EF.Functions.Like((t.DLCPCode ?? "").ToLower(), patDlcp, PortQuery.Esc)
                             && t.PRMCRStatus == "FINISH"
                             && string.Compare(t.EffDateStart, todayStr) <= 0
                             && string.Compare(t.EffDateEnd, todayStr) >= 0)
                    .OrderBy(t => t.EffDateStart)
                    .FirstOrDefaultAsync();
                if (prev != null)
                {
                    var dateEffDateStart = DateTime.Parse(existing.EffDateStart, System.Globalization.CultureInfo.InvariantCulture);
                    var dateToday = DateTime.Parse(todayStr, System.Globalization.CultureInfo.InvariantCulture);
                    prev.EffDateEnd = (dateEffDateStart <= dateToday ? dateToday : dateEffDateStart).AddDays(-1).ToString("yyyy-MM-dd");
                    await _context.SaveChangesAsync();
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
    public Task<IActionResult> Finish([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Prm_CarRecommendApproveReq? req)
        => ChangeStatusAsync(req, "Prm_CarRecommend_Finish", "APPROVE", "FINISH");

    [HttpPost("cancel")]
    public Task<IActionResult> Cancel([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Prm_CarRecommendApproveReq? req)
        => ChangeStatusAsync(req, "Prm_CarRecommend_Cancel", "PENDING, APPROVE", "CANCEL");

    // Gate Delete(strJson) :469-535 => WA_Prm_CarRecommend_Save với FlagIsDelete = "1"; mã client gửi (không sinh mã mới).
    [HttpPost("delete")]
    public async Task<IActionResult> Delete([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Prm_CarRecommendSaveReq? req)
    {
        if (req?.Prm_CarRecommend == null)
        {
            return PortResult.Fail("Prm_CarRecommend_Save", "Check.strJson", null);
        }

        string? strPRMCRCodeSys = StdParam(req.Prm_CarRecommend.PRMCRCodeSys);
        await using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            var prmPat = PortQuery.Equal(strPRMCRCodeSys ?? "");
            var existing = await _context.Set<Prm_CarRecommend>()
                .FirstOrDefaultAsync(x => EF.Functions.Like((x.PRMCRCodeSys ?? "").ToLower(), prmPat, PortQuery.Esc));
            if (existing == null)
            {
                await tx.CommitAsync();
                return Ok(new { Success = true, Data = (object?)null });
            }
            if (!string.Equals(existing.PRMCRStatus, "PENDING", StringComparison.OrdinalIgnoreCase))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Prm_CarRecommend_Save_InvalidPRMCNStatus",
                    "Check.DB.PRMCRStatus", existing.PRMCRStatus,
                    "Check.PRMCRStatus.Expected", "PENDING");
            }

            _context.Set<Prm_CarRecommendSpec>().RemoveRange(await _context.Set<Prm_CarRecommendSpec>()
                .Where(x => EF.Functions.Like((x.PRMCNCodeSys ?? "").ToLower(), prmPat, PortQuery.Esc)).ToListAsync());
            _context.Set<Prm_CarRecommendDtl>().RemoveRange(await _context.Set<Prm_CarRecommendDtl>()
                .Where(x => EF.Functions.Like((x.PRMCNCodeSys ?? "").ToLower(), prmPat, PortQuery.Esc)).ToListAsync());
            _context.Set<Prm_CarRecommend>().RemoveRange(await _context.Set<Prm_CarRecommend>()
                .Where(x => EF.Functions.Like((x.PRMCRCodeSys ?? "").ToLower(), prmPat, PortQuery.Esc)).ToListAsync());
            await _context.SaveChangesAsync();
            await tx.CommitAsync();
            return Ok(new { Success = true, Data = (object?)null });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return PortResult.Fail("Prm_CarRecommend_Save", "Exception", ex.Message);
        }
    }

    // ===================== #263: CalcPrm =====================
    // Gate Prm_CarRecommendController.cs:639 (CalcPrm(string DLCPCode, string ModelCode)) -> WA_Prm_CarRecommend_CalcPrm -> Promotion.Calc.cs (Prm_CarRecommend_CalcPrmX :627).
    public class Prm_CarRecommendCalcPrmReq
    {
        public string? DLCPCode { get; set; }
        public string? ModelCode { get; set; }
    }

    [HttpPost("calcprm")]
    public async Task<IActionResult> CalcPrm([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Prm_CarRecommendCalcPrmReq? req)
    {
        if (string.IsNullOrEmpty(req?.DLCPCode))
        {
            return PortResult.Fail("invalid_DLCPCode", "Check.DLCPCode", req?.DLCPCode);
        }
        if (string.IsNullOrEmpty(req.ModelCode))
        {
            return PortResult.Fail("invalid_ModelCode", "Check.ModelCode", req.ModelCode);
        }
        string? strDLCPCode = StdParam(req.DLCPCode);
        string? strModelCode = StdParam(req.ModelCode);

        // Mst_CarModel_CheckDB(strModelCode, FlagExist "1", FlagActive "1") (Master.Loyalty.cs:1843-1906).
        var patModel = PortQuery.Equal(strModelCode ?? "");
        var model = await _context.Set<Mst_CarModel>().AsNoTracking()
            .FirstOrDefaultAsync(m => EF.Functions.Like((m.ModelCode ?? "").ToLower(), patModel, PortQuery.Esc));
        if (model == null)
        {
            return PortResult.Fail("Mst_CarModel_CheckDB_CarModelNotFound", "Check.ModelCode", strModelCode);
        }
        if (!"1".Contains(model.FlagActive ?? ""))
        {
            return PortResult.Fail("Mst_CarModel_CheckDB_FlagActiveNotMatched",
                "Check.ModelCode", strModelCode, "Check.strFlagActiveListToCheck", "1", "DB.FlagActive", model.FlagActive);
        }

        // #tbl_Prm_CarRecommend_Active: top 1 chương trình FINISH của đại lý có EffDateStart <= hôm nay <= EffDateEnd (không có order by).
        var todayStr = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var patDlcp = PortQuery.Equal(strDLCPCode ?? "");
        var active = await _context.Set<Prm_CarRecommend>().AsNoTracking()
            .Where(t => EF.Functions.Like((t.DLCPCode ?? "").ToLower(), patDlcp, PortQuery.Esc)
                     && t.PRMCRStatus == "FINISH"
                     && string.Compare(t.EffDateStart, todayStr) <= 0 && string.Compare(todayStr, t.EffDateEnd) <= 0)
            .OrderBy(t => t.PRMCRCodeSys)
            .FirstOrDefaultAsync();

        if (active == null)
        {
            return Ok(new { Success = true, Data = new { Prm_CarRecommend = (object?)null, PointVal = 0 } });
        }

        if (active.FlagAllModel != "1")
        {
            // LỖI NGUỒN GIỮ NGUYÊN (gate): Lst_Prm_CarRecommendDtl luôn null => Lst_Prm_CarRecommendDtl[0] ném NullReferenceException.
            throw new NullReferenceException("Object reference not set to an instance of an object.");
        }
        return Ok(new { Success = true, Data = new { Prm_CarRecommend = active, PointVal = active.PointValAllModel } });
    }
}

public class Prm_CarRecommendSearchReq
{
    public string? OrgID { get; set; }
    public string? KeyWord { get; set; }
    public string? PRMCRCodeSys { get; set; }
    public string? PRMCRName { get; set; }
    public string? PRMCRCode { get; set; }
    public string? DLCPCode { get; set; }
    public string? PRMCRStatus { get; set; }
    public string? EffDateStart { get; set; }
    public string? CreateDTimeUTCFrom { get; set; }
    public string? CreateDTimeUTCTo { get; set; }
    public string? FlagShow { get; set; }
    public int Ft_PageIndex { get; set; } = 0;
    public int Ft_PageSize { get; set; } = 10;
}

public class Prm_CarRecommendGetByCodeSysReq
{
    public string? PRMCRCodeSys { get; set; }
}
