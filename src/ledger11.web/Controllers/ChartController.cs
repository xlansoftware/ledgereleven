using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using ledger11.model.Data;
using ledger11.service;

namespace ledger11.web.Controllers;

[Authorize]
[ApiController]
[Route("api/chart")]
public class ChartController : ControllerBase
{
    private readonly ICurrentLedgerService _currentLedger;
    private readonly IChatGptService _chatGptService;
    private readonly ILogger<ChartController> _logger;

    // Use the prompt template discussed earlier
    private readonly string _apiSystemMessage = @"
You are a data assistant that transforms natural language instructions into a SQL query and a chart configuration object for a frontend WidgetComponent.

The database has the following structure (Entity Framework models):

---
[Table(""Categories"")]
public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public string? Color { get; set; }
    public string? Icon { get; set; }
}

[Table(""Transactions"")]
public class Transaction
{
    public int Id { get; set; }
    public decimal Value { get; set; }
    public DateTime? Date { get; set; }
    public int? CategoryId { get; set; }
    public string? Notes { get; set; }
    public string? User { get; set; }
}
---

Given a user prompt, generate:

1. A SQL query for SQLite based on the request. Escape the column names.
2. A JSON object named 'widgetParams' with the following shape:
{
  chartType: 'Line' | 'Bar' | 'Pie',
  title?: string, // short description of the chart
  xAxisKey: string | string[]; // the key(s) for the x-axis
  yAxisKey: string | string[]; // the key(s) for the y-axis
  showLegend?: boolean; // should the legend be visible?
  showYAxis?: boolean; // should the Y axis be visible?
  showCartesianGrid?: boolean; // should the grid be visible?
  data: Record<string, any>[],
  color?: string,
  pieDataKeys?: { nameKey: string; valueKey: string }
}

Return in this format:

SQL:
<SQL QUERY>

widgetParams:
<JSON OBJECT>
";

    public ChartController(
        ICurrentLedgerService currentLedger,
        IChatGptService chatGptService,
        ILogger<ChartController> logger)
    {
        _currentLedger = currentLedger;
        _chatGptService = chatGptService;
        _logger = logger;
    }

    [HttpPost("define-widget")]
    public async Task<IActionResult> DefineWidget([FromBody] DefineWidgetArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.UserDescription))
        {
            _logger.LogWarning("Received an empty user description.");
            return BadRequest("User description cannot be empty.");
        }

        _logger.LogInformation("Processing user description: {UserDescription}", args.UserDescription);

        try
        {
            _logger.LogInformation("Sending request to AI API.");
            var messageContent = await _chatGptService.SendTextToChatGptAsync(args.UserDescription, _apiSystemMessage);

            if (messageContent!.usage != null)
            {
                _logger.LogInformation("Token usage: {tokens}", messageContent!.usage!.Report());
            }

            if (string.IsNullOrWhiteSpace(messageContent.result))
            {
                _logger.LogWarning("AI response was empty or invalid.");
                return BadRequest("Empty or invalid AI response.");
            }

            try
            {
                _logger.LogInformation("Raw AI response: {response}", messageContent.result);

                var result = ParseWidgetDefinition(messageContent.result);

                if (result == null)
                {
                    _logger.LogWarning("Parsed result is null.");
                    return BadRequest("Unable to parse AI response.");
                }

                _logger.LogInformation("Successfully parsed AI response into WidgetDefinition.");

                // Save the widget definition to the database
                var widget = await SaveWidgetAsync(args.UserDescription, result, args.Id);

                return CreatedAtAction(nameof(Execute), new { id = widget.Id }, widget);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse AI response into objects.");
                return BadRequest("AI response was not in the expected format.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while processing the request.");
            return StatusCode(500, $"An error occurred: {ex.Message}");
        }
    }

    [HttpPost("new")]
    public async Task<IActionResult> NewWidget()
    {
        var widget = await SaveWidgetAsync("Show me the purchases I made today", new WidgetDefinition()
        {
            SqlQuery = @"SELECT IFNULL(Categories.Name, 'Uncategorized') AS CategoryName,
       SUM(Transactions.Value) AS TotalValue
FROM Transactions
LEFT JOIN Categories ON Transactions.CategoryId = Categories.Id
WHERE DATE(Transactions.Date) = DATE('now', 'localtime')
GROUP BY CategoryName",
            WidgetParamsJson = JsonSerializer.Serialize(new
            {
                chartType = "Pie",
                pieDataKeys = new { nameKey = "CategoryName", valueKey = "TotalValue" },
                title = "Purchases Today by Category"
            })
        });
        return CreatedAtAction(nameof(Execute), new { id = widget.Id }, widget);
    }

