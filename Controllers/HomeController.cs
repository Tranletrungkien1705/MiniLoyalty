using Microsoft.AspNetCore.Mvc;
using MiniLoyalty.Services;

namespace MiniLoyalty.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => Redirect("/index.html");   // SPA React ở "/"
}

public class LegacyController(ILoyaltyService svc) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewBag.Dash = await svc.DashboardAsync();
        ViewBag.Top = (await svc.MembersAsync(null, null)).Take(6).ToList();
        return View("~/Views/Home/Index.cshtml");
    }
}
