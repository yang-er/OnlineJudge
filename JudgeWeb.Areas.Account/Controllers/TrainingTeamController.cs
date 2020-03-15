using JudgeWeb.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Account.Controllers
{
    [Area("Account")]
    [Authorize]
    [Route("teams")]
    public class TrainingTeamController : Controller2
    {
        UserManager UserManager { get; }

        User User2 { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        public TrainingTeamController(UserManager um) => UserManager = um;

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var user = await UserManager.GetUserAsync(User);
            var userId = UserManager.GetUserId(User);
            ViewBag.User = User2 = user ?? throw new ApplicationException(
                $"Unable to load user with ID '{userId}'.");
            await base.OnActionExecutionAsync(context, next);
        }


        [HttpGet]
        public async Task<IActionResult> List()
        {
            var query =
                from ttu in UserManager.TrainingTeamUsers
                where ttu.UserId == User2.Id && ttu.Accepted == true
                join t in UserManager.TrainingTeams on ttu.TrainingTeamId equals t.TrainingTeamId
                join tu in UserManager.TrainingTeamUsers on t.TrainingTeamId equals tu.TrainingTeamId
                join u in UserManager.Users on tu.UserId equals u.Id
                select new { t, tuu = new TrainingTeamUser(tu, u.UserName, u.Email) };
            var results = await query.ToListAsync();
            return View(results.GroupBy(k => k.t, v => v.tuu));
        }


        [HttpGet("{teamid}")]
        public async Task<IActionResult> Detail(int teamid)
        {
            var team = await UserManager.TrainingTeams
                .Where(tt => tt.TrainingTeamId == teamid)
                .Include(tt => tt.Affiliation)
                .SingleOrDefaultAsync();
            if (team == null) return NotFound();
            ViewBag.Affil = team.Affiliation;

            var uquery =
                from tu in UserManager.TrainingTeamUsers
                where tu.TrainingTeamId == teamid
                join u in UserManager.Users on tu.UserId equals u.Id
                select new TrainingTeamUser(tu, u.UserName, u.Email);
            var users = await uquery.ToListAsync();
            ViewBag.Users = users;
            return View(team);
        }


        [HttpGet("{teamid}/[action]")]
        public async Task<IActionResult> Edit(int teamid)
        {
            var team = await UserManager.TrainingTeams
                .Where(t => t.TrainingTeamId == teamid && t.UserId == User2.Id)
                .SingleOrDefaultAsync();
            if (team == null) return NotFound();
            ViewBag.Affils = await UserManager.TeamAffiliations.ToListAsync();

            var uquery =
                from tu in UserManager.TrainingTeamUsers
                where tu.TrainingTeamId == teamid
                join u in UserManager.Users on tu.UserId equals u.Id
                select new TrainingTeamUser(tu, u.UserName, u.Email);
            var users = await uquery.ToListAsync();
            ViewBag.Users = users;
            return View(team);
        }


        [HttpPost("{teamid}/[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int teamid, TrainingTeam model)
        {
            var team = await UserManager.TrainingTeams
                .Where(t => t.TrainingTeamId == teamid && t.UserId == User2.Id)
                .SingleOrDefaultAsync();
            if (team == null) return NotFound();
            var affcheck = await UserManager.TeamAffiliations
                .Where(a => a.AffiliationId == model.AffiliationId)
                .CountAsync();

            if (affcheck == 0)
            {
                StatusMessage = "Error affiliation not found.";
                return RedirectToAction(nameof(Edit));
            }

            if (string.IsNullOrEmpty(model.TeamName))
            {
                StatusMessage = "Error team name is empty.";
                return RedirectToAction(nameof(Edit));
            }

            await UserManager.TrainingTeams
                .Where(t => t.TrainingTeamId == teamid)
                .BatchUpdateAsync(t => new TrainingTeam { TeamName = model.TeamName, AffiliationId = model.AffiliationId });
            StatusMessage = "Team info updated.";
            return RedirectToAction(nameof(Edit));
        }


        [HttpGet("[action]")]
        [ValidateInAjax]
        public async Task<IActionResult> Create()
        {
            var team = await UserManager.TrainingTeams
                .Where(t => t.UserId == User2.Id)
                .CountAsync();
            if (team >= 10)
                return Message("Create team", "Team count limit exceeded.", MessageType.Danger);
            ViewBag.Affils = await UserManager.TeamAffiliations.ToListAsync();
            return Window();
        }


        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm, Required] string teamName, [FromForm] int affilId)
        {
            var team = await UserManager.TrainingTeams
                .Where(t => t.UserId == User2.Id)
                .CountAsync();
            if (team >= 10)
            {
                StatusMessage = "Error team count limit exceeded.";
                return RedirectToAction(nameof(List));
            }

            var aff = await UserManager.TeamAffiliations
                .Where(a => a.AffiliationId == affilId)
                .CountAsync();
            if (aff == 0)
            {
                StatusMessage = "Error team affiliation.";
                return RedirectToAction(nameof(List));
            }

            var teamid = await UserManager.CreateTeamAsync(teamName, User2, affilId);
            return RedirectToAction(nameof(Detail), new { teamid });
        }


        [HttpPost("{teamid}/[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Invite(int teamid, [FromForm, Required] string username)
        {
            var team = await UserManager.TrainingTeams
                .Where(t => t.TrainingTeamId == teamid && t.UserId == User2.Id)
                .SingleOrDefaultAsync();
            if (team == null) return NotFound();

            var user = await UserManager.FindByNameAsync(username);

            if (user == null)
            {
                StatusMessage = "Error user not found.";
                return RedirectToAction(nameof(Edit));
            }

            var users = await UserManager.TrainingTeamUsers
                .Where(tu => tu.TrainingTeamId == teamid)
                .Select(tu => tu.UserId)
                .ToListAsync();

            if (users.Count >= 5)
            {
                StatusMessage = "Error team member count limit exceeded.";
                return RedirectToAction(nameof(Edit));
            }

            if (users.Any(i => i == user.Id))
            {
                StatusMessage = "Invitee has been a team member.";
                return RedirectToAction(nameof(Edit));
            }

            await UserManager.AddTeamMemberAsync(team, user);
            StatusMessage = "Invitition sent. The invitee should open this team page to accept your invitation.";
            return RedirectToAction(nameof(Edit));
        }


        [HttpPost("{teamid}/[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Accept(int teamid)
        {
            var query =
                from tu in UserManager.TrainingTeamUsers
                where tu.UserId == User2.Id && tu.TrainingTeamId == teamid
                join t in UserManager.TrainingTeams on tu.TrainingTeamId equals t.TrainingTeamId
                where t.UserId != User2.Id
                select new { t, tu };
            var res = await query.SingleOrDefaultAsync();
            if (res == null) return NotFound();

            await UserManager.TrainingTeamUsers
                .Where(tu => tu.UserId == User2.Id && tu.TrainingTeamId == teamid)
                .BatchUpdateAsync(t => new TrainingTeamUser { Accepted = true });
            StatusMessage = "Team invitation accepted.";
            return RedirectToAction(nameof(Detail));
        }


        [HttpPost("{teamid}/[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int teamid)
        {
            var query =
                from tu in UserManager.TrainingTeamUsers
                where tu.UserId == User2.Id && tu.TrainingTeamId == teamid
                join t in UserManager.TrainingTeams on tu.TrainingTeamId equals t.TrainingTeamId
                where t.UserId != User2.Id
                select new { t, tu };
            var res = await query.SingleOrDefaultAsync();
            if (res == null) return NotFound();

            await UserManager.TrainingTeamUsers
                .Where(tu => tu.UserId == User2.Id && tu.TrainingTeamId == teamid)
                .BatchUpdateAsync(t => new TrainingTeamUser { Accepted = false });
            StatusMessage = "Team invitation rejected.";
            return RedirectToAction(nameof(Detail));
        }


        [HttpGet("{teamid}/[action]")]
        [ValidateInAjax]
        public async Task<IActionResult> Dismiss(int teamid)
        {
            var team = await UserManager.TrainingTeams
                .Where(t => t.TrainingTeamId == teamid && t.UserId == User2.Id)
                .SingleOrDefaultAsync();
            if (team == null) return NotFound();
            return AskPost(
                title: "Dismiss team",
                message: $"Are you sure to dismiss team {team.TeamName}?",
                area: "Account", ctrl: "TrainingTeam", act: "Dismiss");
        }


        [HttpPost("{teamid}/[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Dismiss(int teamid, bool post = true)
        {
            var team = await UserManager.TrainingTeams
                .Where(t => t.TrainingTeamId == teamid && t.UserId == User2.Id)
                .SingleOrDefaultAsync();
            if (team == null) return NotFound();
            await UserManager.TrainingTeams
                .Where(t => t.TrainingTeamId == teamid)
                .BatchDeleteAsync();
            StatusMessage = "Team dismissed.";
            return RedirectToAction(nameof(List));
        }


        [HttpGet("{teamid}/[action]/{username}")]
        [ValidateInAjax]
        public async Task<IActionResult> Delete(int teamid, string username)
        {
            var teamPermission = await UserManager.TrainingTeams
                .Where(t => t.TrainingTeamId == teamid && t.UserId == User2.Id)
                .CountAsync();
            if (teamPermission == 0) return NotFound();
            var user = await UserManager.FindByNameAsync(username);
            if (user == null) return NotFound();
            
            if (user.Id == User2.Id)
                return Message("Delete team member", "You can't remove yourself out of team.", MessageType.Warning);

            var teamMemberExisitence = await UserManager.TrainingTeamUsers
                .Where(tu => tu.UserId == user.Id && tu.TrainingTeamId == teamid)
                .CountAsync();
            if (teamMemberExisitence == 0) return NotFound();

            return AskPost(
                title: "Delete team member",
                message: $"Are you sure to remove {username} out of this team?",
                area: "Account", ctrl: "TrainingTeam", act: "Delete");
        }


        [HttpPost("{teamid}/[action]/{username}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string username, int teamid)
        {
            var teamPermission = await UserManager.TrainingTeams
                .Where(t => t.TrainingTeamId == teamid && t.UserId == User2.Id)
                .CountAsync();
            if (teamPermission == 0) return NotFound();
            var user = await UserManager.FindByNameAsync(username);
            if (user == null) return NotFound();

            if (user.Id == User2.Id)
            {
                StatusMessage = "You can't remove yourself out of team.";
                return RedirectToAction(nameof(Edit));
            }

            var teamMemberExisitence = await UserManager.TrainingTeamUsers
                .Where(tu => tu.UserId == user.Id && tu.TrainingTeamId == teamid)
                .BatchDeleteAsync();
            if (teamMemberExisitence == 0) return NotFound();

            StatusMessage = "Team member deleted.";
            return RedirectToAction(nameof(Edit));
        }
    }
}
