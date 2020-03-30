using JudgeWeb.Areas.Contest.Models;
using JudgeWeb.Data;
using JudgeWeb.Domains.Contests;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Contest.Controllers
{
    [Area("Contest")]
    [Route("[area]/{cid}/jury/[controller]")]
    [AuditPoint(AuditlogType.Clarification)]
    public class ClarificationsController : JuryControllerBase
    {
        public IClarificationStore Store { get; }

        [ViewData]
        public Dictionary<int, string> Teams { get; set; }

        public ClarificationsController(IClarificationStore store)
        {
            Store = store;
        }
        
        public override async Task OnActionExecutingAsync(ActionExecutingContext context)
        {
            await base.OnActionExecutingAsync(context);
            Teams = await Facade.Teams.ListNamesAsync(Contest.ContestId);
        }


        [HttpGet]
        public async Task<IActionResult> List(int cid)
        {
            var query = await Store.ListAsync(cid, c => c.Recipient == null);
            
            foreach (var item in query)
                item.TeamName = Teams.GetValueOrDefault(item.Sender ?? -1);
            
            return View(new JuryListClarificationModel
            {
                AllClarifications = query,
                Problems = Problems,
                JuryName = User.GetUserName(),
            });
        }


        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(
            int cid, AddClarificationModel model)
        {
            // validate the model
            if (string.IsNullOrWhiteSpace(model.Body))
                ModelState.AddModelError("xys::clar_empty", "Clarification body cannot be empty.");

            // reply clar
            Clarification replyTo = null;
            if (model.ReplyTo.HasValue)
            {
                replyTo = await Store.FindAsync(cid, model.ReplyTo.Value);
                if (replyTo == null)
                    ModelState.AddModelError("xys::clar_not_found", "The clarification replied to not found.");
            }

            // determine category
            var usage = ClarCategories.SingleOrDefault(cp => model.Type == cp.Item1);
            if (usage.Item1 == null)
                ModelState.AddModelError("xys::error_cate", "The category specified is wrong.");

            if (!ModelState.IsValid) return View(model);
            var clarId = await Store.SendAsync(
                replyTo: replyTo,
                clar: new Clarification
                {
                    Body = model.Body,
                    SubmitTime = DateTimeOffset.Now,
                    ContestId = cid,
                    JuryMember = User.GetUserName(),
                    Sender = null,
                    ResponseToId = model.ReplyTo,
                    Recipient = model.TeamTo == 0 ? default(int?) : model.TeamTo,
                    ProblemId = usage.Item3,
                    Answered = true,
                    Category = usage.Item2,
                });

            await HttpContext.AuditAsync("added", $"{clarId}");
            StatusMessage = $"Clarification {clarId} has been sent.";
            return RedirectToAction(nameof(Detail), new { clarid = clarId });
        }


        [HttpGet("[action]/{teamto}")]
        public IActionResult Send(int teamto)
        {
            return View(new AddClarificationModel { TeamTo = teamto, Body = "" });
        }


        [HttpGet("{clarid}/[action]/{answered}")]
        public async Task<IActionResult> SetAnswered(int cid, int clarid, bool answered)
        {
            var result = await Store.SetAnsweredAsync(cid, clarid, answered);

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
            var clar = await Store.FindAsync(cid, clarid);
            if (clar == null) return NotFound();
            var query = Enumerable.Repeat(clar, 1);

            if (!clar.Sender.HasValue && clar.ResponseToId.HasValue)
            {
                var clar2 = await Store.FindAsync(cid, clar.ResponseToId.Value);
                if (clar2 != null) query = query.Prepend(clar2);
            }
            else if (clar.Sender.HasValue)
            {
                var otherClars = await Store.ListAsync(cid,
                    c => c.ResponseToId == clarid && c.Sender == null);
                query = query.Concat(otherClars);
            }

            foreach (var item in query)
                item.TeamName = item.Sender.HasValue
                    ? Teams.GetValueOrDefault(item.Sender.Value)
                    : item.Recipient.HasValue
                    ? Teams.GetValueOrDefault(item.Recipient.Value)
                    : null;

            return View(new JuryViewClarificationModel
            {
                Associated = query,
                Main = query.First(),
                Problems = Problems,
                Teams = Teams,
                UserName = User.GetUserName(),
            });
        }


        [HttpGet("{clarid}/[action]/{claim}")]
        [ValidateInAjax]
        public async Task<IActionResult> Claim(int cid, int clarid, bool claim)
        {
            var admin = User.GetUserName();
            var result = await Store.ClaimAsync(cid, clarid, admin, claim);

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
