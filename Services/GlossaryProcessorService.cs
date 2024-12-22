using Microsoft.Extensions.Options;

public class GlossaryProcessorService
{
    private readonly IGenAICaller _aiCaller;
    private readonly GlossaryDocumentService _glossDocService;
    private readonly PromptLoaderService _promptLoader;
    private readonly AiSettings _aiSettings;

    public GlossaryProcessorService(
        IGenAICaller aiCaller,
        GlossaryDocumentService glossDocService,
        PromptLoaderService promptLoader,
        IOptions<AiSettings> aiSettings)
    {
        _aiCaller = aiCaller;
        _glossDocService = glossDocService;
        _promptLoader = promptLoader;
        _aiSettings = aiSettings.Value;

        InitializeDirectories();
    }

    private void InitializeDirectories()
    {
        // Ensure directories exist in a separate method
        Directory.CreateDirectory(_aiSettings.TaxClausesDirectory);
        Directory.CreateDirectory(_aiSettings.InstructionsDirectory);
    }

    public async Task ProcessAllClauses()
    {
        try
        {
            Console.WriteLine("Loading prompts from instructions directory...");
            var glossaryExtractorPrompt = _promptLoader.LoadPrompt(_aiSettings.InstructionsDirectory, "glossary_extractor");

            Console.WriteLine($"Reading tax clauses from {_aiSettings.TaxClausesDirectory}...");
            var txtFiles = Directory.GetFiles(_aiSettings.TaxClausesDirectory, "*.txt");

            if (txtFiles.Length == 0)
            {
                Console.WriteLine($"No .txt files found in {_aiSettings.TaxClausesDirectory}");
                return;
            }

            var sortedFiles = SortFiles(txtFiles);
            Console.WriteLine($"Found {txtFiles.Length} clause files to process.");

            foreach (var filePath in sortedFiles)
            {
                var clauseNumber = Path.GetFileNameWithoutExtension(filePath);
                Console.WriteLine($"Processing clause {clauseNumber}...");

                try
                {
                    var clause = await ReadFileAsync(filePath);
                    await ProcessClause(clauseNumber, clause, glossaryExtractorPrompt);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading file {filePath}: {ex.Message}");
                }
            }

            Console.WriteLine("All clauses processed successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during processing: {ex.Message}");
            throw;
        }
    }

    private IEnumerable<string> SortFiles(string[] files)
    {
        // Encapsulated sorting logic
        return files.OrderBy(f => f, new NaturalStringComparer());
    }

    private async Task<string> ReadFileAsync(string filePath)
    {
        // Isolated file reading with error handling
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");

        return await File.ReadAllTextAsync(filePath);
    }

    private async Task ProcessClause(string clauseNumber, string clause, string glossaryExtractorPrompt)
    {
        try
        {
            Console.WriteLine($"Generating glossary for clause {clauseNumber}...");
            var glossary = await _aiCaller.SendToGenAIAsync(glossaryExtractorPrompt, clause);

            Console.WriteLine($"Saving results for clause {clauseNumber} to Glossary Excel...");
            _glossDocService.SaveToFile(glossary);

            Console.WriteLine($"Successfully processed clause {clauseNumber}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing clause {clauseNumber}: {ex.Message}");
            throw;
        }
    }
}


