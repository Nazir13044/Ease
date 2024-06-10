using Collecto.CoreAPI.TransactionManagement.Molels;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;

namespace Collecto.CoreAPI.TransactionManagement.DataAccess
{
    public class TransactionContext : IDisposable
    {
        private SQLSyntax _syntax;

        private Provider _provider;

        private bool _createSession;

        private readonly ConnectionNode _node;

        public DbConnection Connection { get; private set; }

        public DbTransaction Transaction { get; private set; }

        public int CommandTimeOut { get; set; }

        private TransactionContext()
        {
            _node = null;
            CommandTimeOut = 0;
            _createSession = false;
            _syntax = SQLSyntax.SQL;
        }

        private TransactionContext(ConnectionNode node)
            : this()
        {
            _node = node;
        }

        public static TransactionContext Begin(ConnectionNode node)
        {
            TransactionContext transactionContext = new(node)
            {
                _createSession = false
            };
            try
            {
                transactionContext.PrepareConnection();
            }
            catch
            {
            }

            return transactionContext;
        }

        public static TransactionContext Begin(ConnectionNode node, bool createSession)
        {
            TransactionContext transactionContext = new(node)
            {
                _createSession = createSession
            };
            try
            {
                transactionContext.PrepareConnection();
            }
            catch
            {
            }

            return transactionContext;
        }

        public static async Task<TransactionContext> BeginAsync(ConnectionNode node)
        {
            TransactionContext tc = new(node)
            {
                _createSession = false
            };
            try
            {
                await tc.PrepareConnectionAsync();
            }
            catch
            {
            }

            return tc;
        }

        public static async Task<TransactionContext> BeginAsync(ConnectionNode node, bool createSession)
        {
            TransactionContext tc = new(node)
            {
                _createSession = createSession
            };
            try
            {
                await tc.PrepareConnectionAsync();
            }
            catch
            {
            }

            return tc;
        }

        public void End()
        {
            ReleaseResources();
        }

        private void ReleaseResources()
        {
            try
            {
                if (Transaction != null && _createSession)
                {
                    Transaction.Commit();
                }

                if (Connection != null && Connection.State == ConnectionState.Open)
                {
                    Connection.Close();
                }

                Transaction?.Dispose();
                Connection?.Dispose();
                Connection = null;
                CommandTimeOut = 0;
                Transaction = null;
                _createSession = false;
                GC.Collect();
            }
            catch
            {
            }
        }

        private void PrepareConnection()
        {
            if (Connection != null)
            {
                return;
            }

            ConnectionFactory connectionFactory = new(_node.Provider, _node.SqlSyntax, _node.ConnectionString);
            _syntax = connectionFactory.Syntax;
            _provider = connectionFactory.Provider;
            Connection = connectionFactory.CreateConnection();
            try
            {
                Connection.Open();
            }
            catch (SqlException ex)
            {
                Exception ex2 = ex.Number switch
                {
                    2 or 53 or 258 or 976 or 2702 or 4060 or 10054 or 10060 or 10061 or 11001 or 37002 => new Exception($"Error code: {ex.ErrorCode} -> {ex.Message}", ex),
                    _ => new Exception($"Server: {ex.Server} -> {ex.Message}", ex),
                };
                throw ex2;
            }

            if (_createSession)
            {
                Transaction = Connection.BeginTransaction(IsolationLevel.ReadCommitted);
            }
        }

        private async Task PrepareConnectionAsync()
        {
            if (Connection != null)
            {
                return;
            }

            ConnectionFactory connectionFactory = new(_node.Provider, _node.SqlSyntax, _node.ConnectionString);
            _syntax = connectionFactory.Syntax;
            _provider = connectionFactory.Provider;
            Connection = connectionFactory.CreateConnection();
            try
            {
                await Connection.OpenAsync();
            }
            catch (SqlException ex)
            {
                Exception ex2 = ex.Number switch
                {
                    2 or 53 or 258 or 976 or 2702 or 4060 or 10054 or 10060 or 10061 or 11001 or 37002 => new Exception($"Eorror code: {ex.ErrorCode} -> {ex.Message}", ex),
                    _ => new Exception($"Server: {ex.Server} -> {ex.Message}", ex),
                };
                throw ex2;
            }

            if (_createSession)
            {
                Transaction = await Connection.BeginTransactionAsync(IsolationLevel.ReadCommitted);
            }
        }

