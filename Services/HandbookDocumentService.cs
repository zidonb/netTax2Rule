using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.IO;

public class HandbookDocumentService
{
    private const string FileName = "Tax Ordinance to Machine-Readable Rule Handbook.docx";

    public void SaveToFile(string clauseNumber, string clause, string glossary, string logicRule, string pseudoRule, string jsonRule)
    {
        string filePath = Path.Combine(Directory.GetCurrentDirectory(), FileName);

        using (WordprocessingDocument document = File.Exists(filePath)
            ? WordprocessingDocument.Open(filePath, true)
            : WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document))
        {
            if (document.MainDocumentPart == null)
            {
                document.AddMainDocumentPart();
                document.MainDocumentPart.Document = new Document();
                document.MainDocumentPart.Document.AppendChild(new Body());
                InitializeStyles(document);
            }

            var body = document.MainDocumentPart.Document.Body;

            // Add "Clause <clauseNumber>"
            AddHeading(body, $"Clause Number: {clauseNumber}", 3);
            AddRtlParagraph(body, clause);

            // Add "Terms and inputs"
            AddHeading(body, "Terms and inputs", 4);
            AddParagraph(body, glossary);

            // Add "Logic Rule"
            AddHeading(body, "Logic Rule", 3);
            AddParagraph(body, logicRule);

            // Add "Pseudo-code"
            AddHeading(body, "Pseudo-code", 3);
            AddParagraph(body, pseudoRule);

            // Add "JSON"
            AddHeading(body, "JSON", 3);
            AddFormattedJsonParagraph(body, jsonRule);

            // Add page break
            body.AppendChild(new Paragraph(new Run(new Break { Type = BreakValues.Page })));

            document.MainDocumentPart.Document.Save();
        }
    }

    private void InitializeStyles(WordprocessingDocument doc)
    {
        var stylesPart = doc.MainDocumentPart.AddNewPart<StyleDefinitionsPart>();
        var styles = new Styles();

        // Default style with 11pt font
        var normalStyle = new Style
        {
            Type = StyleValues.Paragraph,
            StyleId = "Normal",
            Default = true
        };
        normalStyle.AppendChild(new StyleName { Val = "Normal" });
        var normalPPr = new ParagraphProperties();
        var normalRPr = new StyleRunProperties();
        normalRPr.AppendChild(new FontSize { Val = "22" }); // 11pt
        normalStyle.AppendChild(normalPPr);
        normalStyle.AppendChild(normalRPr);
        styles.AppendChild(normalStyle);

        // Heading styles
        var headingSizes = new Dictionary<int, int>
    {
        { 1, 32 }, // 16pt
        { 2, 28 }, // 14pt
        { 3, 24 }, // 12pt
        { 4, 22 }  // 11pt
    };

        foreach (var (level, fontSize) in headingSizes)
        {
            var headingStyle = new Style
            {
                Type = StyleValues.Paragraph,
                StyleId = $"Heading{level}",
                CustomStyle = false
            };

            headingStyle.AppendChild(new StyleName { Val = $"heading {level}" });
            headingStyle.AppendChild(new BasedOn { Val = "Normal" });
            headingStyle.AppendChild(new NextParagraphStyle { Val = "Normal" });

            var headingPPr = new ParagraphProperties();
            var headingRPr = new StyleRunProperties();

            headingRPr.AppendChild(new FontSize { Val = fontSize.ToString() });
            headingRPr.AppendChild(new Bold());

            headingStyle.AppendChild(headingPPr);
            headingStyle.AppendChild(headingRPr);

            styles.AppendChild(headingStyle);
        }

        stylesPart.Styles = styles;
    }
    private void AddHeading(Body body, string text, int level)
    {
        var paragraph = new Paragraph();
        var run = new Run(new Text(text));

        var paragraphProperties = new ParagraphProperties
        {
            ParagraphStyleId = new ParagraphStyleId { Val = $"Heading{level}" },
            KeepNext = new KeepNext(),
            SpacingBetweenLines = new SpacingBetweenLines
            {
                Before = "240",
                After = "120"
            }
        };

        paragraph.AppendChild(paragraphProperties);
        paragraph.AppendChild(run);
        body.AppendChild(paragraph);
    }

    private void AddRtlParagraph(Body body, string text)
    {
        var paragraph = new Paragraph();
        var run = new Run(new Text(text));

        var paragraphProperties = new ParagraphProperties
        {
            BiDi = new BiDi(),
            Justification = new Justification { Val = JustificationValues.Right }
        };

        paragraph.AppendChild(paragraphProperties);
        paragraph.AppendChild(run);
        body.AppendChild(paragraph);
    }

    private void AddParagraph(Body body, string text)
    {
        var paragraph = new Paragraph();
        var run = new Run(new Text(text));

        var paragraphProperties = new ParagraphProperties
        {
            SpacingBetweenLines = new SpacingBetweenLines
            {
                Before = "120",
                After = "120"
            }
        };

        paragraph.AppendChild(paragraphProperties);
        paragraph.AppendChild(run);
        body.AppendChild(paragraph);
    }

    private void AddFormattedJsonParagraph(Body body, string json)
    {
        var formattedJson = PrettyFormatJson(json);
        var paragraph = new Paragraph();
        var run = new Run(new Text(formattedJson));

        var paragraphProperties = new ParagraphProperties
        {
            SpacingBetweenLines = new SpacingBetweenLines
            {
                Line = "240",
                LineRule = LineSpacingRuleValues.Auto,
                Before = "120",
                After = "120"
            }
        };

        paragraph.AppendChild(paragraphProperties);
        paragraph.AppendChild(run);
        body.AppendChild(paragraph);
    }

    private string PrettyFormatJson(string json)
    {
        try
        {
            var jsonElement = System.Text.Json.JsonDocument.Parse(json).RootElement;
            return System.Text.Json.JsonSerializer.Serialize(jsonElement, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        }
        catch
        {
            return json;
        }
    }
}