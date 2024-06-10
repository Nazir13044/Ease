using System.Data;
using System.Data.Common;
using System.Data.Odbc;

namespace Collecto.CoreAPI.TransactionManagement.DataAccess.Odbc
{
    public sealed class OdbcHelper : TransactionFactory
    {
        private enum OleDbConnectionOwnership
        {
            Internal,
            External
        }

        private static void AttachParameters(OdbcCommand command, OdbcParameter[] commandParameters)
        {
            if (command == null)
            {
                throw new ArgumentNullException("command");
            }

            if (commandParameters == null)
            {
                return;
            }

            foreach (OdbcParameter odbcParameter in commandParameters)
            {
                if (odbcParameter != null)
                {
                    if ((odbcParameter.Direction == ParameterDirection.InputOutput || odbcParameter.Direction == ParameterDirection.Input) && odbcParameter.Value == null)
                    {
                        odbcParameter.Value = DBNull.Value;
                    }

                    command.Parameters.Add(odbcParameter);
                }
            }
        }

        private static void AssignParameterValues(OdbcParameter[] commandParameters, DataRow dataRow)
        {
            if (commandParameters == null || dataRow == null)
            {
                return;
            }

            int num = 0;
            foreach (OdbcParameter odbcParameter in commandParameters)
            {
                if (odbcParameter.ParameterName == null || odbcParameter.ParameterName.Length <= 1)
                {
                    throw new Exception($"Please provide a valid parameter name on the parameter #{num}, the ParameterName property has the following value: '{odbcParameter.ParameterName}'.");
                }

                DataColumnCollection columns = dataRow.Table.Columns;
                string parameterName = odbcParameter.ParameterName;
                if (columns.IndexOf(parameterName.Substring(1, parameterName.Length - 1)) != -1)
                {
                    parameterName = odbcParameter.ParameterName;
                    odbcParameter.Value = dataRow[parameterName.Substring(1, parameterName.Length - 1)];
                }
                num++;
            }
        }

        private static void AssignParameterValues(OdbcParameter[] commandParameters, object[] parameterValues)
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
                if (parameterValues[i] is OdbcParameter)
                {
                    IDbDataParameter dbDataParameter = (OdbcParameter)parameterValues[i];
                    if (dbDataParameter.Value == null)
                    {
                        commandParameters[i].Value = DBNull.Value;
                    }
                    else
                    {
                        commandParameters[i].Value = dbDataParameter.Value;
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

        private static void AssignReturnValues(OdbcParameter[] commandParameters, object[] parameterValues)
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

        private static void PrepareCommand(int commandTimeout, OdbcCommand command, OdbcConnection connection, OdbcTransaction transaction, CommandType commandType, string commandText, OdbcParameter[] commandParameters, out bool mustCloseConnection)
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

            using OdbcCommand odbcCommand = new OdbcCommand();
            PrepareCommand(commandTimeout, odbcCommand, (OdbcConnection)connection, null, commandType, commandText, (OdbcParameter[])commandParameters, out var mustCloseConnection);
            int result = odbcCommand.ExecuteNonQuery();
            odbcCommand.Parameters.Clear();
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
                OdbcParameter[] spParameterSet = OdbcHelperParameterCache.GetSpParameterSet((OdbcConnection)connection, spName);
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

            using OdbcCommand odbcCommand = new OdbcCommand();
            PrepareCommand(commandTimeout, odbcCommand, (OdbcConnection)transaction.Connection, (OdbcTransaction)transaction, commandType, commandText, (OdbcParameter[])commandParameters, out var _);
            int result = odbcCommand.ExecuteNonQuery();
            odbcCommand.Parameters.Clear();
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
                OdbcParameter[] spParameterSet = OdbcHelperParameterCache.GetSpParameterSet((OdbcConnection)transaction.Connection, spName);
                AssignParameterValues(spParameterSet, parameterValues);
                OdbcTransaction transaction2 = (OdbcTransaction)transaction;
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

            using OdbcCommand odbcCommand = new OdbcCommand();
            PrepareCommand(commandTimeout, odbcCommand, (OdbcConnection)connection, null, commandType, commandText, (OdbcParameter[])commandParameters, out var mustCloseConnection);
            Task<int> result = odbcCommand.ExecuteNonQueryAsync();
            odbcCommand.Parameters.Clear();
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
                OdbcParameter[] spParameterSet = OdbcHelperParameterCache.GetSpParameterSet((OdbcConnection)connection, spName);
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

            using OdbcCommand odbcCommand = new OdbcCommand();
            PrepareCommand(commandTimeout, odbcCommand, (OdbcConnection)transaction.Connection, (OdbcTransaction)transaction, commandType, commandText, (OdbcParameter[])commandParameters, out var _);
            Task<int> result = odbcCommand.ExecuteNonQueryAsync();
            odbcCommand.Parameters.Clear();
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
                OdbcParameter[] spParameterSet = OdbcHelperParameterCache.GetSpParameterSet((OdbcConnection)transaction.Connection, spName);
                AssignParameterValues(spParameterSet, parameterValues);
                OdbcTransaction transaction2 = (OdbcTransaction)transaction;
                IDataParameter[] commandParameters = spParameterSet;
                Task<int> result = ExecuteNonQueryAsync(commandTimeout, transaction2, CommandType.StoredProcedure, spName, commandParameters);
                AssignReturnValues(spParameterSet, parameterValues);
                return result;
            }

            return ExecuteNonQueryAsync(commandTimeout, transaction, CommandType.StoredProcedure, spName);
        }

