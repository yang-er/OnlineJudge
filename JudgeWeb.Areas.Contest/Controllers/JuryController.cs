using JudgeWeb.Areas.Contest.Models;
using JudgeWeb.Areas.Contest.Services;
using JudgeWeb.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClarificationCategory = JudgeWeb.Data.Clarification.TargetCategory;

namespace JudgeWeb.Areas.Contest.Controllers
{
    [Area("Contest")]
    [Route("[area]/{cid}/[controller]/[action]")]
    public partial class JuryController : BaseController<JuryService>
    {
        protected override IActionResult BeforeActionExecuting()
        {
            if (!User.IsInRoles("Administrator,JuryOfContest" + Contest.ContestId))
                return Forbid();

            ViewData["InJury"] = true;
            var res = base.BeforeActionExecuting();
            int ucc = Service.GetUnansweredClarificationCount();
            int ptc = Service.GetPendingTeamCount();
            ViewBag.ucc = ucc != 0 ? ucc.ToString() : "";
            ViewBag.ptc = ptc != 0 ? ptc.ToString() : "";
            return res;
        }

        protected override (bool showPublic, bool isJury) ScoreboardChooseStrategy()
        {
            return (false, true);
        }

        protected override string GoAfterSubmit => nameof(Submission);

        [HttpGet]
        [Route("/[area]/{cid}/[controller]")]
        public IActionResult Home()
        {
            return View(new JuryHomeModel
            {
                Contest = Contest,
                Problem = Service.Problems,
                Message = DisplayMessage,
            });
        }

