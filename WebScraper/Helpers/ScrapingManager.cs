using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebScraper.Services;

namespace WebScraper.Helpers
{
    internal class ScrapingManager
    {
        public async Task StartScraping(Dictionary<string, List<string>> links)
        {
            ListScraperService articleListScraper = new ListScraperService();
            foreach (var category in links)
            {
                foreach (string url in category.Value)
                {
                    await articleListScraper.ScrapeList(url);
                }
            }
        }
    }
}
