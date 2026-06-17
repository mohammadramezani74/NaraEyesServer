using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using NaraEyes.Application;
using NaraEyes.Application.Contracts.Interfaces.Devices;
using NaraEyes.Application.Contracts.Interfaces.Metrics;
using NaraEyes.Application.Contracts.Models.Devices;
using NaraEyes.Application.Contracts.Models.Metrics;
using NaraEyes.Application.Contracts.Models.Modules.CDM;
using NaraEyes.Application.Hubs;
using NaraEyes.Domain.Entities.Base;
using NaraEyes.Infrastructure;
using NaraEyes.Infrastructure.Persistence;
using NaraEyes.Infrastructure.Persistence.Context;
using NaraEyes.WebApplication;
using NaraEyes.WebApplication.Components;
using NaraEyes.WebApplication.Extensions;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Sinks.MSSqlServer;
using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;


var calture = new CultureInfo("fa-IR");
CultureInfo.DefaultThreadCurrentCulture = calture;
CultureInfo.DefaultThreadCurrentUICulture = calture;


var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:5209");

builder.Services.AddRazorComponents()
       .AddInteractiveServerComponents(options =>
       {
           options.DetailedErrors = true;
       });

builder.Services.AddMudServices();
builder.Services.AddSignalR();
builder.Services.AddCascadingAuthenticationState();
var columnOptions = new ColumnOptions();

// ستون‌های پیش‌فرض مهم
columnOptions.Store.Remove(StandardColumn.Properties); // مهم: جلوگیری از تداخل


columnOptions.AdditionalColumns = new Collection<SqlColumn>
{
    new SqlColumn
    {
        ColumnName = "UserName",
        PropertyName = "UserName",
        DataType = SqlDbType.NVarChar,
        DataLength = 256
    },
    new SqlColumn
    {
        ColumnName = "IP",
        PropertyName = "IP",
        DataType = SqlDbType.NVarChar,
        DataLength = 50
    },
    new SqlColumn
    {
        ColumnName = "UserAgent",
        PropertyName = "UserAgent",
        DataType = SqlDbType.NVarChar,
        DataLength = 500
    },
    new SqlColumn
    {
        ColumnName = "RequestId",
        PropertyName = "RequestId",
        DataType = SqlDbType.NVarChar,
        DataLength = 100
    }
};


Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .Enrich.WithProcessId()
    .Enrich.WithExceptionDetails()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .WriteTo.Console()
    .WriteTo.File(
        path: "Logs/log-.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        shared: true,
        flushToDiskInterval: TimeSpan.FromSeconds(1))
    .WriteTo.MSSqlServer(
        connectionString: builder.Configuration.GetConnectionString("ApplicationDbContext"),
        sinkOptions: new MSSqlServerSinkOptions
        {
            TableName = "Logs",
            AutoCreateSqlTable = true
        },
        columnOptions: columnOptions,
        restrictedToMinimumLevel: LogEventLevel.Information
    )
    .CreateLogger();
builder.Host.UseSerilog();

builder.Services.RegisterPersistenceServices(builder.Configuration)
    .RegisterPresentationServices(builder.Configuration)
    .RegisterInfraStructureServices(builder.Configuration)
    .RegisterApplicationServices();

builder.Services.AddMemoryCache();

var app = builder.Build();

//app.UseSerilogRequestLogging(options =>
//{
//    options.MessageTemplate =
//        "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";

//    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
//    {
//        diagnosticContext.Set("UserName", httpContext.User.Identity?.Name ?? "Anonymous");
//        diagnosticContext.Set("IP", httpContext.Connection.RemoteIpAddress?.ToString());
//        diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].ToString());
//        diagnosticContext.Set("RequestId", httpContext.TraceIdentifier);
//    };
//});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();
    await AppDataSeeder.SeedAsync(scope.ServiceProvider);

}
app.UseStaticFiles();
app.UseCors("CorsPolicy");
app.UseMiddleware<LicenseMiddleware>();

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<LoggingMiddleware>();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapIdentityEndpoints();
app.MapHub<DeviceHub>("/deviceHub");

app.MapGet("/ping", () => "ok");
app.MapGet("/api/poll", async (
    string ip,
    IDevicePollingService pollingService,
    CancellationToken ct) =>
{
    var response = await pollingService.PollAsync(ip, null, ct);
    return Results.Ok(response);
});
app.UseWebSockets();
app.Map("/api/ws", async context =>
{
    var handler = context.RequestServices.GetRequiredService<WebSocketPollHandler>();
    await handler.HandleAsync(context);
});


app.MapPost("/api/poll", async (
    string ip,
    List<InBoxDeviceMessage> reports,
    IDevicePollingService pollingService,
    CancellationToken ct) =>
{
    var response = await pollingService.PollAsync(ip, reports, ct);
    return Results.Ok(response);
});




app.MapPost("/api/device/register", async (
    RegisterDeviceCommand req,
    IDeviceService service,
    CancellationToken ct) =>
{
    var id = await service.RegisterAsync(
     req,
        ct);

    return Results.Ok(new { DeviceId = id });
});
app.MapPost("/api/device/SubmitMetrics", async (
    DeviceMetricsDto req,
    IDeviceMetrics service,
    CancellationToken ct) =>
{
    var id = await service.SubmitOrUpdateMetrics(
     req,
        ct);

    return Results.Ok(new { DeviceId = id });
});
app.MapPost("/api/device/SubmitStatus", async (
  DeviceMuduleStatusCommand req,
    IDeviceMetrics service,
    CancellationToken ct) =>
{
    var res = await service.SubmitOrUpdateModulesStatus(
     req,
        ct);

    return Results.Ok(res);
});
app.MapPost("/api/device/AgentMode", async (
  IpModel req,
    IDeviceMetrics service,
    CancellationToken ct) =>
{
    var res = await service.UpdateAgentStatus(
     req.Ip,
        ct);

    return Results.Ok(res);
});




try
{
    Log.Information("Application starting up");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application failed to start");
}
finally
{
    Log.CloseAndFlush();
}
