using JudgeWeb.Areas.Api.Models;
using JudgeWeb.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Api.Controllers
{
    /// <summary>
    /// 用于和 DOMjudge 对接的API控制器。
    /// </summary>
    [Area("Api")]
    [Authorize(AuthenticationSchemes = "Basic")]
    [Route("[area]/[action]")]
    [Produces("application/json")]
    public class GeneralController : ControllerBase
    {
        /// <summary>
        /// 数据库上下文
        /// </summary>
        AppDbContext DbContext { get; }

        /// <summary>
        /// 构造 DOMjudge API 的控制器。
        /// </summary>
        /// <param name="rdbc">数据库</param>
        public GeneralController(AppDbContext rdbc)
        {
            DbContext = rdbc;
        }


        /// <summary>
        /// Get the current API version
        /// </summary>
        /// <response code="200">The current API version information</response>
        [HttpGet]
        [AllowAnonymous]
        public ActionResult<object> Version()
        {
            return new { api_version = 4 };
        }


        /// <summary>
        /// Get information about the API and DOMjudge
        /// </summary>
        /// <response code="200">Information about the API and DOMjudge</response>
        [HttpGet]
        [AllowAnonymous]
        public ActionResult<object> Info(
            [FromServices] IHostingEnvironment environment)
        {
            return new
            {
                api_version = 4,
                domjudge_version = "7.0.2",
                environment = environment.IsDevelopment() ? "dev" : "prod",
            };
        }


        /// <summary>
        /// Get general status information
        /// </summary>
        /// <response code="200">General status information for the currently active contests</response>
        [HttpGet]
        [Authorize(Roles = "Judgehost,Administrator")]
        public async Task<ActionResult<List<ServerStatus>>> Status(
            [FromServices] SubmissionManager subManager)
        {
            var judgingStatus = await subManager.Judgings
                .Where(j => j.Active)
                .Join(
                    inner: subManager.Submissions,
                    outerKeySelector: j => j.SubmissionId,
                    innerKeySelector: s => s.SubmissionId,
                    resultSelector: (j, s) => new { j.Status, s.ContestId })
                .GroupBy(g => new { g.Status, g.ContestId })
                .Select(g => new { g.Key.Status, Cid = g.Key.ContestId, Count = g.Count() })
                .ToListAsync();

            return judgingStatus
                .GroupBy(a => a.Cid)
                .Select(g => new ServerStatus
                {
                    cid = g.Key,
                    num_submissions = g.Sum(a => a.Count),
                    num_queued = g
                        .Where(a => a.Status == Verdict.Pending)
                        .Select(a => a.Count)
                        .FirstOrDefault(),
                    num_judging = g
                        .Where(a => a.Status == Verdict.Running)
                        .Select(a => a.Count)
                        .FirstOrDefault(),
                })
                .ToList();
        }
        

        /// <summary>
        /// Get information about the currently logged in user
        /// </summary>
        /// <response code="200">Information about the logged in user</response>
        [HttpGet]
        [ActionName("User")]
        public async Task<ActionResult<UserInfo>> Users(
            [FromServices] UserManager userManager)
        {
            var user = await userManager.GetUserAsync(User);
            var roles = await userManager.GetRolesAsync(user);

            return new UserInfo
            {
                email = user.Email,
                id = user.Id,
                lastip = HttpContext.Connection.RemoteIpAddress.ToString(),
                name = string.IsNullOrEmpty(user.NickName) ? user.UserName : user.NickName,
                username = user.UserName,
                roles = roles,
            };
        }


        /// <summary>
        /// Get configuration variables
        /// </summary>
        /// <param name="name">Get only this configuration variable</param>
        /// <response code="200">The configuration variables</response>
        [HttpGet]
        [Authorize(Roles = "Judgehost,Administrator")]
        public async Task<IActionResult> Config(string name)
        {
            var jo = new JObject();
            var query = DbContext.Configures
                .Select(c => new { c.Name, c.Value });

            if (name != null)
            {
                query = query.Where(c => c.Name == name);
            }

            var value = await query.ToListAsync();
            value.ForEach(a => jo[a.Name] = JToken.Parse(a.Value));
            return new JsonResult(jo);
        }
    }
}
