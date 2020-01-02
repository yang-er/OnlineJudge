namespace JudgeWeb.Areas.Contest.Services
{
    public class ContestResult
    {
        public bool IsValid { get; set; }

        public string Message { get; set; }

        public static ContestResult FromOk(string msg)
        {
            return new ContestResult
            {
                IsValid = true,
                Message = msg,
            };
        }

        public static ContestResult FromError(string msg)
        {
            return new ContestResult
            {
                IsValid = false,
                Message = msg,
            };
        }
    }
}
