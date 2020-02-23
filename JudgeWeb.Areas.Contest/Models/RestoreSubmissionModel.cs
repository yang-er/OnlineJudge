using System;
using System.ComponentModel.DataAnnotations;

namespace JudgeWeb.Areas.Contest.Models
{
    public class RestoreSubmissionModel
    {
        /// <summary>
        /// The Problem ID
        /// </summary>
        [Required]
        public int probid { get; set; }

        /// <summary>
        /// The Team ID
        /// </summary>
        [Required]
        public int teamid { get; set; }

        /// <summary>
        /// Language
        /// </summary>
        [Required]
        public string langid { get; set; }

        /// <summary>
        /// Source code
        /// </summary>
        [Required]
        public string code { get; set; }

        /// <summary>
        /// Submission IP
        /// </summary>
        [Required]
        public string ip { get; set; }

        /// <summary>
        /// Submit time
        /// </summary>
        public DateTimeOffset? time { get; set; }
    }
}
