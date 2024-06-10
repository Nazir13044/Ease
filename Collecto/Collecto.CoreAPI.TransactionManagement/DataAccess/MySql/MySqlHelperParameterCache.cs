using MySql.Data.MySqlClient;
using System.Collections;
using System.Data;

namespace Collecto.CoreAPI.TransactionManagement.DataAccess.MySql
{
    public sealed class MySqlHelperParameterCache
    {
        private static readonly Hashtable paramCache = Hashtable.Synchronized(new Hashtable());

        private MySqlHelperParameterCache()
        {
        }

        private static MySqlParameter[] DiscoverSpParameterSet(MySqlConnection connection, string spName, bool includeReturnValueParameter)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            if (spName == null || spName.Length == 0)
            {
                throw new ArgumentNullException("spName");
            }

            MySqlCommand mySqlCommand = new MySqlCommand(spName, connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            connection.Open();
            MySqlCommandBuilder.DeriveParameters(mySqlCommand);
            connection.Close();
            if (!includeReturnValueParameter)
            {
                mySqlCommand.Parameters.RemoveAt(0);
            }

            MySqlParameter[] array = new MySqlParameter[mySqlCommand.Parameters.Count];
            mySqlCommand.Parameters.CopyTo(array, 0);
            MySqlParameter[] array2 = array;
            for (int i = 0; i < array2.Length; i++)
            {
                array2[i].Value = DBNull.Value;
            }

            return array;
        }

        private static MySqlParameter[] CloneParameters(MySqlParameter[] originalParameters)
        {
            MySqlParameter[] array = new MySqlParameter[originalParameters.Length];
            int i = 0;
            for (int num = originalParameters.Length; i < num; i++)
            {
                array[i] = (MySqlParameter)((ICloneable)originalParameters[i]).Clone();
            }

            return array;
        }

        public static void CacheParameterSet(string connectionString, string commandText, params MySqlParameter[] commandParameters)
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

        public static MySqlParameter[] GetCachedParameterSet(string connectionString, string commandText)
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
            if (!(paramCache[key] is MySqlParameter[] originalParameters))
            {
                return null;
            }

            return CloneParameters(originalParameters);
        }

        public static MySqlParameter[] GetSpParameterSet(string connectionString, string spName)
        {
            return GetSpParameterSet(connectionString, spName, includeReturnValueParameter: false);
        }

        public static MySqlParameter[] GetSpParameterSet(string connectionString, string spName, bool includeReturnValueParameter)
        {
            if (connectionString == null || connectionString.Length == 0)
            {
                throw new ArgumentNullException("connectionString");
            }

            if (spName == null || spName.Length == 0)
            {
                throw new ArgumentNullException("spName");
            }

            using MySqlConnection connection = new MySqlConnection(connectionString);
            return GetSpParameterSetInternal(connection, spName, includeReturnValueParameter);
        }

        internal static MySqlParameter[] GetSpParameterSet(MySqlConnection connection, string spName)
        {
            return GetSpParameterSet(connection, spName, includeReturnValueParameter: false);
        }

        internal static MySqlParameter[] GetSpParameterSet(MySqlConnection connection, string spName, bool includeReturnValueParameter)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            using MySqlConnection connection2 = (MySqlConnection)((ICloneable)connection).Clone();
            return GetSpParameterSetInternal(connection2, spName, includeReturnValueParameter);
        }

        private static MySqlParameter[] GetSpParameterSetInternal(MySqlConnection connection, string spName, bool includeReturnValueParameter)
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
            MySqlParameter[] array = paramCache[key] as MySqlParameter[];
            if (array == null)
            {
                MySqlParameter[] array2 = DiscoverSpParameterSet(connection, spName, includeReturnValueParameter);
                paramCache[key] = array2;
                array = array2;
            }

            return CloneParameters(array);
        }
    }
}
