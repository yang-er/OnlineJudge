using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace idunno.Authentication.Basic
{
    public class BasicAuthenticationValidator<TUser, TRole, TKey, TContext>
        : BasicAuthenticationEvents
        where TUser : IdentityUser<TKey>
        where TRole : IdentityRole<TKey>
        where TKey : IEquatable<TKey>
        where TContext : IdentityDbContext<TUser, TRole, TKey>
    {
        public BasicAuthenticationValidator()
        {
            OnValidateCredentials = ValidateAsync;
        }

        private readonly static IMemoryCache _cache =
            new MemoryCache(new MemoryCacheOptions()
            {
                Clock = new Microsoft.Extensions.Internal.SystemClock()
            });

        private readonly static AsyncLock _locker = new AsyncLock();

        private static async Task ValidateAsync(ValidateCredentialsContext context)
        {
            var dbContext = context.HttpContext.RequestServices
                .GetRequiredService<TContext>();
            var normusername = context.Username.ToUpper();

            var user = await _cache.GetOrCreateAsync("`" + normusername.ToLower(), async entry =>
            {
                var value = await dbContext.Users
                    .Where(u => u.NormalizedUserName == normusername)
                    .Select(u => new { u.Id, u.UserName, u.PasswordHash, u.SecurityStamp })
                    .FirstOrDefaultAsync();
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                return value;
            });

            if (user == null)
            {
                context.Fail("User not found.");
                return;
            }

            var passwordHasher = context.HttpContext.RequestServices
                .GetRequiredService<IPasswordHasher<TUser>>();

            var attempt = passwordHasher.VerifyHashedPassword(
                user: default, // assert that hasher don't need TUser
                hashedPassword: user.PasswordHash,
                providedPassword: context.Password);

            if (attempt == PasswordVerificationResult.Failed)
            {
                context.Fail("Login failed, password not match.");
                return;
            }

            if (!_cache.TryGetValue(normusername, out ClaimsPrincipal principal))
            {
                using (await _locker.LockAsync())
                {
                    if (!_cache.TryGetValue(normusername, out principal))
                    {
                        var uid = user.Id;
                        var ur = await dbContext.UserRoles
                            .Where(r => r.UserId.Equals(uid))
                            .Join(dbContext.Roles, r => r.RoleId, r => r.Id, (_, r) => r.Name)
                            .ToListAsync();

                        var options = context.HttpContext.RequestServices
                            .GetRequiredService<IOptions<IdentityOptions>>().Value;

                        // REVIEW: Used to match Application scheme
                        var id = new ClaimsIdentity("Identity.Application",
                            options.ClaimsIdentity.UserNameClaimType,
                            options.ClaimsIdentity.RoleClaimType);
                        id.AddClaim(new Claim(options.ClaimsIdentity.UserIdClaimType, $"{user.Id}"));
                        id.AddClaim(new Claim(options.ClaimsIdentity.UserNameClaimType, user.UserName));
                        id.AddClaim(new Claim(options.ClaimsIdentity.SecurityStampClaimType, user.SecurityStamp));
                        foreach (var roleName in ur)
                            id.AddClaim(new Claim(options.ClaimsIdentity.RoleClaimType, roleName));
                        principal = new ClaimsPrincipal(id);

                        _cache.Set(normusername, principal, TimeSpan.FromMinutes(20));
                    }
                }
            }

            context.Principal = principal;
            context.Success();
        }
    }
}
