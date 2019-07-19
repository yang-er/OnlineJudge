using Microsoft.AspNetCore.Razor.TagHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Features.Razor
{
    [HtmlTargetElement(Attributes = "show-time")]
    public class ShowTimeTagHelper : TagHelper
    {
        static readonly DateTime TwoKilo = new DateTime(2000, 1, 1);

        [HtmlAttributeName("show-time")]
        public DateTime? ShowTime { get; set; }

        [HtmlAttributeName("null-value")]
        public string NullValue { get; set; } = "-";

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            base.Process(context, output);

            if (!ShowTime.HasValue)
                output.Content.Append(NullValue);
            else
            {
                if (ShowTime.Value < TwoKilo)
                {
                    var span = ShowTime.Value - DateTime.UnixEpoch;
                    output.Content.Append("+" + span.ToString("d\\.hh\\:mm\\:ss"));
                }
                else
                {
                    output.Content.Append(ShowTime.Value.ToString());
                }
            }
        }
    }
}
