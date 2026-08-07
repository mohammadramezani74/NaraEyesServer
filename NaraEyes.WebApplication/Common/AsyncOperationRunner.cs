using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace NaraEyes.WebApplication.Common;

/// <summary>
/// اجرای امن عملیات‌های بلندمدت روی دستگاه، با یک الگوی واحد.
///
/// مسئله‌ای که حل می‌کند: در نسخه‌ی قبلی هر عملیات (اسکرین‌شات، ژورنال،
/// ری‌بوت، ری‌ست) پرچم loading خودش را دستی true/false می‌کرد و در هر
/// catch جداگانه تکرار می‌شد. کافی بود یک مسیر خطا فراموش شود تا
/// چرخنده تا ابد بچرخد — که دقیقاً همان اتفاقی بود که افتاد.
///
/// اینجا پرچم و StateHasChanged در finally مدیریت می‌شوند، پس
/// «فراموش کردن» ممکن نیست.
/// </summary>
public sealed class AsyncOperationRunner
{
    private readonly ComponentBase _owner;
    private readonly ISnackbar _snackbar;
    private readonly Func<Func<Task>, Task> _invokeAsync;
    private readonly Action<bool> _setBusy;
    private readonly Action<Exception, string> _logError;

    /// <summary>عملیات‌های در حال اجرا، برای جلوگیری از کلیک دوباره.</summary>
    private readonly HashSet<string> _running = new();
    private readonly object _gate = new();

    public AsyncOperationRunner(
        ComponentBase owner,
        ISnackbar snackbar,
        Func<Func<Task>, Task> invokeAsync,
        Action<bool> setBusy,
        Action<Exception, string> logError)
    {
        _owner = owner;
        _snackbar = snackbar;
        _invokeAsync = invokeAsync;
        _setBusy = setBusy;
        _logError = logError;
    }

    /// <summary>آیا عملیاتی با این کلید در حال اجراست؟</summary>
    public bool IsRunning(string key)
    {
        lock (_gate) return _running.Contains(key);
    }

    public bool IsAnyRunning
    {
        get { lock (_gate) return _running.Count > 0; }
    }

    /// <summary>عملیاتی که نتیجه برمی‌گرداند.</summary>
    public async Task<T?> RunAsync<T>(
        string operationKey,
        string title,
        Func<CancellationToken, Task<T>> work,
        CancellationToken ct,
        string? successMessage = null,
        string? emptyMessage = null,
        Func<T?, bool>? isEmpty = null)
    {
        lock (_gate)
        {
            if (_running.Contains(operationKey))
            {
                _snackbar.Add($"{title} هم‌اکنون در حال اجراست.", Severity.Info);
                return default;
            }
            _running.Add(operationKey);
        }

        await SetBusyAsync(true);

        try
        {
            T result = await work(ct);

            if (isEmpty is not null && isEmpty(result))
            {
                _snackbar.Add(emptyMessage ?? $"{title}: نتیجه‌ای دریافت نشد.", Severity.Warning);
                return default;
            }

            if (!string.IsNullOrEmpty(successMessage))
                _snackbar.Add(successMessage, Severity.Success);

            return result;
        }
        catch (TimeoutException ex)
        {
            _logError(ex, $"{title} timeout");
            _snackbar.Add($"⏳ {title}: دستگاه در زمان مقرر پاسخ نداد. " +
                          "ممکن است آفلاین باشد یا ایجنت اجرا نشده باشد.", Severity.Warning);
            return default;
        }
        catch (OperationCanceledException)
        {
            // لغو عمدی — پیام لازم نیست
            return default;
        }
        catch (Exception ex)
        {
            _logError(ex, $"{title} failed");
            _snackbar.Add($"{title}: خطا — {ex.Message}", Severity.Error);
            return default;
        }
        finally
        {
            lock (_gate) { _running.Remove(operationKey); }
            await SetBusyAsync(false);
        }
    }

    /// <summary>عملیاتی که فقط موفق/ناموفق است.</summary>
    public async Task<bool> RunAsync(
        string operationKey,
        string title,
        Func<CancellationToken, Task<bool>> work,
        CancellationToken ct,
        string? successMessage = null,
        string? failureMessage = null)
    {
        var r = await RunAsync<bool?>(
            operationKey, title,
            async c => await work(c),
            ct,
            successMessage: null);

        if (r is null) return false;          // خطا رخ داده و پیامش داده شده

        if (r.Value)
        {
            if (!string.IsNullOrEmpty(successMessage))
                _snackbar.Add(successMessage, Severity.Success);
        }
        else
        {
            _snackbar.Add(failureMessage ?? $"{title} انجام نشد.", Severity.Warning);
        }

        return r.Value;
    }

    private async Task SetBusyAsync(bool busy)
    {
        await _invokeAsync(() =>
        {
            _setBusy(busy);
            return Task.CompletedTask;
        });
    }
}