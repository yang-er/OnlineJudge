using EFCore.BulkExtensions;
using JudgeWeb.Areas.Dashboard.Models;
using JudgeWeb.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Dashboard.Controllers
{
    [Area("Dashboard")]
    [Authorize(Roles = "Administrator")]
    [Route("[area]/[controller]")]
    public class GroupsController : Controller3
    {
        private const int ItemPerPage = 50;


        [HttpGet]
        public async Task<IActionResult> List(int page)
        {
            if (page < 1) page = 1;
            int total = await DbContext.Classes.CountAsync();
            int totPage = (total - 1) / ItemPerPage + 1;
            if (page > totPage) page = totPage;
            ViewBag.Page = page;
            ViewBag.TotalPage = totPage;

            var model = await DbContext.Classes
                .OrderBy(c => c.Id)
                .Skip(ItemPerPage * (page - 1))
                .Take(ItemPerPage)
                .ToListAsync();

            int start = model.FirstOrDefault()?.Id ?? 0;
            int end = model.LastOrDefault()?.Id ?? 0;
            var counting = await DbContext.ClassStudent
                .Where(cs => cs.ClassId >= start && cs.ClassId <= end)
                .GroupBy(cs => cs.ClassId)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(k => k.Key, v => v.Count);
            model.ForEach(t => t.Count = counting.GetValueOrDefault(t.Id));

            return View(model);
        }


        [HttpGet("{gid}")]
        public async Task<IActionResult> Detail(int gid, int page = 1)
        {
            var model = await DbContext.Classes
                .Where(tc => tc.Id == gid)
                .FirstOrDefaultAsync();
            if (model == null) return NotFound();

            if (page < 1) page = 1;
            int total = await DbContext.ClassStudent
                .Where(c => c.ClassId == gid)
                .CountAsync();
            int totPage = (total - 1) / ItemPerPage + 1;
            if (page > totPage) page = totPage;
            ViewBag.Page = page;
            ViewBag.TotalPage = totPage;

            var stuQuery =
                from gs in DbContext.ClassStudent
                where gs.ClassId == gid
                join s in DbContext.Students on gs.StudentId equals s.Id
                join u in DbContext.Users on s.Id equals u.StudentId
                into uu from u in uu.DefaultIfEmpty()
                orderby s.Id ascending
                select new Student
                {
                    Id = s.Id,
                    IsVerified = u.StudentVerified,
                    Name = s.Name,
                    Email = u.StudentEmail,
                    UserId = u.Id,
                    UserName = u.UserName,
                };

            ViewBag.Students = await stuQuery
                .Skip(ItemPerPage * (page - 1))
                .Take(ItemPerPage)
                .ToListAsync();
            return View(model);
        }


        [HttpGet("[action]")]
        [ValidateInAjax]
        public IActionResult Create()
        {
            return Window(new TeachingClass());
        }


        [HttpGet("{gid}/[action]")]
        [ValidateInAjax]
        public async Task<IActionResult> Add(int gid)
        {
            var model = await DbContext.Classes
                .Where(tc => tc.Id == gid)
                .FirstOrDefaultAsync();
            if (model == null) return NotFound();
            return Window(new AddStudentsBatchModel());
        }


        [HttpPost("{gid}/[action]")]
        [ValidateInAjax]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int gid, AddStudentsBatchModel model)
        {
            var classes = await DbContext.Classes
                .Where(tc => tc.Id == gid)
                .FirstOrDefaultAsync();
            if (classes == null) return NotFound();

            var stus = model.Students.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var sb = new StringBuilder();
            var opts = new List<int>();

            foreach (var item in stus)
            {
                if (!int.TryParse(item.Trim(), out int stuid))
                {
                    ModelState.AddModelError("xys::parseError", $"Unrecognized number token {item}.");
                    sb.Append(item.Trim()).Append('\n');
                }
                else
                {
                    opts.Add(stuid);
                }
            }

            var intersects = await DbContext.Students
                .Where(s => opts.Contains(s.Id))
                .Select(s => s.Id)
                .ToListAsync();

            foreach (var stuId in opts.Except(intersects))
            {
                sb.Append($"{stuId}\n");
                ModelState.AddModelError("xys::parseError", $"Student {stuId} not found.");
            }

            await DbContext.BulkInsertOrUpdateAsync(
                intersects.Select(a => new ClassStudent { ClassId = gid, StudentId = a }).ToList());

            if (ModelState.IsValid)
            {
                StatusMessage = $"{intersects.Count} students has been added.";
                return RedirectToAction(nameof(Detail));
            }
            else
            {
                ModelState.SetModelValue("Students", sb.ToString(), sb.ToString());
                model.Students = sb.ToString();
                ModelState.AddModelError("xys::other", $"Other {intersects.Count} students has been added.");
                return Window(model);
            }
        }


        [HttpPost("[action]")]
        [ValidateInAjax]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TeachingClass model)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
                ModelState.AddModelError("model", "Class Name should not be empty.");
            if (!ModelState.IsValid) return Window(model);

            model.Id = 0;
            DbContext.Classes.Add(model);
            await DbContext.SaveChangesAsync();
            return RedirectToAction(nameof(Detail), new { gid = model.Id });
        }


        [HttpGet("{gid}/[action]")]
        [ValidateInAjax]
        public async Task<IActionResult> Delete(int gid)
        {
            var model = await DbContext.Classes
                .Where(s => s.Id == gid)
                .FirstOrDefaultAsync();
            if (model == null) return NotFound();

            return AskPost(
                title: $"Delete group {gid}",
                message: $"Are you sure to delete group {model.Name}?",
                area: "Dashboard", ctrl: "Groups", act: "Delete",
                type: MessageType.Danger);
        }


        [HttpPost("{gid}/[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int gid, bool post = true)
        {
            var group = await DbContext.Classes
                .Where(s => s.Id == gid)
                .FirstOrDefaultAsync();
            if (group == null) return NotFound();

            DbContext.Classes.Remove(group);
            await DbContext.SaveChangesAsync();
            return RedirectToAction(nameof(List));
        }


        [HttpGet("{gid}/[action]/{stuid}")]
        [ValidateInAjax]
        public async Task<IActionResult> Kick(int gid, int stuid)
        {
            var model = await DbContext.Classes
                .Where(s => s.Id == gid)
                .FirstOrDefaultAsync();
            if (model == null) return NotFound();

            return AskPost(
                title: $"Kick student from group",
                message: $"Are you sure to kick student {stuid} from group {model.Name}?",
                area: "Dashboard", ctrl: "Groups", act: "Kick",
                type: MessageType.Warning);
        }


        [HttpPost("{gid}/[action]/{stuid}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Kick(int gid, int stuid, bool post = true)
        {
            int count = await DbContext.ClassStudent
                .Where(cs => cs.ClassId == gid && cs.StudentId == stuid)
                .BatchDeleteAsync();
            if (count > 0)
                StatusMessage = $"Kicked student {stuid} from group g{gid}";
            return RedirectToAction(nameof(Detail));
        }
    }
}
