namespace Microsoft.AspNetCore.Mvc.Filters
{
    public interface IBasicAuthorizationService
    {
        bool Authorize(string auth);
    }
}
