using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Judge.Models
{
    public class CodeSubmitModel
    {
        [DisplayName("Your Code")]
        public string Code { get; set; }

        [DisplayName("Language")]
        public int Language { get; set; }

        public int ProblemId { get; set; }
    }
}
