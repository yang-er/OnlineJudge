using JudgeWeb.Areas.Contest.Models;
using JudgeWeb.Areas.Contest.Services;
using JudgeWeb.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Contest.Controllers
{
    [Area("Contest")]
    [Route("[area]/{cid}/jury/team/[action]")]
    public class JuryTeamController : BaseController<JuryService>
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

        protected override bool EnableScoreboard => false;

        [HttpGet]
        [Route("/[area]/{cid}/jury/team")]
        public IActionResult Teams()
        {
            return View("Teams", Service.GetTeams());
        }

        [HttpGet]
        [Route("/[area]/{cid}/jury/team/{teamid}")]
        public IActionResult Team(int teamid)
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
        public IActionResult Add(JuryAddTeamModel model)
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
        public IActionResult Add()
        {
            ViewData["Aff"] = Service.QueryAffiliations(false).ToList();
            ViewData["Cat"] = Service.QueryCategories(null).ToList();
            ViewData["User"] = Service.GetUnregisteredUsers();
            return Window(new JuryAddTeamModel());
        }

        [HttpPost("{teamid}")]
        [ValidateAntiForgeryToken]
        [ValidateInAjax]
        public IActionResult Delete(int teamid, JuryDeleteTeamModel model2)
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
        public IActionResult Edit(int teamid, JuryEditTeamModel model)
        {
            var team = Service.QueryTeam(teamid).FirstOrDefault();
            Service.UpdateTeam(team, model);
            Service.QueryTeam(teamid, true).FirstOrDefault();

            return Message(
                "Edit team",
                $"Team {team.TeamName} (t{teamid}) updated.",
                MessageType.Success);
        }

        [HttpGet("{teamid}")]
        [ValidateInAjax]
        public IActionResult Delete(int teamid)
        {
            var team = Service.QueryTeam(teamid).FirstOrDefault();
            if (team == null) return NotFound();

            return Window(new JuryDeleteTeamModel
            {
                ToDelete = $"{team.TeamName} (t{team.TeamId})"
            });
        }

        [HttpGet("{teamid}")]
        [ValidateInAjax]
        public IActionResult Edit(int teamid)
        {
            var team = Service.QueryTeam(teamid).FirstOrDefault();
            if (team == null) return NotFound();

            ViewData["Aff"] = Service.QueryAffiliations(false).ToList();
            ViewData["Cat"] = Service.QueryCategories(null).ToList();

            return Window(new JuryEditTeamModel
            {
                AffiliationId = team.AffiliationId,
                CategoryId = team.CategoryId,
                TeamName = team.TeamName,
                TeamId = teamid,
            });
        }

        [HttpGet("{teamid}")]
        [ValidateInAjax]
        public IActionResult Accept(int teamid)
        {
            var team = Service.QueryTeam(teamid).FirstOrDefault();
            if (team == null) return NotFound();

            Service.UpdateTeam(team, "accept");

            return Message(
                "Team registration confirm",
                $"Team #{teamid} is now accepted.",
                MessageType.Success);
        }

        [HttpGet("{teamid}")]
        [ValidateInAjax]
        public IActionResult Reject(int teamid)
        {
            var team = Service.QueryTeam(teamid).FirstOrDefault();
            if (team == null) return NotFound();

            Service.UpdateTeam(team, "reject");

            return Message(
                "Team registration confirm",
                $"Team #{teamid} is now rejected.",
                MessageType.Success);
        }

        [HttpGet]
        [ValidateInAjax]
        public IActionResult Import()
        {
            return View();
        }

        [HttpPost]
        [ValidateInAjax]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(1 << 26)]
        public async Task<IActionResult> Import(IFormFile file)
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
