using Microsoft.Data.SqlClient;
using NaraEyes.Application.Abstraction.Dapper;
using System.Data;


namespace NaraEyes.Infrastructure.Dapper
{
    public class DbConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;
        public DbConnectionFactory(string connectionString)
        {
            _connectionString = connectionString;
        }
        public IDbConnection GetOpenConnection()
        {
            SqlConnection connection = new SqlConnection(_connectionString);
            connection.Open();
            return connection;
        }
    }
}