        public void PrepareCommand(IDbCommand command)
        {
            if (Connection == null && Transaction == null)
            {
                throw new Exception("There is no connection to prepare command(PrepareCommand).");
            }

            if (Connection != null)
            {
                command.Connection = Connection;
            }

            if (Transaction != null)
            {
                command.Transaction = Transaction;
            }
        }

        public int ExecuteNonQuery(string commandText, params object[] args)
        {
            if (Connection == null && Transaction == null)
            {
                throw new Exception("There is no connection to prepare command(ExecuteNonQuery).");
            }

            commandText = SQLParser.MakeSQL(_syntax, commandText, args);
            if (Transaction != null)
            {
                return TransactionFactory.Default(_provider).ExecuteNonQuery(CommandTimeOut, Transaction, CommandType.Text, commandText);
            }

            return TransactionFactory.Default(_provider).ExecuteNonQuery(CommandTimeOut, Connection, CommandType.Text, commandText);
        }

        public int ExecuteNonQuery(CommandType commandType, string commandText, IDataParameter[] commandParametes)
        {
            if (Connection == null && Transaction == null)
            {
                throw new Exception("There is no connection to prepare command(ExecuteNonQuery).");
            }

            if (Transaction != null)
            {
                return TransactionFactory.Default(_provider).ExecuteNonQuery(CommandTimeOut, Transaction, commandType, commandText, commandParametes);
            }

            return TransactionFactory.Default(_provider).ExecuteNonQuery(CommandTimeOut, Connection, commandType, commandText, commandParametes);
        }

        public int ExecuteNonQuerySp(string spName, params object[] parameterValues)
        {
            if (Connection == null && Transaction == null)
            {
                throw new Exception("There is no connection to prepare command(ExecuteNonQuery).");
            }

            if (Transaction != null)
            {
                return TransactionFactory.Default(_provider).ExecuteNonQuery(CommandTimeOut, Transaction, spName, parameterValues);
            }

            return TransactionFactory.Default(_provider).ExecuteNonQuery(CommandTimeOut, Connection, spName, parameterValues);
        }

        public Task<int> ExecuteNonQueryAsync(string commandText, params object[] args)
        {
            if (Connection == null && Transaction == null)
            {
                throw new Exception("There is no connection to prepare command(ExecuteNonQuery).");
            }

            commandText = SQLParser.MakeSQL(_syntax, commandText, args);
            if (Transaction != null)
            {
                return TransactionFactory.Default(_provider).ExecuteNonQueryAsync(CommandTimeOut, Transaction, CommandType.Text, commandText);
            }

            return TransactionFactory.Default(_provider).ExecuteNonQueryAsync(CommandTimeOut, Connection, CommandType.Text, commandText);
        }

        public Task<int> ExecuteNonQueryAsync(CommandType commandType, string commandText, IDataParameter[] commandParametes)
        {
            if (Connection == null && Transaction == null)
            {
                throw new Exception("There is no connection to prepare command(ExecuteNonQuery).");
            }

            if (Transaction != null)
            {
                return TransactionFactory.Default(_provider).ExecuteNonQueryAsync(CommandTimeOut, Transaction, commandType, commandText, commandParametes);
            }

            return TransactionFactory.Default(_provider).ExecuteNonQueryAsync(CommandTimeOut, Connection, commandType, commandText, commandParametes);
        }

        public Task<int> ExecuteNonQuerySpAsync(string spName, params object[] parameterValues)
        {
            if (Connection == null && Transaction == null)
            {
                throw new Exception("There is no connection to prepare command(ExecuteNonQuery).");
            }

            if (Transaction != null)
            {
                return TransactionFactory.Default(_provider).ExecuteNonQueryAsync(CommandTimeOut, Transaction, spName, parameterValues);
            }

            return TransactionFactory.Default(_provider).ExecuteNonQueryAsync(CommandTimeOut, Connection, spName, parameterValues);
        }

        public IDataReader ExecuteReader(string commandText, params object[] args)
        {
            if (Connection == null && Transaction == null)
            {
                throw new Exception("There is no connection to prepare command(ExecuteReader).");
            }

            commandText = SQLParser.MakeSQL(_syntax, commandText, args);
            if (Transaction != null)
            {
                return TransactionFactory.Default(_provider).ExecuteReader(CommandTimeOut, Transaction, CommandType.Text, commandText);
            }

            return TransactionFactory.Default(_provider).ExecuteReader(CommandTimeOut, Connection, CommandType.Text, commandText);
        }

