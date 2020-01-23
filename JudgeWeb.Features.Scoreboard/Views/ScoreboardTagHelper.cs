using Microsoft.AspNetCore.Razor.TagHelpers;

namespace JudgeWeb.Features.Scoreboard
{
    [HtmlTargetElement("scoreboard")]
    public class ScoreboardTagHelper : TagHelper
    {
        [HtmlAttributeName("model")]
        public BoardViewModel Model { get; set; }

        [HtmlAttributeName("use-footer")]
        public bool UseFooter { get; set; }

        [HtmlAttributeName("in-jury")]
        public bool InJury { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            base.Process(context, output);
            output.TagName = null;
            output.Content.SetHtmlContent(new BoardViewContent(Model, UseFooter, InJury));
        }
    }
}
