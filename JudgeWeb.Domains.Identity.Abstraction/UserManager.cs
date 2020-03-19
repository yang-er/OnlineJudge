using JudgeWeb.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Identity
{
    public class UserManager : UserManager<User>
    {
        public UserManager(
            IUserStore<User> store,
            IStudentStore db,
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
            studentStore = db;
        }

        readonly IStudentStore studentStore;
        const string ClaimOfNickName = "XYS.NickName";
        const string Email2TokenProvider = "Email2";
        const string Email2TokenPurpose = "Email2Confirmation";

        public override bool SupportsUserTwoFactor => false;
        public override bool SupportsUserTwoFactorRecoveryCodes => false;

        public string GetNickName(ClaimsPrincipal claim)
        {
            var nickName = claim.FindFirstValue(ClaimOfNickName);
            if (string.IsNullOrEmpty(nickName)) nickName = GetUserName(claim);
            return nickName;
        }

        private Task<IdentityResult> SlideExpirationAsync(User user)
        {
            return studentStore.SlideExpirationAsync(user);
        }

        public virtual Task<string> GenerateEmail2ConfirmationTokenAsync(User user)
        {
            ThrowIfDisposed();
            return GenerateUserTokenAsync(user, Email2TokenProvider, Email2TokenPurpose);
        }

        public override async Task<IdentityResult> AddToRoleAsync(User user, string role)
        {
            var result = await base.AddToRoleAsync(user, role);
            if (result.Succeeded) await SlideExpirationAsync(user);
            return result;
        }

        public override async Task<IdentityResult> AddToRolesAsync(User user, IEnumerable<string> roles)
        {
            var result = await base.AddToRolesAsync(user, roles);
            if (result.Succeeded) await SlideExpirationAsync(user);
            return result;
        }

        public override async Task<IdentityResult> RemoveFromRoleAsync(User user, string role)
        {
            var result = await base.RemoveFromRoleAsync(user, role);
            if (result.Succeeded) await SlideExpirationAsync(user);
            return result;
        }

        public override async Task<IdentityResult> RemoveFromRolesAsync(User user, IEnumerable<string> roles)
        {
            var result = await base.RemoveFromRolesAsync(user, roles);
            if (result.Succeeded) await SlideExpirationAsync(user);
            return result;
        }

        public virtual async Task<IdentityResult> ConfirmEmail2Async(User user, string token)
        {
            ThrowIfDisposed();
            if (user == null)
                throw new ArgumentNullException(nameof(user));
            if (!await VerifyUserTokenAsync(user, Email2TokenProvider, Email2TokenPurpose, token))
                return IdentityResult.Failed(ErrorDescriber.InvalidToken());
            user.StudentVerified = true;
            var result = await UpdateUserAsync(user);

            if (result.Succeeded)
            {
                await AddToRoleAsync(user, "Student");
                await SlideExpirationAsync(user);
            }

            return result;
        }

        public virtual Task<Student> FindStudentAsync(int sid)
        {
            return studentStore.FindStudentAsync(sid);
        }

        public virtual Task<User> FindByStudentIdAsync(int sid)
        {
            return studentStore.FindByStudentIdAsync(sid);
        }
    }
}
