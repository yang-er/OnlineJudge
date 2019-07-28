using Microsoft.EntityFrameworkCore;

namespace JudgeWeb.Data
{
    public class ContestTestcase
    {
        /// <summary>
        /// 比赛编号
        /// </summary>
        public int ContestId { get; set; }

        /// <summary>
        /// 题目编号
        /// </summary>
        public int ProblemId { get; set; }

        /// <summary>
        /// 短名称
        /// </summary>
        public string ShortName { get; set; }

        /// <summary>
        /// 题目顺序
        /// </summary>
        public int Rank { get; set; }

        /// <summary>
        /// 是否允许提交
        /// </summary>
        public bool AllowSubmit { get; set; }

        /// <summary>
        /// 是否允许评测
        /// </summary>
        public bool AllowJudge { get; set; }

        /// <summary>
        /// 气球颜色
        /// </summary>
        public string Color { get; set; }

        /// <summary>
        /// 测试样例数
        /// </summary>
        public int TestcaseCount { get; set; }

        internal const string QueryString =
            "SELECT [cp].[ContestId], [cp].[ProblemId], [cp].[ShortName]," +
                  " [cp].[Rank], [cp].[AllowSubmit], [cp].[AllowJudge]," +
                  " [cp].[Color], COUNT(1) AS [TestcaseCount] " +
            "FROM [ContestProblem] AS [cp] " +
            "INNER JOIN [Testcases] AS [t] ON [cp].[ProblemId] = [t].[ProblemId] " +
            "WHERE [cp].[ContestId] = @__cid " +
            "GROUP BY [cp].[ProblemId], [cp].[ShortName], [cp].[ContestId]," +
                    " [cp].[Rank], [cp].[AllowSubmit], [cp].[AllowJudge], [cp].[Color]";
    }
}
