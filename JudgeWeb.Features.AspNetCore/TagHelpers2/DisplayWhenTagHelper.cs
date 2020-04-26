using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc.TagHelpers
{
    /// <summary>
    /// When <c>ViewData.Contains(asp-viewdata-key)</c> or <c>User.IsInRoles(asp-in-roles)</c> is not satisfied, suppress the tag output.
    /// </summary>
    [HtmlTargetElement(Attributes = ViewDataKey)]
    [HtmlTargetElement(Attributes = InRolesKey)]
    [HtmlTargetElement(Attributes = ElseViewDataKey)]
    [HtmlTargetElement(Attributes = ElseInRolesKey)]
    [HtmlTargetElement(Attributes = ConditionKey)]
    public class DisplayWhenTagHelper : TagHelper
    {
        private const string ViewDataKey = "asp-viewdata-key";
        private const string InRolesKey = "asp-in-roles";
        private const string ElseViewDataKey = "asp-no-viewdata-key";
        private const string ElseInRolesKey = "asp-not-in-roles";
        private const string ConditionKey = "asp-show-if";

        public override int Order => -10000;

#pragma warning disable CS8618
        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViewContext { get; set; }
#pragma warning restore CS8618

        /// <summary>
        /// The required ViewData keys
        /// </summary>
        [HtmlAttributeName(ElseViewDataKey)]
        public string? ElseKey { get; set; }

        /// <summary>
        /// The required user roles
        /// </summary>
        [HtmlAttributeName(ElseInRolesKey)]
        public string? ElseRoles { get; set; }

        /// <summary>
        /// The required ViewData keys
        /// </summary>
        [HtmlAttributeName(ViewDataKey)]
        public string? Key { get; set; }

        /// <summary>
        /// The required user roles
        /// </summary>
        [HtmlAttributeName(InRolesKey)]
        public string? Roles { get; set; }

        /// <summary>
        /// The display requirement
        /// </summary>
        [HtmlAttributeName(ConditionKey)]
        public bool ShowIf { get; set; } = true;

        /// <summary>
        /// Is the output suppressed
        /// </summary>
        [HtmlAttributeNotBound]
        private bool Suppressed { get; set; }

        public override void Init(TagHelperContext context)
        {
            base.Init(context);
            context.Items.Add("DisplayWhenTagHelper_" + context.UniqueId, this);
        }

        public static bool Check(TagHelperContext context)
        {
            var lst = context.Items.Values.OfType<DisplayWhenTagHelper>();
            return lst.Aggregate(false, (fst, tgh) => fst || tgh.Suppressed);
        }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            bool suppress = !ShowIf;
            if (Key != null && !ViewContext.ViewData.ContainsKey(Key))
                suppress = true;
            if (Roles != null && !ViewContext.HttpContext.User.IsInRoles(Roles))
                suppress = true;
            if (ElseKey != null && ViewContext.ViewData.ContainsKey(ElseKey))
                suppress = true;
            if (ElseRoles != null && ViewContext.HttpContext.User.IsInRoles(ElseRoles))
                suppress = true;

            if (suppress)
            {
                Suppressed = suppress;
                output.SuppressOutput();
            }
        }
    }

    /// <summary>
    /// Razor tag block without wrapping in output but in code.
    /// </summary>
    [HtmlTargetElement("razor")]
    public class EmptyWrapperTagHelper : XysTagHelper
    {
        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            base.Process(context, output);
            output.TagName = null;
        }
    }
}
