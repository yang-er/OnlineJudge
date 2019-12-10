using JudgeWeb.Areas.Polygon.Services;
using JudgeWeb.Data;
using JudgeWeb.Features.Problem;
using System;
using System.Collections.Generic;
using System.Text;

[assembly: Inject(typeof(IProblemViewProvider), typeof(DefaultProblemViewProvider))]
namespace JudgeWeb.Areas.Polygon.Services
{
    public interface IProblemViewProvider
    {
        StringBuilder Build(string description,
            string inputdesc, string outputdesc, string hint,
            Problem problem, List<TestCase> samples);
    }
}
