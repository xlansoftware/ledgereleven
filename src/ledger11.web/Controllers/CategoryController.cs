using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ledger11.model.Data;
using ledger11.service;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CategoryController : ControllerBase
{
    private readonly ICurrentLedgerService _currentLedger;

    private readonly ILogger<CategoryController> _logger;

    public CategoryController(
        ICurrentLedgerService currentLedger,
        ILogger<CategoryController> logger)
    {
        _currentLedger = currentLedger;
        _logger = logger;
    }

    // GET: api/category
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        using var db = await _currentLedger.GetLedgerDbContextAsync();
        var categories = await db.Categories
            .OrderBy(t => t.DisplayOrder)
            .ToListAsync();

        return Ok(categories);
    }

    // GET: api/category/5
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        using var db = await _currentLedger.GetLedgerDbContextAsync();
        var category = db.Categories
            .FirstOrDefault(t => t.Id == id);

        if (category == null)
            return NotFound();

        return Ok(category);
    }

    // POST: api/category
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Category category)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        using var db = await _currentLedger.GetLedgerDbContextAsync();
        db.Categories.Add(category);
        db.SaveChanges();
        return CreatedAtAction(nameof(Get), new { id = category.Id }, category);
    }

    // POST: api/category/reorder
    [HttpPost("reorder")]
    public async Task<IActionResult> Reorder([FromBody] int[] categoryIds)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        using var db = await _currentLedger.GetLedgerDbContextAsync();
        using var tran = await db.Database.BeginTransactionAsync();

        var categories = await db.Categories
            .Where(c => categoryIds.Contains(c.Id))
            .ToListAsync();

        for (int i = 0; i < categoryIds.Length; i++)
        {
            var category = categories.First(c => c.Id == categoryIds[i]);
            category.DisplayOrder = i + 1;
        }

        await db.SaveChangesAsync();
        await tran.CommitAsync();
        return Ok();
    }

    // PUT: api/category/5
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] Dictionary<string, object> updates)
    {
        using var db = await _currentLedger.GetLedgerDbContextAsync();

        var category = await db.Categories.FindAsync(id);
        if (category == null)
            return NotFound();

        foreach (var kvp in updates)
        {
            var prop = typeof(Category).GetProperty(
                kvp.Key,
                BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance
            );

            if (prop == null || !prop.CanWrite)
                continue;

            var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

            object? value = kvp.Value;
            if (value != null)
            {
                _logger.LogDebug($"Converting {kvp.Key}: {kvp.Value} ({value.GetType()}) to {prop.PropertyType} ...");
            }

            // Handle JsonElement if the value came from JSON
            if (value is JsonElement jsonElement)
            {
                try
                {
                    value = jsonElement.Deserialize(targetType, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch
                {
                    continue; // skip if it can't deserialize
                }
            }

            // Handle nulls safely
            if (value is null && prop.PropertyType.IsValueType && Nullable.GetUnderlyingType(prop.PropertyType) == null)
                continue; // can't assign null to non-nullable value types

            prop.SetValue(category, value);
        }

        await db.SaveChangesAsync();

        return Ok(category);
    }

    // DELETE: api/category/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, int? replaceWithId)
    {
        using var db = await _currentLedger.GetLedgerDbContextAsync();

        using var tran = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

        var category = db.Categories.Find(id);
        if (category == null)
            return NotFound();

        if (replaceWithId.HasValue)
        {
            var replaceWith = db.Categories.Find(replaceWithId.Value);
            if (replaceWith == null)
                return NotFound();

            // Update all Transactions referencing this category
            await db.Transactions
                .Where(t => t.CategoryId == id)
                .ExecuteUpdateAsync(setters =>
                    setters.SetProperty(t => t.CategoryId, replaceWithId.Value));

            // Update all TransactionDetails referencing this category
            await db.TransactionDetail
                .Where(td => td.CategoryId == id)
                .ExecuteUpdateAsync(setters =>
                    setters.SetProperty(td => td.CategoryId, replaceWithId.Value));
        }

        db.Categories.Remove(category);
        db.SaveChanges();

        await tran.CommitAsync();
        return NoContent();
    }

}