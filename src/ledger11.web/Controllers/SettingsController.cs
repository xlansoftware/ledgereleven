using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ledger11.model.Data;
using ledger11.service;

namespace ledger11.web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SettingsController : ControllerBase
{
    private readonly ICurrentLedgerService _currentLedger;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(ICurrentLedgerService currentLedger, ILogger<SettingsController> logger)
    {
        _currentLedger = currentLedger;
        _logger = logger;
    }

    // GET: api/settings
    [HttpGet]
    public async Task<IActionResult> GetAllSettings()
    {
        using var db = await _currentLedger.GetLedgerDbContextAsync();
        var settings = await db.Settings.AsNoTracking().ToListAsync();
        var settingsDictionary = settings.ToDictionary(s => s.Key, s => s.Value);
        return Ok(settingsDictionary);
    }

    // GET: api/settings/somekey
    [HttpGet("{key}")]
    public async Task<IActionResult> GetSetting(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return BadRequest("Setting key cannot be empty.");
        }

        using var db = await _currentLedger.GetLedgerDbContextAsync();
        var setting = await db.Settings.AsNoTracking().FirstOrDefaultAsync(s => s.Key == key);

        if (setting == null)
        {
            return NotFound();
        }

        return Ok(setting);
    }

    // PUT: api/settings/somekey
    [HttpPut("{key}")]
    public async Task<IActionResult> UpsertSetting(string key, [FromBody] UpdateSettingRequest request)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return BadRequest("Setting key cannot be empty.");
        }

        using var db = await _currentLedger.GetLedgerDbContextAsync();
        var setting = await db.Settings.FirstOrDefaultAsync(s => s.Key == key);

        if (setting == null)
        {
            setting = new Setting
            {
                Key = key,
                Value = request.Value
            };
            db.Settings.Add(setting);
            _logger.LogInformation("Creating new setting: {Key}", key);
        }
        else
        {            
            setting.Value = request.Value;
            _logger.LogInformation("Updating setting: {Key}", key);
        }

        await db.SaveChangesAsync();
        return Ok(setting);
    }

    public class UpdateSettingRequest
    {
        public string? Value { get; set; }
    }
}
