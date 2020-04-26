using JudgeWeb.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Problems
{
    public interface IImportProvider
    {
        public static Dictionary<string, Type> ImportServiceKinds;

        StringBuilder LogBuffer { get; }

        Task<List<Problem>> ImportAsync(
            [NotNull] Stream stream,
            [NotNull] string streamFileName,
            [NotNull] string username);
    }
}
