using Oracle.ManagedDataAccess.Client;
using System.Collections;
using System.Data;

namespace Collecto.CoreAPI.TransactionManagement.DataAccess.Oracle
{
    internal sealed class OracleHelperParameterCache
    {
        private static readonly Hashtable paramCache = Hashtable.Synchronized(new Hashtable());

        private OracleHelperParameterCache()
        {
        }

        private static OracleParameter[] DiscoverSpParameterSet(OracleConnection connection, string spName, bool includeReturnValueParameter)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            if (spName == null || spName.Length == 0)
            {
                throw new ArgumentNullException("spName");
            }

            OracleCommand oracleCommand = new OracleCommand(spName, connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            connection.Open();
            OracleCommandBuilder.DeriveParameters(oracleCommand);
            connection.Close();
            if (!includeReturnValueParameter)
            {
                oracleCommand.Parameters.RemoveAt(0);
            }

            OracleParameter[] array = new OracleParameter[oracleCommand.Parameters.Count];
            oracleCommand.Parameters.CopyTo(array, 0);
            OracleParameter[] array2 = array;
            for (int i = 0; i < array2.Length; i++)
            {
                array2[i].Value = DBNull.Value;
            }

            return array;
        }

        private static OracleParameter[] CloneParameters(OracleParameter[] originalParameters)
        {
            OracleParameter[] array = new OracleParameter[originalParameters.Length];
            int i = 0;
            for (int num = originalParameters.Length; i < num; i++)
            {
                array[i] = (OracleParameter)((ICloneable)originalParameters[i]).Clone();
            }

            return array;
        }

        public static void CacheParameterSet(string connectionString, string commandText, params OracleParameter[] commandParameters)
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

        public static OracleParameter[] GetCachedParameterSet(string connectionString, string commandText)
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
            if (!(paramCache[key] is OracleParameter[] originalParameters))
            {
                return null;
            }

            return CloneParameters(originalParameters);
        }

        public static OracleParameter[] GetSpParameterSet(string connectionString, string spName)
        {
            return GetSpParameterSet(connectionString, spName, includeReturnValueParameter: false);
        }

        public static OracleParameter[] GetSpParameterSet(string connectionString, string spName, bool includeReturnValueParameter)
        {
            if (connectionString == null || connectionString.Length == 0)
            {
                throw new ArgumentNullException("connectionString");
            }

            if (spName == null || spName.Length == 0)
            {
                throw new ArgumentNullException("spName");
            }

            using OracleConnection connection = new OracleConnection(connectionString);
            return GetSpParameterSetInternal(connection, spName, includeReturnValueParameter);
        }

        internal static OracleParameter[] GetSpParameterSet(OracleConnection connection, string spName)
        {
            return GetSpParameterSet(connection, spName, includeReturnValueParameter: false);
        }

        internal static OracleParameter[] GetSpParameterSet(OracleConnection connection, string spName, bool includeReturnValueParameter)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            using OracleConnection connection2 = (OracleConnection)((ICloneable)connection).Clone();
            return GetSpParameterSetInternal(connection2, spName, includeReturnValueParameter);
        }

        private static OracleParameter[] GetSpParameterSetInternal(OracleConnection connection, string spName, bool includeReturnValueParameter)
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
            OracleParameter[] array = paramCache[key] as OracleParameter[];
            if (array == null)
            {
                OracleParameter[] array2 = DiscoverSpParameterSet(connection, spName, includeReturnValueParameter);
                paramCache[key] = array2;
                array = array2;
            }

            return CloneParameters(array);
        }
    }
}
