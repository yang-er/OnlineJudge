using System;
using System.Collections.Generic;

namespace JudgeWeb.Areas.Misc.Models
{
    public class NewsViewModel
    {
        public IEnumerable<(int NewsId, string Title)> NewsList { get; set; }

        public int NewsId { get; set; }

        public string Title { get; set; }

        public string Tree { get; set; }

        public DateTime LastUpdate { get; set; }

        public string HtmlContent { get; set; }
    }
}
