namespace Microsoft.AspNetCore.Razor.TagHelpers
{
    public static class TagHelperExtensions
    {
        public static void AddClass(this TagHelperAttributeList attrs, string append_class)
        {
            if (attrs.ContainsName("class")) append_class += " " + attrs["class"].Value;
            attrs.SetAttribute("class", append_class);
        }
    }
}
