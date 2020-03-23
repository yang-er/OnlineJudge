using JudgeWeb.Domains.Problems;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
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
        /// Get the executable with the given ID
        /// </summary>
        /// <param name="target">The ID of the entity to get</param>
        /// <response code="200">Base64-encoded executable contents</response>
        [HttpGet("{target}")]
        [Authorize(Roles = "Judgehost,Administrator")]
        public async Task<ActionResult<string>> OnGet(
            [FromRoute] string target,
            [FromServices] IProblemFacade facade)
        {
            var exec = await facade.Executables.FindAsync(target);
            if (exec is null) return NotFound();
            var base64encoded = Convert.ToBase64String(exec.ZipFile);
            return base64encoded;
        }
    }
}
