using NaraEyes.Application.Abstraction.License;

namespace NaraEyes.WebApplication.Extensions
{
    public class LicenseMiddleware
    {
        private readonly RequestDelegate _next;

        public LicenseMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ILicenseValidationService licenseService)
        {
          
            if (context.Request.Path.StartsWithSegments("/license-status") ||
                context.Request.Path.StartsWithSegments("/error"))
            {
                await _next(context);
                return;
            }

            var isValid = await licenseService.IsLicenseValidAsync();
            if (!isValid)
            {
                context.Response.Redirect("/license-status");
                return;
            }

            await _next(context);
        }
    }
}
