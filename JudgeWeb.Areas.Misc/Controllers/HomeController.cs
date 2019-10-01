using EntityFrameworkCore.Cacheable;
using JudgeWeb.Areas.Misc.Models;
using JudgeWeb.Data;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Diagnostics;
using System.Linq;

namespace JudgeWeb.Areas.Misc.Controllers
{
    [Area("Misc")]
    [Route("[controller]/[action]")]
    public class HomeController : Controller
    {
        private AppDbContext DbContext { get; }

        private static string[] PhotoList { get; } = new[] { "2018qingdao", "2018xian" };

        public HomeController(AppDbContext adbc)
        {
            DbContext = adbc;
        }

        [Route("/")]
        public IActionResult Index()
        {
            var news = DbContext.News
                .Where(n => n.Active)
                .OrderByDescending(n => n.NewsId)
                .Select(n => new { n.Title, n.NewsId })
                .Take(10)
                .Cacheable(TimeSpan.FromMinutes(50))
                .ToList();

            ViewData["Photo"] = PhotoList[DateTimeOffset.Now.Millisecond % PhotoList.Length];
            return View(news.Select(a => (a.NewsId, a.Title)));
        }

        public IActionResult About()
        {
            return View();
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
