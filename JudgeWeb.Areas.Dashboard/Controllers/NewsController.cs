using JudgeWeb.Areas.Dashboard.Models;
using JudgeWeb.Data;
using JudgeWeb.Features;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Dashboard.Controllers
{
    [Area("Dashboard")]
    [Authorize(Roles = "Administrator")]
    [Route("[area]/[controller]")]
    public class NewsController : Controller3
    {
        private Task AuditlogAsync(int nid, string act)
        {
            DbContext.Auditlogs.Add(new Auditlog
            {
                Action = act,
                DataId = $"{nid}",
                DataType = AuditlogType.News,
                Time = DateTimeOffset.Now,
                UserName = UserManager.GetUserName(User),
            });

            return DbContext.SaveChangesAsync();
        }


        [HttpGet]
        public async Task<IActionResult> List()
        {
            var news = await DbContext.News
                .Select(n => new News
                {
                    NewsId = n.NewsId,
                    Title = n.Title,
                    LastUpdate = n.LastUpdate,
                    Active = n.Active,
                })
                .ToListAsync();

            return View(news);
        }


        [HttpGet("{nid}/[action]")]
        [ValidateInAjax]
        public async Task<IActionResult> Delete(int nid)
        {
            var desc = await DbContext.News
                .Where(e => e.NewsId == nid)
                .Select(e => e.Title)
                .FirstOrDefaultAsync();
            if (desc == null) return NotFound();

            return AskPost(
                title: $"Delete news {nid} - \"{desc}\"",
                message: $"You're about to delete news {nid} - \"{desc}\".\n" +
                    "Are you sure?",
                area: "Dashboard", ctrl: "News", act: "Delete",
                type: MessageType.Danger);
        }


        [HttpPost("{nid}/[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int nid, int inajax)
        {
            var news = await DbContext.News
                .Where(e => e.NewsId == nid)
                .FirstOrDefaultAsync();
            if (news == null) return NotFound();

            DbContext.News.Remove(news);

            try
            {
                await DbContext.SaveChangesAsync();
                StatusMessage = $"News {nid} deleted successfully.";
                await AuditlogAsync(nid, "deleted");
            }
            catch (DbUpdateException)
            {
                StatusMessage = $"Error deleting news {nid}.";
            }

            return RedirectToAction(nameof(List));
        }


        [HttpGet("{nid}/[action]")]
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


        [HttpPost("{nid}/[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int nid, NewsEditModel model,
            [FromServices] IMarkdownService markdownService)
        {
            if (model.NewsId != nid) return BadRequest();

            var news = await DbContext.News
                .Where(n => n.NewsId == nid)
                .FirstOrDefaultAsync();
            if (news is null) return NotFound();

            markdownService.Render(model.MarkdownSource, out var html, out var tree);
            news.Source = Encoding.UTF8.GetBytes(model.MarkdownSource);
            news.Title = model.Title;
            news.Active = model.Active;
            news.Content = Encoding.UTF8.GetBytes(html);
            news.Tree = Encoding.UTF8.GetBytes(tree);
            news.LastUpdate = DateTimeOffset.Now;

            await DbContext.SaveChangesAsync();
            StatusMessage = "News updated successfully.";
            await AuditlogAsync(nid, "updated");
            return RedirectToAction(nameof(Edit), new { nid });
        }


        [HttpGet("[action]")]
        public IActionResult Add()
        {
            return View("Edit", new NewsEditModel
            {
                MarkdownSource = "",
                NewsId = 0,
                Title = "标题",
                Active = false,
            });
        }


        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(
            NewsEditModel model,
            [FromServices] IMarkdownService markdownService)
        {
            markdownService.Render(model.MarkdownSource, out var html, out var tree);

            var news = DbContext.News.Add(new News
            {
                Source = Encoding.UTF8.GetBytes(model.MarkdownSource),
                Title = model.Title,
                Active = model.Active,
                LastUpdate = DateTimeOffset.Now,
                Content = Encoding.UTF8.GetBytes(html),
                Tree = Encoding.UTF8.GetBytes(tree),
            });

            await DbContext.SaveChangesAsync();
            StatusMessage = "News created successfully.";
            await AuditlogAsync(news.Entity.NewsId, "added");
            return RedirectToAction("Edit", new { nid = news.Entity.NewsId });
        }
    }
}
