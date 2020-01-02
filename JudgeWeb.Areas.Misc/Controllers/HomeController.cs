using JudgeWeb.Areas.Misc.Models;
using JudgeWeb.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Misc.Controllers
{
    [Area("Misc")]
    [Route("[action]")]
    public class HomeController : Controller
    {
        private AppDbContext DbContext { get; }

        private static string[] PhotoList { get; } = new[] { "2018qingdao", "2018xian", "2018final" };

        public HomeController(AppDbContext adbc)
        {
            DbContext = adbc;
        }

        [Route("/")]
        public async Task<IActionResult> Index()
        {
            var news = await DbContext.News
                .Where(n => n.Active)
                .OrderByDescending(n => n.NewsId)
                .Select(n => new { n.Title, n.NewsId })
                .Take(10)
                .ToListAsync();

            ViewData["Photo"] = PhotoList[DateTimeOffset.Now.Millisecond % PhotoList.Length];
            return View(news.Select(a => (a.NewsId, a.Title)));
        }

        public IActionResult About()
        {
            return View();
        }

        [HttpGet("{nid}")]
        public async Task<IActionResult> News(int nid)
        {
            var news = await DbContext.News
                .Where(n => n.NewsId == nid)
                .FirstOrDefaultAsync();

            var list = await DbContext.News
                .Where(n => n.Active)
                .Select(n => new { n.NewsId, n.Title })
                .OrderByDescending(n => n.NewsId)
                .Take(100)
                .ToListAsync();

            var newsList = list.Select(a => (a.NewsId, a.Title));

            if (news is null || !news.Active && !User.IsInRoles("Administrator"))
            {
                return View(new NewsViewModel
                {
                    NewsList = newsList,
                    NewsId = -1,
                    Title = "404 Not Found",
                    HtmlContent = "Sorry, the requested content is not found.",
                    LastUpdate = DateTimeOffset.Now,
                    Tree = "",
                });
            }
            else
            {
                return View(new NewsViewModel
                {
                    NewsList = newsList,
                    NewsId = nid,
                    Title = news.Title,
                    HtmlContent = Encoding.UTF8.GetString(news.Content),
                    LastUpdate = news.LastUpdate,
                    Tree = Encoding.UTF8.GetString(news.Tree),
                });
            }
        }

        [Route("/error")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            ControllerContext.ActionDescriptor.ControllerName = "";
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
