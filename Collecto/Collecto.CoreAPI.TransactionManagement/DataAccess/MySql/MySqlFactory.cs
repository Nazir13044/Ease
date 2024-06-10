using MySql.Data.MySqlClient;
using System.Data.Common;

namespace Collecto.CoreAPI.TransactionManagement.DataAccess.MySql
{
    public sealed class MySqlFactory : ConnectionFactory
    {
        public override DbConnection CreateConnection()
        {
            return new MySqlConnection(base.ConnectionString);
        }

        internal MySqlFactory(string connectionString)
            : base(connectionString)
        {
        }
    }
}
