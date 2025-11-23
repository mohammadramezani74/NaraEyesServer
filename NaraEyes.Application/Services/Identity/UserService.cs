using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NaraEyes.Application.Abstraction.Identity;
using NaraEyes.Application.Abstraction.Unitofwork;
using NaraEyes.Application.Contracts.Interfaces.Identity;
using NaraEyes.Application.Contracts.Models.Basic;
using NaraEyes.Application.Contracts.Models.Identity;
using NaraEyes.Domain.Entities.Identity;
using System.Threading.Tasks;


namespace NaraEyes.Application.Services.Identity
{
    public class UserService(IApplicationUnitOfWork uow, AuthenticationStateProvider Auth, UserManager<User> userManager, SignInManager<User> SignInManager, IApplicationUserManager applicationUserManager) : IUserService
    {
        private readonly IApplicationUnitOfWork _uow = uow;
        private readonly AuthenticationStateProvider auth = Auth;
        private readonly UserManager<User> _userManager = userManager;

        private readonly SignInManager<User> signInManager = SignInManager;
        private readonly IApplicationUserManager _applicationUserManager=applicationUserManager;
        public async Task<IReadOnlyList<UserViewModel>> AllUsers(CancellationToken cancellationToken)
        {
            var users = await _uow.Users
          .AsNoTracking()
          .OrderBy(u => u.UserName)
          .Select(u => new UserViewModel
          {
              Id = u.Id,
              FullName = u.FirstName + " " + u.LastName,
              UserName = u.UserName!,
              PhoneNumber = u.PhoneNumber,
              LastLoginDate = u.LastLoginDate.HasValue
                              ? u.LastLoginDate.Value.ToString("yyyy/MM/dd HH:mm")
                              : null,
              Role = (
                from ur in _uow.UserRoles
                join r in _uow.Roles on ur.RoleId equals r.Id
                where ur.UserId == u.Id
                select r.Name
            ).FirstOrDefault(),

              RoleId = (
                from ur in _uow.UserRoles
                join r in _uow.Roles on ur.RoleId equals r.Id
                where ur.UserId == u.Id
                select r.Id
            ).FirstOrDefault(),
              IsActive = u.IsActive,
              IsLocked = u.LockoutEnd != null && u.LockoutEnd > DateTimeOffset.UtcNow
          })
          .ToListAsync(cancellationToken);

            return users;
        }
        public async Task<bool> IsUserInRole( string roleName)
        {
            try
            {


                if (string.IsNullOrWhiteSpace(roleName)) return false;
                var state = await Auth.GetAuthenticationStateAsync();
                return state.User?.IsInRole(roleName) ?? false;
            }
            catch (Exception ex)
            {

                return false;
            }

        }


        public async Task<OperationResult> CreateUserAsync(CreateUserModel command, CancellationToken cancellationToken)
        {
            var op = new OperationResult();

            if (command is null)
                return op.Failed("درخواست نامعتبر است.");

            if (string.IsNullOrWhiteSpace(command.UserName))
                return op.Failed("نام کاربری الزامی است.");

            if (string.IsNullOrWhiteSpace(command.Password))
                return op.Failed("رمز عبور الزامی است.");

            if (command.Password != command.ConfirmPassword)
                return op.Failed("رمز عبور و تأیید آن یکسان نیست.");


            var existed = await _userManager.FindByNameAsync(command.UserName);
            if (existed is not null)
                return op.Failed("این نام کاربری قبلاً ثبت شده است.");


            if (!string.IsNullOrWhiteSpace(command.PhoneNumber))
            {
                var normalizedPhone = NormalizePhone(command.PhoneNumber);
                var phoneTaken = await _uow.Users
                    .AsNoTracking()
                    .AnyAsync(u => u.PhoneNumber == normalizedPhone, cancellationToken);
                if (phoneTaken)
                    return op.Failed("این شماره تلفن قبلاً استفاده شده است.");
                command.PhoneNumber = normalizedPhone;
            }

            var user = new User
            {
                UserName = command.UserName.Trim(),
                PhoneNumber = string.IsNullOrWhiteSpace(command.PhoneNumber) ? null : command.PhoneNumber.Trim(),
                PhoneNumberConfirmed = false,
                LockoutEnabled = true

            };
            user.SetName(command.Name, command.LName);


            IdentityResult result;
            try
            {
                result = await _userManager.CreateAsync(user, command.Password);
            }
            catch
            {
                return op.Failed("ایجاد کاربر با خطای غیرمنتظره مواجه شد.");
            }

            if (!result.Succeeded)
            {

                var msg = string.Join("، ", result.Errors.Select(e => e.Description));
                return op.Failed(string.IsNullOrWhiteSpace(msg) ? "ایجاد کاربر ناموفق بود." : msg);
            }

            return op.succedded("کاربر با موفقیت ایجاد شد.");
        }

