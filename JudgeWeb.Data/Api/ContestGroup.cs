namespace JudgeWeb.Data.Api
{
    [EntityType("groups")]
    public class ContestGroup : ContestEventEntity
    {
        public bool hidden { get; set; }
        public string icpc_id { get; set; }
        public string name { get; set; }
        public int sortorder { get; set; }
        public string color { get; set; }

        public ContestGroup() { }

        public ContestGroup(TeamCategory c)
        {
            hidden = !c.IsPublic;
            color = c.Color;
            icpc_id = "cat" + c.CategoryId;
            id = $"{c.CategoryId}";
            name = c.Name;
            sortorder = c.SortOrder;
        }
    }
}
