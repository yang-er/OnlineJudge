using Microsoft.AspNetCore.Razor.TagHelpers;
using System;

namespace JudgeWeb.Features.Razor
{
    public enum VerdictTag
    {
        Badge,
        BadgeSmall,
        StatusText,
        DomJudge
    }

    [HtmlTargetElement("verdict")]
    public class VerdictTagHelper : TagHelper
    {
        [HtmlAttributeName("target")]
        public VerdictTag Target { get; set; }

        [HtmlAttributeName("value")]
        public Verdict? ValueVerdict { get; set; }

        [HtmlAttributeName("val")]
        public int? ValueInt { get; set; }

        [HtmlAttributeName("team-status")]
        public int? TeamStatus { get; set; }

        [HtmlAttributeName("too-late")]
        public bool IsTooLate { get; set; }

        static readonly (string, string, string, string, string)[] st = new[]
        {
            ("Unknown", "secondary", "?", "unkown", "queued"), // 0
            ("Time Limit Exceeded", "warning", "T", "time-limit", "incorrect"),
            ("Memory Limit Exceeded", "warning", "M", "memory-limit", "incorrect"),
            ("Runtime Error", "warning", "R", "run-error", "incorrect"),
            ("Output Limit Exceeded", "warning", "O", "output-limit", "incorrect"),
            ("Wrong Answer", "danger", "&times;", "wrong-answer", "incorrect"), // 5
            ("Compile Error", "primary", "C", "compiler-error", "incorrect"),
            ("Presentation Error", "danger", "P", "wrong-answer", "incorrect"),
            ("Pending", "secondary", "?", "queued", "queued"),
            ("Running", "info", "...", "running", "queued"),
            ("Undefined Error", "warning", "??", "undefined", "incorrect"), // 10
            ("Accepted", "success", "✓", "correct", "correct"),
        };

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "span";
            output.TagMode = TagMode.StartTagAndEndTag;
            string @class, content;

            if (TeamStatus.HasValue)
            {
                if (TeamStatus.Value == 0)
                {
                    @class = "sol sol_queued";
                    content = "Pending";
                }
                else if (TeamStatus.Value == 1)
                {
                    @class = "sol sol_correct";
                    content = "Accepted";
                }
                else if (TeamStatus.Value == 2)
                {
                    @class = "sol sol_incorrect";
                    content = "Rejected";
                }
                else
                {
                    @class = "sol sol_queued";
                    content = "Unknown";
                }
            }
            else
            {
                if (ValueInt.HasValue == ValueVerdict.HasValue
                    && (int)ValueVerdict.Value != ValueInt.Value)
                    throw new InvalidOperationException();
                var v = ValueInt ?? (int)ValueVerdict.Value;

                if (Target == VerdictTag.StatusText)
                {
                    @class = "state state-" + st[v].Item2;
                    content = st[v].Item1;
                }
                else if (Target == VerdictTag.Badge)
                {
                    @class = "badge badge-" + st[v].Item2;
                    content = st[v].Item1;
                }
                else if (Target == VerdictTag.BadgeSmall)
                {
                    @class = "verdict-sm badge badge-" + st[v].Item2;
                    content = st[v].Item3;
                }
                else if (IsTooLate)
                {
                    @class = "sol sol_queued";
                    content = "too-late";
                }
                else
                {
                    @class = "sol sol_" + st[v].Item5;
                    content = st[v].Item4;
                }
            }

            output.Attributes.TryGetAttribute("class", out var clv);
            output.Attributes.SetAttribute("class", (clv?.Value ?? "") + " " + @class);
            output.Content.AppendHtml(content);
        }
    }
}
