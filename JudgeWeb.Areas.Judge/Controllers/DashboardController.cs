using JudgeWeb.Areas.Judge.Models;
using JudgeWeb.Areas.Judge.Services;
using JudgeWeb.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Judge.Controllers
{
    [Area("Judge")]
    [Authorize(Roles = "Administrator")]
    [Route("[area]/[controller]/[action]")]
    public class DashboardController : Controller2
    {
        private JudgeManager JudgeManager { get; }

        private ILogger<DashboardController> Logger { get; }

        public DashboardController(JudgeManager jm)
        {
            JudgeManager = jm;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Toggle(
            string @as, string aj, string jh,
            [FromServices] LanguageManager langMgr)
        {
            if (@as != null)
            {
                await langMgr.ToggleAsync(aj,
                    l => l.AllowSubmit = !l.AllowSubmit);
            }
            else if (aj != null)
            {
                await langMgr.ToggleAsync(aj,
                    l => l.AllowJudge = !l.AllowJudge);
            }
            else if (jh != null)
            {
                await JudgeManager.ToggleJudgehostAsync(jh);
            }
            else
            {
                return BadRequest();
            }

            return RedirectToAction(jh is null
                ? nameof(Language) : nameof(JudgeHost));
        }

        [HttpGet]
        public async Task<IActionResult> Executable()
        {
            if (!IsWindowAjax)
            {
                return View(await JudgeManager.GetExecutablesAsync());
            }
            else
            {
                return Window("ExecutableUpload", new ExecutableUploadModel());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(1 << 26)]
        public async Task<IActionResult> Executable(ExecutableUploadModel model)
        {
            if (string.IsNullOrEmpty(model.ID)) return BadRequest();

            var upd = model.UploadContent == null ? default((byte[], string)?) :
                await model.UploadContent.ReadAsync();
            var result = await JudgeManager.UpdateExecutableAsync(
                model.ID, model.Description, model.Type, upd);
            if (!result) return BadRequest();

            return Message(
                "Executable Upload",
                $"Executable `{model.ID}` uploaded successfully.",
                MessageType.Success);
        }

        [HttpGet("{target}")]
        public async Task<IActionResult> Executable(string target)
        {
            var bytes = await JudgeManager.GetExecutableAsync(target);
            if (bytes is null) return NotFound();
            return File(bytes, "application/zip", $"{target}.zip", false);
        }

        [HttpGet]
        public async Task<IActionResult> InternalError()
        {
            var model = await JudgeManager.ListInternalErrorsAsync();
            return View("InternalErrors", model);
        }

        [HttpGet("{eid}")]
        public async Task<IActionResult> InternalError(int eid, string todo)
        {
            return View(await JudgeManager.UpsolveInternalErrorAsync(eid, todo));
        }

        [HttpGet]
        public async Task<IActionResult> JudgeHost()
        {
            var hosts = await JudgeManager.GetJudgehostsAsync();
            return View("JudgeHosts", hosts);
        }

        [HttpGet("{hostname}")]
        public async Task<IActionResult> JudgeHost(string hostname)
        {
            var host = await JudgeManager.GetJudgehostAsync(hostname);
            if (host is null) return NotFound();
            ViewBag.Host = host;
            (ViewBag.Judgings, ViewData["Count"]) = await JudgeManager
                .ListJudgingsByServerIdAsync(host.ServerId);
            return View();
        }

        public async Task<IActionResult> Language(
            [FromServices] LanguageManager manager)
        {
            ViewBag.Statistics = await manager.StatisticsAsync();
            return View("Languages", manager.GetAll().Values);
        }

        [HttpGet("{extid}")]
        public async Task<IActionResult> Language(string extid,
            [FromServices] LanguageManager manager,
            [FromServices] SubmissionManager manager2)
        {
            var lang = manager.GetAll().Values
                .Where(l => l.ExternalId == extid)
                .FirstOrDefault();
            if (lang is null) return NotFound();

            ViewBag.Language = lang;
            ViewBag.Submissions = await manager2
                .EnumerateAsync(s => s.Language == lang.LangId, 200);
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> CreateContest(
            [FromServices] UserManager userMgr,
            [FromServices] RoleManager<IdentityRole<int>> roleMgr)
        {
            int cid = await JudgeManager.CreateContestAsync(userMgr.GetUserName(User));

            var roleName = $"JuryOfContest{cid}";
            var result = await roleMgr.CreateAsync(new IdentityRole<int>(roleName));
            if (!result.Succeeded) return Json(result);

            var firstUser = await userMgr.GetUserAsync(User);
            var roleAttach = await userMgr.AddToRoleAsync(firstUser, roleName);
            if (!roleAttach.Succeeded) return Json(roleAttach);
            return RedirectToAction("Home", "Jury", new { area = "Contest", cid });
        }

        [HttpGet]
        public IActionResult Images()
        {
            return View(JudgeManager.GetImages());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInAjax]
        [RequestSizeLimit(1 << 26)]
        public async Task<IActionResult> Images(IFormFile file)
        {
            try
            {
                var writeStream = System.IO.File.OpenWrite("wwwroot/images/problem/" + Path.GetFileName(file.FileName));
                await file.CopyToAsync(writeStream);
                writeStream.Close();
                return Message("Upload media", "Upload succeeded.", MessageType.Success);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Upload failed.");
                return Message("Upload media", "Upload failed. " + ex.ToString(), MessageType.Danger);
            }
        }

        [HttpPost("{affid}")]
        [ValidateInAjax]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Affiliation(int affid, TeamAffiliation model, IFormFile logo)
        {
            await JudgeManager.UpdateAffiliationAsync(affid, model);
            var msg = "Affiliation updated. ";

            if (logo != null && logo.FileName.EndsWith(".png"))
            {
                try
                {
                    var write = new FileStream($"wwwroot/images/affiliations/{model.ExternalId}.png", FileMode.OpenOrCreate);
                    await logo.CopyToAsync(write);
                    write.Close();
                    msg = msg + "Logo uploaded.";
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Upload failed. ");
                    msg = msg + "Upload failed!";
                }
            }
            else if (logo != null)
            {
                msg = msg + "Logo should be png!";
            }

            return Message("Update affiliation", msg, msg.EndsWith('!') ? MessageType.Warning : MessageType.Success);
        }

        [HttpGet("{affid}")]
        [ValidateInAjax]
        public async Task<IActionResult> Affiliation(int affid)
        {
            return Window(await JudgeManager.GetAffiliationAsync(affid));
        }

        [HttpGet]
        public async Task<IActionResult> Affiliations()
        {
            return View(await JudgeManager.GetAffiliationsAsync());
        }
    }
}
