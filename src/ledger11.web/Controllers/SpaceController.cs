using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ledger11.data;
using ledger11.model.Data;
using ledger11.service;
using ledger11.web.Models;

namespace ledger11.web.Controllers;

[Authorize]
public class SpaceController : Controller
{
    private readonly ILogger<SpaceController> _logger;
    private readonly IUserSpaceService _userSpace;

    public SpaceController(
        ILogger<SpaceController> logger,
        IUserSpaceService userSpace,
        AppDbContext appDbContext)
    {
        _logger = logger;
        _userSpace = userSpace;
    }

    public async Task<IActionResult> Index()
    {
        var space = await _userSpace.GetUserSpaceAsync();
        if (space != null)
        {
            return Redirect("/app");
        }

        return RedirectToAction("Manage");
    }

    public async Task<IActionResult> Manage()
    {
        var currentSpace = await _userSpace.GetUserSpaceAsync();
        var spaces = await _userSpace.GetAvailableSpacesAsync();

        TempData["current"] = currentSpace?.Id;
        return View(spaces);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSpace(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Space name is required.";
            return RedirectToAction("Manage");
        }

        await _userSpace.CreateSpace(new Space
        {
            Name = name,
        });
        return RedirectToAction("Manage");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetCurrentSpace(Guid spaceId)
    {
        await _userSpace.SetCurrentSpaceAsync(spaceId);
        return RedirectToAction("Manage");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSpace(Guid spaceId)
    {
        await _userSpace.DeleteSpaceAsync(spaceId);
        return RedirectToAction("Manage");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RenameSpace(Guid spaceId, string newName)
    {
        await _userSpace.UpdateSpace(spaceId, new Dictionary<string, object>
        {
            { "Name", newName }
        });
        return RedirectToAction("Manage");
    }

}
