using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Contracts.Interfaces.Base
{
    public interface ICommandDispatchState
    {
        /// وقتی برای یک device فرمان جدید enqueue شد (در DB)، صدا زده می‌شود.
        void MarkCommandEnqueued(string Ip);

        /// آیا الان لازم است DB را برای این device چک کنیم یا می‌توانیم skip کنیم؟
        bool ShouldCheckDatabase(string Ip, DateTime utcNow);

        /// بعد از این‌که از DB خواندیم، نتیجه را گزارش می‌کنیم تا state به‌روز شود.
        void MarkCommandsLoadedFromDb(string Ip, bool anyCommands, DateTime now);

    }
}
