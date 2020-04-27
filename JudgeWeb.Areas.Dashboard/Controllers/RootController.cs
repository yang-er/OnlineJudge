using JudgeWeb.Areas.Dashboard.Models;
using JudgeWeb.Domains.Problems;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Dashboard.Controllers
{
    [Area("Dashboard")]
    [Authorize(Roles = "Administrator,Teacher,Problem")]
    [Route("[area]")]
    public class RootController : Controller3
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }


        [HttpGet("[action]")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Updates(
            [FromServices] IJudgehostStore jh,
            [FromServices] IInternalErrorStore ie)
        {
            var judgehosts = await jh.GetJudgeStatusAsync();
            var internal_error = await ie.GetJudgeStatusAsync();
            return Json(new { judgehosts, internal_error });
        }


        [HttpGet("[action]/{page?}")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Auditlog(
            [FromServices] IAuditlogger auditlogger,
            int page = 1)
        {
            if (page <= 0) return BadRequest();
            ViewBag.Page = page;
            return View(await auditlogger.ViewLogsAsync(null, page, 1000));
        }


        [HttpGet("[action]")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Config(
            [FromServices] IConfigurationRegistry registry)
        {
            return View(await registry.ListPublicAsync());
        }


        [HttpPost("[action]")]
        [Authorize(Roles = "Administrator")]
        [ValidateAntiForgeryToken]
        [AuditPoint(AuditlogType.Configuration)]
        public async Task<IActionResult> Config(
            ConfigureEditModel models,
            [FromServices] IConfigurationRegistry registry)
        {
            var items = await registry.ListPublicAsync();

            foreach (var item in items)
            {
                if (!models.Config.ContainsKey(item.Name)
                    || models.Config[item.Name] == null)
                    continue;
                
                var newVal = models.Config[item.Name];
                if (item.Type == "string") newVal = newVal.ToJson();
                if (newVal == item.Value) continue;
                
                await registry.UpdateValueAsync(item.Name, newVal);
                await HttpContext.AuditAsync("updated", item.Name, "from " + item.Value);
            }

            StatusMessage = "Configurations saved successfully.";
            return RedirectToAction(nameof(Config));
        }


        [Route("[action]")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult NotFound2() => StatusCodePage(404);
        [Route("[action]")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => StatusCodePage();
    }
}
