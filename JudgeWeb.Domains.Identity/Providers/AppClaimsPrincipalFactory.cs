using JudgeWeb.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Identity.Providers
{
    public class UserWithNickNameClaimsPrincipalFactory<TContext> :
        UserClaimsPrincipalFactory<User>
        where TContext : IdentityDbContext<User, Role, int>
    {
        public IdentityDbContext<User, Role, int> Identity { get; }

        public UserWithNickNameClaimsPrincipalFactory(
            UserManager<User> userManager,
            TContext identityDbContext,
            IOptions<IdentityOptions> options) :
            base(userManager, options)
        {
            Identity = identityDbContext;
        }

        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(User user)
        {
            var content = await base.GenerateClaimsAsync(user);

            // 利用 RoleManager 一个一个查太费性能了

            var roleQuery =
                from ur in Identity.UserRoles
                where ur.UserId == user.Id
                join r in Identity.Roles on ur.RoleId equals r.Id
                select r.Name;

            var roles = await roleQuery.ToListAsync();
            content.AddClaims(roles.Select(roleName => new Claim(Options.ClaimsIdentity.RoleClaimType, roleName)));

            var roleClaimsQuery =
                from ur in Identity.UserRoles
                where ur.UserId == user.Id
                join r in Identity.RoleClaims on ur.RoleId equals r.RoleId                
                select new { r.ClaimType, r.ClaimValue };

            var roleClaims = await roleClaimsQuery.Distinct().ToListAsync();
            content.AddClaims(roleClaims.Select(r => new Claim(r.ClaimType, r.ClaimValue)));

            if (!string.IsNullOrWhiteSpace(user.NickName))
                content.AddClaim(new Claim("nickname", user.NickName));

            return content;
        }
    }
}
