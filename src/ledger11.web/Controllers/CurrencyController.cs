using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace ledger11.web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CurrencyController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;

    public CurrencyController(IHttpClientFactory httpClientFactory, IMemoryCache cache)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
    }

    [HttpGet("exchange-rate")]
    public async Task<IActionResult> GetExchangeRate(string fromCurrency, string toCurrency)
    {
        if (string.IsNullOrWhiteSpace(fromCurrency) || string.IsNullOrWhiteSpace(toCurrency))
            return BadRequest("Both 'fromCurrency' and 'toCurrency' must be provided.");

        fromCurrency = fromCurrency.ToUpper();
        toCurrency = toCurrency.ToUpper();

        var cacheKey = $"exchangeRate_{fromCurrency}_{toCurrency}";

        if (_cache.TryGetValue<decimal>(cacheKey, out var cachedRate))
        {
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
            var rate = await GetLiveExchangeRateAsync(fromCurrency, toCurrency);

            if (rate == null)
                return NotFound($"Exchange rate from {fromCurrency} to {toCurrency} not found.");

            // Cache the result for 1 day
            _cache.Set(cacheKey, rate.Value, TimeSpan.FromDays(1));

            return Ok(new
            {
                From = fromCurrency,
                To = toCurrency,
                Rate = rate,
                Source = "live"
            });
        }
        catch
        {
            return StatusCode(500, "An error occurred while retrieving exchange rate.");
        }
    }

    private async Task<decimal?> GetLiveExchangeRateAsync(string from, string to)
    {
        var client = _httpClientFactory.CreateClient();

        // Example using ExchangeRate-API (you'll need to register for a real API key)
        // For free test use: https://api.exchangerate.host/latest?base=USD
        var url = $"https://api.exchangerate.host/latest?base={from}";

        var response = await client.GetAsync(url);
        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<ExchangeRateResponse>(json);

        if (data?.Rates != null && data.Rates.TryGetValue(to, out var rate))
            return rate;

        return null;
    }

    public class ExchangeRateResponse
    {
        public Dictionary<string, decimal> Rates { get; set; } = new Dictionary<string, decimal>();
    }
}
