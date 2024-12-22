using OfficeOpenXml;
using OfficeOpenXml.Table;

public class GlossaryDocumentService
{
    private readonly string _outputFilePath;

    public GlossaryDocumentService(IConfiguration configuration)
    {
        // Read output file path from appsettings.json
        _outputFilePath = configuration["GlossarySettings:OutputFilePath"];
        if (string.IsNullOrEmpty(_outputFilePath))
        {
            throw new InvalidOperationException("OutputFilePath is not set in configuration.");
        }

        // EPPlus license configuration
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    public void SaveToFile(string glossaryText)
    {
        try
        {
            if (!glossaryText.Contains("1. List of Terms") || !glossaryText.Contains("2. List of Necessary Inputs"))
            {
                throw new FormatException("Glossary text is missing required sections.");
            }

            Console.WriteLine("Parsing glossary...");
            var (terms, inputs) = ParseGlossary(glossaryText);

            if (terms.Count == 0)
                Console.WriteLine("No terms found to save.");
            if (inputs.Count == 0)
                Console.WriteLine("No inputs found to save.");

            using var package = new ExcelPackage(new FileInfo(_outputFilePath));

            // Process the "Terms" sheet
            var termsSheet = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == "Terms") ??
                             package.Workbook.Worksheets.Add("Terms");
            AppendDataToSheet(termsSheet, terms);

            // Process the "Inputs" sheet
            var inputsSheet = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == "Inputs") ??
                              package.Workbook.Worksheets.Add("Inputs");
            AppendDataToSheet(inputsSheet, inputs);

            // Save the Excel file
            package.Save();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving to Excel file: {ex.Message}");
            throw;
        }
    }


    private void AppendDataToSheet(ExcelWorksheet sheet,
                                   List<(string termOrInput, string translation, string description, string variableName)> data)
    {
        if (sheet.Dimension == null)
        {
            // Initialize headers if sheet is empty
            sheet.Cells[1, 1].Value = "Term/Input";
            sheet.Cells[1, 2].Value = "Translation";
            sheet.Cells[1, 3].Value = "Description";
            sheet.Cells[1, 4].Value = "Variable Name";
        }

        int startRow = sheet.Dimension?.End.Row + 1 ?? 2; // Determine the starting row for appending data

        for (int i = 0; i < data.Count; i++)
        {
            sheet.Cells[startRow + i, 1].Value = data[i].termOrInput;
            sheet.Cells[startRow + i, 2].Value = data[i].translation;
            sheet.Cells[startRow + i, 3].Value = data[i].description;
            sheet.Cells[startRow + i, 4].Value = data[i].variableName;
        }

        // Format as a table for better readability
        var range = sheet.Cells[1, 1, startRow + data.Count - 1, 4];
        var table = sheet.Tables.FirstOrDefault(t => t.Name == sheet.Name + "Table") ??
                    sheet.Tables.Add(range, sheet.Name + "Table");
        table.TableStyle = TableStyles.Medium9;
    }


    public (List<(string, string, string, string)> terms,
            List<(string, string, string, string)> inputs) ParseGlossary(string glossaryText)
    {
        var terms = new List<(string, string, string, string)>();
        var inputs = new List<(string, string, string, string)>();

        var termsSection = ExtractSection(glossaryText, "1. List of Terms");
        var inputsSection = ExtractSection(glossaryText, "2. List of Necessary Inputs");

        foreach (var line in termsSection.Split(',')) // Assuming items are comma-separated
        {
            var parts = line.Trim().Split(',');
            if (parts.Length == 4)
                terms.Add((parts[0].Trim(), parts[1].Trim(), parts[2].Trim(), parts[3].Trim()));
        }

        foreach (var line in inputsSection.Split(',')) // Assuming items are comma-separated
        {
            var parts = line.Trim().Split(',');
            if (parts.Length == 4)
                inputs.Add((parts[0].Trim(), parts[1].Trim(), parts[2].Trim(), parts[3].Trim()));
        }

        return (terms, inputs);
    }

    private string ExtractSection(string text, string sectionTitle)
    {
        var startIndex = text.IndexOf($"**{sectionTitle}**:") + sectionTitle.Length + 3;
        var endIndex = text.IndexOf("**", startIndex);
        return text.Substring(startIndex, endIndex - startIndex).Trim('`', ' ');
    }

}
