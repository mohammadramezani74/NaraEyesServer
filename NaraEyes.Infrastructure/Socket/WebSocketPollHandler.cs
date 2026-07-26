using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NaraEyes.Application.Contracts.Interfaces.Devices;
using NaraEyes.Application.Contracts.Models.Devices;
using NaraEyes.Domain.Entities.Base;

using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

public class WebSocketPollHandler
{
    private readonly IDevicePollingService _deviceChannel; // جایی که PollAsync داخلشه
    private readonly ILogger<WebSocketPollHandler> _logger;

    public WebSocketPollHandler(IDevicePollingService deviceChannel,
                                ILogger<WebSocketPollHandler> logger)
    {
        _deviceChannel = deviceChannel;
        _logger = logger;
    }

    public async Task HandleAsync(HttpContext context)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        // آی‌پی دستگاه از querystring مثل قبل: /ws?ip=192.168.1.10
        var deviceIp = context.Request.Query["ip"].ToString();
        if (string.IsNullOrWhiteSpace(deviceIp))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("ip query parameter is required.");
            return;
        }

        using var socket = await context.WebSockets.AcceptWebSocketAsync();

        _logger.LogInformation("WebSocket connected for device {Ip}", deviceIp);

        var buffer = new byte[64 * 1024];
        var ct = context.RequestAborted;

        try
        {
            while (!ct.IsCancellationRequested && socket.State == WebSocketState.Open)
            {
                // ========== دریافت درخواست از Agent ==========
                var received = await ReceiveFullMessageAsync(socket, buffer, ct);
                if (received == null)
                {
                    // client closed
                    break;
                }

                string requestJson = received;

                // در پروتکل انتخابی ما، body = List<InBoxDeviceMessage>
                List<InBoxDeviceMessage>? reports = null;
                if (!string.IsNullOrWhiteSpace(requestJson) &&
                    requestJson != "[]" &&
                    requestJson != "null")
                {
                    try
                    {
                        reports = JsonSerializer.Deserialize<List<InBoxDeviceMessage>>(requestJson);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to deserialize reports from device {Ip}", deviceIp);
                        reports = null;
                    }
                }

                // ========== صدا زدن همان PollAsync ==========
                PollResponse resp;
                try
                {
                    resp = await _deviceChannel.PollAsync(deviceIp, reports, ct);
                }
                catch (OperationCanceledException)
                {
                    // connection aborted
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "PollAsync failed for device {Ip}", deviceIp);
                    resp = new PollResponse
                    {
                        ServerTime = DateTime.UtcNow,
                        Commands = new List<OutBoxDeviceMessage>()
                    };
                }

                // ========== ارسال پاسخ روی WebSocket ==========
                string responseJson = JsonSerializer.Serialize(resp);
                var responseBytes = Encoding.UTF8.GetBytes(responseJson);

                await socket.SendAsync(new ArraySegment<byte>(responseBytes),
                    WebSocketMessageType.Text,
                    endOfMessage: true,
                    cancellationToken: ct);
            }
        }
        finally
        {
            if (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseReceived)
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server closing", CancellationToken.None);
            }

            _logger.LogInformation("WebSocket closed for device {Ip}", deviceIp);
        }
    }

    private static async Task<string?> ReceiveFullMessageAsync(WebSocket socket, byte[] buffer, CancellationToken ct)
    {
        var sb = new StringBuilder();

        while (true)
        {
            try
            {

         
            var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), ct);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                return null;
            }

            var chunk = Encoding.UTF8.GetString(buffer, 0, result.Count);
            sb.Append(chunk);

            if (result.EndOfMessage)
                break;
            }
            catch (Exception)
            {

               
            }
        }

        return sb.ToString();
    }
}
