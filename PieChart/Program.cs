using PieChart.Models;
using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

public class Program
{
    public static void Main(string[] args)
    {
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        var response = httpClient.GetAsync("https://rc-vault-fap-live-1.azurewebsites.net/api/gettimeentries?code=vO17RnE8vuzXzPJo5eaLLjXjmRW07law99QTD90zat9FfOQJKKUcgQ==").Result;
        var jsonString = response.Content.ReadAsStringAsync().Result;

        var records = JsonSerializer.Deserialize<List<EmployeeModel>>(jsonString);

        var employeeHours = new List<(string Name, double Hours)>();

        foreach (var record in records!)
        {
            if (record.EmployeeName == null || record.StarTimeUtc == null || record.EndTimeUtc == null)
                continue;
            var diff = DateTime.Parse(record.EndTimeUtc) - DateTime.Parse(record.StarTimeUtc);
            if (diff <= TimeSpan.Zero)
                continue;

            var hours = diff.TotalHours;

            var existingEntry = employeeHours.Find(e => e.Name == record.EmployeeName);
            if (existingEntry != default)
            {
                employeeHours.Remove(existingEntry);
                employeeHours.Add((record.EmployeeName, existingEntry.Hours + hours));
            }
            else
            {
                employeeHours.Add((record.EmployeeName, hours));
            }
        }

        // Print results
        foreach (var t in employeeHours.OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase))
        {
            Console.WriteLine($"{t.Name}: {t.Hours:F2} hours ({FormatTimeSpan(TimeSpan.FromHours(t.Hours))})");
        }
        var totals = employeeHours
            .GroupBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
            .Select(g => (Name: g.Key, Hours: g.Sum(x => x.Hours)))
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Prepare JSON arrays for Chart.js
        var labelsJson = JsonSerializer.Serialize(totals.Select(t => t.Name));
        var dataJson = JsonSerializer.Serialize(totals.Select(t => Math.Round(t.Hours, 2)));

        // Build HTML with embedded Chart.js pie chart
        var sb = new StringBuilder();
        sb.AppendLine("<!doctype html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"utf-8\" />");
        sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />");
        sb.AppendLine("  <title>Employee Hours - Pie Chart</title>");
        sb.AppendLine("  <style>");
        sb.AppendLine("    body { font-family: Arial, sans-serif; padding: 16px; }");
        sb.AppendLine("    .chart-container { max-width: 900px; margin: 0 auto; }");
        sb.AppendLine("  </style>");
        sb.AppendLine("  <script src=\"https://cdn.jsdelivr.net/npm/chart.js\"></script>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("  <h1>Employee Total Hours</h1>");
        sb.AppendLine("  <div class=\"chart-container\">");
        sb.AppendLine("    <canvas id=\"hoursPie\"></canvas>");
        sb.AppendLine("  </div>");
        sb.AppendLine("  <script>");
        sb.AppendLine($"    const labels = {labelsJson};");
        sb.AppendLine($"    const data = {dataJson};");
        sb.AppendLine("    const bgColors = labels.map((_, i) => `hsl(${(i * 360 / (labels.length || 1))}deg 65% 60%)`);");
        sb.AppendLine("    const borderColors = labels.map((_, i) => `hsl(${(i * 360 / (labels.length || 1))}deg 65% 40%)`);");
        sb.AppendLine("    const ctx = document.getElementById('hoursPie').getContext('2d');");
        sb.AppendLine("    new Chart(ctx, {");
        sb.AppendLine("      type: 'pie',");
        sb.AppendLine("      data: {");
        sb.AppendLine("        labels: labels,");
        sb.AppendLine("        datasets: [{");
        sb.AppendLine("          data: data,");
        sb.AppendLine("          backgroundColor: bgColors,");
        sb.AppendLine("          borderColor: borderColors,");
        sb.AppendLine("          borderWidth: 1");
        sb.AppendLine("        }]");
        sb.AppendLine("      },");
        sb.AppendLine("      options: {");
        sb.AppendLine("        plugins: {");
        sb.AppendLine("          legend: { position: 'right' },");
        sb.AppendLine("          tooltip: { callbacks: { label: (ctx) => `${ctx.label}: ${ctx.raw} hrs` } }");
        sb.AppendLine("        }");
        sb.AppendLine("      }");
        sb.AppendLine("    });");
        sb.AppendLine("  </script>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        var outputFile = Path.Combine(dir.Parent!.Parent!.Parent!.FullName, "AishwaryaGS_C#Assignment.html");
        File.WriteAllText(outputFile, sb.ToString());

    }
    private static string FormatTimeSpan(TimeSpan ts) => $"{(int)ts.TotalHours}h {ts.Minutes}m {ts.Seconds}s";

}