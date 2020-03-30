using JudgeWeb.Areas.Contest.Models;
using JudgeWeb.Data;
using JudgeWeb.Domains.Contests;
using JudgeWeb.Domains.Identity;
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
    [Route("[area]/{cid}/jury/[controller]")]
    [AuditPoint(AuditlogType.Team)]
    public class TeamsController : JuryControllerBase
    {
        ITeamStore Store => Facade.Teams;


        [HttpGet]
        public async Task<IActionResult> List(int cid)
        {
            var model = await Store.ListAsync(cid,
                cacheTag: ($"`c{cid}`teams`list_jury", TimeSpan.FromSeconds(10)),
                selector: t => new JuryListTeamModel
                {
                    Affiliation = t.Affiliation.ExternalId,
                    AffiliationName = t.Affiliation.FormalName,
                    Category = t.Category.Name,
                    Status = t.Status,
                    TeamId = t.TeamId,
                    TeamName = t.TeamName,
                    RegisterTime = t.RegisterTime,
                });

            return View(model);
        }


        [HttpGet("{teamid}")]
        public async Task<IActionResult> Detail(int cid, int teamid)
        {
            ViewBag.Submissions = await ListSubmissionsByJuryAsync(cid, teamid);
            var model = await FindScoreboardAsync(teamid);
            if (model == null) return NotFound();

            var allMembers = await Store.ListMembersAsync(cid);
            ViewBag.Member = Enumerable.Empty<string>();
            if (allMembers.Contains(teamid))
                ViewBag.Member = allMembers[teamid];
            return View(model);
        }


        [HttpGet("[action]")]
        [ValidateInAjax]
        public async Task<IActionResult> Add(int cid)
        {
            ViewBag.Aff = await Store.ListAffiliationAsync(cid, false);
            ViewBag.Cat = await Store.ListCategoryAsync(cid);
            return Window(new JuryAddTeamModel());
        }


        [HttpPost("[action]")]
        [ValidateInAjax]
        [ValidateAntiForgeryToken]
        [AuditPoint(AuditlogType.Team)]
        public async Task<IActionResult> Add(
            int cid, JuryAddTeamModel model,
            [FromServices] UserManager userManager)
        {
            HashSet<int> users = new HashSet<int>();
            if (model.UserName != null)
            {
                var userNames = model.UserName.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var userName in userNames)
                {
                    var user = await userManager.FindByNameAsync(userName.Trim());
                    if (user == null)
                        ModelState.AddModelError("xys::no_user", $"No such user {userName.Trim()}.");
                    else if ((await Store.FindByUserAsync(cid, user.Id)) != null)
                        ModelState.AddModelError("xys::duplicate_user", "Duplicate user.");
                    else if (users.Contains(user.Id))
                        continue;
                    else
                        users.Add(user.Id);
                }
            }

            var affs = await Store.ListAffiliationAsync(cid, false);
            var aff = affs.FirstOrDefault(a => a.AffiliationId == model.AffiliationId);
            if (aff == null)
                ModelState.AddModelError("xys::no_aff", "No such affiliation.");

            if (!ModelState.IsValid)
            {
                ViewBag.Aff = affs;
                ViewBag.Cat = await Store.ListCategoryAsync(cid);
                return Window(model);
            }

            var teamid = await Store.CreateAsync(
                uids: users.Count > 0 ? users.ToArray() : null,
                team: new Team
                {
                    AffiliationId = model.AffiliationId,
                    Status = 1,
                    CategoryId = model.CategoryId,
                    ContestId = Contest.ContestId,
                    TeamName = model.TeamName,
                });

            await HttpContext.AuditAsync("added", $"{teamid}");
            return Message(
                title: "Add team",
                message: $"Team {model.TeamName} (t{teamid}) added.",
                type: MessageType.Success);
        }


        [HttpGet("{teamid}/[action]")]
        [ValidateInAjax]
        public async Task<IActionResult> Delete(int cid, int teamid)
        {
            var team = await Store.FindByIdAsync(cid, teamid);
            if (team == null) return NotFound();

            return AskPost(
                title: $"Delete team t{team.TeamId}",
                message: $"You are about to delete {team.TeamName} (t{team.TeamId}). Are you sure?",
                area: "Contest", ctrl: "Teams", act: "Delete", type: MessageType.Danger,
                routeValues: new { cid, teamid });
        }


        [HttpPost("{teamid}/[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int cid, int teamid, bool @checked = true)
        {
            var team = await Store.FindByIdAsync(cid, teamid);
            if (team is null || team.Status == 3) return NotFound();

            var users = await Store.DeleteAsync(team);

            string extra = null;
            if (users.Any())
                extra = "with u" + string.Join(",u", users.Select(it => it.ToString()));
            await HttpContext.AuditAsync("deleted", $"{team.TeamId}", extra);
            StatusMessage = $"Team t{teamid} deleted.";
            return RedirectToAction(nameof(List));
        }


        [HttpGet("{teamid}/[action]")]
        [ValidateInAjax]
        public async Task<IActionResult> Edit(int cid, int teamid)
        {
            var team = await Store.FindByIdAsync(cid, teamid);
            if (team == null) return NotFound();
            ViewBag.Aff = await Store.ListAffiliationAsync(cid, false);
            ViewBag.Cat = await Store.ListCategoryAsync(cid);

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
        public async Task<IActionResult> Edit(int cid, int teamid, JuryEditTeamModel model)
        {
            var team = await Store.FindByIdAsync(cid, teamid);
            if (team == null) return NotFound();

            team.TeamName = model.TeamName;
            team.AffiliationId = model.AffiliationId;
            team.CategoryId = model.CategoryId;
            await Store.UpdateAsync(team);
            await HttpContext.AuditAsync("updated", $"{teamid}");

            return Message(
                title: "Edit team",
                message: $"Team {team.TeamName} (t{teamid}) updated.",
                type: MessageType.Success);
        }


        [HttpGet("{teamid}/[action]")]
        [ValidateInAjax]
        public async Task<IActionResult> Accept(int cid, int teamid)
        {
            var team = await Store.FindByIdAsync(cid, teamid);
            if (team == null) return NotFound();
            team.Status = 1;
            await Store.UpdateAsync(team);
            await HttpContext.AuditAsync("accepted", $"{teamid}");

            return Message(
                title: "Team registration confirm",
                message: $"Team #{teamid} is now accepted.",
                type: MessageType.Success);
        }


        [HttpGet("{teamid}/[action]")]
        [ValidateInAjax]
        public async Task<IActionResult> Reject(int cid, int teamid)
        {
            var team = await Store.FindByIdAsync(cid, teamid);
            if (team == null) return NotFound();
            team.Status = 2;
            await Store.UpdateAsync(team);
            await HttpContext.AuditAsync("rejected", $"{teamid}");

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
