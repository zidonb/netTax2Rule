public class AiSettings
{
    public string ModelProvider { get; set; }
    public string AnthropicApiKey { get; set; }
    public string OpenAiApiKey { get; set; }
    public string HaikuModel { get; set; }
    public string Haiku35Model { get; set; }
    public string OpusModel { get; set; }
    public string ClauseIntro { get; set; }
    public string GlossIntro { get; set; }
    public string LogicIntro { get; set; }
    public string PseudoIntro { get; set; }

    public string TaxClausesDirectory { get; set; } = "tax_clauses";
    public string InstructionsDirectory { get; set; } = "instructions";
    
}