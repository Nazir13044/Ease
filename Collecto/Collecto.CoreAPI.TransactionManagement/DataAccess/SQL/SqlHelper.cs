using System.Data;
using System.Data.Common;
using System.Data.SqlClient;

namespace Collecto.CoreAPI.TransactionManagement.DataAccess.SQL
{
    public sealed class SqlHelper : TransactionFactory
    {
        private enum SqlConnectionOwnership
        {
            Internal,
            External
        }

        private static void AttachParameters(SqlCommand command, SqlParameter[] commandParameters)
        {
            if (command == null)
            {
                throw new ArgumentNullException("command");
            }

            if (commandParameters == null || commandParameters.Length == 0)
            {
                return;
            }

            foreach (SqlParameter sqlParameter in commandParameters)
            {
                if (sqlParameter != null)
                {
                    if ((sqlParameter.Direction == ParameterDirection.InputOutput || sqlParameter.Direction == ParameterDirection.Input) && sqlParameter.Value == null)
                    {
                        sqlParameter.Value = DBNull.Value;
                    }

                    command.Parameters.Add(sqlParameter);
                }
            }
        }

        private static void AssignParameterValues(SqlParameter[] commandParameters, DataRow dataRow)
        {
            if (commandParameters == null || dataRow == null)
            {
                return;
            }

            int num = 0;
            foreach (SqlParameter sqlParameter in commandParameters)
            {
                if (sqlParameter.ParameterName == null || sqlParameter.ParameterName.Length <= 1)
                {
                    throw new Exception($"Please provide a valid parameter name on the parameter #{num}, the ParameterName property has the following value: '{sqlParameter.ParameterName}'.");
                }

                DataColumnCollection columns = dataRow.Table.Columns;
                string parameterName = sqlParameter.ParameterName;
                if (columns.IndexOf(parameterName.Substring(1, parameterName.Length - 1)) != -1)
                {
                    parameterName = sqlParameter.ParameterName;
                    sqlParameter.Value = dataRow[parameterName.Substring(1, parameterName.Length - 1)];
                }

                num++;
            }
        }

        private static void AssignParameterValues(SqlParameter[] commandParameters, object[] parameterValues)
        {
            if (commandParameters == null || parameterValues == null)
            {
                return;
            }

            if (commandParameters.Length != parameterValues.Length)
            {
                throw new ArgumentException("Parameter count does not match Parameter Value count.");
            }

            int i = 0;
            for (int num = commandParameters.Length; i < num; i++)
            {
                if (parameterValues[i] is SqlParameter sqlParameter)
                {
                    if (sqlParameter.Value == null)
                    {
                        commandParameters[i].Value = DBNull.Value;
                    }
                    else
                    {
                        commandParameters[i].Value = sqlParameter.Value;
                    }
                }
                else if (parameterValues[i] == null)
                {
                    commandParameters[i].Value = DBNull.Value;
                }
                else
                {
                    commandParameters[i].Value = parameterValues[i];
                }
            }
        }

        private static void AssignReturnValues(SqlParameter[] commandParameters, object[] parameterValues)
        {
            if (commandParameters == null || parameterValues == null)
            {
                return;
            }

            if (commandParameters.Length != parameterValues.Length)
            {
                throw new ArgumentException("Parameter count does not match Parameter Value count.");
            }

            int i = 0;
            for (int num = commandParameters.Length; i < num; i++)
            {
                if (commandParameters[i].Direction == ParameterDirection.Output || commandParameters[i].Direction == ParameterDirection.InputOutput || commandParameters[i].Direction == ParameterDirection.ReturnValue)
                {
                    parameterValues[i] = commandParameters[i];
                }
            }
        }

        private static void PrepareCommand(int commandTimeout, SqlCommand command, SqlConnection connection, SqlTransaction transaction, CommandType commandType, string commandText, SqlParameter[] commandParameters, out bool mustCloseConnection)
        {
            if (command == null)
            {
                throw new ArgumentNullException("command");
            }

            if (commandText == null || commandText.Length == 0)
            {
                throw new ArgumentNullException("commandText");
            }

            if (connection.State != ConnectionState.Open)
            {
                mustCloseConnection = true;
                connection.Open();
            }
            else
            {
                mustCloseConnection = false;
            }

            command.Connection = connection;
            command.CommandText = commandText;
            if (commandTimeout > 0)
            {
                command.CommandTimeout = commandTimeout;
            }

            if (transaction != null)
            {
                if (transaction.Connection == null)
                {
                    throw new ArgumentException("The transaction was rollbacked or committed, please provide an open transaction.", "transaction");
                }

                command.Transaction = transaction;
            }

            command.CommandType = commandType;
            if (commandParameters != null)
            {
                AttachParameters(command, commandParameters);
            }
        }

        public override int ExecuteNonQuery(int commandTimeout, DbConnection connection, CommandType commandType, string commandText)
        {
            return ExecuteNonQuery(commandTimeout, connection, commandType, commandText, (IDataParameter[])null);
        }

        public override int ExecuteNonQuery(int commandTimeout, DbConnection connection, CommandType commandType, string commandText, params IDataParameter[] commandParameters)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            using SqlCommand sqlCommand = new SqlCommand();
            PrepareCommand(commandTimeout, sqlCommand, (SqlConnection)connection, null, commandType, commandText, (SqlParameter[])commandParameters, out var mustCloseConnection);
            int result = sqlCommand.ExecuteNonQuery();
            sqlCommand.Parameters.Clear();
            if (mustCloseConnection)
            {
                connection.Close();
            }

            return result;
        }

        public override int ExecuteNonQuery(int commandTimeout, DbConnection connection, string spName, params object[] parameterValues)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            if (spName == null || spName.Length == 0)
            {
                throw new ArgumentNullException("spName");
            }

            if (parameterValues != null && parameterValues.Length != 0)
            {
                SqlParameter[] spParameterSet = SqlHelperParameterCache.GetSpParameterSet((SqlConnection)connection, spName);
                AssignParameterValues(spParameterSet, parameterValues);
                IDataParameter[] commandParameters = spParameterSet;
                int result = ExecuteNonQuery(commandTimeout, connection, CommandType.StoredProcedure, spName, commandParameters);
                AssignReturnValues(spParameterSet, parameterValues);
                return result;
            }

