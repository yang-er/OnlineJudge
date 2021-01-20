using System;
using System.Collections.Generic;

namespace JudgeWeb.Migration
{
    public partial class Sender
    {
        public int MsgId { get; set; }
        public int FromUid { get; set; }
        public int ToUid { get; set; }
        public string FromUserid { get; set; }
        public string ToUserid { get; set; }
        public DateTime DateMsg { get; set; }
        public string Message { get; set; }
        public string DelF { get; set; }
        public string DelT { get; set; }
        public string MsgType { get; set; }
    }
}
