using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Mvc.Filters
{
    public class ConfigurationBasicAuthorizationService : IBasicAuthorizationService
    {
        private readonly ISet<string> dict;
        const string sectionName = "BasicAuthorizationSecrects:";

        public ConfigurationBasicAuthorizationService(IConfiguration configuration)
        {
            dict = new HashSet<string>();

            foreach (var (key, val) in configuration.AsEnumerable())
            {
                if (key.StartsWith(sectionName))
                {
                    dict.Add($"{key.Substring(sectionName.Length)}:{val}");
                }
            }
        }
        
        public bool Authorize(string auth)
        {
            return dict.Contains(auth);
        }
    }
}
