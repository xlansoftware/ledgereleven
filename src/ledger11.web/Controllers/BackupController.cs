using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ledger11.model.Data;
using ledger11.service;
using ledger11.model.Api;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using ledger11.data;

namespace ledger11.web.Controllers;

[Authorize]
[ApiController]
[Route("api/backup")]
public class BackupController : ControllerBase
{
    private readonly ILogger<BackupController> _logger;
    private readonly IUserSpaceService _userSpace;
    private readonly ICurrentLedgerService _currentLedgerService;

    private readonly IBackupService _backupService;

    public BackupController(
        ILogger<BackupController> logger,
        IUserSpaceService userSpace,
        ICurrentLedgerService currentLedgerService,
        IBackupService backupService)
    {
        _logger = logger;
        _userSpace = userSpace;
        _currentLedgerService = currentLedgerService;
        _backupService = backupService;
    }

    // GET: api/backup/export
    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] ExportFormat format = ExportFormat.Excel)
    {
        // Get current UTC time in canonical format
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ");

        // Set content type and file name based on format
        string contentType;
        string fileName;

        switch (format)
        {
            case ExportFormat.Excel:
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                fileName = $"Backup-{timestamp}.xlsx";
                break;
            case ExportFormat.Csv:
                contentType = "text/csv";
                fileName = $"Backup-{timestamp}.csv";
                break;
            default:
                return BadRequest("Unsupported export format.");
        }

        var context = await _currentLedgerService.GetLedgerDbContextAsync();

        return new FileCallbackResult(contentType, async (stream, _) =>
        {
            await _backupService.ExportAsync(format, context, stream);
        })
        {
            FileDownloadName = fileName
        };
    }

    // POST: api/backup/import
    [HttpPost("import")]
    public async Task<IActionResult> Import(IFormFile dataFile, [FromQuery] bool clearExistingData = false)
    {
        if (dataFile == null || dataFile.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        await using var stream = dataFile.OpenReadStream();
        var context = await _currentLedgerService.GetLedgerDbContextAsync();

        try
        {
            await _backupService.ImportAsync(stream, context, clearExistingData);
            return Ok("Import successful.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during import.");
            return StatusCode(500, "An error occurred during import.");
        }
    }

}
