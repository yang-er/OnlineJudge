using System;
using System.Linq;

namespace Microsoft.AspNetCore.Mvc.Filters
{
    public class ArgumentBasicAuthorizationService : IBasicAuthorizationService
    {
        public string[] Tokens { get; }

        public ArgumentBasicAuthorizationService(params string[] vs)
        {
            Tokens = vs;
        }

        public bool Authorize(string auth)
        {
            return Tokens.Contains(auth);
        }
    }
}
