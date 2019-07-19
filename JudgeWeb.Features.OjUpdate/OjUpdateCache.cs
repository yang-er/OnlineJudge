using System;
using System.Collections.Generic;

namespace JudgeWeb.Features.OjUpdate
{
    public class OjUpdateCache
    {
        public DateTime LastUpdate { get; set; }

        public List<OjAccount> NameSet { get; set; }
    }
}
