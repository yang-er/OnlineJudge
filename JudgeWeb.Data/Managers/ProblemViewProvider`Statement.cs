using JudgeWeb.Data;
using System.Collections.Generic;

namespace JudgeWeb.Areas.Polygon.Models
{
    public class ProblemStatement
    {
        public string Description { get; set; }
        public string Input { get; set; }
        public string Output { get; set; }
        public string Hint { get; set; }
        public string Interaction { get; set; }
        public Problem Problem { get; set; }
        public List<TestCase> Samples { get; set; }
    }
}
