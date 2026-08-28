using Microsoft.AspNetCore.Mvc;
using MiniLoyalty.Data;
using MiniLoyalty.Models;
using MiniLoyalty.Services;

namespace MiniLoyalty.Controllers;

/// <summary>
/// API JSON cho SPA React. DTO phẳng. Dashboard cache Redis 20s theo tenant (X-Cache).
/// Khách hàng thân thiết: hội viên + hạng thẻ (tự xếp theo điểm trọn đời), tích/đổi điểm, quà. API POST /api/earn (MiniDMS gọi) giữ nguyên.
/// </summary>
[ApiController]
[Route("api/v1")]
[Produces("application/json")]
public class ApiV1Controller(ILoyaltyService svc, ICache cache, ITenantContext tenant) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        var key = $"loy:dash:{tenant.OrgId}";
        var hit = await cache.GetAsync<DashDto>(key);
        if (hit != null) { Response.Headers["X-Cache"] = "HIT"; return Ok(hit); }
        var d = await svc.DashboardAsync();
        var dto = new DashDto(d.Members, d.ActivePoints, d.LifetimeIssued,
            d.ByRank.Select(x => new RankCountDto(x.Rank, x.Color, x.Count)).ToList());
        await cache.SetAsync(key, dto, TimeSpan.FromSeconds(20));
        Response.Headers["X-Cache"] = "MISS";
        return Ok(dto);
    }

    [HttpGet("ranks")]
    public async Task<IActionResult> Ranks()
        => Ok((await svc.RanksAsync()).Select(r => new { r.Id, r.Name, r.MinLifetimePoints, r.DiscountPercent, r.ColorHex }));

    [HttpGet("members")]
    public async Task<IActionResult> Members([FromQuery] string? q, [FromQuery] int? rankId)
        => Ok((await svc.MembersAsync(q, rankId)).Select(m => new
        {
            m.Id, m.Code, m.Name, m.Phone, m.Email, m.Points, m.LifetimePoints,
            rank = m.RankTier?.Name, rankColor = m.RankTier?.ColorHex, discount = m.RankTier?.DiscountPercent, m.JoinedAt
        }));

    [HttpGet("members/{id:int}")]
    public async Task<IActionResult> Member(int id)
    {
        var m = await svc.GetAsync(id);
        if (m == null) return NotFound(new { error = "Không tìm thấy hội viên." });
        return Ok(new
        {
            m.Id, m.Code, m.Name, m.Phone, m.Email, m.Dob, m.Points, m.LifetimePoints,
            rank = m.RankTier?.Name, rankColor = m.RankTier?.ColorHex, discount = m.RankTier?.DiscountPercent, m.JoinedAt,
            transactions = m.Transactions.OrderByDescending(t => t.Id).Take(50).Select(t => new { type = t.Type.ToString(), t.Points, t.BalanceAfter, t.Note, t.RefNo, t.CreatedAt })
        });
    }

    [HttpPost("members")]
    public async Task<IActionResult> Create([FromBody] MemberReq r)
    {
        if (string.IsNullOrWhiteSpace(r.Name)) return BadRequest(new { error = "Cần tên hội viên." });
        var id = await svc.CreateAsync(new Member { Name = r.Name.Trim(), Phone = r.Phone, Email = r.Email, Dob = r.Dob });
        return Ok(new { id });
    }

    [HttpPost("members/{id:int}/earn")]
    public async Task<IActionResult> Earn(int id, [FromBody] EarnReq r)
    {
        if (r.Amount > 0) { var tx = await svc.EarnFromPurchaseAsync(id, r.Amount, r.RefNo); return Ok(new { points = tx.Points, balance = tx.BalanceAfter }); }
        var t = await svc.EarnAsync(id, r.Points, PointTxType.Adjust, r.Note, r.RefNo);
        return Ok(new { points = t.Points, balance = t.BalanceAfter });
    }

    [HttpPost("members/{id:int}/redeem")]
    public async Task<IActionResult> Redeem(int id, [FromBody] RedeemReq r)
    {
        var (ok, msg) = await svc.RedeemAsync(id, r.RewardId);
        return ok ? Ok(new { ok, msg }) : BadRequest(new { ok, error = msg });
    }

    [HttpGet("rewards")]
    public async Task<IActionResult> Rewards()
        => Ok((await svc.RewardsAsync(false)).Select(r => new { r.Id, r.Name, r.PointCost, r.Description, r.Stock, r.IsActive }));
}

public record DashDto(int Members, int ActivePoints, int LifetimeIssued, List<RankCountDto> ByRank);
public record RankCountDto(string Rank, string Color, int Count);

public class MemberReq { public string Name { get; set; } = ""; public string? Phone { get; set; } public string? Email { get; set; } public DateTime? Dob { get; set; } }
public class EarnReq { public int Points { get; set; } public decimal Amount { get; set; } public string? Note { get; set; } public string? RefNo { get; set; } }
public class RedeemReq { public int RewardId { get; set; } }
