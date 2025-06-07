using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging; // Add this namespace
using ledger11.model.Api;
using ledger11.model.Data;
using ledger11.service;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ledger11.web.Controllers;

[ApiController]
[Route("api/purchases")]
public class PurchasesController : ControllerBase
{
    private readonly ICurrentLedgerService _currentLedger;

    private readonly IChatGptService _chatGptService;
    private readonly ILogger<PurchasesController> _logger; // Add logger field
    private readonly string _apiSystemMessage = @"
You are given a verbal description of a purchase. Extract the following information from the text and return it as a JSON object:

1. A list of items, where each item includes:
   - `name`: The name or description of the item.
   - `quantity`: The quantity or weight of the item, if applicable (e.g., 0.934 or 1).
   - `unit_price`: The price per unit or per kilogram/liter, if available.
   - `total_price`: The total price paid for this item (should match quantity × unit_price if possible).
   - `category`: One of {CATEGORY_LIST}.
2. `total_paid`: The total amount paid by the customer.

**Output format:**

```json
{
  ""items"": [
    {
      ""name"": ""<item name or description>"",
      ""quantity"": ""<item quantity or weight>"",
      ""unit_price"": ""<unit price with currency and unit if available>"",
      ""total_price"": ""<total price for this item with currency>"",
      ""category"": ""<category of the item>""
    }
    // ... more items
  ],
  ""total_paid"": ""<final total paid>"",
  ""category"": ""<category>""
}
```

Be sure to extract all items from the text accurately and perform any necessary calculations (e.g., quantity × unit price) to confirm the total prices.

Example input: 'Bought 3 coffees at Starbucks for $15'
Example output:
```json
{
  ""items"": [
    {
      ""name"": ""coffee"",
      ""quantity"": ""3"",
      ""unit_price"": ""5"",
      ""total_price"": ""15"",
      ""category"": ""Food""
    }
  ],
  ""total_paid"": ""15"",
  ""category"": ""Food""
}```

";

    private readonly string _apiSystemMessageReceipt = @"
You are given an image of a retail receipt. Extract the following information from the image and return it as a JSON object:

1. A list of items, where each item includes:
   - `name`: The name or description of the item.
   - `quantity`: The quantity or weight of the item, if applicable (e.g., 0.934 or 1).
   - `unit_price`: The price per unit or per kilogram/liter, if available.
   - `total_price`: The total price paid for this item (should match quantity × unit_price if possible).
   - `category`: One of {CATEGORY_LIST}.
2. `total_paid`: The total amount paid by the customer, as stated at the end of the receipt.

**Output format:**

```json
{
  ""items"": [
    {
      ""name"": ""<item name or description>"",
      ""quantity"": ""<item quantity or weight>"",
      ""unit_price"": ""<unit price with currency and unit if available>"",
      ""total_price"": ""<total price for this item with currency>"",
      ""category"": ""<category of the item>""
    }
    // ... more items
  ],
  ""total_paid"": ""<final total paid>"",
  ""category"": ""<category>""
}
```

Be sure to extract all text from the receipt image accurately and perform any necessary calculations (e.g., quantity × unit price) to confirm the total prices.
";
    //- If the receipt is not in English, translate it to English.

    public PurchasesController(
        ICurrentLedgerService currentLedger,
        IChatGptService chatGptService,
        ILogger<PurchasesController> logger)
    {
        _currentLedger = currentLedger;
        _chatGptService = chatGptService;
        _logger = logger;
    }

    [HttpPost("parse")]
    public async Task<IActionResult> Parse([FromBody] ParseArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.Query))
        {
            _logger.LogWarning("Received an empty query."); // Log warning
            return BadRequest("Query cannot be empty.");
        }

        _logger.LogInformation("Processing query: {Query}", args.Query); // Log query

        var categories = await GetCategoryNames();
        try
        {
            _logger.LogInformation("Sending request to AI API."); // Log API request
            var messageContent = await _chatGptService.SendTextToChatGptAsync(args.Query, _apiSystemMessage.Replace("{CATEGORY_LIST}", categories));
            if (messageContent!.usage != null)
            {
                _logger.LogInformation("Parse: {tokens}", messageContent!.usage!.Report());
            }
            if (string.IsNullOrWhiteSpace(messageContent.result))
            {
                _logger.LogWarning("AI response was empty or invalid."); // Log warning
                return BadRequest("Empty or invalid AI response.");
            }

            try
            {
                _logger.LogInformation(messageContent.result);
                var result = ReponseToReceipt(messageContent.result);
                _logger.LogInformation("Successfully parsed AI response into objects.");
                return Ok(result);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse AI response into objects."); // Log exception
                return BadRequest("AI response was not in the expected format.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while processing the request."); // Log exception
            return StatusCode(500, $"An error occurred: {ex.Message}");
        }
    }

    [HttpPost("scan")]
    public async Task<IActionResult> Scan(IFormFile imageFile)
    {
        if (imageFile != null && imageFile.Length > 0)
        {
            var categories = await GetCategoryNames();
            var result = await _chatGptService.SendImageToChatGptAsync(imageFile, _apiSystemMessageReceipt.Replace("{CATEGORY_LIST}", categories));
            _logger.LogInformation(result.result);
            _logger.LogInformation("Scan: size {size} -- {tokens}", imageFile.Length, result!.usage!.Report());
            var purchases = ChatGptService.FindJsonBlock(result!.result!);
            return Ok(purchases);
        }

        return Ok("");
    }

    private Receipt? ReponseToReceipt(string responseContent)
    {
        var json = ChatGptService.FindJsonBlock(responseContent);
        return JsonSerializer.Deserialize<Receipt>(json);
    }

    public class ParseArgs
    {
        public string Query { get; set; } = "";
        public bool IsReceipt { get; set; } = false;
    }

    private async Task<string> GetCategoryNames()
    {
        using var db = await _currentLedger.GetLedgerDbContextAsync();
        var categories = await db.Categories.ToListAsync();
        return string.Join(", ", categories.Select((c) => $"\"{c.Name}\""));
    }
}