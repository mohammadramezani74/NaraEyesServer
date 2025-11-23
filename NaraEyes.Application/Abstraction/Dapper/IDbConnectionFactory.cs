using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Abstraction.Dapper
{
    public interface IDbConnectionFactory
    {
        IDbConnection GetOpenConnection();
    }
}
