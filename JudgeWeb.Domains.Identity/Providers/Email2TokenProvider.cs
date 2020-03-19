using JudgeWeb.Data;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Identity.Providers
{
    public class Email2TokenProvider : TotpSecurityStampBasedTokenProvider<User>
    {
        public override async Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<User> manager, User user)
        {
            var email = await manager.GetEmailAsync(user);
            return !string.IsNullOrWhiteSpace(email) && await manager.IsEmailConfirmedAsync(user);
        }

        public override Task<string> GetUserModifierAsync(string purpose, UserManager<User> manager, User user)
        {
            return Task.FromResult("Email2:" + purpose + ":" + user.StudentEmail);
        }
    }
}
