using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using NaraEyes.Application.Contracts.Models.Basic;
using NaraEyes.Domain.Entities.Base;
using NaraEyes.Domain.Entities.Identity;
using NaraEyes.Infrastructure.Persistence.Context;

namespace NaraEyes.WebApplication.Extensions
{
 
    public static class AppDataSeeder
    {
        public static async Task SeedAsync(IServiceProvider sp, CancellationToken ct = default)
        {
            using var scope = sp.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // 1) Roles
            string[] roles = { "حراست مرکزی", "مدیریت استان", "مدیریت مانیتورینگ", "مدیریت مرکزی" };
            foreach (var r in roles)
                if (!await roleManager.RoleExistsAsync(r))
                    await roleManager.CreateAsync(new Role { Name = r, NormalizedName = r.ToUpperInvariant() });

            // 2) Admin user
            var adminEmail = "admin@Nara12";
            var admin = await userManager.FindByNameAsync(adminEmail);
            if (admin == null)
            {
                admin = new User
                {
                    Id = Guid.NewGuid(),
                    UserName = adminEmail,
                    Email = adminEmail,
                
                    EmailConfirmed = true,
                    PhoneNumberConfirmed = true
                };
                admin.SetName("ادمین", "مرکزی");
                // پسورد را از appsettings بگیر
                var pwd = scope.ServiceProvider
                    .GetRequiredService<IOptions<ApplicationSettings>>().Value.AdminPassword ?? "Nara@@12";
                var create = await userManager.CreateAsync(admin, pwd);
                if (!create.Succeeded) throw new Exception(string.Join(" | ", create.Errors.Select(e => e.Description)));
            }

            // 3) نقش‌های ادمین
       

            if (!await userManager.IsInRoleAsync(admin, "مدیریت مرکزی"))
            {
                var addRole = await userManager.AddToRoleAsync(admin, "مدیریت مرکزی");
                if (!addRole.Succeeded)
                    throw new Exception(string.Join(" | ", addRole.Errors.Select(e => e.Description)));
            }

        }
    }

}
