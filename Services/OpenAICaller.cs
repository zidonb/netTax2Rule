using System.Net.Http;
using System.Text;
using System.Text.Json;

public class OpenAICaller : IGenAICaller
{
    private readonly HttpClient _httpClient;
    private readonly string _modelName;
    private const string OPENAI_API_URL = "https://api.openai.com/v1/chat/completions";

    public OpenAICaller(string apiKey)
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        _modelName = "gpt-4"; // Default model, could be made configurable
    }

    public async Task<string> SendToGenAIAsync(string systemPrompt, string userPrompt)
    {
        try
        {
            var requestData = new
            {
                model = _modelName,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                max_tokens = 1000,
                temperature = 0
            };

            var content = new StringContent(
                JsonSerializer.Serialize(requestData),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync(OPENAI_API_URL, content);
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<OpenAIResponse>(jsonResponse);

            return result?.Choices?[0]?.Message?.Content ?? "Error: No response received from OpenAI.";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error calling OpenAI model: {ex.Message}");
            throw; // Rethrow to be handled by the calling service
        }
    }

    private class OpenAIResponse
    {
        public List<Choice> Choices { get; set; }
    }

    private class Choice
    {
        public Message Message { get; set; }
    }

    private class Message
    {
        public string Content { get; set; }
    }
}