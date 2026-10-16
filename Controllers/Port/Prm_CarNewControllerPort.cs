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
[Route("api/prm_carnew")]
public class Prm_CarNewControllerPort : ControllerBase
{
    private readonly AppDbContext _db;

    public Prm_CarNewControllerPort(AppDbContext db)
    {
        _db = db;
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
}
