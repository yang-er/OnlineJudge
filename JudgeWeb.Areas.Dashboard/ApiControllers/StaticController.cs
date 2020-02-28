using JudgeWeb.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Threading.Tasks;
using FileUtils = System.IO.File;

namespace JudgeWeb.Areas.Api.Controllers
{
    [Area("Dashboard")]
    [Authorize]
    [Route("api/[controller]/[action]")]
    [Produces("application/json")]
    public class StaticController : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> ImagesUpload(
            [FromQuery] int id, [FromQuery] string type,
            [FromForm(Name = "editormd-image-file")] IFormFile formFile)
        {
            // handle authorize
            bool authorized = User.IsInRole("Administrator");
            if (type == "c")
                authorized = authorized || User.IsInRole($"JuryOfContest{id}");
            else if (type == "p")
                authorized = authorized || User.IsInRole($"AuthorOfProblem{id}");
            else
                authorized = false;
            if (!authorized) return new ObjectResult(new { success = 0, message = "无权限访问。" });

            // upload files
            string fileName;
            do
            {
                var ext = Path.GetExtension(formFile.FileName);
                var guid = Guid.NewGuid().ToString("N").Substring(0, 16);
                fileName = $"/images/problem/{type}{id}.{guid}{ext}";
            }
            while (FileUtils.Exists("wwwroot" + fileName));

            using (var dest = FileUtils.OpenWrite("wwwroot" + fileName))
                await formFile.CopyToAsync(dest);
            return new ObjectResult(new { success = 1, url = fileName });
        }
    }
}
