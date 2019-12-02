using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace JudgeWeb.Areas.Api.Models
{
    public class InternalErrorModel
    {
        /// <summary>
        /// The description of the internal error
        /// </summary>
        [Required]
        public string description { get; set; }

        /// <summary>
        /// The log of the judgehost
        /// </summary>
        [Required]
        public string judgehostlog { get; set; }

        /// <summary>
        /// The object to disable in JSON format
        /// </summary>
        [Required]
        public string disabled { get; set; }

        /// <summary>
        /// The contest ID associated with this internal error
        /// </summary>
        public int? cid { get; set; }

        /// <summary>
        /// The ID of the judging that was being worked on
        /// </summary>
        public int? judgingid { get; set; }
    }
}
