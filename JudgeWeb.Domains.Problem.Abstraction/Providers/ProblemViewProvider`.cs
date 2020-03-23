using JudgeWeb.Data;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Problems
{
    public interface IProblemViewProvider
    {
        StringBuilder BuildHtml(ProblemStatement statement);

        void BuildLatex(ZipArchive zip, ProblemStatement statement, string filePrefix = "");
    }
}
