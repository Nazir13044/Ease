using System.Collections;
using System.Data;
using System.Data.SqlClient;

namespace Collecto.CoreAPI.TransactionManagement.DataAccess.SQL
{
    public sealed class SqlHelperParameterCache
    {
        private static readonly Hashtable paramCache = Hashtable.Synchronized(new Hashtable());

        private SqlHelperParameterCache()
        {
        }

        private static SqlParameter[] DiscoverSpParameterSet(SqlConnection connection, string spName, bool includeReturnValueParameter)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            if (spName == null || spName.Length == 0)
            {
                throw new ArgumentNullException("spName");
            }

            SqlCommand sqlCommand = new SqlCommand(spName, connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            connection.Open();
            SqlCommandBuilder.DeriveParameters(sqlCommand);
            connection.Close();
            if (!includeReturnValueParameter)
            {
                sqlCommand.Parameters.RemoveAt(0);
            }

            SqlParameter[] array = new SqlParameter[sqlCommand.Parameters.Count];
            sqlCommand.Parameters.CopyTo(array, 0);
            SqlParameter[] array2 = array;
            for (int i = 0; i < array2.Length; i++)
            {
                array2[i].Value = DBNull.Value;
            }

            return array;
        }

        private static SqlParameter[] CloneParameters(SqlParameter[] originalParameters)
        {
            SqlParameter[] array = new SqlParameter[originalParameters.Length];
            int i = 0;
            for (int num = originalParameters.Length; i < num; i++)
            {
                array[i] = (SqlParameter)((ICloneable)originalParameters[i]).Clone();
            }

            return array;
        }

        public static void CacheParameterSet(string connectionString, string commandText, params SqlParameter[] commandParameters)
        {
            if (connectionString == null || connectionString.Length == 0)
            {
                throw new ArgumentNullException("connectionString");
            }

            if (commandText == null || commandText.Length == 0)
            {
                throw new ArgumentNullException("commandText");
            }

            string key = connectionString + ":" + commandText;
            paramCache[key] = commandParameters;
        }

        public static SqlParameter[] GetCachedParameterSet(string connectionString, string commandText)
        {
            if (connectionString == null || connectionString.Length == 0)
            {
                throw new ArgumentNullException("connectionString");
            }

            if (commandText == null || commandText.Length == 0)
            {
                throw new ArgumentNullException("commandText");
            }

            string key = connectionString + ":" + commandText;
            if (!(paramCache[key] is SqlParameter[] originalParameters))
            {
                return null;
            }

            return CloneParameters(originalParameters);
        }

        public static SqlParameter[] GetSpParameterSet(string connectionString, string spName)
        {
            return GetSpParameterSet(connectionString, spName, includeReturnValueParameter: false);
        }

        public static SqlParameter[] GetSpParameterSet(string connectionString, string spName, bool includeReturnValueParameter)
        {
            if (connectionString == null || connectionString.Length == 0)
            {
                throw new ArgumentNullException("connectionString");
            }

            if (spName == null || spName.Length == 0)
            {
                throw new ArgumentNullException("spName");
            }

            using SqlConnection connection = new SqlConnection(connectionString);
            return GetSpParameterSetInternal(connection, spName, includeReturnValueParameter);
        }

        internal static SqlParameter[] GetSpParameterSet(SqlConnection connection, string spName)
        {
            return GetSpParameterSet(connection, spName, includeReturnValueParameter: false);
        }

        internal static SqlParameter[] GetSpParameterSet(SqlConnection connection, string spName, bool includeReturnValueParameter)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            using SqlConnection connection2 = (SqlConnection)((ICloneable)connection).Clone();
            return GetSpParameterSetInternal(connection2, spName, includeReturnValueParameter);
        }

        private static SqlParameter[] GetSpParameterSetInternal(SqlConnection connection, string spName, bool includeReturnValueParameter)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            if (spName == null || spName.Length == 0)
            {
                throw new ArgumentNullException("spName");
            }

            string key = connection.ConnectionString + ":" + spName + (includeReturnValueParameter ? ":include ReturnValue Parameter" : "");
            SqlParameter[] array = paramCache[key] as SqlParameter[];
            if (array == null)
            {
                SqlParameter[] array2 = DiscoverSpParameterSet(connection, spName, includeReturnValueParameter);
                paramCache[key] = array2;
                array = array2;
            }

            return CloneParameters(array);
        }
    }
}
