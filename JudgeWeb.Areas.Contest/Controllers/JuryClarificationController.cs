using JudgeWeb.Areas.Contest.Models;
using JudgeWeb.Areas.Contest.Services;
using JudgeWeb.Data;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using ClarificationCategory = JudgeWeb.Data.Clarification.TargetCategory;

namespace JudgeWeb.Areas.Contest.Controllers
{
    [Area("Contest")]
    [Route("[area]/{cid}/jury/clarification/[action]")]
    public class JuryClarificationController : BaseController<JuryService>
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
        [Route("/[area]/{cid}/jury/clarification")]
        public IActionResult Clarifications()
        {
            var model = Service.GetClarifications();
            model.MessageInfo = DisplayMessage;
            return View("Clarifications", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Send(AddClarificationModel model)
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
            return RedirectToAction(nameof(Clarifications), new { cid });
        }

        [HttpGet("{teamto}")]
        public IActionResult Send(int teamto)
        {
            ViewBag.Teams = Service.TeamName;
            ViewBag.Problems = Service.Problems;
            return View(new AddClarificationModel
            {
                TeamTo = teamto,
                Body = "",
            });
        }

        [HttpGet("{clarid}/{answered}")]
        public IActionResult SetAnswered(int clarid, bool answered)
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

        [HttpGet]
        [Route("/[area]/{cid}/jury/clarification/{clarid}")]
        public IActionResult Clarification(int clarid)
        {
            var model = Service.GetClarification(clarid);
            if (model == null) return NotFound();
            ViewBag.Teams = model.Teams;
            ViewBag.Problems = model.Problems;
            return View(model);
        }

        [HttpGet("{clarid}/{claim}")]
        [ValidateInAjax]
        public IActionResult Claim(int clarid, bool claim)
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
    }
}
