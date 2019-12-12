using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// 展示Ajax消息对话框的结果。
    /// </summary>
    public class ShowMessageResult : ViewResult
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
        /// 消息的类型
        /// </summary>
        public MessageType? Type { get; set; }

        public override Task ExecuteResultAsync(ActionContext context)
        {
            if (!ViewData.ContainsKey("HandleKey"))
                throw new InvalidOperationException();
            ViewData["Message"] = Content;
            ViewData["Title"] = Title;
            if (Type.HasValue) ViewData["MsgType"] = Type.ToString().ToLower();
            ViewName = "_MessageAjax";

            return base.ExecuteResultAsync(context);
        }
    }
}
