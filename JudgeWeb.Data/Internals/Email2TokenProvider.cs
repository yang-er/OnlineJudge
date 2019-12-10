using JudgeWeb.Data;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Identity
{
    public class Email2TokenProvider : TotpSecurityStampBasedTokenProvider<User>
    {
        public static TokenProviderDescriptor Descriptor { get; }
            = new TokenProviderDescriptor(typeof(Email2TokenProvider))
            {
                ProviderInstance = new Email2TokenProvider()
            };

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
