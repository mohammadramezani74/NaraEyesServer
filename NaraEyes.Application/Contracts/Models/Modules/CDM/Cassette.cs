using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Contracts.Models.Modules.CDM
{

    public class Cassette
    {
        public Cassette(string name, int denom, int capacity, int remaining,string type,string currency,string totalMoney)
        { Name = $"{name}";
            Type = type;
                Currency = currency;
            TotalMoney = totalMoney;
            
            Denomination = denom; Capacity = capacity; Remaining = remaining; Status = FillPercent < 15 ? "Empty" : (FillPercent < 35 ? "Low" : "OK"); }
        public string Name { get; set; }
        public int Denomination { get; set; }
        public int Capacity { get; set; }
        public int Remaining { get; set; }
        public int FillPercent => (int)(Remaining * 100.0 / Capacity);
        public string Status { get; set; }
        public string Type { get; set; }
        public string Currency { get; set; }
        public string TotalMoney { get; set; }
    }
}
