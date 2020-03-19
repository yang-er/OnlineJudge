using JudgeWeb.Data;

namespace JudgeWeb.Domains.Contests.ApiModels
{
    [EntityType("groups")]
    public class Group : EventEntity
    {
        public bool hidden { get; set; }
        public string icpc_id { get; set; }
        public string name { get; set; }
        public int sortorder { get; set; }
        public string color { get; set; }

        public Group() { }

        public Group(TeamCategory c)
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
