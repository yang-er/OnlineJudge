using JudgeWeb.Data;
using System;
using System.Reflection;
using System.Text;

namespace JudgeWeb.Domains.Contests.ApiModels
{
    public abstract class EventEntity
    {
        public string id { get; set; }

        public Event ToEvent(string action, int cid)
        {
            string content;
            if (action == "delete")
                content = $"{{\"id\":\"{id}\"}}";
            else
                content = this.ToJson();

            return new Event
            {
                Action = action,
                Content = Encoding.UTF8.GetBytes(content),
                ContestId = cid,
                EndPointId = id,
                EndPointType = GetType().GetCustomAttribute<EntityTypeAttribute>(true).EndPointType,
                EventTime = GetTime(action),
            };
        }

        protected virtual DateTimeOffset GetTime(string action) => DateTimeOffset.Now;
    }

    internal class EntityTypeAttribute : Attribute
    {
        public string EndPointType { get; }

        public EntityTypeAttribute(string ept)
        {
            EndPointType = ept;
        }
    }
}
