using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NaraEyes.Application.Abstraction.Identity;
using NaraEyes.Application.Abstraction.Unitofwork;
using NaraEyes.Application.Contracts.Models.Basic;
using NaraEyes.Domain.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using static NaraEyes.Infrastructure.IdentityRepository.ApplicationUserManager;

namespace NaraEyes.Infrastructure.IdentityRepository
{

        internal class ApplicationUserManager(IHttpContextAccessor httpContextAccessor,
  UserManager<User> userManager,
  IApplicationUnitOfWork unitOfWork,
  IApplicationRoleManager roleManager) : IApplicationUserManager
        {
            private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
            private readonly UserManager<User> _userManager = userManager;
            private readonly IApplicationRoleManager _roleManager = roleManager;
            private readonly IApplicationUnitOfWork _unitOfWork = unitOfWork;
            public Guid? UserId =>
                _httpContextAccessor
                    .HttpContext?
                    .User
                    .GetUserId() ?? null;


            public async Task<List<string>> GetUserClaims(Guid userId, CancellationToken cancellationToken = default)
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                    return new List<string>();


                var userClaims = await _userManager.GetClaimsAsync(user);
                var claimList = userClaims.Select(c => c.Value).ToList();



                var roles = await _roleManager.GetRolesByUserId(userId);

                var roleClaims = new List<string>();
                foreach (var role in roles)
                {

                    if (role != null)
                    {
                        var rClaims = await _roleManager.GetClaims(role.RoleId);
                        roleClaims.AddRange(rClaims);
                    }
                }


                var allClaims = claimList.Concat(roleClaims).ToList();
                return allClaims;
            }



            public async Task<OperationResult> CreateUserClaimsAsync(Guid UserId, List<string> claims, CancellationToken cancellationToken = default(CancellationToken))
            {
                var user = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == UserId, cancellationToken);
                if (user is null) return new OperationResult().NotFound("کاربر مورد نظر یافت نشد");
                var claimValues = claims
                                       .Select(c => c.Trim())
                                       .ToList();


                var existingClaims = await _userManager.GetClaimsAsync(user);


                var validNewClaims = claimValues
                    .Where(claimValue =>
                        !existingClaims.Any(existingClaim =>
                            existingClaim.Type == "Permission" && existingClaim.Value == claimValue))
                    .Select(claimValue => new Claim("Permission", claimValue))
                    .ToList();


                if (!validNewClaims.Any())
                {
                    return new OperationResult().succedded("کلایم‌های جدیدی برای اضافه کردن وجود ندارد.");
                }


                var result = await _userManager.AddClaimsAsync(user, validNewClaims);


                if (result.Succeeded)
                {
                    return new OperationResult().succedded("کلایم‌ها با موفقیت اضافه شدند.");
                }
                else
                {

                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return new OperationResult().Failed(errors);
                }
            }

            public async Task<User?> GetUserBy(string username, string password)
            {

                var user = await _userManager.FindByNameAsync(username);
                if (user == null)
                {
                    return null;
                }


                var passwordValid = await _userManager.CheckPasswordAsync(user, password);
                if (!passwordValid)
                {
                    return null;
                }

                return user;
            }
            //public async Task<bool> ChangePasswordAsync(string nationalCode, string phoneNumber, string password)
            //{
            //    var user = _userManager.Users.SingleOrDefault(u => u.NationalCode == nationalCode && u.PhoneNumber == phoneNumber);

            //    if (user == null)
            //    {
            //        return false;
            //    }
            //    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            //    var result = await _userManager.ResetPasswordAsync(user, token, password);
            //    if (!result.Succeeded)
            //    {
            //        return false;
            //    }

            //    return true;
            //}

            public async Task<User?> GetUserBy(Guid Id)
            {
                var user = await _userManager.FindByIdAsync(Id.ToString());
                return user;
            }
            public async Task<List<string>> GetAllCalimsByUserId(Guid UserId)
            {
                var user = await _userManager.FindByIdAsync(UserId.ToString());
                var claims = new List<string>();
                var Uclaims = await _userManager.GetClaimsAsync(user!);
                var userClaims = Uclaims.Select(x => x.Value).ToList();
                claims.AddRange(userClaims);
                var roles = await _roleManager.GetRolesByUserId(user!.Id);
                List<Guid> RoleIds = new();
                foreach (var role in roles)
                {

                    RoleIds.Add(role.RoleId);
                }
                if (RoleIds.Count > 0)
                {
                    foreach (var roleId in RoleIds)
                    {
                        var roleClaims = await _roleManager.GetClaims(roleId);
                        foreach (string claim in roleClaims)
                        {
                            claims.Add(claim);
                        }
                    }

                }

                return claims;

            }

            public bool ExistUserBy(Guid Id)
            {
                return _userManager.Users.Any(x => x.Id == Id);
            }

        }
    }

