using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebScraper.Models
{
    public class ArticleModel : BaseArticleModel
    {
        public string? SubCategory { get; set; }
        public string? Series {  get; set; }
        public string? SeriesNumber { get; set; }
        public List<string?> Author { get; set; }
        [Required]
        public string BodyHtml { get; set; }
        [Required]
        public string BodyInnerText { get; set; }
        public string? Date { get; set; }
        public List<BaseArticleModel>? RelatedArticles { get; set; }

    }
}
