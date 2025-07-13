using ledger11.model.Data;
using ledger11.service;
using ledger11.web.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace ledger11.web.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class BookController : ControllerBase
    {
        private readonly ICurrentLedgerService _currentLedger;
        private readonly ILogger<BookController> _logger;

        public BookController(ICurrentLedgerService currentLedger, ILogger<BookController> logger)
        { 
            _currentLedger = currentLedger;
            _logger = logger;
        }

        // GET: api/book/open/{spaceId}
        [HttpGet("open/{spaceId}")]
        public async Task<IActionResult> OpenBook(string spaceId)
        {
            _logger.LogInformation("Opening book for space {SpaceId}", spaceId);

            // Get the DB context for that space
            using var db = await _currentLedger.GetLedgerDbContextAsync();

            // Fetch all data in parallel
            var settingsTask = db.Settings.AsNoTracking().ToListAsync();
            var categoriesTask = db.Categories.AsNoTracking().OrderBy(c => c.DisplayOrder).ToListAsync();
            var transactionsTask = db.Transactions.AsNoTracking().OrderByDescending(t => t.Date).Take(50).ToListAsync(); // Initial page
            var totalTransactionsTask = db.Transactions.CountAsync();

            await Task.WhenAll(settingsTask, categoriesTask, transactionsTask, totalTransactionsTask);

            // Assemble the response DTO
            var response = new OpenBookResponse
            {
                Settings = settingsTask.Result.ToDictionary(s => s.Key, s => s.Value),
                Categories = categoriesTask.Result,
                Transactions = new PaginatedResult<Transaction>
                {
                    Items = transactionsTask.Result,
                    TotalCount = totalTransactionsTask.Result
                }
            };

            return Ok(response);
        }
    }
}
