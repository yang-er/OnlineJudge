using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace JudgeWeb.Data
{
    public class UserManager : UserManager<User>
    {
        public UserManager(
            IUserStore<User> store,
            IOptions<IdentityOptions> optionsAccessor,
            IPasswordHasher<User> passwordHasher,
            IEnumerable<IUserValidator<User>> userValidators,
            IEnumerable<IPasswordValidator<User>> passwordValidators,
            ILookupNormalizer keyNormalizer,
            IdentityErrorDescriber errors,
            IServiceProvider services,
            ILogger<UserManager> logger)
            : base(store,
                  optionsAccessor,
                  passwordHasher,
                  userValidators,
                  passwordValidators,
                  keyNormalizer,
                  errors,
                  services,
                  logger)
        {
        }
        
        const string ClaimOfNickName = "XYS.NickName";
        const string Email2TokenProvider = "Email2";
        const string Email2TokenPurpose = "Email2Confirmation";

        public string GetNickName(ClaimsPrincipal claim)
        {
            var nickName = claim.FindFirstValue(ClaimOfNickName);
            if (string.IsNullOrEmpty(nickName)) nickName = GetUserName(claim);
            return nickName;
        }

        public virtual Task<string> GenerateEmail2ConfirmationTokenAsync(User user)
        {
            ThrowIfDisposed();
            return GenerateUserTokenAsync(user, Email2TokenProvider, Email2TokenPurpose);
        }

        public virtual async Task<IdentityResult> ConfirmEmail2Async(User user, string token)
        {
            ThrowIfDisposed();
            if (user == null)
                throw new ArgumentNullException(nameof(user));
            if (!await VerifyUserTokenAsync(user, Email2TokenProvider, Email2TokenPurpose, token))
                return IdentityResult.Failed(ErrorDescriber.InvalidToken());
            user.StudentVerified = true;
            return await UpdateUserAsync(user);
        }
    }
}