        public async Task<OperationResult> DeleteUserAsync(Guid userId, CancellationToken cancellationToken)
        {
            var op = new OperationResult();

            try
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user is null)
                    return op.NotFound("کاربر مورد نظر یافت نشد.");

                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    var msg = string.Join("، ", result.Errors.Select(e => e.Description));
                    return op.Failed(string.IsNullOrWhiteSpace(msg) ? "حذف کاربر ناموفق بود." : msg);
                }


                return op.succedded("کاربر با موفقیت حذف شد.");
            }
            catch (Exception)
            {
                return op.Failed("حذف کاربر با خطای غیرمنتظره مواجه شد.");
            }
        }

        public async Task<OperationResult> UpdateUserAsync(UpdateUserModel command, CancellationToken cancellationToken)
        {
            var op = new OperationResult();

            try
            {
                if (command is null)
                    return op.Failed("درخواست نامعتبر است.");


                var user = await _userManager.FindByIdAsync(command.Id.ToString());
                if (user is null)
                    return op.NotFound("کاربر مورد نظر یافت نشد.");


                var existingByUserName = await _userManager.FindByNameAsync(command.UserName);
                if (existingByUserName is not null && existingByUserName.Id != user.Id)
                    return op.Failed("این نام کاربری قبلاً استفاده شده است.");


                if (!string.IsNullOrWhiteSpace(command.PhoneNumber))
                {
                    var normalizedPhone = NormalizePhone(command.PhoneNumber);
                    var phoneTaken = await _uow.Users
                        .AsNoTracking()
                        .AnyAsync(u => u.PhoneNumber == normalizedPhone && u.Id != user.Id, cancellationToken);

                    if (phoneTaken)
                        return op.Failed("این شماره تلفن قبلاً استفاده شده است.");
                    user.PhoneNumber = normalizedPhone;
                }
                else
                {
                    user.PhoneNumber = null;
                }


                user.UserName = command.UserName.Trim();
                user.NormalizedUserName = _userManager.NormalizeName(command.UserName);

                user.SetName(command.Name?.Trim(), command.LName?.Trim());


                user.SetActive(command.IsActive);


                var desiredLocked = command.IsLocked;
                var currentLocked = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow;

                if (desiredLocked != currentLocked)
                {
                    if (desiredLocked)
                    {

                        var lockUntil = DateTimeOffset.UtcNow.AddYears(100);
                        var lockRes = await _userManager.SetLockoutEndDateAsync(user, lockUntil);
                        if (!lockRes.Succeeded)
                        {
                            var msg = string.Join("، ", lockRes.Errors.Select(e => e.Description));
                            return op.Failed(string.IsNullOrWhiteSpace(msg) ? "قفل‌کردن حساب ناموفق بود." : msg);
                        }
                    }
                    else
                    {

                        var unlockRes = await _userManager.SetLockoutEndDateAsync(user, null);
                        if (!unlockRes.Succeeded)
                        {
                            var msg = string.Join("، ", unlockRes.Errors.Select(e => e.Description));
                            return op.Failed(string.IsNullOrWhiteSpace(msg) ? "رفع قفل حساب ناموفق بود." : msg);
                        }
                    }
                }

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    var msg = string.Join("، ", result.Errors.Select(e => e.Description));
                    return op.Failed(string.IsNullOrWhiteSpace(msg) ? "ویرایش کاربر ناموفق بود." : msg);
                }

                return op.succedded("اطلاعات کاربر با موفقیت به‌روزرسانی شد.");
            }
            catch (DbUpdateConcurrencyException)
            {
                return op.Failed("اطلاعات توسط کاربر دیگری تغییر کرده است. لطفاً صفحه را تازه‌سازی کنید.");
            }
            catch
            {
                return op.Failed("ویرایش کاربر با خطای غیرمنتظره مواجه شد.");
            }
        }
        public async Task<OperationResult> ChangePassword(ChangePasswordModel command, CancellationToken cancellationToken)
        {
            var op = new OperationResult();

          
            var newPwd = command?.NewPassword?.Trim();
            var confirm = command?.ConfirmPassword?.Trim();
            var current = command?.CurrentPaaword?.Trim();

            if (string.IsNullOrWhiteSpace(current) || string.IsNullOrWhiteSpace(newPwd) || string.IsNullOrWhiteSpace(confirm))
                return op.Failed("لطفاً همهٔ فیلدها را تکمیل کنید.");

  
            if (!string.Equals(newPwd, confirm, StringComparison.Ordinal))
                return op.Failed("رمز عبور جدید با تأیید رمز عبور مطابقت ندارد.");

    
            if (string.Equals(current, newPwd, StringComparison.Ordinal))
                return op.Failed("رمز عبور جدید نباید با رمز فعلی یکسان باشد.");


            var userId = _applicationUserManager.UserId.Value;
            if (userId == null)
                return op.Failed("کاربر معتبر یافت نشد.");

            var user = await _uow.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
            if (user == null)
                return op.Failed("کاربر موجود نیست.");

            var result = await _userManager.ChangePasswordAsync(user, current, newPwd);
            if (!result.Succeeded)
            {
                var msg = string.Join("، ", result.Errors.Select(e => e.Description));
                return op.Failed(string.IsNullOrWhiteSpace(msg) ? "تغییر رمز عبور ناموفق بود." : msg);
            }

            return op.succedded("رمز عبور با موفقیت تغییر کرد.");
        }


        private static string NormalizePhone(string input)
        {
            var s = input.Trim();
            s = s.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
            s = s
                .Replace('۰', '0').Replace('۱', '1').Replace('۲', '2').Replace('۳', '3').Replace('۴', '4')
                .Replace('۵', '5').Replace('۶', '6').Replace('۷', '7').Replace('۸', '8').Replace('۹', '9')
                .Replace('٠', '0').Replace('١', '1').Replace('٢', '2').Replace('٣', '3').Replace('٤', '4')
                .Replace('٥', '5').Replace('٦', '6').Replace('٧', '7').Replace('٨', '8').Replace('٩', '9');
            return s;
        }

        public async Task SigninUser(string username, CancellationToken cts = default)
        {
  var targetuser = await _uow.Users.Where(x => x.UserName.Trim().Equals(username.Trim())).FirstOrDefaultAsync(cts);
                if (targetuser != null)
                {
                    targetuser.SetLastLoginDate();
                    await _uow.SaveChangesAsync();
               }
            
                
        }

        public async Task<string?> GetUserReport(CancellationToken cancellationToken = default)
        {
            IReadOnlyList<UserViewModel>? list = await AllUsers(cancellationToken);

            using var wb = new ClosedXML.Excel.XLWorkbook();
            var ws = wb.Worksheets.Add("Supervisions");

            ws.Cell(1, 1).Value = "نام";
            ws.Cell(1, 2).Value = "نام کاربری";
            ws.Cell(1, 3).Value = "تلفن همراه";
            ws.Cell(1, 4).Value = "تاریخ آخرین ورود";
            ws.Cell(1, 5).Value = "نقش";
            ws.Cell(1, 6).Value = "فعال";
            ws.Cell(1, 7).Value = "قفل";


            ws.Range("A1:G1").Style.Font.Bold = true;

            var row = 2;
            foreach (var item in list)
            {
                ws.Cell(row, 1).Value = item.FullName;
                ws.Cell(row, 2).Value = item.UserName;
                ws.Cell(row, 3).Value = item.PhoneNumber;
                ws.Cell(row, 4).Value = item.LastLoginDate;
                ws.Cell(row, 5).Value = item.Role;
                ws.Cell(row, 6).Value = item.IsActive?"فعال":"غیر فعال";
                ws.Cell(row, 7).Value = item.IsLocked?"قفل شده":"باز";
                row++;
            }


            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            var bytes = ms.ToArray();
            var base64 = Convert.ToBase64String(bytes);
            return base64;
        }
    }
}

