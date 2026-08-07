namespace NaraEyes.Application.Contracts.Models.Modules.CDM
{
    public class Cassette
    {
        public Cassette(string? name, int denom, int capacity, int remaining,
                        string? type, string? currency, string? totalMoney)
        {
            Name = $"{name}";
            Type = type;
            Currency = currency;
            TotalMoney = totalMoney;
            Denomination = denom;
            Capacity = capacity;
            Remaining = remaining;

            Status = ComputeStatus();
        }

        public string? Name { get; set; }
        public int Denomination { get; set; }
        public int Capacity { get; set; }
        public int Remaining { get; set; }

        /// <summary>
        /// درصد پرشدگی. برای کاست ریجکت و هر کاستی که ظرفیت اولیه‌اش
        /// ثبت نشده، ظرفیت صفر است و درصد معنا ندارد → مقدار -1 برمی‌گردد.
        /// </summary>
        public int FillPercent
        {
            get
            {
                if (Capacity <= 0) return -1;          // نامشخص
                if (Remaining <= 0) return 0;

                double p = Remaining * 100.0 / Capacity;
                return (int)Math.Clamp(p, 0, 100);     // محافظت از مقدار >100
            }
        }

        /// <summary>آیا درصد پرشدگی قابل محاسبه است؟</summary>
        public bool HasFillPercent => Capacity > 0;

        public string? Status { get; set; }
        public string? Type { get; set; }
        public string? Currency { get; set; }
        public string? TotalMoney { get; set; }

        private string ComputeStatus()
        {
            if (Capacity <= 0)
                return Remaining > 0 ? "OK" : "Empty";   // ریجکت: فقط پر/خالی

            int p = FillPercent;
            return p < 15 ? "Empty" : (p < 35 ? "Low" : "OK");
        }
    }
}