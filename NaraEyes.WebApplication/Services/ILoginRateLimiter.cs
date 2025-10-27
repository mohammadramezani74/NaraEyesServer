using Microsoft.Extensions.Caching.Memory;

namespace NaraEyes.WebApplication.Services
{
    public interface ILoginRateLimiter
    {
        bool IsAllowed(string username, string ip);
    }

    public class LoginRateLimiter : ILoginRateLimiter
    {
        private readonly IMemoryCache _cache;
        private const int LimitPerMinute = 5;
        public LoginRateLimiter(IMemoryCache cache) => _cache = cache;

        public bool IsAllowed(string username, string ip)
        {
            var key = $"login:{username?.Trim().ToLowerInvariant()}:{ip}";
            var count = _cache.GetOrCreate(key, e =>
            {
                e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
                return 0;
            });
            int next = (int)count + 1;
            _cache.Set(key, next, TimeSpan.FromMinutes(1));
            return next <= LimitPerMinute;
        }
    }
}
