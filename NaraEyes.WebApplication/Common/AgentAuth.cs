using System.Security.Cryptography;
using System.Text;

namespace NaraEyes.WebApplication.Common
{
    public static class AgentAuth
    {
        public const string HeaderKey = "X-Agent-Key";

        /// <summary>
        /// مقایسه‌ی زمان-ثابت. مقایسه‌ی معمولی رشته‌ها با اولین بایت متفاوت
        /// خارج می‌شود و از روی زمان پاسخ می‌شود کلید را حدس زد.
        /// </summary>
        public static bool IsValid(string? provided, string expected)
        {
            if (string.IsNullOrEmpty(provided) || string.IsNullOrEmpty(expected))
                return false;

            var a = Encoding.UTF8.GetBytes(provided);
            var b = Encoding.UTF8.GetBytes(expected);

            return CryptographicOperations.FixedTimeEquals(a, b);
        }
    }

    public sealed class AgentKeyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string _expectedKey;
        private readonly ILogger<AgentKeyMiddleware> _logger;

        private static readonly string[] Protected =
        {
        "/api/poll",
        "/api/ws",
        "/api/device/register",
        "/api/device/SubmitMetrics",
        "/api/device/SubmitStatus",
        "/api/device/AgentMode",
    };

        public AgentKeyMiddleware(RequestDelegate next, IConfiguration cfg,
                                  ILogger<AgentKeyMiddleware> logger)
        {
            _next = next;
            _logger = logger;
            _expectedKey = cfg["AgentSecurity:SharedKey"] ?? "";

            if (string.IsNullOrWhiteSpace(_expectedKey))
                _logger.LogWarning("AgentSecurity:SharedKey تنظیم نشده — endpointهای ایجنت محافظت ندارند!");
        }

        public async Task InvokeAsync(HttpContext ctx)
        {
            var path = ctx.Request.Path.Value ?? "";

            bool isProtected = Protected.Any(p =>
                path.StartsWith(p, StringComparison.OrdinalIgnoreCase));

            if (!isProtected || string.IsNullOrWhiteSpace(_expectedKey))
            {
                await _next(ctx);
                return;
            }

            string? key = ctx.Request.Headers[AgentAuth.HeaderKey].FirstOrDefault();

            if (!AgentAuth.IsValid(key, _expectedKey))
            {
                _logger.LogWarning("درخواست بدون کلید معتبر — path={Path} remote={Ip}",
                    path, ctx.Connection.RemoteIpAddress);

                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await ctx.Response.WriteAsync("unauthorized");
                return;
            }

            await _next(ctx);
        }
    }
    }
