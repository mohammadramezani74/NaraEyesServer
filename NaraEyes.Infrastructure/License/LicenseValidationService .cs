using Microsoft.AspNetCore.Hosting;
using Microsoft.IdentityModel.Tokens;
using NaraEyes.Application.Abstraction.License;
using NaraEyes.Application.Contracts.Models.License;
using NaraEyes.Infrastructure.License.Key;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Cryptography;

namespace NaraEyes.Infrastructure.License
{
    public class LicenseValidationService : ILicenseValidationService
    {
        private readonly  Microsoft.AspNetCore.Hosting.IHostingEnvironment _env;
        private readonly string _licenseFilePath;
        private readonly string _publicKeyPem;

        public LicenseValidationService(IHostingEnvironment env)
        {
            _env = env;
            _licenseFilePath = Path.Combine(_env.ContentRootPath, "NaraEyes_license.lic");

  
            var assembly = typeof(keyFile).Assembly;
            var all = assembly.GetManifestResourceNames();
            using var stream = assembly.GetManifestResourceStream("NaraEyes.Infrastructure.License.public_key.pem");
            using var reader = new StreamReader(stream);
            _publicKeyPem = reader.ReadToEnd();
        }

        private string GetLicenseToken()
        {
            if (!File.Exists(_licenseFilePath))
                return null;
            return File.ReadAllText(_licenseFilePath).Trim();
        }

        private bool ValidateToken(string token, out JwtSecurityToken validatedToken)
        {
            validatedToken = null;
            try
            {
                var rsa = RSA.Create();
                rsa.ImportFromPem(_publicKeyPem.ToCharArray());

                var validationParams = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true, 
                    IssuerSigningKey = new RsaSecurityKey(rsa),
                    ValidateIssuerSigningKey = true
                };

                var handler = new JwtSecurityTokenHandler();
                var principal = handler.ValidateToken(token, validationParams, out var tokenObj);
                validatedToken = tokenObj as JwtSecurityToken;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> IsLicenseValidAsync()
        {
            var token = GetLicenseToken();
            if (string.IsNullOrEmpty(token))
                return false;

            var isValid = ValidateToken(token, out var jwtToken);
            if (!isValid) return false;

            var expClaim = jwtToken?.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Exp)?.Value;
            if (expClaim != null)
            {
                var expDate = DateTimeOffset.FromUnixTimeSeconds(long.Parse(expClaim)).UtcDateTime;
                if (expDate < DateTime.UtcNow)
                    return false;
            }

            return true;
        }

        public async Task<LicenseInfo> GetLicenseInfoAsync()
        {
            var token = GetLicenseToken();
            if (!ValidateToken(token, out var jwtToken))
                return null;

            return new LicenseInfo
            {
                LicenseId = jwtToken.Claims.FirstOrDefault(c => c.Type == "LicenseId")?.Value,
                CustomerEmail = jwtToken.Claims.FirstOrDefault(c => c.Type == "Email")?.Value,
                ExpirationDate = DateTimeOffset.FromUnixTimeSeconds(
                    long.Parse(jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Exp).Value)
                ).UtcDateTime
            };
        }

        public async Task<bool> RenewLicenseAsync(string newLicenseToken)
        {
       
            if (!ValidateToken(newLicenseToken, out _))
                return false;

            await File.WriteAllTextAsync(_licenseFilePath, newLicenseToken);
            return true;
        }
    }
}
