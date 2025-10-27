using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using NaraEyes.Domain.Entities.Identity;
using System.Security.Claims;

namespace NaraEyes.WebApplication.Extensions
{
    public class CustomClaimsPrincipalFactory : UserClaimsPrincipalFactory<User>
    {
        public CustomClaimsPrincipalFactory(
            UserManager<User> userManager,
            IOptions<IdentityOptions> options
            ) : base(userManager, options)
        {

        }

        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(User user)
        {
            var identity = await base.GenerateClaimsAsync(user);

            identity.AddClaim(new Claim(ClaimTypes.GivenName, user.FirstName + " " + user.LastName ?? "نامشخص"));
            identity.AddClaim(new Claim(ClaimTypes.MobilePhone, user.PhoneNumber ?? "ندارد"));

            return identity;
        }
    }
}
