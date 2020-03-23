using JudgeWeb.Areas.Api.Models;
using JudgeWeb.Domains.Identity;
using JudgeWeb.Domains.Problems;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Text;
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
        private IJudgementFacade Facade { get; }

        public GeneralController(IJudgementFacade facade)
        {
            Facade = facade;
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
            [FromServices] IWebHostEnvironment environment)
        {
            return new
            {
                api_version = 4,
                domjudge_version = "7.2.0",
                environment = environment.IsDevelopment() ? "dev" : "prod",
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
            return await Facade.GetJudgeQueueAsync();
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
            var value = await Facade.Configurations.GetAsync(name);
            var result = new StringBuilder();
            result.Append("{");
            for (int i = 0; i < value.Count; i++)
                result.Append(i != 0 ? ",\"" : "\"")
                      .Append(value[i].Name)
                      .Append("\":")
                      .Append(value[i].Value);
            result.Append("}");
            return Content(result.ToString(), "application/json");
        }
    }
}
