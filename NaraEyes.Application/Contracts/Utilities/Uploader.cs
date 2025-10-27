using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;

namespace NaraEyes.Application.Contracts.Utilities
{
    public static class Uploader
    {
        public static async Task<string> Upload(this IBrowserFile file, Microsoft.AspNetCore.Hosting.IHostingEnvironment env)
        {
            if (file == null) return "";
            var root = env.WebRootPath;
            var directoryPath = Path.Combine(root, "BulkOperations");

            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);

            var ext = Path.GetExtension(file.Name); // استخراج پسوند
            var name = Path.GetFileNameWithoutExtension(file.Name);
            var fileName = $"GroupFile_{DateTime.Now:yyyyMMddHHmmss}_{RandomNumberGenerator.GetInt32(1000, 9999)}{ext}";
            var filePath = $"{directoryPath}//{fileName}";
            using var output = File.Create(filePath);

            await file.OpenReadStream(maxAllowedSize: 1L * 1024 * 1024 * 1024).CopyToAsync(output);
            return $"BulkOperations/{fileName}";

        }
        public static void DeleteFile(string filePath, Microsoft.AspNetCore.Hosting.IHostingEnvironment host)
        {
            var fullFilePath = Path.Combine(host.WebRootPath, "ApplicationPictures", filePath);

            if (File.Exists(fullFilePath))
            {
                File.Delete(fullFilePath);
            }
        }
    }
}
