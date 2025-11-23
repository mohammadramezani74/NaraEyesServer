using DocumentFormat.OpenXml.Office2010.ExcelAc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NaraEyes.Application.Abstraction.Identity;
using NaraEyes.Application.Abstraction.Unitofwork;
using NaraEyes.Application.Contracts.Models.Basic;
using NaraEyes.Application.Contracts.Models.Identity;
using NaraEyes.Domain.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Infrastructure.IdentityRepository
{
    internal class ApplicationRoleManager(RoleManager<Role> roleManager,
          UserManager<User> userManager,
          IApplicationUnitOfWork _uow) : IApplicationRoleManager
    {
        private readonly RoleManager<Role> _roleManager = roleManager;
        private readonly UserManager<User> _userManager = userManager;
        private readonly IApplicationUnitOfWork uow = _uow;

        /// <summary>
        /// افزودن نقش به کاربر
        /// </summary>
        /// <param name="RoleId"></param>
        /// <param name="UserId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<OperationResult> AddUserToRole(Guid roleId, Guid userId, CancellationToken cancellationToken = default)
        {
            var op = new OperationResult();

            // 1) نقش و کاربر
            var role = await _roleManager.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);
            if (role is null)
                return op.NotFound("نقش مورد نظر شما یافت نشد.");

            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
            if (user is null)
                return op.NotFound("کاربر مورد نظر شما یافت نشد.");

            // 2) نقش‌های فعلی کاربر
            var currentRoleNames = await _userManager.GetRolesAsync(user);
            var targetRoleName = role.Name!;

            // اگر همین نقش را دارد
            if (currentRoleNames.Contains(targetRoleName))
            {
                // نقش‌های اضافه را پاک کن تا تک‌نقشی شود
                var extras = currentRoleNames.Where(rn => rn != targetRoleName).ToArray();
                if (extras.Length > 0)
                {
                    var removeRes = await _userManager.RemoveFromRolesAsync(user, extras);
                    if (!removeRes.Succeeded)
                    {
                        var msg = string.Join("، ", removeRes.Errors.Select(e => e.Description));
                        return op.Failed(string.IsNullOrWhiteSpace(msg) ? "حذف نقش‌های اضافه ناموفق بود." : msg);
                    }
                }

                return op.succedded("نقش کاربر به‌روزرسانی شد.");
            }

            // اگر نقش‌های دیگری دارد (و این نقش را ندارد) → همه را پاک کن
            if (currentRoleNames.Count > 0)
            {
                var removeRes = await _userManager.RemoveFromRolesAsync(user, currentRoleNames);
                if (!removeRes.Succeeded)
                {
                    var msg = string.Join("، ", removeRes.Errors.Select(e => e.Description));
                    return op.Failed(string.IsNullOrWhiteSpace(msg) ? "حذف نقش‌های قبلی ناموفق بود." : msg);
                }
            }

            // 3) نقش جدید را اضافه کن
            var addRes = await _userManager.AddToRoleAsync(user, targetRoleName);
            if (!addRes.Succeeded)
            {
                var msg = string.Join("، ", addRes.Errors.Select(e => e.Description));
                return op.Failed(string.IsNullOrWhiteSpace(msg) ? "افزودن نقش جدید ناموفق بود." : msg);
            }

            return op.succedded("نقش کاربر با موفقیت تنظیم شد.");
        }

        /// <summary>
        /// افزودن نقش جدید
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<OperationResult> CreateRole(string Name, CancellationToken cancellationToken = default)
        {
            var op = new OperationResult();
            var result = await _roleManager.CreateAsync(new Role { Name = Name, ConcurrencyStamp = Guid.NewGuid().ToString() });
            if (result.Succeeded)
            {
                return op.succedded();
            }
            foreach (var error in result.Errors)
            {
                op.Failed(error.Description);
            }
            return op.Failed("عملیات با خطا مواجه شد");
        }
        /// <summary>
        /// افزودن ادعا به نقش جدید
        /// </summary>
        /// <param name="roleId"></param>
        /// <param name="claims"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<OperationResult> CreateRoleClaimsAsync(Guid roleId, List<string> claims, CancellationToken cancellationToken = default)
        {
            var op = new OperationResult();

            var role = await _roleManager.Roles.FirstOrDefaultAsync(c => c.Id == roleId, cancellationToken);
            if (role == null)
            {
                return op.NotFound("نقش مورد نظر شما یافت نشد");
            }


            if (claims == null || !claims.Any())
            {
                return op.Failed("لیست ادعا ها نباید خالی باشد");
            }

            foreach (var claimValue in claims)
            {

                var newClaim = new Claim("Permission", claimValue);


                var existingClaims = await _roleManager.GetClaimsAsync(role);
                if (existingClaims.Any(c => c.Type == "Permission" && c.Value == claimValue))
                {
                    continue;
                }


                var result = await _roleManager.AddClaimAsync(role, newClaim);
                if (!result.Succeeded)
                {
                    return op.Failed("خطا در افزودن ادعا به نقش");
                }
            }

            return op.succedded("ادعاها با موفقیت به نقش افزوده شدند");
        }
        /// <summary>
        /// حذف نقش
        /// </summary>
        /// <param name="name"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<OperationResult> DeleteRole(string name, CancellationToken cancellationToken = default)
        {
            var op = new OperationResult();

            var role = await _roleManager.FindByNameAsync(name);
            if (role == null)
            {
                return op.NotFound("نقش مورد نظر یافت نشد");
            }

            var result = await _roleManager.DeleteAsync(role);
            if (!result.Succeeded)
            {

                return op.Failed("خطا در حذف نقش");
            }

            return op.succedded("نقش با موفقیت حذف شد");
        }
        /// <summary>
        /// حذف ادعای یک نقش
        /// </summary>
        /// <param name="roleId"></param>
        /// <param name="claimName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>

        public async Task<OperationResult> DeleteRoleClaimsAsync(Guid roleId, string claimName, CancellationToken cancellationToken = default)
        {
            var op = new OperationResult();


            var role = await _roleManager.Roles.FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);
            if (role == null)
            {
                return op.NotFound("نقش مورد نظر یافت نشد");
            }


            var claims = await _roleManager.GetClaimsAsync(role);


            var claimToRemove = claims.FirstOrDefault(c => c.Type == "Permission" && c.Value == claimName);
            if (claimToRemove == null)
            {
                return op.NotFound("ادعای مورد نظر برای حذف یافت نشد");
            }


            var result = await _roleManager.RemoveClaimAsync(role, claimToRemove);
            if (!result.Succeeded)
            {
                return op.Failed("خطا در حذف ادعا از نقش");
            }


            return op.succedded("ادعای مورد نظر با موفقیت حذف شد");
        }

        /// <summary>
        /// دریافت ادعا های نقش
        /// </summary>
        /// <param name="roleId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<List<string>> GetClaims(Guid roleId, CancellationToken cancellationToken = default)
        {
            List<string> ClaimList = new();

            var role = await _roleManager.Roles.FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);
            if (role == null)
            {
                return ClaimList;
            }
            var claims = await _roleManager.GetClaimsAsync(role);
            foreach (var claim in claims)
            {
                ClaimList.Add(claim.Value);
            }
            return ClaimList;
        }

        public async Task<string?> GetRoleReport(CancellationToken cancellationToken = default)
        {
            var roles = await GetRoles(null);
            using var wb = new ClosedXML.Excel.XLWorkbook();
            var ws = wb.Worksheets.Add("Supervisions");

            ws.Cell(1, 1).Value = "نقش ها";



            ws.Range("A1:G1").Style.Font.Bold = true;

            var row = 2;
            foreach (var item in roles)
            {
                ws.Cell(row, 1).Value = item.Name;

                row++;
            }


            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            var bytes = ms.ToArray();
            var base64 = Convert.ToBase64String(bytes);
            return base64;
        }

        /// <summary>
        /// لیست رول های سیستم
        /// </summary>
        /// <param name="search"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<List<GetRolesResponse>> GetRoles(string? search, CancellationToken cancellationToken = default(CancellationToken))
        {
            var roles = _roleManager.Roles.AsNoTracking();
            if (search is not null)
                roles = roles.Where(x => x.Name!.ToLower().Contains(search!.ToLower()));
            var roleList = await roles.Select(x => new GetRolesResponse(x.Id, x.Name!)
          ).ToListAsync(cancellationToken);
            return roleList;
        }
        /// <summary>
        /// نقش های کاربر
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<UserRolesResponse[]> GetRolesByUserId(Guid userId, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return Array.Empty<UserRolesResponse>();
            }

            var roleNames = await _userManager.GetRolesAsync(user);
            if (roleNames == null || !roleNames.Any())
            {
                return Array.Empty<UserRolesResponse>();
            }

            // لیست آیدی و نام نقش‌ها را برمی‌گردانیم
            var rolesWithIds = new List<UserRolesResponse>();

            foreach (var roleName in roleNames)
            {
                // پیدا کردن نقش با نام
                var role = await _roleManager.FindByNameAsync(roleName);
                if (role != null)
                {
                    rolesWithIds.Add(new UserRolesResponse(role.Id, roleName));
                }
            }

            return rolesWithIds.ToArray();
        }
        /// <summary>
        /// ویرایش نقش
        /// </summary>
        /// <param name="name"></param>
        /// <param name="newName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<OperationResult> UpdateRole(string name, string newName, CancellationToken cancellationToken = default)
        {
            var op = new OperationResult();

            // پیدا کردن نقش بر اساس نام
            var role = await _roleManager.FindByNameAsync(name);
            if (role == null)
            {
                return op.NotFound("نقش مورد نظر یافت نشد");
            }

            // به‌روزرسانی نام نقش
            role.Name = newName;

            // ذخیره تغییرات
            var result = await _roleManager.UpdateAsync(role);
            if (!result.Succeeded)
            {
                return op.Failed("خطا در به‌روزرسانی نقش");
            }

            // در صورت موفقیت
            return op.succedded("نقش با موفقیت به‌روزرسانی شد");
        }
        public   async Task<bool> IsUserInRole(Guid userId, string roleName)
        {
            try
            {

         
            if (string.IsNullOrWhiteSpace(roleName)) return false;
            var normalized = roleName.Trim().ToUpperInvariant();

            return await uow.UserRoles
                .AsNoTracking()
                .Where(ur => ur.UserId == userId)
                .Join(uow.Roles.AsNoTracking(),
                      ur => ur.RoleId,
                      r => r.Id,
                      (ur, r) => r.NormalizedName)
                .AnyAsync(n => n == normalized);
            }
            catch (Exception ex)
            {

                return false;
            }

        }
    }
}
