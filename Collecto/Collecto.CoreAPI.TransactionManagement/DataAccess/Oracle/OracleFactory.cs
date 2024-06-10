using Oracle.ManagedDataAccess.Client;
using System.Data.Common;

namespace Collecto.CoreAPI.TransactionManagement.DataAccess.Oracle
{
    public sealed class OracleFactory : ConnectionFactory
    {
        public override DbConnection CreateConnection()
        {
            return new OracleConnection(base.ConnectionString);
        }

        internal OracleFactory(string connectionString)
            : base(connectionString)
        {
        }
    }
}
