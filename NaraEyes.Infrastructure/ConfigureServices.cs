using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NaraEyes.Application.Abstraction.Dapper;
using NaraEyes.Application.Abstraction.Identity;
using NaraEyes.Application.Abstraction.License;
using NaraEyes.Application.Abstraction.Logger;
using NaraEyes.Application.Abstraction.QueueAbstraction;
using NaraEyes.Infrastructure.ClockService;
using NaraEyes.Infrastructure.Dapper;
using NaraEyes.Infrastructure.IdentityRepository;
using NaraEyes.Infrastructure.License;
using NaraEyes.Infrastructure.logger;
using NaraEyes.Infrastructure.QueueImplemention;
using NaraEyes.SharedKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Infrastructure
{
    public static class ConfigureServices
    {
        public static IServiceCollection RegisterInfraStructureServices(this IServiceCollection services, IConfiguration configuration)
        {

            services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
            services.AddScoped<IApplicationUserManager, ApplicationUserManager>();
            services.AddScoped<IApplicationRoleManager, ApplicationRoleManager>();
            services.AddScoped<IOutboxService, OutboxService>();
            services.AddScoped<IInboxService, InBoxDeviceService>();
            services.AddSingleton<ICommandAwaiter, CommandAwaiter>();
            services.AddSingleton<IAckAwaiter, AckAwaiter>();
            //services.AddScoped<WebSocketPollHandler>();
            services.AddSingleton<IDbConnectionFactory>(_ =>
new DbConnectionFactory(configuration["ConnectionStrings:ApplicationDbContext"]));

            services.AddScoped<IAppLogger,AppLogger>();
           services.AddScoped<ILicenseValidationService, LicenseValidationService>();
            return services;
        }
    }
}
