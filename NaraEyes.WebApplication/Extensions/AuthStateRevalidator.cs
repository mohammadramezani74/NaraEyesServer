using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using NaraEyes.Domain.Entities.Identity;
using System.Security.Claims;

namespace NaraEyes.WebApplication.Extensions
{
    public class AuthStateRevalidator(ILoggerFactory loggerFactory,
       IServiceScopeFactory scopeFactory,
       IOptions<IdentityOptions> options
      )
      : RevalidatingServerAuthenticationStateProvider(loggerFactory)
    {
        protected override TimeSpan RevalidationInterval => TimeSpan.FromSeconds(10);

        protected async override Task<bool> ValidateAuthenticationStateAsync(AuthenticationState authenticationState,
            CancellationToken cancellationToken)
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

            ClaimsPrincipal principal = authenticationState.User;

            var user = await userManager.GetUserAsync(principal);

            if (user is null)
            {
                return false;
            }
            else
            {
                var principalStamp =
                    principal.FindFirstValue(options.Value.ClaimsIdentity.SecurityStampClaimType);

                var userStamp = await userManager.GetSecurityStampAsync(user);
                return principalStamp == userStamp;
            }
        }
    }
}
