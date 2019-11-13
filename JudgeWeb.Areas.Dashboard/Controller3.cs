using EntityFrameworkCore.Cacheable;
using JudgeWeb.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Linq;

namespace JudgeWeb.Areas.Dashboard.Controllers
{
    public class Controller3 : Controller2
    {
        protected AppDbContext DbContext { get; }

        public Controller3(AppDbContext db) => DbContext = db;

        [TempData]
        public string StatusMessage { get; set; }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            base.OnActionExecuted(context);

            if (context.Result is ViewResult)
            {
                ViewBag.JudgehostCriticalCount = DbContext.JudgeHosts
                    .Where(jh => jh.PollTime < DateTimeOffset.Now.AddSeconds(-120) && jh.Active)
                    .Cacheable(TimeSpan.FromSeconds(5))
                    .Count();
                ViewBag.InternalErrorCount = DbContext.InternalErrors
                    .Where(ie => ie.Status == InternalError.ErrorStatus.Open)
                    .Cacheable(TimeSpan.FromSeconds(5))
                    .Count();
            }
        }
    }
}
