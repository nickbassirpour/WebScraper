using System;
using System.Runtime.CompilerServices;
using System.Text.Json;
using WebScraper.Helpers;
using WebScraper.Services;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .CreateLogger();

async void BeginScrape(string url)
{
    ListScraperService articleListScraper = new ListScraperService();
    await articleListScraper.ScrapeList(url);
}

var jsonLinks = System.IO.File.ReadAllText(@"C:\Users\nickb\Desktop\Code_Projects\WebScraperApp\WebScraper\Data\testLinks.json");
var links = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(jsonLinks);

if (links != null)
{
    foreach (var category in links)
    {
        string categoryName = category.Key;

        List<string> urls = category.Value;

        foreach (string url in urls)
        {
            Log.Information($"Scraping section: {url}");
            BeginScrape(url);
        }
    }
}