#if DEBUG
    [HttpPost("test-widget")]
    public async Task<IActionResult> ExecuteWidget([FromBody] WidgetDefinition widgetDefinition)
    {
        try
        {
            var result = await ExecuteWidgetAsync(new Widget
            {
                DataQuery = widgetDefinition.SqlQuery,
                WidgetParamsJson = widgetDefinition.WidgetParamsJson
            });
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute SQL or parse chart params.");
            return BadRequest("Failed to execute query or parse result.");
        }
    }
#endif

    [HttpGet("{id}")]
    public async Task<IActionResult> Execute(int id)
    {
        var db = await _currentLedger.GetLedgerDbContextAsync();

        var widget = await db.Widgets.FindAsync(id);
        if (widget == null)
        {
            return NotFound("Widget not found.");
        }

        try
        {
            var result = await ExecuteWidgetAsync(widget);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute SQL or parse chart params.");
            return BadRequest("Failed to execute query or parse result.");
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var db = await _currentLedger.GetLedgerDbContextAsync();

        var widget = await db.Widgets.FindAsync(id);
        if (widget == null)
        {
            return NotFound("Widget not found.");
        }

        db.Widgets.Remove(widget);
        await db.SaveChangesAsync();

        return Ok();
    }

    private WidgetDefinition? ParseWidgetDefinition(string responseContent)
    {
        // Example response parsing
        var sqlStart = responseContent.IndexOf("SQL:", StringComparison.OrdinalIgnoreCase);
        var widgetStart = responseContent.IndexOf("widgetParams:", StringComparison.OrdinalIgnoreCase);

        if (sqlStart == -1 || widgetStart == -1)
            return null;

        var sql = responseContent.Substring(sqlStart + 4, widgetStart - sqlStart - 4).Trim();
        var widgetJson = responseContent.Substring(widgetStart + "widgetParams:".Length).Trim();

        return new WidgetDefinition
        {
            SqlQuery = ChatGptService.FindSqlBlock(sql),
            WidgetParamsJson = ChatGptService.FindJsonBlock(widgetJson),
        };
    }

    public class WidgetDefinition
    {
        public string SqlQuery { get; set; } = string.Empty;
        public string WidgetParamsJson { get; set; } = string.Empty;
    }

    public class DefineWidgetArgs
    {
        public string UserDescription { get; set; } = "";
        public int? Id { get; set; }
    }


    private async Task<Dictionary<string, object>> ExecuteWidgetAsync(Widget widget)
    {
        var db = await _currentLedger.GetLedgerDbContextAsync();

        var result = new Dictionary<string, object>();

        try
        {
            using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = widget.DataQuery;
            command.CommandType = System.Data.CommandType.Text;
            await db.Database.OpenConnectionAsync();

            using var reader = await command.ExecuteReaderAsync();
            var data = new List<Dictionary<string, object>>();

            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var columnName = reader.GetName(i);
                    var value = await reader.IsDBNullAsync(i) ? null : reader.GetValue(i);
                    row[columnName] = value!;
                }
                data.Add(row);
            }

            result["data"] = data;
            result["params"] = widget.WidgetParamsJson;
            return result;
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }

    private async Task<Widget> SaveWidgetAsync(string userDescription, WidgetDefinition widgetDefinition, int? id = null)
    {
        var db = await _currentLedger.GetLedgerDbContextAsync();

        var widget = new Widget
        {
            Title = TitleFormJson(widgetDefinition.WidgetParamsJson),
            Definition = userDescription,
            DataQuery = widgetDefinition.SqlQuery,
            WidgetParamsJson = widgetDefinition.WidgetParamsJson,
        };

        if (id.HasValue)
        {
            widget.Id = id.Value;
            db.Widgets.Update(widget);
        }
        else
        {
            widget.Order = 0;
            db.Widgets.Add(widget);
        }

        await db.SaveChangesAsync();
        return widget;
    }

    private string TitleFormJson(string json)
    {
        var jsonDoc = JsonDocument.Parse(json);
        if (jsonDoc.RootElement.TryGetProperty("title", out var titleElement))
        {
            return titleElement.GetString() ?? "";
        }
        return "";
    }
}
