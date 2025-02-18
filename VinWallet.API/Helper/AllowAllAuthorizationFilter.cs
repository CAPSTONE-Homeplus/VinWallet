using Hangfire.Dashboard;

namespace VinWallet.API.Helper;
public class AllowAllAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        return true;
    }
}
