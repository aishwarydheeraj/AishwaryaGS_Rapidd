using HtmlTable.Models;
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
            if(record.EmployeeName == null || record.StarTimeUtc == null || record.EndTimeUtc == null)
                continue;
            var diff = DateTime.Parse(record.EndTimeUtc) - DateTime.Parse(record.StarTimeUtc);
            if (diff <= TimeSpan.Zero)
                continue;

            var hours = diff.TotalHours;

            var existingEntry = employeeHours.Find(e => e.Name == record.EmployeeName);
            if(existingEntry != default)
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

        var sb = new StringBuilder();
        sb.AppendLine("<!doctype html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"utf-8\" />");
        sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />");
        sb.AppendLine("  <title>Employee Total Hours</title>");
        sb.AppendLine("  <style>");
        sb.AppendLine("    body { font-family: Arial, sans-serif; padding: 16px; }");
        sb.AppendLine("    th, td { border: 1px solid #ddd; padding: 8px; }");
        sb.AppendLine("    .low-hours { background-color: #ffdddd; color: #900; }");
        sb.AppendLine("  </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("  <h1>Employee Total Hours</h1>");
        sb.AppendLine("  <table>");
        sb.AppendLine("    <thead>");
        sb.AppendLine("      <tr><th>Name</th><th>Total Hours</th></tr>");
        sb.AppendLine("    </thead>");
        sb.AppendLine("    <tbody>");

        foreach (var t in employeeHours.OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase))
        {
            var name = WebUtility.HtmlEncode(t.Name);
            var totalHours = t.Hours.ToString("F2", CultureInfo.InvariantCulture);
            var human = FormatTimeSpan(TimeSpan.FromHours(t.Hours));
            var cssClass = t.Hours < 100.0 ? " class=\"low-hours\"" : string.Empty;
            sb.AppendLine($"      <tr{cssClass}><td>{name}</td><td>{totalHours}</td>");
        }

        sb.AppendLine("    </tbody>");
        sb.AppendLine("  </table>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        var outputFile = Path.Combine(Directory.GetCurrentDirectory(), "AishwaryaGS_C#Assignment.html");
        File.WriteAllText(outputFile, sb.ToString());

    }
    private static string FormatTimeSpan(TimeSpan ts) => $"{(int)ts.TotalHours}h {ts.Minutes}m {ts.Seconds}s";

}