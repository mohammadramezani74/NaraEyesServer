using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using NaraEyes.Domain.Entities.Identity;
using System.Security.Claims;

namespace NaraEyes.WebApplication.Extensions
{
    public class CustomClaimsPrincipalFactory : UserClaimsPrincipalFactory<User>
    {
        private readonly UserManager<User> _userManager;
        public CustomClaimsPrincipalFactory(
            UserManager<User> userManager,
            IOptions<IdentityOptions> options
            ) : base(userManager, options)
        {
            _userManager = userManager;
        }

        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(User user)
        {
            var identity = await base.GenerateClaimsAsync(user);
          
                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
         
            identity.AddClaim(new Claim(ClaimTypes.GivenName, user.FirstName + " " + user.LastName ?? "نامشخص"));
            identity.AddClaim(new Claim(ClaimTypes.MobilePhone, user.PhoneNumber ?? "ندارد"));
            var roles = await _userManager.GetRolesAsync(user);
            foreach (var role in roles)
            {

                identity.AddClaim(new Claim(ClaimTypes.Role, role));
            }

            return identity;
        }
    }
}
