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

        [HtmlAttributeName("judging-pending")]
        public bool IsJudgingPending { get; set; }

        [HtmlAttributeName("reg-status")]
        public int? RegistrationStatus { get; set; }

        [HtmlAttributeName("tooltip")]
        public string TooltipTitle { get; set; }

        [HtmlAttributeName("rule")]
        public int? ContestRule { get; set; }

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

        static readonly (string, string)[] team = new[]
        {
            ("sol sol_queued", "pending"),
            ("sol sol_correct", "accepted"),
            ("sol sol_incorrect", "rejected"),
            ("sol sol_incorrect", "deleted"),
            ("sol sol_queued", "unknown"),
        };

        private (string, string) SolveAsTeamStatus()
        {
            if (TeamStatus.Value >= 0 && TeamStatus.Value <= 3)
            {
                return team[TeamStatus.Value];
            }
            else
            {
                return team[4];
            }
        }

        private (string, string) SolveAsVerdict()
        {
            if (ValueInt.HasValue == ValueVerdict.HasValue
                && (int)ValueVerdict.Value != ValueInt.Value)
                throw new InvalidOperationException();
            var v = ValueInt ?? (int)ValueVerdict.Value;

            if (Target == VerdictTag.StatusText)
            {
                return ("state state-" + st[v].Item2, st[v].Item1);
            }
            else if (Target == VerdictTag.Badge)
            {
                return ("badge badge-" + st[v].Item2, st[v].Item1);
            }
            else if (Target == VerdictTag.BadgeSmall)
            {
                if (IsJudgingPending && v == (int)Verdict.Pending)
                    return ("verdict-sm badge badge-primary", st[v].Item3);
                else
                    return ("verdict-sm badge badge-" + st[v].Item2, st[v].Item3);
            }
            else if (IsTooLate)
            {
                return ("sol sol_queued", "too-late");
            }
            else
            {
                return ("sol sol_" + st[v].Item5, st[v].Item4);
            }
        }

        private (string, string) SolveAsRegistrationStatus()
        {
            if (RegistrationStatus.Value == 0)
            {
                return ("sol sol_incorrect", "closed");
            }
            else
            {
                return ("sol sol_correct", "open");
            }
        }

        private (string, string) SolveAsRule()
        {
            if (ContestRule.Value == 0)
            {
                return ("sol", "ACM-ICPC");
            }
            else if (ContestRule.Value == 1)
            {
                return ("sol", "Codeforces");
            }
            else if (ContestRule.Value == 2)
            {
                return ("sol", "NOI");
            }
            else
            {
                return ("sol sol_incorrect", "Unknown");
            }
        }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "span";
            output.TagMode = TagMode.StartTagAndEndTag;
            string @class, content;

            if (TeamStatus.HasValue)
            {
                (@class, content) = SolveAsTeamStatus();
            }
            else if (RegistrationStatus.HasValue)
            {
                (@class, content) = SolveAsRegistrationStatus();
            }
            else if (ContestRule.HasValue)
            {
                (@class, content) = SolveAsRule();
            }
            else
            {
                (@class, content) = SolveAsVerdict();
            }

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
