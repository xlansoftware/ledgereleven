using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace ledger11.service;

public interface IChatGptService
{
    Task<AIResponse> SendImageToChatGptAsync(IFormFile image, string instruction);
    Task<AIResponse> SendTextToChatGptAsync(string text, string instruction);
}

public class AIResponse
{
    public string? result { get; set; }
    public ChatGptService.Usage? usage { get; set; }
}

public class ChatGptService : IChatGptService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _apiUrl;
    private readonly string _apiModel = "gpt-4.1-mini"; // Default model

    private readonly ILogger<ChatGptService> _logger; // Add logger field


    public ChatGptService(HttpClient httpClient, IConfiguration configuration, ILogger<ChatGptService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Check if configuration is null
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration), "Configuration cannot be null.");
        }

        // Log the initialization
        _logger.LogInformation("ChatGptService initialized with API URL: {ApiUrl}", configuration["AppConfig:AI_API_URL"]);
        // Retrieve API settings from configuration
        _apiKey = configuration["AppConfig:AI_API_KEY"] ?? throw new InvalidOperationException("API_KEY not configured.");
        _apiUrl = configuration["AppConfig:AI_API_URL"] ?? throw new InvalidOperationException("API_URL not configured.");
        _apiModel = configuration["AppConfig:AI_MODEL"] ?? _apiModel;

        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
    }

    public async Task<AIResponse> SendImageToChatGptAsync(IFormFile image, string instruction)
    {
        // Construct JSON body with image+text instruction (using base64 here for vision request)
        var imageBytes = await FileToByteArray(image);
        var base64Image = Convert.ToBase64String(imageBytes);

        var requestBody = new
        {
            model = _apiModel,
            messages = new[]
            {
                // Use the new format for messages
                new {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = instruction },
                        new {
                            type = "image_url",
                            image_url = new {
                                url = $"data:{image.ContentType};base64,{base64Image}"
                            }
                        }
                    }
                }
            },
            max_tokens = 1000
        };

        var json = JsonContent.Create(requestBody);
        var response = await _httpClient.PostAsync(_apiUrl, json);
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to communicate with AI API. Status code: {StatusCode}, Error: {errorContent}", response.StatusCode, errorContent); // Log error
            throw new Exception($"Failed to communicate with AI API. Status code: {response.StatusCode}");
        }
        var result = await response.Content.ReadAsStringAsync();

        return ParseResponse(result);
    }

    public async Task<AIResponse> SendTextToChatGptAsync(string text, string instruction)
    {
        _logger.LogTrace($"SendTextToChatGptAsync: text={text}, instruction={instruction}");
        var requestBody = new
        {
            model = _apiModel,
            messages = new[]
            {
                new { role = "system", content = instruction },
                new { role = "user", content = text }
            },
            max_tokens = 1000
        };

        var json = JsonContent.Create(requestBody);
        var response = await _httpClient.PostAsync(_apiUrl, json);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to communicate with AI API. Status code: {StatusCode}", response.StatusCode); // Log error
            throw new Exception($"Failed to communicate with AI API. Status code: {response.StatusCode}");
        }


        var result = await response.Content.ReadAsStringAsync();

        return ParseResponse(result);
    }

    public static string FindJsonBlock(string responseContent)
    {
        var startIndex = responseContent.IndexOf("```");
        if (startIndex == -1)
        {
            return responseContent; // No code block found, return the whole content
        }
        var endIndex = responseContent.IndexOf("```", startIndex + 3);
        var result = responseContent.Substring(startIndex + 3, endIndex - startIndex - 3);
        if (result.IndexOf("json", StringComparison.OrdinalIgnoreCase) == 0)
        {
            result = result.Substring("json".Length); // Remove the "json" part
        }
        return result;
    }

    public static string FindSqlBlock(string responseContent)
    {
        var startIndex = responseContent.IndexOf("```");
        if (startIndex == -1)
        {
            return responseContent; // No code block found, return the whole content
        }
        var endIndex = responseContent.IndexOf("```", startIndex + 3);
        var result = responseContent.Substring(startIndex + 3, endIndex - startIndex - 3);
        if (result.IndexOf("sql", StringComparison.OrdinalIgnoreCase) == 0)
        {
            result = result.Substring("sql".Length); // Remove the "sql" part
        }
        return result;
    }

    private AIResponse ParseResponse(string responseContent)
    {
        // _logger.LogInformation("Received response from AI API: {ResponseContent}", responseContent); // Log API response
        var parsedResponse = JsonSerializer.Deserialize<ChatGptResponse>(responseContent);
        var messageContent = parsedResponse?.choices?.FirstOrDefault()?.message?.content;
        return new AIResponse
        {
            result = messageContent,
            usage = parsedResponse?.usage
        };

    }


    private async Task<byte[]> FileToByteArray(IFormFile file)
    {
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        return ms.ToArray();
    }

    public class ChatGptResponse
    {
        public List<Choice>? choices { get; set; }
        public Usage? usage { get; set; }
    }
    public class Choice
    {
        public Message? message { get; set; }
    }
    public class Message
    {
        public string? content { get; set; }
    }

    public class Usage
    {
        public int prompt_tokens { get; set; }
        public int completion_tokens { get; set; }
        public int total_tokens { get; set; }

        public string Report() {
            return $"Total Tokens: {total_tokens} (Prompt Tokens: {prompt_tokens}, Completion Tokens: {completion_tokens})";
        }
    }

}
