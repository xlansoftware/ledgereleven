using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;

namespace ledger11.web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CurrencyController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CurrencyController> _logger;

    // Base URL for the Frankfurter exchange rate API
    private const string FrankfurterBaseUrl = "https://api.frankfurter.app";

    public CurrencyController(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        ILogger<CurrencyController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
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

        var cacheKey = $"exchangeRate_{fromCurrency}_{toCurrency}";

        // Try to get the rate from memory cache
        if (_cache.TryGetValue<decimal>(cacheKey, out var cachedRate))
        {
            _logger.LogInformation("Cache hit for {From} to {To}: {Rate}", fromCurrency, toCurrency, cachedRate);
            return Ok(new
            {
                From = fromCurrency,
                To = toCurrency,
                Rate = cachedRate,
                Source = "cache"
            });
        }

        try
        {
            _logger.LogInformation("Cache miss for {From} to {To}. Fetching from API...", fromCurrency, toCurrency);

            var rate = await GetFrankfurterExchangeRateAsync(fromCurrency, toCurrency);
            if (rate == null)
            {
                _logger.LogWarning("Exchange rate from {From} to {To} not found.", fromCurrency, toCurrency);
                return NotFound($"Exchange rate from {fromCurrency} to {toCurrency} not found.");
            }

            // Cache the rate for 1 day
            _cache.Set(cacheKey, rate.Value, TimeSpan.FromDays(1));

            _logger.LogInformation("Fetched and cached exchange rate {From}->{To}: {Rate}", fromCurrency, toCurrency, rate.Value);

            return Ok(new
            {
                From = fromCurrency,
                To = toCurrency,
                Rate = rate.Value,
                Source = "live"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching exchange rate from {From} to {To}.", fromCurrency, toCurrency);
            return StatusCode(500, $"Error fetching exchange rate: {ex.Message}");
        }
    }

    /// <summary>
    /// Calls the Frankfurter API to get the latest exchange rate.
    /// </summary>
    private async Task<decimal?> GetFrankfurterExchangeRateAsync(string from, string to)
    {
        var client = _httpClientFactory.CreateClient();
        var url = $"{FrankfurterBaseUrl}/latest?from={from}&to={to}";

        var response = await client.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("API call failed: {StatusCode} - {Reason}", response.StatusCode, response.ReasonPhrase);
            return null;
        }

        var json = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<FrankfurterResponse>(json);

        if (data?.rates != null && data.rates.TryGetValue(to, out var rate))
            return rate;

        _logger.LogWarning("No rate found in response for {To}", to);
        return null;
    }

    /// <summary>
    /// Deserialization class for Frankfurter API response.
    /// </summary>
    private class FrankfurterResponse
    {
        public Dictionary<string, decimal> rates { get; set; } = new();
    }
}
