using JudgeWeb.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Api.Controllers
{
    /// <summary>
    /// 用于和 DOMjudge 对接的API控制器。
    /// </summary>
    [Area("Api")]
    [Authorize(AuthenticationSchemes = "Basic")]
    [Route("[area]/[controller]")]
    [Produces("application/json")]
    public class ExecutablesController : ControllerBase
    {
        /// <summary>
        /// 数据库上下文
        /// </summary>
        AppDbContext DbContext { get; }

        /// <summary>
        /// 构造 DOMjudge API 的控制器。
        /// </summary>
        /// <param name="rdbc">数据库</param>
        public ExecutablesController(AppDbContext rdbc)
        {
            DbContext = rdbc;
        }

        /// <summary>
        /// Get the executable with the given ID
        /// </summary>
        /// <param name="target">The ID of the entity to get</param>
        /// <response code="200">Base64-encoded executable contents</response>
        [HttpGet("{target}")]
        [Authorize(Roles = "Judgehost,Administrator")]
        public async Task<ActionResult<string>> OnGet(string target)
        {
            var zipContent = await DbContext.Executable
                .Where(e => e.ExecId == target)
                .Select(e => e.ZipFile)
                .FirstOrDefaultAsync();
            if (zipContent is null) return NotFound();
            var base64encoded = Convert.ToBase64String(zipContent);
            return base64encoded;
        }
    }
}