            return ExecuteNonQuery(commandTimeout, connection, CommandType.StoredProcedure, spName);
        }

        public override int ExecuteNonQuery(int commandTimeout, DbTransaction transaction, CommandType commandType, string commandText)
        {
            return ExecuteNonQuery(commandTimeout, transaction, commandType, commandText, (IDataParameter[])null);
        }

        public override int ExecuteNonQuery(int commandTimeout, DbTransaction transaction, CommandType commandType, string commandText, params IDataParameter[] commandParameters)
        {
            if (transaction == null)
            {
                throw new ArgumentNullException("transaction");
            }

            if (transaction != null && transaction.Connection == null)
            {
                throw new ArgumentException("The transaction was rollbacked or committed, please provide an open transaction.", "transaction");
            }

            using SqlCommand sqlCommand = new();
            PrepareCommand(commandTimeout, sqlCommand, (SqlConnection)transaction.Connection, (SqlTransaction)transaction, commandType, commandText, (SqlParameter[])commandParameters, out var _);
            int result = sqlCommand.ExecuteNonQuery();
            sqlCommand.Parameters.Clear();
            return result;
        }

        public override int ExecuteNonQuery(int commandTimeout, DbTransaction transaction, string spName, params object[] parameterValues)
        {
            if (transaction == null)
            {
                throw new ArgumentNullException("transaction");
            }

            if (transaction != null && transaction.Connection == null)
            {
                throw new ArgumentException("The transaction was rollbacked or committed, please provide an open transaction.", "transaction");
            }

            if (spName == null || spName.Length == 0)
            {
                throw new ArgumentNullException("spName");
            }

            if (parameterValues != null && parameterValues.Length != 0)
            {
                SqlParameter[] spParameterSet = SqlHelperParameterCache.GetSpParameterSet((SqlConnection)transaction.Connection, spName);
                AssignParameterValues(spParameterSet, parameterValues);
                SqlTransaction transaction2 = (SqlTransaction)transaction;
                IDataParameter[] commandParameters = spParameterSet;
                int result = ExecuteNonQuery(commandTimeout, transaction2, CommandType.StoredProcedure, spName, commandParameters);
                AssignReturnValues(spParameterSet, parameterValues);
                return result;
            }

            return ExecuteNonQuery(commandTimeout, transaction, CommandType.StoredProcedure, spName);
        }

        public override Task<int> ExecuteNonQueryAsync(int commandTimeout, DbConnection connection, CommandType commandType, string commandText)
        {
            return ExecuteNonQueryAsync(commandTimeout, connection, commandType, commandText, (IDataParameter[])null);
        }

        public override Task<int> ExecuteNonQueryAsync(int commandTimeout, DbConnection connection, CommandType commandType, string commandText, params IDataParameter[] commandParameters)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            using SqlCommand sqlCommand = new SqlCommand();
            PrepareCommand(commandTimeout, sqlCommand, (SqlConnection)connection, null, commandType, commandText, (SqlParameter[])commandParameters, out var mustCloseConnection);
            Task<int> result = sqlCommand.ExecuteNonQueryAsync();
            sqlCommand.Parameters.Clear();
            if (mustCloseConnection)
            {
                connection.Close();
            }

            return result;
        }

        public override Task<int> ExecuteNonQueryAsync(int commandTimeout, DbConnection connection, string spName, params object[] parameterValues)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            if (spName == null || spName.Length == 0)
            {
                throw new ArgumentNullException("spName");
            }

            if (parameterValues != null && parameterValues.Length != 0)
            {
                SqlParameter[] spParameterSet = SqlHelperParameterCache.GetSpParameterSet((SqlConnection)connection, spName);
                AssignParameterValues(spParameterSet, parameterValues);
                IDataParameter[] commandParameters = spParameterSet;
                Task<int> result = ExecuteNonQueryAsync(commandTimeout, connection, CommandType.StoredProcedure, spName, commandParameters);
                AssignReturnValues(spParameterSet, parameterValues);
                return result;
            }

            return ExecuteNonQueryAsync(commandTimeout, connection, CommandType.StoredProcedure, spName);
        }

        public override Task<int> ExecuteNonQueryAsync(int commandTimeout, DbTransaction transaction, CommandType commandType, string commandText)
        {
            return ExecuteNonQueryAsync(commandTimeout, transaction, commandType, commandText, (IDataParameter[])null);
        }

        public override Task<int> ExecuteNonQueryAsync(int commandTimeout, DbTransaction transaction, CommandType commandType, string commandText, params IDataParameter[] commandParameters)
        {
            if (transaction == null)
            {
                throw new ArgumentNullException("transaction");
            }

            if (transaction != null && transaction.Connection == null)
            {
                throw new ArgumentException("The transaction was rollbacked or committed, please provide an open transaction.", "transaction");
            }

            using SqlCommand sqlCommand = new SqlCommand();
            PrepareCommand(commandTimeout, sqlCommand, (SqlConnection)transaction.Connection, (SqlTransaction)transaction, commandType, commandText, (SqlParameter[])commandParameters, out var _);
            Task<int> result = sqlCommand.ExecuteNonQueryAsync();
            sqlCommand.Parameters.Clear();
            return result;
        }

        public override Task<int> ExecuteNonQueryAsync(int commandTimeout, DbTransaction transaction, string spName, params object[] parameterValues)
        {
            if (transaction == null)
            {
                throw new ArgumentNullException("transaction");
            }

            if (transaction != null && transaction.Connection == null)
            {
                throw new ArgumentException("The transaction was rollbacked or committed, please provide an open transaction.", "transaction");
            }

            if (spName == null || spName.Length == 0)
            {
                throw new ArgumentNullException("spName");
            }

            if (parameterValues != null && parameterValues.Length != 0)
            {
                SqlParameter[] spParameterSet = SqlHelperParameterCache.GetSpParameterSet((SqlConnection)transaction.Connection, spName);
                AssignParameterValues(spParameterSet, parameterValues);
                SqlTransaction transaction2 = (SqlTransaction)transaction;
                IDataParameter[] commandParameters = spParameterSet;
                Task<int> result = ExecuteNonQueryAsync(commandTimeout, transaction2, CommandType.StoredProcedure, spName, commandParameters);
                AssignReturnValues(spParameterSet, parameterValues);
                return result;
            }

            return ExecuteNonQueryAsync(commandTimeout, transaction, CommandType.StoredProcedure, spName);
        }

        public override DataSet ExecuteDataset(int commandTimeout, DbConnection connection, CommandType commandType, string commandText)
        {
            return ExecuteDataset(commandTimeout, (DbConnection)(SqlConnection)connection, commandType, commandText, (IDataParameter[])null);
        }

        public override DataSet ExecuteDataset(int commandTimeout, DbConnection connection, CommandType commandType, string commandText, params IDataParameter[] commandParameters)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            using SqlCommand sqlCommand = new SqlCommand();
            PrepareCommand(commandTimeout, sqlCommand, (SqlConnection)connection, null, commandType, commandText, (SqlParameter[])commandParameters, out var mustCloseConnection);
            using SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(sqlCommand);
            DataSet dataSet = new DataSet();
            sqlDataAdapter.Fill(dataSet);
            sqlCommand.Parameters.Clear();
            if (mustCloseConnection)
            {
                connection.Close();
            }

            return dataSet;
        }

        public override DataSet ExecuteDataset(int commandTimeout, DbConnection connection, string spName, params object[] parameterValues)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            if (spName == null || spName.Length == 0)
            {
                throw new ArgumentNullException("spName");
            }

            if (parameterValues != null && parameterValues.Length != 0)
            {
                SqlParameter[] spParameterSet = SqlHelperParameterCache.GetSpParameterSet((SqlConnection)connection, spName);
                AssignParameterValues(spParameterSet, parameterValues);
                SqlConnection connection2 = (SqlConnection)connection;
                IDataParameter[] commandParameters = spParameterSet;
                DataSet result = ExecuteDataset(commandTimeout, connection2, CommandType.StoredProcedure, spName, commandParameters);
                AssignReturnValues(spParameterSet, parameterValues);
                return result;
            }

            return ExecuteDataset(commandTimeout, (SqlConnection)connection, CommandType.StoredProcedure, spName);
        }

        public override DataSet ExecuteDataset(int commandTimeout, DbTransaction transaction, CommandType commandType, string commandText)
        {
            return ExecuteDataset(commandTimeout, (DbTransaction)(SqlTransaction)transaction, commandType, commandText, (IDataParameter[])null);
        }

        public override DataSet ExecuteDataset(int commandTimeout, DbTransaction transaction, CommandType commandType, string commandText, params IDataParameter[] commandParameters)
        {
            if (transaction == null)
            {
                throw new ArgumentNullException("transaction");
            }

            if (transaction != null && transaction.Connection == null)
            {
                throw new ArgumentException("The transaction was rollbacked or committed, please provide an open transaction.", "transaction");
            }

            using SqlCommand sqlCommand = new SqlCommand();
            PrepareCommand(commandTimeout, sqlCommand, (SqlConnection)transaction.Connection, (SqlTransaction)transaction, commandType, commandText, (SqlParameter[])commandParameters, out var _);
            using SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(sqlCommand);
            DataSet dataSet = new DataSet();
            sqlDataAdapter.Fill(dataSet);
            sqlCommand.Parameters.Clear();
            return dataSet;
        }

        public override DataSet ExecuteDataset(int commandTimeout, DbTransaction transaction, string spName, params object[] parameterValues)
        {
            if (transaction == null)
            {
                throw new ArgumentNullException("transaction");
            }

            if (transaction != null && transaction.Connection == null)
            {
                throw new ArgumentException("The transaction was rollbacked or committed, please provide an open transaction.", "transaction");
            }

            if (spName == null || spName.Length == 0)
            {
                throw new ArgumentNullException("spName");
            }

            if (parameterValues != null && parameterValues.Length != 0)
            {
                SqlParameter[] spParameterSet = SqlHelperParameterCache.GetSpParameterSet((SqlConnection)transaction.Connection, spName);
                AssignParameterValues(spParameterSet, parameterValues);
                SqlTransaction transaction2 = (SqlTransaction)transaction;
                IDataParameter[] commandParameters = spParameterSet;
                DataSet result = ExecuteDataset(commandTimeout, transaction2, CommandType.StoredProcedure, spName, commandParameters);
                AssignReturnValues(spParameterSet, parameterValues);
                return result;
            }

            return ExecuteDataset(commandTimeout, transaction, CommandType.StoredProcedure, spName);
        }

        private static Task<DataSet> ExecuteAsyncDataset(int commandTimeout, DbConnection connection, DbTransaction transaction, CommandType commandType, string commandText, params IDataParameter[] commandParameters)
        {
            SqlCommand sqlCommand = new SqlCommand();
            bool mustCloseConnection;
            if (transaction != null)
            {
                PrepareCommand(commandTimeout, sqlCommand, (SqlConnection)transaction.Connection, (SqlTransaction)transaction, commandType, commandText, (SqlParameter[])commandParameters, out mustCloseConnection);
            }
            else
            {
                PrepareCommand(commandTimeout, sqlCommand, (SqlConnection)connection, null, commandType, commandText, (SqlParameter[])commandParameters, out mustCloseConnection);
            }

            DataSet dataSet = new DataSet();
            using (SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(sqlCommand))
            {
                sqlDataAdapter.Fill(dataSet);
                sqlCommand.Parameters.Clear();
                if (mustCloseConnection)
                {
                    connection.Close();
                }
            }

            return Task.FromResult(dataSet);
        }

        public override Task<DataSet> ExecuteDatasetAsync(int commandTimeout, DbConnection connection, CommandType commandType, string commandText)
        {
            return ExecuteDatasetAsync(commandTimeout, (DbConnection)(SqlConnection)connection, commandType, commandText, (IDataParameter[])null);
        }

        public override Task<DataSet> ExecuteDatasetAsync(int commandTimeout, DbConnection connection, CommandType commandType, string commandText, params IDataParameter[] commandParameters)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            return ExecuteAsyncDataset(commandTimeout, connection, null, commandType, commandText, commandParameters);
        }

        public override Task<DataSet> ExecuteDatasetAsync(int commandTimeout, DbConnection connection, string spName, params object[] parameterValues)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            if (spName == null || spName.Length == 0)
            {
                throw new ArgumentNullException("spName");
            }

            if (parameterValues != null && parameterValues.Length != 0)
            {
                SqlParameter[] spParameterSet = SqlHelperParameterCache.GetSpParameterSet((SqlConnection)connection, spName);
                AssignParameterValues(spParameterSet, parameterValues);
                SqlConnection connection2 = (SqlConnection)connection;
                IDataParameter[] commandParameters = spParameterSet;
                return ExecuteDatasetAsync(commandTimeout, connection2, CommandType.StoredProcedure, spName, commandParameters);
            }

            return ExecuteDatasetAsync(commandTimeout, (SqlConnection)connection, CommandType.StoredProcedure, spName);
        }

        public override Task<DataSet> ExecuteDatasetAsync(int commandTimeout, DbTransaction transaction, CommandType commandType, string commandText)
        {
            return ExecuteDatasetAsync(commandTimeout, (DbTransaction)(SqlTransaction)transaction, commandType, commandText, (IDataParameter[])null);
        }

        public override Task<DataSet> ExecuteDatasetAsync(int commandTimeout, DbTransaction transaction, CommandType commandType, string commandText, params IDataParameter[] commandParameters)
        {
            if (transaction == null)
            {
                throw new ArgumentNullException("transaction");
            }

            if (transaction != null && transaction.Connection == null)
            {
                throw new ArgumentException("The transaction was rollbacked or committed, please provide an open transaction.", "transaction");
            }

            return ExecuteAsyncDataset(commandTimeout, null, transaction, commandType, commandText, commandParameters);
        }

        public override Task<DataSet> ExecuteDatasetAsync(int commandTimeout, DbTransaction transaction, string spName, params object[] parameterValues)
        {
            if (transaction == null)
            {
                throw new ArgumentNullException("transaction");
            }

            if (transaction != null && transaction.Connection == null)
            {
                throw new ArgumentException("The transaction was rollbacked or committed, please provide an open transaction.", "transaction");
            }

            if (spName == null || spName.Length == 0)
            {
                throw new ArgumentNullException("spName");
            }

            if (parameterValues != null && parameterValues.Length != 0)
            {
                SqlParameter[] spParameterSet = SqlHelperParameterCache.GetSpParameterSet((SqlConnection)transaction.Connection, spName);
                AssignParameterValues(spParameterSet, parameterValues);
                SqlTransaction transaction2 = (SqlTransaction)transaction;
                IDataParameter[] commandParameters = spParameterSet;
                return ExecuteDatasetAsync(commandTimeout, transaction2, CommandType.StoredProcedure, spName, commandParameters);
            }

            return ExecuteDatasetAsync(commandTimeout, transaction, CommandType.StoredProcedure, spName);
        }

        private static IDataReader ExecuteReader(int commandTimeout, SqlConnection connection, SqlTransaction transaction, CommandType commandType, string commandText, SqlParameter[] commandParameters, SqlConnectionOwnership connectionOwnership)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            bool mustCloseConnection = false;
            SqlCommand sqlCommand = new SqlCommand();
            try
            {
                PrepareCommand(commandTimeout, sqlCommand, connection, transaction, commandType, commandText, commandParameters, out mustCloseConnection);
                SqlDataReader result = ((connectionOwnership != SqlConnectionOwnership.External) ? sqlCommand.ExecuteReader(CommandBehavior.CloseConnection) : sqlCommand.ExecuteReader());
                bool flag = true;
                foreach (SqlParameter parameter in sqlCommand.Parameters)
                {
                    if (parameter.Direction != ParameterDirection.Input)
                    {
                        flag = false;
                    }
                }

                if (flag)
                {
                    sqlCommand.Parameters.Clear();
                }

                return result;
            }
            catch
            {
                if (mustCloseConnection)
                {
                    connection.Close();
                }

                throw;
            }
        }

        public override IDataReader ExecuteReader(int commandTimeout, DbConnection connection, CommandType commandType, string commandText)
        {
            return ExecuteReader(commandTimeout, connection, commandType, commandText, (IDataParameter[])null);
        }

        public override IDataReader ExecuteReader(int commandTimeout, DbConnection connection, CommandType commandType, string commandText, params IDataParameter[] commandParameters)
        {
            return ExecuteReader(commandTimeout, (SqlConnection)connection, null, commandType, commandText, (SqlParameter[])commandParameters, SqlConnectionOwnership.External);
        }

        public override IDataReader ExecuteReader(int commandTimeout, DbConnection connection, string spName, params object[] parameterValues)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            if (spName == null || spName.Length == 0)
            {
                throw new ArgumentNullException("spName");
            }

            if (parameterValues != null && parameterValues.Length != 0)
            {
                SqlParameter[] spParameterSet = SqlHelperParameterCache.GetSpParameterSet((SqlConnection)connection, spName);
                AssignParameterValues(spParameterSet, parameterValues);
                IDataParameter[] commandParameters = spParameterSet;
                IDataReader result = ExecuteReader(commandTimeout, connection, CommandType.StoredProcedure, spName, commandParameters);
                AssignReturnValues(spParameterSet, parameterValues);
                return result;
            }

            return ExecuteReader(commandTimeout, connection, CommandType.StoredProcedure, spName);
        }

        public override IDataReader ExecuteReader(int commandTimeout, DbTransaction transaction, CommandType commandType, string commandText)
        {
            return ExecuteReader(commandTimeout, transaction, commandType, commandText, (IDataParameter[])null);
        }

        public override IDataReader ExecuteReader(int commandTimeout, DbTransaction transaction, CommandType commandType, string commandText, params IDataParameter[] commandParameters)
        {
            if (transaction == null)
            {
                throw new ArgumentNullException("transaction");
            }

            if (transaction != null && transaction.Connection == null)
            {
                throw new ArgumentException("The transaction was rollbacked or committed, please provide an open transaction.", "transaction");
            }

            return ExecuteReader(commandTimeout, (SqlConnection)transaction.Connection, (SqlTransaction)transaction, commandType, commandText, (SqlParameter[])commandParameters, SqlConnectionOwnership.External);
        }

        public override IDataReader ExecuteReader(int commandTimeout, DbTransaction transaction, string spName, params object[] parameterValues)
        {
            if (transaction == null)
            {
                throw new ArgumentNullException("transaction");
            }

            if (transaction != null && transaction.Connection == null)
            {
                throw new ArgumentException("The transaction was rollbacked or committed, please provide an open transaction.", "transaction");
            }

            if (spName == null || spName.Length == 0)
            {
                throw new ArgumentNullException("spName");
            }

            if (parameterValues != null && parameterValues.Length != 0)
            {
                SqlParameter[] spParameterSet = SqlHelperParameterCache.GetSpParameterSet((SqlConnection)transaction.Connection, spName);
                AssignParameterValues(spParameterSet, parameterValues);
                IDataParameter[] commandParameters = spParameterSet;
                IDataReader result = ExecuteReader(commandTimeout, transaction, CommandType.StoredProcedure, spName, commandParameters);
                AssignReturnValues(spParameterSet, parameterValues);
                return result;
            }

            return ExecuteReader(commandTimeout, transaction, CommandType.StoredProcedure, spName);
        }

        public override Task<IDataReader> ExecuteReaderAsync(int commandTimeout, DbConnection connection, CommandType commandType, string commandText)
        {
            return ExecuteReaderAsync(commandTimeout, connection, commandType, commandText, (IDataParameter[])null);
        }

        public override Task<IDataReader> ExecuteReaderAsync(int commandTimeout, DbConnection connection, CommandType commandType, string commandText, params IDataParameter[] commandParameters)
        {
            return ExecuteReaderAsync(commandTimeout, (SqlConnection)connection, null, commandType, commandText, (SqlParameter[])commandParameters, SqlConnectionOwnership.External);
        }

        public override Task<IDataReader> ExecuteReaderAsync(int commandTimeout, DbConnection connection, string spName, params object[] parameterValues)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            if (spName == null || spName.Length == 0)
            {
                throw new ArgumentNullException("spName");
            }

            if (parameterValues != null && parameterValues.Length != 0)
            {
                SqlParameter[] spParameterSet = SqlHelperParameterCache.GetSpParameterSet((SqlConnection)connection, spName);
                AssignParameterValues(spParameterSet, parameterValues);
                IDataParameter[] commandParameters = spParameterSet;
                return ExecuteReaderAsync(commandTimeout, connection, CommandType.StoredProcedure, spName, commandParameters);
            }

            return ExecuteReaderAsync(commandTimeout, connection, CommandType.StoredProcedure, spName);
        }

        public override Task<IDataReader> ExecuteReaderAsync(int commandTimeout, DbTransaction transaction, CommandType commandType, string commandText)
        {
            return ExecuteReaderAsync(commandTimeout, transaction, commandType, commandText, (IDataParameter[])null);
        }

        public override Task<IDataReader> ExecuteReaderAsync(int commandTimeout, DbTransaction transaction, CommandType commandType, string commandText, params IDataParameter[] commandParameters)
        {
            if (transaction == null)
            {
                throw new ArgumentNullException("transaction");
            }

            if (transaction != null && transaction.Connection == null)
            {
                throw new ArgumentException("The transaction was rollbacked or committed, please provide an open transaction.", "transaction");
            }

            return ExecuteReaderAsync(commandTimeout, (SqlConnection)transaction.Connection, (SqlTransaction)transaction, commandType, commandText, (SqlParameter[])commandParameters, SqlConnectionOwnership.External);
        }

        public override Task<IDataReader> ExecuteReaderAsync(int commandTimeout, DbTransaction transaction, string spName, params object[] parameterValues)
        {
            if (transaction == null)
            {
                throw new ArgumentNullException("transaction");
            }

            if (transaction != null && transaction.Connection == null)
            {
                throw new ArgumentException("The transaction was rollbacked or committed, please provide an open transaction.", "transaction");
            }

            if (spName == null || spName.Length == 0)
            {
                throw new ArgumentNullException("spName");
            }

            if (parameterValues != null && parameterValues.Length != 0)
            {
                SqlParameter[] spParameterSet = SqlHelperParameterCache.GetSpParameterSet((SqlConnection)transaction.Connection, spName);
                AssignParameterValues(spParameterSet, parameterValues);
                IDataParameter[] commandParameters = spParameterSet;
                return ExecuteReaderAsync(commandTimeout, transaction, CommandType.StoredProcedure, spName, commandParameters);
            }

            return ExecuteReaderAsync(commandTimeout, transaction, CommandType.StoredProcedure, spName);
        }

        private static async Task<IDataReader> ExecuteReaderAsync(int commandTimeout, SqlConnection connection, SqlTransaction transaction, CommandType commandType, string commandText, SqlParameter[] commandParameters, SqlConnectionOwnership connectionOwnership)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            bool mustCloseConnection = false;
            SqlCommand cmd = new();
            try
            {
                PrepareCommand(commandTimeout, cmd, connection, transaction, commandType, commandText, commandParameters, out mustCloseConnection);
                SqlDataReader result = ((connectionOwnership != SqlConnectionOwnership.External) ? (await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection)) : (await cmd.ExecuteReaderAsync()));
                bool flag = true;
                foreach (SqlParameter parameter in cmd.Parameters)
                {
                    if (parameter.Direction != ParameterDirection.Input)
                    {
                        flag = false;
                    }
                }

                if (flag)
                {
                    cmd.Parameters.Clear();
                }

                return result;
            }
            catch
            {
                if (mustCloseConnection)
                {
                    connection.Close();
                }

                throw;
            }
        }

        public override object ExecuteScalar(int commandTimeout, DbConnection connection, CommandType commandType, string commandText)
        {
            return ExecuteScalar(commandTimeout, connection, commandType, commandText, (IDataParameter[])null);
        }

        public override object ExecuteScalar(int commandTimeout, DbConnection connection, CommandType commandType, string commandText, params IDataParameter[] commandParameters)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            using SqlCommand sqlCommand = new SqlCommand();
            PrepareCommand(commandTimeout, sqlCommand, (SqlConnection)connection, null, commandType, commandText, (SqlParameter[])commandParameters, out var mustCloseConnection);
            object result = sqlCommand.ExecuteScalar();
            sqlCommand.Parameters.Clear();
            if (mustCloseConnection)
            {
                connection.Close();
            }

            return result;
        }

        public override object ExecuteScalar(int commandTimeout, DbConnection connection, string spName, params object[] parameterValues)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            if (spName == null || spName.Length == 0)
            {
                throw new ArgumentNullException("spName");
            }

            if (parameterValues != null && parameterValues.Length != 0)
            {
                SqlParameter[] spParameterSet = SqlHelperParameterCache.GetSpParameterSet((SqlConnection)connection, spName);
                AssignParameterValues(spParameterSet, parameterValues);
                IDataParameter[] commandParameters = spParameterSet;
                return ExecuteScalar(commandTimeout, connection, CommandType.StoredProcedure, spName, commandParameters);
            }

            return ExecuteScalar(commandTimeout, connection, CommandType.StoredProcedure, spName);
        }

        public override object ExecuteScalar(int commandTimeout, DbTransaction transaction, CommandType commandType, string commandText)
        {
            return ExecuteScalar(commandTimeout, transaction, commandType, commandText, (IDataParameter[])null);
        }

        public override object ExecuteScalar(int commandTimeout, DbTransaction transaction, CommandType commandType, string commandText, params IDataParameter[] commandParameters)
        {
            if (transaction == null)
            {
                throw new ArgumentNullException("transaction");
            }

            if (transaction != null && transaction.Connection == null)
            {
                throw new ArgumentException("The transaction was rollbacked or committed, please provide an open transaction.", "transaction");
            }

            using SqlCommand sqlCommand = new SqlCommand();
            PrepareCommand(commandTimeout, sqlCommand, (SqlConnection)transaction.Connection, (SqlTransaction)transaction, commandType, commandText, (SqlParameter[])commandParameters, out var _);
            object result = sqlCommand.ExecuteScalar();
            sqlCommand.Parameters.Clear();
            return result;
        }

        public override object ExecuteScalar(int commandTimeout, DbTransaction transaction, string spName, params object[] parameterValues)
        {
            if (transaction == null)
            {
                throw new ArgumentNullException("transaction");
            }

            if (transaction != null && transaction.Connection == null)
            {
                throw new ArgumentException("The transaction was rollbacked or committed, please provide an open transaction.", "transaction");
            }

            if (spName == null || spName.Length == 0)
            {
                throw new ArgumentNullException("spName");
            }

            if (parameterValues != null && parameterValues.Length != 0)
            {
                SqlParameter[] spParameterSet = SqlHelperParameterCache.GetSpParameterSet((SqlConnection)transaction.Connection, spName);
                AssignParameterValues(spParameterSet, parameterValues);
                IDataParameter[] commandParameters = spParameterSet;
                return ExecuteScalar(commandTimeout, transaction, CommandType.StoredProcedure, spName, commandParameters);
            }

            return ExecuteScalar(commandTimeout, transaction, CommandType.StoredProcedure, spName);
        }

        public override Task<object> ExecuteScalarAsync(int commandTimeout, DbConnection connection, CommandType commandType, string commandText)
        {
            return ExecuteScalarAsync(commandTimeout, connection, commandType, commandText, (IDataParameter[])null);
        }

        public override Task<object> ExecuteScalarAsync(int commandTimeout, DbConnection connection, CommandType commandType, string commandText, params IDataParameter[] commandParameters)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            using SqlCommand sqlCommand = new SqlCommand();
            PrepareCommand(commandTimeout, sqlCommand, (SqlConnection)connection, null, commandType, commandText, (SqlParameter[])commandParameters, out var mustCloseConnection);
            Task<object?> result = sqlCommand.ExecuteScalarAsync();
            sqlCommand.Parameters.Clear();
            if (mustCloseConnection)
            {
                connection.Close();
            }

            return result;
        }

        public override Task<object> ExecuteScalarAsync(int commandTimeout, DbConnection connection, string spName, params object[] parameterValues)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            if (spName == null || spName.Length == 0)
            {
                throw new ArgumentNullException("spName");
            }

            if (parameterValues != null && parameterValues.Length != 0)
            {
                SqlParameter[] spParameterSet = SqlHelperParameterCache.GetSpParameterSet((SqlConnection)connection, spName);
                AssignParameterValues(spParameterSet, parameterValues);
                IDataParameter[] commandParameters = spParameterSet;
                return ExecuteScalarAsync(commandTimeout, connection, CommandType.StoredProcedure, spName, commandParameters);
            }

            return ExecuteScalarAsync(commandTimeout, connection, CommandType.StoredProcedure, spName);
        }

        public override Task<object> ExecuteScalarAsync(int commandTimeout, DbTransaction transaction, CommandType commandType, string commandText)
        {
            return ExecuteScalarAsync(commandTimeout, transaction, commandType, commandText, (IDataParameter[])null);
        }

        public override Task<object> ExecuteScalarAsync(int commandTimeout, DbTransaction transaction, CommandType commandType, string commandText, params IDataParameter[] commandParameters)
        {
            if (transaction == null)
            {
                throw new ArgumentNullException("transaction");
            }

            if (transaction != null && transaction.Connection == null)
            {
                throw new ArgumentException("The transaction was rollbacked or committed, please provide an open transaction.", "transaction");
            }

            using SqlCommand sqlCommand = new SqlCommand();
            PrepareCommand(commandTimeout, sqlCommand, (SqlConnection)transaction.Connection, (SqlTransaction)transaction, commandType, commandText, (SqlParameter[])commandParameters, out var _);
            Task<object?> result = sqlCommand.ExecuteScalarAsync();
            sqlCommand.Parameters.Clear();
            return result;
        }

        public override Task<object> ExecuteScalarAsync(int commandTimeout, DbTransaction transaction, string spName, params object[] parameterValues)
        {
            if (transaction == null)
            {
                throw new ArgumentNullException("transaction");
            }

            if (transaction != null && transaction.Connection == null)
            {
                throw new ArgumentException("The transaction was rollbacked or committed, please provide an open transaction.", "transaction");
            }

            if (spName == null || spName.Length == 0)
            {
                throw new ArgumentNullException("spName");
            }

            if (parameterValues != null && parameterValues.Length != 0)
            {
                SqlParameter[] spParameterSet = SqlHelperParameterCache.GetSpParameterSet((SqlConnection)transaction.Connection, spName);
                AssignParameterValues(spParameterSet, parameterValues);
                IDataParameter[] commandParameters = spParameterSet;
                return ExecuteScalarAsync(commandTimeout, transaction, CommandType.StoredProcedure, spName, commandParameters);
            }

            return ExecuteScalarAsync(commandTimeout, transaction, CommandType.StoredProcedure, spName);
        }

        public override void FillDataset(int commandTimeout, DbConnection connection, CommandType commandType, string commandText, DataSet dataSet, string[] tableNames)
        {
            FillDataset(commandTimeout, connection, commandType, commandText, dataSet, tableNames, (IDataParameter[])null);
        }

        public override void FillDataset(int commandTimeout, DbConnection connection, CommandType commandType, string commandText, DataSet dataSet, string[] tableNames, params IDataParameter[] commandParameters)
        {
            FillDataset(commandTimeout, (SqlConnection)connection, null, commandType, commandText, dataSet, tableNames, (SqlParameter[])commandParameters);
        }

        public override void FillDataset(int commandTimeout, DbConnection connection, string spName, DataSet dataSet, string[] tableNames, params object[] parameterValues)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            if (dataSet == null)
            {
                throw new ArgumentNullException("dataSet");
            }

            if (spName == null || spName.Length == 0)
            {
                throw new ArgumentNullException("spName");
            }

            if (parameterValues != null && parameterValues.Length != 0)
            {
                SqlParameter[] spParameterSet = SqlHelperParameterCache.GetSpParameterSet((SqlConnection)connection, spName);
                AssignParameterValues(spParameterSet, parameterValues);
                IDataParameter[] commandParameters = spParameterSet;
                FillDataset(commandTimeout, connection, CommandType.StoredProcedure, spName, dataSet, tableNames, commandParameters);
            }
            else
            {
                FillDataset(commandTimeout, connection, CommandType.StoredProcedure, spName, dataSet, tableNames);
            }
        }

        public override void FillDataset(int commandTimeout, DbTransaction transaction, CommandType commandType, string commandText, DataSet dataSet, string[] tableNames)
        {
            FillDataset(commandTimeout, transaction, commandType, commandText, dataSet, tableNames, (IDataParameter[])null);
        }

        public override void FillDataset(int commandTimeout, DbTransaction transaction, CommandType commandType, string commandText, DataSet dataSet, string[] tableNames, params IDataParameter[] commandParameters)
        {
            FillDataset(commandTimeout, (SqlConnection)transaction.Connection, (SqlTransaction)transaction, commandType, commandText, dataSet, tableNames, (SqlParameter[])commandParameters);
        }

        public override void FillDataset(int commandTimeout, DbTransaction transaction, string spName, DataSet dataSet, string[] tableNames, params object[] parameterValues)
        {
            if (transaction == null)
            {
                throw new ArgumentNullException("transaction");
            }

            if (transaction != null && transaction.Connection == null)
            {
                throw new ArgumentException("The transaction was rollbacked or committed, please provide an open transaction.", "transaction");
            }

            if (dataSet == null)
            {
                throw new ArgumentNullException("dataSet");
            }

            if (spName == null || spName.Length == 0)
            {
                throw new ArgumentNullException("spName");
            }

            if (parameterValues != null && parameterValues.Length != 0)
            {
                SqlParameter[] spParameterSet = SqlHelperParameterCache.GetSpParameterSet((SqlConnection)transaction.Connection, spName);
                AssignParameterValues(spParameterSet, parameterValues);
                IDataParameter[] commandParameters = spParameterSet;
                FillDataset(commandTimeout, transaction, CommandType.StoredProcedure, spName, dataSet, tableNames, commandParameters);
            }
            else
            {
                FillDataset(commandTimeout, transaction, CommandType.StoredProcedure, spName, dataSet, tableNames);
            }
        }

        private static void FillDataset(int commandTimeout, SqlConnection connection, SqlTransaction transaction, CommandType commandType, string commandText, DataSet dataSet, string[] tableNames, params SqlParameter[] commandParameters)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            if (dataSet == null)
            {
                throw new ArgumentNullException("dataSet");
            }

            SqlCommand sqlCommand = new SqlCommand();
            PrepareCommand(commandTimeout, sqlCommand, connection, transaction, commandType, commandText, commandParameters, out var mustCloseConnection);
            using (SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(sqlCommand))
            {
                if (tableNames != null && tableNames.Length != 0)
                {
                    string text = "Table";
                    for (int i = 0; i < tableNames.Length; i++)
                    {
                        if (tableNames[i] == null || tableNames[i].Length == 0)
                        {
                            throw new ArgumentException("The tableNames parameter must contain a list of tables, a value was provided as null or empty string.", "tableNames");
                        }

                        sqlDataAdapter.TableMappings.Add(text, tableNames[i]);
                        text += i + 1;
                    }
                }

                sqlDataAdapter.Fill(dataSet);
                sqlCommand.Parameters.Clear();
            }

            if (mustCloseConnection)
            {
                connection.Close();
            }
        }

        public override void UpdateDataset(DbCommand insertCommand, DbCommand deleteCommand, DbCommand updateCommand, DataSet dataSet, string tableName)
        {
            if (insertCommand == null)
            {
                throw new ArgumentNullException("insertCommand");
            }

            if (deleteCommand == null)
            {
                throw new ArgumentNullException("deleteCommand");
            }

            if (updateCommand == null)
            {
                throw new ArgumentNullException("updateCommand");
            }

            if (tableName == null || tableName.Length == 0)
            {
                throw new ArgumentNullException("tableName");
            }

            using SqlDataAdapter sqlDataAdapter = new SqlDataAdapter
            {
                UpdateCommand = (SqlCommand)updateCommand,
                InsertCommand = (SqlCommand)insertCommand,
                DeleteCommand = (SqlCommand)deleteCommand
            };
            sqlDataAdapter.Update(dataSet, tableName);
            dataSet.AcceptChanges();
        }

        public override DbCommand CreateCommand(DbConnection connection, string spName, params string[] sourceColumns)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            if (spName == null || spName.Length == 0)
            {
                throw new ArgumentNullException("spName");
            }

            SqlCommand sqlCommand = new SqlCommand(spName, (SqlConnection)connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            if (sourceColumns != null && sourceColumns.Length != 0)
            {
                SqlParameter[] spParameterSet = SqlHelperParameterCache.GetSpParameterSet((SqlConnection)connection, spName);
                for (int i = 0; i < sourceColumns.Length; i++)
                {
                    spParameterSet[i].SourceColumn = sourceColumns[i];
                }

                AttachParameters(sqlCommand, spParameterSet);
            }

            return sqlCommand;
        }

        public override int ExecuteNonQueryTypedParams(int commandTimeout, DbConnection connection, string spName, DataRow dataRow)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            if (spName == null || spName.Length == 0)
            {
                throw new ArgumentNullException("spName");
            }

            if (dataRow != null && dataRow.ItemArray.Length != 0)
            {
                SqlParameter[] spParameterSet = SqlHelperParameterCache.GetSpParameterSet((SqlConnection)connection, spName);
                AssignParameterValues(spParameterSet, dataRow);
                IDataParameter[] commandParameters = spParameterSet;
                return ExecuteNonQuery(commandTimeout, connection, CommandType.StoredProcedure, spName, commandParameters);
            }

            return ExecuteNonQuery(commandTimeout, connection, CommandType.StoredProcedure, spName);
        }

        public override int ExecuteNonQueryTypedParams(int commandTimeout, DbTransaction transaction, string spName, DataRow dataRow)
        {
            if (transaction == null)
            {
                throw new ArgumentNullException("transaction");
            }

            if (transaction != null && transaction.Connection == null)
            {
                throw new ArgumentException("The transaction was rollbacked or committed, please provide an open transaction.", "transaction");
            }

            if (spName == null || spName.Length == 0)
            {
                throw new ArgumentNullException("spName");
            }

            if (dataRow != null && dataRow.ItemArray.Length != 0)
            {
                SqlParameter[] spParameterSet = SqlHelperParameterCache.GetSpParameterSet((SqlConnection)transaction.Connection, spName);
                AssignParameterValues(spParameterSet, dataRow);
                IDataParameter[] commandParameters = spParameterSet;
                return ExecuteNonQuery(commandTimeout, transaction, CommandType.StoredProcedure, spName, commandParameters);
            }

            return ExecuteNonQuery(commandTimeout, transaction, CommandType.StoredProcedure, spName);
        }

        public override Task<int> ExecuteNonQueryTypedParamsAsync(int commandTimeout, DbConnection connection, string spName, DataRow dataRow)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            if (spName == null || spName.Length == 0)
            {
                throw new ArgumentNullException("spName");
            }

            if (dataRow != null && dataRow.ItemArray.Length != 0)
            {
                SqlParameter[] spParameterSet = SqlHelperParameterCache.GetSpParameterSet((SqlConnection)connection, spName);
                AssignParameterValues(spParameterSet, dataRow);
                IDataParameter[] commandParameters = spParameterSet;
                return ExecuteNonQueryAsync(commandTimeout, connection, CommandType.StoredProcedure, spName, commandParameters);
            }

            return ExecuteNonQueryAsync(commandTimeout, connection, CommandType.StoredProcedure, spName);
        }

        public override Task<int> ExecuteNonQueryTypedParamsAsync(int commandTimeout, DbTransaction transaction, string spName, DataRow dataRow)
        {
            if (transaction == null)
            {
                throw new ArgumentNullException("transaction");
            }

            if (transaction != null && transaction.Connection == null)
            {
                throw new ArgumentException("The transaction was rollbacked or committed, please provide an open transaction.", "transaction");
            }

            if (spName == null || spName.Length == 0)
            {
                throw new ArgumentNullException("spName");
            }

            if (dataRow != null && dataRow.ItemArray.Length != 0)
            {
                SqlParameter[] spParameterSet = SqlHelperParameterCache.GetSpParameterSet((SqlConnection)transaction.Connection, spName);
                AssignParameterValues(spParameterSet, dataRow);
                IDataParameter[] commandParameters = spParameterSet;
                return ExecuteNonQueryAsync(commandTimeout, transaction, CommandType.StoredProcedure, spName, commandParameters);
            }

            return ExecuteNonQueryAsync(commandTimeout, transaction, CommandType.StoredProcedure, spName);
        }

        public override DataSet ExecuteDatasetTypedParams(int commandTimeout, DbConnection connection, string spName, DataRow dataRow)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            if (spName == null || spName.Length == 0)
            {
                throw new ArgumentNullException("spName");
            }

            if (dataRow != null && dataRow.ItemArray.Length != 0)
            {
                SqlParameter[] spParameterSet = SqlHelperParameterCache.GetSpParameterSet((SqlConnection)connection, spName);
                AssignParameterValues(spParameterSet, dataRow);
                IDataParameter[] commandParameters = spParameterSet;
                return ExecuteDataset(commandTimeout, connection, CommandType.StoredProcedure, spName, commandParameters);
            }

            return ExecuteDataset(commandTimeout, connection, CommandType.StoredProcedure, spName);
        }

        public override DataSet ExecuteDatasetTypedParams(int commandTimeout, DbTransaction transaction, string spName, DataRow dataRow)
        {
            if (transaction == null)
            {
                throw new ArgumentNullException("transaction");
            }

            if (transaction != null && transaction.Connection == null)
            {
                throw new ArgumentException("The transaction was rollbacked or committed, please provide an open transaction.", "transaction");
            }

            if (spName == null || spName.Length == 0)
            {
                throw new ArgumentNullException("spName");
            }

            if (dataRow != null && dataRow.ItemArray.Length != 0)
            {
                SqlParameter[] spParameterSet = SqlHelperParameterCache.GetSpParameterSet((SqlConnection)transaction.Connection, spName);
                AssignParameterValues(spParameterSet, dataRow);
                IDataParameter[] commandParameters = spParameterSet;
                return ExecuteDataset(commandTimeout, transaction, CommandType.StoredProcedure, spName, commandParameters);
            }

            return ExecuteDataset(commandTimeout, transaction, CommandType.StoredProcedure, spName);
        }

        public override Task<DataSet> ExecuteDatasetTypedParamsAsync(int commandTimeout, DbConnection connection, string spName, DataRow dataRow)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            if (spName == null || spName.Length == 0)
            {
                throw new ArgumentNullException("spName");
            }

            if (dataRow != null && dataRow.ItemArray.Length != 0)
            {
                SqlParameter[] spParameterSet = SqlHelperParameterCache.GetSpParameterSet((SqlConnection)connection, spName);
                AssignParameterValues(spParameterSet, dataRow);
                IDataParameter[] commandParameters = spParameterSet;
                return ExecuteDatasetAsync(commandTimeout, connection, CommandType.StoredProcedure, spName, commandParameters);
            }

            return ExecuteDatasetAsync(commandTimeout, connection, CommandType.StoredProcedure, spName);
        }

        public override Task<DataSet> ExecuteDatasetTypedParamsAsync(int commandTimeout, DbTransaction transaction, string spName, DataRow dataRow)
        {
            if (transaction == null)
            {
                throw new ArgumentNullException("transaction");
            }

            if (transaction != null && transaction.Connection == null)
            {
                throw new ArgumentException("The transaction was rollbacked or committed, please provide an open transaction.", "transaction");
            }

            if (spName == null || spName.Length == 0)
            {
                throw new ArgumentNullException("spName");
            }

            if (dataRow != null && dataRow.ItemArray.Length != 0)
            {
                SqlParameter[] spParameterSet = SqlHelperParameterCache.GetSpParameterSet((SqlConnection)transaction.Connection, spName);
                AssignParameterValues(spParameterSet, dataRow);
                IDataParameter[] commandParameters = spParameterSet;
                return ExecuteDatasetAsync(commandTimeout, transaction, CommandType.StoredProcedure, spName, commandParameters);
            }

            return ExecuteDatasetAsync(commandTimeout, transaction, CommandType.StoredProcedure, spName);
        }

        public override IDataReader ExecuteReaderTypedParams(int commandTimeout, DbConnection connection, string spName, DataRow dataRow)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            if (spName == null || spName.Length == 0)
            {
                throw new ArgumentNullException("spName");
            }

            if (dataRow != null && dataRow.ItemArray.Length != 0)
            {
                SqlParameter[] spParameterSet = SqlHelperParameterCache.GetSpParameterSet((SqlConnection)connection, spName);
                AssignParameterValues(spParameterSet, dataRow);
                IDataParameter[] commandParameters = spParameterSet;
                return ExecuteReader(commandTimeout, connection, CommandType.StoredProcedure, spName, commandParameters);
            }

            return ExecuteReader(commandTimeout, connection, CommandType.StoredProcedure, spName);
        }

        public override IDataReader ExecuteReaderTypedParams(int commandTimeout, DbTransaction transaction, string spName, DataRow dataRow)
        {
            if (transaction == null)
            {
                throw new ArgumentNullException("transaction");
            }

            if (transaction != null && transaction.Connection == null)
            {
                throw new ArgumentException("The transaction was rollbacked or committed, please provide an open transaction.", "transaction");
            }

            if (spName == null || spName.Length == 0)
            {
                throw new ArgumentNullException("spName");
            }

            if (dataRow != null && dataRow.ItemArray.Length != 0)
            {
                SqlParameter[] spParameterSet = SqlHelperParameterCache.GetSpParameterSet((SqlConnection)transaction.Connection, spName);
                AssignParameterValues(spParameterSet, dataRow);
                IDataParameter[] commandParameters = spParameterSet;
                return ExecuteReader(commandTimeout, transaction, CommandType.StoredProcedure, spName, commandParameters);
            }

            return ExecuteReader(commandTimeout, transaction, CommandType.StoredProcedure, spName);
        }

        public override Task<IDataReader> ExecuteReaderTypedParamsAsync(int commandTimeout, DbConnection connection, string spName, DataRow dataRow)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            if (spName == null || spName.Length == 0)
            {
                throw new ArgumentNullException("spName");
            }

            if (dataRow != null && dataRow.ItemArray.Length != 0)
            {
                SqlParameter[] spParameterSet = SqlHelperParameterCache.GetSpParameterSet((SqlConnection)connection, spName);
                AssignParameterValues(spParameterSet, dataRow);
                IDataParameter[] commandParameters = spParameterSet;
                return ExecuteReaderAsync(commandTimeout, connection, CommandType.StoredProcedure, spName, commandParameters);
            }

            return ExecuteReaderAsync(commandTimeout, connection, CommandType.StoredProcedure, spName);
        }

        public override Task<IDataReader> ExecuteReaderTypedParamsAsync(int commandTimeout, DbTransaction transaction, string spName, DataRow dataRow)
        {
            if (transaction == null)
            {
                throw new ArgumentNullException("transaction");
            }

            if (transaction != null && transaction.Connection == null)
            {
                throw new ArgumentException("The transaction was rollbacked or committed, please provide an open transaction.", "transaction");
            }

            if (spName == null || spName.Length == 0)
            {
                throw new ArgumentNullException("spName");
            }

            if (dataRow != null && dataRow.ItemArray.Length != 0)
            {
                SqlParameter[] spParameterSet = SqlHelperParameterCache.GetSpParameterSet((SqlConnection)transaction.Connection, spName);
                AssignParameterValues(spParameterSet, dataRow);
                IDataParameter[] commandParameters = spParameterSet;
                return ExecuteReaderAsync(commandTimeout, transaction, CommandType.StoredProcedure, spName, commandParameters);
            }

            return ExecuteReaderAsync(commandTimeout, transaction, CommandType.StoredProcedure, spName);
        }

        public override object ExecuteScalarTypedParams(int commandTimeout, DbConnection connection, string spName, DataRow dataRow)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            if (spName == null || spName.Length == 0)
            {
                throw new ArgumentNullException("spName");
            }

            if (dataRow != null && dataRow.ItemArray.Length != 0)
            {
                SqlParameter[] spParameterSet = SqlHelperParameterCache.GetSpParameterSet((SqlConnection)connection, spName);
                AssignParameterValues(spParameterSet, dataRow);
                IDataParameter[] commandParameters = spParameterSet;
                return ExecuteScalar(commandTimeout, connection, CommandType.StoredProcedure, spName, commandParameters);
            }

            return ExecuteScalar(commandTimeout, connection, CommandType.StoredProcedure, spName);
        }

        public override object ExecuteScalarTypedParams(int commandTimeout, DbTransaction transaction, string spName, DataRow dataRow)
        {
            if (transaction == null)
            {
                throw new ArgumentNullException("transaction");
            }

            if (transaction != null && transaction.Connection == null)
            {
                throw new ArgumentException("The transaction was rollbacked or committed, please provide an open transaction.", "transaction");
            }

            if (spName == null || spName.Length == 0)
            {
                throw new ArgumentNullException("spName");
            }

            if (dataRow != null && dataRow.ItemArray.Length != 0)
            {
                SqlParameter[] spParameterSet = SqlHelperParameterCache.GetSpParameterSet((SqlConnection)transaction.Connection, spName);
                AssignParameterValues(spParameterSet, dataRow);
                IDataParameter[] commandParameters = spParameterSet;
                return ExecuteScalar(commandTimeout, transaction, CommandType.StoredProcedure, spName, commandParameters);
            }

            return ExecuteScalar(commandTimeout, transaction, CommandType.StoredProcedure, spName);
        }

        public override Task<object> ExecuteScalarTypedParamsAsync(int commandTimeout, DbConnection connection, string spName, DataRow dataRow)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            if (spName == null || spName.Length == 0)
            {
                throw new ArgumentNullException("spName");
            }

            if (dataRow != null && dataRow.ItemArray.Length != 0)
            {
                SqlParameter[] spParameterSet = SqlHelperParameterCache.GetSpParameterSet((SqlConnection)connection, spName);
                AssignParameterValues(spParameterSet, dataRow);
                IDataParameter[] commandParameters = spParameterSet;
                return ExecuteScalarAsync(commandTimeout, connection, CommandType.StoredProcedure, spName, commandParameters);
            }

            return ExecuteScalarAsync(commandTimeout, connection, CommandType.StoredProcedure, spName);
        }

        public override Task<object> ExecuteScalarTypedParamsAsync(int commandTimeout, DbTransaction transaction, string spName, DataRow dataRow)
        {
            if (transaction == null)
            {
                throw new ArgumentNullException("transaction");
            }

            if (transaction != null && transaction.Connection == null)
            {
                throw new ArgumentException("The transaction was rollbacked or committed, please provide an open transaction.", "transaction");
            }

            if (spName == null || spName.Length == 0)
            {
                throw new ArgumentNullException("spName");
            }

            if (dataRow != null && dataRow.ItemArray.Length != 0)
            {
                SqlParameter[] spParameterSet = SqlHelperParameterCache.GetSpParameterSet((SqlConnection)transaction.Connection, spName);
                AssignParameterValues(spParameterSet, dataRow);
                IDataParameter[] commandParameters = spParameterSet;
                return ExecuteScalarAsync(commandTimeout, transaction, CommandType.StoredProcedure, spName, commandParameters);
            }

            return ExecuteScalarAsync(commandTimeout, transaction, CommandType.StoredProcedure, spName);
        }
    }
}
