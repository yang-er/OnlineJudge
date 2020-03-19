using JudgeWeb.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Identity.Providers
{
    public class UserWithNickNameClaimsPrincipalFactory : UserClaimsPrincipalFactory<User, Role>
    {
        public UserWithNickNameClaimsPrincipalFactory(
            UserManager<User> userManager,
            RoleManager<Role> roleManager,
            IOptions<IdentityOptions> options)
            : base(userManager, roleManager, options) { }

        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(User user)
        {
            var content = await base.GenerateClaimsAsync(user);
            if (!string.IsNullOrWhiteSpace(user.NickName))
                content.AddClaim(new Claim("XYS.NickName", user.NickName));
            return content;
        }
    }
}
