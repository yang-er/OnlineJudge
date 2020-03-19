using JudgeWeb.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Identity
{
    public class SignInManager : SignInManager<User>
    {
        public SignInManager(
            UserManager<User> userManager,
            IHttpContextAccessor httpContextAccessor,
            IUserClaimsPrincipalFactory<User> userClaimsPrincipalFactory,
            IOptions<IdentityOptions> options,
            ILogger<SignInManager> logger,
            IAuthenticationSchemeProvider authenticationSchemeProvider,
            IUserConfirmation<User> userConfirmation)
            : base(userManager,
                  httpContextAccessor,
                  userClaimsPrincipalFactory,
                  options,
                  logger,
                  authenticationSchemeProvider,
                  userConfirmation)
        {
        }

        public override async Task<SignInResult> PasswordSignInAsync(string userName, string password, bool isPersistent, bool lockoutOnFailure)
        {
            User user;
            if (string.IsNullOrEmpty(userName))
                return SignInResult.Failed;
            if (userName.Contains('@'))
                user = await UserManager.FindByEmailAsync(userName);
            else
                user = await UserManager.FindByNameAsync(userName);

            if (user == null)
                return SignInResult.Failed;
            return await PasswordSignInAsync(user, password, isPersistent, lockoutOnFailure);
        }
    }
}
