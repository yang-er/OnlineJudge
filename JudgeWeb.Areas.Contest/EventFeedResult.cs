using JudgeWeb.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Api
{
    public class EventFeedResult : IActionResult
    {
        private readonly IQueryable<Event> eventfeedSource;
        private readonly bool keepAlive;
        private readonly int since_id;

        public EventFeedResult(IQueryable<Event> feed, bool stream, int sinceid)
        {
            eventfeedSource = feed;
            keepAlive = stream;
            since_id = sinceid;
        }

        public async Task ExecuteResultAsync(ActionContext context)
        {
            var response = context.HttpContext.Response;
            response.StatusCode = 200;
            response.ContentType = "application/x-ndjson";

            int step = 0, since = since_id;

            while (true)
            {
                if (context.HttpContext.RequestAborted.IsCancellationRequested)
                    break;

                var events = await eventfeedSource
                    .Where(e => e.EventId > since)
                    .ToListAsync();

                if (events.Count > 0)
                    since = events.Last().EventId;
                var after = Encoding.UTF8.GetBytes("}\n");
                var newline = Encoding.UTF8.GetBytes("\n");

                foreach (var item in events)
                {
                    var beforeT = $"{{\"type\":\"{item.EndPointType}\",\"id\":\"{item.EndPointId}\",\"op\":\"{item.Action}\",\"data\":\"";
                    var before = Encoding.UTF8.GetBytes(beforeT);
                    await response.Body.WriteAsync(before, 0, before.Length);
                    await response.Body.WriteAsync(item.Content, 0, item.Content.Length);
                    await response.Body.WriteAsync(after, 0, after.Length);
                    await response.Body.FlushAsync();
                }

                if (!keepAlive) break;
                step++;

                if (step >= 30) // 30s no content and flush
                {
                    await response.Body.WriteAsync(newline, 0, newline.Length);
                    await response.Body.FlushAsync();
                    step = 0;
                }

                if (events.Count == 0) await Task.Delay(1000);
            }
        }
    }
}
