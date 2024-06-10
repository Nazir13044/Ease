using Collecto.CoreAPI.TransactionManagement.DataAccess.MySql;
using Collecto.CoreAPI.TransactionManagement.DataAccess.Odbc;
using Collecto.CoreAPI.TransactionManagement.DataAccess.Oracle;
using Collecto.CoreAPI.TransactionManagement.DataAccess.SQL;
using Collecto.CoreAPI.TransactionManagement.Molels;
using System.Data.Common;
namespace Collecto.CoreAPI.TransactionManagement.DataAccess
{
    public class ConnectionFactory
    {
        internal SQLSyntax Syntax { get; private set; }

        internal Provider Provider { get; private set; }

        internal string ConnectionString { get; private set; }

        public ConnectionFactory(string connectionString)
        {
            ConnectionString = connectionString;
        }

        public ConnectionFactory(string provider, string sqlSyntax, string connectionString)
        {
            ConnectionString = connectionString;
            Provider = provider.ToLower() switch
            {
                "oracle" => Provider.Oracle,
                "odbc" => Provider.Odbc,
                "mysql" => Provider.MySql,
                _ => Provider.Sql,
            };
            Syntax = sqlSyntax.ToLower() switch
            {
                "oracle" => SQLSyntax.Oracle,
                "mysql" => SQLSyntax.MySql,
                "informix" => SQLSyntax.Informix,
                "access" => SQLSyntax.Access,
                _ => SQLSyntax.SQL,
            };
        }
        public virtual DbConnection CreateConnection()
        {
          /*  return (Provider switch
            {
                Provider.Oracle => new OracleFactory(ConnectionString),
                Provider.Odbc => new OdbcFactory(ConnectionString),
                Provider.MySql => new MySqlFactory(ConnectionString),
                Provider.Sql => new SqlFactory(ConnectionString),
                _ => new SqlFactory(ConnectionString),
            }).CreateConnection();
*/

            return Provider switch
            {
                Provider.Oracle => new OracleFactory(ConnectionString).CreateConnection(),
                Provider.Odbc => new OdbcFactory(ConnectionString).CreateConnection(),
                Provider.MySql => new MySqlFactory(ConnectionString).CreateConnection(),
                Provider.Sql => new SqlFactory(ConnectionString).CreateConnection(),
                _ => new SqlFactory(ConnectionString).CreateConnection(),
            };
        }

    }
}
