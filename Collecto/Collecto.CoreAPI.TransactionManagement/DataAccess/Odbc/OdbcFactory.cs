using System.Data.Common;
using System.Data.Odbc;

namespace Collecto.CoreAPI.TransactionManagement.DataAccess.Odbc
{
    public sealed class OdbcFactory : ConnectionFactory
    {
        public override DbConnection CreateConnection()
        {
            return new OdbcConnection(base.ConnectionString);
        }

        internal OdbcFactory(string connectionString)
            : base(connectionString)
        {
        }
    }
}
