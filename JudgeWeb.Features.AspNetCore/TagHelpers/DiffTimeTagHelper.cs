using Microsoft.AspNetCore.Razor.TagHelpers;
using System;

namespace JudgeWeb.Features.Razor
{
    [HtmlTargetElement("difftime")]
    public class DiffTimeTagHelper : XysTagHelper
    {
        static readonly DateTimeOffset TwoKilo = new DateTime(2000, 1, 1);

        [HtmlAttributeName("show-time")]
        public DateTimeOffset? ShowTime { get; set; }

        [HtmlAttributeName("null-value")]
        public string NullValue { get; set; } = "-";

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            base.Process(context, output);
            output.TagName = "span";
            output.TagMode = TagMode.StartTagAndEndTag;

            if (!ShowTime.HasValue)
                output.Content.Append(NullValue);
            else
            {
                if (ShowTime.Value < TwoKilo)
                {
                    var span = ShowTime.Value - DateTimeOffset.UnixEpoch;
                    output.Content.Append("+" + span.ToString("d\\.hh\\:mm\\:ss"));
                }
                else
                {
                    output.Content.Append(ShowTime.Value.ToString("yyyy-MM-dd HH:mm:ss zzz"));
                }
            }
        }
    }
}
