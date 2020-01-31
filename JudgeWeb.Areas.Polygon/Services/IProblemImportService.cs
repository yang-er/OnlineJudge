using JudgeWeb.Data;
using Microsoft.AspNetCore.Http;
using System.Text;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Polygon.Services
{
    public interface IProblemImportService
    {
        StringBuilder LogBuffer { get; }

        Task<Problem> ImportAsync(IFormFile zipFile, string username);
    }
}
