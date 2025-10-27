using CurrieTechnologies.Razor.SweetAlert2;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using NaraEyes.Application.Contracts.Models.Basic;
using NaraEyes.Domain.Entities.Identity;
using NaraEyes.Infrastructure.Persistence.Context;
using NaraEyes.WebApplication.Extensions;
using NaraEyes.WebApplication.Services;

namespace NaraEyes.WebApplication
{
    public static class ConfigureServices
    {
        public static IServiceCollection RegisterPresentationServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHttpContextAccessor();
            services.AddScoped<IUserClaimsPrincipalFactory<User>, CustomClaimsPrincipalFactory>();
            services.AddScoped<AuthenticationStateProvider, AuthStateRevalidator>();
            services.AddSingleton<ICaptchaManager, CaptchaManager>();
            services.AddSingleton<ILoginRateLimiter, LoginRateLimiter>();
            services.Configure<ApplicationSettings>(configuration);
            services.AddSweetAlert2();

            services.AddIdentity<User, Role>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

            services.Configure<IdentityOptions>(o =>
            {
                o.Lockout.AllowedForNewUsers = true;
                o.Lockout.MaxFailedAccessAttempts = 3;
                o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromHours(2);
            });

            services.AddCors(options => options.AddPolicy("CorsPolicy",
    builder =>
    {
        builder
        .SetIsOriginAllowed(origin => true)
        .AllowAnyHeader()
        .AllowAnyMethod();
    }));
            return services;

        }

        }
}
