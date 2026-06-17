using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Abstraction.Logger
{
    public interface IAppLogger
    {
        void Info(string message, params object[] args);
        void Warn(string message, params object[] args);
        void Error(Exception ex, string message, params object[] args);

        void LogInformation(string message, params object[] args);
    }
}
