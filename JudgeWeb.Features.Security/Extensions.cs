using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.AspNetCore.Mvc
{
    public static class IdentityExtensions
    {
        public static IdentityBuilder UsePasswordHasher<THasher, TUser>(this IdentityBuilder builder)
            where THasher : class, IPasswordHasher<TUser> where TUser : class
        {
            builder.Services.Replace(ServiceDescriptor.Scoped<IPasswordHasher<TUser>, THasher>());
            return builder;
        }

        public static IdentityBuilder UseClaimsPrincipalFactory<TFactory, TUser>(this IdentityBuilder builder)
            where TFactory : class, IUserClaimsPrincipalFactory<TUser> where TUser : class
        {
            builder.Services.Replace(ServiceDescriptor.Scoped<IUserClaimsPrincipalFactory<TUser>, TFactory>());
            return builder;
        }
    }
}
