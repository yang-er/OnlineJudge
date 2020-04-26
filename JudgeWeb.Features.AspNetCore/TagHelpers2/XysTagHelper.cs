using Microsoft.AspNetCore.Mvc.TagHelpers;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Razor.TagHelpers
{
    public class XysTagHelper : ITagHelper
    {
        public virtual int Order => 0;

        public virtual void Init(TagHelperContext context)
        {
            
        }

        public virtual void Process(TagHelperContext context, TagHelperOutput output)
        {
        }

        public virtual Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            Process(context, output);
            return Task.CompletedTask;
        }

        Task ITagHelperComponent.ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            if (DisplayWhenTagHelper.Check(context))
                return Task.CompletedTask;
            return ProcessAsync(context, output);
        }
    }
}