        public IDataReader ExecuteReader(CommandType commandType, string commandText, params IDataParameter[] commandParameters)
        {
            if (Connection == null && Transaction == null)
            {
                throw new Exception("There is no connection to prepare command(ExecuteReader).");
            }

            if (Transaction != null)
            {
                return TransactionFactory.Default(_provider).ExecuteReader(CommandTimeOut, Transaction, commandType, commandText, commandParameters);
            }

            return TransactionFactory.Default(_provider).ExecuteReader(CommandTimeOut, Connection, commandType, commandText, commandParameters);
        }

        public IDataReader ExecuteReaderSp(string spName, params object[] parameterValues)
        {
            if (Connection == null && Transaction == null)
            {
                throw new Exception("There is no connection to prepare command(ExecuteReader).");
            }

            if (Transaction != null)
            {
                return TransactionFactory.Default(_provider).ExecuteReader(CommandTimeOut, Transaction, spName, parameterValues);
            }

            return TransactionFactory.Default(_provider).ExecuteReader(CommandTimeOut, Connection, spName, parameterValues);
        }

        public Task<IDataReader> ExecuteReaderAsync(string commandText, params object[] args)
        {
            if (Connection == null && Transaction == null)
            {
                throw new Exception("There is no connection to prepare command(ExecuteReader).");
            }

            commandText = SQLParser.MakeSQL(_syntax, commandText, args);
            if (Transaction != null)
            {
                return TransactionFactory.Default(_provider).ExecuteReaderAsync(CommandTimeOut, Transaction, CommandType.Text, commandText);
            }

            return TransactionFactory.Default(_provider).ExecuteReaderAsync(CommandTimeOut, Connection, CommandType.Text, commandText);
        }

        public Task<IDataReader> ExecuteReaderAync(CommandType commandType, string commandText, params IDataParameter[] commandParameters)
        {
            if (Connection == null && Transaction == null)
            {
                throw new Exception("There is no connection to prepare command(ExecuteReader).");
            }

            if (Transaction != null)
            {
                return TransactionFactory.Default(_provider).ExecuteReaderAsync(CommandTimeOut, Transaction, commandType, commandText, commandParameters);
            }

            return TransactionFactory.Default(_provider).ExecuteReaderAsync(CommandTimeOut, Connection, commandType, commandText, commandParameters);
        }

        public Task<IDataReader> ExecuteReaderSpAsync(string spName, params object[] parameterValues)
        {
            if (Connection == null && Transaction == null)
            {
                throw new Exception("There is no connection to prepare command(ExecuteReader).");
            }

            if (Transaction != null)
            {
                return TransactionFactory.Default(_provider).ExecuteReaderAsync(CommandTimeOut, Transaction, spName, parameterValues);
            }

            return TransactionFactory.Default(_provider).ExecuteReaderAsync(CommandTimeOut, Connection, spName, parameterValues);
        }

        public object ExecuteScalar(string commandText, params object[] args)
        {
            if (Connection == null && Transaction == null)
            {
                throw new Exception("There is no connection to prepare command(ExecuteScalar).");
            }

            commandText = SQLParser.MakeSQL(_syntax, commandText, args);
            if (Transaction != null)
            {
                return TransactionFactory.Default(_provider).ExecuteScalar(CommandTimeOut, Transaction, CommandType.Text, commandText);
            }

            return TransactionFactory.Default(_provider).ExecuteScalar(CommandTimeOut, Connection, CommandType.Text, commandText);
        }

        public object ExecuteScalar(CommandType commandType, string commandText, params IDataParameter[] commandParameters)
        {
            if (Connection == null && Transaction == null)
            {
                throw new Exception("There is no connection to prepare command(ExecuteScalar).");
            }

            if (Transaction != null)
            {
                return TransactionFactory.Default(_provider).ExecuteScalar(CommandTimeOut, Transaction, commandType, commandText, commandParameters);
            }

            return TransactionFactory.Default(_provider).ExecuteScalar(CommandTimeOut, Connection, commandType, commandText, commandParameters);
        }