        public override DataSet ExecuteDataset(int commandTimeout, DbConnection connection, CommandType commandType, string commandText)
        {
            return ExecuteDataset(commandTimeout, (DbConnection)(OdbcConnection)connection, commandType, commandText, (IDataParameter[])null);
        }

        public override DataSet ExecuteDataset(int commandTimeout, DbConnection connection, CommandType commandType, string commandText, params IDataParameter[] commandParameters)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            OdbcCommand odbcCommand = new OdbcCommand();
            PrepareCommand(commandTimeout, odbcCommand, (OdbcConnection)connection, null, commandType, commandText, (OdbcParameter[])commandParameters, out var mustCloseConnection);
            DataSet dataSet = new DataSet();
            using (OdbcDataAdapter odbcDataAdapter = new OdbcDataAdapter(odbcCommand))
            {
                odbcDataAdapter.Fill(dataSet);
                odbcCommand.Parameters.Clear();
                if (mustCloseConnection)
                {
                    connection.Close();
                }
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
                OdbcParameter[] spParameterSet = OdbcHelperParameterCache.GetSpParameterSet((OdbcConnection)connection, spName);
                AssignParameterValues(spParameterSet, parameterValues);
                OdbcConnection connection2 = (OdbcConnection)connection;
                IDataParameter[] commandParameters = spParameterSet;
                return ExecuteDataset(commandTimeout, connection2, CommandType.StoredProcedure, spName, commandParameters);
            }

            return ExecuteDataset(commandTimeout, (OdbcConnection)connection, CommandType.StoredProcedure, spName);
        }

        public override DataSet ExecuteDataset(int commandTimeout, DbTransaction transaction, CommandType commandType, string commandText)
        {
            return ExecuteDataset(commandTimeout, (DbTransaction)(OdbcTransaction)transaction, commandType, commandText, (IDataParameter[])null);
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

            OdbcCommand odbcCommand = new OdbcCommand();
            PrepareCommand(commandTimeout, odbcCommand, (OdbcConnection)transaction.Connection, (OdbcTransaction)transaction, commandType, commandText, (OdbcParameter[])commandParameters, out var _);
            DataSet dataSet = new DataSet();
            using OdbcDataAdapter odbcDataAdapter = new OdbcDataAdapter(odbcCommand);
            odbcDataAdapter.Fill(dataSet);
            odbcCommand.Parameters.Clear();
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
                OdbcParameter[] spParameterSet = OdbcHelperParameterCache.GetSpParameterSet((OdbcConnection)transaction.Connection, spName);
                AssignParameterValues(spParameterSet, parameterValues);
                OdbcTransaction transaction2 = (OdbcTransaction)transaction;
                IDataParameter[] commandParameters = spParameterSet;
                return ExecuteDataset(commandTimeout, transaction2, CommandType.StoredProcedure, spName, commandParameters);
            }

