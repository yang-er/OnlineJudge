﻿using JudgeWeb.Areas.Contest.Models;
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
            var team = await FindTeamByIdAsync(teamid);
            if (team == null) return NotFound();
            ViewBag.ShowTeam = team;
            var scoreboard = await FindScoreboardAsync(teamid);
            ViewBag.Scoreboard = scoreboard;
            ViewBag.Submissions = await ListSubmissionsByJuryAsync(cid, teamid);

            if (scoreboard != null)
            {
                ViewBag.Category = scoreboard.Category;
                ViewBag.Affiliation = scoreboard.Affiliation;
            }
            else
            {
                var cats = await Facade.Teams.ListCategoryAsync(Contest.ContestId);
                var affs = await Facade.Teams.ListAffiliationAsync(Contest.ContestId, false);
                ViewBag.Affiliation = affs.FirstOrDefault(a => a.AffiliationId == team.AffiliationId);
                ViewBag.Category = cats.FirstOrDefault(c => c.CategoryId == team.CategoryId);
            }

            var allMembers = await Store.ListMembersAsync(cid);
            ViewBag.Member = allMembers[teamid];
            return View();
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

            await Store.UpdateAsync(cid, teamid,
                team => new Team
                {
                    TeamName = model.TeamName,
                    AffiliationId = model.AffiliationId,
                    CategoryId = model.CategoryId
                });

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
            await Store.UpdateAsync(cid, teamid, _ => new Team { Status = 1 });
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
            await Store.UpdateAsync(cid, teamid, _ => new Team { Status = 2 });
            await HttpContext.AuditAsync("rejected", $"{teamid}");

            return Message(
                title: "Team registration confirm",
                message: $"Team #{teamid} is now rejected.",
                type: MessageType.Success);
        }


        [HttpGet("[action]")]
        [ValidateInAjax]
        [Authorize(Roles = "Administrator,Teacher")]
        public async Task<IActionResult> ByGroup(int cid,
            [FromServices] IStudentStore store)
        {
            ViewBag.Aff = await Store.ListAffiliationAsync(cid, false);
            ViewBag.Cat = await Store.ListCategoryAsync(cid);
            ViewBag.Class = await store.ListClassAsync();

            return Window(new AddTeamByGroupModel
            {
                AffiliationId = 1,
                CategoryId = 3,
                AddNonTemporaryUser = true
            });
        }


        [HttpGet("[action]")]
        [ValidateInAjax]
        [Authorize(Roles = "Administrator")]
        public IActionResult LockoutTemporary()
        {
            return AskPost(
                title: "Lockout temporary users",
                message: "Are you sure to lockout temporary users? You should only proceed this after the whole contest is over.",
                area: "Contest", ctrl: "Teams", act: nameof(LockoutTemporaryConfirmation), new { cid = Contest.ContestId },
                type: MessageType.Warning);
        }


        [HttpPost("[action]")]
        [Authorize(Roles = "Administrator")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LockoutTemporaryConfirmation()
        {
            await Store.BatchLockOutAsync(Contest.ContestId);
            StatusMessage = "Lockout finished.";
            return RedirectToAction(nameof(List));
        }


        [HttpGet("[action]")]
        [ValidateInAjax]
        [Authorize(Roles = "Administrator,Teacher")]
        public async Task<IActionResult> ByList(int cid)
        {
            ViewBag.Aff = await Store.ListAffiliationAsync(cid, false);
            ViewBag.Cat = await Store.ListCategoryAsync(cid);

            return Window(new AddTeamByListModel
            {
                AffiliationId = 1,
                CategoryId = 3,
            });
        }


        [HttpPost("[action]")]
        [Authorize(Roles = "Administrator,Teacher")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ByListConfirmation(
            int cid, AddTeamByListModel model,
            [FromServices] UserManager userManager)
        {
            var affs = await Store.ListAffiliationAsync(cid, false);
            var cats = await Store.ListCategoryAsync(cid);
            var aff = affs.SingleOrDefault(a => a.AffiliationId == model.AffiliationId);
            var cat = cats.SingleOrDefault(c => c.CategoryId == model.CategoryId);
            if (aff == null || cat == null) return NotFound();

            var names = (model.TeamNames ?? "").Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var result = await Store.BatchCreateAsync(userManager, cid, aff, cat, names);
            return View(result);
        }


        [HttpPost("[action]")]
        [Authorize(Roles = "Administrator,Teacher")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ByGroup(
            int cid, AddTeamByGroupModel model,
            [FromServices] IStudentStore store)
        {
            var cls = await store.FindClassAsync(model.GroupId);
            if (cls == null) return NotFound();

            var affs = await Store.ListAffiliationAsync(cid, false);
            var cats = await Store.ListCategoryAsync(cid);
            var aff = affs.SingleOrDefault(a => a.AffiliationId == model.AffiliationId);
            var cat = cats.SingleOrDefault(c => c.CategoryId == model.CategoryId);
            if (aff == null || cat == null) return NotFound();

            var stus = await store.ListStudentsAsync(cls);
            var stu = stus.ToLookup(s => new { s.Id, s.Name });
            var uids = await Store.ListMemberUidsAsync(cid);

            foreach (var item in stu)
            {
                var lst = item.Where(s => s.IsVerified ?? false);
                if (model.AddNonTemporaryUser) lst = lst.Where(s => s.Email == null);
                var users = lst.Select(s => s.UserId.Value)
                    .Where(i => !uids.Contains(i))
                    .ToHashSet();

                await Store.CreateAsync(
                    uids: users.Count > 0 ? users.ToArray() : null,
                    team: new Team
                    {
                        AffiliationId = model.AffiliationId,
                        Status = 1,
                        CategoryId = model.CategoryId,
                        ContestId = Contest.ContestId,
                        TeamName = $"{item.Key.Id}{item.Key.Name}",
                    });
            }

            StatusMessage = "Import success.";
            return RedirectToAction(nameof(List));
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