        public object ExecuteScalarSP(string spName, params object[] parameterValues)
        {
            if (Connection == null && Transaction == null)
            {
                throw new Exception("There is no connection to prepare command(ExecuteScalar).");
            }

            if (Transaction != null)
            {
                return TransactionFactory.Default(_provider).ExecuteScalar(CommandTimeOut, Transaction, spName, parameterValues);
            }

            return TransactionFactory.Default(_provider).ExecuteScalar(CommandTimeOut, Connection, spName, parameterValues);
        }

        public Task<object> ExecuteScalarAsync(string commandText, params object[] args)
        {
            if (Connection == null && Transaction == null)
            {
                throw new Exception("There is no connection to prepare command(ExecuteScalar).");
            }

            commandText = SQLParser.MakeSQL(_syntax, commandText, args);
            if (Transaction != null)
            {
                return TransactionFactory.Default(_provider).ExecuteScalarAsync(CommandTimeOut, Transaction, CommandType.Text, commandText);
            }

            return TransactionFactory.Default(_provider).ExecuteScalarAsync(CommandTimeOut, Connection, CommandType.Text, commandText);
        }

        public Task<object> ExecuteScalarAsync(CommandType commandType, string commandText, params IDataParameter[] commandParameters)
        {
            if (Connection == null && Transaction == null)
            {
                throw new Exception("There is no connection to prepare command(ExecuteScalar).");
            }

            if (Transaction != null)
            {
                return TransactionFactory.Default(_provider).ExecuteScalarAsync(CommandTimeOut, Transaction, commandType, commandText, commandParameters);
            }

            return TransactionFactory.Default(_provider).ExecuteScalarAsync(CommandTimeOut, Connection, commandType, commandText, commandParameters);
        }

        public Task<object> ExecuteScalarSpAsync(string spName, params object[] parameterValues)
        {
            if (Connection == null && Transaction == null)
            {
                throw new Exception("There is no connection to prepare command(ExecuteScalar).");
            }

            if (Transaction != null)
            {
                return TransactionFactory.Default(_provider).ExecuteScalarAsync(CommandTimeOut, Transaction, spName, parameterValues);
            }

            return TransactionFactory.Default(_provider).ExecuteScalarAsync(CommandTimeOut, Connection, spName, parameterValues);
        }

        public DataSet ExecuteDataSet(string commandText, params object[] args)
        {
            if (Connection == null && Transaction == null)
            {
                throw new Exception("Connection is already closed");
            }

            commandText = SQLParser.MakeSQL(_syntax, commandText, args);
            if (Transaction != null)
            {
                return TransactionFactory.Default(_provider).ExecuteDataset(CommandTimeOut, Transaction, CommandType.Text, commandText);
            }

            return TransactionFactory.Default(_provider).ExecuteDataset(CommandTimeOut, Connection, CommandType.Text, commandText);
        }

        public DataSet ExecuteDataSet(CommandType commandType, string commandText, params IDataParameter[] commandParameters)
        {
            if (Connection == null && Transaction == null)
            {
                throw new Exception("Connection is already closed");
            }

            if (Transaction != null)
            {
                return TransactionFactory.Default(_provider).ExecuteDataset(CommandTimeOut, Transaction, commandType, commandText, commandParameters);
            }

            return TransactionFactory.Default(_provider).ExecuteDataset(CommandTimeOut, Connection, commandType, commandText, commandParameters);
        }

        public DataSet ExecuteDataSetSp(string spName, params object[] parameterValues)
        {
            if (Connection == null && Transaction == null)
            {
                throw new Exception("Connection is already closed");
            }

            if (Transaction != null)
            {
                return TransactionFactory.Default(_provider).ExecuteDataset(CommandTimeOut, Transaction, spName, parameterValues);
            }

            return TransactionFactory.Default(_provider).ExecuteDataset(CommandTimeOut, Connection, spName, parameterValues);
        }

        public Task<DataSet> ExecuteDataSetAsync(string commandText, params object[] args)
        {
            if (Connection == null && Transaction == null)
            {
                throw new Exception("Connection is already closed");
            }

            commandText = SQLParser.MakeSQL(_syntax, commandText, args);
            if (Transaction != null)
            {
                return TransactionFactory.Default(_provider).ExecuteDatasetAsync(CommandTimeOut, Transaction, CommandType.Text, commandText);
            }

            return TransactionFactory.Default(_provider).ExecuteDatasetAsync(CommandTimeOut, Connection, CommandType.Text, commandText);
        }

