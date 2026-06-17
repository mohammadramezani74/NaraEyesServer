using Serilog.Context;

namespace NaraEyes.WebApplication.Extensions
{
    public class LoggingMiddleware
    {
        private readonly RequestDelegate _next;

        public LoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
          
            var userName = context.User.Identity?.Name ?? "Anonymous";
            var ip = context.Connection.RemoteIpAddress?.ToString();
            var requestId = context.TraceIdentifier;
            var userAgent = context.Request.Headers["User-Agent"].ToString();

            using (Serilog.Context.LogContext.PushProperty("UserName", userName))
            using (Serilog.Context.LogContext.PushProperty("IP", ip))
            using (Serilog.Context.LogContext.PushProperty("RequestId", requestId))
            using (Serilog.Context.LogContext.PushProperty("UserAgent", userAgent))
            {
                await _next(context);
            }
        }
    }
}
