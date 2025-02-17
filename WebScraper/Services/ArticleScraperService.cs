using HtmlAgilityPack;
using System;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using WebScraper.Helpers;
using WebScraper.Models;
using static System.Net.Mime.MediaTypeNames;
using System.Drawing;
using RestSharp;

namespace WebScraper.Services
{
    internal class ArticleScraperService
    {
        public async Task<ArticleModel?> ScrapeArticle(HtmlDocument htmlDoc, string url, string titleFromBaseArticle)
        {
            string splitHtmlBody = HtmlParsingHelper.SplitHtmlBody(htmlDoc, url);

            if (splitHtmlBody == null)
            {
                return null;
            }
            string category = HtmlParsingHelper.GetCategoryFromURL(url);

            Console.WriteLine();

            ArticleModel articleModel = new ArticleModel();
            articleModel.Url = url;
            articleModel.Category = category;
            articleModel.SubCategory = GetSubCategory(htmlDoc, category);
            articleModel.Series = GetSeriesNameAndNumber(htmlDoc) != null ? GetSeriesNameAndNumber(htmlDoc)[0] : null;
            articleModel.SeriesNumber = GetSeriesNameAndNumber(htmlDoc) != null ? GetSeriesNameAndNumber(htmlDoc)[1] : null;
            articleModel.Title = GetTitle(htmlDoc, category, titleFromBaseArticle);
            articleModel.Author = GetAuthor(htmlDoc, category);
            articleModel.BodyHtml = GetBody(splitHtmlBody, url);
            articleModel.BodyInnerText = GetBodyInnerText(splitHtmlBody);
            articleModel.ThumbnailURL = GetThumbnailUrl(splitHtmlBody, url);
            articleModel.Date = GetDate(htmlDoc, category);
            articleModel.RelatedArticles = GetRelatedArticles(htmlDoc, splitHtmlBody, url);

            RestClient client = new RestClient("http://localhost:5223/");
            RestRequest request = new RestRequest("add_new_article", Method.Post);

            request.AddJsonBody(articleModel);
            request.AddHeader("Content-Type", "application/json");

            try
            {
                RestResponse response = await client.ExecuteAsync(request);
                if (!response.IsSuccessful)
                {
                    Console.WriteLine("failed");
                }
                else
                {
                    return articleModel;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred: {ex.Message}");
                return null;
            }

            return articleModel;
        }

        public string? GetSubCategory(HtmlDocument htmlDoc, string category)
        {
            if (category.MatchesAnyOf(ScrapingHelper.categoriesWithNoSubcatregory))
            {
                return category;
            }

            HtmlNode? categoryFromTopicHeaderOrH3 = htmlDoc.DocumentNode.Descendants().FirstOrDefault(node => node.Id == "topicHeader" || node.Element("h3") != null);
            if (categoryFromTopicHeaderOrH3 != null)
            {
                return categoryFromTopicHeaderOrH3.InnerText.Trim();
            }

            HtmlNode? categoryFromColorAndSize = htmlDoc.DocumentNode.SelectSingleNode(".//*[@color='#800000' and @size='2']");
            if (categoryFromColorAndSize != null)
            {
                return categoryFromColorAndSize.InnerText.Trim();
            }

            return null;
        }

        private List<BaseArticleModel>? GetRelatedArticles(HtmlDocument htmlDoc, string htmlBody, string url)
        {
            List<string> splitArticleParts = htmlDoc.DocumentNode.InnerHtml.Split("Related Topics of Interest").ToList();

            if (splitArticleParts.Count() == 1)
            {
                return null;
            }

            List<BaseArticleModel> relatedArticles = new List<BaseArticleModel>();
            IEnumerable<HtmlNode> linkElements = HtmlParsingHelper.GetLinkElements(splitArticleParts[1]);

            if (linkElements.Count() == 0)
            {
                return null;
            }
   
            foreach (HtmlNode linkElement in linkElements)
            {
                if (String.IsNullOrWhiteSpace(linkElement.InnerText)) continue;
                if (linkElement.InnerText.MatchesAnyOf(ScrapingHelper.linkTextsNotToScrape)) continue;

                relatedArticles.Add(new BaseArticleModel()
                {
                    Url = HtmlParsingHelper.CleanLink(linkElement.GetAttributeValue("href", ""), url, true),
                    Title = linkElement.InnerText,
                });
            }
            
            return relatedArticles;
        }

        public List<string?> GetSeriesNameAndNumber(HtmlDocument htmlDoc)
        {
            HtmlNode? series = htmlDoc.DocumentNode.SelectSingleNode("//*[@class='GreenSeries' or @class='greenseries' or @size='4' and @color='GREEN']");
            if (series == null || string.IsNullOrWhiteSpace(series.InnerText))
            {
                return null;
            }

            List<string> seriesParts = new List<string>();

            if (series.InnerText.Contains("-"))
            {
                seriesParts = series.InnerText.Split("-").ToList();
                seriesParts[0].Replace("&nbsp;", "").Trim();
                seriesParts[1].Replace("&nbsp;", "").Trim();
                return seriesParts;
            }

            if (series.InnerText.Contains("–"))
            {
                seriesParts = series.InnerText.Split("–").ToList();
                seriesParts[0].Replace("&nbsp;", "").Trim();
                seriesParts[1].Replace("&nbsp;", "").Trim();
                return seriesParts;
            }

            return null;
        }

        public string? GetTitle(HtmlDocument htmlDoc, string category, string titleFromBaseArticle)
        {
            if (category == "bev")
            {
                return titleFromBaseArticle;
            }

            if (category == "RevolutionPhotos")
            {
                HtmlNode? titleForRevolutionPhotos = htmlDoc.DocumentNode.SelectSingleNode("//*[@size=4 and @color='#800000']");
                if (titleForRevolutionPhotos != null)
                {
                    return titleForRevolutionPhotos.InnerText.Trim();
                }

                HtmlNode? titleFromHTagsChurchRev = htmlDoc.DocumentNode.Descendants().FirstOrDefault(node => node.Name == "h1");
                if (titleFromHTagsChurchRev != null)
                {
                    return titleFromHTagsChurchRev.InnerText.Trim();
                }
            }

            HtmlNode? titleFromHTags = htmlDoc.DocumentNode.Descendants().FirstOrDefault(node => node.Name == "h1" || node.Name == "h4");
            if (titleFromHTags != null)
            {
                return titleFromHTags.InnerText.Trim();
            }

            HtmlNode? titleFromSizeAndColorMaroon = htmlDoc.DocumentNode.SelectSingleNode("//*[@size=6 and @color='maroon' or @size=6 and @color='#800000']");
            if (titleFromSizeAndColorMaroon != null)
            {
                return titleFromSizeAndColorMaroon.InnerText.Trim();
            }

            HtmlNode? titleFromSizeAndColor99000 = htmlDoc.DocumentNode.SelectSingleNode("//*[@size=6 and @color='#990000']");
            if (titleFromSizeAndColor99000 != null)
            {
                return titleFromSizeAndColor99000.InnerText.Trim();
            }

            HtmlNode? titleFromSizeAndColorGreen = htmlDoc.DocumentNode.SelectSingleNode("//*[@size=6 and @color='green']");
            if (titleFromSizeAndColorGreen != null)
            {
                return titleFromSizeAndColorGreen.InnerText.Trim();
            }

            HtmlNode? titleFromSizeAndColorMAROON = htmlDoc.DocumentNode.SelectSingleNode("//*[@size=6 and @color='MAROON']");
            if (titleFromSizeAndColorMAROON != null)
            {
                return titleFromSizeAndColorMAROON.InnerText.Trim();
            }

            HtmlNode? titleFromSize5AndColor800000 = htmlDoc.DocumentNode.SelectSingleNode("//*[@size=5 and @color='#800000']");
            if (titleFromSize5AndColor800000 != null)
            {
                return titleFromSize5AndColor800000.InnerText.Trim();
            }

            return null;
        }

        public List<string?> GetAuthor(HtmlDocument htmlDoc, string category)
        {
            if (category == "RevolutionPhotos")
            {
                return null;
            }

            if (category == "bev")
            {
                List<string> atilaAuthorList = new List<string> { "Atila S. Guimarães" };
                return atilaAuthorList;
            }

            if (category == "OrganicSociety")
            {
                List<string> plinioAuthorList = new List<string> { "Plinio Corrêa de Oliveira" };
                return plinioAuthorList;
            }

            if (category == "Questions")
            {
                List<string> correspondenceAuthorList = new List<string> { "TIA Correspondence Desk" };
                return correspondenceAuthorList;
            }

                HtmlNode? authorFromAuthorClass = htmlDoc.DocumentNode.SelectSingleNode("//*[@class='author']");
            if (authorFromAuthorClass != null && !String.IsNullOrWhiteSpace(authorFromAuthorClass.InnerText))
            {
                return SplitAuthors(authorFromAuthorClass);
            }

            HtmlNode? authorFromSizeAndId = htmlDoc.DocumentNode.SelectSingleNode("//*[@size=4 and @id='R']");
            if (authorFromSizeAndId != null && !String.IsNullOrWhiteSpace(authorFromSizeAndId.InnerText))
            {
                return SplitAuthors(authorFromSizeAndId);
            }

            HtmlNode? authorFromSizeAndColor = htmlDoc.DocumentNode.SelectSingleNode("//*[@size=4 and @color='PURPLE']");
            if (authorFromSizeAndColor != null && !String.IsNullOrWhiteSpace(authorFromSizeAndColor.InnerText))
            {
                return SplitAuthors(authorFromSizeAndColor);
            }

            HtmlNode? authorFromSizeOnly = htmlDoc.DocumentNode.SelectSingleNode("//*[@size=4]");
            if (authorFromSizeOnly != null && !String.IsNullOrWhiteSpace(authorFromSizeOnly.InnerText))
            {
                return SplitAuthors(authorFromSizeOnly);
            }

            if (htmlDoc.DocumentNode.InnerText.ToLower().Contains("dr. horvat responds"))
            {
                return new List<string?> { "Dr. Marian Therese Horvat" };
            }

            if (htmlDoc.DocumentNode.InnerText.ToLower().Contains("tia correspondence desk") ||
                htmlDoc.DocumentNode.InnerText.ToLower().Contains("tia responds"))
            {
                return new List<string?> { "TIA Correspondence Desk" };
            }

            return null;
        }

        public List<string> SplitAuthors(HtmlNode authorText)
        {
            return authorText.InnerText.Trim()
                .Split(new[] { "and", ",", "&" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(author => author.Trim())
                .ToList();
        }

        public string GetBody(string htmlBody, string url)
        {
            HtmlDocument htmlDocBody = HtmlParsingHelper.LoadHtmlDocument(htmlBody);
            HtmlNode parsedBody = HtmlParsingHelper.ParseBody(htmlDocBody.DocumentNode, url);
            return parsedBody.InnerHtml;
        }

        private string GetBodyInnerText(string htmlBody)
        {
            HtmlDocument htmlDocBody = HtmlParsingHelper.LoadHtmlDocument(htmlBody);
            return htmlDocBody.DocumentNode.InnerText;
        }

        public string? GetDate(HtmlDocument htmlDoc, string category)
        {
            if (category == "bev")
            {
                return GetBevDate(htmlDoc);
            }

            HtmlNode? dateFromId = htmlDoc.DocumentNode.SelectSingleNode("//*[@id=\'posted\' or @id=\'sitation\']");
            if (!string.IsNullOrWhiteSpace(dateFromId?.InnerText))
            {
                string cleanedDateFromId = dateFromId.InnerText;
                return HtmlParsingHelper.ConvertStringToDate(cleanedDateFromId);
            }

            HtmlNode? dateElementFromSize1AndColorNavy = htmlDoc.DocumentNode.SelectSingleNode("//*[@size='1' and @color='navy']");
            if (!string.IsNullOrWhiteSpace(dateElementFromSize1AndColorNavy?.InnerText) && dateElementFromSize1AndColorNavy.InnerText.Contains("Posted"))
            {
                string cleanedDateFromSizeAndColor = dateElementFromSize1AndColorNavy.InnerText;
                return HtmlParsingHelper.ConvertStringToDate(cleanedDateFromSizeAndColor);
            }

            HtmlNode? dateFromSize1AndColorNAVY = htmlDoc.DocumentNode.SelectSingleNode("//*[@size='1' and @color='NAVY']");
            if (!string.IsNullOrWhiteSpace(dateFromSize1AndColorNAVY?.InnerText) && dateFromSize1AndColorNAVY.InnerText.Contains("Posted"))
            {
                string cleanedDateFromSizeAndColor = dateFromSize1AndColorNAVY.InnerText;
                return HtmlParsingHelper.ConvertStringToDate(cleanedDateFromSizeAndColor);
            };

            HtmlNode? dateFromSize1AndColor000080 = htmlDoc.DocumentNode.SelectSingleNode("//*[@size='1' and @color='#000080']");
            if (!string.IsNullOrWhiteSpace(dateFromSize1AndColor000080?.InnerText) && dateFromSize1AndColor000080.InnerText.Contains("Posted"))
            {
                string cleanedDateFromSizeAndColor = dateFromSize1AndColor000080.InnerText;
                return HtmlParsingHelper.ConvertStringToDate(cleanedDateFromSizeAndColor);
            };

            HtmlNode? dateFromPostedOnly = htmlDoc.DocumentNode.SelectSingleNode("//*[contains(text(), 'Posted')]");
            if (!string.IsNullOrWhiteSpace(dateFromPostedOnly?.InnerText))
            {
                string cleanedDateFromPostedText = dateFromPostedOnly.InnerText;
                return HtmlParsingHelper.ConvertStringToDate(cleanedDateFromPostedText);
            };

            return null; 
        }

        private string? GetBevDate(HtmlDocument htmlDoc)
        {
            HtmlNode? dateFromBEV = null;
            if (htmlDoc.DocumentNode.Descendants().FirstOrDefault(node => node.Id == "topicHeader" || node.Element("h3") != null) != null)
            {
                dateFromBEV = htmlDoc.DocumentNode.Descendants().FirstOrDefault(node => node.Id == "topicHeader" || node.Element("h3") != null);
            }
            else if (htmlDoc.DocumentNode.SelectSingleNode("//*[@size='2' and @color='#800000' or @size='2' and @color='maroon']") != null)
            {
                // add logic to find first or default date at top (descendants, attrib same, text contains posted). 
                dateFromBEV = htmlDoc.DocumentNode.SelectSingleNode("//*[@size='2' and @color='#800000' or @size='2' and @color='maroon']");
            }
            if (!string.IsNullOrWhiteSpace(dateFromBEV?.InnerText))
            {
                string cleanedDateFromBEV = dateFromBEV.InnerText.Replace("NEWS:", "").Replace("News:", "").Replace("news:", "");
                return HtmlParsingHelper.ConvertStringToDate(cleanedDateFromBEV.Trim());
            }
            return null;
        }

        public string? GetThumbnailUrl(string splitHtmlBody, string url)
        {
            HtmlDocument splitBodyNode = HtmlParsingHelper.LoadHtmlDocument(splitHtmlBody);
            HtmlNode? firstImageNode = splitBodyNode.DocumentNode.SelectSingleNode("(//img)[1]");
            if (firstImageNode == null) return null;
            
            if (firstImageNode.GetAttributeValue("src", string.Empty).MatchesAnyOf(ScrapingHelper.skipFirstThumbnailImage))
            {
                firstImageNode = splitBodyNode.DocumentNode.SelectSingleNode("(//img)[2]");
            }

            string src = firstImageNode.GetAttributeValue("src", string.Empty);
            string srcWithDomain = HtmlParsingHelper.CleanLink(src, url, true);
            return srcWithDomain;
        }
    }
}
