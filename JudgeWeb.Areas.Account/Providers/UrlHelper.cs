namespace Microsoft.AspNetCore.Mvc
{
    public static class UrlHelperExtensions
    {
        public static string EmailConfirmationLink(this IUrlHelper urlHelper, string userId, string code, string scheme)
        {
            return urlHelper.Action(
                action: "ConfirmEmail",
                controller: "Sign",
                values: new { userId, code, area = "Account" },
                protocol: scheme);
        }

        public static string Email2ConfirmationLink(this IUrlHelper urlHelper, string userId, string code, string scheme)
        {
            return urlHelper.Action(
                action: "ConfirmEmail2",
                controller: "Sign",
                values: new { userId, code, area = "Account" },
                protocol: scheme);
        }

        public static string ResetPasswordCallbackLink(this IUrlHelper urlHelper, string userId, string code, string scheme)
        {
            return urlHelper.Action(
                action: "ResetPassword",
                controller: "Sign",
                values: new { userId, code, area = "Account" },
                protocol: scheme);
        }
    }
}
