using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ledger11.service;

public interface IExchangeRateService
{
    Task<decimal?> GetExchangeRateAsync(string fromCurrency, string toCurrency);
}

public class ExchangeRateService : IExchangeRateService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ExchangeRateService> _logger;
    private readonly string? _apiKey;

    public ExchangeRateService(HttpClient httpClient, ILogger<ExchangeRateService> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = configuration["AppConfig:EXCHANGE_RATE_API_KEY"];
    }

    private const string FrankfurterBaseUrl = "https://api.frankfurter.app";

    /// <summary>
    /// Calls the Frankfurter API to get the latest exchange rate.
    /// </summary>
    private async Task<decimal?> GetFrankfurterExchangeRateAsync(string from, string to)
    {
        var url = $"{FrankfurterBaseUrl}/latest?from={from}&to={to}";
        var response = await _httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("API call failed: GET {url} [{StatusCode}] - {Reason}", url, response.StatusCode, response.ReasonPhrase);
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

    public async Task<decimal?> GetExchangeRateAsync(string fromCurrency, string toCurrency)
    {
        if (fromCurrency == toCurrency) return 1.0m;
        return await GetFrankfurterExchangeRateAsync(fromCurrency, toCurrency);
    }
}
