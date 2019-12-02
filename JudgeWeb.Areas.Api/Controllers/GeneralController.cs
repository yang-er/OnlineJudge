using JudgeWeb.Areas.Api.Models;
using JudgeWeb.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System;
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
        [HttpGet]
        public ActionResult<object> Version()
        {
            return new { api_version = 4 };
        }


        /// <summary>
        /// Get information about the currently logged in user
        /// </summary>
        /// <response code="200">Information about the logged in user</response>
        [HttpGet]
        [Authorize]
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
        /// Get general status information
        /// </summary>
        /// <response code="200">General status information for the currently active contests</response>
        [HttpGet]
        [Authorize(Roles = "Judgehost,Administrator")]
        public async Task<ActionResult<List<ServerStatus>>> Status()
        {
            var judgingStatus = await DbContext.Judgings
                .Where(j => j.Active)
                .Join(
                    inner: DbContext.Submissions,
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
                    num_submissions = g.Count(),
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
        /// Get the executable with the given ID
        /// </summary>
        /// <param name="target">The ID of the entity to get</param>
        /// <response code="200">Base64-encoded executable contents</response>
        [HttpGet("{target}")]
        [Authorize(Roles = "Judgehost")]
        public async Task<ActionResult<string>> Executables(string target)
        {
            var zipContent = await DbContext.Executable
                .Where(e => e.ExecId == target)
                .Select(e => e.ZipFile)
                .FirstOrDefaultAsync();
            if (zipContent is null) return NotFound();
            var base64encoded = Convert.ToBase64String(zipContent);
            return base64encoded;
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
