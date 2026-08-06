using EFCoreSecondLevelCacheInterceptor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NaraEyes.Application.Abstraction.Unitofwork;
using NaraEyes.Application.Contracts.Models.Basic;
using NaraEyes.Infrastructure.Persistence.Context;
using NaraEyes.Infrastructure.Persistence.Interceptors;
using NaraEyes.Infrastructure.Persistence.Unitofwork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Infrastructure.Persistence
{
    public static class ConfigureServices
    {
        public static IServiceCollection RegisterPersistenceServices(this IServiceCollection services, IConfiguration configuration)
        {

            services.AddEFSecondLevelCache(options =>
            {
                options.UseMemoryCacheProvider()
                      
                       .CacheQueriesContainingTableNames(
                            CacheExpirationMode.Absolute, TimeSpan.FromMinutes(10),
                            TableNameComparison.ContainsOnly,
                            "Branches", "Users", "Roles")   
                       .ConfigureLogging(false);
            });
            services.AddSingleton<CustomSecondLevelCacheInterceptor>();
            services.AddSingleton<AuditInterceptor>();
            services.AddDbContextFactory<ApplicationDbContext>((serviceProvider, options) =>
            {
                var Audiinterceptor = serviceProvider.GetRequiredService<AuditInterceptor>();
                var secondLevelCache = serviceProvider.GetRequiredService<CustomSecondLevelCacheInterceptor>();
                var appSettings = serviceProvider.GetRequiredService<IOptions<ApplicationSettings>>().Value;
                if (appSettings is null) throw new ArgumentNullException(nameof(appSettings));

                options
                .UseSqlServer(appSettings.ConnectionStrings.ApplicationDbContext)
                .AddInterceptors(Audiinterceptor, secondLevelCache);
            });
            services.AddScoped<IApplicationUnitOfWork, ApplicationUnitOfWork>();




            return services;
        }

    }
}
