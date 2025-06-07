using ledger11.auth.Services;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using ledger11.auth.Models;
using Microsoft.EntityFrameworkCore;

namespace ledger11.auth.Controllers;

public class TestController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailSender _emailSender;
    private readonly IEmailTester _emailTester;
    private readonly IWebHostEnvironment _env;

    public TestController(
        UserManager<ApplicationUser> userManager,
        IEmailSender emailSender,
        IEmailTester emailTester,
        IWebHostEnvironment env)
    {
        _userManager = userManager;
        _emailSender = emailSender;
        _emailTester = emailTester;
        _env = env;
    }

    private IActionResult? EnsureDevelopmentMode()
    {
        if (!_env.IsDevelopment())
        {
            return NotFound();
        }
        return null;
    }

    public async Task<IActionResult> SendTestEmail()
    {
        var devCheck = EnsureDevelopmentMode();
        if (devCheck != null) return devCheck;

        await _emailSender.SendEmailAsync("stoyan@xlansoftware.com", "Test Email", "This is a test email");
        return Content("Email sent!");
    }

    [HttpPost]
    public IActionResult ClearEmails()
    {
        var devCheck = EnsureDevelopmentMode();
        if (devCheck != null) return devCheck;

        _emailTester.ClearCapturedEmails();
        return RedirectToAction("ViewEmails");
    }

    public IActionResult ViewEmails()
    {
        var devCheck = EnsureDevelopmentMode();
        if (devCheck != null) return devCheck;

        var emails = _emailTester.GetCapturedEmails();

        var html = $@"TODO: Add emails to table.<br />{string.Join("<br />", emails)}";

        return Content(html, "text/html");
    }

    [HttpPost("/api/test/create-user")]
    public async Task<IActionResult> CreateUser([FromForm] string userName, [FromForm] string password)
    {
        var devCheck = EnsureDevelopmentMode();
        if (devCheck != null) return devCheck;

        var user = await _userManager.FindByNameAsync(userName);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = userName,
                Email = userName,
                EmailConfirmed = true
            };
            await _userManager.CreateAsync(user, password);
        }

        return Ok();
    }

    [HttpPost("/api/test/delete-user")]
    public async Task<IActionResult> DeleteUser([FromForm] string userName)
    {
        var devCheck = EnsureDevelopmentMode();
        if (devCheck != null) return devCheck;

        var user = await _userManager.FindByNameAsync(userName);

        if (user != null)
        {
            await _userManager.DeleteAsync(user);
        }

        return Ok();
    }

    [HttpGet("/api/test/list-users")]
    public async Task<IActionResult> ListUsers()
    {
        var devCheck = EnsureDevelopmentMode();
        if (devCheck != null) return devCheck;

        var users = await _userManager.Users
            .ToListAsync();

        return Ok(users.Select((user) => user.UserName));
    }

    [HttpPost("/api/test/delete-all-users")]
    public async Task<IActionResult> DeleteAllUsers()
    {
        var devCheck = EnsureDevelopmentMode();
        if (devCheck != null) return devCheck;

        var users = await _userManager.Users
            .Where(user => user.UserName != null && user.UserName.EndsWith("example.com"))
            .ToListAsync();

        foreach (var user in users)
        {
            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(new { message = $"Failed to delete user {user.UserName}", errors = result.Errors });
            }
        }

        return Ok(new { deletedCount = users.Count });
    }
    
    [HttpGet("/api/test/mode")]
    public IActionResult IsTestModeEnabled()
    {
        var devCheck = EnsureDevelopmentMode();
        if (devCheck != null) return devCheck;

        return Json(_emailTester.IsTestModeEnabled);
    }

    [HttpGet("/api/test/emails")]
    public IActionResult GetEmailsJson()
    {
        var devCheck = EnsureDevelopmentMode();
        if (devCheck != null) return devCheck;

        return Json(_emailTester.GetCapturedEmails());
    }
}