        [HttpPost("{target}")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public IActionResult ChangeState(string target)
        {
            DisplayMessage = Service.ChangeState(target);
            return RedirectToAction(nameof(Home), new { cid = Contest.ContestId });
        }

        [HttpGet("{page?}")]
        public IActionResult Auditlog(int page = 1)
        {
            if (page <= 0) return BadRequest();
            var model = Service.GetAuditLogs(page);
            ViewBag.Page = page;
            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public IActionResult Edit(JuryEditModel model)
        {
            string msg;
            bool suc = ModelState.IsValid;

            if (suc)
            {
                (msg, suc) = Service.UpdateContest(model);
            }
            else
            {
                var sb = new StringBuilder();
                foreach (var err in ModelState)
                    foreach (var errr in err.Value.Errors)
                        sb.AppendLine(errr.ErrorMessage);
                msg = sb.ToString();
            }

            if (suc)
            {
                DisplayMessage = msg;
                return RedirectToAction(nameof(Edit), new { cid = Contest.ContestId });
            }
            else
            {
                ViewBag.Categories = Service.QueryCategories(null).ToList();
                ViewBag.Repository = Service.GetAllProblems();
                ViewBag.Message = msg;

                return View(model);
            }
        }

        [HttpGet]
        [Authorize(Roles = "Administrator")]
        public IActionResult Edit()
        {
            ViewBag.Categories = Service.QueryCategories(null).ToList();
            ViewBag.Repository = Service.GetAllProblems();
            ViewBag.Message = DisplayMessage;

            var startTime = Contest.StartTime?.ToString() ?? "";
            var startDateTime = Contest.StartTime ?? DateTimeOffset.UnixEpoch;
            var stopTime = Contest.EndTime.HasValue ? (Contest.EndTime.Value - startDateTime).ToDeltaString() : "";
            var unfTime = Contest.UnfreezeTime.HasValue ? (Contest.UnfreezeTime.Value - startDateTime).ToDeltaString() : "";
            var freTime = Contest.FreezeTime.HasValue ? (Contest.FreezeTime.Value - startDateTime).ToDeltaString() : "";

            return View(new JuryEditModel
            {
                ContestId = Contest.ContestId,
                FreezeTime = freTime,
                Name = Contest.Name,
                ShortName = Contest.ShortName,
                RankingStrategy = Contest.RankingStrategy,
                StartTime = startTime,
                StopTime = stopTime,
                UnfreezeTime = unfTime,
                DefaultCategory = Contest.RegisterDefaultCategory,
                BronzeMedal = Contest.BronzeMedal,
                GoldenMedal = Contest.GoldMedal,
                SilverMedal = Contest.SilverMedal,
                IsPublic = Contest.IsPublic,
                Problems = Service.Problems.ToDictionary(p => p.Rank - 1),
            });
        }

        [HttpGet]
        public IActionResult Submission()
        {
            var model = Service.GetSubmissions();
            return View("Submissions", model);
        }

        [HttpGet("{sid}/{gid?}")]
        public IActionResult Submission(int sid, int? gid)
        {
            var tc = Service.QueryTestcaseCount().ToDictionary(t => t.ProblemId);
            var lang = Service.QueryLanguages().ToDictionary(l => l.LangId);
            var model = Service.GetSubmission(sid, gid);
            if (model is null) return NotFound();

            model.LanguageName = lang[model.LanguageId].Name;
            model.LanguageExternalId = lang[model.LanguageId].ExternalId;
            model.Team = Service.QueryTeam(model.Author).FirstOrDefault();
            model.ProblemShortName = tc[model.ProblemId].ShortName;
            model.TestcaseNumber = tc[model.ProblemId].TestcaseCount;
            return View(model);
        }

        [HttpGet]
        [ValidateInAjax]
        public IActionResult Submit()
        {
            if (Service.Team is null || Service.Team.Status != 1)
                return Message("Submit", "You don't belong to a team yet.", MessageType.Danger);
            ViewData["Problems"] = Service.Problems;
            ViewData["Languages"] = Service.QueryLanguages()
                .Where(l => l.AllowSubmit)
                .ToDictionary(a => a.LangId, a => a.Name);
            return Window(new TeamCodeSubmitModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Submit(TeamCodeSubmitModel model)
        {
            if (Service.Team is null || Service.Team.Status != 1)
                return Message("Submit", "You don't belong to a team yet.", MessageType.Danger);
            var sid = SubmitCore(model);
            if (sid == -1) return NotFound();
            return RedirectToAction(nameof(Submission), new { cid = Contest.ContestId, sid });
        }

        [HttpGet]
        [ActionName("Clarification")]
        public IActionResult ListClarification()
        {
            var model = Service.GetClarifications();
            model.MessageInfo = DisplayMessage;
            return View("Clarifications", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ClarificationSend(AddClarificationModel model)
        {
            var cid = Contest.ContestId;
            var probs = Service.Problems;

            string SolveAndAdd()
            {
                if (string.IsNullOrWhiteSpace(model.Body))
                    return "Error sending empty clarification.";

                var newClar = new Clarification
                {
                    Body = model.Body,
                    SubmitTime = DateTimeOffset.Now,
                    ContestId = cid,
                    JuryMember = UserManager.GetUserName(User),
                    Sender = null,
                    ResponseToId = model.ReplyTo,
                    Recipient = model.TeamTo == 0 ? default(int?) : model.TeamTo,
                    ProblemId = null,
                    Answered = true
                };

                if (model.ReplyTo.HasValue)
                {
                    var respTo = Service.GetClarification(model.ReplyTo.Value, true);
                    if (respTo == null) return "Error finding clarification replying to";
                    respTo.Answered = true;
                    Service.UpdateClarificationBeforeInsertOne(respTo);
                }

                if (model.Type == "general")
                    newClar.Category = ClarificationCategory.General;
                else if (model.Type == "tech")
                    newClar.Category = ClarificationCategory.Technical;
                else if (!model.Type.StartsWith("prob-"))
                    return "Error detecting category.";
                else
                {
                    var prob = probs.FirstOrDefault(p => "prob-" + p.ShortName == model.Type);
                    if (prob is null) return "Error detecting problem.";
                    newClar.ProblemId = prob.ProblemId;
                    newClar.Category = ClarificationCategory.Problem;
                }

                Service.SendClarification(newClar);
                return "Clarification sent to common";
            }

            DisplayMessage = SolveAndAdd();
            return RedirectToAction(nameof(Clarification), new { cid });
        }

        [HttpGet("send/{teamto}")]
        [ActionName("Clarification")]
        public IActionResult SendClarification(int teamto)
        {
            ViewBag.Teams = Service.TeamName;
            ViewBag.Problems = Service.Problems;
            return View("ClarificationSend", new AddClarificationModel
            {
                TeamTo = teamto,
                Body = "",
            });
        }

        [HttpGet("{clarid}/{answered}")]
        public IActionResult ClarificationSetAnswered(int clarid, bool answered)
        {
            if (Service.SetAnswerClarification(clarid, answered))
                return Message(
                    "Set clarification",
                    $"clarification #{clarid} is now {(answered ? "" : "un")}answered.",
                    MessageType.Success);

            return Message(
                "Set clarification",
                "Unknown error.",
                MessageType.Danger);
        }

        [HttpGet("{clarid}")]
        [ActionName("Clarification")]
        public IActionResult ViewClarification(int clarid)
        {
            var model = Service.GetClarification(clarid);
            if (model == null) return NotFound();
            ViewBag.Teams = model.Teams;
            ViewBag.Problems = model.Problems;
            return View(model);
        }

        [HttpGet("{clarid}/{claim}")]
        [ValidateInAjax]
        public IActionResult ClarificationClaim(int clarid, bool claim)
        {
            if (Service.ClaimClarification(clarid, claim))
                return Message(
                    "Claim clarification",
                    $"clarification #{clarid} is now {(claim ? "" : "un")}claimed.",
                    MessageType.Success);

            return Message(
                "Claim clarification",
                "Unknown error.",
                MessageType.Danger);
        }

        [HttpGet]
        [ActionName("Team")]
        public IActionResult ListTeam()
        {
            return View("Teams", Service.GetTeams());
        }

        [HttpGet("{teamid}")]
        [ActionName("Team")]
        public IActionResult ViewTeam(int teamid)
        {
            var sc = Service.GetOneTeam(teamid);
            if (sc is null) return NotFound();

            return View(new JuryViewTeamModel
            {
                TeamScoreboard = sc,
                Submissions = Service.GetSubmissions(teamid)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TeamAdd(JuryAddTeamModel model)
        {
            var teamid = Service.CreateTeam(new Team
            {
                AffiliationId = model.AffiliationId,
                Status = 1,
                CategoryId = model.CategoryId,
                ContestId = Contest.ContestId,
                TeamName = model.TeamName,
                UserId = model.UserId,
            });

            return Message(
                "Add team",
                $"Team {model.TeamName} (t{teamid}) added.",
                MessageType.Success);
        }

        [HttpGet]
        public IActionResult TeamAdd()
        {
            ViewData["Aff"] = Service.QueryAffiliations(false).ToList();
            ViewData["Cat"] = Service.QueryCategories(null).ToList();
            ViewData["User"] = Service.GetUnregisteredUsers();
            return Window(new JuryAddTeamModel());
        }

        [HttpPost("{teamid}")]
        [ValidateAntiForgeryToken]
        [ValidateInAjax]
        public IActionResult TeamDelete(int teamid, JuryDeleteTeamModel model2)
        {
            var team = Service.QueryTeam(teamid).FirstOrDefault();

            if (team is null)
                return Message(
                    "Delete team",
                    $"Team #{teamid} not found.",
                    MessageType.Warning);

            Service.DeleteTeam(team);

            return Message(
                "Delete team",
                $"Team #{teamid} {model2.ToDelete} deleted.",
                MessageType.Success);
        }

        [HttpPost("{teamid}")]
        [ValidateAntiForgeryToken]
        [ValidateInAjax]
        public IActionResult TeamEdit(int teamid, JuryEditTeamModel model)
        {
            var team = Service.QueryTeam(teamid).FirstOrDefault();
            Service.UpdateTeam(team, model);
            Service.QueryTeam(teamid, true).FirstOrDefault();

            return Message(
                "Edit team",
                $"Team {team.TeamName} (t{teamid}) updated.",
                MessageType.Success);
        }

        [HttpGet("{teamid}/{act}")]
        [ActionName("Team")]
        [ValidateInAjax]
        public IActionResult ActionTeam(int teamid, string act)
        {
            var team = Service.QueryTeam(teamid).FirstOrDefault();
            if (team == null) return NotFound();

            if (act == "delete")
            {
                return Window(nameof(TeamDelete), new JuryDeleteTeamModel
                {
                    ToDelete = $"{team.TeamName} (t{team.TeamId})"
                });
            }
            else if (act == "edit")
            {
                ViewData["Aff"] = Service.QueryAffiliations(false).ToList();
                ViewData["Cat"] = Service.QueryCategories(null).ToList();

                return Window(nameof(TeamEdit), new JuryEditTeamModel
                {
                    AffiliationId = team.AffiliationId,
                    CategoryId = team.CategoryId,
                    TeamName = team.TeamName,
                    TeamId = teamid,
                });
            }
            else if (act == "accept" || act == "reject")
            {
                Service.UpdateTeam(team, act);

                return Message(
                    "Team registration confirm",
                    $"Team #{teamid} is now {act}ed.",
                    MessageType.Success);
            }
            else
            {
                return NotFound();
            }
        }

        [HttpGet]
        public IActionResult RefreshCache()
        {
            Service.RefreshScoreboardCache();
            return Ok();
        }

        [HttpGet]
        [ValidateInAjax]
        public IActionResult TeamImport()
        {
            return View();
        }

        [HttpPost]
        [ValidateInAjax]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TeamImport(IFormFile file)
        {
            // every line in file: [name]\t[affname]\t[category]\t[uid]\n
            var toAddTeams = new List<Team>();
            var cats = Service.QueryCategories(null).ToList();
            var affs = Service.QueryAffiliations(false).ToList();
            var users = Service.GetUnregisteredUsers();

            IActionResult ErrorReadingTsv(string line)
            {
                return Message("Team Import",
                    "Error reading tsv file with line: " + line,
                    MessageType.Danger);
            }

            IActionResult ErrorLoadingDependency(string name, string value)
            {
                return Message("Team Import",
                    "No " + name + " value " + value + " found.",
                    MessageType.Danger);
            }

            using (var stream = file.OpenReadStream())
            using (var reader = new StreamReader(stream))
            {
                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var one = line.Split('\t');
                    if (one.Length != 4) return ErrorReadingTsv(line);
                    if (string.IsNullOrWhiteSpace(one[0])) return ErrorReadingTsv(line);
                    if (!int.TryParse(one[2], out int catid)) return ErrorReadingTsv(line);
                    if (!int.TryParse(one[3], out int uid)) return ErrorReadingTsv(line);
                    if (string.IsNullOrWhiteSpace(one[1])) return ErrorReadingTsv(line);
                    if (!affs.Any(a => a.ExternalId == one[1])) return ErrorLoadingDependency("affiliation", one[1]);
                    if (!cats.Any(c => c.CategoryId == catid)) return ErrorLoadingDependency("category", one[2]);
                    if (uid != 0 && !users.ContainsKey(uid)) return ErrorLoadingDependency("user id", one[3]);
                    if (uid != 0) users.Remove(uid);

                    toAddTeams.Add(new Team
                    {
                        AffiliationId = affs.First(a => a.ExternalId == one[1]).AffiliationId,
                        Status = 1,
                        CategoryId = catid,
                        ContestId = Contest.ContestId,
                        TeamName = one[0],
                        UserId = uid,
                    });
                }
            }

            Service.CreateTeams(toAddTeams);
            return Message("Team Import", "Import succeeded.", MessageType.Success);
        }
    }
}
