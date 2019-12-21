using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Linq;
using System.Security.Claims;
using System.Text;

namespace Microsoft.AspNetCore.Mvc
{
    public static class Extensions
    {
        public static bool IsInRoles(this ClaimsPrincipal user, string roles)
        {
            return roles.Split(',').Any(role => user.IsInRole(role));
        }

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

        public static string GetErrorStrings(this ModelStateDictionary modelState)
        {
            var sb = new StringBuilder();
            foreach (var state in modelState)
            {
                if (state.Value.ValidationState != ModelValidationState.Invalid) continue;
                foreach (var item in state.Value.Errors)
                    sb.AppendLine(item.ErrorMessage);
            }

            return sb.ToString();
        }
    }
}
