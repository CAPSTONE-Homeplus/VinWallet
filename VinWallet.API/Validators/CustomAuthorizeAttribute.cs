using Microsoft.AspNetCore.Authorization;
using VinWallet.Repository.Enums;
using VinWallet.Repository.Utils;

namespace VinWallet.API.Validators
{
    public class CustomAuthorizeAttribute : AuthorizeAttribute
    {
        public CustomAuthorizeAttribute(params UserEnum.Role[] roleEnums)
        {
            var allowedRolesAsString = roleEnums.Select(x => x.GetDescriptionFromEnum());
            Roles = string.Join(",", allowedRolesAsString);
        }
    }
}
