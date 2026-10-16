using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniLoyalty.Data;
using MiniLoyalty.Models.Port;

namespace MiniLoyalty.Controllers.Port;

public class InvF_TempPrint_GetByTempPrintTypeReq
{
    public string? TempPrintType { get; set; }
}

[ApiController]
[Route("api/invf_tempprint")]
[Produces("application/json")]
public class InvF_TempPrintControllerPort : ControllerBase
{
    private readonly AppDbContext _db;

    public InvF_TempPrintControllerPort(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost("getbytempprinttype")]
    public async Task<IActionResult> GetByTempPrintType([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] InvF_TempPrint_GetByTempPrintTypeReq? req)
    {
        var tempPrintType = req?.TempPrintType;
        InvF_TempPrint? objInvF_TempPrint = null;

        if (tempPrintType != null && !string.IsNullOrEmpty(tempPrintType))
        {
            objInvF_TempPrint = await _db.Set<InvF_TempPrint>()
                .Where(x => x.TempPrintType != null && EF.Functions.Like(x.TempPrintType.ToLower(), PortQuery.Equal(tempPrintType), PortQuery.Esc))
                .OrderBy(x => x.IF_TempPrintNo)
                .FirstOrDefaultAsync();
        }

        return Ok(new
        {
            Success = true,
            Data = new
            {
                InvF_TempPrint = objInvF_TempPrint
            }
        });
    }
}
