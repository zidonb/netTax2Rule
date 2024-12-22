using Microsoft.Extensions.Options;

public class TaxProcessorService
{
    private readonly IGenAICaller _aiCaller;
    private readonly HandbookDocumentService _handbookService;
    private readonly PromptLoaderService _promptLoader;
    private readonly AiSettings _aiSettings;

    public TaxProcessorService(
        IGenAICaller aiCaller,
        HandbookDocumentService handbookService,
        PromptLoaderService promptLoader,
        IOptions<AiSettings> aiSettings)
    {
        _aiCaller = aiCaller;
        _handbookService = handbookService;
        _promptLoader = promptLoader;
        _aiSettings = aiSettings.Value;

        // Ensure directories exist
        Directory.CreateDirectory(_aiSettings.TaxClausesDirectory);
        Directory.CreateDirectory(_aiSettings.InstructionsDirectory);
    }

    public async Task ProcessAllClauses()
    {
        try
        {
            // Load all prompts from instructions directory
            Console.WriteLine("Loading prompts from instructions directory...");
            var glossaryExtractorPrompt = _promptLoader.LoadPrompt(_aiSettings.InstructionsDirectory, "glossary_extractor");
            var toLogicPrompt = _promptLoader.LoadPrompt(_aiSettings.InstructionsDirectory, "clause_to_logic");
            var toPseudoPrompt = _promptLoader.LoadPrompt(_aiSettings.InstructionsDirectory, "logic_to_pseudo");
            var toJsonPrompt = _promptLoader.LoadPrompt(_aiSettings.InstructionsDirectory, "pseudo_to_json");

            // Get all .txt files from the tax_clauses directory
            Console.WriteLine($"Reading tax clauses from {_aiSettings.TaxClausesDirectory}...");
            var txtFiles = Directory.GetFiles(_aiSettings.TaxClausesDirectory, "*.txt");

            if (txtFiles.Length == 0)
            {
                Console.WriteLine($"No .txt files found in {_aiSettings.TaxClausesDirectory}");
                return;
            }

            // Sort files using natural sort order
            var sortedFiles = txtFiles.OrderBy(f => f, new NaturalStringComparer());
            Console.WriteLine($"Found {txtFiles.Length} clause files to process.");

            foreach (var filePath in sortedFiles)
            {
                var clauseNumber = Path.GetFileNameWithoutExtension(filePath);
                Console.WriteLine($"Processing clause {clauseNumber}...");

                var clause = await File.ReadAllTextAsync(filePath);

                await ProcessClause(clauseNumber, clause,
                    glossaryExtractorPrompt, toLogicPrompt,
                    toPseudoPrompt, toJsonPrompt);
            }

            Console.WriteLine("All clauses processed successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during processing: {ex.Message}");
            throw;
        }
    }

    private async Task ProcessClause(
        string clauseNumber,
        string clause,
        string glossaryExtractorPrompt,
        string toLogicPrompt,
        string toPseudoPrompt,
        string toJsonPrompt)
    {
        try
        {
            Console.WriteLine($"Generating glossary for clause {clauseNumber}...");
            // Just use the clause for glossary extraction
            var glossary = await _aiCaller.SendToGenAIAsync(glossaryExtractorPrompt, clause);

            Console.WriteLine($"Generating logic rule for clause {clauseNumber}...");
            // Combine clause and glossary with correct intros
            var logicRule = await _aiCaller.SendToGenAIAsync(
                toLogicPrompt,
                _aiSettings.ClauseIntro + clause + _aiSettings.GlossIntro + glossary
            );

            Console.WriteLine($"Generating pseudo-code for clause {clauseNumber}...");
            // Use logic rule and glossary with correct intros
            var pseudoRule = await _aiCaller.SendToGenAIAsync(
                toPseudoPrompt,
                _aiSettings.LogicIntro + logicRule + _aiSettings.GlossIntro + glossary
            );

            Console.WriteLine($"Generating JSON for clause {clauseNumber}...");
            // Use pseudo rule and glossary with correct intros
            var jsonRule = await _aiCaller.SendToGenAIAsync(
                toJsonPrompt,
                _aiSettings.PseudoIntro + pseudoRule + _aiSettings.GlossIntro + glossary
            );

            Console.WriteLine($"Saving results for clause {clauseNumber} to handbook...");
            _handbookService.SaveToFile(clauseNumber, clause, glossary, logicRule, pseudoRule, jsonRule);

            Console.WriteLine($"Successfully processed clause {clauseNumber}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing clause {clauseNumber}: {ex.Message}");
            throw;
        }
    }
}