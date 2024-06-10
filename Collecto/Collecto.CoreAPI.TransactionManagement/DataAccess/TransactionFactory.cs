using Collecto.CoreAPI.TransactionManagement.DataAccess.MySql;
using Collecto.CoreAPI.TransactionManagement.DataAccess.Odbc;
using Collecto.CoreAPI.TransactionManagement.DataAccess.Oracle;
using Collecto.CoreAPI.TransactionManagement.DataAccess.SQL;
using Collecto.CoreAPI.TransactionManagement.Molels;
using System.Data;
using System.Data.Common;

namespace Collecto.CoreAPI.TransactionManagement.DataAccess
{
    public abstract class TransactionFactory
    {
        private enum IDbConnectionOwnership
        {
            Internal,
            External
        }

        private static Provider _provider;

        private static TransactionFactory _default;

        public static TransactionFactory Default(Provider provider)
        {
            if (_default == null || provider != _provider)
            {
                _provider = provider;
                switch (_provider)
                {
                    case Provider.Sql:
                        _default = new SqlHelper();
                        break;
                    case Provider.Oracle:
                        _default = new OracleHelper();
                        break;
                    case Provider.MySql:
                        _default = new MySqlHelper();
                        break;
                    case Provider.Odbc:
                        _default = new OdbcHelper();
                        break;
                }
            }

            return _default;
        }

        public abstract int ExecuteNonQuery(int commandTimeout, DbConnection connection, CommandType commandType, string commandText);

        public abstract int ExecuteNonQuery(int commandTimeout, DbConnection connection, string spName, params object[] parameterValues);

        public abstract int ExecuteNonQuery(int commandTimeout, DbConnection connection, CommandType commandType, string commandText, params IDataParameter[] commandParameters);

        public abstract Task<int> ExecuteNonQueryAsync(int commandTimeout, DbConnection connection, CommandType commandType, string commandText);

        public abstract Task<int> ExecuteNonQueryAsync(int commandTimeout, DbConnection connection, string spName, params object[] parameterValues);

        public abstract Task<int> ExecuteNonQueryAsync(int commandTimeout, DbConnection connection, CommandType commandType, string commandText, params IDataParameter[] commandParameters);

        public abstract int ExecuteNonQuery(int commandTimeout, DbTransaction transaction, CommandType commandType, string commandText);

        public abstract int ExecuteNonQuery(int commandTimeout, DbTransaction transaction, string spName, params object[] parameterValues);

        public abstract int ExecuteNonQuery(int commandTimeout, DbTransaction transaction, CommandType commandType, string commandText, params IDataParameter[] commandParameters);

        public abstract Task<int> ExecuteNonQueryAsync(int commandTimeout, DbTransaction transaction, CommandType commandType, string commandText);

        public abstract Task<int> ExecuteNonQueryAsync(int commandTimeout, DbTransaction transaction, string spName, params object[] parameterValues);

        public abstract Task<int> ExecuteNonQueryAsync(int commandTimeout, DbTransaction transaction, CommandType commandType, string commandText, params IDataParameter[] commandParameters);

        public abstract DataSet ExecuteDataset(int commandTimeout, DbConnection connection, CommandType commandType, string commandText);

        public abstract DataSet ExecuteDataset(int commandTimeout, DbConnection connection, string spName, params object[] parameterValues);

        public abstract DataSet ExecuteDataset(int commandTimeout, DbConnection connection, CommandType commandType, string commandText, params IDataParameter[] commandParameters);

        public abstract Task<DataSet> ExecuteDatasetAsync(int commandTimeout, DbConnection connection, CommandType commandType, string commandText);

        public abstract Task<DataSet> ExecuteDatasetAsync(int commandTimeout, DbConnection connection, string spName, params object[] parameterValues);

        public abstract Task<DataSet> ExecuteDatasetAsync(int commandTimeout, DbConnection connection, CommandType commandType, string commandText, params IDataParameter[] commandParameters);

