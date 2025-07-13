using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using ledger11.service;

namespace ledger11.web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CurrencyController : ControllerBase
{
    private readonly IExchangeRateService _exchangeRateService;
    private readonly ILogger<CurrencyController> _logger;

    public CurrencyController(
        IExchangeRateService exchangeRateService,
        ILogger<CurrencyController> logger)
    {
        _exchangeRateService = exchangeRateService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves the current exchange rate between two currencies.
    /// Uses daily caching to reduce external API calls.
    /// </summary>
    [HttpGet("exchange-rate")]
    public async Task<IActionResult> GetExchangeRate(string fromCurrency, string toCurrency)
    {
        // Validate inputs
        if (string.IsNullOrWhiteSpace(fromCurrency) || string.IsNullOrWhiteSpace(toCurrency))
            return BadRequest("Both 'fromCurrency' and 'toCurrency' must be provided.");

        fromCurrency = fromCurrency.ToUpper();
        toCurrency = toCurrency.ToUpper();

        try
        {
            var rate = await _exchangeRateService.GetExchangeRateAsync(fromCurrency, toCurrency);

            return Ok(new
            {
                From = fromCurrency,
                To = toCurrency,
                Rate = rate,
                Source = "live"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching exchange rate from {From} to {To}.", fromCurrency, toCurrency);
            return StatusCode(500, $"Error fetching exchange rate: {ex.Message}");
        }
    }
}
