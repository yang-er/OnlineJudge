using Microsoft.AspNetCore.Razor.TagHelpers;

namespace JudgeWeb.Features.Razor
{
    /// <summary>
    /// Show DOMjudge style verdict.
    /// </summary>
    [HtmlTargetElement("verdict3")]
    public class VerdictDomTagHelper : XysTagHelper
    {
        [HtmlAttributeName("value")]
        public Verdict Value { get; set; }

        [HtmlAttributeName("too-late")]
        public bool IsTooLate { get; set; }

        [HtmlAttributeName("tooltip")]
        public string TooltipTitle { get; set; }

        [HtmlAttributeName("skipped")]
        public bool Skipped { get; set; }

        static readonly (string, string)[] st = new[]
        {
            ("unknown", "queued"), // 0
            ("timelimit", "incorrect"),
            ("memory-limit", "incorrect"),
            ("run-error", "incorrect"),
            ("output-limit", "incorrect"),
            ("wrong-answer", "incorrect"), // 5
            ("compiler-error", "incorrect"),
            ("wrong-answer", "incorrect"),
            ("queued", "queued"),
            ("running", "queued"),
            ("undefined", "incorrect"), // 10
            ("correct", "correct"),
        };

        private (string, string) SolveAsVerdict()
        {
            var v = (int)Value;
            if (IsTooLate)
                return ("sol sol_queued", "too-late");
            else if (Skipped)
                return ("sol sol_queued", "skipped");
            else
                return ("sol sol_" + st[v].Item2, st[v].Item1);
        }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "span";
            output.TagMode = TagMode.StartTagAndEndTag;
            var (@class, content) = SolveAsVerdict();

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
