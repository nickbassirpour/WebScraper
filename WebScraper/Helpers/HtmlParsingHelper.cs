using HtmlAgilityPack;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace WebScraper.Helpers
{
    internal static class HtmlParsingHelper
    {
        internal static IEnumerable<HtmlNode> GetLinkElements(string bottomOfArticle)
        {
            HtmlDocument htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(bottomOfArticle);
            IEnumerable<HtmlNode> linkElements = htmlDocument.DocumentNode.SelectNodes("//a");
            return linkElements;
        }
        internal static string? SplitHtmlBody(HtmlDocument htmlDoc, string url)
        {
            if (htmlDoc.DocumentNode.InnerHtml == null)
            {
                Log.Information("Unable to split HTML Body");
                return null;
            }

            string htmlBodyNode = htmlDoc.DocumentNode.InnerHtml;
            List<string> splitHtmlBody = htmlBodyNode.Split("alt=\"contact\">").ToList();
            if (splitHtmlBody.Count > 1)
            {
                List<string> cleanedHtmlBodyListFromComment = Regex.Split(splitHtmlBody[1], @"<!-- AddToAny BEGIN -->", RegexOptions.Singleline).ToList();
                if (cleanedHtmlBodyListFromComment.Count > 1)
                {
                    return cleanedHtmlBodyListFromComment[0];
                }

                List<string> cleandHtmlBodyListFromSocialLinks = Regex.Split(splitHtmlBody[1], @"<div class=""a2a_kit a2a_kit_size_32 a2a_default_style""", RegexOptions.Singleline).ToList();
                if (cleandHtmlBodyListFromSocialLinks.Count > 1)
                {
                    return cleandHtmlBodyListFromSocialLinks[0];
                }
            }

            List<string> splitHtmlBodyOnSrc = htmlBodyNode.Split("src=\"images/A_contact.gif").ToList();
            if (splitHtmlBodyOnSrc.Count > 1)
            {
                List<string> cleanedHtmlBodyList = Regex.Split(splitHtmlBodyOnSrc[1], @"<!-- AddToAny BEGIN -->", RegexOptions.Singleline).ToList();
                if (cleanedHtmlBodyList.Count > 1)
                {
                    return cleanedHtmlBodyList[0];
                }
            }


            Log.Information("Unable to split HTML Body");
            return null;
        }

        internal static HtmlNode ParseBody(HtmlNode node, string url)
        {
            FixLinks(node, url);
            FixImageUrls(node, url);
            return node;
        }

        internal static HtmlDocument LoadHtmlDocument(string html)
        {
            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(html);
            return document;
        }

        internal static void FixLinks(HtmlNode htmlBody, string url)
        {
            HtmlNodeCollection linkNodes = htmlBody.SelectNodes("//a[@href]");
        }
        internal static void FixImageUrls(HtmlNode htmlBody, string url)
        {
            HtmlNodeCollection imageNodes = htmlBody.SelectNodes("//img[@src]");
            if (imageNodes == null)
            {
                return;
            }
            foreach (HtmlNode imageNode in imageNodes)
            {
                string imageSrc = imageNode.GetAttributeValue("src", null);
                CleanLink(imageSrc, url, true);
            }
        }

        internal static string CleanLink(string link, string mainUrl, bool useTIADomain)
        {
            try
            {
                string domain = useTIADomain ? "https://traditioninaction.org" : "";

                if (link.Contains("../"))
                {
                    link = domain + "/" + link.Replace("../", "");
                }
                else
                {
                    string category = GetCategoryFromURL(mainUrl);
                    link = domain + "/" + category + "/" + link;
                }
                return link;
            }
            catch 
            {
                Log.Information($"Unable to clean link for {link}");
                return null;
            }
        }

        internal static string? GetCategoryFromURL(string url)
        {
            string category = url.Split("/")[3];
            if (string.IsNullOrEmpty(category))
            {
                Log.Information($"Unable to get category from link: {url}");
                return null;
            }
            return category;
        }

        internal static string? ConvertStringToDate(string date)
        {
            string dateWithoutPosted = cleanDateValue(date);
            if (DateTime.TryParse(dateWithoutPosted.Trim(), out DateTime parsedDate))
            {
                string formattedDate = parsedDate.ToString("yyyy-MM-dd");
                return formattedDate;
            }
            else
            {
                return null;
            }
        }

        private static string cleanDateValue(string date)
        {
            string dateWithoutPosted = RemovePosted(date);

            dateWithoutPosted = dateWithoutPosted.Replace("&nbsp;", "").Replace("--", "01");

            string cleanedDate = RemoveOrdinalSuffixes(dateWithoutPosted.Trim());

            return cleanedDate;
        }

        private static string RemovePosted(string date)
        {
            if (date.Contains("Posted on"))
            {
                date = date.Split("Posted on")[1].Trim().Replace("--", "01");
            }
            else if (date.Contains("Posted"))
            {
                date = date.Split("Posted")[1].Trim().Replace("--", "01");
            }
            else
            {
                date = date.Trim().Replace("--", "01");
            }
            return date;
        }

        private static string RemoveOrdinalSuffixes(string dateWithoutPosted)
        {
            return System.Text.RegularExpressions.Regex.Replace(dateWithoutPosted, @"\b(\d+)(st|nd|rd|th)\b", "$1");
        }
    }
}
