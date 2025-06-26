using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ledger11.data;
using ledger11.model.Data;
using ledger11.web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;

namespace ledger11.web.Controllers;

public class HomeController : Controller
{
    private readonly IHostEnvironment _hostEnvironment;
    private readonly IConfiguration _configuration;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        ILogger<HomeController> logger,
        IHostEnvironment hostEnvironment,
        IConfiguration configuration)
    {
        _logger = logger;
        _hostEnvironment = hostEnvironment;
        _configuration = configuration;
    }

    public IActionResult Index()
    {

        return View();
    }

    [HttpGet("/api/version")]
    public async Task<IActionResult> Version()
    {
        var filePath = Path.Combine(_hostEnvironment.ContentRootPath, "version.txt");

        if (!System.IO.File.Exists(filePath))
        {
            return NotFound("Version file not found.");
        }

        var lines = await System.IO.File.ReadAllLinesAsync(filePath);
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in lines)
        {
            var parts = line.Split(':', 2);
            if (parts.Length == 2)
            {
                var key = parts[0].Trim().ToLower();  // normalize keys to lowercase
                var value = parts[1].Trim();
                result[key] = value;
            }
        }

        return Ok(result);
    }

#if DEBUG
    public IActionResult CheckHeaders()
    {
        var result = new
        {
            // Original values
            OriginalHost = HttpContext.Request.Host.Value,
            OriginalScheme = HttpContext.Request.Scheme,
            OriginalRemoteIp = HttpContext.Connection.RemoteIpAddress?.ToString(),

            // Forwarded values
            ForwardedHost = HttpContext.Request.Headers["X-Forwarded-Host"],
            ForwardedProto = HttpContext.Request.Headers["X-Forwarded-Proto"],
            ForwardedFor = HttpContext.Request.Headers["X-Forwarded-For"],

            // What ASP.NET Core actually sees after middleware processing
            ResolvedHost = HttpContext.Request.Host.Value,
            ResolvedScheme = HttpContext.Request.Scheme,
            ResolvedRemoteIp = HttpContext.Connection.RemoteIpAddress?.ToString(),

            // Cookies
            Cookies = HttpContext.Request.Cookies.ToDictionary(c => c.Key, c => c.Value)
        };

        return Json(result);
    }
#endif

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

    public async Task<IActionResult> SignOutAll()
    {
        await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return SignOut(
            new AuthenticationProperties
            {
                RedirectUri = Url.Action("Index", "Home")
            },
            "oidc");
    }

    public IActionResult ManageAccount()
    {
        var authServer = _configuration["Authentication:oidc:Authority"]?.TrimEnd('/');
        return new RedirectResult($"{authServer}/Identity/Account/Manage");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
