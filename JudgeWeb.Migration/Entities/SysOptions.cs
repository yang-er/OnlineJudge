using System;
using System.Collections.Generic;

namespace JudgeWeb.Migration
{
    public partial class SysOptions
    {
        public int SysOptId { get; set; }
        public string Category { get; set; }
        public string Name { get; set; }
        public string DataType { get; set; }
        public string SValue { get; set; }
        public int NValue { get; set; }
    }
}
