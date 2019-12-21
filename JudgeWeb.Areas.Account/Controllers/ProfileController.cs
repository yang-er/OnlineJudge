using JudgeWeb.Areas.Account.Models;
using JudgeWeb.Data;
using JudgeWeb.Features.Mailing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Account.Controllers
{
    [Area("Account")]
    [Authorize]
    [Route("[controller]")]
    public class ProfileController : Controller2
    {
        const string _studentMailSuffix = "@mails.jlu.edu.cn";

        UserManager UserManager { get; }
        SignInManager<User> SignInManager { get; }
        ILogger<ProfileController> Logger { get; }
        IEmailSender EmailSender { get; }

        public ProfileController(
            UserManager um, SignInManager<User> sim,
            ILogger<ProfileController> logger,
            IEmailSender emailSender)
        {
            UserManager = um;
            SignInManager = sim;
            Logger = logger;
            EmailSender = emailSender;
        }

        private IActionResult ViewWithError(IdentityResult result, object model)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        private async Task<User> GetUserAsync()
        {
            var user = await UserManager.GetUserAsync(User);
            var userId = UserManager.GetUserId(User);
            ViewBag.User = user;
            return user ?? throw new ApplicationException(
                $"Unable to load user with ID '{userId}'.");
        }

        [TempData]
        public string StatusMessage { get; set; }


        [HttpGet("{username}")]
        [AllowAnonymous]
        public async Task<IActionResult> Show(string username,
            [FromServices] SubmissionManager subMgr)
        {
            var user = await UserManager.FindByNameAsync(username);
            if (user is null) return NotFound();
            ViewBag.User = user;
            ViewBag.Stat = await subMgr.StatisticsByUserAsync(user.Id);
            return View();
        }


        [HttpGet("{username}/[action]")]
        public async Task<IActionResult> Edit(string username)
        {
            var user = await GetUserAsync();
            if (user.NormalizedUserName != username.ToUpper())
                return NotFound();

            var model = new IndexViewModel
            {
                Username = user.UserName,
                NickName = user.NickName,
                Email = user.Email,
                IsEmailConfirmed = user.EmailConfirmed,
                Plan = user.Plan,
            };

            return View(model);
        }


        [HttpGet("{username}/[action]")]
        public async Task<IActionResult> ChangePassword(string username)
        {
            var user = await GetUserAsync();
            if (user.NormalizedUserName != username.ToUpper())
                return NotFound();

            var hasPassword = await UserManager.HasPasswordAsync(user);
            if (!hasPassword)
                return RedirectToAction(nameof(SetPassword));
            
            return View(new ChangePasswordModel());
        }


        [HttpPost("{username}/[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string username, ChangePasswordModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await GetUserAsync();
            if (user.NormalizedUserName != username.ToUpper())
                return NotFound();

            var changePasswordResult = await UserManager
                .ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (!changePasswordResult.Succeeded)
                return ViewWithError(changePasswordResult, model);
            
            await SignInManager.SignInAsync(user, isPersistent: false);
            Logger.LogInformation("User changed their password successfully.");
            StatusMessage = "Your password has been changed.";

            return RedirectToAction(nameof(ChangePassword));
        }


        [HttpGet("{username}/[action]")]
        public async Task<IActionResult> SetPassword(string username)
        {
            var user = await GetUserAsync();
            if (user.NormalizedUserName != username.ToUpper())
                return NotFound();

            var hasPassword = await UserManager.HasPasswordAsync(user);
            if (hasPassword)
                return RedirectToAction(nameof(ChangePassword));
            
            return View(new SetPasswordModel());
        }


        [HttpPost("{username}/[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPassword(string username, SetPasswordModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await GetUserAsync();
            if (user.NormalizedUserName != username.ToUpper())
                return NotFound();

            var addPasswordResult = await UserManager
                .AddPasswordAsync(user, model.NewPassword);
            if (!addPasswordResult.Succeeded)
                return ViewWithError(addPasswordResult, model);

            await SignInManager.SignInAsync(user, isPersistent: false);
            StatusMessage = "Your password has been set.";
            return RedirectToAction(nameof(SetPassword));
        }


        [HttpPost("{username}/[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendVerificationEmail(string username, IndexViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await GetUserAsync();
            if (user.NormalizedUserName != username.ToUpper())
                return NotFound();

            var code = await UserManager.GenerateEmailConfirmationTokenAsync(user);
            var callbackUrl = Url.EmailConfirmationLink(user.Id.ToString(), code, Request.Scheme);

            try
            {
                await EmailSender.SendEmailConfirmationAsync(user.Email, callbackUrl);
                StatusMessage = "Verification email sent. Please check your email.";
            }
            catch (Exception ex)
            {
                StatusMessage = "Error sending mails: " + ex.Message;
                if (user.Email.EndsWith("@qq.com"))
                    StatusMessage += "\nQQ mail is hard to deal with..";
            }

            return RedirectToAction(nameof(Edit));
        }


        [HttpGet("{username}/[action]")]
        public async Task<IActionResult> StudentVerify(string username)
        {
            var user = await GetUserAsync();
            if (user.NormalizedUserName != username.ToUpper())
                return NotFound();

            Student student = null;
            if (user.StudentId.HasValue)
                student = await UserManager.Students
                    .Where(s => s.Id == user.StudentId.Value)
                    .SingleOrDefaultAsync();

            return View(new StudentVerifyModel
            {
                StudentName = student?.Name,
                StudentId = student?.Id ?? 0,
                Email = user.StudentEmail,
                IsEmailConfirmed = user.StudentVerified
            });
        }


        [HttpPost("{username}/[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StudentVerify(string username, StudentVerifyModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await GetUserAsync();
            if (user.NormalizedUserName != username.ToUpper())
                return NotFound();

            if (user.StudentVerified)
            {
                StatusMessage = "You have already been verified.";
                return RedirectToAction(nameof(StudentVerify));
            }

            if (string.IsNullOrEmpty(model.Email))
            {
                user.StudentId = null;
                user.StudentEmail = null;
                await UserManager.UpdateAsync(user);
                StatusMessage = "Previous verification cleared.";
                return RedirectToAction(nameof(StudentVerify));
            }

            if (!model.Email.EndsWith(_studentMailSuffix))
            {
                ModelState.AddModelError("XYS.EmailType", "Your email address is not a student mail of JLU.");
                return View(model);
            }

            var studId = model.StudentId;
            var student = await UserManager.Students
                .Where(s => s.Id == studId)
                .SingleOrDefaultAsync();
            
            if (student == null || student.Name != model.StudentName.Trim())
            {
                ModelState.AddModelError("XYS.StudentInfo", "Your name or ID is invalid. If you believe this is a mistake, please contact us.");
                return View(model);
            }

            user.StudentEmail = model.Email;
            user.StudentId = model.StudentId;
            await UserManager.UpdateAsync(user);
            return RedirectToAction(nameof(StudentVerify));
        }


        [HttpPost("{username}/[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendStudentEmail(string username)
        {
            var user = await GetUserAsync();
            if (user.NormalizedUserName != username.ToUpper())
                return NotFound();

            if (!user.StudentId.HasValue)
            {
                StatusMessage = "Error no student email set.";
                return RedirectToAction(nameof(StudentVerify));
            }

            var code = await UserManager.GenerateEmail2ConfirmationTokenAsync(user);
            var callbackUrl = Url.Email2ConfirmationLink(user.Id.ToString(), code, Request.Scheme);
            await EmailSender.SendEmailConfirmationAsync(user.StudentEmail, callbackUrl);

            StatusMessage = "Verification email sent. Please check your email.";
            return RedirectToAction(nameof(StudentVerify));
        }


        [HttpPost("{username}/[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string username, IndexViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                var user = await GetUserAsync();
                if (user.NormalizedUserName != username.ToUpper())
                    return NotFound();

                if (string.IsNullOrEmpty(model.NickName))
                    model.NickName = null;
                if (string.IsNullOrEmpty(model.Plan))
                    model.Plan = null;

                if (user.NickName != model.NickName)
                    user.NickName = model.NickName;
                if (user.Plan != model.Plan)
                    user.Plan = model.Plan;

                var email = user.Email;
                if (model.Email != email)
                {
                    var setEmailResult = await UserManager.SetEmailAsync(user, model.Email);
                    if (!setEmailResult.Succeeded)
                    {
                        throw new ApplicationException(
                            $"Unexpected error occurred setting email for user with ID '{user.Id}'. "
                            + (setEmailResult.Errors.FirstOrDefault()?.Description ?? ""));
                    }
                }

                await UserManager.UpdateAsync(user);
                await SignInManager.RefreshSignInAsync(user);
                StatusMessage = "Your profile has been updated";
            }
            catch (ApplicationException ex)
            {
                StatusMessage = ex.Message;
            }

            return RedirectToAction(nameof(Edit));
        }
    }
}
