using JudgeWeb.Data;
using System.IO;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Problems
{
    public interface IExportProvider
    {
        ValueTask<(Stream stream, string mime, string fileName)> ExportAsync(Problem problem);
    }
}
