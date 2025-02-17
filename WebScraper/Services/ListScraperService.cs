using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebScraper.Helpers;
using WebScraper.Models;
using System.Data.Common;
using System.ComponentModel;
using System.Collections.Specialized;
using WebScraper.Models;

namespace WebScraper.Services
{
    internal class ListScraperService
    {
        internal async Task<List<BaseArticleModel>> ScrapeList(string url)
        {
            List<BaseArticleModel> articlesFromList = ScrapeArticles(url);
            if (articlesFromList != null)
            {
                List<ArticleModel> scrapedArticles = new List<ArticleModel>();
                List<BaseArticleModel> notScrapedArticles = new List<BaseArticleModel>();

                ArticleScraperService webScraper = new ArticleScraperService();

                foreach (BaseArticleModel article in articlesFromList)
                {
                    if (article.Url.EndsWith(".pdf") || article.Url.Contains("tiabk") || article.Url.EndsWith("pps") || article.Url.EndsWith("mp4"))
                    {
                        continue;
                    }

                    HtmlWeb web = new HtmlWeb { OverrideEncoding = Encoding.UTF8 };
                    HtmlDocument htmlDoc = web.Load(article.Url);
                    ArticleModel? scrapedArticle = await webScraper.ScrapeArticle(htmlDoc, article.Url, article.Title);

                    if (scrapedArticle != null)
                    {
                        scrapedArticles.Add(scrapedArticle);
                    }
                    else
                    {
                        notScrapedArticles.Add(article);
                    } 
                }
                Console.WriteLine();
                Console.WriteLine("Article List Count: " + articlesFromList.Count());
                Console.WriteLine("Article Scrape Count: " + scrapedArticles.Count());
                Console.WriteLine();
                foreach (BaseArticleModel notScrapedArticle in notScrapedArticles)
                {
                    Console.WriteLine(notScrapedArticle.Url);
                    Console.WriteLine(notScrapedArticle.Title);
                    Console.WriteLine(notScrapedArticle.Description);
                    Console.WriteLine();
                }
            }
            return articlesFromList;
        }

        internal List<BaseArticleModel>? ScrapeArticles(string url)
        {
            HtmlDocument htmlDoc = GetHtmlDocument(url);
            string category = htmlDoc.DocumentNode.SelectSingleNode("//font[@size='6' or @size='7']").InnerText;

            IEnumerable<HtmlNode> linkElements = GetLinkElements(url, htmlDoc, category);
            if (linkElements.Count() == 0) return null;

            List<BaseArticleModel> articleLinks = new List<BaseArticleModel>();
            foreach (HtmlNode linkElement in linkElements)
            {
                if (linkElement.containsTDNestedInTD()) continue;
                if (linkElement.IsSeries())
                {
                    List<BaseArticleModel> articleModels = GetBaseArticleListFromSeries(linkElement, url);
                    articleLinks.AddRange(articleModels);
                } 
                else
                {
                    if (linkElement.IsNullOrBadLink()) continue;
                    BaseArticleModel articleModel = GetBaseArticle(linkElement, url, category);
                    articleLinks.Add(articleModel);
                }
            }
            return articleLinks;
        }

        private HtmlDocument GetHtmlDocument(string url)
        {
            HtmlWeb web = new HtmlWeb { OverrideEncoding = Encoding.UTF8 };
            HtmlDocument htmlDoc = web.Load(url);
            return htmlDoc;
        }


        private IEnumerable<HtmlNode> GetLinkElements(string url, HtmlDocument htmlDoc, string category)
        {
            IEnumerable<HtmlNode> linkElements = new List<HtmlNode>();

            if (category.MatchesAnyOf(ScrapingHelper.linksWithNoDescription))
            {
                linkElements = htmlDoc.DocumentNode.SelectNodes("//a");
            }
            else
            {
                linkElements = htmlDoc.DocumentNode.SelectNodes("//td");
            }

            return linkElements;
        }

        private BaseArticleModel GetBaseArticle(HtmlNode linkElement, string url, string category)
        {
            if (category.MatchesAnyOf(ScrapingHelper.linksWithNoDescription))
            {
                return new BaseArticleModel
                {
                    Url = linkElement.GetAttributeValue("href", ""),
                    Title = linkElement.InnerText,
                };
            }
            else
            {
                HtmlDocument linkElementDoc = new HtmlDocument();
                linkElementDoc.LoadHtml(linkElement.InnerHtml);
                IEnumerable<HtmlNode> anchorNodes = linkElementDoc.DocumentNode.SelectNodes(".//a");
                HtmlNode? goodAnchorNode = null;
                foreach (HtmlNode anchorNode in anchorNodes)
                {
                    if (anchorNode.isBadAnchorTag()) continue;
                    goodAnchorNode = anchorNode;
                }

                HtmlNode? descriptionNode = linkElement.SelectSingleNode(".//span")
                    ?? linkElement.SelectSingleNode(".//*[@size='3' and @color='MAROON']")
                    ?? linkElement.SelectSingleNode(".//*[@color='#800000']")
                    ?? linkElement.SelectSingleNode(".//*[@color='#FF0000']")?.SelectSingleNode("text()[normalize-space()]")
                    ?? linkElement.SelectSingleNode(".//*[@size='3']")
                    ?? null;

                if (goodAnchorNode != null)
                {
                    return new BaseArticleModel
                    {
                        Url = HtmlParsingHelper.CleanLink(goodAnchorNode.GetAttributeValue("href", ""), url, true),
                        Title = goodAnchorNode.InnerText.Trim(),
                        Description = descriptionNode?.InnerText.Trim(),
                    };
                }
                return null;
            }
        }


        private List<BaseArticleModel> GetBaseArticleListFromSeries(HtmlNode linkElement, string url)
        {
            var allBElementsDoc = new HtmlDocument();
            allBElementsDoc.LoadHtml(linkElement.InnerHtml);
            IEnumerable<HtmlNode> bElements = allBElementsDoc.DocumentNode.SelectNodes("//b");
            List<BaseArticleModel> baseArticleModelList = new List<BaseArticleModel>();
            if (bElements != null)
            {
                foreach (HtmlNode bElement in bElements)
                {
                    var bElementDoc = new HtmlDocument();
                    bElementDoc.LoadHtml(bElement.InnerHtml);
                    IEnumerable<HtmlNode> anchorNodes = bElementDoc.DocumentNode.SelectNodes(".//a");
                    if (anchorNodes != null)
                    {
                        foreach (HtmlNode anchorNode in anchorNodes)
                        {
                            if (anchorNode.isBadAnchorTag()) continue;
                            string? descriptionBeforeCleanUp = bElement.SelectSingleNode("following-sibling::*[@color='#800000']")?.InnerText;

                            BaseArticleModel baseArticleModel = new BaseArticleModel 
                            { 
                                Url = HtmlParsingHelper.CleanLink(anchorNode.GetAttributeValue("href", ""), url, true),
                                Title = HtmlEntity.DeEntitize(anchorNode.InnerText).Trim(),
                                Description = !String.IsNullOrWhiteSpace(descriptionBeforeCleanUp) ?  HtmlEntity.DeEntitize(descriptionBeforeCleanUp).Trim() : null,
                            };
                            baseArticleModelList.Add(baseArticleModel);
                        }
                    }
                }
            }
            return baseArticleModelList;
        }
    }
}
