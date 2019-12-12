using Microsoft.AspNetCore.Razor.TagHelpers;
using System;
using System.Text;

namespace JudgeWeb.Features.Razor
{
    [HtmlTargetElement("img", Attributes = "gravatar-email")]
    public class GravatarTagHelper : TagHelper
    {
        [HtmlAttributeName("gravatar-email")]
        public string GravatarEmail { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            base.Process(context, output);

            var url = new StringBuilder("//www.gravatar.com/avatar/", 90);
            url.Append(GravatarEmail?.Trim().ToLower().ToMD5(Encoding.ASCII).ToLower() ?? new string('0', 32));
            url.Append("?u=monsterid&s=256");
            output.Attributes.Add("src", url.ToString());
        }
    }
}
