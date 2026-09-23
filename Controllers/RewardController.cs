using Microsoft.AspNetCore.Mvc;
using MiniLoyalty.Services;

namespace MiniLoyalty.Controllers;

public class RewardController(ILoyaltyService svc) : Controller
{
    public async Task<IActionResult> Index() => View(await svc.RewardsAsync(activeOnly: false));
}
