using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ledger11.data;
using ledger11.model.Data;
using ledger11.web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace ledger11.web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger, AppDbContext appDbContext)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public async Task<IActionResult> SignIn(
        [FromServices] IAuthenticationSchemeProvider schemeProvider,
        [FromQuery] string? returnUrl)
    {
        var schemes = await schemeProvider.GetAllSchemesAsync();
        var oidc = schemes.FirstOrDefault(s => s.Name == "oidc");

        var redirectUrl = Url.IsLocalUrl(returnUrl)
            ? returnUrl
            : Url.Action("Index", "Start") ?? "/start";

        if (oidc != null)
        {
            return Challenge(new AuthenticationProperties
            {
                RedirectUri = redirectUrl
            }, oidc.Name);
        }

        return LocalRedirect(redirectUrl);
    }

    public IActionResult SignOutAll()
    {
        return SignOut(
            new AuthenticationProperties
            {
                RedirectUri = Url.Action("Index", "Home")
            },
            CookieAuthenticationDefaults.AuthenticationScheme,
            "oidc");
    }
    
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
