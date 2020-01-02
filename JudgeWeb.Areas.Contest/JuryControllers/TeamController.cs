using JudgeWeb.Areas.Contest.Models;
using JudgeWeb.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Contest.Controllers
{
    [Area("Contest")]
    [Authorize]
    [Route("[area]/{cid}/jury/teams")]
    public class JuryTeamController : JuryControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> List(int cid)
        {
            var query =
                from t in Service.Teams
                where t.ContestId == cid && t.Status != 3
                join a in Service.TeamAffiliations on t.AffiliationId equals a.AffiliationId
                join c in Service.TeamCategories on t.CategoryId equals c.CategoryId
                join u in UserManager.Users on t.UserId equals u.Id into uu
                from u in uu.DefaultIfEmpty()
                select new JuryListTeamModel
                {
                    Affiliation = a.ExternalId,
                    AffiliationName = a.FormalName,
                    Category = c.Name,
                    UserName = u.UserName,
                    Status = t.Status,
                    TeamId = t.TeamId,
                    TeamName = t.TeamName,
                    RegisterTime = t.RegisterTime,
                };

            var model = await query.CachedToListAsync(
                tag: $"`c{cid}`teams`list_jury",
                timeSpan: TimeSpan.FromSeconds(10));
            return View(model);
        }


        [HttpGet("{teamid}")]
        public async Task<IActionResult> Detail(int cid, int teamid)
        {
            ViewBag.Submissions = await Service.ListSubmissionsByJuryAsync(cid, teamid);
            return View(await Service.FindScoreboardAsync(cid, teamid));
        }


        [HttpGet("[action]/{userName?}")]
        public async Task<IActionResult> TestUser(int cid, string userName)
        {
            if (userName != null)
            {
                var user = await UserManager.FindByNameAsync(userName);
                if (user == null)
                    return Content("<span color=\"red\">No such user.</span>", "text/html");
                else if ((await Service.FindTeamByUserAsync(cid, user.Id)) != null)
                    return Content("<span color=\"red\">Duplicate user.</span>", "text/html");
                return Content("", "text/html");
            }
            else
            {
                return Content("", "text/html");
            }
        }


        [HttpGet("[action]")]
        [ValidateInAjax]
        public async Task<IActionResult> Add(int cid)
        {
            ViewBag.Aff = await Service.ListTeamAffiliationAsync(cid, false);
            ViewBag.Cat = await Service.ListTeamCategoryAsync(cid);
            return Window(new JuryAddTeamModel());
        }


        [HttpPost("[action]")]
        [ValidateInAjax]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int cid, JuryAddTeamModel model)
        {
            User user = null;
            if (model.UserName != null)
            {
                user = await UserManager.FindByNameAsync(model.UserName);
                if (user == null)
                    ModelState.AddModelError("xys::no_user", "No such user.");
                else if ((await Service.FindTeamByUserAsync(cid, user.Id)) != null)
                    ModelState.AddModelError("xys::duplicate_user", "Duplicate user.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Aff = await Service.ListTeamAffiliationAsync(cid, false);
                ViewBag.Cat = await Service.ListTeamCategoryAsync(cid);
                return Window(model);
            }

            var teamid = await Service.CreateTeamAsync(new Team
            {
                AffiliationId = model.AffiliationId,
                Status = 1,
                CategoryId = model.CategoryId,
                ContestId = Contest.ContestId,
                TeamName = model.TeamName,
                UserId = user?.Id ?? 0,
            });

            return Message(
                title: "Add team",
                message: $"Team {model.TeamName} (t{teamid}) added.",
                type: MessageType.Success);
        }


        [HttpGet("{teamid}/[action]")]
        [ValidateInAjax]
        public async Task<IActionResult> Delete(int cid, int teamid)
        {
            var team = await Service.FindTeamByIdAsync(cid, teamid);
            if (team == null) return NotFound();

            return AskPost(
                title: $"Delete team t{team.TeamId}",
                message: $"You are about to delete {team.TeamName} (t{team.TeamId}). Are you sure?",
                area: "Contest", ctrl: "JuryTeam", act: "Delete", type: MessageType.Danger,
                routeValues: new Dictionary<string, string> { ["cid"] = $"{cid}", ["teamid"] = $"{teamid}" });
        }


        [HttpPost("{teamid}/[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int teamid)
        {
            var team = await Service.FindTeamByIdAsync(Contest.ContestId, teamid);
            if (team is null || team.Status == 3) return NotFound();
            var oldUid = team.UserId;
            team.Status = 3;
            team.UserId = 0;
            await Service.UpdateTeamAsync(team, oldUid: oldUid,
                comment: $"deleted team {team.TeamName} (t{team.TeamId}, u{oldUid})");

            DisplayMessage = $"Team t{teamid} deleted.";
            return RedirectToAction(nameof(List));
        }


        [HttpGet("{teamid}/[action]")]
        [ValidateInAjax]
        public async Task<IActionResult> Edit(int cid, int teamid)
        {
            var team = await Service.FindTeamByIdAsync(cid, teamid);
            if (team == null) return NotFound();
            ViewBag.Aff = await Service.ListTeamAffiliationAsync(cid, false);
            ViewBag.Cat = await Service.ListTeamCategoryAsync(cid);

            return Window(new JuryEditTeamModel
            {
                AffiliationId = team.AffiliationId,
                CategoryId = team.CategoryId,
                TeamName = team.TeamName,
                TeamId = teamid,
            });
        }


        [HttpPost("{teamid}/[action]")]
        [ValidateAntiForgeryToken]
        [ValidateInAjax]
        public async Task<IActionResult> Edit(int teamid, JuryEditTeamModel model)
        {
            var team = await Service.FindTeamByIdAsync(Contest.ContestId, teamid);
            if (team == null) return NotFound();
            var log = $"edit team t{team.TeamId}";

            if (team.TeamName != model.TeamName)
            {
                log += $", {team.TeamName} -> {model.TeamName}";
                team.TeamName = model.TeamName;
            }

            if (team.AffiliationId != model.AffiliationId)
            {
                log += $", a{team.AffiliationId} -> {model.AffiliationId}";
                team.AffiliationId = model.AffiliationId;
            }

            if (team.CategoryId != model.CategoryId)
            {
                log += $", c{team.CategoryId} -> {model.CategoryId}";
                team.AffiliationId = model.CategoryId;
            }

            await Service.UpdateTeamAsync(team, log);

            return Message(
                title: "Edit team",
                message: $"Team {team.TeamName} (t{teamid}) updated.",
                type: MessageType.Success);
        }


        [HttpGet("{teamid}/[action]")]
        [ValidateInAjax]
        public async Task<IActionResult> Accept(int teamid)
        {
            var team = await Service.FindTeamByIdAsync(Contest.ContestId, teamid);
            if (team == null) return NotFound();
            team.Status = 1;
            await Service.UpdateTeamAsync(team, "accept team");

            return Message(
                title: "Team registration confirm",
                message: $"Team #{teamid} is now accepted.",
                type: MessageType.Success);
        }


        [HttpGet("{teamid}/[action]")]
        [ValidateInAjax]
        public async Task<IActionResult> Reject(int teamid)
        {
            var team = await Service.FindTeamByIdAsync(Contest.ContestId, teamid);
            if (team == null) return NotFound();
            team.Status = 2;
            await Service.UpdateTeamAsync(team, "reject team");

            return Message(
                title: "Team registration confirm",
                message: $"Team #{teamid} is now rejected.",
                type: MessageType.Success);
        }


        [HttpGet("[action]")]
        [ValidateInAjax]
        public IActionResult Import()
        {
            return Message(
                title: "Import teams",
                message: "Sorry, this module hasn't been finished.",
                type: MessageType.Secondary);
        }
    }
}
