using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace idunno.Authentication.Basic
{
    public class BasicAuthenticationValidator<TUser> : BasicAuthenticationEvents where TUser : class
    {
        public BasicAuthenticationValidator()
        {
            OnValidateCredentials = ValidateAsync;
        }

        private static async Task ValidateAsync(ValidateCredentialsContext context)
        {
            var userManager = context.HttpContext.RequestServices
                .GetRequiredService<UserManager<TUser>>();
            var signInManager = context.HttpContext.RequestServices
                .GetRequiredService<SignInManager<TUser>>();

            var user = await userManager.FindByNameAsync(context.Username);

            if (user == null)
            {
                context.Fail("User not found.");
                return;
            }

            var attempt = await signInManager.CheckPasswordSignInAsync(user, context.Password, false);

            if (attempt.Succeeded)
            {
                context.Principal = await signInManager.CreateUserPrincipalAsync(user);
                context.Success();
            }
            else
            {
                context.Fail("Login failed.");
            }
        }
    }
}
