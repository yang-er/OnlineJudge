namespace JudgeWeb.Domains.Contests.ApiModels
{
    [EntityType("problems")]
    public class Problem : EventEntity
    {
        public int ordinal { get; set; }
        public string short_name { get; set; }
        public int internalid { get; set; }
        public string label { get; set; }
        public double time_limit { get; set; }
        public string externalid { get; set; }
        public string name { get; set; }
        public string rgb { get; set; }
        //public string color { get; set; }
        public int test_data_count { get; set; }

        public Problem() { }

        public Problem(Data.ContestProblem cp)
        {
            ordinal = cp.Rank - 1;
            label = cp.ShortName;
            short_name = cp.ShortName;
            internalid = cp.ProblemId;
            id = $"{cp.ProblemId}";
            time_limit = cp.TimeLimit / 1000.0;
            name = cp.Title;
            rgb = cp.Color;
            test_data_count = cp.TestcaseCount;
        }
    }
}
