using NaraEyes.Application.Contracts.Interfaces.Base;
using System;
using System.Collections.Concurrent;
using System.Threading;

public sealed class CommandDispatchState : ICommandDispatchState
{
    private sealed class DeviceDispatchState
    {
        public int PendingHint;          // تعداد تقریبی فرمان‌های تازه
        public bool FirstPollDone;       // اولین Poll بعد از استارت زده شده؟
        public DateTime LastDbCheckUtc;  // آخرین باری که DB را برای این device چک کردیم
    }

    private readonly ConcurrentDictionary<string, DeviceDispatchState> _states = new();

    // هر چند وقت یک‌بار حتی اگر hint=0 بود، یک‌بار DB را چک کنیم (fallback)
    private readonly TimeSpan _dbFallbackInterval = TimeSpan.FromMinutes(5);

    public void MarkCommandEnqueued(string deviceKey)
    {
        if (string.IsNullOrWhiteSpace(deviceKey))
            return;

        var state = _states.GetOrAdd(deviceKey, _ => new DeviceDispatchState());
        Interlocked.Increment(ref state.PendingHint);
        // FirstPollDone و LastDbCheckUtc را اینجا دست نمی‌زنیم
    }

    public bool ShouldCheckDatabase(string deviceKey, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(deviceKey))
            return false;

        var state = _states.GetOrAdd(deviceKey, _ => new DeviceDispatchState());

        // ۱) اولین Poll بعد از استارت: حتماً DB را چک کن
        if (!state.FirstPollDone)
            return true;

        // ۲) اگر hint > 0 است، یعنی به احتمال زیاد فرمان جدید داریم
        if (Volatile.Read(ref state.PendingHint) > 0)
            return true;

        // ۳) fallback: هر X دقیقه یک‌بار، حتی اگر hint=0 باشد، DB را چک کن
        if (utcNow - state.LastDbCheckUtc >= _dbFallbackInterval)
            return true;

        // در بقیه حالت‌ها نیازی نیست DB را چک کنیم
        return false;
    }

    public void MarkCommandsLoadedFromDb(string deviceKey, bool anyCommands, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(deviceKey))
            return;

        var state = _states.GetOrAdd(deviceKey, _ => new DeviceDispatchState());

        state.FirstPollDone = true;
        state.LastDbCheckUtc = utcNow;

        if (anyCommands)
        {
            // ساده‌ترین رفتار: چون خواندیم، hint را صفر کن.
            // اگر در همین فاصله فرمان جدیدی enqueue شده باشد، MarkCommandEnqueued دوباره hint را >0 می‌کند.
            Interlocked.Exchange(ref state.PendingHint, 0);
        }
        else
        {
            // اثبات این‌که فعلاً چیزی در DB نیست → صفر کردن hint اشکالی ندارد
            Interlocked.Exchange(ref state.PendingHint, 0);
        }
    }
}
