namespace JudgeWeb.Domains.Problems
{
    /// <summary>
    /// 表示一组测试样例
    /// </summary>
    public class MemoryTestCase
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
        public MemoryTestCase(string desc, string input, string output, int point)
        {
            Description = desc;
            Input = input.Replace("\r\n", "\n").Replace("\r", "");
            Output = output.Replace("\r\n", "\n").Replace("\r", "");
            Point = point;
        }
    }
}
