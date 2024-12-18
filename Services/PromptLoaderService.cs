using System.Text;
using System.Collections.Concurrent;

public class PromptLoaderService
{
    private readonly ConcurrentDictionary<string, string> _promptCache = new();

    public string LoadPrompt(string directoryPath, string promptName)
    {
        string cacheKey = $"{directoryPath}_{promptName}";
        
        return _promptCache.GetOrAdd(cacheKey, _ =>
        {
            string filePath = Path.Combine(directoryPath, $"{promptName}.txt");
            
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Prompt file not found: {filePath}");
            }

            return File.ReadAllText(filePath, Encoding.UTF8);
        });
    }
}