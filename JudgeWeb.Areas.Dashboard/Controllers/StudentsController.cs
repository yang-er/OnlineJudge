using JudgeWeb.Areas.Dashboard.Models;
using JudgeWeb.Data;
using JudgeWeb.Domains.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Dashboard.Controllers
{
    [Area("Dashboard")]
    [Authorize(Roles = "Administrator,Teacher")]
    [Route("[area]/[controller]")]
    [AuditPoint(AuditlogType.User)]
    public class StudentsController : Controller3
    {
        private UserManager UserManager { get; }
        public StudentsController(UserManager um) => UserManager = um;
        const int ItemPerPage = 50;


        [HttpGet]
        public async Task<IActionResult> List(int page = 1)
        {
            if (page < 1) page = 1;
            var (model, totPage) = await UserManager.ListStudentsAsync(page, ItemPerPage);
            ViewBag.Page = page;
            ViewBag.TotalPage = totPage;
            return View(model);
        }


        [HttpGet("{stuid}/[action]/{uid}")]
        [ValidateInAjax]
        public async Task<IActionResult> Unlink(int page, int stuid, int uid)
        {
            var users = await UserManager.FindByStudentIdAsync(stuid);
            var user = users.SingleOrDefault(u => u.Id == uid);
            if (user == null) return NotFound();

            return AskPost(
                title: $"Unlink student {stuid}",
                message: $"Are you sure to unlink student {stuid} with {user.UserName} (u{user.Id})?",
                area: "Dashboard", ctrl: "Students", act: "Unlink",
                routeValues: new { page },
                type: MessageType.Warning);
        }


        [HttpPost("{stuid}/[action]/{uid}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unlink(int page, int stuid, int uid, bool post = true)
        {
            var users = await UserManager.FindByStudentIdAsync(stuid);
            var user = users.SingleOrDefault(u => u.Id == uid);
            if (user == null) return NotFound();

            user.StudentEmail = null;
            user.StudentId = null;
            user.StudentVerified = false;
            await UserManager.UpdateAsync(user);
            await UserManager.RemoveFromRoleAsync(user, "Student");
            StatusMessage = $"Student ID {stuid} has been unlinked with u{user.Id}.";
            await HttpContext.AuditAsync("unlinked", $"u{user.Id}", user == null ? null : $"student s{stuid}");
            return RedirectToAction(nameof(List), new { page });
        }


        [HttpGet("{stuid}/[action]")]
        [ValidateInAjax]
        public async Task<IActionResult> Delete(int page, int stuid)
        {
            var stuId = await UserManager.FindStudentAsync(stuid);
            if (stuId == null) return NotFound();

            return AskPost(
                title: $"Delete student {stuid}",
                message: $"Are you sure to delete student {stuid}?",
                area: "Dashboard", ctrl: "Students", act: "Delete",
                routeValues: new { page },
                type: MessageType.Warning);
        }


        [HttpPost("{stuid}/[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int page, int stuid, bool post = true)
        {
            var stuId = await UserManager.FindStudentAsync(stuid);
            if (stuId == null) return NotFound();

            var users = await UserManager.FindByStudentIdAsync(stuid);
            string unlinked = "";

            foreach (var user in users)
            {
                unlinked += $" u{user.Id}";
                user.StudentEmail = null;
                user.StudentId = null;
                user.StudentVerified = false;
                await UserManager.UpdateAsync(user);
                await UserManager.RemoveFromRoleAsync(user, "Student");
            }

            await UserManager.DeleteStudentAsync(stuId);
            await HttpContext.AuditAsync("deleted", $"student s{stuid}", string.IsNullOrEmpty(unlinked) ? null : $"unlink{unlinked}");
            StatusMessage = $"Student ID {stuid} has been removed.";
            return RedirectToAction(nameof(List), new { page });
        }


        [HttpGet("{stuid}/[action]/{uid}")]
        [ValidateInAjax]
        public async Task<IActionResult> MarkVerified(int page, int stuid, int uid)
        {
            var stuId = await UserManager.FindStudentAsync(stuid);
            if (stuId == null) return NotFound();

            var users = await UserManager.FindByStudentIdAsync(stuid);
            var user = users.SingleOrDefault(u => u.Id == uid);
            if (user == null) return NotFound();
            
            if (!user.StudentVerified)
            {
                user.StudentVerified = true;
                await UserManager.UpdateAsync(user);
                await UserManager.AddToRoleAsync(user, "Student");
                await HttpContext.AuditAsync("verified", $"student s{stuid}", user == null ? null : $"to u{user.Id}");
            }

            StatusMessage = $"Marked {user.UserName} (u{user.Id}) as verified student.";
            return RedirectToAction(nameof(List), new { page });
        }


        [HttpGet("[action]")]
        [ValidateInAjax]
        public IActionResult Add()
        {
            return Window(new AddStudentsBatchModel());
        }


        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        [ValidateInAjax]
        public async Task<IActionResult> Add(AddStudentsBatchModel model)
        {
            var stus = model.Students.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var adds = new List<Student>();
            var ints = new HashSet<int>();

            foreach (var item in stus)
            {
                var ofs = item.Trim().Split(new[] { ' ', '\t', ',' }, 2, StringSplitOptions.RemoveEmptyEntries);
                if (ofs.Length != 2)
                    ModelState.AddModelError("xys::parseError2", $"Unrecognized token {item.Trim()}.");
                if (!int.TryParse(ofs[0], out int stuid))
                    ModelState.AddModelError("xys::parseError", $"Unrecognized number token {ofs[0]}.");
                else if (ints.Contains(stuid))
                    ModelState.AddModelError("xys::parseError3", $"Duplicate student id {stuid}.");
                else
                    adds.Add(new Student { Id = stuid, Name = ofs[1] });
            }

            if (!ModelState.IsValid) return Window(model);

            int rows = await UserManager.MergeStudentListAsync(adds);
            await HttpContext.AuditAsync("merge", "students");
            StatusMessage = $"{rows} students updated or added.";
            return RedirectToAction(nameof(List), new { page = 1 });
        }
    }
}
