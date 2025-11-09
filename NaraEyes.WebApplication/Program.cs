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

builder.Services.RegisterPersistenceServices(builder.Configuration)
    .RegisterPresentationServices(builder.Configuration)
    .RegisterInfraStructureServices(builder.Configuration)
    .RegisterApplicationServices();

builder.Services.AddMemoryCache();

var app = builder.Build();


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();
}
app.UseStaticFiles();
app.UseCors("CorsPolicy");
app.UseAuthentication();
app.UseAuthorization();

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

app.MapPost("/api/device/reregister", async (
    DeviceReRegisterRequest req,
    IDeviceService service,
    CancellationToken ct) =>
{
    var id = await service.ReRegisterAsync(req.Ip, req.Model, req.AgentVersion, ct);
    return Results.Ok(new { DeviceId = id });
});


app.Run();
