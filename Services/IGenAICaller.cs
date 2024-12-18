using System.Threading.Tasks;

public interface IGenAICaller
{
    /// <summary>
    /// Send a prompt to a generative AI model
    /// </summary>
    /// <param name="systemPrompt">The system-level instructions</param>
    /// <param name="userPrompt">The user's specific query</param>
    /// <returns>The generated response from the AI model</returns>
    Task<string> SendToGenAIAsync(string systemPrompt, string userPrompt);
}