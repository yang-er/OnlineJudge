using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// 展示Ajax对话框的结果。
    /// </summary>
    public class ShowWindowResult : ViewResult
    {
        public override Task ExecuteResultAsync(ActionContext context)
        {
            if (!ViewData.ContainsKey("HandleKey2"))
                throw new InvalidOperationException();
            return base.ExecuteResultAsync(context);
        }
    }
}
