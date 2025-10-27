using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Logging;
using NaraEyes.Application.Abstraction.QueueAbstraction;
using NaraEyes.Application.Contracts.Interfaces.Base;
using NaraEyes.Application.Contracts.Interfaces.Devices;
using NaraEyes.Application.Contracts.Models.Basic;
using NaraEyes.Application.Contracts.Models.Bulkoperations;
using NaraEyes.Application.Contracts.Models.Devices;
using NaraEyes.Application.Contracts.Utilities;
using NaraEyes.Domain.Entities.Base;
using NaraEyes.Domain.Enumerations;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NaraEyes.Application.Services.Devices
{
    public class DevicePollingService : IDevicePollingService
    {
        private readonly IOutboxService _outBoxService;
        private readonly IInboxService _inBoxService;
        private readonly IDeviceService _deviceService;
        private readonly ICommandAwaiter _await;
        private readonly IAckAwaiter _ackAwaiter;
        private readonly IInBoxBatchWriter _inboxWriter;
        private readonly IHeartbeatThrottler _heartbeat;
        private readonly IDeviceSignalHub _signals;
        private readonly ILogger<DevicePollingService> _logger;
        private readonly ConcurrentDictionary<string, ConcurrentQueue<OutBoxDeviceMessage>> _hot = new();

        public DevicePollingService(IOutboxService outBoxService, IInboxService inBoxService, IDeviceService deviceService, ICommandAwaiter await, IAckAwaiter ackAwaiter, IInBoxBatchWriter inboxWriter, IHeartbeatThrottler heartbeat, IDeviceSignalHub signals, ILogger<DevicePollingService> logger)
        {
            _outBoxService = outBoxService;
            _inBoxService = inBoxService;
            _deviceService = deviceService;
            _await = await;
            _ackAwaiter = ackAwaiter;
            _inboxWriter = inboxWriter;
            _heartbeat = heartbeat;
            _signals = signals;
            _logger = logger;
        }
        public async Task<PollResponse> PollAsync(string deviceIp, List<InBoxDeviceMessage>? reports, CancellationToken ct)
        {
            var key = ToolsDate.Key(deviceIp);
            await _heartbeat.UpdateAsync(deviceIp, ct);

            // --- ingest reports ---
            if (reports is { Count: > 0 })
            {
                var toStore = new List<InBoxDeviceMessage>(reports.Count);
                foreach (var msg in reports)
                {
                    msg.DeviceIp = key;

                    try
                    {
                        switch (msg.MessageType)
                        {
                            case MessageType.ScreenshotAck:
                                {
                                    var pl = JsonSerializer.Deserialize<ScreenshotAckPayload>(msg.Payload);
                                    if (pl is not null && pl.CommandId != Guid.Empty && !string.IsNullOrEmpty(pl.DataBase64))
                                    {
                                        var bytes = Convert.FromBase64String(pl.DataBase64);
                                        _await.TrySetResult(pl.CommandId, bytes);
                                        await _outBoxService.MarkCommandAsProcessedAsync(pl.CommandId, ct);
                                    }
                                    break;
                                }
                            case MessageType.EJournal:
                                {
                                    var pl = JsonSerializer.Deserialize<JournalAckPayload>(msg.Payload);
                                    if (pl != null && pl.CommandId != Guid.Empty)
                                    {
                                        var bytes = string.IsNullOrEmpty(pl.DataBase64) ? Array.Empty<byte>() : Convert.FromBase64String(pl.DataBase64);
                                        _await.TrySetResult(pl.CommandId, bytes);
                                        await _outBoxService.MarkCommandAsProcessedAsync(pl.CommandId, ct);
                                    }
                                    else if (pl != null && pl.CommandId == Guid.Empty)
                                    {
                                        var bytes = string.IsNullOrEmpty(pl.DataBase64) ? null : Convert.FromBase64String(pl.DataBase64);
                                        await _outBoxService.MarkAutoJournalProccessor(msg.DeviceIp, bytes, ct);
                                    }
                                    break;
                                }
                            case MessageType.CommandAck:
                                {
                                    var pl = JsonSerializer.Deserialize<CommandAckPayload>(msg.Payload);
                                    if (pl is not null && pl.CommandId != Guid.Empty)
                                    {
                                        _ackAwaiter.TrySetAck(pl.CommandId, new CommandAck { CommandId = pl.CommandId, Accepted = pl.Accepted, Message = pl.Message });
                                        await _outBoxService.MarkCommandAsProcessedAsync(pl.CommandId, ct);
                                    }
                                    toStore.Add(msg);
                                    break;
                                }
                            case MessageType.ErrorReport:
                                {
                                    using var doc = JsonDocument.Parse(msg.Payload);
                                    if (doc.RootElement.TryGetProperty("CommandId", out var idProp) &&
                                        Guid.TryParse(idProp.GetString(), out var cmdId) && cmdId != Guid.Empty)
                                    {
                                        await _outBoxService.MarkCommandAsFailedAsync(cmdId, msg.DeviceIp, ct);
                                    }
                                    break;
                                }
                            case MessageType.FileUpload:
                                {
                                    var pl = JsonSerializer.Deserialize<CommandAckPayload>(msg.Payload);
                                    if (pl is not null && pl.CommandId != Guid.Empty)
                                    {
                                        _ackAwaiter.TrySetAck(pl.CommandId, new CommandAck { CommandId = pl.CommandId, Accepted = pl.Accepted, Message = pl.Message });
                                        await _outBoxService.MarkCommandAsProcessedAsync(pl.CommandId, ct);
                                    }
                                    break;
                                }
                            case MessageType.Group:
                                {
                                    var pl = JsonSerializer.Deserialize<SendGroupInstructionModel>(msg.Payload);
                                    await _outBoxService.MarkCommandGroupProcessedAsync(pl, ct);
                                    break;
                                }
                            default:
                                toStore.Add(msg);
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Report handling failed for {ip} type={type}", msg.DeviceIp, msg.MessageType);
                        await _outBoxService.MarkReportFailedSafeAsync(msg, ct);
                    }
                }

                if (toStore.Count > 0)
                    _inboxWriter.Enqueue(key, toStore); // حتماً bounded
            }

            // --- fast path: hot queue ---
            var cmds = TryDequeueHot(deviceIp); // داخلش خودش key می‌کنه
            if (cmds?.Count > 0)
                return new PollResponse { ServerTime = DateTime.UtcNow, Commands = cmds };

            // --- cold path: DB once ---
            var pending = await _outBoxService.GetPendingCommandsAsync(key, ct);
            if (pending.Any())
                return new PollResponse { ServerTime = DateTime.UtcNow, Commands = pending };

            // --- wait for signal ---
            var jitter = TimeSpan.FromMilliseconds(Random.Shared.Next(0, 50));
            var signaled = await _signals.WaitAsync(key, TimeSpan.FromSeconds(30) + jitter, ct);
            _logger.LogDebug("Wait completed. signaled={sig} key={key}", signaled, key);

            cmds = TryDequeueHot(deviceIp);
            if (cmds?.Count > 0)
                return new PollResponse { ServerTime = DateTime.UtcNow, Commands = cmds };

            if (signaled)
            {
                pending = await _outBoxService.GetPendingCommandsAsync(key, ct);
            }

            return new PollResponse { ServerTime = DateTime.UtcNow, Commands = pending };
        }
        //public async Task<PollResponse> PollAsync(string deviceIp, List<InBoxDeviceMessage>? reports, CancellationToken ct)
        //{
        //    var key = ToolsDate.Key(deviceIp);
        //    await _heartbeat.UpdateAsync(deviceIp, ct);


        //    if (reports is { Count: > 0 })
        //    {
        //        var toStore = new List<InBoxDeviceMessage>(reports.Count);
        //        foreach (var msg in reports)
        //        {
        //            msg.DeviceIp = deviceIp;

        //            if (msg.MessageType == MessageType.ScreenshotAck)
        //            {
        //                try
        //                {
        //                    var pl = JsonSerializer.Deserialize<ScreenshotAckPayload>(msg.Payload);
        //                    if (pl is not null && pl.CommandId != Guid.Empty && !string.IsNullOrEmpty(pl.DataBase64))
        //                    {
        //                        var bytes = Convert.FromBase64String(pl.DataBase64);
        //                        _await.TrySetResult(pl.CommandId, bytes);
        //                        await _outBoxService.MarkCommandAsProcessedAsync(pl.CommandId, ct);
        //                    }
        //                }
        //                catch { }
        //                continue;
        //            }

        //            if (msg.MessageType == MessageType.EJournal)
        //            {
        //                try
        //                {
        //                    var pl = JsonSerializer.Deserialize<JournalAckPayload>(msg.Payload);



        //                    if (pl != null && pl.CommandId != Guid.Empty)
        //                    {
        //                        var bytes = !string.IsNullOrEmpty(pl.DataBase64)
        //                          ? Convert.FromBase64String(pl.DataBase64)
        //                          : Array.Empty<byte>();



        //                        _await.TrySetResult(pl.CommandId, bytes);


        //                        await _outBoxService.MarkCommandAsProcessedAsync(pl.CommandId, ct);
        //                    }
        //                    else if (pl != null && pl.CommandId == Guid.Empty)
        //                    {
        //                        var bytes = !string.IsNullOrEmpty(pl.DataBase64)
        //                          ? Convert.FromBase64String(pl.DataBase64)
        //                          : null;

        //                        await _outBoxService.MarkAutoJournalProccessor(msg.DeviceIp, bytes, ct);
        //                    }
        //                }
        //                catch (Exception ex)
        //                {

        //                    Console.WriteLine($"Error processing EJournal for Device IP {msg.DeviceIp}: {ex.Message}");
        //                }
        //                continue;
        //            }

        //            if (msg.MessageType == MessageType.CommandAck)
        //            {
        //                try
        //                {
        //                    var pl = JsonSerializer.Deserialize<CommandAckPayload>(msg.Payload);
        //                    if (pl is not null && pl.CommandId != Guid.Empty)
        //                    {
        //                        _ackAwaiter.TrySetAck(pl.CommandId, new CommandAck
        //                        {
        //                            CommandId = pl.CommandId,
        //                            Accepted = pl.Accepted,
        //                            Message = pl.Message
        //                        });
        //                        await _outBoxService.MarkCommandAsProcessedAsync(pl.CommandId, ct);
        //                    }
        //                }
        //                catch { }

        //                toStore.Add(msg);
        //                continue;
        //            }
        //            if (msg.MessageType == MessageType.ErrorReport)
        //            {
        //                try
        //                {
        //                    // payload شما قبلاً به شکل new { CommandId, Error, Time } بود
        //                    // پس یک DTO ساده بساز یا از JsonDocument استفاده کن
        //                    using var doc = JsonDocument.Parse(msg.Payload);
        //                    if (doc.RootElement.TryGetProperty("CommandId", out var idProp) &&
        //                        Guid.TryParse(idProp.GetString(), out var cmdId) &&
        //                        cmdId != Guid.Empty)
        //                    {
        //                        var errText = doc.RootElement.TryGetProperty("Error", out var eProp) ? eProp.GetString() : "agent error";
        //                        await _outBoxService.MarkCommandAsFailedAsync(cmdId,msg.DeviceIp, ct);
        //                    }
        //                }
        //                catch { /* لاگ */ }
        //                continue;
        //            }
        //            if (msg.MessageType == MessageType.FileUpload)
        //            {
        //                try
        //                {
        //                    var pl = JsonSerializer.Deserialize<CommandAckPayload>(msg.Payload);
        //                    _ackAwaiter.TrySetAck(pl.CommandId, new CommandAck
        //                    {
        //                        CommandId = pl.CommandId,
        //                        Accepted = pl.Accepted,
        //                        Message = pl.Message
        //                    });
        //                    await _outBoxService.MarkCommandAsProcessedAsync(pl.CommandId, ct);

        //                }
        //                catch { }
        //                continue;
        //            }
        //            if (msg.MessageType == MessageType.Group)
        //            {
        //                var pl = JsonSerializer.Deserialize<SendGroupInstructionModel>(msg.Payload);
        //                await _outBoxService.MarkCommandGroupProcessedAsync(pl, ct);
        //            }

        //            toStore.Add(msg);
        //        }
        //        if (toStore.Count > 0)
        //            _inboxWriter.Enqueue(deviceIp, toStore);
        //    }
        //    var cmds = TryDequeueHot(key);
        //    if (cmds != null && cmds.Count > 0)
        //        return new PollResponse { ServerTime = DateTime.UtcNow, Commands = cmds };

        //    var pending = await _outBoxService.GetPendingCommandsAsync(key, ct);
        //    if (pending.Any())
        //        return new PollResponse { ServerTime = DateTime.UtcNow, Commands = pending };

        //    var t0 = DateTime.Now;
        //    var jitter = TimeSpan.FromMilliseconds(Random.Shared.Next(0, 50));
        //    var res=  await _signals.WaitAsync(key, TimeSpan.FromSeconds(30)+jitter, ct);

        //    var waitedMs = (DateTime.Now - t0).TotalMilliseconds;
        //    _logger.LogCritical("Waited {ms} ms; signaled={sig}; key={key}", waitedMs, res, key);
        //    cmds = TryDequeueHot(key);

        //    if (cmds != null && cmds.Count > 0)
        //        return new PollResponse { ServerTime = DateTime.UtcNow, Commands = cmds };

        //    pending = await _outBoxService.GetPendingCommandsAsync(key, ct);
        //    return new PollResponse { ServerTime = DateTime.UtcNow, Commands = pending };
        //}
        public async Task EnqueueCommandAsync(string deviceIp, OutBoxDeviceMessage cmd, CancellationToken ct)
        {
            var key = ToolsDate.Key(deviceIp);
            cmd.DeviceIp = key; // مهم
            await _outBoxService.EnqueueCommandAsync(cmd, ct);

            _hot.GetOrAdd(key, _ => new ConcurrentQueue<OutBoxDeviceMessage>()).Enqueue(cmd);

            await _signals.Pulse(key);
            _logger.LogInformation("PULSE sent for key={key}, cmdId={id}", key, cmd.Id); // 👈 بیدار کردن فوری long-poll
            Console.WriteLine("PULSE sent for key={key}, cmdId={id}", key, cmd.Id);
        }

        // در PollAsync:
        private List<OutBoxDeviceMessage> TryDequeueHot(string deviceIp)
        {
            var key = ToolsDate.Key(deviceIp);
            if (_hot.TryGetValue(key, out var q) && q != null && q.TryDequeue(out var cmd))
                return new List<OutBoxDeviceMessage> { cmd };
            return null;
        }
        private static string Key(string s) => (s ?? "").Trim().ToLowerInvariant();
        //public async Task<PollResponse> PollAsync(string deviceIp, List<InBoxDeviceMessage>? reports, CancellationToken ct)
        //{
        //    await _deviceService.UpdateHeartbeatAsync(deviceIp, ct);

        //    if (reports is { Count: > 0 })
        //    {
        //        foreach (var msg in reports)
        //        {
        //            msg.DeviceIp = deviceIp;

        //            if (msg.MessageType == MessageType.ScreenshotAck)
        //            {
        //                try
        //                {
        //                    var pl = JsonSerializer.Deserialize<ScreenshotAckPayload>(msg.Payload);
        //                    if (pl is not null && pl.CommandId != Guid.Empty && !string.IsNullOrEmpty(pl.DataBase64))
        //                    {
        //                        var bytes = Convert.FromBase64String(pl.DataBase64);
        //                        _await.TrySetResult(pl.CommandId, bytes);
        //                        await _outBoxService.MarkCommandAsProcessedAsync(pl.CommandId, ct);
        //                    }
        //                }
        //                catch { /* optional: log */ }
        //                continue; // اسکرین‌شات ذخیره نشود
        //            }
        //            if (msg.MessageType == MessageType.CommandAck)
        //            {

        //                    var pl = JsonSerializer.Deserialize<CommandAckPayload>(msg.Payload);
        //                    if (pl is not null && pl.CommandId != Guid.Empty)
        //                    {
        //                        _ackAwaiter.TrySetAck(pl.CommandId, new CommandAck
        //                        {
        //                            CommandId = pl.CommandId,
        //                            Accepted = pl.Accepted,
        //                            Message = pl.Message
        //                        });

        //                        await _outBoxService.MarkCommandAsProcessedAsync(pl.CommandId, ct);
        //                    }


        //                 }




        //            await _inBoxService.StoreMessageAsync(msg, ct);
        //        }
        //    }

        //    var pending = await _outBoxService.GetPendingCommandsAsync(deviceIp, ct);
        //    if (pending.Any())
        //        return new PollResponse { ServerTime = DateTime.UtcNow, Commands = pending };

        //    // ✅ long-poll تا 30s با چک‌های سریع‌تر (250ms)
        //    var timeout = TimeSpan.FromSeconds(30);
        //    var start = DateTime.UtcNow;

        //    while (!ct.IsCancellationRequested && DateTime.UtcNow - start < timeout)
        //    {
        //        pending = await _outBoxService.GetPendingCommandsAsync(deviceIp, ct);
        //        if (pending.Any())
        //            return new PollResponse { ServerTime = DateTime.UtcNow, Commands = pending };

        //        await Task.Delay(250, ct); 
        //    }

        //    return new PollResponse { ServerTime = DateTime.UtcNow, Commands = new List<OutBoxDeviceMessage>() };
        //}


    }
}
