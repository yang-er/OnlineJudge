using EFCore.BulkExtensions;
using JudgeWeb.Areas.Dashboard.Models;
using JudgeWeb.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Dashboard.Controllers
{
    [Area("Dashboard")]
    [Authorize(Roles = "Administrator")]
    [Route("[area]/[controller]")]
    public class StudentsController : Controller3
    {
        const int ItemPerPage = 100;


        [HttpGet]
        public async Task<IActionResult> List(int page = 1)
        {
            if (page < 1) page = 1;
            int total = await DbContext.Students.CountAsync();
            int totPage = (total - 1) / ItemPerPage + 1;
            if (page > totPage) page = totPage;
            ViewBag.Page = page;
            ViewBag.TotalPage = totPage;

            var stuQuery =
                from s in DbContext.Students
                join u in DbContext.Users on s.Id equals u.StudentId
                into uu from u in uu.DefaultIfEmpty()
                orderby s.Id ascending
                select new Student
                {
                    Id = s.Id,
                    IsVerified = u.StudentVerified,
                    Name = s.Name,
                    StudentEmail = u.StudentEmail,
                    UserName = u.UserName,
                };

            var model = await stuQuery
                .Skip(ItemPerPage * (page - 1))
                .Take(ItemPerPage)
                .ToListAsync();
            return View(model);
        }


        [HttpGet("{stuid}/[action]")]
        [ValidateInAjax]
        public async Task<IActionResult> Unlink(int page, int stuid)
        {
            var user = await DbContext.Users
                .Where(u => u.StudentId == stuid)
                .Select(u => new { u.Id, u.UserName })
                .FirstOrDefaultAsync();

            if (user == null) return NotFound();

            return AskPost(
                title: $"Unlink student {stuid}",
                message: $"Are you sure to unlink student {stuid} with {user.UserName} (u{user.Id})?",
                area: "Dashboard", ctrl: "Students", act: "Unlink",
                type: MessageType.Warning);
        }


        [HttpPost("{stuid}/[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unlink(int page, int stuid, bool post = true)
        {
            var user = await DbContext.Users
                .Where(u => u.StudentId == stuid)
                .FirstOrDefaultAsync();
            if (user == null) return NotFound();

            user.StudentEmail = null;
            user.StudentId = null;
            user.StudentVerified = false;
            await UserManager.UpdateAsync(user);
            await UserManager.RemoveFromRoleAsync(user, "Student");
            await UserManager.SlideExpirationAsync(user);
            StatusMessage = $"Student ID {stuid} has been unlinked with u{user.Id}.";
            return RedirectToAction(nameof(List));
        }


        [HttpGet("{stuid}/[action]")]
        [ValidateInAjax]
        public async Task<IActionResult> Delete(int page, int stuid)
        {
            var stuId = await DbContext.Students
                .Where(s => s.Id == stuid)
                .FirstOrDefaultAsync();
            if (stuId == null) return NotFound();

            return AskPost(
                title: $"Delete student {stuid}",
                message: $"Are you sure to delete student {stuid}?",
                area: "Dashboard", ctrl: "Students", act: "Delete",
                type: MessageType.Warning);
        }


        [HttpPost("{stuid}/[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int page, int stuid, bool post = true)
        {
            var stuId = await DbContext.Students
                .Where(s => s.Id == stuid)
                .FirstOrDefaultAsync();
            if (stuId == null) return NotFound();

            var user = await DbContext.Users
                .Where(u => u.StudentId == stuid)
                .FirstOrDefaultAsync();

            if (user != null)
            {
                user.StudentEmail = null;
                user.StudentId = null;
                user.StudentVerified = false;
                await UserManager.UpdateAsync(user);
                await UserManager.RemoveFromRoleAsync(user, "Student");
                await UserManager.SlideExpirationAsync(user);
            }

            DbContext.Students.Remove(stuId);
            await DbContext.SaveChangesAsync();
            StatusMessage = $"Student ID {stuid} has been removed.";
            return RedirectToAction(nameof(List));
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

            foreach (var item in stus)
            {
                var ofs = item.Trim().Split(new[] { ' ', '\t', ',' }, 2, StringSplitOptions.RemoveEmptyEntries);
                if (ofs.Length != 2)
                    ModelState.AddModelError("xys::parseError2", $"Unrecognized token {item.Trim()}.");
                if (!int.TryParse(ofs[0], out int stuid))
                    ModelState.AddModelError("xys::parseError", $"Unrecognized number token {ofs[0]}.");
                else
                    adds.Add(new Student { Id = stuid, Name = ofs[1] });
            }

            if (!ModelState.IsValid) return Window(model);
            await DbContext.BulkInsertOrUpdateAsync(adds);
            return RedirectToAction(nameof(List), new { page = 1 });
        }
    }
}
