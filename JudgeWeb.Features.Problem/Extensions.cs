using System.Xml.Linq;

namespace JudgeWeb.Features.Problem
{
    public static class ProblemExtensions
    {
        /// <summary>
        /// 将问题以XML文档形式保存。
        /// </summary>
        /// <param name="problem">问题</param>
        /// <returns>XML文档</returns>
        public static XDocument ToXml(this ProblemSet problem)
        {
            var doc = new XDocument();
            doc.Add(new XElement("problem"));
            var root = doc.Root;

            root.Add(new XElement("id", problem.ProblemId));
            root.Add(new XElement("title", new XCData(problem.Title)));
            root.Add(new XElement("description", new XCData(problem.Description)));
            root.Add(new XElement("input", new XCData(problem.InputHint)));
            root.Add(new XElement("output", new XCData(problem.OutputHint)));
            root.Add(new XElement("author", new XCData(problem.Author)));
            root.Add(new XElement("time_limit", problem.ExecuteTimeLimit));
            root.Add(new XElement("memory_limit", problem.MemoryLimit));
            root.Add(new XElement("run_script", problem.RunScript));
            root.Add(new XElement("compare_script", problem.CompareScript));

            root.Add(new XElement("samples"));
            var samples = root.Element("samples");
            foreach (var tc in problem.Samples)
                samples.Add(tc.ToXml());

            root.Add(new XElement("test_cases"));
            var test_cases = root.Element("test_cases");
            foreach (var tc in problem.TestCases)
                test_cases.Add(tc.ToXml());

            root.Add(new XElement("hint", new XCData(problem.HintAndNote)));

            return doc;
        }

        /// <summary>
        /// 将XML文档以问题形式读取。
        /// </summary>
        /// <param name="xdoc">XML文档</param>
        /// <param name="web">是否读取网页端需要的数据</param>
        /// <param name="judge">是否读取评测端需要的数据</param>
        /// <returns>问题</returns>
        public static ProblemSet ToProblem(this XDocument xdoc, bool web, bool judge)
        {
            var doc = xdoc.Root;
            var prob = new ProblemSet();

            prob.Title = doc.Element("title").Value;
            prob.ProblemId = int.Parse(doc.Element("id").Value);
            prob.MemoryLimit = int.Parse(doc.Element("memory_limit").Value);
            prob.ExecuteTimeLimit = int.Parse(doc.Element("time_limit").Value);
            prob.RunScript = doc.Element("run_script").Value;
            prob.CompareScript = doc.Element("compare_script").Value;

            if (web)
            {
                prob.Description = doc.Element("description").Value;
                prob.InputHint = doc.Element("input").Value;
                prob.OutputHint = doc.Element("output").Value;
                prob.Author = doc.Element("author").Value;
                prob.HintAndNote = doc.Element("hint").Value;

                // 读取所有测试用例
                var tc = doc.Element("samples");
                foreach (XElement group in tc.Elements())
                    prob.Samples.Add(new TestCase(group));
            }

            if (judge)
            {
                var tc = doc.Element("test_cases");
                foreach (XElement group in tc.Elements())
                    prob.TestCases.Add(new TestCase(group));
            }

            return prob;
        }
    }
}
