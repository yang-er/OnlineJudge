using Microsoft.AspNetCore.Razor.TagHelpers;
using System;

namespace JudgeWeb.Features.Razor
{
    /// <summary>
    /// Verdict icon for misc things
    /// </summary>
    [HtmlTargetElement("verdict0")]
    public class VerdictMiscTagHelper : TagHelper
    {
        public enum UseType
        {
            None,
            TeamStatus,
            RegistrationStatus,
            ContestRule,
        }

        [HtmlAttributeName("type")]
        public UseType Type { get; set; }

        [HtmlAttributeName("value")]
        public int Value { get; set; }

        [HtmlAttributeName("tooltip")]
        public string TooltipTitle { get; set; }

        private (string, string) SolveAsTeamStatus()
        {
            if (Value == 0)
                return ("sol sol_queued", "pending");
            else if (Value == 1)
                return ("sol sol_correct", "accepted");
            else if (Value == 2)
                return ("sol sol_incorrect", "rejected");
            else if (Value == 3)
                return ("sol sol_incorrect", "deleted");
            return ("sol sol_queued", "unknown");
        }

        private (string, string) SolveAsRegistrationStatus()
        {
            return Value == 0
                ? ("sol sol_incorrect", "closed")
                : ("sol sol_correct", "open");
        }

        private (string, string) SolveAsRule()
        {
            if (Value == 0)
                return ("sol", "ACM-ICPC");
            else if (Value == 1)
                return ("sol", "Codeforces");
            else if (Value == 2)
                return ("sol", "NOI");
            return ("sol sol_incorrect", "Unknown");
        }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "span";
            output.TagMode = TagMode.StartTagAndEndTag;
            string @class, content;

            if (Type == UseType.TeamStatus)
                (@class, content) = SolveAsTeamStatus();
            else if (Type == UseType.RegistrationStatus)
                (@class, content) = SolveAsRegistrationStatus();
            else if (Type == UseType.ContestRule)
                (@class, content) = SolveAsRule();
            else
                throw new InvalidOperationException();

            output.Attributes.TryGetAttribute("class", out var clv);
            output.Attributes.SetAttribute("class", (clv?.Value ?? "") + " " + @class);

            if (!string.IsNullOrEmpty(TooltipTitle))
            {
                output.Attributes.SetAttribute("data-toggle", "tooltip");
                output.Attributes.SetAttribute("title", TooltipTitle);
            }

            output.Content.AppendHtml(content);
        }
    }
}
