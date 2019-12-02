using JudgeWeb.Areas.Judge.Models;
using JudgeWeb.Areas.Judge.Services;
using JudgeWeb.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[assembly: Inject(typeof(JudgeManager))]
namespace JudgeWeb.Areas.Judge.Services
{
    public class JudgeManager
    {
        private AppDbContext DbContext { get; }

        private IFileProvider FileProvider { get; }

        public JudgeManager(AppDbContext adbc, IHostingEnvironment he)
        {
            DbContext = adbc;
            FileProvider = he.WebRootFileProvider;
        }

        public IDirectoryContents GetImages()
        {
            return FileProvider.GetDirectoryContents("images/problem");
        }

        public Task<List<Executable>> GetExecutablesByProblemEditorAsync()
        {
            return DbContext.Executable
                .Where(e => e.Type == "run" || e.Type == "compare")
                .Select(e =>
                    new Executable
                    {
                        Type = e.Type,
                        Description = e.Description,
                        ExecId = e.ExecId,
                    })
                .ToListAsync();
        }
    }
}
