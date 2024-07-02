using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;

using System.Collections.Generic;
using System.IO;


public class HtmlSummaryExporter : IExporter
{
    public string Name => nameof(HtmlSummaryExporter);

    public void ExportToLog(Summary summary, ILogger logger)
    {

    }

    public IEnumerable<string> ExportToFiles(Summary summary, ILogger consoleLogger)
    {
        string directoryPath = summary.ResultsDirectoryPath;
        string outputPath = Path.Combine(directoryPath, "Summary.html");

        var htmlFiles = Directory.GetFiles(directoryPath, "*.html");

        using (StreamWriter writer = new StreamWriter(outputPath))
        {
            writer.WriteLine("<html>");
            writer.WriteLine("<head>");
            writer.WriteLine("<title>Benchmark Summary</title>");

            writer.WriteLine("<style>");
            foreach (var file in htmlFiles)
            {
                string content = File.ReadAllText(file);
                string styleContent = GetStyleContent(content);
                writer.WriteLine(styleContent);
            }
            writer.WriteLine("</style>");

            writer.WriteLine("</head>");
            writer.WriteLine("<body>");

            foreach (var file in htmlFiles)
            {
                string fileName = Path.GetFileName(file);
                writer.WriteLine($"<h2>{fileName}</h2>"); 
                string content = File.ReadAllText(file);
                string bodyContent = GetBodyContent(content);
                writer.WriteLine(bodyContent);
            }

            writer.WriteLine("</body>");
            writer.WriteLine("</html>");
        }

        consoleLogger.WriteLine($"Summary HTML file created successfully at {outputPath}.");

        return new[] { outputPath };
    }

    private string GetBodyContent(string html)
    {
        int bodyStartIndex = html.IndexOf("<body>") + "<body>".Length;
        int bodyEndIndex = html.IndexOf("</body>");
        if (bodyStartIndex >= 0 && bodyEndIndex >= 0)
        {
            return html.Substring(bodyStartIndex, bodyEndIndex - bodyStartIndex);
        }
        return string.Empty;
    }

    private string GetStyleContent(string html)
    {
        int styleStartIndex = html.IndexOf("<style>") + "<style>".Length;
        int styleEndIndex = html.IndexOf("</style>");
        if (styleStartIndex >= 0 && styleEndIndex >= 0)
        {
            return html.Substring(styleStartIndex, styleEndIndex - styleStartIndex);
        }
        return string.Empty;
    }
}