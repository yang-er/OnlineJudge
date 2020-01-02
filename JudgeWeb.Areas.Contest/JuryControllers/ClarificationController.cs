using EFCore.BulkExtensions;
using JudgeWeb.Areas.Contest.Models;
using JudgeWeb.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClarificationCategory = JudgeWeb.Data.Clarification.TargetCategory;

namespace JudgeWeb.Areas.Contest.Controllers
{
    [Area("Contest")]
    [Route("[area]/{cid}/jury/clarifications")]
    public class JuryClarificationController : JuryControllerBase
    {
        private IQueryable<Clarification> QueryOf(int cid, int clarid) =>
            Service.Clarifications
                .Where(c => c.ContestId == cid && c.ClarificationId == clarid);


        [HttpGet]
        public async Task<IActionResult> List(int cid)
        {
            var query = await Service.Clarifications
                .Where(c => c.ContestId == cid && c.Recipient == null)
                .ToListAsync();
            
            var teamName = await Service.GetTeamNameAsync(cid);
            foreach (var item in query)
                item.TeamName = teamName.GetValueOrDefault(item.Sender ?? -1);
            var problems = await Service.GetProblemsAsync(cid);
            
            return View(new JuryListClarificationModel
            {
                AllClarifications = query,
                Problems = problems,
                JuryName = UserManager.GetUserName(User),
            });
        }


        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(int cid, AddClarificationModel model)
        {
            // validate the model
            if (string.IsNullOrWhiteSpace(model.Body))
                ModelState.AddModelError("xys::clar_empty", "Clarification body cannot be empty.");

            // reply clar
            Clarification replyTo = null;
            if (model.ReplyTo.HasValue)
            {
                replyTo = await QueryOf(cid, model.ReplyTo.Value)
                    .SingleOrDefaultAsync();
                if (replyTo == null)
                    ModelState.AddModelError("xys::clar_not_found", "The clarification replied to not found.");
            }

            // determine category
            var probs = await Service.GetProblemsAsync(cid);
            var avaliableCategories = probs
                .Select(cp => ($"prob-{cp.ShortName}", (ClarificationCategory.Problem), (int?)cp.ProblemId))
                .Prepend(("tech", ClarificationCategory.Technical, null))
                .Prepend(("general", ClarificationCategory.General, null));
            var usage = avaliableCategories.SingleOrDefault(cp => model.Type == cp.Item1);
            if (usage.Item1 == null)
                ModelState.AddModelError("xys::error_cate", "The category specified is wrong.");

            if (!ModelState.IsValid)
            {
                ViewBag.Teams = await Service.GetTeamNameAsync(cid);
                ViewBag.Problems = probs;
                return View(model);
            }
            else
            {
                var clarId = await Service.SendClarificationAsync(
                    clar: new Clarification
                    {
                        Body = model.Body,
                        SubmitTime = DateTimeOffset.Now,
                        ContestId = cid,
                        JuryMember = UserManager.GetUserName(User),
                        Sender = null,
                        ResponseToId = model.ReplyTo,
                        Recipient = model.TeamTo == 0 ? default(int?) : model.TeamTo,
                        ProblemId = usage.Item3,
                        Answered = true,
                        Category = usage.Item2,
                    },
                    replyTo: replyTo);

                DisplayMessage = $"Clarification {clarId} has been sent.";
                return RedirectToAction(nameof(Detail), new { clarid = clarId });
            }
        }


        [HttpGet("[action]/{teamto}")]
        public async Task<IActionResult> Send(int cid, int teamto)
        {
            ViewBag.Teams = await Service.GetTeamNameAsync(cid);
            ViewBag.Problems = await Service.GetProblemsAsync(cid);

            return View(new AddClarificationModel
            {
                TeamTo = teamto,
                Body = "",
            });
        }


        [HttpGet("{clarid}/[action]/{answered}")]
        public async Task<IActionResult> SetAnswered(int cid, int clarid, bool answered)
        {
            var result = await QueryOf(cid, clarid)
                .BatchUpdateAsync(c => new Clarification { Answered = answered });

            if (result == 1)
                return Message(
                    title: "Set clarification",
                    message: $"clarification #{clarid} is now {(answered ? "" : "un")}answered.",
                    type: MessageType.Success);
            else
                return Message(
                    title: "Set clarification",
                    message: "Unknown error.",
                    type: MessageType.Danger);
        }


        [HttpGet("{clarid}")]
        public async Task<IActionResult> Detail(int cid, int clarid)
        {
            var clar = await QueryOf(cid, clarid)
                .FirstOrDefaultAsync();
            if (clar == null) return NotFound();
            var query = Enumerable.Repeat(clar, 1);

            if (!clar.Sender.HasValue && clar.ResponseToId.HasValue)
            {
                var clar2 = await QueryOf(cid, clar.ResponseToId.Value)
                    .FirstOrDefaultAsync();
                if (clar2 != null) query = query.Prepend(clar2);
            }
            else if (clar.Sender.HasValue)
            {
                var otherClars = await Service.Clarifications
                    .Where(c => c.ContestId == cid)
                    .Where(c => c.ResponseToId == clarid && c.Sender == null)
                    .ToListAsync();
                query = query.Concat(otherClars);
            }

            var teamName = await Service.GetTeamNameAsync(cid);
            var probs = await Service.GetProblemsAsync(cid);
            ViewBag.Teams = teamName;
            ViewBag.Problems = probs;

            foreach (var item in query)
            {
                item.TeamName = item.Sender.HasValue
                    ? teamName.GetValueOrDefault(item.Sender.Value)
                    : item.Recipient.HasValue
                    ? teamName.GetValueOrDefault(item.Recipient.Value)
                    : null;
            }

            return View(new JuryViewClarificationModel
            {
                Associated = query,
                Main = query.First(),
                Problems = probs,
                Teams = teamName,
                UserName = UserManager.GetUserName(User),
            });
        }


        [HttpGet("{clarid}/[action]/{claim}")]
        [ValidateInAjax]
        public async Task<IActionResult> Claim(int cid, int clarid, bool claim)
        {
            var admin = UserManager.GetUserName(User);
            var result = await (claim
                ? QueryOf(cid, clarid)
                    .Where(c => c.JuryMember == null)
                    .BatchUpdateAsync(new Clarification { JuryMember = admin }, new List<string> { nameof(Clarification.JuryMember) })
                : QueryOf(cid, clarid)
                    .Where(c => c.JuryMember == admin)
                    .BatchUpdateAsync(new Clarification { JuryMember = null }, new List<string> { nameof(Clarification.JuryMember) }));

            if (result == 1 && claim == true)
                return RedirectToAction(nameof(Detail));
            else if (result == 1)
                return RedirectToAction(nameof(List));
            else
                return Message(
                    title: "Claim clarification",
                    message: $"Clarification has been {(claim ? "" : "un")}claimed before.",
                    type: MessageType.Danger);
        }
    }
}
