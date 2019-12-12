using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// 展示Ajax消息对话框的结果。
    /// </summary>
    public class ShowMessage2Result : ViewResult
    {
        /// <summary>
        /// 对话框标题
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// 对话框内容
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// 对话框内容
        /// </summary>
        public Dictionary<string, string> RouteValues { get; set; }

        /// <summary>
        /// Action名称
        /// </summary>
        public string ActionName { get; set; }

        /// <summary>
        /// Controller名称
        /// </summary>
        public string ControllerName { get; set; }

        /// <summary>
        /// Area名称
        /// </summary>
        public string AreaName { get; set; }

        /// <summary>
        /// 消息的类型
        /// </summary>
        public MessageType? Type { get; set; }

        public override Task ExecuteResultAsync(ActionContext context)
        {
            if (!ViewData.ContainsKey("HandleKey"))
                throw new InvalidOperationException();
            ViewData["Message"] = Content;
            ViewData["Title"] = Title;
            ViewData["RouteValues"] = RouteValues ?? new Dictionary<string, string>();
            ViewData["AreaName"] = AreaName;
            ViewData["ControllerName"] = ControllerName;
            ViewData["ActionName"] = ActionName;
            if (Type.HasValue) ViewData["MsgType"] = Type.ToString().ToLower();
            ViewName = "_Message2Ajax";

            return base.ExecuteResultAsync(context);
        }
    }
}
