using NaraEyes.Domain.Common;
using NaraEyes.Domain.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Domain.Entities.Devices
{
    public class CashUnit:BaseEntity
    {
        public string Name { get;internal set; }
        public string Currency { get; internal set; }
        public string Serial { get; internal set; }
        public string CurrentCount { get;  set; }
        public string TotalCount { get;  set; }
        public CashUnitStatus Status { get; internal set; }
        public CashUnitType Type { get;  set; }
        public int Denomination { get;  set; }
        public Device Device { get; internal set; }
        public Guid DeviceId { get; internal set; }

        // ========= Factory =========
        public static CashUnit Create(
            Guid deviceId,
            string name,
            string currency,
            string serial,
            int denomination,
            string totalCount,
            string currentCount,
            CashUnitType type,
            CashUnitStatus status)
        {
            return new CashUnit
            {
                Id = Guid.NewGuid(),
                DeviceId = EnsureDeviceId(deviceId),
                Name = NormalizeName(name),
                Currency = NormalizeCurrency(currency),
                Serial = NormalizeSerial(serial),
                Denomination = denomination,
                TotalCount = totalCount,
                CurrentCount = currentCount.ToString(),
                Type = type,
               Deleted = false,
                Status = status,
                CreateDate = DateTime.Now
            };
        }

        // ========= Behavior =========

        /// <summary>
        /// برداشت اسکناس (Dispense). اگر تعداد کافی نباشد، Exception می‌زند.
        /// </summary>
        public void Dispense(int count)
        {
            int current = int.Parse(CurrentCount);
            if (count <= 0) throw new ArgumentException("Dispense count must be positive.");
            if (count > current) throw new InvalidOperationException("Not enough notes in the unit.");

            current -= count;
            CurrentCount = current.ToString();

            UpdateStatusAfterChange();
        }

        /// <summary>
        /// شارژ مجدد (Refill) کاست با تعداد مشخص.
        /// </summary>
        public void Refill(int added)
        {
            if (added <= 0) throw new ArgumentException("Refill count must be positive.");

            int current = int.Parse(CurrentCount);
            int total = int.Parse(TotalCount);

            if (current + added > total)
                throw new InvalidOperationException("Exceeds total capacity.");

            CurrentCount = (current + added).ToString();

            UpdateStatusAfterChange();
        }

        /// <summary>
        /// ست‌کردن ظرفیت کل (مثلاً بعد از تعویض کاست).
        /// </summary>
        public void SetCapacity(int newTotal, int? current = null)
        {
            if (newTotal <= 0) throw new ArgumentException("Capacity must be positive.");
            TotalCount = newTotal.ToString();
            if (current.HasValue)
            {
                if (current.Value > newTotal)
                    throw new InvalidOperationException("Current cannot exceed capacity.");
                CurrentCount = current.Value.ToString();
            }
            UpdateStatusAfterChange();
        }

        /// <summary>
        /// تغییر وضعیت دستی (مثلاً اپراتور کاست را Disable کند).
        /// </summary>
        public void SetStatus(CashUnitStatus status) => Status = status;

        // ========= Guards / Helpers =========

        private void UpdateStatusAfterChange()
        {
            int current = int.Parse(CurrentCount);
            int total = int.Parse(TotalCount);

            if (current == 0) Status = CashUnitStatus.Empty;
            else if (current < total * 0.1) Status = CashUnitStatus.Low;
            else Status = CashUnitStatus.Ok;
        }

        private static Guid EnsureDeviceId(Guid id)
            => id != Guid.Empty ? id : throw new ArgumentException("DeviceId required.");

        private static int EnsurePositive(int value, string field)
            => value > 0 ? value : throw new ArgumentException($"{field} must be positive.");

        private static string NormalizeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name required.");
            return name.Trim();
        }

        private static string NormalizeCurrency(string currency)
        {
            if (string.IsNullOrWhiteSpace(currency)) return "IRR";
            return currency.Trim().ToUpperInvariant();
        }

        private static string NormalizeSerial(string serial)
        {
            if (string.IsNullOrWhiteSpace(serial)) return Guid.NewGuid().ToString("N")[..10];
            return serial.Trim();
        }

        // ========= Helpers =========

        public int CurrentCountValue => int.Parse(CurrentCount);
        public int TotalCountValue => int.Parse(TotalCount);

        /// <summary>برمی‌گرداند تعداد اسکناس‌های مصرف شده.</summary>
        public int ConsumedCount => TotalCountValue - CurrentCountValue;

        /// <summary>ارزش پولی باقی‌مانده (Denomination * CurrentCount).</summary>
        public long RemainingValue => (long)Denomination * CurrentCountValue;

        /// <summary>ارزش کل کاست (Denomination * Capacity).</summary>
        public long TotalValue => (long)Denomination * TotalCountValue;
    }

}
