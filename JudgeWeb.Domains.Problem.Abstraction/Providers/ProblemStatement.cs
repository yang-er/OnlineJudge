using JudgeWeb.Data;
using System.Collections.Generic;

namespace JudgeWeb.Domains.Problems
{
    public class ProblemStatement
    {
        public string Description { get; set; }

        public string Input { get; set; }
        
        public string Output { get; set; }
        
        public string Hint { get; set; }
        
        public string Interaction { get; set; }
        
        public Problem Problem { get; set; }
        
        public string ShortName { get; set; }

        public List<MemoryTestCase> Samples { get; set; }
    }
}
