using Microsoft.AspNetCore.Razor.TagHelpers;
using System;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.AspNetCore.Mvc.TagHelpers
{
    /// <summary>
    /// Setting <c>src</c> to the corresponding gravatar logo.
    /// </summary>
    [HtmlTargetElement("img", Attributes = "gravatar-email")]
    public class GravatarTagHelper : XysTagHelper
    {
        [HtmlAttributeName("gravatar-email")]
        public string? GravatarEmail { get; set; }

        private static readonly char[] _chars = "0123456789abcdef".ToCharArray();

        private static string ToMd5(string? source)
        {
            if (source == null) return new string('0', 32);
            using var md5 = MD5.Create();
            var md5Result = md5.ComputeHash(Encoding.ASCII.GetBytes(source));
            Span<char> buf = stackalloc char[32];
            int i = 0;

            foreach (var ch in md5Result)
            {
                buf[i++] = _chars[(ch & 0x0f0) >> 4];
                buf[i++] = _chars[ch & 0x0f];
            }

            return new string(buf);
        }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            base.Process(context, output);
            var url = new StringBuilder("//www.gravatar.com/avatar/", 90);
            url.Append(ToMd5(GravatarEmail?.Trim().ToLower()));
            url.Append("?u=monsterid&s=256");
            output.Attributes.Add("src", url.ToString());
        }
    }
}
