using System.Data.Common;
using System.Data.SqlClient;

namespace Collecto.CoreAPI.TransactionManagement.DataAccess.SQL
{
    public sealed class SqlFactory : ConnectionFactory
    {
        public override DbConnection CreateConnection()
        {
            return new SqlConnection(base.ConnectionString);
        }

        internal SqlFactory(string connectionString)
            : base(connectionString)
        {
        }
    }
}