            return ExecuteDataset(commandTimeout, transaction, CommandType.StoredProcedure, spName);
        }

        private static Task<DataSet> ExecuteAsyncDataset(int commandTimeout, DbConnection connection, DbTransaction transaction, CommandType commandType, string commandText, params IDataParameter[] commandParameters)
        {
            OdbcCommand odbcCommand = new OdbcCommand();
            bool mustCloseConnection;
            if (transaction != null)
            {
                PrepareCommand(commandTimeout, odbcCommand, (OdbcConnection)transaction.Connection, (OdbcTransaction)transaction, commandType, commandText, (OdbcParameter[])commandParameters, out mustCloseConnection);
            }
            else
            {
                PrepareCommand(commandTimeout, odbcCommand, (OdbcConnection)connection, null, commandType, commandText, (OdbcParameter[])commandParameters, out mustCloseConnection);
            }

            DataSet dataSet = new DataSet();
            using (OdbcDataAdapter odbcDataAdapter = new OdbcDataAdapter(odbcCommand))
            {
                odbcDataAdapter.Fill(dataSet);
                odbcCommand.Parameters.Clear();
                if (mustCloseConnection)
                {
                    connection.Close();
                }
            }

            return Task.FromResult(dataSet);
        }

        public override Task<DataSet> ExecuteDatasetAsync(int commandTimeout, DbConnection connection, CommandType commandType, string commandText)
        {
            return ExecuteDatasetAsync(commandTimeout, (DbConnection)(OdbcConnection)connection, commandType, commandText, (IDataParameter[])null);
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
                OdbcParameter[] spParameterSet = OdbcHelperParameterCache.GetSpParameterSet((OdbcConnection)connection, spName);
                AssignParameterValues(spParameterSet, parameterValues);
                OdbcConnection connection2 = (OdbcConnection)connection;
                IDataParameter[] commandParameters = spParameterSet;
                return ExecuteDatasetAsync(commandTimeout, connection2, CommandType.StoredProcedure, spName, commandParameters);
            }

            return ExecuteDatasetAsync(commandTimeout, (OdbcConnection)connection, CommandType.StoredProcedure, spName);
        }

        public override Task<DataSet> ExecuteDatasetAsync(int commandTimeout, DbTransaction transaction, CommandType commandType, string commandText)
        {
            return ExecuteDatasetAsync(commandTimeout, (DbTransaction)(OdbcTransaction)transaction, commandType, commandText, (IDataParameter[])null);
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
                OdbcParameter[] spParameterSet = OdbcHelperParameterCache.GetSpParameterSet((OdbcConnection)transaction.Connection, spName);
                AssignParameterValues(spParameterSet, parameterValues);
                OdbcTransaction transaction2 = (OdbcTransaction)transaction;
                IDataParameter[] commandParameters = spParameterSet;
                return ExecuteDatasetAsync(commandTimeout, transaction2, CommandType.StoredProcedure, spName, commandParameters);
            }

            return ExecuteDatasetAsync(commandTimeout, transaction, CommandType.StoredProcedure, spName);
        }

        private static IDataReader ExecuteReader(int commandTimeout, OdbcConnection connection, OdbcTransaction transaction, CommandType commandType, string commandText, OdbcParameter[] commandParameters, OleDbConnectionOwnership connectionOwnership)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            bool mustCloseConnection = false;
            OdbcCommand odbcCommand = new OdbcCommand();
            try
            {
                PrepareCommand(commandTimeout, odbcCommand, connection, transaction, commandType, commandText, commandParameters, out mustCloseConnection);
                OdbcDataReader result = ((connectionOwnership != OleDbConnectionOwnership.External) ? odbcCommand.ExecuteReader(CommandBehavior.CloseConnection) : odbcCommand.ExecuteReader());
                bool flag = true;
                foreach (OdbcParameter parameter in odbcCommand.Parameters)
                {
                    if (parameter.Direction != ParameterDirection.Input)
                    {
                        flag = false;
                    }
                }

                if (flag)
                {
                    odbcCommand.Parameters.Clear();
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
            return ExecuteReader(commandTimeout, (OdbcConnection)connection, null, commandType, commandText, (OdbcParameter[])commandParameters, OleDbConnectionOwnership.External);
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
                OdbcParameter[] spParameterSet = OdbcHelperParameterCache.GetSpParameterSet((OdbcConnection)connection, spName);
                AssignParameterValues(spParameterSet, parameterValues);
                IDataParameter[] commandParameters = spParameterSet;
                return ExecuteReader(commandTimeout, connection, CommandType.StoredProcedure, spName, commandParameters);
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

            return ExecuteReader(commandTimeout, (OdbcConnection)transaction.Connection, (OdbcTransaction)transaction, commandType, commandText, (OdbcParameter[])commandParameters, OleDbConnectionOwnership.External);
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
                OdbcParameter[] spParameterSet = OdbcHelperParameterCache.GetSpParameterSet((OdbcConnection)transaction.Connection, spName);
                AssignParameterValues(spParameterSet, parameterValues);
                IDataParameter[] commandParameters = spParameterSet;
                return ExecuteReader(commandTimeout, transaction, CommandType.StoredProcedure, spName, commandParameters);
            }

            return ExecuteReader(commandTimeout, transaction, CommandType.StoredProcedure, spName);
        }

        public override Task<IDataReader> ExecuteReaderAsync(int commandTimeout, DbConnection connection, CommandType commandType, string commandText)
        {
            return ExecuteReaderAsync(commandTimeout, connection, commandType, commandText, (IDataParameter[])null);
        }

        public override Task<IDataReader> ExecuteReaderAsync(int commandTimeout, DbConnection connection, CommandType commandType, string commandText, params IDataParameter[] commandParameters)
        {
            return ExecuteReaderAsync(commandTimeout, (OdbcConnection)connection, null, commandType, commandText, (OdbcParameter[])commandParameters, OleDbConnectionOwnership.External);
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
                OdbcParameter[] spParameterSet = OdbcHelperParameterCache.GetSpParameterSet((OdbcConnection)connection, spName);
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

            return ExecuteReaderAsync(commandTimeout, (OdbcConnection)transaction.Connection, (OdbcTransaction)transaction, commandType, commandText, (OdbcParameter[])commandParameters, OleDbConnectionOwnership.External);
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
                OdbcParameter[] spParameterSet = OdbcHelperParameterCache.GetSpParameterSet((OdbcConnection)transaction.Connection, spName);
                AssignParameterValues(spParameterSet, parameterValues);
                IDataParameter[] commandParameters = spParameterSet;
                return ExecuteReaderAsync(commandTimeout, transaction, CommandType.StoredProcedure, spName, commandParameters);
            }

            return ExecuteReaderAsync(commandTimeout, transaction, CommandType.StoredProcedure, spName);
        }

        private static async Task<IDataReader> ExecuteReaderAsync(int commandTimeout, OdbcConnection connection, OdbcTransaction transaction, CommandType commandType, string commandText, OdbcParameter[] commandParameters, OleDbConnectionOwnership connectionOwnership)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            bool mustCloseConnection = false;
            OdbcCommand cmd = new OdbcCommand();
            try
            {
                PrepareCommand(commandTimeout, cmd, connection, transaction, commandType, commandText, commandParameters, out mustCloseConnection);
                IDataReader result = ((connectionOwnership != OleDbConnectionOwnership.External) ? (await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection)) : (await cmd.ExecuteReaderAsync()));
                bool flag = true;
                foreach (OdbcParameter parameter in cmd.Parameters)
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

            using OdbcCommand odbcCommand = new OdbcCommand();
            PrepareCommand(commandTimeout, odbcCommand, (OdbcConnection)connection, null, commandType, commandText, (OdbcParameter[])commandParameters, out var mustCloseConnection);
            object? result = odbcCommand.ExecuteScalar();
            odbcCommand.Parameters.Clear();
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
                OdbcParameter[] spParameterSet = OdbcHelperParameterCache.GetSpParameterSet((OdbcConnection)connection, spName);
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

            using OdbcCommand odbcCommand = new OdbcCommand();
            PrepareCommand(commandTimeout, odbcCommand, (OdbcConnection)transaction.Connection, (OdbcTransaction)transaction, commandType, commandText, (OdbcParameter[])commandParameters, out var _);
            object? result = odbcCommand.ExecuteScalar();
            odbcCommand.Parameters.Clear();
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
                OdbcParameter[] spParameterSet = OdbcHelperParameterCache.GetSpParameterSet((OdbcConnection)transaction.Connection, spName);
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

            using OdbcCommand odbcCommand = new OdbcCommand();
            PrepareCommand(commandTimeout, odbcCommand, (OdbcConnection)connection, null, commandType, commandText, (OdbcParameter[])commandParameters, out var mustCloseConnection);
            Task<object?> result = odbcCommand.ExecuteScalarAsync();
            odbcCommand.Parameters.Clear();
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
                OdbcParameter[] spParameterSet = OdbcHelperParameterCache.GetSpParameterSet((OdbcConnection)connection, spName);
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

            using OdbcCommand odbcCommand = new OdbcCommand();
            PrepareCommand(commandTimeout, odbcCommand, (OdbcConnection)transaction.Connection, (OdbcTransaction)transaction, commandType, commandText, (OdbcParameter[])commandParameters, out var _);
            Task<object?> result = odbcCommand.ExecuteScalarAsync();
            odbcCommand.Parameters.Clear();
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
                OdbcParameter[] spParameterSet = OdbcHelperParameterCache.GetSpParameterSet((OdbcConnection)transaction.Connection, spName);
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
            FillDataset(commandTimeout, (OdbcConnection)connection, null, commandType, commandText, dataSet, tableNames, (OdbcParameter[])commandParameters);
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
                OdbcParameter[] spParameterSet = OdbcHelperParameterCache.GetSpParameterSet((OdbcConnection)connection, spName);
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
            FillDataset(commandTimeout, (OdbcConnection)transaction.Connection, (OdbcTransaction)transaction, commandType, commandText, dataSet, tableNames, (OdbcParameter[])commandParameters);
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
                OdbcParameter[] spParameterSet = OdbcHelperParameterCache.GetSpParameterSet((OdbcConnection)transaction.Connection, spName);
                AssignParameterValues(spParameterSet, parameterValues);
                IDataParameter[] commandParameters = spParameterSet;
                FillDataset(commandTimeout, transaction, CommandType.StoredProcedure, spName, dataSet, tableNames, commandParameters);
            }
            else
            {
                FillDataset(commandTimeout, transaction, CommandType.StoredProcedure, spName, dataSet, tableNames);
            }
        }

        private static void FillDataset(int commandTimeout, OdbcConnection connection, OdbcTransaction transaction, CommandType commandType, string commandText, DataSet dataSet, string[] tableNames, params OdbcParameter[] commandParameters)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            if (dataSet == null)
            {
                throw new ArgumentNullException("dataSet");
            }

            using OdbcCommand odbcCommand = new OdbcCommand();
            PrepareCommand(commandTimeout, odbcCommand, connection, transaction, commandType, commandText, commandParameters, out var mustCloseConnection);
            using (OdbcDataAdapter odbcDataAdapter = new OdbcDataAdapter(odbcCommand))
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

                        odbcDataAdapter.TableMappings.Add(text, tableNames[i]);
                        text += i + 1;
                    }
                }

                odbcDataAdapter.Fill(dataSet);
                odbcCommand.Parameters.Clear();
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

            OdbcCommand odbcCommand = new OdbcCommand(spName, (OdbcConnection)connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            if (sourceColumns != null && sourceColumns.Length != 0)
            {
                OdbcParameter[] spParameterSet = OdbcHelperParameterCache.GetSpParameterSet((OdbcConnection)connection, spName);
                for (int i = 0; i < sourceColumns.Length; i++)
                {
                    spParameterSet[i].SourceColumn = sourceColumns[i];
                }

                AttachParameters(odbcCommand, spParameterSet);
            }

            return odbcCommand;
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
                OdbcParameter[] spParameterSet = OdbcHelperParameterCache.GetSpParameterSet((OdbcConnection)connection, spName);
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
                OdbcParameter[] spParameterSet = OdbcHelperParameterCache.GetSpParameterSet((OdbcConnection)transaction.Connection, spName);
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
                OdbcParameter[] spParameterSet = OdbcHelperParameterCache.GetSpParameterSet((OdbcConnection)connection, spName);
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
                OdbcParameter[] spParameterSet = OdbcHelperParameterCache.GetSpParameterSet((OdbcConnection)transaction.Connection, spName);
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
                OdbcParameter[] spParameterSet = OdbcHelperParameterCache.GetSpParameterSet((OdbcConnection)connection, spName);
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
                OdbcParameter[] spParameterSet = OdbcHelperParameterCache.GetSpParameterSet((OdbcConnection)transaction.Connection, spName);
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
                OdbcParameter[] spParameterSet = OdbcHelperParameterCache.GetSpParameterSet((OdbcConnection)connection, spName);
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
                OdbcParameter[] spParameterSet = OdbcHelperParameterCache.GetSpParameterSet((OdbcConnection)transaction.Connection, spName);
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
                OdbcParameter[] spParameterSet = OdbcHelperParameterCache.GetSpParameterSet((OdbcConnection)connection, spName);
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
                OdbcParameter[] spParameterSet = OdbcHelperParameterCache.GetSpParameterSet((OdbcConnection)transaction.Connection, spName);
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
                OdbcParameter[] spParameterSet = OdbcHelperParameterCache.GetSpParameterSet((OdbcConnection)connection, spName);
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
                OdbcParameter[] spParameterSet = OdbcHelperParameterCache.GetSpParameterSet((OdbcConnection)transaction.Connection, spName);
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
                OdbcParameter[] spParameterSet = OdbcHelperParameterCache.GetSpParameterSet((OdbcConnection)connection, spName);
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
                OdbcParameter[] spParameterSet = OdbcHelperParameterCache.GetSpParameterSet((OdbcConnection)transaction.Connection, spName);
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
                OdbcParameter[] spParameterSet = OdbcHelperParameterCache.GetSpParameterSet((OdbcConnection)connection, spName);
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
                OdbcParameter[] spParameterSet = OdbcHelperParameterCache.GetSpParameterSet((OdbcConnection)transaction.Connection, spName);
                AssignParameterValues(spParameterSet, dataRow);
                IDataParameter[] commandParameters = spParameterSet;
                return ExecuteScalarAsync(commandTimeout, transaction, CommandType.StoredProcedure, spName, commandParameters);
            }

            return ExecuteScalarAsync(commandTimeout, transaction, CommandType.StoredProcedure, spName);
        }
    }
}
