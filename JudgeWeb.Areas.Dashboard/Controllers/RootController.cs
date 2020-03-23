using JudgeWeb.Areas.Dashboard.Models;
using JudgeWeb.Domains.Problems;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Dashboard.Controllers
{
    [Area("Dashboard")]
    [Authorize(Roles = "Administrator")]
    [Route("[area]")]
    public class RootController : Controller3
    {
        private IJudgementFacade Facade { get; }

        public RootController(IJudgementFacade facade)
        {
            Facade = facade;
        }


        public IActionResult Index()
        {
            return View();
        }


        [HttpGet("[action]")]
        public async Task<IActionResult> Updates()
        {
            var stat = await Facade.GetJudgeStatusAsync();
            return Json(new { stat.judgehosts, stat.internal_error });
        }


        [HttpGet("[action]/{page?}")]
        public async Task<IActionResult> Auditlog(
            [FromServices] IAuditlogger auditlogger,
            int page = 1)
        {
            if (page <= 0) return BadRequest();
            ViewBag.Page = page;
            return View(await auditlogger.ViewLogsAsync(null, page, 1000));
        }


        [HttpGet("[action]")]
        public async Task<IActionResult> Config()
        {
            return View(await Facade.Configurations.ListPublicAsync());
        }


        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        [AuditPoint(AuditlogType.Configuration)]
        public async Task<IActionResult> Config(ConfigureEditModel models)
        {
            var items = await Facade.Configurations.ListPublicAsync();

            foreach (var item in items)
            {
                if (!models.Config.ContainsKey(item.Name)
                    || models.Config[item.Name] == null)
                    continue;
                
                var newVal = models.Config[item.Name];
                if (item.Type == "string") newVal = newVal.ToJson();
                if (newVal == item.Value) continue;
                
                await Facade.Configurations.UpdateValueAsync(item.Name, newVal);
                await HttpContext.AuditAsync("updated", item.Name, "from " + item.Value);
            }

            StatusMessage = "Configurations saved successfully.";
            return RedirectToAction(nameof(Config));
        }


        [Route("[action]")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult NotFound2() => ExplicitNotFound();
        [Route("[action]")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => StatusCodePage();
    }
}
