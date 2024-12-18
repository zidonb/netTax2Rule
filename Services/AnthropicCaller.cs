using System.Text.Json.Serialization;
using System.Text;
using System.Text.Json;
using System.Net.Http;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

public class AnthropicCaller : IGenAICaller
{
    private readonly HttpClient _httpClient;
    private readonly string _modelName;
    private const string ANTHROPIC_API_URL = "https://api.anthropic.com/v1/messages";

    public AnthropicCaller(string apiKey, string modelName)
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
        _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
        _modelName = modelName;
    }

    public async Task<string> SendToGenAIAsync(string systemPrompt, string userPrompt)
    {
        try
        {
            // Prepare the request body
            var requestBody = new
            {
                model = _modelName,
                max_tokens = 1000,
                temperature = 0,
                system = systemPrompt,
                messages = new[] 
                {
                    new { role = "user", content = userPrompt }
                }
            };

            // Serialize request body to JSON
            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // Make the POST request
            var response = await _httpClient.PostAsync(ANTHROPIC_API_URL, content);

            // Get the response body
            var responseBody = await response.Content.ReadAsStringAsync();

            // Log the response for debugging
            Console.WriteLine($"API Response: {responseBody}");

            // Check for unsuccessful response
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Error: {response.StatusCode} - {responseBody}");
                return $"Error: API returned {response.StatusCode}";
            }

            // Deserialize the response into a strongly typed object
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };

            var result = JsonSerializer.Deserialize<AnthropicResponse>(responseBody, options);

            if (result == null)
            {
                Console.WriteLine("Deserialization failed.");
                return "Error: Deserialization failed.";
            }

            // Check if the response contains valid content
            if (result.Content != null && result.Content.Count > 0)
            {
                return result.Content[0].Text ?? "Error: No text content in response.";
            }
            else
            {
                Console.WriteLine($"Error: Content array is null or empty in the response. Full response: {responseBody}");
                return "Error: Invalid response format.";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error calling the model: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return $"Error: {ex.Message}";
        }
    }

    // Nested classes for response deserialization
    private class AnthropicResponse
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string Role { get; set; }
        public string Model { get; set; }

        [JsonPropertyName("content")]
        public List<ContentItem> Content { get; set; }

        [JsonPropertyName("stop_reason")]
        public string StopReason { get; set; }

        [JsonPropertyName("stop_sequence")]
        public string? StopSequence { get; set; }

        [JsonPropertyName("usage")]
        public UsageData Usage { get; set; }
    }

    private class ContentItem
    {
        public string Type { get; set; }
        public string Text { get; set; }
    }

    private class UsageData
    {
        [JsonPropertyName("input_tokens")]
        public int InputTokens { get; set; }

        [JsonPropertyName("output_tokens")]
        public int OutputTokens { get; set; }

        [JsonPropertyName("cache_creation_input_tokens")]
        public int CacheCreationInputTokens { get; set; }

        [JsonPropertyName("cache_read_input_tokens")]
        public int CacheReadInputTokens { get; set; }
    }
}