        public Task<DataSet> ExecuteDataSetAsync(CommandType commandType, string commandText, params IDataParameter[] commandParameters)
        {
            if (Connection == null && Transaction == null)
            {
                throw new Exception("Connection is already closed");
            }

            if (Transaction != null)
            {
                return TransactionFactory.Default(_provider).ExecuteDatasetAsync(CommandTimeOut, Transaction, commandType, commandText, commandParameters);
            }

            return TransactionFactory.Default(_provider).ExecuteDatasetAsync(CommandTimeOut, Connection, commandType, commandText, commandParameters);
        }

        public Task<DataSet> ExecuteDataSetSpAsync(string spName, params object[] parameterValues)
        {
            if (Connection == null && Transaction == null)
            {
                throw new Exception("Connection is already closed");
            }

            if (Transaction != null)
            {
                return TransactionFactory.Default(_provider).ExecuteDatasetAsync(CommandTimeOut, Transaction, spName, parameterValues);
            }

            return TransactionFactory.Default(_provider).ExecuteDatasetAsync(CommandTimeOut, Connection, spName, parameterValues);
        }

        public void FillDataset(DataSet dataSet, string[] tableNames, string commandText, params object[] args)
        {
            if (Connection == null && Transaction == null)
            {
                throw new Exception("There is no connection to prepare command(FillDataset).");
            }

            commandText = SQLParser.MakeSQL(_syntax, commandText, args);
            if (Transaction != null)
            {
                TransactionFactory.Default(_provider).FillDataset(CommandTimeOut, Transaction, CommandType.Text, commandText, dataSet, tableNames);
            }
            else
            {
                TransactionFactory.Default(_provider).FillDataset(CommandTimeOut, Connection, CommandType.Text, commandText, dataSet, tableNames);
            }
        }

        public void FillDataset(CommandType commandType, string commandText, DataSet dataSet, string[] tableNames, params IDataParameter[] commandParameters)
        {
            if (Connection == null && Transaction == null)
            {
                throw new Exception("There is no connection to prepare command(FillDataset).");
            }

            if (Transaction != null)
            {
                TransactionFactory.Default(_provider).FillDataset(CommandTimeOut, Transaction, commandType, commandText, dataSet, tableNames, commandParameters);
            }
            else
            {
                TransactionFactory.Default(_provider).FillDataset(CommandTimeOut, Connection, commandType, commandText, dataSet, tableNames, commandParameters);
            }
        }

        public void FillDataset(string spName, DataSet dataSet, string[] tableNames, params object[] parameterValues)
        {
            if (Connection == null && Transaction == null)
            {
                throw new Exception("There is no connection to prepare command(FillDataset).");
            }

            if (Transaction != null)
            {
                TransactionFactory.Default(_provider).FillDataset(CommandTimeOut, Transaction, spName, dataSet, tableNames, parameterValues);
            }
            else
            {
                TransactionFactory.Default(_provider).FillDataset(CommandTimeOut, Connection, spName, dataSet, tableNames, parameterValues);
            }
        }

        public int ExecuteNonQueryTypedParams(string spName, DataRow dataRow)
        {
            if (Connection == null && Transaction == null)
            {
                throw new Exception("There is no connection to prepare command(ExecuteNonQuery).");
            }

            if (Transaction != null)
            {
                return TransactionFactory.Default(_provider).ExecuteNonQueryTypedParams(CommandTimeOut, Transaction, spName, dataRow);
            }

            return TransactionFactory.Default(_provider).ExecuteNonQueryTypedParams(CommandTimeOut, Connection, spName, dataRow);
        }

        public Task<int> ExecuteNonQueryTypedParamsAsync(string spName, DataRow dataRow)
        {
            if (Connection == null && Transaction == null)
            {
                throw new Exception("There is no connection to prepare command(ExecuteNonQuery).");
            }

            if (Transaction != null)
            {
                return TransactionFactory.Default(_provider).ExecuteNonQueryTypedParamsAsync(CommandTimeOut, Transaction, spName, dataRow);
            }

            return TransactionFactory.Default(_provider).ExecuteNonQueryTypedParamsAsync(CommandTimeOut, Connection, spName, dataRow);
        }

