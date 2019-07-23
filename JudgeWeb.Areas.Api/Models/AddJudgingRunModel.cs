using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace JudgeWeb.Areas.Api.Models
{
    public class AddJudgingRunModel
    {
        static readonly JsonSerializer jd;

        public string batch { get; set; }

        static AddJudgingRunModel()
        {
            jd = JsonSerializer.CreateDefault();
        }

        public List<JudgingRunModel> Parse()
        {
            try
            {
                using (var sr = new StringReader(batch))
                using (var jtr = new JsonTextReader(sr))
                {
                    return jd.Deserialize<List<JudgingRunModel>>(jtr);
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
