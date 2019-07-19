namespace JudgeWeb
{
    /// <summary>
    /// 判断结果
    /// </summary>
    public enum Verdict
    {
        /// <summary>
        /// 未知状态
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// 运行超时
        /// </summary>
        TimeLimitExceeded = 1,

        /// <summary>
        /// 内存超过限制
        /// </summary>
        MemoryLimitExceeded = 2,

        /// <summary>
        /// 运行时错误
        /// </summary>
        RuntimeError = 3,

        /// <summary>
        /// 输出超过限制
        /// </summary>
        OutputLimitExceeded = 4,

        /// <summary>
        /// 答案错误
        /// </summary>
        WrongAnswer = 5,

        /// <summary>
        /// 编译错误
        /// </summary>
        CompileError = 6,

        /// <summary>
        /// 表达形式错误
        /// </summary>
        PresentationError = 7,

        /// <summary>
        /// 等待评测
        /// </summary>
        Pending = 8,

        /// <summary>
        /// 正在评测
        /// </summary>
        Running = 9,

        /// <summary>
        /// 未指定的错误
        /// </summary>
        UndefinedError = 10,

        /// <summary>
        /// 通过测试
        /// </summary>
        Accepted = 11,
    }
}
