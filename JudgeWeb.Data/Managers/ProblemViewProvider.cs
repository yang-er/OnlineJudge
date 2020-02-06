using JudgeWeb.Areas.Polygon.Models;
using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Polygon.Services
{
    public interface IProblemViewProvider
    {
        Task<ProblemStatement> LoadStatement(Problem problem, DbSet<Testcase> testc);

        StringBuilder BuildHtml(ProblemStatement statement);

        void BuildLatex(ZipArchive zip, ProblemStatement statement, string filePrefix = "");
    }
}
