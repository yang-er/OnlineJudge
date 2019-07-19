using System.Xml.Linq;

namespace JudgeWeb.Features.Problem
{
    /// <summary>
    /// 表示一组测试样例
    /// </summary>
    public class TestCase
    {
        /// <summary>
        /// 数据描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 输入数据
        /// </summary>
        public string Input { get; set; }
        
        /// <summary>
        /// 输出数据
        /// </summary>
        public string Output { get; set; }

        /// <summary>
        /// 测试点分数
        /// </summary>
        public int Point { get; set; }

        /// <summary>
        /// 实例化一个空白的测试样例。
        /// </summary>
        /// <param name="desc">数据描述</param>
        /// <param name="input">输入数据</param>
        /// <param name="output">输出数据</param>
        public TestCase(string desc, string input, string output, int point)
        {
            Description = desc;
            Input = input.Replace("\r\n", "\n").Replace("\r", "");
            Output = output.Replace("\r\n", "\n").Replace("\r", "");
            Point = point;
        }
        
        /// <summary>
        /// 从XML中读取测试样例数据。
        /// </summary>
        /// <param name="node">XML节点</param>
        public TestCase(XElement node) : this(
            (string)node.Element("desc"),
            (string)node.Element("input"),
            (string)node.Element("output"),
            (int)node.Element("point")
        ) { }

        /// <summary>
        /// 转换为XML节点。
        /// </summary>
        /// <returns>结果</returns>
        public XElement ToXml()
        {
            var g = new XElement("group");
            g.Add(new XElement("desc", new XCData(Description)));
            g.Add(new XElement("input", new XCData(Input)));
            g.Add(new XElement("output", new XCData(Output)));
            g.Add(new XElement("point", Point));
            return g;
        }
    }
}
