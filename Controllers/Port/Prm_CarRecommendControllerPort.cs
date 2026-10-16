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

    public Prm_CarRecommendControllerPort(AppDbContext context)
    {
        _context = context;
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
