using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace WebScraper.Helpers
{
    internal static class ScrapingHelper
    {
        internal static bool MatchesAnyOf(this string value, params string[] targets)
        {
            return targets.Any(target => target.Equals(value, StringComparison.OrdinalIgnoreCase));
        }

        internal static bool ContainsAnyOf(this string value, params string[] targets)
        {
            return targets.Any(target => target.Contains(value, StringComparison.OrdinalIgnoreCase));
        }

        internal static bool IsSeries(this HtmlNode linkElement)
        {
            if (linkElement.isMainTDElement()) return false;
            HtmlNodeCollection aTags = linkElement.SelectNodes(".//a");
            if (aTags == null || aTags.Count == 0) return false;
            if (aTags.Count > 1 && linkElement.InnerText.Contains("Part 1")) return true;
            return false;
        }

        internal static bool IsNullOrBadLink(this HtmlNode linkElement)
        {
            if (String.IsNullOrWhiteSpace(linkElement.InnerText)) return true;
            if (linkElement.InnerText.MatchesAnyOf(ScrapingHelper.linkTextsNotToScrape.ToArray())) return true;
            HtmlNodeCollection aTags = linkElement.SelectNodes(".//a");
            if (aTags == null || aTags.Count == 0) return true;
            if (aTags.Count > 1 && aTags.Any(a => a.GetAttributeValue("href", null) != null)) return false;
            if (anyBadLinks(aTags)) return true;
            return false;
        }

        internal static bool anyBadLinks(this HtmlNodeCollection aTags)
        {
            if (aTags.Any(a => String.IsNullOrWhiteSpace(a.InnerText))) return true;
            if (aTags.Any(a => a.GetAttributeValue("href", null).MatchesAnyOf(ScrapingHelper.linksNotToScrape.ToArray()))) return true;
            if (!aTags.Any(a => a.GetAttributeValue("href", null).Contains("."))) return true;
            return false;
        }
        internal static bool isBadLink(this string url)
        {
            if (String.IsNullOrWhiteSpace(url)) return true;
            if (url.MatchesAnyOf(ScrapingHelper.linksNotToScrape.ToArray())) return true;
            if (!url.Contains(".")) return true;
            return false;
        }

        internal static bool isBadAnchorTag(this HtmlNode anchorNode)
        {
            if (String.IsNullOrWhiteSpace(anchorNode.GetAttributeValue("href", ""))) return true;
            if (anchorNode.GetAttributeValue("href", "").isBadLink()) return true;
            if (String.IsNullOrWhiteSpace(anchorNode.InnerText)) return true;
            return false;
        }

        internal static bool isMainTDElement(this HtmlNode tdElement)
        {
            if (tdElement.InnerText.Contains("All Rights Reserved")) return true;
            return false;
        }

        internal static bool containsTDNestedInTD(this HtmlNode tdElement)
        {
            if (tdElement.SelectSingleNode(".//td") != null) return true;
            return false;
        }

        internal static string[] linkTextsNotToScrape = new string[]
        {
            "home",
            "books",
            "cds",
            "search",
            "contact us",
            "donate",
            "forgotten truths",
            "religious",
            "news",
            "archives",
            "hot topics",
            "consequences"
        };

        internal static string[] linksNotToScrape = new string[]
        {
            "n000rpForgottenTruths.htm#forgotten"
        };

        internal static string[] linksWithNoDescription = new string[]
        {
            "our lady of good success",
            "the saint of the day",
            "what people are asking",
        };

        internal static string[] categoriesWithNoSubcatregory = new string[]
        {
            "bev",
            "revolutionphotos",
            "progressivistdoc",
            "polemics",
            "bkreviews",
            "movies",
            "bestof",
            "olgs",
            "sod",
        };

        internal static string[] skipFirstThumbnailImage = new string[]
        {
            "bevimages/00_Peregrine.jpg"
        };
    }
}
 