        public abstract DataSet ExecuteDataset(int commandTimeout, DbTransaction transaction, CommandType commandType, string commandText);

        public abstract DataSet ExecuteDataset(int commandTimeout, DbTransaction transaction, string spName, params object[] parameterValues);

        public abstract DataSet ExecuteDataset(int commandTimeout, DbTransaction transaction, CommandType commandType, string commandText, params IDataParameter[] commandParameters);

        public abstract Task<DataSet> ExecuteDatasetAsync(int commandTimeout, DbTransaction transaction, CommandType commandType, string commandText);

        public abstract Task<DataSet> ExecuteDatasetAsync(int commandTimeout, DbTransaction transaction, string spName, params object[] parameterValues);

        public abstract Task<DataSet> ExecuteDatasetAsync(int commandTimeout, DbTransaction transaction, CommandType commandType, string commandText, params IDataParameter[] commandParameters);

        public abstract IDataReader ExecuteReader(int commandTimeout, DbConnection connection, CommandType commandType, string commandText);

        public abstract IDataReader ExecuteReader(int commandTimeout, DbConnection connection, string spName, params object[] parameterValues);

        public abstract IDataReader ExecuteReader(int commandTimeout, DbConnection connection, CommandType commandType, string commandText, params IDataParameter[] commandParameters);

        public abstract Task<IDataReader> ExecuteReaderAsync(int commandTimeout, DbConnection connection, CommandType commandType, string commandText);

        public abstract Task<IDataReader> ExecuteReaderAsync(int commandTimeout, DbConnection connection, string spName, params object[] parameterValues);

        public abstract Task<IDataReader> ExecuteReaderAsync(int commandTimeout, DbConnection connection, CommandType commandType, string commandText, params IDataParameter[] commandParameters);

        public abstract IDataReader ExecuteReader(int commandTimeout, DbTransaction transaction, CommandType commandType, string commandText);

        public abstract IDataReader ExecuteReader(int commandTimeout, DbTransaction transaction, string spName, params object[] parameterValues);

        public abstract IDataReader ExecuteReader(int commandTimeout, DbTransaction transaction, CommandType commandType, string commandText, params IDataParameter[] commandParameters);

        public abstract Task<IDataReader> ExecuteReaderAsync(int commandTimeout, DbTransaction transaction, CommandType commandType, string commandText);

        public abstract Task<IDataReader> ExecuteReaderAsync(int commandTimeout, DbTransaction transaction, string spName, params object[] parameterValues);

        public abstract Task<IDataReader> ExecuteReaderAsync(int commandTimeout, DbTransaction transaction, CommandType commandType, string commandText, params IDataParameter[] commandParameters);

        public abstract object ExecuteScalar(int commandTimeout, DbConnection connection, CommandType commandType, string commandText);

        public abstract object ExecuteScalar(int commandTimeout, DbConnection connection, string spName, params object[] parameterValues);

        public abstract object ExecuteScalar(int commandTimeout, DbConnection connection, CommandType commandType, string commandText, params IDataParameter[] commandParameters);

        public abstract Task<object> ExecuteScalarAsync(int commandTimeout, DbConnection connection, CommandType commandType, string commandText);

        public abstract Task<object> ExecuteScalarAsync(int commandTimeout, DbConnection connection, string spName, params object[] parameterValues);

        public abstract Task<object> ExecuteScalarAsync(int commandTimeout, DbConnection connection, CommandType commandType, string commandText, params IDataParameter[] commandParameters);

        public abstract object ExecuteScalar(int commandTimeout, DbTransaction transaction, CommandType commandType, string commandText);

        public abstract object ExecuteScalar(int commandTimeout, DbTransaction transaction, string spName, params object[] parameterValues);

        public abstract object ExecuteScalar(int commandTimeout, DbTransaction transaction, CommandType commandType, string commandText, params IDataParameter[] commandParameters);

        public abstract Task<object> ExecuteScalarAsync(int commandTimeout, DbTransaction transaction, CommandType commandType, string commandText);

