using JudgeWeb.Areas.Contest.Models;
using JudgeWeb.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Contest.Controllers
{
    [Area("Contest")]
    [Authorize]
    [Route("[area]/{cid}/jury/[controller]")]
    public class TeamsController : JuryControllerBase
    {
        protected async Task UpdateTeamAsync(Team team, (string act, string extra) comment, string action, int? oldUid = null)
        {
            var aff = await DbContext.TeamAffiliations
                .Where(a => a.AffiliationId == team.AffiliationId)
                .SingleOrDefaultAsync();
            if (aff == null) throw new ApplicationException();

            DbContext.Teams.Update(team);

            if (action != null)
            {
                var ct = new Data.Api.ContestTeam(team, aff);
                DbContext.Events.Add(ct.ToEvent(action, team.ContestId));
            }

            InternalLog(AuditlogType.Team, $"{team.TeamId}", comment.act, comment.extra);
            await DbContext.SaveChangesAsync();
            Cache.Remove($"`c{team.ContestId}`teams`t{team.TeamId}");
            Cache.Remove($"`c{team.ContestId}`teams`u{oldUid ?? team.UserId}");
            Cache.Remove($"`c{team.ContestId}`teams`list_jury");
            Cache.Remove($"`c{team.ContestId}`teams`aff0");
            Cache.Remove($"`c{team.ContestId}`teams`cat`1");
            Cache.Remove($"`c{team.ContestId}`teams`cat`2");
        }


        [HttpGet]
        public async Task<IActionResult> List(int cid)
        {
            var query =
                from t in DbContext.Teams
                where t.ContestId == cid && t.Status != 3
                join a in DbContext.TeamAffiliations on t.AffiliationId equals a.AffiliationId
                join c in DbContext.TeamCategories on t.CategoryId equals c.CategoryId
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
            ViewBag.Submissions = await ListSubmissionsByJuryAsync(cid, teamid);
            var model = await FindScoreboardAsync(teamid);
            if (model == null) return NotFound();
            return View(model);
        }


        [HttpGet("[action]/{userName?}")]
        public async Task<IActionResult> TestUser(string userName)
        {
            if (userName != null)
            {
                var user = await UserManager.FindByNameAsync(userName);
                if (user == null)
                    return Content("No such user.", "text/html");
                else if ((await FindTeamByUserAsync(user.Id)) != null)
                    return Content("Duplicate user.", "text/html");
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
            ViewBag.Aff = await DbContext.ListTeamAffiliationAsync(cid, false);
            ViewBag.Cat = await DbContext.ListTeamCategoryAsync(cid);
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
                else if ((await FindTeamByUserAsync(user.Id)) != null)
                    ModelState.AddModelError("xys::duplicate_user", "Duplicate user.");
            }

            var affs = await DbContext.ListTeamAffiliationAsync(cid, false);
            var aff = affs.FirstOrDefault(a => a.AffiliationId == model.AffiliationId);
            if (aff == null)
                ModelState.AddModelError("xys::no_aff", "No such affiliation.");

            if (!ModelState.IsValid)
            {
                ViewBag.Aff = affs;
                ViewBag.Cat = await DbContext.ListTeamCategoryAsync(cid);
                return Window(model);
            }

            var teamid = await CreateTeamAsync(aff: aff, team: new Team
            {
                AffiliationId = model.AffiliationId,
                Status = 1,
                CategoryId = model.CategoryId,
                ContestId = Contest.ContestId,
                TeamName = model.TeamName,
                UserId = user?.Id,
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
            var team = await FindTeamByIdAsync(teamid);
            if (team == null) return NotFound();

            return AskPost(
                title: $"Delete team t{team.TeamId}",
                message: $"You are about to delete {team.TeamName} (t{team.TeamId}). Are you sure?",
                area: "Contest", ctrl: "Teams", act: "Delete", type: MessageType.Danger,
                routeValues: new Dictionary<string, string> { ["cid"] = $"{cid}", ["teamid"] = $"{teamid}" });
        }


        [HttpPost("{teamid}/[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int cid, int teamid, bool @checked = true)
        {
            var team = await DbContext.Teams
                .Where(t => t.ContestId == cid && t.TeamId == teamid)
                .SingleOrDefaultAsync();
            if (team is null || team.Status == 3) return NotFound();

            var oldUid = team.UserId;
            var raiseEvent = team.Status == 1;
            team.Status = 3;
            team.UserId = null;
            await UpdateTeamAsync(team, oldUid: oldUid, action: raiseEvent ? "delete" : null,
                comment: ("deleted", $"with u{oldUid}"));

            StatusMessage = $"Team t{teamid} deleted.";
            return RedirectToAction(nameof(List));
        }


        [HttpGet("{teamid}/[action]")]
        [ValidateInAjax]
        public async Task<IActionResult> Edit(int cid, int teamid)
        {
            var team = await FindTeamByIdAsync(teamid);
            if (team == null) return NotFound();
            ViewBag.Aff = await DbContext.ListTeamAffiliationAsync(cid, false);
            ViewBag.Cat = await DbContext.ListTeamCategoryAsync(cid);

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
            var team = await DbContext.Teams
                .Where(t => t.ContestId == cid && t.TeamId == teamid)
                .SingleOrDefaultAsync();
            if (team == null) return NotFound();

            team.TeamName = model.TeamName;
            team.AffiliationId = model.AffiliationId;
            team.CategoryId = model.CategoryId;
            await UpdateTeamAsync(team, ("updated", null), team.Status == 1 ? "update" : null);

            return Message(
                title: "Edit team",
                message: $"Team {team.TeamName} (t{teamid}) updated.",
                type: MessageType.Success);
        }


        [HttpGet("{teamid}/[action]")]
        [ValidateInAjax]
        public async Task<IActionResult> Accept(int cid, int teamid)
        {
            var team = await DbContext.Teams
                .Where(t => t.ContestId == cid && t.TeamId == teamid)
                .SingleOrDefaultAsync();
            if (team == null) return NotFound();
            bool oldok = team.Status != 1;
            team.Status = 1;
            await UpdateTeamAsync(team, ("accepted", null), oldok ? "create" : null);

            return Message(
                title: "Team registration confirm",
                message: $"Team #{teamid} is now accepted.",
                type: MessageType.Success);
        }


        [HttpGet("{teamid}/[action]")]
        [ValidateInAjax]
        public async Task<IActionResult> Reject(int cid, int teamid)
        {
            var team = await DbContext.Teams
                .Where(t => t.ContestId == cid && t.TeamId == teamid)
                .SingleOrDefaultAsync();
            if (team == null) return NotFound();
            bool oldok = team.Status == 1;
            team.Status = 2;
            await UpdateTeamAsync(team, ("rejected", null), oldok ? "delete" : null);

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
