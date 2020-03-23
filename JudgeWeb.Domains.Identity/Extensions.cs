using JudgeWeb.Domains.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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

        public static IEndpointConventionBuilder RequireRoles(this IEndpointConventionBuilder builder, string roles)
        {
            return builder.RequireAuthorization(new AuthorizeAttribute { Roles = roles });
        }

        public static IdentityBuilder Fuck<TContext>(this IdentityBuilder builder) where TContext : DbContext
        {
            builder.Services.AddScoped<INewsStore, EntityFrameworkCoreNewsStore<TContext>>();
            builder.Services.AddScoped<IStudentStore, EntityFrameworkCoreStudentStore<TContext>>();
            builder.Services.AddScoped<ITeamManager, EntityFrameworkCoreTeamManager<TContext>>();
            return builder;
        }
    }
}