        public abstract Task<object> ExecuteScalarAsync(int commandTimeout, DbTransaction transaction, string spName, params object[] parameterValues);

        public abstract Task<object> ExecuteScalarAsync(int commandTimeout, DbTransaction transaction, CommandType commandType, string commandText, params IDataParameter[] commandParameters);

        public abstract void FillDataset(int commandTimeout, DbConnection connection, CommandType commandType, string commandText, DataSet dataSet, string[] tableNames);

        public abstract void FillDataset(int commandTimeout, DbConnection connection, string spName, DataSet dataSet, string[] tableNames, params object[] parameterValues);

        public abstract void FillDataset(int commandTimeout, DbConnection connection, CommandType commandType, string commandText, DataSet dataSet, string[] tableNames, params IDataParameter[] commandParameters);

        public abstract void FillDataset(int commandTimeout, DbTransaction transaction, CommandType commandType, string commandText, DataSet dataSet, string[] tableNames);

        public abstract void FillDataset(int commandTimeout, DbTransaction transaction, string spName, DataSet dataSet, string[] tableNames, params object[] parameterValues);

        public abstract void FillDataset(int commandTimeout, DbTransaction transaction, CommandType commandType, string commandText, DataSet dataSet, string[] tableNames, params IDataParameter[] commandParameters);

        public abstract void UpdateDataset(DbCommand insertCommand, DbCommand deleteCommand, DbCommand updateCommand, DataSet dataSet, string tableName);

        public abstract DbCommand CreateCommand(DbConnection connection, string spName, params string[] sourceColumns);

        public abstract int ExecuteNonQueryTypedParams(int commandTimeout, DbConnection connection, string spName, DataRow dataRow);

        public abstract int ExecuteNonQueryTypedParams(int commandTimeout, DbTransaction transaction, string spName, DataRow dataRow);

        public abstract Task<int> ExecuteNonQueryTypedParamsAsync(int commandTimeout, DbConnection connection, string spName, DataRow dataRow);

        public abstract Task<int> ExecuteNonQueryTypedParamsAsync(int commandTimeout, DbTransaction transaction, string spName, DataRow dataRow);

        public abstract DataSet ExecuteDatasetTypedParams(int commandTimeout, DbConnection connection, string spName, DataRow dataRow);

        public abstract DataSet ExecuteDatasetTypedParams(int commandTimeout, DbTransaction transaction, string spName, DataRow dataRow);

        public abstract Task<DataSet> ExecuteDatasetTypedParamsAsync(int commandTimeout, DbConnection connection, string spName, DataRow dataRow);

        public abstract Task<DataSet> ExecuteDatasetTypedParamsAsync(int commandTimeout, DbTransaction transaction, string spName, DataRow dataRow);

        public abstract IDataReader ExecuteReaderTypedParams(int commandTimeout, DbConnection connection, string spName, DataRow dataRow);

        public abstract IDataReader ExecuteReaderTypedParams(int commandTimeout, DbTransaction transaction, string spName, DataRow dataRow);

        public abstract Task<IDataReader> ExecuteReaderTypedParamsAsync(int commandTimeout, DbConnection connection, string spName, DataRow dataRow);

        public abstract Task<IDataReader> ExecuteReaderTypedParamsAsync(int commandTimeout, DbTransaction transaction, string spName, DataRow dataRow);

        public abstract object ExecuteScalarTypedParams(int commandTimeout, DbConnection connection, string spName, DataRow dataRow);

        public abstract object ExecuteScalarTypedParams(int commandTimeout, DbTransaction transaction, string spName, DataRow dataRow);

        public abstract Task<object> ExecuteScalarTypedParamsAsync(int commandTimeout, DbConnection connection, string spName, DataRow dataRow);

        public abstract Task<object> ExecuteScalarTypedParamsAsync(int commandTimeout, DbTransaction transaction, string spName, DataRow dataRow);
    }
}