        public DataSet ExecuteDatasetTypedParams(string spName, DataRow dataRow)
        {
            if (Connection == null && Transaction == null)
            {
                throw new Exception("There is no connection to prepare command(ExecuteNonQuery).");
            }

            if (Transaction != null)
            {
                return TransactionFactory.Default(_provider).ExecuteDatasetTypedParams(CommandTimeOut, Transaction, spName, dataRow);
            }

            return TransactionFactory.Default(_provider).ExecuteDatasetTypedParams(CommandTimeOut, Connection, spName, dataRow);
        }

        public Task<DataSet> ExecuteDatasetTypedParamsAsync(string spName, DataRow dataRow)
        {
            if (Connection == null && Transaction == null)
            {
                throw new Exception("There is no connection to prepare command(ExecuteNonQuery).");
            }

            if (Transaction != null)
            {
                return TransactionFactory.Default(_provider).ExecuteDatasetTypedParamsAsync(CommandTimeOut, Transaction, spName, dataRow);
            }

            return TransactionFactory.Default(_provider).ExecuteDatasetTypedParamsAsync(CommandTimeOut, Connection, spName, dataRow);
        }

        public IDataReader ExecuteReaderTypedParams(string spName, DataRow dataRow)
        {
            if (Connection == null && Transaction == null)
            {
                throw new Exception("There is no connection to prepare command(ExecuteNonQuery).");
            }

            if (Transaction != null)
            {
                return TransactionFactory.Default(_provider).ExecuteReaderTypedParams(CommandTimeOut, Transaction, spName, dataRow);
            }

            return TransactionFactory.Default(_provider).ExecuteReaderTypedParams(CommandTimeOut, Connection, spName, dataRow);
        }

        public Task<IDataReader> ExecuteReaderTypedParamsAsync(string spName, DataRow dataRow)
        {
            if (Connection == null && Transaction == null)
            {
                throw new Exception("There is no connection to prepare command(ExecuteNonQuery).");
            }

            if (Transaction != null)
            {
                return TransactionFactory.Default(_provider).ExecuteReaderTypedParamsAsync(CommandTimeOut, Transaction, spName, dataRow);
            }

            return TransactionFactory.Default(_provider).ExecuteReaderTypedParamsAsync(CommandTimeOut, Connection, spName, dataRow);
        }

        public object ExecuteScalarTypedParams(string spName, DataRow dataRow)
        {
            if (Connection == null && Transaction == null)
            {
                throw new Exception("There is no connection to prepare command(ExecuteNonQuery).");
            }

            if (Transaction != null)
            {
                return TransactionFactory.Default(_provider).ExecuteScalarTypedParams(CommandTimeOut, Transaction, spName, dataRow);
            }

            return TransactionFactory.Default(_provider).ExecuteScalarTypedParams(CommandTimeOut, Connection, spName, dataRow);
        }

        public Task<object> ExecuteScalarTypedParamsAsync(string spName, DataRow dataRow)
        {
            if (Connection == null && Transaction == null)
            {
                throw new Exception("There is no connection to prepare command(ExecuteNonQuery).");
            }

            if (Transaction != null)
            {
                return TransactionFactory.Default(_provider).ExecuteScalarTypedParamsAsync(CommandTimeOut, Transaction, spName, dataRow);
            }

            return TransactionFactory.Default(_provider).ExecuteScalarTypedParamsAsync(CommandTimeOut, Connection, spName, dataRow);
        }

        public int GenerateID(string tableName, string fieldName)
        {
            return GenerateID(tableName, fieldName, string.Empty);
        }

        public int GenerateID(string tableName, string fieldName, string whereClause)
        {
            object obj = ExecuteScalar("SELECT MAX(%q) FROM %q %q", fieldName, tableName, whereClause);
            if (obj == null || obj == DBNull.Value)
            {
                obj = 1;
            }
            else
            {
                obj = Convert.ToInt32(obj) + 1;
                if ((int)obj <= 0)
                {
                    obj = 1;
                }
            }

            return Convert.ToInt32(obj);
        }

        public void HandleError()
        {
            if (Transaction != null)
            {
                try
                {
                    if (_createSession)
                    {
                        _createSession = false;
                        Transaction.Rollback();
                    }
                }
                catch
                {
                }
            }

            ReleaseResources();
        }

        public void Dispose()
        {
            ReleaseResources();
        }
    }
}
