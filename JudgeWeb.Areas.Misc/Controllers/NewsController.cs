using JudgeWeb.Areas.Misc.Models;
using JudgeWeb.Data;
using JudgeWeb.Features;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Misc.Controllers
{
    [Area("Misc")]
    [Authorize(Roles = privilege)]
    [Route("[controller]/[action]")]
    public class NewsController : Controller
    {
        const string privilege = "Administrator,Guide";

        private AppDbContext DbContext { get; }

        private IMarkdownService MarkdownService { get; }

        public NewsController(AppDbContext adbc, IMarkdownService ms)
        {
            DbContext = adbc;
            MarkdownService = ms;
        }

        [HttpPost("{nid}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int nid, NewsEditModel model)
        {
            if (model.NewsId != nid) return BadRequest();

            var news = await DbContext.News
                .Where(n => n.NewsId == nid)
                .FirstOrDefaultAsync();
            if (news is null) return NotFound();

            MarkdownService.Render(model.MarkdownSource, out var html, out var tree);
            news.Source = Encoding.UTF8.GetBytes(model.MarkdownSource);
            news.Title = model.Title;
            news.Active = model.Active;
            news.Content = Encoding.UTF8.GetBytes(html);
            news.Tree = Encoding.UTF8.GetBytes(tree);
            news.LastUpdate = DateTime.Now;

            await DbContext.SaveChangesAsync();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(NewsEditModel model)
        {
            MarkdownService.Render(model.MarkdownSource, out var html, out var tree);

            var news = DbContext.News.Add(new News
            {
                Source = Encoding.UTF8.GetBytes(model.MarkdownSource),
                Title = model.Title,
                Active = model.Active,
                LastUpdate = DateTime.Now,
                Content = Encoding.UTF8.GetBytes(html),
                Tree = Encoding.UTF8.GetBytes(tree),
            });

            await DbContext.SaveChangesAsync();
            return RedirectToAction("Edit", new { nid = news.Entity.NewsId });
        }

        [HttpGet("{nid}")]
        public async Task<IActionResult> Edit(int nid)
        {
            var news = await DbContext.News
                .Where(n => n.NewsId == nid)
                .Select(n => new { n.NewsId, n.Source, n.Active, n.Title })
                .FirstOrDefaultAsync();
            if (news is null) return NotFound();

            return View(new NewsEditModel
            {
                MarkdownSource = Encoding.UTF8.GetString(news.Source),
                NewsId = news.NewsId,
                Active = news.Active,
                Title = news.Title,
            });
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View("Edit", new NewsEditModel
            {
                MarkdownSource = "",
                NewsId = 0,
                Title = "标题",
                Active = false,
            });
        }

        [HttpGet("{nid}")]
        [AllowAnonymous]
        public async Task<IActionResult> View(int nid)
        {
            var news = await DbContext.News
                .Where(n => n.NewsId == nid)
                .Select(n => new { n.Active, n.Content, n.LastUpdate, n.Title, n.Tree })
                .FirstOrDefaultAsync();

            var newsList = await DbContext.News
                .Where(n => n.Active)
                .Select(n => new { n.NewsId, n.Title, n.LastUpdate })
                .OrderByDescending(n => n.NewsId)
                .Take(100)
                .ToListAsync();

            if (news is null || !news.Active && !User.IsInRoles(privilege))
            {
                return View(new NewsViewModel
                {
                    NewsList = newsList.Select(n => (n.NewsId, n.Title)),
                    NewsId = -1,
                    Title = "404 Not Found",
                    HtmlContent = "Sorry, the requested content is not found.",
                    LastUpdate = DateTime.Now,
                    Tree = "",
                });
            }
            else
            {
                return View(new NewsViewModel
                {
                    NewsList = newsList.Select(n => (n.NewsId, n.Title)),
                    NewsId = nid,
                    Title = news.Title,
                    HtmlContent = Encoding.UTF8.GetString(news.Content),
                    LastUpdate = news.LastUpdate,
                    Tree = Encoding.UTF8.GetString(news.Tree),
                });
            }
        }
    }
}
