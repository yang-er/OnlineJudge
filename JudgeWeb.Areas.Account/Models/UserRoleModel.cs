using System.ComponentModel.DataAnnotations;

namespace JudgeWeb.Areas.Account.Models
{
    public class UserRoleModel
    {
        public int UserId { get; set; }

        [Display(Name = "Site Administration")]
        public bool SiteAdministrator { get; set; }

        [Display(Name = "Problem Provide")]
        public bool ProblemProvider { get; set; }

        [Display(Name = "Guide Writing")]
        public bool GuideWriter { get; set; }

        [Display(Name = "Blocked Submission")]
        public bool SubmissionBlocked { get; set; }
    }
}
