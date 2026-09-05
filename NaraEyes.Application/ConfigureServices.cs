using Microsoft.Extensions.DependencyInjection;
using NaraEyes.Application.Contracts.Interfaces.Base;
using NaraEyes.Application.Contracts.Interfaces.Bulkoperations;
using NaraEyes.Application.Contracts.Interfaces.Devices;
using NaraEyes.Application.Contracts.Interfaces.Identity;
using NaraEyes.Application.Contracts.Interfaces.Metrics;
using NaraEyes.Application.Contracts.Interfaces.Modules;
using NaraEyes.Application.Contracts.Interfaces.Reports;
using NaraEyes.Application.Services.Base;
using NaraEyes.Application.Services.Bulkoperations;
using NaraEyes.Application.Services.Devices;
using NaraEyes.Application.Services.Identity;
using NaraEyes.Application.Services.Metrics;
using NaraEyes.Application.Services.Modules;
using NaraEyes.Application.Services.Reports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application
{
    public static class ConfigureServices
    {
        public static IServiceCollection RegisterApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<ISupervisionStateService, SupervisionStateService>();
            services.AddScoped<IBranchService, BranchService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IDevicePollingService, DevicePollingService>();
            services.AddScoped<IDeviceService, DeviceService>();
            services.AddScoped<IDeviceMetrics, DeviceMetricsService>();
            services.AddSingleton<IDeviceSignalHub, DeviceSignalHub>();
            services.AddSingleton<IHeartbeatThrottler, HeartbeatThrottler>();
            services.AddScoped<IModuleServices, ModulesService>();
            services.AddScoped<IBulkoperationsService, BulkoperationsService>();
            services.AddSingleton<IInBoxBatchWriter, InBoxBatchWriter>();
            services.AddScoped<IReportService, ReportService>();
            services.AddHostedService(sp => (InBoxBatchWriter)sp.GetRequiredService<IInBoxBatchWriter>());
            services.AddSingleton<ICommandDispatchState, CommandDispatchState>();
            services.AddHostedService<HeartbeatMonitor>();
            services.AddScoped<IModuleFaultReportService, ModuleFaultReportService>();
            services.AddHostedService<DataRetentionService>();
            services.AddScoped<ICashInventoryReportService, CashInventoryReportService>();
            services.AddScoped<IDeviceAvailabilityReportService, DeviceAvailabilityReportService>();
            services.AddHostedService<ServerUptimeTracker>();
            return services;
        }
    }
}
