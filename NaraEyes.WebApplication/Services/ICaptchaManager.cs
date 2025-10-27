using Microsoft.Extensions.Caching.Memory;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Security.Cryptography;
using System.Text;

namespace NaraEyes.WebApplication.Services
{
    public record CaptchaTicket(string TokenId, DateTimeOffset ExpireAt, string CodeHash);

    public interface ICaptchaManager
    {
        (string tokenId, string imageDataUrl) Generate();
        bool ValidateAndConsume(string tokenId, string userInput);
    }

    public class CaptchaManager : ICaptchaManager
    {
        private readonly IMemoryCache _cache;
        private static readonly char[] _alphabet =
            "ABCDEFGHJKLMNPQRSTUVWXYZ23456789".ToCharArray(); // حذف O/0/I/1 برای خطا کمتر
        private readonly TimeSpan _ttl = TimeSpan.FromMinutes(2);

        public CaptchaManager(IMemoryCache cache) => _cache = cache;

        public (string tokenId, string imageDataUrl) Generate()
        {
            var code = CreateCode(6);
            var tokenId = Guid.NewGuid().ToString("N");
            var ticket = new CaptchaTicket(tokenId, DateTimeOffset.UtcNow.Add(_ttl), Hash(code));
            _cache.Set(tokenId, ticket, ticket.ExpireAt);


            var img = RenderPng(code);
            var dataUrl = $"data:image/png;base64,{Convert.ToBase64String(img)}";
            return (tokenId, dataUrl);
        }

        public bool ValidateAndConsume(string tokenId, string userInput)
        {
            if (!_cache.TryGetValue<CaptchaTicket>(tokenId, out var ticket))
                return false;


            _cache.Remove(tokenId);

            if (ticket.ExpireAt < DateTimeOffset.UtcNow) return false;
            return SlowEquals(ticket.CodeHash, Hash(userInput?.Trim() ?? ""));
        }

        private static string CreateCode(int len)
        {
            var rng = RandomNumberGenerator.Create();
            var bytes = new byte[len];
            rng.GetBytes(bytes);
            var sb = new StringBuilder(len);
            for (int i = 0; i < len; i++)
                sb.Append(_alphabet[bytes[i] % _alphabet.Length]);
            return sb.ToString();
        }

        private static string Hash(string s)
        {
            using var sha = SHA256.Create();
            return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(s ?? "")));
        }

        private static bool SlowEquals(string a, string b)
        {
            var ba = Convert.FromHexString(a);
            var bb = Convert.FromHexString(b);
            var diff = ba.Length ^ bb.Length;
            for (int i = 0; i < Math.Min(ba.Length, bb.Length); i++) diff |= ba[i] ^ bb[i];
            return diff == 0;
        }

       
        private static byte[] RenderPng(string code)
        {
            using var img = new Image<Rgba32>(140, 50);
            img.Mutate(ctx =>
            {
                ctx.Fill(Color.White);
                var font = SystemFonts.CreateFont("Arial", 24, FontStyle.Bold);
                var blue = Color.ParseHex("#42A5F5");
                ctx.DrawText(code, font, blue, new PointF(10, 8));
            });
            using var ms = new MemoryStream();
            img.SaveAsPng(ms);
            return ms.ToArray();
        }
    }
}
