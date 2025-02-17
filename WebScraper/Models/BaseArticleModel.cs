using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebScraper.Models
{
    public class BaseArticleModel
    {
        public string Title { get; set; }
        public string Url { get; set; }
        public string Category { get; set; }
        public string? Description { get; set; }
        public string? ThumbnailURL { get; set; }
    }
}
