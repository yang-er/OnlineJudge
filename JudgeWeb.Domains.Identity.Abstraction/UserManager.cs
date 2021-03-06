﻿using JudgeWeb.Data;
using JudgeWeb.Features.OjUpdate;
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
        const string Email2TokenProvider = "Email2";
        const string Email2TokenPurpose = "Email2Confirmation";

        public override bool SupportsUserTwoFactor => false;
        public override bool SupportsUserTwoFactorRecoveryCodes => false;

        public override string GetUserId(ClaimsPrincipal principal)
        {
            // Be careful of the old logins
            return base.GetUserId(principal)
                ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        public string GetNickName(ClaimsPrincipal claim)
        {
            var nickName = claim.FindFirstValue("nickname");
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

        public async Task<List<OjAccount>> GetRanklistAsync(int cid, int year)
        {
            var list = await studentStore.GetRanklistAsync(cid, year);
            list.Sort();
            return list;
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

        public virtual Task<User> FindByIdAsync(int uid)
        {
            return FindByIdAsync($"{uid}");
        }

        public virtual Task<List<Role>> ListRolesAsync(User user)
        {
            return studentStore.ListRolesByUserIdAsync(user.Id);
        }

        public virtual Task<(List<User>, int)> ListUsersAsync(int page, int pageCount)
        {
            return studentStore.ListUsersAsync(page, pageCount);
        }

        public virtual Task<Student> FindStudentAsync(int sid)
        {
            return studentStore.FindStudentAsync(sid);
        }

        public virtual Task<List<User>> FindByStudentIdAsync(int sid)
        {
            return studentStore.FindByStudentIdAsync(sid);
        }

        public virtual Task<List<IdentityUserRole<int>>> ListUserRolesAsync(int minUid, int maxUid)
        {
            return studentStore.ListUserRolesAsync(minUid, maxUid);
        }

        public virtual Task<Dictionary<int, Role>> ListNamedRolesAsync()
        {
            return studentStore.ListNamedRolesAsync();
        }

        public virtual Task DeleteStudentAsync(Student student)
        {
            return studentStore.DeleteAsync(student);
        }

        public virtual Task<int> MergeStudentListAsync(List<Student> students)
        {
            return studentStore.MergeStudentListAsync(students);
        }

        public Task<(IEnumerable<Student>, int)> ListStudentsAsync(int page, int pageCount)
        {
            return studentStore.ListStudentsAsync(page, pageCount);
        }
    }
}
