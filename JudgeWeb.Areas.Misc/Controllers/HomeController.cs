using JudgeWeb.Areas.Misc.Models;
using JudgeWeb.Domains.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Misc.Controllers
{
    [Area("Misc")]
    [Route("[action]")]
    public class HomeController : Controller2
    {
        public static string ProgramVersion { get; } =
            typeof(GitVersionAttribute).Assembly
                .GetCustomAttribute<GitVersionAttribute>()?
                .CommitId?.Substring(0, 7) ?? "unknown";

        private INewsStore Store { get; }

        private static string[] PhotoList { get; } = new[] { "2018qingdao", "2018xian", "2018final" };

        public HomeController(INewsStore adbc) => Store = adbc;


        [Route("/")]
        public async Task<IActionResult> Index()
        {
            ViewData["Photo"] = PhotoList[DateTimeOffset.Now.Millisecond % PhotoList.Length];
            return View(await Store.ListActiveAsync(10));
        }


        public IActionResult About()
        {
            return View();
        }


        [HttpGet("{nid}")]
        public async Task<IActionResult> News(int nid)
        {
            var news = await Store.FindAsync(nid);
            var newsList = await Store.ListActiveAsync(100);

            if (news is null || !news.Active && !User.IsInRoles("Administrator"))
            {
                Response.StatusCode = 404;

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


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult NotFound2() => StatusCodePage(404);
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => StatusCodePage();
    }
}
