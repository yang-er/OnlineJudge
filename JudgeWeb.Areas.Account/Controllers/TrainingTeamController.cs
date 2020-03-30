using JudgeWeb.Data;
using JudgeWeb.Domains.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Account.Controllers
{
    [Area("Account")]
    [Authorize]
    [Route("teams")]
    public class TrainingTeamController : Controller2
    {
        UserManager UserManager { get; }

        ITrainingStore TeamManager { get; }

        IAffiliationStore Affiliations { get; }

        User User2 { get; set; }

        public TrainingTeamController(
            UserManager um,
            ITrainingStore teamStore,
            IAffiliationStore affs)
        {
            UserManager = um;
            TeamManager = teamStore;
            Affiliations = affs;
        }

        public override async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
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
            return View(await TeamManager.ListAsync(User2.Id));
        }


        [HttpGet("{teamid}")]
        public async Task<IActionResult> Detail(int teamid)
        {
            var team = await TeamManager.FindTeamByIdAsync(teamid);
            if (team == null) return NotFound();
            ViewBag.Affil = team.Affiliation;
            ViewBag.Users = await TeamManager.ListMembersAsync(team);
            return View(team);
        }


        [HttpGet("{teamid}/[action]")]
        public async Task<IActionResult> Edit(int teamid)
        {
            var team = await TeamManager.FindTeamByIdAsync(teamid);
            if (team == null || team.UserId != User2.Id) return NotFound();
            ViewBag.Affils = await Affiliations.ListAsync();
            ViewBag.Users = await TeamManager.ListMembersAsync(team);
            return View(team);
        }


        [HttpPost("{teamid}/[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int teamid, TrainingTeam model)
        {
            var team = await TeamManager.FindTeamByIdAsync(teamid);
            if (team == null || team.UserId != User2.Id) return NotFound();

            var aff = await Affiliations.FindAsync(model.AffiliationId);
            if (null == aff)
            {
                StatusMessage = "Error affiliation not found.";
                return RedirectToAction(nameof(Edit));
            }

            if (string.IsNullOrEmpty(model.TeamName))
            {
                StatusMessage = "Error team name is empty.";
                return RedirectToAction(nameof(Edit));
            }

            team.Affiliation = aff;
            team.AffiliationId = aff.AffiliationId;
            team.TeamName = model.TeamName;
            await TeamManager.UpdateAsync(team);
            StatusMessage = "Team info updated.";
            return RedirectToAction(nameof(Edit));
        }


        [HttpGet("[action]")]
        [ValidateInAjax]
        public async Task<IActionResult> Create()
        {
            if (!await TeamManager.CheckCreateAsync(User2))
                return Message("Create team", "Team count limit exceeded.", MessageType.Danger);
            ViewBag.Affils = await Affiliations.ListAsync();
            return Window();
        }


        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [FromForm, Required] string teamName,
            [FromForm] int affilId)
        {
            if (null == await Affiliations.FindAsync(affilId))
            {
                StatusMessage = "Error no such affiliation.";
                return RedirectToAction(nameof(List));
            }

            if (!await TeamManager.CheckCreateAsync(User2))
            {
                StatusMessage = "Error max team count exceeded.";
                return RedirectToAction(nameof(List));
            }

            var teamid = await TeamManager.CreateTeamAsync(teamName, User2, affilId);
            return RedirectToAction(nameof(Detail), new { teamid });
        }


        [HttpPost("{teamid}/[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Invite(
            int teamid,
            [FromForm, Required] string username)
        {
            var team = await TeamManager.FindTeamByIdAsync(teamid);
            if (team == null || team.UserId != User2.Id) return NotFound();

            var user = await UserManager.FindByNameAsync(username);

            if (user == null)
            {
                StatusMessage = "Error user not found.";
                return RedirectToAction(nameof(Edit));
            }

            if (!await TeamManager.CheckCreateAsync(team))
            {
                StatusMessage = "Error team member count limitation exceeded.";
                return RedirectToAction(nameof(Edit));
            }

            await TeamManager.AddTeamMemberAsync(team, user);
            StatusMessage = "Invitition sent. The invitee should open this team page to accept your invitation.";

            return RedirectToAction(nameof(Edit));
        }


        [HttpPost("{teamid}/[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Accept(int teamid)
        {
            var team = await TeamManager.FindTeamByIdAsync(teamid);
            if (team == null || team.UserId == User2.Id) return NotFound();
            var user = await TeamManager.IsInTeamAsync(User2, team);
            if (user == null) return NotFound();

            user.Accepted = true;
            await TeamManager.UpdateAsync(user);
            StatusMessage = "Team invitation accepted.";
            return RedirectToAction(nameof(Detail));
        }


        [HttpPost("{teamid}/[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int teamid)
        {
            var team = await TeamManager.FindTeamByIdAsync(teamid);
            if (team == null || team.UserId == User2.Id) return NotFound();
            var user = await TeamManager.IsInTeamAsync(User2, team);
            if (user == null) return NotFound();

            user.Accepted = false;
            await TeamManager.UpdateAsync(user);
            StatusMessage = "Team invitation rejected.";
            return RedirectToAction(nameof(Detail));
        }


        [HttpGet("{teamid}/[action]")]
        [ValidateInAjax]
        public async Task<IActionResult> Dismiss(int teamid)
        {
            var team = await TeamManager.FindTeamByIdAsync(teamid);
            if (team == null || team.UserId != User2.Id) return NotFound();
            return AskPost(
                title: "Dismiss team",
                message: $"Are you sure to dismiss team {team.TeamName}?",
                area: "Account", ctrl: "TrainingTeam", act: "Dismiss");
        }


        [HttpPost("{teamid}/[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Dismiss(int teamid, bool post = true)
        {
            var team = await TeamManager.FindTeamByIdAsync(teamid);
            if (team == null || team.UserId != User2.Id) return NotFound();
            await TeamManager.DeleteAsync(team);
            StatusMessage = "Team dismissed.";
            return RedirectToAction(nameof(List));
        }


        [HttpGet("{teamid}/[action]/{username}")]
        [ValidateInAjax]
        public async Task<IActionResult> Delete(int teamid, string username)
        {
            var team = await TeamManager.FindTeamByIdAsync(teamid);
            if (team == null || team.UserId != User2.Id) return NotFound();

            var user = await UserManager.FindByNameAsync(username);
            if (user == null) return NotFound();
            
            if (user.Id == User2.Id)
                return Message("Delete team member", "You can't remove yourself out of team.", MessageType.Warning);

            var tu = await TeamManager.IsInTeamAsync(user, team);
            if (tu == null) return NotFound();

            return AskPost(
                title: "Delete team member",
                message: $"Are you sure to remove {username} out of this team?",
                area: "Account", ctrl: "TrainingTeam", act: "Delete");
        }


        [HttpPost("{teamid}/[action]/{username}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string username, int teamid)
        {
            var team = await TeamManager.FindTeamByIdAsync(teamid);
            if (team == null || team.UserId != User2.Id) return NotFound();

            var user = await UserManager.FindByNameAsync(username);
            if (user == null) return NotFound();

            if (user.Id == User2.Id)
            {
                StatusMessage = "You can't remove yourself out of team.";
                return RedirectToAction(nameof(Edit));
            }

            var tu = await TeamManager.IsInTeamAsync(user, team);
            if (tu == null) return NotFound();
            await TeamManager.DeleteAsync(tu);
            StatusMessage = "Team member deleted.";
            return RedirectToAction(nameof(Edit));
        }
    }
}
