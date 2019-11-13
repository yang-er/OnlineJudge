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
    }
}
