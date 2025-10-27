using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NaraEyes.Domain.Entities.Identity;
using System.Security.Claims;

namespace NaraEyes.WebApplication.Extensions
{
    public static class IdentityEndpointsExtensions
    {
        public static IEndpointConventionBuilder
            MapIdentityEndpoints(this IEndpointRouteBuilder endpoint)
        {
            ArgumentNullException.ThrowIfNull(endpoint);

            var accountGroup = endpoint.MapGroup("/Account")
                .RequireAuthorization();


            accountGroup.MapPost("/Logout", async (
             ClaimsPrincipal user,
             [FromServices] UserManager<User> userManager,
              [FromServices] SignInManager<User> signInManager,
              [FromForm] string returnUrl = "/") =>
            {
                await signInManager.SignOutAsync();
                var currentUser = await userManager.FindByNameAsync(user.Identity.Name);
                await userManager.UpdateSecurityStampAsync(currentUser);

                return TypedResults.LocalRedirect(returnUrl);
            });




            accountGroup.MapGet("/RefreshAuth", async (
            ClaimsPrincipal user,
            [FromServices] UserManager<User> userManager,
            [FromServices] SignInManager<User> signInManager,
            [FromQuery] string returnUrl = "/account/profile"
                ) =>
            {
                var User = await userManager.GetUserAsync(user);
                await signInManager.RefreshSignInAsync(User);
                return TypedResults.LocalRedirect(returnUrl);
            });

            return accountGroup;

        }
    }
}
