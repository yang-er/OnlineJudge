using JudgeWeb.Areas.Dashboard.Models;
using JudgeWeb.Data;
using JudgeWeb.Domains.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Dashboard.Controllers
{
    [Area("Dashboard")]
    [Authorize(Roles = "Administrator,Teacher")]
    [Route("[area]/[controller]")]
    public class GroupsController : Controller3
    {
        private const int ItemPerPage = 50;
        private IStudentStore Store { get; }
        public GroupsController(IStudentStore store) => Store = store;


        [HttpGet]
        public async Task<IActionResult> List()
        {
            return View(await Store.ListClassAsync());
        }


        [HttpGet("{gid}")]
        public async Task<IActionResult> Detail(int gid, int page = 1)
        {
            var model = await Store.FindClassAsync(gid);
            if (model == null) return NotFound();

            if (page < 1) return BadRequest();
            var (stus, totPage) = await Store.ListStudentsAsync(page, ItemPerPage);
            ViewBag.Page = page;
            ViewBag.TotalPage = totPage;
            ViewBag.Students = stus;
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
            var model = await Store.FindClassAsync(gid);
            if (model == null) return NotFound();
            return Window(new AddStudentsBatchModel());
        }


        [HttpPost("{gid}/[action]")]
        [ValidateInAjax]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int gid, AddStudentsBatchModel model)
        {
            var classes = await Store.FindClassAsync(gid);
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

            var intersects = await Store.CheckStudentIdAsync(opts);

            foreach (var stuId in opts.Except(intersects))
            {
                sb.Append($"{stuId}\n");
                ModelState.AddModelError("xys::parseError", $"Student {stuId} not found.");
            }

            await Store.MergeClassStudentAsync(gid, intersects);

            if (ModelState.IsValid)
            {
                StatusMessage = $"{intersects.Length} students has been added.";
                return RedirectToAction(nameof(Detail));
            }
            else
            {
                ModelState.SetModelValue("Students", sb.ToString(), sb.ToString());
                model.Students = sb.ToString();
                ModelState.AddModelError("xys::other", $"Other {intersects.Length} students has been added.");
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
            await Store.CreateAsync(model);
            return RedirectToAction(nameof(Detail), new { gid = model.Id });
        }


        [HttpGet("{gid}/[action]")]
        [ValidateInAjax]
        public async Task<IActionResult> Delete(int gid)
        {
            var model = await Store.FindClassAsync(gid);
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
            var group = await Store.FindClassAsync(gid);
            if (group == null) return NotFound();

            await Store.DeleteAsync(group);
            return RedirectToAction(nameof(List));
        }


        [HttpGet("{gid}/[action]/{stuid}")]
        [ValidateInAjax]
        public async Task<IActionResult> Kick(int gid, int stuid)
        {
            var model = await Store.FindClassAsync(gid);
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
            if (await Store.ClassKickAsync(gid, stuid))
                StatusMessage = $"Kicked student {stuid} from group g{gid}";
            return RedirectToAction(nameof(Detail));
        }
    }
}
