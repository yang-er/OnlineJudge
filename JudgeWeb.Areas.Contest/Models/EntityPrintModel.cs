﻿using System;

namespace JudgeWeb.Areas.Contest.Models
{
    public class EntityPrintModel
    {
        public int id { get; set; }
        public DateTimeOffset time { get; set; }
        public string lang { get; set; }
        public string team { get; set; }
        public string filename { get; set; }
        public string room { get; set; }
        public bool processed { get; set; }
        public bool done { get; set; }
        public string sourcecode { get; set; }
    }
}
