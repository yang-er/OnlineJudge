using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace JudgeWeb.Data
{
    public class UserManager : UserManager<User>
    {
        public UserManager(
            IUserStore<User> store,
            AppDbContext db,
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
            _dbContext = db;
        }

        readonly AppDbContext _dbContext;
        const string ClaimOfNickName = "XYS.NickName";
        const string Email2TokenProvider = "Email2";
        const string Email2TokenPurpose = "Email2Confirmation";

        public override bool SupportsUserTwoFactor => false;
        public override bool SupportsUserTwoFactorRecoveryCodes => false;

        public IQueryable<Student> Students => _dbContext.Students;
        public IQueryable<TeachingClass> Classes => _dbContext.Classes;
        public IQueryable<TrainingTeam> TrainingTeams => _dbContext.TrainingTeams;
        public IQueryable<TrainingTeamUser> TrainingTeamUsers => _dbContext.TrainingTeamUsers;

        public string GetNickName(ClaimsPrincipal claim)
        {
            var nickName = claim.FindFirstValue(ClaimOfNickName);
            if (string.IsNullOrEmpty(nickName)) nickName = GetUserName(claim);
            return nickName;
        }

        public Task<IdentityResult> SlideExpirationAsync(User user)
        {
            SlideExpireMemoryCache.Set(
                key: user.NormalizedUserName,
                value: DateTimeOffset.UtcNow,
                absoluteExpirationRelativeToNow: TimeSpan.FromMinutes(20));
            return Task.FromResult(IdentityResult.Success);
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
            var result = await UpdateUserAsync(user);

            if (result.Succeeded)
            {
                await AddToRoleAsync(user, "Student");
                await SlideExpirationAsync(user);
            }

            return result;
        }

        public static readonly MemoryCache SlideExpireMemoryCache =
            new MemoryCache(new MemoryCacheOptions { Clock = new Microsoft.Extensions.Internal.SystemClock() });
    }
}
